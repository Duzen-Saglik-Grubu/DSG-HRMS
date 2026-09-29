/**
 * Izin kodlari (ADR-0007 §1). Arka uctaki sabit listeyle (`Permissions.cs`) ve
 * `docs/mimari/izin-listesi.md` ile AYNIDIR; yalnizca gosterim kararlari icin kullanilir.
 */
export const permissions = {
  accountView: 'identity.account.view',
  accountUpdate: 'identity.account.update',
  inviteCreate: 'identity.invite.create',
  syncView: 'identity.sync.view',
  syncCreate: 'identity.sync.create',
  parameterView: 'system.parameter.view',
  parameterUpdate: 'system.parameter.update',
} as const;

export type Permission = (typeof permissions)[keyof typeof permissions];
