import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { recordServerDate, resetServerClock } from '@/shared/api/serverClock';
import { formatCountdown, useCountdown } from '../useCountdown';

describe('useCountdown', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-09-27T09:00:00Z'));
  });

  afterEach(() => {
    resetServerClock();
    vi.useRealTimers();
  });

  it('sunucu saati geride olsa da kalan sureyi dogru gosterir (#103)', () => {
    // UAT: sunucu 10 dakika geride; 5 dakikalik kodun bitis ani sunucu saatine gore verilir.
    recordServerDate('Sun, 27 Sep 2026 08:50:00 GMT');

    const { result } = renderHook(() => useCountdown('2026-09-27T08:55:00Z'));

    expect(result.current).toBe(300);
  });

  it('sunucunun bitis anindan geriye sayar', () => {
    const { result } = renderHook(() => useCountdown('2026-09-27T09:05:00Z'));
    expect(result.current).toBe(300);

    act(() => {
      vi.advanceTimersByTime(61_000);
    });

    expect(result.current).toBe(239);
  });

  it('sure dolunca sifirda durur', () => {
    const { result } = renderHook(() => useCountdown('2026-09-27T09:00:02Z'));

    act(() => {
      vi.advanceTimersByTime(10_000);
    });

    expect(result.current).toBe(0);
  });

  it('bitis ani yoksa sifir doner', () => {
    expect(renderHook(() => useCountdown(undefined)).result.current).toBe(0);
  });
});

describe('formatCountdown', () => {
  it.each([
    [300, '5:00'],
    [65, '1:05'],
    [9, '0:09'],
    [0, '0:00'],
  ])('%i saniye -> %s', (seconds, expected) => {
    expect(formatCountdown(seconds)).toBe(expected);
  });
});
