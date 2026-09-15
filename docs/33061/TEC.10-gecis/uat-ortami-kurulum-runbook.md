# UAT Ortamı — Kurulum ve Dağıtım Runbook'u

**Belge kimliği:** TEC.10-RB-001
**Son güncelleme:** 2026-09-15
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
| **Adres** | http://insankaynaklaritest.duzen.com.tr |
| **Uygulama dizini** | `/opt/dsg-hrms` |
| **Erişim** | SSH anahtarı (`dsg-hrms-uat-deploy`) |
| **Amaç** | İK kabul testi (`KR-024`) — **gerçek veri değil**, maskelenmiş kopya |

### Yayımlanan portlar

| Port | Erişim | Ne |
|---|---|---|
| `80` | **Ağa açık** | Web arayüzü (nginx) |
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

```bash
cd /opt/dsg-hrms/docker
PAROLA=$(openssl rand -base64 30 | tr -d "/+=" | head -c 32)

cat > .env.uat <<SON
POSTGRES_DB=dsg_hrms_uat
POSTGRES_USER=postgres
POSTGRES_PASSWORD=$PAROLA
POSTGRES_PORT=127.0.0.1:5434
API_PORT=127.0.0.1:5299
WEB_PORT=80
API_IMAGE=dsg-hrms-api:uat
WEB_IMAGE=dsg-hrms-web:uat
SON

chmod 600 .env.uat
```

> `POSTGRES_PORT` ve `API_PORT` değerlerindeki `127.0.0.1:` öneki bilinçlidir:
> Compose bu değeri `<host>:<port>:<konteyner portu>` olarak yorumlar ve servisi
> yalnızca sunucu içine açar.

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

> `.env.uat` silinmez: yalnızca `src` ve `docker` dizinleri yenilenir. Compose dosyası
> `docker/` altında olduğu için yeniden kopyalanır; ortam dosyası ise `.gitignore`
> kapsamında olduğundan paket içinde gelmez.

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
dotnet ef migrations script --idempotent \
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
docker compose -f compose.uat.yml --env-file .env.uat up -d
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
| Durum | `docker ps` · `docker compose -f compose.uat.yml --env-file .env.uat ps` |
| Günlükler | `docker compose -f compose.uat.yml --env-file .env.uat logs -f api` |
| Yeniden başlat | `docker compose -f compose.uat.yml --env-file .env.uat restart` |
| Durdur | `docker compose -f compose.uat.yml --env-file .env.uat down` |
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
| **HTTPS (TLS)** | ⚠️ Açık — şu an düz HTTP. Kurum içi sertifika ile kapatılmalı |
| Güvenlik duvarı (ufw) | ⚠️ Açık — yalnızca 22 ve 80'e izin verilmesi önerilir |
| Kimlik doğrulama | ⏳ T3 Kimlik Yönetimi ile gelecek; şu an uygulamada oturum yok |

> **HTTPS neden önemli:** Kimlik Yönetimi devreye girdiğinde parolalar ve oturum
> çerezleri ağ üzerinden geçecek. Düz HTTP'de bunlar aynı ağdaki biri tarafından
> okunabilir. Ayrıca oturum çerezi `Secure` işaretiyle tanımlandığında (ADR-0006 §8)
> HTTP üzerinden **hiç çalışmaz**. TLS, T3 devreye alınmadan önce kurulmalıdır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-15 | 0.1 | İlk oluşturma — UAT sunucusu kurulumu ve dağıtım adımları | Bilgi İşlem |
