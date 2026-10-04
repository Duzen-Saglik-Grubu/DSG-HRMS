# T3 Kimlik Yönetimi — Doğrulama Raporu

**Belge kimliği:** TEC.9-RPR-2026-10-04-T3
**Süreç:** TEC.9 — Doğrulama (`TEC.9.BP3`, `TEC.9.BP4`)
**Doğrulanan:** T3 Kimlik Yönetimi, `main` @ `fb58fbe` (PR #142 dahil); UAT'de `2d79f01` ve sonrası
**Ölçüt:** `SYG-KMLK` 1.1, 79 sistem gereksinimi (`TEC.3`)
**CI çalışması:** [37197387185](https://github.com/Duzen-Saglik-Grubu/DSG-HRMS/actions/runs/37197387185) — başarılı (04.10.2026)
**Tarih:** 2026-10-04
**Hazırlayan:** Bilgi İşlem

> Doğrulama, ürünün **doğru yapılıp yapılmadığını** gösterir (ADR-0011 §8). Doğru ürünün
> yapılıp yapılmadığını, yani İK kabulünü, TEC.11 geçerlemesi gösterir; bu rapor onun
> yerine geçmez.

---

## 1. Özet

| Ölçü | Değer |
|---|---|
| Sistem gereksinimi | 79 |
| Kanıtı olan | **79** (CI'da her PR'da denetleniyor, #122) |
| Testle doğrulanan | 71 (yöntem: Test) |
| Ölçümle doğrulanan | 3 (yöntem: Analiz; SYG-077, 078, 079) |
| Testle desteklenen inceleme / gösterim | 5 (006, 064 İnceleme; 028, 068, 073 Gösterim) |
| Açık nokta | 1 kısmi (SYG-064); 3 gösterim kabulde (TEC.11) |
| Backend testi | 767 geçti (Domain 136, Application 280, Infrastructure 75, Mimari 10, Entegrasyon 266) |
| Ön yüz testi | 232 geçti |
| Kod kapsamı | Backend genel **%92,9** (eşik %75), Domain **%98,9** (eşik %90), ön yüz satır **%88,5** |

**Sonuç:** T3'ün 79 sistem gereksiniminden 75'i doğrulandı. SYG-064 kısmen doğrulandı.
SYG-028, 068 ve 073 testle doğrulandı; gösterim yöntemi gereği İK'ya kabulde
gösterilecek. G2 kapısı için önerilen karar §5'tedir.

---

## 2. Kapsam ve yöntem

### 2.1 Doğrulama yöntemleri

`TEC.9-dogrulama/YAKLASIM.md` §3'te tanımlıdır.

| Yöntem | Kanıt | Bu çevrimde |
|---|---|---|
| **Test** | Otomatik test (birim, entegrasyon, bileşen); CI'da her PR'da çalışır | 71 SYG |
| **Analiz** | Ölçüm veya hesap; yazılı rapor | 3 SYG ([performans ölçüm raporu](2026-10-02-t3-performans-olcumu.md)) |
| **İnceleme** | Belge veya kodun bir ölçüte karşı gözden geçirilmesi; bulgular kayıtlı | 2 SYG |
| **Gösterim** | Davranışın çalışan sistemde gösterilmesi | 3 SYG |

### 2.2 Kanıtın nasıl bulunduğu

Tablodaki kanıt sütunu, gereksinim kimliğini anan test dosyalarından üretildi. Aynı
eşleme CI'da her PR'da denetleniyor: kanıtı olmayan bir SYG kapıdan geçemiyor
(`requirement-traceability-check.mjs`, 5. denetim). Bir test dosyası birden fazla
gereksinimi doğrulayabilir; dörtten fazla dosya varsa ilk dördü gösterilir.

### 2.3 Sonucun anlamı

"✅ Geçti": gereksinimi anan testlerin tamamı yukarıdaki CI çalışmasında geçti.
CI çalışmasının kayıtları GitHub'da 90 gün saklanır; bu rapor o kaydın kalıcı özetidir
(#136).

---

## 3. Zorunlu test türleri (ADR-0011 §2)

| Tür | Durum | Kanıt |
|---|---|---|
| İş kuralı birim testleri | ✅ | Domain 136, Application 280 |
| API entegrasyon testleri (ana akış ve doğrulama hataları) | ✅ | 266 entegrasyon testi; gerçek PostgreSQL (Testcontainers) |
| Yetki sızıntısı testi | ✅ | `AccessControlApiTests`, `AccountsApiTests`, `SystemParametersApiTests` (izinsiz istek `403`); `PasswordChangeRequirementApiTests` |
| Maskeleme testi | ✅ | `IdentityLogMaskingTests` (günlük dosyası okunur), `SecurityEventApiTests`, `NotificationDispatcherTests` |
| Ön yüz bileşen testleri | ✅ | 232 test |
| Uçtan uca test (5–8 senaryo, Playwright) | ✅ (1.1) | 7 kimlik senaryosu: üyelik, giriş/çıkış, hatalı giriş, parola değiştirme, sıfırlama, İK daveti, yetki (#146). İlk sürümde yoktu |

---

## 4. Ek doğrulamalar

### 4.1 Performans (Analiz)

[Performans ölçüm raporu](2026-10-02-t3-performans-olcumu.md) 0.2:
- **SYG-077 (giriş):** 60 saniyeye yayılan 50 girişte p95 0,279 sn; aynı anda 50 girişte p95 4,42 sn.
- **SYG-078 (senkronizasyon):** p95 0,43 sn.
- **SYG-079 (kod teslimi):** SMS p95 0,50 sn, e-posta p95 0,43 sn.

### 4.2 Gerçek tarayıcıda bulunan hata

Davet bağlantısı hatası (#137) birim testlerinde görünmüyordu, kullanıcı UAT'de buldu.
Gerçek tarayıcıda yeniden üretildi, düzeltildi ve regresyon testiyle kapatıldı (PR #139).
Bu, uçtan uca test eksiğinin (§3) somut sonucudur.

### 4.3 Dar ekran (SYG-KMLK-065)

jsdom sayfa düzeni hesaplamadığı için bileşen testleri yatay kaydırmayı ölçemez.
04.10.2026'da yerel üretim derlemesinde, gerçek tarayıcıda (Edge, başsız) ölçüldü:

| Genişlik | `/login` | `/register` | `/forgot-password` | `/invite` |
|---|---|---|---|---|
| 360 px | yok | yok | yok | yok |
| 390 px | yok | yok | yok | yok |
| 768 px | yok | yok | yok | yok |

"Yok": `scrollWidth = clientWidth`. Ölçüm her akışın ilk adımını kapsar; kod ve parola
adımları kabulde telefonla denenecek (#124).

---

## 5. G2 kapı kaydı — "Geliştirme tamam"

**Kapı:** G2 (proje planı §3.1). **Karar veren:** Bilgi İşlem. **Ölçüt:** ADR-0011 §6'daki CI kalite kapılarının tamamı.

| Kapı (ADR-0011 §6) | Durum | Kanıt |
|---|---|---|
| Derleme (uyarı yok) | ✅ | CI 37197387185 |
| Birim ve entegrasyon testleri | ✅ | 767 + 232 geçti |
| Kapsam eşiği (genel %75, Domain %90) | ✅ | %92,9 / %98,9 |
| Mimari testi | ✅ | 10 geçti |
| Statik analiz (.NET, ESLint) | ✅ | CI |
| Sır taraması (gitleaks) | ✅ | CI |
| Bağımlılık güvenliği | ✅ | CI |
| Konteyner taraması (trivy) | ✅ | CI (api, web) |
| Migration kontrolü | ✅ | CI "Bekleyen model değişikliği yok" |
| Biçim (`dotnet format`, Prettier) | ✅ | CI |
| Kod gözden geçirme: en az bir onay | ✅ (1.2) | 65 PR'ın onayı geriye dönük kayda geçti (`MAN.8-kalite-guvence/kayitlar/2026-10-04-pr-onay-kaydi.md`). Bundan sonra onay yorumu CI'da denetleniyor (`KR-096`, #128) |
| Gereksinim izlenebilirliği (proje ek kapısı) | ✅ | REQ → SYG → test, CI'da |

**Önerilen karar: KOŞULLU GEÇTİ.**
- Otomatik kapıların tamamı geçti.
- **Koşul 1:** ~~Kod gözden geçirme kanıtı~~ — karşılandı (#128, 1.2).
- **Koşul 2:** ~~Uçtan uca test eksiği~~ — karşılandı (#146, 1.1).
- **Koşul 3:** SYG-064 incelemesi yapıldı (1.3); bulgular için karar bekleniyor.

| Karar | Tarih | Karar veren |
|---|---|---|
| _Bekliyor_ | | Doğuş Uçanok (Bilgi İşlem) |

---

## 6. Açık noktalar

| # | Nokta | Etki | İzleme |
|---|---|---|---|
| 1 | ~~Uçtan uca test yok~~ | **Kapandı (1.1):** 7 senaryo her PR'da çalışıyor. Davet senaryosu, düzeltme öncesi kodla çalıştırıldığında #137'yi yakaladı | #146 |
| 2 | SYG-064 metin incelemesi yapıldı (1.3); 6 düzeltme ve 1 kaldırma önerisi, 3 istisna kararda | Düzeltmeler kabul öncesinde uygulanır | [metin incelemesi](2026-10-04-syg-064-metin-incelemesi.md) |
| 3 | ~~Kod gözden geçirme onayı GitHub'da kayıtlı değil~~ | **Kapandı (1.2):** geriye dönük kayıt ve CI denetimi | #128 |
| 4 | CI kayıtları 90 gün saklanıyor | Ham test kanıtı kaybolur; bu rapor kalıcı özettir | #136 |
| 5 | Gösterim yöntemli 3 SYG (028, 068, 073) | Testle doğrulandı; gösterim İK'ya kabulde | #124 |

---

## 7. Gereksinim bazında doğrulama

| SYG | Gereksinim (özet) | Yöntem | Kanıt | Sonuç |
|---|---|---|---|---|
| SYG-KMLK-001 | Sistem kişi ve istihdam kayıtlarını ayrı tutar. Kişi TCKN ile, istihdam sicil… | Test | `PersonnelSynchronizerTests` | ✅ Geçti |
| SYG-KMLK-002 | LOGO'ya erişim yalnızca ILogoPersonnelSource arayüzü üzerinden yapılır. Bu erişimde… | Test | `LogoReadOnlyAccessTests` | ✅ Geçti |
| SYG-KMLK-003 | LOGO'ya yazma denemesinin veritabanı tarafından reddedildiği, CI'da çalışan otomatik… | Test | `LogoReadOnlyAccessTests` | ✅ Geçti |
| SYG-KMLK-004 | Senkronizasyon periyodik olarak ve elle tetiklenerek çalışır. Her çalışmada kaynaktaki… | Test | `LogoFieldScopeTests`, `PersonnelSyncServiceTests`, `PersonnelSyncStoreTests`, `PersonnelSyncWorkerTests` | ✅ Geçti |
| SYG-KMLK-005 | Her senkronizasyon çalışması kalıcı bir kayıt üretir: başlangıç ve bitiş zamanı, sonuç… | Test | `PersonnelSyncServiceTests` | ✅ Geçti |
| SYG-KMLK-006 | T3 kapsamında senkronize edilen alanlar yalnızca şunlardır — kişi: TCKN, ad, soyad,… | İnceleme | `LogoFieldScopeTests` | ✅ Geçti — inceleme sonucu testle sabitlendi (#122) |
| SYG-KMLK-007 | TCKN'si boş veya geçersiz olan LOGO kartı için kişi kaydı oluşturulmaz; kart,… | Test | `PersonnelSynchronizerTests` | ✅ Geçti |
| SYG-KMLK-008 | Aynı kurumsal e-posta adresi, aktif istihdamı olan birden fazla kişiye (farklı TCKN)… | Test | `InvitationApiTests`, `PersonnelSynchronizerTests`, `RegistrationApiTests` | ✅ Geçti |
| SYG-KMLK-009 | E-posta ve telefon senkronizasyonda normalleştirilir. E-posta: baştaki ve sondaki… | Test | `ContactNormalizerTests` | ✅ Geçti |
| SYG-KMLK-010 | Senkronizasyon hatası sessiz kalmaz: hata kaydedilir, çalışma kaydı "başarısız" olarak… | Test | `HealthCheckTests`, `PersonnelSyncHealthCheckTests`, `PersonnelSyncServiceTests`, `PersonnelSyncStoreTests` | ✅ Geçti |
| SYG-KMLK-011 | LOGO erişilemez olduğunda sistem çalışmaya devam eder; üyelik eşleştirmesi ve hesap… | Test | `HealthCheckTests`, `PersonnelSyncServiceTests` | ✅ Geçti |
| SYG-KMLK-012 | Beklenen LOGO tablo, kolon ve veri tiplerinin varlığı (şema sapması) CI'da entegrasyon… | Test | `LogoReadOnlyAccessTests` | ✅ Geçti |
| SYG-KMLK-013 | Üyelik; TCKN, doğum tarihi ve kurumsal e-posta ile başlatılır. Üç bilgi de, son anlık… | Test | `MaskRulesTests`, `RegistrationApiTests`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-014 | TCKN, 11 hane ve resmî sağlama algoritmasıyla hem istemcide hem sunucuda doğrulanır.… | Test | `RegistrationApiTests`, `RegistrationPage.test.tsx`, `nationalId.test.ts` | ✅ Geçti |
| SYG-KMLK-015 | Eşleşme olsa da olmasa da sunucu aynı durum kodunu ve aynı yanıt gövdesini döndürür.… | Test | `RegistrationApiTests`, `RegistrationPage.test.tsx`, `RegistrationTimingTests`, `SignInTimingTests` | ✅ Geçti |
| SYG-KMLK-016 | Eşleşme yoksa hiçbir kod gönderilmez ve kanal seçimi ekranında iki kanal da gösterilir.… | Test | `RegistrationApiTests`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-017 | Eşleşme varsa yalnızca kullanılabilir kanallar sunulur: e-posta kanalı için adresin… | Test | `PersonnelSynchronizerTests`, `RegistrationApiTests`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-018 | Kodun gönderileceği hedef (e-posta adresi veya telefon numarası), maskeli biçimde dahi… | Test | `RegistrationApiTests`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-019 | Kullanıcı kanal değiştirdiğinde önceki kod geçersiz kılınır ve seçilen kanala yeni kod… | Test | `RegistrationApiTests`, `RegistrationPage.test.tsx`, `VerificationCodeServiceTests`, `VerificationCodeTests` | ✅ Geçti |
| SYG-KMLK-020 | Kişinin zaten aktif bir hesabı varsa akış aynı biçimde ilerler; doğrulama başarıyla… | Test | `RegistrationApiTests`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-021 | Bir kişiye en fazla bir hesap bağlanabilir; bu, veritabanında tekillik kısıtıyla zorlanır. | Test | `PersonnelSyncStoreTests`, `RegistrationApiTests`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-022 | Doğrulama kodu kriptografik olarak güvenli rastgele sayı üreteciyle üretilen,… | Test | `RegistrationPage.test.tsx`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests` | ✅ Geçti |
| SYG-KMLK-023 | Kod, üretildikten sonra parametredeki süre boyunca geçerlidir (varsayılan 5 dakika);… | Test | `RegistrationPage.test.tsx`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests`, `VerificationCodeTests` | ✅ Geçti |
| SYG-KMLK-024 | Yanlış deneme sayısı parametredeki sınırı aştığında (varsayılan 3) kod iptal edilir;… | Test | `RegistrationPage.test.tsx`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests` | ✅ Geçti |
| SYG-KMLK-025 | Kod veritabanında anahtarlı özet (HMAC-SHA256) olarak saklanır; anahtar sır… | Test | `DeliveryPolicyAndHashTests`, `RegistrationPage.test.tsx`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests` | ✅ Geçti |
| SYG-KMLK-026 | Kod ve kodu içeren ileti gövdesi hiçbir günlük, denetim kaydı veya hata iletisine… | Test | `IdentityLogMaskingTests`, `NotificationDispatcherTests`, `RegistrationApiTests`, `RegistrationPage.test.tsx` +2 | ✅ Geçti |
| SYG-KMLK-027 | Doğrulanan kod aynı işlemde geçersiz kılınır; eş zamanlı iki doğrulama isteğinden… | Test | `RegistrationPage.test.tsx`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests`, `VerificationCodeTests` | ✅ Geçti |
| SYG-KMLK-028 | Kod ekranı, kalan geçerlilik süresini geri sayım olarak gösterir ve "kodu tekrar… | Gösterim | `RegistrationPage.test.tsx` | ✅ Geçti (test) — gösterim kabulde (TEC.11) |
| SYG-KMLK-029 | SMS, NetGSM standart servisiyle ve parametredeki gönderici başlığıyla gönderilir.… | Test | `NetGsmSmsSenderTests` | ✅ Geçti |
| SYG-KMLK-030 | E-posta, kurum SMTP sunucusu üzerinden, Türkçe ve kurum adını taşıyan bir şablonla… | Test | `InvitationApiTests`, `SmtpEmailSenderTests`, `VerificationMessagesTests` | ✅ Geçti |
| SYG-KMLK-031 | Giriş kimliği kurumsal e-posta adresidir; karşılaştırma büyük/küçük harf duyarsızdır.… | Test | `LoginPage.test.tsx`, `SessionApiTests` | ✅ Geçti |
| SYG-KMLK-032 | Hatalı girişte, kullanıcının var olup olmadığından bağımsız olarak aynı ileti ve aynı… | Test | `LoginPage.test.tsx`, `SessionApiTests`, `SignInTimingTests` | ✅ Geçti |
| SYG-KMLK-033 | Başarısız giriş sayısı sınırı aştığında (varsayılan 5) giriş, parametredeki süre… | Test | `LoginPage.test.tsx`, `PasswordApiTests`, `SessionApiTests`, `SessionDomainTests` | ✅ Geçti |
| SYG-KMLK-034 | İki adımlı doğrulama parametreyle açılıp kapatılır (varsayılan kapalı). Açıkken, parola… | Test | `LoginPage.test.tsx`, `SessionApiTests` | ✅ Geçti |
| SYG-KMLK-035 | İki adımlı doğrulama açılmadan önce, hiçbir doğrulama kanalı bulunmayan aktif hesap… | Test | `ParametersPage.test.tsx`, `SessionApiTests`, `SystemParametersApiTests`, `TwoFactorImpactServiceTests` | ✅ Geçti |
| SYG-KMLK-036 | İki adımlı doğrulamanın açık ve kapalı hâlleri ayrı otomatik testlerle doğrulanır. | Test | `SessionApiTests` | ✅ Geçti |
| SYG-KMLK-037 | Oturum, kısa ömürlü bir erişim jetonu (varsayılan 15 dakika, tarayıcı belleğinde) ve… | Test | `SessionApiTests`, `SessionDomainTests`, `sessionManager.test.ts` | ✅ Geçti |
| SYG-KMLK-038 | Hareketsizlik süresi (varsayılan 30 dakika) kullanıcı etkileşimine göre ölçülür. Arka… | Test | `SessionActivity.test.tsx`, `SessionApiTests`, `SessionDomainTests`, `sessionManager.test.ts` | ✅ Geçti |
| SYG-KMLK-039 | Sistem, meşru uzun süreli etkinlik için bir etkinlik sinyali uç noktası sunar. Sinyal… | Test | `SessionActivity.test.tsx`, `SessionApiTests`, `SessionDomainTests`, `sessionManager.test.ts` | ✅ Geçti |
| SYG-KMLK-040 | Yenileme jetonu her kullanımda yenilenir ve eskisi geçersizleşir. Kullanılmış bir jeton… | Test | `RegistrationApiFixture`, `SessionApiTests`, `SessionDomainTests`, `sessionManager.test.ts` | ✅ Geçti |
| SYG-KMLK-041 | Tek aktif oturum kuralı açıkken (varsayılan açık), başarılı bir giriş kullanıcının… | Test | `SessionApiTests`, `SessionDomainTests`, `sessionManager.test.ts` | ✅ Geçti |
| SYG-KMLK-042 | Oturumu başka bir girişle sonlandırılan istemci, bir sonraki yenilemede makine… | Test | `LoginPage.test.tsx`, `SessionApiTests`, `sessionManager.test.ts` | ✅ Geçti |
| SYG-KMLK-043 | Çıkışta yenileme jetonu sunucu tarafında iptal edilir; iptal edilmiş jetonla yenileme… | Test | `RequireSession.test.tsx`, `SessionApiTests`, `sessionManager.test.ts` | ✅ Geçti |
| SYG-KMLK-044 | Parola en az parametredeki uzunlukta (varsayılan 6), en fazla 128 karakter olabilir.… | Test | `PasswordPolicyTests`, `RegistrationApiTests` | ✅ Geçti |
| SYG-KMLK-045 | Parola, uygulamaya gömülü yaygın parola listesinde bulunursa reddedilir. Liste en az… | Test | `PasswordInfrastructureTests`, `PasswordPolicyTests`, `RegistrationApiTests`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-046 | Zorunlu periyodik parola değişimi parametreyle açılır (varsayılan kapalı). Açıkken,… | Test | `ChangePasswordPage.test.tsx`, `PasswordChangeRequirementApiTests`, `RequireSession.test.tsx`, `SessionDomainTests` +2 | ✅ Geçti |
| SYG-KMLK-047 | Parola sıfırlama, üyelikle aynı eşleştirme, kanal ve kod akışını kullanır; ayrı bir… | Test | `ForgotPasswordPage.test.tsx`, `PasswordApiTests`, `RegistrationAttemptTests`, `RegistrationPage.test.tsx` +1 | ✅ Geçti |
| SYG-KMLK-048 | Oturum içinde parola değişikliği mevcut parolanın girilmesini gerektirir. Değişiklikten… | Test | `ChangePasswordPage.test.tsx`, `PasswordApiTests`, `RegistrationAttemptTests`, `RequireSession.test.tsx` +2 | ✅ Geçti |
| SYG-KMLK-049 | Parola, uyarlanabilir maliyetli bir özet işleviyle (PBKDF2-HMAC-SHA512, en az 100.000… | Test | `PasswordInfrastructureTests` | ✅ Geçti |
| SYG-KMLK-050 | İlk girişte parola değiştirme zorunluluğu parametreyle açılır (varsayılan kapalı).… | Test | `ChangePasswordPage.test.tsx`, `PasswordChangeRequirementApiTests`, `RequireSession.test.tsx`, `SessionDomainTests` +2 | ✅ Geçti |
| SYG-KMLK-051 | İK, bir personele tek kullanımlık parola oluşturma bağlantısı gönderebilir. Bağlantı… | Test | `AccountsPage.test.tsx`, `InvitationApiTests`, `InvitePage.test.tsx` | ✅ Geçti |
| SYG-KMLK-052 | Bağlantı, en az 256 bit rastgelelikte bir jeton taşır; jeton sunucuda özet olarak… | Test | `AccountInvitationTests`, `InvitationApiTests`, `InvitePage.test.tsx` | ✅ Geçti |
| SYG-KMLK-053 | Bağlantı gönderimi gerekçe olmadan yapılamaz; gönderen, alıcı kişi, gerekçe ve zaman… | Test | `AccountInvitationTests`, `AccountsPage.test.tsx`, `InvitationApiTests` | ✅ Geçti |
| SYG-KMLK-054 | Senkronizasyonda kişinin tüm istihdamları sona ermişse hesabı pasife alınır ve açık… | Test | `AccountLifecycleTests`, `AccountsApiTests`, `PersonnelSyncStoreTests`, `SessionApiTests` +1 | ✅ Geçti |
| SYG-KMLK-055 | Pasif hesapla giriş yapılamaz ve pasif hesabın sahibi hiçbir kaydı görüntüleyemez. | Test | `LoginPage.test.tsx`, `RequireSession.test.tsx`, `SessionApiTests` | ✅ Geçti |
| SYG-KMLK-056 | Pasif hesabı olan kişi için senkronizasyonda yeni bir aktif istihdam görülürse mevcut… | Test | `AccountLifecycleTests`, `PersonnelSyncStoreTests`, `UserAccountTests` | ✅ Geçti |
| SYG-KMLK-057 | Yetkili kullanıcı bir hesabı gerekçe girerek elle pasife alabilir ve yeniden… | Test | `AccountsApiTests`, `AccountsPage.test.tsx`, `UserAccountTests` | ✅ Geçti |
| SYG-KMLK-058 | Hesap durumundaki her değişiklik (oluşma, pasifleşme, yeniden aktifleşme, kilitlenme,… | Test | `AccountsApiTests`, `PersonnelSyncStoreTests`, `SessionApiTests` | ✅ Geçti |
| SYG-KMLK-059 | Hız sınırları: üyelik denemesi TCKN başına saatte ve IP başına saatte, kod gönderimi… | Test | `RegistrationApiTests`, `RegistrationPage.test.tsx`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests` | ✅ Geçti |
| SYG-KMLK-060 | Şu kimlik olayları denetim izine yazılır: üyelik başlatma, kod gönderimi, kod doğrulama… | Test | `InvitationApiTests`, `SecurityEventApiTests`, `VerificationCodeServiceTests` | ✅ Geçti |
| SYG-KMLK-061 | TCKN, telefon ve e-posta günlük kayıtlarında mevcut maskeleme altyapısıyla maskelenir… | Test | `IdentityLogMaskingTests` | ✅ Geçti |
| SYG-KMLK-062 | Doğrulama kodu, parola sıfırlama ve davet iletileri bildirim istisnasından muaftır. | Test | `NotificationDispatcherTests`, `NotificationPurposeRulesTests` | ✅ Geçti |
| SYG-KMLK-063 | Kimlik uç noktaları yalnızca HTTPS üzerinden hizmet verir; yenileme jetonu çerezi… | Test | `HttpsRequirementApiTests` | ✅ Geçti |
| SYG-KMLK-064 | Tüm ekran metinleri ve hata iletileri Türkçedir, teknik terim içermez ve kullanıcıya ne… | İnceleme | `LoginPage.test.tsx`, `RegistrationPage.test.tsx` | ⚠️ İnceleme yapıldı (1.3): 10 bulgu, kararda — [metin incelemesi](2026-10-04-syg-064-metin-incelemesi.md) |
| SYG-KMLK-065 | Giriş, üyelik ve parola ekranları 360 piksel genişlikten itibaren yatay kaydırma… | Test | `LoginPage.test.tsx`, `RegistrationPage.test.tsx`; gerçek tarayıcı ölçümü (§4.3) | ✅ Geçti — 360/390/768 px yatay kaydırma yok (her akışın ilk adımı) |
| SYG-KMLK-066 | Ekranlar yalnızca klavyeyle eksiksiz kullanılabilir; her form alanının erişilebilir bir… | Test | `LoginPage.test.tsx`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-067 | Beklenmeyen hata ekranında kullanıcıya izleme kimliği gösterilir. | Test | `LoginPage.test.tsx`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-068 | Giriş, üyelik, doğrulama ve parola ekranları iki bölümlü düzendedir: bir bölümde form,… | Gösterim | `LoginPage.test.tsx`, `RegistrationPage.test.tsx` | ✅ Geçti (test) — gösterim kabulde (TEC.11) |
| SYG-KMLK-069 | Kurumsal logo parametreden yüklenir; yüklenmemişse assets/duzen_logo.png kullanılır.… | Test | `AuthLayout.test.tsx`, `BrandingServiceTests`, `LoginPage.test.tsx`, `RegistrationPage.test.tsx` +1 | ✅ Geçti |
| SYG-KMLK-070 | Üyelik ve giriş ekranlarında destek birimi olarak Bilgi İşlem ve parametredeki iletişim… | Test | `LoginPage.test.tsx`, `RegistrationApiTests`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-071 | Kimlik işlemleri /api/v1/identity/ altında sunulur, OpenAPI sözleşmesinde tanımlıdır ve… | Test | `IdentityRouteTests` | ✅ Geçti |
| SYG-KMLK-072 | Senkronizasyonun elle tetiklenmesi ve son çalışma bilgisinin okunması için yetki… | Test | `AccessControlApiTests`, `PersonnelSyncWorkerTests` | ✅ Geçti |
| SYG-KMLK-073 | T3, İK için en küçük bir hesap işlemleri ekranı sunar: kişiyi sicil numarası, ad veya… | Gösterim | `AccountsApiTests`, `AccountsPage.test.tsx` | ✅ Geçti (test) — gösterim kabulde (TEC.11) |
| SYG-KMLK-074 | T3, ADR-0007'deki eylem yetkisi altyapısını (kullanıcı–rol–izin, sabit izinler,… | Test | `AccessControlApiTests`, `AccessControlServiceTests`, `AccountsApiTests` | ✅ Geçti |
| SYG-KMLK-075 | T3'ün kullandığı parametreler koda gömülmez; veritabanındaki parametre deposunda, Y4… | Test | `AesGcmSecretProtectorTests`, `ParameterCatalogTests`, `SystemParameterStoreTests`, `SystemParameterTests` +1 | ✅ Geçti |
| SYG-KMLK-076 | T3, sistem yöneticisi için yalnızca T3 parametrelerini listeleyen ve düzenleyen en… | Test | `ParameterCatalogTests`, `ParametersPage.test.tsx`, `SystemParameterStoreTests`, `SystemParametersApiTests` | ✅ Geçti |
| SYG-KMLK-077 | Giriş isteğinin yanıt süresi, girişleri mesai başına yayılan 50 kullanıcıda (60… | Analiz | `SignInTimingTests`; Performans ölçüm raporu (UAT) | ✅ Karşılandı (ölçüm) |
| SYG-KMLK-078 | Tam bir LOGO senkronizasyonu (bugünkü hacim ≈1.540 kart) 60 saniyenin altında… | Analiz | Performans ölçüm raporu (UAT) | ✅ Karşılandı (ölçüm) |
| SYG-KMLK-079 | Doğrulama kodu, e-posta ve SMS için istekten itibaren 95. yüzdelikte 60 saniye içinde… | Analiz | `NotificationDispatcherTests`; Performans ölçüm raporu (UAT) | ✅ Karşılandı (ölçüm) |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-04 | 1.0 | İlk sürüm: T3 doğrulama raporu ve G2 kapı kaydı (#123) | Bilgi İşlem |
| 2026-10-04 | 1.1 | Uçtan uca testler eklendi (#146): §3, §5 koşul 2, §6 madde 1 | Bilgi İşlem |
| 2026-10-04 | 1.2 | Kod gözden geçirme kanıtı (#128): §5 koşul 1, §6 madde 3 | Bilgi İşlem |
| 2026-10-04 | 1.3 | SYG-064 metin incelemesi yapıldı (#124) | Bilgi İşlem |
