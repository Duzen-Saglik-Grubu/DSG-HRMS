import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { CORRECTION_FIELDS, correctionsApi, type CorrectionField } from '../api/correctionsApi';

/** Adres ve egitimin en buyuk uzunlugu (SYG-PERS-045). */
const MAX_TEXT = 500;

interface ReportToHrDialogProps {
  onClose: () => void;
  onSent: () => void;
}

/**
 * "IK'ya bildir" formu (PG-PERS-37; SYG-PERS-045). Kisi alani secer, dogru degeri yazar ve
 * gonderir. Acik talebi olan alan secilemez (AN-08). Deger sunucuda alana gore dogrulanir;
 * hata ilgili alanin altinda gorunur.
 */
export function ReportToHrDialog({ onClose, onSent }: ReportToHrDialogProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [field, setField] = useState<CorrectionField | ''>('');
  const [value, setValue] = useState('');
  const [missing, setMissing] = useState<'field' | 'value' | null>(null);

  const state = useQuery({
    queryKey: queryKeys.personnel.ownCorrections,
    queryFn: correctionsApi.own,
  });
  const openFields = new Set(state.data?.openFields ?? []);

  const send = useMutation({
    mutationFn: () => correctionsApi.submit(field as CorrectionField, value.trim()),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.personnel.ownCorrections });
      onSent();
    },
  });

  const fieldError = send.error instanceof ApiError ? send.error.errors?.['value']?.[0] : undefined;
  const serverError =
    send.error && !fieldError
      ? send.error instanceof ApiError
        ? send.error.message
        : t('error.generic')
      : null;

  const valueError =
    missing === 'value' ? t('personnel.report.valueRequired') : (fieldError ?? null);

  return (
    <Dialog open onClose={onClose} aria-labelledby="report-dialog-title" fullWidth maxWidth="sm">
      <Box
        component="form"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          if (!field) {
            setMissing('field');
            return;
          }

          if (!value.trim()) {
            setMissing('value');
            return;
          }

          setMissing(null);
          send.mutate();
        }}
      >
        <DialogTitle id="report-dialog-title">{t('personnel.report.title')}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5}>
            <DialogContentText>{t('personnel.report.text')}</DialogContentText>

            <TextField
              select
              label={t('personnel.report.field')}
              value={field}
              onChange={(event) => {
                setField(event.target.value as CorrectionField);
                setValue('');
                send.reset();
              }}
              required
              error={missing === 'field'}
              helperText={missing === 'field' ? t('personnel.report.fieldRequired') : undefined}
            >
              {CORRECTION_FIELDS.map((option) => (
                <MenuItem key={option} value={option} disabled={openFields.has(option)}>
                  {t(`personnel.report.fields.${option}`)}
                  {openFields.has(option) ? ` — ${t('personnel.report.pending')}` : ''}
                </MenuItem>
              ))}
            </TextField>

            {field ? (
              <ValueInput
                field={field}
                value={value}
                onChange={(next) => {
                  setValue(next);
                  if (missing === 'value') {
                    setMissing(null);
                  }
                }}
                error={valueError}
                bloodGroups={state.data?.bloodGroups ?? []}
              />
            ) : null}

            {serverError ? (
              <Alert severity="error" role="alert">
                {serverError}
              </Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>{t('confirm.cancel')}</Button>
          <Button type="submit" variant="contained" disabled={send.isPending}>
            {t('personnel.report.send')}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

interface ValueInputProps {
  field: CorrectionField;
  value: string;
  onChange: (value: string) => void;
  error: string | null;
  bloodGroups: { value: string; label: string }[];
}

/** Alana gore deger girisi: secim listesi, telefon, e-posta veya serbest metin. */
function ValueInput({ field, value, onChange, error, bloodGroups }: ValueInputProps) {
  const { t } = useTranslation();
  const common = {
    label: t('personnel.report.value'),
    value,
    onChange: (event: React.ChangeEvent<HTMLInputElement>) => onChange(event.target.value),
    required: true,
    error: Boolean(error),
    helperText: error ?? t(`personnel.report.hints.${field}`),
  };

  if (field === 'maritalStatus' || field === 'bloodGroup') {
    const options =
      field === 'maritalStatus'
        ? [
            { value: 'married', label: t('personnel.report.marital.married') },
            { value: 'notMarried', label: t('personnel.report.marital.notMarried') },
          ]
        : bloodGroups;

    return (
      <TextField select {...common}>
        {options.map((option) => (
          <MenuItem key={option.value} value={option.value}>
            {option.label}
          </MenuItem>
        ))}
      </TextField>
    );
  }

  const multiline = field === 'address' || field === 'education';
  return (
    <TextField
      {...common}
      autoFocus
      multiline={multiline}
      minRows={multiline ? 2 : undefined}
      type={field === 'email' ? 'email' : field === 'mobilePhone' ? 'tel' : 'text'}
      slotProps={{
        htmlInput: {
          maxLength: multiline ? MAX_TEXT : 254,
          autoComplete: field === 'email' ? 'email' : field === 'mobilePhone' ? 'tel' : 'off',
        },
      }}
    />
  );
}
