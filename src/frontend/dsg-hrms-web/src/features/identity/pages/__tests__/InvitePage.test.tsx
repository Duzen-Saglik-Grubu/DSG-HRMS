import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { renderWithProviders } from '@/test/render';
import { invitationApi } from '../../api/invitationApi';
import { registrationApi } from '../../api/registrationApi';
import { InvitePage } from '../InvitePage';

/** IK'nin gonderdigi parola olusturma baglantisi (SYG-KMLK-051, 052). Veriler SENTETIKTIR. */
const TOKEN = 'abcDEF123_-xyz';
const FUTURE = () => new Date(Date.now() + 3 * 60 * 60_000).toISOString();

function openWith(hash: string) {
  window.history.replaceState(null, '', `/invite${hash}`);
}

describe('InvitePage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.spyOn(registrationApi, 'publicSettings').mockResolvedValue({
      supportContact: 'Bilgi İşlem',
      passwordRules: { minLength: 6, maxLength: 128, requireComplexity: false },
      verificationCodeLength: 6,
    });
  });

  afterEach(() => {
    window.history.replaceState(null, '', '/');
  });

  it('jetonu adresten siler, parolayi kaydeder ve girise yonlendirir', async () => {
    const lookup = vi
      .spyOn(invitationApi, 'lookup')
      .mockResolvedValue({ firstName: 'Mehmet', accountExists: false, expiresAt: FUTURE() });
    const accept = vi.spyOn(invitationApi, 'accept').mockResolvedValue(undefined);
    openWith(`#token=${TOKEN}`);
    const user = userEvent.setup();
    renderWithProviders(<InvitePage />);

    expect(window.location.hash).toBe('');
    expect(
      await screen.findByText('Merhaba Mehmet, hesabınızı oluşturmak için bir parola belirleyin.'),
    ).toBeInTheDocument();
    expect(lookup).toHaveBeenCalledWith(TOKEN);

    await user.type(screen.getByLabelText('Parola'), 'Mavi deniz 42 kez');
    await user.type(screen.getByLabelText('Parola (tekrar)'), 'Mavi deniz 42 kez');
    await user.click(screen.getByRole('button', { name: 'Parolayı kaydet' }));

    expect(await screen.findByRole('status')).toHaveTextContent('Parolanız kaydedildi.');
    expect(accept).toHaveBeenCalledWith(TOKEN, 'Mavi deniz 42 kez');
    expect(screen.getByRole('link', { name: 'Giriş yap' })).toHaveAttribute('href', '/login');
  });

  it('hesabi olan kisiye oturumlarinin kapanacagini soyler', async () => {
    vi.spyOn(invitationApi, 'lookup').mockResolvedValue({
      firstName: 'Mehmet',
      accountExists: true,
      expiresAt: FUTURE(),
    });
    openWith(`#token=${TOKEN}`);
    renderWithProviders(<InvitePage />);

    expect(
      await screen.findByText('Merhaba Mehmet, hesabınız için yeni bir parola belirleyin.'),
    ).toBeInTheDocument();
    expect(screen.getByText(/açık olan tüm oturumlarınız kapatılır/)).toBeInTheDocument();
  });

  it('gecersiz veya suresi dolmus baglantida ne yapilacagini soyler', async () => {
    vi.spyOn(invitationApi, 'lookup').mockRejectedValue(
      new ApiError({ message: 'Bağlantı geçersiz', status: 404, isNetworkError: false }),
    );
    openWith(`#token=${TOKEN}`);
    renderWithProviders(<InvitePage />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Bu bağlantı geçersiz');
    expect(
      screen.getByRole('link', { name: 'Parolamı kendim sıfırlamak istiyorum' }),
    ).toHaveAttribute('href', '/forgot-password');
  });

  it('jetonsuz acilan sayfa sunucuya sormadan gecersiz der', async () => {
    const lookup = vi.spyOn(invitationApi, 'lookup');
    openWith('');
    renderWithProviders(<InvitePage />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Bu bağlantı geçersiz');
    expect(lookup).not.toHaveBeenCalled();
  });

  it('sunucunun parola kurali iletisini gosterir', async () => {
    vi.spyOn(invitationApi, 'lookup').mockResolvedValue({
      firstName: 'Mehmet',
      accountExists: false,
      expiresAt: FUTURE(),
    });
    vi.spyOn(invitationApi, 'accept').mockRejectedValue(
      new ApiError({
        message: 'Doğrulama hatası',
        status: 400,
        isNetworkError: false,
        errors: { password: ['Bu parola çok yaygın ve kolay tahmin edilir.'] },
      }),
    );
    openWith(`#token=${TOKEN}`);
    const user = userEvent.setup();
    renderWithProviders(<InvitePage />);

    await user.type(await screen.findByLabelText('Parola'), 'Qwerty12');
    await user.type(screen.getByLabelText('Parola (tekrar)'), 'Qwerty12');
    await user.click(screen.getByRole('button', { name: 'Parolayı kaydet' }));

    expect(
      await screen.findByText('Bu parola çok yaygın ve kolay tahmin edilir.'),
    ).toBeInTheDocument();
  });
});
