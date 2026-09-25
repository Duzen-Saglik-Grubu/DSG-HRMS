#!/usr/bin/env bash
#
# Kod kapsami esik denetimi (ADR-0011 §1, §6).
#
# Esikler bir TABANDIR, hedef degil. Yuksek kapsamli ama zayif test, dusuk
# kapsamli iyi testten kotudur; testin NEYI dogruladigina kod incelemesinde
# bakilir. Bu betik yalnizca tabanin altina dusulmedigini garanti eder.
#
# Kullanim: kapsam-denetimi.sh <cobertura.xml>

set -euo pipefail

report="${1:?Cobertura raporu yolu verilmedi}"

OVERALL_THRESHOLD=75
DOMAIN_THRESHOLD=90

if [[ ! -f "$report" ]]; then
  echo "::error::Kapsam raporu bulunamadi: $report"
  exit 1
fi

# Cobertura 'line-rate' degeri 0-1 arasindadir; yuzdeye cevrilir.
rate_to_percent() {
  awk -v rate="$1" 'BEGIN { printf "%.1f", rate * 100 }'
}

overall_rate=$(grep -m1 -o 'line-rate="[0-9.]*"' "$report" | head -1 | sed 's/line-rate="//; s/"//')
overall=$(rate_to_percent "$overall_rate")

echo "Genel kapsam       : %${overall} (esik: %${OVERALL_THRESHOLD})"

failed=0

if awk -v o="$overall" -v t="$OVERALL_THRESHOLD" 'BEGIN { exit !(o < t) }'; then
  echo "::error::Genel kod kapsami esigin altinda: %${overall} < %${OVERALL_THRESHOLD}"
  failed=1
fi

# Domain katmani ayri esige tabidir: is kurallari orada yasar ve hatalari
# en pahali olan kod parcasidir.
domain_rate=$(grep -o '<package name="Dsg.Hrms.Domain" line-rate="[0-9.]*"' "$report" \
  | head -1 | sed 's/.*line-rate="//; s/"//' || true)

if [[ -z "$domain_rate" ]]; then
  # Domain su an yalnizca otomatik ozelliklerden olusuyor; olculecek satir yok.
  # Ilk is kurali eklendiginde bu dal kendiliginden devre disi kalir.
  echo "Domain kapsami     : olculecek kod yok (atlandi)"
else
  domain=$(rate_to_percent "$domain_rate")
  echo "Domain kapsami     : %${domain} (esik: %${DOMAIN_THRESHOLD})"

  if awk -v d="$domain" -v t="$DOMAIN_THRESHOLD" 'BEGIN { exit !(d < t) }'; then
    echo "::error::Domain kod kapsami esigin altinda: %${domain} < %${DOMAIN_THRESHOLD}"
    failed=1
  fi
fi

if [[ "$failed" -ne 0 ]]; then
  echo ''
  echo 'Kapsam kapisi UYARI DEGIL, ENGELDIR (ADR-0011 §6).'
  echo 'Eksik testleri yazin; esigi dusurmek icin gerekce ve duzeltici faaliyet gerekir.'
  exit 1
fi

echo 'Kapsam esikleri saglandi.'
