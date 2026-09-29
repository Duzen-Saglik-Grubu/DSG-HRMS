import type { Permission } from './permissions';
import { useSession } from './session';

/**
 * Oturumdaki kullanicinin izni var mi (ADR-0007). Oturum yoksa `false`. Yalnizca gosterim
 * kararlari icindir; denetim sunucudadir.
 */
export function usePermission(permission: Permission): boolean {
  const state = useSession();

  return state.status === 'authenticated' && state.user.permissions.includes(permission);
}
