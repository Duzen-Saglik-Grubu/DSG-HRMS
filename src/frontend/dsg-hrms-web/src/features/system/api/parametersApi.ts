import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';

export type Parameter = ApiSchemas['ParameterResponse'];

/**
 * En kucuk parametre ekrani (SYG-KMLK-076) ve kurumsal logo (PRM-GRN-01).
 *
 * Sir parametrelerin degeri hicbir zaman donmez (`KR-071`); ekran yalnizca tanimli olup
 * olmadigini gosterir ve yeni deger yazdirir.
 */
export const parametersApi = {
  list: async (): Promise<Parameter[]> => {
    const response = await apiClient.get<ApiSchemas['PagedResponseOfParameterResponse']>(
      '/system/parameters',
      { params: { page: 1, pageSize: 100 } },
    );
    return response.data.items;
  },

  update: async (key: string, value: string): Promise<void> => {
    await apiClient.put(`/system/parameters/${encodeURIComponent(key)}`, { value });
  },

  uploadLogo: async (file: File): Promise<void> => {
    const form = new FormData();
    form.append('file', file);
    await apiClient.put('/system/logo', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  removeLogo: async (): Promise<void> => {
    await apiClient.delete('/system/logo');
  },
};
