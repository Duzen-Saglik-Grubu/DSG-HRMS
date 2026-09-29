/**
 * Sunucunun saati (#103).
 *
 * Sunucu bazi anlari KENDI saatiyle verir (ornegin dogrulama kodunun bitis ani). Istemci
 * bunlari kendi saatiyle karsilastirirsa, iki saat arasindaki fark kadar yanlis sonuc cikar:
 * UAT sunucusunun saati 10 dakika geride oldugunda 5 dakikalik kod ekranda "suresi doldu"
 * gorunuyordu. Fark her yanitin `Date` basligindan olculur; sunucunun verdigi anlarla
 * yapilan hesaplar `serverNow()` kullanir.
 *
 * `Date` basligi saniye hassasiyetindedir; fark en fazla bir saniye sapar. Geri sayim icin
 * bu yeterlidir.
 */
let offsetMs = 0;

/** Yanitin `Date` basligini kaydeder. Baslik yoksa veya okunamazsa fark degismez. */
export function recordServerDate(header: unknown, receivedAt: number = Date.now()): void {
  if (typeof header !== 'string') {
    return;
  }

  const serverMs = Date.parse(header);
  if (!Number.isNaN(serverMs)) {
    offsetMs = serverMs - receivedAt;
  }
}

/** Sunucunun saatine gore simdiki an (ms). */
export function serverNow(): number {
  return Date.now() + offsetMs;
}

/** Testler icin: farki sifirlar. */
export function resetServerClock(): void {
  offsetMs = 0;
}
