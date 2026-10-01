import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';

export type InvitationInfo = ApiSchemas['InvitationInfoResponse'];

const BASE = '/identity/invitations';

/**
 * IK davet baglantisi (SYG-KMLK-051…053). Gonderim oturumun jetonuyla gider; baglantinin
 * kullanimi oturum gerektirmez. Jeton adreste degil istek govdesinde tasinir.
 */
export const invitationApi = {
  send: async (personId: string, reason: string): Promise<{ expiresAt: string }> => {
    const response = await apiClient.post<ApiSchemas['InvitationSentResponse']>(
      `/identity/accounts/${personId}/invitations`,
      { reason },
    );
    return response.data;
  },

  lookup: async (token: string): Promise<InvitationInfo> => {
    const response = await apiClient.post<InvitationInfo>(
      `${BASE}/lookup`,
      { token },
      { skipAuth: true },
    );
    return response.data;
  },

  accept: async (token: string, password: string): Promise<void> => {
    await apiClient.post(`${BASE}/acceptance`, { token, password }, { skipAuth: true });
  },
};
