import { useEffect, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  IconButton,
  InputAdornment,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { queryKeys } from '@/shared/api/queryKeys';
import { ApiError } from '@/shared/api/problemDetails';
import { ErrorState } from '@/shared/components/ErrorState';
import { session, useSession } from '@/shared/auth/session';
import { PageHeader } from '@/shared/components/PageHeader';
import { passwordApi } from '../api/passwordApi';
import { registrationApi } from '../api/registrationApi';

type ChangeForm = { currentPassword: string; newPassword: string; confirm: string };

const FIELDS = ['currentPassword', 'newPassword'] as const;

/**
 * Oturum icinde parola degisikligi (SYG-KMLK-048).
 *
 * Mevcut parola istenir: acik birakilmis bir oturumu bulan kisi parolayi degistirip hesabi ele
 * geciremez. Yanlis mevcut parola giris kilidine sayilir. Degisiklikten sonra bu oturum acik
 * kalir; diger cihazlardaki oturumlar kapanir ve kullaniciya bu soylenir.
 *
 * Oturum parola degisimi bekliyorsa (SYG-KMLK-046, 050) nedeni gosterilir; degisince kisit
 * kalkar ve uygulamanin geri kalani kullanilabilir.
 */
export function ChangePasswordPage() {
  const { t } = useTranslation();
  const [visible, setVisible] = useState(false);
  const [changed, setChanged] = useState(false);
  const successRef = useRef<HTMLDivElement>(null);
  const state = useSession();
  const required = state.status === 'authenticated' ? state.user.passwordChangeRequired : null;

  const settings = useQuery({
    queryKey: queryKeys.identity.publicSettings,
    queryFn: registrationApi.publicSettings,
    staleTime: 5 * 60_000,
  });
  const rules = settings.data?.passwordRules ?? {
    minLength: 6,
    maxLength: 128,
    requireComplexity: false,
  };

  const schema = z
    .object({
      currentPassword: z.string().min(1, t('identity.change.currentRequired')),
      newPassword: z
        .string()
        .min(1, t('identity.registration.password.required'))
        .refine(
          (value) => [...value].length >= rules.minLength,
          t('identity.registration.password.tooShort', { count: rules.minLength }),
        )
        .refine(
          (value) => [...value].length <= rules.maxLength,
          t('identity.registration.password.tooLong', { count: rules.maxLength }),
        ),
      confirm: z.string(),
    })
    .refine((form) => form.newPassword === form.confirm, {
      path: ['confirm'],
      message: t('identity.registration.password.mismatch'),
    });

  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors },
  } = useForm<ChangeForm>({
    resolver: zodResolver(schema),
    defaultValues: { currentPassword: '', newPassword: '', confirm: '' },
  });

  const change = useMutation({
    mutationFn: (form: ChangeForm) =>
      passwordApi.change({ currentPassword: form.currentPassword, newPassword: form.newPassword }),
    onMutate: () => setChanged(false),
    onSuccess: () => {
      reset();
      setChanged(true);
      session.passwordChanged();
    },
  });

  // Sunucunun alan hatalari ilgili alana baglanir (ADR-0015 §4).
  useEffect(() => {
    const serverErrors = change.error instanceof ApiError ? change.error.errors : undefined;
    for (const field of FIELDS) {
      const messages = serverErrors?.[field];
      if (messages && messages.length > 0) {
        setError(field, { message: messages.join(' ') });
      }
    }
  }, [change.error, setError]);

  useEffect(() => {
    if (changed) {
      successRef.current?.focus();
    }
  }, [changed]);

  const error = change.error;
  const hasFieldErrors = error instanceof ApiError && Boolean(error.errors);

  const toggle = (
    <InputAdornment position="end">
      <IconButton
        aria-label={
          visible
            ? t('identity.registration.password.hide')
            : t('identity.registration.password.show')
        }
        onClick={() => setVisible((value) => !value)}
        edge="end"
      >
        {visible ? <VisibilityOff /> : <Visibility />}
      </IconButton>
    </InputAdornment>
  );

  return (
    <Stack spacing={3}>
      <PageHeader title={t('identity.change.title')} description={t('identity.change.subtitle')} />

      <Paper variant="outlined" sx={{ p: { xs: 2, sm: 3 }, maxWidth: 520 }}>
        <Stack
          component="form"
          spacing={2}
          noValidate
          onSubmit={(event) => void handleSubmit((form) => change.mutate(form))(event)}
        >
          {required ? (
            <Alert severity="warning" role="alert">
              {required === 'firstSignIn'
                ? t('identity.change.required.firstSignIn')
                : t('identity.change.required.expired')}
            </Alert>
          ) : null}

          {changed ? (
            <Alert severity="success" role="status" ref={successRef} tabIndex={-1}>
              {t('identity.change.done')}
            </Alert>
          ) : null}

          <TextField
            {...register('currentPassword')}
            type={visible ? 'text' : 'password'}
            label={t('identity.change.current')}
            autoComplete="current-password"
            autoFocus
            error={Boolean(errors.currentPassword)}
            helperText={errors.currentPassword?.message}
            slotProps={{ input: { endAdornment: toggle } }}
          />

          <Box component="section" aria-labelledby="password-rules">
            <Typography id="password-rules" variant="body2">
              {t('identity.registration.password.rulesTitle')}
            </Typography>
            <Box component="ul" sx={{ m: 0, pl: 3, typography: 'body2', color: 'text.secondary' }}>
              <li>{t('identity.registration.password.ruleLength', { count: rules.minLength })}</li>
              {rules.requireComplexity ? (
                <li>{t('identity.registration.password.ruleComplexity')}</li>
              ) : null}
              <li>{t('identity.registration.password.ruleCommon')}</li>
              <li>{t('identity.registration.password.rulePersonal')}</li>
            </Box>
          </Box>

          <TextField
            {...register('newPassword')}
            type={visible ? 'text' : 'password'}
            label={t('identity.change.new')}
            autoComplete="new-password"
            error={Boolean(errors.newPassword)}
            helperText={errors.newPassword?.message}
          />

          <TextField
            {...register('confirm')}
            type={visible ? 'text' : 'password'}
            label={t('identity.change.confirm')}
            autoComplete="new-password"
            error={Boolean(errors.confirm)}
            helperText={errors.confirm?.message}
          />

          {error instanceof ApiError && error.status === 429 ? (
            <Alert severity="warning" role="alert">
              {error.message}
            </Alert>
          ) : null}

          {error && !hasFieldErrors && !(error instanceof ApiError && error.status === 429) ? (
            <ErrorState
              error={
                error instanceof ApiError
                  ? error
                  : new ApiError({ message: t('error.generic'), isNetworkError: false })
              }
            />
          ) : null}

          <Box>
            <Button type="submit" variant="contained" disabled={change.isPending}>
              {t('identity.change.submit')}
            </Button>
          </Box>
        </Stack>
      </Paper>
    </Stack>
  );
}
