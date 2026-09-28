import { apiClient } from '@/shared/api/client';
import type { SessionApi, SessionResponse } from './sessionManager';

const BASE = '/identity/sessions';

/**
 * Oturum uclari (#94). Yenileme ve cikis istekleri erisim jetonu TASIMAZ (`skipAuth`):
 * oturumun kendisidir ve yalnizca `HttpOnly` cerezle calisir. Etkinlik sinyali ise dogrulanmis
 * oturumdan kabul edilir (SYG-KMLK-039).
 */
export const sessionApi: SessionApi = {
  refresh: async () => {
    const response = await apiClient.post<SessionResponse>(`${BASE}/refresh`, undefined, {
      skipAuth: true,
    });
    return response.data;
  },

  signOut: async () => {
    await apiClient.delete(`${BASE}/current`, { skipAuth: true });
  },

  activity: async () => {
    await apiClient.post(`${BASE}/activity`);
  },
};
