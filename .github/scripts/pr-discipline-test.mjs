import { readFileSync } from 'node:fs';

// PR disiplini kurallari (#216) is akisi dosyasindan AYNEN okunur. Kopyalayip yeniden
// yazmak, sinanan kuralin calisandan sessizce ayrismasina yol acardi.
const workflowText = readFileSync('.github/workflows/pr-traceability-check.yml', 'utf8');
const start = workflowText.indexOf('// BASLA: pr-disiplini');
const end = workflowText.indexOf('// BITIR: pr-disiplini');
if (start < 0 || end < 0) {
  console.error('pr-disiplini blogu is akisi dosyasinda bulunamadi.');
  process.exit(1);
}
const block = workflowText.slice(start, end);
const { missingMandatoryDod, reviewableLines, needsSizeJustification } = new Function(
  `${block}\nreturn { missingMandatoryDod, reviewableLines, needsSizeJustification };`)();

let failures = 0;
const check = (name, actual, expected) => {
  const ok = JSON.stringify(actual) === JSON.stringify(expected);
  if (!ok) failures++;
  console.log(`  ${ok ? 'OK    ' : 'YANLIS'} ${name}${ok ? '' : `  (beklenen ${JSON.stringify(expected)}, gelen ${JSON.stringify(actual)})`}`);
};

console.log('Atlanamaz tamamlanma maddeleri:');
check('ikisi de isaretli',
  missingMandatoryDod('- [x] **Yetki sızıntısı testi** yazıldı\n- [x] **Maskeleme testi** yazıldı'), []);
check('ikisi de gerekceyle ustu cizili',
  missingMandatoryDod('- [ ] ~~Yetki sızıntısı testi~~ — kod değişikliği yok\n- [ ] ~~Maskeleme testi~~ — kod değişikliği yok'), []);
check('ASCII yazim',
  missingMandatoryDod('- [x] Yetki sizintisi testi\n- [X] Maskeleme testi'), []);
check('isaretsiz birakilmis',
  missingMandatoryDod('- [ ] **Yetki sızıntısı testi** yazıldı\n- [x] **Maskeleme testi** yazıldı'), ['Yetki sizintisi testi']);
check('ustu cizili ama gerekcesiz',
  missingMandatoryDod('- [x] Yetki sızıntısı testi\n- [ ] ~~Maskeleme testi~~'), ['Maskeleme testi']);
check('hic yazilmamis',
  missingMandatoryDod('## Tamamlanma Tanımı\n- [x] Testler'), ['Yetki sizintisi testi', 'Maskeleme testi']);
check('bos govde', missingMandatoryDod(null), ['Yetki sizintisi testi', 'Maskeleme testi']);

console.log('PR boyutu:');
const files = [
  { filename: 'src/backend/Dsg.Hrms.Api/Foo.cs', additions: 900, deletions: 100 },
  { filename: 'src/frontend/dsg-hrms-web/src/shared/api/generated/schema.ts', additions: 3000, deletions: 0 },
  { filename: 'docs/api/openapi-v1.json', additions: 800, deletions: 10 },
  { filename: 'src/backend/Dsg.Hrms.Infrastructure/Data/Migrations/20261010203210_X.Designer.cs', additions: 2000, deletions: 0 },
  { filename: 'src/backend/Dsg.Hrms.Infrastructure/Data/Migrations/HrmsDbContextModelSnapshot.cs', additions: 50, deletions: 2 },
  { filename: 'src/frontend/dsg-hrms-web/package-lock.json', additions: 400, deletions: 400 },
  { filename: 'src/backend/Dsg.Hrms.Infrastructure/Data/Migrations/20261010203210_X.cs', additions: 40, deletions: 0 },
];
check('uretilen dosyalar sayilmaz', reviewableLines(files), 1040);
check('sinirin altinda gerekce gerekmez', needsSizeJustification(files, ''), false);
const big = [...files, { filename: 'src/frontend/dsg-hrms-web/src/Page.tsx', additions: 600, deletions: 0 }];
check('sinir asilinca gerekce gerekir', needsSizeJustification(big, 'govde'), true);
check('gerekce yazilmissa gecer', needsSizeJustification(big, '**Boyut gerekçesi:** iki ozellik ayrilamazdi'), false);
check('ASCII gerekce', needsSizeJustification(big, 'Boyut gerekcesi: ...'), false);

console.log(failures === 0 ? '\nTum ornekler beklendigi gibi.' : `\n${failures} ornek beklenenden farkli!`);
process.exit(failures ? 1 : 0);
