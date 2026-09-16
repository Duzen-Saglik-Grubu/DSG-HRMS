# DSG-HRMS web imaji
#
# Derleme ciktisi STATIK dosyalardir; calisma zamaninda Node gerekmez.
# Nihai imajda yalnizca nginx ve uretilmis dosyalar bulunur.

# ---------- 1. Asama: derleme ----------
FROM node:22-alpine AS derleme
WORKDIR /kaynak

# "npm ci" package-lock.json'a birebir uyar; imaj her derlemede AYNI surumleri
# icerir. "npm install" surum kaydirmasi yapabilirdi.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---------- 2. Asama: sunum ----------
FROM nginx:1.29-alpine AS calisma

# Taban imajdaki paketler guncellenir.
#
# Resmi imaj etiketi, yayimlandigi andaki paket surumleriyle dondurulur; aradan
# gecen surede cikan yamalar imajda YOKTUR. Konteyner taramasi bu imajda 36
# yuksek onemli acik buldu ve hepsinin yamasi mevcuttu.
#
# Uygulama kodumuz temiz olsa bile taban imajdaki bir acik sistemi savunmasiz
# birakir; bu satir CI kalite kapisinin (ADR-0011 §6) gereğidir.
RUN apk upgrade --no-cache

COPY --from=derleme /kaynak/dist /usr/share/nginx/html

# Iki yapilandirma da imaja girer; hangisinin kullanilacagina acilista karar
# verilir (nginx/secim.sh). Boylece gelistirme ve uretim AYNI imaji kullanir.
COPY nginx/http.conf nginx/tls.conf /etc/nginx/sablon/
COPY nginx/guvenlik-basliklari.conf /etc/nginx/guvenlik-basliklari.conf
COPY nginx/secim.sh /docker-entrypoint.d/40-yapilandirma-secimi.sh

# nginx imaji root olmayan bir kullaniciyla gelir; onu kullaniriz.
# Calisma dizinlerinin sahipligi degistirilerek 8080 portunda (ayricaliksiz
# port) dinlemesi saglanir.
RUN touch /var/run/nginx.pid \
    && chmod +x /docker-entrypoint.d/40-yapilandirma-secimi.sh \
    && chown -R nginx:nginx /var/run/nginx.pid /var/cache/nginx /usr/share/nginx/html \
    && chown -R nginx:nginx /etc/nginx/conf.d

USER nginx

EXPOSE 8080 8443

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://127.0.0.1:8080/nginx-saglik || exit 1
