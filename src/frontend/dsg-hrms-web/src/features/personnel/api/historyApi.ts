import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';

export type EmploymentHistory = ApiSchemas['EmploymentHistoryResponse'];
export type AssignmentPeriod = ApiSchemas['AssignmentPeriodResponse'];

/**
 * Istihdam tarihcesi ve tarihli sorgu (PG-PERS-12; SYG-PERS-020). Yetki ve kapsam kartla
 * aynidir; kisi kendi tarihcesini izinsiz gorur.
 */
export const historyApi = {
  /** Butun tarihce; `date` ("YYYY-AA-GG") verilirse yalnizca o tarihte gecerli donemler. */
  get: async (
    personId: string,
    registryCode: string,
    date: string | null,
  ): Promise<EmploymentHistory> => {
    const response = await apiClient.get<EmploymentHistory>(
      `/personnel/persons/${encodeURIComponent(personId)}/employments/${encodeURIComponent(registryCode)}/history`,
      { params: date ? { date } : {} },
    );
    return response.data;
  },
};
