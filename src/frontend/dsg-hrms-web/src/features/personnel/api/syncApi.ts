import { apiClient } from '@/shared/api/client';
import type { PagedResult } from '@/shared/api/paging';
import type { ApiSchemas } from '@/shared/api/schemas';

/** Bir senkronizasyon calismasi; sayilar sayiya cevrilmis. */
export interface SyncRun {
  id: string;
  trigger: string;
  status: string;
  startedAt: string;
  finishedAt: string | null;
  failureReason: string | null;
  recordsRead: number;
  personsCreated: number;
  personsUpdated: number;
  employmentsCreated: number;
  employmentsUpdated: number;
  employmentsDeactivated: number;
  warningCount: number;
}

/** "Senkronizasyon durumu" ekraninin verisi. */
export interface SyncRunList {
  enabled: boolean;
  running: boolean;
  runs: PagedResult<SyncRun>;
}

const BASE = '/personnel/sync-runs';

function toRun(value: ApiSchemas['SyncRunItemResponse']): SyncRun {
  return {
    id: value.id,
    trigger: value.trigger,
    status: value.status,
    startedAt: value.startedAt,
    finishedAt: value.finishedAt ?? null,
    failureReason: value.failureReason ?? null,
    recordsRead: Number(value.recordsRead),
    personsCreated: Number(value.personsCreated),
    personsUpdated: Number(value.personsUpdated),
    employmentsCreated: Number(value.employmentsCreated),
    employmentsUpdated: Number(value.employmentsUpdated),
    employmentsDeactivated: Number(value.employmentsDeactivated),
    warningCount: Number(value.warningCount),
  };
}

/**
 * Senkronizasyon durumu (SYG-PERS-008, 009). Gormek <c>personnel.sync.view</c>, elle baslatmak
 * <c>personnel.sync.create</c> ister.
 */
export const syncApi = {
  /** Son calismalar, en yeni once. */
  list: async (page: number, pageSize: number): Promise<SyncRunList> => {
    const response = await apiClient.get<ApiSchemas['SyncRunListResponse']>(BASE, {
      params: { page, pageSize },
    });
    const { runs } = response.data;
    return {
      enabled: response.data.enabled,
      running: response.data.running,
      runs: {
        items: runs.items.map(toRun),
        page: Number(runs.page),
        pageSize: Number(runs.pageSize),
        totalCount: Number(runs.totalCount),
        totalPages: Number(runs.totalPages),
      },
    };
  },

  /** Elle calisma ister; calismayi beklemez. Calisma surerken sunucu 409 doner. */
  start: async (): Promise<{ alreadyQueued: boolean }> => {
    const response = await apiClient.post<ApiSchemas['SyncStartResponse']>(BASE);
    return { alreadyQueued: response.data.alreadyQueued };
  },
};
