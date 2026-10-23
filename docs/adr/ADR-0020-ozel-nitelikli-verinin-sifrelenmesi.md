# ADR-0020 — Özel Nitelikli Verinin Şifrelenmesi

**Durum:** **Kabul Edildi**
**Tarih:** 2026-10-22
**Karar defteri karşılığı:** `KR-122`, `KR-125`, `KR-126`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)
**Uyguladığı:** ADR-0009 §5 (özel nitelikli verinin saklanması)
**İlgili gereksinimler:** PG-PERS-36; `KR-113`, `KR-116` (kan grubu işleme koşulları), `KR-120`
**İlgili iş:** #245 maddeler 3 ve 4; tasarım gözden geçirmesi B-03

---

## Bağlam

T1 ile HRMS ilk kez özel nitelikli kişisel veri saklayacak: **kan grubu** (KVKK md. 6,
sağlık). KVKK Sorumlusunun belirlediği ilk işleme koşulu, verinin kriptografik yöntemle
şifrelenerek saklanmasıdır (`KR-116`). ADR-0009 §5 kolon bazlı şifrelemeyi öngörür ama
altyapısı yoktur:

- Var olan `AesGcmSecretProtector` yalnızca sır parametreler (SMTP, NetGSM parolası) için
  yazıldı. Tek anahtar kullanır, anahtar kimliği ve döndürme yoktur; anahtar tanımlı
  değilse uygulama yine açılır (B-03).
- Şifreli alanda veritabanı araması yapılamaz. İK ise kartta gösterilen her alanın
  filtrelenebilmesini istedi (PG-PERS-14).
- Yerel geliştirme ortamı ve UAT gerçek personel verisiyle çalışıyor (ADR-0018). Bu riski
  Üst Yönetim T3 verisi için kabul etmişti (R-25); o veride özel nitelikli veri yoktu.

## Karar

### 1. Şifreleme

- Özel nitelikli alanlar (`[SpecialCategoryData]`, `KR-120`) veritabanında **AES-256-GCM**
  ile şifrelenir.
- **Kan grubunda şifrelenen değer LOGO kodudur** (1–8; 23.10.2026, `KR-129`). Ad ekranda referans
  veriden okunur (ADR-0022 §1). Kod 0 "Belirtilmemiş" değer yok sayılır ve saklanmaz.
- **Şifreleme yeri (revizyon 1.2, B-02):** EF değer dönüştürücüsü kullanılmaz. Dönüştürücü
  yalnızca kolonun değerini görür, satırın başka bir alanını (kişinin `public_id`'sini)
  göremez; bu yüzden AAD'yi uygulayamaz. Özel nitelikli değerler ayrı bir tabloda tutulur
  (`person_sensitive`, ADR-0021 §1). Şifreleme ve çözme bu tablonun **depo katmanında**
  yapılır: depo değeri yazarken şifreler, okurken çözer. Uygulama kodu düz değeri görür,
  veritabanı yalnızca şifreli değeri görür.
- **Ayrı veri anahtarı** kullanılır (`PersonalDataProtection:Keys`). Sır parametre
  anahtarıyla paylaşılmaz: biri ele geçerse diğeri korunmaya devam eder.
- Şifreli değer **anahtar kimliği** taşır: `k<n>:<nonce>:<şifreli metin>`. Böylece yeni
  anahtara geçilirken eski değerler hâlâ çözülebilir.
- Her değer için rastgele nonce kullanılır; aynı kan grubu iki kişide farklı şifreli metin
  üretir. Veritabanına erişen biri değerleri gruplayamaz.
- **İlişkilendirilmiş veri (AAD):** Şifreleme **tablo adı + alan adı + kayıt sahibinin
  `public_id`'si** üçlüsüne bağlanır (revizyon 1.2, B-02). Bir kişinin şifreli değeri başka
  bir kişinin satırına veya başka bir kolona kopyalanırsa çözülemez. Düzeltme talebinde
  önerilen kan grubu da aynı kuralla, talebin tablosu ve kimliğiyle şifrelenir.

### 2. Anahtar zorunludur

- Özel nitelikli alan içeren bir model varsa ve anahtar tanımlı değilse **uygulama açılmaz**
  (açılış doğrulaması, ADR-0008). Sır parametrelerdeki "anahtar yoksa çalış" davranışı burada
  kabul edilmez: anahtarsız çalışmak ya düz metin yazmak ya da veriyi okuyamamak demektir.
- Anahtar sır dosyasında durur (UAT: `.env.uat`, BK-12; yerel: `docker/.env`, BK-16).
  Veritabanında, depoda ve günlükte yer almaz.

### 3. Döndürme ve kurtarma

- **Döndürme (revizyon 1.2, B-03):** Yeni anahtar yeni kimlikle eklenir ve yeni yazımlar
  onunla yapılır. Eski değerleri **açık bir yeniden şifreleme işi** yeni anahtarla yeniden
  yazar. Senkronizasyona güvenilmez, çünkü senkronizasyon yalnızca değişen değeri yazar.
  İş Sistem Yöneticisi tarafından başlatılır. Değer değişmediği için denetim izine "değişti"
  kaydı üretmez; bunun yerine işin kendisi tek bir güvenlik olayı olarak kaydedilir: kim
  başlattı, kaç kayıt, hangi anahtardan hangisine. Eski anahtar, onunla şifreli kayıt
  kalmayınca çıkarılır; iş bunu sayarak doğrular. Yordam runbook'a yazılır.
- **Anahtar kaybı:** LOGO kaynaktır. Anahtar kaybolursa alan temizlenir ve yeni anahtarla
  yeniden senkronize edilir; kan grubunda veri kaybı olmaz.
- **HRMS'te girilen özel nitelikli değer:** Tek örnek, düzeltme talebinde önerilen kan
  grubudur. Bu değer **talep kapatılınca silinir**; talepte yalnızca hangi alanın
  bildirildiği ve talebin durumu kalır (KVKK Sorumlusu kararı, 22.10.2026, `KR-126`). Açık
  talepler yeniden şifreleme işine dahildir. Anahtar kaybında açık taleplerin önerilen değeri
  kaybolur; personel talebi yeniden gönderir. Bu kabul edilen bir ödündür: değer kısa ömürlüdür
  ve yalnızca LOGO düzeltmesi için tutulur.

### 4. Arama ve filtre

- Şifreli alanda **veritabanı araması ve dizin yoktur.** Değerin deterministik özeti
  (blind index) de tutulmaz: kan grubunun sekiz değeri vardır, özet değeri gruplar ve tek bir
  bilinen kişiyle bütün grubun değeri çıkarılabilir. Bu, şifrelemeyi anlamsız kılar.
- Kan grubu filtresi **uygulama belleğinde** yapılır. Önce kapsam ve diğer filtreler
  veritabanında uygulanır, kalan kayıtların kan grubu çözülür ve süzülür. Veri hacmi küçük
  olduğu için bu yol yeterlidir: 21.10.2026'da 1.371 kişi, 585 aktif kart. Filtreyi
  yalnızca `personnel.person.view_sensitive` izni olan kullanıcı kullanabilir (ADR-0019 §2).
- **Sıra (revizyon 1.2, B-10):** Kan grubu süzgeci varsa **süzme, sıralama, sayfalama ve
  toplam sayı bellekte**, bu sırayla yapılır. Veritabanında sayfalayıp sonra süzmek eksik
  sayfa ve yanlış toplam üretir. Toplamın doğruluğu testle sınanır. Süzgeç yoksa bütün
  işlemler veritabanında kalır.
- Sıralama kan grubuna göre yapılmaz.

### 5. Denetim izi ve erişim kaydı

- Denetim izinde kan grubunun değeri yazılmaz, yalnızca alanın değiştiği görünür
  (`[SpecialCategoryData]`, `KR-120`).
- Kan grubunun ekranda gösterilmesi `SpecialCategoryView` erişim kaydı üretir (ADR-0019 §5).
- Erişim kaydı değiştirilemez (`KR-118`).

### 6. Ortamlar (ADR-0018 / R-25)

| Ortam | Kan grubu |
|---|---|
| CI ve otomatik testler | Sentetik |
| **Yerel geliştirme** | **Senkronize edilmez.** Senkronizasyon ayarı `Logo:SyncSpecialCategoryData` yerelde kapalıdır; alan boş kalır |
| **UAT** | Kabul testi için senkronize edilir; şifreli, erişimi sınırlı. **KVKK Sorumlusu onayladı (22.10.2026, `KR-125`)** |
| Üretim | Senkronize edilir |

Yerel ortamda gerçek kan grubunun bulunmasının bir faydası yoktur. Geliştirici şifreleme
ve görünürlük davranışını CI'daki sentetik veriyle ve UAT'de dener.

**Ayarın niteliği (revizyon 1.2, B-04):** `Logo:SyncSpecialCategoryData` bir **ortam
yapılandırmasıdır** (sır dosyası veya ortam değişkeni). Sistem Yönetimi parametresi değildir
ve ekrandan değiştirilemez. Ekrandan açılabilseydi yerel ortamda da açılabilir ve `KR-125`
çiğnenebilirdi. Ayar kapalıyken senkronizasyon özel nitelikli alanı LOGO'dan **okumaz** ve
HRMS'teki mevcut değeri **temizler**. Böylece ayar sonradan kapatılan bir ortamda eski kopya
kalmaz. Temizleme denetim izine "değişti" olarak yazılır.

> **Karar (22.10.2026, `KR-125`):** KVKK Sorumlusu UAT'de gerçek kan grubunun bulunmasını
> kabul testi için onayladı; yerel geliştirme ortamında bulunmaz. R-25'in kapsamı buna göre
> güncellendi.

## Gerekçe

- Ayrı anahtar ve anahtar kimliği, B-03'teki üç eksiği birlikte kapatır: anahtar paylaşımı,
  döndürme yokluğu ve isteğe bağlı anahtar.
- AAD, şifreli değerin satırdan satıra taşınarak başka bir kişiye ait gösterilmesini
  engeller.
- Blind index'ten kaçınmak, sekiz değerli bir alanda şifrelemenin asıl amacını korur.
  Bellek içi filtre, bu veri hacminde maliyetsizdir.
- Kan grubu LOGO'dan geldiği için anahtar kaybı veri kaybı değildir. Bu, anahtar yönetimini
  basitleştirir.

## Değerlendirilen alternatifler

| Alternatif | Artıları | Eksileri | Neden seçilmedi |
|---|---|---|---|
| PostgreSQL `pgcrypto` | Veritabanında şifreleme | Anahtar sorgu metninde veritabanına gider; günlüğe ve `pg_stat_statements`'a düşebilir | Anahtar veritabanını görmemeli |
| Disk/volume şifrelemesi | Şeffaf | Veritabanına bağlanan herkes düz metni görür | Kolon düzeyinde koruma vermez |
| Blind index ile veritabanında filtre | Hızlı filtre | Sekiz değerde gruplamayı açığa çıkarır | Şifrelemeyi anlamsızlaştırır |
| Sır parametre anahtarını paylaşmak | Tek anahtar | Biri ele geçerse ikisi de açılır; anahtar zorunlu değil | B-03 |
| Yerelde de gerçek kan grubu | Ortamlar aynı | Fayda yok, risk var | Gereksiz özel nitelikli veri kopyası |

## Sonuçlar

**Olumlu:**
- Kan grubu işleme koşulu 1 (şifreli saklama) teknik olarak karşılanır; koşul 3
  (değiştirilemez erişim kaydı) `KR-118` ile zaten karşılanıyor.
- Aynı altyapı sonraki modüllerin özel nitelikli verisinde kullanılır (İSG, sağlık raporu).

**Olumsuz / kabul edilen ödünler:**
- Kan grubuna göre veritabanı sıralaması ve dizin yok; filtre bellekte yapılır.
- Anahtar yönetimi bir işletim yükü getirir: yedekleme, döndürme yordamı.

**Bu kararın getirdiği yükümlülükler:**
- Şifreleme dönüştürücüsü, açılış doğrulaması ve "veritabanında düz metin yok" testi kan
  grubu alanıyla aynı PR'da gelir.
- UAT ve yerel sır dosyasına anahtar eklenir; runbook'a döndürme yordamı yazılır.
- Bilgi kayıt defterinde BK-29'a anahtarın yeri işlenir.

## Geri dönüş maliyeti

Düşük–orta. Şifreli değerler LOGO'dan yeniden üretilebildiği için yöntem değişikliği bir
yeniden senkronizasyonla uygulanabilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-22 | 1.0 | İlk sürüm: T1 tasarım kararları (#245 maddeler 3, 4) | Bilgi İşlem |
| 2026-10-22 | 1.1 | §6: UAT'de gerçek kan grubu KVKK Sorumlusu tarafından onaylandı (`KR-125`, #265) | Bilgi İşlem |
| 2026-10-22 | 1.2 | Revizyon (#267, #268): şifreleme depo katmanında ve ayrı tabloda, AAD tablo + alan + kimlik (B-02); açık yeniden şifreleme işi, önerilen kan grubu talep kapanınca silinir (B-03); ayar ortam yapılandırması, kapalıyken mevcut değer temizlenir (B-04); bellek içi filtrede sıra (B-10) | Bilgi İşlem |
| 2026-10-23 | 1.3 | §1: şifrelenen değer kan grubu kodudur; ad referans veriden (`KR-129`, #280) | Bilgi İşlem |
