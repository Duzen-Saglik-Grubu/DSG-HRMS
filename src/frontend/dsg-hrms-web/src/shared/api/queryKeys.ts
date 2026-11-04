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
  personnel: {
    persons: ['personnel', 'persons'] as const,
    list: (query: string) => ['personnel', 'persons', query] as const,
    filterOptions: ['personnel', 'filter-options'] as const,
    card: (personId: string) => ['personnel', 'card', personId] as const,
    me: ['personnel', 'card', 'me'] as const,
    photo: (personId: string) => ['personnel', 'photo', personId] as const,
    syncFreshness: ['personnel', 'sync-freshness'] as const,
    ownCorrections: ['personnel', 'corrections', 'me'] as const,
    correctionsAll: ['personnel', 'corrections'] as const,
    corrections: (status: string, page: number, pageSize: number) =>
      ['personnel', 'corrections', 'list', status, page, pageSize] as const,
    correction: (requestId: string) => ['personnel', 'corrections', 'item', requestId] as const,
  },
  notifications: {
    all: ['notifications'] as const,
    inbox: ['notifications', 'inbox'] as const,
    unread: ['notifications', 'unread'] as const,
  },
  identity: {
    publicSettings: ['identity', 'public-settings'] as const,
    twoFactor: ['identity', 'two-factor'] as const,
    accounts: ['identity', 'accounts'] as const,
    accountList: (params: object) => ['identity', 'accounts', params] as const,
    invitation: (token: string | null) => ['identity', 'invitation', token] as const,
  },
} as const;
