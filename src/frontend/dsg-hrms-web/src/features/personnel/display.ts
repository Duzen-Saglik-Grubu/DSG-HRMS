/** Iki parcayi " / " ile birlestirir; ikisi de yoksa bos isareti. */
export function joinParts(first: string | null, second: string | null): string {
  const parts = [first, second].filter((part): part is string => Boolean(part?.trim()));
  return parts.length > 0 ? parts.join(' / ') : '—';
}
