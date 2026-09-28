import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';

export type VerificationChannel = ApiSchemas['VerificationChannelKind'];
export type VerificationOutcome = ApiSchemas['VerificationOutcome'];
export type RegistrationStarted = ApiSchemas['RegistrationStartedResponse'];
export type CodeRequested = ApiSchemas['CodeRequestedResponse'];
export type VerificationResponse = ApiSchemas['VerificationResponse'];

/** Giris ve uyelik ekranlarinin ayarlari (sayilar tip uretecinden dolayi normallestirilir). */
export interface PublicSettings {
  supportContact: string;
  passwordRules: { minLength: number; maxLength: number; requireComplexity: boolean };
  verificationCodeLength: number;
}

const BASE = '/identity/registrations';

/**
 * Uyelik akisinin API cagrilari (ADR-0006 §1, PR #88).
 *
 * Eslesme olsa da olmasa da sunucu ayni yaniti dondurur (KR-085); istemci de bu
 * yanitlari AYIRT ETMEYE CALISMAZ.
 */
export const registrationApi = {
  start: async (body: ApiSchemas['StartRegistrationRequest']): Promise<RegistrationStarted> => {
    const response = await apiClient.post<RegistrationStarted>(BASE, body);
    return response.data;
  },

  requestCode: async (
    registrationId: string,
    channel: VerificationChannel,
  ): Promise<CodeRequested> => {
    const response = await apiClient.post<CodeRequested>(`${BASE}/${registrationId}/code`, {
      channel,
    });
    return response.data;
  },

  verify: async (registrationId: string, code: string): Promise<VerificationResponse> => {
    const response = await apiClient.post<VerificationResponse>(
      `${BASE}/${registrationId}/verification`,
      { code },
    );
    return response.data;
  },

  complete: async (registrationId: string, password: string): Promise<void> => {
    await apiClient.post(`${BASE}/${registrationId}/account`, { password });
  },

  publicSettings: async (): Promise<PublicSettings> => {
    const response = await apiClient.get<ApiSchemas['PublicSettingsResponse']>(
      '/identity/public-settings',
    );
    const data = response.data;

    return {
      supportContact: data.supportContact,
      passwordRules: {
        minLength: Number(data.passwordRules.minLength),
        maxLength: Number(data.passwordRules.maxLength),
        requireComplexity: data.passwordRules.requireComplexity,
      },
      verificationCodeLength: Number(data.verificationCodeLength),
    };
  },
};
