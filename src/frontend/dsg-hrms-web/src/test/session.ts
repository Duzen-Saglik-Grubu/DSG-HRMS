import { vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { session } from '@/shared/auth/session';
import { sessionApi } from '@/shared/auth/sessionApi';
import type {
  PasswordChangeReason,
  SessionEndReason,
  SessionResponse,
} from '@/shared/auth/sessionManager';

/** Sahte oturum yaniti. Jeton imzasizdir ve `iat` tasimaz (saat farki sifir sayilir). */
export function fakeSession(
  firstName = 'Ahmet',
  lastName = 'Yılmaz',
  permissions: string[] = [],
  passwordChangeRequired: PasswordChangeReason | null = null,
): SessionResponse {
  const now = Date.now();

  return {
    accessToken: 'test.jeton.imza',
    accessTokenExpiresAt: new Date(now + 15 * 60_000).toISOString(),
    sessionExpiresAt: new Date(now + 8 * 60 * 60_000).toISOString(),
    idleTimeoutMinutes: 30,
    user: { firstName, lastName, permissions },
    passwordChangeRequired,
  };
}

/** Sunucunun "oturum sona erdi" yaniti. */
export function sessionEnded(reason: SessionEndReason | 'invalid'): ApiError {
  return new ApiError({
    message: 'Oturum sona erdi',
    status: 401,
    isNetworkError: false,
    type: `https://dsg-hrms/errors/session-ended/${reason}`,
  });
}

/**
 * Uygulamanin oturumunu istenen duruma getirir. Oturum uclari sahtedir; hicbir istek
 * sunucuya gitmez.
 */
export async function setSession(
  target:
    | { status: 'anonymous'; endReason?: SessionEndReason }
    | {
        status: 'authenticated';
        permissions?: string[];
        passwordChangeRequired?: PasswordChangeReason;
      },
): Promise<void> {
  vi.spyOn(sessionApi, 'signOut').mockResolvedValue(undefined);
  vi.spyOn(sessionApi, 'activity').mockResolvedValue(undefined);

  await session.signOut();
  session.clearEndReason();

  if (target.status === 'authenticated') {
    session.start(
      fakeSession('Ahmet', 'Yılmaz', target.permissions, target.passwordChangeRequired ?? null),
    );
    return;
  }

  if (target.endReason) {
    session.start(fakeSession());
    vi.spyOn(sessionApi, 'refresh').mockRejectedValueOnce(sessionEnded(target.endReason));
    await session.refreshAccessToken();
  }
}
