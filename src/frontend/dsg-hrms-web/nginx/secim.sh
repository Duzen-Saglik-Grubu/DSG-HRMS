#!/bin/sh
#
# Yapilandirma secimi: sertifika varsa TLS, yoksa duz HTTP.
#
# Neden otomatik secim? Ayni imaj hem gelistirme (sertifikasiz) hem UAT/uretim
# (sertifikali) ortamda calisir. Iki ayri imaj uretmek, "uretimde test edilmemis
# imaj" riskini dogururdu.

set -eu

SERTIFIKA=/etc/nginx/tls/fullchain.pem
ANAHTAR=/etc/nginx/tls/privkey.pem
HEDEF=/etc/nginx/conf.d/default.conf

# Icerik ">" ile yazilir, "cp" ile degil: taban imajda default.conf zaten vardir
# ve busybox "cp" uzerine yazmak yerine hata verir.
if [ -r "$SERTIFIKA" ] && [ -r "$ANAHTAR" ]; then
    echo "[nginx] Sertifika bulundu - TLS etkin."
    cat /etc/nginx/sablon/tls.conf > "$HEDEF"
else
    echo "[nginx] Sertifika yok - duz HTTP ile calisiliyor."
    cat /etc/nginx/sablon/http.conf > "$HEDEF"
fi
