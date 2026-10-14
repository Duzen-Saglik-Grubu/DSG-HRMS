# T3 Kimlik Yönetimi — Doğrulama Raporu

**Belge kimliği:** TEC.9-RPR-2026-10-04-T3
**Süreç:** TEC.9 — Doğrulama (`TEC.9.BP3`, `TEC.9.BP4`)
**Doğrulanan:** T3 Kimlik Yönetimi, `v0.2.0` = `149f6ee` (`v0.2.0-rc.5` ile aynı commit; İK 13.10.2026'da kabul etti, `KR-105`). Sürüm 1.x `main` @ `fb58fbe`'yi kapsıyordu (§1.1)
**Ölçüt:** `SYG-KMLK` 1.6, 81 sistem gereksinimi (`TEC.3`). `149f6ee`'deki 1.5 ile aynı 81 madde; 1.6 yalnızca #138 kuralını SYG-KMLK-057'ye yazdı (AN-26). Sürüm 1.x: `SYG-KMLK` 1.1, 79 madde
**CI çalışması:** [38299593371](https://github.com/Duzen-Saglik-Grubu/DSG-HRMS/actions/runs/38299593371) — başarılı (12.10.2026); kalıcı özet: [`2026-10-12-v0.2.0-rc.5-ci-kanit-ozeti.md`](../kayitlar/2026-10-12-v0.2.0-rc.5-ci-kanit-ozeti.md). Sürüm 1.x: [37197387185](https://github.com/Duzen-Saglik-Grubu/DSG-HRMS/actions/runs/37197387185) (04.10.2026)
**Tarih:** 2026-10-04
**Son güncelleme:** 2026-10-14 (2.0)
**Hazırlayan:** Bilgi İşlem

> Doğrulama, ürünün **doğru yapılıp yapılmadığını** gösterir (ADR-0011 §8). Doğru ürünün
> yapılıp yapılmadığını, yani İK kabulünü, TEC.11 geçerlemesi gösterir; bu rapor onun
> yerine geçmez.

---

## 1. Özet

| Ölçü | Değer |
|---|---|
| Sistem gereksinimi | 81 |
| Kanıtı olan | **81** (CI'da her PR'da denetleniyor, #122) |
| Testle doğrulanan | 73 (yöntem: Test) |
| Ölçümle doğrulanan | 3 (yöntem: Analiz; SYG-077 `v0.2.0`'da yeniden ölçüldü; 078 ve 079 önceki ölçüm, §4.1) |
| Testle desteklenen inceleme / gösterim | 5 (006, 064 İnceleme; 028, 068, 073 Gösterim) |
| Açık nokta | Doğrulamayı engelleyen yok; gösterimler 13.10.2026 kabulünde yapıldı (§4.4). İzlenenler §6'da: CI kayıt süresi (#136), kabul sonrası bağımlılık açığı (#223), #198'in sonradan gelen testi (#211) |
| Backend testi | 796 geçti (Domain 142, Application 282, Infrastructure 75, Mimari 11, Entegrasyon 286) |
| Ön yüz testi | 247 geçti |
| Uçtan uca test | 7 kimlik senaryosu geçti (CI 38299593371) |
| Kod kapsamı | Backend genel **%92,9** (eşik %75), Domain **%98,9** (eşik %90), ön yüz satır **%87,6** |

**Sonuç:** T3'ün 81 sistem gereksiniminin **81'i** kabul edilen sürümde (`v0.2.0`) doğrulandı:
73'ü testle, 3'ü ölçümle, 2'si inceleme ve testle, 3'ü testle ve kabulde gösterimle.
G2 kapısı 04.10.2026'da `fb58fbe` için GEÇTİ olarak kayda geçti; `v0.2.0` için
14.10.2026'da yeniden kaydedildi (§5.2).

### 1.1 Sürüm 1.x'ten bu yana değişenler

Sürüm 1.x, `fb58fbe`'yi ve `SYG-KMLK` 1.1'in 79 maddesini kapsıyordu. G2'den (04.10) sonra
iki değişiklik talebi ve iki davranış değiştiren düzeltme geldi. Kabul edilen `v0.2.0`
(`149f6ee`) bunların tamamını içerir (`git log fb58fbe..149f6ee`).

| Değişiklik | Issue / PR | Etkilenen SYG | Doğrulama kanıtı (`v0.2.0`) |
|---|---|---|---|
| Kullanıcı kendi hesabını ve aktif kalan son Sistem Yöneticisi hesabını pasife alamaz (düzeltme) | #138 / PR #159 (`v0.2.0-rc.2`) | 057 (kural 14.10.2026'da SYG-KMLK 1.6 ve AN-26 ile yazıya geçti, #211) | `AccountsApiTests` (`Own_account_cannot_be_deactivated_and_is_marked_in_the_list`, `Last_active_system_administrator_cannot_be_deactivated`), `AccountsPage.test.tsx`; KS-09 geçti |
| Kod gönderim (PRM-KML-17) ve üyelik deneme (PRM-KML-18) sınırlarının varsayılanı 10 (değişiklik talebi; İK onayı 10.10.2026) | #185 / PR #186 (`v0.2.0-rc.3`) | 059; parametreyi kullanan 019, 028 | `RegistrationApiTests`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests` yeni varsayılanla; Kabul Planı T-03 uygun |
| İki adımlı doğrulama kullanıcı tercihine bağlı; İK'nın kurtarma için kapatması (değişiklik talebi; İK onayı 10.10.2026, `KR-104`) | #190 / PR #191 (`v0.2.0-rc.4`) | 034, 035, 036 değişti; **080, 081 yeni**; 033, 058, 060, 062 yeni testlerle genişledi | `TwoFactorPreferenceApiTests` (yeni, 13 test), `SessionApiTests` (sistem/kullanıcı dört birleşim), `AccountsApiTests`, `UserAccountTests`, `TwoFactorImpactServiceTests`, `SystemParametersApiTests`, `AccountSecurityPage.test.tsx` (yeni, 10 test), `AccountsPage.test.tsx`, `AppLayout.test.tsx`; KS-09, KS-10 ve T-04 uygun |
| T5'e ait PRM-HSP-03 T3 parametre ekranından çıkarıldı (düzeltme) | #198 / PR #199 (`v0.2.0-rc.5`) | 076 | `v0.2.0`'da testsiz geldi; ekran KS-10'da İK'ya gösterildi. Geri yön kapsam testi `ParameterCatalogScopeTests` kabulden sonra `main`'e eklendi (#211, PR #226) |
| SYG-KMLK-077 kabul edilen sürümde yeniden ölçüldü | #209 | 077 | [Performans ölçüm raporu](2026-10-02-t3-performans-olcumu.md) 0.3 §2.6: p95 0,261 sn; aynı anda 50 girişte 4,38 sn |

Davranışı değiştirmeyen diğer değişiklikler (aynı aralıkta): mimari testinin modül sınırı
kuralı gerçek düzene uyarlandı (#171, PR #173; §5.1 notu), ön yüz testlerine yavaş CI payı
(#182), bağımlılık açıkları (#149, #175), belge ve CI değişiklikleri. Sürüm 1.1–1.4'te
rapora işlenen uçtan uca testler (#146), onay denetimi (#128) ve SYG-064 düzeltmeleri (#152)
de bu aralıktadır: 1.x bunları rapora işlemiş, ancak başlıktaki commit'i `fb58fbe` olarak
bırakmıştı. 2.0 ile başlık, tablo ve CI kanıtı aynı commit'i gösterir.

**Kabulden sonra (`v0.2.0`'da yok):** `System.Security.Cryptography.Pkcs` 10.0.13'e
sabitlendi (#223, PR #225). Bu rapor o commit'i kapsamaz; bkz. §6 madde 6.

---

## 2. Kapsam ve yöntem

### 2.1 Doğrulama yöntemleri

`TEC.9-dogrulama/YAKLASIM.md` §3'te tanımlıdır.

| Yöntem | Kanıt | Bu çevrimde |
|---|---|---|
| **Test** | Otomatik test (birim, entegrasyon, bileşen); CI'da her PR'da çalışır | 73 SYG |
| **Analiz** | Ölçüm veya hesap; yazılı rapor | 3 SYG ([performans ölçüm raporu](2026-10-02-t3-performans-olcumu.md)) |
| **İnceleme** | Belge veya kodun bir ölçüte karşı gözden geçirilmesi; bulgular kayıtlı | 2 SYG |
| **Gösterim** | Davranışın çalışan sistemde gösterilmesi | 3 SYG |

### 2.2 Kanıtın nasıl bulunduğu

Tablodaki kanıt sütunu, gereksinim kimliğini anan test dosyalarından üretildi. Aynı
eşleme CI'da her PR'da denetleniyor: kanıtı olmayan bir SYG kapıdan geçemiyor
(`requirement-traceability-check.mjs`, 5. denetim). Bir test dosyası birden fazla
gereksinimi doğrulayabilir; dörtten fazla dosya varsa ilk dördü gösterilir.

**2.0'da:** Eşleme `149f6ee`'deki test dosyalarından yeniden üretildi. Mimari testi
`LayerDependencyTests` modül bağımlılıklarının gerekçesinde bazı SYG'leri anar (013, 026–031,
054, 073, 075) ama o davranışı doğrulamaz; bu yüzden tabloda gösterilmez. Kabulden sonra
eklenen testler (`ParameterCatalogScopeTests`) yalnızca not olarak yazılır.

### 2.3 Sonucun anlamı

"✅ Geçti": gereksinimi anan testlerin tamamı yukarıdaki CI çalışmasında (`149f6ee`) geçti.
CI çalışmasının kayıtları GitHub'da 90 gün saklanır; bu rapor o kaydın kalıcı özetidir
(#136).

---

## 3. Zorunlu test türleri (ADR-0011 §2)

| Tür | Durum | Kanıt |
|---|---|---|
| İş kuralı birim testleri | ✅ | Domain 142, Application 282 (1.x: 136, 281) |
| API entegrasyon testleri (ana akış ve doğrulama hataları) | ✅ | 286 entegrasyon testi; gerçek PostgreSQL (Testcontainers) |
| Yetki sızıntısı testi | ✅ | `AccessControlApiTests`, `AccountsApiTests` (SYG-081 kurtarma işlemi dahil), `SystemParametersApiTests` (izinsiz istek `403`); `PasswordChangeRequirementApiTests`; `TwoFactorPreferenceApiTests` (oturumsuz istek `401`; başka kişinin kodu 2FA açamaz) |
| Maskeleme testi | ✅ | `IdentityLogMaskingTests` (günlük dosyası okunur), `SecurityEventApiTests`, `NotificationDispatcherTests` |
| Ön yüz bileşen testleri | ✅ | 247 test (1.x: 232) |
| Uçtan uca test (5–8 senaryo, Playwright) | ✅ (1.1) | 7 kimlik senaryosu: üyelik, giriş/çıkış, hatalı giriş, parola değiştirme, sıfırlama, İK daveti, yetki (#146). İlk sürümde yoktu. `149f6ee`'de CI 38299593371 "Uçtan uca testler" işi geçti |

---

## 4. Ek doğrulamalar

### 4.1 Performans (Analiz)

[Performans ölçüm raporu](2026-10-02-t3-performans-olcumu.md) 0.3:
- **SYG-077 (giriş):** `v0.2.0`'da 14.10.2026'da yeniden ölçüldü (§2.6): 60 saniyeye yayılan 50 girişte p95 **0,261 sn**; aynı anda 50 girişte p95 **4,38 sn**. G2'den sonra giriş akışı değiştiği için (#190, #138) yinelendi. Önceki ölçüm (04.10, `2d79f01`): 0,279 sn; 4,42 sn.
- **SYG-078 (senkronizasyon):** p95 0,43 sn (26.09–02.10, 573 çalışma).
- **SYG-079 (kod teslimi):** SMS p95 0,50 sn, e-posta p95 0,43 sn (02–04.10).

**078 ve 079 neden yinelenmedi?** `fb58fbe..149f6ee` aralığında senkronizasyon kodu
(`Personnel`) hiç değişmedi. İleti gönderiminde (SMTP, NetGSM, dağıtıcı) yalnızca yeni bir
ileti amacı eklendi (2FA açma kodu, #190); gönderim yolu aynıdır. Önceki ölçümler bu
gerekçeyle `v0.2.0` için geçerli sayılır. Gerekçe bir etki analizidir, yeni ölçüm değildir.

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

**2.0:** Kod adımı 13.10.2026 kabulünde telefonla denendi (KS-02, `v0.2.0-rc.5`): yatay
kaydırma olmadan kullanıldı, senaryo geçti.

### 4.4 Kabulde gösterim (TEC.11)

Gösterim yöntemli üç SYG, 13.10.2026 kabul oturumunda kabul edilen commit'le (`149f6ee`)
gösterildi ([kabul raporu](../../TEC.11-gecerleme/raporlar/2026-10-13-t3-kabul-raporu.md), KS-01…KS-11 11/11 geçti):

| SYG | Senaryo | Sonuç |
|---|---|---|
| SYG-KMLK-028 (geri sayım, kodu tekrar gönder) | KS-02 | Geçti |
| SYG-KMLK-068 (iki bölümlü düzen) | KS-01, KS-10 | Geçti |
| SYG-KMLK-073 (İK hesap işlemleri ekranı) | KS-09 | Geçti |

---

## 5. G2 kapı kaydı — "Geliştirme tamam"

**Kapı:** G2 (proje planı §3.1). **Karar veren:** Bilgi İşlem. **Ölçüt:** ADR-0011 §6'daki CI kalite kapılarının tamamı.

### 5.1 İlk kayıt — `fb58fbe` (04.10.2026)

| Kapı (ADR-0011 §6) | Durum | Kanıt |
|---|---|---|
| Derleme (uyarı yok) | ✅ | CI 37197387185 |
| Birim ve entegrasyon testleri | ✅ | 768 + 232 geçti |
| Kapsam eşiği (genel %75, Domain %90) | ✅ | %92,9 / %98,9 |
| Mimari testi | ✅ | 10 geçti. **Not (1.6):** Bu testlerden modül yalıtım kuralı o tarihte hiçbir tipi denetlemiyordu (#171); katman kuralları denetleniyordu. 05.10.2026'dan beri modül sınırı da denetleniyor (ADR-0016, 11 test) |
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
- **Koşul 3:** ~~SYG-064 bulguları~~ — karşılandı (#152, 1.4): 7 düzeltme uygulandı, 3 istisna kayda geçti.

Üç koşul da karşılandı. **Önerilen karar artık: GEÇTİ.** Karar satırı karar verenindir.

| Karar | Tarih | Karar veren |
|---|---|---|
| **GEÇTİ** | 04.10.2026 | Doğuş Uçanok (Bilgi İşlem) |

### 5.2 Yeniden kayıt — `v0.2.0` (14.10.2026)

**Neden?** İlk G2 kaydından (04.10) sonra iki değişiklik talebi (#185, #190) ve iki
davranış değiştiren düzeltme (#138, #198) geldi (§1.1). İlk kayıt, kabul edilen sürümü
(`v0.2.0` = `149f6ee`) ve SYG-KMLK-080, 081'i kapsamıyordu (T3 modül sonu denetimi M-03,
Ek C YB-01). G2 bu yüzden kabul edilen sürüm için yeniden kaydedilir (#209).

| Kapı (ADR-0011 §6) | Durum | Kanıt |
|---|---|---|
| Derleme (uyarı yok) | ✅ | CI 38299593371 (`149f6ee`) |
| Birim ve entegrasyon testleri | ✅ | 796 + 247 geçti, başarısız yok ([CI kanıt özeti](../kayitlar/2026-10-12-v0.2.0-rc.5-ci-kanit-ozeti.md)) |
| Kapsam eşiği (genel %75, Domain %90) | ✅ | %92,9 / %98,9 |
| Mimari testi | ✅ | 11 geçti; modül sınırı kuralı gerçek tipleri denetliyor (#171, PR #173) |
| Statik analiz (.NET, ESLint) | ✅ | CI |
| Sır taraması (gitleaks) | ✅ | CI |
| Bağımlılık güvenliği | ✅ | CI, 12.10.2026 itibarıyla. 13.10'da yayımlanan iki açık için §6 madde 6 |
| Konteyner taraması (trivy) | ✅ | CI (api, web) |
| Migration kontrolü | ✅ | CI "Bekleyen model değişikliği yok" |
| Biçim (`dotnet format`, Prettier) | ✅ | CI |
| Uçtan uca testler | ✅ | CI "Uçtan uca testler" (7 senaryo) |
| Kod gözden geçirme: en az bir onay | ✅ | G2'den sonraki davranış değiştiren PR'ların (#159, #186, #191, #199) ve sürüm PR'ının (#201) her birinde Doğuş Uçanok'un onay yorumu var; CI'da denetleniyor (`KR-096`) |
| Gereksinim izlenebilirliği (proje ek kapısı) | ✅ | REQ → SYG → test, CI'da; 81 SYG'nin 81'inin kanıtı var (§7) |

**Önerilen karar: GEÇTİ.** Otomatik kapıların tamamı kabul edilen commit'te geçti; 81 SYG
doğrulandı (§1). Karar, bu raporu getiren PR'ın Doğuş Uçanok tarafından onaylanmasıyla
kayda geçer (`KR-096`); PR onaylanmadan birleşirse bu satır geçersizdir.

| Karar | Tarih | Karar veren | Kapsam |
|---|---|---|---|
| **GEÇTİ** | 14.10.2026 | Doğuş Uçanok (Bilgi İşlem), bu PR'ın onayıyla (`KR-096`) | `v0.2.0` = `149f6ee`; `SYG-KMLK` 1.6, 81 madde |

---

## 6. Açık noktalar

| # | Nokta | Etki | İzleme |
|---|---|---|---|
| 1 | ~~Uçtan uca test yok~~ | **Kapandı (1.1):** 7 senaryo her PR'da çalışıyor. Davet senaryosu, düzeltme öncesi kodla çalıştırıldığında #137'yi yakaladı | #146 |
| 2 | ~~SYG-064 metin bulguları~~ | **Kapandı (1.4):** düzeltmeler uygulandı ve testlendi; B-07, B-08, B-10 istisna; uygulama sırasında bulunan B-11 de düzeltildi | #152, [metin incelemesi](2026-10-04-syg-064-metin-incelemesi.md) 1.1 |
| 3 | ~~Kod gözden geçirme onayı GitHub'da kayıtlı değil~~ | **Kapandı (1.2):** geriye dönük kayıt ve CI denetimi | #128 |
| 4 | CI kayıtları 90 gün saklanıyor | Ham test kanıtı kaybolur; bu rapor kalıcı özettir | #136 |
| 5 | ~~Gösterim yöntemli 3 SYG (028, 068, 073)~~ | **Kapandı (2.0):** 13.10.2026 kabulünde gösterildi; KS-01, 02, 09, 10 geçti (§4.4) | #124 |
| 6 | `v0.2.0`'daki `System.Security.Cryptography.Pkcs` 10.0.0'da 13.10.2026'da iki DoS açığı yayımlandı | Kabulden sonra; `main`'de 10.0.13'e sabitlendi (PR #225). `v0.2.0` bu düzeltmeyi içermez; bir sonraki sürümle UAT'ye gider. Bu raporun kapsamı değişmez | #223, R-29 |
| 7 | #198 düzeltmesi (SYG-076) `v0.2.0`'da regresyon testi olmadan geldi | Ekran kabulde (KS-10) gösterildi. Geri yön kapsam testi kabulden sonra `main`'e eklendi | #211, PR #226 |

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
| SYG-KMLK-028 | Kod ekranı, kalan geçerlilik süresini geri sayım olarak gösterir ve "kodu tekrar… | Gösterim | `RegistrationPage.test.tsx` | ✅ Geçti (test) ve gösterildi — KS-02, 13.10.2026 (§4.4) |
| SYG-KMLK-029 | SMS, NetGSM standart servisiyle ve parametredeki gönderici başlığıyla gönderilir.… | Test | `NetGsmSmsSenderTests` | ✅ Geçti |
| SYG-KMLK-030 | E-posta, kurum SMTP sunucusu üzerinden, Türkçe ve kurum adını taşıyan bir şablonla… | Test | `InvitationApiTests`, `SmtpEmailSenderTests`, `VerificationMessagesTests` | ✅ Geçti |
| SYG-KMLK-031 | Giriş kimliği kurumsal e-posta adresidir; karşılaştırma büyük/küçük harf duyarsızdır.… | Test | `LoginPage.test.tsx`, `SessionApiTests` | ✅ Geçti |
| SYG-KMLK-032 | Hatalı girişte, kullanıcının var olup olmadığından bağımsız olarak aynı ileti ve aynı… | Test | `LoginPage.test.tsx`, `SessionApiTests`, `SignInTimingTests` | ✅ Geçti |
| SYG-KMLK-033 | Başarısız giriş sayısı sınırı aştığında (varsayılan 5) giriş, parametredeki süre… | Test | `LoginPage.test.tsx`, `PasswordApiTests`, `SessionApiTests`, `SessionDomainTests` +1 | ✅ Geçti — 2FA açma ekranındaki parola denemesi de aynı kilide takılır (#190) |
| SYG-KMLK-034 | İki adımlı doğrulama parametreyle kullanıma açılıp kapatılır (varsayılan kapalı). Parametre açıkken, 2FA'yı kendi… | Test | `LoginPage.test.tsx`, `SessionApiTests`, `TwoFactorPreferenceApiTests` | ✅ Geçti — #190 metniyle (2.0) |
| SYG-KMLK-035 | İki adımlı doğrulama parametresi açılmadan önce, 2FA'yı kendi hesabında açmış aktif hesap… | Test | `ParametersPage.test.tsx`, `SessionApiTests`, `SystemParametersApiTests`, `TwoFactorImpactServiceTests` | ✅ Geçti — #190 metniyle (2.0): yalnızca tercihi açık aktif hesaplar sayılır |
| SYG-KMLK-036 | İki adımlı doğrulamanın sistem ve kullanıcı düzeyindeki açık ve kapalı hâllerinin her… | Test | `SessionApiTests`; ayrıca `TwoFactorPreferenceApiTests` (REQ-KMLK-053) | ✅ Geçti — #190 metniyle (2.0): dört birleşimin her biri ayrı testte (sistem/kullanıcı: kapalı/kapalı, açık/açık, açık/kapalı, kapalı/açık) |
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
| SYG-KMLK-057 | Yetkili kullanıcı bir hesabı gerekçe girerek elle pasife alabilir ve yeniden… | Test | `AccountsApiTests`, `AccountsPage.test.tsx`, `UserAccountTests` | ✅ Geçti — kendi hesabı ve son Sistem Yöneticisi kuralı (#138, AN-26) `AccountsApiTests` ve `AccountsPage.test.tsx`'te (2.0) |
| SYG-KMLK-058 | Hesap durumundaki her değişiklik (oluşma, pasifleşme, yeniden aktifleşme, kilitlenme,… | Test | `AccountsApiTests`, `PersonnelSyncStoreTests`, `SessionApiTests`, `TwoFactorPreferenceApiTests` | ✅ Geçti |
| SYG-KMLK-059 | Hız sınırları: üyelik denemesi TCKN başına saatte ve IP başına saatte, kod gönderimi… | Test | `RegistrationApiTests`, `RegistrationPage.test.tsx`, `VerificationCodeServiceTests`, `VerificationCodeStoreTests` | ✅ Geçti — PRM-KML-17 ve 18 varsayılanı 10 (#185) ile (2.0) |
| SYG-KMLK-060 | Şu kimlik olayları denetim izine yazılır: üyelik başlatma, kod gönderimi, kod doğrulama… | Test | `InvitationApiTests`, `SecurityEventApiTests`, `TwoFactorPreferenceApiTests`, `VerificationCodeServiceTests` | ✅ Geçti |
| SYG-KMLK-061 | TCKN, telefon ve e-posta günlük kayıtlarında mevcut maskeleme altyapısıyla maskelenir… | Test | `IdentityLogMaskingTests` | ✅ Geçti |
| SYG-KMLK-062 | Doğrulama kodu, parola sıfırlama ve davet iletileri bildirim istisnasından muaftır. | Test | `NotificationDispatcherTests`, `NotificationPurposeRulesTests` | ✅ Geçti — 2FA açma kodu da muaf (#190) |
| SYG-KMLK-063 | Kimlik uç noktaları yalnızca HTTPS üzerinden hizmet verir; yenileme jetonu çerezi… | Test | `HttpsRequirementApiTests` | ✅ Geçti |
| SYG-KMLK-064 | Tüm ekran metinleri ve hata iletileri Türkçedir, teknik terim içermez ve kullanıcıya ne… | İnceleme | `HttpsRequirementApiTests`, `InvitationApiTests`, `LoginPage.test.tsx`, `ParameterCatalogTests` +2 | ✅ İnceleme ve düzeltme (1.4): 11 bulgu kapandı — [metin incelemesi](2026-10-04-syg-064-metin-incelemesi.md) 1.1; İK teyidi KS-11 geçti (13.10.2026) |
| SYG-KMLK-065 | Giriş, üyelik ve parola ekranları 360 piksel genişlikten itibaren yatay kaydırma… | Test | `LoginPage.test.tsx`, `RegistrationPage.test.tsx`; gerçek tarayıcı ölçümü (§4.3) | ✅ Geçti — 360/390/768 px yatay kaydırma yok (her akışın ilk adımı); kod adımı telefonda KS-02'de (13.10.2026) |
| SYG-KMLK-066 | Ekranlar yalnızca klavyeyle eksiksiz kullanılabilir; her form alanının erişilebilir bir… | Test | `LoginPage.test.tsx`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-067 | Beklenmeyen hata ekranında kullanıcıya izleme kimliği gösterilir. | Test | `LoginPage.test.tsx`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-068 | Giriş, üyelik, doğrulama ve parola ekranları iki bölümlü düzendedir: bir bölümde form,… | Gösterim | `LoginPage.test.tsx`, `RegistrationPage.test.tsx` | ✅ Geçti (test) ve gösterildi — KS-01, KS-10, 13.10.2026 (§4.4) |
| SYG-KMLK-069 | Kurumsal logo parametreden yüklenir; yüklenmemişse assets/duzen_logo.png kullanılır.… | Test | `AuthLayout.test.tsx`, `BrandingServiceTests`, `LoginPage.test.tsx`, `RegistrationPage.test.tsx` +1 | ✅ Geçti |
| SYG-KMLK-070 | Üyelik ve giriş ekranlarında destek birimi olarak Bilgi İşlem ve parametredeki iletişim… | Test | `LoginPage.test.tsx`, `RegistrationApiTests`, `RegistrationPage.test.tsx` | ✅ Geçti |
| SYG-KMLK-071 | Kimlik işlemleri /api/v1/identity/ altında sunulur, OpenAPI sözleşmesinde tanımlıdır ve… | Test | `IdentityRouteTests` | ✅ Geçti |
| SYG-KMLK-072 | Senkronizasyonun elle tetiklenmesi ve son çalışma bilgisinin okunması için yetki… | Test | `AccessControlApiTests`, `PersonnelSyncWorkerTests` | ✅ Geçti |
| SYG-KMLK-073 | T3, İK için en küçük bir hesap işlemleri ekranı sunar: kişiyi sicil numarası, ad veya… | Gösterim | `AccountsApiTests`, `AccountsPage.test.tsx` | ✅ Geçti (test) ve gösterildi — KS-09, 13.10.2026 (§4.4) |
| SYG-KMLK-074 | T3, ADR-0007'deki eylem yetkisi altyapısını (kullanıcı–rol–izin, sabit izinler,… | Test | `AccessControlApiTests`, `AccessControlServiceTests`, `AccountsApiTests` | ✅ Geçti |
| SYG-KMLK-075 | T3'ün kullandığı parametreler koda gömülmez; veritabanındaki parametre deposunda, Y4… | Test | `AesGcmSecretProtectorTests`, `ParameterCatalogTests`, `SystemParameterStoreTests`, `SystemParameterTests` +1 | ✅ Geçti |
| SYG-KMLK-076 | T3, sistem yöneticisi için yalnızca T3 parametrelerini listeleyen ve düzenleyen en… | Test | `ParameterCatalogTests`, `ParametersPage.test.tsx`, `SystemParameterStoreTests`, `SystemParametersApiTests` | ✅ Geçti — PRM-HSP-03 çıkarıldı (#198); bu düzeltmenin testi `v0.2.0`'da yok, ekran KS-10'da gösterildi. `ParameterCatalogScopeTests` kabulden sonra (#211, PR #226) |
| SYG-KMLK-077 | Giriş isteğinin yanıt süresi, girişleri mesai başına yayılan 50 kullanıcıda (60… | Analiz | `SignInTimingTests`; Performans ölçüm raporu 0.3 §2.6 (UAT, `v0.2.0`) | ✅ Karşılandı (ölçüm, 14.10.2026): p95 0,261 sn; aynı anda 4,38 sn |
| SYG-KMLK-078 | Tam bir LOGO senkronizasyonu (bugünkü hacim ≈1.540 kart) 60 saniyenin altında… | Analiz | Performans ölçüm raporu (UAT) | ✅ Karşılandı (ölçüm, 26.09–02.10) — `v0.2.0`'da yinelenmedi; senkronizasyon kodu değişmedi (§4.1) |
| SYG-KMLK-079 | Doğrulama kodu, e-posta ve SMS için istekten itibaren 95. yüzdelikte 60 saniye içinde… | Analiz | `NotificationDispatcherTests`; Performans ölçüm raporu (UAT) | ✅ Karşılandı (ölçüm, 02–04.10) — `v0.2.0`'da yinelenmedi; gönderim yolu değişmedi (§4.1) |
| SYG-KMLK-080 | Parametre açıkken kullanıcı, "Hesap güvenliği" ekranından kendi 2FA tercihini açar ve… | Test | `AccountSecurityPage.test.tsx`, `AppLayout.test.tsx`, `ParametersPage.test.tsx`, `RegistrationApiTests` +6 | ✅ Geçti (2.0, yeni; #190) — ayrıca `TwoFactorPreferenceApiTests`, `UserAccountTests`, `VerificationCodeServiceTests`; KS-10 |
| SYG-KMLK-081 | Doğrulama kodunu alamayan kişi için (e-posta ve telefon erişimi yok) kurtarma: İK… | Test | `AccountsApiTests`, `AccountsPage.test.tsx`, `UserAccountTests` | ✅ Geçti (2.0, yeni; #190) — gerekçe zorunlu, kendi hesabına uygulanamaz; KS-09 |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-04 | 1.0 | İlk sürüm: T3 doğrulama raporu ve G2 kapı kaydı (#123) | Bilgi İşlem |
| 2026-10-04 | 1.1 | Uçtan uca testler eklendi (#146): §3, §5 koşul 2, §6 madde 1 | Bilgi İşlem |
| 2026-10-04 | 1.2 | Kod gözden geçirme kanıtı (#128): §5 koşul 1, §6 madde 3 | Bilgi İşlem |
| 2026-10-04 | 1.3 | SYG-064 metin incelemesi yapıldı (#124) | Bilgi İşlem |
| 2026-10-04 | 1.4 | SYG-064 düzeltmeleri (#152): §1, §5 koşul 3, §6 madde 2 | Bilgi İşlem |
| 2026-10-04 | 1.5 | §5: G2 kararı GEÇTİ (Doğuş Uçanok) | Bilgi İşlem |
| 2026-10-05 | 1.6 | §5: mimari testi satırına modül yalıtım kuralının boş geçtiği notu (#171) | Bilgi İşlem |
| 2026-10-14 | 2.0 | Kabul edilen sürüm için güncellendi (#209; denetim M-03, Ek C YB-01, YB-13): kapsam `v0.2.0` = `149f6ee` ve `SYG-KMLK` 1.6 (81 madde). SYG-080 ve 081 eklendi; 033–036, 057–060, 062, 064, 065, 076–079 güncellendi; gösterimler kabulde yapıldı (§4.4); §1.1 değişenler; §5.2 G2 yeniden kaydı; §6 madde 5 kapandı, 6 ve 7 eklendi. **1.0–1.6 `fb58fbe`'yi ve `SYG-KMLK` 1.1'in 79 maddesini kapsıyordu** (CI 37197387185); o sürümün sonuçları §5.1'de ve git geçmişinde korunur | Bilgi İşlem |
