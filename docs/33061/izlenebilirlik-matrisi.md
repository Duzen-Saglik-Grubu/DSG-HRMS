# İzlenebilirlik Matrisi

**Belge kimliği:** 33061-IZM
**Son güncelleme:** 2026-10-01
**Hizmet ettiği süreçler:** TEC.2, TEC.3, TEC.5, TEC.7, TEC.8, TEC.9, TEC.10, TEC.11, TEC.13
**Sahibi:** Bilgi İşlem

> **Bu belge, "bu kod hangi gereksinimi karşılıyor?" ve "bu gereksinim nerede
> doğrulandı?" sorularının cevabıdır.** Sekiz sürecin çıktı listesinde izlenebilirlik
> maddesi bulunur; hiçbiri bu matris olmadan karşılanamaz.

---

## 1. Neden var?

TS ISO/IEC TS 33061, aşağıdaki süreç çıktılarında izlenebilirlik ister:

| Süreç | Çıktı | Ne istiyor |
|---|---|---|
| TEC.2 | (i) | Paydaş gereksiniminin, paydaşa ve ihtiyacına kadar izlenebilmesi |
| TEC.3 | (f) | Sistem gereksiniminin paydaş gereksinimine bağlanması |
| TEC.5 | (h) | Tasarım özelliklerinin mimari öğelere bağlanması |
| TEC.7 | (e) | Gerçekleştirmenin izlenebilir olması |
| TEC.8 | (h) | Entegre edilen öğelerin izlenebilir olması |
| TEC.9 | (g) | Doğrulanan öğelerin izlenebilir olması |
| TEC.10 | (h) | Geçişi yapılan öğelerin izlenebilir olması |
| TEC.11 | (h) | Geçerlenen öğelerin izlenebilir olması |

18.09.2026 süreç gözden geçirmesi, bu matrisin **hiç oluşturulmamış** olmasını en
yüksek önemli bulgu (BULGU-01) olarak kaydetti: tek başına bu eksik, sekiz sürecin
`PA 1.1 = F` almasını yapısal olarak engelliyordu.

---

## 2. Zincir

```
Paydaş ihtiyacı  →  PG-<MODÜL>-nn  →  SYG-<MODÜL>-nnn  →  Tasarım  →  Kod / PR  →  Test  →  Kabul
   (toplantı)        (TEC.2)          (TEC.3)         (TEC.5)     (TEC.7)    (TEC.9)  (TEC.11)
```

Her halka bir öncekine **kimlikle** bağlanır. Bir halka kopuksa, kopukluk matriste
görünür — gizlenmez.

### 2.1 Sütunlar

| Sütun | Ne yazılır | Kaynak |
|---|---|---|
| **Kaynak** | Paydaş gereksinimi (`PG-…`) veya kurumsal karar (`KR-…`) | TEC.2 / karar kayıt defteri |
| **Sistem gereksinimi** | `SYG-<MODÜL>-nnn` (TEC.3 YAKLASIM §3.1) | TEC.3 |
| **Tasarım** | `ADR-nnnn` veya `docs/mimari/` altındaki belge | TEC.5 |
| **Gerçekleştirme** | Kod yolu + birleştiren PR numarası | TEC.7 |
| **Doğrulama** | Test dosyası veya CI kapısı | TEC.9 |
| **Kabul** | Kabul formu / kabul raporu | TEC.11 |

### 2.2 Kaynak neden iki türlü olabilir?

Zincirin başı çoğunlukla bir **paydaş gereksinimidir**. Ancak bazı kalemler paydaştan
değil, **kurumsal bir karardan** doğar: "LOGO'ya yazma yapılmayacak" gibi. Bunlar
`KR-` kimliğiyle girer ve aynı zinciri izler.

Bu ayrım bilinçlidir: kurumsal kısıtı paydaş gereksinimi gibi göstermek, gereksinimin
kaynağını yanlış kaydeder — ki izlenebilirliğin asıl amacı **kaynağı** doğru
tutmaktır.

---

## 3. Bakım kuralı

> **Matris, değişikliğin yapıldığı PR'da güncellenir.** Ayrı bir iş olarak
> bırakılmaz.

Gerekçe: ayrı bırakılan izlenebilirlik kaydı **çürür**. Bir hafta sonra kimse hangi
kodun hangi gereksinimden geldiğini hatırlamaz; matris "sonra doldurulacak" satırlarla
dolar ve kanıt değerini kaybeder. Denetimde değerli olan, bağın **iş yapılırken**
kurulmuş olmasıdır.

| Ne zaman | Kim | Ne yapar |
|---|---|---|
| Paydaş gereksinimi onaylandığında | Bilgi İşlem | Satırı açar; `PG` ve kaynak dolu, gerisi boş |
| Sistem gereksinimi yazıldığında | Bilgi İşlem | "Sistem gereksinimi" sütununu doldurur; tutarlılık her PR'da otomatik denetlenir (TEC.3 YAKLASIM §4) |
| Kod birleştirildiğinde | PR sahibi | `Gerçekleştirme` ve `Doğrulama` sütunlarını doldurur |
| Modül kabul edildiğinde | Bilgi İşlem | `Kabul` sütununu doldurur |

**Boş hücre yasak değildir; açıklamasız boş hücre yasaktır.** Sırası gelmemiş bir
halka `—` ile, uygulanmayan bir halka `yok (gerekçe)` ile işaretlenir.

---

## 4. A1 — Teknik İskelet

> Bu bölüm, matrisin **çalıştığını gösterir**. A1'de paydaş gereksinimi üretilmedi
> (modüller başlamamıştı); zincirlerin başı kurumsal kararlardır. Buna rağmen karar →
> tasarım → kod → test zinciri **eksiksiz izlenebilmektedir**.

| # | Kaynak | Sistem gereksinimi | Tasarım | Gerçekleştirme | Doğrulama | Kabul |
|---|---|---|---|---|---|---|
| 1 | `KR-060` Denetim izi değiştirilemez | — (A2'de) | `ADR-0009` §2 | `src/backend/.../Audit/` · PR #12 | `AuditImmutabilityTests.cs`, `AuditTrailTests.cs` | — |
| 2 | `KR-061` Erişim kaydı fail-closed | — | `ADR-0009` §3 | PR #14 | `AccessLogTests.cs` | — |
| 3 | `KR-059` Günlükte iki katmanlı maskeleme | — | `ADR-0009` §4 | `MaskingDestructuringPolicy` · PR #10 | `MaskingDestructuringPolicyTests.cs`, `MaskTests.cs`, `MaskRulesTests.cs` | — |
| 4 | `KR-062` Hata ayrıntısı hiçbir ortamda sızmaz | — | `ADR-0010` §5 | Problem Details ara katmanı · PR #16 | `ProblemDetailsTests.cs`, `problemDetails.test.ts` | — |
| 5 | `KR-063` Sağlık kontrollerinin ayrımı | — | `ADR-0011` | `/health/live`, `/health/ready` · PR #22 | `HealthCheckTests.cs`, `PersonalDataScrubbingProcessorTests.cs` | — |
| 6 | `ADR-0002` Katman bağımlılık kuralları | — | `ADR-0002` | Katman projeleri · PR #8 | `LayerDependencyTests.cs` (NetArchTest) | — |
| 7 | `ADR-0004` Veritabanı adlandırma standartları | — | `ADR-0004` | EF yapılandırması · PR #8 | `DatabaseConventionTests.cs` | — |
| 8 | `KR-067` UAT ve üretimde TLS zorunlu | — | Runbook §7 | `nginx/tls.conf`, `renew-tls.sh` · PR #43 | Dağıtım betiğinin doğrulaması (`deploy-uat.sh`) + elle ölçüm | — |

**Kabul sütunu neden boş?** TEC.11 geçerleme, **kabul edilecek bir modül** gerektirir.
A1 teknik iskelettir; İK kabulüne sunulan bir işlev üretmedi. İlk kabul T3 ile
gelecektir.

### 4.1 Zinciri tamamlanmamış kalemler

| Kaynak | Eksik halka | Neden |
|---|---|---|
| ~~`KR-003`, `KR-004` LOGO salt-okunur~~ | ~~Kod ve test~~ | ✅ **Kapandı (PR #76, 26.09.2026).** Oturum betiği depoda (`docker/logo/read-only-login.sql`); CI bu betiğin kendisini SQL Server Express'te çalıştırıp yazma denemelerinin reddedildiğini her PR'da doğruluyor (`LogoReadOnlyAccessTests`). Üretimde senkronizasyon her çalışmadan önce yetkiyi hiçbir şey yazmadan denetliyor. Zincir: `KR-003` → `ADR-0003` §1 → `Infrastructure/Logo/` · PR #76 → `LogoReadOnlyAccessTests` |
| `KR-056` Bildirim istisnası (#3) | Tasarım sonrası tüm halkalar | Y1 Bildirim Merkezi'nde uygulanacak (A3) |
| `KR-068` Modül başlangıç koşulu | Kod/test | Süreç kuralı; kodda karşılığı yok — `yok (süreç kuralı)` |

> Bu tablo bilinçli olarak tutuluyor: **kopuk halkayı göstermek, matrisi dolu
> göstermekten daha değerlidir.** Bir denetçi, eksiğin bilindiğini ve nereye
> bağlandığını görmek ister.

---

## 5. T3 — Kimlik Yönetimi

> İlk gerçek modül çevrimi. Gereksinimler **23.09.2026 İK toplantısında onaylandı**
> (`TEC.2/kayitlar/2026-09-23-kimlik-gereksinim-toplantisi.md`); 56 satırın tamamı
> açıldı. **Kaynak** ve **Sistem gereksinimi** sütunları dolu (`TEC.3/gereksinimler/SYG-KMLK.md`,
> 79 madde); diğer halkalar işin yapıldığı PR'larda doldurulacak
> (§3). Kaynak metinlerin tamamı: `TEC.2/paydas-gereksinimleri/PG-KMLK.md`.

| # | Kaynak | Sistem gereksinimi | Tasarım | Gerçekleştirme | Doğrulama | Kabul |
|---|---|---|---|---|---|---|
| 1 | `REQ-KMLK-001` Personel, giriş ekranındaki "Üye Ol" bağlantısıyla kendi hesabını kend… | SYG-KMLK-013, 079 | `ADR-0006` §1, `KR-085` | kısmen: SYG-013 (üyelik API'si) · PR #88 — ekranlar sonraki iş | `RegistrationApiTests` | — |
| 2 | `REQ-KMLK-002` Üyelik doğrulaması TCKN + doğum tarihi + kurumsal e-posta bilgileriyle… | SYG-KMLK-006, 013, 014 | `ADR-0003` §4, `ADR-0005` §1; `ADR-0006` §1, `KR-085` | kısmen: SYG-006 (senkronize alanlar) · PR #75; SYG-013 (üç bilgiyle eşleşme), 014 (TCKN sağlama; istemci tarafı ekranlarda) · PR #88; SYG-014 istemci tarafı (sağlama algoritması; geçersiz numara gönderilmez) · PR #91 | `PersonnelSynchronizerTests`, `PersonnelSyncStoreTests`; `RegistrationApiTests`; `nationalId.test.ts`, `RegistrationPage.test.tsx` | — |
| 3 | `REQ-KMLK-003` Eşleşme LOGO anlık görüntüsü üzerinden yapılır; yalnızca AKTİF istihda… | SYG-KMLK-001, 002, 003, 004, 006, 011, 012, 013, 072 | `ADR-0003`; `ADR-0006` §1, `KR-085` | kısmen: SYG-001, 002, 003, 004, 006, 011, 012 · PR #75, #76 — SYG-013 (üyelik), 072 (elle tetikleme API) sonraki işler; SYG-013 (en az bir aktif istihdam) · PR #88; SYG-072 (son çalışma bilgisi ve elle tetikleme, izin gerektirir, yetkisiz `403`) · PR #101 | `PersonnelSynchronizerTests`, `PersonnelSyncStoreTests`, `PersonnelSyncServiceTests`, `LogoReadOnlyAccessTests`; `RegistrationApiTests`; `AccessControlApiTests`, `PersonnelSyncWorkerTests` | — |
| 4 | `REQ-KMLK-004` Bilgiler eşleşse de eşleşmese de kullanıcıya AYNI ekran ve AYNI mesaj … | SYG-KMLK-015, 016, 018, 020 | `ADR-0012` §5, `KR-082`; `KR-085` | kısmen: SYG-015 (arka plan kuyruğu: gönderim yanıtı bekletmez) · PR #86 — aynı yanıt ve 016, 018, 020 üyelik işinde; SYG-015 (aynı yanıt, sabit alt süre: medyan farkı 0,08 ms), 016 (eşleşmede kod yok, sahte kod), 018 (hedef dönmez), 020 (mevcut hesap doğrulamadan sonra) · PR #88; SYG-016 ekran iletisi ("eşleşiyorsa gelir", her durumda) · PR #91 | `NotificationDispatcherTests`; `RegistrationApiTests`, `RegistrationTimingTests`; `RegistrationPage.test.tsx` | — |
| 5 | `REQ-KMLK-005` Doğrulama kanalı kullanıcıya seçtirilir: e-posta veya SMS. Hangi kanal… | SYG-KMLK-016, 017, 030, 075 | `ADR-0008` §7; `ADR-0012` §3; `ADR-0015` | kısmen: SYG-075 (parametre deposu, şifreli sırlar) · PR #84 — SYG-076 (parametre ekranı) sonraki iş; SYG-030 (SMTP gönderici, Türkçe şablon) · PR #86; SYG-016, 017 (kanal seçimi) · PR #88; SYG-016, 017 kanal seçim ekranı · PR #91 | `ParameterCatalogTests`, `SystemParameterStoreTests`, `AesGcmSecretProtectorTests`; `SmtpEmailSenderTests`, `VerificationMessagesTests`; `RegistrationApiTests`; `RegistrationPage.test.tsx` | — |
| 6 | `REQ-KMLK-006` SMS seçilip kod ulaşmazsa kullanıcı aynı ekrandan e-posta kanalına geç… | SYG-KMLK-019 | `ADR-0006` §1, `KR-085` | SYG-019 (kanal değişimi önceki kodu geçersiz kılar, hız sınırına tabi) · PR #88 | `RegistrationApiTests` | — |
| 7 | `REQ-KMLK-007` Kurumsal e-posta adresi bulunmayan personel için e-posta kanalı sunulm… | SYG-KMLK-017 | `ADR-0006` §1, `KR-085` | SYG-017 · PR #88 | `RegistrationApiTests` | — |
| 8 | `REQ-KMLK-008` Kurumsal alan adı dışındaki e-posta adreslerine (gmail, hotmail vb.) d… | SYG-KMLK-009, 017 | `ADR-0003` §5 | kısmen: SYG-009 (normalleştirme) · PR #75; SYG-017 · PR #88 | `ContactNormalizerTests`, `PersonnelSynchronizerTests`; `RegistrationApiTests` | — |
| 9 | `REQ-KMLK-009` Aynı e-posta adresi birden fazla kişiye tanımlıysa o adrese doğrulama … | SYG-KMLK-008, 017 | `ADR-0003` §5 | kısmen: SYG-008 (paylaşılan adres işaretleme; yalnızca aktif kişiler, `KR-079`) · PR #75, #78; SYG-017 (ortak adresle üyelik yok) · PR #88 | `PersonnelSynchronizerTests`; `RegistrationApiTests` | — |
| 10 | `REQ-KMLK-010` Cep telefonu tanımlı olmayan personel için SMS kanalı sunulmaz. | SYG-KMLK-009, 017 | `ADR-0003` §5 | kısmen: SYG-009 (telefon doğrulama) · PR #75; SYG-017 (telefonu olmayana yalnızca e-posta; KR-078) · PR #88 | `ContactNormalizerTests`, `PersonnelSynchronizerTests`; `RegistrationApiTests` | — |
| 11 | `REQ-KMLK-011` İK, personele tek kullanımlık parola bağlantısı gönderebilir; yalnızca LOGO'daki kurumsal e-postaya… | SYG-KMLK-030, 051, 052, 053, 073, 074 | `ADR-0012` §3 | kısmen: SYG-030 (SMTP gönderici) · PR #86 — 051–053 (davet), 073, 074 sonraki işler; SYG-074 (kullanıcı–rol–izin, sabit izinler, `[HasPermission]`, hazır iki rol, ilk sistem yöneticisi yapılandırmayla) · PR #101; SYG-073 (arama, hesap durumu, elle pasife alma ve aktifleştirme; yalnızca ad, soyad, sicil, firma) · PR #106; SYG-051–053 (yalnızca LOGO'daki kurumsal ve kişiye tekil adrese tek kullanımlık bağlantı; 256 bit jeton, özeti saklanır, 3 saat, yenisi öncekileri geçersiz kılar; gerekçe zorunlu, denetim izinde; bağlantıyla hesap oluşturma veya parola yenileme) · PR #108 | `SmtpEmailSenderTests`; `AccessControlApiTests`, `AccessControlServiceTests`; `AccountsApiTests`, `AccountsPage.test.tsx`; `InvitationApiTests`, `AccountInvitationTests`, `InvitePage.test.tsx` | — |
| 12 | `REQ-KMLK-012` Kabul edilen kurumsal alan adları koda gömülmez; Sistem Yönetimi param… | SYG-KMLK-017, 075, 076 | `ADR-0008` §7 | SYG-075 (parametre deposu, şifreli sırlar) · PR #84; SYG-017 (alan adı parametresi) · PR #88; SYG-076 (yalnızca T3 parametreleri; türe ve aralığa göre doğrulama, Türkçe iletiler; sırlar yalnızca yazılabilir; `system.parameter.view/update`) · PR #110 | `ParameterCatalogTests`, `SystemParameterStoreTests`, `AesGcmSecretProtectorTests`; `RegistrationApiTests`; `SystemParametersApiTests`, `ParametersPage.test.tsx` | — |
| 13 | `REQ-KMLK-013` TCKN’si bulunmayan LOGO kartı için sistemde Kişi kaydı oluşturulmaz; k… | SYG-KMLK-007 | `ADR-0003` §5, `KR-043` | `PersonnelSynchronizer` · PR #75 | `PersonnelSynchronizerTests`, `PersonnelSyncStoreTests`, `NationalIdTests` | — |
| 14 | `REQ-KMLK-014` Bir kişinin birden fazla sicil numarası olsa da TEK hesabı olur. | SYG-KMLK-001, 020, 021 | `ADR-0005` §1–2, `ADR-0006` | kısmen: SYG-001 (kişi/istihdam ayrımı) · PR #75; SYG-021 (kişiye tek hesap, tekillik kısıtı) · PR #84; SYG-020 (mevcut hesap, ikinci hesap yok) · PR #88 | `PersonAndEmploymentTests`, `PersonnelSynchronizerTests`, `PersonnelSyncStoreTests`, `UserAccountTests`; `RegistrationApiTests` | — |
| 15 | `REQ-KMLK-015` Doğrulama kodu 6 hanedir (parametre: PRM-KML-09) ve kriptografik güven… | SYG-KMLK-022 | `ADR-0006` §3 | SYG-022 · PR #86 | `VerificationCodeTests`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests` | — |
| 16 | `REQ-KMLK-016` Kod 5 dakika geçerlidir (parametre: PRM-KML-10). | SYG-KMLK-023, 079 | `ADR-0006` §3 | kısmen: SYG-023 (süre, "süresi doldu" sonucu) · PR #86 — 028 (geri sayım ekranı) üyelik işinde | `VerificationCodeTests`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests` | — |
| 17 | `REQ-KMLK-017` Kod en fazla 3 kez yanlış girilebilir; aşılırsa kod iptal edilir ve ye… | SYG-KMLK-024 | `ADR-0006` §3 | SYG-024 · PR #86 | `VerificationCodeTests`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests` | — |
| 18 | `REQ-KMLK-018` Kod veritabanında şifrelenmiş (hash) saklanır; düz metin tutulmaz ve h… | SYG-KMLK-025, 026 | `ADR-0006` §3, `KR-082` | SYG-025 (HMAC-SHA256, anahtar ortam değişkeninde), 026 (kod günlüğe, denetim izine, gönderim kaydına yazılmaz) · PR #86 | `VerificationCodeTests`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests`, `NotificationDispatcherTests` | — |
| 19 | `REQ-KMLK-019` Doğrulanan kod anında geçersiz kılınır (tek kullanımlık). | SYG-KMLK-027 | `ADR-0004` §4 | SYG-027 (satır sürümüyle tek kullanım) · PR #86 | `VerificationCodeTests`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests` | — |
| 20 | `REQ-KMLK-020` Kod ekranında kalan süre görünür ve "kodu tekrar gönder" seçeneği bulu… | SYG-KMLK-028 | `ADR-0015` | SYG-028 (sunucunun bitiş anından geri sayım, tekrar gönder) · PR #91 | `useCountdown.test.ts`, `RegistrationPage.test.tsx` | — |
| 21 | `REQ-KMLK-021` SMS gönderimleri NetGSM standart servisi üzerinden, DUZEN başlığıyla y… | SYG-KMLK-029, 075, 079 | `ADR-0008` §7; `ADR-0012` §2, `analiz/02` | kısmen: SYG-075 (parametre deposu, şifreli sırlar) · PR #84 — SYG-076 (parametre ekranı) sonraki iş; SYG-029 (NetGSM REST v2, tüm hata kodları), 079 (yeniden deneme, gönderim kaydı) · PR #86 — 079 ölçümü UAT'de | `ParameterCatalogTests`, `SystemParameterStoreTests`, `AesGcmSecretProtectorTests`; `NetGsmSmsSenderTests`, `NotificationDispatcherTests` | — |
| 22 | `REQ-KMLK-022` Kullanıcı kurumsal e-posta adresi ve parolasıyla giriş yapar. | SYG-KMLK-008, 031, 077 | `ADR-0006` §8 | kısmen: SYG-008 (paylaşılan adres işaretleme; yalnızca aktif kişiler, `KR-079`) · PR #75, #78; SYG-031 (giriş kimliği kurumsal e-posta, büyük/küçük harf duyarsız; TCKN/sicil kabul edilmez) · PR #94; giriş ekranı · PR #96 | `PersonnelSynchronizerTests`; `SessionApiTests`; `LoginPage.test.tsx` | — |
| 23 | `REQ-KMLK-023` Hatalı girişte "kullanıcı adı veya parola hatalı" denir; hangisinin ya… | SYG-KMLK-032 | `ADR-0006` §5 | SYG-032 (aynı yanıt; var olmayan kullanıcı için de parola özeti) · PR #94 | `SessionApiTests` | — |
| 24 | `REQ-KMLK-024` 5 başarısız giriş denemesinden sonra hesap 15 dakika kilitlenir (param… | SYG-KMLK-033 | `ADR-0006` §5 | SYG-033 (girilen e-posta başına 5 hatada 15 dk kilit; doğru parola da reddedilir) · PR #94 | `SessionApiTests`, `SessionDomainTests` | — |
| 25 | `REQ-KMLK-025` Giriş sonrası ikinci doğrulama adımı (2FA) GELİŞTİRİLİR; varsayılan ol… | SYG-KMLK-034 | `ADR-0006` §7 | SYG-034 (2FA parametreyle; üyelikle aynı kod altyapısı) · PR #94; iki adımlı giriş ekranı (kanal ve kod adımları üyelikle ortak) · PR #96 | `SessionApiTests`; `LoginPage.test.tsx` | — |
| 26 | `REQ-KMLK-026` Oturum 15 dakikalık erişim jetonu ve 8 saatlik yenileme jetonu ile yön… | SYG-KMLK-037, 038, 040, 063 | `ADR-0006` §8 | kısmen: SYG-037 (JWT 15 dk, yenileme jetonu HttpOnly/Secure/SameSite=Strict çerez, 8 sa üst sınır), 038 (hareketsizlik 30 dk; yenileme saymaz), 040 (her kullanımda yenileme, yeniden kullanımda tüm oturumlar kapanır) · PR #94; istemci: erişim jetonu yalnızca bellekte, açılışta çerezle geri getirme, sekme içinde tek yenileme ve sekmeler arasında kilitle sıralı yenileme (jeton tekrarı yaşanmaz), kullanıcı etkileşimine göre sinyal, hareketsizlik uyarısı · PR #96 — 063 sonraki iş | `SessionApiTests`, `SessionDomainTests`; `sessionManager.test.ts`, `clientAuth.test.ts`, `SessionActivity.test.tsx` | — |
| 27 | `REQ-KMLK-027` Çıkış yapıldığında oturum sunucu tarafında da sonlandırılır. | SYG-KMLK-043 | `ADR-0006` §8 | SYG-043 (çıkışta sunucuda iptal) · PR #94; çıkış menüsü, açık diğer sekmeler de girişe döner, sorgu önbelleği boşaltılır · PR #96 | `SessionApiTests`; `sessionManager.test.ts`, `RequireSession.test.tsx` | — |
| 28 | `REQ-KMLK-028` Parola en az 6 karakter olmalıdır; karmaşıklık (büyük/küçük harf, raka… | SYG-KMLK-044, 075, 076 | `ADR-0008` §7; `ADR-0006` §6 | SYG-075 (parametre deposu, şifreli sırlar) · PR #84; SYG-044 (uzunluk, karmaşıklık, NFKC) · PR #88; SYG-076 (yalnızca T3 parametreleri; türe ve aralığa göre doğrulama, Türkçe iletiler; sırlar yalnızca yazılabilir; `system.parameter.view/update`) · PR #110 | `ParameterCatalogTests`, `SystemParameterStoreTests`, `AesGcmSecretProtectorTests`; `PasswordPolicyTests`; `SystemParametersApiTests`, `ParametersPage.test.tsx` | — |
| 29 | `REQ-KMLK-029` Yaygın sızıntı listesinde bulunan parolalar kabul edilmez; kontrol çev… | SYG-KMLK-045 | `ADR-0006` §6 | SYG-045 (143.672 kayıtlık açık lisanslı liste, kurum ve kişi sözcükleri) · PR #88 | `PasswordPolicyTests`, `PasswordInfrastructureTests`, `RegistrationApiTests` | — |
| 30 | `REQ-KMLK-030` Zorunlu periyodik parola değişimi varsayılan olarak uygulanmaz (parame… | SYG-KMLK-046 | `ADR-0006` §6, `KR-092` | SYG-046 (`PRM-KML-07` açıkken `PRM-KML-21` günden eski parola girişte değiştirilir; değişene kadar oturum yalnızca parola ve oturum uçlarını kullanır, sunucuda) · PR #114 | `PasswordChangeRequirementApiTests`, `UserAccountTests`, `SessionDomainTests`; `ChangePasswordPage.test.tsx`, `RequireSession.test.tsx`, `sessionManager.test.ts` | — |
| 31 | `REQ-KMLK-031` Parola sıfırlama, üyelikle aynı doğrulama akışını kullanır (kanal seçi… | SYG-KMLK-047 | `ADR-0006` §6, `KR-088` | SYG-047 (üyelikle aynı deneme, eşleştirme, kanal, kod ve hız sınırları; yalnızca amaç ve ileti metni farklı; eşleşme gizliliği; hesabın tüm oturumları kapanır, giriş kilidi kalkar; elle pasif hesapta reddedilir; üyelikte "hesabınız var" sonucundan doğrudan sıfırlama; "Parolamı unuttum" ekranı) · PR #99 | `PasswordApiTests`, `RegistrationAttemptTests`; `ForgotPasswordPage.test.tsx`, `RegistrationPage.test.tsx`, `passwordApi.test.ts` | — |
| 32 | `REQ-KMLK-032` Oturum içinde parola değiştirirken mevcut parola sorulur. | SYG-KMLK-048 | `ADR-0006` §6, `KR-088` | SYG-048 (mevcut parola zorunlu; yanlış deneme giriş kilidi sayacına eklenir; yeni parola politikaya uyar ve mevcut parolayla aynı olamaz; bu oturum açık kalır, diğerleri `session-ended/password-changed` ile kapanır; "Parolamı değiştir" ekranı) · PR #99 | `PasswordApiTests`, `SessionDomainTests`; `ChangePasswordPage.test.tsx`, `RequireSession.test.tsx`, `LoginPage.test.tsx` | — |
| 33 | `REQ-KMLK-033` Parola geri döndürülebilir biçimde saklanmaz ve hiçbir kayda yazılmaz. | SYG-KMLK-049 | `ADR-0006` §6 | kısmen: SYG-049 (PBKDF2-HMAC-SHA512, 210.000 yineleme) · PR #88 — 100–500 ms ölçümü UAT'de | `PasswordInfrastructureTests` | — |
| 34 | `REQ-KMLK-034` Personelin tüm aktif istihdamları sona erdiğinde hesabı otomatik pasif… | SYG-KMLK-004, 005, 054, 055, 078 | `ADR-0003` §4, `ADR-0006` | kısmen: SYG-004 (motor, kilit, periyodik çalışma), 005 (çalışma kaydı) · PR #75, #76; SYG-054 (hesap pasifleşmesi, güvenlik damgası; periyot parametreden) · PR #84 — SYG-054 jeton iptali ve 055 (giriş reddi) giriş işinde, 078 (performans ölçümü) sonraki işler; SYG-054 (pasifleşmede açık oturumlar her istekte kapanır), 055 (pasif hesapla giriş yok) · PR #94 | `PersonnelSynchronizerTests`, `PersonnelSyncStoreTests`, `PersonnelSyncRunTests`, `PersonnelSyncWorkerTests`, `AccountLifecycleTests`, `UserAccountTests`; `SessionApiTests` | — |
| 35 | `REQ-KMLK-035` Personel yeniden işe girdiğinde mevcut hesabı yeniden aktifleşir; yeni… | SYG-KMLK-056 | `ADR-0006`, `KR-080` | SYG-056 · PR #84 | `AccountLifecycleTests`, `UserAccountTests`, `PersonnelSyncStoreTests` | — |
| 36 | `REQ-KMLK-036` İK, bir hesabı elle pasife alabilir; gerekçe zorunludur. | SYG-KMLK-057, 073, 074 | `ADR-0006`, `KR-080` | kısmen: SYG-057 (domain kuralı: gerekçe zorunlu, veritabanı kısıtı) · PR #84 — 073 (İK ekranı), 074 (yetki) sonraki işler; SYG-074 (`identity.account.update` izni ve İK Kimlik İşlemleri rolü) · PR #101; SYG-057, 073 (İK ekranından gerekçeyle pasife alma ve aktifleştirme; açık oturumlar hemen kapanır; aktif istihdamı olmayan aktifleştirilemez; denetim izi ve erişim kaydı) · PR #106 | `UserAccountTests`, `PersonnelSyncStoreTests`; `AccessControlApiTests`; `AccountsApiTests`, `AccountsPage.test.tsx` | — |
| 37 | `REQ-KMLK-037` Hesap durum değişiklikleri (açılma, pasifleşme, kilitlenme) denetim iz… | SYG-KMLK-053, 058 | `ADR-0009` §2 | kısmen: SYG-058 (pasifleşme ve aktifleşme denetim izinde) · PR #84 — kilitlenme giriş işinde, 053 (davet) sonraki iş; SYG-058 kilitlenme ve kilit kalkması · PR #94 | `PersonnelSyncStoreTests`; `SessionApiTests` | — |
| 38 | `REQ-KMLK-038` Üyelik denemeleri TCKN başına saatte 5, IP başına saatte 20 ile sınırl… | SYG-KMLK-059 | `ADR-0006` §5, `KR-085` | SYG-059 (TCKN ve IP başına saatlik sınır, 429; IP ters vekilden) · PR #88 | `RegistrationApiTests`, `ReverseProxyRegistrationTests` | — |
| 39 | `REQ-KMLK-039` Kod gönderimi kişi başına 15 dakikada en fazla 3 kez yapılabilir; sını… | SYG-KMLK-059 | `ADR-0006` §5 | kısmen: SYG-059 (kişi başına 15 dakikada kod sınırı, olay günlüğü) · PR #86 — 429 yanıtı ve üyelik deneme sınırları üyelik işinde; SYG-059 (üyelikte TCKN özetine göre kod sınırı, 429) · PR #88 | `VerificationCodeServiceTests`, `VerificationCodeStoreTests`; `RegistrationApiTests` | — |
| 40 | `REQ-KMLK-040` Tüm kimlik olayları (giriş, başarısız giriş, kilitlenme, kod gönderimi… | SYG-KMLK-005, 010, 060 | `ADR-0003` §4, `ADR-0009` | kısmen: SYG-005 (çalışma kaydı), 010 (günlük, `/health/sync`) · PR #75, #76 — SYG-060 (kimlik olayları) sonraki işler | `PersonnelSyncServiceTests`, `PersonnelSyncStoreTests`, `PersonnelSyncHealthCheckTests` | — |
| 41 | `REQ-KMLK-041` Kişisel veriler günlük kayıtlarında maskelenir (TCKN 123*901, telefon … | SYG-KMLK-026, 061 | `ADR-0009` §4, `KR-083` | kısmen: SYG-026 (kod ve alıcı günlüğe yazılmaz, alıcı maskeli) · PR #86 — 061 kimlik olayları işinde | `NotificationDispatcherTests` | — |
| 42 | `REQ-KMLK-042` Doğrulama kodu ve parola sıfırlama iletileri bildirim istisnasından MU… | SYG-KMLK-062 | — | — | — | — |
| 43 | `REQ-KMLK-043` Tüm ekranlar ve hata mesajları Türkçedir; teknik terim kullanılmaz. | SYG-KMLK-064, 071 | `ADR-0010`, `KR-084`; `ADR-0015` | kısmen: SYG-071 (`/api/v1/identity/`, OpenAPI, Problem Details) · PR #88 — 064 (ekranlar) sonraki iş; SYG-064 (üyelik ekranları ve sunucu iletileri Türkçe, i18n) · PR #91; giriş ekranı ve oturum sonu iletileri · PR #96 | `RegistrationApiTests`; `LoginPage.test.tsx` | — |
| 44 | `REQ-KMLK-044` Giriş ve üyelik ekranları telefon ve tablette de kullanılabilir. | SYG-KMLK-065 | `ADR-0015` | kısmen: SYG-065 (düzen 360 px'ten itibaren tek sütun, sabit genişlik yok) · PR #91; giriş ekranı aynı düzende · PR #96 — ölçüm kabulde (Gösterim) | — | — |
| 45 | `REQ-KMLK-045` Ekranlar klavye ile kullanılabilir; form alanlarının etiketi vardır. | SYG-KMLK-066 | `ADR-0015` §7 | kısmen: SYG-066 (etiketli alanlar, klavyeyle akış, adım başlığına odak) · PR #91; giriş ekranı · PR #96 | `RegistrationPage.test.tsx`, `LoginPage.test.tsx` | — |
| 46 | `REQ-KMLK-046` Hata ekranında kullanıcıya bir takip numarası gösterilir. | SYG-KMLK-067 | `ADR-0009` §2 | kısmen: SYG-067 (üyelikte beklenmeyen hatada takip numarası) · PR #91; girişte de · PR #96 | `RegistrationPage.test.tsx`, `LoginPage.test.tsx` | — |
| 47 | `REQ-KMLK-047` Giriş, üye ol, doğrulama ve parola ekranları ekranı iki bölüme ayıran … | SYG-KMLK-068 | `ADR-0015` | SYG-068 (iki bölümlü düzen, 900 px altında form üstte; kurumsal görsel S-13 gereği kodla üretilen canlandırılmış sahne) · PR #91 — ekranda doğrulama kabulde (Gösterim) | — | — |
| 48 | `REQ-KMLK-048` Giriş ve doğrulama ekranlarında kurumsal logo kullanılır; logo, Sistem… | SYG-KMLK-069, 076 | `ADR-0015` | SYG-069 (varsayılan logo ve alternatif metni; kopya CI'da denetlenir) · PR #91; SYG-069, 076 (logo parametre ekranından yüklenir: yalnızca PNG/JPEG, içerik imzasıyla denetlenir, en fazla 512 KB; yüklenmemişse varsayılan; sürümle önbellek) · PR #110 | `RegistrationPage.test.tsx`, `brand-asset-check.mjs`; `SystemParametersApiTests`, `BrandingServiceTests`, `AuthLayout.test.tsx`, `ParametersPage.test.tsx` | — |
| 49 | `REQ-KMLK-049` Bir kullanıcı yeni bir tarayıcıdan giriş yaptığında önceki oturumu son… | SYG-KMLK-041 | `ADR-0006` §8 | SYG-041 (tek aktif oturum; parametreyle kapatılabilir) · PR #94 | `SessionApiTests` | — |
| 50 | `REQ-KMLK-050` Oturumun başka bir yerden sonlandırıldığı kullanıcıya açıkça bildirili… | SYG-KMLK-042 | `ADR-0006` §8 | kısmen: SYG-042 (makine tarafından okunabilir neden kodu `session-ended/signed-in-elsewhere`) · PR #94; kullanıcı iletisi ve diğer oturum sonu nedenleri giriş ekranında · PR #96 | `SessionApiTests`; `LoginPage.test.tsx` | — |
| 51 | `REQ-KMLK-051` 2FA açıkken, parola doğrulandıktan sonra kullanıcıdan doğrulama kodu i… | SYG-KMLK-034 | `ADR-0006` §7 | SYG-034 (2FA kodu üyelikle aynı altyapı) · PR #94 | `SessionApiTests` | — |
| 52 | `REQ-KMLK-052` 2FA açıkken hiçbir doğrulama kanalı bulunmayan kullanıcı sisteme girem… | SYG-KMLK-035 | `ADR-0006` §7 | SYG-035 (2FA açılmadan önce, girişle aynı kanal kuralına göre hiçbir doğrulama kanalı olmayan aktif hesap sahibi sayısı; uyarı açıkken onaysız açma sunucuda reddedilir; parametre ekranında onay penceresi) · PR #112 | `SystemParametersApiTests`, `TwoFactorImpactServiceTests`, `ParametersPage.test.tsx` | — |
| 53 | `REQ-KMLK-053` 2FA'nın açık ve kapalı hâli (PRM-KML-08) ayrı ayrı test edilir. | SYG-KMLK-036 | `ADR-0006` §7 | SYG-036 (2FA kapalı ve açık hâl ayrı testler) · PR #94 | `SessionApiTests` | — |
| 54 | `REQ-KMLK-054` Meşru uzun süreli etkinlik sırasında (ör. eğitim videosu oynatılırken)… | SYG-KMLK-038, 039 | `ADR-0006` §8 | kısmen: SYG-038, 039 (etkinlik sinyali ucu, dakikada 2, toplam süreyi uzatmaz) · PR #94; istemci sinyali: görünür sekmede kullanıcı etkileşimi ve oynayan medya, dakikada en fazla 1 (AN-23) · PR #96 | `SessionApiTests`, `SessionDomainTests`; `SessionActivity.test.tsx` | — |
| 55 | `REQ-KMLK-055` İlk girişte parola değiştirme zorunluluğu Sistem Yönetimi parametresiy… | SYG-KMLK-050 | `KR-092` | SYG-050 (hiç giriş yapmamış her hesap; ilk giriş anı hesapta, mevcut hesaplar oturum kayıtlarından dolduruldu; değiştirmeden çıkılırsa zorunluluk sürer) · PR #114 | `PasswordChangeRequirementApiTests`, `UserAccountTests`; `ChangePasswordPage.test.tsx`, `RequireSession.test.tsx` | — |
| 56 | `REQ-KMLK-056` Üyelik ve giriş ekranlarında "sorun yaşarsanız" başvurulacak birim ola… | SYG-KMLK-070, 076 | `ADR-0015` | SYG-070 (üyelik ekranında PRM-GRN-04; `/api/v1/identity/public-settings`) · PR #91; giriş ekranında da · PR #96; SYG-076 (destek bilgisi parametre ekranından değiştirilir) · PR #110 | `RegistrationPage.test.tsx`, `LoginPage.test.tsx`, `RegistrationApiTests`; `ParametersPage.test.tsx` | — |

---

## 6. Kapsama ölçütü

Aşağıdaki oran, her modül kapanışında ölçülür ve öz değerlendirmeye girer:

```
İzlenebilirlik kapsaması = zinciri eksiksiz kalem / toplam kalem
```

Bir kalem, **kaynağından testine kadar** her halkası dolu (veya gerekçeli `yok`) ise
"eksiksiz" sayılır.

**A1 ölçümü:** 8 kalemden 8'i eksiksiz (kabul halkası hariç — sırası gelmedi) → **%100**.
LOGO kalemi zinciri tamamlanmamış olduğu için §4.1'de ayrıca listelendi ve bu orana
dâhil edilmedi. **26.09.2026: zinciri PR #76 ile tamamlandı** (§4.1).

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-18 | 0.1 | İlk oluşturma — yapı, bakım kuralı ve A1 kalemleri (BULGU-01) | Bilgi İşlem |
| 2026-09-24 | 0.2 | §5 T3 satırları açıldı — 23.09.2026 İK toplantısında onaylanan 56 gereksinim | Bilgi İşlem |
| 2026-09-24 | 0.3 | Sistem gereksinimi kimliği `SYG-<MODÜL>-nnn`; §5 "Sistem gereksinimi" sütunu SYG-KMLK ile dolduruldu; LOGO kalemi T1 → T3 (`KR-077`) | Bilgi İşlem |
| 2026-09-26 | 0.4 | §5: PR #75 ile 10 satırın tasarım, gerçekleştirme ve doğrulama sütunları dolduruldu (T3 ilk parça: kişi/istihdam modeli ve senkronizasyon motoru); kısmen karşılanan satırlar "kısmen" olarak işaretlendi | Bilgi İşlem |
| 2026-09-26 | 0.5 | §4.1 LOGO salt-okunur kalemi kapandı (PR #76: CI'da yazma reddi kanıtı); §5 REQ-KMLK-003, 034, 040 satırları güncellendi | Bilgi İşlem |
| 2026-09-26 | 0.6 | REQ-KMLK-009 ve 022 satırlarına PR #78 eklendi (`KR-079`) | Bilgi İşlem |
| 2026-09-26 | 0.7 | §5: REQ-KMLK-005, 012, 014, 021, 028, 034, 035, 036, 037 satırlarına parametre deposu ve hesap yaşam döngüsü halkaları eklendi (#83, PR #84) | Bilgi İşlem |
| 2026-09-27 | 0.8 | §5: REQ-KMLK-004, 005, 011, 015–019, 021, 039, 041 satırlarına doğrulama kodu ve iletim altyapısı halkaları eklendi (#85, PR #86) | Bilgi İşlem |
| 2026-09-27 | 0.9 | §5: üyelik akışı API'si ve parola politikası halkaları eklendi (REQ-KMLK-001–010, 012, 014, 028, 029, 033, 038, 039, 043; #87, PR #88) | Bilgi İşlem |
| 2026-09-27 | 1.0 | §5: üyelik ekranları halkaları eklendi (REQ-KMLK-002, 004, 005, 020, 043–048, 056; #90, PR #91) | Bilgi İşlem |
| 2026-09-28 | 1.1 | §5: giriş ve oturum API'si halkaları eklendi (REQ-KMLK-022–027, 034, 037, 049–051, 053, 054; #92, PR #94) | Bilgi İşlem |
| 2026-09-28 | 1.2 | §5: giriş ekranı ve istemci oturum yönetimi halkaları eklendi (REQ-KMLK-022, 025–027, 043–046, 050, 054, 056; #95, PR #96) | Bilgi İşlem |
| 2026-09-29 | 1.3 | §5: parola sıfırlama ve oturum içinde parola değişikliği halkaları eklendi (REQ-KMLK-031, 032; #98, PR #99) | Bilgi İşlem |
| 2026-09-29 | 1.4 | §5: eylem yetkisi altyapısı ve senkronizasyon uçları halkaları eklendi (REQ-KMLK-003, 011, 036; #100, PR #101) | Bilgi İşlem |
| 2026-09-30 | 1.5 | §5: İK hesap işlemleri ekranı halkaları eklendi (REQ-KMLK-011, 036; #105, PR #106) | Bilgi İşlem |
| 2026-09-30 | 1.6 | §5: İK davet bağlantısı halkaları eklendi (REQ-KMLK-011; #107, PR #108) | Bilgi İşlem |
| 2026-09-30 | 1.7 | §5: parametre ekranı ve kurumsal logo halkaları eklendi (REQ-KMLK-012, 028, 048, 056; #109, PR #110) | Bilgi İşlem |
| 2026-10-01 | 1.8 | §5: 2FA etki uyarısı halkası eklendi (REQ-KMLK-052; #111, PR #112) | Bilgi İşlem |
| 2026-10-01 | 1.9 | §5: zorunlu parola değişimi halkaları eklendi (REQ-KMLK-030, 055; #113, PR #114) | Bilgi İşlem |
