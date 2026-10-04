import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { permissions } from '@/shared/auth/permissions';
import { renderWithProviders } from '@/test/render';
import { setSession } from '@/test/session';
import { accountsApi, type AccountSummary } from '../../api/accountsApi';
import { invitationApi } from '../../api/invitationApi';
import { AccountsPage } from '../AccountsPage';

/** IK hesap islemleri (SYG-KMLK-057, 073). Veriler SENTETIKTIR. */
const ACTIVE: AccountSummary = {
  personId: 'p-1',
  firstName: 'Ayşe',
  lastName: 'Demir',
  employments: [{ registryCode: '00012', companyName: 'Düzen Laboratuvarlar', isActive: true }],
  state: 'active',
  statusReason: null,
  isCurrentUser: false,
};

const PASSIVE: AccountSummary = {
  personId: 'p-2',
  firstName: 'Mehmet',
  lastName: 'Kaya',
  employments: [{ registryCode: '00034', companyName: 'Düzen Laboratuvarlar', isActive: false }],
  state: 'passive',
  statusReason: 'employmentEnded',
  isCurrentUser: false,
};

const NONE: AccountSummary = {
  personId: 'p-3',
  firstName: 'Ali',
  lastName: 'Can',
  employments: [{ registryCode: '00056', companyName: 'Düzen Laboratuvarlar', isActive: true }],
  state: 'none',
  statusReason: null,
  isCurrentUser: false,
};

function page(items: AccountSummary[]) {
  return { items, page: 1, pageSize: 25, totalCount: items.length, totalPages: 1 };
}

describe('AccountsPage', () => {
  beforeEach(async () => {
    vi.restoreAllMocks();
    await setSession({
      status: 'authenticated',
      permissions: [permissions.accountView, permissions.accountUpdate, permissions.inviteCreate],
    });
    vi.spyOn(accountsApi, 'search').mockResolvedValue(page([ACTIVE, PASSIVE, NONE]));
  });

  it('kisileri yalnizca ad, sicil, firma ve hesap durumuyla listeler', async () => {
    renderWithProviders(<AccountsPage />);

    expect(await screen.findByText('Ayşe Demir')).toBeInTheDocument();
    expect(screen.getByText('00012 · Düzen Laboratuvarlar')).toBeInTheDocument();
    expect(screen.getByText('00034 · Düzen Laboratuvarlar (ayrıldı)')).toBeInTheDocument();
    expect(screen.getByText('Aktif')).toBeInTheDocument();
    expect(screen.getByText('Pasif')).toBeInTheDocument();
    expect(screen.getByText('Çalışma kaydı sona erdi')).toBeInTheDocument();
    expect(screen.getByText('Hesap yok')).toBeInTheDocument();
  });

  it('yazma durunca arar ve ilk sayfadan baslar', async () => {
    const search = vi.mocked(accountsApi.search);
    const user = userEvent.setup();
    renderWithProviders(<AccountsPage />);
    await screen.findByText('Ayşe Demir');

    await user.type(screen.getByLabelText('Sicil numarası, ad veya soyad'), 'demir');

    await waitFor(() =>
      expect(search).toHaveBeenLastCalledWith(expect.objectContaining({ q: 'demir', page: 1 })),
    );
  });

  it('durumuna gore dogru eylemi sunar; hesabi olmayana eylem yok', async () => {
    renderWithProviders(<AccountsPage />);

    const active = (await screen.findByText('Ayşe Demir')).closest('tr')!;
    const passive = screen.getByText('Mehmet Kaya').closest('tr')!;
    const none = screen.getByText('Ali Can').closest('tr')!;

    expect(within(active).getByRole('button', { name: 'Pasife al' })).toBeInTheDocument();
    expect(within(passive).getByRole('button', { name: 'Aktifleştir' })).toBeInTheDocument();
    expect(
      within(none).queryByRole('button', { name: /Pasife al|Aktifleştir/ }),
    ).not.toBeInTheDocument();
    expect(within(none).getByRole('button', { name: 'Bağlantı gönder' })).toBeInTheDocument();
    expect(
      within(passive).queryByRole('button', { name: 'Bağlantı gönder' }),
    ).not.toBeInTheDocument();
  });

  it('kendi hesabi icin pasife alma sunmaz', async () => {
    // #138: yonetici kendi hesabini pasife alip sistem disinda kalmamali.
    vi.mocked(accountsApi.search).mockResolvedValue(page([{ ...ACTIVE, isCurrentUser: true }]));
    renderWithProviders(<AccountsPage />);

    const own = (await screen.findByText('Ayşe Demir')).closest('tr')!;
    expect(within(own).queryByRole('button', { name: 'Pasife al' })).not.toBeInTheDocument();
    expect(within(own).getByText('Kendi hesabınız')).toBeInTheDocument();
  });

  it('pasife alma gerekce ister, gonderir ve sonucu bildirir', async () => {
    const deactivate = vi.spyOn(accountsApi, 'deactivate').mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderWithProviders(<AccountsPage />);

    const row = (await screen.findByText('Ayşe Demir')).closest('tr')!;
    await user.click(within(row).getByRole('button', { name: 'Pasife al' }));
    const dialog = await screen.findByRole('dialog', { name: 'Hesabı pasife al' });
    expect(within(dialog).getByText(/açık oturumları hemen kapatılacak/)).toBeInTheDocument();

    await user.click(within(dialog).getByRole('button', { name: 'Pasife al' }));
    expect(within(dialog).getByText('Gerekçe girin.')).toBeInTheDocument();
    expect(deactivate).not.toHaveBeenCalled();

    await user.type(within(dialog).getByLabelText(/Gerekçe/), '  Uzun süreli izin ');
    await user.click(within(dialog).getByRole('button', { name: 'Pasife al' }));

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Ayşe Demir adlı kişinin hesabı pasife alındı.',
    );
    expect(deactivate).toHaveBeenCalledWith('p-1', 'Uzun süreli izin');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('sunucunun reddini pencerede gosterir', async () => {
    vi.spyOn(accountsApi, 'activate').mockRejectedValue(
      new ApiError({
        message: 'Aktif çalışma kaydı olmayan kişinin hesabı aktifleştirilemez.',
        status: 422,
        isNetworkError: false,
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<AccountsPage />);

    const row = (await screen.findByText('Mehmet Kaya')).closest('tr')!;
    await user.click(within(row).getByRole('button', { name: 'Aktifleştir' }));
    const dialog = await screen.findByRole('dialog', { name: 'Hesabı aktifleştir' });
    await user.type(within(dialog).getByLabelText(/Gerekçe/), 'Yeniden işe başladı');
    await user.click(within(dialog).getByRole('button', { name: 'Aktifleştir' }));

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(
      'Aktif çalışma kaydı olmayan kişinin hesabı aktifleştirilemez.',
    );
  });

  it('baglanti gerekceyle gonderilir ve gecerlilik saati bildirilir (SYG-KMLK-051, 053)', async () => {
    const send = vi
      .spyOn(invitationApi, 'send')
      .mockResolvedValue({ expiresAt: new Date(2026, 8, 30, 15, 45).toISOString() });
    const user = userEvent.setup();
    renderWithProviders(<AccountsPage />);

    const row = (await screen.findByText('Ali Can')).closest('tr')!;
    await user.click(within(row).getByRole('button', { name: 'Bağlantı gönder' }));
    const dialog = await screen.findByRole('dialog', {
      name: 'Parola oluşturma bağlantısı gönder',
    });
    expect(
      within(dialog).getByText(/LOGO'da tanımlı kurumsal e-posta adresine/),
    ).toBeInTheDocument();

    await user.type(within(dialog).getByLabelText(/Gerekçe/), 'Telefonu yok');
    await user.click(within(dialog).getByRole('button', { name: 'Bağlantı gönder' }));

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Bağlantı saat 15:45 olana kadar geçerli.',
    );
    expect(send).toHaveBeenCalledWith('p-3', 'Telefonu yok');
  });

  it('davet izni yoksa baglanti dugmesi gorunmez', async () => {
    await setSession({
      status: 'authenticated',
      permissions: [permissions.accountView, permissions.accountUpdate],
    });
    renderWithProviders(<AccountsPage />);

    await screen.findByText('Ali Can');

    expect(screen.queryByRole('button', { name: 'Bağlantı gönder' })).not.toBeInTheDocument();
  });

  it('degistirme izni yoksa eylem dugmeleri gorunmez', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.accountView] });
    renderWithProviders(<AccountsPage />);

    await screen.findByText('Ayşe Demir');

    expect(screen.queryByRole('button', { name: 'Pasife al' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Aktifleştir' })).not.toBeInTheDocument();
  });

  it('goruntuleme izni yoksa listeyi istemez', async () => {
    await setSession({ status: 'authenticated', permissions: [] });
    renderWithProviders(<AccountsPage />);

    expect(await screen.findByText('Bu işlem için yetkiniz bulunmuyor.')).toBeInTheDocument();
    expect(accountsApi.search).not.toHaveBeenCalled();
  });

  it('sonuc yoksa ne yapilacagini soyler', async () => {
    vi.mocked(accountsApi.search).mockResolvedValue(page([]));
    renderWithProviders(<AccountsPage />);

    expect(await screen.findByText('Kayıt bulunamadı')).toBeInTheDocument();
  });
});
