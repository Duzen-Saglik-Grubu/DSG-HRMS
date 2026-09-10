import { apiClient } from './client';

/** Backend saglik yaniti (ADR-0011). */
export interface HealthResponse {
  status: string;
  totalDurationMs: number;
  checks: { name: string; status: string; durationMs: number }[];
}

/**
 * Sistem durumunu sorgular.
 *
 * Iskelet asamasinda uctan uca akisin (tarayici -> vekil -> API) calistigini
 * gosteren tek gercek cagridir.
 *
 * Saglik uclari API sozlesmesinin PARCASI DEGILDIR ve surumlenmez; bu yuzden
 * "/api/v1" tabani burada bilincli olarak devre disi birakilir.
 */
export async function fetchHealth(): Promise<HealthResponse> {
  const response = await apiClient.get<HealthResponse>('/health/ready', { baseURL: '/' });

  return response.data;
}
