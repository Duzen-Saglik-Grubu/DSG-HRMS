import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Skeleton,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { saveFile } from '@/shared/utils/saveFile';
import {
  personsApi,
  type ExportField,
  type ExportFieldName,
  type ExportFormat,
} from '../api/personsApi';

/** Tek aktarimdaki en fazla kisi (ADR-0014 §5); sunucu da ayni siniri uygular. */
export const EXPORT_ROW_LIMIT = 50_000;

/** Son secimin tarayicida tutuldugu anahtar (yalnizca bu kullanicinin kolayligi). */
const CHOICE_KEY = 'dsg-hrms.person-export';

/** Ilk aktarimda secili gelen alanlar. */
const DEFAULT_FIELDS: readonly ExportFieldName[] = [
  'registryCode',
  'company',
  'branch',
  'department',
  'title',
  'duty',
];

interface Choice {
  format: ExportFormat;
  fields: ExportFieldName[];
}

function readChoice(): Choice {
  try {
    const raw = localStorage.getItem(CHOICE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as Partial<Choice>;
      if ((parsed.format === 'excel' || parsed.format === 'pdf') && Array.isArray(parsed.fields)) {
        return { format: parsed.format, fields: parsed.fields };
      }
    }
  } catch {
    // Tarayici depolamaya izin vermiyorsa varsayilan secim kullanilir.
  }

  return { format: 'excel', fields: [...DEFAULT_FIELDS] };
}

function writeChoice(choice: Choice): void {
  try {
    localStorage.setItem(CHOICE_KEY, JSON.stringify(choice));
  } catch {
    // Tercih yalnizca bu oturumda gecerli kalir.
  }
}

interface ExportDialogProps {
  /** Listenin API parametreleri (arama, durum, siralama, suzgecler). */
  listParams: URLSearchParams;
  /** Suzgece uyan kisi sayisi. */
  totalCount: number;
  onClose: () => void;
}

/**
 * Personel listesini disa aktarma (PG-PERS-29, 42; SYG-PERS-032). Kullanici bicimi ve alanlari
 * secer; suzgece uyan BUTUN kisiler aktarilir. Secilebilir alanlar sunucudan gelir: kullanici
 * goremedigi alani secemez. Ad ve soyad her satirda vardir.
 */
export function ExportDialog({ listParams, totalCount, onClose }: ExportDialogProps) {
  const { t } = useTranslation();
  const [choice, setChoice] = useState<Choice>(readChoice);

  const fields = useQuery({
    queryKey: queryKeys.personnel.exportFields,
    queryFn: personsApi.exportFields,
    staleTime: 5 * 60_000,
  });

  // Sunucunun artik sunmadigi alan (izin degisti) secimden duser; fotograf yalnizca PDF'te.
  const offered = fields.data ?? [];
  const usable = (field: ExportField) => !field.pdfOnly || choice.format === 'pdf';
  const selected = offered
    .filter((field) => usable(field) && choice.fields.includes(field.field))
    .map((field) => field.field);

  const download = useMutation({
    mutationFn: () => personsApi.exportFile(listParams, choice.format, selected),
    onSuccess: (file) => {
      writeChoice({ format: choice.format, fields: selected });
      saveFile(file.blob, file.fileName);
      onClose();
    },
  });

  const toggle = (name: ExportFieldName, checked: boolean) => {
    download.reset();
    setChoice((current) => ({
      ...current,
      fields: checked
        ? [...current.fields.filter((f) => f !== name), name]
        : current.fields.filter((f) => f !== name),
    }));
  };

  const tooMany = totalCount > EXPORT_ROW_LIMIT;
  const empty = totalCount === 0;
  const error = download.error
    ? download.error instanceof ApiError
      ? download.error.message
      : t('error.generic')
    : null;

  return (
    <Dialog open onClose={onClose} aria-labelledby="export-dialog-title" fullWidth maxWidth="md">
      <DialogTitle id="export-dialog-title">{t('personnel.export.title')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2.5}>
          <Typography>
            {empty
              ? t('personnel.export.none')
              : t('personnel.export.count', {
                  count: totalCount,
                  value: totalCount.toLocaleString('tr-TR'),
                })}
          </Typography>

          <Box>
            <Typography id="export-format-label" variant="subtitle2" component="p" sx={{ mb: 1 }}>
              {t('personnel.export.format')}
            </Typography>
            <ToggleButtonGroup
              exclusive
              size="small"
              color="primary"
              value={choice.format}
              aria-labelledby="export-format-label"
              onChange={(_, value: ExportFormat | null) => {
                if (value) {
                  download.reset();
                  setChoice((current) => ({ ...current, format: value }));
                }
              }}
            >
              <ToggleButton value="excel">{t('personnel.export.excel')}</ToggleButton>
              <ToggleButton value="pdf">{t('personnel.export.pdf')}</ToggleButton>
            </ToggleButtonGroup>
          </Box>

          <Box role="group" aria-labelledby="export-fields-label">
            {/* Dar ekranda dugmeler basligin altina iner; "Tumunu sec" bolunmez. */}
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={{ xs: 0.5, sm: 1 }}
              sx={{
                alignItems: { xs: 'flex-start', sm: 'center' },
                justifyContent: 'space-between',
                mb: 0.5,
                '& .MuiButton-root': { whiteSpace: 'nowrap' },
              }}
            >
              <Typography id="export-fields-label" variant="subtitle2" component="p">
                {t('personnel.export.fields')}
              </Typography>
              <Stack direction="row" spacing={0.5} sx={{ ml: { xs: -1, sm: 0 } }}>
                <Button
                  size="small"
                  onClick={() => {
                    download.reset();
                    setChoice((current) => ({
                      ...current,
                      fields: offered.filter(usable).map((f) => f.field),
                    }));
                  }}
                  disabled={offered.length === 0}
                >
                  {t('personnel.export.selectAll')}
                </Button>
                <Button
                  size="small"
                  onClick={() => {
                    download.reset();
                    setChoice((current) => ({ ...current, fields: [] }));
                  }}
                  disabled={selected.length === 0}
                >
                  {t('personnel.export.clear')}
                </Button>
              </Stack>
            </Stack>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              {t('personnel.export.alwaysIncluded')}
            </Typography>

            {fields.isPending ? (
              <Skeleton variant="rounded" height={160} />
            ) : fields.isError ? (
              <Alert severity="error" role="alert">
                {fields.error instanceof ApiError ? fields.error.message : t('error.generic')}
              </Alert>
            ) : (
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: {
                    xs: 'minmax(0, 1fr)',
                    sm: 'repeat(2, minmax(0, 1fr))',
                    md: 'repeat(3, minmax(0, 1fr))',
                  },
                  columnGap: 2,
                }}
              >
                {offered.map((field) => (
                  <FormControlLabel
                    key={field.field}
                    disabled={!usable(field)}
                    control={
                      <Checkbox
                        size="small"
                        checked={selected.includes(field.field)}
                        onChange={(event) => toggle(field.field, event.target.checked)}
                      />
                    }
                    label={
                      field.pdfOnly
                        ? `${field.label} (${t('personnel.export.pdfOnly')})`
                        : field.label
                    }
                  />
                ))}
              </Box>
            )}
          </Box>

          {/* Duragan bilgi: ekran okuyucu uyari gibi okumasin. */}
          <Alert severity="info" role="note">
            {t('personnel.export.notice')}
          </Alert>

          {tooMany ? (
            <Alert severity="warning" role="alert">
              {t('personnel.export.tooMany', {
                limit: EXPORT_ROW_LIMIT.toLocaleString('tr-TR'),
              })}
            </Alert>
          ) : null}

          {error ? (
            <Alert severity="error" role="alert">
              {error}
            </Alert>
          ) : null}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('confirm.cancel')}</Button>
        <Button
          variant="contained"
          onClick={() => download.mutate()}
          disabled={download.isPending || tooMany || empty || fields.isPending || fields.isError}
        >
          {download.isPending ? t('personnel.export.preparing') : t('personnel.export.download')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
