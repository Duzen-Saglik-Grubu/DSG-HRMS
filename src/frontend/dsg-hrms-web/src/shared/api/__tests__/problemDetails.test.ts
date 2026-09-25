import { describe, expect, it } from 'vitest';
import { isProblemDetails, toApiError } from '../problemDetails';

/**
 * Backend hata sozlesmesinin (ADR-0010 §5) istemci tarafinda dogru cozumlendigini
 * dogrular. Bu cevrim bozulursa kullanici, sunucunun urettigi anlamli Turkce
 * mesaj yerine genel bir hata gorur.
 */
describe('problemDetails', () => {
  const fallback = 'Beklenmeyen bir sorun olustu.';

  it('sunucunun Turkce mesajini oldugu gibi gosterir', () => {
    const error = toApiError(
      {
        type: 'https://dsg-hrms/errors/business-rule',
        title: 'Is kurali ihlali',
        status: 422,
        detail: 'Yillik izin bakiyeniz yetersiz.',
        traceId: 'DESTEK-2026-0042',
      },
      422,
      fallback,
    );

    expect(error.message).toBe('Yillik izin bakiyeniz yetersiz.');
    expect(error.status).toBe(422);
    expect(error.isNetworkError).toBe(false);
  });

  it('takip numarasini korur', () => {
    // Kullanici bu numarayi destek talebinde iletir; kaybolursa kayit
    // gunlukte aranmak zorunda kalir.
    const error = toApiError(
      { status: 500, title: 'Beklenmeyen hata', traceId: 'abc123' },
      500,
      fallback,
    );

    expect(error.traceId).toBe('abc123');
  });

  it('alan bazli dogrulama hatalarini cikarir', () => {
    const error = toApiError(
      {
        status: 400,
        title: 'Dogrulama hatasi',
        errors: {
          startDate: ['Baslangic tarihi gecmis olamaz.'],
          dayCount: ['Gun sayisi sifirdan buyuk olmalidir.'],
        },
      },
      400,
      fallback,
    );

    expect(error.errors?.['startDate']).toHaveLength(1);
    expect(error.errors?.['dayCount']?.[0]).toBe('Gun sayisi sifirdan buyuk olmalidir.');
  });

  it('metin olmayan hata mesajlarini eler', () => {
    const error = toApiError(
      { status: 400, errors: { field: [42, 'gecerli mesaj'] } },
      400,
      fallback,
    );

    expect(error.errors?.['field']).toEqual(['gecerli mesaj']);
  });

  it('taninmayan govdede varsayilan mesaji kullanir', () => {
    // Sunucudan beklenmeyen bir sey gelirse kullaniciya ham icerik GOSTERILMEZ.
    const error = toApiError('<html>500 Internal Server Error</html>', 500, fallback);

    expect(error.message).toBe(fallback);
  });

  it('sunucuya ulasilamadigini isaretler', () => {
    const error = toApiError(undefined, undefined, 'Sunucuya ulasilamadi.');

    expect(error.isNetworkError).toBe(true);
  });

  it('Problem Details bicimini tanir', () => {
    expect(isProblemDetails({ status: 404 })).toBe(true);
    expect(isProblemDetails({ title: 'Hata' })).toBe(true);
    expect(isProblemDetails({ text: 'hata' })).toBe(false);
    expect(isProblemDetails(null)).toBe(false);
    expect(isProblemDetails('hata')).toBe(false);
  });
});
