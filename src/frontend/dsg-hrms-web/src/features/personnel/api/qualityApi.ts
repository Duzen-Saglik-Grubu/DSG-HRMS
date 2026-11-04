import { apiClient } from '@/shared/api/client';
import type { PagedResult } from '@/shared/api/paging';
import type { ApiSchemas } from '@/shared/api/schemas';
import { fileNameOf, type ExportedFile } from './personsApi';

export type DataQualityIssue = ApiSchemas['DataQualityIssueName'];
export type DataQualityItem = ApiSchemas['DataQualityItemResponse'];
export type IneligiblePerson = ApiSchemas['IneligiblePersonResponse'];
export type MembershipBlocker = ApiSchemas['MembershipBlockerName'];

/** Senkronizasyon calismasinin ozeti; sayilar sayiya cevrilmis. */
export interface SyncRunSummary {
  status: string;
  startedAt: string;
  finishedAt: string | null;
  recordsRead: number;
  warningCount: number;
}

/** Rapor ekraninin verisi. */
export interface DataQualityReport {
  lastRun: SyncRunSummary | null;
  warningsRun: SyncRunSummary | null;
  counts: Record<DataQualityIssue, number>;
  ineligibleCount: number;
  items: PagedResult<DataQualityItem>;
}

/** Uyari turlerinin ekrandaki sirasi (sunucunun siralamasiyla ayni). */
export const DATA_QUALITY_ISSUES: readonly DataQualityIssue[] = [
  'missingNationalId',
  'invalidNationalId',
  'missingBirthDate',
  'noContact',
  'invalidMobilePhone',
  'sharedEmail',
  'missingTitle',
  'missingDuty',
];

const BASE = '/personnel/quality';

function run(value: ApiSchemas['SyncRunResponse'] | null): SyncRunSummary | null {
  return value
    ? {
        status: value.status,
        startedAt: value.startedAt,
        finishedAt: value.finishedAt ?? null,
        recordsRead: Number(value.recordsRead),
        warningCount: Number(value.warningCount),
      }
    : null;
}

function paged<T>(data: {
  items: T[];
  page: number | string;
  pageSize: number | string;
  totalCount: number | string;
  totalPages: number | string;
}): PagedResult<T> {
  return {
    items: data.items,
    page: Number(data.page),
    pageSize: Number(data.pageSize),
    totalCount: Number(data.totalCount),
    totalPages: Number(data.totalPages),
  };
}

/**
 * Veri kalitesi raporu (SYG-PERS-070, 071). Gormek <c>personnel.quality.view</c>, Excel'e
 * aktarmak <c>personnel.quality.export</c> ister; yanitta TCKN, e-posta ve telefon yoktur.
 */
export const qualityApi = {
  /** Son calisma, tur sayilari ve suzgece uyan uyarilar. */
  report: async (
    issue: DataQualityIssue | null,
    page: number,
    pageSize: number,
  ): Promise<DataQualityReport> => {
    const response = await apiClient.get<ApiSchemas['DataQualityReportResponse']>(BASE, {
      params: { ...(issue ? { issue } : {}), page, pageSize },
    });
    const data = response.data;
    const counts = Object.fromEntries(DATA_QUALITY_ISSUES.map((i) => [i, 0])) as Record<
      DataQualityIssue,
      number
    >;
    data.counts.forEach((c) => {
      counts[c.issue] = Number(c.count);
    });

    return {
      lastRun: run(data.lastRun),
      warningsRun: run(data.warningsRun),
      counts,
      ineligibleCount: Number(data.ineligibleCount),
      items: paged(data.items),
    };
  },

  /** Uye olamayacak kisiler. */
  ineligible: async (page: number, pageSize: number): Promise<PagedResult<IneligiblePerson>> => {
    const response = await apiClient.get<ApiSchemas['PagedResponseOfIneligiblePersonResponse']>(
      `${BASE}/ineligible`,
      { params: { page, pageSize } },
    );
    return paged(response.data);
  },

  /** Raporu Excel olarak indirir; tur suzgeci yalnizca uyarilara uygulanir. */
  exportFile: async (issue: DataQualityIssue | null): Promise<ExportedFile> => {
    const response = await apiClient.get<Blob>(`${BASE}/export`, {
      params: issue ? { issue } : {},
      responseType: 'blob',
    });
    return {
      blob: response.data,
      fileName: fileNameOf(response.headers['content-disposition']) ?? 'veri-kalitesi.xlsx',
    };
  },
};
