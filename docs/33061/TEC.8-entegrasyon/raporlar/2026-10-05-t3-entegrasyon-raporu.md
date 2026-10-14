# T3 Kimlik Yönetimi — Entegrasyon Raporu

**Belge kimliği:** TEC.8-RPR-2026-10-05-T3
**Süreç:** TEC.8 — Entegrasyon (`TEC.8.BP2`, `BP3`)
**Entegre edilen sürüm:** `v0.2.0` = `v0.2.0-rc.5` (commit `149f6ee`), UAT'de 12.10.2026 19:51 UTC'den beri (`TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`); İK kabulü 13.10.2026 (`KR-105`). Sürüm 1.0–1.1 `v0.2.0-rc.2`'yi (`69bafad`) kapsıyordu (§4.1)
**Dayanak:** SYG-KMLK §2.3 dış arayüzler; ADR-0003 (LOGO), ADR-0012 (bildirim), `KR-093` (HTTPS ve ters vekil)
**Tarih:** 2026-10-05 · **Son güncelleme:** 2026-10-14 (1.2) · **Hazırlayan:** Bilgi İşlem · **Kaynak:** T3 süreç denetimi, Ek C §1 (#134); 1.2: T3 modül sonu denetimi Ek C YB-07 (#214)

> Bu rapor, T3'ün dış sistemlerle ve kendi bileşenleri arasındaki arayüzlerinin **nasıl,
> ne zaman ve hangi sonuçla** sınandığını tek yerde toplar. Sonuçlar daha önce PR
> metinlerine, CI kayıtlarına ve runbook'a dağılmıştı. Entegrasyon yaklaşımı:
> `TEC.8-entegrasyon/YAKLASIM.md` (#130, 05.10.2026).

---

## 1. Özet

| Arayüz | Yön | CI'da sınama | UAT'de canlı sınama | Durum (13.10.2026, `v0.2.0`) | Açık iş |
|---|---|---|---|---|---|
| LOGO veritabanı | Okuma | ✅ Yazma reddi | ✅ 26.09'dan beri senkronizasyon | `/health/sync`: **Healthy** | — |
| NetGSM (SMS) | Giden | ✅ 12 sonuç kodu | ✅ Gerçek SMS ulaştı (29.09) | `/health/notifications` sms: **Healthy** | Teslim durumu sorgusu (R-14) |
| Kurum SMTP | Giden | ✅ Gerçek SMTP konuşması | ⚠️ 27.09 Unhealthy → 01.10 düzeldi | email: **Healthy**; sertifika 06.10.2026'da yenilendi (#179), bitiş **27.03.2027** | — (sonraki yenileme R-20'de) |
| Tarayıcı ↔ nginx ↔ API | Gelen | ✅ Vekil ve HTTPS kuralları, uçtan uca testler | ✅ Her dağıtımda öz denetim | `/health/ready`: **Healthy**; UAT TLS sertifikası bitişi 16.12.2026 | — |
| API ↔ PostgreSQL | İç | ✅ Gerçek PostgreSQL, şema betiği | ✅ Her dağıtımda şema betiği | **Healthy**; UAT şeması 15/15 migration depoyla aynı | — |

**Sonuç:** T3'ün beş arayüzü de CI'da ve UAT'de sınandı; kabul edilen `v0.2.0` UAT'de
çalışırken (13.10.2026) beşi de sağlıklıydı. Entegrasyon sırasında **7 anomali** bulundu;
hepsi kapandı (§3). SMTP sertifikası 06.10.2026'da yenilendi (bitiş 27.03.2027, #179);
sonraki yenileme R-20'de izleniyor. Açık kalan tek iş SMS teslim durumu sorgusudur (R-14).
`v0.2.0-rc.3`…`rc.5`'in getirdiği arayüz değişiklikleri §2.6'dadır.

> **Durum sütununun kaynağı:** 13.10.2026 18:10–18:20 UTC'de UAT'de yapılan canlı denetim
> (`MAN.5-konfigurasyon-yonetimi/raporlar/2026-10-13-t3-konfigurasyon-denetimi.md`, D-04,
> D-07, D-10, D-11, D-12). Çalışan API ve web konteynerlerinin commit'i `149f6ee`.

---

## 2. Arayüzler

### 2.1 LOGO veritabanı (salt okuma)

| | |
|---|---|
| **Kısıt** | Yazma, güncelleme ve silme kesinlikle yapılmaz (`KR-003`, `KR-004`, SYG-KMLK-002, 003). LOGO erişimi yalnızca `LogoDbContext` ve `LogoPersonnelSource` üzerinden; mimari testi başka yerden SQL Server bağımlılığını engeller. |
| **CI'da sınama** | `LogoReadOnlyAccessTests` (PR #76, 26.09.2026): Gerçek bir SQL Server Express konteynerinde, salt okuma yetkili hesabın yazma girişimi reddedilir. Okunan alanların kapsamı `LogoFieldScopeTests` ile sınanır. Senkronizasyon motoru `PersonnelSynchronizerTests`. |
| **UAT'de sınama** | 26.09.2026'dan beri her 15 dakikada canlı senkronizasyon (PR #75, #76). Canlı veride bulunan ad ve ortak e-posta çelişkisi (AN-17) kurala bağlandı: yalnızca aktif siciller (`KR-079`, #77, PR #78). |
| **Bugün** | `/health/sync`: `personnel-sync` **Healthy** (13.10.2026). |
| **Anomali** | A-1 (§3) |

### 2.2 NetGSM (SMS)

| | |
|---|---|
| **Kısıt** | Standart SMS servisi (`KR-042`), HTTPS; gönderici başlığı `DUZEN` (PRM-ENT-03). UAT'de izin listesi kipi (`KR-083`). |
| **CI'da sınama** | `NetGsmSmsSenderTests` (PR #86, 27.09.2026): NetGSM'in 12 sonuç kodu sahte yanıtlarla; yeniden deneme yalnızca geçici hatalarda. Dağıtıcı ve gönderim kaydı `NotificationDispatcherTests`. |
| **UAT'de sınama** | Kimlik doğrulama denetimi 27.09.2026'da **Healthy**. Gerçek SMS UAT'de 29.09.2026'da ulaştı (üyelik denemesi; #103 bu denemede bulundu). |
| **Bugün** | `/health/notifications`: sms **Healthy** (13.10.2026). |
| **Açık iş** | Teslim durumu sorgusu ve kara liste raporu yapılmadı (R-14 önlem 2–3). |

### 2.3 Kurum SMTP sunucusu (e-posta)

| | |
|---|---|
| **Kısıt** | `mail.duzen.com.tr:587`, STARTTLS; sunucu sertifikası **doğrulanır**, doğrulama kapatılmaz (runbook §10.5). |
| **CI'da sınama** | `SmtpEmailSenderTests` (PR #86): Mailpit konteyneriyle gerçek SMTP konuşması; Türkçe şablon. Uçtan uca testler (#146) e-postayla gelen kodu ve davet bağlantısını Mailpit'ten okur. |
| **UAT'de sınama** | 27.09.2026: **Unhealthy**. 587 portu süresi 07.03.2026'da dolmuş, kendinden imzalı bir sertifika sunuyordu (A-5). Sertifika 01.10.2026'da düzeltildi; 02.10.2026'da `email: Healthy`. 03.10.2026'da İK davet bağlantıları e-postayla ulaştı. |
| **Bugün** | `email` **Healthy** (13.10.2026). 587 portu `CN=mail.duzen.com.tr` sertifikasını sunuyor (Sectigo); geçerlilik sonu **27.03.2027 23:59:59 GMT** (konfigürasyon denetimi D-11). 05.10.2026'da sunulan önceki sertifikanın bitişi 11.10.2026'ydı; 06.10'da yenilendi (#179). |
| **Açık iş** | ~~Sertifikanın yenilenmesi~~ — **Kapandı (1.1, 06.10.2026):** Sectigo, geçerlilik 04.10.2026–27.03.2027; 587 portunda doğrulama `0 (ok)`; email Healthy. Sonraki yenileme R-20'de izleniyor. |

### 2.4 Tarayıcı ↔ nginx ↔ API

| | |
|---|---|
| **Kısıt** | Yalnızca HTTPS (`KR-093`, SYG-KMLK-063). API, istemci IP'sini ve şemayı yalnızca güvenilen ağdaki nginx'ten okur (`ReverseProxy__TrustedNetworks`, runbook §10.4). API ve veritabanı dışarıya kapalı. |
| **CI'da sınama** | `ReverseProxyRegistrationTests` (PR #88), `HttpsRequirementApiTests` (PR #116). Uçtan uca testler (#146) web imajının nginx'i üzerinden gerçek API ile çalışır. OpenAPI sözleşmesi ve üretilen ön yüz tipleri her PR'da denetlenir. |
| **UAT'de sınama** | Dağıtım betiği her dağıtımda sınar: web 200, `/health/ready` Healthy, HTTP → HTTPS 301, API portu dışarıdan kapalı. 12.10.2026'daki son dağıtımda (`v0.2.0-rc.5`) hepsi geçti; ayrıca çalışan konteynerlerin commit'i doğrulandı (#125). |
| **Bugün** | `/health/ready` **Healthy** (13.10.2026). Dışarıya açık TCP portları yalnızca 22, 80 ve 443 (konfigürasyon denetimi D-09). |
| **Anomali** | A-6 (§3) |

### 2.5 API ↔ PostgreSQL

| | |
|---|---|
| **CI'da sınama** | Entegrasyon testleri gerçek PostgreSQL ile (Testcontainers); `v0.2.0-rc.2`'de 268, `v0.2.0-rc.5` = `v0.2.0`'da 286 entegrasyon testi geçti (`TEC.9-dogrulama/kayitlar/2026-10-04-v0.2.0-rc.2-ci-kanit-ozeti.md`, `2026-10-12-v0.2.0-rc.5-ci-kanit-ozeti.md`). `IdempotentSchemaScriptTests`, dağıtımın kullandığı şema betiğini iki kez uygular (PR #101). Model ile migration arasındaki fark CI'da denetlenir. |
| **UAT'de sınama** | Her dağıtımda idempotent şema betiği uygulanır. 13.10.2026'da UAT'deki migration geçmişi `149f6ee`'deki 15 migration ile birebir aynıydı; sonuncusu `20261010203210_UserTwoFactorPreference` (konfigürasyon denetimi D-07). |
| **Anomali** | A-2, A-3 (§3) |

### 2.6 `v0.2.0-rc.3`…`rc.5` değişikliklerinin arayüz etkisi (1.2)

| Aday | Değişiklik | Etkilenen arayüz | Nasıl sınandı |
|---|---|---|---|
| `rc.3` (`fa3e194`, #185) | Kod gönderim ve üyelik deneme sınırlarının varsayılanları (PRM-KML-17, PRM-KML-18) | Arayüz değişmedi; yalnızca parametre değerleri | Birim ve entegrasyon testleri; CI 38051374806 |
| `rc.4` (`353eae7`, #190) | 2FA kullanıcı tercihine bağlı; İK'nın kurtarma işlemi | **API:** yeni uçlar `/api/v1/identity/account/two-factor`, `…/two-factor/setup`, `…/two-factor/setup/verification`, `…/two-factor/disable`, `/api/v1/identity/accounts/{personId}/two-factor/reset`; OpenAPI sözleşmesi ve üretilen ön yüz tipleri güncellendi. **PostgreSQL:** migration `20261010203210_UserTwoFactorPreference`. **NetGSM ve SMTP:** 2FA'yı açma kodu üyelikle aynı kod altyapısı ve kanallarla gönderilir | Entegrasyon testleri (`TwoFactorPreferenceApiTests`, `AccountsApiTests`), sözleşme denetimi, ekran testleri; CI 38089012238. **Uçtan uca senaryolar 2FA akışını içermiyor** (YAKLASIM §9) |
| `rc.5` (`149f6ee`, #198) | T5'e ait PRM-HSP-03 parametre ekranından çıkarıldı (parametre kataloğu) | Arayüz değişmedi | Mevcut parametre testleri (test kodu değişmedi); CI 38299593371 |

05–13.10.2026 arasında açılan `[HATA]` kayıtlarının (#171, #175, #182, #198) hiçbiri bir
arayüz anomalisi değildir; §3'e yeni anomali eklenmedi.

---

## 3. Anomaliler

| # | Arayüz | Tarih | Anomali | Bulunduğu yer | Düzeltme |
|---|---|---|---|---|---|
| A-1 | LOGO | 26.09 | Sır dosyasında tırnaksız LOGO bağlantı dizesi dağıtım betiğini bozuyordu | UAT dağıtımı | #79 → PR #80 |
| A-2 | PostgreSQL | 28.09 | Denetim izine her değişiklik iki kez yazılıyordu (bağlam fabrikası ara katmanları iki kez ekliyordu) | Entegrasyon testi | #93 → PR #94 |
| A-3 | PostgreSQL | 29.09 | Şema betiği UAT'yi düşürecekti | CI (`IdempotentSchemaScriptTests`) | PR #101 |
| A-4 | Saat | 29.09 | Erişim jetonu uygulama saatiyle üretilip sistem saatiyle doğrulanıyordu | Entegrasyon testi | #97 → PR #96 |
| A-5 | SMTP | 27.09 | 587 portunda süresi dolmuş, kendinden imzalı sertifika | UAT sağlık ucu | Sunucu tarafında, 01.10 (olay O-4) |
| A-6 | Tarayıcı ↔ API | 29.09 | Sunucu saati ~10 dk geri; kod ekranı hemen "süresi doldu" diyordu | UAT, gerçek SMS denemesi | #103 → PR #104 + NTP (risk R-21) |
| A-7 | Konteyner | 30.09 | Taban imajda CVE-2026-84782 | CI (trivy) | PR #108 (olay O-2) |

Anomalilerden dördü (A-2, A-3, A-4, A-7) CI'da, üçü (A-1, A-5, A-6) UAT'de bulundu. UAT'de
bulunanların ortak noktası **ortam yapılandırmasıdır** (sır dosyası biçimi, sertifika,
saat). Bu yüzden üretim kurulumunda ortam öğelerinin listesi ve denetimi gerekir
(#132, konfigürasyon öğeleri).

---

## 4. Entegre edilen yapı

| | |
|---|---|
| Sürüm | `v0.2.0` — commit `149f6ee` (açıklamalı Git etiketi; `v0.2.0-rc.5` ile aynı commit, `KR-097`) |
| İmajlar | `dsg-hrms-api:v0.2.0-rc.5`, `dsg-hrms-web:v0.2.0-rc.5` (ve `:uat`); commit OCI `revision` etiketinde. Sunucuda `v0.2.0` adlı imaj etiketi yoktur: kabul, kurulu adayın commit'ini onayladı ve yeniden dağıtım yapılmadı |
| Dağıtım | 12.10.2026 19:51 UTC, `TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`; sunucuda `VERSION` = `v0.2.0-rc.5` |
| CI kanıtı | Çalışma 38299593371; `TEC.9-dogrulama/kayitlar/2026-10-12-v0.2.0-rc.5-ci-kanit-ozeti.md` |

### 4.1 Önceki sürüm (1.0–1.1)

Raporun 1.0 ve 1.1 sürümleri `v0.2.0-rc.2`'yi (`69bafad`; dağıtım 04.10.2026 20:04 UTC;
CI 37229959287) entegre yapı olarak kaydediyordu. 1.2, kabul edilen yapıya göre
güncellendi; aradaki değişiklikler §2.6'dadır.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk sürüm (#134) | Bilgi İşlem |
| 2026-10-06 | 1.1 | SMTP sertifikası yenilendi (#179) | Bilgi İşlem |
| 2026-10-14 | 1.2 | Kabul edilen `v0.2.0` (`149f6ee`) için güncellendi (T3 modül sonu denetimi Ek C YB-07, #214): giriş ve §4 entegre yapı; §1 durum 13.10 canlı denetimine göre ve Sonuç paragrafı 1.1 ile tutarlı (SMTP yenilemesi kapandı); §2.3 "Bugün" yeni sertifika bitişi; §2.6 `rc.3`…`rc.5` arayüz etkisi | Bilgi İşlem |
