import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { permissions } from '@/shared/auth/permissions';
import { renderWithProviders } from '@/test/render';
import { setSession } from '@/test/session';
import { referenceApi, type ReferenceDefinition, type ReferenceType } from '../../api/referenceApi';
import { ReferenceDataPage } from '../ReferenceDataPage';

/** Referans veri ekrani (SYG-PERS-072; KR-129, KR-135). Tanimlar SENTETIKTIR. */
const definition = (
  type: ReferenceType,
  code: string,
  name: string,
  extra: Partial<ReferenceDefinition> = {},
): ReferenceDefinition => ({
  id: `${type}-${code}`,
  type,
  code,
  name,
  sortOrder: 1,
  isActive: true,
  isSystem: false,
  ...extra,
});

const DATA: Record<ReferenceType, ReferenceDefinition[]> = {
  title: [definition('title', '1', 'Birim Operatörü')],
  duty: [definition('duty', 'G001', 'Hemşire')],
  relationship: [
    definition('relationship', 'Y001', 'Eşi'),
    definition('relationship', 'Y005', 'Amcası', { isActive: false }),
  ],
  bloodGroup: [definition('bloodGroup', '1', '0 Rh+', { isSystem: true })],
};

async function openTab(user: ReturnType<typeof userEvent.setup>, name: string) {
  await user.click(screen.getByRole('tab', { name }));
}

describe('ReferenceDataPage', () => {
  beforeEach(async () => {
    vi.restoreAllMocks();
    await setSession({
      status: 'authenticated',
      permissions: [permissions.referenceView, permissions.referenceUpdate],
    });
    vi.spyOn(referenceApi, 'list').mockImplementation((type) => Promise.resolve(DATA[type]));
  });

  it('turleri sekmelerde, tanimlari pasifleriyle listeler', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ReferenceDataPage />);

    expect(await screen.findByText('Birim Operatörü')).toBeInTheDocument();
    expect(referenceApi.list).toHaveBeenCalledWith('title');

    await openTab(user, 'Yakınlık');
    const amca = (await screen.findByText('Amcası')).closest('tr')!;
    expect(within(amca).getByText('Pasif')).toBeInTheDocument();
    // Pasif tanim yeniden pasife alinamaz; adi degistirilebilir.
    expect(within(amca).queryByRole('button', { name: /pasife al/ })).not.toBeInTheDocument();
    expect(
      within(amca).getByRole('button', { name: 'Amcası tanımının adını değiştir' }),
    ).toBeVisible();
    expect(within(screen.getByText('Eşi').closest('tr')!).getByText('Aktif')).toBeInTheDocument();
  });

  it('yeni yakinlik ekler; kod bos birakilirsa sunucuya bos gider', async () => {
    const create = vi
      .spyOn(referenceApi, 'create')
      .mockResolvedValue(definition('relationship', 'Y006', 'Teyzesi'));
    const user = userEvent.setup();
    renderWithProviders(<ReferenceDataPage />);

    await openTab(user, 'Yakınlık');
    await user.click(await screen.findByRole('button', { name: 'Yeni yakınlık' }));
    const dialog = await screen.findByRole('dialog', { name: 'Yeni yakınlık tanımı' });
    await user.type(within(dialog).getByRole('textbox', { name: /^Ad/ }), 'Teyzesi');
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(create).toHaveBeenCalledWith('relationship', '', 'Teyzesi');
    expect(await screen.findByText('“Teyzesi” eklendi (kod Y006).')).toBeInTheDocument();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('unvanda kod zorunludur; kullanilan kod hatasi pencerede gorunur', async () => {
    // Unvan kodu LOGO kodudur; sunucu uretmez (ADR-0022 §1).
    const create = vi.spyOn(referenceApi, 'create').mockRejectedValueOnce(
      new ApiError({
        message: 'Bu kod zaten kullanılıyor: 1.',
        status: 422,
        isNetworkError: false,
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<ReferenceDataPage />);

    await user.click(await screen.findByRole('button', { name: 'Yeni ünvan' }));
    const dialog = await screen.findByRole('dialog');
    await user.type(within(dialog).getByRole('textbox', { name: /^Ad/ }), 'Başhekim');
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(create).not.toHaveBeenCalled();
    expect(within(dialog).getByText('Bu alanı doldurun.')).toBeInTheDocument();

    await user.type(within(dialog).getByRole('textbox', { name: /^Kod/ }), '1');
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(create).toHaveBeenCalledWith('title', '1', 'Başhekim');
    expect(await within(dialog).findByText('Bu kod zaten kullanılıyor: 1.')).toBeInTheDocument();
  });

  it('sunucunun ad dogrulama hatasini alanin altinda gosterir', async () => {
    vi.spyOn(referenceApi, 'rename').mockRejectedValue(
      new ApiError({
        message: 'Gönderilen veri geçerli değil.',
        status: 400,
        isNetworkError: false,
        errors: { name: ['Ad en fazla 200 karakter olabilir.'] },
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<ReferenceDataPage />);

    await openTab(user, 'Görev');
    await user.click(
      await screen.findByRole('button', { name: 'Hemşire tanımının adını değiştir' }),
    );
    const dialog = await screen.findByRole('dialog', { name: 'Tanımın adını değiştir' });
    expect(within(dialog).getByText(/Kod \(G001\) değişmez/)).toBeInTheDocument();
    expect(within(dialog).queryByRole('textbox', { name: /^Kod/ })).not.toBeInTheDocument();

    const name = within(dialog).getByRole('textbox', { name: /^Ad/ });
    await user.clear(name);
    await user.type(name, 'Sorumlu Hemşire');
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(referenceApi.rename).toHaveBeenCalledWith('duty-G001', 'Sorumlu Hemşire');
    expect(
      await within(dialog).findByText('Ad en fazla 200 karakter olabilir.'),
    ).toBeInTheDocument();
  });

  it('pasife almadan once neyin etkilenecegini sorar', async () => {
    const deactivate = vi.spyOn(referenceApi, 'deactivate').mockResolvedValue();
    const user = userEvent.setup();
    renderWithProviders(<ReferenceDataPage />);

    await openTab(user, 'Yakınlık');
    await user.click(await screen.findByRole('button', { name: 'Eşi tanımını pasife al' }));
    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveTextContent('“Eşi” tanımı pasife alınacak.');

    await user.click(within(dialog).getByRole('button', { name: 'Pasife al' }));

    expect(deactivate).toHaveBeenCalledWith('relationship-Y001');
    expect(await screen.findByText('“Eşi” pasife alındı.')).toBeInTheDocument();
  });

  it('kan grubu sistem tanimidir; degistirme dugmesi yoktur', async () => {
    // KR-129: kan grubu adlari ekrandan degistirilemez.
    const user = userEvent.setup();
    renderWithProviders(<ReferenceDataPage />);

    await openTab(user, 'Kan grubu');

    expect(await screen.findByText('0 Rh+')).toBeInTheDocument();
    expect(screen.getByText(/ekrandan değiştirilemez/)).toBeInTheDocument();
    expect(within(screen.getByRole('tabpanel')).queryByRole('button')).not.toBeInTheDocument();
  });

  it('yalnizca gorme izni olan kullanici listeyi gorur, degistiremez', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.referenceView] });
    renderWithProviders(<ReferenceDataPage />);

    expect(await screen.findByText('Birim Operatörü')).toBeInTheDocument();
    expect(within(screen.getByRole('tabpanel')).queryByRole('button')).not.toBeInTheDocument();
  });

  it('gorme izni yoksa listeyi istemez', async () => {
    await setSession({ status: 'authenticated', permissions: [] });
    renderWithProviders(<ReferenceDataPage />);

    expect(screen.getByText('Bu işlem için yetkiniz bulunmuyor.')).toBeInTheDocument();
    expect(referenceApi.list).not.toHaveBeenCalled();
  });
});
