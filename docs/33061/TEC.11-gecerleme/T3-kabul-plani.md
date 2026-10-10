# T3 Kimlik Yönetimi — Kabul Planı

**Belge kimliği:** TEC.11-PLN-T3
**Süreç:** TEC.11 — Geçerleme (`TEC.11.BP1`, `BP2`)
**Modül:** T3 Kimlik Yönetimi
**Ölçüt:** `PG-KMLK` 1.2: 56 onaylı paydaş gereksinimi ve kabul kriterleri (İK onayı 23.09.2026)
**Hazırlayan:** Bilgi İşlem · **Gözden geçirecek ve kabul kararını verecek:** İK
**Son güncelleme:** 2026-10-04 · **Durum:** Taslak; İK gözden geçirmesi ve takvim bekleniyor

---

## 1. Amaç

T3'ün, İK'nın 23.09.2026'da onayladığı gereksinimleri **gerçek kullanımda** karşıladığını
İK ve personelle birlikte göstermek ve İK'nın kabul kararını (G3) almak.

---

## 2. Katılımcılar

| Rol | Kim | Görevi |
|---|---|---|
| Kabul kararı | İK birim yöneticisi | Kabul formunu imzalar (G3) |
| İK kullanıcısı | İK'dan 1–2 kişi | Hesap işlemleri senaryolarını uygular (KS-09) |
| Personel | **İK dışından 2–3 personel** (`KR-048`). En az biri telefonla, en az biri cep telefonu kayıtlı **olmayan** kişi | Üyelik, giriş ve parola senaryolarını kendi bilgileriyle uygular |
| Sistem yöneticisi | Bilgi İşlem (Doğuş Uçanok) | KS-10'u uygular; kanıt gösterimini yapar; gözlem notu tutar |

**Personel seçimi İK'ya aittir.** Seçilen kişilerin hesabı olmamalıdır; üyelik senaryosu ilk kez uygulanmalıdır. Kişiler gönüllü olmalı ve kendi gerçek bilgilerini kullanmalıdır.

---

## 3. Ortam ve sürüm

| | |
|---|---|
| Ortam | UAT — https://insankaynaklaritest.duzen.com.tr (gerçek LOGO verisi, `KR-024`) |
| Sürüm | **`v0.2.0-rc.4`**: `v0.2.0-rc.3`'ün yerine; iki adımlı doğrulamanın kullanıcı tercihine bağlanmasını ve İK'nın kurtarma işlemini içerir (#190, §6.1 T-04). Dağıtım kaydı: `TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`. Oturumdan önce çalışan sürüm yeniden doğrulanır; forma etiket yazılır |
| İletiler | UAT izin listesi kipinde (`AllowList`, `KR-083`): yalnızca listedeki adreslere ve numaralara e-posta ve SMS gider. **Katılımcıların kurumsal e-postaları ve cep telefonları oturumdan önce listeye eklenir** (`NOTIFICATIONS_ALLOWED_RECIPIENTS`), oturumdan sonra çıkarılır |
| Cihazlar | Masaüstü tarayıcı + en az bir cep telefonu (REQ-KMLK-044) |
| Süre | Yaklaşık 2 saat (senaryolar 75 dk, kanıt gösterimi 30 dk, değerlendirme 15 dk) |

---

## 4. Giriş ölçütleri

Kabul oturumu ancak aşağıdakilerin tamamı sağlanınca yapılır:

- [x] **G2 "Geliştirme tamam"** kapısı geçti (T3 doğrulama raporu §5; 04.10.2026)
- [x] **SYG-KMLK-064 metin incelemesi** kararı verildi ve onaylanan düzeltmeler uygulandı (04.10.2026, #152)
- [ ] Kabul adayı sürüm **etiketlendi** ve UAT'ye bu etiketten kuruldu (`v0.2.0-rc.4`, #190). Önceki adaylar: `v0.2.0-rc.1` (#157), `v0.2.0-rc.2` (#160) ve `v0.2.0-rc.3` (#185)
- [ ] Katılımcılar İK tarafından belirlendi; adresleri ve numaraları UAT izin listesine eklendi
- [ ] Katılımcılara kısa kullanım notu verildi (`T3-kullanim-notu.md`, #154)
- [ ] Bilinen açık `[HATA]` kaydı yok veya her biri kabul öncesinde İK'ya bildirildi

---

## 5. Kabul senaryoları (`BP2`)

Her senaryo **katılımcının kendisi tarafından** uygulanır. Bilgi İşlem adım söylemez, yalnızca gözlemler. Kullanıcı takılırsa gözlem notu düşülür (REQ-KMLK-001: "İK ile temas etmeden").

### KS-01 — Üyelik (e-posta ile)
**Kim:** Personel 1 · **Cihaz:** Masaüstü
1. Giriş ekranında "Üye olun" bağlantısına tıklar.
2. T.C. Kimlik Numarası, doğum tarihi ve kurumsal e-postasını girer.
3. Doğrulama yöntemi olarak e-postayı seçer; gelen kodu girer.
4. Kurallara uyan bir parola belirler; giriş yapar.
5. Aynı bilgilerle ikinci kez üye olmayı dener.

**Beklenen:**
- Hesap İK'ya başvurmadan açılır. Kod birkaç dakika içinde gelir.
- Ekranlar iki bölümlü düzendedir; logo ve "Sorun mu yaşıyorsunuz? Bilgi İşlem" bilgisi görünür.
- İkinci denemede "Hesabınız zaten var" iletisi çıkar.

**Kapsadığı gereksinimler:** REQ-KMLK-001, 002, 005, 014, 022, 047, 048, 056

### KS-02 — Üyelik (SMS ile), kanal değiştirme ve kodu tekrar gönderme
**Kim:** Personel 2 · **Cihaz:** Cep telefonu
1. Telefondan üyeliği başlatır; SMS'i seçer.
2. Kod ekranındaki geri sayımı görür; "Kodu tekrar gönder"e basar.
3. "Başka bir yöntemle doğrula" ile e-postaya geçer; e-posta kodunu girer ve üyeliği tamamlar.

**Beklenen:**
- SMS "DUZEN" başlığıyla gelir.
- Kanal değişince önceki kod geçersizleşir.
- Ekranlar telefonda yatay kaydırma olmadan kullanılır.

**Kapsadığı gereksinimler:** REQ-KMLK-005, 006, 020, 021, 044

### KS-03 — Kanalların sunulması
**Kim:** Personel 3 (cep telefonu kayıtlı **olmayan**) · **Cihaz:** Masaüstü
1. Üyeliği başlatır.

**Beklenen:**
- Yalnızca e-posta seçeneği çıkar.
- Bilgi İşlem, LOGO'da kurumsal e-postası olmayan bir kişide e-posta seçeneğinin hiç sunulmadığını kanıt gösteriminde gösterir (KG-02).

**Kapsadığı gereksinimler:** REQ-KMLK-007, 008, 009, 010

### KS-04 — Eşleşmeyen bilgiler
**Kim:** Personel 1 · **Cihaz:** Masaüstü
1. Kendi T.C. Kimlik Numarasıyla ama **yanlış doğum tarihiyle** üyeliği başlatır; kanal seçip kodu bekler.

**Beklenen:**
- Ekran ve ileti, eşleşen durumla aynıdır.
- Kod **gelmez**.

**Kapsadığı gereksinimler:** REQ-KMLK-002, 004

> **İK'ya ayrıca gösterilecek kalan risk (AN-01, `KR-078`):** Cep telefonu olmayan kişide eşleşme varsa yalnızca e-posta kanalı, eşleşme yoksa iki kanal görünür. Bu fark, üç bilginin de doğru olduğunu dolaylı olarak ele verir. Risk 25.09.2026'da Bilgi İşlem tarafından kabul edildi. Risk defterinde R-19 olarak kayıtlıdır; puanı 2'dir. Puan 6'nın altında olduğu için RACI'ye göre kabul yetkisi Bilgi İşlem'dedir (#126). Kabul formunda ayrıca işaretlenir.

### KS-05 — Doğrulama kodu kuralları
**Kim:** Personel 2 · **Cihaz:** Masaüstü
1. Parola sıfırlamayı başlatır, e-posta kodu ister ve kodu 3 kez yanlış girer.
2. Yeni kod ister; kodu doğru girer; aynı kodu ikinci kez kullanmayı dener.
3. Yeni kod ister ve 5 dakika bekleyip girer.

**Beklenen:**
- 3 yanlış denemeden sonra kod iptal olur ve "Yeni kod isteyin" iletisi çıkar.
- Kullanılmış kod kabul edilmez.
- Süresi dolan kod için anlaşılır bir ileti çıkar.

**Kapsadığı gereksinimler:** REQ-KMLK-016, 017, 019

### KS-06 — Giriş ve hatalı giriş
**Kim:** Personel 1 · **Cihaz:** Masaüstü
1. Doğru e-posta ve parolayla giriş yapar.
2. Sicil numarası veya T.C. Kimlik Numarasıyla giriş yapmayı dener.
3. Yanlış parolayla, sonra var olmayan bir e-postayla giriş dener; iletileri karşılaştırır.
4. Yanlış parolayla 5 kez dener; ardından doğru parolayla dener.

**Beklenen:**
- Giriş yalnızca kurumsal e-postayla yapılabilir.
- İki hatalı girişin iletisi aynıdır.
- 5 hatadan sonra doğru parola da 15 dakika kabul edilmez. Bilgi İşlem, kilidi beklemek yerine parametreyle kısaltabilir.

**Kapsadığı gereksinimler:** REQ-KMLK-022, 023, 024

### KS-07 — Oturum
**Kim:** Personel 2 · **Cihaz:** Masaüstü ve cep telefonu
1. Masaüstünde giriş yapar; telefonda aynı hesapla giriş yapar; masaüstünde sayfayı yeniler.
2. Masaüstünde yeniden giriş yapar; 30 dakika işlem yapmaz. Bilgi İşlem hareketsizlik süresini parametreyle kısaltabilir.
3. Giriş yapar ve uzun bir video oynatır.
4. Çıkış yapar; tarayıcının geri tuşuyla korumalı sayfaya dönmeyi dener.

**Beklenen:**
- Masaüstünde "Hesabınıza başka bir cihazdan giriş yapıldı" iletisi çıkar.
- Hareketsizlikte önce uyarı, sonra kapanış iletisi gelir.
- Video oynarken oturum kapanmaz.
- Çıkıştan sonra sayfa açılmaz.

**Kapsadığı gereksinimler:** REQ-KMLK-026, 027, 049, 050, 054

### KS-08 — Parola işlemleri
**Kim:** Personel 3 · **Cihaz:** Masaüstü
1. Oturumdayken "Parolamı değiştir"e girer. Mevcut parolayı yanlış girer, sonra doğru girer. Yeni parola olarak yaygın bir parola (örn. "123456") ve adını içeren bir parola dener; sonra kurallara uyan bir parola belirler.
2. Çıkış yapar. "Parolamı unuttum" ile kimliğini doğrulayıp yeni parola belirler ve giriş yapar.

**Beklenen:**
- Mevcut parola girilmeden değişiklik yapılamaz.
- Yaygın ve kişisel parolalar ne yapılacağını söyleyen iletiyle reddedilir.
- Sıfırlama İK'ya başvurmadan tamamlanır.
- Sistem periyodik parola değişimi dayatmaz (parametre kapalı).

**Kapsadığı gereksinimler:** REQ-KMLK-028, 029, 030, 031, 032

### KS-09 — İK hesap işlemleri
**Kim:** İK kullanıcısı · **Cihaz:** Masaüstü
1. "Hesap işlemleri" ekranında bir personeli sicil numarasıyla, sonra adıyla arar.
2. Hesabı olmayan, kurumsal e-postası olan bir katılımcıya "Bağlantı gönder" der. Katılımcı bağlantıyla parolasını belirler ve aynı bağlantıyı ikinci kez açar.
3. Bir katılımcının hesabını gerekçe girmeden, sonra gerekçeyle pasife alır. Katılımcı giriş yapmayı dener. Sonra hesabı gerekçeyle yeniden aktifleştirir.
4. KS-10'da 2FA'sını açmış katılımcının, kodunu alamadığını varsayarak 2FA'sını önce gerekçesiz, sonra gerekçeyle kapatır. Katılımcı yalnızca parolasıyla giriş yapar.

**Beklenen:**
- Bağlantı yalnızca bir kez kullanılır; ikinci açılışta "geçersiz" iletisi çıkar.
- Gerekçe olmadan pasife alma yapılamaz.
- Pasif hesapla giriş reddedilir.
- Gerekçe olmadan 2FA kapatılamaz; kapatıldıktan sonra katılımcıdan kod istenmez.
- Bilgi İşlem, işlemlerin denetim izine kim ve ne zaman bilgisiyle yazıldığını gösterir.

**Kapsadığı gereksinimler:** REQ-KMLK-011, 036, 037, 052

### KS-10 — Sistem yönetimi
**Kim:** Bilgi İşlem (İK izler) · **Cihaz:** Masaüstü
1. Parametre ekranında kabul edilen alan adlarına bir alan adı ekler ve çıkarır.
2. Parola en az uzunluğunu 8 yapar; KS-08'deki kısa parolanın artık reddedildiğini gösterir; değeri geri alır.
3. Destek iletişim bilgisini değiştirir; giriş ekranında güncellendiğini gösterir.
4. İki adımlı doğrulamayı açar; 2FA'yı kendi hesabında açmış kişi varsa sayısının gösterildiğini ve onay istendiğini gösterir. Bir katılımcı "Hesap güvenliği" ekranından kendi 2FA'sını parolası ve gelen kodla açar, çıkış yapıp iki adımlı giriş yapar. 2FA'sı kapalı başka bir katılımcı tek adımla girer. (Bu katılımcının 2FA'sı KS-09 adım 4'te kapatılır.) Parametreyi en sonda kapatır.
5. İlk girişte parola değiştirme parametresini açar. Hiç giriş yapmamış bir katılımcı giriş yapar ve parola ekranına yönlendirilir. Parametreyi kapatır.
6. Kurumsal logoyu yükler; giriş ekranında ve telefonda göstererek bozulmadığını teyit eder; varsayılana döner.

**Beklenen:** Her değişiklik kod değişikliği olmadan, en geç 1 dakikada etkili olur.

**Kapsadığı gereksinimler:** REQ-KMLK-012, 025, 028, 048, 051, 052, 055, 056

### KS-11 — Dil, erişilebilirlik ve hata ekranı
**Kim:** Personel 1 ve İK · **Cihaz:** Masaüstü
1. Giriş ve üyelik ekranlarını yalnızca klavyeyle (Tab, Enter) kullanır.
2. Senaryolar boyunca görülen iletileri değerlendirir. SYG-064 incelemesinin kabul edilen istisnaları gösterilir.
3. Bilgi İşlem bir hata ekranında takip numarasını gösterir.

**Beklenen:**
- Klavye sırası mantıklıdır ve her alanın etiketi vardır.
- İletiler Türkçedir, teknik terim içermez ve ne yapılacağını söyler.
- Hata ekranında takip numarası görünür.

**Kapsadığı gereksinimler:** REQ-KMLK-043, 045, 046

---

## 6. Kanıt gösterimi (Bilgi İşlem gösterir, İK inceler)

Bu gereksinimler kullanıcı arayüzünden gözlenemez. Kanıtları T3 doğrulama raporunda ve ilgili testlerde yer alır.

| # | Gösterilen | Kapsadığı gereksinimler | Kanıt |
|---|---|---|---|
| KG-01 | LOGO'daki ayrılmış (çıkış tarihi dolu) bir personelle üyelik denemesi; TCKN'siz kartların uyarı listesi; ayrılan personelin hesabının senkronizasyonda kapanması ve yeniden işe girişte aynı hesabın açılması | REQ-KMLK-003, 013, 034, 035 | UAT senkronizasyon kayıtları; `PersonnelSynchronizerTests` |
| KG-02 | Kurumsal e-postası olmayan, kişisel adresli ve ortak adresli kişilere e-posta kanalının sunulmaması | REQ-KMLK-007, 008, 009 | `RegistrationApiTests` |
| KG-03 | Kodların güvenli üreteçle üretilmesi; kod ve parolanın veritabanında özet olarak saklanması | REQ-KMLK-015, 018, 033 | Veritabanı görünümü; `VerificationCodeTests`, `PasswordInfrastructureTests` |
| KG-04 | Günlükte kişisel veri, kod ve parola bulunmaması; kimlik olaylarının denetim izinde kullanıcı, zaman, IP ve takip numarasıyla kaydı | REQ-KMLK-018, 040, 041 | `IdentityLogMaskingTests`; `audit.security_event` örneği |
| KG-05 | Üyelik ve kod gönderim hız sınırları | REQ-KMLK-038, 039 | `RegistrationApiTests` |
| KG-06 | İşlemsel iletilerin bildirim istisnasından muaf olması | REQ-KMLK-042 | `NotificationDispatcherTests` (istisnanın kendisi Y1'de gelecek) |
| KG-07 | 2FA'nın sistem ve kullanıcı düzeyinde açık ve kapalı hâllerinin otomatik testi | REQ-KMLK-053 | `SessionApiTests`, `TwoFactorPreferenceApiTests` |
| KG-08 | Eşleşen ve eşleşmeyen üyelik isteklerinin yanıt süresi farkı (< 20 ms) | REQ-KMLK-004 | `RegistrationTimingTests` (KPÖ-KMLK-2) |

### 6.1 İK'nın teyit edeceği yorumlar (#135)

Onaylı gereksinimler iki noktada ayrıntı vermiyordu. Bilgi İşlem bu boşlukları geliştirme
sırasında aşağıdaki gibi yorumladı. Yorumlar kabul oturumunda İK'ya anlatılır; İK'nın kararı
kabul formuna yazılır. İK farklı karar verirse değişiklik kabul öncesinde veya iş listesinde
ele alınır.

| # | Konu | Bilgi İşlem'in yorumu | Gösterildiği senaryo | Kaynak |
|---|---|---|---|---|
| T-01 | Hareketsizlik neye göre sayılır? | 30 dakikalık hareketsizlik süresi, kişinin açık sekmede **tıklama, tuşa basma, kaydırma veya dokunma** yapmadığı süredir. Sekmede video oynarken de kişi etkin sayılır. Fareyi yalnızca gezdirmek, başka bir sekmede veya programda çalışmak etkinlik sayılmaz. | KS-07 | REQ-KMLK-054; AN-23, `KR-087` |
| T-02 | Parola değişimi açılırsa kimleri, ne sıklıkla etkiler? | Şu anda ikisi de **kapalı**. (1) Periyodik değişim açılırsa süre 90 gündür; 30 ile 365 gün arasında ayarlanabilir. (2) İlk girişte değişim açılırsa, sisteme hiç giriş yapmamış **herkes** ilk girişinde parolasını değiştirir; üyelikte parolasını kendisi belirleyenler de buna dahildir. Kural açılmadan önce giriş yapmış olanlar etkilenmez. (3) Parolasını değiştirmesi gereken kişi, değiştirene kadar sistemin başka bir ekranını kullanamaz. | KS-08, KS-10 | REQ-KMLK-030, 055; AN-24, `KR-092` |
| T-03 | Üyelikte deneme ve kod gönderim sınırları | Onaylı gereksinimde TCKN başına saatte 5 üyelik denemesi ve 15 dakikada 3 kod vardı. UAT'de İK ile yapılan denemelerde sistem çok çabuk engelledi; iki varsayılan da **10** yapıldı. Kod gönderim sınırı Sistem Yönetimi ekranından 1–15, üyelik deneme sınırı 3–20 arasında ayarlanabilir. **Değişiklik talebi; İK onayı 10.10.2026'da alındı.** Kabul oturumunda yeni davranış gösterilir | KS-01, KS-02, KS-05 | REQ-KMLK-038, 039; değişiklik talebi #185 |
| T-04 | İki adımlı doğrulama kimlere uygulanır? | Onaylı gereksinimde parametre açılınca 2FA herkese uygulanıyordu. Artık parametre 2FA'yı yalnızca **kullanıma açar**; her kullanıcı "Hesap güvenliği" ekranından kendi hesabında açar, varsayılan kapalıdır. Açmak için parola ve gelen kod, kapatmak için parola gerekir. Kodu alamayan kişinin 2FA'sını İK "Hesap işlemleri" ekranından gerekçe girerek kapatır. **Değişiklik talebi; kullanıcı tercihi için İK onayı 10.10.2026'da alındı.** Kabul oturumunda yeni davranış gösterilir | KS-09, KS-10 | REQ-KMLK-025, 051, 052, 053; değişiklik talebi #190, `KR-104` |

---

## 7. Kapsam kontrolü

56 gereksinimin her biri en az bir senaryoya veya kanıt gösterimine bağlıdır. Eşleme izlenebilirlik matrisinin "Kabul" sütunundadır.

| Bölüm | Gereksinim | Senaryo / kanıt |
|---|---|---|
| Üyelik | 001–014 | KS-01…04, KS-09, KS-10, KG-01, KG-02 |
| Doğrulama kodu | 015–021 | KS-02, KS-05, KG-03, KG-04 |
| Giriş | 022–027, 049–054 | KS-06, KS-07, KS-10, KG-07 |
| Parola | 028–033, 055 | KS-08, KS-10, KG-03 |
| Hesap yaşam döngüsü | 034–037 | KS-09, KG-01 |
| Güvenlik | 038–042 | KG-04…KG-06 |
| Kullanılabilirlik | 043–046, 056 | KS-01, KS-02, KS-10, KS-11 |
| Görsel tasarım | 047, 048 | KS-01, KS-10 |

---

## 8. Kabul ölçütü ve karar (G3)

- **Kabul:** Zorunlu gereksinimlerin tamamı karşılanır.
- **Koşullu kabul:** Karşılanmayan bir Zorunlu gereksinim için İK gerekçeli koşul ve tarih belirler.
- **Ret:** Koşul belirlenemiyorsa.
- **"Olmalı" gereksinimler (045, 046, 055, 056):** Karşılanmayanlar forma yazılır ve iş listesine alınır.

Karar `docs/sablonlar/kabul-formu.md` şablonuyla kayda geçer. Form, sürüm etiketini, AN-01 kalan riskinin İK'ya gösterildiğini ve §6.1'deki yorumlar için İK'nın kararını içerir.

---

## 9. Kabul sırasında bulunan sorunlar

Her sorun bir `[HATA]` issue'su olur ve şunları taşır: senaryo kimliği (örn. KS-06), adımlar, beklenen ve görülen. Kişisel veri issue'ya yazılmaz; gerekirse ekran görüntüsünde kişisel veri görünmez kılınır.

Kabul kararından önce düzeltilmesi gerekenler İK ile birlikte belirlenir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-04 | 0.1 | İlk taslak (#124) | Bilgi İşlem |
| 2026-10-04 | 0.2 | §4: SYG-064 giriş ölçütü karşılandı (#152) | Bilgi İşlem |
| 2026-10-04 | 0.3 | §4: kullanım notu #154 ile hazırlandı (#130 yerine); G2 giriş ölçütü karşılandı | Bilgi İşlem |
| 2026-10-04 | 0.4 | §3, §4: kabul adayı `v0.2.0-rc.1` etiketlendi ve UAT'ye kuruldu (#157) | Bilgi İşlem |
| 2026-10-04 | 0.5 | §3, §4: kabul adayı `v0.2.0-rc.2` (#138 düzeltmesi; #160) | Bilgi İşlem |
| 2026-10-04 | 0.6 | §3, §4: `v0.2.0-rc.2` UAT'ye kuruldu (#160) | Bilgi İşlem |
| 2026-10-04 | 0.7 | §6.1: İK'nın teyit edeceği yorumlar T-01 (AN-23) ve T-02 (AN-24) (#135) | Bilgi İşlem |
| 2026-10-04 | 0.8 | KS-04 notu: kalan risk R-19 olarak kayıtlı, kabul yetkisi (#126) | Bilgi İşlem |
| 2026-10-10 | 0.9 | §6.1: T-03 — deneme ve kod gönderim sınırlarındaki değişiklik talebi (#185) İK teyidine; §3, §4 kabul adayı `v0.2.0-rc.3` | Bilgi İşlem |
| 2026-10-10 | 0.10 | §6.1 T-03: İK onayı alındı (#187) | Bilgi İşlem |
| 2026-10-10 | 0.11 | KS-09 adım 4 (İK'nın 2FA kapatması), KS-10 adım 4 ve KG-07 kullanıcı tercihine bağlı 2FA'ya göre; §6.1 T-04; §3, §4 kabul adayı `v0.2.0-rc.4` (#190) | Bilgi İşlem |
