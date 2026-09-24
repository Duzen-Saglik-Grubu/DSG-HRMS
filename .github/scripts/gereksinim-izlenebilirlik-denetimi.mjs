// Gereksinim izlenebilirlik denetimi (TEC.3 çıktı f · TEC.2 çıktı i)
//
// Her modül için şunlar denetlenir:
//   1. Her paydaş gereksinimi en az bir sistem gereksinimine bağlı.
//   2. Sistem gereksinimlerinin kaynak kimlikleri paydaş gereksinimleri belgesinde var.
//   3. Sistem gereksinimi kimlikleri tekil ve sıralı; metin içi atıflar boşa düşmüyor.
//   4. SG belgesinin §8 ters tablosu ve izlenebilirlik matrisinin "Sistem gereksinimi"
//      sütunu, SG §4 ile AYNI eşlemeyi gösteriyor.
//
// Neden otomatik? Eşleme üç yerde duruyor (SG §4, SG §8, matris). Elle tutulan üç
// kopya, ilk değişiklikte birbirinden ayrılır ve izlenebilirlik varmış gibi görünür.
//
// Denetimin KENDİSİ de sınanır: gerçek belgelerden bir paydaş gereksinimi silinmiş
// gibi yapılır ve denetimin bunu yakalaması beklenir. Hiçbir satırı eşleştiremeyen
// bozuk bir ifade her şeyi geçirir; kapı çalışıyor görünürken hiçbir şey denetlemez.
//
// Kullanım: node .github/scripts/gereksinim-izlenebilirlik-denetimi.mjs

import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { join } from 'node:path';

const KOK = 'docs/33061';
const TEC2 = join(KOK, 'TEC.2-paydas-ihtiyac-ve-gereksinimleri/paydas-gereksinimleri');
const TEC3 = join(KOK, 'TEC.3-sistem-yazilim-gereksinimleri/gereksinimler');
const MATRIS = join(KOK, 'izlenebilirlik-matrisi.md');

/** "SG-KMLK-013, 016" -> ["SG-KMLK-013", "SG-KMLK-016"]; "—" -> [] */
function ac(hucre, onek) {
  const s = hucre.replace(/`/g, '').trim();
  if (s === '—' || s === '') return [];
  return s.split(',').map((p) => p.trim()).map((p) => (/^\d+$/.test(p) ? `${onek}${p}` : p));
}

const esit = (a, b) => a.length === b.length && a.every((x, i) => x === b[i]);

export function denetle(modul, pg, sg, matris) {
  const h = [];
  const sgOnek = `SG-${modul}-`;

  // Paydaş gereksinimleri: "| **REQ-KMLK-001** |" veya "| **PG-KMLK-01** |"
  const pgKimlik = [...pg.matchAll(new RegExp(`^\\| \\*\\*((?:REQ|PG)-${modul}-\\d+)\\*\\*`, 'gm'))].map((m) => m[1]);
  if (!pgKimlik.length) return [`${modul}: paydaş gereksinimi satırı bulunamadı`];
  const pgKume = new Set(pgKimlik);

  // SG satırları: | **SG-KMLK-001** | metin | tür | kaynak | doğrulama | parametre |
  const sgSatir = [...sg.matchAll(new RegExp(`^\\| \\*\\*(${sgOnek}\\d{3})\\*\\* \\|(.*)$`, 'gm'))].map((m) => {
    const hucre = m[2].split(' | ');
    return { id: m[1], metin: hucre[0], kaynak: ac(hucre[2] ?? '', '') };
  });
  if (!sgSatir.length) return [`${modul}: sistem gereksinimi satırı bulunamadı`];

  const gorulen = new Set();
  sgSatir.forEach((s, i) => {
    const beklenen = `${sgOnek}${String(i + 1).padStart(3, '0')}`;
    if (s.id !== beklenen) h.push(`${s.id}: sıra bozuk (beklenen ${beklenen})`);
    if (gorulen.has(s.id)) h.push(`${s.id}: tekrar eden kimlik`);
    gorulen.add(s.id);
    if (!s.kaynak.length) h.push(`${s.id}: kaynak yok`);
    for (const k of s.kaynak) if (!pgKume.has(k)) h.push(`${s.id}: kaynak ${k} paydaş gereksinimlerinde yok`);
  });
  for (const s of sgSatir) {
    for (const m of s.metin.matchAll(new RegExp(`${sgOnek}\\d{3}`, 'g'))) {
      if (!gorulen.has(m[0])) h.push(`${s.id}: metindeki ${m[0]} atfı karşılıksız`);
    }
  }

  // Beklenen eşleme: paydaş -> [SG]
  const beklenen = new Map(pgKimlik.map((k) => [k, []]));
  for (const s of sgSatir) for (const k of s.kaynak) beklenen.get(k)?.push(s.id);
  for (const [k, v] of beklenen) if (!v.length) h.push(`${k}: hiçbir sistem gereksinimine bağlı değil`);

  // SG §8 ters tablo: | `REQ-KMLK-001` | SG-KMLK-013, 016 |
  const ters = new Map([...sg.matchAll(new RegExp(`^\\| \`((?:REQ|PG)-${modul}-\\d+)\` \\| ([^|]*) \\|$`, 'gm'))]
    .map((m) => [m[1], ac(m[2], sgOnek)]));
  for (const [k, v] of beklenen) {
    if (!ters.has(k)) h.push(`SG §8: ${k} satırı yok`);
    else if (!esit(ters.get(k), v)) h.push(`SG §8: ${k} = ${ters.get(k).join(', ')}; beklenen ${v.join(', ')}`);
  }

  // Matris: | n | `REQ-KMLK-001` metin | Sistem gereksinimi | ...
  const mat = new Map([...matris.matchAll(new RegExp(`^\\| \\d+ \\| \`((?:REQ|PG)-${modul}-\\d+)\`[^|]*\\| ([^|]*) \\|`, 'gm'))]
    .map((m) => [m[1], ac(m[2], sgOnek)]));
  for (const [k, v] of beklenen) {
    if (!mat.has(k)) h.push(`Matris: ${k} satırı yok`);
    else if (!esit(mat.get(k), v)) h.push(`Matris: ${k} = ${mat.get(k).join(', ') || '—'}; beklenen ${v.join(', ')}`);
  }

  return h;
}

// ------------------------------------------------------------------ çalıştır
const moduller = existsSync(TEC3)
  ? readdirSync(TEC3).map((f) => /^SG-([A-Z]+)\.md$/.exec(f)?.[1]).filter(Boolean)
  : [];
if (!moduller.length) {
  console.log('Sistem gereksinimi belgesi yok; denetlenecek bir şey yok.');
  process.exit(0);
}

const matris = readFileSync(MATRIS, 'utf8');
let hata = 0;

for (const modul of moduller) {
  const pgYol = join(TEC2, `PG-${modul}.md`);
  if (!existsSync(pgYol)) {
    console.error(`✗ ${modul}: ${pgYol} yok — sistem gereksinimi, paydaş gereksinimi olmadan yazılamaz`);
    hata++;
    continue;
  }
  const pg = readFileSync(pgYol, 'utf8');
  const sg = readFileSync(join(TEC3, `SG-${modul}.md`), 'utf8');

  // Önce denetimin kendisi: ilk paydaş gereksinimine yapılan tüm atıflar SG'den
  // silinirse denetim bunu YAKALAMALI.
  const ilk = new RegExp(`\\| \\*\\*((?:REQ|PG)-${modul}-\\d+)\\*\\*`).exec(pg)?.[1];
  if (ilk) {
    const bozuk = sg.replace(new RegExp(`${ilk}(, )?`, 'g'), '');
    if (!denetle(modul, pg, bozuk, matris).some((x) => x.startsWith(`${ilk}:`))) {
      console.error(`✗ ${modul}: denetim kendi sınamasını geçemedi — bağsız kalan ${ilk} yakalanmadı`);
      hata++;
      continue;
    }
  }

  const sonuc = denetle(modul, pg, sg, matris);
  if (sonuc.length) {
    console.error(`✗ ${modul}: ${sonuc.length} sorun`);
    for (const s of sonuc) console.error(`    ${s}`);
    hata++;
  } else {
    console.log(`✓ ${modul}: her paydaş gereksinimi karşılanıyor; SG §8 ve izlenebilirlik matrisi SG §4 ile tutarlı`);
  }
}

process.exit(hata ? 1 : 0);
