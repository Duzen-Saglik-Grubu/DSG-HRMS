import { apiClient } from '@/shared/api/client';
import type { PagedResult } from '@/shared/api/paging';
import type { ApiSchemas } from '@/shared/api/schemas';

export type PersonListItem = ApiSchemas['PersonListItemResponse'];
export type PersonFilterOptions = ApiSchemas['PersonFilterOptionsResponse'];
export type FilterOption = ApiSchemas['FilterOptionResponse'];
export type PersonCard = ApiSchemas['PersonCardResponse'];
export type EmploymentCard = ApiSchemas['EmploymentCardResponse'];

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

  /**
   * Kisinin fotografi (SYG-PERS-041): kucultulmus JPEG. Oturum jetonu bellekte tutuldugu icin
   * goruntu `<img src>` ile degil yetkili istekle alinir. Sunucu `private, no-cache` ve ETag
   * gonderir; tarayici degismeyen fotografi yeniden indirmez.
   */
  photo: async (personId: string): Promise<Blob> => {
    const response = await apiClient.get<Blob>(`${BASE}/${encodeURIComponent(personId)}/photo`, {
      responseType: 'blob',
    });
    return response.data;
  },

  /** Kullanicinin kendi fotografi. */
  myPhoto: async (): Promise<Blob> => {
    const response = await apiClient.get<Blob>(`${BASE}/me/photo`, { responseType: 'blob' });
    return response.data;
  },

  /** Secim listelerinin degerleri, kullanilabilen korumali suzgecler ve sayfa boyutlari. */
  filterOptions: async (): Promise<PersonFilterOptions> => {
    const response = await apiClient.get<PersonFilterOptions>(`${BASE}/filter-options`);
    return response.data;
  },
};
