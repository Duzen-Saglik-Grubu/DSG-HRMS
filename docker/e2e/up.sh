#!/usr/bin/env bash
#
# Uctan uca test yiginini kurar (#146). Depo kokunde calistirilir; CI de bunu kullanir.
#
#   1. PostgreSQL ve Mailpit'i baslatir
#   2. Semayi idempotent betikle uygular (UAT ile ayni yol)
#   3. Sentetik veriyi yukler (docker/e2e/seed.sql)
#   4. API ve web'i derleyip baslatir; hazir olmalarini bekler
#
# Anahtarlar her calistirmada rastgele uretilir; yigin atilabilirdir.

set -euo pipefail

COMPOSE=(docker compose -f docker/compose.e2e.yml)
WORK="${RUNNER_TEMP:-${TMPDIR:-/tmp}}/dsg-hrms-e2e"
mkdir -p "$WORK"

key() { node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"; }
export E2E_CODE_HASH_KEY="${E2E_CODE_HASH_KEY:-$(key)}"
export E2E_JWT_SIGNING_KEY="${E2E_JWT_SIGNING_KEY:-$(key)}"
export E2E_PARAMETER_PROTECTION_KEY="${E2E_PARAMETER_PROTECTION_KEY:-$(key)}"

echo '==> Veritabani ve Mailpit'
"${COMPOSE[@]}" up -d --wait postgres mailpit

echo '==> Sema'
# Betik tasarim zamaninda uretilir; baglanti dizesi ve anahtar yalnizca sema uretimi icindir.
Database__Hrms='Host=e2e-design-time;Database=dsg_hrms;Username=e2e;Password=e2e' \
Identity__JwtSigningKey="$E2E_JWT_SIGNING_KEY" \
  dotnet ef migrations script --idempotent \
    --project src/backend/Dsg.Hrms.Infrastructure \
    --startup-project src/backend/Dsg.Hrms.Api \
    --context HrmsDbContext \
    --output "$WORK/schema.sql" > /dev/null
"${COMPOSE[@]}" exec -T postgres psql -U postgres -d dsg_hrms -v ON_ERROR_STOP=1 -q < "$WORK/schema.sql"

echo '==> Sentetik veri'
"${COMPOSE[@]}" exec -T postgres psql -U postgres -d dsg_hrms -v ON_ERROR_STOP=1 -q < docker/e2e/seed.sql

echo '==> API ve web'
"${COMPOSE[@]}" up -d --build --wait api web

for _ in $(seq 1 60); do
  if curl -fsS -o /dev/null http://localhost:8090/api/v1/identity/public-settings; then
    echo 'Yigin hazir: http://localhost:8090 (Mailpit: http://localhost:8025)'
    exit 0
  fi
  sleep 2
done

echo 'Yigin hazir olmadi.' >&2
"${COMPOSE[@]}" logs api | tail -50 >&2
exit 1
