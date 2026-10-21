#!/usr/bin/env bash
#
# Uygulamanin calisma rolunu kurar veya gunceller (#244, B-01; app-role.sql).
#
# Kullanim (ortam degiskenleri sir dosyasindan yuklenmis olarak):
#   bash docker/postgres/apply-app-role.sh <postgres-konteyneri>
#
# Gerekli ortam degiskenleri:
#   POSTGRES_USER, POSTGRES_DB  sahip rol ve veritabani (migration'lari uygulayan rol)
#   HRMS_APP_DB_PASSWORD        calisma rolunun parolasi (sir dosyasinda; en az 24 harf/rakam)
#   HRMS_APP_DB_USER            calisma rolunun adi (varsayilan: hrms_app)
#
# Parola komut satirina YAZILMAZ: psql'e standart girdiden \set ile verilir. Komut
# satirindaki deger sunucudaki process listesinde (ps) gorunurdu.

set -euo pipefail

container="${1:?Kullanim: apply-app-role.sh <postgres-konteyneri>}"
: "${POSTGRES_USER:?POSTGRES_USER tanimli olmalidir}"
: "${POSTGRES_DB:?POSTGRES_DB tanimli olmalidir}"
: "${HRMS_APP_DB_PASSWORD:?HRMS_APP_DB_PASSWORD tanimli olmalidir (runbook §2.2)}"
role="${HRMS_APP_DB_USER:-hrms_app}"

# Deger psql \set satirina yazildigi icin bosluk, tirnak ve ters bolu iceremez.
[[ "$HRMS_APP_DB_PASSWORD" =~ ^[A-Za-z0-9]{24,}$ ]] \
  || { echo 'HRMS_APP_DB_PASSWORD en az 24 karakter olmali ve yalnizca harf ve rakam icermelidir (openssl rand -hex 32).' >&2; exit 1; }
[[ "$role" =~ ^[a-z_][a-z0-9_]*$ ]] || { echo "Gecersiz rol adi: $role" >&2; exit 1; }

dir="$(cd "$(dirname "$0")" && pwd)"
{ printf '\\set app_role %s\n\\set app_password %s\n' "$role" "$HRMS_APP_DB_PASSWORD"; cat "$dir/app-role.sql"; } \
  | docker exec -i "$container" psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 -q

echo "Calisma rolu hazir: $role"
