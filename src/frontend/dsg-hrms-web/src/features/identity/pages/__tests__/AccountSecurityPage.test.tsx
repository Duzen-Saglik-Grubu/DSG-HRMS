import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { renderWithProviders } from '@/test/render';
import { setSession } from '@/test/session';
import { registrationApi } from '../../api/registrationApi';
import { twoFactorApi, type TwoFactorStatus } from '../../api/twoFactorApi';
import { AccountSecurityPage } from '../AccountSecurityPage';

/** Kullanicinin kendi iki adimli dogrulama tercihi (SYG-KMLK-080). Veriler SENTETIKTIR. */
const FUTURE = () => new Date(Date.now() + 5 * 60_000).toISOString();

function status(overrides: Partial<TwoFactorStatus> = {}): TwoFactorStatus {
  return { available: true, enabled: false, channels: ['email', 'sms'], ...overrides };
}

function wrongPassword() {
  return new ApiError({
    message: 'Doğrulama hatası',
    status: 400,
    isNetworkError: false,
    errors: { currentPassword: ['Mevcut parolanız hatalı.'] },
  });
}

describe('AccountSecurityPage', () => {
  beforeEach(async () => {
    vi.restoreAllMocks();
    await setSession({ status: 'authenticated' });
    vi.spyOn(registrationApi, 'publicSettings').mockResolvedValue({
      supportContact: 'Bilgi İşlem',
      passwordRules: { minLength: 6, maxLength: 128, requireComplexity: false },
      verificationCodeLength: 6,
      logoVersion: null,
      twoFactorAvailable: true,
    });
  });

  it('parola, kanal ve kodla iki adimli dogrulamayi acar', async () => {
    vi.spyOn(twoFactorApi, 'status')
      .mockResolvedValueOnce(status())
      .mockResolvedValue(status({ enabled: true }));
    const startSetup = vi
      .spyOn(twoFactorApi, 'startSetup')
      .mockResolvedValue({ codeId: 'kod-1', codeExpiresAt: FUTURE() });
    const verifySetup = vi
      .spyOn(twoFactorApi, 'verifySetup')
      .mockResolvedValue({ result: 'verified' });
    const user = userEvent.setup();
    renderWithProviders(<AccountSecurityPage />);

    expect(await screen.findByText(/hesabınızda kapalı/)).toBeInTheDocument();
    await user.type(screen.getByLabelText('Mevcut parola'), 'Kediler uyur 7');
    await user.click(screen.getByRole('radio', { name: 'SMS ile' }));
    await user.click(screen.getByRole('button', { name: 'Kodu gönder' }));

    expect(await screen.findByText(/seçtiğiniz yönteme gönderildi/)).toBeInTheDocument();
    await user.type(screen.getByLabelText('Doğrulama kodu'), '123456');
    await user.click(screen.getByRole('button', { name: 'Doğrula' }));

    expect(await screen.findByRole('status')).toHaveTextContent('İki adımlı doğrulama açıldı');
    expect(startSetup).toHaveBeenCalledWith({ currentPassword: 'Kediler uyur 7', channel: 'sms' });
    expect(verifySetup).toHaveBeenCalledWith('kod-1', '123456');
    expect(await screen.findByText(/hesabınızda açık/)).toBeInTheDocument();
  });

  it('yalnizca kisinin kullanabilecegi kanallari sunar', async () => {
    vi.spyOn(twoFactorApi, 'status').mockResolvedValue(status({ channels: ['email'] }));
    renderWithProviders(<AccountSecurityPage />);

    expect(await screen.findByRole('radio', { name: 'E-posta ile' })).toBeChecked();
    expect(screen.queryByRole('radio', { name: 'SMS ile' })).not.toBeInTheDocument();
  });

  it('mevcut parola hataliysa alanin altinda soyler ve kod adimina gecmez', async () => {
    vi.spyOn(twoFactorApi, 'status').mockResolvedValue(status());
    vi.spyOn(twoFactorApi, 'startSetup').mockRejectedValue(wrongPassword());
    const user = userEvent.setup();
    renderWithProviders(<AccountSecurityPage />);

    await user.type(await screen.findByLabelText('Mevcut parola'), 'Yanlis parola 1');
    await user.click(screen.getByRole('button', { name: 'Kodu gönder' }));

    expect(await screen.findByText('Mevcut parolanız hatalı.')).toBeInTheDocument();
    expect(screen.getByLabelText('Mevcut parola')).toHaveAttribute('aria-invalid', 'true');
    expect(screen.queryByLabelText('Doğrulama kodu')).not.toBeInTheDocument();
  });

  it('mevcut parola girilmeden istek gondermez', async () => {
    vi.spyOn(twoFactorApi, 'status').mockResolvedValue(status());
    const startSetup = vi.spyOn(twoFactorApi, 'startSetup');
    const user = userEvent.setup();
    renderWithProviders(<AccountSecurityPage />);

    await user.click(await screen.findByRole('button', { name: 'Kodu gönder' }));

    expect(await screen.findByText('Mevcut parolanızı girin.')).toBeInTheDocument();
    expect(startSetup).not.toHaveBeenCalled();
  });

  it('kanali yoksa ne yapacagini soyler ve acma formunu gostermez', async () => {
    vi.spyOn(twoFactorApi, 'status').mockResolvedValue(status({ channels: [] }));
    renderWithProviders(<AccountSecurityPage />);

    expect(
      await screen.findByText(/İnsan Kaynakları birimiyle iletişime geçin/),
    ).toBeInTheDocument();
    expect(screen.queryByLabelText('Mevcut parola')).not.toBeInTheDocument();
  });

  it('parola ve onayla iki adimli dogrulamayi kapatir', async () => {
    vi.spyOn(twoFactorApi, 'status')
      .mockResolvedValueOnce(status({ enabled: true }))
      .mockResolvedValue(status());
    const disable = vi.spyOn(twoFactorApi, 'disable').mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderWithProviders(<AccountSecurityPage />);

    expect(await screen.findByText(/hesabınızda açık/)).toBeInTheDocument();
    await user.type(screen.getByLabelText('Mevcut parola'), 'Kediler uyur 7');
    await user.click(screen.getByRole('button', { name: 'Kapat' }));

    const dialog = await screen.findByRole('dialog', {
      name: 'İki adımlı doğrulama kapatılsın mı?',
    });
    expect(disable).not.toHaveBeenCalled();
    await user.click(within(dialog).getByRole('button', { name: 'Kapat' }));

    expect(await screen.findByRole('status')).toHaveTextContent('İki adımlı doğrulama kapatıldı');
    expect(disable).toHaveBeenCalledWith({ currentPassword: 'Kediler uyur 7' });
  });

  it('kapatirken mevcut parola hataliysa alanin altinda soyler', async () => {
    vi.spyOn(twoFactorApi, 'status').mockResolvedValue(status({ enabled: true }));
    vi.spyOn(twoFactorApi, 'disable').mockRejectedValue(wrongPassword());
    const user = userEvent.setup();
    renderWithProviders(<AccountSecurityPage />);

    await user.type(await screen.findByLabelText('Mevcut parola'), 'Yanlis parola 1');
    await user.click(screen.getByRole('button', { name: 'Kapat' }));
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: 'Kapat' }),
    );

    expect(await screen.findByText('Mevcut parolanız hatalı.')).toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('sistemde kullanilmiyorsa bunu soyler ve acma formunu gostermez', async () => {
    vi.spyOn(twoFactorApi, 'status').mockResolvedValue(status({ available: false }));
    renderWithProviders(<AccountSecurityPage />);

    expect(await screen.findByText(/şu anda sistemde kullanılmıyor/)).toBeInTheDocument();
    expect(screen.queryByLabelText('Mevcut parola')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Kodu gönder' })).not.toBeInTheDocument();
  });

  it('sistemde kullanilmiyorken acik kalan tercihi kapatabilir', async () => {
    vi.spyOn(twoFactorApi, 'status').mockResolvedValue(status({ available: false, enabled: true }));
    renderWithProviders(<AccountSecurityPage />);

    expect(await screen.findByText(/girişte kod istenmiyor/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Kapat' })).toBeInTheDocument();
  });

  it('sistem yoneticisi kapattiysa sunucunun iletisini gosterir', async () => {
    vi.spyOn(twoFactorApi, 'status').mockResolvedValue(status());
    vi.spyOn(twoFactorApi, 'startSetup').mockRejectedValue(
      new ApiError({
        message: 'İki adımlı doğrulama sistem yöneticisi tarafından kapatıldı. Şu anda açılamaz.',
        status: 422,
        isNetworkError: false,
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<AccountSecurityPage />);

    await user.type(await screen.findByLabelText('Mevcut parola'), 'Kediler uyur 7');
    await user.click(screen.getByRole('button', { name: 'Kodu gönder' }));

    expect(await screen.findByText(/sistem yöneticisi tarafından kapatıldı/)).toBeInTheDocument();
    expect(screen.queryByLabelText('Doğrulama kodu')).not.toBeInTheDocument();
  });
});
