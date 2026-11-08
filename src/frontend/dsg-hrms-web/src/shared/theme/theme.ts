import { createTheme } from '@mui/material/styles';
import { trTR } from '@mui/material/locale';

/** Sol menunun koyu zemin renkleri (ornek tasarim MainScreen2.jpeg, #322). */
interface NavigationPalette {
  /** Zemin gecisinin ust ve alt renkleri. */
  backgroundTop: string;
  backgroundBottom: string;
  /** Metin; koyu zeminde en az 4.5:1 kontrast (ADR-0015 §7). */
  text: string;
  /** Secili ogenin dolgusu; uzerindeki beyaz metin en az 4.5:1. */
  active: string;
}

/** Kart ve panellerin yumusak golgesi; cerceve yerine derinlik verir. */
interface SurfacePalette {
  shadow: string;
  shadowHover: string;
  /** Baslik seridi, dalga deseni ve fotograf halkasindaki acik mavi vurgunun kaynagi. */
  accent: string;
}

declare module '@mui/material/styles' {
  interface Palette {
    navigation: NavigationPalette;
    surface: SurfacePalette;
  }
  interface PaletteOptions {
    navigation?: NavigationPalette;
    surface?: SurfacePalette;
  }
}

/**
 * Dokunmatik ekran (telefon, tablet). Dokunma hedefleri burada en az 44 x 44 CSS pikseldir
 * (SYG-PERS-073); fareyle kullanilan ekranda boyutlar degismez.
 */
const TOUCH = '@media (pointer: coarse)';
const TOUCH_TARGET = 44;

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
      // Giris ekranindaki kurumsal lacivertle uyumlu, beyazdan ayrisan acik mavi-gri zemin.
      background: { default: '#EEF2F8', paper: '#FFFFFF' },
      navigation: {
        backgroundTop: '#14284F',
        backgroundBottom: '#0E1D3B',
        text: '#FFFFFF',
        active: '#2B62C9',
      },
      surface: {
        shadow: '0 1px 2px rgba(20, 40, 79, 0.05), 0 6px 20px -10px rgba(20, 40, 79, 0.18)',
        shadowHover: '0 2px 4px rgba(20, 40, 79, 0.06), 0 14px 32px -12px rgba(20, 40, 79, 0.28)',
        accent: '#2B62C9',
      },
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
        styleOverrides: { root: { [TOUCH]: { minHeight: TOUCH_TARGET } } },
      },
      MuiIconButton: {
        styleOverrides: { root: { [TOUCH]: { minWidth: TOUCH_TARGET, minHeight: TOUCH_TARGET } } },
      },
      MuiToggleButton: {
        styleOverrides: { root: { [TOUCH]: { minWidth: TOUCH_TARGET, minHeight: TOUCH_TARGET } } },
      },
      MuiChip: {
        styleOverrides: {
          // Kaldirilabilen veya tiklanabilen etiket parmakla secilir; yalnizca bilgi veren etiket
          // (durum, anahtar turu) kucuk kalir.
          root: {
            [TOUCH]: {
              '&.MuiChip-deletable, &.MuiChip-clickable': {
                height: TOUCH_TARGET,
                borderRadius: TOUCH_TARGET / 2,
              },
            },
          },
          deleteIcon: { [TOUCH]: { fontSize: 24 } },
        },
      },
      MuiOutlinedInput: {
        styleOverrides: {
          // Kucuk alan 40 piksel; dokunmatik ekranda ic bosluk buyutulerek 44'e cikar.
          root: {
            [TOUCH]: {
              '&.MuiInputBase-sizeSmall:not(.MuiAutocomplete-inputRoot) .MuiOutlinedInput-input': {
                paddingTop: 10.5,
                paddingBottom: 10.5,
              },
            },
          },
        },
      },
      MuiInputLabel: {
        styleOverrides: {
          sizeSmall: {
            [TOUCH]: {
              '&:not(.MuiInputLabel-shrink)': { transform: 'translate(14px, 11px) scale(1)' },
            },
          },
        },
      },
      MuiAutocomplete: {
        styleOverrides: {
          inputRoot: {
            [TOUCH]: { '&.MuiInputBase-sizeSmall': { paddingTop: 8, paddingBottom: 8 } },
          },
        },
      },
      MuiTablePagination: {
        styleOverrides: {
          // Telefonda "sayfa basina kayit", aralik ve sayfa dugmeleri tek satira sigmaz;
          // satir kirilir, dugmeler ekran disina tasmaz (SYG-PERS-073).
          toolbar: { flexWrap: 'wrap', rowGap: 4 },
          spacer: ({ theme }) => ({ [theme.breakpoints.down('sm')]: { display: 'none' } }),
          input: {
            [TOUCH]: {
              '& .MuiSelect-select.MuiInputBase-input': {
                height: TOUCH_TARGET,
                boxSizing: 'border-box',
                display: 'flex',
                alignItems: 'center',
              },
            },
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          // Cerceveli yuzeyler (kartlar, tablolar, paneller) acik zeminde golgeyle ayrisir.
          outlined: ({ theme }) => ({
            borderColor: theme.palette.divider,
            borderRadius: Number(theme.shape.borderRadius) * 1.5,
            boxShadow: theme.palette.surface.shadow,
          }),
        },
      },
    },
  },

  // MUI bilesenlerinin kendi metinleri de Turkce olur.
  trTR,
);
