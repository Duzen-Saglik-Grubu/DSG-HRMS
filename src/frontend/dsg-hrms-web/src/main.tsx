import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router';
import { AppProviders } from '@/app/AppProviders';
import { router } from '@/app/router';
import '@/shared/i18n/i18n';

const kok = document.getElementById('root');

if (!kok) {
  throw new Error('Uygulama kok ogesi (#root) bulunamadi.');
}

createRoot(kok).render(
  <StrictMode>
    <AppProviders>
      <RouterProvider router={router} />
    </AppProviders>
  </StrictMode>,
);
