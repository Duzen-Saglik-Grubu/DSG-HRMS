import { useState } from 'react';
import { Alert, Box, Button, Chip, Link, Paper, Stack, Tab, Tabs, Typography } from '@mui/material';
import FileDownloadOutlined from '@mui/icons-material/FileDownloadOutlined';
import RuleRounded from '@mui/icons-material/RuleRounded';
import SyncRounded from '@mui/icons-material/SyncRounded';
import TaskAltRounded from '@mui/icons-material/TaskAltRounded';
import { keepPreviousData, useMutation, useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router';
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
import { personCardPath } from '@/shared/routes';
import { saveFile } from '@/shared/utils/saveFile';
import {
  DATA_QUALITY_ISSUES,
  qualityApi,
  type DataQualityIssue,
  type DataQualityItem,
  type IneligiblePerson,
  type SyncRunSummary,
} from '../api/qualityApi';

type Section = 'issues' | 'ineligible';

const STATUS_COLOR: Record<string, 'success' | 'warning' | 'error' | 'default'> = {
  succeeded: 'success',
  completedWithWarnings: 'warning',
  partiallyCompleted: 'warning',
  failed: 'error',
};

const when = (value: string) => format(new Date(value), 'dd.MM.yyyy HH:mm');

const asApiError = (error: unknown, fallback: string) =>
  error instanceof ApiError ? error : new ApiError({ message: fallback, isNetworkError: false });

/**
 * Veri kalitesi raporu (PG-PERS-32, 33; SYG-PERS-070, 071). IK, LOGO'da duzeltilmesi gereken
 * kayitlari sicil numarasiyla gorur. Raporda TCKN, e-posta ve telefon yoktur; goruntuleme ve
 * aktarim sunucuda erisim kaydina yazilir.
 */
export function DataQualityPage() {
  const { t } = useTranslation();
  const canView = usePermission(permissions.qualityView);
  const [section, setSection] = useState<Section>('issues');
  const [issue, setIssue] = useState<DataQualityIssue | null>(null);
  const [issuePage, setIssuePage] = useState<PageRequest>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });
  const [ineligiblePage, setIneligiblePage] = useState<PageRequest>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });

  const report = useQuery({
    queryKey: queryKeys.personnel.quality(issue ?? 'all', issuePage.page, issuePage.pageSize),
    queryFn: () => qualityApi.report(issue, issuePage.page, issuePage.pageSize),
    placeholderData: keepPreviousData,
    enabled: canView,
  });
  const ineligible = useQuery({
    queryKey: queryKeys.personnel.qualityIneligible(ineligiblePage.page, ineligiblePage.pageSize),
    queryFn: () => qualityApi.ineligible(ineligiblePage.page, ineligiblePage.pageSize),
    placeholderData: keepPreviousData,
    enabled: canView && section === 'ineligible',
  });

  const exportFile = useMutation({
    mutationFn: () => qualityApi.exportFile(issue),
    onSuccess: (file) => saveFile(file.blob, file.fileName),
  });

  const header = (
    <PageHeader
      icon={RuleRounded}
      title={t('personnel.quality.title')}
      description={t('personnel.quality.subtitle')}
      actions={
        canView ? (
          <PermissionGate permission={permissions.qualityExport}>
            <Button
              variant="outlined"
              startIcon={<FileDownloadOutlined />}
              disabled={exportFile.isPending}
              onClick={() => exportFile.mutate()}
              sx={{ whiteSpace: 'nowrap' }}
            >
              {exportFile.isPending
                ? t('personnel.quality.exporting')
                : t('personnel.quality.export')}
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

  const counts = report.data?.counts;
  const total = counts ? Object.values(counts).reduce((sum, c) => sum + c, 0) : null;

  const person = (row: {
    personId: string | null;
    firstName: string | null;
    lastName: string | null;
  }) =>
    row.personId ? (
      <Link component={RouterLink} to={personCardPath(row.personId)}>
        {row.firstName} {row.lastName}
      </Link>
    ) : (
      <Typography variant="body2" color="text.secondary">
        {t('personnel.quality.noPerson')}
      </Typography>
    );

  const place = <T extends DataQualityItem | IneligiblePerson>(): DataTableColumn<T>[] => [
    {
      field: 'company',
      header: t('personnel.quality.columns.company'),
      render: (r) => r.company ?? '—',
    },
    {
      field: 'branch',
      header: t('personnel.quality.columns.branch'),
      render: (r) => r.branch ?? '—',
    },
    {
      field: 'department',
      header: t('personnel.quality.columns.department'),
      render: (r) => r.department ?? '—',
    },
  ];

  const issueColumns: DataTableColumn<DataQualityItem>[] = [
    {
      field: 'issue',
      header: t('personnel.quality.columns.issue'),
      render: (r) => (
        <Chip size="small" variant="outlined" label={t(`personnel.quality.issues.${r.issue}`)} />
      ),
    },
    { field: 'registryCode', header: t('personnel.quality.columns.registryCode') },
    { field: 'person', header: t('personnel.quality.columns.person'), render: person },
    ...place<DataQualityItem>(),
  ];

  const ineligibleColumns: DataTableColumn<IneligiblePerson>[] = [
    { field: 'registryCode', header: t('personnel.quality.columns.registryCode') },
    { field: 'person', header: t('personnel.quality.columns.person'), render: person },
    ...place<IneligiblePerson>(),
    {
      field: 'reasons',
      header: t('personnel.quality.columns.reasons'),
      render: (r) => (
        <Stack spacing={0.5} sx={{ alignItems: 'flex-start' }}>
          {r.reasons.map((reason) => (
            <Chip
              key={reason}
              size="small"
              color="warning"
              variant="outlined"
              label={t(`personnel.quality.reasons.${reason}`)}
            />
          ))}
        </Stack>
      ),
    },
  ];

  const chooseIssue = (next: DataQualityIssue | null) => {
    setIssue(next);
    setIssuePage((current) => ({ ...current, page: 1 }));
  };

  return (
    <Stack spacing={3}>
      {header}

      {exportFile.error ? (
        <Alert severity="error" onClose={() => exportFile.reset()}>
          {asApiError(exportFile.error, t('personnel.quality.exportFailed')).message}
        </Alert>
      ) : null}

      {report.data ? (
        <RunSummary lastRun={report.data.lastRun} warningsRun={report.data.warningsRun} />
      ) : null}

      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs
          value={section}
          onChange={(_, next: Section) => setSection(next)}
          aria-label={t('personnel.quality.tabs.label')}
          variant="scrollable"
          allowScrollButtonsMobile
        >
          <Tab
            value="issues"
            label={`${t('personnel.quality.tabs.issues')}${total === null ? '' : ` (${total})`}`}
            id="quality-tab-issues"
            aria-controls="quality-panel-issues"
          />
          <Tab
            value="ineligible"
            label={`${t('personnel.quality.tabs.ineligible')}${report.data ? ` (${report.data.ineligibleCount})` : ''}`}
            id="quality-tab-ineligible"
            aria-controls="quality-panel-ineligible"
          />
        </Tabs>
      </Box>

      {section === 'issues' ? (
        <Stack
          spacing={2}
          role="tabpanel"
          id="quality-panel-issues"
          aria-labelledby="quality-tab-issues"
        >
          <Stack
            direction="row"
            useFlexGap
            role="group"
            aria-label={t('personnel.quality.filter')}
            sx={{ flexWrap: 'wrap', gap: 1 }}
          >
            <Chip
              label={`${t('personnel.quality.all')}${total === null ? '' : ` · ${total}`}`}
              color={issue === null ? 'primary' : 'default'}
              variant={issue === null ? 'filled' : 'outlined'}
              aria-pressed={issue === null}
              onClick={() => chooseIssue(null)}
            />
            {DATA_QUALITY_ISSUES.map((item) => (
              <Chip
                key={item}
                label={`${t(`personnel.quality.issues.${item}`)}${counts ? ` · ${counts[item]}` : ''}`}
                color={issue === item ? 'primary' : 'default'}
                variant={issue === item ? 'filled' : 'outlined'}
                aria-pressed={issue === item}
                onClick={() => chooseIssue(item)}
              />
            ))}
          </Stack>

          {issue ? (
            <Alert severity="info" role="note">
              {t(`personnel.quality.hints.${issue}`)}
            </Alert>
          ) : null}

          {report.error ? (
            <ErrorState
              error={asApiError(report.error, t('error.generic'))}
              onRetry={() => void report.refetch()}
            />
          ) : (
            <DataTable
              caption={t('personnel.quality.tabs.issues')}
              columns={issueColumns}
              rows={report.data?.items.items ?? []}
              rowKey={(r) => `${r.issue}-${r.registryCode}`}
              totalCount={report.data?.items.totalCount ?? 0}
              request={issuePage}
              onRequestChange={setIssuePage}
              loading={report.isPending}
              empty={
                <EmptyState
                  icon={<TaskAltRounded fontSize="large" />}
                  title={t('personnel.quality.empty')}
                  description={t('personnel.quality.emptyText')}
                />
              }
            />
          )}
        </Stack>
      ) : (
        <Stack
          spacing={2}
          role="tabpanel"
          id="quality-panel-ineligible"
          aria-labelledby="quality-tab-ineligible"
        >
          <Alert severity="info" role="note">
            {t('personnel.quality.ineligibleNote')}
          </Alert>
          {ineligible.error ? (
            <ErrorState
              error={asApiError(ineligible.error, t('error.generic'))}
              onRetry={() => void ineligible.refetch()}
            />
          ) : (
            <DataTable
              caption={t('personnel.quality.tabs.ineligible')}
              columns={ineligibleColumns}
              rows={ineligible.data?.items ?? []}
              rowKey={(r) => r.personId}
              totalCount={ineligible.data?.totalCount ?? 0}
              request={ineligiblePage}
              onRequestChange={setIneligiblePage}
              loading={ineligible.isPending}
              empty={
                <EmptyState
                  icon={<TaskAltRounded fontSize="large" />}
                  title={t('personnel.quality.ineligibleEmpty')}
                  description={t('personnel.quality.ineligibleEmptyText')}
                />
              }
            />
          )}
        </Stack>
      )}
    </Stack>
  );
}

/** Son senkronizasyonun sonucu; kart uyarilari baska bir calismadan geliyorsa o da yazar. */
function RunSummary({
  lastRun,
  warningsRun,
}: {
  lastRun: SyncRunSummary | null;
  warningsRun: SyncRunSummary | null;
}) {
  const { t } = useTranslation();

  return (
    <Paper
      variant="outlined"
      component="section"
      aria-label={t('personnel.quality.run.title')}
      sx={{ p: 2 }}
    >
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
        <SyncRounded aria-hidden color="action" sx={{ mt: 0.25 }} />
        {lastRun?.finishedAt ? (
          <Stack spacing={0.5} sx={{ minWidth: 0 }}>
            <Stack
              direction="row"
              useFlexGap
              sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}
            >
              <Typography variant="body2">
                {t('personnel.quality.run.title')}: <strong>{when(lastRun.finishedAt)}</strong>
              </Typography>
              <Chip
                size="small"
                color={STATUS_COLOR[lastRun.status] ?? 'default'}
                label={t(`personnel.quality.run.status.${lastRun.status}`, {
                  defaultValue: lastRun.status,
                })}
              />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {t('personnel.quality.run.recordsRead', { count: lastRun.recordsRead })}
              {' · '}
              {t('personnel.quality.run.warnings', { count: lastRun.warningCount })}
            </Typography>
            {warningsRun?.finishedAt && warningsRun.finishedAt !== lastRun.finishedAt ? (
              <Typography variant="body2" color="text.secondary">
                {t('personnel.quality.run.cardWarningsFrom', {
                  when: when(warningsRun.finishedAt),
                })}
              </Typography>
            ) : null}
          </Stack>
        ) : (
          <Typography variant="body2" color="text.secondary">
            {t('personnel.quality.run.none')}
          </Typography>
        )}
      </Stack>
    </Paper>
  );
}
