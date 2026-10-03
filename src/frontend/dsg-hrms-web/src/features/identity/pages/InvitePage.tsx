import { useEffect, useState } from 'react';
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
 * Baglantidaki jetonu okur. Adresi DEGISTIRMEZ.
 *
 * Jeton adresin `#` kismindadir: tarayici bu kismi sunucuya gondermez. E-posta istemcisi
 * veya ag gecidi adresi kodlamis olabilir (`%3D`); kod cozulerek okunur.
 *
 * Okuma yan etkisiz OLMALIDIR: React, ilk cizimde agactaki bir bilesen askiya alinirsa
 * islenmemis agaci atar ve baslaticiyi yeniden cagirir. Jeton burada adresten silinseydi
 * ikinci cagri bos adres bulur ve gecerli baglanti "gecersiz" gorunurdu (#137).
 */
function readToken(): string | null {
  let hash = window.location.hash;
  try {
    hash = decodeURIComponent(hash);
  } catch {
    // Bozuk kodlama: adres oldugu gibi okunur.
  }

  return /(?:^#|&)token=([A-Za-z0-9_-]+)/.exec(hash)?.[1] ?? null;
}

/**
 * IK'nin gonderdigi parola olusturma baglantisi (SYG-KMLK-051, 052).
 *
 * Hesap yoksa hesap olusturulur; varsa parola yenilenir ve acik oturumlar kapanir. Baglanti
 * tek kullanimliktir.
 */
export function InvitePage() {
  const { t } = useTranslation();
  const [token] = useState(readToken);

  // Jeton, sayfa ekrana yerlestikten SONRA adresten kaldirilir: tarayici gecmisinde ve ekran
  // paylasiminda gorunmez. Yonlendiricinin gecmis kaydi (state) korunur.
  useEffect(() => {
    if (window.location.hash) {
      window.history.replaceState(
        window.history.state,
        '',
        window.location.pathname + window.location.search,
      );
    }
  }, []);
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
