import { keyframes } from '@emotion/react';
import type { SystemStyleObject, Theme } from '@mui/system';

/**
 * Kimlik ekranlarinin animasyonlari.
 *
 * Kurallar (tarayiciyi yormamak icin):
 * - Yalnizca `transform` ve `opacity` canlandirilir; bunlar yerlesim hesaplatmaz ve
 *   ekran kartinda calisir. `filter`, `box-shadow`, `width` gibi ozellikler
 *   canlandirilmaz.
 * - Surekli animasyonlar yavas (20 sn ve ustu) ve az sayidadir.
 * - Kullanici isletim sisteminde "hareketi azalt" secmisse HICBIR animasyon calismaz
 *   (`prefers-reduced-motion`): hareket bazi kullanicilarda bas donmesi yapar.
 */
export const fadeUp = keyframes`
  from { opacity: 0; transform: translate3d(0, 16px, 0); }
  to   { opacity: 1; transform: translate3d(0, 0, 0); }
`;

export const fadeSlide = keyframes`
  from { opacity: 0; transform: translate3d(12px, 0, 0); }
  to   { opacity: 1; transform: translate3d(0, 0, 0); }
`;

export const drift = keyframes`
  0%   { transform: translate3d(0, 0, 0) scale(1); }
  50%  { transform: translate3d(6%, -4%, 0) scale(1.08); }
  100% { transform: translate3d(0, 0, 0) scale(1); }
`;

export const glow = keyframes`
  0%, 100% { opacity: 0.35; }
  50%      { opacity: 0.95; }
`;

export const float = keyframes`
  0%, 100% { transform: translate3d(0, 0, 0); }
  50%      { transform: translate3d(0, -10px, 0); }
`;

export const popIn = keyframes`
  0%   { opacity: 0; transform: scale(0.6); }
  70%  { opacity: 1; transform: scale(1.08); }
  100% { opacity: 1; transform: scale(1); }
`;

/** Hareketi azaltmak isteyen kullanicida animasyonu kapatir. */
export const reducedMotion: SystemStyleObject<Theme> = {
  '@media (prefers-reduced-motion: reduce)': { animation: 'none', transition: 'none' },
};

/** Ana eylem dugmesi: hafif yukselme ve yumusak gecis. */
export const primaryButtonSx: SystemStyleObject<Theme> = {
  py: 1.25,
  fontWeight: 600,
  borderRadius: 2,
  transition: 'transform 160ms ease, background-color 160ms ease',
  '&:hover': { transform: 'translate3d(0, -1px, 0)' },
  '&:active': { transform: 'translate3d(0, 0, 0)' },
  ...reducedMotion,
};
