import { apiClient } from '@/shared/api/client';
import type { PagedResult } from '@/shared/api/paging';
import type { ApiSchemas } from '@/shared/api/schemas';

export type AuditEntry = ApiSchemas['AuditEntryResponse'];
export type AuditEntryType = AuditEntry['type'];
export type AuditPerson = ApiSchemas['AuditPersonResponse'];

/** Liste suzgecleri; gunler "YYYY-AA-GG" (Europe/Istanbul), ikisi de dahil. */
export interface AuditFilters {
  from: string | null;
  to: string | null;
  type: AuditEntryType | null;
  actor: AuditPerson | null;
  subject: AuditPerson | null;
}

/** Kisi secicide bir secenek. */
export interface PersonChoice {
  personId: string;
  fullName: string;
  registryCode: string | null;
}

/**
 * Denetim kayitlari (PG-PERS-28, 30; SYG-PERS-069). Yalnizca `audit.log.view` ile okunur; salt
 * okunurdur. Goruntuleme sunucuda erisim kaydina yazilir.
 */
export const auditApi = {
  list: async (
    filters: AuditFilters,
    page: number,
    pageSize: number,
  ): Promise<PagedResult<AuditEntry>> => {
    const params: Record<string, string | number> = { page, pageSize };
    if (filters.from) params.from = filters.from;
    if (filters.to) params.to = filters.to;
    if (filters.type) params.type = filters.type;
    if (filters.actor) params.actorId = filters.actor.personId;
    if (filters.subject) params.subjectId = filters.subject.personId;

    const response = await apiClient.get<PagedResult<AuditEntry>>('/audit/entries', { params });
    return response.data;
  },

  /**
   * Kisi suzgeci icin arama: personel listesi ucu (`personnel.person.view`), ayrilanlar dahil. Bu
   * arama da sunucuda erisim kaydina yazilir.
   */
  searchPeople: async (q: string): Promise<PersonChoice[]> => {
    const response = await apiClient.get<PagedResult<ApiSchemas['PersonListItemResponse']>>(
      '/personnel/persons',
      { params: { q, status: 'all', page: 1, pageSize: 10 } },
    );
    return response.data.items.map((item) => ({
      personId: item.personId,
      fullName: `${item.firstName} ${item.lastName}`,
      registryCode: item.registryCode,
    }));
  },
};
