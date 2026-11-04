import { apiClient } from '@/shared/api/client';
import type { PagedResult } from '@/shared/api/paging';
import type { ApiSchemas } from '@/shared/api/schemas';

export type CorrectionField = ApiSchemas['CorrectionFieldName'];
export type Correction = ApiSchemas['CorrectionResponse'];
export type OwnCorrectionState = ApiSchemas['OwnCorrectionStateResponse'];
export type CorrectionStatusFilter = 'new' | 'completed' | 'all';

/** Formdaki alan sirasi (PG-PERS-37). */
export const CORRECTION_FIELDS: readonly CorrectionField[] = [
  'mobilePhone',
  'email',
  'address',
  'education',
  'maritalStatus',
  'bloodGroup',
];

const BASE = '/personnel/corrections';

/**
 * "IK'ya bildir" duzeltme talepleri (SYG-PERS-045…049). Kisi yalnizca kendi adina talep acar;
 * talepleri gormek ve kapatmak izin ister. Deger dogrulamasi sunucudadir.
 */
export const correctionsApi = {
  /** Acik talebi olan alanlar ve kan grubu secenekleri. */
  own: async (): Promise<OwnCorrectionState> => {
    const response = await apiClient.get<OwnCorrectionState>(`${BASE}/me`);
    return response.data;
  },

  /** Kendi adina talep gonderir. */
  submit: async (field: CorrectionField, value: string): Promise<string> => {
    const response = await apiClient.post<ApiSchemas['CorrectionSubmittedResponse']>(`${BASE}/me`, {
      field,
      value,
    });
    return response.data.id;
  },

  /** Talepler, en yeni once. */
  list: async (
    status: CorrectionStatusFilter,
    page: number,
    pageSize: number,
  ): Promise<PagedResult<Correction>> => {
    const response = await apiClient.get<ApiSchemas['PagedResponseOfCorrectionResponse']>(BASE, {
      params: { status, page, pageSize },
    });
    const data = response.data;

    return {
      items: data.items,
      page: Number(data.page),
      pageSize: Number(data.pageSize),
      totalCount: Number(data.totalCount),
      totalPages: Number(data.totalPages),
    };
  },

  /** Tek talep (bildirimden). */
  get: async (requestId: string): Promise<Correction> => {
    const response = await apiClient.get<Correction>(`${BASE}/${encodeURIComponent(requestId)}`);
    return response.data;
  },

  /** Talebi "Tamamlandi" olarak kapatir; onerilen deger silinir. */
  complete: async (requestId: string): Promise<Correction> => {
    const response = await apiClient.post<Correction>(
      `${BASE}/${encodeURIComponent(requestId)}/complete`,
    );
    return response.data;
  },
};
