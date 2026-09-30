import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider, useLocation } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { session } from '@/shared/auth/session';
import { sessionApi } from '@/shared/auth/sessionApi';
import { setSession } from '@/test/session';
import { AppLayout } from '../AppLayout';
import { AppProviders } from '../AppProviders';
import { RequireSession } from '../RequireSession';

/** Oturum gerektiren sayfalar ve cikis (SYG-KMLK-043, 055). */
function LoginProbe() {
  const location = useLocation();
  return <p>Giriş ekranı {location.search}</p>;
}

function renderAt(path: string) {
  const router = createMemoryRouter(
    [
      { path: '/login', element: <LoginProbe /> },
      {
        path: '/',
        element: (
          <RequireSession>
            <AppLayout />
          </RequireSession>
        ),
        children: [
          { index: true, element: <p>Ana içerik</p> },
          { path: 'leave', element: <p>İzin sayfası</p> },
          { path: 'account/password', element: <p>Parola sayfası</p> },
          { path: 'accounts', element: <p>Hesaplar sayfası</p> },
        ],
      },
    ],
    { initialEntries: [path] },
  );
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  render(
    <AppProviders queryClient={queryClient}>
      <RouterProvider router={router} />
    </AppProviders>,
  );
}

describe('RequireSession', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('oturum geri getirilirken sayfa yerine bekleme gostergesi cizer', () => {
    vi.spyOn(session, 'getState').mockReturnValue({ status: 'unknown' });
    renderAt('/leave');

    expect(screen.getByRole('progressbar')).toBeInTheDocument();
    expect(screen.queryByText('İzin sayfası')).not.toBeInTheDocument();
  });

  it('oturum yoksa girise yonlendirir ve istenen adresi tasir', async () => {
    await setSession({ status: 'anonymous' });
    renderAt('/leave?page=2');

    expect(
      await screen.findByText(`Giriş ekranı ?returnTo=${encodeURIComponent('/leave?page=2')}`),
    ).toBeInTheDocument();
  });

  it('oturum aciksa sayfayi ve kullanicinin adini gosterir', async () => {
    await setSession({ status: 'authenticated' });
    renderAt('/leave');

    expect(await screen.findByText('İzin sayfası')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Ahmet Yılmaz' })).toBeInTheDocument();
  });

  it('cikis jetonu sunucuda iptal eder ve adres tasimadan girise doner', async () => {
    await setSession({ status: 'authenticated' });
    const signOut = vi.spyOn(sessionApi, 'signOut').mockResolvedValue(undefined);
    signOut.mockClear();
    const user = userEvent.setup();
    renderAt('/leave');

    await user.click(await screen.findByRole('button', { name: 'Ahmet Yılmaz' }));
    await user.click(await screen.findByRole('menuitem', { name: 'Çıkış yap' }));

    expect(await screen.findByText(/^Giriş ekranı\s*$/)).toBeInTheDocument();
    expect(signOut).toHaveBeenCalledOnce();
  });

  it('kullanici menusunden parola degistirme sayfasina gider (SYG-KMLK-048)', async () => {
    await setSession({ status: 'authenticated' });
    const user = userEvent.setup();
    renderAt('/leave');

    await user.click(await screen.findByRole('button', { name: 'Ahmet Yılmaz' }));
    await user.click(await screen.findByRole('menuitem', { name: 'Parolamı değiştir' }));

    expect(await screen.findByText('Parola sayfası')).toBeInTheDocument();
  });

  it('hesap islemleri baglantisi yalnizca izni olana gorunur', async () => {
    const user = userEvent.setup();
    await setSession({ status: 'authenticated' });
    renderAt('/leave');

    await user.click(await screen.findByRole('button', { name: 'Ahmet Yılmaz' }));
    expect(await screen.findByRole('menuitem', { name: 'Parolamı değiştir' })).toBeInTheDocument();
    expect(screen.queryByRole('menuitem', { name: 'Hesap işlemleri' })).not.toBeInTheDocument();
  });

  it('izni olan kullanici menuden hesap islemlerine gider', async () => {
    const user = userEvent.setup();
    await setSession({ status: 'authenticated', permissions: ['identity.account.view'] });
    renderAt('/leave');

    await user.click(await screen.findByRole('button', { name: 'Ahmet Yılmaz' }));
    await user.click(await screen.findByRole('menuitem', { name: 'Hesap işlemleri' }));

    expect(await screen.findByText('Hesaplar sayfası')).toBeInTheDocument();
  });

  it('oturum baska cihazdan giriste kapaninca girise doner', async () => {
    await setSession({ status: 'authenticated' });
    renderAt('/leave');
    await screen.findByText('İzin sayfası');

    await setSession({ status: 'anonymous', endReason: 'signed-in-elsewhere' });

    expect(await screen.findByText(/^Giriş ekranı \?returnTo=/)).toBeInTheDocument();
  });
});
