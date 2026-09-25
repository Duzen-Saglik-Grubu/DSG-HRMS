import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ErrorState } from '../ErrorState';
import { ApiError } from '@/shared/api/problemDetails';

describe('ErrorState', () => {
  it('kullaniciya sunucunun mesajini gosterir', () => {
    render(
      <ErrorState
        error={new ApiError({ message: 'Yillik izin bakiyeniz yetersiz.', isNetworkError: false })}
      />,
    );

    expect(screen.getByText('Yillik izin bakiyeniz yetersiz.')).toBeInTheDocument();
  });

  it('takip numarasini gorunur kilar', () => {
    // Destek surecinin hizlanmasi bu numaranin EKRANDA olmasina baglidir
    // (ADR-0009 §2).
    render(
      <ErrorState
        error={
          new ApiError({
            message: 'Beklenmeyen hata',
            traceId: 'DESTEK-2026-0042',
            isNetworkError: false,
          })
        }
      />,
    );

    expect(screen.getByText(/DESTEK-2026-0042/)).toBeInTheDocument();
  });

  it('kullaniciya yapabilecegi bir eylem sunar', async () => {
    // "Bir hata olustu" deyip birakmak kullaniciyi caresiz birakir (ADR-0015 §6).
    const retryButton = vi.fn();

    render(
      <ErrorState
        error={new ApiError({ message: 'Sunucuya ulasilamadi.', isNetworkError: true })}
        onRetry={retryButton}
      />,
    );

    await userEvent.click(screen.getByRole('button', { name: 'Tekrar dene' }));

    expect(retryButton).toHaveBeenCalledOnce();
  });

  it('yeniden deneme yoksa dugme gostermez', () => {
    render(
      <ErrorState
        error={
          new ApiError({ message: 'Bu islem icin yetkiniz bulunmuyor.', isNetworkError: false })
        }
      />,
    );

    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
