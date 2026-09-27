/**
 * T.C. Kimlik Numarasi dogrulamasi (SYG-KMLK-014).
 *
 * Sunucu ayni kurali ayrica uygular; istemcideki denetim yalnizca kullaniciya hatayi
 * aninda gostermek icindir. Gecersiz numara sunucuya gonderilmez.
 *
 * Kurallar: 11 hane, ilk hane sifir degil; 10. hane tek sira haneleri toplaminin 7
 * katindan cift sira haneleri toplaminin cikarilmasinin 10'a gore kalani; 11. hane ilk
 * 10 hanenin toplaminin 10'a gore kalani.
 */
export function isValidNationalId(value: string): boolean {
  if (!/^[1-9]\d{10}$/.test(value)) {
    return false;
  }

  const digits = Array.from(value, Number);
  const odd = digits[0]! + digits[2]! + digits[4]! + digits[6]! + digits[8]!;
  const even = digits[1]! + digits[3]! + digits[5]! + digits[7]!;
  const tenth = (((odd * 7 - even) % 10) + 10) % 10;
  const eleventh = digits.slice(0, 10).reduce((sum, digit) => sum + digit, 0) % 10;

  return digits[9] === tenth && digits[10] === eleventh;
}
