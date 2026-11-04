import { apiClient } from '@/shared/api/client';
import type { PagedResult } from '@/shared/api/paging';
import type { ApiSchemas } from '@/shared/api/schemas';

export type PersonListItem = ApiSchemas['PersonListItemResponse'];
export type PersonFilterOptions = ApiSchemas['PersonFilterOptionsResponse'];
export type FilterOption = ApiSchemas['FilterOptionResponse'];
export type PersonCard = ApiSchemas['PersonCardResponse'];
export type EmploymentCard = ApiSchemas['EmploymentCardResponse'];
export type SyncFreshness = ApiSchemas['SyncFreshnessResponse'];
export type ExportField = ApiSchemas['PersonExportFieldResponse'];
export type ExportFieldName = ExportField['field'];
export type ExportFormat = 'excel' | 'pdf';

/** Indirilen dosya. */
export interface ExportedFile {
  blob: Blob;
  fileName: string;
}

/** `Content-Disposition` basligindan dosya adi; once RFC 5987 bicimi (`filename*`). */
export function fileNameOf(header: unknown): string | null {
  if (typeof header !== 'string') {
    return null;
  }

  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (encoded?.[1]) {
    try {
      return decodeURIComponent(encoded[1]);
    } catch {
      return null;
    }
  }

  const plain = /filename="?([^";]+)"?/i.exec(header);
  return plain?.[1] ?? null;
}

const BASE = '/personnel/persons';

/**
 * Personel listesi ve suzgec secenekleri (SYG-PERS-022–031). Kimin listelenecegini ve hangi
 * suzgecin kullanilabilecegini sunucu belirler; ekran yalnizca gosterir.
 */
export const personsApi = {
  /**
   * Listeyi getirir. Parametreler adres cubugundakiyle aynidir; coklu secimler ayni adla
   * tekrarlanir (`branchId=a&branchId=b`), sunucu bu bicimi bekler.
   */
  list: async (params: URLSearchParams): Promise<PagedResult<PersonListItem>> => {
    const response = await apiClient.get<ApiSchemas['PagedResponseOfPersonListItemResponse']>(
      BASE,
      { params },
    );
    const data = response.data;

    return {
      items: data.items,
      page: Number(data.page),
      pageSize: Number(data.pageSize),
      totalCount: Number(data.totalCount),
      totalPages: Number(data.totalPages),
    };
  },

  /**
   * Kisi karti (SYG-PERS-034–039). Kapsam disindaki ve var olmayan kisi ayni yaniti (`404`) alir;
   * gorulemeyen bolumler yanitta hic yer almaz (ADR-0019).
   */
  card: async (personId: string): Promise<PersonCard> => {
    const response = await apiClient.get<PersonCard>(`${BASE}/${encodeURIComponent(personId)}`);
    return response.data;
  },

  /** Kullanicinin kendi karti, "Profilim" (SYG-PERS-044). Izin gerektirmez. */
  me: async (): Promise<PersonCard> => {
    const response = await apiClient.get<PersonCard>(`${BASE}/me`);
    return response.data;
  },

  /** "Son senkronizasyon" bilgisi (SYG-PERS-010). Giris yapmis her kullaniciya aciktir. */
  syncFreshness: async (): Promise<SyncFreshness> => {
    const response = await apiClient.get<SyncFreshness>('/personnel/sync-freshness');
    return response.data;
  },

  /**
   * Disa aktarilabilen alanlar (PG-PERS-42). Gorulemeyen alan listede yoktur; sunucu
   * gorunurluk politikasindan verir (ADR-0019 §2).
   */
  exportFields: async (): Promise<ExportField[]> => {
    const response = await apiClient.get<ExportField[]>(`${BASE}/export/fields`);
    return response.data;
  },

  /**
   * Suzgece uyan butun kisileri dosya olarak indirir (SYG-PERS-032). Suzgecler listeyle
   * aynidir; sayfa ve sayfa boyutu gonderilmez. Aktarim sunucuda kayda gecer (SYG-PERS-033).
   */
  exportFile: async (
    listParams: URLSearchParams,
    format: ExportFormat,
    fields: readonly ExportFieldName[],
  ): Promise<ExportedFile> => {
    const params = new URLSearchParams(listParams);
    params.delete('page');
    params.delete('pageSize');
    params.set('format', format);
    fields.forEach((field) => params.append('field', field));

    const response = await apiClient.get<Blob>(`${BASE}/export`, {
      params,
      responseType: 'blob',
    });
    return {
      blob: response.data,
      fileName:
        fileNameOf(response.headers['content-disposition']) ??
        `personel-listesi.${format === 'pdf' ? 'pdf' : 'xlsx'}`,
    };
  },

  /** Secim listelerinin degerleri, kullanilabilen korumali suzgecler ve sayfa boyutlari. */
  filterOptions: async (): Promise<PersonFilterOptions> => {
    const response = await apiClient.get<PersonFilterOptions>(`${BASE}/filter-options`);
    return response.data;
  },
};
