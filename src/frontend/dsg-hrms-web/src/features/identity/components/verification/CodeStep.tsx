import { useState, type ReactNode } from 'react';
import { Alert, Button, Stack, TextField, Typography } from '@mui/material';
import TimerOutlined from '@mui/icons-material/TimerOutlined';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import type {
  CodeRequested,
  VerificationChannel,
  VerificationOutcome,
} from '../../api/registrationApi';
import { formatCountdown, useCountdown } from '../../hooks/useCountdown';
import { primaryButtonSx } from '../motion';

interface CodeStepProps<TResponse extends { result: VerificationOutcome }> {
  /** Adimin basindaki aciklama; akisa gore degisir. */
  intro: string;
  channel: VerificationChannel;
  channels: VerificationChannel[];
  codeExpiresAt: string;
  codeLength: number;
  requestCode: (channel: VerificationChannel) => Promise<CodeRequested>;
  verify: (code: string) => Promise<TResponse>;
  onVerified: (response: TResponse) => void;
  onCodeResent: (requested: CodeRequested) => void;
  onChangeChannel: () => void;
  renderError: (error: unknown) => ReactNode;
}

/** Yeni kod istenmeden duzelmeyen sonuclar. */
const TERMINAL: VerificationOutcome[] = ['expired', 'attemptsExceeded', 'notUsable'];

/**
 * Dogrulama kodu (SYG-KMLK-016, 019, 023, 024, 028, 034). Uyelik ve iki adimli giris ayni
 * adimi kullanir; kod kurallari da aynidir.
 *
 * Uyelikte "bilgileriniz eslesiyorsa kod gelir" iletisi HER DURUMDA gosterilir
 * (SYG-KMLK-016): ekran eslesmenin olup olmadigini soylemez. Kalan sure sunucunun verdigi bitis
 * anindan hesaplanir. Tekrar gonderim ve kanal degisimi sunucuda hiz sinirina tabidir.
 */
export function CodeStep<TResponse extends { result: VerificationOutcome }>({
  intro,
  channel,
  channels,
  codeExpiresAt,
  codeLength,
  requestCode,
  verify: verifyCode,
  onVerified,
  onCodeResent,
  onChangeChannel,
  renderError,
}: CodeStepProps<TResponse>) {
  const { t } = useTranslation();
  const remaining = useCountdown(codeExpiresAt);
  const [code, setCode] = useState('');
  const [outcome, setOutcome] = useState<VerificationOutcome | null>(null);
  const [inputError, setInputError] = useState<string | null>(null);
  const [resent, setResent] = useState(false);

  const verify = useMutation({
    mutationFn: () => verifyCode(code.trim()),
    onSuccess: (response) => {
      if (response.result === 'verified') {
        onVerified(response);
        return;
      }

      setOutcome(response.result);
      setCode('');
    },
  });

  const resend = useMutation({
    mutationFn: () => requestCode(channel),
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
          setInputError(t('identity.verification.code.required'));
          return;
        }

        setInputError(null);
        setResent(false);
        verify.mutate();
      }}
    >
      <Alert severity="info">{intro}</Alert>

      <Stack
        direction="row"
        spacing={1}
        sx={{ alignItems: 'center', color: remaining > 60 ? 'text.secondary' : 'warning.main' }}
      >
        <TimerOutlined fontSize="small" aria-hidden />
        <Typography variant="body2" aria-live="off" sx={{ fontVariantNumeric: 'tabular-nums' }}>
          {t('identity.verification.code.remaining', { time: formatCountdown(remaining) })}
        </Typography>
      </Stack>

      <TextField
        label={t('identity.verification.code.label')}
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
        helperText={inputError ?? t('identity.verification.code.hint', { count: codeLength })}
      />

      {message ? (
        <Alert severity={message === 'mismatch' ? 'error' : 'warning'} role="alert">
          {t(`identity.verification.code.${message}`)}
        </Alert>
      ) : null}

      {resent ? <Alert severity="success">{t('identity.verification.code.resent')}</Alert> : null}

      {verify.error ? renderError(verify.error) : null}
      {resend.error ? renderError(resend.error) : null}

      <Button
        type="submit"
        variant="contained"
        size="large"
        disabled={expired || verify.isPending}
        sx={primaryButtonSx}
      >
        {t('identity.verification.code.submit')}
      </Button>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
        <Button variant="text" onClick={() => resend.mutate()} disabled={resend.isPending}>
          {t('identity.verification.code.resend')}
        </Button>
        {channels.length > 1 ? (
          <Button variant="text" onClick={onChangeChannel}>
            {t('identity.verification.code.changeChannel')}
          </Button>
        ) : null}
      </Stack>
    </Stack>
  );
}
