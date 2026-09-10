import { AppBar, Box, Container, Toolbar, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { Outlet } from 'react-router';

/**
 * Uygulama kabugu: ust cubuk ve icerik alani.
 *
 * Gezinme menusu, yetkilendirme altyapisi kurulduktan sonra eklenecektir;
 * menu ogeleri kullanicinin yetkisine gore belirlenir (ADR-0007).
 */
export function AppLayout() {
  const { t } = useTranslation();

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="static" color="primary">
        <Toolbar>
          <Typography variant="h6" component="h1" sx={{ flexGrow: 1 }}>
            {t('uygulama.ad')}
          </Typography>
          <Typography variant="body2">{t('uygulama.kurum')}</Typography>
        </Toolbar>
      </AppBar>

      <Container component="main" maxWidth="lg" sx={{ py: 3, flexGrow: 1 }}>
        <Outlet />
      </Container>
    </Box>
  );
}
