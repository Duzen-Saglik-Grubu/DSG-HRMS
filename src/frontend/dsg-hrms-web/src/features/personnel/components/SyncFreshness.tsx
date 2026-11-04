import { Stack, Typography } from '@mui/material';
import SyncRounded from '@mui/icons-material/SyncRounded';
import WarningAmberRounded from '@mui/icons-material/WarningAmberRounded';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/shared/api/queryKeys';
import { personsApi } from '../api/personsApi';

/**
 * "Son senkronizasyon" (SYG-PERS-010, PG-PERS-07): kullanici gordugu verinin ne kadar guncel
 * oldugunu bilir. Zaman son BASARILI calismanin bitisidir; son calisma basarisizsa yaninda uyari
 * yazar. Senkronizasyon bu ortamda tanimli degilse veya bilgi alinamazsa hicbir sey gosterilmez:
 * bu bilgi ekranin asil icerigini engellememelidir.
 */
export function SyncFreshness() {
  const { t } = useTranslation();
  const freshness = useQuery({
    queryKey: queryKeys.personnel.syncFreshness,
    queryFn: personsApi.syncFreshness,
    staleTime: 60_000,
  });

  const data = freshness.data;
  if (!data?.enabled) {
    return null;
  }

  const when = data.lastSucceededAt
    ? format(new Date(data.lastSucceededAt), 'dd.MM.yyyy HH:mm')
    : t('personnel.freshness.never');

  return (
    <Stack
      direction="row"
      spacing={0.75}
      useFlexGap
      sx={{ alignItems: 'center', flexWrap: 'wrap', color: 'text.secondary' }}
    >
      <SyncRounded aria-hidden fontSize="small" />
      <Typography variant="body2">
        {t('personnel.freshness.label')}: <strong>{when}</strong>
      </Typography>
      {data.lastRunFailed ? (
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', color: 'warning.dark' }}>
          <WarningAmberRounded aria-hidden fontSize="small" />
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            {t('personnel.freshness.lastRunFailed')}
          </Typography>
        </Stack>
      ) : null}
    </Stack>
  );
}
