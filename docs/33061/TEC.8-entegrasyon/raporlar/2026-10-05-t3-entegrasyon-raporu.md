# T3 Kimlik Yönetimi — Entegrasyon Raporu

**Belge kimliği:** TEC.8-RPR-2026-10-05-T3
**Süreç:** TEC.8 — Entegrasyon (`TEC.8.BP2`, `BP3`)
**Entegre edilen sürüm:** `v0.2.0-rc.2` (commit `69bafad`), UAT'de 04.10.2026'dan beri (`TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`)
**Dayanak:** SYG-KMLK §2.3 dış arayüzler; ADR-0003 (LOGO), ADR-0012 (bildirim), `KR-093` (HTTPS ve ters vekil)
**Tarih:** 2026-10-05 · **Hazırlayan:** Bilgi İşlem · **Kaynak:** T3 süreç denetimi, Ek C §1 (#134)

> Bu rapor, T3'ün dış sistemlerle ve kendi bileşenleri arasındaki arayüzlerinin **nasıl,
> ne zaman ve hangi sonuçla** sınandığını tek yerde toplar. Sonuçlar daha önce PR
> metinlerine, CI kayıtlarına ve runbook'a dağılmıştı. Entegrasyon yaklaşımı
> (`TEC.8-entegrasyon/YAKLASIM.md`) #130 kapsamında yazılacak.

---

## 1. Özet

| Arayüz | Yön | CI'da sınama | UAT'de canlı sınama | Bugünkü durum (05.10.2026) | Açık iş |
|---|---|---|---|---|---|
| LOGO veritabanı | Okuma | ✅ Yazma reddi | ✅ 26.09'dan beri senkronizasyon | `/health/sync`: **Healthy** | — |
| NetGSM (SMS) | Giden | ✅ 12 sonuç kodu | ✅ Gerçek SMS ulaştı (29.09) | `/health/notifications` sms: **Healthy** | Teslim durumu sorgusu (R-14) |
| Kurum SMTP | Giden | ✅ Gerçek SMTP konuşması | ⚠️ 27.09 Unhealthy → 01.10 düzeldi | email: **Healthy**; sertifika 06.10.2026'da yenilenmiş bulundu, bitiş **27.03.2027** (1.1) | — |
| Tarayıcı ↔ nginx ↔ API | Gelen | ✅ Vekil ve HTTPS kuralları, uçtan uca testler | ✅ Her dağıtımda öz denetim | `/health/ready`: **Healthy**; HTTP → HTTPS 301 | — |
| API ↔ PostgreSQL | İç | ✅ Gerçek PostgreSQL, şema betiği | ✅ Her dağıtımda şema betiği | **Healthy** | — |

**Sonuç:** T3'ün beş arayüzü de CI'da ve UAT'de sınandı ve bugün sağlıklı. Entegrasyon
sırasında **7 anomali** bulundu; hepsi kapandı (§3). Açık kalan iki iş risk defterinde
izleniyor: SMTP sertifikasının yenilenmesi (R-20) ve SMS teslim durumu sorgusu (R-14).

---

## 2. Arayüzler

### 2.1 LOGO veritabanı (salt okuma)

| | |
|---|---|
| **Kısıt** | Yazma, güncelleme ve silme kesinlikle yapılmaz (`KR-003`, `KR-004`, SYG-KMLK-002, 003). LOGO erişimi yalnızca `LogoDbContext` ve `LogoPersonnelSource` üzerinden; mimari testi başka yerden SQL Server bağımlılığını engeller. |
| **CI'da sınama** | `LogoReadOnlyAccessTests` (PR #76, 26.09.2026): Gerçek bir SQL Server Express konteynerinde, salt okuma yetkili hesabın yazma girişimi reddedilir. Okunan alanların kapsamı `LogoFieldScopeTests` ile sınanır. Senkronizasyon motoru `PersonnelSynchronizerTests`. |
| **UAT'de sınama** | 26.09.2026'dan beri her 15 dakikada canlı senkronizasyon (PR #75, #76). Canlı veride bulunan ad ve ortak e-posta çelişkisi (AN-17) kurala bağlandı: yalnızca aktif siciller (`KR-079`, #77, PR #78). |
| **Bugün** | `/health/sync`: `personnel-sync` **Healthy** (05.10.2026). |
| **Anomali** | A-1 (§3) |

### 2.2 NetGSM (SMS)

| | |
|---|---|
| **Kısıt** | Standart SMS servisi (`KR-042`), HTTPS; gönderici başlığı `DUZEN` (PRM-ENT-03). UAT'de izin listesi kipi (`KR-083`). |
| **CI'da sınama** | `NetGsmSmsSenderTests` (PR #86, 27.09.2026): NetGSM'in 12 sonuç kodu sahte yanıtlarla; yeniden deneme yalnızca geçici hatalarda. Dağıtıcı ve gönderim kaydı `NotificationDispatcherTests`. |
| **UAT'de sınama** | Kimlik doğrulama denetimi 27.09.2026'da **Healthy**. Gerçek SMS UAT'de 29.09.2026'da ulaştı (üyelik denemesi; #103 bu denemede bulundu). |
| **Bugün** | `/health/notifications`: sms **Healthy** (05.10.2026). |
| **Açık iş** | Teslim durumu sorgusu ve kara liste raporu yapılmadı (R-14 önlem 2–3). |

### 2.3 Kurum SMTP sunucusu (e-posta)

| | |
|---|---|
| **Kısıt** | `mail.duzen.com.tr:587`, STARTTLS; sunucu sertifikası **doğrulanır**, doğrulama kapatılmaz (runbook §10.5). |
| **CI'da sınama** | `SmtpEmailSenderTests` (PR #86): Mailpit konteyneriyle gerçek SMTP konuşması; Türkçe şablon. Uçtan uca testler (#146) e-postayla gelen kodu ve davet bağlantısını Mailpit'ten okur. |
| **UAT'de sınama** | 27.09.2026: **Unhealthy**. 587 portu süresi 07.03.2026'da dolmuş, kendinden imzalı bir sertifika sunuyordu (A-5). Sertifika 01.10.2026'da düzeltildi; 02.10.2026'da `email: Healthy`. 03.10.2026'da İK davet bağlantıları e-postayla ulaştı. |
| **Bugün** | `email` **Healthy** (05.10.2026). 587 portu `CN=mail.duzen.com.tr` sertifikasını sunuyor; geçerlilik sonu **11.10.2026 23:59 GMT**. |
| **Açık iş** | ~~Sertifikanın yenilenmesi~~ — **Kapandı (1.1, 06.10.2026):** Sectigo, geçerlilik 04.10.2026–27.03.2027; 587 portunda doğrulama `0 (ok)`; email Healthy. Sonraki yenileme R-20'de izleniyor. |

### 2.4 Tarayıcı ↔ nginx ↔ API

| | |
|---|---|
| **Kısıt** | Yalnızca HTTPS (`KR-093`, SYG-KMLK-063). API, istemci IP'sini ve şemayı yalnızca güvenilen ağdaki nginx'ten okur (`ReverseProxy__TrustedNetworks`, runbook §10.4). API ve veritabanı dışarıya kapalı. |
| **CI'da sınama** | `ReverseProxyRegistrationTests` (PR #88), `HttpsRequirementApiTests` (PR #116). Uçtan uca testler (#146) web imajının nginx'i üzerinden gerçek API ile çalışır. OpenAPI sözleşmesi ve üretilen ön yüz tipleri her PR'da denetlenir. |
| **UAT'de sınama** | Dağıtım betiği her dağıtımda sınar: web 200, `/health/ready` Healthy, HTTP → HTTPS 301, API portu dışarıdan kapalı. 04.10.2026'daki son dağıtımda (`v0.2.0-rc.2`) hepsi geçti; ayrıca çalışan konteynerlerin commit'i doğrulandı (#125). |
| **Bugün** | `/health/ready` **Healthy** (05.10.2026). |
| **Anomali** | A-6 (§3) |

### 2.5 API ↔ PostgreSQL

| | |
|---|---|
| **CI'da sınama** | Entegrasyon testleri gerçek PostgreSQL ile (Testcontainers); `v0.2.0-rc.2`'de 268 entegrasyon testi geçti (`TEC.9-dogrulama/kayitlar/2026-10-04-v0.2.0-rc.2-ci-kanit-ozeti.md`). `IdempotentSchemaScriptTests`, dağıtımın kullandığı şema betiğini iki kez uygular (PR #101). Model ile migration arasındaki fark CI'da denetlenir. |
| **UAT'de sınama** | Her dağıtımda idempotent şema betiği uygulanır. |
| **Anomali** | A-2, A-3 (§3) |

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
| Sürüm | `v0.2.0-rc.2` — commit `69bafad` (açıklamalı Git etiketi) |
| İmajlar | `dsg-hrms-api:v0.2.0-rc.2`, `dsg-hrms-web:v0.2.0-rc.2`; commit OCI etiketinde |
| Dağıtım | 04.10.2026 20:04 UTC, `TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md` |
| CI kanıtı | Çalışma 37229959287; `TEC.9-dogrulama/kayitlar/2026-10-04-v0.2.0-rc.2-ci-kanit-ozeti.md` |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk sürüm (#134) | Bilgi İşlem |
| 2026-10-06 | 1.1 | SMTP sertifikası yenilendi (#179) | Bilgi İşlem |
