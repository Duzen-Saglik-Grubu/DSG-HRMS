import { useState } from 'react';
import {
  Button,
  FormControl,
  FormControlLabel,
  FormLabel,
  Radio,
  RadioGroup,
  Stack,
} from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  registrationApi,
  type CodeRequested,
  type VerificationChannel,
} from '../../api/registrationApi';
import { RegistrationError } from './registrationErrors';

interface ChannelStepProps {
  registrationId: string;
  channels: VerificationChannel[];
  initialChannel?: VerificationChannel | undefined;
  onCodeRequested: (channel: VerificationChannel, requested: CodeRequested) => void;
  onRestart: () => void;
}

/**
 * 2. adim: dogrulama kanali (SYG-KMLK-016, 017, 018).
 *
 * Yalnizca sunucunun sundugu kanallar listelenir. Kodun gidecegi adres veya numara
 * maskeli bile GOSTERILMEZ (SYG-KMLK-018): sunucu zaten dondurmez.
 */
export function ChannelStep({
  registrationId,
  channels,
  initialChannel,
  onCodeRequested,
  onRestart,
}: ChannelStepProps) {
  const { t } = useTranslation();
  const [channel, setChannel] = useState<VerificationChannel>(
    initialChannel ?? channels[0] ?? 'email',
  );

  const request = useMutation({
    mutationFn: () => registrationApi.requestCode(registrationId, channel),
    onSuccess: (requested) => onCodeRequested(channel, requested),
  });

  return (
    <Stack
      component="form"
      spacing={2}
      noValidate
      onSubmit={(event) => {
        event.preventDefault();
        request.mutate();
      }}
    >
      <FormControl>
        <FormLabel id="channel-label">{t('identity.registration.channel.intro')}</FormLabel>
        <RadioGroup
          aria-labelledby="channel-label"
          name="channel"
          value={channel}
          onChange={(event) => setChannel(event.target.value as VerificationChannel)}
        >
          {channels.map((item, index) => (
            <FormControlLabel
              key={item}
              value={item}
              control={<Radio autoFocus={index === 0} />}
              label={t(`identity.registration.channel.${item}`)}
            />
          ))}
        </RadioGroup>
      </FormControl>

      {request.error ? <RegistrationError error={request.error} onRestart={onRestart} /> : null}

      <Button type="submit" variant="contained" size="large" disabled={request.isPending}>
        {t('identity.registration.channel.submit')}
      </Button>
    </Stack>
  );
}
