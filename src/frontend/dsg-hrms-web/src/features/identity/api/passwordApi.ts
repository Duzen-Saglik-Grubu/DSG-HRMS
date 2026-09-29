import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';
import type {
  CodeRequested,
  RegistrationStarted,
  VerificationChannel,
  VerificationResponse,
} from './registrationApi';

const RESETS = '/identity/password-resets';

/**
 * Parola sifirlama ve oturum icinde parola degisikligi (SYG-KMLK-047, 048).
 *
 * Sifirlama uyelikle AYNI akistir (eslesme, kanal, kod); yanitlar eslesme olsa da olmasa da
 * aynidir (KR-085) ve istemci bunlari ayirt etmeye calismaz. Sifirlama istekleri oturum
 * gerektirmez; degisiklik ise oturumun erisim jetonuyla gider.
 */
export const passwordApi = {
  startReset: async (
    body: ApiSchemas['StartRegistrationRequest'],
  ): Promise<RegistrationStarted> => {
    const response = await apiClient.post<RegistrationStarted>(RESETS, body, { skipAuth: true });
    return response.data;
  },

  requestCode: async (resetId: string, channel: VerificationChannel): Promise<CodeRequested> => {
    const response = await apiClient.post<CodeRequested>(
      `${RESETS}/${resetId}/code`,
      { channel },
      { skipAuth: true },
    );
    return response.data;
  },

  verify: async (resetId: string, code: string): Promise<VerificationResponse> => {
    const response = await apiClient.post<VerificationResponse>(
      `${RESETS}/${resetId}/verification`,
      { code },
      { skipAuth: true },
    );
    return response.data;
  },

  /**
   * Yeni parolayi belirler; hesabin tum oturumlari kapanir. Uyelikte "hesabiniz zaten var"
   * sonucunu alan kisi de ayni islemin kimligiyle buraya gelir.
   */
  reset: async (resetId: string, password: string): Promise<void> => {
    await apiClient.post(`${RESETS}/${resetId}/password`, { password }, { skipAuth: true });
  },

  /** Oturum icinde degisiklik; diger oturumlar kapanir, bu oturum acik kalir. */
  change: async (body: ApiSchemas['ChangePasswordRequest']): Promise<void> => {
    await apiClient.post('/identity/account/password', body);
  },
};
