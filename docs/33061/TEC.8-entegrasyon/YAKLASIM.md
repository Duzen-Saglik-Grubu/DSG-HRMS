# TEC.8 — Süreç Yaklaşımı

**Belge kimliği:** TEC.8-YAK
**Süreç:** TEC.8 — Entegrasyon
**Son güncelleme:** 2026-10-05
**Karşıladığı öznitelik maddeleri:** `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir. `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir. Ayrıca `PA 2.1` için hedef, sorumluluk, kaynak ve taraflar arası arayüzler (§1, §4, §5).

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Kontrollerin kendisi CI
> iş akışlarında, dağıtım betiğinde ve runbook'tadır; bu belge onları bir entegrasyon
> stratejisi olarak bir araya getirir.

---

## 1. Sürecin amacı ve sınırı

Entegrasyon, sistem öğelerini ve dış sistemleri **tutarlı biçimde birleştirir** ve
birleşen arayüzlerin doğru çalıştığını gösterir (TS ISO/IEC TS 33061, Madde 5, TEC.8).

**Kapsamdadır:** backend, frontend (nginx), PostgreSQL arasındaki iç arayüzler; LOGO,
NetGSM ve kurum SMTP sunucusuyla dış arayüzler; birleşik yapının kimliği (sürüm,
imaj, dağıtım kaydı).

**Kapsamda değildir:** tek öğenin gereksinimini karşıladığının kanıtı (TEC.9), UAT
sunucusunun kurulumu ve dağıtım yordamı (TEC.10, runbook), İK kabulü (TEC.11).

### 1.1 Hedefler

- Her PR'da öğeler gerçek bağımlılıklarıyla birlikte derlenir ve sınanır; CI kırmızıysa birleştirilmez.
- UAT'ye yalnızca commit edilmiş ve GitHub'a gönderilmiş kod kurulur; çalışan imajın commit'i dağıtılanla aynıdır.
- Kabule sunulan birleşik yapı etiketle tanımlıdır (`KR-097`).
- Her modül kapanışında arayüz bazında bir entegrasyon raporu yazılır.

---

## 2. Arayüzler ve entegrasyon kısıtları

| Arayüz | Yön / protokol | Kısıt | Kaynak |
|---|---|---|---|
| **LOGO** (MSSQL) | Okuma, TDS | Yazma, güncelleme, silme yapılmaz; ayrı `LogoDbContext`, şema yönetilmez (`--context HrmsDbContext`) | `KR-003`, `KR-004`, ADR-0003 |
| **NetGSM** | Giden, HTTPS | Standart SMS servisi; gönderim kipi | ADR-0012, `KR-083` |
| **Kurum SMTP** (`mail.duzen.com.tr:587`) | Giden, SMTP + STARTTLS | Sunucu sertifikası doğrulanır, doğrulama kapatılmaz | Runbook §10.5, R-20 |
| **Tarayıcı ↔ nginx ↔ API** | Gelen, HTTPS | Kurum içi erişim; API yalnızca HTTPS isteğe hizmet verir; `X-Forwarded-*` yalnızca güvenilen vekil ağından okunur | `KR-067`, `KR-093`, runbook §10.4, R-22 |
| **API ↔ PostgreSQL** | İç ağ | Şema idempotent betikle uygulanır; veritabanı ve API dışarıya kapalı | ADR-0004, runbook §1 |

Kaynak liste: `SYG-KMLK.md` §2.3. Yeni modül yeni dış arayüz getirirse önce SYG §2.3'e,
sonra bu tabloya eklenir.

---

## 3. Süreç nasıl işletilir?

Entegrasyon dört kontrol noktasında yapılır. Her nokta bir öncekinin geçmesini bekler.

### 3.1 Her PR'da (CI)

| Kontrol | Ne birleştirilir / sınanır | Yer |
|---|---|---|
| Entegrasyon testleri | Gerçek HTTP + PostgreSQL 17 (Testcontainers) | `ci.yml` `build-and-test` |
| LOGO salt okuma | Depodaki oturum betiği gerçek SQL Server'da çalışır; yazma denemeleri reddedilmeli (`LogoReadOnlyAccessTests`) | Aynı iş |
| SMTP | Gerçek SMTP konuşması, Mailpit'e (`SmtpEmailSenderTests`) | Aynı iş |
| NetGSM | Sonuç kodları sahte yanıtlarla (`NetGsmSmsSenderTests`) | Aynı iş |
| Dağıtım şeması | Dağıtımın kullandığı idempotent betik iki kez uygulanır (`IdempotentSchemaScriptTests`) | Aynı iş |
| API sözleşmesi | Koddan üretilen OpenAPI belgesi = depodaki; frontend tipleri = belge | `api-contract-check.yml`; `ci.yml` `frontend` |
| İmajlar | API ve web imajı derlenir, `trivy` ile taranır | `ci.yml` `container-scan` |
| Uçtan uca | Web (nginx) + API + PostgreSQL + Mailpit atılabilir yığında, gerçek tarayıcıyla (Playwright) | `ci.yml` `e2e`, `docker/compose.e2e.yml` |

### 3.2 Kabul adayı sürümde

Kabule sunulacak commit açıklamalı etiketle işaretlenir (ör. `v0.2.0-rc.2`) ve
`CHANGELOG.md`'de bölümü yazılır. Etiketli commit'in CI çalışmasından kanıt özeti
üretilir (`TEC.9-dogrulama/kayitlar/`, #136).

### 3.3 UAT dağıtımında (`docker/deploy-uat.sh`)

Betik, kaynağı `git archive` ile aktarır, imajları sürüm etiketi ve
`org.opencontainers.image.revision` / `.version` etiketleriyle derler, şemayı uygular
ve **şunları doğrulamadan dağıtımı kaydetmez:**

| Öz denetim | Beklenen |
|---|---|
| Web arayüzü | `200` |
| `/health/ready` | `Healthy` |
| Düz HTTP (TLS etkinse) | `301`, hedef `https://` |
| API portu (`5299`) dışarıdan | Kapalı |
| Çalışan API ve web konteynerinin `revision` etiketi | Dağıtılan commit |

Geçerse satır sunucudaki `deployments.log`'a ve
`TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`'ye yazılır.

### 3.4 Dağıtım sonrası, kabulden önce (canlı dış arayüzler)

| Kontrol | Beklenen | Runbook |
|---|---|---|
| `/health/sync` | `Healthy` (LOGO son çalışması başarılı ve güncel) | §8.2 |
| `/health/notifications` | Her iki kanal `Healthy` (SMTP oturumu ve NetGSM kimliği; ileti göndermez) | §10.3 |
| Güvenilen vekil ağı | Docker ağı `172.16.0.0/12` içinde; doğrudan şifresiz API isteği `403` | §10.4 |
| SMTP sertifikası | `Verify return code: 0 (ok)` | §10.5 |

### 3.5 Modül kapanışında

Entegrasyon raporu yazılır: `raporlar/YYYY-AA-GG-<modül>-entegrasyon-raporu.md`. Her
arayüz için CI'daki sınama, UAT'deki canlı sınama, güncel durum ve açık iş; ayrıca
bulunan anomaliler. T3 raporu: `raporlar/2026-10-05-t3-entegrasyon-raporu.md` (#134).
Beş arayüzü kapsar ve yedi anomali kaydeder (A-1…A-7): dördü CI'da, üçü UAT'de
bulundu; UAT'dekilerin üçü de ortam yapılandırmasıydı.

### 3.6 Anomaliler

CI'da bulunan anomali PR'da düzeltilir; `main`'de bulunursa `main-failure-check.yml`
`[HATA]` issue'su açar (#129). UAT'de bulunan anomali `[HATA]` issue'su ve gerekiyorsa
runbook değişikliğiyle kapatılır; tekrarlayan ortam riski risk defterine girer
(ör. R-20, R-21, R-22). Hepsi modülün entegrasyon raporunda listelenir.

---

## 4. Roller ve taraflar (`PA 2.1`)

| Rol | Sorumluluk |
|---|---|
| Bilgi İşlem (R1) | Entegrasyonu planlar, yürütür, raporu yazar (RACI: TEC.8 **A**/R) |
| Mali İşler / Bordro (P7) | LOGO erişim kurallarında danışılır (RACI: C) |
| Kurum e-posta sunucusu | `587` sertifikasının geçerliliği ve Postfix'in yeniden yüklenmesi (R-20) |
| NetGSM | SMS hizmeti; hesap ve gönderici adı |

RACI: `docs/33061/MAN.1-proje-planlama/roller-ve-sorumluluklar.md` §3.1.

---

## 5. Destekleyici sistemler

GitHub Actions (§3.1); Testcontainers ile PostgreSQL 17, SQL Server ve Mailpit; uçtan
uca yığın (`docker/compose.e2e.yml`, `docker/e2e/up.sh`: sentetik veri, e-posta dışarı
çıkmaz); canlı dış arayüzler için UAT sunucusu (`docker/compose.uat.yml`).

---

## 6. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Arayüz tanımı | `SYG-<MODÜL>.md` §2.3; `docs/api/openapi-v1.json` | Tablo; koddan üretilen OpenAPI | Bilgi İşlem |
| CI entegrasyon sonuçları | GitHub Actions | Otomatik; yapıtlar 90 gün | CI |
| CI kanıt özeti | `TEC.9-dogrulama/kayitlar/<tarih>-<sürüm>-ci-kanit-ozeti.md` | `.github/scripts/ci-evidence-summary.mjs` çıktısı | Bilgi İşlem |
| Birleşik yapının kimliği | Git etiketi; imaj etiketi ve OCI `revision` etiketi; `CHANGELOG.md` | `KR-097` | Bilgi İşlem |
| Dağıtım kaydı | `TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`; sunucuda `deployments.log` | Zaman (UTC), sürüm, commit, dağıtan | `deploy-uat.sh` + PR |
| Entegrasyon raporu | `raporlar/YYYY-AA-GG-<modül>-entegrasyon-raporu.md` | §3.5 | Bilgi İşlem |
| Anomali kaydı | GitHub issue (`[HATA]`) | Belirti, kök neden, düzeltme | Bulan |

---

## 7. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** Rapor, dağıtım kaydı ve bu belge yalnızca PR ile değişir.
- **Değişmezlik:** Dağıtım kaydına yalnızca satır eklenir. Raporda sonradan değişen sonuç
  silinmez; yeni durum tarihiyle eklenir.
- **Kalıcılık:** CI kayıtları 90 gün sonra silindiği için kabul adayı etiketinde ve
  modül kapanışında kanıt özeti depoya alınır (TEC.9 YAKLASIM §6).
- **Sır ve kişisel veri:** Raporlara ve kayıtlara sır, bağlantı dizesi, gerçek kişisel
  veri yazılmaz; runbook komutları sırrı ekrana yazdırmaz.

---

## 8. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (TEC.8) | Karşılığı |
|---|---|
| a) Arayüzler dâhil entegrasyon kısıtları belirlenir | §2 |
| b) Entegrasyon yaklaşımı ve kontrol noktaları belirlenir | §3 |
| c) Destekleyici sistemler mevcut | §5 |
| d) Sistem entegre edilir | §3.3; etiket, imaj, dağıtım kaydı |
| e) Öğeler arası arayüzler sınanır | §3.1 (entegrasyon, sözleşme, uçtan uca) |
| f) Sistemle dış çevre arasındaki arayüzler sınanır | §3.1 (CI) ve §3.4 (UAT, canlı) |
| g) Sonuçlar ve anomaliler belirlenir | Entegrasyon raporu, `[HATA]` issue'ları |
| h) Entegre öğelerin izlenebilirliği | Etiket → commit → imaj `revision` → dağıtım kaydı; matris |

---

## 9. Açık noktalar

| No | Açık nokta |
|---|---|
| 1 | `deploy-uat.sh` dış arayüzleri denetlemez; §3.4 elle yapılır ve sonucu dağıtım kaydına girmez. Başarısız `/health/notifications` dağıtımı durdurmaz |
| 2 | UAT imajları sunucuda yeniden derlenir. CI'da `trivy` ile taranan imaj aynı commit'ten derlenir ama aynı ikili dosya değildir; imaj özeti (digest) karşılaştırılmaz |
| 3 | Uçtan uca yığın HTTPS'siz çalışır (`Security__RequireHttps: 'false'`) ve LOGO ile NetGSM'i içermez. HTTPS zinciri ve iki dış arayüz yalnızca UAT'de canlı sınanır |
| 4 | Modül kapanışları arasında entegrasyon sonucu yalnızca PR metinlerinde ve 90 günlük CI kayıtlarında durur |

---

## 10. Gözden geçirme

Bu belge her modül sonu süreç denetiminde (MAN.8) ve yeni bir dış arayüz eklendiğinde
gözden geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
