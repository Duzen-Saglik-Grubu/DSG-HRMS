import type { ReactNode } from 'react';
import type { Permission } from './permissions';
import { usePermission } from './usePermission';

interface PermissionGateProps {
  permission: Permission;
  children: ReactNode;
  /** Izin yoksa gosterilecek icerik; varsayilan hicbir sey. */
  fallback?: ReactNode;
}

/**
 * Icerigi izne gore gosterir (ADR-0015 §5).
 *
 * <b>Yalnizca gorsel bir kolayliktir.</b> Gercek yetki denetimi sunucudadir (ADR-0007 §3);
 * arayuzde gizlemek guvenlik onlemi degildir. Izni olmayan kullanici dugmeyi gormez, ama API
 * dogrudan cagrilsa da `403` alir.
 */
export function PermissionGate({ permission, children, fallback = null }: PermissionGateProps) {
  return usePermission(permission) ? children : fallback;
}
