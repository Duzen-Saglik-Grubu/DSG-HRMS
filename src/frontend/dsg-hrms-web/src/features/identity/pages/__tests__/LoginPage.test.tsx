import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AppProviders } from '@/app/AppProviders';
import { ApiError } from '@/shared/api/problemDetails';
import { session } from '@/shared/auth/session';
import { fakeSession, setSession } from '@/test/session';
import { registrationApi } from '../../api/registrationApi';
import { signInApi } from '../../api/signInApi';
import { LoginPage } from '../LoginPage';

/**
 * Giris ekrani (SYG-KMLK-031…034, 042, 055, 064…070). Veriler SENTETIKTIR.
 *
 * Alanlar etiketleriyle bulunur (SYG-KMLK-066).
 */
const FUTURE = () => new Date(Date.now() + 5 * 60_000).toISOString();

function renderAt(path: string) {
  const router = createMemoryRouter(
    [
      { path: '/login', element: <LoginPage /> },
      { path: '/', element: <p>Ana sayfa</p> },
      { path: '/leave', element: <p>İzin sayfası</p> },
      { path: '/register', element: <p>Üyelik sayfası</p> },
    ],
    { initialEntries: [path] },
  );
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  render(
    <AppProviders queryClient={queryClient}>
      <RouterProvider router={router} />
    </AppProviders>,
  );

  return router;
}

function apiError(status: number, message: string, type?: string) {
  return new ApiError({ message, status, isNetworkError: false, type });
}

async function signIn(
  user: ReturnType<typeof userEvent.setup>,
  email = ' ahmet.yilmaz@duzen.com.tr ',
  password = 'Gizli.Parola1',
) {
  await user.type(await screen.findByLabelText('Kurumsal e-posta adresi'), email);
  await user.type(screen.getByLabelText('Parola'), password);
  await user.click(screen.getByRole('button', { name: 'Giriş yap' }));
}

describe('LoginPage', () => {
  beforeEach(async () => {
    vi.restoreAllMocks();
    await setSession({ status: 'anonymous' });
    vi.spyOn(registrationApi, 'publicSettings').mockResolvedValue({
      supportContact: 'Bilgi İşlem - dahili 1234',
      passwordRules: { minLength: 6, maxLength: 128, requireComplexity: false },
      verificationCodeLength: 6,
    });
  });

  it('e-posta ve parolayla giris yapar ve istenen sayfaya doner', async () => {
    const signInCall = vi
      .spyOn(signInApi, 'signIn')
      .mockResolvedValue({ status: 'signedIn', session: fakeSession(), challenge: null });
    const user = userEvent.setup();
    renderAt('/login?returnTo=%2Fleave');

    await signIn(user);

    expect(await screen.findByText('İzin sayfası')).toBeInTheDocument();
    expect(signInCall).toHaveBeenCalledWith({
      email: 'ahmet.yilmaz@duzen.com.tr',
      password: 'Gizli.Parola1',
    });
    expect(session.getState().status).toBe('authenticated');
  });

  it('baska siteye donus adresini yok sayar', async () => {
    vi.spyOn(signInApi, 'signIn').mockResolvedValue({
      status: 'signedIn',
      session: fakeSession(),
      challenge: null,
    });
    const user = userEvent.setup();
    renderAt('/login?returnTo=%2F%2Fornek.com');

    await signIn(user);

    expect(await screen.findByText('Ana sayfa')).toBeInTheDocument();
  });

  it('hatali giriste sunucunun iletisini gosterir, parolayi temizler, e-postayi korur', async () => {
    // Ileti hesap olsa da olmasa da aynidir (SYG-KMLK-032); ekran onu oldugu gibi gosterir.
    vi.spyOn(signInApi, 'signIn').mockRejectedValue(
      apiError(401, 'E-posta adresi veya parola hatalı.'),
    );
    const user = userEvent.setup();
    renderAt('/login');

    await signIn(user);

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'E-posta adresi veya parola hatalı.',
    );
    expect(screen.getByLabelText('Parola')).toHaveValue('');
    expect(screen.getByLabelText('Kurumsal e-posta adresi')).toHaveValue(
      'ahmet.yilmaz@duzen.com.tr',
    );
    expect(screen.getByLabelText('Parola')).toHaveFocus();
  });

  it.each([
    [429, 'Çok fazla hatalı deneme yapıldı. 15 dakika sonra tekrar deneyin.'],
    [403, 'Hesabınız kullanıma kapalı. İnsan Kaynakları birimine başvurun.'],
  ])('kilit ve kapali hesap iletisini gosterir (%i)', async (status, message) => {
    vi.spyOn(signInApi, 'signIn').mockRejectedValue(apiError(status, message));
    const user = userEvent.setup();
    renderAt('/login');

    await signIn(user);

    expect(await screen.findByRole('alert')).toHaveTextContent(message);
  });

  it('beklenmeyen hatada takip numarasini gosterir (SYG-KMLK-067)', async () => {
    vi.spyOn(signInApi, 'signIn').mockRejectedValue(
      new ApiError({
        message: 'Beklenmeyen bir sorun oluştu.',
        status: 500,
        isNetworkError: false,
        traceId: 'abc123',
      }),
    );
    const user = userEvent.setup();
    renderAt('/login');

    await signIn(user);

    expect(await screen.findByText(/abc123/)).toBeInTheDocument();
  });

  it('bos veya gecersiz alanlari sunucuya gondermez', async () => {
    const signInCall = vi.spyOn(signInApi, 'signIn');
    const user = userEvent.setup();
    renderAt('/login');

    await user.click(await screen.findByRole('button', { name: 'Giriş yap' }));

    expect(await screen.findByText('Geçerli bir e-posta adresi girin.')).toBeInTheDocument();
    expect(screen.getByText('Parolanızı girin.')).toBeInTheDocument();
    expect(signInCall).not.toHaveBeenCalled();
  });

  it('parolayi gosterip gizler', async () => {
    const user = userEvent.setup();
    renderAt('/login');

    const password = await screen.findByLabelText('Parola');
    expect(password).toHaveAttribute('type', 'password');

    await user.click(screen.getByRole('button', { name: 'Parolayı göster' }));
    expect(password).toHaveAttribute('type', 'text');

    await user.click(screen.getByRole('button', { name: 'Parolayı gizle' }));
    expect(password).toHaveAttribute('type', 'password');
  });

  it('baska cihazdan giris yapildiysa nedenini soyler; yeni denemede ileti kalkar (SYG-KMLK-042)', async () => {
    await setSession({ status: 'anonymous', endReason: 'signed-in-elsewhere' });
    vi.spyOn(signInApi, 'signIn').mockRejectedValue(
      apiError(401, 'E-posta adresi veya parola hatalı.'),
    );
    const user = userEvent.setup();
    renderAt('/login');

    expect(
      await screen.findByText(
        'Hesabınıza başka bir cihazdan giriş yapıldı. Devam etmek için yeniden giriş yapın.',
      ),
    ).toBeInTheDocument();

    await signIn(user);

    await waitFor(() =>
      expect(screen.queryByText(/başka bir cihazdan giriş yapıldı/)).not.toBeInTheDocument(),
    );
  });

  it.each([
    ['idle-timeout', 'Uzun süre işlem yapılmadığı için oturumunuz kapandı.'],
    ['expired', 'Oturum süreniz doldu.'],
    ['token-reuse', 'tüm oturumlar kapatıldı'],
    ['account-changed', 'Hesap bilgileriniz değiştiği için'],
    ['logged-out', 'Çıkış yaptınız.'],
    ['password-changed', 'Parolanız değiştirildiği için oturumunuz kapandı.'],
  ] as const)('oturum sonu nedenini soyler: %s', async (reason, text) => {
    await setSession({ status: 'anonymous', endReason: reason });
    renderAt('/login');

    expect(await screen.findByText(new RegExp(text))).toBeInTheDocument();
  });

  it('iki adimli dogrulama aciksa kanal ve kod adimlarindan sonra oturum acar (SYG-KMLK-034)', async () => {
    vi.spyOn(signInApi, 'signIn').mockResolvedValue({
      status: 'verificationRequired',
      session: null,
      challenge: { challengeId: 'c-1', channels: ['email', 'sms'], expiresAt: FUTURE() },
    });
    const requestCode = vi
      .spyOn(signInApi, 'requestCode')
      .mockResolvedValue({ codeExpiresAt: FUTURE() });
    const verify = vi
      .spyOn(signInApi, 'verify')
      .mockResolvedValue({ result: 'verified', session: fakeSession() });
    const user = userEvent.setup();
    renderAt('/login?returnTo=%2Fleave');

    await signIn(user);
    await user.click(await screen.findByRole('radio', { name: 'SMS ile' }));
    await user.click(screen.getByRole('button', { name: 'Kodu gönder' }));

    expect(await screen.findByText(/seçtiğiniz yönteme gönderildi/)).toBeInTheDocument();
    await user.type(screen.getByLabelText('Doğrulama kodu'), '123456');
    await user.click(screen.getByRole('button', { name: 'Doğrula' }));

    expect(await screen.findByText('İzin sayfası')).toBeInTheDocument();
    expect(requestCode).toHaveBeenCalledWith('c-1', 'sms');
    expect(verify).toHaveBeenCalledWith('c-1', '123456');
  });

  it('yanlis kodda oturum acmaz ve nedenini soyler', async () => {
    vi.spyOn(signInApi, 'signIn').mockResolvedValue({
      status: 'verificationRequired',
      session: null,
      challenge: { challengeId: 'c-1', channels: ['email'], expiresAt: FUTURE() },
    });
    vi.spyOn(signInApi, 'requestCode').mockResolvedValue({ codeExpiresAt: FUTURE() });
    vi.spyOn(signInApi, 'verify').mockResolvedValue({ result: 'mismatch', session: null });
    const user = userEvent.setup();
    renderAt('/login');

    await signIn(user);
    await user.click(await screen.findByRole('button', { name: 'Kodu gönder' }));
    await user.type(await screen.findByLabelText('Doğrulama kodu'), '000000');
    await user.click(screen.getByRole('button', { name: 'Doğrula' }));

    expect(await screen.findByText('Kod hatalı. Lütfen tekrar deneyin.')).toBeInTheDocument();
    expect(session.getState().status).toBe('anonymous');
  });

  it('bekleyen girisin suresi dolduysa yeniden girise dondurur', async () => {
    vi.spyOn(signInApi, 'signIn').mockResolvedValue({
      status: 'verificationRequired',
      session: null,
      challenge: { challengeId: 'c-1', channels: ['email'], expiresAt: FUTURE() },
    });
    vi.spyOn(signInApi, 'requestCode').mockRejectedValue(
      apiError(404, 'Kayıt bulunamadı', 'https://dsg-hrms/errors/not-found'),
    );
    const user = userEvent.setup();
    renderAt('/login');

    await signIn(user);
    await user.click(await screen.findByRole('button', { name: 'Kodu gönder' }));

    expect(
      await screen.findByText('Giriş işleminin süresi doldu. Lütfen yeniden giriş yapın.'),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Yeniden giriş yap' }));

    expect(await screen.findByLabelText('Kurumsal e-posta adresi')).toBeInTheDocument();
  });

  it('oturumu acik kullaniciyi dogrudan yonlendirir', async () => {
    await setSession({ status: 'authenticated' });
    renderAt('/login?returnTo=%2Fleave');

    expect(await screen.findByText('İzin sayfası')).toBeInTheDocument();
  });

  it('parola sifirlama ekranina baglanti verir', async () => {
    renderAt('/login');

    expect(await screen.findByRole('link', { name: 'Parolamı unuttum' })).toHaveAttribute(
      'href',
      '/forgot-password',
    );
  });

  it('uyelik ekranina baglanti verir ve destek bilgisini gosterir (SYG-KMLK-070)', async () => {
    const user = userEvent.setup();
    renderAt('/login');

    expect(await screen.findByText(/Bilgi İşlem - dahili 1234/)).toBeInTheDocument();
    await user.click(screen.getByRole('link', { name: 'Üye olun' }));

    expect(await screen.findByText('Üyelik sayfası')).toBeInTheDocument();
  });
});
