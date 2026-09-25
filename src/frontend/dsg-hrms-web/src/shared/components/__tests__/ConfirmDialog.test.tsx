import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ConfirmDialog } from '../ConfirmDialog';

describe('ConfirmDialog', () => {
  const defaultProps = {
    open: true,
    title: 'İzin talebini sil',
    description: "Ahmet Yılmaz'ın 12-16 Eylül tarihli izin talebi silinecek.",
    onConfirm: vi.fn(),
    onCancel: vi.fn(),
  };

  it('neyin etkilenecegini acikca yazar', () => {
    // "Emin misiniz?" tek basina yetersizdir; kullanici hangi kaydi sildigini
    // hatirlamayabilir (ADR-0015 §6).
    render(<ConfirmDialog {...defaultProps} />);

    expect(
      screen.getByText("Ahmet Yılmaz'ın 12-16 Eylül tarihli izin talebi silinecek."),
    ).toBeInTheDocument();
  });

  it('yikici islemde metinle de uyarir', () => {
    // Renk TEK BASINA anlam tasimaz (ADR-0015 §7).
    render(<ConfirmDialog {...defaultProps} destructive />);

    expect(screen.getByText('Bu işlem geri alınamaz.')).toBeInTheDocument();
  });

  it('yikici olmayan islemde ek uyari gostermez', () => {
    render(<ConfirmDialog {...defaultProps} />);

    expect(screen.queryByText('Bu işlem geri alınamaz.')).not.toBeInTheDocument();
  });

  it('onay ve vazgecme eylemlerini bildirir', async () => {
    const confirmButton = vi.fn();
    const cancelButton = vi.fn();

    render(<ConfirmDialog {...defaultProps} onConfirm={confirmButton} onCancel={cancelButton} />);

    await userEvent.click(screen.getByRole('button', { name: 'Onayla' }));
    expect(confirmButton).toHaveBeenCalledOnce();

    await userEvent.click(screen.getByRole('button', { name: 'Vazgeç' }));
    expect(cancelButton).toHaveBeenCalledOnce();
  });

  it('islem surerken cift gonderimi engeller', async () => {
    // Kullanici yavas bir istekte dugmeye iki kez basarsa, kayit iki kez
    // silinmeye calisilirdi (ADR-0015 §4).
    let complete: () => void = () => undefined;
    const confirmButton = vi.fn(() => new Promise<void>((resolve) => (complete = resolve)));

    render(<ConfirmDialog {...defaultProps} onConfirm={confirmButton} />);

    const button = screen.getByRole('button', { name: 'Onayla' });
    await userEvent.click(button);

    expect(button).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Vazgeç' })).toBeDisabled();

    complete();
  });

  it('erisilebilir baslik ve aciklama baglar', () => {
    render(<ConfirmDialog {...defaultProps} />);

    const dialog = screen.getByRole('dialog');
    expect(dialog).toHaveAccessibleName('İzin talebini sil');
    expect(dialog).toHaveAccessibleDescription(
      "Ahmet Yılmaz'ın 12-16 Eylül tarihli izin talebi silinecek.",
    );
  });
});
