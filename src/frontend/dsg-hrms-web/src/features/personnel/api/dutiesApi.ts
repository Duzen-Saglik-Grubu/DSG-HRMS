import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';

export type Duty = ApiSchemas['DutyResponse'];
export type DutyOption = ApiSchemas['DutyOptionResponse'];

const path = (personId: string, registryCode: string) =>
  `/personnel/persons/${encodeURIComponent(personId)}/employments/${encodeURIComponent(registryCode)}/duties`;

/**
 * Kisiye gorev girisi (PG-PERS-43; SYG-PERS-042). Yalnizca `personnel.person.update` izni olan
 * kullanici yazar; guncel gorev kartin calisma hayati bolumunden okunur.
 */
export const dutiesApi = {
  /** Yeni secim icin aktif gorev tanimlari (referans veri). */
  options: async (): Promise<DutyOption[]> => {
    const response = await apiClient.get<DutyOption[]>('/personnel/duty-options');
    return response.data;
  },

  /** Gorev atar; baslangic "YYYY-AA-GG". Onceki acik gorev yeni baslangicla kapanir. */
  assign: async (
    personId: string,
    registryCode: string,
    definitionId: string,
    validFrom: string,
  ): Promise<Duty> => {
    const response = await apiClient.post<Duty>(path(personId, registryCode), {
      definitionId,
      validFrom,
    });
    return response.data;
  },

  /** Gorev kaydini siler (yanlis girisi duzeltmek icin; kayit yumusak silinir). */
  remove: async (personId: string, registryCode: string, dutyId: string): Promise<void> => {
    await apiClient.delete(`${path(personId, registryCode)}/${encodeURIComponent(dutyId)}`);
  },
};
