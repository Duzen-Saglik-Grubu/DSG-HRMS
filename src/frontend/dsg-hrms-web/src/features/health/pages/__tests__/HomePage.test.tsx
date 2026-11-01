import { describe, expect, it, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router';
import { AppProviders } from '@/app/AppProviders';
import { HomePage } from '../HomePage';
import * as healthApi from '@/shared/api/health';
import { permissions } from '@/shared/auth/permissions';
import { setSession } from '@/test/session';

/**
 * Ana sayfanin uc durumu da dogrulanir: yukleniyor, basarili, hatali.
 *
 * Kullanicinin en cok karsilastigi durum "yukleniyor" ve "hata"dir; yalnizca
 * mutlu yolu test etmek, gercek kullanimda en sik gorunen ekranlari
 * dogrulamamak demektir.
 */
function renderHomePage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <AppProviders queryClient={queryClient}>
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    </AppProviders>,
  );
}

describe('HomePage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('yuklenirken bos ekran degil, durum bilgisi gosterir', () => {
    vi.spyOn(healthApi, 'fetchHealth').mockReturnValue(new Promise(() => undefined));

    renderHomePage();

    expect(screen.getByText('Kontrol ediliyor…')).toBeInTheDocument();
  });

  it('saglik kontrollerini durumlariyla listeler', async () => {
    vi.spyOn(healthApi, 'fetchHealth').mockResolvedValue({
      status: 'Healthy',
      totalDurationMs: 12.3,
      checks: [{ name: 'postgresql', status: 'Healthy', durationMs: 11.1 }],
    });

    renderHomePage();

    await waitFor(() => {
      expect(screen.getByText('Sistem çalışıyor')).toBeInTheDocument();
    });

    expect(screen.getByText('postgresql: Healthy')).toBeInTheDocument();
  });

  it('hata durumunda takip numarasini gosterir', async () => {
    vi.spyOn(healthApi, 'fetchHealth').mockRejectedValue({
      message: 'Sunucuya ulaşılamadı.',
      traceId: 'DESTEK-2026-0042',
      isNetworkError: true,
    });

    renderHomePage();

    await waitFor(() => {
      expect(screen.getByText('Sunucuya ulaşılamadı.')).toBeInTheDocument();
    });

    expect(screen.getByText(/DESTEK-2026-0042/)).toBeInTheDocument();
  });
});

describe('HomePage hizli erisim', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.spyOn(healthApi, 'fetchHealth').mockReturnValue(new Promise(() => undefined));
  });

  it('kartlar izne gore gorunur (#309)', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.personView] });

    renderHomePage();

    expect(screen.getByRole('heading', { level: 1, name: /Ahmet/ })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Personel/ })).toHaveAttribute('href', '/personnel');
    expect(screen.getByRole('link', { name: /Parolamı değiştir/ })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /Hesap işlemleri/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /Sistem parametreleri/ })).not.toBeInTheDocument();
  });
});
