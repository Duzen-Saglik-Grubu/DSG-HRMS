import { format, parseISO } from 'date-fns';
import type { PersonFilterOptions } from './api/personsApi';

/**
 * Personel listesinin durumu adres cubugunda tutulur (SYG-PERS-023, 025).
 *
 * Parametre adlari API'ninkilerle AYNIDIR; boylece adres cubugu, istek ve suzgec etiketleri tek
 * kaynaktan uretilir. Kullanici karta gidip geri geldiginde ya da adresi paylastiginda liste ayni
 * suzgec, sayfa ve gorunumle acilir. Adreste yalnizca suzgec DEGERLERI degil, korumali alanlarin
 * (adres, TCKN) metni de durabilir; bu yuzden bu iki suzgec yalnizca izni olana sunulur ve sunucu
 * izinsiz istegi reddeder (SYG-PERS-026).
 */

/** Liste gorunumu. */
export type ListView = 'grid' | 'table';

/** Calisma durumu (SYG-PERS-027). */
export type StatusFilter = 'active' | 'terminated' | 'all';

/** Secim listesi olan suzgecler: secilenlerden herhangi birine uyan gelir (SYG-PERS-025). */
export const MULTI_FILTERS = [
  'companyId',
  'city',
  'branchId',
  'departmentId',
  'titleCode',
  'dutyId',
  'statusCode',
  'occupationCode',
  'education',
  'gender',
  'maritalStatus',
] as const;
export type MultiFilter = (typeof MULTI_FILTERS)[number];

/** Metin suzgecleri: icinde gecen arama. */
export const TEXT_FILTERS = [
  'firstName',
  'lastName',
  'previousSurname',
  'registryCode',
  'nationalId',
  'email',
  'phone',
  'address',
] as const;
export type TextFilter = (typeof TEXT_FILTERS)[number];

/** Tarih araligi suzgecleri; adreste `<ad>From` ve `<ad>To` olarak durur. */
export const DATE_FILTERS = ['birthDate', 'hireDate', 'terminationDate'] as const;
export type DateFilter = (typeof DATE_FILTERS)[number];

/** Yil araligi suzgecleri; adreste `<ad>Min` ve `<ad>Max` olarak durur. */
export const RANGE_FILTERS = ['age', 'seniority'] as const;
export type RangeFilter = (typeof RANGE_FILTERS)[number];

/** Secim listesi seceneklerin `filter-options` yanitindaki alani. */
export const OPTION_SOURCE: Record<
  Exclude<MultiFilter, 'gender' | 'maritalStatus'>,
  keyof Pick<
    PersonFilterOptions,
    | 'companies'
    | 'cities'
    | 'branches'
    | 'departments'
    | 'titles'
    | 'duties'
    | 'statuses'
    | 'occupations'
    | 'educations'
  >
> = {
  companyId: 'companies',
  city: 'cities',
  branchId: 'branches',
  departmentId: 'departments',
  titleCode: 'titles',
  dutyId: 'duties',
  statusCode: 'statuses',
  occupationCode: 'occupations',
  education: 'educations',
};

/** Sabit secenekli suzgeclerin degerleri. */
export const FIXED_VALUES = {
  gender: ['female', 'male'],
  maritalStatus: ['married', 'notMarried'],
} as const;

/** Liste durumunu tasiyan, suzgec OLMAYAN parametreler. */
const CONTROL_PARAMS = ['q', 'status', 'sort', 'order', 'page', 'pageSize', 'view'] as const;

/** Bir suzgecin adresteki butun parametre adlari. */
export function filterParamNames(): string[] {
  return [
    ...MULTI_FILTERS,
    ...TEXT_FILTERS,
    ...DATE_FILTERS.flatMap((name) => [`${name}From`, `${name}To`]),
    ...RANGE_FILTERS.flatMap((name) => [`${name}Min`, `${name}Max`]),
  ];
}

/** Gorunum; adreste yoksa kart izgarasi (SYG-PERS-023). */
export function viewOf(params: URLSearchParams): ListView {
  return params.get('view') === 'table' ? 'table' : 'grid';
}

/** Calisma durumu; adreste yoksa Aktif (SYG-PERS-027). */
export function statusOf(params: URLSearchParams): StatusFilter {
  const value = params.get('status');
  return value === 'terminated' || value === 'all' ? value : 'active';
}

/** Sayfa numarasi (1'den baslar). */
export function pageOf(params: URLSearchParams): number {
  const value = Number(params.get('page'));
  return Number.isInteger(value) && value > 0 ? value : 1;
}

/**
 * API istegi: adresteki parametrelerin gorunum disindakileri. Bos degerler gonderilmez; sayfa
 * boyutu verilmezse sunucu parametredeki varsayilani kullanir (SYG-PERS-029).
 */
export function toApiParams(params: URLSearchParams): URLSearchParams {
  const result = new URLSearchParams();
  const known = new Set<string>([...CONTROL_PARAMS, ...filterParamNames()]);
  params.forEach((value, key) => {
    if (key !== 'view' && known.has(key) && value.trim() !== '') {
      result.append(key, value.trim());
    }
  });
  return result;
}

/** Herhangi bir suzgec secili mi. */
export function hasFilters(params: URLSearchParams): boolean {
  return filterParamNames().some((name) => params.has(name)) || statusOf(params) !== 'active';
}

/**
 * Durumu degistirir; sayfa numarasi silinir. Suzgec degisince eski sayfa numarasi baska
 * kayitlari gosterirdi (ADR-0015 §6).
 */
export function withChanges(
  params: URLSearchParams,
  changes: Record<string, string | string[] | null>,
): URLSearchParams {
  const next = new URLSearchParams(params);
  for (const [key, value] of Object.entries(changes)) {
    next.delete(key);
    const values = value === null ? [] : Array.isArray(value) ? value : [value];
    for (const item of values) {
      if (item.trim() !== '') {
        next.append(key, item);
      }
    }
  }

  if (!('page' in changes)) {
    next.delete('page');
  }

  return next;
}

/**
 * "Temizle": butun suzgecler kalkar, calisma durumu varsayilana (Aktif) doner (SYG-PERS-025).
 * Hizli arama, gorunum, siralama ve sayfa boyutu suzgec degildir; korunur.
 */
export function cleared(params: URLSearchParams): URLSearchParams {
  const changes: Record<string, null> = { status: null };
  for (const name of filterParamNames()) {
    changes[name] = null;
  }

  return withChanges(params, changes);
}

/** Sayfa boyutu secenekleri: varsayilan, iki ve dort kati; en buyugu asmaz (SYG-PERS-029). */
export function pageSizeOptions(defaultSize: number, maxSize: number): number[] {
  const base = Math.max(1, Math.min(defaultSize, maxSize));
  return [...new Set([base, base * 2, base * 4].filter((size) => size <= maxSize))];
}

/** Adresteki `yyyy-MM-dd` tarihini Date'e cevirir; gecersizse null. */
export function parseDate(value: string | null): Date | null {
  if (!value) {
    return null;
  }

  const date = parseISO(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

/** Tarihi adres ve API bicimine cevirir. Saat dilimi kaymasin diye yerel tarih kullanilir. */
export function formatDate(value: Date | null): string | null {
  return value && !Number.isNaN(value.getTime()) ? format(value, 'yyyy-MM-dd') : null;
}
