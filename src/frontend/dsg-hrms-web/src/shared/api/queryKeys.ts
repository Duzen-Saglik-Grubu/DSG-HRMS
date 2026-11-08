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
    encryption: ['system', 'encryption'] as const,
  },
  personnel: {
    persons: ['personnel', 'persons'] as const,
    list: (query: string) => ['personnel', 'persons', query] as const,
    filterOptions: ['personnel', 'filter-options'] as const,
    /** Butun kart sorgulari: kisi karti ve Profilim (#357). */
    cards: ['personnel', 'card'] as const,
    card: (personId: string) => ['personnel', 'card', personId] as const,
    me: ['personnel', 'card', 'me'] as const,
    /** Kart onekinin altindadir: gorev girisi kartla birlikte tarihceyi de yeniler (#357). */
    history: (personId: string, registryCode: string, date: string | null) =>
      ['personnel', 'card', personId, 'history', registryCode, date ?? 'all'] as const,
    photo: (personId: string) => ['personnel', 'photo', personId] as const,
    syncFreshness: ['personnel', 'sync-freshness'] as const,
    exportFields: ['personnel', 'export-fields'] as const,
    ownCorrections: ['personnel', 'corrections', 'me'] as const,
    correctionsAll: ['personnel', 'corrections'] as const,
    corrections: (status: string, page: number, pageSize: number) =>
      ['personnel', 'corrections', 'list', status, page, pageSize] as const,
    correction: (requestId: string) => ['personnel', 'corrections', 'item', requestId] as const,
    quality: (issue: string, page: number, pageSize: number) =>
      ['personnel', 'quality', issue, page, pageSize] as const,
    qualityIneligible: (page: number, pageSize: number) =>
      ['personnel', 'quality', 'ineligible', page, pageSize] as const,
    emergencyRelationships: ['personnel', 'emergency-contact-relationships'] as const,
    dutyOptions: ['personnel', 'duty-options'] as const,
    syncRunsAll: ['personnel', 'sync-runs'] as const,
    syncRuns: (page: number, pageSize: number) =>
      ['personnel', 'sync-runs', page, pageSize] as const,
  },
  audit: {
    entries: (
      from: string | null,
      to: string | null,
      type: string | null,
      actor: string | null,
      subject: string | null,
      page: number,
      pageSize: number,
    ) => ['audit', 'entries', from, to, type, actor, subject, page, pageSize] as const,
    people: (term: string) => ['audit', 'people', term] as const,
  },
  reference: {
    all: ['reference', 'definitions'] as const,
    definitions: (type: string) => ['reference', 'definitions', type] as const,
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
