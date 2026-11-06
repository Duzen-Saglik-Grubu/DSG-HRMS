import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  FormHelperText,
  FormLabel,
  Grid,
  Radio,
  RadioGroup,
  Stack,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import DeleteOutlineRounded from '@mui/icons-material/DeleteOutlineRounded';
import EditOutlined from '@mui/icons-material/EditOutlined';
import { format, parseISO } from 'date-fns';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { marriageApi, type Marriage } from '../api/marriageApi';
import { displayDate } from '../display';

type FieldName = 'marriageDate' | 'allowanceReceived';

interface MarriageActionsProps {
  personId: string;
  marriage: Marriage;
  /** Giris izni var mi (`personnel.person.update`); duzenlenemezse nedenini yazmak icin. */
  canEdit: boolean;
}

/**
 * Evlilik bilgisinin duzenleme ve silme dugmeleri (PG-PERS-19; SYG-PERS-036). Sunucu, giris izni
 * olan kullanicida ve medeni hal Evli iken `editable` doner. Medeni hal Bekar'a donmusse bilgi
 * gorunur kalir ama duzenlenemez (KR-125); izni olana nedeni yazilir.
 */
export function MarriageActions({ personId, marriage, canEdit }: MarriageActionsProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const recorded = marriage.marriageDate !== null;

  // Kart iki anahtarla okunur (kisi karti ve Profilim); ikisi de yenilenir (#357).
  const refresh = () => queryClient.invalidateQueries({ queryKey: queryKeys.personnel.cards });

  if (!marriage.editable) {
    return canEdit ? (
      <Grid size={12}>
        <Alert severity="info" variant="outlined">
          {t('personnel.marriage.notMarried')}
        </Alert>
      </Grid>
    ) : null;
  }

  return (
    <Grid size={12}>
      <Stack direction="row" spacing={1}>
        <Button size="small" startIcon={<EditOutlined />} onClick={() => setEditing(true)}>
          {recorded ? t('personnel.marriage.edit') : t('personnel.marriage.add')}
        </Button>
        {recorded ? (
          <Button
            size="small"
            color="error"
            startIcon={<DeleteOutlineRounded />}
            onClick={() => {
              setDeleteError(null);
              setDeleting(true);
            }}
          >
            {t('personnel.marriage.delete')}
          </Button>
        ) : null}
      </Stack>

      {deleteError ? (
        <Alert severity="error" role="alert" sx={{ mt: 1 }} onClose={() => setDeleteError(null)}>
          {deleteError}
        </Alert>
      ) : null}

      {editing ? (
        <MarriageDialog
          personId={personId}
          marriage={marriage}
          onClose={() => setEditing(false)}
          onSaved={async () => {
            setEditing(false);
            await refresh();
          }}
        />
      ) : null}

      <ConfirmDialog
        open={deleting}
        title={t('personnel.marriage.deleteTitle')}
        description={t('personnel.marriage.deleteText', {
          date: displayDate(marriage.marriageDate),
        })}
        destructive
        confirmLabel={t('personnel.marriage.delete')}
        onCancel={() => setDeleting(false)}
        onConfirm={async () => {
          try {
            await marriageApi.remove(personId);
            setDeleting(false);
            await refresh();
          } catch (error) {
            setDeleting(false);
            setDeleteError(error instanceof ApiError ? error.message : t('error.generic'));
          }
        }}
      />
    </Grid>
  );
}

interface DialogProps {
  personId: string;
  marriage: Marriage;
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}

/** Evlilik bilgisi formu: iki alan da zorunludur. Tarih kurallari sunucudadir. */
function MarriageDialog({ personId, marriage, onClose, onSaved }: DialogProps) {
  const { t } = useTranslation();
  const [date, setDate] = useState<Date | null>(
    marriage.marriageDate ? parseISO(marriage.marriageDate) : null,
  );
  const [allowance, setAllowance] = useState<'yes' | 'no' | ''>(
    marriage.allowanceReceived === null ? '' : marriage.allowanceReceived ? 'yes' : 'no',
  );
  const [missing, setMissing] = useState<Set<FieldName>>(new Set());

  const save = useMutation({
    mutationFn: (values: { date: Date; allowance: boolean }) =>
      marriageApi.set(personId, format(values.date, 'yyyy-MM-dd'), values.allowance),
    onSuccess: onSaved,
  });

  const serverErrors = save.error instanceof ApiError ? save.error.errors : undefined;
  const fieldError = (field: FieldName): string | undefined =>
    missing.has(field) ? t('personnel.marriage.required') : serverErrors?.[field]?.[0];
  const generalError =
    save.error && !serverErrors?.marriageDate && !serverErrors?.allowanceReceived
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
    <Dialog open onClose={onClose} aria-labelledby="marriage-dialog-title" fullWidth maxWidth="sm">
      <Box
        component="form"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          const empty = new Set<FieldName>();
          if (!date || Number.isNaN(date.getTime())) {
            empty.add('marriageDate');
          }
          if (allowance === '') {
            empty.add('allowanceReceived');
          }
          setMissing(empty);
          if (empty.size === 0 && date) {
            save.mutate({ date, allowance: allowance === 'yes' });
          }
        }}
      >
        <DialogTitle id="marriage-dialog-title">{t('personnel.marriage.title')}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            <DatePicker
              label={t('personnel.marriage.fields.marriageDate')}
              value={date}
              onChange={(value) => {
                setDate(value);
                clearMissing('marriageDate');
              }}
              format="dd.MM.yyyy"
              disableFuture
              slotProps={{
                textField: {
                  required: true,
                  error: Boolean(fieldError('marriageDate')),
                  helperText: fieldError('marriageDate'),
                },
              }}
            />

            <FormControl required error={Boolean(fieldError('allowanceReceived'))}>
              <FormLabel id="marriage-allowance-label">
                {t('personnel.marriage.fields.allowanceReceived')}
              </FormLabel>
              <RadioGroup
                row
                aria-labelledby="marriage-allowance-label"
                value={allowance}
                onChange={(event) => {
                  setAllowance(event.target.value as 'yes' | 'no');
                  clearMissing('allowanceReceived');
                }}
              >
                <FormControlLabel
                  value="yes"
                  control={<Radio />}
                  label={t('personnel.marriage.received')}
                />
                <FormControlLabel
                  value="no"
                  control={<Radio />}
                  label={t('personnel.marriage.notReceived')}
                />
              </RadioGroup>
              {fieldError('allowanceReceived') ? (
                <FormHelperText>{fieldError('allowanceReceived')}</FormHelperText>
              ) : null}
            </FormControl>

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
            {t('personnel.marriage.save')}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}
