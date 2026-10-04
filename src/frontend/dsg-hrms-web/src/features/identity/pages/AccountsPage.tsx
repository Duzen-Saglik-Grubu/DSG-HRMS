import { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  InputAdornment,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import SearchRounded from '@mui/icons-material/SearchRounded';
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
import { accountsApi, type AccountState, type AccountSummary } from '../api/accountsApi';
import { invitationApi } from '../api/invitationApi';

/** Arama kutusunda yazma durduktan sonra istegin gidecegi sure. */
const SEARCH_DELAY_MS = 300;

const STATE_COLOR: Record<AccountState, 'default' | 'success' | 'warning' | 'error'> = {
  none: 'default',
  active: 'success',
  passive: 'error',
  locked: 'warning',
};

type Action = { kind: 'deactivate' | 'activate' | 'invite'; account: AccountSummary };

/**
 * IK hesap islemleri (SYG-KMLK-057, 073): kisiyi sicil, ad veya soyadla arama, hesap durumunu
 * gorme, hesabi gerekceyle pasife alma ve yeniden aktiflestirme.
 *
 * Ekran kisisel veri olarak yalnizca ad, soyad, sicil ve firma gosterir. Dugmeler izne gore
 * gorunur; asil denetim sunucudadir.
 */
export function AccountsPage() {
  const { t } = useTranslation();
  const canView = usePermission(permissions.accountView);
  const [term, setTerm] = useState('');
  const [query, setQuery] = useState('');
  const [request, setRequest] = useState<PageRequest>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
    sort: 'lastName',
    order: 'asc',
  });
  const [action, setAction] = useState<Action | null>(null);
  const [done, setDone] = useState<string | null>(null);

  // Yazma durunca aranir; her tusta istek gitmez. Yeni arama ilk sayfadan baslar.
  useEffect(() => {
    const timer = setTimeout(() => {
      setQuery(term.trim());
      setRequest((current) => ({ ...current, page: 1 }));
    }, SEARCH_DELAY_MS);
    return () => clearTimeout(timer);
  }, [term]);

  const params = { ...request, q: query };
  const accounts = useQuery({
    queryKey: queryKeys.identity.accountList(params),
    queryFn: () => accountsApi.search(params),
    placeholderData: keepPreviousData,
    enabled: canView,
  });

  const columns: DataTableColumn<AccountSummary>[] = [
    {
      field: 'lastName',
      header: t('identity.accounts.columns.name'),
      sortable: true,
      render: (row) => `${row.firstName} ${row.lastName}`,
    },
    {
      field: 'employments',
      header: t('identity.accounts.columns.employments'),
      render: (row) => (
        <Stack spacing={0.25}>
          {row.employments.map((employment) => (
            <Typography
              key={`${employment.registryCode}-${employment.companyName}`}
              variant="body2"
              color={employment.isActive ? 'text.primary' : 'text.disabled'}
            >
              {employment.registryCode} · {employment.companyName}
              {employment.isActive ? '' : ` (${t('identity.accounts.ended')})`}
            </Typography>
          ))}
        </Stack>
      ),
    },
    {
      field: 'state',
      header: t('identity.accounts.columns.state'),
      render: (row) => (
        <Stack spacing={0.5} sx={{ alignItems: 'flex-start' }}>
          <Chip
            size="small"
            color={STATE_COLOR[row.state]}
            variant={row.state === 'none' ? 'outlined' : 'filled'}
            label={t(`identity.accounts.state.${row.state}`)}
          />
          {row.statusReason && row.state === 'passive' ? (
            <Typography variant="caption" color="text.secondary">
              {t(`identity.accounts.reason.${row.statusReason}`)}
            </Typography>
          ) : null}
        </Stack>
      ),
    },
    {
      field: 'actions',
      header: t('identity.accounts.columns.actions'),
      align: 'right',
      render: (row) => (
        <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
          {row.state !== 'passive' ? (
            // Aktif calisma kaydi olmayan kisiye baglanti gonderilemez; sunucu reddeder.
            <PermissionGate permission={permissions.inviteCreate}>
              <Button size="small" onClick={() => setAction({ kind: 'invite', account: row })}>
                {t('identity.accounts.invite')}
              </Button>
            </PermissionGate>
          ) : null}
          <PermissionGate permission={permissions.accountUpdate}>
            {row.state === 'passive' ? (
              <Button size="small" onClick={() => setAction({ kind: 'activate', account: row })}>
                {t('identity.accounts.activate')}
              </Button>
            ) : null}
            {(row.state === 'active' || row.state === 'locked') && row.isCurrentUser ? (
              // Kendi hesabini pasife alan yonetici sistem disinda kalir (#138); sunucu da reddeder.
              <Typography variant="body2" color="text.secondary" sx={{ alignSelf: 'center' }}>
                {t('identity.accounts.self')}
              </Typography>
            ) : null}
            {(row.state === 'active' || row.state === 'locked') && !row.isCurrentUser ? (
              <Button
                size="small"
                color="error"
                onClick={() => setAction({ kind: 'deactivate', account: row })}
              >
                {t('identity.accounts.deactivate')}
              </Button>
            ) : null}
          </PermissionGate>
        </Stack>
      ),
    },
  ];

  if (!canView) {
    return (
      <Stack spacing={3}>
        <PageHeader title={t('identity.accounts.title')} />
        <Alert severity="warning">{t('error.forbidden')}</Alert>
      </Stack>
    );
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        title={t('identity.accounts.title')}
        description={t('identity.accounts.subtitle')}
      />

      <TextField
        label={t('identity.accounts.search')}
        value={term}
        onChange={(event) => setTerm(event.target.value)}
        autoFocus
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
        sx={{ maxWidth: 480 }}
      />

      {done ? (
        <Alert severity="success" role="status" onClose={() => setDone(null)}>
          {done}
        </Alert>
      ) : null}

      {accounts.error ? (
        <ErrorState
          error={
            accounts.error instanceof ApiError
              ? accounts.error
              : new ApiError({ message: t('error.generic'), isNetworkError: false })
          }
          onRetry={() => void accounts.refetch()}
        />
      ) : (
        <DataTable
          caption={t('identity.accounts.title')}
          columns={columns}
          rows={accounts.data?.items ?? []}
          rowKey={(row) => row.personId}
          totalCount={accounts.data?.totalCount ?? 0}
          request={request}
          onRequestChange={setRequest}
          loading={accounts.isPending}
          empty={
            <EmptyState
              title={t('identity.accounts.emptyTitle')}
              description={t('identity.accounts.emptyText')}
            />
          }
        />
      )}

      {action ? (
        <StatusDialog
          action={action}
          onClose={() => setAction(null)}
          onDone={(message) => {
            setAction(null);
            setDone(message);
          }}
        />
      ) : null}
    </Stack>
  );
}

interface StatusDialogProps {
  action: Action;
  onClose: () => void;
  onDone: (message: string) => void;
}

/**
 * Pasife alma, aktiflestirme ve davet baglantisi. Gerekce zorunludur ve denetim izine yazilir
 * (SYG-KMLK-053, 057).
 */
function StatusDialog({ action, onClose, onDone }: StatusDialogProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);
  const name = `${action.account.firstName} ${action.account.lastName}`;
  const key = action.kind;

  const change = useMutation({
    mutationFn: async (): Promise<string | undefined> => {
      if (key === 'invite') {
        return (await invitationApi.send(action.account.personId, reason.trim())).expiresAt;
      }

      await (key === 'deactivate'
        ? accountsApi.deactivate(action.account.personId, reason.trim())
        : accountsApi.activate(action.account.personId, reason.trim()));
      return undefined;
    },
    onSuccess: async (expiresAt) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.identity.accounts });
      onDone(
        t(`identity.accounts.${key}Done`, {
          name,
          time: expiresAt ? format(new Date(expiresAt), 'HH:mm') : '',
        }),
      );
    },
  });

  const serverError =
    change.error instanceof ApiError
      ? (change.error.errors?.['reason']?.[0] ?? change.error.message)
      : change.error
        ? t('error.generic')
        : null;

  return (
    <Dialog open onClose={onClose} aria-labelledby="status-dialog-title" fullWidth maxWidth="sm">
      <Box
        component="form"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          if (!reason.trim()) {
            setError(t('identity.accounts.reasonRequired'));
            return;
          }

          setError(null);
          change.mutate();
        }}
      >
        <DialogTitle id="status-dialog-title">{t(`identity.accounts.${key}Title`)}</DialogTitle>
        <DialogContent>
          <Stack spacing={2}>
            <DialogContentText>{t(`identity.accounts.${key}Text`, { name })}</DialogContentText>
            <TextField
              label={t('identity.accounts.reason.label')}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              multiline
              minRows={2}
              autoFocus
              required
              error={Boolean(error)}
              helperText={error ?? t('identity.accounts.reason.hint')}
              slotProps={{ htmlInput: { maxLength: 500 } }}
            />
            {serverError ? (
              <Alert severity="error" role="alert">
                {serverError}
              </Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>{t('confirm.cancel')}</Button>
          <Button
            type="submit"
            variant="contained"
            color={key === 'deactivate' ? 'error' : 'primary'}
            disabled={change.isPending}
          >
            {t(`identity.accounts.${key}`)}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}
