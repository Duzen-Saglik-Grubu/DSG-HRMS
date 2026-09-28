import { Box, Stack, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useTranslation } from 'react-i18next';
import { reducedMotion } from '../motion';

interface StepProgressProps {
  current: number;
  total: number;
}

/**
 * Adim gostergesi: dolan bolumler ve "Adim 2 / 4" metni.
 *
 * Ilerleme renkle birlikte METINLE de verilir (ADR-0015 §7: renk tek basina anlam
 * tasimaz). Gosterge `progressbar` rolundedir; ekran okuyucu degeri okur.
 */
export function StepProgress({ current, total }: StepProgressProps) {
  const { t } = useTranslation();
  const label = t('identity.registration.stepLabel', { current, total });

  return (
    <Stack spacing={1}>
      <Box
        role="progressbar"
        aria-valuemin={1}
        aria-valuemax={total}
        aria-valuenow={current}
        aria-valuetext={label}
        sx={{ display: 'grid', gridTemplateColumns: `repeat(${total}, 1fr)`, gap: 0.75 }}
      >
        {Array.from({ length: total }, (_, index) => (
          <Box
            key={index}
            sx={(theme) => ({
              height: 4,
              borderRadius: 2,
              overflow: 'hidden',
              bgcolor: alpha(theme.palette.primary.main, 0.12),
            })}
          >
            <Box
              sx={(theme) => ({
                height: '100%',
                transformOrigin: 'left',
                transform: `scaleX(${index < current ? 1 : 0})`,
                background: `linear-gradient(90deg, ${theme.palette.primary.main}, ${theme.palette.secondary.main})`,
                transition: 'transform 400ms cubic-bezier(0.2, 0.8, 0.2, 1)',
                ...reducedMotion,
              })}
            />
          </Box>
        ))}
      </Box>
      <Typography variant="caption" color="text.secondary" aria-hidden>
        {label}
      </Typography>
    </Stack>
  );
}
