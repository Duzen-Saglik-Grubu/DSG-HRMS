import { useTranslation } from 'react-i18next';
import { FlowError } from '../verification/FlowError';

interface RegistrationErrorProps {
  error: unknown;
  onRestart: () => void;
  /** Uyelik islemi baslamis mi; ilk adimda "suresi doldu" soylenemez. */
  canExpire?: boolean;
}

/** Uyelik akisindaki hatanin gosterimi. */
export function RegistrationError({ error, onRestart, canExpire = true }: RegistrationErrorProps) {
  const { t } = useTranslation();

  return (
    <FlowError
      error={error}
      onRestart={onRestart}
      canExpire={canExpire}
      expiredText={t('identity.registration.errors.sessionExpired')}
      restartText={t('identity.registration.restart')}
    />
  );
}
