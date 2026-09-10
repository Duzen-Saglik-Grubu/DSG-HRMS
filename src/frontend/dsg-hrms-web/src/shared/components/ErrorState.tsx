import { Alert, AlertTitle, Box, Button, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';

interface ErrorStateProps {
  error: ApiError;
  onRetry?: () => void;
}

/**
 * Hata durumu gosterimi (ADR-0015 §5).
 *
 * Iki sey yapar ve ikisi de bilinclidir:
 *
 * 1. Kullaniciya NE YAPACAGINI soyler. "Bir hata olustu" mesaji kullaniciyi
 *    caresiz birakir; burada her zaman bir eylem (tekrar dene) sunulur.
 * 2. TAKIP NUMARASINI gosterir. Kullanici bu numarayi destek talebinde iletir
 *    ve kayit dogrudan bulunur (ADR-0009 §2). Numara olmadan "saat 14:20'de
 *    hata aldim" bilgisiyle arama yapmak gerekirdi.
 */
export function ErrorState({ error, onRetry }: ErrorStateProps) {
  const { t } = useTranslation();

  return (
    <Alert
      severity="error"
      action={
        onRetry ? (
          <Button color="inherit" size="small" onClick={onRetry}>
            {t('hata.tekrarDene')}
          </Button>
        ) : undefined
      }
    >
      <AlertTitle>{t('hata.baslik')}</AlertTitle>

      <Typography variant="body2">{error.message}</Typography>

      {error.traceId ? (
        <Box sx={{ mt: 1 }}>
          <Typography variant="caption" component="div" color="text.secondary">
            {t('hata.takipAciklama')}
          </Typography>
          <Typography variant="caption" component="code" sx={{ fontFamily: 'monospace' }}>
            {t('hata.takipNumarasi')}: {error.traceId}
          </Typography>
        </Box>
      ) : null}
    </Alert>
  );
}
