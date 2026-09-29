import { useEffect, useState } from 'react';
import { Alert, Button, IconButton, InputAdornment, Stack, TextField } from '@mui/material';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { ApiError } from '@/shared/api/problemDetails';
import { ErrorState } from '@/shared/components/ErrorState';
import { signInApi, type SignInResponse } from '../../api/signInApi';
import { primaryButtonSx } from '../motion';

type CredentialsForm = { email: string; password: string };

interface CredentialsStepProps {
  onSubmitted: () => void;
  onSignedIn: (response: SignInResponse) => void;
}

/** Sunucunun kullaniciya yonelik iletisini oldugu gibi gosterdigimiz durumlar. */
const KNOWN_STATUSES = [401, 403, 429];

/**
 * Kurumsal e-posta ve parola (SYG-KMLK-031…033, 055).
 *
 * Hatali parola ile var olmayan hesap AYNI iletiyi alir; ileti sunucudan gelir ve ekran onu
 * ayirt etmeye calismaz (SYG-KMLK-032). Kilit ve kapali hesap iletileri de sunucunundur.
 * Hatali giriste parola alani temizlenir, e-posta korunur.
 */
export function CredentialsStep({ onSubmitted, onSignedIn }: CredentialsStepProps) {
  const { t } = useTranslation();
  const [visible, setVisible] = useState(false);

  const schema = z.object({
    email: z.string().trim().email(t('identity.login.emailInvalid')),
    password: z.string().min(1, t('identity.login.passwordRequired')),
  });

  const {
    register,
    handleSubmit,
    setError,
    setValue,
    setFocus,
    formState: { errors },
  } = useForm<CredentialsForm>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
  });

  const signIn = useMutation({
    mutationFn: (form: CredentialsForm) =>
      signInApi.signIn({ email: form.email.trim(), password: form.password }),
    onSuccess: onSignedIn,
    onError: () => {
      setValue('password', '');
      setFocus('password');
    },
  });

  // Sunucunun alan hatalari ilgili alana baglanir (ADR-0015 §4).
  useEffect(() => {
    const serverErrors = signIn.error instanceof ApiError ? signIn.error.errors : undefined;
    for (const field of ['email', 'password'] as const) {
      const message = serverErrors?.[field]?.[0];
      if (message) {
        setError(field, { message });
      }
    }
  }, [signIn.error, setError]);

  const error = signIn.error;
  const known = error instanceof ApiError && KNOWN_STATUSES.includes(error.status ?? 0);

  return (
    <Stack
      component="form"
      spacing={2.5}
      noValidate
      onSubmit={(event) =>
        void handleSubmit((form) => {
          onSubmitted();
          signIn.mutate(form);
        })(event)
      }
    >
      <TextField
        {...register('email')}
        type="email"
        label={t('identity.login.email')}
        autoComplete="username"
        autoFocus
        error={Boolean(errors.email)}
        helperText={errors.email?.message ?? t('identity.login.emailHint')}
      />

      <TextField
        {...register('password')}
        type={visible ? 'text' : 'password'}
        label={t('identity.login.password')}
        autoComplete="current-password"
        error={Boolean(errors.password)}
        helperText={errors.password?.message}
        slotProps={{
          input: {
            endAdornment: (
              <InputAdornment position="end">
                <IconButton
                  onClick={() => setVisible((value) => !value)}
                  aria-label={t(
                    visible ? 'identity.login.hidePassword' : 'identity.login.showPassword',
                  )}
                  edge="end"
                >
                  {visible ? <VisibilityOff /> : <Visibility />}
                </IconButton>
              </InputAdornment>
            ),
          },
        }}
      />

      {error && known ? (
        <Alert severity={error.status === 401 ? 'error' : 'warning'} role="alert">
          {error.message}
        </Alert>
      ) : null}

      {error && !known && !(error instanceof ApiError && error.errors) ? (
        <ErrorState
          error={
            error instanceof ApiError
              ? error
              : new ApiError({ message: t('error.generic'), isNetworkError: false })
          }
        />
      ) : null}

      <Button
        type="submit"
        variant="contained"
        size="large"
        disabled={signIn.isPending}
        sx={primaryButtonSx}
      >
        {t('identity.login.submit')}
      </Button>
    </Stack>
  );
}
