import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';

// PR disiplini kurallari (#216, #390) tek modulde: is akislari bu modulu require ile yukler,
// test de AYNI modulu sinar. Kopyalayip yeniden yazmak, sinanan kuralin calisandan sessizce
// ayrismasina yol acardi.
const require = createRequire(import.meta.url);
const {
  missingMandatoryDod, reviewableLines, needsSizeJustification, hasSizeJustification,
  templateProblems, hasReviewNote, needsReviewNote,
} = require('./pr-rules.cjs');
const workflowText = readFileSync('.github/workflows/pr-traceability-check.yml', 'utf8');
const template = readFileSync('.github/PULL_REQUEST_TEMPLATE.md', 'utf8').replace(/\r\n/g, '\n');

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
check('gerekce yazilmissa gecer', needsSizeJustification(big, '**Boyut gerekçesi:** iki ozellik ayrilamazdi, birbirine bagli'), false);
check('ASCII gerekce', needsSizeJustification(big, 'Boyut gerekcesi: iki ozellik birbirine bagli, ayrilamazdi'), false);
// #390: eski kural sablonun kendi DoD satiriyla her zaman geciyordu. Sablonun bos hali ve
// yalnizca DoD satiri isaretlenmis govde gerekce SAYILMAZ.
check('sablonun bos hali gerekce degildir', needsSizeJustification(big, template), true);
check('isaretli DoD satiri gerekce degildir',
  needsSizeJustification(big, "- [x] Üretilen dosyalar hariç 1.500 satırı aşıyorsa **Boyut gerekçesi:** yazıldı (CI denetler): Notlar'da"), true);
check('aciklamasiz gerekce yetmez', needsSizeJustification(big, '**Boyut gerekçesi:** yok'), true);
check('gerekce paragrafi Notlar icinde',
  hasSizeJustification('## Notlar\n\n**Boyut gerekçesi:** Değişiklik 1.569 satır, hepsi belge; rapor ve ekleri tek değerlendirme.'), true);

console.log('Sablon yapisi (#390):');
// Sablondan doldurulmus bir govde uretilir: yorumlar atilir, bolumler doldurulur, maddeler isaretlenir.
function filled(t) {
  return t.replace(/<!--[\s\S]*?-->/g, '')
    .replace('Closes #', 'Closes #1')
    .replace('## Ne değişti?\n', '## Ne değişti?\n\nBir değişiklik yapıldı ve anlatıldı.\n')
    .replace('## Neden?\n', '## Neden?\n\nÇünkü denetim bulgusu böyle istedi.\n')
    .replace('## Nasıl doğrulandı?\n', '## Nasıl doğrulandı?\n\nTestler çalıştırıldı ve geçti.\n')
    .replace(/^- \[ \] /gm, '- [x] ')
    .replace(/^\| (İlgili süreç\(ler\)|Gereksinim kimliği|Modül) \|\s*\|$/gm, '| $1 | değer |');
}
const ok = filled(template);
check('doldurulmus sablon gecer', templateProblems(ok, template), []);
const raw = templateProblems(template, template);
check('bos sablon: uc zorunlu bolum bos', raw.filter((p) => p.includes('bolumu bos')).length, 3);
check('bos sablon: izlenebilirlik tablosu bos', raw.filter((p) => p.includes('izlenebilirlik tablosunda')).length, 3);
const dodLines = ok.split('\n').filter((l) => /^- \[x\] /.test(l));
check('sablonda 26 DoD maddesi var', dodLines.length, 26);
const trimmed = ok.split('\n').filter((l) => !dodLines.slice(8, 18).includes(l)).join('\n');
check('10 madde silinmis govde duser',
  templateProblems(trimmed, template).some((p) => p.startsWith('Tamamlanma tanimindan 10 madde silinmis')), true);
check('Neden? basligi silinmis govde duser',
  templateProblems(ok.replace(/## Neden\?\n\nÇünkü[^\n]*\n/, ''), template), ['Sablon basliklari eksik: "## Neden?"']);
check('gerekceyle ustu cizili madde gecer',
  templateProblems(ok.replace('- [x] Listeleme uç noktaları sayfalanmış', '- [ ] ~~Listeleme uç noktaları sayfalanmış~~ — yeni uç yok'), template), []);
check('gerekcesiz ustu cizili madde duser',
  templateProblems(ok.replace('- [x] Listeleme uç noktaları sayfalanmış', '- [ ] ~~Listeleme uç noktaları sayfalanmış~~'), template).length, 1);
check('cok satirli madde ustu cizili ve gerekceli gecer',
  templateProblems(ok
    .replace('- [x] **İzlenebilirlik matrisi**', '- [ ] ~~**İzlenebilirlik matrisi**')
    .replace('(`docs/33061/izlenebilirlik-matrisi.md` §3)', '(`docs/33061/izlenebilirlik-matrisi.md` §3)~~ — kod yok'), template), []);

console.log('Buyuk PR inceleme notu (KR-140):');
check('not var', hasReviewNote('İnceledim ve onaylıyorum.\nİncelenen: arka uç satır satır, belgeler örneklemeyle'), true);
check('ASCII ve kalin yazim', hasReviewNote('**Incelenen:** testler ve is akisi satir satir'), true);
check('yalniz onay', hasReviewNote('PR #396 inceledim ve onaylıyorum.'), false);
check('bos not', hasReviewNote('İncelenen: hepsi'), false);
check('sinir asilinca not gerekir', needsReviewNote(big), true);
check('sinirin altinda not gerekmez', needsReviewNote(files), false);

// Gereksinim belgesinde degisiklik gecmisi satiri (#235): is akisindan AYNEN okunur.
const hStart = workflowText.indexOf('// BASLA: gecmis-satiri');
const hEnd = workflowText.indexOf('// BITIR: gecmis-satiri');
if (hStart < 0 || hEnd < 0) {
  console.error('gecmis-satiri blogu is akisi dosyasinda bulunamadi.');
  process.exit(1);
}
const missingHistoryRows = new Function(`${workflowText.slice(hStart, hEnd)}\nreturn missingHistoryRows;`)();
const SYG = 'docs/33061/TEC.3-sistem-yazilim-gereksinimleri/gereksinimler/SYG-KMLK.md';
const PG = 'docs/33061/TEC.2-paydas-ihtiyac-ve-gereksinimleri/paydas-gereksinimleri/PG-KMLK.md';
const MATRIS = 'docs/33061/izlenebilirlik-matrisi.md';
const row = '+| 2026-10-15 | 1.9 | Degisiklik (#1) | Bilgi Islem |';

console.log('Gereksinim belgesinde gecmis satiri:');
check('SYG degisti, gecmis satiri eklendi',
  missingHistoryRows([{ filename: SYG, status: 'modified', patch: `@@ -1 +1 @@\n-eski\n+yeni\n${row}` }]), []);
check('SYG degisti, gecmis satiri yok',
  missingHistoryRows([{ filename: SYG, status: 'modified', patch: '@@ -1 +1 @@\n-eski\n+yeni' }]), [SYG]);
check('PG ve matris degisti, yalniz matriste satir var',
  missingHistoryRows([
    { filename: PG, status: 'modified', patch: '+| **REQ-KMLK-001** | metin |' },
    { filename: MATRIS, status: 'modified', patch: row },
  ]), [PG]);
check('silinen gecmis satiri sayilmaz',
  missingHistoryRows([{ filename: SYG, status: 'modified', patch: '-| 2026-10-14 | 1.8 | Eski | Bilgi Islem |' }]), [SYG]);
check('sablon ve yeni dosya denetlenmez',
  missingHistoryRows([
    { filename: 'docs/33061/TEC.2-paydas-ihtiyac-ve-gereksinimleri/paydas-gereksinimleri/SABLON-PG.md', status: 'modified', patch: '+x' },
    { filename: 'docs/33061/TEC.3-sistem-yazilim-gereksinimleri/gereksinimler/SYG-PRSN.md', status: 'added', patch: '+x' },
  ]), []);
check('baska belge denetlenmez',
  missingHistoryRows([{ filename: 'docs/adr/ADR-0006-kimlik-dogrulama-ve-oturum.md', status: 'modified', patch: '+x' }]), []);
check('patch yoksa (cok buyuk) gecer',
  missingHistoryRows([{ filename: SYG, status: 'modified' }]), []);

// Is akislarindaki betikler derlenebilir mi? Yazim hatasi aksi halde CI'da ilk calismada,
// main'deki denetimde ise ancak birlestirmeden sonra gorulurdu (#390).
console.log('Is akisi betikleri:');
const AsyncFunction = Object.getPrototypeOf(async () => {}).constructor;
for (const wf of ['.github/workflows/pr-traceability-check.yml', '.github/workflows/branch-protection-check.yml']) {
  const lines = readFileSync(wf, 'utf8').replace(/\r\n/g, '\n').split('\n');
  lines.forEach((line, i) => {
    if (!/^\s+script: \|\s*$/.test(line)) return;
    const indent = line.search(/\S/);
    const body = [];
    for (const l of lines.slice(i + 1)) {
      if (l.trim() !== '' && l.search(/\S/) <= indent) break;
      body.push(l);
    }
    let compiled = true;
    try {
      new AsyncFunction('github', 'context', 'core', 'require', body.join('\n'));
    } catch {
      compiled = false;
    }
    check(`${wf.split('/').pop()}:${i + 1} derleniyor`, compiled, true);
  });
}

console.log(failures === 0 ? '\nTum ornekler beklendigi gibi.' : `\n${failures} ornek beklenenden farkli!`);
process.exit(failures ? 1 : 0);
