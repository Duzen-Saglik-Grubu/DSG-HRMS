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
 * Istihdam sekmelerinin sirasi (SYG-PERS-017, KR-130): once aktifler ise giris tarihi eskiden
 * yeniye, sonra ayrilanlar cikis tarihi yeniden eskiye. Ilk sekme esas sicildir; sunucu da ayni
 * sirayla dondurur (Employment.InPrimaryOrder).
 */
export function orderEmployments<
  T extends {
    isActive: boolean;
    hireDate: string;
    terminationDate: string | null;
    registryCode: string;
  },
>(employments: readonly T[]): T[] {
  const ended = (e: T) => e.terminationDate ?? e.hireDate;
  return [...employments].sort(
    (a, b) =>
      Number(b.isActive) - Number(a.isActive) ||
      (a.isActive ? a.hireDate.localeCompare(b.hireDate) : ended(b).localeCompare(ended(a))) ||
      b.hireDate.localeCompare(a.hireDate) ||
      (a.registryCode < b.registryCode ? -1 : a.registryCode > b.registryCode ? 1 : 0),
  );
}
