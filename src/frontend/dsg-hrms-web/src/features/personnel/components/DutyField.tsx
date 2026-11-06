import { useState } from 'react';
import { format } from 'date-fns';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  MenuItem,
  Stack,
  TextField,
  Tooltip,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import DeleteOutlineRounded from '@mui/icons-material/DeleteOutlineRounded';
import WorkOutlineRounded from '@mui/icons-material/WorkOutlineRounded';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { dutiesApi, type Duty } from '../api/dutiesApi';
import { displayDate } from '../display';

type FieldName = 'definitionId' | 'validFrom';

interface DutyFieldProps {
  personId: string;
  registryCode: string;
  duty: Duty | null | undefined;
}

/**
 * Kartin "Gorevi" alani, gorev girme izni olan kullanici icin (PG-PERS-43; SYG-PERS-042).
 * Kullanici sicile gorev atar veya yanlis girilen guncel gorevi siler. Gorev istihdama
 * baglidir: iki sicilli kiside her sicilin gorevi ayridir. Tarih ve cakisma kurallari
 * sunucudadir; hata pencerede gosterilir.
 */
export function DutyField({ personId, registryCode, duty }: DutyFieldProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [assigning, setAssigning] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const refresh = () =>
    // Kart iki anahtarla okunur: kisi karti (kimlikle) ve Profilim ('me'). Ikisi de yenilenir (#357).
    queryClient.invalidateQueries({ queryKey: queryKeys.personnel.cards });

  return (
    <Box component="span" sx={{ display: 'block' }}>
      <Stack component="span" direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
        <Box
          component="span"
          sx={{ color: duty ? 'text.primary' : 'text.disabled', flexGrow: 1, minWidth: 0 }}
        >
          {duty ? duty.name : '—'}
        </Box>
        <Tooltip title={t('personnel.duty.assign')}>
          <IconButton
            size="small"
            aria-label={t('personnel.duty.assignNamed', { registryCode })}
            onClick={() => setAssigning(true)}
          >
            <WorkOutlineRounded fontSize="small" />
          </IconButton>
        </Tooltip>
        {duty ? (
          <Tooltip title={t('personnel.duty.delete')}>
            <IconButton
              size="small"
              aria-label={t('personnel.duty.deleteNamed', { name: duty.name })}
              onClick={() => {
                setDeleteError(null);
                setDeleting(true);
              }}
            >
              <DeleteOutlineRounded fontSize="small" />
            </IconButton>
          </Tooltip>
        ) : null}
      </Stack>

      {deleteError ? (
        <Alert severity="error" role="alert" sx={{ mt: 1 }} onClose={() => setDeleteError(null)}>
          {deleteError}
        </Alert>
      ) : null}

      {assigning ? (
        <DutyDialog
          personId={personId}
          registryCode={registryCode}
          onClose={() => setAssigning(false)}
          onSaved={async () => {
            setAssigning(false);
            await refresh();
          }}
        />
      ) : null}

      <ConfirmDialog
        open={deleting}
        title={t('personnel.duty.deleteTitle')}
        description={
          duty
            ? t('personnel.duty.deleteText', {
                name: duty.name,
                validFrom: displayDate(duty.validFrom),
              })
            : ''
        }
        destructive
        confirmLabel={t('personnel.duty.delete')}
        onCancel={() => setDeleting(false)}
        onConfirm={async () => {
          if (!duty) {
            return;
          }

          try {
            await dutiesApi.remove(personId, registryCode, duty.id);
            setDeleting(false);
            await refresh();
          } catch (error) {
            setDeleting(false);
            setDeleteError(error instanceof ApiError ? error.message : t('error.generic'));
          }
        }}
      />
    </Box>
  );
}

interface DialogProps {
  personId: string;
  registryCode: string;
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}

/**
 * Gorev atama formu. Gorev referans verideki aktif tanimlardan secilir; baslangic varsayilan
 * olarak bugundur (SYG-PERS-042). Onceki acik gorev yeni baslangicla kapanir.
 */
function DutyDialog({ personId, registryCode, onClose, onSaved }: DialogProps) {
  const { t } = useTranslation();
  const [definitionId, setDefinitionId] = useState('');
  const [validFrom, setValidFrom] = useState<Date | null>(() => new Date());
  const [missing, setMissing] = useState<Set<FieldName>>(new Set());

  const options = useQuery({
    queryKey: queryKeys.personnel.dutyOptions,
    queryFn: dutiesApi.options,
  });

  const save = useMutation({
    mutationFn: (start: Date) =>
      dutiesApi.assign(personId, registryCode, definitionId, format(start, 'yyyy-MM-dd')),
    onSuccess: onSaved,
  });

  const serverErrors = save.error instanceof ApiError ? save.error.errors : undefined;
  const fieldError = (field: FieldName): string | undefined =>
    missing.has(field) ? t('personnel.duty.required') : serverErrors?.[field]?.[0];
  const generalError =
    save.error && !serverErrors?.definitionId && !serverErrors?.validFrom
      ? save.error instanceof ApiError
        ? save.error.message
        : t('error.generic')
      : null;

  const clearMissing = (field: FieldName) =>
    setMissing((current) => {
      const next = new Set(current);
      next.delete(field);
      return next;
    });

  return (
    <Dialog open onClose={onClose} aria-labelledby="duty-dialog-title" fullWidth maxWidth="sm">
      <Box
        component="form"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          const empty = new Set<FieldName>();
          if (!definitionId) {
            empty.add('definitionId');
          }
          if (!validFrom || Number.isNaN(validFrom.getTime())) {
            empty.add('validFrom');
          }
          setMissing(empty);
          if (empty.size === 0 && validFrom) {
            save.mutate(validFrom);
          }
        }}
      >
        <DialogTitle id="duty-dialog-title">{t('personnel.duty.assignTitle')}</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 1 }}>
            {t('personnel.duty.assignText', { registryCode })}
          </DialogContentText>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            <TextField
              select
              label={t('personnel.duty.fields.definitionId')}
              value={options.isPending ? '' : definitionId}
              onChange={(event) => {
                setDefinitionId(event.target.value);
                clearMissing('definitionId');
              }}
              required
              autoFocus
              error={Boolean(fieldError('definitionId')) || options.isError}
              helperText={
                fieldError('definitionId') ??
                (options.isError ? t('personnel.duty.optionsFailed') : undefined)
              }
            >
              {(options.data ?? []).map((option) => (
                <MenuItem key={option.id} value={option.id}>
                  {option.name}
                </MenuItem>
              ))}
            </TextField>

            <DatePicker
              label={t('personnel.duty.fields.validFrom')}
              value={validFrom}
              onChange={(date) => {
                setValidFrom(date);
                clearMissing('validFrom');
              }}
              format="dd.MM.yyyy"
              slotProps={{
                textField: {
                  required: true,
                  error: Boolean(fieldError('validFrom')),
                  helperText: fieldError('validFrom') ?? t('personnel.duty.validFromHint'),
                },
              }}
            />

            {generalError ? (
              <Alert severity="error" role="alert">
                {generalError}
              </Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>{t('confirm.cancel')}</Button>
          <Button type="submit" variant="contained" disabled={save.isPending}>
            {t('personnel.duty.save')}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}
