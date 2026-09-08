# ADR-0012 — Bildirim Altyapısı

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-036`, `KR-042`, `KR-044`
**İlgili süreç:** TEC.5 (Tasarım), TEC.8 (Entegrasyon)
**İlgili riskler:** `R-14`
**Ayrıntı:** `docs/analiz/02-netgsm-sms-entegrasyonu.md`

---

## Bağlam

Sistem üç kanaldan bildirim gönderecektir: e-posta, SMS ve uygulama içi. Kullanım
alanları: üyelik doğrulama kodu, izin talebi/onayı, eğitim duyuruları, sistem uyarıları.

Mevcut altyapı: kurum içi **Postfix** SMTP sunucusu ve **NetGSM** SMS servisi.

Mevcut sistemdeki uygulamada tespit edilen sorunlar (kimlik bilgilerinin koda gömülü
olması, zaman aşımı ve yeniden deneme olmaması, hata ayrıntısının yutulması)
tekrarlanmayacaktır.

## Karar

### 1. Kanal soyutlaması

```
Application katmanı
   └── INotificationService  ← modüller yalnızca bunu bilir
            │
            ├── IEmailSender          → SmtpEmailSender  /  LoglayanEmailSender
            ├── ISmsSender            → NetGsmSmsSender  /  LoglayanSmsSender
            └── IInAppNotifier        → veritabanı tabanlı
```

Modüller "e-posta gönder" demez; **"şu olayı bildir"** der. Hangi kanalların
kullanılacağına bildirim türü ve kullanıcı tercihi karar verir.

### 2. SMS — NetGSM standart servisi

| Konu | Karar |
|---|---|
| Uç nokta | `POST https://api.netgsm.com.tr/sms/rest/v2/send` |
| Kimlik doğrulama | HTTP Basic Auth |
| Gönderici adı | **`DUZEN`** — her istekte `msgheader` olarak |
| Kodlama | `encoding: "TR"` (Türkçe karakter) |
| İYS | `iysfilter: "0"` — bilgilendirme; ticari ileti gönderilmez |
| Uygulama adı | `appname: "DSG-HRMS"` — panelde ayrı filtrelenebilmesi için |
| OTP servisi | **Kullanılmayacak** — hesapta OTP paketi yok (`KR-042`) |

**Karakter sınırı:** `TR` kodlamasında 1 SMS **150 karakter** üzerinden hesaplanır.
Doğrulama mesajı tek boya sığacak şekilde yazılır.

Hata kodlarının tamamı ve her biri için "yeniden denenecek mi" kararı
`docs/analiz/02-netgsm-sms-entegrasyonu.md` §3.6'da tablolanmıştır.

### 3. E-posta — kurum içi Postfix

| Konu | Karar |
|---|---|
| Sunucu | Kurum içi Postfix (SMTP) |
| Kimlik bilgileri | Yapılandırmadan (ADR-0008); koda yazılmaz |
| Şablon | HTML + düz metin alternatifli; şablonlar kod dışında, yönetilebilir |
| Gönderen | Kurumsal İK adresi |
| Ekler | Yalnızca gerekli olduğunda; kişisel veri içeren ek gönderilmez |

### 4. Uygulama içi bildirim

- Veritabanında tutulur; kullanıcı arayüzünde okunmamış sayacı ile gösterilir.
- Okundu/okunmadı durumu, ilişkili kayda bağlantı ve tür bilgisi taşır.
- Kalıcı kayıttır; saklama süresi `KR-023` kapsamındadır.

### 5. Dayanıklılık kuralları

| Kural | Değer |
|---|---|
| Zaman aşımı | 10 saniye |
| Yeniden deneme | Yalnızca geçici hatalarda (ağ, 5xx), üstel geri çekilme, en fazla 2 kez |
| Kalıcı hatada | Yeniden denenmez; kayıt altına alınır |
| Yapılandırma hatası (`30`, `40`) | Yöneticiye bildirim üretir |
| Kütüphane | `Microsoft.Extensions.Http.Resilience` |

**Bildirim gönderimi ana işlemi bloke etmez.** İzin talebi kaydedilirken SMS
gönderilemezse talep yine de kaydedilir; bildirim kuyruğa alınır ve yeniden denenir.
Bildirim başarısızlığı iş işleminin başarısızlığı değildir.

### 6. Gönderim kaydı

Her gönderim veritabanına yazılır:

| Alan | İçerik |
|---|---|
| Alıcı | Personel kimliği (numara/adres **maskeli**) |
| Kanal | E-posta / SMS / uygulama içi |
| Amaç | Doğrulama kodu, izin bildirimi, … |
| Zaman | UTC |
| Sonuç | Kod + açıklama |
| Dış kimlik | NetGSM `jobid`, `referansID` |

**Mesaj içeriği kaydedilmez** — doğrulama kodu içerebilir (ADR-0009 §4).

Bu kayıt üç işe yarar: sorun teşhisi, ulaşmayan bildirimlerin tespiti (`R-14`) ve
KVKK işlem kaydı.

### 7. Test ve alt ortamlar

Geliştirme ve UAT ortamlarında `LoglayanSmsSender` ve `LoglayanEmailSender` devrededir:
gönderim yapılmaz, içerik log'a yazılır (kişisel veri maskeli). Böylece hem maliyet
oluşmaz hem de yanlışlıkla gerçek personele mesaj gitmez.

### 8. Numara ve adres normalizasyonu

LOGO'daki telefon alanı serbest metindir. Gönderim öncesi `5XXXXXXXXX` biçimine
normalize edilir. Normalize edilemeyen numara için gönderim **denenmez**; veri kalitesi
uyarısı üretilir ve İK raporuna düşer.

## Gerekçe

- Kanal soyutlaması, sağlayıcı değişikliğini tek sınıflık bir işe indirger ve
  modülleri bildirim ayrıntısından korur.
- Bildirim başarısızlığının iş işlemini bozmaması, İK süreçlerinin dış servis
  kesintisinden etkilenmemesini sağlar.
- Gönderim kaydı olmadan "SMS gitti mi" sorusuna cevap verilemez; bu, üyelik
  devreye alınırken en sık sorulacak sorudur.

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| NetGSM OTP servisi | Hesapta OTP paketi tanımlı değil |
| Eski XML uç noktası (`/sms/send/xml`) | REST v2 daha temiz ve belgeli; JSON ile çalışıyor |
| Mesajları eşzamanlı (senkron) göndermek | Dış servis gecikmesi kullanıcı işlemini bekletir |
| Harici kuyruk sistemi (RabbitMQ vb.) | Ek altyapı; veritabanı tabanlı kuyruk bu ölçek için yeterli |
| Bildirim içeriğini loglamak | Doğrulama kodu sızar |

## Sonuçlar

**Olumlu:** Sağlayıcıdan bağımsız, dayanıklı, izlenebilir bildirim altyapısı.

**Olumsuz / kabul edilen ödünler:**
- Standart SMS servisi kara liste filtresine tabidir; bir personelin numarası kara
  listedeyse SMS ulaşmaz (`R-14`). Telafi: kullanıcı e-posta ile doğrulamaya geçebilir.
- Veritabanı tabanlı kuyruk, yüksek hacimde harici kuyruk kadar verimli değildir;
  bu ölçekte sorun değildir.

**Yükümlülükler:**
- Tüm NetGSM hata kodları için birim testi yazılacaktır.
- Kredi bakiyesi periyodik kontrol edilecek; eşik altında yöneticiye uyarı verilecektir.

## Geri dönüş maliyeti

**Düşük.** Sağlayıcı değişikliği tek sınıf; kanal ekleme arayüz uygulaması eklemekten
ibarettir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
