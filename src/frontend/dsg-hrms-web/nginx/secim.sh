#!/bin/sh
#
# Yapilandirma secimi: sertifika varsa TLS, yoksa duz HTTP.
#
# Neden otomatik secim? Ayni imaj hem gelistirme (sertifikasiz) hem UAT/uretim
# (sertifikali) ortamda calisir. Iki ayri imaj uretmek, "uretimde test edilmemis
# imaj" riskini dogururdu.

set -eu

CERT_FILE=/etc/nginx/tls/fullchain.pem
KEY_FILE=/etc/nginx/tls/privkey.pem
TARGET_CONF=/etc/nginx/conf.d/default.conf

# Icerik ">" ile yazilir, "cp" ile degil: taban imajda default.conf zaten vardir
# ve busybox "cp" uzerine yazmak yerine hata verir.
if [ -r "$CERT_FILE" ] && [ -r "$KEY_FILE" ]; then
    echo "[nginx] Sertifika bulundu - TLS etkin."
    cat /etc/nginx/variants/tls.conf > "$TARGET_CONF"
else
    echo "[nginx] Sertifika yok - duz HTTP ile calisiliyor."
    cat /etc/nginx/variants/http.conf > "$TARGET_CONF"
fi
