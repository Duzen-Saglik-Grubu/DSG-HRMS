import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { renderWithProviders } from '@/test/render';
import { marriageApi, type Marriage } from '../../api/marriageApi';
import { MarriageActions } from '../MarriageActions';

/** Evlilik bilgisi (PG-PERS-19; SYG-PERS-036; KR-125). Veriler SENTETIKTIR. */
const EMPTY: Marriage = { marriageDate: null, allowanceReceived: null, editable: true };
const RECORDED: Marriage = { marriageDate: '2015-06-20', allowanceReceived: true, editable: true };

describe('MarriageActions', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('kayit yokken girer: iki alan da zorunludur', async () => {
    const set = vi
      .spyOn(marriageApi, 'set')
      .mockResolvedValue({ marriageDate: '2015-06-20', allowanceReceived: false, editable: true });
    const user = userEvent.setup();
    renderWithProviders(<MarriageActions personId="p-1" marriage={EMPTY} canEdit />);

    expect(screen.queryByRole('button', { name: /sil/ })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Evlilik bilgisi gir' }));
    const dialog = await screen.findByRole('dialog', { name: 'Evlilik bilgisi' });
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(set).not.toHaveBeenCalled();
    expect(within(dialog).getAllByText('Bu alanı doldurun.')).toHaveLength(2);

    const date = within(dialog).getByRole('group', { name: /Evlilik tarihi/ });
    await user.click(within(date).getAllByRole('spinbutton')[0]!);
    await user.keyboard('20062015');
    await user.click(within(dialog).getByRole('radio', { name: 'Almadı' }));
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(set).toHaveBeenCalledWith('p-1', '2015-06-20', false);
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('girilmis bilgiyi duzenler; sunucunun kural hatasini pencerede gosterir', async () => {
    vi.spyOn(marriageApi, 'set').mockRejectedValue(
      new ApiError({
        message: 'Evlilik tarihi doğum tarihinden önce olamaz.',
        status: 422,
        isNetworkError: false,
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<MarriageActions personId="p-1" marriage={RECORDED} canEdit />);

    await user.click(screen.getByRole('button', { name: 'Evlilik bilgisini düzenle' }));
    const dialog = await screen.findByRole('dialog', { name: 'Evlilik bilgisi' });
    expect(within(dialog).getByRole('group', { name: /Evlilik tarihi/ })).toHaveTextContent(
      '20.06.2015',
    );
    expect(within(dialog).getByRole('radio', { name: 'Aldı' })).toBeChecked();
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(marriageApi.set).toHaveBeenCalledWith('p-1', '2015-06-20', true);
    expect(
      await within(dialog).findByText('Evlilik tarihi doğum tarihinden önce olamaz.'),
    ).toBeInTheDocument();
  });

  it('onaydan sonra siler', async () => {
    const remove = vi.spyOn(marriageApi, 'remove').mockResolvedValue();
    const user = userEvent.setup();
    renderWithProviders(<MarriageActions personId="p-1" marriage={RECORDED} canEdit />);

    await user.click(screen.getByRole('button', { name: 'Evlilik bilgisini sil' }));
    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveTextContent('20.06.2015 tarihli evlilik bilgisi silinecek.');
    await user.click(within(dialog).getByRole('button', { name: 'Evlilik bilgisini sil' }));

    expect(remove).toHaveBeenCalledWith('p-1');
  });

  it('medeni hal Evli degilse duzenlenemez; izni olana nedeni yazar', () => {
    // KR-125: Bekar'a donerse bilgi korunur ama duzenlenemez.
    const locked: Marriage = { ...RECORDED, editable: false };
    const { unmount } = renderWithProviders(
      <MarriageActions personId="p-1" marriage={locked} canEdit />,
    );

    expect(screen.queryByRole('button')).not.toBeInTheDocument();
    expect(screen.getByText(/Medeni hâl Evli olmadığı için/)).toBeInTheDocument();
    unmount();

    renderWithProviders(<MarriageActions personId="p-1" marriage={locked} canEdit={false} />);
    expect(screen.queryByText(/Medeni hâl Evli olmadığı için/)).not.toBeInTheDocument();
  });
});
