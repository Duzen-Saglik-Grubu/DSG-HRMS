import { apiClient } from './client';

const BASE = '/personnel/persons';

/**
 * Kisi fotograflari (SYG-PERS-041): kucultulmus JPEG. Oturum jetonu bellekte tutuldugu icin
 * goruntu `<img src>` ile degil yetkili istekle alinir. Sunucu `private, no-cache` ve ETag
 * gonderir; tarayici degismeyen fotografi yeniden indirmez.
 *
 * Personel ekranlari ve uygulama kabugundaki kullanici karti birlikte kullandigi icin
 * `shared/` altindadir (ADR-0015 §1, #315).
 */
export const personPhotosApi = {
  /** Kisinin fotografi. */
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
};
