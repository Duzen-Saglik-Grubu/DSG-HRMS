import { createTheme } from '@mui/material/styles';
import { trTR } from '@mui/material/locale';

/**
 * Kurumsal tema (ADR-0015 §10).
 *
 * Renkler, tipografi ve bilesen varsayilanlari YALNIZCA burada tanimlanir.
 * Bilesen icinde dogrudan renk kodu yazilmaz; tema belirtecleri kullanilir.
 * Aksi hâlde kurumsal kimlik degistiginde onlarca dosya taranmak zorunda kalir.
 *
 * Koyu tema ilk surumde kapsam disidir (YAGNI), ancak yapi buna izin verir:
 * palet "mode" uzerinden ikinci bir tanimla genisletilebilir.
 */
export const theme = createTheme(
  {
    palette: {
      mode: 'light',
      primary: { main: '#1F3864' },
      secondary: { main: '#2E7D6F' },
      error: { main: '#B3261E' },
      warning: { main: '#9A6700' },
      success: { main: '#1B5E20' },
      background: { default: '#F6F7F9', paper: '#FFFFFF' },
    },

    typography: {
      // Turkce karakterleri iyi destekleyen, sistemde bulunan yazi tipleri.
      fontFamily: ['Inter', 'Segoe UI', 'Roboto', 'Helvetica', 'Arial', 'sans-serif'].join(','),
      // Veri giris ekranlarinda okunabilirlik icin taban boyut biraz buyuk.
      fontSize: 14,
      button: { textTransform: 'none' },
    },

    shape: { borderRadius: 8 },

    components: {
      MuiTextField: {
        // Etiket zorunludur (ADR-0015 §7); yer tutucu tek basina yeterli degildir.
        defaultProps: { size: 'small', fullWidth: true },
      },
      MuiButton: {
        defaultProps: { disableElevation: true },
      },
    },
  },

  // MUI bilesenlerinin kendi metinleri de Turkce olur.
  trTR,
);
