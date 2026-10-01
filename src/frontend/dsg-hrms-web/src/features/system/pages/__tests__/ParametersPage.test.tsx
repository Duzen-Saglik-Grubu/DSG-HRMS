import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { permissions } from '@/shared/auth/permissions';
import { renderWithProviders } from '@/test/render';
import { setSession } from '@/test/session';
import { parametersApi, type Parameter } from '../../api/parametersApi';
import { ParametersPage } from '../ParametersPage';

/** En kucuk parametre ekrani (SYG-KMLK-076) ve kurumsal logo (PRM-GRN-01). Veriler SENTETIKTIR. */
const PARAMETERS: Parameter[] = [
  {
    key: 'PRM-KML-03',
    description: 'Hesap kilitlenmeden onceki basarisiz giris sayisi',
    type: 'number',
    min: 3,
    max: 10,
    value: '5',
    isSet: true,
    source: 'default',
  },
  {
    key: 'PRM-KML-08',
    description: 'Iki adimli dogrulama (2FA)',
    type: 'toggle',
    min: null,
    max: null,
    value: 'false',
    isSet: true,
    source: 'default',
  },
  {
    key: 'PRM-ENT-06',
    description: 'SMTP parolasi',
    type: 'secret',
    min: null,
    max: null,
    value: null,
    isSet: true,
    source: 'configuration',
  },
  {
    key: 'PRM-GRN-04',
    description: 'Destek iletisim bilgisi',
    type: 'text',
    min: null,
    max: null,
    value: 'Bilgi İşlem',
    isSet: true,
    source: 'database',
  },
];

function rowOf(label: string) {
  return screen.getByText(label).closest('div.MuiStack-root')!.parentElement!;
}

describe('ParametersPage', () => {
  beforeEach(async () => {
    vi.restoreAllMocks();
    await setSession({
      status: 'authenticated',
      permissions: [permissions.parameterView, permissions.parameterUpdate],
    });
    vi.spyOn(parametersApi, 'list').mockResolvedValue(PARAMETERS);
  });

  it('parametreleri Turkce adlari, kaynaklari ve gruplariyla listeler', async () => {
    renderWithProviders(<ParametersPage />);

    expect(
      await screen.findByText('Hesap kilitlenmeden önceki başarısız giriş sayısı'),
    ).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Kimlik ve oturum' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Entegrasyonlar' })).toBeInTheDocument();
    expect(screen.getByText('3 ile 10 arasında')).toBeInTheDocument();
    expect(screen.getByText('Bu ekrandan')).toBeInTheDocument();
  });

  it('gizli degeri gostermez; yalnizca tanimli oldugunu soyler ve yeni degeri yazdirir', async () => {
    const update = vi.spyOn(parametersApi, 'update').mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderWithProviders(<ParametersPage />);

    const field = await screen.findByLabelText('SMTP parolası');
    expect(field).toHaveAttribute('type', 'password');
    expect(field).toHaveValue('');
    expect(screen.getByText('Tanımlı')).toBeInTheDocument();

    await user.type(field, 'Yeni.Parola1');
    await user.click(within(rowOf('SMTP parolası')).getByRole('button', { name: 'Kaydet' }));

    expect(update).toHaveBeenCalledWith('PRM-ENT-06', 'Yeni.Parola1');
    expect(await screen.findByText('Kaydedildi.')).toBeInTheDocument();
    expect(field).toHaveValue('');
  });

  it('sunucunun dogrulama iletisini alanin altinda gosterir', async () => {
    vi.spyOn(parametersApi, 'update').mockRejectedValue(
      new ApiError({
        message: 'Değer 3–10 aralığında olmalıdır.',
        status: 422,
        isNetworkError: false,
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<ParametersPage />);

    const field = await screen.findByLabelText('Hesap kilitlenmeden önceki başarısız giriş sayısı');
    await user.clear(field);
    await user.type(field, '99');
    await user.click(
      within(rowOf('Hesap kilitlenmeden önceki başarısız giriş sayısı')).getByRole('button', {
        name: 'Kaydet',
      }),
    );

    expect(await screen.findByText('Değer 3–10 aralığında olmalıdır.')).toBeInTheDocument();
  });

  it('acik/kapali parametre dugmeyle hemen kaydedilir; uyari kapaliysa 2FA onaysiz acilir', async () => {
    vi.spyOn(parametersApi, 'twoFactorImpact').mockResolvedValue({
      affectedCount: 0,
      confirmationRequired: false,
    });
    const update = vi.spyOn(parametersApi, 'update').mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderWithProviders(<ParametersPage />);

    await user.click(await screen.findByLabelText('İki adımlı doğrulama (2FA)'));

    await waitFor(() => expect(update).toHaveBeenCalledWith('PRM-KML-08', 'true'));
  });

  it('2FA acilmadan once etkilenecek kisi sayisini gosterir ve onay ister (SYG-KMLK-035)', async () => {
    vi.spyOn(parametersApi, 'twoFactorImpact').mockResolvedValue({
      affectedCount: 2,
      confirmationRequired: true,
    });
    const update = vi.spyOn(parametersApi, 'update').mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderWithProviders(<ParametersPage />);

    await user.click(await screen.findByLabelText('İki adımlı doğrulama (2FA)'));
    const dialog = await screen.findByRole('dialog', { name: 'İki adımlı doğrulamayı aç' });
    expect(
      within(dialog).getByText(/2 aktif hesap sahibinin hiçbir doğrulama kanalı/),
    ).toBeInTheDocument();
    expect(update).not.toHaveBeenCalled();

    await user.click(within(dialog).getByRole('button', { name: 'Vazgeç' }));
    expect(update).not.toHaveBeenCalled();
    expect(screen.getByLabelText('İki adımlı doğrulama (2FA)')).not.toBeChecked();

    await user.click(screen.getByLabelText('İki adımlı doğrulama (2FA)'));
    await user.click(
      within(await screen.findByRole('dialog', { name: 'İki adımlı doğrulamayı aç' })).getByRole(
        'button',
        {
          name: 'Aç',
        },
      ),
    );

    expect(update).toHaveBeenCalledWith('PRM-KML-08', 'true', true);
  });

  it('degistirme izni yoksa alanlar kapali ve kaydet dugmesi yok', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.parameterView] });
    renderWithProviders(<ParametersPage />);

    expect(
      await screen.findByLabelText('Destek iletişim bilgisi (üyelik ve giriş ekranları)'),
    ).toBeDisabled();
    expect(screen.queryByRole('button', { name: 'Kaydet' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Logo yükle' })).not.toBeInTheDocument();
  });

  it('goruntuleme izni yoksa listeyi istemez', async () => {
    await setSession({ status: 'authenticated', permissions: [] });
    renderWithProviders(<ParametersPage />);

    expect(await screen.findByText('Bu işlem için yetkiniz bulunmuyor.')).toBeInTheDocument();
    expect(parametersApi.list).not.toHaveBeenCalled();
  });

  it('logo yukler ve sunucunun reddini gosterir', async () => {
    const upload = vi
      .spyOn(parametersApi, 'uploadLogo')
      .mockResolvedValueOnce(undefined)
      .mockRejectedValueOnce(
        new ApiError({
          message: 'Logo PNG veya JPEG biçiminde olmalıdır.',
          status: 422,
          isNetworkError: false,
        }),
      );
    const user = userEvent.setup();
    renderWithProviders(<ParametersPage />);

    const input = await screen.findByLabelText('Logo yükle', { selector: 'input' });
    await user.upload(
      input,
      new File([new Uint8Array([0x89, 0x50])], 'logo.png', { type: 'image/png' }),
    );
    expect(await screen.findByText('Logo yüklendi.')).toBeInTheDocument();

    await user.upload(input, new File([new Uint8Array([1])], 'logo.jpg', { type: 'image/jpeg' }));
    expect(await screen.findByText('Logo PNG veya JPEG biçiminde olmalıdır.')).toBeInTheDocument();
    expect(upload).toHaveBeenCalledTimes(2);
  });
});
