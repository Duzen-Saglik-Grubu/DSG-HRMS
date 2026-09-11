import type { ReactNode } from 'react';
import { CssBaseline, ThemeProvider } from '@mui/material';
import { QueryClientProvider, type QueryClient } from '@tanstack/react-query';
import { theme } from '@/shared/theme/theme';
import { createQueryClient } from './queryClient';

interface AppProvidersProps {
  children: ReactNode;
  queryClient?: QueryClient;
}

/**
 * Uygulama genelinde gecerli saglayicilar.
 */
export function AppProviders({ children, queryClient }: AppProvidersProps) {
  return (
    <QueryClientProvider client={queryClient ?? createQueryClient()}>
      <ThemeProvider theme={theme}>
        {/* Tarayici varsayilanlarini sifirlar; tema tutarli calisir. */}
        <CssBaseline />
        {children}
      </ThemeProvider>
    </QueryClientProvider>
  );
}
