import axios, { AxiosError, type AxiosInstance } from 'axios';
import i18n from '@/shared/i18n/i18n';
import { toApiError } from './problemDetails';

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

  const baytlar = new Uint8Array(16);
  crypto.getRandomValues(baytlar);

  return Array.from(baytlar, (bayt) => bayt.toString(16).padStart(2, '0')).join('');
}

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
  (response) => response,
  (error: unknown) => {
    if (!(error instanceof AxiosError)) {
      return Promise.reject(toApiError(undefined, undefined, i18n.t('hata.genel')));
    }

    const status = error.response?.status;

    // Sunucuya hic ulasilamadi. Kullaniciya "bir hata olustu" demek yerine ne
    // yapacagini soyleriz (ADR-0015 §6).
    if (status === undefined) {
      return Promise.reject(toApiError(undefined, undefined, i18n.t('hata.ag')));
    }

    return Promise.reject(toApiError(error.response?.data, status, defaultMessageFor(status)));
  },
);

/**
 * Sunucu bir mesaj dondurmediyse duruma uygun Turkce mesaj uretir.
 */
function defaultMessageFor(status: number): string {
  switch (status) {
    case 403:
      return i18n.t('hata.yetki');
    case 404:
      return i18n.t('hata.bulunamadi');
    case 409:
      return i18n.t('hata.cakisma');
    default:
      return i18n.t('hata.genel');
  }
}
