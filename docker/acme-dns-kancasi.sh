#!/usr/bin/env bash
#
# Let's Encrypt DNS-01 dogrulama kancasi.
#
# Certbot bu betigi cagirdiginda gereken TXT degerini dosyaya yazar ve kaydin
# INTERNETTEN gorunur hale gelmesini bekler.
#
# Neden DNS-over-HTTPS? Sunucudan disariya 53/udp KAPALI; dis DNS sunucularina
# dogrudan sorgu gonderilemiyor. Dogrulama HTTPS uzerinden yapilir.
#
# Neden IKI ayri cozucu? duzen.com.tr'nin iki yetkili sunucusu vardir
# (ankara 212.57.13.19, cayyolu 212.156.67.66) ve bunlar FARKLI icerik
# sunabiliyor. Let's Encrypt dogrulamayi birden fazla noktadan yapar; kayit
# sunuculardan yalnizca birinde varsa dogrulama "secondary validation" asamasinda
# NXDOMAIN ile duser. Bu bir kez yasandi (2026-09-16). Tek cozucuye bakan kancasi
# "kayit gorundu" deyip certbot'u bos yere ateslemisti.
#
# Tek cozucu tutarliligi garanti etmez, ancak iki bagimsiz cozucunun ayni cevabi
# vermesi, kaydin sunuculara YAYILDIGINA dair cok daha guclu bir kanittir.
#
# Certbot ortam degiskenleri:
#   CERTBOT_DOMAIN     -> insankaynaklaritest.duzen.com.tr
#   CERTBOT_VALIDATION -> DNS'e yazilacak TXT degeri

set -uo pipefail

RECORD_NAME="_acme-challenge.${CERTBOT_DOMAIN}"
STATUS_FILE=/opt/acme/expected-record.txt
TIMEOUT_SECONDS=43200   # 12 saat - kayit ayri bir ekipten geciyor
POLL_INTERVAL=30

mkdir -p /opt/acme

{
  echo "Ad   : ${RECORD_NAME}"
  echo "Tur  : TXT"
  echo "Deger: ${CERTBOT_VALIDATION}"
} > "$STATUS_FILE"

echo "[acme] DNS kaydi bekleniyor:"
cat "$STATUS_FILE"

# Tek bir cozucude kayit var mi? 0 = var, 1 = yok.
query_resolver() {
  local url="$1"
  local response
  response=$(curl -s --max-time 10 -H 'accept: application/dns-json' "$url" || true)
  printf '%s' "$response" | grep -q "$CERTBOT_VALIDATION"
}

elapsed=0

while [ "$elapsed" -lt "$TIMEOUT_SECONDS" ]; do
  cloudflare=false
  google=false

  query_resolver "https://cloudflare-dns.com/dns-query?name=${RECORD_NAME}&type=TXT" && cloudflare=true
  query_resolver "https://dns.google/resolve?name=${RECORD_NAME}&type=TXT"           && google=true

  if [ "$cloudflare" = true ] && [ "$google" = true ]; then
    echo "[acme] Kayit her iki cozucude de gorundu (${elapsed} saniye sonra)."
    echo "[acme] Yayilmanin tamamlanmasi icin 60 saniye bekleniyor."
    sleep 60
    exit 0
  fi

  # Kismi gorunurluk, yayilmanin surdugu anlamina gelebilir; ancak kalici hale
  # gelirse sunucular arasi TUTARSIZLIK vardir (seri numarasi artirilmadigi icin
  # bolge aktarimi yapilmamis olabilir). Kayda gecirilir ki tani kolay olsun.
  if [ "$cloudflare" != "$google" ]; then
    echo "[acme] UYARI (${elapsed}. saniye): cozuculer AYRISIYOR - cloudflare=${cloudflare} google=${google}."
    echo "[acme] Kayit yetkili sunuculardan yalnizca birinde olabilir; SOA seri numarasi artirilmali."
  fi

  sleep "$POLL_INTERVAL"
  elapsed=$((elapsed + POLL_INTERVAL))
done

echo "[acme] ZAMAN ASIMI: TXT kaydi ${TIMEOUT_SECONDS} saniye icinde iki cozucude birden gorunmedi." >&2
exit 1
