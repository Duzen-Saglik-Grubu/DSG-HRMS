# DSG-HRMS API imaji
#
# Cok asamali derleme: derleme araclari (SDK, ~800 MB) nihai imaja GIRMEZ.
# Uretim imajinda yalnizca calisma zamani ve uygulama dosyalari bulunur; daha
# kucuk imaj, daha az saldiri yuzeyi demektir.

# ---------- 1. Asama: derleme ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Once yalnizca proje dosyalari kopyalanir ve paketler geri yuklenir.
# Kaynak kodu degistiginde bu katman ONBELEKTEN gelir; derleme suresi kisalir.
COPY Directory.Build.props Directory.Packages.props Dsg.Hrms.slnx ./
COPY Dsg.Hrms.Domain/*.csproj Dsg.Hrms.Domain/
COPY Dsg.Hrms.Application/*.csproj Dsg.Hrms.Application/
COPY Dsg.Hrms.Infrastructure/*.csproj Dsg.Hrms.Infrastructure/
COPY Dsg.Hrms.Api/*.csproj Dsg.Hrms.Api/
COPY tests/Dsg.Hrms.Domain.Tests/*.csproj tests/Dsg.Hrms.Domain.Tests/
COPY tests/Dsg.Hrms.Application.Tests/*.csproj tests/Dsg.Hrms.Application.Tests/
COPY tests/Dsg.Hrms.Infrastructure.Tests/*.csproj tests/Dsg.Hrms.Infrastructure.Tests/
COPY tests/Dsg.Hrms.Architecture.Tests/*.csproj tests/Dsg.Hrms.Architecture.Tests/
COPY tests/Dsg.Hrms.Api.IntegrationTests/*.csproj tests/Dsg.Hrms.Api.IntegrationTests/

RUN dotnet restore Dsg.Hrms.Api/Dsg.Hrms.Api.csproj

COPY . .

# Uyarilar hata sayilir: imaj uretimi de kalite kapisidir.
RUN dotnet publish Dsg.Hrms.Api/Dsg.Hrms.Api.csproj \
    --no-restore \
    --configuration Release \
    --output /app \
    -warnaserror

# ---------- 2. Asama: calisma zamani ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime

# Turkce kultur ve ICU gereklidir (ad siralamasi, tarih bicimi).
# InvariantGlobalization kapali oldugu icin ICU kutuphaneleri imajda bulunmalidir.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=Europe/Istanbul \
    ASPNETCORE_HTTP_PORTS=8080

# Saglik kontrolu icin curl.
#
# .NET calisma zamani imaji bilincli olarak minimaldir; wget ve curl bulunmaz.
# Saglik kontrolu olmadan kapsayici duzeni, sureci ayakta ama ISLEVSIZ bir
# konteyneri "calisiyor" sayardi.
#
# Alternatifler tartildi: bash /dev/tcp ile port yoklamak yalnizca portun acik
# oldugunu soyler (uygulama saglikli olmayabilir); ayri bir saglik ikili dosyasi
# yazmak ise bakim maliyeti uretirdi. curl ~1 MB ve tek amacli kullanilir.
#
# Guvenlik yukseltmesi: taban imaj yeniden yayimlanmadan once Ubuntu'nun yayimladigi
# guvenlik duzeltmeleri de imaja girer. Onceki surum yukseltme yapmiyordu; 30.09.2026'da
# tarama taban imajdaki libssl3t64'te duzeltmesi yayimlanmis bir acik (CVE-2026-84782,
# HIGH) buldu ve derleme durdu (#107).
RUN apt-get update \
    && apt-get upgrade --no-install-recommends --yes \
    && apt-get install --no-install-recommends --yes curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .

# Gunluk dosyalari icin yazilabilir dizin (ADR-0009 §1).
RUN mkdir -p /app/logs

# ROOT OLMAYAN KULLANICI.
# Konteyner icinde root calismak, bir guvenlik acigi durumunda saldirganin
# elindeki yetkiyi buyutur. .NET imajlari "app" kullanicisiyla gelir.
RUN chown -R app:app /app
USER app

EXPOSE 8080

# Canlilik ucu bagimlilik yoklamaz (KR-063): veritabani birkac saniye
# erisilemez oldugunda konteyner YENIDEN BASLATILMAZ.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Dsg.Hrms.Api.dll"]
