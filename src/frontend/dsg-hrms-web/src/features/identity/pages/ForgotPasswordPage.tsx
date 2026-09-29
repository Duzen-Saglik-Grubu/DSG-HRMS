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
import { passwordApi } from '../api/passwordApi';
import { registrationApi, type VerificationChannel } from '../api/registrationApi';
import { AuthLayout } from '../components/AuthLayout';
import { fadeSlide, popIn, primaryButtonSx, reducedMotion } from '../components/motion';
import { IdentityStep } from '../components/registration/IdentityStep';
import { PasswordStep } from '../components/registration/PasswordStep';
import { StepProgress } from '../components/registration/StepProgress';
import { ChannelStep } from '../components/verification/ChannelStep';
import { CodeStep } from '../components/verification/CodeStep';
import { FlowError } from '../components/verification/FlowError';

type Step =
  | { name: 'identity' }
  | {
      name: 'channel';
      resetId: string;
      channels: VerificationChannel[];
      channel?: VerificationChannel | undefined;
    }
  | {
      name: 'code';
      resetId: string;
      channels: VerificationChannel[];
      channel: VerificationChannel;
      codeExpiresAt: string;
    }
  | { name: 'password'; resetId: string }
  | { name: 'done' }
  | { name: 'noAccount' };

const TOTAL_STEPS = 4;

const STEP_NUMBER: Record<Step['name'], number> = {
  identity: 1,
  channel: 2,
  code: 3,
  password: 4,
  done: 4,
  noAccount: 3,
};

const HEADING: Record<Step['name'], string> = {
  identity: 'identity.registration.identity.heading',
  channel: 'identity.verification.channel.heading',
  code: 'identity.verification.code.heading',
  password: 'identity.reset.password.heading',
  done: 'identity.reset.done.heading',
  noAccount: 'identity.reset.noAccount.heading',
};

/**
 * Parola sifirlama (SYG-KMLK-047): bilgiler, kanal, kod, yeni parola.
 *
 * Uyelikle AYNI adimlar ve ayni kurallar kullanilir; ayri bir dogrulama mekanizmasi yoktur.
 * Ilk uc adimin yanitlari eslesme olsa da olmasa da aynidir (KR-085). Hesabin olup olmadigi
 * YALNIZCA kod dogrulandiktan sonra soylenir: hesap yoksa kullanici uyelige yonlendirilir.
 */
export function ForgotPasswordPage() {
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

  // Adim degisince odak adim basligina tasinir (SYG-KMLK-066).
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

  const renderError = (error: unknown) => (
    <FlowError
      error={error}
      onRestart={restart}
      expiredText={t('identity.reset.expired')}
      restartText={t('identity.registration.restart')}
    />
  );

  const finished = step.name === 'done' || step.name === 'noAccount';

  return (
    <AuthLayout>
      <Stack spacing={2}>
        <Box>
          <Typography
            variant="h4"
            component="h1"
            sx={{ fontWeight: 700, fontSize: { xs: '1.625rem', sm: '1.875rem' } }}
          >
            {t('identity.reset.title')}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {t('identity.reset.subtitle')}
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
            start={passwordApi.startReset}
            intro={t('identity.reset.identityIntro')}
            onRestart={restart}
            onStarted={(started) =>
              setStep({
                name: 'channel',
                resetId: started.registrationId,
                channels: started.channels,
              })
            }
          />
        ) : null}

        {step.name === 'channel' ? (
          <ChannelStep
            channels={step.channels}
            initialChannel={step.channel}
            requestCode={(channel) => passwordApi.requestCode(step.resetId, channel)}
            renderError={renderError}
            onCodeRequested={(channel, requested) =>
              setStep({
                name: 'code',
                resetId: step.resetId,
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
            requestCode={(channel) => passwordApi.requestCode(step.resetId, channel)}
            verify={(code) => passwordApi.verify(step.resetId, code)}
            renderError={renderError}
            onCodeResent={(requested) =>
              setStep({ ...step, codeExpiresAt: requested.codeExpiresAt })
            }
            onChangeChannel={() =>
              setStep({
                name: 'channel',
                resetId: step.resetId,
                channels: step.channels,
                channel: step.channels.find((item) => item !== step.channel),
              })
            }
            onVerified={({ accountExists }) =>
              setStep(
                accountExists ? { name: 'password', resetId: step.resetId } : { name: 'noAccount' },
              )
            }
          />
        ) : null}

        {step.name === 'password' ? (
          <PasswordStep
            registrationId={step.resetId}
            rules={
              settings.data?.passwordRules ?? {
                minLength: 6,
                maxLength: 128,
                requireComplexity: false,
              }
            }
            submit={(password) => passwordApi.reset(step.resetId, password)}
            intro={t('identity.reset.password.intro')}
            submitLabel={t('identity.reset.password.submit')}
            renderError={renderError}
            onRestart={restart}
            onCompleted={() => setStep({ name: 'done' })}
          />
        ) : null}

        {step.name === 'done' ? (
          <Outcome
            tone="success"
            icon={<CheckCircleRounded sx={{ fontSize: 56 }} />}
            action={t('identity.registration.goToLogin')}
            to={routes.login}
          >
            {t('identity.reset.done.text')}
          </Outcome>
        ) : null}

        {step.name === 'noAccount' ? (
          <Outcome
            tone="info"
            icon={<InfoRounded sx={{ fontSize: 56 }} />}
            action={t('identity.reset.noAccount.action')}
            to={routes.register}
          >
            {t('identity.reset.noAccount.text')}
          </Outcome>
        ) : null}
      </Box>

      {step.name === 'identity' ? (
        <Button
          component={RouterLink}
          to={routes.login}
          variant="text"
          sx={{ alignSelf: 'center' }}
        >
          {t('identity.reset.backToLogin')}
        </Button>
      ) : null}
    </AuthLayout>
  );
}

interface OutcomeProps {
  tone: 'success' | 'info';
  icon: ReactNode;
  action: string;
  to: string;
  children: ReactNode;
}

/** Akisin sonu: canlandirilmis simge, aciklama ve sonraki adim. */
function Outcome({ tone, icon, action, to, children }: OutcomeProps) {
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
        to={to}
        variant="contained"
        size="large"
        fullWidth
        sx={primaryButtonSx}
      >
        {action}
      </Button>
    </Stack>
  );
}
