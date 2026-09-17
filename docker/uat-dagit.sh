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

SUNUCU="${1:-root@192.168.3.202}"
DIZIN="/opt/dsg-hrms"
GIZLI="/opt/dsg-hrms/gizli/.env.uat"
ALAN_ADI="insankaynaklaritest.duzen.com.tr"
# Adres, sunucuda sertifika olup olmamasina gore belirlenir (asagida).
ADRES="http://${ALAN_ADI}"

bilgi() { printf '\n\033[1;34m==> %s\033[0m\n' "$1"; }
hata()  { printf '\n\033[1;31mHATA: %s\033[0m\n' "$1" >&2; exit 1; }

[[ -f docker/compose.uat.yml ]] || hata 'Bu betik depo kokunden calistirilmalidir.'

bilgi 'Ortam dosyasi kontrol ediliyor'
# Sir uretimi dagitimin isi degildir; yoksa kurulum adimlari uygulanmalidir.
ssh "$SUNUCU" "test -f $GIZLI" \
  || hata "Sunucuda $GIZLI yok. Runbook §2.2'yi uygulayin."

bilgi 'Kaynak aktariliyor'
tar czf - \
    --exclude=node_modules --exclude=bin --exclude=obj --exclude=dist \
    --exclude=coverage --exclude=logs \
    src docker global.json assets \
  | ssh "$SUNUCU" "rm -rf $DIZIN/src $DIZIN/docker && mkdir -p $DIZIN && tar xzf - -C $DIZIN"

bilgi 'Sema betigi uretiliyor'
dotnet ef migrations script --idempotent \
  --project src/backend/Dsg.Hrms.Infrastructure \
  --startup-project src/backend/Dsg.Hrms.Api \
  --output /tmp/dsg-sema.sql

ssh "$SUNUCU" "cat > $DIZIN/sema.sql" < /tmp/dsg-sema.sql

bilgi 'Imajlar derleniyor (backend 10-20 dk surebilir)'
ssh "$SUNUCU" "cd $DIZIN \
  && docker build -f src/backend/Dsg.Hrms.Api/Dockerfile -t dsg-hrms-api:uat src/backend \
  && docker build -t dsg-hrms-web:uat src/frontend/dsg-hrms-web"

bilgi 'Yigin baslatiliyor'
ssh "$SUNUCU" "cd $DIZIN/docker && docker compose -f compose.uat.yml --env-file $GIZLI up -d"

bilgi 'Sema uygulaniyor'
# Veritabaninin hazir olmasi beklenir; aksi hâlde ilk dagitimda yaris olusur.
ssh "$SUNUCU" "cd $DIZIN/docker && set -a && . $GIZLI && set +a \
  && for i in \$(seq 1 30); do docker exec dsg-hrms-uat-postgres pg_isready -U \"\$POSTGRES_USER\" -q && break; sleep 2; done \
  && docker exec -i dsg-hrms-uat-postgres psql -U \"\$POSTGRES_USER\" -d \"\$POSTGRES_DB\" -v ON_ERROR_STOP=1 < $DIZIN/sema.sql > /dev/null \
  && echo 'sema uygulandi'"

bilgi 'Dogrulaniyor'

# Sertifika varsa yigin TLS ile calisir; duz HTTP artik 200 degil 301 doner.
# Bu ayrim yapilmazsa dogrulama, calisan bir sistemi "basarisiz" ilan ederdi.
if ssh "$SUNUCU" "test -r $DIZIN/tls/fullchain.pem"; then
  TLS_ETKIN=evet
  ADRES="https://${ALAN_ADI}"
  echo "  TLS etkin; dogrulama HTTPS uzerinden yapilacak."
else
  TLS_ETKIN=hayir
  echo "  Sertifika yok; dogrulama duz HTTP uzerinden yapilacak."
fi

for i in $(seq 1 20); do
  durum=$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "$ADRES/" || true)
  [[ "$durum" == '200' ]] && break
  sleep 5
done

[[ "${durum:-}" == '200' ]] || hata "Web arayuzu yanit vermiyor (son durum: ${durum:-yok})"

saglik=$(curl -s --max-time 10 "$ADRES/health/ready" || true)
grep -q '"status":"Healthy"' <<<"$saglik" || hata "Saglik kontrolu basarisiz: $saglik"

# TLS etkinse: duz HTTP yonlendirmeli ve sertifika GECERLI olmali.
#
# "curl -k" KULLANILMAZ: dogrulamayi kapatmak, sertifika hatali oldugunda bile
# testin gecmesi demektir - yani kontrolun kendisini islevsiz kilar.
if [[ "$TLS_ETKIN" == 'evet' ]]; then
  yonlendirme=$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "http://${ALAN_ADI}/" || true)
  [[ "$yonlendirme" == '301' ]] || hata "HTTP, HTTPS'e yonlendirmiyor (durum: ${yonlendirme:-yok})"

  hedef=$(curl -s -o /dev/null -w '%{redirect_url}' --max-time 5 "http://${ALAN_ADI}/" || true)
  [[ "$hedef" == https://* ]] || hata "Yonlendirme HTTPS'e gitmiyor: ${hedef:-yok}"
fi

# API ve veritabani DISARIYA KAPALI olmali (runbook §1).
sunucu_ip="${SUNUCU#*@}"
disari=$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "http://$sunucu_ip:5299/health/live" || true)
[[ "$disari" == '000' ]] || hata "API disariya acik gorunuyor (durum: $disari)"

printf '\n\033[1;32mDagitim tamam.\033[0m %s\n' "$ADRES"
ssh "$SUNUCU" 'docker ps --format "  {{.Names}}\t{{.Status}}"'
