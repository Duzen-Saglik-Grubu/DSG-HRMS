import { useState } from 'react';
import { Alert, Box, Button, CircularProgress, Stack, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import CheckCircleRounded from '@mui/icons-material/CheckCircleRounded';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { routes } from '@/shared/routes';
import { invitationApi } from '../api/invitationApi';
import { registrationApi } from '../api/registrationApi';
import { AuthLayout } from '../components/AuthLayout';
import { popIn, primaryButtonSx, reducedMotion } from '../components/motion';
import { PasswordStep } from '../components/registration/PasswordStep';
import { FlowError } from '../components/verification/FlowError';

/**
 * Baglantidaki jetonu okur ve adresten SILER.
 *
 * Jeton adresin `#` kismindadir: tarayici bu kismi sunucuya gondermez. Sayfa acilir acilmaz
 * adresten de kaldirilir; tarayici gecmisinde ve ekran paylasiminda gorunmez.
 */
function takeToken(): string | null {
  const match = /(?:^#|&)token=([A-Za-z0-9_-]+)/.exec(window.location.hash);
  if (window.location.hash) {
    window.history.replaceState(null, '', window.location.pathname + window.location.search);
  }

  return match?.[1] ?? null;
}

/**
 * IK'nin gonderdigi parola olusturma baglantisi (SYG-KMLK-051, 052).
 *
 * Hesap yoksa hesap olusturulur; varsa parola yenilenir ve acik oturumlar kapanir. Baglanti
 * tek kullanimliktir.
 */
export function InvitePage() {
  const { t } = useTranslation();
  const [token] = useState(takeToken);
  const [done, setDone] = useState(false);

  const settings = useQuery({
    queryKey: queryKeys.identity.publicSettings,
    queryFn: registrationApi.publicSettings,
    staleTime: 5 * 60_000,
  });

  const info = useQuery({
    queryKey: queryKeys.identity.invitation(token),
    queryFn: () => invitationApi.lookup(token!),
    enabled: token !== null,
    retry: false,
    staleTime: Infinity,
  });

  const invalid = token === null || (info.error instanceof ApiError && info.error.status === 404);

  return (
    <AuthLayout>
      <Box>
        <Typography
          variant="h4"
          component="h1"
          sx={{ fontWeight: 700, fontSize: { xs: '1.625rem', sm: '1.875rem' } }}
        >
          {t('identity.invite.title')}
        </Typography>
        {info.data && !done ? (
          <Typography variant="body1" color="text.secondary" sx={{ mt: 1 }}>
            {t(
              info.data.accountExists
                ? 'identity.invite.greetingReset'
                : 'identity.invite.greeting',
              {
                name: info.data.firstName,
              },
            )}
          </Typography>
        ) : null}
      </Box>

      {invalid ? (
        <Stack spacing={2}>
          <Alert severity="warning" role="alert">
            {t('identity.invite.invalid')}
          </Alert>
          <Button component={RouterLink} to={routes.forgotPassword} variant="text">
            {t('identity.invite.useReset')}
          </Button>
        </Stack>
      ) : null}

      {!invalid && info.isPending ? (
        <Stack sx={{ alignItems: 'center', py: 4 }}>
          <CircularProgress aria-label={t('status.loading')} />
        </Stack>
      ) : null}

      {!invalid && info.error && !(info.error instanceof ApiError && info.error.status === 404) ? (
        <FlowError
          error={info.error}
          onRestart={() => void info.refetch()}
          expiredText={t('identity.invite.invalid')}
          restartText={t('error.retry')}
        />
      ) : null}

      {info.data && token && !done ? (
        <PasswordStep
          registrationId=""
          rules={
            settings.data?.passwordRules ?? {
              minLength: 6,
              maxLength: 128,
              requireComplexity: false,
            }
          }
          submit={(password) => invitationApi.accept(token, password)}
          intro={t(
            info.data.accountExists ? 'identity.invite.introReset' : 'identity.invite.intro',
          )}
          submitLabel={t('identity.invite.submit')}
          renderError={(error) => (
            <FlowError
              error={error}
              onRestart={() => undefined}
              expiredText={t('identity.invite.invalid')}
              restartText={t('error.retry')}
            />
          )}
          onRestart={() => undefined}
          onCompleted={() => setDone(true)}
        />
      ) : null}

      {done ? (
        <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center', py: 2 }}>
          <Box
            aria-hidden
            sx={(theme) => ({
              display: 'grid',
              placeItems: 'center',
              width: 88,
              height: 88,
              borderRadius: '50%',
              color: 'success.main',
              bgcolor: alpha(theme.palette.success.main, 0.1),
              animation: `${popIn} 520ms cubic-bezier(0.2, 0.8, 0.2, 1) both`,
              ...reducedMotion,
            })}
          >
            <CheckCircleRounded sx={{ fontSize: 56 }} />
          </Box>
          <Typography role="status" color="text.secondary">
            {t('identity.invite.done')}
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
      ) : null}
    </AuthLayout>
  );
}
