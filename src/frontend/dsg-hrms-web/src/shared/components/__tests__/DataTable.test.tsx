import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/test/render';
import { DataTable, type DataTableColumn } from '../DataTable';
import { EmptyState } from '../EmptyState';
import type { PageRequest } from '@/shared/api/paging';

interface Personel {
  id: string;
  adSoyad: string;
  birim: string | null;
}

const sutunlar: DataTableColumn<Personel>[] = [
  { field: 'adSoyad', header: 'Ad Soyad', sortable: true },
  { field: 'birim', header: 'Birim' },
];

const satirlar: Personel[] = [
  { id: '1', adSoyad: 'Ahmet Yılmaz', birim: 'Biyokimya' },
  { id: '2', adSoyad: 'Ayşe Demir', birim: null },
];

const istek: PageRequest = { page: 1, pageSize: 25 };

function renderTable(props: Partial<Parameters<typeof DataTable<Personel>>[0]> = {}) {
  const onRequestChange = vi.fn();

  renderWithProviders(
    <DataTable<Personel>
      columns={sutunlar}
      rows={satirlar}
      rowKey={(row) => row.id}
      totalCount={2}
      request={istek}
      onRequestChange={onRequestChange}
      {...props}
    />,
  );

  return { onRequestChange };
}

describe('DataTable', () => {
  it('satirlari ve sutun basliklarini gosterir', () => {
    renderTable();

    expect(screen.getByRole('columnheader', { name: /Ad Soyad/ })).toBeInTheDocument();
    expect(screen.getByText('Ahmet Yılmaz')).toBeInTheDocument();
    expect(screen.getByText('Biyokimya')).toBeInTheDocument();
  });

  it('bos degerleri okunabilir bir isaretle gosterir', () => {
    // Bos hucre, verinin eksik mi yoksa ekranin bozuk mu oldugunu belirsiz birakir.
    renderTable();

    expect(screen.getByText('—')).toBeInTheDocument();
  });

  it('siralama degisince ILK SAYFAYA doner', async () => {
    // Kullanici 7. sayfadayken siralama degistirirse, o sayfadaki kayitlar
    // tamamen baska kayitlar olurdu.
    const { onRequestChange } = renderTable({ request: { page: 7, pageSize: 25 } });

    await userEvent.click(screen.getByRole('button', { name: /Ad Soyad/ }));

    expect(onRequestChange).toHaveBeenCalledWith({
      page: 1,
      pageSize: 25,
      sort: 'adSoyad',
      order: 'asc',
    });
  });

  it('ayni sutuna tekrar tiklandiginda yonu cevirir', async () => {
    const { onRequestChange } = renderTable({
      request: { page: 1, pageSize: 25, sort: 'adSoyad', order: 'asc' },
    });

    await userEvent.click(screen.getByRole('button', { name: /Ad Soyad/ }));

    expect(onRequestChange).toHaveBeenCalledWith(
      expect.objectContaining({ sort: 'adSoyad', order: 'desc' }),
    );
  });

  it('siralanamayan sutunda dugme gostermez', () => {
    renderTable();

    expect(screen.queryByRole('button', { name: /Birim/ })).not.toBeInTheDocument();
  });

  it('sayfa numarasini 1 tabanli olarak bildirir', async () => {
    // MUI 0'dan sayar, sozlesme 1'den (ADR-0010 §4). Cevrim yapilmazsa
    // kullanici ilk sayfayi bir daha goremezdi.
    const { onRequestChange } = renderTable({ totalCount: 100 });

    await userEvent.click(screen.getByRole('button', { name: /sonraki sayfa/i }));

    expect(onRequestChange).toHaveBeenCalledWith(expect.objectContaining({ page: 2 }));
  });

  it('yuklenirken bos ekran degil iskelet gosterir', () => {
    renderTable({ loading: true, rows: [] });

    expect(screen.queryByText('Henüz kayıt yok')).not.toBeInTheDocument();
  });

  it('liste bos oldugunda verilen bos durumu gosterir', () => {
    renderTable({ rows: [], totalCount: 0, empty: <EmptyState title="Henüz personel yok" /> });

    expect(screen.getByText('Henüz personel yok')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });
});
