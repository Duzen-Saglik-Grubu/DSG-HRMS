import { useSyncExternalStore } from 'react';
import { configureAuth } from '@/shared/api/client';
import { sessionApi } from './sessionApi';
import { SessionManager, type SessionChannel, type SessionState } from './sessionManager';

function createChannel(): SessionChannel | null {
  // Eski tarayicida kanal yoksa sekmeler durumu kendi isteklerinde ogrenir.
  return typeof BroadcastChannel === 'function'
    ? (new BroadcastChannel('dsg-hrms-session') as unknown as SessionChannel)
    : null;
}

/** Uygulamanin TEK oturumu. */
export const session = new SessionManager({ api: sessionApi, channel: createChannel() });

configureAuth({
  getAccessToken: session.getAccessToken,
  refreshAccessToken: session.refreshAccessToken,
});

/** Oturum durumu; degistiginde bilesen yeniden cizilir. */
export function useSession(): SessionState {
  return useSyncExternalStore(session.subscribe, session.getState);
}
