import { Alert } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/shared/api/queryKeys';
import { personsApi } from '../api/personsApi';
import { CardContent } from './PersonCardPage';

/**
 * "Profilim" (SYG-PERS-044): her kullanici, rolu ve izni ne olursa olsun kendi kartini acar. Kart,
 * kisinin kendisi icin gecerli gorunurluk kurallariyla gelir (adres gorunur, TCKN maskeli).
 */
export function ProfilePage() {
  const { t } = useTranslation();
  const card = useQuery({ queryKey: queryKeys.personnel.me, queryFn: personsApi.me });

  return (
    <CardContent
      card={card}
      notFound={<Alert severity="warning">{t('personnel.card.profileMissing')}</Alert>}
    />
  );
}
