/** Varsayilan kurumsal logo (SYG-KMLK-069). Kaynagi depodaki assets/duzen_logo.png'dir. */
export const DEFAULT_LOGO_URL = '/brand/duzen_logo.png';

/**
 * Parametre ekranindan yuklenen logo (PRM-GRN-01). Surum, logo degisince tarayicinin
 * yenisini almasini saglar.
 */
export const uploadedLogoUrl = (version: string) => `/api/v1/system/logo?v=${version}`;
