import { afterEach, describe, expect, it } from 'vitest';
import { AxiosError, type AxiosAdapter, type AxiosResponse } from 'axios';
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

const varsayilanAdapter = apiClient.defaults.adapter;

/** Sunucu yerine gecen sahte adapter kurar. */
function sahteYanit(
  handler: (config: Parameters<AxiosAdapter>[0]) => Promise<AxiosResponse> | AxiosResponse,
): void {
  apiClient.defaults.adapter = async (config) => handler(config);
}

/** Belirtilen durum kodu ve govdeyle hata donduren adapter. */
function sahteHata(status: number, data: unknown): void {
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
  if (varsayilanAdapter === undefined) {
    delete apiClient.defaults.adapter;
  } else {
    apiClient.defaults.adapter = varsayilanAdapter;
  }
});

describe('apiClient', () => {
  it('her istege izleme kimligi ekler', async () => {
    let gonderilen: string | undefined;

    sahteYanit((config) => {
      gonderilen = config.headers.get(CORRELATION_HEADER) as string;

      return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
    });

    await apiClient.get('/deneme');

    // Kimlik olmadan, kullanicinin bildirdigi bir sorun gunluk kaydiyla
    // eslestirilemezdi (ADR-0009 §2).
    expect(gonderilen).toMatch(/^[0-9a-f]{32}$/);
  });

  it('her istekte FARKLI bir izleme kimligi uretir', async () => {
    const kimlikler: string[] = [];

    sahteYanit((config) => {
      kimlikler.push(config.headers.get(CORRELATION_HEADER) as string);

      return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
    });

    await apiClient.get('/deneme');
    await apiClient.get('/deneme');

    expect(kimlikler[0]).not.toBe(kimlikler[1]);
  });

  it('Problem Details mesajini kullaniciya tasir', async () => {
    sahteHata(422, {
      status: 422,
      title: 'Is kurali ihlali',
      detail: 'Yillik izin bakiyeniz yetersiz.',
      traceId: 'DESTEK-2026-0042',
    });

    const hata = (await apiClient.get('/deneme').catch((e: unknown) => e)) as ApiError;

    expect(hata.message).toBe('Yillik izin bakiyeniz yetersiz.');
    expect(hata.traceId).toBe('DESTEK-2026-0042');
    expect(hata.status).toBe(422);
  });

  it('sunucu mesaj dondurmediginde duruma uygun Turkce mesaj uretir', async () => {
    sahteHata(403, '');

    const hata = (await apiClient.get('/deneme').catch((e: unknown) => e)) as ApiError;

    expect(hata.message).toBe('Bu işlem için yetkiniz bulunmuyor.');
  });

  it('sunucuya ulasilamadiginda ne yapilacagini soyler', async () => {
    apiClient.defaults.adapter = (config) =>
      Promise.reject(new AxiosError('Network Error', 'ERR_NETWORK', config));

    const hata = (await apiClient.get('/deneme').catch((e: unknown) => e)) as ApiError;

    // "Bir hata olustu" demek kullaniciyi caresiz birakirdi (ADR-0015 §6).
    expect(hata.message).toBe(
      'Sunucuya ulaşılamadı. İnternet bağlantınızı kontrol edip tekrar deneyin.',
    );
    expect(hata.isNetworkError).toBe(true);
  });
});

describe('fetchHealth', () => {
  it('saglik ucunu surumsuz adresten cagirir', async () => {
    let istenenUrl: string | undefined;

    sahteYanit((config) => {
      istenenUrl = `${config.baseURL ?? ''}${config.url ?? ''}`;

      return {
        data: { status: 'Healthy', totalDurationMs: 1, checks: [] },
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      };
    });

    const sonuc = await fetchHealth();

    // Saglik uclari sozlesmenin parcasi degildir ve "/api/v1" altinda yasamaz.
    expect(istenenUrl).toBe('//health/ready');
    expect(sonuc.status).toBe('Healthy');
  });
});
