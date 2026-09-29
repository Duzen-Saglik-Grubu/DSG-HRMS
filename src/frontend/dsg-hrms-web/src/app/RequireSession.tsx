import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { useSession } from '@/shared/auth/session';
import { RETURN_TO_PARAM, routes } from '@/shared/routes';
import { RouteFallback } from './pages/RouteFallback';

interface RequireSessionProps {
  children: ReactNode;
}

/**
 * Oturum gerektiren sayfalar (SYG-KMLK-055).
 *
 * Oturum yoksa giris ekranina gidilir ve istenen adres `returnTo` ile tasinir; giristen sonra
 * kullanici kaldigi yere doner. Kullanici kendisi cikis yaptiysa adres tasinmaz: cikistan
 * sonraki girisin ayni sayfaya donmesi beklenmez.
 *
 * Bu bir KOLAYLIKTIR, guvenlik onlemi degildir: veriyi koruyan, her istekte jetonu ve oturumu
 * denetleyen sunucudur.
 */
export function RequireSession({ children }: RequireSessionProps) {
  const state = useSession();
  const location = useLocation();

  if (state.status === 'unknown') {
    return <RouteFallback />;
  }

  if (state.status === 'anonymous') {
    const target =
      state.endReason === 'logged-out'
        ? routes.login
        : `${routes.login}?${new URLSearchParams({
            [RETURN_TO_PARAM]: location.pathname + location.search,
          }).toString()}`;

    return <Navigate to={target} replace />;
  }

  return children;
}
