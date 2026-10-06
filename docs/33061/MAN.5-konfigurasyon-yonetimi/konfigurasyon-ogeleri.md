# Konfigürasyon Öğeleri Listesi

**Belge kimliği:** MAN.5-KOL
**Süreç:** MAN.5 — Konfigürasyon Yönetimi (`MAN.5.BP2`, `BP3`)
**Son güncelleme:** 2026-10-05 · **Sahibi:** Bilgi İşlem
**Kaynak:** T3 süreç denetimi, bulgu BULGU-05 (#132)

> Konfigürasyon öğesi (KÖ), değiştiğinde sistemin davranışını veya kanıtını değiştiren ve
> bu yüzden **kim, neyi, ne zaman değiştirdi** sorusunun cevaplanabilmesi gereken öğedir.
> Bu liste depodaki öğelerle birlikte **depo dışındaki** öğeleri de sayar. Bir öğe
> eklendiğinde veya yeri değiştiğinde liste PR ile güncellenir.

---

## 1. Baseline nasıl oluşur?

| Öğe grubu | Baseline | Kayıt |
|---|---|---|
| Depodaki öğeler (§2) | **Git etiketi** (`KR-097`): kabul adayı `vX.Y.0-rc.N`, kabul edilen `vX.Y.0` | `CHANGELOG.md` bölümü, etiket açıklaması |
| Dağıtılan yapı (§3.1) | Etiketten derlenen imajlar; commit OCI etiketinde | `TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`, sunucuda `deployments.log` |
| Depo dışı öğeler (§3.2, §3.3) | Bu listedeki konum ve değişiklik yolu | Konfigürasyon denetimi raporu (`raporlar/`) |

Modül kapanışında ve kabul adayı etiketlendiğinde konfigürasyon denetimi yapılır.
Denetim, depo dışı öğelerin bu listeyle uyumunu sınar.

---

## 2. Depodaki öğeler

Değişiklik yolu hepsinde aynıdır: dal → PR → CI → inceleme onayı (`KR-096`) → squash birleştirme.

| KÖ | Öğe | Yer | Not |
|---|---|---|---|
| KÖ-01 | Backend kaynak kodu | `src/backend/Dsg.Hrms.*` | |
| KÖ-02 | Backend testleri | `src/backend/tests/` | |
| KÖ-03 | Veritabanı migration'ları | `src/backend/Dsg.Hrms.Infrastructure/Data/Migrations/` | Uygulanmış migration değiştirilmez (ADR-0004); model–migration farkı CI'da denetlenir |
| KÖ-04 | Ön yüz kaynak kodu ve testleri | `src/frontend/dsg-hrms-web/src/`, `e2e/` | |
| KÖ-05 | Ön yüz metinleri | `src/frontend/dsg-hrms-web/src/locales/tr/` | SYG-KMLK-064 |
| KÖ-06 | API sözleşmesi | `docs/api/openapi-v1.json`; üretilen tipler `src/shared/api/generated/` | İkisi de derlemeden üretilir; güncelliği CI'da denetlenir |
| KÖ-07 | Bağımlılık sürümleri | `Directory.Packages.props`, `global.json`, `package-lock.json` | Açık taraması CI'da (.NET ve npm) |
| KÖ-08 | Gömülü veri | Yaygın parola listesi `Infrastructure/Identity/Passwords/` | Kaynak ve lisans aynı klasörün README'sinde |
| KÖ-09 | Konteyner imaj tanımları | `src/backend/Dsg.Hrms.Api/Dockerfile`, `src/frontend/dsg-hrms-web/Dockerfile` | Taban imajlar özetle (digest) sabit; trivy taraması CI'da |
| KÖ-10 | nginx yapılandırması | `src/frontend/dsg-hrms-web/nginx/` | TLS ve düz HTTP kipleri |
| KÖ-11 | Compose yığınları | `docker/compose.yml` (geliştirme), `compose.uat.yml`, `compose.e2e.yml` | |
| KÖ-12 | Örnek ortam dosyaları | `docker/.env.example`, `docker/.env.uat.example` | Yığının kullandığı her değişkeni içermeli (§ denetim D-04) |
| KÖ-13 | Dağıtım ve TLS betikleri | `docker/deploy-uat.sh`, `renew-tls.sh`, `acme-dns-hook.sh` | |
| KÖ-14 | CI iş akışları ve betikleri | `.github/workflows/`, `.github/scripts/` | |
| KÖ-15 | Git kancaları | `.githooks/` (`commit-msg`, `pre-push`) | Her makinede `core.hooksPath` ile etkinleştirilir (CONTRIBUTING §0) |
| KÖ-16 | GitHub şablonları ve sahiplik | `.github/ISSUE_TEMPLATE/`, PR şablonu, `CODEOWNERS` | |
| KÖ-17 | Proje belgeleri | `docs/` (ADR, karar defteri, 33061 kanıtları, şablonlar), `CHANGELOG.md`, `CONTRIBUTING.md`, `README.md` | |

## 3. Depo dışındaki öğeler

### 3.1 Dağıtılan yapı (UAT sunucusu)

| KÖ | Öğe | Yer | Değişiklik yolu | Doğrulama |
|---|---|---|---|---|
| KÖ-20 | API ve web imajları | Sunucuda `dsg-hrms-api:<sürüm>`, `dsg-hrms-web:<sürüm>` (ve `:uat`) | Yalnızca `deploy-uat.sh` | `docker inspect` OCI etiketi (runbook §3.2) |
| KÖ-21 | Dağıtım kaydı | `/opt/dsg-hrms/deployments.log`, `/opt/dsg-hrms/VERSION` | Yalnızca `deploy-uat.sh` yazar | Depodaki kayıtla karşılaştırma |
| KÖ-22 | Sunucudaki kaynak kopyası | `/opt/dsg-hrms/src`, `/opt/dsg-hrms/docker` | Her dağıtımda silinip `git archive` ile yeniden yazılır | Elle değiştirilmez |

### 3.2 Sunucu yapılandırması (UAT)

| KÖ | Öğe | Yer | Sahibi | Değişiklik yolu |
|---|---|---|---|---|
| KÖ-30 | UAT sır dosyası | `/opt/dsg-hrms/secrets/.env.uat` (izin `600`) | Bilgi İşlem | Elle, runbook §2.2 ve §9–§10'a göre. **İçerik depoya girmez**; değişken listesi KÖ-12 ile aynıdır |
| KÖ-31 | UAT TLS sertifikası | `/opt/dsg-hrms/tls/` | Bilgi İşlem | `renew-tls.sh` ile 90 günde bir, elle (`KR-067`, R-18). Bitiş: 16.12.2026 |
| KÖ-32 | NTP kaynakları | `/etc/chrony/sources.d/dsg-hrms-ntp.sources` | Bilgi İşlem | Elle, runbook NTP bölümü (#103, R-21) |
| KÖ-33 | Docker Engine ve işletim sistemi | UAT sunucusu (Ubuntu 26.04) | Bilgi İşlem | Elle, runbook §2.1 |
| KÖ-34 | SSH erişimi | UAT sunucusu `/root/.ssh/authorized_keys`; geliştirici makinesinde `~/.ssh/dsg-hrms-uat` | Bilgi İşlem | Elle. Anahtarla erişim; parola girişi bilerek açık (`KR-102`, #174) |
| KÖ-35 | ~~Webmin yönetim paneli~~ | — | — | **Kaldırıldı (06.10.2026, #179).** 06.10.2026'da belgelenmemiş olarak bulunmuştu; kullanılmıyordu |

### 3.3 Dış hizmetler ve kurum altyapısı

| KÖ | Öğe | Yer | Sahibi | Not |
|---|---|---|---|---|
| KÖ-40 | LOGO salt-okunur veritabanı hesabı | LOGO SQL Server | Bilgi İşlem | `DENY` yazma; CI'da benzer hesapla sınanır (PR #76) |
| KÖ-41 | NetGSM hesabı ve gönderici başlığı | NetGSM | Bilgi İşlem | Başlık `DUZEN` (PRM-ENT-03) |
| KÖ-42 | SMTP gönderen hesabı | `mail.duzen.com.tr` | Bilgi İşlem | PRM-ENT-05 |
| KÖ-43 | E-posta sunucusu sertifikası (587) | `mail.duzen.com.tr` Postfix | Bilgi İşlem (e-posta yöneticisi) | Sectigo; bitiş **27.03.2027**; bitişten 2–3 gün önce yenilenir, ardından Postfix yeniden yüklenir (R-20) |
| KÖ-44 | DNS kayıtları | Kurum DNS'i: `insankaynaklaritest` A kaydı, DNS-01 TXT kaydı | Kurum DNS yöneticisi | TXT kaydı her yenilemede elle (R-18) |
| KÖ-45 | GitHub depo ayarları | Depo: etiketler, milestone'lar, Actions izinleri, plan | Bilgi İşlem | Sunucu tarafı dal koruması yok (`KR-055`, R-16) |

### 3.4 Uygulama içindeki yapılandırma

| KÖ | Öğe | Yer | Değişiklik yolu |
|---|---|---|---|
| KÖ-50 | Sistem parametreleri (PRM-*) | Veritabanı `settings.system_parameter`; varsayılanlar kodda (`ParameterCatalog`), bir kısmı yapılandırmadan | Parametre ekranı (Sistem Yöneticisi); her değişiklik denetim izinde (SYG-KMLK-075, 076) |
| KÖ-51 | Kurumsal logo | Veritabanı | Parametre ekranı (SYG-KMLK-069) |
| KÖ-52 | Rol ve izin atamaları | Veritabanı; izin kodları kodda sabit | T4'e kadar migration ve ilk yönetici yapılandırması (`KR-089`); atamalar denetim izinde |

> **KÖ sayılmayanlar:** UAT veritabanındaki iş verisi (personel, hesaplar, denetim izi)
> konfigürasyon değil **veridir**; yedekleme kuralı `KR-098`'dedir. CI yapıtları kanıttır;
> kalıcı özeti `TEC.9-dogrulama/kayitlar/` altındadır (#136).

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#132) | Bilgi İşlem |
| 2026-10-06 | 1.1 | KÖ-34 SSH anahtarı; KÖ-35 Webmin eklendi (#177) | Bilgi İşlem |
| 2026-10-06 | 1.2 | KÖ-35 Webmin kaldırıldı; KÖ-43 yeni sertifika (#179) | Bilgi İşlem |
