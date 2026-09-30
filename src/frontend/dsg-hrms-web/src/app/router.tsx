import { lazy, Suspense } from 'react';
import { createBrowserRouter } from 'react-router';
import { routes } from '@/shared/routes';
import { AppLayout } from './AppLayout';
import { NotFoundPage } from './pages/NotFoundPage';
import { RouteFallback } from './pages/RouteFallback';
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
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
