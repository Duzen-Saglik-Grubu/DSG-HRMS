import { Button, Chip, Stack, Typography } from '@mui/material';
import CancelRounded from '@mui/icons-material/CancelRounded';
import { useTranslation } from 'react-i18next';
import type { PersonFilterOptions } from '../api/personsApi';
import { filterTags } from '../filterTags';

interface FilterChipsProps {
  params: URLSearchParams;
  options: PersonFilterOptions | undefined;
  onChange: (next: URLSearchParams) => void;
  onClear: () => void;
}

/**
 * Secili suzgecler gorunur ve tek tusla temizlenir (PG-PERS-15): kullanici hangi suzgecin acik
 * oldugunu gormezse "kayit gelmiyor" sanir (ADR-0015 §6).
 */
export function FilterChips({ params, options, onChange, onClear }: FilterChipsProps) {
  const { t } = useTranslation();
  const tags = filterTags(params, options, t);

  if (tags.length === 0) {
    return null;
  }

  return (
    <Stack
      direction="row"
      spacing={1}
      useFlexGap
      sx={{ flexWrap: 'wrap', alignItems: 'center' }}
      role="group"
      aria-label={t('personnel.list.activeFilters')}
    >
      <Typography variant="body2" color="text.secondary">
        {t('personnel.list.activeFilters')}:
      </Typography>
      {tags.map((tag) => (
        <Chip
          key={tag.key}
          label={tag.label}
          size="small"
          onDelete={() => onChange(tag.remove(params))}
          deleteIcon={
            <CancelRounded aria-label={t('personnel.list.removeFilter', { label: tag.label })} />
          }
        />
      ))}
      <Button size="small" onClick={onClear}>
        {t('personnel.list.clear')}
      </Button>
    </Stack>
  );
}
