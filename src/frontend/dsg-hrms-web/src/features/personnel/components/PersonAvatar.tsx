import { useEffect, useMemo } from 'react';
import { Avatar } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/shared/api/queryKeys';
import { personsApi } from '../api/personsApi';

/** Fotografin kaynagi: kisinin kimligi ya da kullanicinin kendisi ("Profilim"). */
export type PhotoSource = { personId: string } | 'me';

/**
 * Fotograf ya da yoksa ad ve soyadin bas harfleri (SYG-PERS-040). Fotograf yalnizca sunucu
 * `hasPhoto` dediyse istenir; gelmezse (ornegin ag hatasi) bas harfler kalir.
 */
export function PersonAvatar({
  source,
  hasPhoto,
  firstName,
  lastName,
  size,
}: {
  source: PhotoSource;
  hasPhoto: boolean;
  firstName: string;
  lastName: string;
  size: number;
}) {
  const { t } = useTranslation();
  const name = `${firstName} ${lastName}`;
  const url = usePhotoUrl(source, hasPhoto);

  return (
    <Avatar
      src={url}
      alt={t('personnel.photo', { name })}
      role={url ? undefined : 'img'}
      aria-label={url ? undefined : t('personnel.photo', { name })}
      sx={{ width: size, height: size, bgcolor: 'primary.main', fontSize: size / 3.2 }}
    >
      {`${firstName.charAt(0)}${lastName.charAt(0)}`}
    </Avatar>
  );
}

/** Fotografi yetkili istekle alir ve gosterim icin gecici bir nesne adresine cevirir. */
function usePhotoUrl(source: PhotoSource, enabled: boolean): string | undefined {
  const key = source === 'me' ? 'me' : source.personId;
  const photo = useQuery({
    queryKey: queryKeys.personnel.photo(key),
    queryFn: () => (source === 'me' ? personsApi.myPhoto() : personsApi.photo(key)),
    enabled,
    staleTime: 5 * 60_000,
    retry: false,
  });

  const url = useMemo(
    () => (photo.data ? URL.createObjectURL(photo.data) : undefined),
    [photo.data],
  );

  // Nesne adresi bellekte goruntuyu tutar; bilesen kalkinca ya da fotograf degisince birakilir.
  useEffect(
    () => () => {
      if (url) {
        URL.revokeObjectURL(url);
      }
    },
    [url],
  );

  return url;
}
