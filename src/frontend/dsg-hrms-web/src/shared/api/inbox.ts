import { apiClient } from './client';
import type { ApiSchemas } from './schemas';

export type InboxItem = ApiSchemas['InboxItemResponse'];

const BASE = '/notifications/inbox';

/**
 * Uygulama ici bildirim kutusu (SYG-PERS-046; ADR-0022 §3). Herkes yalnizca kendi bildirimlerini
 * gorur. Bildirim kisisel veri tasimaz: tur ve ilgili kaydin kimligi gelir, metni ekran uretir.
 *
 * Uygulama kabugundaki zil kullandigi icin `shared/` altindadir (ADR-0015 §1).
 */
export const inboxApi = {
  /** En yeni bildirimler (en fazla 50). */
  list: async (): Promise<InboxItem[]> => {
    const response = await apiClient.get<InboxItem[]>(BASE);
    return response.data;
  },

  /** Okunmamis bildirim sayisi. */
  unreadCount: async (): Promise<number> => {
    const response = await apiClient.get<ApiSchemas['UnreadCountResponse']>(`${BASE}/unread-count`);
    return Number(response.data.count);
  },

  /** Bildirimi okundu olarak isaretler. */
  markRead: async (itemId: string): Promise<InboxItem> => {
    const response = await apiClient.post<InboxItem>(`${BASE}/${encodeURIComponent(itemId)}/read`);
    return response.data;
  },
};
