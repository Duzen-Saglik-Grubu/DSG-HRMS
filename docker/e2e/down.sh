#!/usr/bin/env bash
#
# Uctan uca test yiginini ve verisini siler (#146). Yigin atilabilirdir; icinde gercek veri
# yoktur.
#
# "-v" sarttir (#391, D-15): compose dosyasinda birim tanimli olmasa da PostgreSQL imaji kendi
# VOLUME'unu tanimlar ve her kurulumda ANONIM bir birim olusur. "-v" olmadan "down" bu birimi
# birakir; yerelde 66, UAT'de bir sahipsiz PostgreSQL birimi bu yolla birikti.

set -euo pipefail

E2E_CODE_HASH_KEY=x E2E_JWT_SIGNING_KEY=x E2E_PARAMETER_PROTECTION_KEY=x E2E_PERSONAL_DATA_KEY=x \
  docker compose -f docker/compose.e2e.yml down -v --remove-orphans
