// Saglik taramasinin kayit secim kurali (#291). Ornekler GERCEK olaydan: #286 acikken
// tarama #290'i acmamaliydi.
import { findOpenIssue, redTitle, unverifiedTitle } from './health-sweep-issues.mjs';

const red286 = { number: 286, title: redTitle('CI') };
const unverified290 = { number: 290, title: unverifiedTitle('CI') };
const redApi = { number: 300, title: redTitle('API sozlesme denetimi') };
const other = { number: 250, title: '[HATA] follow-redirects açığı' };
const ci = { title: unverifiedTitle('CI'), workflowName: 'CI' };
const recorder = { title: '[HATA] Denetim çalışmadı: main kirmizi denetimi' };

const cases = [
  ['#286 acikken dogrulanmadi sorunu #286ya yazilir (#290 acilmaz)', findOpenIssue(ci, [other, red286]), 286],
  ['kirmizi kayit, kendi basligindan once secilir', findOpenIssue(ci, [unverified290, red286]), 286],
  ['kirmizi kayit yoksa kendi basligi', findOpenIssue(ci, [other, unverified290]), 290],
  ['baska is akisinin kirmizi kaydi sayilmaz', findOpenIssue(ci, [redApi]), null],
  ['hic acik kayit yoksa yeni kayit', findOpenIssue(ci, []), null],
  ['is akisi adi olmayan sorun yalnizca kendi basligina bakar', findOpenIssue(recorder, [red286]), null],
];

let failed = 0;
for (const [name, actual, expected] of cases) {
  const got = actual?.number ?? null;
  if (got === expected) {
    console.log(`  ✓ ${name}`);
  } else {
    console.error(`  ✗ ${name}: #${got}, beklenen #${expected}`);
    failed++;
  }
}

if (failed) {
  console.error(`✗ Saglik taramasi kayit secimi: ${failed} hata`);
  process.exit(1);
}
console.log('✓ Saglik taramasi kayit secimi: tum ornekler gecti');
