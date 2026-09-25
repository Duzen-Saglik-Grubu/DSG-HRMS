import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/render';
import { DateRangeField } from '../DateRangeField';

/**
 * MUI X tarih alani, tek bir input yerine bolumlerden (gun / ay / yil) olusan
 * bir "group" render eder; testler bu yapiya gore yazilir.
 */
describe('DateRangeField', () => {
  const emptyValue = { start: null, end: null };

  it('Turkce etiketlerle iki tarih alani sunar', () => {
    renderWithProviders(<DateRangeField value={emptyValue} onChange={vi.fn()} />);

    // Etiket ZORUNLUDUR; yalnizca yer tutucu kullanilmaz (ADR-0015 §7).
    expect(screen.getByRole('group', { name: 'Başlangıç tarihi' })).toBeInTheDocument();
    expect(screen.getByRole('group', { name: 'Bitiş tarihi' })).toBeInTheDocument();
  });

  it('tarihleri GG.AA.YYYY biciminde gosterir', () => {
    // Bicim tek yerden (LocalizationProvider) gelir; her alanda ayri
    // ayarlansaydi bir yerde unutulur ve ayni ekranda iki bicim gorunurdu.
    renderWithProviders(
      <DateRangeField
        value={{ start: new Date(2026, 8, 7), end: new Date(2026, 8, 13) }}
        onChange={vi.fn()}
      />,
    );

    expect(screen.getByRole('group', { name: 'Başlangıç tarihi' })).toHaveTextContent('07.09.2026');
    expect(screen.getByRole('group', { name: 'Bitiş tarihi' })).toHaveTextContent('13.09.2026');
  });

  it('ozel etiketler verilebilir', () => {
    renderWithProviders(
      <DateRangeField
        value={emptyValue}
        onChange={vi.fn()}
        startLabel="İşe giriş tarihi"
        endLabel="Çıkış tarihi"
      />,
    );

    expect(screen.getByRole('group', { name: 'İşe giriş tarihi' })).toBeInTheDocument();
    expect(screen.getByRole('group', { name: 'Çıkış tarihi' })).toBeInTheDocument();
  });

  it('sunucudan gelen alan hatasini gosterir', () => {
    // Alan bazli hatalar dogrudan ilgili alana baglanir (ADR-0010 §8).
    renderWithProviders(
      <DateRangeField
        value={emptyValue}
        onChange={vi.fn()}
        error="Başlangıç tarihi geçmiş bir tarih olamaz."
      />,
    );

    expect(screen.getByText('Başlangıç tarihi geçmiş bir tarih olamaz.')).toBeInTheDocument();
  });

  it('devre disi birakilabilir', () => {
    renderWithProviders(<DateRangeField value={emptyValue} onChange={vi.fn()} disabled />);

    // Devre disi alanda takvim dugmesi de kullanilamaz olmalidir; aksi hâlde
    // kullanici acilan takvimden secim yapip hicbir sey olmadigini gorurdu.
    for (const button of screen.getAllByRole('button')) {
      expect(button).toBeDisabled();
    }
  });
});
