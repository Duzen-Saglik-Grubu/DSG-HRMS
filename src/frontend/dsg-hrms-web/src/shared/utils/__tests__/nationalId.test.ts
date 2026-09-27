import { describe, expect, it } from 'vitest';
import { isValidNationalId } from '../nationalId';

/**
 * TCKN sagla algoritmasi (SYG-KMLK-014). Numaralar SENTETIKTIR: sunucu testleriyle
 * ayni ureteckle uretildi.
 */
describe('isValidNationalId', () => {
  it.each(['10000000146', '10000000214', '10000009910'])(
    'gecerli numarayi kabul eder: %s',
    (value) => {
      expect(isValidNationalId(value)).toBe(true);
    },
  );

  it.each([
    ['sagla hatali', '10000000147'],
    ['ilk hane sifir', '01234567890'],
    ['10 hane', '1000000014'],
    ['12 hane', '100000001460'],
    ['harf', '1000000014a'],
    ['bos', ''],
    ['bosluklu', ' 10000000146'],
  ])('gecersiz numarayi reddeder (%s)', (_, value) => {
    expect(isValidNationalId(value)).toBe(false);
  });
});
