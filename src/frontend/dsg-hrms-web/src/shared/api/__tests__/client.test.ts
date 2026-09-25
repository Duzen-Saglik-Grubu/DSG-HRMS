import { afterEach, describe, expect, it } from 'vitest';
import axios, { AxiosError, type AxiosAdapter, type AxiosResponse } from 'axios';
import { apiClient, CORRELATION_HEADER } from '../client';
import { fetchHealth } from '../health';
import type { ApiError } from '../problemDetails';

/**
 * Axios ara katmanlarinin davranisi (ADR-0015 §3).
 *
 * Bu ara katmanlar uygulamadaki TUM isteklerin ortak davranisini tasir; bir
 * hatasi tek bir ekrani degil, uygulamanin tamamini etkiler. Bu yuzden hata
 * cevrimi ve izleme kimligi burada dogrudan olculur.
 */

const defaultAdapter = apiClient.defaults.adapter;

/** Sunucu yerine gecen sahte adapter kurar. */
function fakeResponse(
  handler: (config: Parameters<AxiosAdapter>[0]) => Promise<AxiosResponse> | AxiosResponse,
): void {
  apiClient.defaults.adapter = async (config) => handler(config);
}

/** Belirtilen durum kodu ve govdeyle hata donduren adapter. */
function fakeError(status: number, data: unknown): void {
  apiClient.defaults.adapter = (config) =>
    Promise.reject(
      new AxiosError('istek basarisiz', String(status), config, null, {
        status,
        statusText: '',
        headers: {},
        config,
        data,
      } as AxiosResponse),
    );
}

afterEach(() => {
  // exactOptionalPropertyTypes acik oldugu icin "undefined atama" yerine alan
  // tamamen kaldirilir; boylece Axios kendi varsayilan adapter'ina doner.
  if (defaultAdapter === undefined) {
    delete apiClient.defaults.adapter;
  } else {
    apiClient.defaults.adapter = defaultAdapter;
  }
});

describe('apiClient', () => {
  it('her istege izleme kimligi ekler', async () => {
    let sentConfig: string | undefined;

    fakeResponse((config) => {
      sentConfig = config.headers.get(CORRELATION_HEADER) as string;

      return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
    });

    await apiClient.get('/deneme');

    // Kimlik olmadan, kullanicinin bildirdigi bir sorun gunluk kaydiyla
    // eslestirilemezdi (ADR-0009 §2).
    expect(sentConfig).toMatch(/^[0-9a-f]{32}$/);
  });

  it('her istekte FARKLI bir izleme kimligi uretir', async () => {
    const ids: string[] = [];

    fakeResponse((config) => {
      ids.push(config.headers.get(CORRELATION_HEADER) as string);

      return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
    });

    await apiClient.get('/deneme');
    await apiClient.get('/deneme');

    expect(ids[0]).not.toBe(ids[1]);
  });

  it('Problem Details mesajini kullaniciya tasir', async () => {
    fakeError(422, {
      status: 422,
      title: 'Is kurali ihlali',
      detail: 'Yillik izin bakiyeniz yetersiz.',
      traceId: 'DESTEK-2026-0042',
    });

    const error = (await apiClient.get('/deneme').catch((e: unknown) => e)) as ApiError;

    expect(error.message).toBe('Yillik izin bakiyeniz yetersiz.');
    expect(error.traceId).toBe('DESTEK-2026-0042');
    expect(error.status).toBe(422);
  });

  it('sunucu mesaj dondurmediginde duruma uygun Turkce mesaj uretir', async () => {
    fakeError(403, '');

    const error = (await apiClient.get('/deneme').catch((e: unknown) => e)) as ApiError;

    expect(error.message).toBe('Bu işlem için yetkiniz bulunmuyor.');
  });

  it('sunucuya ulasilamadiginda ne yapilacagini soyler', async () => {
    apiClient.defaults.adapter = (config) =>
      Promise.reject(new AxiosError('Network Error', 'ERR_NETWORK', config));

    const error = (await apiClient.get('/deneme').catch((e: unknown) => e)) as ApiError;

    // "Bir hata olustu" demek kullaniciyi caresiz birakirdi (ADR-0015 §6).
    expect(error.message).toBe(
      'Sunucuya ulaşılamadı. İnternet bağlantınızı kontrol edip tekrar deneyin.',
    );
    expect(error.isNetworkError).toBe(true);
  });
});

describe('izleme kimligi - guvenli olmayan baglam', () => {
  // "crypto.randomUUID" YALNIZCA guvenli baglamda (HTTPS veya localhost) tanimlidir.
  // Duz HTTP uzerinden bir alan adiyla acildiginda yoktur; bu durumda uygulama
  // hicbir istek gonderemiyordu (#39).
  // Metot referansini dogrudan almak yerine baglami korunmus bir sarmalayici
  // saklanir; boylece geri yuklendiginde "this" baglantisi bozulmaz.
  const realRandomUUID = crypto.randomUUID.bind(crypto);

  afterEach(() => {
    Object.defineProperty(crypto, 'randomUUID', {
      value: realRandomUUID,
      configurable: true,
      writable: true,
    });
  });

  function removeRandomUUID(): void {
    Object.defineProperty(crypto, 'randomUUID', {
      value: undefined,
      configurable: true,
      writable: true,
    });
  }

  it('randomUUID yokken de istek gonderilir', async () => {
    removeRandomUUID();

    let reached = false;
    fakeResponse((config) => {
      reached = true;

      return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
    });

    await apiClient.get('/deneme');

    // Asil dogrulama: istek SUNUCUYA ULASTI. Onceki kodda ara katman hata
    // firlattigi icin istek hic gonderilmiyordu.
    expect(reached).toBe(true);
  });

  it('randomUUID yokken de gecerli bir kimlik uretilir', async () => {
    removeRandomUUID();

    let sentConfig: string | undefined;
    fakeResponse((config) => {
      sentConfig = config.headers.get(CORRELATION_HEADER) as string;

      return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
    });

    await apiClient.get('/deneme');

    expect(sentConfig).toMatch(/^[0-9a-f]{32}$/);
  });

  it('kimlik uretimi tamamen basarisiz olsa bile istek engellenmez', async () => {
    // Izleme kimligi bir kolayliktir; asil islevi engellememelidir.
    removeRandomUUID();

    const realGetRandomValues = crypto.getRandomValues.bind(crypto);
    Object.defineProperty(crypto, 'getRandomValues', {
      value: () => {
        throw new Error('kullanilamiyor');
      },
      configurable: true,
      writable: true,
    });

    let reached = false;
    fakeResponse((config) => {
      reached = true;

      return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
    });

    try {
      await apiClient.get('/deneme');
    } finally {
      Object.defineProperty(crypto, 'getRandomValues', {
        value: realGetRandomValues,
        configurable: true,
        writable: true,
      });
    }

    expect(reached).toBe(true);
  });
});

describe('fetchHealth', () => {
  it('saglik ucunu surumsuz adresten cagirir', async () => {
    let requestedUrl: string | undefined;

    fakeResponse((config) => {
      // Axios'un GERCEKTE urettigi adres olculur. Daha once baseURL ve url
      // dizgileri birlestiriliyordu; bu, istegin gittigi adresi degil testin
      // kendi kurdugu metni dogruluyordu (#39).
      requestedUrl = axios.getUri(config);

      return {
        data: { status: 'Healthy', totalDurationMs: 1, checks: [] },
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      };
    });

    const result = await fetchHealth();

    // Saglik uclari sozlesmenin parcasi degildir ve "/api/v1" altinda yasamaz.
    expect(requestedUrl).toBe('/health/ready');
    expect(result.status).toBe('Healthy');
  });
});
