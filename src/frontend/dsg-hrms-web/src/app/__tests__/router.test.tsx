import { describe, expect, it } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { QueryClient } from '@tanstack/react-query';
import { AppProviders } from '../AppProviders';
import { AppLayout } from '../AppLayout';
import { NotFoundPage } from '../pages/NotFoundPage';

/**
 * Uygulama kabugu ve yonlendirme.
 */
function renderAt(path: string) {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: <AppLayout />,
        children: [
          { index: true, element: <p>Ana içerik</p> },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
    { initialEntries: [path] },
  );

  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <AppProviders queryClient={queryClient}>
      <RouterProvider router={router} />
    </AppProviders>,
  );
}

describe('uygulama kabugu', () => {
  it('kurum kimligini ust cubukta gosterir', async () => {
    renderAt('/');

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1, name: 'DSG-HRMS' })).toBeInTheDocument();
    });

    expect(screen.getByText('Düzen Sağlık Grubu')).toBeInTheDocument();
  });

  it('eslesmeyen adreste ne yapilacagini soyler', async () => {
    // Bos bir "404" ekrani kullaniciyi cikmaza sokar (ADR-0015 §6).
    renderAt('/olmayan-sayfa');

    await waitFor(() => {
      expect(screen.getByText('Sayfa bulunamadı')).toBeInTheDocument();
    });

    expect(screen.getByRole('link', { name: 'Ana sayfaya dön' })).toBeInTheDocument();
  });
});
