import { apiClient } from '@/shared/api/client';
import type { PagedResult } from '@/shared/api/paging';
import type { ApiSchemas } from '@/shared/api/schemas';

export type PersonListItem = ApiSchemas['PersonListItemResponse'];
export type PersonFilterOptions = ApiSchemas['PersonFilterOptionsResponse'];
export type FilterOption = ApiSchemas['FilterOptionResponse'];

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

  /** Secim listelerinin degerleri, kullanilabilen korumali suzgecler ve sayfa boyutlari. */
  filterOptions: async (): Promise<PersonFilterOptions> => {
    const response = await apiClient.get<PersonFilterOptions>(`${BASE}/filter-options`);
    return response.data;
  },
};
