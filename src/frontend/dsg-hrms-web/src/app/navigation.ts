import type { SvgIconComponent } from '@mui/icons-material';
import AccountCircleOutlined from '@mui/icons-material/AccountCircleOutlined';
import BadgeOutlined from '@mui/icons-material/BadgeOutlined';
import DashboardOutlined from '@mui/icons-material/DashboardOutlined';
import FactCheckOutlined from '@mui/icons-material/FactCheckOutlined';
import CloudSyncOutlined from '@mui/icons-material/CloudSyncOutlined';
import RuleRounded from '@mui/icons-material/RuleRounded';
import ManageAccountsOutlined from '@mui/icons-material/ManageAccountsOutlined';
import TuneRounded from '@mui/icons-material/TuneRounded';
import { permissions, type Permission } from '@/shared/auth/permissions';
import { routes } from '@/shared/routes';

/** Sol menudeki bir baglanti. */
export interface NavItem {
  to: string;
  /** Ceviri anahtari. */
  label: string;
  icon: SvgIconComponent;
  /** Baglantiyi gormek icin gereken izin; yoksa herkes gorur. */
  permission?: Permission;
}

/** Basligi olan bir menu bolumu (ornek tasarim MainScreen.jpeg, #309). */
export interface NavSection {
  key: string;
  /** Ceviri anahtari. */
  label: string;
  items: NavItem[];
}

/**
 * Sol menunun tamami. Baglantilar izne gore gorunur; izin yalnizca GOSTERIM icindir, asil
 * denetim sunucudadir (ADR-0007). Ogesi gorunmeyen bolumun basligi da gorunmez.
 */
export const navigation: NavSection[] = [
  {
    key: 'overview',
    label: 'nav.sections.overview',
    items: [{ to: routes.home, label: 'nav.home', icon: DashboardOutlined }],
  },
  {
    key: 'people',
    label: 'nav.sections.people',
    items: [
      // Herkes kendi kartini acar (SYG-PERS-044).
      { to: routes.profile, label: 'nav.profile', icon: AccountCircleOutlined },
      {
        to: routes.personnel,
        label: 'nav.personnel',
        icon: BadgeOutlined,
        permission: permissions.personView,
      },
      {
        to: routes.corrections,
        label: 'nav.corrections',
        icon: FactCheckOutlined,
        permission: permissions.correctionView,
      },
      {
        to: routes.quality,
        label: 'nav.quality',
        icon: RuleRounded,
        permission: permissions.qualityView,
      },
      {
        to: routes.syncRuns,
        label: 'nav.syncRuns',
        icon: CloudSyncOutlined,
        permission: permissions.syncView,
      },
    ],
  },
  {
    key: 'system',
    label: 'nav.sections.system',
    items: [
      {
        to: routes.accounts,
        label: 'nav.accounts',
        icon: ManageAccountsOutlined,
        permission: permissions.accountView,
      },
      {
        to: routes.parameters,
        label: 'nav.parameters',
        icon: TuneRounded,
        permission: permissions.parameterView,
      },
    ],
  },
];

/** Menude yer almayan ama konum yolunda adi gorunen sayfalar. */
export const accountPages: Record<string, string> = {
  [routes.changePassword]: 'session.changePassword',
  [routes.accountSecurity]: 'session.accountSecurity',
};

/**
 * Konum yolu: adrese uyan menu ogesi ve bolumu. Ana sayfa yalnizca tam eslesmede secilir;
 * digerleri alt yollarda da (ornegin ileride kisi karti) secili kalir.
 */
export function findNavItem(
  pathname: string,
): { section: NavSection; item: NavItem } | { account: string } | null {
  // En uzun eslesen yol kazanir: /personnel/corrections, /personnel altinda olsa da kendi ogesidir.
  let best: { section: NavSection; item: NavItem } | null = null;
  for (const section of navigation) {
    for (const item of section.items) {
      const matches =
        item.to === routes.home
          ? pathname === routes.home
          : pathname === item.to || pathname.startsWith(`${item.to}/`);
      if (matches && (!best || item.to.length > best.item.to.length)) {
        best = { section, item };
      }
    }
  }

  if (best) {
    return best;
  }

  const account = accountPages[pathname];
  return account ? { account } : null;
}
