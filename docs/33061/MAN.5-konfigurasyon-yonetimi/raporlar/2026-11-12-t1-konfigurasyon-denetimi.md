# T1 Geliştirme Sonu Konfigürasyon Denetimi

**Belge kimliği:** MAN.5-KD-2026-11-12
**Süreç:** MAN.5 — Konfigürasyon Yönetimi (`MAN.5.BP5`)
**Denetlenen yapı:** UAT'de çalışan `v0.2.0-89-g6dbe9d5` (commit `6dbe9d5`, 09.11.2026 kurulumu); son baseline `v0.2.0` (`149f6ee`); `main` @ `388da21`
**Ölçüt:** `konfigurasyon-ogeleri.md` 1.4, `YAKLASIM.md` (14.10.2026), `KR-110`, `TEC.10-gecis/surum-kontrol-listesi.md` 1.0
**Son güncelleme:** 2026-11-12
**Tarih:** 2026-11-12 (sunucu ölçümleri 20:15–20:35 UTC) · **Denetleyen:** Bilgi İşlem · **Kaynak:** T1 geliştirme sonu süreç denetimi (#385); önceki denetim `2026-10-13-t3-konfigurasyon-denetimi.md`

> Denetim şu soruyu cevaplar: **UAT'de çalışan yapı, kayıtlarda yazan yapı mı?**
> T1 döneminde yeni etiket yok; UAT'ye iki etiketsiz geliştirme yapısı kuruldu (21.10, 09.11).
> Sunucuda yalnızca okuma komutları çalıştırıldı. Sır dosyalarında yalnızca **değişken adları**
> listelendi (`grep -oE '^[A-Z0-9_]+='`); hiçbir dosyanın içeriği açılmadı. LOGO'ya bağlanılmadı.

---

## 1. Sonuç

| | Sayı |
|---|---|
| Denetim maddesi | 20 |
| Uygun | **9** |
| Kısmen uygun (gözlem/öneri var) | 7 — D-02, D-11, D-14, D-16, D-17, D-18, D-20 |
| Uygunsuzluk | **4** — D-07, D-08, D-09, D-15 |

UAT'de çalışan imajlar, dağıtım kaydı, `VERSION` ve veritabanı şeması **birbiriyle tutarlı**.
UAT `6dbe9d5`'i çalıştırıyor; şemadaki 35 migration depodakiyle aynı. Dışarıya açık portlar
yalnızca TCP 22, 80, 443.

Sapmalar sunucudaki **elle yapılan işlerde** ve **öğe listesinin kapsamında**:

- `secrets/` altında 03.11.2026 tarihli, kayıt dışı yeni bir sır kopyası var (D-09). 13.10'da silinen kopyanın (#207) aynı türü.
- Örnek ortam dosyaları (KÖ-12) yığının zorunlu değişkenlerini içermiyor (D-07).
- 10.11 sentetik yük yığınından sahipsiz bir PostgreSQL birimi kaldı; #381 raporu §6 "silindi" diyor (D-15).

---

## 2. Maddeler

### 2.1 Baseline ve sürüm kayıtları

| # | Madde | Yöntem | Sonuç |
|---|---|---|---|
| D-01 | Etiketler ve CHANGELOG (`KR-097`) | `git tag -l --format`, `CHANGELOG.md` | ✅ 7 açıklamalı etiket, dönemde yeni etiket yok; `v0.2.0` → `149f6ee` değişmedi. T1 değişiklikleri `[Yayımlanmamış]` bölümünde (Güvenlik 7, Eklendi 29, Değişti 8, Düzeltildi 7 madde). T1 kabul adayı henüz yok; etiket gerekmiyor |
| D-02 | Dağıtım kaydı depo ↔ sunucu (KÖ-21) | `deployments.log` ↔ `TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md` | ⚠️ **7/7 satır eşleşiyor** (zaman, sürüm, tam commit, dağıtan). Yeni satırlar: `2026-10-21T11:33:58Z v0.2.0-27-ge488898` ve `2026-11-09T21:26:10Z v0.2.0-89-g6dbe9d5`. **Gözlem:** depodaki son iki satır `v0.2.0` notunun (alıntı bloğu) altına yazıldığı için Markdown tablosunun dışında kalıyor; tablo olarak görünmüyor (G-1) |
| D-03 | `VERSION` dosyası | `cat /opt/dsg-hrms/VERSION` | ✅ `v0.2.0-89-g6dbe9d5 6dbe9d50…`, kayıtla aynı |

### 2.2 UAT'de çalışan yapı (KÖ-20…KÖ-22, KÖ-30…KÖ-33)

| # | Madde | Yöntem | Sonuç |
|---|---|---|---|
| D-04 | Çalışan imajların commit etiketi (KÖ-20) | `docker inspect … org.opencontainers.image.revision` | ✅ `dsg-hrms-uat-api` ve `-web`: `6dbe9d50…`, sürüm `v0.2.0-89-g6dbe9d5`; ikisi de `healthy`, 2 gündür açık. `:uat` ile `:v0.2.0-89-g6dbe9d5` aynı imaj (API `2aa0e892fd65`, web `d033e58c3acd`). Eski aday imajları (`rc.1`…`rc.5`, `e488898`) duruyor (geri dönüş için) |
| D-05 | Veritabanı şeması (KÖ-03) | `__ef_migrations_history` sayısı ve son kimlik ↔ `git ls-tree 6dbe9d5 …/Migrations` | ✅ 35/35; son migration iki tarafta `20261108164933_SystemEncryptionPermission`. Yalnızca sayı ve kimlik okundu |
| D-06 | Çalışan commit ile `main` farkı | `git diff --stat 6dbe9d5 388da21` | ✅ Fark yalnızca belgeler ve `docker/uat-ik-rolu-ata.sh` (yerelden SSH ile çalışan araç). `src/` değişmedi |
| D-07 | Örnek ortam dosyaları yığınları karşılıyor (KÖ-12) | `compose*.yml` `${…}` ↔ `.env*.example` | ❌ **`.env.uat.example`'da yok:** `HRMS_APP_DB_PASSWORD` (compose'da `:?` ile **zorunlu**, #258, 21.10), `HRMS_APP_DB_USER`, `PERSONAL_DATA_KEY_K1`, `K2`, `K3` (#318, #376). **`.env.example`'da yok:** `HRMS_APP_DB_PASSWORD`, `HRMS_APP_DB_USER`. 13.10'da iki yönde fark yoktu (D-08). Yeni kurulum örnek dosyadan yapılırsa yığın açılmaz. Değişkenler runbook §2.2.1 ve §5.1'de yazılı; öğe listesi kuralı (KÖ-12 "her değişkeni içermeli") bozuldu. Bulgu B-2 |
| D-08 | Sır dosyasının değişken adları (KÖ-30) | `.env.uat` adları ↔ `.env.uat.example` | ❌ (D-07'nin sonucu) Sır dosyasında 23 değişken var. Örnekte olmayan iki değişken var: `HRMS_APP_DB_PASSWORD`, `PERSONAL_DATA_KEY_K1`. KÖ-30'daki "örneğin alt kümesidir" ifadesi artık doğru değil. `IDENTITY_PUBLIC_BASE_URL` 13.10'daki gibi isteğe bağlı |
| D-09 | `secrets/` dizininde yalnızca `.env.uat` (KÖ-30, D-12 genişletmesi #207, A6) | `ls -la /opt/dsg-hrms/secrets` | ❌ **`.env.uat.bak-20261103`** (`600 root`, değiştirilme 21.10.2026 14:01, dizin 03.11.2026 17:55). Değişken adları: güncel dosyanın 23 adından `PERSONAL_DATA_KEY_K1` dışındaki 22'si. Yani `HRMS_APP_DB_PASSWORD` dahil bugün geçerli sırların bir kopyası olabilir; değerler karşılaştırılmadı. Kopya, kan grubu anahtarının eklendiği gün (03.11, #318) bırakılmış görünüyor. 13.10'da #207 ile aynı türden bir kopya silinmişti; runbook §2.2.1 ve §5.1 "yedek kopya bırakılmaz" diyor. Ne bilgi kayıt defterinde ne öğe listesinde. Bulgu B-1 |
| D-10 | Dinleyen portlar (runbook §6, KÖ-33) | `ss -ltnup` | ✅ Dışarıya açık: TCP 22 (`sshd`), 80 ve 443 (`docker-proxy` → web). Yalnızca yerel: 5299 (API), 5434 (PostgreSQL), 53 (`systemd-resolved`), 323/udp (`chronyd`). **Dışarıya açık UDP yok.** Avahi ve CUPS `masked` (#207). Runbook §6 tablosuyla birebir |
| D-11 | UAT TLS sertifikası (KÖ-31) | `openssl s_client` (UAT'den, 127.0.0.1:443, SNI) | ⚠️ `CN=insankaynaklaritest.duzen.com.tr`, bitiş **16.12.2026 05:50:35 GMT**; KÖ-31 ile aynı. **Yenileme penceresi 16.11'de açılıyor** (4 gün sonra); yenileme elle (R-18, puan 9). Yenileme için takvimde iş/issue görülmedi |
| D-12 | Sağlık uçları (KÖ-40…42) | `curl 127.0.0.1:5299/health/{ready,notifications,sync}` | ✅ Üçü de `200` |
| D-13 | NTP (KÖ-32) | `timedatectl` | ✅ `System clock synchronized: yes` |
| D-14 | İşletim sistemi güncellemeleri ve yeniden başlatma (KÖ-33) | `/var/log/apt/history.log`, `/var/run/reboot-required.pkgs`, `uname -r`, `uptime -s` | ⚠️ `unattended-upgrades` çalışıyor (03.11, 05.11, 07.11, 10.11, 12.11). **Yeniden başlatma 03.11'den beri bekliyor:** çekirdek `7.0.0-42` 03.11'de, `-43` 12.11'de kuruldu; çalışan `7.0.0-38`. Son yeniden başlatma 13.10 (#207). 09.11 kurulumu bir bakım penceresiydi; yeniden başlatma yapılmadı. KÖ-33 "duyurulmuş bir zamanda yapılır" diyor; zaman kayıtlı değil. Bulgu B-4 |
| D-15 | 10.11 sentetik yük yığınının kalıntısı (#381 raporu §6) | `docker ps -a`, `docker images`, `docker network ls`, `docker volume ls`, `docker volume inspect` | ❌ Kapsayıcı, ağ ve geçici imajlar (`node:24-alpine`, `axllent/mailpit`, `mssql-tools`) **yok** ✅. Ancak **sahipsiz anonim bir birim** var: `e7b63157…`, oluşturulma **10.11.2026 00:29:47 (+03)**, 135 MB, içinde PostgreSQL veri dizini; hiçbir kapsayıcıya bağlı değil. Zaman, sentetik yığının kuruluşuyla örtüşüyor (UAT kurulumu 00:26, ölçüm 00:34'te başladı). Rapor §6 "veritabanı birimi … silindi" diyor. Muhtemel neden: `docker compose down` `-v` olmadan çalıştı ve `postgres` imajının anonim birimi kaldı (aynı örüntü yerelde `docker/e2e/down.sh`'ta da var, D-18). İçerik açılmadı; rapora göre veri sentetik (sentetik hesaplar, yığının kendi anahtarıyla şifreli kan grubu). Bulgu B-3 |

### 2.3 Öğe listesinin kapsamı

| # | Madde | Yöntem | Sonuç |
|---|---|---|---|
| D-16 | Depo dışı adlar ↔ öğe listesi ve bilgi kayıt defteri | `ls -la /opt`, `/opt/dsg-hrms`, `/opt/dsg-hrms/{secrets,tls,docker}` (yalnızca adlar) | ⚠️ `/opt/dsg-hrms`: `VERSION`, `deployments.log` (KÖ-21, BK-14), `assets/`, `docker/`, `src/`, `global.json`, `sema.sql` (KÖ-22, BK-18), `secrets/` (KÖ-30, BK-12), `tls/` (KÖ-31, BK-12) ✅. `yedek-*.sql.gz` yok ✅ (#381'deki kopya silinmiş, BK-13). **Kayıt dışı:** `secrets/.env.uat.bak-20261103` (D-09); `/opt/acme/` (`certbot.log`, `expected-record.txt`, `hook.sh`) runbook'ta anılıyor ama KÖ-31'de ve bilgi kayıt defterinde yok; `/opt/containerd` (Docker'ın kendi dizini, öğe değil) |
| D-17 | Depodaki yeni öğeler listede (KÖ-01…17) | `git diff --name-status d354e94..388da21` (src/ ve docs/ dışı) | ⚠️ Yeni dosyalar: `docker/postgres/app-role.sql`, `docker/postgres/apply-app-role.sh` (çalışma rolü, `KR-118`), `docker/uat-ik-rolu-ata.sh` (UAT'de rol ataması, #383), `.github/scripts/*` ve `main-health-sweep.yml` (KÖ-14 kapsamında ✅). `docker/postgres/` KÖ-11 ve KÖ-13'te sayılmıyor. **Depo dışı yeni öğeler listede yok:** veritabanı çalışma rolü `hrms_app` ve yetkileri; özel nitelikli veri anahtar yuvaları (`PERSONAL_DATA_KEY_K1…K3`, KÖ-30'da anılmıyor). KÖ-52'nin değişiklik yolu ("T4'e kadar migration ve ilk yönetici") artık `uat-ik-rolu-ata.sh`'ı da kapsamalı. Öğe listesi 14.10'dan beri değişmedi. Bulgu B-5 |
| D-18 | Yerel geliştirme ortamı (KÖ-36) | yerel `docker ps`, `docker inspect`, `docker volume ls` | ⚠️ Yığın çalışıyor (3 kapsayıcı `healthy`, imajlar 08.11.2026 19:53 UTC). **İmajlarda commit etiketi yok** (`revision` boş); KÖ-36 "hangi commit'ten derlendiği not edilir" diyor, not bulunamadı. Derleme zamanı `6d587f3` (#378) birleşmesinden sonrasına denk geliyor. Ayrıca yerelde **66 sahipsiz anonim birim** var (11.09–08.11; 18'i 08.11). `docker/e2e/down.sh` `down --remove-orphans` çalıştırıyor, `-v` yok; e2e PostgreSQL'in anonim birimi her çalıştırmada kalıyor. İçerik açılmadı (e2e verisi sentetik) |

### 2.4 Telafi kontrolleri ve önceki bulgular

| # | Madde | Yöntem | Sonuç |
|---|---|---|---|
| D-19 | Telafi kontrolleri örneklemi (`d354e94..388da21`, 89 commit) | `gh run list` (branch-protection-check, ci, main-failure-check, main-health-sweep) | ✅ 89 commit'in 89'unda Dal Koruma Denetimi `success`. `main-health-sweep` 14.10'dan beri 93 kez çalıştı, hepsi `success`. `main` kırmızıya düşen altı commit'in (`39d38f9`, `f7560bd`, `ba3cf4d`, `66ee13f`, `5e0203c`, `d8db501`) hepsinde kayıt otomatik açıldı (#223, #242, #255, #286, #305; #248 "doğrulanmadı"). 20.10'daki yanlış alarm (#252, PR ilişkisi gecikmesi) kayda geçti ve #254 ile düzeltildi |
| D-20 | 13.10 denetiminin bulguları | Önceki rapor §3; sunucu ve depo | ⚠️ **B-1** (Avahi/CUPS) ✅ kapalı, D-10. **B-2** (öğe listesi) ✅ 1.4 ile kapandı; T1'de yeni boşluk (D-17). **B-3** ⚠️ kısmen: yeniden başlatma 13.10'da yapıldı, 03.11'den beri yine bekliyor (D-14); KÖ-36 eklendi ama kuralı uygulanmıyor (D-18). **B-4** ✅ #208 ile kapandı, D-19 |

---

## 3. Bulgular

| # | Bulgu | Önem | Madde |
|---|---|---|---|
| B-1 | UAT'de kayıt dışı sır kopyası `secrets/.env.uat.bak-20261103`; bugünkü sırların çoğunu içerebilir. #207'nin kapattığı açığın tekrarı. Sunucuda elle yapılan sır değişikliği bir kayıt PR'ı ve kontrol adımı (A6, D-12) üretmedi | **Yüksek** | D-09, D-16 |
| B-2 | Örnek ortam dosyaları yığının zorunlu `HRMS_APP_DB_PASSWORD` değişkenini ve `PERSONAL_DATA_KEY_*` yuvalarını içermiyor; D-08 karşılaştırması CI'da değil (13.10 Ö-9) | Orta | D-07, D-08 |
| B-3 | Sentetik yük yığınından sahipsiz PostgreSQL birimi kaldı; ölçüm raporu "silindi" diyor. Kök neden `down`'un `-v`'siz çalışması; yerelde de 66 sahipsiz birim | Orta | D-15, D-18 |
| B-4 | Çekirdek güncellemesi sonrası yeniden başlatma 03.11'den beri bekliyor; zaman kayıtlı değil | Düşük | D-14 |
| B-5 | Öğe listesi T1'de güncellenmedi: `docker/postgres/`, `uat-ik-rolu-ata.sh`, `hrms_app` rolü, anahtar yuvaları, `/opt/acme/`; yerel imajlarda commit etiketi yok | Düşük | D-16, D-17, D-18 |

---

## 4. Önerilen düzeltmeler

Bu denetim hiçbir öğeyi değiştirmedi. Öneriler ilgili issue ve PR'larla uygulanır.

| # | Öneri | Bulgu | Sahibi |
|---|---|---|---|
| Ö-1 | `.env.uat.bak-20261103` güncel dosyayla **değişken adı ve değer eşitliği** düzeyinde, değer yazdırmadan karşılaştırılır (#207'deki yöntem). Kopyada bugün geçerli olmayan ama başka yerde geçerli bir sır varsa döndürülür; dosya silinir ve silme issue'ya yazılır | B-1 | Bilgi İşlem |
| Ö-2 | `deploy-uat.sh` doğrulama adımına "`secrets/` dizininde `.env.uat`'tan başka dosya yok" denetimi eklenir; varsa kurulum durur. Böylece A6'nın bu maddesi her kurulumda, etiketli olsun olmasın, kendiliğinden çalışır | B-1 | Bilgi İşlem |
| Ö-3 | Sır dosyasını değiştiren her elle iş için kısa kayıt (issue yorumu): hangi değişken eklendi, yedek bırakılmadığı (`ls secrets/`). Runbook §2.2.1 ve §5.1'e "işlem sonunda `ls -la secrets/` çıktısı yalnızca `.env.uat` göstermeli" adımı | B-1 | Bilgi İşlem |
| Ö-4 | `.env.uat.example` ve `.env.example`'a eksik değişkenler açıklamalarıyla eklenir. 13.10 Ö-9 uygulanır: compose `${…}` ↔ örnek dosya karşılaştırması `.github/scripts/` altında CI denetimi olur | B-2 | Bilgi İşlem |
| Ö-5 | UAT'deki sahipsiz birim (`e7b63157…`) silinir; silme kayda geçer. #381 raporu §6 düzeltilir (rapora 0.3 satırı). Sentetik yığın ve `docker/e2e/down.sh` `down -v` ile kapatılır; yerel sahipsiz birimler temizlenir | B-3 | Bilgi İşlem |
| Ö-6 | Yeniden başlatma için zaman belirlenir ve İK'ya duyurulur (T1 kabul oturumlarıyla çakışmamalı); sonrasında `uname -r` ve sağlık uçları kayda geçer | B-4 | Bilgi İşlem |
| Ö-7 | Öğe listesi 1.5: `docker/postgres/` KÖ-13'e; `uat-ik-rolu-ata.sh` KÖ-13 ve KÖ-52'ye; `hrms_app` rolü yeni öğe (KÖ-37 önerisi; yetkileri `ApplicationRoleTests` ile sınanır); KÖ-30'a anahtar yuvaları ve "alt küme" notunun düzeltilmesi; KÖ-31'e `/opt/acme/` | B-5 | Bilgi İşlem |
| Ö-8 | Yerel derlemede imajlara `org.opencontainers.image.revision` etiketi verilir (`compose.yml` `build.labels` veya `--build-arg`); KÖ-36 kuralı böylece kendiliğinden kanıtlanır | B-5 | Bilgi İşlem |
| Ö-9 | Depodaki dağıtım kaydında `v0.2.0` notu tablonun altına taşınır; tablo tek parça kalır (G-1) | D-02 | Bilgi İşlem |
| Ö-10 | TLS yenilemesi (R-18) için 16.11–13.12 penceresinde bir iş açılır; DNS yöneticisiyle tarih belirlenir | D-11 | Bilgi İşlem |

---

## 5. Gözlemler

- **G-1:** Dağıtım kaydı tablosu, `v0.2.0` notu (alıntı bloğu) yüzünden ikiye bölündü; 21.10 ve 09.11 satırları tablo olarak görünmüyor. İçerik doğru, biçim bozuk.
- **G-2:** UAT 21.10'dan beri etiketsiz geliştirme yapıları çalıştırıyor (`v0.2.0-27-…`, `v0.2.0-89-…`). `deploy-uat.sh` bunlara da OCI etiketi ve kayıt satırı yazdığı için izlenebilirlik korunuyor. Ancak `KR-097`, `KR-110` ve sürüm kontrol listesi yalnızca `rc.N` adaylarını tanımlıyor; etiketsiz kurulumun kim tarafından, hangi kontrolle yapılacağı yazılı değil (Ek A, A-03).
- **G-3:** 09.11 kurulumunu "Claude (Doğuş Uçanok onayıyla)" yaptı; onay kaydı #381'de. Kurulum öncesi alınan veritabanı kopyası (`yedek-2026-11-10.sql.gz`) silinmiş; BK-13 kuralı işledi.
- **G-4:** Sunucuda yedi eski yapının imajları duruyor (`rc.1`…`rc.5`, `e488898`). Disk %50 dolu; silme kuralı hâlâ yazılı değil (13.10 G-6).

---

## 6. Bir sonraki denetimde

Aşağıdaki komutlar hiçbir şeyi değiştirmez:

```bash
ls -la /opt/dsg-hrms/secrets                       # yalnızca .env.uat beklenir
docker volume ls -f dangling=true                  # sahipsiz birim beklenmez
cat /var/run/reboot-required.pkgs 2>/dev/null; uname -r; uptime -s
diff <(grep -oE '^[A-Z0-9_]+=' /opt/dsg-hrms/secrets/.env.uat | sort) \
     <(grep -oE '^[A-Z0-9_]+=' /opt/dsg-hrms/docker/.env.uat.example | sort)
```

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-11-12 | 1.0 | İlk sürüm (#385) | Bilgi İşlem |
