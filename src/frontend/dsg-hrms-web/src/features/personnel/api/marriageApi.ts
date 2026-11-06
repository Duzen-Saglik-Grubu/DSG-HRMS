import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';

export type Marriage = ApiSchemas['MarriageResponse'];

const path = (personId: string) => `/personnel/persons/${encodeURIComponent(personId)}/marriage`;

/**
 * Evlilik tarihi ve evlilik yardimi (PG-PERS-19; SYG-PERS-036). Yalnizca `personnel.person.update`
 * izni olan kullanici ve medeni hali Evli olan kiside yazilir; okuma kartin `marriage` bolumundendir.
 */
export const marriageApi = {
  /** Girer veya degistirir; tarih "YYYY-AA-GG". */
  set: async (
    personId: string,
    marriageDate: string,
    allowanceReceived: boolean,
  ): Promise<Marriage> => {
    const response = await apiClient.put<Marriage>(path(personId), {
      marriageDate,
      allowanceReceived,
    });
    return response.data;
  },

  remove: async (personId: string): Promise<void> => {
    await apiClient.delete(path(personId));
  },
};
