import { afterEach, describe, expect, it, vi } from 'vitest';
import { AxiosError, type AxiosAdapter, type AxiosResponse } from 'axios';
import { apiClient, configureAuth, type AuthHandlers } from '../client';
import { ApiError } from '../problemDetails';

/**
 * API istemcisinin oturumla calismasi (ADR-0015 §3): jeton ekleme, `401`'de bir kez yenileyip
 * tekrar deneme.
 */
const defaultAdapter = apiClient.defaults.adapter;

type Reply = { status: number; data?: unknown };

/** Her istegin Authorization basligini kaydeder ve sirayla verilen yanitlari dondurur. */
function fakeServer(...replies: Reply[]) {
  const seen: (string | undefined)[] = [];
  const adapter: AxiosAdapter = (config) => {
    seen.push(config.headers.get('Authorization') as string | undefined);
    const reply = replies.shift() ?? { status: 200 };
    const response = {
      status: reply.status,
      statusText: '',
      headers: {},
      config,
      data: reply.data ?? {},
    } as AxiosResponse;

    return reply.status < 400
      ? Promise.resolve(response)
      : Promise.reject(new AxiosError('hata', String(reply.status), config, null, response));
  };
  apiClient.defaults.adapter = adapter;

  return seen;
}

function handlers(overrides: Partial<AuthHandlers> = {}) {
  const value = {
    getAccessToken: vi.fn<AuthHandlers['getAccessToken']>().mockResolvedValue('eski'),
    refreshAccessToken: vi.fn<AuthHandlers['refreshAccessToken']>().mockResolvedValue('yeni'),
    ...overrides,
  };
  configureAuth(value);
  return value;
}

afterEach(() => {
  configureAuth(null);
  if (defaultAdapter === undefined) {
    delete apiClient.defaults.adapter;
  } else {
    apiClient.defaults.adapter = defaultAdapter;
  }
});

describe('apiClient — oturum', () => {
  it('istege erisim jetonunu ekler', async () => {
    handlers();
    const seen = fakeServer({ status: 200 });

    await apiClient.get('/deneme');

    expect(seen).toEqual(['Bearer eski']);
  });

  it('oturum yoksa jeton eklenmez', async () => {
    handlers({ getAccessToken: vi.fn().mockResolvedValue(null) });
    const seen = fakeServer({ status: 200 });

    await apiClient.get('/deneme');

    expect(seen).toEqual([undefined]);
  });

  it('401 yanitinda jetonu yeniler ve istegi BIR KEZ tekrarlar', async () => {
    const auth = handlers();
    const seen = fakeServer({ status: 401 }, { status: 200, data: { ok: true } });

    const response = await apiClient.get('/deneme');

    expect(response.data).toEqual({ ok: true });
    expect(seen).toEqual(['Bearer eski', 'Bearer yeni']);
    expect(auth.refreshAccessToken).toHaveBeenCalledOnce();
  });

  it('tekrar da 401 alirsa yeniden denemez', async () => {
    const auth = handlers();
    fakeServer({ status: 401 }, { status: 401 });

    await expect(apiClient.get('/deneme')).rejects.toBeInstanceOf(ApiError);
    expect(auth.refreshAccessToken).toHaveBeenCalledOnce();
  });

  it('oturum sona erdiyse istegi tekrarlamaz; hata Problem Details olarak doner', async () => {
    handlers({ refreshAccessToken: vi.fn().mockResolvedValue(null) });
    const seen = fakeServer({ status: 401, data: { status: 401, title: 'Yetkisiz' } });

    const error = await apiClient.get('/deneme').catch((e: unknown) => e);

    expect(seen).toHaveLength(1);
    expect(error).toMatchObject({ status: 401 });
  });

  it('jeton yenilenemezse (ag hatasi) istek jetonsuz gider ve hata doner', async () => {
    handlers({
      getAccessToken: vi.fn().mockRejectedValue(new Error('ag')),
      refreshAccessToken: vi.fn().mockRejectedValue(new Error('ag')),
    });
    const seen = fakeServer({ status: 401 });

    await expect(apiClient.get('/deneme')).rejects.toMatchObject({ status: 401 });
    expect(seen).toEqual([undefined]);
  });

  it('oturum istekleri (skipAuth) jeton tasimaz ve 401de yenileme denemez', async () => {
    const auth = handlers();
    const seen = fakeServer({ status: 401 });

    await expect(
      apiClient.post('/identity/sessions/refresh', undefined, { skipAuth: true }),
    ).rejects.toMatchObject({ status: 401 });

    expect(seen).toEqual([undefined]);
    expect(auth.getAccessToken).not.toHaveBeenCalled();
    expect(auth.refreshAccessToken).not.toHaveBeenCalled();
  });

  it('401 disindaki hatalarda yenileme denemez', async () => {
    const auth = handlers();
    fakeServer({ status: 403 });

    await expect(apiClient.get('/deneme')).rejects.toMatchObject({ status: 403 });
    expect(auth.refreshAccessToken).not.toHaveBeenCalled();
  });
});
