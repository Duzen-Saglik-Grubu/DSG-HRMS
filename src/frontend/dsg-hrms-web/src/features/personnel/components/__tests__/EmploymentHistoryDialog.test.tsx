import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { renderWithProviders } from '@/test/render';
import { historyApi, type EmploymentHistory } from '../../api/historyApi';
import { EmploymentHistoryDialog } from '../EmploymentHistoryDialog';

/** Istihdam tarihcesi (PG-PERS-12; SYG-PERS-020). Veriler SENTETIKTIR. */
const HISTORY: EmploymentHistory = {
  date: null,
  assignments: [
    {
      validFrom: '2026-06-01',
      validTo: null,
      companyName: 'Düzen Laboratuvarlar',
      branchName: 'Kızılay',
      departmentName: 'Biyokimya',
      titleName: 'Uzman',
    },
    {
      validFrom: '2026-01-10',
      validTo: '2026-06-01',
      companyName: 'Düzen Laboratuvarlar',
      branchName: 'Merkez',
      departmentName: 'Biyokimya',
      titleName: null,
    },
  ],
  duties: [{ id: 'd-1', name: 'Biyolog', validFrom: '2026-01-10', validTo: '2026-03-01' }],
};

const EMPTY: EmploymentHistory = { date: '2025-12-31', assignments: [], duties: [] };

function rows(name: string) {
  return within(screen.getByRole('table', { name })).getAllByRole('row').slice(1);
}

describe('EmploymentHistoryDialog', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('donemleri yeniden eskiye, bitisi donemin son gunuyle listeler', async () => {
    vi.spyOn(historyApi, 'get').mockResolvedValue(HISTORY);
    renderWithProviders(
      <EmploymentHistoryDialog personId="p-1" registryCode="00012" onClose={vi.fn()} />,
    );

    expect(
      await screen.findByRole('dialog', { name: '00012 sicilinin tarihçesi' }),
    ).toBeInTheDocument();
    expect(historyApi.get).toHaveBeenCalledWith('p-1', '00012', null);

    const assignments = await screen.findByRole('table', { name: 'Firma, şube, birim ve ünvan' });
    expect(assignments).toBeInTheDocument();
    const [current, previous] = rows('Firma, şube, birim ve ünvan');
    expect(current).toHaveTextContent('Kızılay');
    expect(current).toHaveTextContent('01.06.2026');
    expect(current).toHaveTextContent('Devam ediyor');
    // Bitis haric tutulur; ekranda donemin son gunu yazar (AN-23).
    expect(previous).toHaveTextContent('Merkez');
    expect(previous).toHaveTextContent('10.01.2026');
    expect(previous).toHaveTextContent('31.05.2026');
    // Karsiligi olmayan unvan bos isaretiyle.
    expect(within(previous!).getAllByRole('cell')[3]).toHaveTextContent('—');

    const [duty] = rows('Görev');
    expect(duty).toHaveTextContent('Biyolog');
    expect(duty).toHaveTextContent('28.02.2026');
  });

  it('tarih secilince o tarihte gecerli donemleri ister; kayit yoksa soyler', async () => {
    // ADR-0021 §2: T1 oncesi bir tarih icin kayit yoktur, deger tahmin edilmez.
    const get = vi
      .spyOn(historyApi, 'get')
      .mockImplementation((_, __, date) => Promise.resolve(date ? EMPTY : HISTORY));
    const user = userEvent.setup();
    renderWithProviders(
      <EmploymentHistoryDialog personId="p-1" registryCode="00012" onClose={vi.fn()} />,
    );
    await screen.findByRole('table', { name: 'Görev' });

    const field = screen.getByRole('group', { name: 'Tarihteki durum' });
    await user.click(within(field).getAllByRole('spinbutton')[0]!);
    await user.keyboard('31122025');

    expect(await screen.findByText('Bu tarih için kayıt yok.')).toBeInTheDocument();
    expect(get).toHaveBeenLastCalledWith('p-1', '00012', '2025-12-31');

    await user.click(screen.getByRole('button', { name: 'Bütün tarihçe' }));
    expect(await screen.findByRole('table', { name: 'Görev' })).toBeInTheDocument();
  });

  it('bos tarihcede kayit olmadigini yazar', async () => {
    vi.spyOn(historyApi, 'get').mockResolvedValue({ date: null, assignments: [], duties: [] });
    renderWithProviders(
      <EmploymentHistoryDialog personId="p-1" registryCode="00012" onClose={vi.fn()} />,
    );

    expect(await screen.findByText('Bu sicil için tarihçe kaydı yok.')).toBeInTheDocument();
  });

  it('sunucu hatasini gosterir ve kapatilabilir', async () => {
    vi.spyOn(historyApi, 'get').mockRejectedValue(
      new ApiError({ message: 'Kişi bulunamadı.', status: 404, isNetworkError: false }),
    );
    const onClose = vi.fn();
    const user = userEvent.setup();
    renderWithProviders(
      <EmploymentHistoryDialog personId="p-1" registryCode="00012" onClose={onClose} />,
    );

    expect(await screen.findByText('Kişi bulunamadı.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Kapat' }));
    expect(onClose).toHaveBeenCalled();
  });
});
