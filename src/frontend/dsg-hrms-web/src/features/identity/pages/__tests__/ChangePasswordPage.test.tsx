import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { renderWithProviders } from '@/test/render';
import { passwordApi } from '../../api/passwordApi';
import { registrationApi } from '../../api/registrationApi';
import { ChangePasswordPage } from '../ChangePasswordPage';

/** Oturum icinde parola degisikligi (SYG-KMLK-048). Veriler SENTETIKTIR. */
async function fill(
  user: ReturnType<typeof userEvent.setup>,
  current = 'Kediler uyur 7',
  next = 'Mavi deniz 42 kez',
  confirm = next,
) {
  await user.type(await screen.findByLabelText('Mevcut parola'), current);
  await user.type(screen.getByLabelText('Yeni parola'), next);
  await user.type(screen.getByLabelText('Yeni parola (tekrar)'), confirm);
  await user.click(screen.getByRole('button', { name: 'Parolayı değiştir' }));
}

function fieldError(field: string, message: string) {
  return new ApiError({
    message: 'Doğrulama hatası',
    status: 400,
    isNetworkError: false,
    errors: { [field]: [message] },
  });
}

describe('ChangePasswordPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.spyOn(registrationApi, 'publicSettings').mockResolvedValue({
      supportContact: 'Bilgi İşlem',
      passwordRules: { minLength: 6, maxLength: 128, requireComplexity: false },
      verificationCodeLength: 6,
      logoVersion: null,
    });
  });

  it('mevcut parolayla birlikte gonderir; basarida diger oturumlarin kapandigini soyler', async () => {
    const change = vi.spyOn(passwordApi, 'change').mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderWithProviders(<ChangePasswordPage />);

    await fill(user);

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Diğer cihazlardaki oturumlarınız kapatıldı',
    );
    expect(change).toHaveBeenCalledWith({
      currentPassword: 'Kediler uyur 7',
      newPassword: 'Mavi deniz 42 kez',
    });
    expect(screen.getByLabelText('Mevcut parola')).toHaveValue('');
  });

  it('mevcut parola hataliysa alanin altinda soyler', async () => {
    vi.spyOn(passwordApi, 'change').mockRejectedValue(
      fieldError('currentPassword', 'Mevcut parolanız hatalı.'),
    );
    const user = userEvent.setup();
    renderWithProviders(<ChangePasswordPage />);

    await fill(user, 'Yanlis parola 1');

    expect(await screen.findByText('Mevcut parolanız hatalı.')).toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('yeni parola kural disiysa sunucunun iletisini yeni parola alaninda gosterir', async () => {
    vi.spyOn(passwordApi, 'change').mockRejectedValue(
      fieldError('newPassword', 'Yeni parola mevcut parolanızdan farklı olmalıdır.'),
    );
    const user = userEvent.setup();
    renderWithProviders(<ChangePasswordPage />);

    await fill(user, 'Kediler uyur 7', 'Kediler uyur 7');

    expect(
      await screen.findByText('Yeni parola mevcut parolanızdan farklı olmalıdır.'),
    ).toBeInTheDocument();
  });

  it('kilit iletisini gosterir', async () => {
    vi.spyOn(passwordApi, 'change').mockRejectedValue(
      new ApiError({
        message: 'Çok fazla hatalı deneme yapıldı. 15 dakika sonra tekrar deneyin.',
        status: 429,
        isNetworkError: false,
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<ChangePasswordPage />);

    await fill(user);

    expect(await screen.findByRole('alert')).toHaveTextContent('15 dakika sonra');
  });

  it('istemcide eksik veya uyusmayan alanlari gondermez', async () => {
    const change = vi.spyOn(passwordApi, 'change');
    const user = userEvent.setup();
    renderWithProviders(<ChangePasswordPage />);

    await user.click(await screen.findByRole('button', { name: 'Parolayı değiştir' }));
    expect(await screen.findByText('Mevcut parolanızı girin.')).toBeInTheDocument();

    await fill(user, 'Kediler uyur 7', 'Mavi deniz 42 kez', 'Baska bir sey 1');
    expect(await screen.findByText('Parolalar aynı değil.')).toBeInTheDocument();
    expect(change).not.toHaveBeenCalled();
  });

  it('beklenmeyen hatada takip numarasini gosterir', async () => {
    vi.spyOn(passwordApi, 'change').mockRejectedValue(
      new ApiError({ message: 'Sorun', status: 500, isNetworkError: false, traceId: 'iz-42' }),
    );
    const user = userEvent.setup();
    renderWithProviders(<ChangePasswordPage />);

    await fill(user);

    expect(await screen.findByText(/iz-42/)).toBeInTheDocument();
  });
});
