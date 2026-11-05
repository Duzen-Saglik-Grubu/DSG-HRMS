import { useEffect, useRef, useState } from 'react';
import { Alert, Button, Chip, CircularProgress, Stack, Typography } from '@mui/material';
import CloudSyncOutlined from '@mui/icons-material/CloudSyncOutlined';
import HistoryRounded from '@mui/icons-material/HistoryRounded';
import SyncRounded from '@mui/icons-material/SyncRounded';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { useTranslation } from 'react-i18next';
import { DEFAULT_PAGE_SIZE, type PageRequest } from '@/shared/api/paging';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { PermissionGate } from '@/shared/auth/PermissionGate';
import { permissions } from '@/shared/auth/permissions';
import { usePermission } from '@/shared/auth/usePermission';
import { DataTable, type DataTableColumn } from '@/shared/components/DataTable';
import { EmptyState } from '@/shared/components/EmptyState';
import { ErrorState } from '@/shared/components/ErrorState';
import { PageHeader } from '@/shared/components/PageHeader';
import { syncApi, type SyncRun } from '../api/syncApi';

/** Calisma surerken veya baslatma istendikten sonra ekranin yenilenme araligi. */
const POLL_MS = 5_000;

/** Istenen calisma bu sure icinde baslamazsa ekran beklemeyi birakir (periyodik yenileme kalir). */
const AWAIT_LIMIT_MS = 2 * 60_000;

const STATUS_COLOR: Record<string, 'success' | 'warning' | 'error' | 'info' | 'default'> = {
  running: 'info',
  succeeded: 'success',
  completedWithWarnings: 'warning',
  partiallyCompleted: 'warning',
  failed: 'error',
  interrupted: 'error',
};

const when = (value: string) => format(new Date(value), 'dd.MM.yyyy HH:mm:ss');

const asApiError = (error: unknown, fallback: string) =>
  error instanceof ApiError ? error : new ApiError({ message: fallback, isNetworkError: false });

/**
 * Senkronizasyon durumu (PG-PERS-05, 06; SYG-PERS-008, 009). Son calismalar sayilariyla listelenir;
 * yetkili kullanici calismayi elle baslatir. Calisma surerken dugme pasiftir ve ekran kendini
 * yeniler; calisma bitince sonucu listede gorunur.
 */
export function SyncRunsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const canView = usePermission(permissions.syncView);
  const [request, setRequest] = useState<PageRequest>({ page: 1, pageSize: DEFAULT_PAGE_SIZE });
  const [awaitingSince, setAwaitingSince] = useState<number | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const list = useQuery({
    queryKey: queryKeys.personnel.syncRuns(request.page, request.pageSize),
    queryFn: () => syncApi.list(request.page, request.pageSize),
    placeholderData: keepPreviousData,
    enabled: canView,
    refetchInterval: (query) =>
      query.state.data?.running || awaitingSince !== null ? POLL_MS : false,
  });

  const running = list.data?.running ?? false;
  const enabled = list.data?.enabled ?? true;

  // Calisma bitince (suruyor -> bitti) "Son senkronizasyon" ve veri kalitesi de yenilenir.
  const wasRunning = useRef(false);
  useEffect(() => {
    if (!list.data) {
      return;
    }

    if (running) {
      wasRunning.current = true;
      return;
    }

    if (wasRunning.current) {
      wasRunning.current = false;
      setAwaitingSince(null);
      void queryClient.invalidateQueries({ queryKey: queryKeys.personnel.syncFreshness });
      void queryClient.invalidateQueries({ queryKey: ['personnel', 'quality'] });
    }
  }, [list.data, running, queryClient]);

  useEffect(() => {
    if (awaitingSince === null) {
      return undefined;
    }

    const timer = window.setTimeout(() => setAwaitingSince(null), AWAIT_LIMIT_MS);
    return () => window.clearTimeout(timer);
  }, [awaitingSince]);

  const start = useMutation({
    mutationFn: syncApi.start,
    onSuccess: (result) => {
      setNotice(
        result.alreadyQueued ? t('personnel.sync.alreadyQueued') : t('personnel.sync.started'),
      );
      setAwaitingSince(Date.now());
      void queryClient.invalidateQueries({ queryKey: ['personnel', 'sync-runs'] });
    },
    onError: () => {
      setNotice(null);
      // 409: baska bir calisma suruyor; liste "calisiyor" durumunu gostersin.
      void queryClient.invalidateQueries({ queryKey: ['personnel', 'sync-runs'] });
    },
  });

  const header = (
    <PageHeader
      icon={CloudSyncOutlined}
      title={t('personnel.sync.title')}
      description={t('personnel.sync.subtitle')}
      actions={
        canView ? (
          <PermissionGate permission={permissions.syncCreate}>
            <Button
              variant="contained"
              startIcon={
                start.isPending || running ? (
                  <CircularProgress size={16} color="inherit" aria-hidden />
                ) : (
                  <SyncRounded />
                )
              }
              disabled={start.isPending || running || !enabled || !list.data}
              onClick={() => start.mutate()}
              sx={{ whiteSpace: 'nowrap' }}
            >
              {running ? t('personnel.sync.running') : t('personnel.sync.start')}
            </Button>
          </PermissionGate>
        ) : undefined
      }
    />
  );

  if (!canView) {
    return (
      <Stack spacing={3}>
        {header}
        <Alert severity="warning">{t('error.forbidden')}</Alert>
      </Stack>
    );
  }

  const columns: DataTableColumn<SyncRun>[] = [
    {
      field: 'startedAt',
      header: t('personnel.sync.columns.startedAt'),
      render: (r) => (
        <Stack spacing={0.25}>
          <Typography variant="body2" sx={{ whiteSpace: 'nowrap' }}>
            {when(r.startedAt)}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {t(`personnel.sync.trigger.${r.trigger}`, { defaultValue: r.trigger })}
          </Typography>
        </Stack>
      ),
    },
    {
      field: 'duration',
      header: t('personnel.sync.columns.duration'),
      render: (r) => (
        <Typography variant="body2" component="span" sx={{ whiteSpace: 'nowrap' }}>
          {r.finishedAt ? duration(r.startedAt, r.finishedAt, t) : '—'}
        </Typography>
      ),
    },
    {
      field: 'status',
      header: t('personnel.sync.columns.status'),
      render: (r) => (
        <Stack spacing={0.5} sx={{ alignItems: 'flex-start' }}>
          <Chip
            size="small"
            color={STATUS_COLOR[r.status] ?? 'default'}
            label={t(`personnel.sync.status.${r.status}`, { defaultValue: r.status })}
          />
          {r.failureReason ? (
            <Typography variant="caption" color="text.secondary">
              {t(`personnel.sync.reason.${r.failureReason}`, { defaultValue: r.failureReason })}
            </Typography>
          ) : null}
        </Stack>
      ),
    },
    {
      field: 'recordsRead',
      header: t('personnel.sync.columns.recordsRead'),
      align: 'right',
    },
    {
      field: 'created',
      header: t('personnel.sync.columns.created'),
      align: 'right',
      render: (r) => pair(r.personsCreated, r.employmentsCreated, t),
    },
    {
      field: 'updated',
      header: t('personnel.sync.columns.updated'),
      align: 'right',
      render: (r) => pair(r.personsUpdated, r.employmentsUpdated, t),
    },
    {
      field: 'employmentsDeactivated',
      header: t('personnel.sync.columns.deactivated'),
      align: 'right',
    },
    {
      field: 'warningCount',
      header: t('personnel.sync.columns.warnings'),
      align: 'right',
    },
  ];

  return (
    <Stack spacing={3}>
      {header}

      {list.data && !enabled ? <Alert severity="info">{t('personnel.sync.disabled')}</Alert> : null}

      {running ? (
        <Alert severity="info" icon={<CircularProgress size={20} aria-hidden />} role="status">
          {t('personnel.sync.runningNote')}
        </Alert>
      ) : null}

      {notice && !running ? (
        <Alert severity="success" role="status" onClose={() => setNotice(null)}>
          {notice}
        </Alert>
      ) : null}

      {start.error ? (
        <Alert severity="error" onClose={() => start.reset()}>
          {asApiError(start.error, t('personnel.sync.startFailed')).message}
        </Alert>
      ) : null}

      {list.error ? (
        <ErrorState
          error={asApiError(list.error, t('error.generic'))}
          onRetry={() => void list.refetch()}
        />
      ) : (
        <DataTable
          caption={t('personnel.sync.caption')}
          columns={columns}
          rows={list.data?.runs.items ?? []}
          rowKey={(r) => r.id}
          totalCount={list.data?.runs.totalCount ?? 0}
          request={request}
          onRequestChange={setRequest}
          loading={list.isPending}
          empty={
            <EmptyState
              icon={<HistoryRounded fontSize="large" />}
              title={t('personnel.sync.empty')}
              description={t('personnel.sync.emptyText')}
            />
          }
        />
      )}
    </Stack>
  );
}

type Translate = ReturnType<typeof useTranslation>['t'];

/** Kisi ve istihdam sayisi: "3 kişi · 4 sicil". */
function pair(persons: number, employments: number, t: Translate) {
  return (
    <Typography variant="body2" component="span" sx={{ whiteSpace: 'nowrap' }}>
      {t('personnel.sync.persons', { count: persons })}
      {' · '}
      {t('personnel.sync.employments', { count: employments })}
    </Typography>
  );
}

/** Calismanin suresi: "45 sn", "2 dk 05 sn". */
function duration(startedAt: string, finishedAt: string, t: Translate) {
  const seconds = Math.max(
    0,
    Math.round((new Date(finishedAt).getTime() - new Date(startedAt).getTime()) / 1000),
  );
  const minutes = Math.floor(seconds / 60);
  const rest = seconds % 60;

  return minutes > 0
    ? t('personnel.sync.durationMinutes', { minutes, seconds: String(rest).padStart(2, '0') })
    : t('personnel.sync.durationSeconds', { seconds });
}
