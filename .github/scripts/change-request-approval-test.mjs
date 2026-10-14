import { readFileSync } from 'node:fs';

// Degisiklik talebi onay denetimi (#210) is akisi dosyasindan AYNEN okunur. Kopyalayip
// yeniden yazmak, sinanan fonksiyonun calisandan sessizce ayrismasina yol acardi.
const workflowText = readFileSync('.github/workflows/pr-traceability-check.yml', 'utf8');
const start = workflowText.indexOf('// BASLA: onay-denetimi');
const end = workflowText.indexOf('// BITIR: onay-denetimi');
if (start < 0 || end < 0) {
  console.error('onay-denetimi blogu is akisi dosyasinda bulunamadi.');
  process.exit(1);
}
const block = workflowText.slice(start, end);
const hasStakeholderApproval = new Function(`${block}\nreturn hasStakeholderApproval;`)();

const template = (value) => `### Ne değişsin?\n\nBir şey\n\n### Paydaş onayı\n\n${value}\n\n### Karar (değerlendirme sonrası doldurulur)\n\n_No response_`;

const cases = [
  ['Sablon alani dolu (GG.AA.YYYY)', [template('10.10.2026 — İK birim yöneticisi — e-posta')], true],
  ['Sablon alani dolu (YYYY-AA-GG)', [template('2026-10-10, İK, toplantı')], true],
  ['Sablon alani bos', [template('_No response_')], false],
  ['Sablon alani tarihsiz', [template('İK onayladı')], false],
  ['Onay sonradan yorumla', [template('_No response_'), 'Paydaş onayı: 10.10.2026 — İK — e-posta (BK-27)'], true],
  ['Yorum kalin yazimla', ['**Paydaş onayı:** 13.10.2026 — İK birim yöneticisi — kabul oturumu'], true],
  ['ASCII yazim', ['Paydas onayi: 14.10.2026 - IK - e-posta'], true],
  ['Onaysiz, baska yerde tarih var', ['Talep 10.10.2026 tarihinde geldi.', 'İK değerlendirecek.'], false],
  ['Onay kelimesi var, tarih yok', ['Paydaş onayı bekleniyor'], false],
  ['Bos govde ve yorum yok', [null], false],
];

let failures = 0;
for (const [name, texts, expected] of cases) {
  const actual = hasStakeholderApproval(texts);
  const ok = actual === expected;
  if (!ok) failures++;
  console.log(`  ${ok ? 'OK    ' : 'YANLIS'} ${actual ? 'onay var ' : 'onay yok '}  ${name}`);
}
console.log(failures === 0 ? '\nTum ornekler beklendigi gibi.' : `\n${failures} ornek beklenenden farkli!`);
process.exit(failures ? 1 : 0);
