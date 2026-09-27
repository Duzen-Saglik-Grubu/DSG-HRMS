import { useEffect } from 'react';
import { Button, Stack, TextField, Typography } from '@mui/material';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { ApiError } from '@/shared/api/problemDetails';
import { isValidNationalId } from '@/shared/utils/nationalId';
import { registrationApi, type RegistrationStarted } from '../../api/registrationApi';
import { parseBirthDate } from '../../utils/birthDate';
import { RegistrationError } from './registrationErrors';

type IdentityForm = { nationalId: string; birthDate: string; email: string };

interface IdentityStepProps {
  onStarted: (started: RegistrationStarted) => void;
  onRestart: () => void;
}

/**
 * 1. adim: TCKN, dogum tarihi, kurumsal e-posta (SYG-KMLK-013, 014).
 *
 * TCKN istemcide de sagla algoritmasiyla denetlenir; gecersiz numara sunucuya
 * gonderilmez. Yanit, eslesme olsa da olmasa da aynidir (KR-085).
 */
export function IdentityStep({ onStarted, onRestart }: IdentityStepProps) {
  const { t } = useTranslation();

  const schema = z.object({
    nationalId: z
      .string()
      .trim()
      .refine(isValidNationalId, t('identity.registration.identity.nationalIdInvalid')),
    birthDate: z
      .string()
      .refine(
        (value) => parseBirthDate(value) !== null,
        t('identity.registration.identity.birthDateInvalid'),
      ),
    email: z.string().trim().email(t('identity.registration.identity.emailInvalid')),
  });

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<IdentityForm>({
    resolver: zodResolver(schema),
    defaultValues: { nationalId: '', birthDate: '', email: '' },
  });

  const start = useMutation({
    mutationFn: (form: IdentityForm) =>
      registrationApi.start({
        nationalId: form.nationalId.trim(),
        birthDate: parseBirthDate(form.birthDate)!,
        email: form.email.trim(),
      }),
    onSuccess: onStarted,
  });

  // Sunucunun alan hatalari ilgili alana baglanir (ADR-0015 §4).
  useEffect(() => {
    const serverErrors = start.error instanceof ApiError ? start.error.errors : undefined;
    for (const field of ['nationalId', 'birthDate', 'email'] as const) {
      const message = serverErrors?.[field]?.[0];
      if (message) {
        setError(field, { message });
      }
    }
  }, [start.error, setError]);

  return (
    <Stack
      component="form"
      spacing={2}
      noValidate
      onSubmit={(event) => void handleSubmit((form) => start.mutate(form))(event)}
    >
      <Typography variant="body2" color="text.secondary">
        {t('identity.registration.identity.intro')}
      </Typography>

      <TextField
        {...register('nationalId')}
        label={t('identity.registration.identity.nationalId')}
        autoComplete="off"
        autoFocus
        slotProps={{ htmlInput: { inputMode: 'numeric', maxLength: 11 } }}
        error={Boolean(errors.nationalId)}
        helperText={errors.nationalId?.message}
      />

      <TextField
        {...register('birthDate')}
        label={t('identity.registration.identity.birthDate')}
        autoComplete="bday"
        slotProps={{ htmlInput: { inputMode: 'numeric', maxLength: 10 } }}
        error={Boolean(errors.birthDate)}
        helperText={errors.birthDate?.message ?? t('identity.registration.identity.birthDateHint')}
      />

      <TextField
        {...register('email')}
        type="email"
        label={t('identity.registration.identity.email')}
        autoComplete="email"
        error={Boolean(errors.email)}
        helperText={errors.email?.message ?? t('identity.registration.identity.emailHint')}
      />

      {start.error ? <RegistrationError error={start.error} onRestart={onRestart} /> : null}

      <Button type="submit" variant="contained" size="large" disabled={start.isPending}>
        {t('identity.registration.identity.submit')}
      </Button>
    </Stack>
  );
}
