import { expect, type Page } from '@playwright/test';

/** Telefon olcusu (SYG-PERS-073): 360 CSS piksel genislik, dokunmatik ekran. */
export const phone = { viewport: { width: 360, height: 740 }, hasTouch: true, isMobile: true };

/** En kucuk dokunma hedefi, CSS piksel (SYG-PERS-073). */
const TOUCH_TARGET = 44;

const INTERACTIVE = [
  'button',
  'a[href]',
  '[role=button]',
  '[role=tab]',
  '[role=combobox]',
  '[role=checkbox]',
  '[role=menuitem]',
  'input:not([type=hidden])',
  'textarea',
  // Tarih alani: gunu, ayi ve yili tasiyan cerceve.
  '.MuiPickersInputBase-root',
].join(',');

interface LayoutReport {
  /** Sayfa yatay kayiyor mu. */
  horizontalScroll: boolean;
  /** Ekranin sag kenarini asan gorunur ogeler. */
  overflowing: string[];
  /** 44 x 44'ten kucuk gorunur dokunma hedefleri. */
  small: string[];
}

/**
 * Acik ekranin telefonda kullanilabilir oldugunu dogrular: sayfa yatay kaymaz, hicbir oge sag
 * kenari asmaz ve gorunur her dokunma hedefi en az 44 x 44 pikseldir. Metin alani icin olculen,
 * parmagin dokundugu cerceve kutusudur (ic `input` daha dardir).
 */
export async function expectPhoneLayout(page: Page, screen: string): Promise<void> {
  // Panel ve pencereler kayarak acilir; olcum ekran durulana kadar yinelenir.
  await expect
    .poll(() => measure(page), { message: `${screen}: telefon duzeni` })
    .toEqual({ horizontalScroll: false, overflowing: [], small: [] });
}

async function measure(page: Page): Promise<LayoutReport> {
  return page.evaluate(
    ({ selector, min }): LayoutReport => {
      const root = document.scrollingElement ?? document.documentElement;
      const width = root.clientWidth;
      const describe = (element: Element): string => {
        const name =
          element.getAttribute('aria-label') ??
          (element as HTMLElement).innerText ??
          element.tagName;
        return `${element.tagName.toLowerCase()} "${name.replace(/\s+/g, ' ').trim().slice(0, 40)}"`;
      };
      const visible = (box: DOMRect, element: Element): boolean =>
        box.width > 0 && box.height > 0 && getComputedStyle(element).visibility !== 'hidden';

      const overflowing: string[] = [];
      document.querySelectorAll('body *').forEach((element) => {
        const box = element.getBoundingClientRect();
        if (visible(box, element) && box.right > width + 1) {
          overflowing.push(`${describe(element)} sag kenar ${Math.round(box.right)}`);
        }
      });

      const small: string[] = [];
      document.querySelectorAll(selector).forEach((element) => {
        const target =
          element.matches('input, textarea') && element.closest('.MuiInputBase-root')
            ? element.closest('.MuiInputBase-root')!
            : element;
        const box = target.getBoundingClientRect();
        // Dokunulmayan input'lar: MUI onay kutusunun saydam input'u (kutusu ust ogeyle ayni) ve
        // tarih alaninin ekran okuyucudan da gizli degeri.
        const hiddenInput =
          getComputedStyle(element).opacity === '0' ||
          element.getAttribute('aria-hidden') === 'true';
        if (!hiddenInput && visible(box, target) && (box.width < min || box.height < min)) {
          small.push(`${describe(element)} ${Math.round(box.width)}x${Math.round(box.height)}`);
        }
      });

      return { horizontalScroll: root.scrollWidth > width, overflowing, small };
    },
    { selector: INTERACTIVE, min: TOUCH_TARGET },
  );
}
