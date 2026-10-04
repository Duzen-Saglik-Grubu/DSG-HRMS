#!/usr/bin/env bash
#
# UAT dagitimi (TEC.10).
#
# Kullanim (depo kokunde):
#   ./docker/deploy-uat.sh [sunucu]
#
# Ne yapar:
#   1. Dagitilacak commit'i belirler; temiz ve GitHub'a gonderilmis olmasini sart kosar
#   2. Kaynagi bu commit'ten (git archive) sunucuya aktarir
#   3. Imajlari sunucuda derler; "uat" ve surum etiketiyle, commit'i etikete yazarak isaretler
#   4. Veritabani semasini idempotent betikle uygular
#   5. Yigini baslatir ve DOGRULAR; calisan imajin commit'i dagitilanla ayni olmali
#   6. Dagitimi sunucudaki kayda ve depodaki dagitim kaydina yazar (TEC.10, #125)
#
# Belirli bir surumu kurmak icin once o etikete gecin:
#   git checkout v0.2.0-rc.1 && ./docker/deploy-uat.sh
#
# Ne YAPMAZ:
#   - Sir uretmez/degistirmez. ".env.uat" sunucuda bir kez olusturulur ve
#     dokunulmaz (KR-038). Ayrinti: docs/33061/TEC.10-gecis/uat-ortami-kurulum-runbook.md
#
# Sir dosyasinin yeri: "<dizin>/secrets/.env.uat" - KAYNAK AGACININ DISINDA.
#
# Neden disarida? Aktarim adimi "<dizin>/docker" dizinini silip yeniden olusturur.
# Dosya orada dururken her dagitim, ihtiyac duydugu sirri KENDI ELIYLE siliyordu;
# yigin "couldn't find env file" ile ayaga kalkmiyordu. Bir sir, dagitimin sildigi
# agacin icinde yasamamalidir.

set -euo pipefail

SERVER="${1:-root@192.168.3.202}"
APP_DIR="/opt/dsg-hrms"
SECRETS_FILE="/opt/dsg-hrms/secrets/.env.uat"
DOMAIN="insankaynaklaritest.duzen.com.tr"
# Adres, sunucuda sertifika olup olmamasina gore belirlenir (asagida).
BASE_URL="http://${DOMAIN}"

info() { printf '\n\033[1;34m==> %s\033[0m\n' "$1"; }
fail() { printf '\n\033[1;31mHATA: %s\033[0m\n' "$1" >&2; exit 1; }

[[ -f docker/compose.uat.yml ]] || fail 'Bu betik depo kokunden calistirilmalidir.'

info 'Dagitilacak surum belirleniyor'
# Kurulan sey bir commit olmalidir: aksi hâlde "UAT'de ne calisiyor?" sorusunun
# cevabi yoktur ve kabul formu bir surume atif yapamaz (TEC.10, MAN.5).
# Denetim, sunucuya giden ve sema betigini ureten yollarla sinirlidir; belgelerdeki
# degisiklik (orn. commit edilmemis dagitim kaydi satiri) dagitimi engellemez.
git diff --quiet HEAD -- src docker global.json assets \
  || fail 'Calisma kopyasinda commit edilmemis degisiklik var. Dagitim yalnizca commit edilmis koddan yapilir.'
COMMIT=$(git rev-parse HEAD)
[[ -n "$(git branch -r --contains "$COMMIT" 2>/dev/null)" ]] \
  || fail "$COMMIT GitHub'da yok. Once gonderin (push); dagitilan her surum depoda izlenebilir olmalidir."
# Etiketli commit'te etiketin kendisi (v0.2.0-rc.1), degilse en yakin etiket + uzaklik + kisa SHA.
VERSION=$(git describe --tags --always)
DEPLOYER=$(git config user.name || echo bilinmiyor)
echo "  Surum: $VERSION  Commit: $COMMIT"
git describe --tags --exact-match >/dev/null 2>&1 \
  || echo '  Uyari: commit etiketli degil. Kabul oturumu yalnizca etiketli surumle yapilir.'

info 'Ortam dosyasi kontrol ediliyor'
# Sir uretimi dagitimin isi degildir; yoksa kurulum adimlari uygulanmalidir.
ssh "$SERVER" "test -f $SECRETS_FILE" \
  || fail "Sunucuda $SECRETS_FILE yok. Runbook §2.2'yi uygulayin."

info 'Kaynak aktariliyor'
# git archive: yalnizca commit'teki dosyalar gider. Calisma kopyasindaki yerel
# dosyalar (appsettings.Local.json, .env gibi .gitignore'dakiler) sunucuya ULASMAZ.
git archive --format=tar.gz "$COMMIT" src docker global.json assets \
  | ssh "$SERVER" "rm -rf $APP_DIR/src $APP_DIR/docker && mkdir -p $APP_DIR && tar xzf - -C $APP_DIR"

info 'Sema betigi uretiliyor'
# --context: projede ikinci bir baglam (LogoDbContext) vardir; LOGO semasi
# HRMS tarafindan YONETILMEZ, yalnizca okunur.
dotnet ef migrations script --idempotent \
  --context HrmsDbContext \
  --project src/backend/Dsg.Hrms.Infrastructure \
  --startup-project src/backend/Dsg.Hrms.Api \
  --output /tmp/dsg-sema.sql

ssh "$SERVER" "cat > $APP_DIR/sema.sql" < /tmp/dsg-sema.sql

info 'Imajlar derleniyor (backend 10-20 dk surebilir)'
ssh "$SERVER" "cd $APP_DIR \
  && docker build -f src/backend/Dsg.Hrms.Api/Dockerfile -t dsg-hrms-api:uat -t dsg-hrms-api:$VERSION \
       --label org.opencontainers.image.revision=$COMMIT --label org.opencontainers.image.version=$VERSION src/backend \
  && docker build -t dsg-hrms-web:uat -t dsg-hrms-web:$VERSION \
       --label org.opencontainers.image.revision=$COMMIT --label org.opencontainers.image.version=$VERSION src/frontend/dsg-hrms-web"

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

# Calisan konteynerler gercekten bu commit'ten mi? Eski bir imaj ayakta kaldiysa
# kayit yanlis bir surumu gosterirdi.
for c in dsg-hrms-uat-api dsg-hrms-uat-web; do
  running=$(ssh "$SERVER" "docker inspect --format '{{index .Config.Labels \"org.opencontainers.image.revision\"}}' $c")
  [[ "$running" == "$COMMIT" ]] || fail "$c dagitilan commit'i calistirmiyor (calisan: ${running:-etiketsiz})"
done

info 'Dagitim kaydediliyor'
DEPLOYED_AT=$(date -u +%Y-%m-%dT%H:%M:%SZ)
# Sunucudaki kayit: sunucuya bakan herkes ne zaman neyin kuruldugunu gorur.
ssh "$SERVER" "printf '%s\t%s\t%s\t%s\n' '$DEPLOYED_AT' '$VERSION' '$COMMIT' '$DEPLOYER' >> $APP_DIR/deployments.log \
  && printf '%s %s\n' '$VERSION' '$COMMIT' > $APP_DIR/VERSION"
# Depodaki kayit: PR ile commit edilir; boylece dagitim gecmisi kalici kanittir.
RECORD=docs/33061/TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md
printf '| %s | %s | `%s` | %s |\n' "$DEPLOYED_AT" "$VERSION" "${COMMIT:0:7}" "$DEPLOYER" >> "$RECORD"
echo "  $RECORD dosyasina satir eklendi; PR ile commit edin."

printf '\n\033[1;32mDagitim tamam.\033[0m %s (%s)\n' "$BASE_URL" "$VERSION"
ssh "$SERVER" 'docker ps --format "  {{.Names}}\t{{.Status}}"'
