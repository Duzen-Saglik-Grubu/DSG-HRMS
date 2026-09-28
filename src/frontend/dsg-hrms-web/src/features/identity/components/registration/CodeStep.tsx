import { useState } from 'react';
import { Alert, Button, Stack, TextField, Typography } from '@mui/material';
import TimerOutlined from '@mui/icons-material/TimerOutlined';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  registrationApi,
  type CodeRequested,
  type VerificationChannel,
  type VerificationOutcome,
} from '../../api/registrationApi';
import { formatCountdown, useCountdown } from '../../hooks/useCountdown';
import { primaryButtonSx } from '../motion';
import { RegistrationError } from './registrationErrors';

interface CodeStepProps {
  registrationId: string;
  channel: VerificationChannel;
  channels: VerificationChannel[];
  codeExpiresAt: string;
  codeLength: number;
  onVerified: (accountExists: boolean) => void;
  onCodeResent: (requested: CodeRequested) => void;
  onChangeChannel: () => void;
  onRestart: () => void;
}

/** Yeni kod istenmeden duzelmeyen sonuclar. */
const TERMINAL: VerificationOutcome[] = ['expired', 'attemptsExceeded', 'notUsable'];

/**
 * 3. adim: dogrulama kodu (SYG-KMLK-016, 019, 023, 024, 028).
 *
 * "Bilgileriniz eslesiyorsa kod gelir" iletisi HER DURUMDA gosterilir (SYG-KMLK-016):
 * ekran eslesmenin olup olmadigini soylemez. Kalan sure sunucunun verdigi bitis
 * anindan hesaplanir. Tekrar gonderim ve kanal degisimi sunucuda hiz sinirina tabidir.
 */
export function CodeStep({
  registrationId,
  channel,
  channels,
  codeExpiresAt,
  codeLength,
  onVerified,
  onCodeResent,
  onChangeChannel,
  onRestart,
}: CodeStepProps) {
  const { t } = useTranslation();
  const remaining = useCountdown(codeExpiresAt);
  const [code, setCode] = useState('');
  const [outcome, setOutcome] = useState<VerificationOutcome | null>(null);
  const [inputError, setInputError] = useState<string | null>(null);
  const [resent, setResent] = useState(false);

  const verify = useMutation({
    mutationFn: () => registrationApi.verify(registrationId, code.trim()),
    onSuccess: (response) => {
      if (response.result === 'verified') {
        onVerified(response.accountExists);
        return;
      }

      setOutcome(response.result);
      setCode('');
    },
  });

  const resend = useMutation({
    mutationFn: () => registrationApi.requestCode(registrationId, channel),
    onSuccess: (requested) => {
      setOutcome(null);
      setCode('');
      setResent(true);
      onCodeResent(requested);
    },
  });

  const expired = remaining === 0 || (outcome !== null && TERMINAL.includes(outcome));
  const message = remaining === 0 && outcome === null ? 'expired' : outcome;

  return (
    <Stack
      component="form"
      spacing={2}
      noValidate
      onSubmit={(event) => {
        event.preventDefault();
        if (code.trim().length === 0) {
          setInputError(t('identity.registration.code.required'));
          return;
        }

        setInputError(null);
        setResent(false);
        verify.mutate();
      }}
    >
      <Alert severity="info">{t('identity.registration.code.intro')}</Alert>

      <Stack
        direction="row"
        spacing={1}
        sx={{ alignItems: 'center', color: remaining > 60 ? 'text.secondary' : 'warning.main' }}
      >
        <TimerOutlined fontSize="small" aria-hidden />
        <Typography variant="body2" aria-live="off" sx={{ fontVariantNumeric: 'tabular-nums' }}>
          {t('identity.registration.code.remaining', { time: formatCountdown(remaining) })}
        </Typography>
      </Stack>

      <TextField
        label={t('identity.registration.code.label')}
        value={code}
        onChange={(event) => setCode(event.target.value.replace(/\D/g, ''))}
        autoComplete="one-time-code"
        autoFocus
        disabled={expired}
        slotProps={{ htmlInput: { inputMode: 'numeric', maxLength: codeLength } }}
        sx={{
          '& input': {
            fontSize: '1.5rem',
            fontWeight: 600,
            letterSpacing: '0.45em',
            textAlign: 'center',
            fontVariantNumeric: 'tabular-nums',
          },
        }}
        error={Boolean(inputError)}
        helperText={inputError ?? t('identity.registration.code.hint', { count: codeLength })}
      />

      {message ? (
        <Alert severity={message === 'mismatch' ? 'error' : 'warning'} role="alert">
          {t(`identity.registration.code.${message}`)}
        </Alert>
      ) : null}

      {resent ? <Alert severity="success">{t('identity.registration.code.resent')}</Alert> : null}

      {verify.error ? <RegistrationError error={verify.error} onRestart={onRestart} /> : null}
      {resend.error ? <RegistrationError error={resend.error} onRestart={onRestart} /> : null}

      <Button
        type="submit"
        variant="contained"
        size="large"
        disabled={expired || verify.isPending}
        sx={primaryButtonSx}
      >
        {t('identity.registration.code.submit')}
      </Button>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
        <Button variant="text" onClick={() => resend.mutate()} disabled={resend.isPending}>
          {t('identity.registration.code.resend')}
        </Button>
        {channels.length > 1 ? (
          <Button variant="text" onClick={onChangeChannel}>
            {t('identity.registration.code.changeChannel')}
          </Button>
        ) : null}
      </Stack>
    </Stack>
  );
}
