# UAT Ortamı — Kurulum ve Dağıtım Runbook'u

**Belge kimliği:** TEC.10-RB-001
**Son güncelleme:** 2026-09-26
**İlgili süreçler:** TEC.10 (Geçiş), MAN.5 (Konfigürasyon Yönetimi)
**İlgili kararlar:** `KR-024`, `KR-038`, `KR-065`

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
| **Erişim** | SSH anahtarı (`dsg-hrms-uat-deploy`) |
| **Amaç** | İK kabul testi (`KR-024`) — **gerçek veri değil**, maskelenmiş kopya |
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

### 3.1 Kaynağı aktar

```bash
# Geliştirici makinesinde, depo kökünde:
tar czf - --exclude=node_modules --exclude=bin --exclude=obj --exclude=dist \
    --exclude=coverage --exclude=logs src docker global.json assets \
  | ssh root@192.168.3.202 'rm -rf /opt/dsg-hrms/src /opt/dsg-hrms/docker \
      && mkdir -p /opt/dsg-hrms && tar xzf - -C /opt/dsg-hrms'
```

> **Bu adım `docker/` dizinini SİLER.** Bu yüzden ortam dosyası oraya konmaz;
> `/opt/dsg-hrms/secrets/.env.uat` altında, aktarımın dokunmadığı bir yerde durur
> (bkz. §2.2). Belgenin önceki sürümü "`.env.uat` silinmez" diyordu; bu **yanlıştı**
> ve 17.09.2026'da dağıtım sırrı kendi eliyle sildi.

### 3.2 İmajları derle

```bash
cd /opt/dsg-hrms
docker build -f src/backend/Dsg.Hrms.Api/Dockerfile -t dsg-hrms-api:uat src/backend
docker build -t dsg-hrms-web:uat src/frontend/dsg-hrms-web
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
set -a && . ./.env.uat && set +a
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

**Yedek:** UAT verisi maskelenmiş test verisidir; düzenli yedeklenmez. İK kabul testi
sırasında oluşturulan veri korunacaksa test öncesinde anlık kopya alınır:

```bash
docker exec dsg-hrms-uat-postgres pg_dump -U postgres dsg_hrms_uat | gzip > /opt/dsg-hrms/yedek-$(date +%F).sql.gz
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

---

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
