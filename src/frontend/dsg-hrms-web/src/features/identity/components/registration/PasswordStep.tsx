import { useEffect, useState } from 'react';
import {
  Box,
  Button,
  IconButton,
  InputAdornment,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { ApiError } from '@/shared/api/problemDetails';
import { registrationApi, type PublicSettings } from '../../api/registrationApi';
import { primaryButtonSx } from '../motion';
import { RegistrationError } from './RegistrationError';

type PasswordForm = { password: string; confirm: string };

interface PasswordStepProps {
  registrationId: string;
  rules: PublicSettings['passwordRules'];
  onCompleted: () => void;
  onRestart: () => void;
}

/**
 * 4. adim: parola (SYG-KMLK-044, 045).
 *
 * Kurallar ekranda YARDIM olarak listelenir; istemci yalnizca uzunlugu ve iki alanin
 * ayniligini denetler. Yaygin parola ve kisisel sozcuk denetimi sunucudadir; sunucunun
 * iletisi parola alaninin altinda gosterilir.
 */
export function PasswordStep({ registrationId, rules, onCompleted, onRestart }: PasswordStepProps) {
  const { t } = useTranslation();
  const [visible, setVisible] = useState(false);

  const schema = z
    .object({
      password: z
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
    .refine((form) => form.password === form.confirm, {
      path: ['confirm'],
      message: t('identity.registration.password.mismatch'),
    });

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<PasswordForm>({
    resolver: zodResolver(schema),
    defaultValues: { password: '', confirm: '' },
  });

  const complete = useMutation({
    mutationFn: (form: PasswordForm) => registrationApi.complete(registrationId, form.password),
    onSuccess: onCompleted,
  });

  useEffect(() => {
    const messages =
      complete.error instanceof ApiError ? complete.error.errors?.['password'] : undefined;
    if (messages && messages.length > 0) {
      setError('password', { message: messages.join(' ') });
    }
  }, [complete.error, setError]);

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
    <Stack
      component="form"
      spacing={2}
      noValidate
      onSubmit={(event) => void handleSubmit((form) => complete.mutate(form))(event)}
    >
      <Typography variant="body2" color="text.secondary">
        {t('identity.registration.password.intro')}
      </Typography>

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
        {...register('password')}
        type={visible ? 'text' : 'password'}
        label={t('identity.registration.password.label')}
        autoComplete="new-password"
        autoFocus
        error={Boolean(errors.password)}
        helperText={errors.password?.message}
        slotProps={{ input: { endAdornment: toggle } }}
      />

      <TextField
        {...register('confirm')}
        type={visible ? 'text' : 'password'}
        label={t('identity.registration.password.confirm')}
        autoComplete="new-password"
        error={Boolean(errors.confirm)}
        helperText={errors.confirm?.message}
      />

      {complete.error ? <RegistrationError error={complete.error} onRestart={onRestart} /> : null}

      <Button
        type="submit"
        variant="contained"
        size="large"
        disabled={complete.isPending}
        sx={primaryButtonSx}
      >
        {t('identity.registration.password.submit')}
      </Button>
    </Stack>
  );
}
