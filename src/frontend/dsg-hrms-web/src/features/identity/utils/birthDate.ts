/** Secilebilecek en eski dogum tarihi. */
export const MIN_BIRTH_DATE = new Date(1900, 0, 1);

/**
 * Tarihi sunucunun bekledigi "YYYY-AA-GG" bicimine cevirir (yerel takvim gunu).
 *
 * `toISOString` KULLANILMAZ: UTC'ye cevirir ve Turkiye saatinde gece yarisi secilen
 * tarih bir gun geriye kayardi (12.04.1985 -> 1985-04-11).
 */
export function toIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${date.getFullYear()}-${month}-${day}`;
}
