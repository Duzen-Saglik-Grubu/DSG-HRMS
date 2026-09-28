import { useEffect, useState } from 'react';
import type { SessionManager } from './sessionManager';

/** Kullanici etkilesimi sayilan olaylar (SYG-KMLK-038). Fare hareketi sayilmaz. */
const INTERACTION_EVENTS = ['pointerdown', 'keydown', 'wheel', 'touchstart'] as const;

/** Olagan denetim araligi. */
const TICK_MS = 15_000;

/** Uyari penceresindeki denetim araligi (geri sayim saniyede bir guncellenir). */
const WARNING_TICK_MS = 1_000;

const isVisible = () => document.visibilityState === 'visible';

/** Sayfada oynayan bir video veya ses var mi. */
function isMediaPlaying(): boolean {
  return Array.from(document.querySelectorAll<HTMLMediaElement>('video, audio')).some(
    (media) => !media.paused && !media.ended,
  );
}

/**
 * Oturum acikken kullanici etkilesimini izler, etkinlik sinyalini gonderir ve hareketsizlik
 * suresini denetler (SYG-KMLK-038, 039).
 *
 * - Etkilesim yalnizca GORUNEN sekmede sayilir.
 * - Uzun bir egitim videosu gibi mesru etkinlik: medya oynarken ve sekme gorunurken
 *   etkilesim olmasa da sinyal gider. Arka planda oynayan medya sayilmaz.
 * - Arka plan istekleri (veri yenileme vb.) hicbir zaman etkinlik sayilmaz; sinyali yalnizca
 *   bu kanca gonderir.
 *
 * @returns Hareketsizlik uyarisi gosterilecekse kalan sure (ms); degilse `null`.
 */
export function useSessionActivity(manager: SessionManager): number | null {
  const [idleRemainingMs, setIdleRemainingMs] = useState<number | null>(null);

  useEffect(() => {
    let timer: ReturnType<typeof setTimeout> | undefined;

    const tick = () => {
      clearTimeout(timer);

      if (isVisible() && isMediaPlaying()) {
        manager.recordInteraction();
      }

      void manager.flushActivity();

      const check = manager.checkDeadlines();
      const warning =
        check && check.idleRemainingMs <= check.warningWindowMs ? check.idleRemainingMs : null;
      setIdleRemainingMs(warning);

      if (check) {
        timer = setTimeout(tick, warning === null ? TICK_MS : WARNING_TICK_MS);
      }
    };

    const onInteraction = () => {
      if (!isVisible()) {
        return;
      }

      manager.recordInteraction();
      tick();
    };

    // Arka plandaki sekmenin zamanlayicisi tarayicida yavaslar; sekme one gelince hemen
    // denetlenir.
    const onVisibility = () => {
      if (isVisible()) {
        tick();
      }
    };

    INTERACTION_EVENTS.forEach((name) =>
      window.addEventListener(name, onInteraction, { capture: true, passive: true }),
    );
    document.addEventListener('visibilitychange', onVisibility);
    tick();

    return () => {
      clearTimeout(timer);
      INTERACTION_EVENTS.forEach((name) =>
        window.removeEventListener(name, onInteraction, { capture: true }),
      );
      document.removeEventListener('visibilitychange', onVisibility);
    };
  }, [manager]);

  return idleRemainingMs;
}
