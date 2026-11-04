import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { permissions } from '@/shared/auth/permissions';
import { renderWithProviders } from '@/test/render';
import { setSession } from '@/test/session';
import { correctionsApi, type Correction } from '../../api/correctionsApi';
import { CorrectionsPage } from '../CorrectionsPage';

/** Duzeltme talepleri ekrani (SYG-PERS-048). Veriler SENTETIKTIR. */
const ADDRESS: Correction = {
  id: 'c-1',
  personId: 'p-1',
  firstName: 'AYŞE',
  lastName: 'DEMİR',
  field: 'address',
  proposedValue: 'Yeni Mah. 3. Sok. No: 4',
  status: 'new',
  submittedAt: '2026-11-04T06:15:00Z',
  completedAt: null,
};

const MARITAL: Correction = {
  ...ADDRESS,
  id: 'c-2',
  field: 'maritalStatus',
  proposedValue: 'married',
};

function page(items: Correction[]) {
  return { items, page: 1, pageSize: 25, totalCount: items.length, totalPages: 1 };
}

describe('CorrectionsPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('izni olmayan kullaniciya listeyi gostermez', async () => {
    await setSession({ status: 'authenticated', permissions: [] });
    const list = vi.spyOn(correctionsApi, 'list');
    renderWithProviders(<CorrectionsPage />);

    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(list).not.toHaveBeenCalled();
  });

  it('bekleyen talepleri alan ve onerilen degerle listeler', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.correctionView] });
    vi.spyOn(correctionsApi, 'list').mockResolvedValue(page([ADDRESS, MARITAL]));
    renderWithProviders(<CorrectionsPage />);

    // Ilk cizimde iskelet satirlar var; veri gelene kadar beklenir.
    await screen.findByText('Yeni Mah. 3. Sok. No: 4');
    const rows = screen.getAllByRole('row');
    expect(within(rows[1]!).getByRole('link', { name: 'AYŞE DEMİR' })).toHaveAttribute(
      'href',
      '/personnel/p-1',
    );
    expect(within(rows[1]!).getByText('Yeni Mah. 3. Sok. No: 4')).toBeInTheDocument();
    expect(within(rows[2]!).getByText('Medeni hâl')).toBeInTheDocument();
    expect(within(rows[2]!).getByText('Evli')).toBeInTheDocument();
    expect(correctionsApi.list).toHaveBeenCalledWith('new', 1, 25);

    // Kapatma izni olmayan kullanici dugmeyi gormez.
    expect(
      screen.queryByRole('button', { name: 'Tamamlandı olarak kapat' }),
    ).not.toBeInTheDocument();
  });

  it('onay sonrasi talebi kapatir', async () => {
    await setSession({
      status: 'authenticated',
      permissions: [permissions.correctionView, permissions.correctionUpdate],
    });
    vi.spyOn(correctionsApi, 'list').mockResolvedValue(page([ADDRESS]));
    const complete = vi
      .spyOn(correctionsApi, 'complete')
      .mockResolvedValue({ ...ADDRESS, status: 'completed', proposedValue: null });
    const user = userEvent.setup();
    renderWithProviders(<CorrectionsPage />);

    await user.click(await screen.findByRole('button', { name: 'Tamamlandı olarak kapat' }));
    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveTextContent('önerilen değer silinir');
    await user.click(within(dialog).getByRole('button', { name: 'Tamamlandı olarak kapat' }));

    expect(await screen.findByText('AYŞE DEMİR için talep kapatıldı.')).toBeInTheDocument();
    expect(complete).toHaveBeenCalledWith('c-1');
  });

  it('durum suzgeci tamamlanan talepleri getirir; silinen deger aciklanir', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.correctionView] });
    const list = vi
      .spyOn(correctionsApi, 'list')
      .mockResolvedValue(page([{ ...ADDRESS, status: 'completed', proposedValue: null }]));
    const user = userEvent.setup();
    renderWithProviders(<CorrectionsPage />);

    await user.click(await screen.findByRole('button', { name: 'Tamamlandı' }));

    await vi.waitFor(() => expect(list).toHaveBeenLastCalledWith('completed', 1, 25));
    expect(await screen.findByText('Talep kapatıldığı için silindi')).toBeInTheDocument();
  });
});
