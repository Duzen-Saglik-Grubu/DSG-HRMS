import type { ReactNode } from 'react';
import { Box, Breadcrumbs, Link as MuiLink, Stack, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import type { SvgIconComponent } from '@mui/icons-material';
import { Link } from 'react-router';

export interface Breadcrumb {
  label: string;
  to?: string;
}

interface PageHeaderProps {
  title: string;
  description?: ReactNode;
  breadcrumbs?: Breadcrumb[];
  /** Sag ustte yer alan eylem dugmeleri. */
  actions?: ReactNode;
  /** Basligin solundaki daire icindeki simge; genelde menudeki simgenin aynisi. */
  icon?: SvgIconComponent;
  /** Baslik duzeyi: sayfa basligi h2'dir; yalnizca kabugun h1'i olmayan ekranlar h1 kullanir. */
  headingLevel?: 'h1' | 'h2';
}

/**
 * Sayfa basligi (ADR-0015 §5; ornek tasarim MainScreen2.jpeg, #322).
 *
 * Her ekranin ayni yapida baslamasi, kullanicinin "neredeyim ve ne yapabilirim"
 * sorusunu her modulde ayni yerden cevaplamasini saglar. Modul basina ayri
 * yazilsaydi, baslik konumu ve dugme yerlesimi ekrandan ekrana degisirdi.
 *
 * Baslik, hafif gecisli ve dalga desenli bir seritte durur; desen yalnizca susleme amaclidir
 * ve ekran okuyuculardan gizlenir.
 */
export function PageHeader({
  title,
  description,
  breadcrumbs,
  actions,
  icon: Icon,
  headingLevel = 'h2',
}: PageHeaderProps) {
  return (
    <Box component="header" sx={{ mb: 2.5 }}>
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

      <Box
        sx={(theme) => ({
          position: 'relative',
          overflow: 'hidden',
          isolation: 'isolate',
          px: { xs: 2.5, sm: 3.5 },
          py: { xs: 1.75, sm: 2 },
          borderRadius: 3,
          border: `1px solid ${alpha(theme.palette.primary.main, 0.08)}`,
          background: `linear-gradient(110deg, ${theme.palette.background.paper} 0%, ${alpha(theme.palette.surface.accent, 0.07)} 50%, ${alpha(theme.palette.surface.accent, 0.16)} 100%)`,
          boxShadow: theme.palette.surface.shadow,
        })}
      >
        <Wave />

        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' } }}
        >
          <Stack direction="row" spacing={2} sx={{ alignItems: 'center', minWidth: 0 }}>
            {Icon ? (
              <Box
                aria-hidden
                sx={(theme) => ({
                  display: { xs: 'none', sm: 'grid' },
                  placeItems: 'center',
                  width: 48,
                  height: 48,
                  flexShrink: 0,
                  borderRadius: '50%',
                  color: 'primary.contrastText',
                  bgcolor: 'primary.main',
                  boxShadow: `0 0 0 5px ${alpha(theme.palette.primary.main, 0.08)}, 0 8px 20px -10px ${alpha(theme.palette.primary.main, 0.7)}`,
                })}
              >
                <Icon fontSize="small" />
              </Box>
            ) : null}

            <Box sx={{ minWidth: 0 }}>
              {/* Sayfa basligi h1 DEGIL h2'dir: h1 uygulama kabugundadir ve sayfada
                  tek bir h1 bulunur (erisilebilirlik - ADR-0015 §7). */}
              <Typography
                variant="h5"
                component={headingLevel}
                sx={{ fontWeight: 700, color: 'primary.main' }}
              >
                {title}
              </Typography>

              {description ? (
                <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
                  {description}
                </Typography>
              ) : null}
            </Box>
          </Stack>

          {actions ? (
            <Stack direction="row" spacing={1}>
              {actions}
            </Stack>
          ) : null}
        </Stack>
      </Box>
    </Box>
  );
}

/** Seridin sagindaki yumusak dalga deseni. */
function Wave() {
  return (
    <Box
      aria-hidden
      sx={{
        position: 'absolute',
        inset: 'auto 0 0 auto',
        width: { xs: '70%', md: '45%' },
        height: '100%',
        zIndex: -1,
        color: 'surface.accent',
        pointerEvents: 'none',
      }}
    >
      <svg viewBox="0 0 400 120" width="100%" height="100%" preserveAspectRatio="none">
        <path
          d="M0 120 C 70 112, 150 112, 240 86 S 360 26, 400 30 L 400 120 Z"
          fill="currentColor"
          fillOpacity="0.1"
        />
        <path
          d="M120 120 C 200 108, 250 104, 310 70 S 380 10, 400 0 L 400 120 Z"
          fill="currentColor"
          fillOpacity="0.12"
        />
      </svg>
    </Box>
  );
}
