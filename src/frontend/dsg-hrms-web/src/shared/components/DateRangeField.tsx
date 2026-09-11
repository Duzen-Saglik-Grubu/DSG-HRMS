import { Stack } from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { useTranslation } from 'react-i18next';

export interface DateRange {
  start: Date | null;
  end: Date | null;
}

interface DateRangeFieldProps {
  value: DateRange;
  onChange: (value: DateRange) => void;
  startLabel?: string;
  endLabel?: string;
  disabled?: boolean;
  /** Alan bazli sunucu hatasi (ADR-0010 §8). */
  error?: string | undefined;
}

/**
 * Tarih araligi secimi (ADR-0015 §5).
 *
 * <b>Neden iki ayri DatePicker?</b> MUI'nin hazir <c>DateRangePicker</c> bileseni
 * yalnizca ucretli (Pro) pakette bulunur. Kosullu ticari lisansli bilesen
 * kullanmama ilkesi geregi (KR-025) iki adet ucretsiz <c>DatePicker</c>
 * birlestirilerek ayni islev elde edilir.
 *
 * Bitis tarihi, baslangictan ONCE secilemez: izin talebi gibi formlarda bu hata
 * sik yapilir ve sunucuya gitmeden burada engellenir.
 */
export function DateRangeField({
  value,
  onChange,
  startLabel,
  endLabel,
  disabled = false,
  error,
}: DateRangeFieldProps) {
  const { t } = useTranslation();

  // Bitis tarihi baslangictan ONCE secilemez ve tersi de gecerlidir; izin talebi
  // gibi formlarda bu hata sik yapilir ve sunucuya gitmeden burada engellenir.
  //
  // "exactOptionalPropertyTypes" acik oldugu icin sinir yokken ozellik
  // "undefined" olarak DEGIL, hic verilmeyerek gecilir.
  const ustSinir = value.end ? { maxDate: value.end } : {};
  const altSinir = value.start ? { minDate: value.start } : {};

  return (
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
      <DatePicker
        label={startLabel ?? t('tarih.baslangic')}
        value={value.start}
        onChange={(yeni) => onChange({ ...value, start: yeni })}
        disabled={disabled}
        {...ustSinir}
        slotProps={{
          textField: {
            error: Boolean(error),
            helperText: error,
          },
        }}
      />

      <DatePicker
        label={endLabel ?? t('tarih.bitis')}
        value={value.end}
        onChange={(yeni) => onChange({ ...value, end: yeni })}
        disabled={disabled}
        {...altSinir}
        slotProps={{
          textField: {
            error: Boolean(error),
          },
        }}
      />
    </Stack>
  );
}
