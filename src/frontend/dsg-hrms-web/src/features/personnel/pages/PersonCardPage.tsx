import { Button, Skeleton, Stack } from '@mui/material';
import ArrowBackRounded from '@mui/icons-material/ArrowBackRounded';
import PersonSearchOutlined from '@mui/icons-material/PersonSearchOutlined';
import { useQuery, type UseQueryResult } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink, useLocation, useParams } from 'react-router';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { EmptyState } from '@/shared/components/EmptyState';
import { ErrorState } from '@/shared/components/ErrorState';
import { routes } from '@/shared/routes';
import { personsApi, type PersonCard } from '../api/personsApi';
import { PersonProfile } from '../components/PersonProfile';

/** Listeden gelindiyse listenin adres parametreleri; geri donuste liste ayni acilir. */
export interface CardLocationState {
  listSearch?: string;
}

/**
 * Listeden acilan kisi karti (SYG-PERS-034). Kapsam disindaki ve var olmayan kisi ayni
 * "bulunamadi" ekranini gorur; sunucu ikisini ayirt etmez (ADR-0019 §1).
 */
export function PersonCardPage() {
  const { t } = useTranslation();
  const { personId = '' } = useParams();
  const location = useLocation();
  const listSearch = (location.state as CardLocationState | null)?.listSearch ?? '';
  const card = useQuery({
    queryKey: queryKeys.personnel.card(personId),
    queryFn: () => personsApi.card(personId),
  });

  return (
    <Stack spacing={2}>
      <Button
        component={RouterLink}
        to={`${routes.personnel}${listSearch}`}
        startIcon={<ArrowBackRounded />}
        sx={{ alignSelf: 'flex-start' }}
      >
        {t('personnel.card.backToList')}
      </Button>
      <CardContent
        card={card}
        notFound={
          <EmptyState
            icon={<PersonSearchOutlined fontSize="large" />}
            title={t('personnel.card.notFound')}
            description={t('personnel.card.notFoundText')}
          />
        }
      />
    </Stack>
  );
}

/** Kartin yukleniyor, bulunamadi, hata ve dolu halleri (ADR-0015 §6). */
export function CardContent({
  card,
  notFound,
}: {
  card: UseQueryResult<PersonCard>;
  notFound: React.ReactNode;
}) {
  const { t } = useTranslation();

  if (card.isPending) {
    return (
      <Stack spacing={2.5} aria-busy="true">
        <Skeleton variant="rounded" height={150} />
        <Skeleton variant="rounded" height={320} />
      </Stack>
    );
  }

  if (card.error) {
    if (card.error instanceof ApiError && card.error.status === 404) {
      return <>{notFound}</>;
    }

    return (
      <ErrorState
        error={
          card.error instanceof ApiError
            ? card.error
            : new ApiError({ message: t('error.generic'), isNetworkError: false })
        }
        onRetry={() => void card.refetch()}
      />
    );
  }

  return <PersonProfile person={card.data} />;
}
