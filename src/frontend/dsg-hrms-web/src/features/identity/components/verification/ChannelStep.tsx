import { useState, type ReactNode } from 'react';
import {
  Box,
  Button,
  FormControl,
  FormLabel,
  Radio,
  RadioGroup,
  Stack,
  Typography,
} from '@mui/material';
import { alpha } from '@mui/material/styles';
import EmailOutlined from '@mui/icons-material/EmailOutlined';
import SmsOutlined from '@mui/icons-material/SmsOutlined';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import type { CodeRequested, VerificationChannel } from '../../api/registrationApi';
import { primaryButtonSx } from '../motion';

interface ChannelStepProps {
  channels: VerificationChannel[];
  initialChannel?: VerificationChannel | undefined;
  /** Secilen kanala kod gonderir (uyelik veya iki adimli giris). */
  requestCode: (channel: VerificationChannel) => Promise<CodeRequested>;
  onCodeRequested: (channel: VerificationChannel, requested: CodeRequested) => void;
  renderError: (error: unknown) => ReactNode;
}

/**
 * Dogrulama kanali secimi (SYG-KMLK-016, 017, 018, 034). Uyelik ve iki adimli giris ayni
 * adimi kullanir.
 *
 * Yalnizca sunucunun sundugu kanallar listelenir. Kodun gidecegi adres veya numara
 * maskeli bile GOSTERILMEZ (SYG-KMLK-018): sunucu zaten dondurmez.
 *
 * Secenekler kart gorunumundedir ama altta gercek bir radyo grubu vardir: ok tuslariyla
 * gezilir, ekran okuyucu secimi duyurur.
 */
export function ChannelStep({
  channels,
  initialChannel,
  requestCode,
  onCodeRequested,
  renderError,
}: ChannelStepProps) {
  const { t } = useTranslation();
  const [channel, setChannel] = useState<VerificationChannel>(
    initialChannel ?? channels[0] ?? 'email',
  );

  const request = useMutation({
    mutationFn: () => requestCode(channel),
    onSuccess: (requested) => onCodeRequested(channel, requested),
  });

  return (
    <Stack
      component="form"
      spacing={2.5}
      noValidate
      onSubmit={(event) => {
        event.preventDefault();
        request.mutate();
      }}
    >
      <FormControl>
        <FormLabel
          id="channel-label"
          sx={{ mb: 1.5, color: 'text.secondary', typography: 'body2' }}
        >
          {t('identity.verification.channel.intro')}
        </FormLabel>
        <RadioGroup
          aria-labelledby="channel-label"
          name="channel"
          value={channel}
          onChange={(event) => setChannel(event.target.value as VerificationChannel)}
          sx={{ gap: 1.5 }}
        >
          {channels.map((item, index) => {
            const selected = item === channel;

            return (
              <Box
                key={item}
                component="label"
                sx={(theme) => ({
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1.5,
                  p: 1.5,
                  pr: 2,
                  borderRadius: 3,
                  cursor: 'pointer',
                  border: `1.5px solid ${selected ? theme.palette.primary.main : theme.palette.divider}`,
                  bgcolor: selected ? alpha(theme.palette.primary.main, 0.05) : 'background.paper',
                  transition: 'border-color 160ms ease, background-color 160ms ease',
                  '&:hover': { borderColor: theme.palette.primary.main },
                  '&:focus-within': {
                    outline: `2px solid ${alpha(theme.palette.primary.main, 0.35)}`,
                    outlineOffset: 2,
                  },
                })}
              >
                <Box
                  sx={(theme) => ({
                    display: 'grid',
                    placeItems: 'center',
                    width: 40,
                    height: 40,
                    borderRadius: 2,
                    color: 'primary.main',
                    bgcolor: alpha(theme.palette.primary.main, 0.08),
                    flexShrink: 0,
                  })}
                  aria-hidden
                >
                  {item === 'email' ? <EmailOutlined /> : <SmsOutlined />}
                </Box>
                <Typography sx={{ flexGrow: 1, fontWeight: 500 }}>
                  {t(`identity.verification.channel.${item}`)}
                </Typography>
                <Radio
                  value={item}
                  autoFocus={index === 0}
                  slotProps={{
                    input: { 'aria-label': t(`identity.verification.channel.${item}`) },
                  }}
                />
              </Box>
            );
          })}
        </RadioGroup>
      </FormControl>

      {request.error ? renderError(request.error) : null}

      <Button
        type="submit"
        variant="contained"
        size="large"
        disabled={request.isPending}
        sx={primaryButtonSx}
      >
        {t('identity.verification.channel.submit')}
      </Button>
    </Stack>
  );
}
