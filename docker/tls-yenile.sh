#!/usr/bin/env bash
#
# Sertifika yenileme sonrasi calisan kanca (certbot --deploy-hook).
#
# Certbot sertifikalari /etc/letsencrypt altinda root'a ait ve kisitli izinlerle
# tutar. Web konteyneri root OLMAYAN bir kullaniciyla calistigi icin (KR-065)
# dosyalar okunabilir izinlerle ayri bir dizine kopyalanir; boylece konteynere
# /etc/letsencrypt'in tamami baglanmak zorunda kalmaz.

set -euo pipefail

DOMAIN="${DOMAIN:-insankaynaklaritest.duzen.com.tr}"
SOURCE_DIR="/etc/letsencrypt/live/${DOMAIN}"
TARGET_DIR="/opt/dsg-hrms/tls"
# nginx imajindaki "nginx" kullanicisinin kimligi.
NGINX_UID=101

[[ -r "$SOURCE_DIR/fullchain.pem" ]] || { echo "Sertifika bulunamadi: $SOURCE_DIR" >&2; exit 1; }

mkdir -p "$TARGET_DIR"
install -o "$NGINX_UID" -g "$NGINX_UID" -m 644 "$SOURCE_DIR/fullchain.pem" "$TARGET_DIR/fullchain.pem"
install -o "$NGINX_UID" -g "$NGINX_UID" -m 600 "$SOURCE_DIR/privkey.pem"   "$TARGET_DIR/privkey.pem"

echo "Sertifikalar kopyalandi: $TARGET_DIR"

# Web konteyneri yeniden baslatilir; nginx sertifikayi acilista okur.
if docker ps --format '{{.Names}}' | grep -q '^dsg-hrms-uat-web$'; then
    docker restart dsg-hrms-uat-web > /dev/null
    echo "Web konteyneri yeniden baslatildi."
fi
