# NetGSM SMS Entegrasyonu — İnceleme ve Tasarım Notu

**Belge kimliği:** ANL-002
**Son güncelleme:** 2026-09-06
**Kapsam:** SMS gönderim altyapısı (üyelik doğrulama kodu ve bildirimler)
**İlgili süreçler:** TEC.5 (tasarım), TEC.8 (entegrasyon), MAN.4 (risk)
**İlgili kararlar:** `KR-036`, `KR-037`, `KR-038`, `KR-042`

> **Sır uyarısı:** Bu belgede hiçbir kullanıcı adı, parola veya API anahtarı yer almaz.
> Erişim bilgileri geliştirmede .NET User Secrets, üretimde ortam değişkeni ile yönetilir
> (`KR-038`).

---

## 1. Mevcut sistemdeki uygulamanın incelenmesi

Mevcut HRMS'te SMS gönderimi `Api/Sms/NetGsm/NetGsmSmsApi.cs` sınıfı ile yapılmaktadır.
İncelemede tespit edilen sorunlar:

| # | Sorun | Etkisi |
|---|---|---|
| 1 | Kullanıcı kodu ve parola **kaynak koda gömülü** | Koda erişimi olan herkes SMS gönderebilir; parola sürüm geçmişinde kalıcıdır |
| 2 | İstek **GET** yöntemiyle, parola **URL sorgu dizesinde** gönderiliyor | Parola ara sunucu, güvenlik duvarı ve erişim loglarına düz metin olarak yazılır |
| 3 | Mesaj metni **URL kodlamasından geçirilmiyor** | `#`, `&`, `+` gibi karakterler mesajı bozar veya gönderimi engeller. Kod içindeki *"# işareti olan metni göndermeye çalıştığımda göndermedi"* notu bu sorunun sonucudur |
| 4 | `WebRequest` / `WebClient` sınıfları kullanılıyor | .NET 6+ ile kullanımdan kaldırıldı; bağlantı havuzu yönetimi yok |
| 5 | **Zaman aşımı tanımlı değil** | NetGSM yanıt vermezse istek belirsiz süre bekler, uygulama iş parçacığı bloke olur |
| 6 | **Yeniden deneme yok** | Geçici ağ hatasında doğrulama kodu hiç gitmez |
| 7 | Hata ayrıntısı yutuluyor; tüm hatalar tek bir `ERROR` değerine indirgeniyor | Sorun teşhis edilemez; "30 = yetkisiz" ile "50 = numara hatalı" ayırt edilemez |
| 8 | Başarı kontrolü `content.Contains("00 ")` ile yapılıyor | Mesaj içinde `"00 "` geçen bir hata yanıtı yanlışlıkla başarı sayılabilir |

Bu sınıf yeni sisteme **taşınmayacak**, sıfırdan yazılacaktır.

---

## 2. Kullanılacak servis: Standart SMS (`KR-042`)

NetGSM iki ayrı SMS servisi sunar: **Standart SMS** ve **OTP SMS**. Projede
**tüm gönderimler standart servis üzerinden** yapılacaktır.

**Gerekçeler:**

1. **Hesabımızda OTP paketi tanımlı değil.** OTP uç noktasına yapılan istek `60`
   ("Hesabınızda OTP SMS Paketi tanımlı değildir") hatasıyla döner. Yalnızca standart
   SMS hizmet paketimiz bulunmaktadır.
2. **Teslim süresi yeterli.** Kurumun hâlihazırda standart servisi kullanan diğer
   uygulamalarında SMS'ler en geç 1 dakika içinde ulaşmaktadır.
3. **Mükerrer filtresi bizim senaryomuzu etkilemez.** NetGSM dokümanına göre mükerrer
   filtresi *"bir saat içerisinde **aynı mesaj içeriğini** aynı numaraya"* göndermeye
   çalışıldığında devreye girer. Doğrulama kodu her gönderimde değiştiği için mesaj
   içeriği de her seferinde farklıdır; filtre tetiklenmez.
4. **Türkçe karakter desteği kazanılır.** OTP servisi Türkçe karakter desteklemez;
   standart servis destekler. Bildirim mesajları düzgün Türkçe yazılabilir.

**Sonuçta ortaya çıkan tek kalıntı risk** §6'da ele alınmıştır (kara liste).

---

## 3. Doğrulanmış API bilgileri

Bilgiler NetGSM API dokümanından (`docs/NetGSM/Netgsm_API_Document.pdf` — depo dışında,
2026-09-05 tarihli tam sürüm) alınmıştır.

### 3.1 Uç noktalar

| Amaç | Yöntem | Adres |
|---|---|---|
| **SMS gönderimi** | POST | `https://api.netgsm.com.tr/sms/rest/v2/send` |
| Gönderim raporu (durum sorgulama) | POST | `https://api.netgsm.com.tr/sms/rest/v2/report` |
| Mesaj uzunluğu / boy hesaplama | POST | `https://api.netgsm.com.tr/sms/rest/v2/length` |
| Gönderici adı (başlık) sorgulama | GET | `https://api.netgsm.com.tr/sms/rest/v2/msgheader` |
| Kredi bakiyesi sorgulama | — | `https://api.netgsm.com.tr/balance` |
| *(Kullanılmayacak)* OTP gönderimi | POST | `https://api.netgsm.com.tr/sms/rest/v2/otp` |
| *(Kullanılmayacak)* Eski XML uç noktası | POST | `https://api.netgsm.com.tr/sms/send/xml` |

### 3.2 Kimlik doğrulama

**HTTP Basic Authentication.** Kullanıcı adı (abone numarası) ve alt kullanıcı parolası
Base64 kodlanarak `Authorization` başlığına eklenir.

> **Not:** API için **IP kısıtlaması uygulanmayacaktır** (kurum kararı). Bu, erişim
> bilgilerinin tek koruma katmanı olduğu anlamına gelir; `KR-038` kapsamındaki sır
> yönetimi bu nedenle daha da önemlidir. İleride üretim sunucusunun sabit çıkış IP'si
> netleştiğinde panelden IP kısıtlaması açılması ek bir güvenlik katmanı sağlar.

### 3.3 İstek gövdesi

```json
{
  "msgheader": "DUZEN",
  "messages": [
    { "msg": "DSG-HRMS doğrulama kodunuz: 123456", "no": "5XXXXXXXXX" }
  ],
  "encoding": "TR",
  "iysfilter": "0",
  "appname": "DSG-HRMS"
}
```

| Parametre | Zorunlu | Bizdeki değer / kural |
|---|---|---|
| `msgheader` | Evet | **`DUZEN`** — API kullanıcısında tanımlı gönderici adı. Her istekte gönderilecek |
| `messages[].no` | Evet | `5XXXXXXXXX` biçiminde, başında sıfır olmadan. Numara normalizasyonu uygulama tarafında yapılacak |
| `messages[].msg` | Evet | Mesaj metni |
| `encoding` | Hayır | **`TR`** — Türkçe karakter desteği |
| `iysfilter` | Hayır | **`0`** — bilgilendirme amaçlı içerik, İYS kontrolü yapılmaz. Personele ticari elektronik ileti gönderilmeyecektir |
| `appname` | Hayır | **`DSG-HRMS`** — NetGSM panelinde bu uygulamanın gönderimlerinin ayrıştırılabilmesi için |
| `referansID` | Hayır | Her istek için üretilecek tekil kimlik; sonradan takip ve mutabakat için |
| `startdate` / `stopdate` | Hayır | Kullanılmayacak (ileri tarihli gönderim yok) |

### 3.4 Karakter sınırları

| `encoding` | Toplam sınır | 1 SMS (boy) başına |
|---|---:|---:|
| `TR` | 883 | **150 karakter** |
| `UTF-8` | 912 | 155 karakter |
| `UNICODE` (emoji) | 391 | — |

> Türkçe gönderimde 1 SMS 155 değil **150** karakter üzerinden hesaplanır; 5 karakter
> Türkçe karakter desteği için harcanır. Doğrulama mesajı tek boya sığacak şekilde
> **150 karakterin altında** tutulacaktır.
>
> Örnek: `DSG-HRMS dogrulama kodunuz: 123456. Kod 5 dakika gecerlidir. Bu kodu kimseyle paylasmayin.` → 92 karakter.

### 3.5 Yanıtlar

**Başarılı:**
```json
{ "code": "00", "jobid": "17377215342605050417149344", "description": "queued" }
```

**Hatalı:**
```json
{ "code": "XX", "jobid": null, "description": "error message desc." }
```

### 3.6 Hata kodları ve uygulama davranışı

| Kod | Anlamı | Yeniden dene? | Uygulama davranışı |
|---|---|---|---|
| `00` | Başarılı (kuyruğa alındı) | — | `jobid` kaydedilir |
| `01` / `02` | Tarih düzeltildi | — | Bizde ileri tarihli gönderim yok, oluşmamalı |
| `20` | Mesaj metni veya boyu hatalı | ❌ | Kalıcı hata — kod hatası, uyarı üretilir |
| `30` | Geçersiz kullanıcı/parola, API izni yok veya IP kısıtı | ❌ | **Kritik** — yapılandırma hatası, yöneticiye bildirim |
| `40` | Gönderici adı (`DUZEN`) sistemde tanımlı değil | ❌ | **Kritik** — yapılandırma hatası |
| `50` / `51` | İYS ile ilgili hatalar | ❌ | `iysfilter=0` kullandığımız için oluşmamalı |
| `70` | Parametre hatalı veya zorunlu alan eksik | ❌ | Kalıcı hata — kod hatası |
| `80` | Gönderim sınırı aşıldı | ⏳ | Bekle ve sonra dene; kredi/kota kontrolü |
| `85` | Mükerrer gönderim sınırı: aynı numaraya **1 dakikada 20'den fazla** görev | ❌ | Bizim hız sınırlamamız bunun çok altında olacak; oluşursa kötüye kullanım işaretidir |
| Ağ hatası / 5xx | Geçici | ✅ | Üstel geri çekilme ile en fazla 2 kez yeniden dene |

---

## 4. Yeni entegrasyonun tasarımı

```
Application katmanı
   └── ISmsSender  (arayüz: GonderAsync(numara, mesaj, ct))
            │
Infrastructure katmanı
   ├── NetGsmSmsSender     → REST v2 /sms/rest/v2/send
   └── LoglayanSmsSender   → geliştirme/UAT ortamında SMS göndermez, loglar
```

**Uygulama ilkeleri:**

1. **Sağlayıcı bağımsızlığı.** Uygulama katmanı yalnızca `ISmsSender` arayüzünü bilir.
   Başka bir sağlayıcıya geçilmesi gerekirse tek bir sınıf değişir.
2. **`IHttpClientFactory`** kullanılacak; bağlantı havuzu ve DNS yenileme doğru yönetilecek.
3. **Zaman aşımı** tanımlı olacak (öneri: 10 saniye).
4. **Seçici yeniden deneme.** Yalnızca §3.6'da ✅ işaretli durumlarda, üstel geri
   çekilme ile. Kalıcı hatalarda yeniden denenmez.
5. **Hata kodları anlamlı istisnalara eşlenecek**; log kaydına kod ve açıklaması yazılacak.
   `30` ve `40` kodları yapılandırma hatası olduğu için ayrıca yöneticiye bildirim üretecek.
6. **Loglarda maskeleme** (`R-03`): telefon numarası kısmi maskelenir (`5XX***XX67`),
   **mesaj içeriği hiç loglanmaz** (doğrulama kodu içerir). Yalnızca sonuç kodu,
   `jobid` ve `referansID` kaydedilir.
7. **Numara normalizasyonu.** LOGO'daki telefon alanı serbest metindir; boşluk, parantez,
   `+90`, başında `0` gibi biçimler beklenir. Gönderim öncesi `5XXXXXXXXX` biçimine
   normalize edilecek; normalize edilemeyen numara için gönderim denenmeyecek,
   veri kalitesi uyarısı üretilecektir.
8. **Geliştirme ve UAT ortamında gerçek SMS gönderilmeyecek**; `LoglayanSmsSender`
   devreye girecek. Hem maliyet hem de yanlışlıkla gerçek personele mesaj gitmesi önlenir.
9. **Birim testleri** sahte HTTP yanıtlarıyla §3.6'daki tüm kodları kapsayacak;
   entegrasyon testlerinde gerçek gönderim yapılmayacaktır.
10. **Gönderim kaydı.** Her gönderim veritabanına yazılacak: kime (personel kimliği),
    ne zaman, hangi amaçla, sonuç kodu, `jobid`. Bu kayıt hem sorun teşhisi hem de
    KVKK erişim/işlem kaydı için gereklidir.

---

## 5. Mesaj başlığı ve içerik kuralları

- **Gönderici adı her gönderimde `DUZEN` olacaktır** (`msgheader` parametresi zorunlu).
- Doğrulama mesajı tek SMS boyuna sığacak (< 150 karakter).
- Mesajda kurum adı, kodun geçerlilik süresi ve "kimseyle paylaşmayın" uyarısı bulunacak.
- Personele **ticari elektronik ileti gönderilmeyecektir**; `iysfilter` daima `0`
  (bilgilendirme) olarak gönderilecektir. İYS yükümlülüğü doğmaz.

---

## 6. Kalıntı riskler

| Risk | Açıklama | Önlem |
|---|---|---|
| **Kara liste** | Standart servis kara liste filtresine tabidir. Bir personelin numarası geçmişte NetGSM kara listesine girdiyse (örneğin bir gönderime "RED" yanıtı verdiyse) doğrulama SMS'i ulaşmaz. OTP servisinde bu filtre uygulanmaz, ancak OTP paketimiz yoktur | (1) SMS ulaşmadığında kullanıcıya **e-posta ile doğrulama** seçeneği sunulacak. (2) Gönderim kaydı ve rapor sorgulama sayesinde ulaşmayan mesajlar tespit edilip İK'ya raporlanacak. (3) Kara liste, NetGSM panelinden veya `sms/blacklist` servisiyle sorgulanabilir |
| **IP kısıtlaması yok** | Erişim bilgileri ele geçirilirse herhangi bir yerden SMS gönderilebilir | Sır yönetimi (`KR-038`); ileride sabit çıkış IP'si netleşince panelden kısıtlama açılması önerilir |
| **Kredi tükenmesi** | Bakiye biterse doğrulama kodları gönderilemez ve kimse üye olamaz | Bakiye sorgulama servisi ile periyodik kontrol; eşik altına düşünce yöneticiye uyarı |

---

## 7. Açık madde kalmadı

| No | Konu | Sonuç |
|---|---|---|
| 1 | OTP paketi tanımlı mı? | **Hayır** — yalnızca standart SMS paketi var. Standart servis kullanılacak (`KR-042`) |
| 2 | `DUZEN` mesaj başlığı tanımlı mı? | **Evet** — her gönderimde `msgheader: "DUZEN"` gönderilecek |
| 3 | API için IP kısıtlaması uygulanacak mı? | **Hayır** — uygulanmayacak |
| 4 | REST v2 uç noktasının tam adresi | **Doğrulandı** — `https://api.netgsm.com.tr/sms/rest/v2/send` |

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-05 | 0.1 | NetGSM API dokümanı ve mevcut uygulama incelemesi | Bilgi İşlem |
| 2026-09-06 | 0.2 | Tam sürüm doküman incelendi; uç noktalar, parametreler, karakter sınırları ve hata kodları doğrulandı. Standart servis kararı (`KR-042`) işlendi; açık maddeler kapatıldı | Bilgi İşlem |
