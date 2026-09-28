import { useEffect, useRef, useState, type ReactNode } from 'react';
import { Box, Button, Stack, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import CheckCircleRounded from '@mui/icons-material/CheckCircleRounded';
import InfoRounded from '@mui/icons-material/InfoRounded';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router';
import { queryKeys } from '@/shared/api/queryKeys';
import { routes } from '@/shared/routes';
import { registrationApi, type VerificationChannel } from '../api/registrationApi';
import { AuthLayout } from '../components/AuthLayout';
import { fadeSlide, popIn, primaryButtonSx, reducedMotion } from '../components/motion';
import { IdentityStep } from '../components/registration/IdentityStep';
import { PasswordStep } from '../components/registration/PasswordStep';
import { RegistrationError } from '../components/registration/RegistrationError';
import { StepProgress } from '../components/registration/StepProgress';
import { ChannelStep } from '../components/verification/ChannelStep';
import { CodeStep } from '../components/verification/CodeStep';

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

const TOTAL_STEPS = 4;

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
  channel: 'identity.verification.channel.heading',
  code: 'identity.verification.code.heading',
  password: 'identity.registration.password.heading',
  done: 'identity.registration.done.heading',
  exists: 'identity.registration.exists.heading',
};

/**
 * Uyelik akisi (ADR-0006 §1): bilgiler, kanal, kod, parola.
 *
 * Adim degistiginde odak adim basligina tasinir: klavye ve ekran okuyucu kullanicisi
 * yeni adimin basladigini duyar ve sayfanin basina donmek zorunda kalmaz (SYG-KMLK-066).
 *
 * "Bastan basla" akisi tamamen sifirlar: ilk adimdaki form ve hata durumu da temizlenir
 * (adim bileseni yeniden kurulur).
 */
export function RegistrationPage() {
  const { t } = useTranslation();
  const [step, setStep] = useState<Step>({ name: 'identity' });
  const [attempt, setAttempt] = useState(0);
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
  }, [step.name, attempt]);

  const restart = () => {
    setStep({ name: 'identity' });
    setAttempt((value) => value + 1);
  };

  const finished = step.name === 'done' || step.name === 'exists';

  return (
    <AuthLayout>
      <Stack spacing={2}>
        <Box>
          <Typography
            variant="h4"
            component="h1"
            sx={{ fontWeight: 700, fontSize: { xs: '1.625rem', sm: '1.875rem' } }}
          >
            {t('identity.registration.title')}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {t('identity.registration.subtitle')}
          </Typography>
        </Box>
        {finished ? null : <StepProgress current={STEP_NUMBER[step.name]} total={TOTAL_STEPS} />}
        <Typography
          ref={headingRef}
          tabIndex={-1}
          variant="h6"
          component="h2"
          sx={{ outline: 'none', fontWeight: 600 }}
        >
          {t(HEADING[step.name])}
        </Typography>
      </Stack>

      <Box
        key={`${step.name}-${attempt}`}
        sx={{
          animation: `${fadeSlide} 320ms cubic-bezier(0.2, 0.8, 0.2, 1) both`,
          ...reducedMotion,
        }}
      >
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
            channels={step.channels}
            initialChannel={step.channel}
            requestCode={(channel) => registrationApi.requestCode(step.registrationId, channel)}
            renderError={(error) => <RegistrationError error={error} onRestart={restart} />}
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
            intro={t('identity.registration.codeIntro')}
            channel={step.channel}
            channels={step.channels}
            codeExpiresAt={step.codeExpiresAt}
            codeLength={settings.data?.verificationCodeLength ?? 6}
            requestCode={(channel) => registrationApi.requestCode(step.registrationId, channel)}
            verify={(code) => registrationApi.verify(step.registrationId, code)}
            renderError={(error) => <RegistrationError error={error} onRestart={restart} />}
            onCodeResent={(requested) =>
              setStep({ ...step, codeExpiresAt: requested.codeExpiresAt })
            }
            onChangeChannel={() =>
              setStep({
                name: 'channel',
                registrationId: step.registrationId,
                channels: step.channels,
                channel: step.channels.find((item) => item !== step.channel),
              })
            }
            onVerified={({ accountExists }) =>
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
          <Outcome tone="success" icon={<CheckCircleRounded sx={{ fontSize: 56 }} />}>
            {t('identity.registration.done.text')}
          </Outcome>
        ) : null}

        {step.name === 'exists' ? (
          <Outcome tone="info" icon={<InfoRounded sx={{ fontSize: 56 }} />}>
            {t('identity.registration.exists.text')}
          </Outcome>
        ) : null}
      </Box>
    </AuthLayout>
  );
}

interface OutcomeProps {
  tone: 'success' | 'info';
  icon: ReactNode;
  children: ReactNode;
}

/** Akisin sonu: canlandirilmis simge ve aciklama. */
function Outcome({ tone, icon, children }: OutcomeProps) {
  const { t } = useTranslation();

  return (
    <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center', py: 2 }}>
      <Box
        aria-hidden
        sx={(theme) => ({
          display: 'grid',
          placeItems: 'center',
          width: 88,
          height: 88,
          borderRadius: '50%',
          color: `${tone}.main`,
          bgcolor: alpha(theme.palette[tone].main, 0.1),
          animation: `${popIn} 520ms cubic-bezier(0.2, 0.8, 0.2, 1) both`,
          ...reducedMotion,
        })}
      >
        {icon}
      </Box>
      <Typography role="status" color="text.secondary">
        {children}
      </Typography>
      <Button
        component={RouterLink}
        to={routes.login}
        variant="contained"
        size="large"
        fullWidth
        sx={primaryButtonSx}
      >
        {t('identity.registration.goToLogin')}
      </Button>
    </Stack>
  );
}
