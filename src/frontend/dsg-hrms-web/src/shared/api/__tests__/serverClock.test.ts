import { afterEach, describe, expect, it, vi } from 'vitest';
import { AxiosError, type AxiosAdapter, type AxiosResponse } from 'axios';
import { apiClient } from '../client';
import { recordServerDate, resetServerClock, serverNow } from '../serverClock';

/** Sunucunun saati (#103). */
const defaultAdapter = apiClient.defaults.adapter;

afterEach(() => {
  resetServerClock();
  vi.useRealTimers();
  if (defaultAdapter === undefined) {
    delete apiClient.defaults.adapter;
  } else {
    apiClient.defaults.adapter = defaultAdapter;
  }
});

describe('serverClock', () => {
  it('Date basligindan farki olcer; baslik yoksa veya bozuksa degistirmez', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-09-29T13:38:30Z'));

    expect(serverNow()).toBe(Date.parse('2026-09-29T13:38:30Z'));

    recordServerDate('Tue, 29 Sep 2026 13:28:30 GMT');
    expect(serverNow()).toBe(Date.parse('2026-09-29T13:28:30Z'));

    recordServerDate(undefined);
    recordServerDate('gecersiz');
    expect(serverNow()).toBe(Date.parse('2026-09-29T13:28:30Z'));
  });

  it('API istemcisi basarili ve hatali yanitlarin Date basligini kaydeder', async () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-09-29T13:38:30Z'));
    const reply =
      (status: number, date: string): AxiosAdapter =>
      (config) => {
        const response = {
          status,
          statusText: '',
          headers: { date },
          config,
          data: {},
        } as AxiosResponse;
        return status < 400
          ? Promise.resolve(response)
          : Promise.reject(new AxiosError('hata', String(status), config, null, response));
      };

    apiClient.defaults.adapter = reply(200, 'Tue, 29 Sep 2026 13:28:30 GMT');
    await apiClient.get('/deneme');
    expect(serverNow()).toBe(Date.parse('2026-09-29T13:28:30Z'));

    apiClient.defaults.adapter = reply(500, 'Tue, 29 Sep 2026 13:40:30 GMT');
    await apiClient.get('/deneme').catch(() => undefined);
    expect(serverNow()).toBe(Date.parse('2026-09-29T13:40:30Z'));
  });
});
