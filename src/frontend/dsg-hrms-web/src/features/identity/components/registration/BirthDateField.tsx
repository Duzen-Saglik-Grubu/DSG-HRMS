import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { useTranslation } from 'react-i18next';
import { MIN_BIRTH_DATE } from '../../utils/birthDate';

interface BirthDateFieldProps {
  value: Date | null;
  onChange: (value: Date | null) => void;
  onBlur?: () => void;
  error?: string | undefined;
}

/**
 * Dogum tarihi: elle yazilabilir (GG.AA.YYYY) veya takvimden secilebilir.
 *
 * Takvim YIL gorunumuyle acilir: dogum tarihine gunden gune ilerleyerek ulasmak
 * yorucu olurdu. Gelecekteki ve 1900 oncesi tarihler secilemez. Tarih bicimi ve gun/ay
 * adlari uygulama genelindeki Turkce ayardan gelir (AppProviders).
 */
export function BirthDateField({ value, onChange, onBlur, error }: BirthDateFieldProps) {
  const { t } = useTranslation();

  return (
    <DatePicker
      label={t('identity.registration.identity.birthDate')}
      value={value}
      onChange={onChange}
      format="dd.MM.yyyy"
      openTo="year"
      views={['year', 'month', 'day']}
      disableFuture
      minDate={MIN_BIRTH_DATE}
      slotProps={{
        textField: {
          fullWidth: true,
          ...(onBlur ? { onBlur } : {}),
          error: Boolean(error),
          helperText: error ?? t('identity.registration.identity.birthDateHint'),
        },
        openPickerButton: { 'aria-label': t('identity.registration.identity.birthDatePicker') },
      }}
    />
  );
}
