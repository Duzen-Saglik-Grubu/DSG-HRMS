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

KAYIT="_acme-challenge.${CERTBOT_DOMAIN}"
DURUM_DOSYASI=/opt/acme/beklenen-kayit.txt
BEKLEME_SANIYE=43200   # 12 saat - kayit ayri bir ekipten geciyor
ARALIK=30

mkdir -p /opt/acme

{
  echo "Ad   : ${KAYIT}"
  echo "Tur  : TXT"
  echo "Deger: ${CERTBOT_VALIDATION}"
} > "$DURUM_DOSYASI"

echo "[acme] DNS kaydi bekleniyor:"
cat "$DURUM_DOSYASI"

# Tek bir cozucude kayit var mi? 0 = var, 1 = yok.
sorgula() {
  local adres="$1"
  local yanit
  yanit=$(curl -s --max-time 10 -H 'accept: application/dns-json' "$adres" || true)
  printf '%s' "$yanit" | grep -q "$CERTBOT_VALIDATION"
}

gecen=0

while [ "$gecen" -lt "$BEKLEME_SANIYE" ]; do
  cloudflare=hayir
  google=hayir

  sorgula "https://cloudflare-dns.com/dns-query?name=${KAYIT}&type=TXT" && cloudflare=evet
  sorgula "https://dns.google/resolve?name=${KAYIT}&type=TXT"           && google=evet

  if [ "$cloudflare" = evet ] && [ "$google" = evet ]; then
    echo "[acme] Kayit her iki cozucude de gorundu (${gecen} saniye sonra)."
    echo "[acme] Yayilmanin tamamlanmasi icin 60 saniye bekleniyor."
    sleep 60
    exit 0
  fi

  # Kismi gorunurluk, yayilmanin surdugu anlamina gelebilir; ancak kalici hale
  # gelirse sunucular arasi TUTARSIZLIK vardir (seri numarasi artirilmadigi icin
  # bolge aktarimi yapilmamis olabilir). Kayda gecirilir ki tani kolay olsun.
  if [ "$cloudflare" != "$google" ]; then
    echo "[acme] UYARI (${gecen}. saniye): cozuculer AYRISIYOR - cloudflare=${cloudflare} google=${google}."
    echo "[acme] Kayit yetkili sunuculardan yalnizca birinde olabilir; SOA seri numarasi artirilmali."
  fi

  sleep "$ARALIK"
  gecen=$((gecen + ARALIK))
done

echo "[acme] ZAMAN ASIMI: TXT kaydi ${BEKLEME_SANIYE} saniye icinde iki cozucude birden gorunmedi." >&2
exit 1
