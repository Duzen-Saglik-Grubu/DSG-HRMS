import { Avatar } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/shared/api/queryKeys';
import { personPhotosApi } from '@/shared/api/personPhotos';

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
      sx={{
        width: size,
        height: size,
        bgcolor: 'primary.main',
        fontSize: Math.max(14, size / 3.2),
      }}
    >
      {`${firstName.charAt(0)}${lastName.charAt(0)}`}
    </Avatar>
  );
}

/**
 * Fotografi yetkili istekle alir ve `data:` adresi olarak onbellekte tutar (#315). Gecici nesne
 * adresi (`blob:`) kullanilmaz: bilesen kalkinca birakilmasi gerekir ve StrictMode'da ya da
 * onbellekten yeniden kullanimda hala gosterilirken birakilip goruntuyu bozuyordu. Kucuk hal
 * yaklasik 15 KB oldugundan metin olarak tutmanin bellek maliyeti onemsizdir.
 */
function usePhotoUrl(source: PhotoSource, enabled: boolean): string | undefined {
  const key = source === 'me' ? 'me' : source.personId;
  const photo = useQuery({
    queryKey: queryKeys.personnel.photo(key),
    queryFn: async () =>
      toDataUrl(await (source === 'me' ? personPhotosApi.myPhoto() : personPhotosApi.photo(key))),
    enabled,
    staleTime: 5 * 60_000,
    retry: false,
  });

  return photo.data;
}

function toDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = () => reject(reader.error ?? new Error('photo'));
    reader.readAsDataURL(blob);
  });
}
