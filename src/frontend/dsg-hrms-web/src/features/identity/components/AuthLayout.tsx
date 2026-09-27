import type { ReactNode } from 'react';
import { Box, Paper, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/shared/api/queryKeys';
import { registrationApi } from '../api/registrationApi';

/** Varsayilan kurumsal logo (SYG-KMLK-069). Kaynagi depodaki assets/duzen_logo.png'dir. */
export const DEFAULT_LOGO_URL = '/brand/duzen_logo.png';

interface AuthLayoutProps {
  children: ReactNode;
}

/**
 * Giris, uyelik, dogrulama ve parola ekranlarinin iki bolumlu duzeni (SYG-KMLK-068).
 *
 * Bir bolumde form, digerinde kurumsal bolum yer alir. 900 pikselin altinda bolumler
 * alt alta gecer ve FORM USTTE kalir: telefonda kullanici once yapacagi isi gorur.
 * Kurumsal gorsel Bilgi Islem tarafindan uretilecektir; gelene kadar kurumsal renk ve
 * metin kullanilir.
 */
export function AuthLayout({ children }: AuthLayoutProps) {
  const { t } = useTranslation();

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: { xs: 'column', md: 'row' },
        bgcolor: 'background.default',
      }}
    >
      <Box
        component="main"
        sx={{
          flex: { md: '0 0 50%' },
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          // 360 piksel genislikte yatay kaydirma olmamali (SYG-KMLK-065).
          px: { xs: 2, sm: 4 },
          py: { xs: 3, md: 6 },
        }}
      >
        <Paper variant="outlined" sx={{ width: '100%', maxWidth: 440, p: { xs: 2.5, sm: 4 } }}>
          <Stack spacing={3}>
            <Box
              component="img"
              src={DEFAULT_LOGO_URL}
              alt={t('identity.layout.logoAlt')}
              sx={{ height: 48, width: 'auto', alignSelf: 'flex-start' }}
            />
            {children}
            <SupportContact />
          </Stack>
        </Paper>
      </Box>

      <Box
        component="aside"
        aria-label={t('identity.layout.panelTitle')}
        sx={{
          flex: { md: '0 0 50%' },
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'center',
          px: { xs: 3, md: 8 },
          py: { xs: 4, md: 6 },
          bgcolor: 'primary.main',
          color: 'primary.contrastText',
        }}
      >
        <Typography
          variant="h4"
          component="p"
          sx={{ fontWeight: 600, fontSize: { xs: '1.5rem', md: '2.125rem' } }}
        >
          {t('identity.layout.panelTitle')}
        </Typography>
        <Typography variant="body1" sx={{ mt: 2, opacity: 0.9 }}>
          {t('identity.layout.panelText')}
        </Typography>
      </Box>
    </Box>
  );
}

/**
 * Destek birimi ve iletisim bilgisi (SYG-KMLK-070, PRM-GRN-04).
 *
 * Bilgi parametreden gelir; yuklenemezse bolum gosterilmez. Ekranin geri kalani bu
 * bilgiye bagli degildir.
 */
function SupportContact() {
  const { t } = useTranslation();
  const { data } = useQuery({
    queryKey: queryKeys.identity.publicSettings,
    queryFn: registrationApi.publicSettings,
    staleTime: 5 * 60_000,
  });

  if (!data?.supportContact) {
    return null;
  }

  return (
    <Box
      component="section"
      aria-labelledby="support-title"
      sx={{ pt: 2, borderTop: 1, borderColor: 'divider' }}
    >
      <Typography id="support-title" variant="subtitle2" component="h2">
        {t('identity.layout.supportTitle')}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {t('identity.layout.supportText', { contact: data.supportContact })}
      </Typography>
    </Box>
  );
}
