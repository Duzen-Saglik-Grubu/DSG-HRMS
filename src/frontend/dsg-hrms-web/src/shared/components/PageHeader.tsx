import type { ReactNode } from 'react';
import { Box, Breadcrumbs, Link as MuiLink, Stack, Typography } from '@mui/material';
import { Link } from 'react-router';

export interface Breadcrumb {
  label: string;
  to?: string;
}

interface PageHeaderProps {
  title: string;
  description?: string;
  breadcrumbs?: Breadcrumb[];
  /** Sag ustte yer alan eylem dugmeleri. */
  actions?: ReactNode;
}

/**
 * Sayfa basligi (ADR-0015 §5).
 *
 * Her ekranin ayni yapida baslamasi, kullanicinin "neredeyim ve ne yapabilirim"
 * sorusunu her modulde ayni yerden cevaplamasini saglar. Modul basina ayri
 * yazilsaydi, baslik konumu ve dugme yerlesimi ekrandan ekrana degisirdi.
 */
export function PageHeader({ title, description, breadcrumbs, actions }: PageHeaderProps) {
  return (
    <Box component="header" sx={{ mb: 3 }}>
      {breadcrumbs && breadcrumbs.length > 0 ? (
        <Breadcrumbs sx={{ mb: 1 }} aria-label="sayfa yolu">
          {breadcrumbs.map((item) =>
            item.to ? (
              <MuiLink
                key={item.label}
                component={Link}
                to={item.to}
                underline="hover"
                color="inherit"
              >
                {item.label}
              </MuiLink>
            ) : (
              <Typography key={item.label} color="text.primary">
                {item.label}
              </Typography>
            ),
          )}
        </Breadcrumbs>
      ) : null}

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' } }}
      >
        <Box>
          {/* Sayfa basligi h1 DEGIL h2'dir: h1 uygulama kabugundadir ve sayfada
              tek bir h1 bulunur (erisilebilirlik - ADR-0015 §7). */}
          <Typography variant="h5" component="h2">
            {title}
          </Typography>

          {description ? (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              {description}
            </Typography>
          ) : null}
        </Box>

        {actions ? (
          <Stack direction="row" spacing={1}>
            {actions}
          </Stack>
        ) : null}
      </Stack>
    </Box>
  );
}
