import { describe, expect, it } from 'vitest';
import { safeReturnPath } from '../routes';

describe('safeReturnPath', () => {
  it.each(['/', '/personnel', '/leave?page=2#x'])('uygulama ici yolu kabul eder: %s', (path) => {
    expect(safeReturnPath(path)).toBe(path);
  });

  // Kabul edilseydi giris ekrani baska siteye yonlendiren bir araca donusurdu.
  it.each([
    null,
    undefined,
    '',
    'https://ornek.com',
    '//ornek.com',
    '/\\ornek.com',
    'javascript:alert(1)',
    'personnel',
    '/login',
    '/login?returnTo=/x',
  ])('guvenli olmayan veya dongu yaratan degeri ana sayfaya cevirir: %s', (value) => {
    expect(safeReturnPath(value)).toBe('/');
  });
});
