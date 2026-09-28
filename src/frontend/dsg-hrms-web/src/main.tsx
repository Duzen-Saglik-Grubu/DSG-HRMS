import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router';
import { AppProviders } from '@/app/AppProviders';
import { router } from '@/app/router';
import { session } from '@/shared/auth/session';
import '@/shared/i18n/i18n';

// Erisim jetonu yalnizca bellekte tutuldugu icin sayfa her acildiginda oturum, yenileme
// cereziyle geri getirilir (ADR-0006 §8). Istek sayfa cizilirken paralel gider.
void session.restore();

const rootElement = document.getElementById('root');

if (!rootElement) {
  throw new Error('Uygulama kok ogesi (#root) bulunamadi.');
}

createRoot(rootElement).render(
  <StrictMode>
    <AppProviders>
      <RouterProvider router={router} />
    </AppProviders>
  </StrictMode>,
);
