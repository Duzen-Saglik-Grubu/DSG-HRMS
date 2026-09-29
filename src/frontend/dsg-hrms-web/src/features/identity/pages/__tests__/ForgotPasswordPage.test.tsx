import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { renderWithProviders } from '@/test/render';
import { passwordApi } from '../../api/passwordApi';
import { registrationApi } from '../../api/registrationApi';
import { ForgotPasswordPage } from '../ForgotPasswordPage';

/**
 * Parola sifirlama ekrani (SYG-KMLK-047). Veriler SENTETIKTIR.
 *
 * Adimlar uyelikle ayni bilesenlerdir; burada sifirlamaya ozgu cagrilar ve sonuclar denetlenir.
 */
const VALID_NATIONAL_ID = '10000000146';
const FUTURE = () => new Date(Date.now() + 5 * 60_000).toISOString();

async function reachCodeStep(user: ReturnType<typeof userEvent.setup>) {
  await user.type(await screen.findByLabelText('T.C. Kimlik Numarası'), VALID_NATIONAL_ID);
  const group = screen.getByRole('group', { name: 'Doğum tarihi' });
  await user.click(within(group).getAllByRole('spinbutton')[0]!);
  await user.keyboard('12041985');
  await user.type(screen.getByLabelText('Kurumsal e-posta adresi'), 'ahmet.yilmaz@duzen.com.tr');
  await user.click(screen.getByRole('button', { name: 'Devam et' }));
  await user.click(await screen.findByRole('button', { name: 'Kodu gönder' }));
  await user.type(await screen.findByLabelText('Doğrulama kodu'), '123456');
  await user.click(screen.getByRole('button', { name: 'Doğrula' }));
}

describe('ForgotPasswordPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.spyOn(registrationApi, 'publicSettings').mockResolvedValue({
      supportContact: 'Bilgi İşlem - dahili 1234',
      passwordRules: { minLength: 6, maxLength: 128, requireComplexity: false },
      verificationCodeLength: 6,
    });
    vi.spyOn(passwordApi, 'startReset').mockResolvedValue({
      registrationId: 'r-1',
      channels: ['email', 'sms'],
      expiresAt: FUTURE(),
    });
    vi.spyOn(passwordApi, 'requestCode').mockResolvedValue({ codeExpiresAt: FUTURE() });
    vi.spyOn(passwordApi, 'verify').mockResolvedValue({ result: 'verified', accountExists: true });
  });

  it('uyelikle ayni adimlarla kimligi dogrular ve yeni parolayi belirler', async () => {
    const reset = vi.spyOn(passwordApi, 'reset').mockResolvedValue(undefined);
    const register = vi.spyOn(registrationApi, 'start');
    const user = userEvent.setup();
    renderWithProviders(<ForgotPasswordPage />);

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Parolamı unuttum' }),
    ).toBeInTheDocument();
    await reachCodeStep(user);

    expect(await screen.findByRole('heading', { name: 'Yeni parola' })).toBeInTheDocument();
    expect(screen.getByText(/Açık olan tüm oturumlarınız kapatılacak/)).toBeInTheDocument();
    await user.type(screen.getByLabelText('Parola'), 'Mavi deniz 42 kez');
    await user.type(screen.getByLabelText('Parola (tekrar)'), 'Mavi deniz 42 kez');
    await user.click(screen.getByRole('button', { name: 'Parolayı değiştir' }));

    expect(
      await screen.findByRole('heading', { name: 'Parolanız değiştirildi' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Giriş yap' })).toHaveAttribute('href', '/login');
    expect(reset).toHaveBeenCalledWith('r-1', 'Mavi deniz 42 kez');
    expect(passwordApi.startReset).toHaveBeenCalledWith({
      nationalId: VALID_NATIONAL_ID,
      birthDate: '1985-04-12',
      email: 'ahmet.yilmaz@duzen.com.tr',
    });
    // Sifirlama uyelik ucunu kullanmaz; amaci sunucuya ayri uc soyler (kod iletisi metni).
    expect(register).not.toHaveBeenCalled();
  });

  it('hesap yoksa bunu YALNIZCA kod dogrulandiktan sonra soyler ve uyelige yonlendirir', async () => {
    vi.spyOn(passwordApi, 'verify').mockResolvedValue({ result: 'verified', accountExists: false });
    const user = userEvent.setup();
    renderWithProviders(<ForgotPasswordPage />);

    await reachCodeStep(user);

    expect(
      await screen.findByRole('heading', { name: 'Hesabınız bulunmuyor' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Üye ol' })).toHaveAttribute('href', '/register');
  });

  it('sunucunun parola kurali iletisini alanin altinda gosterir', async () => {
    vi.spyOn(passwordApi, 'reset').mockRejectedValue(
      new ApiError({
        message: 'Doğrulama hatası',
        status: 400,
        isNetworkError: false,
        errors: { password: ['Bu parola çok yaygın ve kolay tahmin edilir.'] },
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<ForgotPasswordPage />);

    await reachCodeStep(user);
    await user.type(await screen.findByLabelText('Parola'), 'Qwerty12');
    await user.type(screen.getByLabelText('Parola (tekrar)'), 'Qwerty12');
    await user.click(screen.getByRole('button', { name: 'Parolayı değiştir' }));

    expect(
      await screen.findByText('Bu parola çok yaygın ve kolay tahmin edilir.'),
    ).toBeInTheDocument();
  });

  it('islemin suresi dolduysa sifirlamaya ozgu iletiyle bastan baslatir', async () => {
    vi.spyOn(passwordApi, 'requestCode').mockRejectedValue(
      new ApiError({
        message: 'Kayıt bulunamadı',
        status: 404,
        isNetworkError: false,
        type: 'https://dsg-hrms/errors/not-found',
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<ForgotPasswordPage />);

    await user.type(await screen.findByLabelText('T.C. Kimlik Numarası'), VALID_NATIONAL_ID);
    const group = screen.getByRole('group', { name: 'Doğum tarihi' });
    await user.click(within(group).getAllByRole('spinbutton')[0]!);
    await user.keyboard('12041985');
    await user.type(screen.getByLabelText('Kurumsal e-posta adresi'), 'ahmet.yilmaz@duzen.com.tr');
    await user.click(screen.getByRole('button', { name: 'Devam et' }));
    await user.click(await screen.findByRole('button', { name: 'Kodu gönder' }));

    expect(
      await screen.findByText('Parola sıfırlama işleminin süresi doldu. Lütfen baştan başlayın.'),
    ).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Baştan başla' }));
    expect(await screen.findByLabelText('T.C. Kimlik Numarası')).toHaveValue('');
  });

  it('giris ekranina donus baglantisi verir', async () => {
    renderWithProviders(<ForgotPasswordPage />);

    expect(await screen.findByRole('link', { name: 'Giriş ekranına dön' })).toHaveAttribute(
      'href',
      '/login',
    );
  });
});
