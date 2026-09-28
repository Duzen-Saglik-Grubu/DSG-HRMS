import { Alert, Button } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { ErrorState } from '@/shared/components/ErrorState';
import { classifyFlowError } from '../../utils/flowErrorKind';

interface FlowErrorProps {
  error: unknown;
  onRestart: () => void;
  /** Suresi dolan islemin iletisi ("Uyelik isleminin suresi doldu" gibi). */
  expiredText: string;
  /** Islemi bastan baslatan dugmenin metni. */
  restartText: string;
  /** Islem baslamis mi; ilk adimda "suresi doldu" soylenemez. */
  canExpire?: boolean;
}

/**
 * Uyelik ve iki adimli giris akisindaki hatanin gosterimi.
 *
 * Bilinen durumlar kullaniciya ne yapacagini soyler (SYG-KMLK-064). Beklenmeyen
 * hatada takip numarasi gosterilir (SYG-KMLK-067). Alan hatalari burada gosterilmez;
 * ilgili alanin altinda gorunur.
 */
export function FlowError({
  error,
  onRestart,
  expiredText,
  restartText,
  canExpire = true,
}: FlowErrorProps) {
  const { t } = useTranslation();
  const kind = classifyFlowError(error, { canExpire });

  switch (kind) {
    case 'fields':
      return null;
    case 'rateLimited':
      return <Alert severity="warning">{t('identity.verification.errors.rateLimited')}</Alert>;
    case 'channelUnavailable':
      return (
        <Alert severity="warning">{t('identity.verification.errors.channelUnavailable')}</Alert>
      );
    case 'sessionExpired':
      return (
        <Alert
          severity="warning"
          action={
            <Button color="inherit" size="small" onClick={onRestart}>
              {restartText}
            </Button>
          }
        >
          {expiredText}
        </Alert>
      );
    default:
      return (
        <ErrorState
          error={
            error instanceof ApiError
              ? error
              : new ApiError({ message: t('error.generic'), isNetworkError: false })
          }
        />
      );
  }
}
