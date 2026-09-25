import { Button, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';

/**
 * Eslesmeyen rota.
 *
 * Bos bir ekran yerine ne oldugunu ve ne yapilacagini soyler (ADR-0015 §6).
 */
export function NotFoundPage() {
  const { t } = useTranslation();

  return (
    <Stack spacing={2} sx={{ alignItems: 'flex-start' }}>
      <Typography variant="h5" component="h2">
        {t('error.pageNotFound')}
      </Typography>
      <Typography variant="body1" color="text.secondary">
        {t('error.pageNotFoundHint')}
      </Typography>
      <Button component={Link} to="/" variant="contained">
        {t('error.backToHome')}
      </Button>
    </Stack>
  );
}
