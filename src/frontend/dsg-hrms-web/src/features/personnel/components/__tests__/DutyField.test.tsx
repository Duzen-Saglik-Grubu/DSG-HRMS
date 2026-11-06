import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { format } from 'date-fns';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { renderWithProviders } from '@/test/render';
import { dutiesApi, type Duty } from '../../api/dutiesApi';
import { DutyField } from '../DutyField';

/** Kartin "Gorevi" alani (PG-PERS-43; SYG-PERS-042). Veriler SENTETIKTIR. */
const BIOLOGIST: Duty = { id: 'd-1', name: 'Biyolog', validFrom: '2021-03-01', validTo: null };

const OPTIONS = [
  { id: 'g-1', name: 'Biyolog' },
  { id: 'g-2', name: 'Hemşire' },
];

describe('DutyField', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.spyOn(dutiesApi, 'options').mockResolvedValue(OPTIONS);
  });

  async function openAssign(user: ReturnType<typeof userEvent.setup>) {
    await user.click(screen.getByRole('button', { name: '00012 siciline görev ata' }));
    return screen.findByRole('dialog', { name: 'Görev ata' });
  }

  async function chooseDuty(
    user: ReturnType<typeof userEvent.setup>,
    dialog: HTMLElement,
    name: string,
  ) {
    await user.click(within(dialog).getByRole('combobox', { name: /Görevi/ }));
    await user.click(await screen.findByRole('option', { name }));
  }

  it('aktif gorev tanimini bugunden baslatarak atar', async () => {
    // Baslangic varsayilan olarak bugundur (SYG-PERS-042).
    const assign = vi
      .spyOn(dutiesApi, 'assign')
      .mockResolvedValue({ id: 'd-2', name: 'Hemşire', validFrom: '2026-11-06', validTo: null });
    const user = userEvent.setup();
    renderWithProviders(<DutyField personId="p-1" registryCode="00012" duty={BIOLOGIST} />);

    expect(screen.getByText('Biyolog')).toBeInTheDocument();
    const dialog = await openAssign(user);
    expect(dialog).toHaveTextContent('Önceki açık görev, yeni görevin başlangıç tarihiyle kapanır');
    await chooseDuty(user, dialog, 'Hemşire');
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(assign).toHaveBeenCalledWith('p-1', '00012', 'g-2', format(new Date(), 'yyyy-MM-dd'));
    expect(dutiesApi.options).toHaveBeenCalled();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('gecmis bir tarihten baslatilabilir', async () => {
    const assign = vi
      .spyOn(dutiesApi, 'assign')
      .mockResolvedValue({ id: 'd-2', name: 'Hemşire', validFrom: '2024-01-15', validTo: null });
    const user = userEvent.setup();
    renderWithProviders(<DutyField personId="p-1" registryCode="00012" duty={null} />);

    const dialog = await openAssign(user);
    await chooseDuty(user, dialog, 'Hemşire');
    const date = within(dialog).getByRole('group', { name: /Başlangıç tarihi/ });
    await user.click(within(date).getAllByRole('spinbutton')[0]!);
    await user.keyboard('15012024');
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(assign).toHaveBeenCalledWith('p-1', '00012', 'g-2', '2024-01-15');
  });

  it('gorev secilmeden kaydetmez', async () => {
    const assign = vi.spyOn(dutiesApi, 'assign');
    const user = userEvent.setup();
    renderWithProviders(<DutyField personId="p-1" registryCode="00012" duty={null} />);

    const dialog = await openAssign(user);
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(assign).not.toHaveBeenCalled();
    expect(within(dialog).getByText('Bu alanı doldurun.')).toBeInTheDocument();
  });

  it('sunucunun tarih kurali hatasini pencerede gosterir', async () => {
    // SYG-PERS-019: cakisan donem 409; istihdam araligi disi 422 (AN-16).
    vi.spyOn(dutiesApi, 'assign').mockRejectedValue(
      new ApiError({
        message: 'Görev, aynı istihdamdaki başka bir görev dönemiyle çakışıyor.',
        status: 409,
        isNetworkError: false,
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<DutyField personId="p-1" registryCode="00012" duty={BIOLOGIST} />);

    const dialog = await openAssign(user);
    await chooseDuty(user, dialog, 'Hemşire');
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(
      await within(dialog).findByText(
        'Görev, aynı istihdamdaki başka bir görev dönemiyle çakışıyor.',
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('guncel gorevi onaydan sonra siler', async () => {
    const remove = vi.spyOn(dutiesApi, 'remove').mockResolvedValue();
    const user = userEvent.setup();
    renderWithProviders(<DutyField personId="p-1" registryCode="00012" duty={BIOLOGIST} />);

    await user.click(screen.getByRole('button', { name: 'Biyolog görevini sil' }));
    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveTextContent('“Biyolog” görevi (01.03.2021 başlangıçlı) silinecek.');
    expect(dialog).toHaveTextContent('önceki görev yeniden açılmaz');

    await user.click(within(dialog).getByRole('button', { name: 'Görevi sil' }));

    expect(remove).toHaveBeenCalledWith('p-1', '00012', 'd-1');
  });

  it('gorev girilmemisse bos isareti ve yalnizca atama dugmesi vardir', () => {
    renderWithProviders(<DutyField personId="p-1" registryCode="00012" duty={null} />);

    expect(screen.getByText('—')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: '00012 siciline görev ata' })).toBeVisible();
    expect(screen.queryByRole('button', { name: /görevini sil/ })).not.toBeInTheDocument();
  });
});
