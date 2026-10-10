import { apiClient } from './client';
import type { ApiSchemas } from './schemas';

/** Giris ve uyelik ekranlarinin ayarlari (sayilar tip uretecinden dolayi normallestirilir). */
export interface PublicSettings {
  supportContact: string;
  passwordRules: { minLength: number; maxLength: number; requireComplexity: boolean };
  verificationCodeLength: number;
  /** Yuklu kurumsal logonun surumu (PRM-GRN-01); yoksa varsayilan logo kullanilir. */
  logoVersion: string | null;
  /** Iki adimli dogrulama sistemde kullaniliyor mu (PRM-KML-08, SYG-KMLK-080). */
  twoFactorAvailable: boolean;
}

/**
 * Herkese acik ayarlari okur. Kimlik ekranlari ve uygulama kabugu (kullanici menusu) ayni
 * sorgu anahtarini (`queryKeys.identity.publicSettings`) kullanir; bu yuzden okuma tek yerdedir.
 */
export async function fetchPublicSettings(): Promise<PublicSettings> {
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
    logoVersion: data.logoVersion ?? null,
    twoFactorAvailable: data.twoFactorAvailable,
  };
}
