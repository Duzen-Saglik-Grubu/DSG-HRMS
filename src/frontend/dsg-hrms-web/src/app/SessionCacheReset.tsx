import { useEffect, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useSession } from '@/shared/auth/session';

/**
 * Oturum kapaninca sorgu onbellegini bosaltir.
 *
 * Kurumda bilgisayarlar ortak kullanilabilir: cikis yapan kullanicinin verisi bellekte
 * kalsaydi, ayni sekmede giris yapan bir sonraki kisi onu bir an icin gorebilirdi.
 */
export function SessionCacheReset() {
  const queryClient = useQueryClient();
  const state = useSession();
  const wasAuthenticated = useRef(false);

  useEffect(() => {
    if (state.status === 'authenticated') {
      wasAuthenticated.current = true;
      return;
    }

    if (state.status === 'anonymous' && wasAuthenticated.current) {
      wasAuthenticated.current = false;
      queryClient.clear();
    }
  }, [state, queryClient]);

  return null;
}
