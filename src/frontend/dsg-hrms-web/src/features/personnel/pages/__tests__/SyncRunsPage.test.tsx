import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { permissions } from '@/shared/auth/permissions';
import { renderWithProviders } from '@/test/render';
import { setSession } from '@/test/session';
import { syncApi, type SyncRun, type SyncRunList } from '../../api/syncApi';
import { SyncRunsPage } from '../SyncRunsPage';

/** Senkronizasyon durumu (SYG-PERS-008, 009). Veriler SENTETIKTIR. */
const SUCCEEDED: SyncRun = {
  id: 'r-1',
  trigger: 'scheduled',
  status: 'succeeded',
  startedAt: '2026-11-05T06:00:00Z',
  finishedAt: '2026-11-05T06:01:12Z',
  failureReason: null,
  recordsRead: 612,
  personsCreated: 3,
  personsUpdated: 40,
  employmentsCreated: 4,
  employmentsUpdated: 50,
  employmentsDeactivated: 1,
  warningCount: 2,
};

const FAILED: SyncRun = {
  ...SUCCEEDED,
  id: 'r-2',
  trigger: 'manual',
  status: 'failed',
  startedAt: '2026-11-05T06:15:00Z',
  finishedAt: '2026-11-05T06:15:05Z',
  failureReason: 'schemaDrift',
  recordsRead: 0,
  personsCreated: 0,
  personsUpdated: 0,
  employmentsCreated: 0,
  employmentsUpdated: 0,
  employmentsDeactivated: 0,
  warningCount: 0,
};

function list(items: SyncRun[], overrides: Partial<SyncRunList> = {}): SyncRunList {
  return {
    enabled: true,
    running: false,
    runs: { items, page: 1, pageSize: 25, totalCount: items.length, totalPages: 1 },
    ...overrides,
  };
}

describe('SyncRunsPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('izni olmayan kullaniciya listeyi gostermez', async () => {
    await setSession({ status: 'authenticated', permissions: [] });
    const load = vi.spyOn(syncApi, 'list');
    renderWithProviders(<SyncRunsPage />);

    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(load).not.toHaveBeenCalled();
    expect(screen.queryByRole('button', { name: 'Şimdi senkronize et' })).not.toBeInTheDocument();
  });

  it('calismalari sonucu, nedeni ve sayilariyla listeler', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.syncView] });
    vi.spyOn(syncApi, 'list').mockResolvedValue(list([FAILED, SUCCEEDED]));
    renderWithProviders(<SyncRunsPage />);

    await screen.findByText('Başarısız');
    const rows = screen.getAllByRole('row');
    expect(within(rows[1]!).getByText('Başarısız')).toBeInTheDocument();
    expect(
      within(rows[1]!).getByText('LOGO tablolarının yapısı beklenenden farklı'),
    ).toBeInTheDocument();
    expect(within(rows[1]!).getByText('Elle')).toBeInTheDocument();
    expect(within(rows[1]!).getByText('5 sn')).toBeInTheDocument();

    expect(within(rows[2]!).getByText('Başarılı')).toBeInTheDocument();
    expect(within(rows[2]!).getByText('Zamanlanmış')).toBeInTheDocument();
    expect(within(rows[2]!).getByText('1 dk 12 sn')).toBeInTheDocument();
    expect(within(rows[2]!).getByText('612')).toBeInTheDocument();
    expect(within(rows[2]!).getByText('3 kişi · 4 sicil')).toBeInTheDocument();
    expect(within(rows[2]!).getByText('40 kişi · 50 sicil')).toBeInTheDocument();

    // Gorme izni baslatma hakki vermez.
    expect(screen.queryByRole('button', { name: 'Şimdi senkronize et' })).not.toBeInTheDocument();
  });

  it('elle baslatir ve sonucu bekledigini soyler', async () => {
    await setSession({
      status: 'authenticated',
      permissions: [permissions.syncView, permissions.syncCreate],
    });
    const load = vi.spyOn(syncApi, 'list').mockResolvedValue(list([SUCCEEDED]));
    const start = vi.spyOn(syncApi, 'start').mockResolvedValue({ alreadyQueued: false });
    const user = userEvent.setup();
    renderWithProviders(<SyncRunsPage />);

    await user.click(await screen.findByRole('button', { name: 'Şimdi senkronize et' }));

    expect(start).toHaveBeenCalledTimes(1);
    expect(
      await screen.findByText('Senkronizasyon başlatıldı. Sonucu birazdan listede görünecek.'),
    ).toBeInTheDocument();
    // Liste yeniden istenir.
    expect(load.mock.calls.length).toBeGreaterThan(1);
  });

  it('calisma surerken dugme pasiftir ve calistigi yazar', async () => {
    await setSession({
      status: 'authenticated',
      permissions: [permissions.syncView, permissions.syncCreate],
    });
    vi.spyOn(syncApi, 'list').mockResolvedValue(
      list([{ ...SUCCEEDED, id: 'r-3', status: 'running', finishedAt: null }], { running: true }),
    );
    renderWithProviders(<SyncRunsPage />);

    expect(await screen.findByRole('button', { name: 'Çalışıyor…' })).toBeDisabled();
    expect(screen.getByText(/Senkronizasyon çalışıyor\./)).toBeInTheDocument();
    expect(within(screen.getAllByRole('row')[1]!).getByText('Sürüyor')).toBeInTheDocument();
  });

  it('sunucu calismanin surdugunu soylerse nedenini gosterir', async () => {
    await setSession({
      status: 'authenticated',
      permissions: [permissions.syncView, permissions.syncCreate],
    });
    vi.spyOn(syncApi, 'list').mockResolvedValue(list([SUCCEEDED]));
    vi.spyOn(syncApi, 'start').mockRejectedValue(
      new ApiError({
        message: 'Senkronizasyon şu anda çalışıyor. Bitince yeniden başlatabilirsiniz.',
        isNetworkError: false,
        status: 409,
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<SyncRunsPage />);

    await user.click(await screen.findByRole('button', { name: 'Şimdi senkronize et' }));

    expect(
      await screen.findByText(
        'Senkronizasyon şu anda çalışıyor. Bitince yeniden başlatabilirsiniz.',
      ),
    ).toBeInTheDocument();
  });

  it('senkronizasyon tanimli degilse dugme pasiftir', async () => {
    await setSession({
      status: 'authenticated',
      permissions: [permissions.syncView, permissions.syncCreate],
    });
    vi.spyOn(syncApi, 'list').mockResolvedValue(list([], { enabled: false }));
    renderWithProviders(<SyncRunsPage />);

    expect(await screen.findByText(/bu ortamda tanımlı değil/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Şimdi senkronize et' })).toBeDisabled();
    expect(screen.getByText('Henüz çalışma yok')).toBeInTheDocument();
  });
});
