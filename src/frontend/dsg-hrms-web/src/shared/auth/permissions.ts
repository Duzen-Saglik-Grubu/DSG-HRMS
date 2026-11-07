/**
 * Izin kodlari (ADR-0007 §1). Arka uctaki sabit listeyle (`Permissions.cs`) ve
 * `docs/mimari/izin-listesi.md` ile AYNIDIR; yalnizca gosterim kararlari icin kullanilir.
 */
export const permissions = {
  accountView: 'identity.account.view',
  accountUpdate: 'identity.account.update',
  inviteCreate: 'identity.invite.create',
  personView: 'personnel.person.view',
  personViewNationalId: 'personnel.person.view_national_id',
  personViewSensitive: 'personnel.person.view_sensitive',
  personUpdate: 'personnel.person.update',
  personExport: 'personnel.person.export',
  correctionView: 'personnel.correction.view',
  correctionUpdate: 'personnel.correction.update',
  qualityView: 'personnel.quality.view',
  qualityExport: 'personnel.quality.export',
  syncView: 'personnel.sync.view',
  syncCreate: 'personnel.sync.create',
  referenceView: 'reference.definition.view',
  referenceUpdate: 'reference.definition.update',
  auditLogView: 'audit.log.view',
  parameterView: 'system.parameter.view',
  parameterUpdate: 'system.parameter.update',
} as const;

export type Permission = (typeof permissions)[keyof typeof permissions];
