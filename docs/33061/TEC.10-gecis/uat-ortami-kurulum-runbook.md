# UAT Ortamı — Kurulum ve Dağıtım Runbook'u

**Belge kimliği:** TEC.10-RB-001
**Son güncelleme:** 2026-10-04
**İlgili süreçler:** TEC.10 (Geçiş), MAN.5 (Konfigürasyon Yönetimi)
**İlgili kararlar:** `KR-024`, `KR-038`, `KR-065`, `KR-097`

> Elle yapılan bir kurulum, ikinci kez yapıldığında farklı çıkar. Bu belge kurulumun
> **tekrarlanabilir** olmasını sağlar; ayrıca kurulumu yapan kişi dışındakilerin de
> ortamı anlamasına ve devralmasına imkân verir (`R-05`).

---

## 1. Ortam bilgileri

| | |
|---|---|
| **Sunucu** | `192.168.3.202` — Ubuntu 26.04 LTS · 2 çekirdek · 7,3 GB RAM · 87 GB disk |
| **Adres** | **https://insankaynaklaritest.duzen.com.tr** — HTTP, HTTPS'e yönlendirilir |
| **Uygulama dizini** | `/opt/dsg-hrms` (sırlar: `/opt/dsg-hrms/secrets/`, sertifika: `/opt/dsg-hrms/tls/`) |
| **Erişim** | SSH, parola ile (kalıcı anahtar tanımlı değil; parola girişinin kapatılması açık iş, R-25) |
| **Amaç** | İK kabul testi (`KR-024`). **Gerçek personel verisiyle** çalışır: LOGO'dan okunur, maskelenmez; kabul testleri gerçek personelle yapılır. İleti gönderimi izin listesiyle sınırlıdır (§10.2). Risk ve önlemler: R-25 |
| **Sertifika** | Let's Encrypt · **bitiş 16.12.2026** · yenileme **elle** (`KR-067`, `R-18`) |

### Yayımlanan portlar

| Port | Erişim | Ne |
|---|---|---|
| `443` | **Ağa açık** | Web arayüzü — **HTTPS** (nginx) |
| `80` | **Ağa açık** | Yalnızca HTTPS'e yönlendirme (301) |
| `5299` | Yalnızca `127.0.0.1` | API — web konteyneri Docker ağı üzerinden erişir |
| `5434` | Yalnızca `127.0.0.1` | PostgreSQL |

> API ve veritabanı **bilinçli olarak dışarı açılmaz.** Web konteyneri API'ye Docker
> ağı üzerinden ulaşır; dışarıya açmak gereksiz saldırı yüzeyidir. Hata ayıklamak
> gerekirse SSH tüneli kullanılır:
> `ssh -L 5299:127.0.0.1:5299 root@192.168.3.202`

---

## 2. İlk kurulum (bir kez)

### 2.1 Docker Engine

```bash
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y ca-certificates curl gnupg
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc
KOD=$(. /etc/os-release && echo "$VERSION_CODENAME")
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] \
  https://download.docker.com/linux/ubuntu $KOD stable" > /etc/apt/sources.list.d/docker.list
apt-get update -qq
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl is-enabled docker   # "enabled" dönmeli: sunucu yeniden başlarsa yığın da kalkar
```

### 2.2 Ortam dosyası

**Parola sunucuda üretilir; hiçbir yere yazılmaz, depoya girmez** (`KR-038`).

Dosya **kaynak ağacının dışında**, `/opt/dsg-hrms/secrets/` altında durur.

> **Neden dışarıda?** Dağıtım betiği `/opt/dsg-hrms/docker` dizinini **silip yeniden
> oluşturur**. Dosya orada dururken her dağıtım, ihtiyaç duyduğu sırrı kendi eliyle
> siliyordu ve yığın `couldn't find env file` ile ayağa kalkmıyordu. Bu 17.09.2026'da
> yaşandı; parola çalışan konteynerin ortamından kurtarılabildi. **Bir sır, dağıtımın
> sildiği ağacın içinde yaşamamalıdır.**

```bash
mkdir -p /opt/dsg-hrms/secrets && chmod 700 /opt/dsg-hrms/secrets
PAROLA=$(openssl rand -base64 30 | tr -d "/+=" | head -c 32)

cat > /opt/dsg-hrms/secrets/.env.uat <<SON
POSTGRES_DB=dsg_hrms_uat
POSTGRES_USER=postgres
POSTGRES_PASSWORD=$PAROLA
POSTGRES_PORT=5434
API_PORT=5299
WEB_PORT=80
API_IMAGE=dsg-hrms-api:uat
WEB_IMAGE=dsg-hrms-web:uat
TLS_DIR=/opt/dsg-hrms/tls
WEB_TLS_PORT=443
SON

chmod 600 /opt/dsg-hrms/secrets/.env.uat
```

> **Port değerlerine `127.0.0.1:` öneki YAZILMAZ.** Önek artık `compose.uat.yml`
> içinde sabittir. Önceden güvenli bağlama, elle yazılan bu dosyanın biçimine
> bağlıydı; bir kez sade port yazıldığında API tüm ağa açıldı (17.09.2026 —
> dağıtım betiğinin kendi kontrolü yakaladı). Bir güvenlik özelliği, yazım hatasına
> dayanıklı olmalıdır.

---

## 3. Dağıtım (her sürümde)

Dağıtım `./docker/deploy-uat.sh` ile yapılır; betik aşağıdaki adımların hepsini sırasıyla
uygular. Adımlar, betiğin ne yaptığını anlamak ve gerektiğinde elle uygulamak içindir.

### 3.0 Sürümü seç

```bash
# Kabul oturumu için: etiketli sürüm (KR-097)
git checkout v0.2.0-rc.1
# Ara dağıtım için: main'in son hâli
git checkout main && git pull
```

Betik iki koşul arar:
- **Kod commit edilmiş olmalı.** `src`, `docker`, `global.json` ve `assets` altında commit edilmemiş değişiklik varsa dağıtım durur.
- **Commit GitHub'a gönderilmiş olmalı.** Böylece UAT'de çalışan her sürüm depoda bulunur.

Etiketsiz bir commit de dağıtılabilir; betik bu durumda uyarı verir. Kabul oturumu yalnızca
etiketli sürümle yapılır.

### 3.1 Kaynağı aktar

```bash
# Geliştirici makinesinde, depo kökünde:
git archive --format=tar.gz HEAD src docker global.json assets \
  | ssh root@192.168.3.202 'rm -rf /opt/dsg-hrms/src /opt/dsg-hrms/docker \
      && mkdir -p /opt/dsg-hrms && tar xzf - -C /opt/dsg-hrms'
```

> **Neden `git archive`:** Yalnızca commit'teki dosyalar gider. Çalışma kopyasındaki yerel
> dosyalar (`docker/.env`, `appsettings.Local.json`) sunucuya ulaşmaz. Önceki yöntem
> (`tar`) bunları da taşıyordu: 04.10.2026'ya kadar geliştirici makinesindeki `docker/.env`
> her dağıtımda sunucuya kopyalandı (#125). Yığın `--env-file` ile yalnızca UAT sır
> dosyasını okuduğu için bu kopya kullanılmadı. Bir sonraki dağıtım `docker/` dizinini
> silerek kopyayı da kaldırır.

> **Bu adım `docker/` dizinini SİLER.** Bu yüzden ortam dosyası oraya konmaz;
> `/opt/dsg-hrms/secrets/.env.uat` altında, aktarımın dokunmadığı bir yerde durur
> (bkz. §2.2). Belgenin önceki sürümü "`.env.uat` silinmez" diyordu; bu **yanlıştı**
> ve 17.09.2026'da dağıtım sırrı kendi eliyle sildi.

### 3.2 İmajları derle

```bash
cd /opt/dsg-hrms
# SURUM: git describe --tags --always, COMMIT: git rev-parse HEAD (geliştirici makinesinde)
docker build -f src/backend/Dsg.Hrms.Api/Dockerfile -t dsg-hrms-api:uat -t dsg-hrms-api:$SURUM \
  --label org.opencontainers.image.revision=$COMMIT --label org.opencontainers.image.version=$SURUM src/backend
docker build -t dsg-hrms-web:uat -t dsg-hrms-web:$SURUM \
  --label org.opencontainers.image.revision=$COMMIT --label org.opencontainers.image.version=$SURUM src/frontend/dsg-hrms-web
```

İmaj, hangi commit'ten derlendiğini etiketinde taşır. Çalışan sürüm şöyle okunur:

```bash
docker inspect --format '{{index .Config.Labels "org.opencontainers.image.version"}} {{index .Config.Labels "org.opencontainers.image.revision"}}' dsg-hrms-uat-api
cat /opt/dsg-hrms/VERSION
```

> Backend derlemesi 2 çekirdekli sunucuda **10–20 dakika** sürer. Uzun sürerse
> `nohup ... &` ile arka planda çalıştırıp günlüğü izleyin.

### 3.3 Veritabanı şemasını uygula

Sunucuda **.NET SDK yoktur**; şema, geliştirici makinesinde üretilen **idempotent**
SQL betiğiyle uygulanır. Betik daha önce uygulanmış migration'ları atlar, tekrar
çalıştırmak güvenlidir.

```bash
# Geliştirici makinesinde:
cd src/backend
dotnet ef migrations script --idempotent --context HrmsDbContext \
  --project Dsg.Hrms.Infrastructure --startup-project Dsg.Hrms.Api \
  --output /tmp/sema.sql
scp /tmp/sema.sql root@192.168.3.202:/opt/dsg-hrms/sema.sql

# Sunucuda:
cd /opt/dsg-hrms/docker
set -a && . /opt/dsg-hrms/secrets/.env.uat && set +a
docker exec -i dsg-hrms-uat-postgres \
  psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 < /opt/dsg-hrms/sema.sql
```

> **Şema değişikliği uygulamanın açılışında yapılmaz** (`KR-065`). Üretimde şema
> değişikliği kontrollü bir adımdır, uygulama başlatmanın yan etkisi değil.

### 3.4 Yığını başlat

```bash
cd /opt/dsg-hrms/docker
docker compose -f compose.uat.yml --env-file /opt/dsg-hrms/secrets/.env.uat up -d
```

### 3.5 Dağıtımı kaydet

Doğrulama (§4) geçince betik dağıtımı iki yere yazar:

| Kayıt | Yer | İçerik |
|---|---|---|
| Sunucu | `/opt/dsg-hrms/deployments.log`; son sürüm `/opt/dsg-hrms/VERSION` | Zaman (UTC), sürüm, commit, dağıtan |
| Depo | `docs/33061/TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md` | Aynı bilgi; betik satırı ekler, PR ile commit edilir |

Betik, çalışan konteynerlerin etiketindeki commit'i dağıtılan commit'le karşılaştırır.
Eşleşmezse, yani eski imaj ayakta kalmışsa, dağıtım başarısız sayılır ve kayıt yazılmaz.

---

## 4. Doğrulama

Dağıtım, aşağıdakilerin tamamı geçmeden **tamamlanmış sayılmaz**:

```bash
# Sunucuda — üç konteyner de "healthy" olmalı
docker ps --format "{{.Names}}\t{{.Status}}"

# Ağdan — web ve sağlık ucu
curl -s -o /dev/null -w "%{http_code}\n" http://insankaynaklaritest.duzen.com.tr/
curl -s http://insankaynaklaritest.duzen.com.tr/health/ready

# Ağdan — API ve veritabanı DIŞARI KAPALI olmalı
curl -s -o /dev/null -w "%{http_code}\n" --max-time 5 http://192.168.3.202:5299/health/live  # 000 beklenir
```

| Kontrol | Beklenen |
|---|---|
| Üç konteyner | `healthy` |
| `http://insankaynaklaritest.duzen.com.tr/` | `200` |
| `/health/ready` | `{"status":"Healthy", ... "postgresql":"Healthy"}` |
| Eşleşmeyen yol (SPA geri dönüşü) | `200` |
| Güvenlik başlıkları | `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` |
| Dışarıdan `5299` ve `5434` | Erişilemez |

---

## 5. İşletim

| İş | Komut |
|---|---|
| Durum | `docker ps` · `docker compose -f compose.uat.yml --env-file /opt/dsg-hrms/secrets/.env.uat ps` |
| Günlükler | `docker compose -f compose.uat.yml --env-file /opt/dsg-hrms/secrets/.env.uat logs -f api` |
| Yeniden başlat | `docker compose -f compose.uat.yml --env-file /opt/dsg-hrms/secrets/.env.uat restart` |
| Durdur | `docker compose -f compose.uat.yml --env-file /opt/dsg-hrms/secrets/.env.uat down` |
| **Veriyi de sil** | `... down -v` — **UAT verisi gider**, İK'ya haber verilmeden yapılmaz |

**Yedek (`KR-098`):** UAT veritabanı **düzenli yedeklenmez**. Veritabanında gerçek kişisel veri
olduğu için kopya sayısı az tutulur. Kayıp durumunda personel verisi LOGO'dan yeniden çekilir;
kişiler yeniden üye olur. Kabul kanıtları (kabul formu, raporlar) depodadır.

Riskli bir işlemden (elle şema değişikliği, volume taşıma) **önce** elle kopya alınır.
İşlem doğrulandıktan sonra kopya **silinir**:

```bash
cd /opt/dsg-hrms/docker && set -a && . /opt/dsg-hrms/secrets/.env.uat && set +a
docker exec dsg-hrms-uat-postgres pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB" | gzip > /opt/dsg-hrms/yedek-$(date +%F).sql.gz
chmod 600 /opt/dsg-hrms/yedek-*.sql.gz
# Islem dogrulandiktan sonra:
rm /opt/dsg-hrms/yedek-*.sql.gz
```

---

## 6. Güvenlik notları ve açık işler

| Konu | Durum |
|---|---|
| Veritabanı ve API dışarı kapalı | ✅ Yapıldı |
| Sırlar sunucuda üretiliyor, depoda yok | ✅ Yapıldı |
| Konteynerler root olmayan kullanıcıyla çalışıyor | ✅ (`KR-065`) |
| **SSH parola ile girişin kapatılması** | ⚠️ **Öneriliyor** — anahtar kuruldu, parola hâlâ açık |
| **Sunucu parolasının değiştirilmesi** | ⚠️ **Öneriliyor** — kurulum sırasında paylaşıldı |
| **HTTPS (TLS)** | ✅ Yapıldı — Let's Encrypt, 17.09.2026 (`KR-067`) |
| **Sertifikanın süresinin dolması** | ⚠️ **İzleniyor** (`R-18`) — yenileme elle; bitiş **16.12.2026** |
| **HSTS** | ⛔ Bilinçli olarak kapalı — gerekçe §7 |
| Güvenlik duvarı (ufw) | ⚠️ Açık — yalnızca 22 ve 80'e izin verilmesi önerilir |
| Kimlik doğrulama | ⏳ T3 Kimlik Yönetimi ile gelecek; şu an uygulamada oturum yok |

---

## 7. TLS sertifikası

### 7.1 Nasıl çalışıyor

Sertifika **Let's Encrypt**'ten, **DNS-01** doğrulamasıyla alınır (`KR-067`). DNS-01
seçilmesinin nedeni, sunucunun internete açılmasını gerektirmemesidir; sistem yalnızca
kurum içinden erişilebilir kalır.

nginx imajı **tek imaj, iki mod** çalışır (`nginx/select-config.sh`): `/etc/nginx/tls` altında
sertifika varsa TLS, yoksa düz HTTP. Böylece geliştirme ortamı sertifikasız çalışmaya
devam ederken UAT ve üretim **aynı imajı** kullanır.

| Dosya | Yer |
|---|---|
| Certbot sertifikaları | `/etc/letsencrypt/live/insankaynaklaritest.duzen.com.tr/` |
| Konteynerin okuduğu kopya | `/opt/dsg-hrms/tls/` (sahip UID 101 = konteyner içindeki `nginx`) |
| Doğrulama kancası | `/opt/acme/hook.sh` — depoda: `docker/acme-dns-hook.sh` |
| Dağıtım kancası | `/usr/local/sbin/dsg-renew-tls.sh` — depoda: `docker/renew-tls.sh` |

> Her iki kanca da **kaynak ağacının dışında** kurulur. Dağıtım `/opt/dsg-hrms/docker`
> dizinini sildiği için, orada duran bir betik ilk dağıtımda kaybolurdu.

### 7.2 Yenileme (90 günde bir, ELLE)

Kurum DNS'i ayrı bir ekip tarafından yönetildiğinden TXT kaydı otomatik
güncellenemiyor; yenileme bilinçli olarak elle yapılır (`KR-067`). `certbot.timer`
bu nedenle **kapatılmıştır** — açık bırakılsaydı certbot günde iki kez kancayı
çalıştırır, kanca da kimsenin eklemediği kaydı 12 saat beklerdi.

**Sıralama önemlidir. Şu sırayla yapılır:**

1. **Önce certbot başlatılır**, sonra kayıt eklenir — tersi değil:

   ```bash
   setsid nohup certbot certonly --manual --preferred-challenges dns      --manual-auth-hook /opt/acme/hook.sh      --agree-tos --no-eff-email --register-unsafely-without-email --non-interactive      --domain insankaynaklaritest.duzen.com.tr      > /opt/acme/certbot.log 2>&1 < /dev/null &
   ```

2. Beklenen değer okunur: `cat /opt/acme/expected-record.txt`

3. TXT kaydı **HER İKİ yetkili sunucuya birden** eklenir:
   **`ankara.duzen.com.tr` (212.57.13.19)** ve **`cayyolu.duzen.com.tr` (212.156.67.66)**.
   Her ikisinde de **SOA seri numarası artırılır** ve `rndc reload` yapılır.

4. Doğrulama izlenir: `tail -f /opt/acme/certbot.log`

5. Başarılı olduğunda sertifika `renew_hook` ile `/opt/dsg-hrms/tls` altına kopyalanır
   ve web konteyneri yeniden başlatılır — ek işlem gerekmez.

6. Doğrulanır (bkz. §7.3) ve **yeni bitiş tarihi bu runbook'un §1 tablosuna yazılır**.

> **Neden bu sıra?** certbot koşumu bir kez düşerse doğrulama yetkisi geçersizleşir ve
> **yeni bir jeton** üretilir; eklenen kayıt işe yaramaz. 16–17.09.2026'da bu üç kez
> yaşandı. certbot beklediği sürece jeton sabit kalır.

> **İki sunucu da neden şart?** Let's Encrypt doğrulamayı **birden çok noktadan** yapar.
> Kayıt yalnızca birinde olduğunda hata `During secondary validation: NXDOMAIN`
> şeklinde gelir — teşhisi zor bir belirtidir. Bu iki sunucu arasında bölge aktarımı
> **yoktur**, bağımsız yönetilirler; birine eklemek yetmez.

> **Eski kayıt silinmeyebilir.** Bir ada birden fazla TXT kaydı tanımlanabilir; yenisi
> eklenince Let's Encrypt aradığı değeri bulur.

### 7.3 Doğrulama

```bash
# Sertifika geçerli mi? "-k" KULLANILMAZ - doğrulamayı kapatmak kontrolü anlamsız kılar.
curl -s -o /dev/null -w 'durum=%{http_code} tls=%{ssl_verify_result}
' https://insankaynaklaritest.duzen.com.tr/
# Beklenen: durum=200 tls=0

# HTTP, HTTPS'e yönlendiriyor mu?
curl -s -o /dev/null -w 'durum=%{http_code} hedef=%{redirect_url}
' http://insankaynaklaritest.duzen.com.tr/
# Beklenen: durum=301, hedef https:// ile başlar

# Bitiş tarihi
openssl x509 -in /opt/dsg-hrms/tls/fullchain.pem -noout -enddate
```

`./docker/deploy-uat.sh` bu kontrolleri dağıtımın sonunda **kendisi yapar** ve
başarısız olursa dağıtımı hata ile bitirir.

### 7.4 HSTS neden açık değil

HSTS, tarayıcıya "bu siteye artık yalnızca HTTPS ile bağlan" der ve bu bilgi
tarayıcıda saklanır. Sertifika bir gün yenilenmeyi kaçırırsa kullanıcı uyarıyı
**atlayamaz** ve sistem tamamen erişilemez hâle gelir. Yenileme elle yapıldığı sürece
bu cezanın ağırlığı kabul edilemez. HSTS, yenileme otomatikleştiğinde açılacaktır.

---

## 8. LOGO personel senkronizasyonu

API, LOGO'dan personel verisini 15 dakikada bir okur (`KR-007`, ADR-0003). Bağlantı
tanımlı değilse senkronizasyon **devre dışı** kalır ve sistem çalışmaya devam eder.

### 8.1 Etkinleştirme

Sır dosyasına (`/opt/dsg-hrms/secrets/.env.uat`) **salt-okunur** oturumun bağlantı
dizesi eklenir:

```bash
LOGO_CONNECTION_STRING='Server=<logo-sunucusu>;Database=BORDRO;User Id=hrms_logo_reader;Password=...;TrustServerCertificate=true;ApplicationIntent=ReadOnly'
```

> **Değer tek tırnak içinde yazılır.** Sır dosyasının iki okuyucusu vardır: Docker
> Compose dosyayı düz metin olarak okur, dağıtım betiği ise şema adımında **kabuk
> komutu olarak** okur (`. dosya`). Tırnaksız değerde kabuk noktalı virgüllerde satırı
> böler ve `User Id=...` kısmını komut olarak çalıştırmaya kalkar (#79). Tek tırnak
> iki okuyucuda da aynı değeri verir; parolada tek tırnak karakteri kullanılmamalıdır.

Oturum `docker/logo/read-only-login.sql` ile oluşturulmuş olmalıdır (`KR-004`).
**Yazma yetkili bir oturum kullanılmaz.** Kullanılırsa senkronizasyon bunu her
çalışmadan önce tespit eder, çalışmayı reddeder ve kritik düzeyde günlüğe yazar.

Değişiklikten sonra yığın yeniden başlatılır (§5) veya dağıtım yapılır (§3).

### 8.2 Doğrulama

```bash
curl -s https://insankaynaklaritest.duzen.com.tr/health/sync
```

| `status` | Anlamı |
|---|---|
| `Healthy` | Son çalışma başarılı ve güncel; ya da bağlantı bilinçli olarak tanımlı değil |
| `Degraded` | Henüz çalışma yok ya da son başarılı çalışma 45 dakikadan eski |
| `Unhealthy` | Son çalışma başarısız; neden API günlüğündedir |

Bu uç nokta `/health/ready`'den **ayrıdır**: LOGO kesintisi HRMS'i "hazır değil"
göstermez, çünkü sistem son anlık görüntüyle çalışmaya devam eder (SYG-KMLK-011).

Çalışma geçmişi ve veri kalitesi uyarıları `personnel.sync_run` ve
`personnel.sync_warning` tablolarındadır. Uyarılar kişisel veri içermez; kartlar sicil
koduyla tanımlanır.

Periyot `PRM-ENT-07` parametresidir (varsayılan 15 dakika); yapılandırmadaki
`PersonnelSync:IntervalMinutes` yalnızca veritabanında değer yoksa kullanılır.
"Eski" eşiği periyodun üç katıdır.

---

## 9. Sistem parametreleri ve sırlar

T3 parametreleri `settings.system_parameter` tablosundadır (SYG-KMLK-075). Satırı
olmayan parametre sırasıyla sır dosyasındaki değerle veya katalog varsayılanıyla
çalışır. Değişiklik yeniden başlatma gerektirmez; en geç 1 dakikada etkili olur.

### 9.1 Şifreleme anahtarı (bir kez)

SMTP ve NetGSM parolalarının parametre ekranından girilebilmesi için sır dosyasına
şifreleme anahtarı eklenir (`KR-081`, ADR-0008 §7):

```bash
# Anahtar ekrana YAZDIRILMADAN doğrudan sır dosyasına eklenir.
printf "PARAMETER_PROTECTION_KEY='%s'
" "$(openssl rand -base64 32)" >> /opt/dsg-hrms/secrets/.env.uat
chmod 600 /opt/dsg-hrms/secrets/.env.uat
```

Anahtarın bir kopyası veritabanı yedeğinden **ayrı** bir yerde saklanır. Anahtar
kaybolursa veritabanındaki sır parametreler çözülemez ve ekrandan yeniden girilmeleri
gerekir. Anahtar tanımlı değilse uygulama çalışır; sırlar yalnızca sır dosyasından
okunur.

## 10. Doğrulama kodu ve ileti gönderimi

### 10.1 Sır dosyasına eklenecekler (bir kez)

```bash
# Kod özet anahtarı (SYG-KMLK-025); ekrana yazdırılmadan eklenir.
printf "IDENTITY_CODE_HASH_KEY='%s'
" "$(openssl rand -base64 32)" >> /opt/dsg-hrms/secrets/.env.uat
```

> **Zorunlu (#92):** `IDENTITY_JWT_SIGNING_KEY` tanımlı değilse API **açılmaz**. Bu sürümle
> birlikte ilk dağıtımdan ÖNCE eklenir:
>
> ```bash
> printf "IDENTITY_JWT_SIGNING_KEY='%s'
" "$(openssl rand -base64 32)" >> /opt/dsg-hrms/secrets/.env.uat
> ```
>
> Anahtar değiştirilirse açık erişim jetonları geçersizleşir; kullanıcılar yenileme jetonuyla
> yeni jeton alır, yeniden giriş gerekmez.

SMTP ve NetGSM erişim bilgileri `SMTP_SERVER`, `SMTP_PASSWORD`, `NETGSM_USER_CODE`,
`NETGSM_PASSWORD` değişkenleriyle, **tek tırnak içinde** eklenir (`.env.uat.example`).

### 10.2 Gönderim kipi (`KR-083`)

UAT **gerçek LOGO verisiyle** çalışır. `NOTIFICATIONS_DELIVERY_MODE=Send` gerçek personele
kod gönderir ve UAT'de **kullanılmaz**.

| Kip | Ne zaman |
|---|---|
| `LogOnly` (varsayılan) | Hiçbir ileti gitmez; gönderim kaydı `Suppressed` yazılır |
| `AllowList` | Kabul testleri: `NOTIFICATIONS_ALLOWED_RECIPIENTS` yalnızca test yapanların kurumsal e-posta ve `5XXXXXXXXX` telefonları (virgülle) |

### 10.3 Doğrulama

```bash
curl -s https://insankaynaklaritest.duzen.com.tr/health/notifications
```

Uç **hiçbir ileti göndermez**: SMTP'de oturum açıp kimlik doğrular, NetGSM'de bakiye ve
gönderici adını sorgular. Sonuç 5 dakika önbellekte tutulur.

| Durum | Anlamı |
|---|---|
| `Healthy` | Kimlik doğrulaması başarılı; ya da kanal bilinçli olarak tanımlı değil |
| `Degraded` | Sunucuya ulaşılamadı |
| `Unhealthy` | Parola, kullanıcı, gönderici adı veya **sunucu sertifikası** hatalı; hiç kimse kod alamaz |

Gönderim kayıtları `notification.delivery` tablosundadır; içerik tutulmaz, alıcı maskelidir.

### 10.4 Üyelik ve ters vekil (#87)

Üyelik akışı `IDENTITY_CODE_HASH_KEY` olmadan çalışmaz (kod ve TCKN özetleri bu anahtarla
alınır). Anahtar değiştirilirse açık kodlar ve son bir saatin hız sınırı sayaçları
geçersizleşir.

API, gerçek istemci IP'sini nginx'in `X-Forwarded-For` başlığından **yalnızca**
`ReverseProxy__TrustedNetworks` ağından gelen isteklerde okur (compose'da
`172.16.0.0/12`). Yığının Docker ağı bu aralıkta olmalıdır:

```bash
docker network inspect dsg-hrms-uat_default --format '{{(index .IPAM.Config 0).Subnet}}'
# Beklenen: 172.16.0.0/12 icinde bir alt ag (orn. 172.18.0.0/16)
```

Aralık dışındaysa denetim izine ve IP başına hız sınırına (SYG-KMLK-059) nginx'in adresi
yazılır; `ReverseProxy__TrustedNetworks` ağın alt ağına göre güncellenir.

**HTTPS zorunluluğu (#115):** API yalnızca HTTPS ile gelen `/api/` isteklerine hizmet verir
(`Security__RequireHttps`, SYG-KMLK-063). Şemayı da aynı ağdan gelen `X-Forwarded-Proto`
başlığından okur. Bu yüzden ağ aralık dışındaysa **tüm API istekleri** `403 https-required`
ile reddedilir. Sertifika eksik kalıp nginx düz HTTP kipine düştüğünde de durum aynıdır:
API şifresiz hizmet vermez. Sağlık uçları (`/health/…`) denetim dışıdır.

```bash
curl -s -o /dev/null -w '%{http_code}\n' -X POST http://127.0.0.1:5299/api/v1/identity/sessions
# Beklenen: 403 (dogrudan, sifresiz istek)
```

### 10.5 SMTP sunucusu sertifikası

Gönderici sunucu sertifikasını **doğrular**; doğrulama kapatılmaz. SMTP adresi,
sertifikadaki adla yazılır: `mail.duzen.com.tr:587`.

27.09.2026 ölçümü (#85):

| Port | Sertifika |
|---|---|
| 443 (web) | `CN=mail.duzen.com.tr`, Sectigo, geçerlilik sonu 11.10.2026 |
| **587 (Postfix)** | Kendinden imzalı `CN=localhost`, süresi 07.03.2026'da **dolmuş** |

Postfix geçerli sertifikayı kullanana kadar e-posta kanalı `Unhealthy` görünür ve
e-postayla kod gönderilemez. Düzeltme sunucu tarafındadır: Postfix'in
`smtpd_tls_cert_file` ve `smtpd_tls_key_file` ayarları web sunucusundaki
`mail.duzen.com.tr` sertifikasını (tam zincir) göstermeli, ardından `postfix reload`.
Sertifika her yenilendiğinde Postfix de yeniden yüklenmelidir.

Doğrulama (ileti göndermez):

```bash
openssl s_client -starttls smtp -connect mail.duzen.com.tr:587 -servername mail.duzen.com.tr   -verify_hostname mail.duzen.com.tr </dev/null 2>&1 | grep -E "subject=|Verify return code"
# Beklenen: subject=CN=mail.duzen.com.tr ve "Verify return code: 0 (ok)"
```

---

## 11. İlk sistem yöneticisi (#100)

Rol yönetim ekranı T4'tedir. O zamana kadar sistem yöneticisi, sır dosyasındaki listeyle belirlenir (SYG-KMLK-074):

```bash
# Kurumsal e-postalar, virgülle. Kişinin hesabı olmalı (üye olmuş olmalı).
echo "ACCESS_CONTROL_BOOTSTRAP_ADMINISTRATORS='ad.soyad@duzen.com.tr'" >> /opt/dsg-hrms/secrets/.env.uat
cd /opt/dsg-hrms/docker && docker compose -f compose.uat.yml --env-file /opt/dsg-hrms/secrets/.env.uat up -d api
```

- Listedeki e-postayla **giriş yapıldığında** Sistem Yöneticisi rolü **bir kez** atanır. Atama denetim izine (`audit.change_log`, varlık `UserRole`) yazılır.
- Listeden çıkarmak atamayı **geri almaz**. Rolün kaldırılması bilinçli bir işlemdir (T4).
- Geçersiz bir adres yazılırsa API açılmaz; hata iletisi listeyi gösterir.

Doğrulama: yönetici hesabıyla girişte oturum yanıtındaki `user.permissions` 7 izni içerir. İzinler `docs/mimari/izin-listesi.md` belgesindedir.

### 11.1 Davet bağlantısının adresi (#107)

İK'nin gönderdiği parola oluşturma bağlantısı `Identity__PublicBaseUrl` + `/invite` adresini taşır. UAT'de varsayılan `https://insankaynaklaritest.duzen.com.tr` olduğundan ayrıca bir şey yapılmaz. Başka bir adres için sır dosyasına `IDENTITY_PUBLIC_BASE_URL='https://…'` yazılır.

## 12. Sunucu saati (#103)

Sunucunun saati **eşitlenmiş olmalıdır**. Bu gereklilik üç yerden doğar:
- Doğrulama kodunun ve oturumun süreleri sunucunun saatiyle hesaplanır.
- Denetim izine sunucunun saati yazılır.
- Günlük kayıtlarındaki zaman sunucunun saatidir.

Uygulama tarayıcıyla sunucu arasındaki farkı `Date` başlığından ölçüp geri sayımı buna göre düzeltir. Yine de sunucu saatinin kendisi doğru olmalıdır.

### 12.1 Bulgu ve yapılan ayar (29.09.2026)

UAT sunucusu yaklaşık **10 dakika (594 sn) gerideydi**; `System clock synchronized: no`.

| Denetim | Sonuç |
|---|---|
| Ubuntu 26.04 varsayılan kaynakları | Yalnızca **NTS**'li (şifreli) Canonical sunucuları |
| NTS anahtar değişimi (TCP 4460) | **Kapalı** (kurum ağı) — varsayılan kaynaklara hiç ulaşılamıyor |
| Düz NTP (UDP 123) | **Açık** — `pool.ntp.org`, `time.google.com` yanıt veriyor |
| 192.168.3.180 | Ağ geçidi; zaman hizmeti vermiyor |

Varsayılan dosyalar değiştirilmeden iki dosya eklendi:

```bash
# /etc/chrony/sources.d/dsg-hrms-ntp.sources — duz NTP kaynaklari
pool tr.pool.ntp.org iburst maxsources 3
pool time.google.com iburst maxsources 1

# /etc/chrony/conf.d/dsg-hrms.conf — NTS kaynaklari tanimliyken dogrulanmamis
# kaynaklarin bekletilmemesi icin (varsayilan "mix" kipi onlari hic secmiyordu)
authselectmode ignore
```

Ardından `systemctl restart chrony && chronyc makestep`. Sonuç `System clock synchronized: yes` oldu; chrony açılışta başlıyor (`enabled`).

> Kurum içinde bir NTP sunucusu kurulursa `dsg-hrms-ntp.sources` dosyasına o yazılır; genel havuz kaldırılabilir.

### 12.2 Doğrulama

```bash
timedatectl | grep synchronized            # "yes" beklenir
chronyc tracking | grep -E "Reference ID|System time"
curl -sI https://insankaynaklaritest.duzen.com.tr/health/live | grep -i "^date"; date -u
```

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-15 | 0.1 | İlk oluşturma — UAT sunucusu kurulumu ve dağıtım adımları | Bilgi İşlem |
| 2026-09-17 | 0.2 | TLS devreye alındı (§7): Let's Encrypt sertifikası, elle yenileme yordamı, doğrulama. Ortam dosyası `gizli/` altına taşındı — dağıtım onu siliyordu. `127.0.0.1:` öneki compose dosyasına sabitlendi | Bilgi İşlem |
| 2026-09-25 | 0.3 | Sunucuya bağlı adlar İngilizceye çevrildi (`KR-058`, düzeltici faaliyet #66): `gizli/` → `secrets/`, `TLS_DIZINI` → `TLS_DIR`, `beklenen-kayit.txt` → `expected-record.txt`. Sunucu tarafı aynı gün uygulandı | Bilgi İşlem |
| 2026-09-25 | 0.4 | Volume adları İngilizceye çevrildi: `uat-postgres-verisi` → `uat-postgres-data`, `uat-api-gunlukleri` → `uat-api-logs` (`KR-058`, #66). Veri kopyalanarak taşındı (sağlama değerleri ve satır sayıları eşit); eski volume'ler yedek olarak bırakıldı | Bilgi İşlem |
| 2026-09-26 | 0.5 | §8 LOGO personel senkronizasyonu (etkinleştirme, `/health/sync`); §3.3 `dotnet ef` komutuna `--context HrmsDbContext` eklendi (#74) | Bilgi İşlem |
| 2026-09-26 | 0.6 | §8.1: sır dosyasında LOGO bağlantı dizesi tek tırnak içinde yazılır (#79) | Bilgi İşlem |
| 2026-09-26 | 0.7 | Betik ve dosya adları İngilizce: `deploy-uat.sh`, `renew-tls.sh` (sunucuda `dsg-renew-tls.sh`), `acme-dns-hook.sh`, `read-only-login.sql`, `.env.uat.example` (#81) | Bilgi İşlem |
| 2026-09-26 | 0.8 | §9 sistem parametreleri ve sır parametre şifreleme anahtarı; §8.2 periyot parametreden (#83) | Bilgi İşlem |
| 2026-09-27 | 0.9 | §10 doğrulama kodu ve ileti gönderimi: özet anahtarı, gönderim kipleri, `/health/notifications` (#85) | Bilgi İşlem |
| 2026-09-27 | 1.0 | §10.4 üyelik anahtarı ve ters vekil ağı denetimi (#87) | Bilgi İşlem |
| 2026-09-28 | 1.1 | §10.1: JWT imzalama anahtarı zorunlu (#92) | Bilgi İşlem |
| 2026-09-29 | 1.2 | §11: ilk sistem yöneticisi (#100) | Bilgi İşlem |
| 2026-09-29 | 1.3 | §12: sunucu saati eşitlemesi (#103) | Bilgi İşlem |
| 2026-09-30 | 1.4 | §12.1: NTS kapalı, düz NTP ile eşitleme ayarı; §12.2 doğrulama (#103) | Bilgi İşlem |
| 2026-09-30 | 1.5 | §11.1: davet bağlantısının adresi (#107) | Bilgi İşlem |
| 2026-10-02 | 1.6 | §10.4: HTTPS zorunluluğu ve doğrulaması (#115) | Bilgi İşlem |
| 2026-10-04 | 1.7 | §3: sürüm seçimi, git archive ile aktarım, imajda commit etiketi, dağıtım kaydı; §3.3 sır dosyası yolu düzeltildi (#125) | Bilgi İşlem |
| 2026-10-04 | 1.8 | §1: erişim yöntemi ve veri sınıfı (gerçek veri) düzeltildi; §5 yedek kuralı (`KR-098`, #133) | Bilgi İşlem |
