import { describe, expect, it } from 'vitest';
import { MAX_PAGE_SIZE, toQueryParams } from '../paging';

/**
 * Sayfalama sozlesmesi (ADR-0010 §4).
 */
describe('toQueryParams', () => {
  it('sayfa ve boyutu sorgu parametrelerine cevirir', () => {
    expect(toQueryParams({ page: 2, pageSize: 25 })).toEqual({ page: '2', pageSize: '25' });
  });

  it('siralama verildiginde yonu de gonderir', () => {
    expect(toQueryParams({ page: 1, pageSize: 10, sort: 'lastName' })).toEqual({
      page: '1',
      pageSize: '10',
      sort: 'lastName',
      order: 'asc',
    });
  });

  it('siralama yoksa yon gondermez', () => {
    expect(toQueryParams({ page: 1, pageSize: 10, order: 'desc' })).toEqual({
      page: '1',
      pageSize: '10',
    });
  });

  it('sayfa boyutunu ust sinira ceker', () => {
    // Sunucu 100'u asan istege 400 doner; kullaniciya gereksiz hata gostermeyiz.
    expect(toQueryParams({ page: 1, pageSize: 500 })['pageSize']).toBe(String(MAX_PAGE_SIZE));
  });

  it('gecersiz sayfa degerlerini duzeltir', () => {
    expect(toQueryParams({ page: 0, pageSize: 0 })).toEqual({ page: '1', pageSize: '1' });
    expect(toQueryParams({ page: -5, pageSize: 10 })['page']).toBe('1');
  });
});
