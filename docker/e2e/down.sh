#!/usr/bin/env bash
#
# Uctan uca test yiginini ve verisini siler (#146). Yigin atilabilirdir; icinde gercek veri
# yoktur. Volume tanimli olmadigi icin veritabani konteynerle birlikte gider.

set -euo pipefail

E2E_CODE_HASH_KEY=x E2E_JWT_SIGNING_KEY=x E2E_PARAMETER_PROTECTION_KEY=x \
  docker compose -f docker/compose.e2e.yml down --remove-orphans
