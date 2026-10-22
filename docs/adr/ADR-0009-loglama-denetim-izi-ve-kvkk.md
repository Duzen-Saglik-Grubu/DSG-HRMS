# ADR-0009 — Loglama, Denetim İzi ve KVKK Kontrolleri

**Durum:** Kabul Edildi; §7 → ADR-0018
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-023`, `KR-118`, `KR-120`
**İlgili süreç:** TEC.5 (Tasarım), MAN.6 (Bilgi Yönetimi)
**İlgili riskler:** `R-03`

---

## Bağlam

Kurumun açık gereksinimi: *"Kullanıcı ve sistem işlemleri mutlaka loglanacak."*

Ancak İK verisi sıradan bir veri değildir. Düzen Sağlık Grubu personel verisi, KVKK
madde 6 anlamında **özel nitelikli kişisel veri** içerir: sağlık raporu, engellilik
durumu, adli sicil kaydı, kan grubu, sendika üyeliği. Bu, loglamayı hem zorunlu hem de
tehlikeli kılar: dikkatsiz bir log satırı, korunması gereken veriyi düz metin olarak
diske yazar.

## Karar

Üç ayrı kayıt türü tanımlanır ve **birbirine karıştırılmaz**.

| Tür | Amaç | Nerede | Saklama |
|---|---|---|---|
| **Uygulama logu** | Teknik teşhis (hata, performans, akış) | Dosya + konsol (Serilog) | 90 gün |
| **Denetim izi** | Kim, neyi, ne zaman **değiştirdi** | Veritabanı (`audit` şeması) | Uzun süreli (`KR-023`) |
| **Erişim kaydı** | Kim, kimin verisini **görüntüledi** | Veritabanı (`audit` şeması) | Uzun süreli |

### 1. Uygulama logu

- **Serilog**, yapılandırılmış (structured) log; JSON biçiminde.
- Her isteğe **korelasyon kimliği** (`TraceId`) eklenir; bir isteğin tüm log satırları
  ilişkilendirilebilir.
- Seviyeler: `Debug` yalnızca geliştirmede; üretimde `Information` ve üzeri.
- Log dosyaları günlük döner, boyut sınırı vardır.
- **Kişisel veri log'a yazılmaz** (§4).

### 2. Denetim izi (değişiklik kaydı)

EF Core `SaveChanges` **ara katmanında otomatik** üretilir; her serviste elle yazılmaz.

| Alan | İçerik |
|---|---|
| `occurred_at` | UTC zaman damgası |
| `user_account_id` | İşlemi yapan kullanıcı |
| `entity_name` | Değişen varlık |
| `entity_id` | Kayıt kimliği |
| `operation` | `Insert` / `Update` / `Delete` |
| `changes` | `jsonb` — değişen alanların **eski ve yeni** değerleri |
| `trace_id` | Uygulama logu ile ilişkilendirme |
| `ip_address` | İstek kaynağı |

**Kapsam:** Tüm iş varlıkları. Ayrıca rol/yetki değişiklikleri, hesap
aktifleştirme/pasifleştirme ve sistem yöneticisi rolüyle yapılan her eylem
(ADR-0007 §6) zorunlu olarak kaydedilir.

**Denetim kayıtları değiştirilemez ve silinemez.** Uygulama bu tabloya yalnızca ekleme
yapar; güncelleme ve silme uç noktası yoktur.

> **Gerçekleştirme notu (21.10.2026, `KR-118`, #244):** Değiştirilemezlik iki katmanla
> sağlanır. (1) Tetikleyici: `UPDATE`, `DELETE` ve `TRUNCATE` veritabanında reddedilir.
> (2) **Yetki:** uygulama veritabanına tabloların sahibi olmayan, süper kullanıcı olmayan
> `hrms_app` rolüyle bağlanır. Bu rol `audit` şemasında yalnızca okuma ve ekleme yapabilir;
> tetikleyiciyi kapatamaz, şemayı değiştiremez. Önceden uygulama süper kullanıcıyla
> bağlanıyordu ve tetikleyici tek korumaydı (tasarım gözden geçirmesi B-01, R-31). Rol
> `docker/postgres/app-role.sql` ile kurulur; migration'lar sahip rolle uygulanır. Yetkiler
> `ApplicationRoleTests` ile gerçek PostgreSQL'de, uygulamanın bu rolle çalıştığı e2e
> yığınında sınanır. Sahip rol ve süper kullanıcı yine tetikleyiciyi kapatabilir; bu yetki
> yalnızca dağıtımı yapan Bilgi İşlem'dedir.

### 3. Erişim kaydı (görüntüleme kaydı)

> **Bu, en sık atlanan ve KVKK denetiminde en sık sorulan kayıttır.** Değişiklik logu
> tek başına yeterli değildir: bir kullanıcının 400 personelin özlük dosyasını
> görüntülemesi hiçbir değişiklik üretmez, ancak ciddi bir olaydır.

Kaydedilecek olaylar:

| Olay | Örnek |
|---|---|
| Kişisel veri görüntüleme | Personel özlük kartının açılması |
| Özel nitelikli veri görüntüleme | Sağlık raporu, engellilik bilgisi |
| **Dışa aktarma** | Excel/PDF indirme — kaç kayıt, hangi filtre |
| Raporlama | Toplu rapor üretimi |
| Dosya indirme | Özlük dosyası eki |

Dışa aktarma kayıtlarında **kaç kişinin verisinin** dışarı çıktığı da tutulur; bu,
olası bir veri sızıntısı incelemesinin ilk sorusudur.

> **Gerçekleştirme notu (22.10.2026, #246):** Erişim kaydı iş işleminden **ayrı bir bağlam ve
> işlemle** yazılır (`AccessLogger`, `KR-061`). İş işlemi geri alınsa da görüntüleme kaydı
> kalır; kayıt yazılamazsa veri sunulmaz (fail-closed).

### 3.1 Kimlik olayı kaydı

*Gerçekleştirme (03.10.2026, `KR-094`, #119):* Giriş, kod, parola, oturum ve davet olayları
`audit.security_event` tablosunda tutulur (SYG-KMLK-060). Kayıt kişisel veri taşımaz. Olay
işle aynı işlemde yazılır, kayıt değiştirilemez (`KR-060`).

### 4. Maskeleme — zorunlu kural

Aşağıdaki alanlar **hiçbir log veya denetim kaydında düz metin olarak yer almaz**:

| Alan | Davranış |
|---|---|
| TCKN | Maskelenir: `123*****901` |
| IBAN / banka bilgisi | Maskelenir: `TR***1326` |
| Telefon | Kısmi maskelenir: `532*****67` (operatör ön eki görünür kalır) |
| E-posta | Kısmi maskelenir: `ab***@duzen.com.tr` |
| Parola, doğrulama kodu, jeton | **Hiç yazılmaz** — maskelenmez, tamamen dışlanır |
| Özel nitelikli veri içeriği | Denetim izinde değer değil, **"değişti" bilgisi** tutulur |
| SMS / e-posta içeriği | **Hiç yazılmaz** (doğrulama kodu içerebilir) |

Maskeleme, Serilog `destructuring` politikası ve denetim ara katmanında **merkezî**
olarak uygulanır. Her çağrı yerinde elle yapılmaz — unutulur.

Maskeleme kurallarının çalıştığı **birim testleriyle doğrulanır**; maskelenmemiş bir
alanın log'a düşmesi test hatası üretir.

> **Gerçekleştirme notu (21.10.2026, #244):** Erişim kaydındaki **serbest arama metni**
> (`q`, `Search`, `Term`, `Query` süzgeçleri) ayrı bir kuralla maskelenir. Metindeki e-posta
> adresleri ve en az 6 rakamlı diziler maskelenir: 11 rakam TCKN kuralıyla, cep telefonu
> telefon kuralıyla, diğerleri tamamen. Ad gibi düz metin okunur kalır, çünkü erişim kaydı
> "ne arandı" sorusunu cevaplamalıdır. Önceden süzgeç adı tanınmadığı için arama kutusuna
> yazılan TCKN düz metin kaydediliyordu (tasarım gözden geçirmesi B-07). Liste
> görüntülemesinde ekranda gösterilen kişilerin kimliklerinin kaydedilip kaydedilmeyeceği
> T1 tasarımında kararlaştırılır (#245).

> **Gerçekleştirme notu (21.10.2026, `KR-120`, #244):** Varlık alanlarının sınıflandırması
> **zorunludur.** Personel varlıklarının (`Dsg.Hrms.Domain.Personnel`) her metin ve tarih
> alanı tam olarak bir öznitelik taşır:
>
> | Öznitelik | Denetim izinde ve günlükte |
> |---|---|
> | `[PersonalData(tür)]` | Türün kuralıyla kısmen maskelenir (TCKN, telefon, e-posta) veya tamamen (`Unspecified`) |
> | `[SpecialCategoryData]` | **Değer hiç yazılmaz**; yalnızca alanın değiştiği görünür (kan grubu gibi KVKK md. 6 verisi) |
> | `[Secret]` | Hiç yazılmaz |
> | `[AuditVisible("gerekçe")]` | Bilerek açık yazılır; gerekçe zorunlu (ad, soyad, sicil, istihdam tarihleri) |
>
> Öznitelikler Domain katmanına taşındı (`Dsg.Hrms.Domain.Common`); önceden Application
> katmanındaydı ve Domain varlıkları onları göremiyordu. Maskeleme bu yüzden yalnızca ad
> listesine dayanıyordu ve listede olmayan bir ad (ör. `BloodType`, `Address`) düz metin
> yazılırdı (tasarım gözden geçirmesi B-02). `DataClassificationTests` işaretsiz alanı,
> gerekçesiz `[AuditVisible]`'ı ve hassas ada (`NationalId`, `Email`) verilmiş
> `[AuditVisible]`'ı yakalar. Ad listesi ikinci savunma hattı olarak kalır.

### 5. Özel nitelikli verinin saklanması

- Özel nitelikli alanlar veritabanında **uygulama seviyesinde şifrelenir**
  (kolon bazlı). Veritabanı yedeği ele geçse dahi bu alanlar okunamaz.
- Şifreleme anahtarı yapılandırmadan gelir (ADR-0008); veritabanında tutulmaz.
- Şifreli alanlar üzerinde arama yapılmaz; gerekiyorsa ayrı bir arama anahtarı
  (hash) tasarlanır.

### 6. Saklama süresi ve imha

`KR-023` uyarınca:

- Saklama süresi **veri türü bazında yapılandırılabilir** bir parametredir; koda
  gömülmez.
- Süre dolduğunda kayıt **arşivlenir veya anonimleştirilir**; ne yapılacağı veri
  türüne göre tanımlanır.
- Süreler başlangıçta **çok uzun** tanımlanacaktır; kurumun KVKK sorumlusundan görüş
  alındıkça daraltılabilir.
- İmha işlemleri kayıt altına alınır (ne, ne zaman, hangi kural gereği).

> **Karar (14.10.2026, `KR-109`):** Kurumun KVKK Sorumlusu denetim izi (`PRM-KVK-01`),
> erişim kaydı (`PRM-KVK-02`) ve kimlik güvenlik olayları için saklama süresini **10 yıl**
> olarak belirledi. Süre dolan kaydın imhası Y2 Denetim ve KVKK modülünde geliştirilir;
> o zamana kadar kayıt silinmez. Diğer bilgi kümelerinin süreleri bilgi kayıt defterindedir.

> **Not:** "Süresiz saklama" KVKK md. 7 karşısında savunulabilir bir konum değildir.
> Sistem, bugünkü ihtiyacı karşılarken gelecekteki uyum çalışmasını da kod değişikliği
> olmadan mümkün kılacak şekilde tasarlanmıştır.

### 7. Test ortamı verisi

~~UAT ortamına gerçek personel verisi kopyalanacaksa **maskelenerek** kopyalanır: TCKN,
telefon, e-posta ve özel nitelikli alanlar gerçek değerlerini taşımaz. Bu, KVKK'nın
"amaçla sınırlılık" ilkesinin gereğidir.~~

> **Yerini aldı → ADR-0018 (`KR-108`):** Yerel geliştirme ortamı ve UAT gerçek personel
> verisiyle çalışır, maskeleme yapılmaz. Gerekçe, korumalar ve kabul edilen risk (R-25,
> `KR-103`) ADR-0018'dedir. Bu bölüm #212'de gözden kaçmış, #215'te işaretlendi.

### 8. Gözlemlenebilirlik

- **OpenTelemetry** ile izleme (tracing) ve ölçüm (metrics) toplanır.
- `/health/live` ve `/health/ready` uç noktaları bulunur; LOGO erişimi, veritabanı ve
  NAS erişimi hazır olma kontrolüne dâhildir.
- Kritik olaylar (senkronizasyon hatası, yapılandırma hatası, jeton yeniden kullanımı,
  SMS/e-posta gönderim hatası) yöneticiye bildirim üretir.

> **Gerçekleştirme notu (22.10.2026, #246):** Yöneticiye bildirim **Y1 Bildirim Merkezi'ne
> ertelendi** (SYG-KMLK-010). Bugün bu olaylar günlüğe, güvenlik olayı kaydına ve ilgili
> kayıtlara yazılır; senkronizasyon ve bildirim kanallarının durumu `/health/sync` ve
> `/health/notifications` ile izlenir (tasarım gözden geçirmesi B-08).

## Gerekçe

- Üç kayıt türünün ayrılması, her birinin farklı saklama süresi, farklı erişim yetkisi
  ve farklı hacim özelliklerine sahip olmasından kaynaklanır. Tek bir "log" tablosunda
  toplamak hem performans hem uyum açısından yanlıştır.
- Erişim kaydı olmadan KVKK'nın veri sahibi başvurularına ("verilerimi kim gördü")
  cevap verilemez.
- Merkezî maskeleme, insan hatasına karşı tek etkili yöntemdir.
- Denetim kayıtlarının değiştirilemez olması, kaydın kanıt değeri taşıması için gereklidir
  (33061 MAN.6 ve MAN.8 açısından da anlamlıdır).

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| Tek birleşik log tablosu | Farklı saklama süreleri ve erişim yetkileri yönetilemez; hacim sorunları |
| Yalnızca değişiklik logu | Görüntüleme ve dışa aktarma izlenemez; KVKK açısından yetersiz |
| Veritabanı tetikleyicisi (trigger) ile denetim | Uygulama bağlamı (kullanıcı, IP, trace) yakalanamaz |
| Tüm veritabanını şifrelemek (TDE benzeri) | PostgreSQL'de yerleşik değil; ayrıca uygulama içi erişim yine açık |
| Log'a her şeyi yazıp sonra temizlemek | Yazıldığı an sızıntı gerçekleşmiş olur |

## Sonuçlar

**Olumlu:** KVKK denetiminde sorulacak soruların tamamına kanıtla cevap verilebilir;
veri sızıntısı incelemesi mümkün olur.

**Olumsuz / kabul edilen ödünler:**
- Erişim kaydı yüksek hacimlidir (her personel görüntüleme bir satır). Tabloların
  bölümlenmesi (partitioning) ve arşivleme planlanacaktır.
- Kolon bazlı şifreleme, o alanlarda arama ve sıralamayı imkânsız kılar; bu alanlar
  filtre olarak kullanılamaz.

**Yükümlülükler:**
- Maskeleme testleri yazılacak ve kalite kapısına dâhil edilecektir.
- Erişim kaydı üretilmesi gereken uç noktalar, kod gözden geçirme kontrol listesinde
  ayrı bir maddedir.
- Saklama sürelerinin kurumun KVKK sorumlusuyla netleştirilmesi açık bir iştir.

## Geri dönüş maliyeti

**Orta–Yüksek.** Denetim ve erişim kaydı baştan kurulmazsa geçmiş veri geri
üretilemez. Maskeleme ve şifreleme kararlarından dönmek veri göçü gerektirir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
| 2026-09-10 | 0.2 | §4 maskeleme biçimleri kesinleştirildi (telefon, IBAN); gerçekleştirme `KR-059` ile iki katmanlı yapıldı | Bilgi İşlem |
| 2026-10-03 | 0.3 | §3.1 kimlik olayı kaydı gerçekleştirme notu (`KR-094`, #119) | Bilgi İşlem |
| 2026-10-14 | 0.4 | §6: KVKK saklama süresi kararı (`KR-109`); §7 → ADR-0018 (`KR-108`) (#215) | Bilgi İşlem |
| 2026-10-21 | 0.5 | §2: denetim kayıtlarının değiştirilemezliğinin ikinci katmanı, sahip olmayan çalışma rolü (`KR-118`, #244) | Bilgi İşlem |
| 2026-10-21 | 0.6 | §4: serbest arama metninin maskelenmesi (#244, B-07) | Bilgi İşlem |
| 2026-10-21 | 0.7 | §4: varlık alanlarının zorunlu sınıflandırması; `[SpecialCategoryData]` ve `[AuditVisible]` (`KR-120`, #244) | Bilgi İşlem |
| 2026-10-22 | 0.8 | §3 erişim kaydının ayrı işlemi; §8 yönetici bildirimi Y1'e ertelendi (#246) | Bilgi İşlem |
