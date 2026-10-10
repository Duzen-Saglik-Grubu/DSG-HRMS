import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';

export type TwoFactorStatus = ApiSchemas['TwoFactorStatusResponse'];
export type TwoFactorSetup = ApiSchemas['TwoFactorSetupResponse'];
export type TwoFactorSetupVerification = ApiSchemas['TwoFactorSetupVerificationResponse'];

const BASE = '/identity/account/two-factor';

/**
 * Kullanicinin kendi iki adimli dogrulama tercihi (SYG-KMLK-080). Istekler oturumun erisim
 * jetonuyla gider.
 *
 * Acmak iki adimlidir: mevcut parola ve secilen kanalla kod istenir, kod dogrulaninca tercih
 * acilir. Kapatmak yalnizca mevcut parolayi ister.
 */
export const twoFactorApi = {
  status: async (): Promise<TwoFactorStatus> => {
    const response = await apiClient.get<TwoFactorStatus>(BASE);
    return response.data;
  },

  /** Mevcut parolayi denetler ve secilen kanala kod gonderir; tekrar gonderim de bu cagridir. */
  startSetup: async (body: ApiSchemas['TwoFactorSetupRequest']): Promise<TwoFactorSetup> => {
    const response = await apiClient.post<TwoFactorSetup>(`${BASE}/setup`, body);
    return response.data;
  },

  verifySetup: async (codeId: string, code: string): Promise<TwoFactorSetupVerification> => {
    const response = await apiClient.post<TwoFactorSetupVerification>(
      `${BASE}/setup/verification`,
      { codeId, code },
    );
    return response.data;
  },

  disable: async (body: ApiSchemas['TwoFactorDisableRequest']): Promise<void> => {
    await apiClient.post(`${BASE}/disable`, body);
  },
};
