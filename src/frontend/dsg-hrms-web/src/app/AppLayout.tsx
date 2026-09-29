import { useState } from 'react';
import {
  AppBar,
  Box,
  Button,
  Container,
  ListItemIcon,
  Menu,
  MenuItem,
  Toolbar,
  Typography,
} from '@mui/material';
import AccountCircleOutlined from '@mui/icons-material/AccountCircleOutlined';
import LockResetRounded from '@mui/icons-material/LockResetRounded';
import LogoutRounded from '@mui/icons-material/LogoutRounded';
import { useTranslation } from 'react-i18next';
import { Outlet, useNavigate } from 'react-router';
import { session, useSession } from '@/shared/auth/session';
import { SessionActivity } from '@/shared/auth/SessionActivity';
import { routes } from '@/shared/routes';

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
        <Toolbar sx={{ gap: 2 }}>
          <Typography variant="h6" component="h1" sx={{ flexGrow: 1 }}>
            {t('app.name')}
          </Typography>
          <Typography variant="body2" sx={{ display: { xs: 'none', sm: 'block' } }}>
            {t('app.organization')}
          </Typography>
          <UserMenu />
        </Toolbar>
      </AppBar>

      <Container component="main" maxWidth="lg" sx={{ py: 3, flexGrow: 1 }}>
        <Outlet />
      </Container>

      <SessionActivity />
    </Box>
  );
}

/** Oturumdaki kullanicinin adi, parola degisikligi ve cikis (SYG-KMLK-043, 048). */
function UserMenu() {
  const { t } = useTranslation();
  const state = useSession();
  const navigate = useNavigate();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  if (state.status !== 'authenticated') {
    return null;
  }

  const name = `${state.user.firstName} ${state.user.lastName}`;

  return (
    <>
      <Button
        color="inherit"
        startIcon={<AccountCircleOutlined />}
        aria-haspopup="menu"
        aria-expanded={anchor ? 'true' : undefined}
        aria-controls={anchor ? 'user-menu' : undefined}
        onClick={(event) => setAnchor(event.currentTarget)}
        sx={{ textTransform: 'none', minWidth: 0 }}
      >
        <Box
          component="span"
          sx={{
            maxWidth: { xs: 120, sm: 240 },
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          {name}
        </Box>
      </Button>
      <Menu id="user-menu" anchorEl={anchor} open={Boolean(anchor)} onClose={() => setAnchor(null)}>
        <MenuItem
          onClick={() => {
            setAnchor(null);
            void navigate(routes.changePassword);
          }}
        >
          <ListItemIcon>
            <LockResetRounded fontSize="small" />
          </ListItemIcon>
          {t('session.changePassword')}
        </MenuItem>
        <MenuItem
          onClick={() => {
            setAnchor(null);
            void session.signOut();
          }}
        >
          <ListItemIcon>
            <LogoutRounded fontSize="small" />
          </ListItemIcon>
          {t('session.signOut')}
        </MenuItem>
      </Menu>
    </>
  );
}
