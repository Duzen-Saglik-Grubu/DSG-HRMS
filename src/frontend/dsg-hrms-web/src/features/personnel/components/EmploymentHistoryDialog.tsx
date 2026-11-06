import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Skeleton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { format, parseISO, subDays } from 'date-fns';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { historyApi, type EmploymentHistory } from '../api/historyApi';
import { displayDate } from '../display';

interface EmploymentHistoryDialogProps {
  personId: string;
  registryCode: string;
  onClose: () => void;
}

/**
 * Istihdam tarihcesi (PG-PERS-12; SYG-PERS-020): atama (firma, sube, birim, unvan) ve gorev
 * donemleri, yeniden eskiye. "Tarihteki durum" verilirse yalnizca o tarihte gecerli donemler
 * gosterilir; T1 oncesi bir tarih icin kayit yoktur, deger tahmin edilmez (ADR-0021 §2).
 */
export function EmploymentHistoryDialog({
  personId,
  registryCode,
  onClose,
}: EmploymentHistoryDialogProps) {
  const { t } = useTranslation();
  const [asOf, setAsOf] = useState<Date | null>(null);
  const date = asOf && !Number.isNaN(asOf.getTime()) ? format(asOf, 'yyyy-MM-dd') : null;

  const history = useQuery({
    queryKey: queryKeys.personnel.history(personId, registryCode, date),
    queryFn: () => historyApi.get(personId, registryCode, date),
  });

  const empty =
    history.data && history.data.assignments.length === 0 && history.data.duties.length === 0;

  return (
    <Dialog open onClose={onClose} aria-labelledby="history-dialog-title" fullWidth maxWidth="md">
      <DialogTitle id="history-dialog-title">
        {t('personnel.history.title', { registryCode })}
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} sx={{ pt: 1 }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ alignItems: 'center' }}>
            <DatePicker
              label={t('personnel.history.asOf')}
              value={asOf}
              onChange={setAsOf}
              format="dd.MM.yyyy"
              slotProps={{ textField: { helperText: t('personnel.history.asOfHint') } }}
            />
            {asOf ? (
              <Button onClick={() => setAsOf(null)}>{t('personnel.history.showAll')}</Button>
            ) : null}
          </Stack>

          {history.isError ? (
            <Alert severity="error" role="alert">
              {history.error instanceof ApiError ? history.error.message : t('error.generic')}
            </Alert>
          ) : history.isPending ? (
            <Skeleton variant="rounded" height={160} />
          ) : empty ? (
            <Alert severity="info" role="status">
              {date ? t('personnel.history.noRecordOnDate') : t('personnel.history.noRecord')}
            </Alert>
          ) : (
            <HistoryTables history={history.data} />
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('personnel.history.close')}</Button>
      </DialogActions>
    </Dialog>
  );
}

function HistoryTables({ history }: { history: EmploymentHistory }) {
  const { t } = useTranslation();
  const column = (name: string) => t(`personnel.history.columns.${name}`);

  return (
    <>
      <Box>
        <Typography variant="subtitle2" component="h3" sx={{ mb: 1 }}>
          {t('personnel.history.assignments')}
        </Typography>
        <TableContainer>
          <Table size="small" aria-label={t('personnel.history.assignments')}>
            <TableHead>
              <TableRow>
                <TableCell>{column('company')}</TableCell>
                <TableCell>{column('branch')}</TableCell>
                <TableCell>{column('department')}</TableCell>
                <TableCell>{column('title')}</TableCell>
                <TableCell>{column('from')}</TableCell>
                <TableCell>{column('to')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {history.assignments.length === 0 ? (
                <EmptyRow span={6} />
              ) : (
                history.assignments.map((period) => (
                  <TableRow key={period.validFrom}>
                    <TableCell>{period.companyName}</TableCell>
                    <TableCell>{period.branchName ?? '—'}</TableCell>
                    <TableCell>{period.departmentName ?? '—'}</TableCell>
                    <TableCell>{period.titleName ?? '—'}</TableCell>
                    <TableCell>{displayDate(period.validFrom)}</TableCell>
                    <TableCell>
                      <PeriodEnd validTo={period.validTo} />
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Box>

      <Box>
        <Typography variant="subtitle2" component="h3" sx={{ mb: 1 }}>
          {t('personnel.history.duties')}
        </Typography>
        <TableContainer>
          <Table size="small" aria-label={t('personnel.history.duties')}>
            <TableHead>
              <TableRow>
                <TableCell>{column('duty')}</TableCell>
                <TableCell>{column('from')}</TableCell>
                <TableCell>{column('to')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {history.duties.length === 0 ? (
                <EmptyRow span={3} />
              ) : (
                history.duties.map((duty) => (
                  <TableRow key={duty.id}>
                    <TableCell>{duty.name}</TableCell>
                    <TableCell>{displayDate(duty.validFrom)}</TableCell>
                    <TableCell>
                      <PeriodEnd validTo={duty.validTo} />
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Box>
    </>
  );
}

/**
 * Donemin son gunu. Sunucudaki bitis haric tutulur (ADR-0021 §2); ekranda donemin son gunu
 * gosterilir (AN-23). Acik donemde "Devam ediyor" yazar.
 */
function PeriodEnd({ validTo }: { validTo: string | null | undefined }) {
  const { t } = useTranslation();

  return validTo ? (
    <>{format(subDays(parseISO(validTo), 1), 'dd.MM.yyyy')}</>
  ) : (
    <>{t('personnel.history.ongoing')}</>
  );
}

function EmptyRow({ span }: { span: number }) {
  const { t } = useTranslation();

  return (
    <TableRow>
      <TableCell colSpan={span} sx={{ color: 'text.disabled' }}>
        {t('personnel.history.noPeriod')}
      </TableCell>
    </TableRow>
  );
}
