import { lazy, Suspense } from 'react';
import { createBrowserRouter } from 'react-router';
import { AppLayout } from './AppLayout';
import { NotFoundPage } from './pages/NotFoundPage';
import { RouteFallback } from './pages/RouteFallback';

// Rota bazli kod bolme (ADR-0015 §11): ilk yukleme kucuk kalir, her modul
// yalnizca acildiginda indirilir. Modul sayisi 35'e ciktiginda bunun etkisi
// belirginlesir; sonradan eklemek ise tum rotalari elden gecirmeyi gerektirirdi.
const HomePage = lazy(async () => {
  const page = await import('@/features/health/pages/HomePage');

  return { default: page.HomePage };
});

const RegistrationPage = lazy(async () => {
  const page = await import('@/features/identity/pages/RegistrationPage');

  return { default: page.RegistrationPage };
});

/** Kimlik ekranlarinin yollari. Uygulama kabugunun (ust cubuk) DISINDADIR (SYG-KMLK-068). */
export const identityRoutes = {
  register: '/register',
} as const;

export const router = createBrowserRouter([
  {
    path: identityRoutes.register,
    element: (
      <Suspense fallback={<RouteFallback />}>
        <RegistrationPage />
      </Suspense>
    ),
  },
  {
    path: '/',
    element: <AppLayout />,
    children: [
      {
        index: true,
        element: (
          <Suspense fallback={<RouteFallback />}>
            <HomePage />
          </Suspense>
        ),
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
