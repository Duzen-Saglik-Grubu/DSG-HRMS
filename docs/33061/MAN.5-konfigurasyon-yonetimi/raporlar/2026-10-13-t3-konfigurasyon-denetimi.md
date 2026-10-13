# T3 Modül Sonu Konfigürasyon Denetimi

**Belge kimliği:** MAN.5-KD-2026-10-13
**Süreç:** MAN.5 — Konfigürasyon Yönetimi (`MAN.5.BP5`)
**Denetlenen baseline:** `v0.2.0` (commit `149f6ee`), T3 Kimlik Yönetimi — İK kabulü 13.10.2026 (G3, `KR-105`)
**Ölçüt:** `konfigurasyon-ogeleri.md` 1.2, `YAKLASIM.md` 1.0
**Tarih:** 2026-10-13 (sunucu ölçümleri 18:10–18:20 UTC) · **Denetleyen:** Bilgi İşlem · **Kaynak:** modül sonu denetimi (T3 kabulü); önceki denetim `2026-10-05-ilk-konfigurasyon-denetimi.md` G-1

> Denetim şu soruyu cevaplar: **Kabul edilen yapı, kayıtlarda yazan yapı mı?** Etiketler,
> CHANGELOG, dağıtım kaydı ve CI kanıtı birbirleriyle; UAT sunucusundaki imajlar,
> veritabanı şeması, açık portlar ve sertifikalar da bu kayıtlarla karşılaştırıldı.
> Denetim sırasında depoda ve sunucuda hiçbir şey değiştirilmedi.

---

## 1. Sonuç

| | Sayı |
|---|---|
| Denetim maddesi | 20 |
| Uygun | **14** |
| Kısmen uygun (gözlem/öneri var) | 4 — D-09, D-16, D-17, D-20 |
| Bu denetimde sınanamadı | 1 — D-18 (yerel Docker çalışmıyordu) |
| Uygunsuzluk | **1** — D-19 (`main` kırmızıyken kayıt issue'su açılmadı) |

Kabul edilen `v0.2.0` baseline'ı için etiket, CHANGELOG, CI kanıtı, UAT'deki imajlar,
dağıtım kaydı ve veritabanı şeması **birbiriyle tutarlı**. UAT, tam olarak `149f6ee`
commit'ini çalıştırıyor; şemadaki 15 migration depodakiyle birebir aynı.

Sapmalar baseline'ın kendisinde değil, **öğe listesinin kapsamında** ve bir **telafi
kontrolünün işleyişinde**:

- Öğe listesinde bulunmayan depo içi ve depo dışı öğeler var (D-16, D-17).
- UAT'de belgelenmemiş iki servis dinliyor: `cupsd` yalnızca yerelde, `avahi-daemon` ise tüm arayüzlerde UDP 5353'te (D-09).
- 05.10.2026'da `main` yaklaşık 13,5 saat kırmızı kaldı. `main-failure-check.yml` altı kez tetiklendi; her seferinde iptal oldu ve `[HATA] main kırmızı` issue'su açılmadı (D-19).

---

## 2. Maddeler

### 2.1 Baseline ve sürüm kayıtları

| # | Madde | Yöntem | Sonuç |
|---|---|---|---|
| D-01 | Her sürüm etiketi açıklamalı ve CHANGELOG'da bölümü var (`KR-097`) | `git cat-file -t`, `git tag -l --format='%(contents)'` ↔ `CHANGELOG.md` başlıkları; `git ls-remote --tags origin` | ✅ 7 etiketin 7'si de açıklamalı (`tag`) ve `origin`'de: `v0.1.0` → `065a7c3`, `rc.1` → `6cfeef0`, `rc.2` → `69bafad`, `rc.3` → `fa3e194`, `rc.4` → `353eae7`, `rc.5` → `149f6ee`, `v0.2.0` → `149f6ee`. Her etiket açıklaması kendi CHANGELOG bölümüne atıf yapıyor (`[0.1.0]`, `[0.2.0-rc.1]` … `[0.2.0-rc.5]`, `[0.2.0]`). Bölümlerin hepsi `main`'de var. Gözlem G-3, G-4 |
| D-02 | Kabul baseline'ı kabul edilen aday ile aynı commit | `git rev-list -n1 v0.2.0` ↔ `v0.2.0-rc.5`; kabul formu | ✅ İkisi de `149f6ee21c3f…`. `v0.2.0` 13.10.2026 21:10 (+03) tarihinde etiketlendi. Açıklama: "IK tarafindan 13.10.2026'da kabul edildi (G3, KR-105). v0.2.0-rc.5 ile ayni commit. … CHANGELOG [0.2.0]; KR-097". Kabul formu (`TEC.11-gecerleme/kayitlar/2026-10-13-t3-kabul-formu.md`) `v0.2.0-rc.5` (`149f6ee`) sürümüne atıf yapıyor. CHANGELOG `[0.2.0]` bölümü de aynı commit'i yazıyor |
| D-03 | Etiketli her commit CI'dan geçmiş; adayın kalıcı CI kanıtı var | `gh run list --commit <sha> --workflow ci.yml` | ✅ Etiketli altı commit'in altısında da CI başarılı: `065a7c3` 34700656926, `6cfeef0` 37225003786, `69bafad` 37229959287, `fa3e194` 38051374806, `353eae7` 38089012238, `149f6ee` 38299593371. `149f6ee`'de "Dal Koruma Denetimi" ve "API sozlesme denetimi" de başarılı. Kanıt özeti `TEC.9-dogrulama/kayitlar/2026-10-12-v0.2.0-rc.5-ci-kanit-ozeti.md`: backend 796/796 test, satır kapsamı %92,9; ön yüz 247/247 test. rc.5 etiketi 19:50:47Z'de, CI bittikten sonra atıldı. Özeti olmayan tek aday `rc.1`; özet düzeni (#136) ondan sonra kuruldu |

### 2.2 UAT'de çalışan yapı (KÖ-20…KÖ-22, KÖ-30…KÖ-33)

| # | Madde | Yöntem | Sonuç |
|---|---|---|---|
| D-04 | UAT'de çalışan imajlar baseline commit'inden (KÖ-20) | `docker ps`; `docker inspect` OCI `revision`/`version` etiketleri; `/opt/dsg-hrms/VERSION` | ✅ `dsg-hrms-uat-api` (`dsg-hrms-api:uat`) ve `dsg-hrms-uat-web` (`dsg-hrms-web:uat`) çalışıyor; ikisi de 22 saattir `healthy`. İkisinin etiketi de `v0.2.0-rc.5` / `149f6ee21c3f…`. `:uat` ve `:v0.2.0-rc.5` aynı imaj (API `0f4e21428cbb`, web `5d19fd47d5cf`). `VERSION`: `v0.2.0-rc.5 149f6ee…`. `dsg-hrms-uat-postgres` (`postgres:17-alpine`) iki haftadır çalışıyor. Eski aday imajları (`rc.1`…`rc.4`) kendi commit etiketleriyle sunucuda duruyor. Gözlem G-5 |
| D-05 | Sunucudaki dağıtım kaydı depodaki kayıtla aynı (KÖ-21) | `deployments.log` ↔ `TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md` | ✅ İki kayıtta da aynı beş satır var: `rc.1` 2026-10-04T18:47:00Z, `rc.2` 2026-10-04T20:04:34Z, `rc.3` 2026-10-10T13:02:28Z, `rc.4` 2026-10-10T21:56:02Z, `rc.5` 2026-10-12T19:51:46Z. Zaman, sürüm, commit ve dağıtan alanları birebir eşleşiyor |
| D-06 | Sunucudaki kaynak kopyası commit'ten (KÖ-22) | `ls -la /opt/dsg-hrms`, `/opt/dsg-hrms/docker`; `deploy-uat.sh` satır 49, 69, 79–81 | ✅ `src/`, `docker/`, `global.json`, `assets/` 12.10.2026 22:43 (+03) tarihli; bu, rc.5 dağıtımının `git archive` adımıdır. `docker/` altında yalnızca örnek ortam dosyaları var, yerel `.env` yok. Betik bunlara ek olarak `global.json`, `assets/` ve `sema.sql` (idempotent migration betiği) de yazıyor; KÖ-22 bunları saymıyor (D-16, öneri Ö-3) |
| D-07 | UAT veritabanı şeması baseline'daki migration'larla aynı (KÖ-03) | UAT `__ef_migrations_history` (kullanıcı ve veritabanı adı `printenv` ile alındı, parola kullanılmadı) ↔ `git ls-tree 149f6ee …/Data/Migrations` | ✅ 15/15 aynı; fark yok. Son migration iki tarafta da `20261010203210_UserTwoFactorPreference` (EF 10.0.12). `149f6ee..HEAD` arasında `src/` değişmedi |
| D-08 | Örnek ortam dosyaları yığınları tam karşılıyor (KÖ-12); sunucudaki sır dosyası örnekle uyumlu (KÖ-30) | `compose*.yml` `${…}` ↔ `.env*.example`; sır dosyasında yalnızca **değişken adları** (`grep -oE '^[A-Z0-9_]+='`) ↔ `.env.uat.example` | ✅ Geliştirme 19/19, UAT 22/22; iki yönde de fark yok. Sunucudaki sır dosyasında 21 değişken var; örnekte olup dosyada olmayan tek değişken `IDENTITY_PUBLIC_BASE_URL`. Bu değişken isteğe bağlı: `compose.uat.yml` varsayılanı `https://insankaynaklaritest.duzen.com.tr`, runbook bunu yazıyor ve konteynerdeki değer doğru. KÖ-30'daki "değişken listesi KÖ-12 ile aynıdır" ifadesi bu durumu karşılamıyor (öneri Ö-6) |
| D-09 | UAT'de yalnızca belgelenmiş portlar dinliyor | `ss -ltnp`, `ss -lunp` ↔ runbook §2 port tablosu ve §11 ("Dışarıya açık portlar: 22, 80, 443") | ⚠️ **Dışarıya açık TCP** yalnızca 22 (`sshd`), 80 ve 443 (`docker-proxy` → web). Yalnızca `127.0.0.1`'de 5299 (API) ve 5434 (PostgreSQL) var; runbook'ta belgeli. Webmin (10000) yok. **Belgelenmemiş servisler:** `cupsd` `127.0.0.1:631/tcp`'de (yazdırma servisi, yalnızca yerel); `avahi-daemon` `0.0.0.0:5353/udp`'de (mDNS, **tüm arayüzlerde**). İkisi de `enabled` ve `active`. `systemd-resolved` (53, yerel) ve `chronyd` (323/udp, yerel) işletim sisteminin olağan bileşenleri. Sunucu masaüstü kurulumu (`ubuntu-desktop`, `graphical.target`). Bulgu B-1 |
| D-10 | UAT TLS sertifikası kayıttaki tarihte (KÖ-31) | `openssl s_client -connect insankaynaklaritest.duzen.com.tr:443` (UAT'den); `openssl x509 -in /opt/dsg-hrms/tls/fullchain.pem` | ✅ `CN=insankaynaklaritest.duzen.com.tr`, Let's Encrypt `YE2`, bitiş **16.12.2026 05:50:35 GMT**. Bu tarih KÖ-31 ile ve diskteki dosyayla aynı. Yenileme penceresi kabaca 16.11–13.12.2026 (R-18) |
| D-11 | E-posta sunucusu sertifikası kayıttaki tarihte (KÖ-43) | `openssl s_client -connect mail.duzen.com.tr:587 -starttls smtp` (UAT'den) | ✅ `CN=mail.duzen.com.tr`, Sectigo `DV R36`, bitiş **27.03.2027 23:59:59 GMT**. KÖ-43 (1.2) ile aynı |
| D-12 | Dış arayüzler sağlıklı (KÖ-40…42) | UAT'de `curl http://127.0.0.1:5299/health/{ready,notifications,sync}` | ✅ Üçü de `Healthy`: `postgresql`, `email`, `sms`, `personnel-sync` |
| D-13 | NTP eşitlemesi çalışıyor (KÖ-32) | `timedatectl`; `/etc/chrony/sources.d/dsg-hrms-ntp.sources` | ✅ `System clock synchronized: yes`, `NTP service: active`. Kaynaklar: `tr.pool.ntp.org` (en çok 3), `time.google.com` (en çok 1) |
| D-14 | Sır dosyasının yeri ve izni (KÖ-30) | `stat -c '%a %U %n'` | ✅ `600 root /opt/dsg-hrms/secrets/.env.uat`; `secrets/` klasörü `700`. TLS özel anahtarı `600`; sahibi UID 101 (web konteyneri kullanıcısı, sunucuda `uuidd` adına denk geliyor) |
| D-15 | Git kancaları geliştirme makinesinde etkin (KÖ-15) | `git config core.hooksPath` | ✅ `.githooks` (`commit-msg`, `pre-push`) |

### 2.3 Öğe listesinin kapsamı

| # | Madde | Yöntem | Sonuç |
|---|---|---|---|
| D-16 | Depodaki KÖ'ler (KÖ-01…17) listedeki yerde; listeden sonra eklenen öğe yok | Her KÖ yolu için varlık denetimi; `git diff --name-status 3f6833d..HEAD` (öğe listesinin oluşturulduğu `3f6833d`'den bu yana 19 commit, 101 dosya); `git ls-files` üst klasörleri | ⚠️ 05.10'dan bu yana eklenen ve değişen dosyaların hepsi KÖ-01…05 ve KÖ-17 kapsamında: kod, test, bir migration, belgeler, ADR-0016. Bu dönemde `.github/`, `docker/`, bağımlılık ve imaj dosyaları değişmedi. **Ancak:** (a) İki yol listede eksik yazılmış. KÖ-06'daki `src/shared/api/generated/` gerçekte `src/frontend/dsg-hrms-web/src/shared/api/generated/`; KÖ-07'deki `Directory.Packages.props` gerçekte `src/backend/Directory.Packages.props`. (b) Depoda izlenen ama hiçbir KÖ'de sayılmayan öğeler var. `assets/duzen_logo.png` UAT'ye dağıtılıyor. `docker/logo/read-only-login.sql` LOGO salt-okunur hesabının (KÖ-40) betiği. `.config/dotnet-tools.json` araç sürümlerini belirliyor; migration betiğini üreten `dotnet-ef` bunlardan biri. `.gitleaks.toml` CI'daki sır taramasının ayarı. Ayrıca `.gitattributes`, `.gitignore`, `docker/e2e/` (`up.sh`, `down.sh`, `seed.sql`) ve `docker/tls-empty/` sayılmıyor. Bulgu B-2 |
| D-17 | Depo dışı öğeler listede ve listedeki değişiklik yoluyla değişiyor (KÖ-30…52) | UAT: `/var/log/apt/history.log`, `systemctl is-enabled unattended-upgrades`, `/var/run/reboot-required`, `uptime -s`; MAN.6 bilgi kayıt defteri | ⚠️ (a) KÖ-33 "Docker Engine ve işletim sistemi — elle" diyor. Oysa `unattended-upgrades` **etkin** ve güvenlik güncellemelerini kendiliğinden kuruyor: 08.10 `sudo`, 10.10 `bluez`/`libxml2`/`libarchive`/`libpng`, 13.10 `libx11`. Docker Engine (29.8.0, Docker deposu) otomatik güncelleme kapsamı dışında; yalnızca elle güncelleniyor. (b) `/var/run/reboot-required` var. Sunucu 12.09.2026'dan beri açık; çalışan çekirdek `7.0.0-31-generic`, Ubuntu `26.04.1`. Yeniden başlatma kararı kayıtlı değil. (c) **Yerel geliştirme ortamı listede yok.** Bu ortam gerçek LOGO verisiyle, gerçek SMTP/NetGSM gönderimiyle ve gerçek sırlarla (`docker/.env`, depo dışı, `.gitignore`'da) çalışıyor (28.09.2026 kararı). Verisi MAN.6'da BK-15 olarak kayıtlı; yapılandırması ve sır dosyası bir KÖ değil. Bulgu B-3 |
| D-18 | Yerel geliştirme ortamı hangi commit'i çalıştırıyor? | `docker ps` (yerel); `git diff d009c25 149f6ee` | ⚠️ **Sınanamadı:** Docker Desktop denetim sırasında çalışmıyordu (`dockerDesktopLinuxEngine` kanalı yok). Denetim için başlatılmadı. Yerel imajlar 12.10.2026'da `d009c25`'ten derlenmişti. `d009c25..149f6ee` farkı yalnızca `CHANGELOG.md`, `README.md` ve `T3-kabul-plani.md`; `src/`, `docker/`, `global.json`, `assets/` ve `.config/` değişmedi. Yani yerel imajlar kod bakımından baseline ile aynı, **fark önemsiz**. Yerel ortam bir baseline değil, kabul kanıtı da üretmiyor; yine de "yerelde denenir, sonra UAT'ye gider" kuralı yüzünden hangi commit'te olduğu bilinmeli (Ö-5) |

### 2.4 Telafi kontrolleri ve önceki bulgular

| # | Madde | Yöntem | Sonuç |
|---|---|---|---|
| D-19 | Telafi kontrolleri işliyor (YAKLASIM §8 açık nokta 2; `dal-koruma-telafi-kontrolleri.md` §6 iş 3): 05.10'dan bu yana `main` örneklemi | `gh run list --workflow branch-protection-check.yml / ci.yml / main-failure-check.yml --branch main`; `git log 3f6833d^..HEAD`; `[DÜZELTİCİ]` ve `[HATA]` issue'ları | ❌ **Dal koruma denetimi** uygun: `main`'e gelen 20 commit'in (`3f6833d`…`d354e94`) hepsinde çalışmış ve hepsi `success`; `[DÜZELTİCİ]` açılması gereken bir olay yok. **`main` kırmızı denetimi uygunsuz.** 05.10.2026 20:19Z ile 06.10.2026 09:52Z arasında `main`'deki dört commit'te CI başarısız oldu: `2592fab`, `3f6833d`, `c6a9c86`, `90a25a9`. Hepsinde düşen iş "Konteyner taramasi (trivy) (api)" idi; trivy açık veritabanını indiremedi (`failed to download vulnerability DB`), yani kodla ilgili bir hata yoktu. `main-failure-check.yml` bu aralıkta altı kez tetiklendi (37370628312 … 37372670226). Altısında da iş **adım çalıştırmadan iptal oldu** (`cancelled`, 0 adım), bu yüzden `[HATA] main kırmızı: …` issue'su açılmadı. İptalin nedeni günlükten bulunamadı; günlükler artık yok (`BlobNotFound`). Kırmızı aralıkta birleşen #170 ve #172 belge PR'larıydı; CONTRIBUTING §1.1 yalnızca özellik birleştirmeyi yasaklar. `main` 06.10 09:52Z'de (`e8dfc87`) yeşile döndü. Kontrol "tespit edici, atlatılamaz" diye tanımlı; bu olayda ne tespit etti ne de iz bıraktı. Bulgu B-4 |
| D-20 | 05.10 denetiminin bulguları ve YAKLASIM §8 açık noktaları | Önceki rapor §4; `YAKLASIM.md` §8; sunucu ve depo | ⚠️ **Kapandı:** G-0 Webmin. 10000 portu dinlemiyor, `webmin` paketi kurulu değil (D-09). G-1 bu denetimle yapıldı; `v0.2.0` aday ile aynı commit (D-02). §8-2 D-19 ile ilk kez yapıldı. §8-3 (D-11/D-12) 06.10'da kapandı, bugün de uygun (D-13, D-14). **Açık:** G-2 / §8-6, yani D-04/D-08 denetimi hâlâ elle; CI'da karşılığı yok. §8-4: `docs/00-DOKUMAN-HARITASI.md` satır 198 hâlâ "GitHub Releases" diyor. **Belge güncelliği:** `YAKLASIM.md` §2.3 madde 4 ("`v0.2.0` T3 kabulünü bekliyor") ve §2.4 ("10'u uygun, 2'si sınanamadı") güncel değil. Önceki raporun §4 başlığının altında başıboş bir `(m) => m` satırı kalmış. Öneri Ö-7, Ö-8 |

---

## 3. Bulgular

| # | Bulgu | Önem | Madde |
|---|---|---|---|
| B-1 | UAT'de belgelenmemiş iki servis var: `avahi-daemon` (UDP 5353, tüm arayüzlerde) ve `cupsd` (TCP 631, yalnızca yerel). Sunucu masaüstü kurulumu; bu servislerin bir görevi yok | Orta. Avahi yerel ağa açık ve gereksiz bir saldırı yüzeyi. CUPS yalnızca yerel | D-09 |
| B-2 | Öğe listesinde iki yol eksik yazılmış (KÖ-06, KÖ-07). Depoda izlenen yedi öğe hiçbir KÖ'de sayılmıyor; bunlardan biri UAT'ye dağıtılıyor (`assets/`), biri LOGO hesabının betiği (`docker/logo/`) | Düşük. Öğeler zaten PR yoluyla değişiyor; eksik olan sayım ve izlenebilirlik | D-16 |
| B-3 | Depo dışı üç durum listede yok veya listeyle çelişiyor: işletim sistemi kendiliğinden güncelleniyor (KÖ-33 "elle" diyor); bekleyen bir yeniden başlatma var; yerel geliştirme ortamı gerçek sırlarla çalıştığı hâlde bir KÖ değil | Orta. Otomatik güncellemeler UAT'yi kayıtsız değiştiriyor; yerel `.env` gerçek sırları tutuyor | D-17 |
| B-4 | `main-failure-check.yml` 05.10'daki kırmızı dönemde altı kez iptal oldu ve issue açmadı. #129'un kapatmak istediği "kırmızı dönem kayda geçmiyor" açığı bu olayda yeniden yaşandı | Orta. Kontrolün kendisi denetlenmiyor; bir sonraki kırmızı dönem de sessiz geçebilir | D-19 |

---

## 4. Önerilen düzeltmeler

Bu denetim hiçbir öğeyi değiştirmedi. Öneriler ilgili issue ve PR'larla uygulanır.

| # | Öneri | Bulgu | Sahibi |
|---|---|---|---|
| Ö-1 | UAT'de `avahi-daemon` ve `cups` servisleri durdurulup devre dışı bırakılır: `systemctl disable --now avahi-daemon.socket avahi-daemon.service cups.socket cups.path cups.service cups-browsed.service`. Yeniden açılmamaları için `mask` de değerlendirilir. Ardından `ss -ltnup` ile doğrulanır ve sonuç runbook §11'e yazılır. Daha kalıcı seçenek: `ubuntu-desktop` ve grafik hedef (`systemctl set-default multi-user.target`) ayrı bir değişiklik olarak ele alınır | B-1 | Bilgi İşlem |
| Ö-2 | Runbook §11 güvenlik tablosuna **UDP** dinleyicileri de yazılır. Port denetimi bundan sonra `ss -ltnup` ile yapılır (TCP ve UDP) | B-1 | Bilgi İşlem |
| Ö-3 | `konfigurasyon-ogeleri.md` §2 ve §3.1 güncellenir: yollar düzeltilir, eksik öğeler eklenir (aşağıdaki öneri metni). KÖ-22'ye `global.json`, `assets/` ve `sema.sql` eklenir | B-2 | Bilgi İşlem |
| Ö-4 | KÖ-33'ün değişiklik yolu gerçeğe uydurulur: "Güvenlik güncellemeleri `unattended-upgrades` ile kendiliğinden (`/var/log/apt/history.log`); Docker Engine ve sürüm yükseltmeleri elle". Bekleyen yeniden başlatma bir bakım penceresinde yapılır. Zamanı İK'ya duyurulur; bir kabul oturumuyla çakışmamalı. Ardından `uname -r` ve konteyner sağlığı kayda geçer. Her konfigürasyon denetimine `apt` geçmişi ve `reboot-required` maddesi eklenir | B-3 | Bilgi İşlem |
| Ö-5 | Yerel geliştirme ortamı yeni bir KÖ olarak eklenir (öneri metninde KÖ-36). Sır dosyası `docker/.env` depo dışında, Bilgi İşlem'de; değişken listesi KÖ-12 ile aynı. Ortamın hangi commit'ten derlendiği her güncellemede not edilir: imaj `version` etiketi ya da `git describe` | B-3 | Bilgi İşlem |
| Ö-6 | KÖ-30 notu düzeltilir: "değişken listesi KÖ-12'nin alt kümesidir; varsayılanı olan isteğe bağlı değişkenler (örn. `IDENTITY_PUBLIC_BASE_URL`) yazılmayabilir" | D-08 | Bilgi İşlem |
| Ö-7 | `main-failure-check.yml` incelenir: işler neden iptal oldu (Actions kesintisi, çalıştırıcı kotası, eşzamanlılık)? Kontrolün kendisi için ikinci bir güvence eklenir. Örneğin `main-failure-check` başarısız veya iptal olursa `branch-protection-check`'teki gibi 3 deneme yapılır, olmazsa "denetim yapılamadı" issue'su açılır (#129 düzeni). Bu iyileştirme için bir `[DÜZELTİCİ]` issue'su açılır. 05.10 kırmızı dönemi TEC.13 olay kaydına geriye dönük yazılır. Her konfigürasyon denetimine sabit madde olarak "telafi kontrolleri örneklemi" (bu rapordaki D-19) eklenir | B-4 | Bilgi İşlem |
| Ö-8 | Belge güncellikleri tek PR'da düzeltilir: `YAKLASIM.md` §2.3 madde 4 ve §2.4 güncellenir, §8 tablosunda 2 ve 3 kapatılır. `00-DOKUMAN-HARITASI.md` satır 198 "`CHANGELOG.md` + açıklamalı Git etiketi" olur. 05.10 raporundaki `(m) => m` satırı silinir; raporun 1.3 sürümü olarak, değişmezlik kuralına uygun biçimde | D-20 | Bilgi İşlem |
| Ö-9 | G-2'ye bağlı olarak D-04/D-08 (örnek ortam ↔ compose) denetimi küçük bir CI betiğine (`.github/scripts/`) alınır | D-20 | Bilgi İşlem |

---

## 5. Gözlemler

- **G-3:** Etiket açıklamaları ASCII yazılmış ("Yonetimi", "kabul adayi"). CHANGELOG başlığıyla aynı değiller; `YAKLASIM.md` §5 "açıklama CHANGELOG başlığıyla aynı" diyor. İçerik tutarlı ve her açıklama bölümüne adıyla atıf yapıyor. Etiketler taşınmayacağı için (`KR-097`) bu durum düzeltilmez. YAKLASIM §5'teki ifade "açıklama CHANGELOG bölümüne atıf yapar" olarak yumuşatılabilir.
- **G-4:** `v0.2.0`, kabul edilen adayla aynı commit'i (`149f6ee`) gösterir. Bu nedenle o commit'teki `CHANGELOG.md`'de `[0.2.0]` bölümü yoktur; bölüm bir sonraki commit'te (`d354e94`) yazıldı. Bu durum `KR-097`'nin "kabulden sonra aynı commit etiketlenir" kuralının doğal sonucu, bir uygunsuzluk değil. YAKLASIM §2.3'te açıkça yazılması önerilir.
- **G-5:** UAT'de çalışan imajların OCI `version` etiketi `v0.2.0-rc.5`; sunucuda `v0.2.0` adlı bir imaj etiketi yok. Bu durum doğru: dağıtım adayın etiketiyle yapıldı, kabul aynı commit'i onayladı. İleride yalnızca etiket adı için yeniden dağıtım **yapılmamalı**. Gerekirse `docker tag` ile `:v0.2.0` adı eklenebilir; bu, KÖ-20'nin "yalnızca `deploy-uat.sh`" kuralına göre ayrıca kararlaştırılmalı.
- **G-6:** Sunucuda beş adayın imajları duruyor (`rc.1`…`rc.5`). Geri dönüş için yararlı; disk dolmadıkça silinmesi gerekmez. Silme kuralı runbook'a yazılabilir.

---

## 6. Bir sonraki denetimde

Aşağıdaki komutlar hiçbir şeyi değiştirmez ve ileti göndermez:

```bash
ss -ltnup                                            # TCP ve UDP
grep -E "^(Start-Date|Upgrade|Install)" /var/log/apt/history.log | tail -20
ls /var/run/reboot-required 2>/dev/null; uptime -s; uname -r
docker inspect dsg-hrms-uat-api --format '{{index .Config.Labels "org.opencontainers.image.revision"}}'
```

Yerel ortam için Docker Desktop açıkken `docker ps` ve imaj etiketleri (D-18) yeniden sınanır.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-13 | 1.0 | T3 modül sonu konfigürasyon denetimi (`v0.2.0`) | Bilgi İşlem |
