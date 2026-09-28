import type { ReactNode } from 'react';
import { Box, Paper, Stack, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import SupportAgentOutlined from '@mui/icons-material/SupportAgentOutlined';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/shared/api/queryKeys';
import { registrationApi } from '../api/registrationApi';
import { BrandPanel } from './BrandPanel';
import { fadeUp, reducedMotion } from './motion';

/** Varsayilan kurumsal logo (SYG-KMLK-069). Kaynagi depodaki assets/duzen_logo.png'dir. */
export const DEFAULT_LOGO_URL = '/brand/duzen_logo.png';

interface AuthLayoutProps {
  children: ReactNode;
}

/**
 * Giris, uyelik, dogrulama ve parola ekranlarinin iki bolumlu duzeni (SYG-KMLK-068).
 *
 * Genis ekranda kurumsal gorsel solda, form sagda. 900 pikselin altinda bolumler alt
 * alta gecer ve FORM USTTE kalir: telefonda kullanici once yapacagi isi gorur. Form
 * belgede once gelir; klavye odagi gorsel alana hic ugramaz (odaklanabilir oge yoktur).
 */
export function AuthLayout({ children }: AuthLayoutProps) {
  const { t } = useTranslation();

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'grid',
        gridTemplateColumns: {
          xs: 'minmax(0, 1fr)',
          md: 'minmax(0, 1fr) minmax(0, 1fr)',
          lg: 'minmax(0, 7fr) minmax(0, 5fr)',
        },
        // Telefonda form kendi yuksekligini alir; kalan alani kurumsal bolum doldurur.
        gridTemplateRows: { xs: 'auto 1fr', md: '1fr' },
        bgcolor: 'background.default',
      }}
    >
      <Box
        component="main"
        sx={(theme) => ({
          order: { xs: 0, md: 1 },
          display: 'flex',
          alignItems: { xs: 'flex-start', md: 'center' },
          justifyContent: 'center',
          // 360 piksel genislikte yatay kaydirma olmamali (SYG-KMLK-065).
          minWidth: 0,
          px: { xs: 2, sm: 4 },
          py: { xs: 3, md: 6 },
          background: `radial-gradient(1200px 600px at 100% 0%, ${alpha(theme.palette.primary.main, 0.06)}, transparent 60%)`,
        })}
      >
        <Paper
          elevation={0}
          sx={(theme) => ({
            width: '100%',
            maxWidth: 460,
            p: { xs: 3, sm: 4.5 },
            borderRadius: 4,
            border: `1px solid ${alpha(theme.palette.primary.main, 0.08)}`,
            boxShadow: `0 30px 60px -30px ${alpha(theme.palette.primary.dark, 0.35)}, 0 8px 24px -12px ${alpha(theme.palette.primary.dark, 0.12)}`,
            animation: `${fadeUp} 500ms cubic-bezier(0.2, 0.8, 0.2, 1) both`,
            ...reducedMotion,
          })}
        >
          <Stack spacing={3}>
            <Box
              component="img"
              src={DEFAULT_LOGO_URL}
              alt={t('identity.layout.logoAlt')}
              sx={{ height: 44, width: 'auto', maxWidth: '100%', alignSelf: 'flex-start' }}
            />
            {children}
            <SupportContact />
          </Stack>
        </Paper>
      </Box>

      <Box sx={{ order: { xs: 1, md: 0 }, display: 'flex', minWidth: 0, '& > *': { flex: 1 } }}>
        <BrandPanel />
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
    <Stack
      component="section"
      aria-labelledby="support-title"
      direction="row"
      spacing={1.5}
      sx={(theme) => ({
        alignItems: 'flex-start',
        p: 1.5,
        borderRadius: 2,
        bgcolor: alpha(theme.palette.primary.main, 0.04),
      })}
    >
      <SupportAgentOutlined color="primary" fontSize="small" sx={{ mt: 0.25 }} aria-hidden />
      <Box>
        <Typography id="support-title" variant="subtitle2" component="h2">
          {t('identity.layout.supportTitle')}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {t('identity.layout.supportText', { contact: data.supportContact })}
        </Typography>
      </Box>
    </Stack>
  );
}
