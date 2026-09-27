import { useEffect, useRef, useState } from 'react';
import { Alert, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/shared/api/queryKeys';
import { registrationApi, type VerificationChannel } from '../api/registrationApi';
import { AuthLayout } from '../components/AuthLayout';
import { ChannelStep } from '../components/registration/ChannelStep';
import { CodeStep } from '../components/registration/CodeStep';
import { IdentityStep } from '../components/registration/IdentityStep';
import { PasswordStep } from '../components/registration/PasswordStep';

type Step =
  | { name: 'identity' }
  | {
      name: 'channel';
      registrationId: string;
      channels: VerificationChannel[];
      channel?: VerificationChannel | undefined;
    }
  | {
      name: 'code';
      registrationId: string;
      channels: VerificationChannel[];
      channel: VerificationChannel;
      codeExpiresAt: string;
    }
  | { name: 'password'; registrationId: string }
  | { name: 'done' }
  | { name: 'exists' };

const STEP_NUMBER: Record<Step['name'], number> = {
  identity: 1,
  channel: 2,
  code: 3,
  password: 4,
  done: 4,
  exists: 3,
};
const HEADING: Record<Step['name'], string> = {
  identity: 'identity.registration.identity.heading',
  channel: 'identity.registration.channel.heading',
  code: 'identity.registration.code.heading',
  password: 'identity.registration.password.heading',
  done: 'identity.registration.done.heading',
  exists: 'identity.registration.exists.heading',
};

/**
 * Uyelik akisi (ADR-0006 §1): bilgiler, kanal, kod, parola.
 *
 * Adim degistiginde odak adim basligina tasinir: klavye ve ekran okuyucu kullanicisi
 * yeni adimin basladigini duyar ve sayfanin basina donmek zorunda kalmaz (SYG-KMLK-066).
 */
export function RegistrationPage() {
  const { t } = useTranslation();
  const [step, setStep] = useState<Step>({ name: 'identity' });
  const headingRef = useRef<HTMLHeadingElement>(null);
  const firstRender = useRef(true);

  const settings = useQuery({
    queryKey: queryKeys.identity.publicSettings,
    queryFn: registrationApi.publicSettings,
    staleTime: 5 * 60_000,
  });

  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }

    headingRef.current?.focus();
  }, [step.name]);

  const restart = () => setStep({ name: 'identity' });

  return (
    <AuthLayout>
      <Stack spacing={1}>
        <Typography variant="h5" component="h1">
          {t('identity.registration.title')}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {t('identity.registration.stepLabel', { current: STEP_NUMBER[step.name], total: 4 })}
        </Typography>
        <Typography
          ref={headingRef}
          tabIndex={-1}
          variant="h6"
          component="h2"
          sx={{ outline: 'none' }}
        >
          {t(HEADING[step.name])}
        </Typography>
      </Stack>

      {step.name === 'identity' ? (
        <IdentityStep
          onRestart={restart}
          onStarted={(started) =>
            setStep({
              name: 'channel',
              registrationId: started.registrationId,
              channels: started.channels,
            })
          }
        />
      ) : null}

      {step.name === 'channel' ? (
        <ChannelStep
          registrationId={step.registrationId}
          channels={step.channels}
          initialChannel={step.channel}
          onRestart={restart}
          onCodeRequested={(channel, requested) =>
            setStep({
              name: 'code',
              registrationId: step.registrationId,
              channels: step.channels,
              channel,
              codeExpiresAt: requested.codeExpiresAt,
            })
          }
        />
      ) : null}

      {step.name === 'code' ? (
        <CodeStep
          registrationId={step.registrationId}
          channel={step.channel}
          channels={step.channels}
          codeExpiresAt={step.codeExpiresAt}
          codeLength={settings.data?.verificationCodeLength ?? 6}
          onRestart={restart}
          onCodeResent={(requested) => setStep({ ...step, codeExpiresAt: requested.codeExpiresAt })}
          onChangeChannel={() =>
            setStep({
              name: 'channel',
              registrationId: step.registrationId,
              channels: step.channels,
              channel: step.channels.find((item) => item !== step.channel),
            })
          }
          onVerified={(accountExists) =>
            setStep(
              accountExists
                ? { name: 'exists' }
                : { name: 'password', registrationId: step.registrationId },
            )
          }
        />
      ) : null}

      {step.name === 'password' ? (
        <PasswordStep
          registrationId={step.registrationId}
          rules={
            settings.data?.passwordRules ?? {
              minLength: 6,
              maxLength: 128,
              requireComplexity: false,
            }
          }
          onRestart={restart}
          onCompleted={() => setStep({ name: 'done' })}
        />
      ) : null}

      {step.name === 'done' ? (
        <Alert severity="success">{t('identity.registration.done.text')}</Alert>
      ) : null}
      {step.name === 'exists' ? (
        <Alert severity="info">{t('identity.registration.exists.text')}</Alert>
      ) : null}
    </AuthLayout>
  );
}
