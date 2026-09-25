#!/usr/bin/env bash
#
# UAT dagitimi (TEC.10).
#
# Kullanim (depo kokunde):
#   ./docker/uat-dagit.sh [sunucu]
#
# Ne yapar:
#   1. Kaynagi sunucuya aktarir
#   2. Imajlari sunucuda derler ve "uat" etiketiyle isaretler
#   3. Veritabani semasini idempotent betikle uygular
#   4. Yigini baslatir ve DOGRULAR
#
# Ne YAPMAZ:
#   - Sir uretmez/degistirmez. ".env.uat" sunucuda bir kez olusturulur ve
#     dokunulmaz (KR-038). Ayrinti: docs/33061/TEC.10-gecis/uat-ortami-kurulum-runbook.md
#
# Sir dosyasinin yeri: "<dizin>/gizli/.env.uat" - KAYNAK AGACININ DISINDA.
#
# Neden disarida? Aktarim adimi "<dizin>/docker" dizinini silip yeniden olusturur.
# Dosya orada dururken her dagitim, ihtiyac duydugu sirri KENDI ELIYLE siliyordu;
# yigin "couldn't find env file" ile ayaga kalkmiyordu. Bir sir, dagitimin sildigi
# agacin icinde yasamamalidir.

set -euo pipefail

SERVER="${1:-root@192.168.3.202}"
APP_DIR="/opt/dsg-hrms"
SECRETS_FILE="/opt/dsg-hrms/gizli/.env.uat"
DOMAIN="insankaynaklaritest.duzen.com.tr"
# Adres, sunucuda sertifika olup olmamasina gore belirlenir (asagida).
BASE_URL="http://${DOMAIN}"

info() { printf '\n\033[1;34m==> %s\033[0m\n' "$1"; }
fail() { printf '\n\033[1;31mHATA: %s\033[0m\n' "$1" >&2; exit 1; }

[[ -f docker/compose.uat.yml ]] || fail 'Bu betik depo kokunden calistirilmalidir.'

info 'Ortam dosyasi kontrol ediliyor'
# Sir uretimi dagitimin isi degildir; yoksa kurulum adimlari uygulanmalidir.
ssh "$SERVER" "test -f $SECRETS_FILE" \
  || fail "Sunucuda $SECRETS_FILE yok. Runbook §2.2'yi uygulayin."

info 'Kaynak aktariliyor'
tar czf - \
    --exclude=node_modules --exclude=bin --exclude=obj --exclude=dist \
    --exclude=coverage --exclude=logs \
    src docker global.json assets \
  | ssh "$SERVER" "rm -rf $APP_DIR/src $APP_DIR/docker && mkdir -p $APP_DIR && tar xzf - -C $APP_DIR"

info 'Sema betigi uretiliyor'
dotnet ef migrations script --idempotent \
  --project src/backend/Dsg.Hrms.Infrastructure \
  --startup-project src/backend/Dsg.Hrms.Api \
  --output /tmp/dsg-sema.sql

ssh "$SERVER" "cat > $APP_DIR/sema.sql" < /tmp/dsg-sema.sql

info 'Imajlar derleniyor (backend 10-20 dk surebilir)'
ssh "$SERVER" "cd $APP_DIR \
  && docker build -f src/backend/Dsg.Hrms.Api/Dockerfile -t dsg-hrms-api:uat src/backend \
  && docker build -t dsg-hrms-web:uat src/frontend/dsg-hrms-web"

info 'Yigin baslatiliyor'
ssh "$SERVER" "cd $APP_DIR/docker && docker compose -f compose.uat.yml --env-file $SECRETS_FILE up -d"

info 'Sema uygulaniyor'
# Veritabaninin hazir olmasi beklenir; aksi hâlde ilk dagitimda yaris olusur.
ssh "$SERVER" "cd $APP_DIR/docker && set -a && . $SECRETS_FILE && set +a \
  && for i in \$(seq 1 30); do docker exec dsg-hrms-uat-postgres pg_isready -U \"\$POSTGRES_USER\" -q && break; sleep 2; done \
  && docker exec -i dsg-hrms-uat-postgres psql -U \"\$POSTGRES_USER\" -d \"\$POSTGRES_DB\" -v ON_ERROR_STOP=1 < $APP_DIR/sema.sql > /dev/null \
  && echo 'sema uygulandi'"

info 'Dogrulaniyor'

# Sertifika varsa yigin TLS ile calisir; duz HTTP artik 200 degil 301 doner.
# Bu ayrim yapilmazsa dogrulama, calisan bir sistemi "basarisiz" ilan ederdi.
if ssh "$SERVER" "test -r $APP_DIR/tls/fullchain.pem"; then
  TLS_ENABLED=true
  BASE_URL="https://${DOMAIN}"
  echo "  TLS etkin; dogrulama HTTPS uzerinden yapilacak."
else
  TLS_ENABLED=false
  echo "  Sertifika yok; dogrulama duz HTTP uzerinden yapilacak."
fi

for i in $(seq 1 20); do
  status=$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "$BASE_URL/" || true)
  [[ "$status" == '200' ]] && break
  sleep 5
done

[[ "${status:-}" == '200' ]] || fail "Web arayuzu yanit vermiyor (son durum: ${status:-yok})"

health=$(curl -s --max-time 10 "$BASE_URL/health/ready" || true)
grep -q '"status":"Healthy"' <<<"$health" || fail "Saglik kontrolu basarisiz: $health"

# TLS etkinse: duz HTTP yonlendirmeli ve sertifika GECERLI olmali.
#
# "curl -k" KULLANILMAZ: dogrulamayi kapatmak, sertifika hatali oldugunda bile
# testin gecmesi demektir - yani kontrolun kendisini islevsiz kilar.
if [[ "$TLS_ENABLED" == 'true' ]]; then
  redirect_status=$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "http://${DOMAIN}/" || true)
  [[ "$redirect_status" == '301' ]] || fail "HTTP, HTTPS'e yonlendirmiyor (durum: ${redirect_status:-yok})"

  redirect_target=$(curl -s -o /dev/null -w '%{redirect_url}' --max-time 5 "http://${DOMAIN}/" || true)
  [[ "$redirect_target" == https://* ]] || fail "Yonlendirme HTTPS'e gitmiyor: ${redirect_target:-yok}"
fi

# API ve veritabani DISARIYA KAPALI olmali (runbook §1).
server_ip="${SERVER#*@}"
external_status=$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "http://$server_ip:5299/health/live" || true)
[[ "$external_status" == '000' ]] || fail "API disariya acik gorunuyor (durum: $external_status)"

printf '\n\033[1;32mDagitim tamam.\033[0m %s\n' "$BASE_URL"
ssh "$SERVER" 'docker ps --format "  {{.Names}}\t{{.Status}}"'
