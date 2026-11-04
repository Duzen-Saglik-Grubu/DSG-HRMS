import { useState } from 'react';
import {
  Alert,
  Button,
  Chip,
  Link,
  Paper,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import FactCheckOutlined from '@mui/icons-material/FactCheckOutlined';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink, useSearchParams } from 'react-router';
import { DEFAULT_PAGE_SIZE, type PageRequest } from '@/shared/api/paging';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { PermissionGate } from '@/shared/auth/PermissionGate';
import { permissions } from '@/shared/auth/permissions';
import { usePermission } from '@/shared/auth/usePermission';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { DataTable, type DataTableColumn } from '@/shared/components/DataTable';
import { EmptyState } from '@/shared/components/EmptyState';
import { ErrorState } from '@/shared/components/ErrorState';
import { PageHeader } from '@/shared/components/PageHeader';
import { personCardPath } from '@/shared/routes';
import {
  correctionsApi,
  type Correction,
  type CorrectionStatusFilter,
} from '../api/correctionsApi';

/** Bildirimden gelen talebin adres parametresi. */
const REQUEST_PARAM = 'request';

/**
 * Duzeltme talepleri (SYG-PERS-048; <c>personnel.correction.view</c>). IK, kisinin bildirdigi
 * dogru degeri gorur, duzeltmeyi LOGO'da yapar ve talebi kapatir. Kapatilan talepte onerilen
 * deger silinir; listede yalnizca alan, durum ve tarihler kalir.
 */
export function CorrectionsPage() {
  const { t } = useTranslation();
  const canView = usePermission(permissions.correctionView);
  const queryClient = useQueryClient();
  const [params, setParams] = useSearchParams();
  const [status, setStatus] = useState<CorrectionStatusFilter>('new');
  const [request, setRequest] = useState<PageRequest>({ page: 1, pageSize: DEFAULT_PAGE_SIZE });
  const [closing, setClosing] = useState<Correction | null>(null);
  const [done, setDone] = useState<string | null>(null);
  const highlighted = params.get(REQUEST_PARAM);

  const list = useQuery({
    queryKey: queryKeys.personnel.corrections(status, request.page, request.pageSize),
    queryFn: () => correctionsApi.list(status, request.page, request.pageSize),
    placeholderData: keepPreviousData,
    enabled: canView,
  });
  const single = useQuery({
    queryKey: queryKeys.personnel.correction(highlighted ?? ''),
    queryFn: () => correctionsApi.get(highlighted ?? ''),
    enabled: canView && Boolean(highlighted),
  });

  const complete = useMutation({
    mutationFn: (item: Correction) => correctionsApi.complete(item.id),
    onSuccess: async (item) => {
      setClosing(null);
      setDone(t('personnel.corrections.completed', { name: `${item.firstName} ${item.lastName}` }));
      await queryClient.invalidateQueries({ queryKey: queryKeys.personnel.correctionsAll });
    },
  });

  const value = (item: Correction) => {
    if (item.proposedValue === null) {
      return (
        <Typography variant="body2" color="text.secondary">
          {t('personnel.corrections.valueRemoved')}
        </Typography>
      );
    }

    return item.field === 'maritalStatus'
      ? t(`personnel.report.marital.${item.proposedValue}`, { defaultValue: item.proposedValue })
      : item.proposedValue;
  };

  const action = (item: Correction) =>
    item.status === 'new' ? (
      <PermissionGate permission={permissions.correctionUpdate}>
        <Button size="small" variant="outlined" onClick={() => setClosing(item)}>
          {t('personnel.corrections.complete')}
        </Button>
      </PermissionGate>
    ) : (
      <Chip size="small" color="success" label={t('personnel.corrections.statusCompleted')} />
    );

  const columns: DataTableColumn<Correction>[] = [
    {
      field: 'person',
      header: t('personnel.corrections.columns.person'),
      render: (item) => (
        <Link component={RouterLink} to={personCardPath(item.personId)}>
          {item.firstName} {item.lastName}
        </Link>
      ),
    },
    {
      field: 'field',
      header: t('personnel.corrections.columns.field'),
      render: (item) => t(`personnel.report.fields.${item.field}`),
    },
    {
      field: 'value',
      header: t('personnel.corrections.columns.value'),
      render: (item) => <span style={{ whiteSpace: 'pre-line' }}>{value(item)}</span>,
    },
    {
      field: 'submittedAt',
      header: t('personnel.corrections.columns.submittedAt'),
      render: (item) => format(new Date(item.submittedAt), 'dd.MM.yyyy HH:mm'),
    },
    { field: 'action', header: t('personnel.corrections.columns.action'), render: action },
  ];

  if (!canView) {
    return (
      <Stack spacing={3}>
        <PageHeader icon={FactCheckOutlined} title={t('personnel.corrections.title')} />
        <Alert severity="warning">{t('error.forbidden')}</Alert>
      </Stack>
    );
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        icon={FactCheckOutlined}
        title={t('personnel.corrections.title')}
        description={t('personnel.corrections.subtitle')}
      />

      {done ? (
        <Alert severity="success" role="status" onClose={() => setDone(null)}>
          {done}
        </Alert>
      ) : null}

      {single.data ? (
        <Paper
          variant="outlined"
          component="section"
          aria-label={t('personnel.corrections.fromNotification')}
          sx={{ p: 2.5, borderColor: 'primary.light' }}
        >
          <Stack spacing={1.5}>
            <Typography variant="subtitle1" component="h2" sx={{ fontWeight: 700 }}>
              {t('personnel.corrections.fromNotification')}
            </Typography>
            <Typography>
              <Link component={RouterLink} to={personCardPath(single.data.personId)}>
                {single.data.firstName} {single.data.lastName}
              </Link>
              {' · '}
              {t(`personnel.report.fields.${single.data.field}`)}
              {' · '}
              {format(new Date(single.data.submittedAt), 'dd.MM.yyyy HH:mm')}
            </Typography>
            <Typography sx={{ whiteSpace: 'pre-line', fontWeight: 600 }}>
              {value(single.data)}
            </Typography>
            <Stack direction="row" spacing={1}>
              {action(single.data)}
              <Button
                size="small"
                onClick={() => {
                  const next = new URLSearchParams(params);
                  next.delete(REQUEST_PARAM);
                  setParams(next, { replace: true });
                }}
              >
                {t('personnel.corrections.dismiss')}
              </Button>
            </Stack>
          </Stack>
        </Paper>
      ) : null}

      <ToggleButtonGroup
        exclusive
        size="small"
        value={status}
        aria-label={t('personnel.corrections.statusFilter')}
        onChange={(_, next: CorrectionStatusFilter | null) => {
          if (next) {
            setStatus(next);
            setRequest((current) => ({ ...current, page: 1 }));
          }
        }}
        sx={{ alignSelf: 'flex-start', bgcolor: 'background.paper' }}
      >
        <ToggleButton value="new">{t('personnel.corrections.statusNew')}</ToggleButton>
        <ToggleButton value="completed">{t('personnel.corrections.statusCompleted')}</ToggleButton>
        <ToggleButton value="all">{t('personnel.corrections.statusAll')}</ToggleButton>
      </ToggleButtonGroup>

      {list.error ? (
        <ErrorState
          error={
            list.error instanceof ApiError
              ? list.error
              : new ApiError({ message: t('error.generic'), isNetworkError: false })
          }
          onRetry={() => void list.refetch()}
        />
      ) : (
        <DataTable
          caption={t('personnel.corrections.title')}
          columns={columns}
          rows={list.data?.items ?? []}
          rowKey={(item) => item.id}
          totalCount={list.data?.totalCount ?? 0}
          request={request}
          onRequestChange={setRequest}
          loading={list.isPending}
          empty={
            <EmptyState
              icon={<FactCheckOutlined fontSize="large" />}
              title={t('personnel.corrections.empty')}
              description={t('personnel.corrections.emptyText')}
            />
          }
        />
      )}

      <ConfirmDialog
        open={closing !== null}
        title={t('personnel.corrections.confirmTitle')}
        description={t('personnel.corrections.confirmText')}
        confirmLabel={t('personnel.corrections.complete')}
        onConfirm={() => {
          if (closing) {
            complete.mutate(closing);
          }
        }}
        onCancel={() => setClosing(null)}
      />
    </Stack>
  );
}
