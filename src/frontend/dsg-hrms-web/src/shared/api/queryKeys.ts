/**
 * TanStack Query anahtarlari (ADR-0015 §2).
 *
 * Anahtarlar merkezi olarak tanimlanir; bilesen icinde dize elle YAZILMAZ.
 * Elle yazilan bir anahtar, degisiklik sonrasi gecersiz kilma (invalidate)
 * cagrisiyla uyusmazsa ekran eski veriyi gostermeye devam eder - ve bu hata
 * sessizdir.
 */
export const queryKeys = {
  saglik: ['saglik'] as const,
  system: {
    parameters: ['system', 'parameters'] as const,
  },
  identity: {
    publicSettings: ['identity', 'public-settings'] as const,
    accounts: ['identity', 'accounts'] as const,
    accountList: (params: object) => ['identity', 'accounts', params] as const,
    invitation: (token: string | null) => ['identity', 'invitation', token] as const,
  },
} as const;
