import { act, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test/render';
import { fakeSession, setSession } from '@/test/session';
import { session } from '../session';
import { sessionApi } from '../sessionApi';
import { PermissionGate } from '../PermissionGate';
import { permissions } from '../permissions';

/** Izne gore gosterim (ADR-0015 §5). Yalnizca gorsel kolaylik; denetim sunucudadir. */
describe('PermissionGate', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('izin varsa icerigi, yoksa yedek icerigi gosterir', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.syncView] });

    renderWithProviders(
      <>
        <PermissionGate permission={permissions.syncView}>
          <p>Durum</p>
        </PermissionGate>
        <PermissionGate permission={permissions.syncCreate} fallback={<p>Yetki yok</p>}>
          <p>Başlat</p>
        </PermissionGate>
      </>,
    );

    expect(screen.getByText('Durum')).toBeInTheDocument();
    expect(screen.getByText('Yetki yok')).toBeInTheDocument();
    expect(screen.queryByText('Başlat')).not.toBeInTheDocument();
  });

  it('oturum yoksa hicbir izni yok sayar', async () => {
    await setSession({ status: 'anonymous' });

    renderWithProviders(
      <PermissionGate permission={permissions.syncView}>
        <p>Durum</p>
      </PermissionGate>,
    );

    expect(screen.queryByText('Durum')).not.toBeInTheDocument();
  });

  it('jeton yenilenince degisen izinleri yansitir', async () => {
    await setSession({ status: 'authenticated', permissions: [] });
    renderWithProviders(
      <PermissionGate permission={permissions.accountView}>
        <p>Hesaplar</p>
      </PermissionGate>,
    );
    expect(screen.queryByText('Hesaplar')).not.toBeInTheDocument();

    vi.spyOn(sessionApi, 'refresh').mockResolvedValue(
      fakeSession('Ahmet', 'Yılmaz', [permissions.accountView]),
    );
    await act(() => session.refreshAccessToken());

    expect(screen.getByText('Hesaplar')).toBeInTheDocument();
  });
});
