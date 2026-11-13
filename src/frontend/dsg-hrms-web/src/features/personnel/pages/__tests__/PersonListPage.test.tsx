import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useNavigate } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { permissions } from '@/shared/auth/permissions';
import { renderWithProviders } from '@/test/render';
import { setSession } from '@/test/session';
import { personsApi, type PersonFilterOptions, type PersonListItem } from '../../api/personsApi';
import { personPhotosApi } from '@/shared/api/personPhotos';
import { PersonListPage } from '../PersonListPage';

/** Personel listesi ekrani (PG-PERS-13–18). Veriler SENTETIKTIR. */
const AYSE: PersonListItem = {
  personId: 'p-1',
  firstName: 'AYŞE',
  lastName: 'DEMİR',
  isActive: true,
  employmentCount: 2,
  registryCode: '00012',
  companyName: 'Düzen Laboratuvarlar',
  branchName: 'Merkez',
  departmentName: 'Bilgi İşlem',
  titleName: 'Uzman',
  dutyName: 'Biyolog',
  hasPhoto: false,
};

const MEHMET: PersonListItem = {
  personId: 'p-2',
  firstName: 'MEHMET',
  lastName: 'KAYA',
  isActive: false,
  employmentCount: 1,
  registryCode: '00034',
  companyName: 'Düzen Laboratuvarlar',
  branchName: 'Bornova',
  departmentName: null,
  titleName: null,
  dutyName: null,
  hasPhoto: false,
};

function options(overrides: Partial<PersonFilterOptions> = {}): PersonFilterOptions {
  return {
    companies: [],
    cities: [],
    branches: [
      { value: 'b-1', label: 'Merkez' },
      { value: 'b-2', label: 'Bornova' },
    ],
    departments: [],
    titles: [],
    duties: [],
    statuses: [],
    occupations: [],
    educations: [],
    canFilterByNationalId: false,
    canFilterBySensitive: false,
    defaultPageSize: 24,
    maxPageSize: 100,
    ...overrides,
  };
}

function lastQuery(): URLSearchParams {
  const calls = vi.mocked(personsApi.list).mock.calls;
  return calls[calls.length - 1]?.[0] ?? new URLSearchParams();
}

describe('PersonListPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  beforeEach(async () => {
    vi.restoreAllMocks();
    vi.spyOn(personsApi, 'syncFreshness').mockResolvedValue({
      enabled: true,
      lastSucceededAt: '2026-11-04T06:15:00Z',
      lastRunFailed: false,
    });
    await setSession({ status: 'authenticated', permissions: [permissions.personView] });
    vi.spyOn(personsApi, 'list').mockResolvedValue({
      items: [AYSE, MEHMET],
      page: 1,
      pageSize: 24,
      totalCount: 2,
      totalPages: 1,
    });
    vi.spyOn(personsApi, 'filterOptions').mockResolvedValue(options());
  });

  it('kart izgarasiyla ve yalnizca aktif personel istegiyle acilir', async () => {
    renderWithProviders(<PersonListPage />);

    const card = await screen.findByRole('article', { name: 'AYŞE DEMİR' });
    expect(within(card).getByText('(00012)')).toBeInTheDocument();
    expect(within(card).getByText('Merkez / Bilgi İşlem')).toBeInTheDocument();
    expect(within(card).getByText('Uzman / Biyolog')).toBeInTheDocument();
    expect(within(card).getByText('2 sicil')).toBeInTheDocument();
    // Kart, kisinin kartina goturur (#311).
    expect(card.closest('a')).toHaveAttribute('href', '/personnel/p-1');
    expect(
      within(screen.getByRole('article', { name: 'MEHMET KAYA' })).getByText('Ayrıldı'),
    ).toBeInTheDocument();
    // Durum gonderilmez: sunucunun varsayilani Aktif (SYG-PERS-027).
    expect(lastQuery().has('status')).toBe(false);
    expect(screen.getByRole('button', { name: 'Aktif' })).toHaveAttribute('aria-pressed', 'true');
  });

  it('fotografi olan kiside fotograf, olmayanda bas harfler gorunur (SYG-PERS-040)', async () => {
    vi.spyOn(personsApi, 'list').mockResolvedValue({
      items: [{ ...AYSE, hasPhoto: true }, MEHMET],
      page: 1,
      pageSize: 24,
      totalCount: 2,
      totalPages: 1,
    });
    const photo = vi
      .spyOn(personPhotosApi, 'photo')
      .mockResolvedValue(new Blob(['jpeg'], { type: 'image/jpeg' }));
    renderWithProviders(<PersonListPage />);

    const ayse = await screen.findByRole('article', { name: 'AYŞE DEMİR' });
    await waitFor(() =>
      expect(within(ayse).getByRole('img', { name: 'AYŞE DEMİR fotoğrafı' })).toHaveAttribute(
        'src',
        'data:image/jpeg;base64,anBlZw==',
      ),
    );
    const mehmet = screen.getByRole('article', { name: 'MEHMET KAYA' });
    expect(within(mehmet).getByRole('img', { name: 'MEHMET KAYA fotoğrafı' })).toHaveTextContent(
      'MK',
    );
    // Fotografi olmayan kisi icin istek yapilmaz.
    expect(photo).toHaveBeenCalledTimes(1);
    expect(photo).toHaveBeenCalledWith('p-1');
  });

  it('gorunum degistirici ayni suzgec ve sayfayla tabloya gecer ve geri doner', async () => {
    const user = userEvent.setup();
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });
    await user.click(screen.getByRole('button', { name: 'Ayrılan' }));
    await waitFor(() => expect(lastQuery().get('status')).toBe('terminated'));
    const calls = vi.mocked(personsApi.list).mock.calls.length;

    await user.click(screen.getByRole('button', { name: 'Tablo görünümü' }));

    const table = await screen.findByRole('table');
    expect(within(table).getByText('AYŞE')).toBeInTheDocument();
    expect(within(table).getByText('Merkez / Bilgi İşlem')).toBeInTheDocument();
    // Gorunum degisikligi yeni istek uretmez: suzgec ve sayfa aynidir.
    expect(vi.mocked(personsApi.list).mock.calls.length).toBe(calls);

    await user.click(screen.getByRole('button', { name: 'Kart görünümü' }));
    expect(await screen.findByRole('article', { name: 'AYŞE DEMİR' })).toBeInTheDocument();
    expect(lastQuery().get('status')).toBe('terminated');
  });

  it('telefonda tablo adresi de kart izgarasiyla acilir; gorunum secimi sunulmaz (SYG-PERS-073)', async () => {
    // Telefon genisligi: MUI'nin `down('sm')` sorgusu (max-width) eslesir.
    vi.stubGlobal(
      'matchMedia',
      (query: string) =>
        ({
          matches: query.includes('max-width'),
          media: query,
          onchange: null,
          addEventListener: () => undefined,
          removeEventListener: () => undefined,
          addListener: () => undefined,
          removeListener: () => undefined,
          dispatchEvent: () => false,
        }) as MediaQueryList,
    );
    function TableLink() {
      const navigate = useNavigate();
      return <button onClick={() => void navigate('/?view=table')}>Tablo adresi</button>;
    }
    renderWithProviders(
      <>
        <TableLink />
        <PersonListPage />
      </>,
    );
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });

    fireEvent.click(screen.getByRole('button', { name: 'Tablo adresi' }));

    expect(await screen.findByRole('article', { name: 'MEHMET KAYA' })).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Tablo görünümü' })).not.toBeInTheDocument();
  });

  it('disa aktarma dugmesi yalnizca izni olana gorunur ve diyalogu acar (SYG-PERS-032)', async () => {
    vi.spyOn(personsApi, 'exportFields').mockResolvedValue([
      { field: 'registryCode', label: 'Sicil numarası', pdfOnly: false },
    ]);
    const { unmount } = renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });
    expect(screen.queryByRole('button', { name: 'Dışa aktar' })).not.toBeInTheDocument();
    unmount();

    await setSession({
      status: 'authenticated',
      permissions: [permissions.personView, permissions.personExport],
    });
    const user = userEvent.setup();
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });
    await user.click(screen.getByRole('button', { name: 'Dışa aktar' }));

    const dialog = await screen.findByRole('dialog', { name: 'Personel listesini dışa aktar' });
    expect(within(dialog).getByText('Süzgece uyan 2 kişi aktarılacak.')).toBeInTheDocument();
  });

  it('basliktaki son senkronizasyon bilgisini gosterir (SYG-PERS-010)', async () => {
    renderWithProviders(<PersonListPage />);

    expect(await screen.findByText(/Son senkronizasyon/)).toBeInTheDocument();
  });

  it('yazma durunca hizli arama yapar', async () => {
    const user = userEvent.setup();
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });

    await user.type(screen.getByLabelText('Ad, soyad veya sicil'), 'isik');

    await waitFor(() => expect(lastQuery().get('q')).toBe('isik'));
  });

  it('ust cubuktan gelen arama korunur ve kutuya yazilir', async () => {
    // Ust cubuk ayni sayfadayken adresi degistirir; sayfa eski terimi geri yazmamali.
    function TopBarSearch() {
      const navigate = useNavigate();
      return <button onClick={() => void navigate('/?q=kaya')}>Üst çubukta ara</button>;
    }
    const user = userEvent.setup();
    renderWithProviders(
      <>
        <TopBarSearch />
        <PersonListPage />
      </>,
    );
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });

    await user.click(screen.getByRole('button', { name: 'Üst çubukta ara' }));

    await waitFor(() => expect(screen.getByLabelText('Ad, soyad veya sicil')).toHaveValue('kaya'));
    await new Promise((resolve) => setTimeout(resolve, 600));
    expect(lastQuery().get('q')).toBe('kaya');
  });

  it('secilen suzgec etiket olarak gorunur; Temizle hepsini kaldirir', async () => {
    const user = userEvent.setup();
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });

    await user.click(screen.getByRole('button', { name: /Süzgeçler/ }));
    const panel = await screen.findByRole('dialog', { name: 'Süzgeçler' });
    await user.click(within(panel).getByLabelText('Şube'));
    await user.click(await screen.findByRole('option', { name: 'Merkez' }));
    // Liste secimden sonra acik kalir; ikinci deger yeniden acmadan secilir (#306).
    await user.click(screen.getByRole('option', { name: 'Bornova' }));
    expect(
      within(screen.getByRole('option', { name: 'Merkez' })).getByRole('checkbox'),
    ).toBeChecked();
    await user.keyboard('{Escape}');
    await user.click(within(panel).getByRole('button', { name: 'Uygula' }));

    await waitFor(() => expect(lastQuery().getAll('branchId')).toEqual(['b-1', 'b-2']));
    const tags = await screen.findByRole('group', { name: 'Seçili süzgeçler' });
    expect(within(tags).getByText('Şube: Merkez')).toBeInTheDocument();
    expect(within(tags).getByText('Şube: Bornova')).toBeInTheDocument();

    await user.click(within(tags).getByLabelText('Şube: Merkez süzgecini kaldır'));
    await waitFor(() => expect(lastQuery().getAll('branchId')).toEqual(['b-2']));

    await user.click(screen.getByRole('button', { name: 'Temizle' }));
    await waitFor(() => expect(lastQuery().has('branchId')).toBe(false));
    expect(screen.queryByRole('group', { name: 'Seçili süzgeçler' })).not.toBeInTheDocument();
  });

  it('adres ve TCKN suzgeci yalnizca izni olana sunulur', async () => {
    const user = userEvent.setup();
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });

    await user.click(screen.getByRole('button', { name: /Süzgeçler/ }));
    let panel = await screen.findByRole('dialog', { name: 'Süzgeçler' });
    expect(within(panel).getByLabelText('Soyad')).toBeInTheDocument();
    expect(within(panel).queryByLabelText('Adres')).not.toBeInTheDocument();
    expect(within(panel).queryByLabelText('TCKN')).not.toBeInTheDocument();
    await user.click(within(panel).getAllByRole('button', { name: 'Kapat' })[0]!);

    vi.mocked(personsApi.filterOptions).mockResolvedValue(
      options({ canFilterBySensitive: true, canFilterByNationalId: true }),
    );
    renderWithProviders(<PersonListPage />);
    await waitFor(() =>
      expect(screen.getByLabelText('Ad, soyad, sicil veya TCKN')).toBeInTheDocument(),
    );
    await user.click(screen.getAllByRole('button', { name: /Süzgeçler/ }).at(-1)!);
    panel = await screen.findByRole('dialog', { name: 'Süzgeçler' });
    expect(within(panel).getByLabelText('Adres')).toBeInTheDocument();
    expect(within(panel).getByLabelText('TCKN')).toBeInTheDocument();
  });

  it('kan grubu, evlilik ve acil durum suzgecleri izni olmayana sunulmaz; fotograf herkese (SYG-PERS-026)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });

    await user.click(screen.getByRole('button', { name: /Süzgeçler/ }));
    const panel = await screen.findByRole('dialog', { name: 'Süzgeçler' });
    expect(within(panel).getByLabelText('Fotoğraf')).toBeInTheDocument();
    for (const label of ['Kan grubu', 'Evlilik yardımı', 'Acil durum kişisi']) {
      expect(within(panel).queryByLabelText(label)).not.toBeInTheDocument();
    }
    expect(within(panel).queryByText('Evlilik tarihi')).not.toBeInTheDocument();
  });

  it('izni olan kan grubu, evlilik yardimi ve acil durum kisisiyle suzer; secimler etiket olur (SYG-PERS-024)', async () => {
    const user = userEvent.setup();

    vi.mocked(personsApi.filterOptions).mockResolvedValue(
      options({
        canFilterBySensitive: true,
        bloodGroups: [
          { value: '1', label: '0 Rh+' },
          { value: '3', label: 'A Rh+' },
        ],
      }),
    );
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });
    await waitFor(() => expect(personsApi.filterOptions).toHaveBeenCalled());
    await user.click(screen.getByRole('button', { name: /Süzgeçler/ }));
    const panel = await screen.findByRole('dialog', { name: 'Süzgeçler' });
    expect(within(panel).getByText('Evlilik tarihi')).toBeInTheDocument();

    await user.click(within(panel).getByLabelText('Kan grubu'));
    await user.click(await screen.findByRole('option', { name: 'A Rh+' }));
    await user.keyboard('{Escape}');
    await user.click(within(panel).getByLabelText('Evlilik yardımı'));
    await user.click(await screen.findByRole('option', { name: 'Aldı' }));
    await user.keyboard('{Escape}');
    await user.click(within(panel).getByLabelText('Acil durum kişisi'));
    await user.click(await screen.findByRole('option', { name: 'Yok' }));
    await user.click(within(panel).getByRole('button', { name: 'Uygula' }));

    await waitFor(() => expect(lastQuery().getAll('bloodGroup')).toEqual(['3']));
    expect(lastQuery().getAll('marriageAllowance')).toEqual(['received']);
    expect(lastQuery().get('hasEmergencyContact')).toBe('false');
    const tags = await screen.findByRole('group', { name: 'Seçili süzgeçler' });
    expect(within(tags).getByText('Kan grubu: A Rh+')).toBeInTheDocument();
    expect(within(tags).getByText('Acil durum kişisi: Yok')).toBeInTheDocument();
    // Uc acilir menuden secim yapar; CI kapsam olcumuyle yerelin birkac kati yavas (#376).
  }, 30_000);

  it('T1 suzgec panelinde "Yoneticisi" suzgeci yoktur; butun izinler acik olsa da (SYG-PERS-056)', async () => {
    const user = userEvent.setup();
    // Yonetici iliskisi T2 Organizasyon Yonetimi ile gelir (AN-12); o zamana kadar suzgec sunulmaz.
    vi.mocked(personsApi.filterOptions).mockResolvedValue(
      options({ canFilterByNationalId: true, canFilterBySensitive: true }),
    );
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });
    await waitFor(() => expect(personsApi.filterOptions).toHaveBeenCalled());
    await user.click(screen.getByRole('button', { name: /Süzgeçler/ }));
    const panel = await screen.findByRole('dialog', { name: 'Süzgeçler' });

    expect(within(panel).getByLabelText('Kan grubu')).toBeInTheDocument();
    expect(within(panel).queryByLabelText(/yönetici/i)).not.toBeInTheDocument();
    expect(within(panel).queryByText(/yönetici/i)).not.toBeInTheDocument();
  });

  it('suzgec uygulandiktan hemen sonra Temizle kalici olur (#373)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });

    await user.click(screen.getByRole('button', { name: /Süzgeçler/ }));
    const panel = await screen.findByRole('dialog', { name: 'Süzgeçler' });
    await user.click(within(panel).getByLabelText('Fotoğraf'));
    await user.click(await screen.findByRole('option', { name: 'Var' }));
    // Uygula ve Temizle beklemesiz art arda: kullanici arama beklemesi (300 ms) dolmadan temizler.
    // Not: hatanin kendisi React Router'in tarayicida cizimi ertelemesine bagli; bellekteki yonlendiricide
    // yeniden uretilemez. Asil kanit e2e senaryo 14; bu test davranisin kendisini korur.
    fireEvent.click(within(panel).getByRole('button', { name: 'Uygula' }));
    const tags = await screen.findByRole('group', { name: 'Seçili süzgeçler' }, { interval: 10 });
    fireEvent.click(within(tags).getByRole('button', { name: 'Temizle' }));

    // Arama kutusunun beklemesi (300 ms) gectikten sonra da suzgec geri gelmez.
    await new Promise((resolve) => setTimeout(resolve, 700));
    expect(screen.queryByRole('group', { name: 'Seçili süzgeçler' })).not.toBeInTheDocument();
    expect(lastQuery().has('hasPhoto')).toBe(false);
  });

  it('sayfa boyutu secenekleri parametredeki en buyugu asmaz', async () => {
    vi.mocked(personsApi.filterOptions).mockResolvedValue(options({ maxPageSize: 50 }));
    const user = userEvent.setup();
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });
    await waitFor(() => expect(personsApi.filterOptions).toHaveBeenCalled());

    await user.click(screen.getByRole('combobox', { name: /Sayfa başına kişi/ }));

    const choices = (await screen.findAllByRole('option')).map((option) => option.textContent);
    expect(choices).toEqual(['24', '48']);
  });
});
