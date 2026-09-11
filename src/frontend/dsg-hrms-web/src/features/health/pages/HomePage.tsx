import { Box, Chip, CircularProgress, Paper, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { fetchHealth } from '@/shared/api/health';
import { queryKeys } from '@/shared/api/queryKeys';
import { ErrorState } from '@/shared/components/ErrorState';
import { ApiError } from '@/shared/api/problemDetails';

/**
 * Ana sayfa - iskelet asamasinin uctan uca dogrulamasi.
 *
 * Bu ekran bilincli olarak sadedir: amaci arayuz gostermek degil, zincirin
 * (tarayici -> Vite vekili -> API -> PostgreSQL) gercekten calistigini
 * gostermektir. Ilk is modulu geldiginde yerini gercek panoya birakacaktir.
 */
export function HomePage() {
  const { t } = useTranslation();

  const { data, error, isPending, refetch } = useQuery({
    queryKey: queryKeys.saglik,
    queryFn: fetchHealth,
  });

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h5" component="h2" gutterBottom>
          {t('uygulama.aciklama')}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {t('uygulama.kurum')}
        </Typography>
      </Box>

      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" component="h3" gutterBottom>
          {t('saglik.baslik')}
        </Typography>

        {isPending ? (
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <CircularProgress size={20} />
            <Typography variant="body2">{t('saglik.kontrolEdiliyor')}</Typography>
          </Stack>
        ) : null}

        {error ? (
          <ErrorState error={error as unknown as ApiError} onRetry={() => void refetch()} />
        ) : null}

        {data ? (
          <Stack spacing={2}>
            <Typography variant="body1">{t('saglik.calisiyor')}</Typography>

            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }} useFlexGap>
              {data.checks.map((check) => (
                <Chip
                  key={check.name}
                  label={`${check.name}: ${check.status}`}
                  color={check.status === 'Healthy' ? 'success' : 'error'}
                  variant="outlined"
                  size="small"
                />
              ))}
            </Stack>
          </Stack>
        ) : null}
      </Paper>
    </Stack>
  );
}
