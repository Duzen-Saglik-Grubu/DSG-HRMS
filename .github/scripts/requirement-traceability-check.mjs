// Gereksinim izlenebilirlik denetimi (TEC.3 çıktı f · TEC.2 çıktı i)
//
// Her modül için şunlar denetlenir:
//   1. Her paydaş gereksinimi en az bir sistem gereksinimine bağlı.
//   2. Sistem gereksinimlerinin kaynak kimlikleri paydaş gereksinimleri belgesinde var.
//   3. Sistem gereksinimi kimlikleri tekil ve sıralı; metin içi atıflar boşa düşmüyor.
//   4. SYG belgesinin §8 ters tablosu ve izlenebilirlik matrisinin "Sistem gereksinimi"
//      sütunu, SYG §4 ile AYNI eşlemeyi gösteriyor.
//
// Neden otomatik? Eşleme üç yerde duruyor (SYG §4, SYG §8, matris). Elle tutulan üç
// kopya, ilk değişiklikte birbirinden ayrılır ve izlenebilirlik varmış gibi görünür.
//
// Denetimin KENDİSİ de sınanır: gerçek belgelerden bir paydaş gereksinimi silinmiş
// gibi yapılır ve denetimin bunu yakalaması beklenir. Hiçbir satırı eşleştiremeyen
// bozuk bir ifade her şeyi geçirir; kapı çalışıyor görünürken hiçbir şey denetlemez.
//
// Kullanım: node .github/scripts/requirement-traceability-check.mjs

import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = 'docs/33061';
const STAKEHOLDER_DIR = join(ROOT, 'TEC.2-paydas-ihtiyac-ve-gereksinimleri/paydas-gereksinimleri');
const SYSTEM_DIR = join(ROOT, 'TEC.3-sistem-yazilim-gereksinimleri/gereksinimler');
const MATRIX_PATH = join(ROOT, 'izlenebilirlik-matrisi.md');

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

  const errors = check(module, stakeholderDoc, systemDoc, matrixDoc);
  if (errors.length) {
    console.error(`✗ ${module}: ${errors.length} sorun`);
    for (const e of errors) console.error(`    ${e}`);
    failures++;
  } else {
    console.log(`✓ ${module}: her paydaş gereksinimi karşılanıyor; SYG §8 ve izlenebilirlik matrisi SYG §4 ile tutarlı`);
  }
}

process.exit(failures ? 1 : 0);
