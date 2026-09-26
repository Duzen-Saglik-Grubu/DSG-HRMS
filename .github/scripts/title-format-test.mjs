import { readFileSync } from 'node:fs';

// Regex, is akisi dosyasindan AYNEN okunur. Kopyalayip yeniden yazmak, sinanan
// ifadenin calisandan sessizce ayrismasina yol acardi.
const workflowText = readFileSync('.github/workflows/pr-traceability-check.yml', 'utf8');
const lines = workflowText.split('\n');
const typesLine = lines.find((l) => l.includes('const TYPES ='));
const patternStart = lines.findIndex((l) => l.includes('const TITLE_PATTERN ='));
const patternLines = lines.slice(patternStart, patternStart + 2).join('\n');

const source = (typesLine + '\n' + patternLines).replace(/^\s+/gm, '') + '\nreturn TITLE_PATTERN;';
const TITLE_PATTERN = new Function(source)();

const cases = [
  ['feat(uat): TLS (HTTPS) devreye alındı', true],
  ['fix(frontend): izleme kimliği güvenli olmayan bağlamda da üretiliyor', true],
  ['ci(github): PR izlenebilirlik denetimi', true],
  ['docs(33061): A1 aşama kapanış değerlendirmesi', true],
  ['feat(api)!: kirici degisiklik', true],
  ['feat: kapsamsiz da gecerli', true],
  ['[GÖREV] UAT ortamında TLS kurulumu', false],
  ['[HATA] bir sey bozuk', false],
  ['TLS kurulumu', false],
  ['feat(uat) TLS devreye alindi', false],
  ['Feat(uat): buyuk harf', false],
  ['feat(uat):', false],
  ['yenilik(uat): gecersiz tur', false],
];

let failures = 0;
for (const [title, expected] of cases) {
  const actual = TITLE_PATTERN.test(title);
  const ok = actual === expected;
  if (!ok) failures++;
  console.log(`  ${ok ? 'OK    ' : 'YANLIS'} ${actual ? 'gecti' : 'dustu'}  ${JSON.stringify(title)}`);
}
console.log(failures === 0 ? '\nTum ornekler beklendigi gibi.' : `\n${failures} ornek beklenenden farkli!`);
process.exit(failures ? 1 : 0);
