import { ApiError } from '@/shared/api/problemDetails';

/** Uyelik ve iki adimli giris akisinda bir hatanin kullaniciya nasil gosterilecegi. */
export type FlowErrorKind =
  'rateLimited' | 'sessionExpired' | 'channelUnavailable' | 'fields' | 'unexpected';

/** Sunucunun "kayit bulunamadi" hata turu (ADR-0010 §5). */
const NOT_FOUND_TYPE = /\/not-found$/;

/**
 * Hatayi turune ayirir. Alan hatalari formun kendisine baglanir.
 *
 * "Suresi doldu" YALNIZCA iki kosulda soylenir: hata uygulamanin kendi "kayit yok"
 * yanitidir ve islem (uyelik veya bekleyen giris) zaten baslamistir (`canExpire`). Aksi halde ornegin
 * guncellenmemis bir sunucunun 404'u kullaniciya yanlis bir aciklama olarak gosterilirdi.
 */
export function classifyFlowError(
  error: unknown,
  options: { canExpire: boolean } = { canExpire: true },
): FlowErrorKind {
  if (!(error instanceof ApiError)) {
    return 'unexpected';
  }

  switch (error.status) {
    case 429:
      return 'rateLimited';
    case 404:
      return options.canExpire && error.type !== undefined && NOT_FOUND_TYPE.test(error.type)
        ? 'sessionExpired'
        : 'unexpected';
    case 422:
      return 'channelUnavailable';
    case 400:
      return error.errors ? 'fields' : 'unexpected';
    default:
      return 'unexpected';
  }
}
