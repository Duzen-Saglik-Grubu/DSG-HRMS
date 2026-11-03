import { useState, type FormEvent } from 'react';
import {
  Box,
  Breadcrumbs,
  Button,
  Divider,
  Drawer,
  IconButton,
  InputAdornment,
  ListItemIcon,
  Menu,
  MenuItem,
  TextField,
  Tooltip,
  Typography,
  useMediaQuery,
} from '@mui/material';
import { alpha, useTheme, type Theme } from '@mui/material/styles';
import AccountCircleOutlined from '@mui/icons-material/AccountCircleOutlined';
import ChevronRightRounded from '@mui/icons-material/ChevronRightRounded';
import HomeRounded from '@mui/icons-material/HomeRounded';
import KeyboardDoubleArrowLeftRounded from '@mui/icons-material/KeyboardDoubleArrowLeftRounded';
import KeyboardDoubleArrowRightRounded from '@mui/icons-material/KeyboardDoubleArrowRightRounded';
import LockResetRounded from '@mui/icons-material/LockResetRounded';
import LogoutRounded from '@mui/icons-material/LogoutRounded';
import MenuRounded from '@mui/icons-material/MenuRounded';
import SearchRounded from '@mui/icons-material/SearchRounded';
import SecurityOutlined from '@mui/icons-material/SecurityOutlined';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router';
import { fetchPublicSettings } from '@/shared/api/publicSettings';
import { queryKeys } from '@/shared/api/queryKeys';
import { permissions } from '@/shared/auth/permissions';
import { session, useSession } from '@/shared/auth/session';
import { SessionActivity } from '@/shared/auth/SessionActivity';
import { PersonAvatar } from '@/shared/components/PersonAvatar';
import { usePermission } from '@/shared/auth/usePermission';
import { DEFAULT_LOGO_URL, uploadedLogoUrl } from '@/shared/branding';
import { routes } from '@/shared/routes';
import { findNavItem, navigation } from './navigation';

/** Acik ve daraltilmis sol menu genisligi. */
const SIDEBAR_WIDTH = 264;
const SIDEBAR_MINI_WIDTH = 76;

/** Daraltma tercihinin tarayicida tutuldugu anahtar (yalnizca gorunum tercihi). */
const COLLAPSED_KEY = 'dsg-hrms.sidebar-collapsed';

function readCollapsed(): boolean {
  try {
    return localStorage.getItem(COLLAPSED_KEY) === '1';
  } catch {
    return false;
  }
}

function writeCollapsed(value: boolean): void {
  try {
    localStorage.setItem(COLLAPSED_KEY, value ? '1' : '0');
  } catch {
    // Tarayici depolamaya izin vermiyorsa tercih yalnizca bu oturumda gecerlidir.
  }
}

/** Sol menunun koyu, gecisli zemini; telefondaki cekmecede de ayni. */
const sidebarSurface = (theme: Theme) => ({
  color: theme.palette.navigation.text,
  background: `linear-gradient(180deg, ${theme.palette.navigation.backgroundTop} 0%, ${theme.palette.navigation.backgroundBottom} 100%)`,
});

/** Koyu zemin uzerinde ikincil metin ve ince cizgiler (beyazin saydami). */
const onDark = (theme: Theme, opacity: number) => alpha(theme.palette.navigation.text, opacity);

/**
 * Uygulama kabugu (ornek tasarimlar MainScreen.jpeg #309, MainScreen2.jpeg #322): koyu zeminli,
 * bolum basliklari olan sol menu; konum yolu ve hizli arama bulunan ust cubuk; icerik alani.
 * Menunun lacivert zemini giris ekraninin kurumsal paneliyle uyumludur.
 *
 * Genis ekranda menu sabittir ve daraltilabilir; telefonda ust cubuktaki dugmeyle acilan cekmece
 * olur. Menu ogeleri izne gore gorunur (ADR-0007); asil denetim sunucudadir.
 */
export function AppLayout() {
  const theme = useTheme();
  // Ilk cizimde dogru duzen secilir (titreme olmaz); matchMedia olmayan ortamda genis duzen.
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true, defaultMatches: true });
  const [collapsed, setCollapsed] = useState(readCollapsed);
  const [mobileOpen, setMobileOpen] = useState(false);
  const mini = desktop && collapsed;
  const width = mini ? SIDEBAR_MINI_WIDTH : SIDEBAR_WIDTH;

  const toggle = () => {
    setCollapsed((current) => {
      writeCollapsed(!current);
      return !current;
    });
  };

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>
      {desktop ? (
        <Box
          component="nav"
          sx={{
            width,
            flexShrink: 0,
            transition: theme.transitions.create('width'),
          }}
        >
          <Box
            sx={{
              position: 'fixed',
              insetBlock: 0,
              left: 0,
              width,
              transition: theme.transitions.create('width'),
              zIndex: theme.zIndex.appBar,
              ...sidebarSurface(theme),
            }}
          >
            <Sidebar mini={mini} onToggle={toggle} />
          </Box>
        </Box>
      ) : (
        <Drawer
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          slotProps={{ paper: { sx: (t) => ({ width: SIDEBAR_WIDTH, ...sidebarSurface(t) }) } }}
        >
          <Box component="nav" sx={{ height: '100%' }}>
            <Sidebar mini={false} onNavigate={() => setMobileOpen(false)} />
          </Box>
        </Drawer>
      )}

      <Box sx={{ flexGrow: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        <TopBar onOpenMenu={desktop ? undefined : () => setMobileOpen(true)} />
        <Box
          component="main"
          sx={{ flexGrow: 1, px: { xs: 2, sm: 3, lg: 4 }, py: 3, maxWidth: 1600, width: '100%' }}
        >
          <Outlet />
        </Box>
      </Box>

      <SessionActivity />
    </Box>
  );
}

interface SidebarProps {
  mini: boolean;
  onToggle?: () => void;
  onNavigate?: () => void;
}

/** Logo, bolumlu gezinme ve altta kullanici karti ile cikis. */
function Sidebar({ mini, onToggle, onNavigate }: SidebarProps) {
  const { t } = useTranslation();
  const state = useSession();
  const granted = state.status === 'authenticated' ? state.user.permissions : [];
  const sections = navigation
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => !item.permission || granted.includes(item.permission)),
    }))
    .filter((section) => section.items.length > 0);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Brand mini={mini} onToggle={onToggle} />

      <Box sx={{ flexGrow: 1, overflowY: 'auto', overflowX: 'hidden', px: mini ? 1.5 : 2, py: 1 }}>
        {sections.map((section) => (
          <Box key={section.key} component="section" aria-label={t(section.label)} sx={{ mb: 2 }}>
            {mini ? (
              <Box sx={{ height: 12 }} />
            ) : (
              <Typography
                variant="overline"
                component="h2"
                sx={(theme) => ({
                  display: 'block',
                  px: 1.5,
                  color: onDark(theme, 0.62),
                  fontWeight: 600,
                  letterSpacing: '0.08em',
                })}
              >
                {t(section.label)}
              </Typography>
            )}
            <Box component="ul" sx={{ listStyle: 'none', m: 0, p: 0 }}>
              {section.items.map((item) => (
                <Box component="li" key={item.to} sx={{ mb: 0.5 }}>
                  <NavEntry item={item} mini={mini} onNavigate={onNavigate} />
                </Box>
              ))}
            </Box>
          </Box>
        ))}
      </Box>

      <UserPanel mini={mini} />
    </Box>
  );
}

function Brand({ mini, onToggle }: { mini: boolean; onToggle: (() => void) | undefined }) {
  const { t } = useTranslation();
  const [logoFailed, setLogoFailed] = useState(false);
  const settings = useQuery({
    queryKey: queryKeys.identity.publicSettings,
    queryFn: fetchPublicSettings,
    staleTime: 5 * 60_000,
  });
  // Yuklu logo varsa o, yoksa veya yuklenemezse depodaki varsayilan logo (SYG-KMLK-069).
  const version = settings.data?.logoVersion;
  const logoUrl = version && !logoFailed ? uploadedLogoUrl(version) : DEFAULT_LOGO_URL;

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 1.5,
        px: mini ? 1.5 : 2.5,
        height: 80,
        justifyContent: mini ? 'center' : 'flex-start',
      }}
    >
      {mini ? null : (
        <>
          {/* Logo beyaz bir zeminde: yuklenen logonun renkleri koyu menude de okunur. */}
          <Box
            sx={{
              display: 'grid',
              placeItems: 'center',
              width: 44,
              height: 44,
              flexShrink: 0,
              borderRadius: 2,
              bgcolor: 'background.paper',
            }}
          >
            <Box
              component="img"
              src={logoUrl}
              alt={t('app.organization')}
              onError={() => setLogoFailed(true)}
              sx={{ width: 36, height: 36, objectFit: 'contain' }}
            />
          </Box>
          <Box sx={{ flexGrow: 1, minWidth: 0 }}>
            <Typography
              variant="subtitle1"
              component="p"
              sx={{ fontWeight: 800, lineHeight: 1.2, letterSpacing: '0.02em' }}
            >
              {t('app.name')}
            </Typography>
            <Typography
              variant="caption"
              noWrap
              component="p"
              sx={(theme) => ({ color: onDark(theme, 0.72) })}
            >
              {t('app.organization')}
            </Typography>
          </Box>
        </>
      )}
      {onToggle ? (
        <Tooltip title={mini ? t('nav.expand') : t('nav.collapse')} placement="right">
          <IconButton
            size="small"
            onClick={onToggle}
            aria-label={mini ? t('nav.expand') : t('nav.collapse')}
            sx={(theme) => ({
              color: 'inherit',
              borderRadius: '50%',
              bgcolor: onDark(theme, 0.1),
              '&:hover': { bgcolor: onDark(theme, 0.18) },
            })}
          >
            {mini ? (
              <KeyboardDoubleArrowRightRounded fontSize="small" />
            ) : (
              <KeyboardDoubleArrowLeftRounded fontSize="small" />
            )}
          </IconButton>
        </Tooltip>
      ) : null}
    </Box>
  );
}

function NavEntry({
  item,
  mini,
  onNavigate,
}: {
  item: (typeof navigation)[number]['items'][number];
  mini: boolean;
  onNavigate: (() => void) | undefined;
}) {
  const { t } = useTranslation();
  const Icon = item.icon;
  const label = t(item.label);

  const link = (
    <Box
      component={NavLink}
      to={item.to}
      end={item.to === routes.home}
      onClick={onNavigate}
      aria-label={mini ? label : undefined}
      sx={(theme) => ({
        display: 'flex',
        alignItems: 'center',
        gap: 1.5,
        px: 1.5,
        py: 1.25,
        justifyContent: mini ? 'center' : 'flex-start',
        borderRadius: 2.5,
        color: onDark(theme, 0.82),
        textDecoration: 'none',
        fontWeight: 500,
        transition: theme.transitions.create(['background-color', 'color', 'box-shadow']),
        '&:hover': { bgcolor: onDark(theme, 0.08), color: theme.palette.navigation.text },
        '&:focus-visible': {
          outline: `2px solid ${theme.palette.navigation.text}`,
          outlineOffset: 2,
        },
        '&.active': {
          bgcolor: theme.palette.navigation.active,
          color: theme.palette.navigation.text,
          fontWeight: 600,
          boxShadow: `0 8px 20px -8px ${alpha(theme.palette.navigation.active, 0.9)}`,
        },
      })}
    >
      <Icon fontSize="small" />
      {mini ? null : <Box component="span">{label}</Box>}
    </Box>
  );

  return mini ? (
    <Tooltip title={label} placement="right">
      {link}
    </Tooltip>
  ) : (
    link
  );
}

/**
 * Oturumdaki kullanici, parola degisikligi, hesap guvenligi ve cikis (SYG-KMLK-043, 048, 080).
 * Hesap guvenligi yalnizca iki adimli dogrulama sistemde kullaniliyorsa gorunur; sayfa yine de
 * adresiyle acilabilir.
 */
function UserPanel({ mini }: { mini: boolean }) {
  const { t } = useTranslation();
  const state = useSession();
  const navigate = useNavigate();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const settings = useQuery({
    queryKey: queryKeys.identity.publicSettings,
    queryFn: fetchPublicSettings,
    staleTime: 5 * 60_000,
    enabled: state.status === 'authenticated',
  });
  const twoFactorAvailable = settings.data?.twoFactorAvailable === true;

  if (state.status !== 'authenticated') {
    return null;
  }

  const name = `${state.user.firstName} ${state.user.lastName}`;
  const go = (to: string) => {
    setAnchor(null);
    void navigate(to);
  };

  return (
    <Box sx={{ p: mini ? 1.5 : 2 }}>
      <Button
        color="inherit"
        fullWidth
        aria-label={name}
        aria-haspopup="menu"
        aria-expanded={anchor ? 'true' : undefined}
        aria-controls={anchor ? 'user-menu' : undefined}
        onClick={(event) => setAnchor(event.currentTarget)}
        sx={(theme) => ({
          justifyContent: mini ? 'center' : 'flex-start',
          gap: 1.5,
          p: 1,
          minWidth: 0,
          border: 1,
          borderColor: onDark(theme, 0.12),
          borderRadius: 2.5,
          bgcolor: onDark(theme, 0.06),
          textAlign: 'left',
          '&:hover': { bgcolor: onDark(theme, 0.12) },
        })}
      >
        <PersonAvatar
          source="me"
          hasPhoto={state.user.hasPhoto}
          firstName={state.user.firstName}
          lastName={state.user.lastName}
          size={36}
        />
        {mini ? null : (
          <>
            <Box sx={{ flexGrow: 1, minWidth: 0 }}>
              <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap>
                {name}
              </Typography>
              {/* Tasarimdaki gibi adin hemen altinda giris e-postasi (#311). */}
              <Typography
                variant="caption"
                noWrap
                component="p"
                sx={(theme) => ({ color: onDark(theme, 0.72) })}
              >
                {state.user.email ?? t('nav.myAccount')}
              </Typography>
            </Box>
            <ChevronRightRounded
              fontSize="small"
              sx={(theme) => ({ color: onDark(theme, 0.72) })}
            />
          </>
        )}
      </Button>
      <Menu
        id="user-menu"
        anchorEl={anchor}
        open={Boolean(anchor)}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
        transformOrigin={{ vertical: 'bottom', horizontal: 'left' }}
      >
        {/* Sol menudeki "Profilim" kullanici menusunden de acilir (#311). */}
        <MenuItem onClick={() => go(routes.profile)}>
          <ListItemIcon>
            <AccountCircleOutlined fontSize="small" />
          </ListItemIcon>
          {t('nav.profile')}
        </MenuItem>
        <Divider />
        <MenuItem onClick={() => go(routes.changePassword)}>
          <ListItemIcon>
            <LockResetRounded fontSize="small" />
          </ListItemIcon>
          {t('session.changePassword')}
        </MenuItem>
        {twoFactorAvailable ? (
          <MenuItem onClick={() => go(routes.accountSecurity)}>
            <ListItemIcon>
              <SecurityOutlined fontSize="small" />
            </ListItemIcon>
            {t('session.accountSecurity')}
          </MenuItem>
        ) : null}
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
      {mini ? null : (
        <Button
          fullWidth
          color="inherit"
          endIcon={<LogoutRounded fontSize="small" />}
          onClick={() => void session.signOut()}
          sx={(theme) => ({
            mt: 1.5,
            py: 1,
            borderRadius: 2.5,
            fontWeight: 600,
            border: 1,
            borderColor: onDark(theme, 0.12),
            bgcolor: onDark(theme, 0.1),
            '&:hover': { bgcolor: onDark(theme, 0.18) },
          })}
        >
          {t('session.signOut')}
        </Button>
      )}
    </Box>
  );
}

/**
 * Ust cubuk: konum yolu ve hizli personel aramasi. Arama, personeli gorebilen kullaniciya sunulur
 * ve personel listesini o terimle acar (SYG-PERS-030).
 */
function TopBar({ onOpenMenu }: { onOpenMenu: (() => void) | undefined }) {
  const { t } = useTranslation();
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const canSearch = usePermission(permissions.personView);
  const [term, setTerm] = useState('');
  const found = findNavItem(pathname);

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const value = term.trim();
    if (value) {
      void navigate(`${routes.personnel}?q=${encodeURIComponent(value)}`);
      setTerm('');
    }
  };

  return (
    <Box
      component="header"
      sx={{
        position: 'sticky',
        top: 0,
        zIndex: (theme) => theme.zIndex.appBar - 1,
        display: 'flex',
        alignItems: 'center',
        gap: 2,
        px: { xs: 2, sm: 3, lg: 4 },
        height: 76,
        // Zeminle butunlesik; kaydirilan icerik altinda hafifce bulaniklasir.
        bgcolor: (theme) => alpha(theme.palette.background.default, 0.86),
        backdropFilter: 'blur(8px)',
      }}
    >
      {onOpenMenu ? (
        <IconButton onClick={onOpenMenu} aria-label={t('nav.openMenu')} edge="start">
          <MenuRounded />
        </IconButton>
      ) : null}

      <Box
        aria-hidden
        sx={{
          display: { xs: 'none', sm: 'grid' },
          placeItems: 'center',
          width: 40,
          height: 40,
          flexShrink: 0,
          borderRadius: '50%',
          color: 'primary.main',
          bgcolor: 'background.paper',
          boxShadow: (theme) => theme.palette.surface.shadow,
        }}
      >
        <HomeRounded fontSize="small" />
      </Box>

      <Breadcrumbs aria-label={t('nav.breadcrumb')} sx={{ flexShrink: 0, minWidth: 0 }}>
        {found && 'section' in found ? (
          <Typography color="text.secondary">{t(found.section.label)}</Typography>
        ) : null}
        {found && 'account' in found ? (
          <Typography color="text.secondary">{t('nav.myAccount')}</Typography>
        ) : null}
        <Typography color="text.primary" sx={{ fontWeight: 600 }}>
          {found && 'section' in found
            ? t(found.item.label)
            : found
              ? t(found.account)
              : t('app.name')}
        </Typography>
      </Breadcrumbs>

      <Box sx={{ flexGrow: 1 }} />

      {canSearch ? (
        <Box
          component="form"
          role="search"
          onSubmit={submit}
          sx={{ width: { xs: 0, sm: 280, lg: 380 }, display: { xs: 'none', sm: 'block' } }}
        >
          <TextField
            value={term}
            onChange={(event) => setTerm(event.target.value)}
            placeholder={t('nav.searchPersonnel')}
            slotProps={{
              htmlInput: { 'aria-label': t('nav.searchPersonnel'), maxLength: 100 },
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchRounded fontSize="small" />
                  </InputAdornment>
                ),
                sx: (theme) => ({
                  borderRadius: 999,
                  bgcolor: 'background.paper',
                  boxShadow: theme.palette.surface.shadow,
                  '& fieldset': { borderColor: alpha(theme.palette.primary.main, 0.12) },
                }),
              },
            }}
          />
        </Box>
      ) : null}
    </Box>
  );
}
