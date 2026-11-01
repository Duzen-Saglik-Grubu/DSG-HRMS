import { format } from 'date-fns';
import type { TFunction } from 'i18next';
import type { PersonFilterOptions } from './api/personsApi';
import {
  DATE_FILTERS,
  FIXED_VALUES,
  MULTI_FILTERS,
  OPTION_SOURCE,
  RANGE_FILTERS,
  TEXT_FILTERS,
  parseDate,
  statusOf,
  withChanges,
} from './listState';

/** Liste ustundeki bir etiket ve kaldirildiginda adresin alacagi hal. */
export interface FilterTag {
  key: string;
  label: string;
  remove: (params: URLSearchParams) => URLSearchParams;
}

function optionLabel(
  name: (typeof MULTI_FILTERS)[number],
  value: string,
  options: PersonFilterOptions | undefined,
  t: TFunction,
): string {
  if (name === 'gender' || name === 'maritalStatus') {
    return (FIXED_VALUES[name] as readonly string[]).includes(value)
      ? t(`personnel.list.values.${name}.${value}`)
      : value;
  }

  // Secenekler henuz gelmediyse ya da deger artik yoksa degerin kendisi gosterilir.
  return options?.[OPTION_SOURCE[name]].find((option) => option.value === value)?.label ?? value;
}

function displayDate(value: string | null): string | null {
  const date = parseDate(value);
  return date ? format(date, 'dd.MM.yyyy') : null;
}

function rangeText(from: string | null, to: string | null, t: TFunction): string {
  if (from && to) {
    return t('personnel.list.range.between', { from, to });
  }

  return from ? t('personnel.list.range.from', { from }) : t('personnel.list.range.to', { to });
}

/**
 * Secili suzgeclerin etiketleri (SYG-PERS-025): coklu secimde her deger ayri etikettir ve
 * yalnizca o deger kaldirilir. Aralik suzgeclerinde iki ucu tek etikettir.
 */
export function filterTags(
  params: URLSearchParams,
  options: PersonFilterOptions | undefined,
  t: TFunction,
): FilterTag[] {
  const tags: FilterTag[] = [];
  const field = (name: string) => t(`personnel.list.fields.${name}`);

  const status = statusOf(params);
  if (status !== 'active') {
    tags.push({
      key: 'status',
      label: `${t('personnel.list.status.label')}: ${t(`personnel.list.status.${status}`)}`,
      remove: (current) => withChanges(current, { status: null }),
    });
  }

  for (const name of MULTI_FILTERS) {
    const values = params.getAll(name);
    for (const value of values) {
      tags.push({
        key: `${name}=${value}`,
        label: `${field(name)}: ${optionLabel(name, value, options, t)}`,
        remove: (current) =>
          withChanges(current, { [name]: current.getAll(name).filter((item) => item !== value) }),
      });
    }
  }

  for (const name of TEXT_FILTERS) {
    const value = params.get(name);
    if (value) {
      tags.push({
        key: name,
        label: `${field(name)}: ${value}`,
        remove: (current) => withChanges(current, { [name]: null }),
      });
    }
  }

  for (const name of DATE_FILTERS) {
    const from = displayDate(params.get(`${name}From`));
    const to = displayDate(params.get(`${name}To`));
    if (from || to) {
      tags.push({
        key: name,
        label: `${field(name)}: ${rangeText(from, to, t)}`,
        remove: (current) => withChanges(current, { [`${name}From`]: null, [`${name}To`]: null }),
      });
    }
  }

  for (const name of RANGE_FILTERS) {
    const min = params.get(`${name}Min`);
    const max = params.get(`${name}Max`);
    if (min || max) {
      const text =
        min && max
          ? t('personnel.list.range.between', { from: min, to: max })
          : min
            ? t('personnel.list.range.min', { min })
            : t('personnel.list.range.max', { max });
      tags.push({
        key: name,
        label: `${field(name)}: ${text} ${t('personnel.list.panel.years')}`,
        remove: (current) => withChanges(current, { [`${name}Min`]: null, [`${name}Max`]: null }),
      });
    }
  }

  return tags;
}
