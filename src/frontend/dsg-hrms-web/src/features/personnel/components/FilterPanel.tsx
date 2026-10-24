import { useState } from 'react';
import {
  Autocomplete,
  Box,
  Button,
  Divider,
  Drawer,
  IconButton,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import CloseRounded from '@mui/icons-material/CloseRounded';
import { useTranslation } from 'react-i18next';
import { DateRangeField } from '@/shared/components/DateRangeField';
import type { FilterOption, PersonFilterOptions } from '../api/personsApi';
import {
  DATE_FILTERS,
  FIXED_VALUES,
  OPTION_SOURCE,
  RANGE_FILTERS,
  TEXT_FILTERS,
  formatDate,
  parseDate,
  withChanges,
  type MultiFilter,
  type RangeFilter,
  type TextFilter,
} from '../listState';

const ORGANIZATION: MultiFilter[] = [
  'companyId',
  'city',
  'branchId',
  'departmentId',
  'titleCode',
  'dutyId',
  'statusCode',
  'occupationCode',
];
const PERSONAL: MultiFilter[] = ['gender', 'maritalStatus', 'education'];

/** Metin suzgeclerinin girdi siniri; sunucu da 100 karakterde keser. */
const TEXT_MAX_LENGTH = 100;

interface FilterPanelProps {
  open: boolean;
  params: URLSearchParams;
  options: PersonFilterOptions | undefined;
  onClose: () => void;
  onApply: (next: URLSearchParams) => void;
}

/**
 * Suzgec paneli (SYG-PERS-024). Degisiklikler "Uygula" ile listeye gecer; her tusta istek gitmez.
 * Adres ve TCKN suzgeci yalnizca bu alanlari gorebilen kullaniciya sunulur (SYG-PERS-026): sunucu
 * secenekler yanitinda bunu soyler, izinsiz istegi de reddeder.
 */
export function FilterPanel({ open, params, options, onClose, onApply }: FilterPanelProps) {
  return (
    <Drawer anchor="right" open={open} onClose={onClose}>
      {open ? (
        <PanelBody params={params} options={options} onClose={onClose} onApply={onApply} />
      ) : null}
    </Drawer>
  );
}

function PanelBody({ params, options, onClose, onApply }: Omit<FilterPanelProps, 'open'>) {
  const { t } = useTranslation();
  // Panel her acilista adresteki durumdan baslar; "Kapat" degisiklikleri atar.
  const [draft, setDraft] = useState(() => new URLSearchParams(params));
  const change = (changes: Record<string, string | string[] | null>) =>
    setDraft((current) => withChanges(current, changes));

  const visibleText = TEXT_FILTERS.filter(
    (name) =>
      (name !== 'nationalId' || options?.canFilterByNationalId === true) &&
      (name !== 'address' || options?.canFilterBySensitive === true),
  );
  const rangeInvalid = RANGE_FILTERS.some((name) => {
    const min = draft.get(`${name}Min`);
    const max = draft.get(`${name}Max`);
    return min !== null && max !== null && Number(min) > Number(max);
  });

  return (
    <Box
      component="form"
      role="dialog"
      aria-labelledby="filter-panel-title"
      noValidate
      sx={{
        width: { xs: '100vw', sm: 440 },
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
      }}
      onSubmit={(event) => {
        event.preventDefault();
        if (!rangeInvalid) {
          onApply(draft);
        }
      }}
    >
      <Stack direction="row" sx={{ alignItems: 'center', px: 2, py: 1.5 }}>
        <Typography id="filter-panel-title" variant="h6" component="h2" sx={{ flexGrow: 1 }}>
          {t('personnel.list.panel.title')}
        </Typography>
        <IconButton aria-label={t('personnel.list.panel.close')} onClick={onClose}>
          <CloseRounded />
        </IconButton>
      </Stack>
      <Divider />

      <Stack spacing={3} sx={{ p: 2, overflowY: 'auto', flexGrow: 1 }}>
        <Section title={t('personnel.list.panel.groups.organization')}>
          {ORGANIZATION.map((name) => (
            <MultiSelect key={name} name={name} draft={draft} options={options} onChange={change} />
          ))}
        </Section>

        <Section title={t('personnel.list.panel.groups.personal')}>
          {PERSONAL.map((name) => (
            <MultiSelect key={name} name={name} draft={draft} options={options} onChange={change} />
          ))}
        </Section>

        <Section title={t('personnel.list.panel.groups.text')}>
          {visibleText.map((name) => (
            <TextFilterField key={name} name={name} draft={draft} onChange={change} />
          ))}
        </Section>

        <Section title={t('personnel.list.panel.groups.dates')}>
          {DATE_FILTERS.map((name) => (
            <Stack key={name} spacing={1}>
              <Typography variant="body2">{t(`personnel.list.fields.${name}`)}</Typography>
              <DateRangeField
                value={{
                  start: parseDate(draft.get(`${name}From`)),
                  end: parseDate(draft.get(`${name}To`)),
                }}
                onChange={(range) =>
                  change({
                    [`${name}From`]: formatDate(range.start),
                    [`${name}To`]: formatDate(range.end),
                  })
                }
              />
            </Stack>
          ))}
        </Section>

        <Section title={t('personnel.list.panel.groups.ranges')}>
          {RANGE_FILTERS.map((name) => (
            <RangeField key={name} name={name} draft={draft} onChange={change} />
          ))}
        </Section>
      </Stack>

      <Divider />
      <Stack direction="row" spacing={1} sx={{ p: 2, justifyContent: 'flex-end' }}>
        <Button onClick={onClose}>{t('personnel.list.panel.close')}</Button>
        <Button type="submit" variant="contained" disabled={rangeInvalid}>
          {t('personnel.list.panel.apply')}
        </Button>
      </Stack>
    </Box>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <Stack spacing={1.5} component="section" aria-label={title}>
      <Typography variant="subtitle2" color="text.secondary">
        {title}
      </Typography>
      {children}
    </Stack>
  );
}

interface FieldProps<TName extends string> {
  name: TName;
  draft: URLSearchParams;
  onChange: (changes: Record<string, string | string[] | null>) => void;
}

/** Secim listesi; birden fazla deger secilebilir (PG-PERS-14). */
function MultiSelect({
  name,
  draft,
  options,
  onChange,
}: FieldProps<MultiFilter> & { options: PersonFilterOptions | undefined }) {
  const { t } = useTranslation();
  const choices: FilterOption[] =
    name === 'gender' || name === 'maritalStatus'
      ? FIXED_VALUES[name].map((value) => ({
          value,
          label: t(`personnel.list.values.${name}.${value}`),
        }))
      : (options?.[OPTION_SOURCE[name]] ?? []);
  // Adresteki deger seceneklerde yoksa (ornegin tanim pasife alindi) yine de gorunur ve kaldirilabilir.
  const selected = draft
    .getAll(name)
    .map((value) => choices.find((choice) => choice.value === value) ?? { value, label: value });

  return (
    <Autocomplete
      multiple
      size="small"
      options={choices}
      value={selected}
      getOptionLabel={(option) => option.label}
      isOptionEqualToValue={(option, value) => option.value === value.value}
      filterSelectedOptions
      noOptionsText={t('personnel.list.panel.noOptions')}
      onChange={(_, values) => onChange({ [name]: values.map((value) => value.value) })}
      renderInput={(input) => <TextField {...input} label={t(`personnel.list.fields.${name}`)} />}
    />
  );
}

function TextFilterField({ name, draft, onChange }: FieldProps<TextFilter>) {
  const { t } = useTranslation();

  return (
    <TextField
      size="small"
      label={t(`personnel.list.fields.${name}`)}
      value={draft.get(name) ?? ''}
      onChange={(event) => onChange({ [name]: event.target.value })}
      slotProps={{ htmlInput: { maxLength: TEXT_MAX_LENGTH } }}
    />
  );
}

/** Yil araligi; ikisi de bos birakilabilir. */
function RangeField({ name, draft, onChange }: FieldProps<RangeFilter>) {
  const { t } = useTranslation();
  const min = draft.get(`${name}Min`) ?? '';
  const max = draft.get(`${name}Max`) ?? '';
  const invalid = min !== '' && max !== '' && Number(min) > Number(max);
  const label = t(`personnel.list.fields.${name}`);
  const limit = name === 'age' ? 120 : 80;

  return (
    <Stack spacing={1}>
      <Typography variant="body2">
        {label} ({t('personnel.list.panel.years')})
      </Typography>
      <Stack direction="row" spacing={2}>
        <TextField
          size="small"
          type="number"
          label={`${label} — ${t('personnel.list.panel.min')}`}
          value={min}
          onChange={(event) => onChange({ [`${name}Min`]: event.target.value })}
          error={invalid}
          helperText={invalid ? t('personnel.list.panel.rangeError') : undefined}
          slotProps={{ htmlInput: { min: 0, max: limit, inputMode: 'numeric' } }}
        />
        <TextField
          size="small"
          type="number"
          label={`${label} — ${t('personnel.list.panel.max')}`}
          value={max}
          onChange={(event) => onChange({ [`${name}Max`]: event.target.value })}
          error={invalid}
          slotProps={{ htmlInput: { min: 0, max: limit, inputMode: 'numeric' } }}
        />
      </Stack>
    </Stack>
  );
}
