import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import {
  ACTIVITY_SIGNAL_INTERVAL_MS,
  clockSkew,
  SessionManager,
  sessionEndReasonOf,
  type SessionApi,
  type SessionChannel,
  type SessionMessage,
  type SessionResponse,
} from '../sessionManager';

/**
 * Tarayici tarafindaki oturum (SYG-KMLK-037…043). Saat elle ilerletilir; sunucu, sekmeler
 * arasi kanal ve kilit sahtedir.
 */
const T0 = Date.parse('2026-09-28T09:00:00Z');
const MINUTE = 60_000;

/** Sunucunun `iat` alani verilen an olan, imzasiz sahte bir erisim jetonu. */
function token(issuedAtMs: number, id = 'a'): string {
  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '');

  return `${encode({ alg: 'HS256' })}.${encode({ iat: Math.floor(issuedAtMs / 1000), jti: id })}.imza`;
}

function sessionAt(serverNow: number, id = 'a', idleMinutes = 30): SessionResponse {
  return {
    accessToken: token(serverNow, id),
    accessTokenExpiresAt: new Date(serverNow + 15 * MINUTE).toISOString(),
    sessionExpiresAt: new Date(serverNow + 8 * 60 * MINUTE).toISOString(),
    idleTimeoutMinutes: idleMinutes,
    user: { firstName: 'Ahmet', lastName: 'Yılmaz', permissions: [] },
  };
}

function ended(reason: string): ApiError {
  return new ApiError({
    message: 'Oturum sona erdi',
    status: 401,
    isNetworkError: false,
    type: `https://dsg-hrms/errors/session-ended/${reason}`,
  });
}

/** Ayni tarayicidaki iki sekmenin kanali: biri gonderince digeri alir. */
function linkedChannels(): [SessionChannel, SessionChannel] {
  const a: SessionChannel = { postMessage: (m) => b.onmessage?.({ data: m }), onmessage: null };
  const b: SessionChannel = { postMessage: (m) => a.onmessage?.({ data: m }), onmessage: null };
  return [a, b];
}

let now: number;
let api: { [K in keyof SessionApi]: ReturnType<typeof vi.fn<SessionApi[K]>> };

function manager(options: { channel?: SessionChannel } = {}) {
  return new SessionManager({ api, now: () => now, channel: options.channel ?? null });
}

beforeEach(() => {
  now = T0;
  api = {
    refresh: vi.fn<SessionApi['refresh']>().mockResolvedValue(sessionAt(T0, 'yenilenen')),
    signOut: vi.fn<SessionApi['signOut']>().mockResolvedValue(undefined),
    activity: vi.fn<SessionApi['activity']>().mockResolvedValue(undefined),
  };
});

describe('SessionManager — jeton', () => {
  it('giristen sonra jetonu bellekten verir; suresi dolmak uzereyse once yeniler', async () => {
    const session = manager();
    session.start(sessionAt(T0, 'ilk'));

    expect(session.getState()).toEqual({
      status: 'authenticated',
      user: { firstName: 'Ahmet', lastName: 'Yılmaz', permissions: [] },
    });
    expect(await session.getAccessToken()).toBe(token(T0, 'ilk'));
    expect(api.refresh).not.toHaveBeenCalled();

    now = T0 + 14.6 * MINUTE;
    expect(await session.getAccessToken()).toBe(token(T0, 'yenilenen'));
    expect(api.refresh).toHaveBeenCalledOnce();
  });

  it('ayni anda gelen yenilemeler TEK istek olur (jeton tekrari yasanmaz, SYG-KMLK-040)', async () => {
    let resolve!: (value: SessionResponse) => void;
    api.refresh.mockReturnValue(new Promise((r) => (resolve = r)));
    const session = manager();
    session.start(sessionAt(T0));

    const calls = [
      session.refreshAccessToken(),
      session.refreshAccessToken(),
      session.refreshAccessToken(),
    ];
    resolve(sessionAt(T0, 'yeni'));

    expect(await Promise.all(calls)).toEqual(Array(3).fill(token(T0, 'yeni')));
    expect(api.refresh).toHaveBeenCalledOnce();
  });

  it('yenileme sekmeler arasi kilitle calisir', async () => {
    const locked = vi.fn();
    const runExclusive = <T>(task: () => Promise<T>): Promise<T> => {
      locked();
      return task();
    };
    const session = new SessionManager({ api, now: () => now, runExclusive });
    session.start(sessionAt(T0));

    await session.refreshAccessToken();

    expect(locked).toHaveBeenCalledOnce();
  });

  it('oturum baska cihazdan giriste kapandiysa nedeniyle anonim olur (SYG-KMLK-042)', async () => {
    api.refresh.mockRejectedValue(ended('signed-in-elsewhere'));
    const session = manager();
    session.start(sessionAt(T0));

    expect(await session.refreshAccessToken()).toBeNull();
    expect(session.getState()).toEqual({ status: 'anonymous', endReason: 'signed-in-elsewhere' });
    expect(await session.getAccessToken()).toBeNull();
  });

  it('ag hatasinda oturum KAPANMAZ; hata cagirana doner', async () => {
    const network = new ApiError({ message: 'ag', isNetworkError: true });
    api.refresh.mockRejectedValueOnce(network);
    const session = manager();
    session.start(sessionAt(T0));

    await expect(session.refreshAccessToken()).rejects.toBe(network);
    expect(session.getState().status).toBe('authenticated');
    expect(await session.refreshAccessToken()).toBe(token(T0, 'yenilenen'));
  });

  it('sunucu saati sapmissa jetonun bitisi istemci saatine gore hesaplanir', async () => {
    // Sunucu 10 dakika ileride: sapma dikkate alinmasaydi jeton hic yenilenmez sanilirdi.
    const session = manager();
    session.start(sessionAt(T0 + 10 * MINUTE, 'ilk'));

    now = T0 + 14.6 * MINUTE;
    expect(await session.getAccessToken()).toBe(token(T0, 'yenilenen'));
  });
});

describe('SessionManager — acilis ve cikis', () => {
  it('sayfa acilisinda oturumu cerezden geri getirir ve etkinlik bildirir', async () => {
    const session = manager();
    expect(session.getState().status).toBe('unknown');

    await session.restore();

    expect(session.getState().status).toBe('authenticated');
    expect(api.activity).toHaveBeenCalledOnce();
  });

  it('acilista durum degisikligiyle ayni anda calisan hareketsizlik denetimi oturumu KAPATMAZ (#102)', async () => {
    // React, oturum durumu degisince uygulama kabugunu HEMEN cizer ve hareketsizlik denetimi
    // calisir; restore() henuz sonraki satirina gecmemistir.
    const session = manager();
    session.subscribe(() => {
      if (session.getState().status === 'authenticated') {
        session.checkDeadlines();
      }
    });

    await session.restore();

    expect(session.getState().status).toBe('authenticated');
    expect(api.signOut).not.toHaveBeenCalled();
  });

  it('cerez yoksa ileti gostermeden anonim olur', async () => {
    api.refresh.mockRejectedValue(ended('invalid'));
    const session = manager();

    await session.restore();

    expect(session.getState()).toEqual({ status: 'anonymous', endReason: undefined });
  });

  it('acilista sunucuya ulasilamazsa anonim olur', async () => {
    api.refresh.mockRejectedValue(new ApiError({ message: 'ag', isNetworkError: true }));
    const session = manager();

    await session.restore();

    expect(session.getState()).toEqual({ status: 'anonymous' });
  });

  it('cikista jeton sunucuda iptal edilir; sunucuya ulasilamasa da oturum kapanir', async () => {
    api.signOut.mockRejectedValue(new Error('ag'));
    const session = manager();
    session.start(sessionAt(T0));

    await session.signOut();

    expect(api.signOut).toHaveBeenCalledOnce();
    expect(session.getState()).toEqual({ status: 'anonymous', endReason: 'logged-out' });
  });

  it('oturum sonu iletisi yeni giris denemesinde temizlenir', async () => {
    const session = manager();
    session.start(sessionAt(T0));
    await session.signOut();

    session.clearEndReason();

    expect(session.getState()).toEqual({ status: 'anonymous' });
  });
});

describe('SessionManager — etkinlik ve hareketsizlik (SYG-KMLK-038, 039)', () => {
  it('girisin hemen ardindan sinyal gondermez; sunucu giris anini zaten kaydeder', async () => {
    const session = manager();
    session.start(sessionAt(T0));

    await session.flushActivity();

    expect(api.activity).not.toHaveBeenCalled();
  });

  it('etkilesim olduysa dakikada en fazla bir sinyal gonderir', async () => {
    const session = manager();
    session.start(sessionAt(T0));

    now += ACTIVITY_SIGNAL_INTERVAL_MS;
    session.recordInteraction();
    await session.flushActivity();
    now += 10_000;
    session.recordInteraction();
    await session.flushActivity();

    expect(api.activity).toHaveBeenCalledOnce();

    now += ACTIVITY_SIGNAL_INTERVAL_MS;
    await session.flushActivity();
    expect(api.activity).toHaveBeenCalledTimes(2);
  });

  it('etkilesim yoksa sinyal gondermez (arka plan etkinlik sayilmaz)', async () => {
    const session = manager();
    session.start(sessionAt(T0));

    now += 5 * MINUTE;
    await session.flushActivity();

    expect(api.activity).not.toHaveBeenCalled();
  });

  it('sinyal reddedilirse (429) oturum kapanmaz', async () => {
    api.activity.mockRejectedValue(
      new ApiError({ message: 'cok sik', status: 429, isNetworkError: false }),
    );
    const session = manager();
    session.start(sessionAt(T0));

    now += ACTIVITY_SIGNAL_INTERVAL_MS;
    session.recordInteraction();
    await session.flushActivity();

    expect(session.getState().status).toBe('authenticated');
  });

  it('hareketsizlik suresinin son iki dakikasinda uyari penceresine girer', () => {
    const session = manager();
    session.start(sessionAt(T0));

    now = T0 + 27 * MINUTE;
    expect(session.checkDeadlines()).toEqual({
      idleRemainingMs: 3 * MINUTE,
      warningWindowMs: 2 * MINUTE,
    });

    now = T0 + 28.5 * MINUTE;
    expect(session.checkDeadlines()?.idleRemainingMs).toBe(1.5 * MINUTE);
  });

  it('hareketsizlik dolunca oturumu kapatir ve jetonu sunucuda iptal eder', () => {
    const session = manager();
    session.start(sessionAt(T0));

    now = T0 + 30 * MINUTE;

    expect(session.checkDeadlines()).toBeNull();
    expect(session.getState()).toEqual({ status: 'anonymous', endReason: 'idle-timeout' });
    expect(api.signOut).toHaveBeenCalledOnce();
  });

  it('etkilesim hareketsizlik sayacini sifirlar ama 8 saati gecemez', () => {
    const session = manager();
    session.start(sessionAt(T0));

    for (let minute = 20; minute < 8 * 60; minute += 20) {
      now = T0 + minute * MINUTE;
      session.recordInteraction();
      expect(session.checkDeadlines()).not.toBeNull();
    }

    now = T0 + 8 * 60 * MINUTE;
    expect(session.checkDeadlines()).toBeNull();
    expect(session.getState()).toEqual({ status: 'anonymous', endReason: 'expired' });
  });

  it('kisa hareketsizlik suresinde uyari penceresi surenin dortte biridir', () => {
    const session = manager();
    session.start(sessionAt(T0, 'a', 4));

    expect(session.checkDeadlines()?.warningWindowMs).toBe(MINUTE);
  });
});

describe('SessionManager — sekmeler arasi', () => {
  it('bir sekmedeki etkilesim diger sekmeyi hareketsiz saydirmaz', () => {
    const [left, right] = linkedChannels();
    const a = manager({ channel: left });
    const b = manager({ channel: right });
    a.start(sessionAt(T0));
    b.start(sessionAt(T0));

    now = T0 + 25 * MINUTE;
    a.recordInteraction();
    now = T0 + 31 * MINUTE;

    expect(b.checkDeadlines()).not.toBeNull();
  });

  it('bir sekmede cikis yapilinca diger sekme de girise doner', async () => {
    const [left, right] = linkedChannels();
    const a = manager({ channel: left });
    const b = manager({ channel: right });
    a.start(sessionAt(T0));
    b.start(sessionAt(T0));

    await a.signOut();

    expect(b.getState()).toEqual({ status: 'anonymous', endReason: 'logged-out' });
    expect(api.signOut).toHaveBeenCalledOnce();
  });

  it('bir sekmede giris yapilinca girisi bekleyen sekme oturumu geri getirir', async () => {
    const [left, right] = linkedChannels();
    const a = manager({ channel: left });
    const b = manager({ channel: right });
    api.refresh.mockRejectedValueOnce(ended('invalid'));
    await b.restore();
    expect(b.getState().status).toBe('anonymous');

    a.start(sessionAt(T0));

    await vi.waitFor(() => expect(b.getState().status).toBe('authenticated'));
  });

  it('acilista oturumu bulamayan sekme diger sekmeleri KAPATMAZ', async () => {
    const posted: SessionMessage[] = [];
    const channel: SessionChannel = { postMessage: (m) => posted.push(m), onmessage: null };
    api.refresh.mockRejectedValue(ended('logged-out'));

    await manager({ channel }).restore();

    expect(posted).toEqual([]);
  });

  it('sinyal zamani paylasilir; iki sekme dakikalik siniri asmaz', async () => {
    const [left, right] = linkedChannels();
    const a = manager({ channel: left });
    const b = manager({ channel: right });
    a.start(sessionAt(T0));
    b.start(sessionAt(T0));

    now += ACTIVITY_SIGNAL_INTERVAL_MS;
    a.recordInteraction();
    await a.flushActivity();
    b.recordInteraction();
    await b.flushActivity();

    expect(api.activity).toHaveBeenCalledOnce();
  });
});

describe('yardimcilar', () => {
  it('oturum sonu nedenini hata turunden okur', () => {
    expect(sessionEndReasonOf(ended('token-reuse'))).toBe('token-reuse');
    expect(sessionEndReasonOf(ended('invalid'))).toBeNull();
    expect(sessionEndReasonOf(ended('bilinmeyen'))).toBeNull();
    expect(
      sessionEndReasonOf(new ApiError({ message: 'x', status: 401, isNetworkError: false })),
    ).toBeNull();
    expect(
      sessionEndReasonOf(new ApiError({ message: 'x', status: 500, isNetworkError: false })),
    ).toBeUndefined();
    expect(sessionEndReasonOf(new Error('x'))).toBeUndefined();
  });

  it('saat farkini jetonun iat alanindan hesaplar; okunamazsa sifir', () => {
    expect(clockSkew(token(T0 - 5_000), T0)).toBe(5_000);
    expect(clockSkew('bozuk', T0)).toBe(0);
    expect(clockSkew('a.!!!.b', T0)).toBe(0);
    expect(clockSkew(`x.${btoa('{"sub":"1"}')}.y`, T0)).toBe(0);
  });
});
