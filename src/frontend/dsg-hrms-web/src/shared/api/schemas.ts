import type { components } from './generated/schema';

/**
 * OpenAPI'den uretilen tiplerin tek erisim noktasi (ADR-0010 §1).
 *
 * Ozellikler uretilen dosyayi dogrudan ice aktarmaz (ESLint kurali); tipler buradan
 * alinir. Uretilen dosyanin yapisi arac surumuyle degisirse yalnizca bu dosya
 * guncellenir.
 */
export type ApiSchemas = components['schemas'];
