import { ApiError } from '@/shared/api/problemDetails';
import type { ApiSchemas } from '@/shared/api/schemas';

/** Sunucunun actigi oturum. Yenileme jetonu burada DEGIL, `HttpOnly` cerezdedir. */
export type SessionResponse = ApiSchemas['SessionResponse'];

/** Oturumdaki kullanici. */
export interface SessionUser {
  firstName: string;
  lastName: string;
}

/**
 * Oturumun kapanma nedeni; sunucunun hata turundeki `session-ended/<neden>` degeridir
 * (SYG-KMLK-042, `KR-086`).
 */
export type SessionEndReason =
  | 'logged-out'
  | 'signed-in-elsewhere'
  | 'token-reuse'
  | 'idle-timeout'
  | 'expired'
  | 'account-changed'
  | 'password-changed';

const END_REASONS: readonly SessionEndReason[] = [
  'logged-out',
  'signed-in-elsewhere',
  'token-reuse',
  'idle-timeout',
  'expired',
  'account-changed',
  'password-changed',
];

/** Oturum durumu. `unknown`: sayfa yeni acildi, oturum cerezden geri getiriliyor. */
export type SessionState =
  | { status: 'unknown' }
  | { status: 'authenticated'; user: SessionUser }
  | { status: 'anonymous'; endReason?: SessionEndReason | undefined };

/** Oturum uclari. */
export interface SessionApi {
  refresh: () => Promise<SessionResponse>;
  signOut: () => Promise<void>;
  activity: () => Promise<void>;
}

/** Sekmeler arasi ileti kanali (`BroadcastChannel`'in kullanilan kismi). */
export interface SessionChannel {
  postMessage: (message: SessionMessage) => void;
  onmessage: ((event: { data: SessionMessage }) => void) | null;
}

/** Sekmeler arasi iletiler. Jeton ASLA tasinmaz; her sekme kendi jetonunu tutar. */
export type SessionMessage =
  | { type: 'signed-in' }
  | { type: 'ended'; reason?: SessionEndReason | undefined }
  | { type: 'interaction'; at: number }
  | { type: 'signal'; at: number };

export interface SessionManagerOptions {
  api: SessionApi;
  now?: () => number;
  channel?: SessionChannel | null;

  /**
   * Gorevi sekmeler arasinda TEK SEFERDE bir tane calistirir. Varsayilan: tarayicinin
   * `navigator.locks` kilidi; yoksa gorev dogrudan calisir.
   */
  runExclusive?: <T>(task: () => Promise<T>) => Promise<T>;
}

/** Hareketsizlik denetiminin sonucu. */
export interface DeadlineCheck {
  /** Hareketsizlik suresinin dolmasina kalan sure (ms). */
  idleRemainingMs: number;

  /** Uyarinin gosterilecegi pencere (ms). */
  warningWindowMs: number;
}

/** Erisim jetonu bu kadar sure icinde dolacaksa istekten ONCE yenilenir. */
export const TOKEN_REFRESH_MARGIN_MS = 30_000;

/**
 * Etkinlik sinyalleri arasindaki en kisa sure. Sunucu dakikada en fazla 2 sinyal kabul eder
 * (SYG-KMLK-039); istemci dakikada en fazla 1 gonderir ki birden fazla sekme siniri asmasin.
 */
export const ACTIVITY_SIGNAL_INTERVAL_MS = 60_000;

/** Hareketsizlik uyarisi en gec bu kadar once gosterilir. */
export const IDLE_WARNING_MS = 2 * 60_000;

/** Etkilesimin diger sekmelere en sik bildirilme araligi. */
const INTERACTION_BROADCAST_MS = 5_000;

const LOCK_NAME = 'dsg-hrms-session-refresh';
const SESSION_ENDED_TYPE = /session-ended\/([a-z-]+)$/;

interface Tokens {
  accessToken: string;
  /** Tum anlar ISTEMCI saatine cevrilmistir (bkz. `clockSkew`). */
  accessTokenExpiresAt: number;
  sessionExpiresAt: number;
  idleTimeoutMs: number;
}

/**
 * Tarayici tarafindaki oturum (ADR-0006 §8, SYG-KMLK-037…043).
 *
 * <b>Jeton nerede?</b> Erisim jetonu YALNIZCA bu nesnenin bellegindedir; `localStorage`
 * kullanilmaz (XSS'te okunurdu). Sayfa yenilendiginde oturum, tarayicinin gonderdigi
 * `HttpOnly` yenileme cereziyle geri getirilir.
 *
 * <b>Neden yenileme tek seferde bir tane?</b> Yenileme jetonu her kullanimda degisir;
 * kullanilmis jeton tekrar gelirse sunucu bunu calinma sayar ve TUM oturumlari kapatir
 * (SYG-KMLK-040). Ayni anda gelen iki istek ayri ayri yenileseydi ikincisi eski jetonu
 * gonderir ve kullanici kendi kendini disari atardi. Sekme icinde tek bir yenileme
 * paylasilir; sekmeler arasinda yenilemeler kilitle SIRAYA girer. Siradaki sekme, cerez
 * artik yenilendigi icin yeni jetonu gonderir.
 *
 * <b>Hareketsizlik.</b> Sunucu hareketsizligi son etkinlik sinyaline gore olcer
 * (SYG-KMLK-038). Istemci kullanici etkilesimini izler ve dakikada en fazla bir sinyal
 * gonderir; sekmeler son etkilesimi birbirine bildirir, boylece bir sekmede calisan
 * kullanicinin diger sekmesi "hareketsiz" sayilip oturumu kapatmaz.
 */
export class SessionManager {
  private readonly api: SessionApi;
  private readonly now: () => number;
  private readonly channel: SessionChannel | null;
  private readonly runExclusive: <T>(task: () => Promise<T>) => Promise<T>;
  private readonly listeners = new Set<() => void>();

  private state: SessionState = { status: 'unknown' };
  private tokens: Tokens | null = null;
  private refreshing: Promise<string | null> | null = null;
  private lastInteractionAt = 0;
  private lastSignalAt = 0;
  private lastInteractionBroadcastAt = 0;

  constructor(options: SessionManagerOptions) {
    this.api = options.api;
    this.now = options.now ?? Date.now;
    this.channel = options.channel ?? null;
    this.runExclusive = options.runExclusive ?? runWithBrowserLock;

    if (this.channel) {
      this.channel.onmessage = (event) => this.receive(event.data);
    }
  }

  /** Guncel durum (`useSyncExternalStore` icin ayni nesne doner). */
  getState = (): SessionState => this.state;

  /** Durum degisikliklerini dinler. */
  subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  };

  /** Giristen gelen oturumu baslatir; diger sekmeler de oturumu geri getirir. */
  start(session: SessionResponse): void {
    this.accept(session);
    this.recordInteraction();

    // Sunucu giris anini son etkinlik olarak kaydeder; hemen sinyal gondermeye gerek yok.
    this.lastSignalAt = this.lastInteractionAt;
    this.post({ type: 'signed-in' });
  }

  /**
   * Sayfa acilisinda oturumu yenileme cereziyle geri getirir. Oturum yoksa durum
   * `anonymous` olur; ag hatasinda da (kullanici giris ekraninda hatayi gorur).
   */
  async restore(): Promise<void> {
    try {
      const token = await this.refreshAccessToken();
      if (token) {
        // Sayfayi acmak bir kullanici etkilesimidir.
        this.recordInteraction();
        void this.flushActivity();
      }
    } catch {
      if (this.state.status === 'unknown') {
        this.setState({ status: 'anonymous' });
      }
    }
  }

  /** Gecerli erisim jetonu; suresi dolmak uzereyse once yenilenir. */
  getAccessToken = async (): Promise<string | null> => {
    if (!this.tokens) {
      return null;
    }

    if (this.tokens.accessTokenExpiresAt - this.now() > TOKEN_REFRESH_MARGIN_MS) {
      return this.tokens.accessToken;
    }

    return this.refreshAccessToken();
  };

  /**
   * Jetonu yeniler. Ayni anda gelen cagrilar TEK yenilemeyi bekler. Oturum sona erdiyse
   * `null` doner ve oturum nedeniyle kapanir; ag veya sunucu hatasinda hata firlatilir
   * (oturum kapatilmaz, bir sonraki istek yeniden dener).
   */
  refreshAccessToken = (): Promise<string | null> => {
    this.refreshing ??= this.runExclusive(() => this.api.refresh())
      .then((session) => {
        this.accept(session);
        return session.accessToken;
      })
      .catch((error: unknown) => {
        const ended = sessionEndReasonOf(error);
        if (ended === undefined) {
          throw error;
        }

        this.end(ended ?? undefined);
        return null;
      })
      .finally(() => {
        this.refreshing = null;
      });

    return this.refreshing;
  };

  /** Cikis: jeton sunucuda iptal edilir (SYG-KMLK-043); diger sekmeler de girise doner. */
  async signOut(): Promise<void> {
    try {
      await this.api.signOut();
    } catch {
      // Sunucuya ulasilamasa da istemci oturumu kapatilir; jeton zaten bellekten silinir.
    }

    this.end('logged-out');
  }

  /** Kullanici etkilesimi (tiklama, tus, kaydirma) veya oynayan medya. */
  recordInteraction(): void {
    const now = this.now();
    this.lastInteractionAt = now;

    if (now - this.lastInteractionBroadcastAt >= INTERACTION_BROADCAST_MS) {
      this.lastInteractionBroadcastAt = now;
      this.post({ type: 'interaction', at: now });
    }
  }

  /**
   * Son sinyalden bu yana etkilesim olduysa ve sinyal araligi dolduysa etkinlik sinyali
   * gonderir (SYG-KMLK-038, 039).
   */
  async flushActivity(): Promise<void> {
    const now = this.now();
    if (
      this.state.status !== 'authenticated' ||
      this.lastInteractionAt <= this.lastSignalAt ||
      now - this.lastSignalAt < ACTIVITY_SIGNAL_INTERVAL_MS
    ) {
      return;
    }

    this.lastSignalAt = now;
    this.post({ type: 'signal', at: now });

    try {
      await this.api.activity();
    } catch {
      // Sinir asimi (429) veya ag hatasi: bir sonraki sinyal yeniden dener. Oturum
      // kapandiysa API istemcisi yenilemeyi dener ve oturum nedeniyle kapanir.
    }
  }

  /**
   * Toplam sureyi ve hareketsizligi denetler; dolduysa oturumu kapatir ve jetonu sunucuda
   * iptal eder. Oturum yoksa `null`.
   */
  checkDeadlines(): DeadlineCheck | null {
    if (!this.tokens) {
      return null;
    }

    const now = this.now();
    if (now >= this.tokens.sessionExpiresAt) {
      this.expire('expired');
      return null;
    }

    const idleRemainingMs = this.lastInteractionAt + this.tokens.idleTimeoutMs - now;
    if (idleRemainingMs <= 0) {
      this.expire('idle-timeout');
      return null;
    }

    return {
      idleRemainingMs,
      warningWindowMs: Math.min(IDLE_WARNING_MS, this.tokens.idleTimeoutMs / 4),
    };
  }

  /** Giris ekranindaki oturum sonu iletisini temizler. */
  clearEndReason(): void {
    if (this.state.status === 'anonymous' && this.state.endReason) {
      this.setState({ status: 'anonymous' });
    }
  }

  private expire(reason: SessionEndReason): void {
    this.end(reason);

    // Sunucu oturumu kendi de kapatir; iptal, cerezi hemen gecersiz kilmak icindir.
    this.api.signOut().catch(() => undefined);
  }

  private accept(session: SessionResponse): void {
    const skew = clockSkew(session.accessToken, this.now());
    const toLocal = (value: string) => Date.parse(value) + skew;

    this.tokens = {
      accessToken: session.accessToken,
      accessTokenExpiresAt: toLocal(session.accessTokenExpiresAt),
      sessionExpiresAt: toLocal(session.sessionExpiresAt),
      idleTimeoutMs: Number(session.idleTimeoutMinutes) * 60_000,
    };

    const user = { firstName: session.user.firstName, lastName: session.user.lastName };
    const current = this.state;
    if (
      current.status !== 'authenticated' ||
      current.user.firstName !== user.firstName ||
      current.user.lastName !== user.lastName
    ) {
      this.setState({ status: 'authenticated', user });
    }
  }

  private end(reason: SessionEndReason | undefined, broadcast = true): void {
    const previous = this.state.status;
    this.tokens = null;

    if (previous === 'anonymous') {
      return;
    }

    this.setState({ status: 'anonymous', endReason: reason });

    // Yalnizca ACIK bir oturumun kapanisi yayinlanir. Acilista oturumu geri getiremeyen
    // sekme bunu yayinlasaydi, ayni anda acik diger sekmeleri de kapatirdi.
    if (broadcast && previous === 'authenticated') {
      this.post({ type: 'ended', reason });
    }
  }

  private receive(message: SessionMessage): void {
    switch (message.type) {
      case 'signed-in':
        // Baska sekmede giris yapildi: bu sekme de oturumu cerezden geri getirir.
        if (this.state.status === 'anonymous') {
          void this.restore();
        }
        break;
      case 'ended':
        if (this.state.status === 'authenticated') {
          this.end(message.reason, false);
        }
        break;
      case 'interaction':
        this.lastInteractionAt = Math.max(this.lastInteractionAt, message.at);
        break;
      case 'signal':
        this.lastSignalAt = Math.max(this.lastSignalAt, message.at);
        break;
    }
  }

  private post(message: SessionMessage): void {
    try {
      this.channel?.postMessage(message);
    } catch {
      // Kanal kapaliysa diger sekmeler kendi isteklerinde durumu ogrenir.
    }
  }

  private setState(state: SessionState): void {
    this.state = state;
    this.listeners.forEach((listener) => listener());
  }
}

/**
 * Hatanin oturum sonu olup olmadigi. Oturum sonuysa nedeni (bilinmiyorsa `null`), degilse
 * `undefined` doner.
 */
export function sessionEndReasonOf(error: unknown): SessionEndReason | null | undefined {
  if (!(error instanceof ApiError) || error.status !== 401) {
    return undefined;
  }

  const match = error.type ? SESSION_ENDED_TYPE.exec(error.type) : null;
  const reason = match?.[1] as SessionEndReason | undefined;

  return reason && END_REASONS.includes(reason) ? reason : null;
}

/**
 * Istemci saati ile sunucu saati arasindaki fark (ms). Sunucunun verdigi anlar bu farkla
 * istemci saatine cevrilir; aksi hâlde saati birkac dakika sapmis bir bilgisayarda jeton
 * zamaninda yenilenmez veya oturum erken kapanirdi. Jetonun `iat` alani sunucunun
 * saatidir; okunamazsa fark sifir kabul edilir.
 */
export function clockSkew(accessToken: string, now: number): number {
  try {
    const payload = accessToken.split('.')[1];
    if (!payload) {
      return 0;
    }

    const json = atob(payload.replaceAll('-', '+').replaceAll('_', '/'));
    const issuedAt = (JSON.parse(json) as { iat?: unknown }).iat;

    return typeof issuedAt === 'number' ? now - issuedAt * 1000 : 0;
  } catch {
    return 0;
  }
}

/** Gorevi sekmeler arasi kilitle calistirir; tarayici desteklemiyorsa dogrudan. */
function runWithBrowserLock<T>(task: () => Promise<T>): Promise<T> {
  const locks = typeof navigator !== 'undefined' ? navigator.locks : undefined;

  // Kilit, geri cagirmanin sonucunu bekleyip dondurur; tip tanimi bunu ic ice Promise
  // olarak gosterdigi icin sonuc acikca beklenir.
  return locks ? (locks.request(LOCK_NAME, async () => await task()) as Promise<T>) : task();
}
