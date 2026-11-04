import { describe, expect, it } from 'vitest';
import { routes } from '@/shared/routes';
import { findNavItem } from '../navigation';

/** Menude secili oge ve konum yolu (#327). */
describe('findNavItem', () => {
  it('en ozel eslesmeyi secer: talepler, personel listesinin altinda olsa da kendi ogesidir', () => {
    const found = findNavItem(routes.corrections);

    expect(found && 'item' in found ? found.item.to : null).toBe(routes.corrections);
  });

  it('kisi karti personel listesinin altinda sayilir', () => {
    const found = findNavItem('/personnel/p-1');

    expect(found && 'item' in found ? found.item.to : null).toBe(routes.personnel);
  });

  it('ana sayfa yalnizca kendi adresinde secilir', () => {
    const found = findNavItem('/');

    expect(found && 'item' in found ? found.item.to : null).toBe(routes.home);
  });
});
