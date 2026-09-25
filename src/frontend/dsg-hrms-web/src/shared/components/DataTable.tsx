import type { ReactNode } from 'react';
import {
  Paper,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TableSortLabel,
} from '@mui/material';
import { useTranslation } from 'react-i18next';
import type { PageRequest, SortOrder } from '@/shared/api/paging';
import { PAGE_SIZE_OPTIONS } from '@/shared/api/paging';

export interface DataTableColumn<TRow> {
  /** Sunucudaki alan adi; siralama bu deger ile istenir. */
  field: string;
  header: string;
  /** Hucre icerigini uretir. Verilmezse alan degeri metne cevrilir. */
  render?: (row: TRow) => ReactNode;
  sortable?: boolean;
  align?: 'left' | 'right' | 'center';
  width?: number | string;
}

interface DataTableProps<TRow> {
  columns: DataTableColumn<TRow>[];
  rows: TRow[];
  /** Satirin degismez kimligi; React anahtarı olarak kullanilir. */
  rowKey: (row: TRow) => string;

  totalCount: number;
  request: PageRequest;
  onRequestChange: (request: PageRequest) => void;

  loading?: boolean;
  /** Liste bos oldugunda gosterilecek icerik (EmptyState onerilir). */
  empty?: ReactNode;
  caption?: string;
}

/**
 * Sunucu tarafi sayfalama ve siralama yapan tablo (ADR-0015 §5, ADR-0010 §4).
 *
 * <b>Sayfalama sunucudadir.</b> 1.500 personelin tamamini indirip tarayicida
 * sayfalamak hem yavas hem de KVKK acisindan yanlistir: kullanicinin gormesi
 * gerekmeyen kayitlar da istemciye inerdi.
 *
 * Yukleme sirasinda bos ekran degil, iskelet (skeleton) satirlar gosterilir;
 * tablo "ziplamaz" ve kullanici islemin surdugunu gorur (ADR-0015 §6).
 */
export function DataTable<TRow>({
  columns,
  rows,
  rowKey,
  totalCount,
  request,
  onRequestChange,
  loading = false,
  empty,
  caption,
}: DataTableProps<TRow>) {
  const { t } = useTranslation();

  const toggleSort = (field: string) => {
    const isSameField = request.sort === field;
    const nextOrder: SortOrder = isSameField && request.order === 'asc' ? 'desc' : 'asc';

    // Siralama degisince ilk sayfaya donulur: kullanici 7. sayfadayken siralama
    // degistirirse, o sayfadaki kayitlar tamamen baska kayitlar olurdu.
    onRequestChange({ ...request, sort: field, order: nextOrder, page: 1 });
  };

  const isEmpty = !loading && rows.length === 0;

  if (isEmpty && empty) {
    return <>{empty}</>;
  }

  return (
    <Paper variant="outlined">
      <TableContainer>
        <Table size="small">
          {caption ? <caption>{caption}</caption> : null}

          <TableHead>
            <TableRow>
              {columns.map((column) => (
                <TableCell
                  key={column.field}
                  align={column.align ?? 'left'}
                  sx={{ width: column.width, fontWeight: 600 }}
                  sortDirection={request.sort === column.field ? request.order : false}
                >
                  {column.sortable ? (
                    <TableSortLabel
                      active={request.sort === column.field}
                      direction={request.sort === column.field ? (request.order ?? 'asc') : 'asc'}
                      onClick={() => toggleSort(column.field)}
                    >
                      {column.header}
                    </TableSortLabel>
                  ) : (
                    column.header
                  )}
                </TableCell>
              ))}
            </TableRow>
          </TableHead>

          <TableBody>
            {loading
              ? Array.from({ length: Math.min(request.pageSize, 5) }, (_, index) => (
                  <TableRow key={`iskelet-${String(index)}`}>
                    {columns.map((column) => (
                      <TableCell key={column.field}>
                        <Skeleton variant="text" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              : rows.map((row) => (
                  <TableRow key={rowKey(row)} hover>
                    {columns.map((column) => (
                      <TableCell key={column.field} align={column.align ?? 'left'}>
                        {column.render ? column.render(row) : defaultCell(row, column.field)}
                      </TableCell>
                    ))}
                  </TableRow>
                ))}

            {isEmpty ? (
              <TableRow>
                <TableCell colSpan={columns.length} align="center" sx={{ py: 4 }}>
                  {t('status.emptyList')}
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </TableContainer>

      <TablePagination
        component="div"
        count={totalCount}
        // MUI sayfa numarasini 0'dan sayar; sozlesme 1'den (ADR-0010 §4).
        page={Math.max(0, request.page - 1)}
        rowsPerPage={request.pageSize}
        rowsPerPageOptions={[...PAGE_SIZE_OPTIONS]}
        onPageChange={(_, yeniSayfa) => onRequestChange({ ...request, page: yeniSayfa + 1 })}
        onRowsPerPageChange={(event) =>
          onRequestChange({ ...request, pageSize: Number(event.target.value), page: 1 })
        }
        labelRowsPerPage={t('table.rowsPerPage')}
        labelDisplayedRows={({ from, to, count }) => t('table.rowRange', { from, to, count })}
      />
    </Paper>
  );
}

/**
 * Ozel bir gosterim verilmediginde hucre icerigi.
 */
function defaultCell<TRow>(row: TRow, field: string): ReactNode {
  const value = (row as Record<string, unknown>)[field];

  // Bos hucre, verinin eksik mi yoksa ekranin bozuk mu oldugunu belirsiz birakir.
  if (value === null || value === undefined) {
    return '—';
  }

  if (typeof value === 'string') {
    return value;
  }

  if (typeof value === 'number' || typeof value === 'boolean' || typeof value === 'bigint') {
    return String(value);
  }

  // Nesne ve dizi degerleri "[object Object]" olarak gorunurdu. Bu bir
  // GELISTIRICI hatasidir: boyle bir alan icin "render" verilmelidir. Kullaniciya
  // anlamsiz metin gostermek yerine bos isareti gosterilir.
  return '—';
}
