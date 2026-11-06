import { lazy, Suspense } from 'react';
import { createBrowserRouter } from 'react-router';
import { routes } from '@/shared/routes';
import { AppLayout } from './AppLayout';
import { NotFoundPage } from './pages/NotFoundPage';
import { RouteFallback } from './pages/RouteFallback';
import { PersonnelListRoute } from './PersonnelListRoute';
import { RequireSession } from './RequireSession';

// Rota bazli kod bolme (ADR-0015 §11): ilk yukleme kucuk kalir, her modul
// yalnizca acildiginda indirilir. Modul sayisi 35'e ciktiginda bunun etkisi
// belirginlesir; sonradan eklemek ise tum rotalari elden gecirmeyi gerektirirdi.
const HomePage = lazy(async () => {
  const page = await import('@/features/health/pages/HomePage');

  return { default: page.HomePage };
});

const LoginPage = lazy(async () => {
  const page = await import('@/features/identity/pages/LoginPage');

  return { default: page.LoginPage };
});

const ForgotPasswordPage = lazy(async () => {
  const page = await import('@/features/identity/pages/ForgotPasswordPage');

  return { default: page.ForgotPasswordPage };
});

const ChangePasswordPage = lazy(async () => {
  const page = await import('@/features/identity/pages/ChangePasswordPage');

  return { default: page.ChangePasswordPage };
});

const AccountSecurityPage = lazy(async () => {
  const page = await import('@/features/identity/pages/AccountSecurityPage');

  return { default: page.AccountSecurityPage };
});

const AccountsPage = lazy(async () => {
  const page = await import('@/features/identity/pages/AccountsPage');

  return { default: page.AccountsPage };
});

const InvitePage = lazy(async () => {
  const page = await import('@/features/identity/pages/InvitePage');

  return { default: page.InvitePage };
});

const ParametersPage = lazy(async () => {
  const page = await import('@/features/system/pages/ParametersPage');

  return { default: page.ParametersPage };
});

const ReferenceDataPage = lazy(async () => {
  const page = await import('@/features/reference/pages/ReferenceDataPage');

  return { default: page.ReferenceDataPage };
});

const PersonListPage = lazy(async () => {
  const page = await import('@/features/personnel/pages/PersonListPage');

  return { default: page.PersonListPage };
});

const PersonCardPage = lazy(async () => {
  const page = await import('@/features/personnel/pages/PersonCardPage');

  return { default: page.PersonCardPage };
});

const CorrectionsPage = lazy(async () => {
  const page = await import('@/features/personnel/pages/CorrectionsPage');

  return { default: page.CorrectionsPage };
});

const SyncRunsPage = lazy(async () => {
  const page = await import('@/features/personnel/pages/SyncRunsPage');
  return { default: page.SyncRunsPage };
});

const DataQualityPage = lazy(async () => {
  const page = await import('@/features/personnel/pages/DataQualityPage');

  return { default: page.DataQualityPage };
});

const ProfilePage = lazy(async () => {
  const page = await import('@/features/personnel/pages/ProfilePage');

  return { default: page.ProfilePage };
});

const RegistrationPage = lazy(async () => {
  const page = await import('@/features/identity/pages/RegistrationPage');

  return { default: page.RegistrationPage };
});

// Kimlik ekranlari uygulama kabugunun (ust cubuk) DISINDADIR (SYG-KMLK-068) ve oturum
// gerektirmez. Kabuk ve altindaki her sayfa oturum ister (SYG-KMLK-055).
export const router = createBrowserRouter([
  {
    path: routes.login,
    element: (
      <Suspense fallback={<RouteFallback />}>
        <LoginPage />
      </Suspense>
    ),
  },
  {
    path: routes.forgotPassword,
    element: (
      <Suspense fallback={<RouteFallback />}>
        <ForgotPasswordPage />
      </Suspense>
    ),
  },
  {
    path: routes.invite,
    element: (
      <Suspense fallback={<RouteFallback />}>
        <InvitePage />
      </Suspense>
    ),
  },
  {
    path: routes.register,
    element: (
      <Suspense fallback={<RouteFallback />}>
        <RegistrationPage />
      </Suspense>
    ),
  },
  {
    path: routes.home,
    element: (
      <RequireSession>
        <AppLayout />
      </RequireSession>
    ),
    children: [
      {
        index: true,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <HomePage />
          </Suspense>
        ),
      },
      {
        path: routes.changePassword,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <ChangePasswordPage />
          </Suspense>
        ),
      },
      {
        path: routes.accountSecurity,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <AccountSecurityPage />
          </Suspense>
        ),
      },
      {
        path: routes.accounts,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <AccountsPage />
          </Suspense>
        ),
      },
      {
        path: routes.parameters,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <ParametersPage />
          </Suspense>
        ),
      },
      {
        path: routes.referenceData,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <ReferenceDataPage />
          </Suspense>
        ),
      },
      {
        path: routes.personnel,
        element: (
          <PersonnelListRoute>
            <Suspense fallback={<RouteFallback />}>
              <PersonListPage />
            </Suspense>
          </PersonnelListRoute>
        ),
      },
      {
        // Statik yol, kisi kartinin dinamik yolundan once eslesir.
        path: routes.corrections,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <CorrectionsPage />
          </Suspense>
        ),
      },
      {
        path: routes.quality,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <DataQualityPage />
          </Suspense>
        ),
      },
      {
        path: routes.syncRuns,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <SyncRunsPage />
          </Suspense>
        ),
      },
      {
        path: `${routes.personnel}/:personId`,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <PersonCardPage />
          </Suspense>
        ),
      },
      {
        path: routes.profile,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <ProfilePage />
          </Suspense>
        ),
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
