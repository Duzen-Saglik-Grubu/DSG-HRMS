import type { ReactNode } from 'react';
import {
  Box,
  Card,
  CardActionArea,
  Chip,
  CircularProgress,
  Grid,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import { alpha } from '@mui/material/styles';
import ArrowForwardRounded from '@mui/icons-material/ArrowForwardRounded';
import BadgeOutlined from '@mui/icons-material/BadgeOutlined';
import DashboardOutlined from '@mui/icons-material/DashboardOutlined';
import LockResetRounded from '@mui/icons-material/LockResetRounded';
import ManageAccountsOutlined from '@mui/icons-material/ManageAccountsOutlined';
import TuneRounded from '@mui/icons-material/TuneRounded';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router';
import { fetchHealth } from '@/shared/api/health';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { permissions, type Permission } from '@/shared/auth/permissions';
import { useSession } from '@/shared/auth/session';
import { ErrorState } from '@/shared/components/ErrorState';
import { PageHeader } from '@/shared/components/PageHeader';
import { routes } from '@/shared/routes';

interface QuickLink {
  key: 'personnel' | 'accounts' | 'parameters' | 'password';
  to: string;
  icon: ReactNode;
  permission?: Permission;
}

const QUICK_LINKS: QuickLink[] = [
  {
    key: 'personnel',
    to: routes.personnel,
    icon: <BadgeOutlined />,
    permission: permissions.personView,
  },
  {
    key: 'accounts',
    to: routes.accounts,
    icon: <ManageAccountsOutlined />,
    permission: permissions.accountView,
  },
  {
    key: 'parameters',
    to: routes.parameters,
    icon: <TuneRounded />,
    permission: permissions.parameterView,
  },
  { key: 'password', to: routes.changePassword, icon: <LockResetRounded /> },
];

/** Gunun saatine gore selamlama. */
function greetingKey(hour: number): 'morning' | 'day' | 'evening' {
  if (hour < 12) {
    return 'morning';
  }

  return hour < 18 ? 'day' : 'evening';
}

/**
 * Ana sayfa (#309): karsilama, yetkiye gore hizli erisim kartlari ve sistem durumu.
 *
 * Ornek tasarimdaki (MainScreen.jpeg) bordro, devam ve duyuru gostergeleri bu modullerle gelir;
 * sistemde verisi olmayan bilgi gosterilmez.
 */
export function HomePage() {
  const { t } = useTranslation();
  const state = useSession();
  const now = new Date();
  const granted = state.status === 'authenticated' ? state.user.permissions : [];
  const links = QUICK_LINKS.filter((link) => !link.permission || granted.includes(link.permission));
  const firstName = state.status === 'authenticated' ? state.user.firstName : '';

  return (
    <Stack spacing={3}>
      <Box sx={{ '& > header': { mb: 0 } }}>
        <PageHeader
          headingLevel="h1"
          icon={DashboardOutlined}
          title={t(`home.greeting.${greetingKey(now.getHours())}`, { name: firstName })}
          description={`${format(now, 'd MMMM yyyy, EEEE', { locale: tr })} · ${t('home.subtitle')}`}
        />
      </Box>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <Typography variant="h6" component="h2" sx={{ mb: 1.5, fontWeight: 600 }}>
            {t('home.quickAccess')}
          </Typography>
          <Grid container spacing={2}>
            {links.map((link) => (
              <Grid key={link.key} size={{ xs: 12, sm: 6 }}>
                <QuickLinkCard link={link} />
              </Grid>
            ))}
          </Grid>
        </Grid>
        <Grid size={{ xs: 12, lg: 4 }}>
          <Typography variant="h6" component="h2" sx={{ mb: 1.5, fontWeight: 600 }}>
            {t('health.title')}
          </Typography>
          <HealthCard />
        </Grid>
      </Grid>
    </Stack>
  );
}

function QuickLinkCard({ link }: { link: QuickLink }) {
  const { t } = useTranslation();

  return (
    <Card
      variant="outlined"
      sx={(theme) => ({
        height: '100%',
        borderColor: 'transparent',
        transition: theme.transitions.create('box-shadow', { duration: 150 }),
        '&:hover': { boxShadow: theme.palette.surface.shadowHover },
      })}
    >
      <CardActionArea
        component={RouterLink}
        to={link.to}
        sx={{ height: '100%', p: 2.5, display: 'flex', alignItems: 'flex-start', gap: 2 }}
      >
        <Box
          sx={(theme) => ({
            display: 'grid',
            placeItems: 'center',
            width: 44,
            height: 44,
            flexShrink: 0,
            borderRadius: 2,
            color: 'primary.main',
            bgcolor: alpha(theme.palette.primary.main, 0.08),
          })}
        >
          {link.icon}
        </Box>
        <Box sx={{ flexGrow: 1, minWidth: 0 }}>
          <Typography variant="subtitle1" component="h3" sx={{ fontWeight: 600 }}>
            {t(`home.cards.${link.key}.title`)}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {t(`home.cards.${link.key}.text`)}
          </Typography>
        </Box>
        <ArrowForwardRounded fontSize="small" color="action" sx={{ alignSelf: 'center' }} />
      </CardActionArea>
    </Card>
  );
}

/** Sistem durumu: API ve veritabani saglik denetimi. */
function HealthCard() {
  const { t } = useTranslation();
  const { data, error, isPending, refetch } = useQuery({
    queryKey: queryKeys.saglik,
    queryFn: fetchHealth,
  });

  return (
    <Paper variant="outlined" sx={{ p: 2.5 }}>
      {isPending ? (
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <CircularProgress size={20} />
          <Typography variant="body2">{t('health.checking')}</Typography>
        </Stack>
      ) : null}

      {error ? (
        <ErrorState error={error as unknown as ApiError} onRetry={() => void refetch()} />
      ) : null}

      {data ? (
        <Stack spacing={1.5}>
          <Typography variant="body1" sx={{ fontWeight: 600 }}>
            {t('health.healthy')}
          </Typography>
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }} useFlexGap>
            {data.checks.map((check) => (
              <Chip
                key={check.name}
                label={`${check.name}: ${check.status}`}
                color={check.status === 'Healthy' ? 'success' : 'error'}
                variant="outlined"
                size="small"
              />
            ))}
          </Stack>
        </Stack>
      ) : null}
    </Paper>
  );
}
