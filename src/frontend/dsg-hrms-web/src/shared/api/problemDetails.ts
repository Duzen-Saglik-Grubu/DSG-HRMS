/**
 * Backend'in dondurdugu RFC 9457 hata yanitinin istemci tarafi karsiligi
 * (ADR-0010 §5).
 *
 * Backend tum hatalari tek bicimde dondurur; bu yuzden istemcide de tek bir
 * hata tipi vardir. Her cagri yerinde farkli hata sekli ele alinsaydi, kullanici
 * ekrandan ekrana farkli davranan bir uygulama gorurdu.
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;

  /** Kullaniciya gosterilen ve destek talebinde iletilen takip numarasi. */
  traceId?: string;

  /** Alan bazli dogrulama hatalari (ADR-0010 §8). */
  errors?: Record<string, string[]>;
}

/**
 * Uygulama genelinde tasinan hata nesnesi.
 *
 * <c>Error</c> turevi olmasi bilinclidir: Promise reddi, hata sinirlari
 * (error boundary) ve gunluk araclari standart hata nesnesi bekler. Duz bir
 * nesneyle reddedilen bir Promise, yigin izi tasimaz ve araclarda "bilinmeyen
 * hata" olarak gorunur.
 */
export class ApiError extends Error {
  /** HTTP durum kodu. Sunucuya ulasilamadiysa tanimsiz. */
  readonly status: number | undefined;

  /** Kullaniciya gosterilen ve destek talebinde iletilen takip numarasi. */
  readonly traceId: string | undefined;

  /** Alan bazli dogrulama hatalari (ADR-0010 §8). */
  readonly errors: Record<string, string[]> | undefined;

  /** Sunucuya hic ulasilamadi (ag hatasi, zaman asimi). */
  readonly isNetworkError: boolean;

  /**
   * Hata turu (Problem Details `type`, ör. `https://dsg-hrms/errors/not-found`).
   * Ayni durum kodunun farkli anlamlarini ayirmak icindir: uygulamanin "kayit yok"
   * yaniti ile var olmayan bir adrese yapilan istegin 404'u ayni sey degildir.
   */
  readonly type: string | undefined;

  constructor(options: {
    message: string;
    status?: number | undefined;
    traceId?: string | undefined;
    errors?: Record<string, string[]> | undefined;
    isNetworkError: boolean;
    type?: string | undefined;
  }) {
    // message alani kullaniciya GOSTERILEBILIR Turkce metindir.
    super(options.message);

    this.name = 'ApiError';
    this.status = options.status;
    this.traceId = options.traceId;
    this.errors = options.errors;
    this.isNetworkError = options.isNetworkError;
    this.type = options.type;
  }
}

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null;

/**
 * Bir degerin Problem Details bicimine uyup uymadigini kontrol eder.
 */
export function isProblemDetails(value: unknown): value is ProblemDetails {
  if (!isRecord(value)) {
    return false;
  }

  // "status" veya "title" bulunuyorsa Problem Details kabul edilir; her ikisi de
  // zorunlu degildir (RFC 9457), ancak biri olmadan yanit anlamsizdir.
  return typeof value['status'] === 'number' || typeof value['title'] === 'string';
}

/**
 * Alan bazli hatalari guvenli bicimde cikarir.
 */
function readFieldErrors(value: unknown): Record<string, string[]> | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const result: Record<string, string[]> = {};

  for (const [field, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) {
      result[field] = messages.filter((message): message is string => typeof message === 'string');
    }
  }

  return Object.keys(result).length > 0 ? result : undefined;
}

/**
 * Sunucu yanitini uygulama hatasina cevirir.
 *
 * @param body Yanit govdesi.
 * @param status HTTP durum kodu.
 * @param fallbackMessage Yanit anlasilamadiginda gosterilecek Turkce mesaj.
 */
export function toApiError(
  body: unknown,
  status: number | undefined,
  fallbackMessage: string,
): ApiError {
  if (isProblemDetails(body)) {
    return new ApiError({
      // Backend mesajlari zaten Turkce ve kullaniciya yoneliktir (ADR-0010 §5);
      // oldugu gibi gosterilir.
      message: body.detail ?? body.title ?? fallbackMessage,
      status: body.status ?? status,
      traceId: body.traceId,
      errors: readFieldErrors(body.errors),
      isNetworkError: false,
      type: body.type,
    });
  }

  return new ApiError({
    message: fallbackMessage,
    status,
    isNetworkError: status === undefined,
  });
}
