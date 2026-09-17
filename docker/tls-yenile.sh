#!/usr/bin/env bash
#
# Sertifika yenileme sonrasi calisan kanca (certbot --deploy-hook).
#
# Certbot sertifikalari /etc/letsencrypt altinda root'a ait ve kisitli izinlerle
# tutar. Web konteyneri root OLMAYAN bir kullaniciyla calistigi icin (KR-065)
# dosyalar okunabilir izinlerle ayri bir dizine kopyalanir; boylece konteynere
# /etc/letsencrypt'in tamami baglanmak zorunda kalmaz.

set -euo pipefail

ALAN_ADI="${ALAN_ADI:-insankaynaklaritest.duzen.com.tr}"
KAYNAK="/etc/letsencrypt/live/${ALAN_ADI}"
HEDEF="/opt/dsg-hrms/tls"
# nginx imajindaki "nginx" kullanicisinin kimligi.
NGINX_UID=101

[[ -r "$KAYNAK/fullchain.pem" ]] || { echo "Sertifika bulunamadi: $KAYNAK" >&2; exit 1; }

mkdir -p "$HEDEF"
install -o "$NGINX_UID" -g "$NGINX_UID" -m 644 "$KAYNAK/fullchain.pem" "$HEDEF/fullchain.pem"
install -o "$NGINX_UID" -g "$NGINX_UID" -m 600 "$KAYNAK/privkey.pem"   "$HEDEF/privkey.pem"

echo "Sertifikalar kopyalandi: $HEDEF"

# Web konteyneri yeniden baslatilir; nginx sertifikayi acilista okur.
if docker ps --format '{{.Names}}' | grep -q '^dsg-hrms-uat-web$'; then
    docker restart dsg-hrms-uat-web > /dev/null
    echo "Web konteyneri yeniden baslatildi."
fi
