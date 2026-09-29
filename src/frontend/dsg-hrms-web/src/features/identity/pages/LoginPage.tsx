import { useEffect, useRef, useState } from 'react';
import { Alert, Box, CircularProgress, Link, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink, Navigate, useSearchParams } from 'react-router';
import { queryKeys } from '@/shared/api/queryKeys';
import { session, useSession } from '@/shared/auth/session';
import type { SessionEndReason, SessionResponse } from '@/shared/auth/sessionManager';
import { RETURN_TO_PARAM, routes, safeReturnPath } from '@/shared/routes';
import { registrationApi, type VerificationChannel } from '../api/registrationApi';
import { signInApi, type TwoFactorChallenge } from '../api/signInApi';
import { AuthLayout } from '../components/AuthLayout';
import { CredentialsStep } from '../components/login/CredentialsStep';
import { fadeSlide, reducedMotion } from '../components/motion';
import { ChannelStep } from '../components/verification/ChannelStep';
import { CodeStep } from '../components/verification/CodeStep';
import { FlowError } from '../components/verification/FlowError';

type Step =
  | { name: 'credentials' }
  | {
      name: 'channel';
      challenge: TwoFactorChallenge;
      channel?: VerificationChannel | undefined;
    }
  | {
      name: 'code';
      challenge: TwoFactorChallenge;
      channel: VerificationChannel;
      codeExpiresAt: string;
    };

const HEADING: Record<Step['name'], string> = {
  credentials: 'identity.login.heading',
  channel: 'identity.verification.channel.heading',
  code: 'identity.verification.code.heading',
};

/** Oturum sonu iletisinin onem derecesi: kullanicinin kendi cikisi bir uyari degildir. */
const END_SEVERITY: Record<SessionEndReason, 'info' | 'warning'> = {
  'logged-out': 'info',
  'signed-in-elsewhere': 'warning',
  'token-reuse': 'warning',
  'idle-timeout': 'warning',
  expired: 'warning',
  'account-changed': 'warning',
  'password-changed': 'warning',
};

/**
 * Giris ekrani (SYG-KMLK-031…034, 042, 055, 064…070).
 *
 * Iki adimli dogrulama aciksa parola dogrulandiktan sonra kanal ve kod adimlari gelir; bu
 * adimlar uyelik ekranlariyla AYNI bilesenlerdir (SYG-KMLK-034). Giristen sonra kullanici
 * istedigi sayfaya doner (`returnTo`); adres yalnizca uygulama ici bir yol olabilir.
 *
 * Oturum baska bir girisle veya hareketsizlikle kapandiysa nedeni burada soylenir
 * (SYG-KMLK-042).
 */
export function LoginPage() {
  const { t } = useTranslation();
  const state = useSession();
  const [params] = useSearchParams();
  const returnTo = safeReturnPath(params.get(RETURN_TO_PARAM));
  const [step, setStep] = useState<Step>({ name: 'credentials' });
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
  }, [step.name]);

  // Oturum acilinca durum degisir ve asagidaki yonlendirme kullaniciyi `returnTo`'ya goturur.
  const signedIn = (response: SessionResponse) => session.start(response);

  const restart = () => setStep({ name: 'credentials' });

  if (state.status === 'authenticated') {
    return <Navigate to={returnTo} replace />;
  }

  const renderError = (error: unknown) => (
    <FlowError
      error={error}
      onRestart={restart}
      expiredText={t('identity.login.expired')}
      restartText={t('identity.login.restart')}
    />
  );

  const endReason = state.status === 'anonymous' ? state.endReason : undefined;

  return (
    <AuthLayout>
      <Stack spacing={2}>
        <Box>
          <Typography
            variant="h4"
            component="h1"
            sx={{ fontWeight: 700, fontSize: { xs: '1.625rem', sm: '1.875rem' } }}
          >
            {t('identity.login.title')}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {t('identity.login.subtitle')}
          </Typography>
        </Box>
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

      {endReason && step.name === 'credentials' ? (
        <Alert severity={END_SEVERITY[endReason]} role="status">
          {t(`session.ended.${endReason}`)}
        </Alert>
      ) : null}

      {state.status === 'unknown' ? (
        // Oturum cerezden geri getiriliyor; acik oturum varsa kullanici dogrudan yonlendirilir.
        <Stack sx={{ alignItems: 'center', py: 4 }}>
          <CircularProgress aria-label={t('status.loading')} />
        </Stack>
      ) : (
        <Box
          key={step.name}
          sx={{
            animation: `${fadeSlide} 320ms cubic-bezier(0.2, 0.8, 0.2, 1) both`,
            ...reducedMotion,
          }}
        >
          {step.name === 'credentials' ? (
            <CredentialsStep
              onSubmitted={() => session.clearEndReason()}
              onSignedIn={(response) => {
                if (response.session) {
                  signedIn(response.session);
                } else if (response.challenge) {
                  setStep({ name: 'channel', challenge: response.challenge });
                }
              }}
            />
          ) : null}

          {step.name === 'channel' ? (
            <ChannelStep
              channels={step.challenge.channels}
              initialChannel={step.channel}
              requestCode={(channel) => signInApi.requestCode(step.challenge.challengeId, channel)}
              renderError={renderError}
              onCodeRequested={(channel, requested) =>
                setStep({
                  name: 'code',
                  challenge: step.challenge,
                  channel,
                  codeExpiresAt: requested.codeExpiresAt,
                })
              }
            />
          ) : null}

          {step.name === 'code' ? (
            <CodeStep
              intro={t('identity.login.codeIntro')}
              channel={step.channel}
              channels={step.challenge.channels}
              codeExpiresAt={step.codeExpiresAt}
              codeLength={settings.data?.verificationCodeLength ?? 6}
              requestCode={(channel) => signInApi.requestCode(step.challenge.challengeId, channel)}
              verify={(code) => signInApi.verify(step.challenge.challengeId, code)}
              renderError={renderError}
              onCodeResent={(requested) =>
                setStep({ ...step, codeExpiresAt: requested.codeExpiresAt })
              }
              onChangeChannel={() =>
                setStep({
                  name: 'channel',
                  challenge: step.challenge,
                  channel: step.challenge.channels.find((item) => item !== step.channel),
                })
              }
              onVerified={(response) => {
                if (response.session) {
                  signedIn(response.session);
                }
              }}
            />
          ) : null}
        </Box>
      )}

      {step.name === 'credentials' ? (
        <Stack spacing={1} sx={{ alignItems: 'center' }}>
          <Link component={RouterLink} to={routes.forgotPassword} variant="body2">
            {t('identity.login.forgotPassword')}
          </Link>
          <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
            {t('identity.login.noAccount')}{' '}
            <Link component={RouterLink} to={routes.register} sx={{ fontWeight: 600 }}>
              {t('identity.login.register')}
            </Link>
          </Typography>
        </Stack>
      ) : null}
    </AuthLayout>
  );
}
