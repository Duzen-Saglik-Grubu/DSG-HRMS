import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { permissions } from '@/shared/auth/permissions';
import { renderWithProviders } from '@/test/render';
import { setSession } from '@/test/session';
import { personsApi, type PersonFilterOptions, type PersonListItem } from '../../api/personsApi';
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
  beforeEach(async () => {
    vi.restoreAllMocks();
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
    expect(within(card).getByText('00012')).toBeInTheDocument();
    expect(within(card).getByText('Merkez / Bilgi İşlem')).toBeInTheDocument();
    expect(within(card).getByText('Uzman / Biyolog')).toBeInTheDocument();
    expect(within(card).getByText('2 sicil')).toBeInTheDocument();
    expect(
      within(screen.getByRole('article', { name: 'MEHMET KAYA' })).getByText('Ayrıldı'),
    ).toBeInTheDocument();
    // Durum gonderilmez: sunucunun varsayilani Aktif (SYG-PERS-027).
    expect(lastQuery().has('status')).toBe(false);
    expect(screen.getByRole('button', { name: 'Aktif' })).toHaveAttribute('aria-pressed', 'true');
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

  it('yazma durunca hizli arama yapar', async () => {
    const user = userEvent.setup();
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });

    await user.type(screen.getByLabelText('Ad, soyad veya sicil'), 'isik');

    await waitFor(() => expect(lastQuery().get('q')).toBe('isik'));
  });

  it('secilen suzgec etiket olarak gorunur; Temizle hepsini kaldirir', async () => {
    const user = userEvent.setup();
    renderWithProviders(<PersonListPage />);
    await screen.findByRole('article', { name: 'AYŞE DEMİR' });

    await user.click(screen.getByRole('button', { name: /Süzgeçler/ }));
    const panel = await screen.findByRole('dialog', { name: 'Süzgeçler' });
    await user.click(within(panel).getByLabelText('Şube'));
    await user.click(await screen.findByRole('option', { name: 'Merkez' }));
    await user.click(within(panel).getByLabelText('Şube'));
    await user.click(await screen.findByRole('option', { name: 'Bornova' }));
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
