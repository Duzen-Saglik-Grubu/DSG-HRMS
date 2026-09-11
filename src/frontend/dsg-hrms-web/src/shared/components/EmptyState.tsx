import type { ReactNode } from 'react';
import { Box, Paper, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';

interface EmptyStateProps {
  /** Ne olmadigini soyler. Ornek: "Henuz izin talebiniz yok". */
  title?: string;
  /** Kullanicinin ne yapabilecegini soyler. */
  description?: string;
  /** Onerilen eylem (ornegin "Yeni talep olustur"). */
  action?: ReactNode;
  icon?: ReactNode;
}

/**
 * Bos liste durumu (ADR-0015 §6).
 *
 * "Kayit yok" demek yeterli DEGILDIR: kullanici bunun bir hata mi, yetki sorunu
 * mu, yoksa gercekten bos bir liste mi oldugunu ayirt edemez. Bu bilesen ne
 * olmadigini soyler ve bir sonraki adimi onerir.
 */
export function EmptyState({ title, description, action, icon }: EmptyStateProps) {
  const { t } = useTranslation();

  return (
    <Paper variant="outlined" sx={{ p: 4 }}>
      <Stack spacing={1.5} sx={{ alignItems: 'center', textAlign: 'center' }}>
        {icon ? <Box sx={{ color: 'text.disabled' }}>{icon}</Box> : null}

        <Typography variant="subtitle1">{title ?? t('durum.bosListe')}</Typography>

        {description ? (
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        ) : null}

        {action ? <Box sx={{ pt: 1 }}>{action}</Box> : null}
      </Stack>
    </Paper>
  );
}
