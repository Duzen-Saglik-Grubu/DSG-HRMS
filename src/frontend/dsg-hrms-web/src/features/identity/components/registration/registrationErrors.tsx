import { Alert, Button } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { ErrorState } from '@/shared/components/ErrorState';
import { classifyRegistrationError } from '../../utils/registrationErrorKind';

interface RegistrationErrorProps {
  error: unknown;
  onRestart: () => void;
}

/**
 * Uyelik akisindaki hatanin gosterimi.
 *
 * Bilinen durumlar kullaniciya ne yapacagini soyler (SYG-KMLK-064). Beklenmeyen
 * hatada takip numarasi gosterilir (SYG-KMLK-067). Alan hatalari burada gosterilmez;
 * ilgili alanin altinda gorunur.
 */
export function RegistrationError({ error, onRestart }: RegistrationErrorProps) {
  const { t } = useTranslation();
  const kind = classifyRegistrationError(error);

  switch (kind) {
    case 'fields':
      return null;
    case 'rateLimited':
      return <Alert severity="warning">{t('identity.registration.errors.rateLimited')}</Alert>;
    case 'channelUnavailable':
      return (
        <Alert severity="warning">{t('identity.registration.errors.channelUnavailable')}</Alert>
      );
    case 'sessionExpired':
      return (
        <Alert
          severity="warning"
          action={
            <Button color="inherit" size="small" onClick={onRestart}>
              {t('identity.registration.restart')}
            </Button>
          }
        >
          {t('identity.registration.errors.sessionExpired')}
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
