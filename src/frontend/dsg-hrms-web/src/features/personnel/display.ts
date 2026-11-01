/** Iki parcayi " / " ile birlestirir; ikisi de yoksa bos isareti. */
export function joinParts(first: string | null, second: string | null): string {
  const parts = [first, second].filter((part): part is string => Boolean(part?.trim()));
  return parts.length > 0 ? parts.join(' / ') : '—';
}

/** Tarihi gun.ay.yil bicimine cevirir; tarih yoksa bos isareti. */
export function displayDate(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }

  const [year, month, day] = value.slice(0, 10).split('-');
  return year && month && day ? `${day}.${month}.${year}` : value;
}

/**
 * Istihdam sekmelerinin sirasi (SYG-PERS-017): once aktifler, kendi icinde ise giris tarihi
 * yeniden eskiye.
 */
export function orderEmployments<T extends { isActive: boolean; hireDate: string }>(
  employments: readonly T[],
): T[] {
  return [...employments].sort(
    (a, b) => Number(b.isActive) - Number(a.isActive) || b.hireDate.localeCompare(a.hireDate),
  );
}
