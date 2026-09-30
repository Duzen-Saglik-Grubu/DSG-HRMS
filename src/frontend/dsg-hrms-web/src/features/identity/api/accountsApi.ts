import { apiClient } from '@/shared/api/client';
import { toQueryParams, type PageRequest, type PagedResult } from '@/shared/api/paging';
import type { ApiSchemas } from '@/shared/api/schemas';

export type AccountSummary = ApiSchemas['AccountSummaryResponse'];
export type AccountState = ApiSchemas['AccountStateKind'];

export interface AccountSearch extends PageRequest {
  q?: string | undefined;
}

const BASE = '/identity/accounts';

/**
 * IK hesap islemleri (SYG-KMLK-057, 073). Yanit kisisel veri olarak yalnizca ad, soyad, sicil
 * ve firma tasir.
 */
export const accountsApi = {
  search: async (request: AccountSearch): Promise<PagedResult<AccountSummary>> => {
    const params = toQueryParams(request);
    if (request.q?.trim()) {
      params['q'] = request.q.trim();
    }

    const response = await apiClient.get<ApiSchemas['PagedResponseOfAccountSummaryResponse']>(
      BASE,
      {
        params,
      },
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

  deactivate: async (personId: string, reason: string): Promise<void> => {
    await apiClient.post(`${BASE}/${personId}/deactivation`, { reason });
  },

  activate: async (personId: string, reason: string): Promise<void> => {
    await apiClient.post(`${BASE}/${personId}/activation`, { reason });
  },
};
