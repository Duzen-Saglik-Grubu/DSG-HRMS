import { CircularProgress, Stack } from '@mui/material';

/**
 * Rota yuklenirken gosterilen ara durum.
 *
 * Bos ekran gosterilmez (ADR-0015 §6): kullanici uygulamanin donduğunu degil,
 * calistigini gormelidir.
 */
export function RouteFallback() {
  return (
    <Stack sx={{ alignItems: 'center', py: 6 }}>
      <CircularProgress />
    </Stack>
  );
}
