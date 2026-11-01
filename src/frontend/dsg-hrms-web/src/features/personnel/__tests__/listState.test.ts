import i18n from '@/shared/i18n/i18n';
import { describe, expect, it } from 'vitest';
import type { PersonFilterOptions } from '../api/personsApi';
import { filterTags } from '../filterTags';
import {
  cleared,
  hasFilters,
  pageSizeOptions,
  statusOf,
  toApiParams,
  viewOf,
  withChanges,
} from '../listState';

/** Liste durumunun adres cubugu ile API arasindaki donusumleri (SYG-PERS-023, 025, 029). */
const OPTIONS: PersonFilterOptions = {
  companies: [],
  cities: [],
  branches: [
    { value: 'b-1', label: 'Merkez' },
    { value: 'b-2', label: 'Bornova' },
  ],
  departments: [],
  titles: [],
  duties: [],
  statuses: [],
  occupations: [],
  educations: [],
  canFilterByNationalId: false,
  canFilterBySensitive: false,
  defaultPageSize: 24,
  maxPageSize: 100,
};

describe('listState', () => {
  it('varsayilan gorunum kart izgarasi, varsayilan durum Aktif', () => {
    const params = new URLSearchParams();

    expect(viewOf(params)).toBe('grid');
    expect(statusOf(params)).toBe('active');
    expect(hasFilters(params)).toBe(false);
  });

  it('API istegine gorunumu ve bos degerleri koymaz, coklu secimi tekrarlar', () => {
    const params = new URLSearchParams(
      'view=table&branchId=b-1&branchId=b-2&firstName=&page=2&bilinmeyen=x',
    );

    expect(toApiParams(params).toString()).toBe('branchId=b-1&branchId=b-2&page=2');
  });

  it('suzgec degisince ilk sayfaya doner, gorunumu korur', () => {
    const params = new URLSearchParams('view=table&page=3&branchId=b-1');

    const next = withChanges(params, { branchId: ['b-1', 'b-2'] });

    expect(next.getAll('branchId')).toEqual(['b-1', 'b-2']);
    expect(next.has('page')).toBe(false);
    expect(viewOf(next)).toBe('table');
  });

  it('Temizle butun suzgecleri kaldirir ve durumu Aktife dondurur; arama ve gorunum kalir', () => {
    const params = new URLSearchParams(
      'q=ali&view=table&status=all&branchId=b-1&ageMin=30&hireDateFrom=2020-01-01&address=x',
    );

    const next = cleared(params);

    expect(next.toString()).toBe('q=ali&view=table');
    expect(hasFilters(next)).toBe(false);
  });

  it('sayfa boyutu secenekleri en buyuk sayfa boyutunu asmaz', () => {
    expect(pageSizeOptions(24, 100)).toEqual([24, 48, 96]);
    expect(pageSizeOptions(24, 50)).toEqual([24, 48]);
    expect(pageSizeOptions(30, 24)).toEqual([24]);
  });

  it('her secili deger ayri etikettir; kaldirinca yalnizca o deger gider', () => {
    const params = new URLSearchParams(
      'branchId=b-1&branchId=b-2&gender=female&ageMin=30&ageMax=40&hireDateFrom=2020-01-31&status=terminated',
    );

    const tags = filterTags(params, OPTIONS, i18n.t);

    expect(tags.map((tag) => tag.label)).toEqual([
      'Çalışma durumu: Ayrılan',
      'Şube: Merkez',
      'Şube: Bornova',
      'Cinsiyet: Kadın',
      'İşe giriş tarihi: 31.01.2020 ve sonrası',
      'Yaş: 30 – 40 yıl',
    ]);
    const merkez = tags.find((tag) => tag.key === 'branchId=b-1');
    expect(merkez?.remove(params).getAll('branchId')).toEqual(['b-2']);
  });
});
