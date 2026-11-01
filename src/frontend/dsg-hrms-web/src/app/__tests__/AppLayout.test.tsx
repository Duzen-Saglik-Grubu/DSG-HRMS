import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fetchPublicSettings } from '@/shared/api/publicSettings';
import { permissions } from '@/shared/auth/permissions';
import { routes } from '@/shared/routes';
import { setSession } from '@/test/session';
import { AppProviders } from '../AppProviders';
import { AppLayout } from '../AppLayout';
import { useLocation } from 'react-router';

/** Hizli aramanin actigi adresi gosterir. */
function PersonnelProbe() {
  const { search } = useLocation();
  return <p>Personel sayfası {search}</p>;
}

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
          { path: routes.personnel, element: <PersonnelProbe /> },
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

/** Uygulama kabugu (#309): bolumlu sol menu, daraltma, konum yolu ve hizli arama. */
describe('sol menu ve ust cubuk', () => {
  beforeEach(() => {
    vi.mocked(fetchPublicSettings).mockReset();
    vi.mocked(fetchPublicSettings).mockResolvedValue(settings(false));
    try {
      localStorage.clear();
    } catch {
      // depolama yoksa daraltma tercihi zaten tutulmaz
    }
  });

  it('baglantilar izne gore gorunur; etkin sayfa konum yolunda yazar', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.personView] });
    renderLayout();

    expect(await screen.findByRole('link', { name: 'Personel listesi' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Ana Sayfa' })).toHaveClass('active');
    expect(screen.queryByRole('link', { name: 'Hesap işlemleri' })).not.toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Sistem' })).not.toBeInTheDocument();
    const breadcrumb = screen.getByRole('navigation', { name: 'Konum' });
    expect(breadcrumb).toHaveTextContent('Genel Bakış');
    expect(breadcrumb).toHaveTextContent('Ana Sayfa');
  });

  it('menu daraltilinca baglantilar adlariyla erisilebilir kalir', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.personView] });
    const user = userEvent.setup();
    renderLayout();

    await user.click(await screen.findByRole('button', { name: 'Menüyü daralt' }));

    expect(screen.getByRole('button', { name: 'Menüyü genişlet' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Personel listesi' })).toBeInTheDocument();
    expect(screen.queryByText('Genel Bakış', { selector: 'h2' })).not.toBeInTheDocument();
  });

  it('hizli arama personel listesini terimle acar; izni olmayana sunulmaz', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.personView] });
    const user = userEvent.setup();
    renderLayout();

    await user.type(await screen.findByRole('textbox', { name: /Personel ara/ }), 'işık{Enter}');

    expect(await screen.findByText('Personel sayfası ?q=i%C5%9F%C4%B1k')).toBeInTheDocument();
  });

  it('personeli goremeyen kullaniciya hizli arama sunulmaz', async () => {
    await setSession({ status: 'authenticated' });
    renderLayout();

    await screen.findByRole('button', { name: 'Ahmet Yılmaz' });
    expect(screen.queryByRole('search')).not.toBeInTheDocument();
  });
});
