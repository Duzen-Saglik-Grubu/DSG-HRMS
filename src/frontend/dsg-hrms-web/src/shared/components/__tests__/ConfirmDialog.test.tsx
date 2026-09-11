import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ConfirmDialog } from '../ConfirmDialog';

describe('ConfirmDialog', () => {
  const varsayilan = {
    open: true,
    title: 'İzin talebini sil',
    description: "Ahmet Yılmaz'ın 12-16 Eylül tarihli izin talebi silinecek.",
    onConfirm: vi.fn(),
    onCancel: vi.fn(),
  };

  it('neyin etkilenecegini acikca yazar', () => {
    // "Emin misiniz?" tek basina yetersizdir; kullanici hangi kaydi sildigini
    // hatirlamayabilir (ADR-0015 §6).
    render(<ConfirmDialog {...varsayilan} />);

    expect(
      screen.getByText("Ahmet Yılmaz'ın 12-16 Eylül tarihli izin talebi silinecek."),
    ).toBeInTheDocument();
  });

  it('yikici islemde metinle de uyarir', () => {
    // Renk TEK BASINA anlam tasimaz (ADR-0015 §7).
    render(<ConfirmDialog {...varsayilan} destructive />);

    expect(screen.getByText('Bu işlem geri alınamaz.')).toBeInTheDocument();
  });

  it('yikici olmayan islemde ek uyari gostermez', () => {
    render(<ConfirmDialog {...varsayilan} />);

    expect(screen.queryByText('Bu işlem geri alınamaz.')).not.toBeInTheDocument();
  });

  it('onay ve vazgecme eylemlerini bildirir', async () => {
    const onayla = vi.fn();
    const vazgec = vi.fn();

    render(<ConfirmDialog {...varsayilan} onConfirm={onayla} onCancel={vazgec} />);

    await userEvent.click(screen.getByRole('button', { name: 'Onayla' }));
    expect(onayla).toHaveBeenCalledOnce();

    await userEvent.click(screen.getByRole('button', { name: 'Vazgeç' }));
    expect(vazgec).toHaveBeenCalledOnce();
  });

  it('islem surerken cift gonderimi engeller', async () => {
    // Kullanici yavas bir istekte dugmeye iki kez basarsa, kayit iki kez
    // silinmeye calisilirdi (ADR-0015 §4).
    let tamamla: () => void = () => undefined;
    const onayla = vi.fn(() => new Promise<void>((resolve) => (tamamla = resolve)));

    render(<ConfirmDialog {...varsayilan} onConfirm={onayla} />);

    const dugme = screen.getByRole('button', { name: 'Onayla' });
    await userEvent.click(dugme);

    expect(dugme).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Vazgeç' })).toBeDisabled();

    tamamla();
  });

  it('erisilebilir baslik ve aciklama baglar', () => {
    render(<ConfirmDialog {...varsayilan} />);

    const pencere = screen.getByRole('dialog');
    expect(pencere).toHaveAccessibleName('İzin talebini sil');
    expect(pencere).toHaveAccessibleDescription(
      "Ahmet Yılmaz'ın 12-16 Eylül tarihli izin talebi silinecek.",
    );
  });
});
