import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';
import type { CodeRequested, VerificationChannel } from './registrationApi';

export type SignInResponse = ApiSchemas['SignInResponse'];
export type TwoFactorChallenge = ApiSchemas['TwoFactorChallengeResponse'];
export type TwoFactorVerification = ApiSchemas['TwoFactorVerificationResponse'];

const BASE = '/identity/sessions';

/**
 * Giris ve iki adimli dogrulama cagrilari (#94). Istekler erisim jetonu TASIMAZ: oturum
 * henuz yoktur; `401` yanitinda jeton yenileme de denenmez.
 */
export const signInApi = {
  signIn: async (body: ApiSchemas['SignInRequest']): Promise<SignInResponse> => {
    const response = await apiClient.post<SignInResponse>(BASE, body, { skipAuth: true });
    return response.data;
  },

  requestCode: async (
    challengeId: string,
    channel: VerificationChannel,
  ): Promise<CodeRequested> => {
    const response = await apiClient.post<CodeRequested>(
      `${BASE}/challenges/${challengeId}/code`,
      { channel },
      { skipAuth: true },
    );
    return response.data;
  },

  verify: async (challengeId: string, code: string): Promise<TwoFactorVerification> => {
    const response = await apiClient.post<TwoFactorVerification>(
      `${BASE}/challenges/${challengeId}/verification`,
      { code },
      { skipAuth: true },
    );
    return response.data;
  },
};
