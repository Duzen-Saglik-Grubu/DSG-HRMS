import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test/render';
import { personsApi } from '../../api/personsApi';
import { SyncFreshness } from '../SyncFreshness';

/** "Son senkronizasyon" bilgisi (SYG-PERS-010). */
describe('SyncFreshness', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('son basarili calismanin gununu ve saatini gosterir', async () => {
    vi.spyOn(personsApi, 'syncFreshness').mockResolvedValue({
      enabled: true,
      lastSucceededAt: new Date(2026, 10, 4, 9, 15).toISOString(),
      lastRunFailed: false,
    });
    renderWithProviders(<SyncFreshness />);

    expect(await screen.findByText('04.11.2026 09:15')).toBeInTheDocument();
    expect(screen.getByText(/Son senkronizasyon/)).toBeInTheDocument();
    expect(screen.queryByText('Son deneme başarısız')).not.toBeInTheDocument();
  });

  it('son calisma basarisizsa uyariyi metin olarak da verir', async () => {
    // Uyari yalnizca renk veya simgeyle verilmez (ADR-0015 §7).
    vi.spyOn(personsApi, 'syncFreshness').mockResolvedValue({
      enabled: true,
      lastSucceededAt: new Date(2026, 10, 4, 9, 15).toISOString(),
      lastRunFailed: true,
    });
    renderWithProviders(<SyncFreshness />);

    expect(await screen.findByText('Son deneme başarısız')).toBeInTheDocument();
    expect(screen.getByText('04.11.2026 09:15')).toBeInTheDocument();
  });

  it('hic basarili calisma yoksa bunu soyler', async () => {
    vi.spyOn(personsApi, 'syncFreshness').mockResolvedValue({
      enabled: true,
      lastSucceededAt: null,
      lastRunFailed: false,
    });
    renderWithProviders(<SyncFreshness />);

    expect(await screen.findByText('henüz yapılmadı')).toBeInTheDocument();
  });

  it('senkronizasyon tanimli degilse hicbir sey gostermez', async () => {
    const call = vi.spyOn(personsApi, 'syncFreshness').mockResolvedValue({
      enabled: false,
      lastSucceededAt: null,
      lastRunFailed: false,
    });
    const { container } = renderWithProviders(<SyncFreshness />);

    await vi.waitFor(() => expect(call).toHaveBeenCalled());
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(container).toBeEmptyDOMElement();
  });
});
