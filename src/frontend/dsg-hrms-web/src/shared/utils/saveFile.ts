/** Gecici adresin geri alinmasi icin bekleme; bazi tarayicilar indirmeyi tiklamadan sonra baslatir. */
const REVOKE_DELAY_MS = 30_000;

/**
 * Indirilen dosyayi tarayiciya kaydettirir. Gecici adres kisa sure sonra geri alinir; dosya
 * bellekte gereginden uzun kalmaz.
 */
export function saveFile(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.style.display = 'none';
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), REVOKE_DELAY_MS);
}
