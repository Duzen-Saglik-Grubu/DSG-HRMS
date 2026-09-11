import { QueryClient } from '@tanstack/react-query';

/**
 * TanStack Query istemcisi (ADR-0015 §2).
 */
export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Kurum ici bir IK sisteminde veri saniyede bir degismez; her odaklanmada
        // yeniden cekmek gereksiz yuk ve titreyen arayuz uretir.
        staleTime: 60_000,
        refetchOnWindowFocus: false,

        // Yetki ve bulunamadi hatalarinda yeniden denemek anlamsizdir; yalnizca
        // gecici sorunlar (5xx) icin bir kez denenir.
        retry: (failureCount, error) => {
          const status = (error as { status?: number }).status;

          if (status !== undefined && status < 500) {
            return false;
          }

          return failureCount < 1;
        },
      },
    },
  });
}
