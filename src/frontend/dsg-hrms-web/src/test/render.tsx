import type { ReactElement, ReactNode } from 'react';
import { render, type RenderOptions, type RenderResult } from '@testing-library/react';
import { QueryClient } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router';
import { AppProviders } from '@/app/AppProviders';

/**
 * Bilesenleri UYGULAMADAKI saglayicilarla birlikte render eder.
 *
 * Saglayicisiz render, gercekte olmayan bir ortamda test yapmak demektir:
 * MUI'nin Turkce metinleri, tema ve tarih bicimi tema saglayicisindan gelir.
 * Bir test saglayicisiz gectiginde, uygulamada farkli davranan bir bilesen
 * "dogrulanmis" sayilirdi.
 */
export function renderWithProviders(
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>,
): RenderResult {
  const queryClient = new QueryClient({
    // Testlerde yeniden deneme yok: basarisiz bir istek aninda sonuclanmalidir.
    defaultOptions: { queries: { retry: false } },
  });

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <AppProviders queryClient={queryClient}>
        <MemoryRouter>{children}</MemoryRouter>
      </AppProviders>
    );
  }

  return render(ui, { wrapper: Wrapper, ...options });
}
