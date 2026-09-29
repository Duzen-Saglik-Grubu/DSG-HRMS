import { useEffect, useState } from 'react';
import { serverNow } from '@/shared/api/serverClock';

/**
 * Verilen ana kadar kalan saniye (SYG-KMLK-028: kod ekranindaki geri sayim).
 *
 * Sure sunucunun verdigi bitis anindan, SUNUCUNUN saatine gore hesaplanir (#103); istemcide
 * sayac tutulmaz.
 * Boylece sekme arka planda kalip zamanlayici yavaslasa bile gosterilen sure dogru
 * kalir. Saniyede bir yalnizca "simdi" guncellenir.
 */
export function useCountdown(until: string | undefined, clock: () => number = serverNow): number {
  const [now, setNow] = useState(clock);

  useEffect(() => {
    const timer = setInterval(() => setNow(clock()), 1000);
    return () => clearInterval(timer);
  }, [clock]);

  if (!until) {
    return 0;
  }

  return Math.max(0, Math.ceil((Date.parse(until) - now) / 1000));
}

export { formatCountdown } from '@/shared/utils/formatCountdown';
