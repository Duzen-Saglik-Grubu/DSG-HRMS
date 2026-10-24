import { useEffect, useState } from 'react';
import {
  Badge,
  Box,
  Button,
  Grid,
  InputAdornment,
  MenuItem,
  Paper,
  Skeleton,
  Stack,
  TablePagination,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
} from '@mui/material';
import FilterListRounded from '@mui/icons-material/FilterListRounded';
import GridViewRounded from '@mui/icons-material/GridViewRounded';
import SearchRounded from '@mui/icons-material/SearchRounded';
import TableRowsRounded from '@mui/icons-material/TableRowsRounded';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import type { PageRequest, SortOrder } from '@/shared/api/paging';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { DataTable, type DataTableColumn } from '@/shared/components/DataTable';
import { EmptyState } from '@/shared/components/EmptyState';
import { ErrorState } from '@/shared/components/ErrorState';
import { PageHeader } from '@/shared/components/PageHeader';
import { personsApi, type PersonListItem } from '../api/personsApi';
import { FilterChips } from '../components/FilterChips';
import { FilterPanel } from '../components/FilterPanel';
import { PersonCard } from '../components/PersonCard';
import { joinParts } from '../display';
import { filterTags } from '../filterTags';
import {
  cleared,
  pageOf,
  pageSizeOptions,
  statusOf,
  toApiParams,
  viewOf,
  withChanges,
  type ListView,
  type StatusFilter,
} from '../listState';

/** Arama kutusunda yazma durduktan sonra istegin gidecegi sure. */
const SEARCH_DELAY_MS = 300;

/** Sunucunun varsayilan sayfa boyutu gelmeden once kullanilan deger (PRM-GRN-02 varsayilani). */
const FALLBACK_PAGE_SIZE = 24;

type SortField = 'firstName' | 'lastName';
const SORTS = ['firstName-asc', 'firstName-desc', 'lastName-asc', 'lastName-desc'] as const;

/**
 * Personel listesi (PG-PERS-13–18): kart izgarasi ve tablo, hizli arama, calisma durumu, suzgec
 * paneli, secili suzgec etiketleri ve sayfalama.
 *
 * Kimin listelenecegini sunucu belirler: rolu olmayan personel yalnizca kendini gorur (ADR-0019
 * §1). Ekranin durumu adres cubugundadir; gorunum degistirici ayni suzgec ve sayfayla tabloya
 * gecer ve geri doner (SYG-PERS-023).
 */
export function PersonListPage() {
  const { t } = useTranslation();
  const [params, setParams] = useSearchParams();
  const [panelOpen, setPanelOpen] = useState(false);
  const [term, setTerm] = useState(() => params.get('q') ?? '');

  // Yazma durunca aranir; her tusta istek gitmez. Yeni arama ilk sayfadan baslar.
  useEffect(() => {
    const timer = setTimeout(() => {
      setParams(
        (current) =>
          (current.get('q') ?? '') === term.trim()
            ? current
            : withChanges(current, { q: term.trim() || null }),
        { replace: true },
      );
    }, SEARCH_DELAY_MS);
    return () => clearTimeout(timer);
  }, [term, setParams]);

  const apiParams = toApiParams(params);
  const list = useQuery({
    queryKey: queryKeys.personnel.list(apiParams.toString()),
    queryFn: () => personsApi.list(apiParams),
    placeholderData: keepPreviousData,
  });
  const options = useQuery({
    queryKey: queryKeys.personnel.filterOptions,
    queryFn: personsApi.filterOptions,
    staleTime: 5 * 60_000,
  });

  const view = viewOf(params);
  const status = statusOf(params);
  const sort: SortField = params.get('sort') === 'lastName' ? 'lastName' : 'firstName';
  const order: SortOrder = params.get('order') === 'desc' ? 'desc' : 'asc';
  const defaultPageSize = Number(options.data?.defaultPageSize ?? FALLBACK_PAGE_SIZE);
  const pageSize = Number(params.get('pageSize')) || list.data?.pageSize || defaultPageSize;
  const sizes = options.data
    ? pageSizeOptions(defaultPageSize, Number(options.data.maxPageSize))
    : [pageSize];
  const request: PageRequest = { page: pageOf(params), pageSize, sort, order };
  const filterCount = filterTags(params, options.data, t).length;

  const update = (changes: Record<string, string | string[] | null>) =>
    setParams((current) => withChanges(current, changes));
  const changeRequest = (next: PageRequest) =>
    update({
      page: String(next.page),
      pageSize: String(next.pageSize),
      sort: next.sort ?? null,
      order: next.order ?? null,
    });

  const columns: DataTableColumn<PersonListItem>[] = [
    { field: 'firstName', header: t('personnel.list.columns.firstName'), sortable: true },
    { field: 'lastName', header: t('personnel.list.columns.lastName'), sortable: true },
    { field: 'registryCode', header: t('personnel.list.columns.registryCode') },
    { field: 'companyName', header: t('personnel.list.columns.company') },
    {
      field: 'unit',
      header: t('personnel.list.columns.unit'),
      render: (row) => joinParts(row.branchName, row.departmentName),
    },
    {
      field: 'role',
      header: t('personnel.list.columns.role'),
      render: (row) => joinParts(row.titleName, row.dutyName),
    },
    {
      field: 'state',
      header: t('personnel.list.columns.state'),
      render: (row) =>
        row.isActive ? t('personnel.list.status.active') : t('personnel.list.terminated'),
    },
  ];

  const empty = (
    <EmptyState
      title={t('personnel.list.emptyTitle')}
      description={t('personnel.list.emptyText')}
    />
  );

  return (
    <Stack spacing={2.5}>
      <PageHeader title={t('personnel.list.title')} description={t('personnel.list.subtitle')} />

      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={2}
        sx={{ alignItems: { md: 'center' } }}
      >
        <TextField
          label={
            options.data?.canFilterByNationalId
              ? t('personnel.list.searchWithNationalId')
              : t('personnel.list.search')
          }
          value={term}
          onChange={(event) => setTerm(event.target.value)}
          size="small"
          slotProps={{
            htmlInput: { maxLength: 100 },
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchRounded />
                </InputAdornment>
              ),
            },
          }}
          sx={{ flexGrow: 1, maxWidth: { md: 420 } }}
        />

        <ToggleButtonGroup
          exclusive
          size="small"
          value={status}
          aria-label={t('personnel.list.status.label')}
          onChange={(_, value: StatusFilter | null) => {
            if (value) {
              update({ status: value === 'active' ? null : value });
            }
          }}
        >
          {(['active', 'terminated', 'all'] as const).map((value) => (
            <ToggleButton key={value} value={value}>
              {t(`personnel.list.status.${value}`)}
            </ToggleButton>
          ))}
        </ToggleButtonGroup>

        <Badge badgeContent={filterCount} color="primary">
          <Button
            variant="outlined"
            startIcon={<FilterListRounded />}
            onClick={() => setPanelOpen(true)}
          >
            {t('personnel.list.filters')}
          </Button>
        </Badge>

        <Box sx={{ flexGrow: 1 }} />

        {view === 'grid' ? (
          <TextField
            select
            size="small"
            label={t('personnel.list.sort.label')}
            value={`${sort}-${order}`}
            onChange={(event) => {
              const [field, direction] = event.target.value.split('-');
              update({ sort: field ?? null, order: direction ?? null });
            }}
            sx={{ minWidth: 160 }}
          >
            {SORTS.map((value) => {
              const [field, direction] = value.split('-');
              const key = `${field ?? ''}${direction === 'desc' ? 'Desc' : 'Asc'}`;
              return (
                <MenuItem key={value} value={value}>
                  {t(`personnel.list.sort.${key}`)}
                </MenuItem>
              );
            })}
          </TextField>
        ) : null}

        <ToggleButtonGroup
          exclusive
          size="small"
          value={view}
          aria-label={t('personnel.list.view.label')}
          onChange={(_, value: ListView | null) => {
            if (value) {
              // Gorunum suzgec degildir: sayfa korunur.
              setParams((current) => {
                const next = new URLSearchParams(current);
                if (value === 'grid') {
                  next.delete('view');
                } else {
                  next.set('view', value);
                }
                return next;
              });
            }
          }}
        >
          <ToggleButton value="grid" aria-label={t('personnel.list.view.grid')}>
            <GridViewRounded fontSize="small" />
          </ToggleButton>
          <ToggleButton value="table" aria-label={t('personnel.list.view.table')}>
            <TableRowsRounded fontSize="small" />
          </ToggleButton>
        </ToggleButtonGroup>
      </Stack>

      <FilterChips
        params={params}
        options={options.data}
        onChange={(next) => setParams(next)}
        onClear={() => setParams((current) => cleared(current))}
      />

      {list.error ? (
        <ErrorState
          error={
            list.error instanceof ApiError
              ? list.error
              : new ApiError({ message: t('error.generic'), isNetworkError: false })
          }
          onRetry={() => void list.refetch()}
        />
      ) : view === 'table' ? (
        <DataTable
          caption={t('personnel.list.title')}
          columns={columns}
          rows={list.data?.items ?? []}
          rowKey={(row) => row.personId}
          totalCount={list.data?.totalCount ?? 0}
          request={request}
          onRequestChange={changeRequest}
          loading={list.isPending}
          empty={empty}
          pageSizeOptions={sizes}
          labelRowsPerPage={t('personnel.list.pageSize')}
        />
      ) : (
        <CardGrid
          rows={list.data?.items}
          loading={list.isPending}
          totalCount={list.data?.totalCount ?? 0}
          request={request}
          sizes={sizes}
          empty={empty}
          onRequestChange={changeRequest}
        />
      )}

      <FilterPanel
        open={panelOpen}
        params={params}
        options={options.data}
        onClose={() => setPanelOpen(false)}
        onApply={(next) => {
          setPanelOpen(false);
          setParams(next);
        }}
      />
    </Stack>
  );
}

interface CardGridProps {
  rows: PersonListItem[] | undefined;
  loading: boolean;
  totalCount: number;
  request: PageRequest;
  sizes: number[];
  empty: React.ReactNode;
  onRequestChange: (request: PageRequest) => void;
}

/** Kart izgarasi (SYG-PERS-023); sayfalama tabloyla aynidir. */
function CardGrid({
  rows,
  loading,
  totalCount,
  request,
  sizes,
  empty,
  onRequestChange,
}: CardGridProps) {
  const { t } = useTranslation();

  if (!loading && rows?.length === 0) {
    return <>{empty}</>;
  }

  return (
    <Stack spacing={1}>
      <Grid container spacing={2} component="section" aria-label={t('personnel.list.title')}>
        {loading || !rows
          ? Array.from({ length: 6 }, (_, index) => (
              <Grid key={`iskelet-${String(index)}`} size={{ xs: 12, sm: 6, md: 4, lg: 3 }}>
                <Skeleton variant="rounded" height={120} />
              </Grid>
            ))
          : rows.map((person) => (
              <Grid key={person.personId} size={{ xs: 12, sm: 6, md: 4, lg: 3 }}>
                <PersonCard person={person} />
              </Grid>
            ))}
      </Grid>
      <Paper variant="outlined">
        <TablePagination
          component="div"
          count={totalCount}
          // MUI sayfa numarasini 0'dan sayar; sozlesme 1'den (ADR-0010 §4).
          page={Math.max(0, request.page - 1)}
          rowsPerPage={request.pageSize}
          rowsPerPageOptions={sizes}
          onPageChange={(_, page) => onRequestChange({ ...request, page: page + 1 })}
          onRowsPerPageChange={(event) =>
            onRequestChange({ ...request, pageSize: Number(event.target.value), page: 1 })
          }
          labelRowsPerPage={t('personnel.list.pageSize')}
          labelDisplayedRows={({ from, to, count }) => t('table.rowRange', { from, to, count })}
        />
      </Paper>
    </Stack>
  );
}
