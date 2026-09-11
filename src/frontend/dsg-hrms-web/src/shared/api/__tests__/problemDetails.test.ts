import { describe, expect, it } from 'vitest';
import { isProblemDetails, toApiError } from '../problemDetails';

/**
 * Backend hata sozlesmesinin (ADR-0010 §5) istemci tarafinda dogru cozumlendigini
 * dogrular. Bu cevrim bozulursa kullanici, sunucunun urettigi anlamli Turkce
 * mesaj yerine genel bir hata gorur.
 */
describe('problemDetails', () => {
  const varsayilan = 'Beklenmeyen bir sorun olustu.';

  it('sunucunun Turkce mesajini oldugu gibi gosterir', () => {
    const hata = toApiError(
      {
        type: 'https://dsg-hrms/errors/business-rule',
        title: 'Is kurali ihlali',
        status: 422,
        detail: 'Yillik izin bakiyeniz yetersiz.',
        traceId: 'DESTEK-2026-0042',
      },
      422,
      varsayilan,
    );

    expect(hata.message).toBe('Yillik izin bakiyeniz yetersiz.');
    expect(hata.status).toBe(422);
    expect(hata.isNetworkError).toBe(false);
  });

  it('takip numarasini korur', () => {
    // Kullanici bu numarayi destek talebinde iletir; kaybolursa kayit
    // gunlukte aranmak zorunda kalir.
    const hata = toApiError(
      { status: 500, title: 'Beklenmeyen hata', traceId: 'abc123' },
      500,
      varsayilan,
    );

    expect(hata.traceId).toBe('abc123');
  });

  it('alan bazli dogrulama hatalarini cikarir', () => {
    const hata = toApiError(
      {
        status: 400,
        title: 'Dogrulama hatasi',
        errors: {
          startDate: ['Baslangic tarihi gecmis olamaz.'],
          dayCount: ['Gun sayisi sifirdan buyuk olmalidir.'],
        },
      },
      400,
      varsayilan,
    );

    expect(hata.errors?.['startDate']).toHaveLength(1);
    expect(hata.errors?.['dayCount']?.[0]).toBe('Gun sayisi sifirdan buyuk olmalidir.');
  });

  it('metin olmayan hata mesajlarini eler', () => {
    const hata = toApiError(
      { status: 400, errors: { alan: [42, 'gecerli mesaj'] } },
      400,
      varsayilan,
    );

    expect(hata.errors?.['alan']).toEqual(['gecerli mesaj']);
  });

  it('taninmayan govdede varsayilan mesaji kullanir', () => {
    // Sunucudan beklenmeyen bir sey gelirse kullaniciya ham icerik GOSTERILMEZ.
    const hata = toApiError('<html>500 Internal Server Error</html>', 500, varsayilan);

    expect(hata.message).toBe(varsayilan);
  });

  it('sunucuya ulasilamadigini isaretler', () => {
    const hata = toApiError(undefined, undefined, 'Sunucuya ulasilamadi.');

    expect(hata.isNetworkError).toBe(true);
  });

  it('Problem Details bicimini tanir', () => {
    expect(isProblemDetails({ status: 404 })).toBe(true);
    expect(isProblemDetails({ title: 'Hata' })).toBe(true);
    expect(isProblemDetails({ mesaj: 'hata' })).toBe(false);
    expect(isProblemDetails(null)).toBe(false);
    expect(isProblemDetails('hata')).toBe(false);
  });
});
