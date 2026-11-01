import type { ReactNode } from 'react';
import { Navigate } from 'react-router';
import { permissions } from '@/shared/auth/permissions';
import { usePermission } from '@/shared/auth/usePermission';
import { routes } from '@/shared/routes';

/**
 * Personel listesi yalnizca personeli gorme izni olana acilir. Rolu olmayan personel listede
 * zaten yalnizca kendini gorurdu (ADR-0019 §1); adresi elle yazsa da listeye degil, kendi
 * profiline gider (#311). Asil denetim sunucudadir; bu yalnizca gezinme kararidir.
 */
export function PersonnelListRoute({ children }: { children: ReactNode }) {
  const canViewPersonnel = usePermission(permissions.personView);

  return canViewPersonnel ? <>{children}</> : <Navigate to={routes.profile} replace />;
}
