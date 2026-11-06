import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';

export type ReferenceDefinition = ApiSchemas['ReferenceDefinitionResponse'];
export type ReferenceType = ApiSchemas['ReferenceTypeKind'];

/**
 * Referans tanimlari (ADR-0022 §1; SYG-PERS-072). Okuma `reference.definition.view`, yazma
 * `reference.definition.update` ister. Kan grubu sistem tanimidir ve degistirilemez (`KR-129`).
 */
export const referenceApi = {
  /** Turun tum tanimlari; yonetim ekrani pasifleri de gosterir. */
  list: async (type: ReferenceType): Promise<ReferenceDefinition[]> => {
    const response = await apiClient.get<ReferenceDefinition[]>('/reference/definitions', {
      params: { type, includeInactive: true },
    });
    return response.data;
  },

  /** Yeni tanim; unvanda kod zorunludur, gorev ve yakinlikta bos birakilirsa uretilir. */
  create: async (type: ReferenceType, code: string, name: string): Promise<ReferenceDefinition> => {
    const response = await apiClient.post<ReferenceDefinition>('/reference/definitions', {
      type,
      code: code.trim() === '' ? null : code.trim(),
      name,
    });
    return response.data;
  },

  rename: async (id: string, name: string): Promise<void> => {
    await apiClient.put(`/reference/definitions/${encodeURIComponent(id)}/name`, { name });
  },

  deactivate: async (id: string): Promise<void> => {
    await apiClient.post(`/reference/definitions/${encodeURIComponent(id)}/deactivation`);
  },

  /** Pasif tanimi yeniden aktif eder (`KR-136`). */
  reactivate: async (id: string): Promise<void> => {
    await apiClient.post(`/reference/definitions/${encodeURIComponent(id)}/reactivation`);
  },
};
