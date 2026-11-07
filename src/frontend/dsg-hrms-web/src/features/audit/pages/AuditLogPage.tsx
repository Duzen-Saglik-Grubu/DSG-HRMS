import { useState } from 'react';
import {
  Alert,
  Autocomplete,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import PolicyOutlined from '@mui/icons-material/PolicyOutlined';
import { format, subDays } from 'date-fns';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import type { TFunction } from 'i18next';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { DEFAULT_PAGE_SIZE, type PageRequest } from '@/shared/api/paging';
import { queryKeys } from '@/shared/api/queryKeys';
import { permissions } from '@/shared/auth/permissions';
import { usePermission } from '@/shared/auth/usePermission';
import { DataTable, type DataTableColumn } from '@/shared/components/DataTable';
import { DateRangeField, type DateRange } from '@/shared/components/DateRangeField';
import { EmptyState } from '@/shared/components/EmptyState';
import { ErrorState } from '@/shared/components/ErrorState';
import { PageHeader } from '@/shared/components/PageHeader';
import {
  auditApi,
  type AuditEntry,
  type AuditEntryType,
  type AuditFilters,
  type AuditPerson,
  type PersonChoice,
} from '../api/auditApi';

const asApiError = (error: unknown, fallback: string) =>
  error instanceof ApiError ? error : new ApiError({ message: fallback, isNetworkError: false });

const TYPES: readonly AuditEntryType[] = ['change', 'access', 'export', 'security'];

/** Varsayilan aralik: son 7 gun (bugun dahil). */
function defaultRange(): DateRange {
  const today = new Date();
  return { start: subDays(today, 6), end: today };
}

function isValid(date: Date | null): date is Date {
  return date !== null && !Number.isNaN(date.getTime());
}

/**
 * "Denetim kayitlari" (PG-PERS-28, 30; SYG-PERS-069; ADR-0022 §2). Degisiklik, erisim, disa
 * aktarma ve guvenlik olayi kayitlari tek, sayfali, salt okunur listede. Maskeli degerler maskeli
 * kalir; ekrani goruntulemek de sunucuda erisim kaydina yazilir.
 */
export function AuditLogPage() {
  const { t } = useTranslation();
  const canView = usePermission(permissions.auditLogView);
  // Kisi suzgecleri personel aramasini kullanir; personel gorme izni yoksa sunulmaz (AN-25).
  const canSearchPeople = usePermission(permissions.personView);
  const [range, setRange] = useState<DateRange>(defaultRange);
  const [type, setType] = useState<AuditEntryType | ''>('');
  const [actor, setActor] = useState<AuditPerson | null>(null);
  const [subject, setSubject] = useState<AuditPerson | null>(null);
  const [request, setRequest] = useState<PageRequest>({ page: 1, pageSize: DEFAULT_PAGE_SIZE });
  const [detail, setDetail] = useState<AuditEntry | null>(null);

  const filters: AuditFilters = {
    from: isValid(range.start) ? format(range.start, 'yyyy-MM-dd') : null,
    to: isValid(range.end) ? format(range.end, 'yyyy-MM-dd') : null,
    type: type === '' ? null : type,
    actor,
    subject,
  };

  const list = useQuery({
    queryKey: queryKeys.audit.entries(
      filters.from,
      filters.to,
      filters.type,
      actor?.personId ?? null,
      subject?.personId ?? null,
      request.page,
      request.pageSize,
    ),
    queryFn: () => auditApi.list(filters, request.page, request.pageSize),
    enabled: canView,
    placeholderData: keepPreviousData,
  });

  // Suzgec degisince ilk sayfaya donulur.
  const change =
    <T,>(set: (value: T) => void) =>
    (value: T) => {
      set(value);
      setRequest((current) => ({ ...current, page: 1 }));
    };

  const columns: DataTableColumn<AuditEntry>[] = [
    {
      field: 'occurredAt',
      header: t('audit.columns.occurredAt'),
      render: (row) => format(new Date(row.occurredAt), 'dd.MM.yyyy HH:mm:ss'),
      width: 170,
    },
    {
      field: 'type',
      header: t('audit.columns.type'),
      render: (row) => (
        <Chip size="small" variant="outlined" label={t(`audit.types.${row.type}`)} />
      ),
    },
    {
      field: 'action',
      header: t('audit.columns.action'),
      render: (row) => actionLabel(t, row.action),
    },
    {
      field: 'actor',
      header: t('audit.columns.actor'),
      render: (row) => row.actor?.fullName ?? t('audit.system'),
    },
    {
      field: 'entityName',
      header: t('audit.columns.entity'),
      render: (row) => entityLabel(t, row.entityName),
    },
    {
      field: 'subject',
      header: t('audit.columns.subject'),
      render: (row) =>
        row.subject?.fullName ??
        (Number(row.subjectCount) > 0
          ? t('audit.subjectCount', { count: Number(row.subjectCount) })
          : '—'),
    },
    {
      field: 'details',
      header: '',
      align: 'right',
      render: (row) => (
        <Button size="small" onClick={() => setDetail(row)}>
          {t('audit.detail')}
        </Button>
      ),
    },
  ];

  if (!canView) {
    return (
      <>
        <PageHeader icon={PolicyOutlined} title={t('audit.title')} />
        <Alert severity="warning">{t('error.forbidden')}</Alert>
      </>
    );
  }

  return (
    <>
      <PageHeader
        icon={PolicyOutlined}
        title={t('audit.title')}
        description={t('audit.subtitle')}
      />

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, md: 6 }}>
            <DateRangeField value={range} onChange={change(setRange)} />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 3 }}>
            <TextField
              select
              fullWidth
              label={t('audit.filters.type')}
              value={type}
              onChange={(event) => change(setType)(event.target.value as AuditEntryType | '')}
            >
              <MenuItem value="">{t('audit.filters.allTypes')}</MenuItem>
              {TYPES.map((value) => (
                <MenuItem key={value} value={value}>
                  {t(`audit.types.${value}`)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          {canSearchPeople ? (
            <>
              <Grid size={{ xs: 12, sm: 6, md: 3 }} />
              <Grid size={{ xs: 12, md: 6 }}>
                <PersonPicker
                  label={t('audit.filters.actor')}
                  value={actor}
                  onChange={change(setActor)}
                />
              </Grid>
              <Grid size={{ xs: 12, md: 6 }}>
                <PersonPicker
                  label={t('audit.filters.subject')}
                  value={subject}
                  onChange={change(setSubject)}
                />
              </Grid>
            </>
          ) : null}
        </Grid>
      </Paper>

      {list.isError ? (
        <ErrorState
          error={asApiError(list.error, t('error.generic'))}
          onRetry={() => void list.refetch()}
        />
      ) : (
        <DataTable
          caption={t('audit.caption')}
          columns={columns}
          rows={list.data?.items ?? []}
          rowKey={(row) => row.id}
          totalCount={Number(list.data?.totalCount ?? 0)}
          request={request}
          onRequestChange={setRequest}
          loading={list.isPending}
          empty={
            <EmptyState
              icon={<PolicyOutlined fontSize="large" />}
              title={t('audit.empty')}
              description={t('audit.emptyText')}
            />
          }
        />
      )}

      {detail ? <DetailDialog entry={detail} onClose={() => setDetail(null)} /> : null}
    </>
  );
}

function PersonPicker({
  label,
  value,
  onChange,
}: {
  label: string;
  value: AuditPerson | null;
  onChange: (value: AuditPerson | null) => void;
}) {
  const { t } = useTranslation();
  const [input, setInput] = useState('');
  const term = input.trim();

  const options = useQuery({
    queryKey: queryKeys.audit.people(term),
    queryFn: () => auditApi.searchPeople(term),
    enabled: term.length >= 2 && term !== value?.fullName,
  });

  return (
    <Autocomplete<PersonChoice | AuditPerson>
      value={value}
      options={options.data ?? []}
      filterOptions={(choices) => choices}
      getOptionLabel={(option) => option.fullName}
      isOptionEqualToValue={(option, selected) => option.personId === selected.personId}
      onChange={(_, selected) =>
        onChange(selected ? { personId: selected.personId, fullName: selected.fullName } : null)
      }
      onInputChange={(_, text) => setInput(text)}
      loading={options.isFetching}
      noOptionsText={
        term.length < 2 ? t('audit.filters.typeToSearch') : t('audit.filters.noPerson')
      }
      renderOption={({ key: _key, ...props }, option) => (
        <li key={option.personId} {...props}>
          {option.fullName}
          {'registryCode' in option && option.registryCode ? ` · ${option.registryCode}` : ''}
        </li>
      )}
      renderInput={(params) => <TextField {...params} label={label} />}
    />
  );
}

/** Kaydin ayrintisi; deger kayitta nasilsa oyle gosterilir, maske acilmaz. */
function DetailDialog({ entry, onClose }: { entry: AuditEntry; onClose: () => void }) {
  const { t } = useTranslation();
  const rows: [string, string][] = [
    [t('audit.columns.occurredAt'), format(new Date(entry.occurredAt), 'dd.MM.yyyy HH:mm:ss')],
    [t('audit.columns.type'), t(`audit.types.${entry.type}`)],
    [t('audit.columns.action'), `${actionLabel(t, entry.action)} (${entry.action})`],
    [t('audit.columns.actor'), entry.actor?.fullName ?? t('audit.system')],
    [t('audit.columns.entity'), `${entityLabel(t, entry.entityName)} (${entry.entityName})`],
    [t('audit.fields.entityId'), entry.entityId ?? '—'],
    [t('audit.columns.subject'), entry.subject?.fullName ?? '—'],
    [t('audit.fields.recordCount'), entry.recordCount == null ? '—' : String(entry.recordCount)],
    [t('audit.fields.subjectCount'), String(entry.subjectCount)],
    [t('audit.fields.traceId'), entry.traceId ?? '—'],
    [t('audit.fields.ipAddress'), entry.ipAddress ?? '—'],
  ];

  return (
    <Dialog open onClose={onClose} aria-labelledby="audit-detail-title" fullWidth maxWidth="md">
      <DialogTitle id="audit-detail-title">{t('audit.detailTitle')}</DialogTitle>
      <DialogContent>
        <Stack component="dl" spacing={1} sx={{ m: 0 }}>
          {rows.map(([label, value]) => (
            <Stack key={label} direction={{ xs: 'column', sm: 'row' }} spacing={{ sm: 2 }}>
              <Typography component="dt" color="text.secondary" sx={{ minWidth: 200 }}>
                {label}
              </Typography>
              <Typography component="dd" sx={{ m: 0, wordBreak: 'break-all' }}>
                {value}
              </Typography>
            </Stack>
          ))}
        </Stack>
        <Typography variant="subtitle2" component="h3" sx={{ mt: 2, mb: 1 }}>
          {t('audit.fields.details')}
        </Typography>
        <Paper
          variant="outlined"
          component="pre"
          aria-label={t('audit.fields.details')}
          sx={{ p: 1.5, m: 0, overflow: 'auto', fontSize: 13, whiteSpace: 'pre-wrap' }}
        >
          {prettyDetails(entry.details)}
        </Paper>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('audit.close')}</Button>
      </DialogActions>
    </Dialog>
  );
}

/** JSON ayrintiyi okunur girintiyle; JSON degilse oldugu gibi. */
function prettyDetails(details: string | null | undefined): string {
  if (!details) {
    return '—';
  }

  try {
    return JSON.stringify(JSON.parse(details), null, 2);
  } catch {
    return details;
  }
}

/** Islem kodunun Turkce adi; tanimsiz kod oldugu gibi gosterilir. */
function actionLabel(t: TFunction, action: string): string {
  return t(`audit.actions.${action}`, { defaultValue: action });
}

/** Kayit turunun Turkce adi; tanimsiz tur oldugu gibi gosterilir. */
function entityLabel(t: TFunction, entity: string): string {
  return t(`audit.entities.${entity}`, { defaultValue: entity });
}
