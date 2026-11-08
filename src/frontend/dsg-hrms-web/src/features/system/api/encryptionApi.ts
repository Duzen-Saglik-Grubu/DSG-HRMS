import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';

/** Bir anahtar ve onunla sifreli deger sayisi. */
export interface EncryptionKey {
  keyId: string;
  count: number;
  isCurrent: boolean;
  isConfigured: boolean;
}

/** Ozel nitelikli verinin anahtar durumu (SYG-PERS-062). Yalnizca sayilar. */
export interface EncryptionStatus {
  currentKeyId: string;
  keys: EncryptionKey[];
}

/** Yeniden sifreleme sonucu. */
export interface ReEncryptionResult {
  targetKeyId: string;
  sourceKeyIds: string[];
  reEncrypted: number;
  undecryptable: number;
  remainingOnOldKeys: number;
}

/**
 * Ozel nitelikli verinin sifrelemesi: anahtar durumu ve yeniden sifreleme isi (SYG-PERS-062;
 * ADR-0020 §3). Yalnizca `system.encryption.update` (Sistem Yoneticisi, KR-138).
 */
export const encryptionApi = {
  status: async (): Promise<EncryptionStatus> => {
    const response =
      await apiClient.get<ApiSchemas['EncryptionStatusResponse']>('/personnel/encryption');
    return {
      currentKeyId: response.data.currentKeyId,
      keys: response.data.keys.map((key) => ({
        keyId: key.keyId,
        count: Number(key.count),
        isCurrent: key.isCurrent,
        isConfigured: key.isConfigured,
      })),
    };
  },

  reEncrypt: async (): Promise<ReEncryptionResult> => {
    const response = await apiClient.post<ApiSchemas['ReEncryptionResponse']>(
      '/personnel/encryption/re-encryption',
    );
    return {
      targetKeyId: response.data.targetKeyId,
      sourceKeyIds: [...response.data.sourceKeyIds],
      reEncrypted: Number(response.data.reEncrypted),
      undecryptable: Number(response.data.undecryptable),
      remainingOnOldKeys: Number(response.data.remainingOnOldKeys),
    };
  },
};
