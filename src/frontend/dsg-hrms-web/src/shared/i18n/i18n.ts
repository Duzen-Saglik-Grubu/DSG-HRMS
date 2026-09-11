import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import tr from '@/locales/tr/common.json';

/**
 * Coklu dil altyapisi (ADR-0015 §9, KR-039).
 *
 * Baslangic dili Turkce; ikinci dil eklendiginde KOD DEGISIKLIGI GEREKMEZ,
 * yalnizca yeni bir ceviri dosyasi eklenir. Bu yuzden arayuzde sabit metin
 * yazilmaz - tek bir sabit metin bile, ceviri eklendiginde gozden kacan bir
 * bosluk birakir.
 */
await i18n.use(initReactI18next).init({
  resources: {
    tr: { common: tr },
  },
  lng: 'tr',
  fallbackLng: 'tr',
  defaultNS: 'common',
  interpolation: {
    // React zaten kacis (escaping) yapar; iki kez kacis metni bozardi.
    escapeValue: false,
  },
});

export default i18n;
