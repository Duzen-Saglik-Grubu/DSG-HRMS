import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { renderWithProviders } from '@/test/render';
import { passwordApi } from '../../api/passwordApi';
import { registrationApi } from '../../api/registrationApi';
import { RegistrationPage } from '../RegistrationPage';

/**
 * Uyelik ekranlari (SYG-KMLK-013…028, 064…070). Veriler SENTETIKTIR.
 *
 * Testler kullanici gibi davranir: alanlar ETIKETLERIYLE bulunur (SYG-KMLK-066). Etiketi
 * olmayan bir alan bu testlerde bulunamazdi.
 */
const VALID_NATIONAL_ID = '10000000146';
const FUTURE = () => new Date(Date.now() + 5 * 60_000).toISOString();

function apiError(
  status: number,
  extra: Partial<{ errors: Record<string, string[]>; traceId: string; message: string }> = {},
) {
  return new ApiError({
    message: extra.message ?? 'Sunucu hatası',
    status,
    isNetworkError: false,
    ...extra,
  });
}

/** Dogum tarihini klavyeyle yazar: alan gun, ay ve yil bolumlerinden olusur; rakamlar sirayla ilerler. */
async function typeBirthDate(user: ReturnType<typeof userEvent.setup>, digits = '12041985') {
  const group = screen.getByRole('group', { name: 'Doğum tarihi' });
  await user.click(within(group).getAllByRole('spinbutton')[0]!);
  await user.keyboard(digits);
}

async function fillIdentity(
  user: ReturnType<typeof userEvent.setup>,
  nationalId = VALID_NATIONAL_ID,
) {
  await user.type(screen.getByLabelText('T.C. Kimlik Numarası'), nationalId);
  await typeBirthDate(user);
  await user.type(screen.getByLabelText('Kurumsal e-posta adresi'), 'ahmet.yilmaz@duzen.com.tr');
  await user.click(screen.getByRole('button', { name: 'Devam et' }));
}

async function reachCodeStep(user: ReturnType<typeof userEvent.setup>) {
  await fillIdentity(user);
  await user.click(await screen.findByRole('button', { name: 'Kodu gönder' }));
  await screen.findByLabelText('Doğrulama kodu');
}

describe('RegistrationPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.spyOn(registrationApi, 'publicSettings').mockResolvedValue({
      supportContact: 'Bilgi İşlem - dahili 1234',
      passwordRules: { minLength: 6, maxLength: 128, requireComplexity: false },
      verificationCodeLength: 6,
      logoVersion: null,
    });
    vi.spyOn(registrationApi, 'start').mockResolvedValue({
      registrationId: 'r-1',
      channels: ['email', 'sms'],
      expiresAt: FUTURE(),
    });
    vi.spyOn(registrationApi, 'requestCode').mockImplementation(() =>
      Promise.resolve({ codeExpiresAt: FUTURE() }),
    );
    vi.spyOn(registrationApi, 'verify').mockResolvedValue({
      result: 'verified',
      accountExists: false,
    });
    vi.spyOn(registrationApi, 'complete').mockResolvedValue(undefined);
  });

  it('dort adimda hesap olusturur', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);

    await fillIdentity(user);
    expect(registrationApi.start).toHaveBeenCalledWith({
      nationalId: VALID_NATIONAL_ID,
      birthDate: '1985-04-12',
      email: 'ahmet.yilmaz@duzen.com.tr',
    });

    await user.click(await screen.findByRole('radio', { name: 'SMS ile' }));
    await user.click(screen.getByRole('button', { name: 'Kodu gönder' }));
    expect(registrationApi.requestCode).toHaveBeenCalledWith('r-1', 'sms');

    await user.type(await screen.findByLabelText('Doğrulama kodu'), '482915');
    await user.click(screen.getByRole('button', { name: 'Doğrula' }));
    expect(registrationApi.verify).toHaveBeenCalledWith('r-1', '482915');

    await user.type(await screen.findByLabelText('Parola'), 'Kediler uyur 7');
    await user.type(screen.getByLabelText('Parola (tekrar)'), 'Kediler uyur 7');
    await user.click(screen.getByRole('button', { name: 'Hesabı oluştur' }));

    expect(registrationApi.complete).toHaveBeenCalledWith('r-1', 'Kediler uyur 7');
    expect(
      await screen.findByText(
        'Artık kurumsal e-posta adresiniz ve parolanızla giriş yapabilirsiniz.',
      ),
    ).toBeInTheDocument();
  });

  it('logoyu alternatif metniyle ve destek iletisim bilgisini gosterir (SYG-KMLK-069, 070)', async () => {
    renderWithProviders(<RegistrationPage />);

    expect(screen.getByRole('img', { name: 'Düzen Sağlık Grubu logosu' })).toHaveAttribute(
      'src',
      '/brand/duzen_logo.png',
    );
    expect(
      await screen.findByText('Bilgi İşlem - dahili 1234 ile iletişime geçin.'),
    ).toBeInTheDocument();
  });

  it('gecersiz TCKN sunucuya gonderilmez (SYG-KMLK-014)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);

    await fillIdentity(user, '10000000147');

    expect(await screen.findByText('Geçerli bir T.C. Kimlik Numarası girin.')).toBeInTheDocument();
    expect(registrationApi.start).not.toHaveBeenCalled();
  });

  it('gelecekteki dogum tarihini ve gecersiz e-postayi alanlarinda bildirir', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);

    await user.type(screen.getByLabelText('T.C. Kimlik Numarası'), VALID_NATIONAL_ID);
    await typeBirthDate(user, '01012999');
    await user.type(screen.getByLabelText('Kurumsal e-posta adresi'), 'eposta');
    await user.click(screen.getByRole('button', { name: 'Devam et' }));

    expect(
      await screen.findByText('Doğum tarihini GG.AA.YYYY biçiminde girin.'),
    ).toBeInTheDocument();
    expect(screen.getByText('Geçerli bir e-posta adresi girin.')).toBeInTheDocument();
    expect(registrationApi.start).not.toHaveBeenCalled();
  });

  it('yalnizca sunulan kanallari listeler ve hedefi gostermez (SYG-KMLK-017, 018)', async () => {
    vi.mocked(registrationApi.start).mockResolvedValue({
      registrationId: 'r-1',
      channels: ['email'],
      expiresAt: FUTURE(),
    });
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);

    await fillIdentity(user);

    expect(await screen.findByRole('radio', { name: 'E-posta ile' })).toBeChecked();
    expect(screen.queryByRole('radio', { name: 'SMS ile' })).not.toBeInTheDocument();
    expect(screen.queryByText(/ahmet\.yilmaz/)).not.toBeInTheDocument();
  });

  it('dogum tarihi takvimden de secilebilir', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);

    await user.click(screen.getByRole('button', { name: 'Takvimden tarih seç' }));

    // Takvim yil gorunumuyle acilir: dogum yilina gunden gune ilerlemek gerekmez.
    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('radio', { name: '1985' }));
    await user.click(await within(dialog).findByRole('radio', { name: 'Nisan' }));
    await user.click(await within(dialog).findByRole('gridcell', { name: '12' }));

    await user.type(screen.getByLabelText('T.C. Kimlik Numarası'), VALID_NATIONAL_ID);
    await user.type(screen.getByLabelText('Kurumsal e-posta adresi'), 'ahmet.yilmaz@duzen.com.tr');
    await user.click(screen.getByRole('button', { name: 'Devam et' }));

    await waitFor(() =>
      expect(registrationApi.start).toHaveBeenCalledWith(
        expect.objectContaining({ birthDate: '1985-04-12' }),
      ),
    );
  });

  it('ilk adimdaki 404 "suresi doldu" sayilmaz; bastan baslat formu temizler', async () => {
    // Guncellenmemis bir sunucu uyelik adresini tanimaz ve 404 dondurur; bu, islemin
    // suresinin dolmasi DEGILDIR (#90 geri bildirimi).
    vi.mocked(registrationApi.start).mockRejectedValue(
      apiError(404, { traceId: 'IZ-404', message: 'Aradığınız kayıt bulunamadı.' }),
    );
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);

    await fillIdentity(user);

    expect(await screen.findByText(/IZ-404/)).toBeInTheDocument();
    expect(
      screen.queryByText('Üyelik işleminin süresi doldu. Lütfen baştan başlayın.'),
    ).not.toBeInTheDocument();
  });

  it('kod ekraninda her durumda "eslesiyorsa gelir" iletisini ve kalan sureyi gosterir (SYG-KMLK-016, 028)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);

    await reachCodeStep(user);

    expect(
      screen.getByText(
        /Bilgileriniz kayıtlarımızla eşleşiyorsa doğrulama kodu birkaç dakika içinde gelir/,
      ),
    ).toBeInTheDocument();
    expect(screen.getByText(/Kalan süre: [45]:\d{2}/)).toBeInTheDocument();
  });

  it('yanlis kodda deneme hakki surer; hak bitince yeni kod ister (SYG-KMLK-024)', async () => {
    vi.mocked(registrationApi.verify)
      .mockResolvedValueOnce({ result: 'mismatch', accountExists: false })
      .mockResolvedValueOnce({ result: 'attemptsExceeded', accountExists: false });
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);
    await reachCodeStep(user);

    await user.type(screen.getByLabelText('Doğrulama kodu'), '111111');
    await user.click(screen.getByRole('button', { name: 'Doğrula' }));
    expect(await screen.findByText('Kod hatalı. Lütfen tekrar deneyin.')).toBeInTheDocument();

    await user.type(screen.getByLabelText('Doğrulama kodu'), '222222');
    await user.click(screen.getByRole('button', { name: 'Doğrula' }));
    expect(
      await screen.findByText('Çok fazla hatalı deneme yapıldı. Yeni kod isteyin.'),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Doğrula' })).toBeDisabled();

    await user.click(screen.getByRole('button', { name: 'Kodu tekrar gönder' }));
    expect(
      await screen.findByText('Yeni kod gönderildi. Önceki kod artık geçersiz.'),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Doğrula' })).toBeEnabled();
    expect(registrationApi.requestCode).toHaveBeenCalledTimes(2);
  });

  it('suresi dolmus kodda dogrulamayi kapatir (SYG-KMLK-023)', async () => {
    vi.mocked(registrationApi.requestCode).mockResolvedValue({
      codeExpiresAt: new Date(Date.now() - 1000).toISOString(),
    });
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);
    await reachCodeStep(user);

    expect(screen.getByText('Kodun süresi doldu. Yeni kod isteyin.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Doğrula' })).toBeDisabled();
  });

  it('kanal degisiminde diger kanal secili gelir (SYG-KMLK-019)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);
    await reachCodeStep(user);

    await user.click(screen.getByRole('button', { name: 'Başka bir yöntemle doğrula' }));

    expect(await screen.findByRole('radio', { name: 'SMS ile' })).toBeChecked();
  });

  it('mevcut hesap yalnizca dogrulamadan sonra bildirilir (SYG-KMLK-020)', async () => {
    vi.mocked(registrationApi.verify).mockResolvedValue({
      result: 'verified',
      accountExists: true,
    });
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);
    await reachCodeStep(user);

    await user.type(screen.getByLabelText('Doğrulama kodu'), '482915');
    await user.click(screen.getByRole('button', { name: 'Doğrula' }));

    expect(await screen.findByRole('heading', { name: 'Hesabınız zaten var' })).toBeInTheDocument();
    expect(screen.queryByLabelText('Parola')).not.toBeInTheDocument();
  });

  it('mevcut hesabi olan kisi ayni islemle parolasini sifirlayabilir (SYG-KMLK-047)', async () => {
    vi.mocked(registrationApi.verify).mockResolvedValue({
      result: 'verified',
      accountExists: true,
    });
    const reset = vi.spyOn(passwordApi, 'reset').mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);
    await reachCodeStep(user);
    await user.type(screen.getByLabelText('Doğrulama kodu'), '482915');
    await user.click(screen.getByRole('button', { name: 'Doğrula' }));

    await user.click(await screen.findByRole('button', { name: 'Parolamı sıfırla' }));
    await user.type(await screen.findByLabelText('Parola'), 'Mavi deniz 42 kez');
    await user.type(screen.getByLabelText('Parola (tekrar)'), 'Mavi deniz 42 kez');
    await user.click(screen.getByRole('button', { name: 'Parolayı değiştir' }));

    expect(
      await screen.findByRole('heading', { name: 'Parolanız değiştirildi' }),
    ).toBeInTheDocument();
    expect(reset).toHaveBeenCalledWith('r-1', 'Mavi deniz 42 kez');
    expect(registrationApi.complete).not.toHaveBeenCalled();
  });

  it('sunucunun parola hatasini parola alaninda gosterir (SYG-KMLK-045)', async () => {
    vi.mocked(registrationApi.complete).mockRejectedValue(
      apiError(400, {
        errors: {
          password: [
            'Bu parola çok yaygın ve kolay tahmin edilir. Daha az bilinen bir parola seçin.',
          ],
        },
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);
    await reachCodeStep(user);
    await user.type(screen.getByLabelText('Doğrulama kodu'), '482915');
    await user.click(screen.getByRole('button', { name: 'Doğrula' }));

    await user.type(await screen.findByLabelText('Parola'), '123456789');
    await user.type(screen.getByLabelText('Parola (tekrar)'), '123456789');
    await user.click(screen.getByRole('button', { name: 'Hesabı oluştur' }));

    expect(await screen.findByText(/Bu parola çok yaygın/)).toBeInTheDocument();
    expect(screen.getByLabelText('Parola')).toHaveAttribute('aria-invalid', 'true');
  });

  it('parolalar ayni degilse gondermez; kurallari listeler', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);
    await reachCodeStep(user);
    await user.type(screen.getByLabelText('Doğrulama kodu'), '482915');
    await user.click(screen.getByRole('button', { name: 'Doğrula' }));

    expect(await screen.findByText('en az 6 karakter olmalı,')).toBeInTheDocument();
    await user.type(screen.getByLabelText('Parola'), 'Kediler uyur 7');
    await user.type(screen.getByLabelText('Parola (tekrar)'), 'Kediler uyur 8');
    await user.click(screen.getByRole('button', { name: 'Hesabı oluştur' }));

    expect(await screen.findByText('Parolalar aynı değil.')).toBeInTheDocument();
    expect(registrationApi.complete).not.toHaveBeenCalled();
  });

  it('parola gosterme dugmesinin erisilebilir adi vardir', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);
    await reachCodeStep(user);
    await user.type(screen.getByLabelText('Doğrulama kodu'), '482915');
    await user.click(screen.getByRole('button', { name: 'Doğrula' }));

    await user.click(await screen.findByRole('button', { name: 'Parolayı göster' }));

    expect(screen.getByLabelText('Parola')).toHaveAttribute('type', 'text');
    expect(screen.getByRole('button', { name: 'Parolayı gizle' })).toBeInTheDocument();
  });

  it('hiz sinirinda ne yapilacagini soyler (SYG-KMLK-059)', async () => {
    vi.mocked(registrationApi.start).mockRejectedValue(apiError(429));
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);

    await fillIdentity(user);

    expect(
      await screen.findByText('Çok fazla deneme yapıldı. Lütfen bir süre sonra tekrar deneyin.'),
    ).toBeInTheDocument();
  });

  it('suresi dolan islemde bastan baslatir', async () => {
    vi.mocked(registrationApi.requestCode).mockRejectedValue(
      new ApiError({
        message: 'x',
        status: 404,
        isNetworkError: false,
        type: 'https://dsg-hrms/errors/not-found',
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);
    await fillIdentity(user);
    await user.click(await screen.findByRole('button', { name: 'Kodu gönder' }));

    const alert = await screen.findByText('Üyelik işleminin süresi doldu. Lütfen baştan başlayın.');
    await user.click(
      within(alert.closest('[role="alert"]') as HTMLElement).getByRole('button', {
        name: 'Baştan başla',
      }),
    );

    // Form ve hata durumu temizlenir: ilk adim bos gelir.
    expect(await screen.findByLabelText('T.C. Kimlik Numarası')).toHaveValue('');
  });

  it('beklenmeyen hatada takip numarasini gosterir (SYG-KMLK-067)', async () => {
    vi.mocked(registrationApi.start).mockRejectedValue(
      apiError(500, { traceId: 'DESTEK-2026-0042', message: 'Beklenmeyen bir sorun oluştu.' }),
    );
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);

    await fillIdentity(user);

    expect(await screen.findByText(/DESTEK-2026-0042/)).toBeInTheDocument();
  });

  it('yalnizca klavyeyle kullanilabilir; odak sirasi gorsel sirayla aynidir (SYG-KMLK-066)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegistrationPage />);

    // Ilk alan otomatik odaklanir; sekme tusu gorsel sirayla ilerler.
    expect(screen.getByLabelText('T.C. Kimlik Numarası')).toHaveFocus();
    await user.keyboard(VALID_NATIONAL_ID);
    await user.tab();
    const dateSections = within(screen.getByRole('group', { name: 'Doğum tarihi' })).getAllByRole(
      'spinbutton',
    );
    expect(dateSections[0]).toHaveFocus();
    await user.keyboard('12041985');
    // Tarih alani tek sekme duragidir; ardindan takvim dugmesi gelir.
    await user.tab();
    expect(screen.getByRole('button', { name: 'Takvimden tarih seç' })).toHaveFocus();
    await user.tab();
    expect(screen.getByLabelText('Kurumsal e-posta adresi')).toHaveFocus();
    await user.keyboard('ahmet.yilmaz@duzen.com.tr{Enter}');

    // Adim degisince odak adim basligina tasinir; kanal Enter ile gonderilir.
    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Doğrulama yöntemi' })).toHaveFocus(),
    );
    await user.tab();
    expect(screen.getByRole('radio', { name: 'E-posta ile' })).toHaveFocus();
    await user.keyboard('{Enter}');

    await waitFor(() => expect(registrationApi.requestCode).toHaveBeenCalledWith('r-1', 'email'));
  });
});
