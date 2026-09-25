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

export const router = createBrowserRouter([
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
