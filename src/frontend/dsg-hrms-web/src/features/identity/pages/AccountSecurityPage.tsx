import { useEffect, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  FormControl,
  FormControlLabel,
  FormLabel,
  IconButton,
  InputAdornment,
  Paper,
  Radio,
  RadioGroup,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/shared/api/queryKeys';
import { ApiError } from '@/shared/api/problemDetails';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { ErrorState } from '@/shared/components/ErrorState';
import { PageHeader } from '@/shared/components/PageHeader';
import { registrationApi, type VerificationChannel } from '../api/registrationApi';
import { twoFactorApi, type TwoFactorStatus } from '../api/twoFactorApi';
import { CodeStep } from '../components/verification/CodeStep';

type Step =
  { name: 'form' } | { name: 'code'; channel: VerificationChannel; codeExpiresAt: string };

/**
 * Hesap guvenligi: kullanicinin kendi iki adimli dogrulama tercihi (SYG-KMLK-080).
 *
 * Giriste kod yalnizca sistem parametresi (PRM-KML-08) ve bu tercih birlikte acikken istenir.
 * Sistemde kullanilmiyorsa sayfa bunu soyler; menude de gorunmez. Acmak icin mevcut parola ve
 * kodun gidecegi kanal secilir, gelen kod girilince tercih acilir. Kapatmak mevcut parolayi ve
 * onayi ister. Mevcut parola hataliysa sunucunun iletisi alanin altinda gosterilir.
 */
export function AccountSecurityPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [notice, setNotice] = useState<'enabled' | 'disabled' | null>(null);
  const noticeRef = useRef<HTMLDivElement>(null);

  const status = useQuery({ queryKey: queryKeys.identity.twoFactor, queryFn: twoFactorApi.status });

  useEffect(() => {
    if (notice) {
      noticeRef.current?.focus();
    }
  }, [notice]);

  const changed = async (next: 'enabled' | 'disabled') => {
    setNotice(next);
    await queryClient.invalidateQueries({ queryKey: queryKeys.identity.twoFactor });
  };

  return (
    <Stack spacing={3}>
      <PageHeader
        title={t('identity.security.title')}
        description={t('identity.security.subtitle')}
      />

      <Paper variant="outlined" sx={{ p: { xs: 2, sm: 3 }, maxWidth: 560 }}>
        <Stack spacing={2}>
          <Typography variant="h6" component="h2">
            {t('identity.security.heading')}
          </Typography>

          {notice ? (
            <Alert severity="success" role="status" ref={noticeRef} tabIndex={-1}>
              {t(`identity.security.${notice === 'enabled' ? 'enable' : 'disable'}.done`)}
            </Alert>
          ) : null}

          {status.isPending ? (
            <Box sx={{ display: 'grid', placeItems: 'center', py: 3 }}>
              <CircularProgress aria-label={t('identity.security.heading')} />
            </Box>
          ) : status.isError ? (
            <ErrorState error={toApiError(status.error, t('error.generic'))} />
          ) : (
            <SecurityContent
              status={status.data}
              onChanged={(next) => void changed(next)}
              onStart={() => setNotice(null)}
            />
          )}
        </Stack>
      </Paper>
    </Stack>
  );
}

interface SecurityContentProps {
  status: TwoFactorStatus;
  onChanged: (next: 'enabled' | 'disabled') => void;
  onStart: () => void;
}

function SecurityContent({ status, onChanged, onStart }: SecurityContentProps) {
  const { t } = useTranslation();

  if (status.enabled) {
    return (
      <>
        <Alert severity={status.available ? 'success' : 'info'}>
          {status.available
            ? t('identity.security.enabled')
            : t('identity.security.unavailableEnabled')}
        </Alert>
        <DisableForm onDisabled={() => onChanged('disabled')} onStart={onStart} />
      </>
    );
  }

  if (!status.available) {
    return <Alert severity="info">{t('identity.security.unavailable')}</Alert>;
  }

  return (
    <>
      <Alert severity="info">{t('identity.security.disabled')}</Alert>
      <Typography variant="body2" color="text.secondary">
        {t('identity.security.explain')}
      </Typography>
      {status.channels.length === 0 ? (
        <Alert severity="warning">{t('identity.security.noChannel')}</Alert>
      ) : (
        <EnableFlow
          channels={status.channels}
          onEnabled={() => onChanged('enabled')}
          onStart={onStart}
        />
      )}
    </>
  );
}

interface EnableFlowProps {
  channels: VerificationChannel[];
  onEnabled: () => void;
  onStart: () => void;
}

/** Acma: mevcut parola ve kanal, ardindan gelen kod (SYG-KMLK-080). */
function EnableFlow({ channels, onEnabled, onStart }: EnableFlowProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState<Step>({ name: 'form' });
  const [password, setPassword] = useState('');
  const [channel, setChannel] = useState<VerificationChannel>(channels[0] ?? 'email');
  const [passwordError, setPasswordError] = useState<string | null>(null);
  // Tekrar gonderimde yeni kodun kimligi; dogrulama her zaman son kodla yapilir.
  const codeId = useRef<string | null>(null);

  const settings = useQuery({
    queryKey: queryKeys.identity.publicSettings,
    queryFn: registrationApi.publicSettings,
    staleTime: 5 * 60_000,
  });

  const requestCode = async (selected: VerificationChannel) => {
    const issued = await twoFactorApi.startSetup({ currentPassword: password, channel: selected });
    codeId.current = issued.codeId;
    return issued;
  };

  const start = useMutation({
    mutationFn: () => requestCode(channel),
    onMutate: () => {
      setPasswordError(null);
      onStart();
    },
    onSuccess: (issued) => setStep({ name: 'code', channel, codeExpiresAt: issued.codeExpiresAt }),
    onError: (error) => setPasswordError(currentPasswordError(error)),
  });

  const restart = () => {
    codeId.current = null;
    setPassword('');
    start.reset();
    setStep({ name: 'form' });
  };

  if (step.name === 'code') {
    return (
      <Stack spacing={2}>
        <Typography variant="subtitle1" component="h3">
          {t('identity.security.enable.heading')}
        </Typography>
        <CodeStep
          intro={t('identity.security.enable.codeIntro')}
          channel={step.channel}
          channels={channels}
          codeExpiresAt={step.codeExpiresAt}
          codeLength={settings.data?.verificationCodeLength ?? 6}
          requestCode={requestCode}
          verify={(code) => twoFactorApi.verifySetup(codeId.current ?? '', code)}
          onVerified={() => {
            codeId.current = null;
            setPassword('');
            onEnabled();
          }}
          onCodeResent={(requested) => setStep({ ...step, codeExpiresAt: requested.codeExpiresAt })}
          onChangeChannel={restart}
          renderError={(error) => <RequestError error={error} />}
        />
        <Box>
          <Button variant="text" onClick={restart}>
            {t('identity.security.enable.cancel')}
          </Button>
        </Box>
      </Stack>
    );
  }

  return (
    <Stack
      component="form"
      spacing={2}
      noValidate
      aria-labelledby="enable-heading"
      onSubmit={(event) => {
        event.preventDefault();
        if (password.length === 0) {
          setPasswordError(t('identity.change.currentRequired'));
          return;
        }

        start.mutate();
      }}
    >
      <Typography id="enable-heading" variant="subtitle1" component="h3">
        {t('identity.security.enable.heading')}
      </Typography>
      <Typography variant="body2">{t('identity.security.enable.intro')}</Typography>

      <PasswordField
        value={password}
        onChange={(value) => {
          setPassword(value);
          setPasswordError(null);
        }}
        error={passwordError}
      />

      <FormControl>
        <FormLabel id="two-factor-channel">{t('identity.verification.channel.intro')}</FormLabel>
        <RadioGroup
          aria-labelledby="two-factor-channel"
          name="channel"
          value={channel}
          onChange={(event) => setChannel(event.target.value as VerificationChannel)}
        >
          {channels.map((item) => (
            <FormControlLabel
              key={item}
              value={item}
              control={<Radio />}
              label={t(`identity.verification.channel.${item}`)}
            />
          ))}
        </RadioGroup>
      </FormControl>

      {start.error && !currentPasswordError(start.error) ? (
        <RequestError error={start.error} />
      ) : null}

      <Box>
        <Button type="submit" variant="contained" disabled={start.isPending}>
          {t('identity.security.enable.submit')}
        </Button>
      </Box>
    </Stack>
  );
}

interface DisableFormProps {
  onDisabled: () => void;
  onStart: () => void;
}

/** Kapatma: mevcut parola ve onay (SYG-KMLK-080). */
function DisableForm({ onDisabled, onStart }: DisableFormProps) {
  const { t } = useTranslation();
  const [password, setPassword] = useState('');
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [confirming, setConfirming] = useState(false);

  const disable = useMutation({
    mutationFn: () => twoFactorApi.disable({ currentPassword: password }),
    onMutate: () => {
      setPasswordError(null);
      onStart();
    },
    onSuccess: () => {
      setPassword('');
      onDisabled();
    },
    onError: (error) => setPasswordError(currentPasswordError(error)),
  });

  return (
    <Stack
      component="form"
      spacing={2}
      noValidate
      aria-labelledby="disable-heading"
      onSubmit={(event) => {
        event.preventDefault();
        if (password.length === 0) {
          setPasswordError(t('identity.change.currentRequired'));
          return;
        }

        setConfirming(true);
      }}
    >
      <Typography id="disable-heading" variant="subtitle1" component="h3">
        {t('identity.security.disable.heading')}
      </Typography>
      <Typography variant="body2">{t('identity.security.disable.intro')}</Typography>

      <PasswordField
        value={password}
        onChange={(value) => {
          setPassword(value);
          setPasswordError(null);
        }}
        error={passwordError}
      />

      {disable.error && !currentPasswordError(disable.error) ? (
        <RequestError error={disable.error} />
      ) : null}

      <Box>
        <Button type="submit" variant="outlined" color="error" disabled={disable.isPending}>
          {t('identity.security.disable.submit')}
        </Button>
      </Box>

      <ConfirmDialog
        open={confirming}
        title={t('identity.security.disable.confirmTitle')}
        description={t('identity.security.disable.confirmText')}
        destructive
        confirmLabel={t('identity.security.disable.confirm')}
        onCancel={() => setConfirming(false)}
        onConfirm={() => {
          setConfirming(false);
          disable.mutate();
        }}
      />
    </Stack>
  );
}

interface PasswordFieldProps {
  value: string;
  onChange: (value: string) => void;
  error: string | null;
}

function PasswordField({ value, onChange, error }: PasswordFieldProps) {
  const { t } = useTranslation();
  const [visible, setVisible] = useState(false);

  return (
    <TextField
      value={value}
      onChange={(event) => onChange(event.target.value)}
      type={visible ? 'text' : 'password'}
      label={t('identity.change.current')}
      autoComplete="current-password"
      error={Boolean(error)}
      helperText={error}
      slotProps={{
        input: {
          endAdornment: (
            <InputAdornment position="end">
              <IconButton
                aria-label={
                  visible
                    ? t('identity.registration.password.hide')
                    : t('identity.registration.password.show')
                }
                onClick={() => setVisible((current) => !current)}
                edge="end"
              >
                {visible ? <VisibilityOff /> : <Visibility />}
              </IconButton>
            </InputAdornment>
          ),
        },
      }}
    />
  );
}

/**
 * Islem hatasi. Kilit ve hiz siniri (429) ile is kurali (422: sistemde kapali, kanal
 * kullanilamiyor) sunucunun Turkce iletisiyle; beklenmeyen hata takip numarasiyla gosterilir.
 */
function RequestError({ error }: { error: unknown }) {
  const { t } = useTranslation();

  if (error instanceof ApiError && (error.status === 429 || error.status === 422)) {
    return (
      <Alert severity="warning" role="alert">
        {error.message}
      </Alert>
    );
  }

  return <ErrorState error={toApiError(error, t('error.generic'))} />;
}

/** Sunucunun mevcut parola alan hatasi (ADR-0015 §4); yoksa `null`. */
function currentPasswordError(error: unknown): string | null {
  const messages = error instanceof ApiError ? error.errors?.['currentPassword'] : undefined;
  return messages && messages.length > 0 ? messages.join(' ') : null;
}

function toApiError(error: unknown, fallback: string): ApiError {
  return error instanceof ApiError
    ? error
    : new ApiError({ message: fallback, isNetworkError: false });
}
