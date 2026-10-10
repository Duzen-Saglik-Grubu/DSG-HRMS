import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fetchPublicSettings } from '@/shared/api/publicSettings';
import { routes } from '@/shared/routes';
import { setSession } from '@/test/session';
import { AppProviders } from '../AppProviders';
import { AppLayout } from '../AppLayout';

vi.mock('@/shared/api/publicSettings', () => ({ fetchPublicSettings: vi.fn() }));

/** Kullanici menusu: hesap guvenligi yalnizca 2FA sistemde kullaniliyorsa gorunur (SYG-KMLK-080). */
function settings(twoFactorAvailable: boolean) {
  return {
    supportContact: 'Bilgi İşlem',
    passwordRules: { minLength: 6, maxLength: 128, requireComplexity: false },
    verificationCodeLength: 6,
    logoVersion: null,
    twoFactorAvailable,
  };
}

function renderLayout() {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: <AppLayout />,
        children: [
          { index: true, element: <p>Ana içerik</p> },
          { path: routes.accountSecurity, element: <p>Güvenlik sayfası</p> },
        ],
      },
    ],
    { initialEntries: ['/'] },
  );
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <AppProviders queryClient={queryClient}>
      <RouterProvider router={router} />
    </AppProviders>,
  );
}

async function openMenu(user: ReturnType<typeof userEvent.setup>) {
  await user.click(await screen.findByRole('button', { name: 'Ahmet Yılmaz' }));
  await screen.findByRole('menu');
}

describe('kullanici menusu', () => {
  beforeEach(async () => {
    vi.mocked(fetchPublicSettings).mockReset();
    await setSession({ status: 'authenticated' });
  });

  it('iki adimli dogrulama sistemde kullaniliyorsa hesap guvenligi sayfasina goturur', async () => {
    vi.mocked(fetchPublicSettings).mockResolvedValue(settings(true));
    const user = userEvent.setup();
    renderLayout();

    await openMenu(user);
    await user.click(await screen.findByRole('menuitem', { name: 'Hesap güvenliği' }));

    expect(await screen.findByText('Güvenlik sayfası')).toBeInTheDocument();
  });

  it('iki adimli dogrulama sistemde kullanilmiyorsa hesap guvenligini gostermez', async () => {
    vi.mocked(fetchPublicSettings).mockResolvedValue(settings(false));
    const user = userEvent.setup();
    renderLayout();

    await openMenu(user);

    expect(screen.getByRole('menuitem', { name: 'Parolamı değiştir' })).toBeInTheDocument();
    expect(screen.queryByRole('menuitem', { name: 'Hesap güvenliği' })).not.toBeInTheDocument();
    expect(fetchPublicSettings).toHaveBeenCalled();
  });
});
