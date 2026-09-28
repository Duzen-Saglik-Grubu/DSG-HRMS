import type { ReactNode } from 'react';
import { CssBaseline, ThemeProvider } from '@mui/material';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { tr } from 'date-fns/locale';
import { QueryClientProvider, type QueryClient } from '@tanstack/react-query';
import { theme } from '@/shared/theme/theme';
import { createQueryClient } from './queryClient';
import { SessionCacheReset } from './SessionCacheReset';

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
        <SessionCacheReset />

        {/*
          Tarih bileseni Turkce kullanir: gun ve ay adlari, haftanin ilk gunu
          (pazartesi) ve GG.AA.YYYY bicimi buradan gelir. Bicim her tarih
          alaninda ayri ayri ayarlansaydi, bir yerde unutulur ve kullanici
          ayni ekranda iki farkli tarih bicimi gorurdu.
        */}
        <LocalizationProvider dateAdapter={AdapterDateFns} adapterLocale={tr}>
          {children}
        </LocalizationProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
