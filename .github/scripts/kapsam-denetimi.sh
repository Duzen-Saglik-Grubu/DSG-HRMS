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

rapor="${1:?Cobertura raporu yolu verilmedi}"

GENEL_ESIK=75
DOMAIN_ESIK=90

if [[ ! -f "$rapor" ]]; then
  echo "::error::Kapsam raporu bulunamadi: $rapor"
  exit 1
fi

# Cobertura 'line-rate' degeri 0-1 arasindadir; yuzdeye cevrilir.
oran_yuzdeye() {
  awk -v deger="$1" 'BEGIN { printf "%.1f", deger * 100 }'
}

genel_oran=$(grep -m1 -o 'line-rate="[0-9.]*"' "$rapor" | head -1 | sed 's/line-rate="//; s/"//')
genel=$(oran_yuzdeye "$genel_oran")

echo "Genel kapsam       : %${genel} (esik: %${GENEL_ESIK})"

basarisiz=0

if awk -v g="$genel" -v e="$GENEL_ESIK" 'BEGIN { exit !(g < e) }'; then
  echo "::error::Genel kod kapsami esigin altinda: %${genel} < %${GENEL_ESIK}"
  basarisiz=1
fi

# Domain katmani ayri esige tabidir: is kurallari orada yasar ve hatalari
# en pahali olan kod parcasidir.
domain_oran=$(grep -o '<package name="Dsg.Hrms.Domain" line-rate="[0-9.]*"' "$rapor" \
  | head -1 | sed 's/.*line-rate="//; s/"//' || true)

if [[ -z "$domain_oran" ]]; then
  # Domain su an yalnizca otomatik ozelliklerden olusuyor; olculecek satir yok.
  # Ilk is kurali eklendiginde bu dal kendiliginden devre disi kalir.
  echo "Domain kapsami     : olculecek kod yok (atlandi)"
else
  domain=$(oran_yuzdeye "$domain_oran")
  echo "Domain kapsami     : %${domain} (esik: %${DOMAIN_ESIK})"

  if awk -v d="$domain" -v e="$DOMAIN_ESIK" 'BEGIN { exit !(d < e) }'; then
    echo "::error::Domain kod kapsami esigin altinda: %${domain} < %${DOMAIN_ESIK}"
    basarisiz=1
  fi
fi

if [[ "$basarisiz" -ne 0 ]]; then
  echo ''
  echo 'Kapsam kapisi UYARI DEGIL, ENGELDIR (ADR-0011 §6).'
  echo 'Eksik testleri yazin; esigi dusurmek icin gerekce ve duzeltici faaliyet gerekir.'
  exit 1
fi

echo 'Kapsam esikleri saglandi.'
