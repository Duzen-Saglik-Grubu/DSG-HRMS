/** "GG.AA.YYYY" -> "YYYY-AA-GG"; gecersiz veya gelecekteki tarihte `null`. */
export function parseBirthDate(value: string): string | null {
  const match = /^(\d{2})\.(\d{2})\.(\d{4})$/.exec(value.trim());
  if (!match) {
    return null;
  }

  const [, day, month, year] = match;
  const date = new Date(Date.UTC(Number(year), Number(month) - 1, Number(day)));
  const valid =
    date.getUTCFullYear() === Number(year) &&
    date.getUTCMonth() === Number(month) - 1 &&
    date.getUTCDate() === Number(day) &&
    Number(year) >= 1900 &&
    date.getTime() <= Date.now();

  return valid ? `${year}-${month}-${day}` : null;
}
