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
#   4. Veritabanini baslatir; semayi idempotent betikle, sonra uygulamanin calisma
#      rolunu uygular (#244, B-01)
#   5. Yigini baslatir ve DOGRULAR; calisan imajin commit'i dagitilanla ayni olmali,
#      uygulama sahip olmayan calisma roluyle baglanmis olmali
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
# Dagitilan commit main'in atasi olmali (#391, YB-04): 21.10-09.11.2026 arasinda UAT'de PR #258'in
# dal commit'i (e488898) calisti; kodu birlesen commit'le ayniydi ama kayit main'de olmayan bir
# commit'i gosteriyordu. Dal commit'i yalnizca bilerek kurulur ve kayda "(dal)" olarak gecer.
git fetch -q origin main || fail "origin/main alinamadi; main atasi denetimi yapilamaz."
BRANCH_NOTE=''
if ! git merge-base --is-ancestor "$COMMIT" origin/main; then
  [[ "${ALLOW_NON_MAIN:-}" == '1' ]] \
    || fail "$COMMIT main'in atasi degil. Once PR'i birlestirip main'den kurun. Dal commit'i bilerek kurulacaksa: ALLOW_NON_MAIN=1 ./docker/deploy-uat.sh"
  BRANCH_NOTE=' (dal)'
  echo '  Uyari: main disi commit kuruluyor (ALLOW_NON_MAIN=1); kayda "(dal)" olarak yazilacak.'
fi
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
# Calisma rolunun parolasi (#244, B-01). Yoksa yigin baslamaz; once runbook §2.2.1.
ssh "$SERVER" "grep -q '^HRMS_APP_DB_PASSWORD=' $SECRETS_FILE" \
  || fail "$SECRETS_FILE icinde HRMS_APP_DB_PASSWORD yok. Runbook §2.2.1'i uygulayin."
# Sir dizininde .env.uat'tan baska dosya olmamali (A6; #391, D-09). 13.10'da (#207) ve 03.11'de
# (A-01) kayit disi bir sir kopyasi bulundu; ikisi de elle denetimde, gunler sonra goruldu.
# Yalnizca dosya ADLARI yazilir, icerik okunmaz.
extra_secrets=$(ssh "$SERVER" "ls -A $(dirname "$SECRETS_FILE") | grep -vx '.env.uat' || true")
[[ -z "$extra_secrets" ]] \
  || fail "Sir dizininde .env.uat disinda dosya var: $(tr '\n' ' ' <<<"$extra_secrets"). Kayit disi sir kopyasidir; silin ya da gerekcesini bilgi kayit defterine yazin (runbook §2.2)."

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

info 'Veritabani baslatiliyor'
# Once yalnizca veritabani: sema ve calisma rolu hazir olmadan uygulama baslamaz.
ssh "$SERVER" "cd $APP_DIR/docker && docker compose -f compose.uat.yml --env-file $SECRETS_FILE up -d --wait postgres"

info 'Sema uygulaniyor'
# Veritabaninin hazir olmasi beklenir; aksi hâlde ilk dagitimda yaris olusur.
ssh "$SERVER" "cd $APP_DIR/docker && set -a && . $SECRETS_FILE && set +a \
  && for i in \$(seq 1 30); do docker exec dsg-hrms-uat-postgres pg_isready -U \"\$POSTGRES_USER\" -q && break; sleep 2; done \
  && docker exec -i dsg-hrms-uat-postgres psql -U \"\$POSTGRES_USER\" -d \"\$POSTGRES_DB\" -v ON_ERROR_STOP=1 < $APP_DIR/sema.sql > /dev/null \
  && echo 'sema uygulandi'"

info 'Uygulama rolu uygulaniyor'
# Sema'dan SONRA: betik var olan semalarin tablolarina yetki verir (#244, B-01; KR-118).
ssh "$SERVER" "cd $APP_DIR/docker && set -a && . $SECRETS_FILE && set +a \
  && bash postgres/apply-app-role.sh dsg-hrms-uat-postgres"

info 'Yigin baslatiliyor'
# A6'daki gunluk denetimi bu andan sonraki kayitlara bakar.
STACK_STARTED=$(date -u +%Y-%m-%dT%H:%M:%SZ)
ssh "$SERVER" "cd $APP_DIR/docker && docker compose -f compose.uat.yml --env-file $SECRETS_FILE up -d"

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

# Uygulama sahip olmayan calisma roluyle mi bagli? (#244, B-01; R-31) Parola yazdirilmaz;
# yalnizca kullanici adi okunur.
app_user=$(ssh "$SERVER" "docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' dsg-hrms-uat-api \
  | grep '^Database__Hrms=' | grep -o 'Username=[^;]*' | cut -d= -f2")
[[ -n "$app_user" && "$app_user" != 'postgres' ]] || fail "API calisma roluyle baglanmiyor (kullanici: ${app_user:-yok})"
role_flags=$(ssh "$SERVER" "cd $APP_DIR/docker && set -a && . $SECRETS_FILE && set +a \
  && docker exec dsg-hrms-uat-postgres psql -U \"\$POSTGRES_USER\" -d \"\$POSTGRES_DB\" -tAc \\
     \"SELECT rolsuper::text || ',' || (SELECT count(*) FROM pg_stat_activity WHERE usename = '$app_user')::text FROM pg_roles WHERE rolname = '$app_user'\"")
[[ "${role_flags%%,*}" == 'false' ]] || fail "Calisma rolu super kullanici veya yok ($app_user: ${role_flags:-yok})"
[[ "${role_flags##*,}" -gt 0 ]] || fail "API veritabanina $app_user ile baglanmamis"
echo "  Uygulama rolu: $app_user (super kullanici degil, ${role_flags##*,} baglanti)"

# Calisan konteynerler gercekten bu commit'ten mi? Eski bir imaj ayakta kaldiysa
# kayit yanlis bir surumu gosterirdi.
for c in dsg-hrms-uat-api dsg-hrms-uat-web; do
  running=$(ssh "$SERVER" "docker inspect --format '{{index .Config.Labels \"org.opencontainers.image.revision\"}}' $c")
  [[ "$running" == "$COMMIT" ]] || fail "$c dagitilan commit'i calistirmiyor (calisan: ${running:-etiketsiz})"
done

info 'Kurulum sonrasi kisa kontrol (A6)'
# Surum kontrol listesi A6 (KR-110) her kurulumda calisir; etiketsiz kurulum da dahil (#391).
# Onceden elle yapiliyordu ve yalnizca adaylarda zorunluydu; 21.10-09.11 arasindaki etiketsiz
# kurulumlarda kayit disi sir kopyasi ve main disi commit fark edilmedi. Calisan commit ve
# sir dizini yukarida denetlendi.

# Konteynerler saglikli.
unhealthy=$(ssh "$SERVER" "docker ps --filter name=dsg-hrms-uat --format '{{.Names}} {{.Status}}' | grep -v '(healthy)' || true")
[[ -z "$unhealthy" ]] || fail "Saglikli olmayan konteyner: $unhealthy"

# Saglik uclari: hazirlik, senkronizasyon ve bildirim. Yalnizca durum alani okunur.
for ep in ready sync notifications; do
  for i in $(seq 1 12); do
    ep_status=$(curl -s --max-time 10 "$BASE_URL/health/$ep" | grep -o '"status":"[A-Za-z]*"' | head -1 | cut -d'"' -f4 || true)
    [[ "$ep_status" == 'Healthy' ]] && break
    sleep 5
  done
  [[ "$ep_status" == 'Healthy' ]] || fail "/health/$ep saglikli degil (durum: ${ep_status:-yok})"
done

# Son migration depodakiyle ayni.
repo_migration=$(git ls-tree --name-only "$COMMIT" src/backend/Dsg.Hrms.Infrastructure/Data/Migrations/ \
  | xargs -n1 basename | grep -E '^[0-9]{14}_.*\.cs$' | grep -v '\.Designer\.cs$' | sort | tail -1 | sed 's/\.cs$//')
db_migration=$(ssh "$SERVER" "cd $APP_DIR/docker && set -a && . $SECRETS_FILE && set +a \
  && docker exec dsg-hrms-uat-postgres psql -U \"\$POSTGRES_USER\" -d \"\$POSTGRES_DB\" -tAc \
     'SELECT max(migration_id) FROM public.__ef_migrations_history'")
[[ -n "$repo_migration" && "$db_migration" == "$repo_migration" ]] \
  || fail "Son migration farkli (depo: ${repo_migration:-yok}, veritabani: ${db_migration:-yok})"

# Disariya acik portlar yalnizca 22, 80, 443 (TCP ve UDP; runbook §6).
open_ports=$(ssh "$SERVER" "ss -ltnupH" | awk '{print $5}' \
  | grep -vE '^(127\.|\[::1\]|[^ ]*%lo:)' | sed -E 's/.*:([0-9]+)$/\1/' | sort -un | tr '\n' ' ')
for port in $open_ports; do
  [[ "$port" =~ ^(22|80|443)$ ]] || fail "Disariya acik beklenmeyen port: $port (acik: $open_ports)"
done

# API gunlugunde yigin basladiktan sonra hata yok. Ileti icerigi yazdirilmaz, yalnizca sayilir.
log_errors=$(ssh "$SERVER" "docker logs --since $STACK_STARTED dsg-hrms-uat-api 2>&1 | grep -cE '\"Level\":\"(Error|Fatal)\"' || true")
[[ "${log_errors:-0}" -eq 0 ]] || fail "API gunlugunde $log_errors hata kaydi var (docker logs --since $STACK_STARTED dsg-hrms-uat-api)"

A6_RESULT="A6 ✓ sağlık 3/3, migration \`${repo_migration%%_*}\`, port $(tr ' ' '/' <<<"${open_ports% }"), sır dizini, günlük 0 hata"
echo "  $A6_RESULT"

info 'Dagitim kaydediliyor'
DEPLOYED_AT=$(date -u +%Y-%m-%dT%H:%M:%SZ)
# Sunucudaki kayit: sunucuya bakan herkes ne zaman neyin kuruldugunu gorur.
ssh "$SERVER" "printf '%s\t%s\t%s\t%s\t%s\n' '$DEPLOYED_AT' '$VERSION$BRANCH_NOTE' '$COMMIT' '$DEPLOYER' 'A6 ok' >> $APP_DIR/deployments.log \
  && printf '%s %s\n' '$VERSION' '$COMMIT' > $APP_DIR/VERSION"
# Depodaki kayit: PR ile commit edilir; boylece dagitim gecmisi kalici kanittir.
RECORD=docs/33061/TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md
printf '| %s | %s | `%s` | %s | %s |\n' "$DEPLOYED_AT" "$VERSION$BRANCH_NOTE" "${COMMIT:0:7}" "$DEPLOYER" "$A6_RESULT" >> "$RECORD"
echo "  $RECORD dosyasina satir eklendi; PR ile commit edin."

printf '\n\033[1;32mDagitim tamam.\033[0m %s (%s)\n' "$BASE_URL" "$VERSION"
ssh "$SERVER" 'docker ps --format "  {{.Names}}\t{{.Status}}"'
