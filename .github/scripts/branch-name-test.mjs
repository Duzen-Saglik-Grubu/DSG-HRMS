import { readFileSync } from 'node:fs';

// Dal adi kurali is akisi dosyasindan AYNEN okunur (bkz. title-format-test.mjs).
// Orneklerin cogu depodaki GERCEK dal adlaridir: kurala uyanlar gecmeli, #67'de
// tespit edilen 10 hatali dal dusmelidir. Boylece kural, gecmiste yakalamasi
// gereken hatayi gercekten yakalayip yakalamadigina gore sinanir.
const workflowText = readFileSync('.github/workflows/pr-traceability-check.yml', 'utf8');
const lines = workflowText.split('\n');
const typesLine = lines.find((l) => l.includes('const BRANCH_TYPES ='));
const patternStart = lines.findIndex((l) => l.includes('const BRANCH_PATTERN ='));
const patternLines = lines.slice(patternStart, patternStart + 2).join('\n');

const source = (typesLine + '\n' + patternLines).replace(/^\s+/gm, '') + '\nreturn BRANCH_PATTERN;';
const BRANCH_PATTERN = new Function(source)();

const cases = [
  // Depodaki gercek, kurala uyan dallar
  ['bakim/1-github-akis-altyapisi', true],
  ['dokuman/33-a1-kapanis', true],
  ['duzeltici/66-ci-tanimlayicilar-ingilizce', true],
  ['hata/39-izleme-kimligi-guvenli-baglam', true],
  ['ozellik/3-veri-kalitesi-ve-bildirim-istisnasi', true],
  ['ozellik/41-uat-tls', true],
  // #67: depodaki gercek, kurala UYMAYAN dallar
  ['gorev/46-modul-baslangic-kosulu', false],
  ['gorev/60-logo-olcum-22-09', false],
  ['gorev/64-t3-sistem-gereksinimleri', false],
  ['gereksinim/62-t3-kimlik-onayli-gereksinimler', false],
  // Diger hatali bicimler
  ['main', false],
  ['dokuman/t3-gereksinimler', false],
  ['dokuman/64', false],
  ['Dokuman/64-buyuk-harf', false],
  ['dokuman/64-türkçe-karakter', false],
  ['dokuman/64-sonda-tire-', false],
  ['dokuman/64--cift-tire', false],
  ['feature/64-ingilizce-tur', false],
];

let failures = 0;
for (const [branch, expected] of cases) {
  const actual = BRANCH_PATTERN.test(branch);
  const ok = actual === expected;
  if (!ok) failures++;
  console.log(`  ${ok ? 'OK    ' : 'YANLIS'} ${actual ? 'gecti' : 'dustu'}  ${JSON.stringify(branch)}`);
}
console.log(failures === 0 ? '\nTum ornekler beklendigi gibi.' : `\n${failures} ornek beklenenden farkli!`);
process.exit(failures ? 1 : 0);
