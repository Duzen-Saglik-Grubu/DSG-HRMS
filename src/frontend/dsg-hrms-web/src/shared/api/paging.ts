/**
 * Sunucu tarafi sayfalama sozlesmesi (ADR-0010 §4).
 *
 * Tum listeleme uc noktalari bu bicimde doner. Sozlesmenin istemci tarafinda da
 * TEK BIR YERDE tanimlanmasi, her modulun kendi sayfalama tipini yazmasini
 * onler; aksi hâlde alan adlari zamanla birbirinden ayrisirdi.
 */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** Siralama yonu. */
export type SortOrder = 'asc' | 'desc';

/**
 * Listeleme isteginin parametreleri.
 */
export interface PageRequest {
  page: number;
  pageSize: number;
  sort?: string | undefined;
  order?: SortOrder | undefined;
}

/**
 * Sunucunun kabul ettigi en buyuk sayfa boyutu (ADR-0010 §4).
 *
 * Asan istek sunucudan <c>400</c> alir; istemci tarafinda da sinirlanir ki
 * kullanici gereksiz bir hata gormesin.
 */
export const MAX_PAGE_SIZE = 100;

/** Arayuzde sunulan sayfa boyutu secenekleri. */
export const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

/** Varsayilan sayfa boyutu. */
export const DEFAULT_PAGE_SIZE = 25;

/**
 * Sayfa istegini gecerli sinirlara cekerek sorgu parametrelerine cevirir.
 */
export function toQueryParams(request: PageRequest): Record<string, string> {
  const params: Record<string, string> = {
    // Sayfa numarasi 1'den baslar (ADR-0010 §4).
    page: String(Math.max(1, Math.trunc(request.page))),
    pageSize: String(Math.min(MAX_PAGE_SIZE, Math.max(1, Math.trunc(request.pageSize)))),
  };

  if (request.sort) {
    params['sort'] = request.sort;
    params['order'] = request.order ?? 'asc';
  }

  return params;
}
