import { act, fireEvent, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test/render';
import { SessionActivity } from '../SessionActivity';
import { SessionManager, type SessionApi, type SessionResponse } from '../sessionManager';

/**
 * Etkinlik sinyali ve hareketsizlik uyarisi (SYG-KMLK-038, 039). Saat sahtedir.
 */
const MINUTE = 60_000;
const T0 = Date.parse('2026-09-28T09:00:00Z');

function sessionResponse(): SessionResponse {
  return {
    accessToken: 'test.jeton.imza',
    accessTokenExpiresAt: new Date(T0 + 15 * MINUTE).toISOString(),
    sessionExpiresAt: new Date(T0 + 8 * 60 * MINUTE).toISOString(),
    idleTimeoutMinutes: 30,
    user: { firstName: 'Ahmet', lastName: 'Yılmaz', permissions: [] },
    passwordChangeRequired: null,
  };
}

let api: { [K in keyof SessionApi]: ReturnType<typeof vi.fn<SessionApi[K]>> };
let manager: SessionManager;

async function advance(ms: number) {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ms);
  });
}

function addVideo(playing: boolean) {
  const video = document.createElement('video');
  Object.defineProperty(video, 'paused', { value: !playing });
  Object.defineProperty(video, 'ended', { value: false });
  document.body.append(video);
  return video;
}

beforeEach(() => {
  vi.useFakeTimers({
    toFake: ['setTimeout', 'clearTimeout', 'setInterval', 'clearInterval', 'Date'],
  });
  vi.setSystemTime(T0);
  api = {
    refresh: vi.fn<SessionApi['refresh']>().mockResolvedValue(sessionResponse()),
    signOut: vi.fn<SessionApi['signOut']>().mockResolvedValue(undefined),
    activity: vi.fn<SessionApi['activity']>().mockResolvedValue(undefined),
  };
  manager = new SessionManager({ api });
  manager.start(sessionResponse());
});

afterEach(() => {
  document.querySelectorAll('video').forEach((video) => video.remove());
  vi.useRealTimers();
});

describe('SessionActivity', () => {
  it('kullanici etkilesiminde dakikada en fazla bir sinyal gonderir', async () => {
    renderWithProviders(<SessionActivity manager={manager} />);

    await advance(MINUTE);
    fireEvent.keyDown(window, { key: 'a' });
    fireEvent.pointerDown(window);
    fireEvent.keyDown(window, { key: 'b' });
    await advance(0);

    expect(api.activity).toHaveBeenCalledOnce();
  });

  it('etkilesim yoksa sinyal gondermez', async () => {
    renderWithProviders(<SessionActivity manager={manager} />);

    await advance(10 * MINUTE);

    expect(api.activity).not.toHaveBeenCalled();
  });

  it('gorunmeyen sekmedeki etkilesim sayilmaz', async () => {
    const visibility = vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden');
    renderWithProviders(<SessionActivity manager={manager} />);

    await advance(MINUTE);
    fireEvent.keyDown(window, { key: 'a' });
    await advance(MINUTE);

    expect(api.activity).not.toHaveBeenCalled();
    visibility.mockRestore();
  });

  it('video oynarken etkilesim olmasa da sinyal gonderir; duraklatilinca gondermez (SYG-KMLK-039)', async () => {
    const video = addVideo(true);
    renderWithProviders(<SessionActivity manager={manager} />);

    await advance(2 * MINUTE);
    expect(api.activity).toHaveBeenCalled();

    video.remove();
    addVideo(false);
    const calls = api.activity.mock.calls.length;
    await advance(5 * MINUTE);

    expect(api.activity).toHaveBeenCalledTimes(calls);
  });

  it('video oynasa bile sekme gorunmuyorsa sinyal gondermez', async () => {
    addVideo(true);
    const visibility = vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden');
    renderWithProviders(<SessionActivity manager={manager} />);

    await advance(5 * MINUTE);

    expect(api.activity).not.toHaveBeenCalled();
    visibility.mockRestore();
  });

  it('hareketsizlik dolmadan once uyarir; "Oturumu surdur" oturumu surdurur', async () => {
    renderWithProviders(<SessionActivity manager={manager} />);

    await advance(28 * MINUTE + 30_000);

    expect(screen.getByRole('dialog', { name: 'Oturumunuz kapanmak üzere' })).toBeInTheDocument();
    expect(screen.getByText(/1:30 içinde kapanacak/)).toBeInTheDocument();

    // Gercek bir tiklama once pointerdown uretir.
    const button = screen.getByRole('button', { name: 'Oturumu sürdür' });
    fireEvent.pointerDown(button);
    fireEvent.click(button);
    await advance(1_000);

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(api.activity).toHaveBeenCalledOnce();
    expect(manager.getState().status).toBe('authenticated');
  });

  it('uyariya yanit verilmezse oturumu kapatir', async () => {
    renderWithProviders(<SessionActivity manager={manager} />);

    await advance(30 * MINUTE);

    expect(manager.getState()).toEqual({ status: 'anonymous', endReason: 'idle-timeout' });
    expect(api.signOut).toHaveBeenCalledOnce();
  });

  it('uyaridan cikis yapilabilir', async () => {
    renderWithProviders(<SessionActivity manager={manager} />);

    await advance(29 * MINUTE);
    fireEvent.click(screen.getByRole('button', { name: 'Çıkış yap' }));
    await advance(0);

    expect(manager.getState()).toEqual({ status: 'anonymous', endReason: 'logged-out' });
  });
});
