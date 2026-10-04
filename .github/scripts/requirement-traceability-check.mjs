// Gereksinim izlenebilirlik denetimi (TEC.3 çıktı f · TEC.2 çıktı i)
//
// Her modül için şunlar denetlenir:
//   1. Her paydaş gereksinimi en az bir sistem gereksinimine bağlı.
//   2. Sistem gereksinimlerinin kaynak kimlikleri paydaş gereksinimleri belgesinde var.
//   3. Sistem gereksinimi kimlikleri tekil ve sıralı; metin içi atıflar boşa düşmüyor.
//   4. SYG belgesinin §8 ters tablosu ve izlenebilirlik matrisinin "Sistem gereksinimi"
//      sütunu, SYG §4 ile AYNI eşlemeyi gösteriyor.
//   5. Her sistem gereksinimi doğrulanmış: en az bir test dosyası onu anıyor. Doğrulama
//      yöntemi Analiz, İnceleme veya Gösterim olanlar için TEC.9 doğrulama raporu da kanıttır.
//      (#122: SYG-KMLK-060/061/062 bu denetim olmadığı için hiçbir kapıya takılmadan kaldı.)
//
// Neden otomatik? Eşleme üç yerde duruyor (SYG §4, SYG §8, matris). Elle tutulan üç
// kopya, ilk değişiklikte birbirinden ayrılır ve izlenebilirlik varmış gibi görünür.
//
// Denetimin KENDİSİ de sınanır: gerçek belgelerden bir paydaş gereksinimi silinmiş
// gibi yapılır ve denetimin bunu yakalaması beklenir. Hiçbir satırı eşleştiremeyen
// bozuk bir ifade her şeyi geçirir; kapı çalışıyor görünürken hiçbir şey denetlemez.
//
// Kullanım: node .github/scripts/requirement-traceability-check.mjs

import { readFileSync, readdirSync, existsSync, statSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = 'docs/33061';
const STAKEHOLDER_DIR = join(ROOT, 'TEC.2-paydas-ihtiyac-ve-gereksinimleri/paydas-gereksinimleri');
const SYSTEM_DIR = join(ROOT, 'TEC.3-sistem-yazilim-gereksinimleri/gereksinimler');
const MATRIX_PATH = join(ROOT, 'izlenebilirlik-matrisi.md');
const REPORT_DIR = join(ROOT, 'TEC.9-dogrulama');
const TEST_DIRS = ['src/backend/tests', 'src/frontend/dsg-hrms-web/src'];
const NON_TEST_METHODS = ['Analiz', 'İnceleme', 'Gösterim'];

/** Dizindeki dosyalar (derleme ve bağımlılık klasörleri hariç). */
function walk(dir, accept, out = []) {
  if (!existsSync(dir)) return out;
  for (const name of readdirSync(dir)) {
    if (['node_modules', 'bin', 'obj', 'dist', '.git'].includes(name)) continue;
    const path = join(dir, name);
    if (statSync(path).isDirectory()) walk(path, accept, out);
    else if (accept(path)) out.push(path);
  }
  return out;
}

const isTestFile = (p) => p.endsWith('.cs') ? /tests[\\/]/.test(p) : /\.test\.tsx?$/.test(p);

/**
 * Metindeki SYG atıfları; kısaltmalar açılır: "SYG-KMLK-046, 050" -> 046, 050;
 * "SYG-KMLK-031…034" ve "SYG-KMLK-051–053" -> aralık.
 */
export function referencedIds(text, module) {
  const ids = new Set();
  const prefix = `SYG-${module}-`;
  for (const m of text.matchAll(new RegExp(`${prefix}(\\d{3})((?:\\s*(?:,|…|–)\\s*\\d{3})*)`, 'g'))) {
    let previous = Number(m[1]);
    ids.add(previous);
    for (const t of m[2].matchAll(/(,|…|–)\s*(\d{3})/g)) {
      const n = Number(t[2]);
      if (t[1] === ',') ids.add(n);
      else for (let k = previous; k <= n; k++) ids.add(k);
      previous = n;
    }
  }
  return new Set([...ids].map((n) => `${prefix}${String(n).padStart(3, '0')}`));
}

/** 5. denetim: her SYG bir teste (veya yöntemine uygun bir doğrulama raporuna) bağlı. */
export function checkVerification(module, systemDoc, testTexts, reportTexts) {
  const sygPrefix = `SYG-${module}-`;
  const rows = [...systemDoc.matchAll(new RegExp(`^\\| \\*\\*(${sygPrefix}\\d{3})\\*\\* \\|(.*)$`, 'gm'))]
    .map((m) => ({ id: m[1], method: (m[2].split(' | ')[3] ?? '').trim() }));
  const inTests = new Set(testTexts.flatMap((t) => [...referencedIds(t, module)]));
  const inReports = new Set(reportTexts.flatMap((t) => [...referencedIds(t, module)]));
  return rows
    .filter((r) => !inTests.has(r.id) && !(NON_TEST_METHODS.includes(r.method) && inReports.has(r.id)))
    .map((r) => `${r.id}: doğrulama kanıtı yok (yöntem: ${r.method || '—'}; hiçbir test${NON_TEST_METHODS.includes(r.method) ? ' veya TEC.9 raporu' : ''} anmıyor)`);
}

/** "SYG-KMLK-013, 016" -> ["SYG-KMLK-013", "SYG-KMLK-016"]; "—" -> [] */
function expandIds(cell, prefix) {
  const s = cell.replace(/`/g, '').trim();
  if (s === '—' || s === '') return [];
  return s.split(',').map((p) => p.trim()).map((p) => (/^\d+$/.test(p) ? `${prefix}${p}` : p));
}

const sameList = (a, b) => a.length === b.length && a.every((x, i) => x === b[i]);

export function check(module, stakeholderDoc, systemDoc, matrixDoc) {
  const errors = [];
  const sygPrefix = `SYG-${module}-`;

  // Paydaş gereksinimleri: "| **REQ-KMLK-001** |" veya "| **PG-KMLK-01** |"
  const stakeholderIds = [...stakeholderDoc.matchAll(new RegExp(`^\\| \\*\\*((?:REQ|PG)-${module}-\\d+)\\*\\*`, 'gm'))]
    .map((m) => m[1]);
  if (!stakeholderIds.length) return [`${module}: paydaş gereksinimi satırı bulunamadı`];
  const stakeholderSet = new Set(stakeholderIds);

  // SYG satırları: | **SYG-KMLK-001** | metin | tür | kaynak | doğrulama | parametre |
  const rows = [...systemDoc.matchAll(new RegExp(`^\\| \\*\\*(${sygPrefix}\\d{3})\\*\\* \\|(.*)$`, 'gm'))].map((m) => {
    const cells = m[2].split(' | ');
    return { id: m[1], text: cells[0], sources: expandIds(cells[2] ?? '', '') };
  });
  if (!rows.length) return [`${module}: sistem gereksinimi satırı bulunamadı`];

  const seen = new Set();
  rows.forEach((row, i) => {
    const expectedId = `${sygPrefix}${String(i + 1).padStart(3, '0')}`;
    if (row.id !== expectedId) errors.push(`${row.id}: sıra bozuk (beklenen ${expectedId})`);
    if (seen.has(row.id)) errors.push(`${row.id}: tekrar eden kimlik`);
    seen.add(row.id);
    if (!row.sources.length) errors.push(`${row.id}: kaynak yok`);
    for (const s of row.sources) {
      if (!stakeholderSet.has(s)) errors.push(`${row.id}: kaynak ${s} paydaş gereksinimlerinde yok`);
    }
  });
  for (const row of rows) {
    for (const m of row.text.matchAll(new RegExp(`${sygPrefix}\\d{3}`, 'g'))) {
      if (!seen.has(m[0])) errors.push(`${row.id}: metindeki ${m[0]} atfı karşılıksız`);
    }
  }

  // Beklenen eşleme: paydaş -> [SYG]
  const expected = new Map(stakeholderIds.map((id) => [id, []]));
  for (const row of rows) for (const s of row.sources) expected.get(s)?.push(row.id);
  for (const [id, sgs] of expected) if (!sgs.length) errors.push(`${id}: hiçbir sistem gereksinimine bağlı değil`);

  // SYG §8 ters tablo: | `REQ-KMLK-001` | SYG-KMLK-013, 016 |
  const reverse = new Map([...systemDoc.matchAll(new RegExp(`^\\| \`((?:REQ|PG)-${module}-\\d+)\` \\| ([^|]*) \\|$`, 'gm'))]
    .map((m) => [m[1], expandIds(m[2], sygPrefix)]));
  for (const [id, sgs] of expected) {
    if (!reverse.has(id)) errors.push(`SYG §8: ${id} satırı yok`);
    else if (!sameList(reverse.get(id), sgs)) errors.push(`SYG §8: ${id} = ${reverse.get(id).join(', ')}; beklenen ${sgs.join(', ')}`);
  }

  // Matris: | n | `REQ-KMLK-001` metin | Sistem gereksinimi | ...
  const matrix = new Map([...matrixDoc.matchAll(new RegExp(`^\\| \\d+ \\| \`((?:REQ|PG)-${module}-\\d+)\`[^|]*\\| ([^|]*) \\|`, 'gm'))]
    .map((m) => [m[1], expandIds(m[2], sygPrefix)]));
  for (const [id, sgs] of expected) {
    if (!matrix.has(id)) errors.push(`Matris: ${id} satırı yok`);
    else if (!sameList(matrix.get(id), sgs)) errors.push(`Matris: ${id} = ${matrix.get(id).join(', ') || '—'}; beklenen ${sgs.join(', ')}`);
  }

  return errors;
}

// ------------------------------------------------------------------ çalıştır
const modules = existsSync(SYSTEM_DIR)
  ? readdirSync(SYSTEM_DIR).map((f) => /^SYG-([A-Z]+)\.md$/.exec(f)?.[1]).filter(Boolean)
  : [];
if (!modules.length) {
  console.log('Sistem gereksinimi belgesi yok; denetlenecek bir şey yok.');
  process.exit(0);
}

const matrixDoc = readFileSync(MATRIX_PATH, 'utf8');
const testTexts = TEST_DIRS.flatMap((d) => walk(d, isTestFile)).map((p) => readFileSync(p, 'utf8'));
const reportTexts = walk(REPORT_DIR, (p) => p.endsWith('.md')).map((p) => readFileSync(p, 'utf8'));
let failures = 0;

for (const module of modules) {
  const stakeholderPath = join(STAKEHOLDER_DIR, `PG-${module}.md`);
  if (!existsSync(stakeholderPath)) {
    console.error(`✗ ${module}: ${stakeholderPath} yok — sistem gereksinimi, paydaş gereksinimi olmadan yazılamaz`);
    failures++;
    continue;
  }
  const stakeholderDoc = readFileSync(stakeholderPath, 'utf8');
  const systemDoc = readFileSync(join(SYSTEM_DIR, `SYG-${module}.md`), 'utf8');

  // Önce denetimin kendisi: ilk paydaş gereksinimine yapılan tüm atıflar SYG'den
  // silinirse denetim bunu YAKALAMALI.
  const firstId = new RegExp(`\\| \\*\\*((?:REQ|PG)-${module}-\\d+)\\*\\*`).exec(stakeholderDoc)?.[1];
  if (firstId) {
    const broken = systemDoc.replace(new RegExp(`${firstId}(, )?`, 'g'), '');
    if (!check(module, stakeholderDoc, broken, matrixDoc).some((e) => e.startsWith(`${firstId}:`))) {
      console.error(`✗ ${module}: denetim kendi sınamasını geçemedi — bağsız kalan ${firstId} yakalanmadı`);
      failures++;
      continue;
    }
  }

  // 5. denetimin kendisi: ilk SYG'nin bütün atıfları testlerden silinirse YAKALANMALI.
  const firstSyg = new RegExp(`\\| \\*\\*(SYG-${module}-\\d{3})\\*\\*`).exec(systemDoc)?.[1];
  if (firstSyg) {
    const strip = (t) => [...referencedIds(t, module)].includes(firstSyg) ? '' : t;
    if (!checkVerification(module, systemDoc, testTexts.map(strip), reportTexts.map(strip)).some((e) => e.startsWith(`${firstSyg}:`))) {
      console.error(`✗ ${module}: doğrulama denetimi kendi sınamasını geçemedi — kanıtsız kalan ${firstSyg} yakalanmadı`);
      failures++;
      continue;
    }
  }

  const errors = [
    ...check(module, stakeholderDoc, systemDoc, matrixDoc),
    ...checkVerification(module, systemDoc, testTexts, reportTexts),
  ];
  if (errors.length) {
    console.error(`✗ ${module}: ${errors.length} sorun`);
    for (const e of errors) console.error(`    ${e}`);
    failures++;
  } else {
    console.log(`✓ ${module}: her paydaş gereksinimi karşılanıyor; SYG §8 ve izlenebilirlik matrisi SYG §4 ile tutarlı; her sistem gereksinimi doğrulanmış`);
  }
}

process.exit(failures ? 1 : 0);
