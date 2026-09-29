import axios, { AxiosError, type AxiosInstance } from 'axios';
import i18n from '@/shared/i18n/i18n';
import { toApiError } from './problemDetails';
import { recordServerDate } from './serverClock';

/**
 * Uygulamadaki TEK Axios ornegi (ADR-0015 §3).
 *
 * Bilesenler dogrudan `axios` veya `fetch` cagirmaz; her istek buradan gecer.
 * Merkezi olmasinin nedeni, ortak davranisin (jeton ekleme, hata cevrimi,
 * izleme kimligi) tek yerde yasamasidir - her cagri yerinde tekrarlansaydi
 * bir yerde unutulur ve kullanici tutarsiz davranis gorurdu.
 */
export const apiClient: AxiosInstance = axios.create({
  baseURL: '/api/v1',
  timeout: 30_000,
  headers: { 'Content-Type': 'application/json' },
});

/**
 * Istek basina uretilen izleme kimliginin tasindigi baslik.
 * Backend bu degeri dogrular ve gunluk kaydiyla eslestirir (ADR-0009 §2).
 */
export const CORRELATION_HEADER = 'X-Correlation-Id';

/**
 * Tarayici tarafinda istek kimligi uretir (32 onaltilik karakter).
 *
 * <b>Neden `crypto.randomUUID` tek basina kullanilmaz?</b> Bu islev yalnizca
 * GUVENLI BAGLAMDA (HTTPS veya `localhost`) tanimlidir. Duz HTTP uzerinden bir
 * alan adiyla acildiginda `undefined` olur ve cagri hata firlatir. Gelistirmede
 * `localhost` guvenli baglam sayildigi icin bu durum ancak sunucuya
 * dagitildiginda gorunur hâle gelir.
 *
 * `crypto.getRandomValues` boyle bir kisitlamaya tabi degildir; yedek olarak
 * kullanilir.
 */
function createCorrelationId(): string {
  if (typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID().replaceAll('-', '');
  }

  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);

  return Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('');
}

declare module 'axios' {
  interface AxiosRequestConfig {
    /**
     * Istege erisim jetonu eklenmez ve `401` yanitinda jeton yenilenmez. Giris, jeton
     * yenileme ve cikis istekleri icindir: bu istekler oturumun KENDISIDIR; kendi
     * `401`'lerinde yenileme denemek sonsuz donguye yol acardi.
     */
    skipAuth?: boolean;

    /** Istek yenilenmis jetonla bir kez tekrarlandi; ikinci kez tekrarlanmaz. */
    authRetried?: boolean;
  }
}

/**
 * Oturum yonetiminin API istemcisine sagladigi islevler.
 *
 * Istemci oturum modulunu ICE AKTARMAZ; oturum modulu kendini buraya kaydeder
 * (`configureAuth`). Aksi hâlde iki modul birbirini ice aktarirdi: oturum modulu jeton
 * yenilemek icin bu istemciyi kullanir.
 */
export interface AuthHandlers {
  /** Gecerli erisim jetonu; suresi dolmak uzereyse once yenilenir. Oturum yoksa `null`. */
  getAccessToken: () => Promise<string | null>;

  /** Jetonu yeniler. Oturum sona erdiyse `null`. */
  refreshAccessToken: () => Promise<string | null>;
}

let auth: AuthHandlers | null = null;

/** Oturum yonetimini istemciye baglar (ADR-0015 §3). */
export function configureAuth(handlers: AuthHandlers | null): void {
  auth = handlers;
}

const AUTHORIZATION_HEADER = 'Authorization';

apiClient.interceptors.request.use(async (config) => {
  // Tekrarlanan istek yenilenmis jetonu zaten tasir; uzerine yazilmaz.
  if (!config.skipAuth && !config.authRetried && auth) {
    let token: string | null = null;

    try {
      token = await auth.getAccessToken();
    } catch {
      // Jeton yenilenemedi (ag hatasi). Istek jetonsuz gider; sunucunun 401'i asagida
      // ele alinir ve kullanici ag hatasi iletisini gorur.
    }

    if (token) {
      config.headers.set(AUTHORIZATION_HEADER, `Bearer ${token}`);
    }
  }

  return config;
});

apiClient.interceptors.request.use((config) => {
  // Izleme kimligi bir KOLAYLIKTIR: destek talebini gunluk kaydiyla eslestirir.
  // Uretilemiyorsa istek yine de gitmelidir - yardimci bir ozelligin asil islevi
  // engellemesi kabul edilemez. Bu hata bir kez yasandi (#39).
  try {
    config.headers.set(CORRELATION_HEADER, createCorrelationId());
  } catch {
    // Kimlik uretilemedi; istek kimliksiz devam eder.
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => {
    recordServerDate(response.headers['date']);
    return response;
  },
  async (error: unknown) => {
    if (error instanceof AxiosError) {
      recordServerDate(error.response?.headers['date']);
    }

    const retried = await retryWithFreshToken(error);
    if (retried) {
      return retried;
    }

    if (!(error instanceof AxiosError)) {
      return Promise.reject(toApiError(undefined, undefined, i18n.t('error.generic')));
    }

    const status = error.response?.status;

    // Sunucuya hic ulasilamadi. Kullaniciya "bir hata olustu" demek yerine ne
    // yapacagini soyleriz (ADR-0015 §6).
    if (status === undefined) {
      return Promise.reject(toApiError(undefined, undefined, i18n.t('error.network')));
    }

    return Promise.reject(toApiError(error.response?.data, status, defaultMessageFor(status)));
  },
);

/**
 * `401` yanitinda jetonu yeniler ve istegi BIR KEZ tekrarlar (ADR-0015 §3).
 *
 * Jeton, istemcinin beklediginden once gecersiz olabilir: sunucu her istekte oturumun acik
 * oldugunu da denetler (`KR-086`). Yenileme oturumun kapandigini soylerse oturum modulu
 * kullaniciyi girise dondurur; istek tekrarlanmaz.
 */
async function retryWithFreshToken(error: unknown) {
  if (!(error instanceof AxiosError) || error.response?.status !== 401 || !auth) {
    return undefined;
  }

  const config = error.config;
  if (!config || config.skipAuth || config.authRetried) {
    return undefined;
  }

  let token: string | null;
  try {
    token = await auth.refreshAccessToken();
  } catch {
    return undefined;
  }

  if (!token) {
    return undefined;
  }

  config.authRetried = true;
  config.headers.set(AUTHORIZATION_HEADER, `Bearer ${token}`);

  return apiClient.request(config);
}

/**
 * Sunucu bir mesaj dondurmediyse duruma uygun Turkce mesaj uretir.
 */
function defaultMessageFor(status: number): string {
  switch (status) {
    case 403:
      return i18n.t('error.forbidden');
    case 404:
      return i18n.t('error.notFound');
    case 409:
      return i18n.t('error.conflict');
    default:
      return i18n.t('error.generic');
  }
}
