import { Box, Stack, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import BadgeOutlined from '@mui/icons-material/BadgeOutlined';
import LockOutlined from '@mui/icons-material/LockOutlined';
import VerifiedUserOutlined from '@mui/icons-material/VerifiedUserOutlined';
import { useTranslation } from 'react-i18next';
import { drift, fadeUp, float, glow, reducedMotion } from './motion';

/** Ag gorselinin dugumleri: kisiler ve birimler arasi bag (yuzde koordinat). */
const NODES = [
  { x: 18, y: 22, r: 4 },
  { x: 42, y: 12, r: 3 },
  { x: 70, y: 20, r: 5 },
  { x: 86, y: 42, r: 3 },
  { x: 62, y: 46, r: 6 },
  { x: 34, y: 44, r: 4 },
  { x: 14, y: 62, r: 3 },
  { x: 46, y: 70, r: 5 },
  { x: 78, y: 74, r: 4 },
  { x: 28, y: 86, r: 3 },
  { x: 60, y: 90, r: 3 },
] as const;

const LINKS: readonly [number, number][] = [
  [0, 1],
  [1, 2],
  [2, 3],
  [2, 4],
  [4, 5],
  [5, 0],
  [5, 6],
  [5, 7],
  [4, 7],
  [4, 8],
  [3, 8],
  [7, 8],
  [6, 9],
  [7, 9],
  [7, 10],
  [8, 10],
];

/**
 * Kurumsal gorsel (SYG-KMLK-068; IK karari S-13: "Claude tarafindan en uygun gorsel
 * uretilsin").
 *
 * Gorsel bir resim dosyasi degil, kodla cizilen bir sahnedir: kurum renklerinde gecisli
 * bir zemin, yavasca suzulen isik halkalari ve kisileri birbirine baglayan bir ag.
 * Ag, IK'nin isini (insanlari ve birimleri bir arada tutmak) ve laboratuvar kimligini
 * (molekul) birlikte cagristirir. Resim dosyasi yoktur: her ekran boyutunda keskin
 * kalir ve indirme yuku getirmez.
 *
 * Gorsel yalnizca susleme amaclidir; ekran okuyuculardan gizlenir. Anlam tasiyan tek
 * icerik metinlerdir.
 */
export function BrandPanel() {
  const { t } = useTranslation();

  const highlights = [
    { icon: <LockOutlined fontSize="small" />, text: t('identity.layout.highlightSecure') },
    { icon: <BadgeOutlined fontSize="small" />, text: t('identity.layout.highlightSingleAccount') },
    {
      icon: <VerifiedUserOutlined fontSize="small" />,
      text: t('identity.layout.highlightPrivacy'),
    },
  ];

  return (
    <Box
      component="aside"
      aria-label={t('identity.layout.panelTitle')}
      sx={(theme) => ({
        position: 'relative',
        overflow: 'hidden',
        isolation: 'isolate',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: { xs: 'flex-start', md: 'center' },
        px: { xs: 3, sm: 5, md: 8 },
        py: { xs: 5, md: 8 },
        color: 'common.white',
        background: `linear-gradient(145deg, ${theme.palette.primary.dark} 0%, ${theme.palette.primary.main} 48%, ${theme.palette.secondary.dark} 100%)`,
      })}
    >
      <Aurora />
      <Network />

      <Stack
        spacing={4}
        sx={{
          position: 'relative',
          maxWidth: 520,
          animation: `${fadeUp} 700ms ease-out both`,
          ...reducedMotion,
        }}
      >
        <Box>
          <Typography
            variant="overline"
            component="p"
            sx={{ letterSpacing: 2, opacity: 0.8, fontWeight: 600 }}
          >
            {t('app.organization')}
          </Typography>
          <Typography
            component="p"
            sx={{
              fontWeight: 700,
              lineHeight: 1.15,
              fontSize: { xs: '1.75rem', sm: '2.25rem', md: '2.75rem' },
            }}
          >
            {t('identity.layout.panelTitle')}
          </Typography>
          <Typography sx={{ mt: 2, opacity: 0.88, fontSize: { xs: '1rem', md: '1.125rem' } }}>
            {t('identity.layout.panelText')}
          </Typography>
        </Box>

        <Stack component="ul" spacing={1.5} sx={{ listStyle: 'none', m: 0, p: 0 }}>
          {highlights.map((item, index) => (
            <Stack
              key={item.text}
              component="li"
              direction="row"
              spacing={1.5}
              sx={(theme) => ({
                alignItems: 'center',
                px: 2,
                py: 1.25,
                borderRadius: 3,
                bgcolor: alpha(theme.palette.common.white, 0.1),
                border: `1px solid ${alpha(theme.palette.common.white, 0.16)}`,
                animation: `${fadeUp} 600ms ease-out ${200 + index * 120}ms both`,
                ...reducedMotion,
              })}
            >
              <Box
                sx={(theme) => ({
                  display: 'grid',
                  placeItems: 'center',
                  width: 32,
                  height: 32,
                  borderRadius: '50%',
                  bgcolor: alpha(theme.palette.common.white, 0.14),
                  flexShrink: 0,
                })}
              >
                {item.icon}
              </Box>
              <Typography variant="body2" sx={{ opacity: 0.95 }}>
                {item.text}
              </Typography>
            </Stack>
          ))}
        </Stack>
      </Stack>

      <Typography
        variant="caption"
        component="p"
        sx={{
          position: { xs: 'relative', md: 'absolute' },
          bottom: { md: 32 },
          left: { md: 64 },
          mt: { xs: 4, md: 0 },
          opacity: 0.6,
        }}
      >
        © {new Date().getFullYear()} {t('app.organization')}
      </Typography>
    </Box>
  );
}

/** Yavasca suzulen, bulanik gorunumlu isik halkalari. Filtre KULLANILMAZ; yumusaklik radyal gecisten gelir. */
function Aurora() {
  const blobs = [
    {
      top: '-20%',
      left: '-10%',
      size: '70vmax',
      color: 'secondary.light',
      delay: '0s',
      duration: '26s',
    },
    {
      top: '40%',
      left: '45%',
      size: '60vmax',
      color: 'primary.light',
      delay: '-8s',
      duration: '32s',
    },
    {
      top: '70%',
      left: '-20%',
      size: '50vmax',
      color: 'secondary.main',
      delay: '-16s',
      duration: '28s',
    },
  ];

  return (
    <Box aria-hidden sx={{ position: 'absolute', inset: 0, zIndex: -1, pointerEvents: 'none' }}>
      {blobs.map((blob) => (
        <Box
          key={blob.top + blob.left}
          sx={(theme) => {
            const [palette, shade] = blob.color.split('.') as [
              'primary' | 'secondary',
              'main' | 'light',
            ];
            return {
              position: 'absolute',
              top: blob.top,
              left: blob.left,
              width: blob.size,
              height: blob.size,
              borderRadius: '50%',
              background: `radial-gradient(circle, ${alpha(theme.palette[palette][shade], 0.45)} 0%, transparent 65%)`,
              willChange: 'transform',
              animation: `${drift} ${blob.duration} ease-in-out ${blob.delay} infinite`,
              ...reducedMotion,
            };
          }}
        />
      ))}
    </Box>
  );
}

/** Kisileri ve birimleri birbirine baglayan ag; dugumler sirayla hafifce parlar. */
function Network() {
  return (
    <Box
      aria-hidden
      sx={{
        position: 'absolute',
        inset: { xs: '-10% -30% auto auto', md: '8% -14% 8% auto' },
        width: { xs: '85%', md: '52%' },
        height: { xs: '70%', md: '90%' },
        zIndex: -1,
        opacity: { xs: 0.35, md: 0.6 },
        pointerEvents: 'none',
        animation: `${float} 14s ease-in-out infinite`,
        ...reducedMotion,
      }}
    >
      <svg viewBox="0 0 100 100" width="100%" height="100%" preserveAspectRatio="xMidYMid meet">
        <g stroke="currentColor" strokeOpacity="0.28" strokeWidth="0.35" fill="none">
          {LINKS.map(([a, b]) => (
            <line
              key={`${a}-${b}`}
              x1={NODES[a]!.x}
              y1={NODES[a]!.y}
              x2={NODES[b]!.x}
              y2={NODES[b]!.y}
            />
          ))}
          <circle cx="62" cy="46" r="14" strokeOpacity="0.14" />
          <circle cx="62" cy="46" r="22" strokeOpacity="0.08" />
        </g>
        {NODES.map((node, index) => (
          <Box
            key={`${node.x}-${node.y}`}
            component="circle"
            cx={node.x}
            cy={node.y}
            r={node.r * 0.35}
            fill="currentColor"
            sx={{
              animation: `${glow} ${5 + (index % 4)}s ease-in-out ${index * 0.6}s infinite`,
              ...reducedMotion,
            }}
          />
        ))}
      </svg>
    </Box>
  );
}
