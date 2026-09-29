import { afterEach, describe, expect, it } from 'vitest';
import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { apiClient } from '@/shared/api/client';
import { passwordApi } from '../passwordApi';

/**
 * Parola uclarinin adresleri ve kimlik dogrulama davranisi (SYG-KMLK-047, 048). Sifirlama
 * oturum gerektirmez (`skipAuth`); degisiklik oturumun jetonuyla gider.
 */
const defaultAdapter = apiClient.defaults.adapter;
const seen: InternalAxiosRequestConfig[] = [];

function fakeServer(data: unknown = {}) {
  seen.length = 0;
  apiClient.defaults.adapter = (config) => {
    seen.push(config);
    return Promise.resolve({
      data,
      status: 200,
      statusText: '',
      headers: {},
      config,
    } as AxiosResponse);
  };
}

afterEach(() => {
  if (defaultAdapter === undefined) {
    delete apiClient.defaults.adapter;
  } else {
    apiClient.defaults.adapter = defaultAdapter;
  }
});

describe('passwordApi', () => {
  it('sifirlama adimlari parola sifirlama ucuna oturumsuz gider', async () => {
    fakeServer({ registrationId: 'r-1', channels: ['email'], expiresAt: 'x', codeExpiresAt: 'y' });

    await passwordApi.startReset({ nationalId: '1', birthDate: '1985-04-12', email: 'a@b.c' });
    await passwordApi.requestCode('r-1', 'sms');
    await passwordApi.verify('r-1', '123456');
    await passwordApi.reset('r-1', 'Yeni parola 1');

    expect(seen.map((c) => `${c.method?.toUpperCase()} ${c.url}`)).toEqual([
      'POST /identity/password-resets',
      'POST /identity/password-resets/r-1/code',
      'POST /identity/password-resets/r-1/verification',
      'POST /identity/password-resets/r-1/password',
    ]);
    expect(seen.every((c) => c.skipAuth === true)).toBe(true);
    expect(JSON.parse(seen[1]!.data as string)).toEqual({ channel: 'sms' });
    expect(JSON.parse(seen[3]!.data as string)).toEqual({ password: 'Yeni parola 1' });
  });

  it('oturum icinde degisiklik oturumun jetonuyla gider', async () => {
    fakeServer();

    await passwordApi.change({ currentPassword: 'Eski 1', newPassword: 'Yeni 2' });

    expect(seen[0]!.url).toBe('/identity/account/password');
    expect(seen[0]!.skipAuth).toBeUndefined();
    expect(JSON.parse(seen[0]!.data as string)).toEqual({
      currentPassword: 'Eski 1',
      newPassword: 'Yeni 2',
    });
  });
});
