// Belge tutarliligi denetimi (MAN.6 / TEC.3; #214, denetim A-06 ve TEC.3-7)
//
// Uc kural denetlenir (3. kural #389'da eklendi; ayrintisi kendi bolumunde):
//   1. "Son guncelleme" basligi, degisiklik gecmisinin en yeni satirindan eski olamaz
//      (harita bolum 4.4, MAN.6 YAKLASIM bolum 2.1). docs/ altindaki her .md dosyasinda, hem
//      basinda "**Son guncelleme:** YYYY-AA-GG" hem sonunda "Degisiklik Gecmisi"
//      tablosu ("| YYYY-AA-GG | surum | ...") olan belgeler denetlenir.
//   2. SYG-*.md bolum 1 ozet sayimlari bolum 4 tablosundan (ve bolum 5 KPO tablosundan) yeniden
//      hesaplananla ayni. bolum 1'de taninmayan bir satir varsa denetim duser: sayilmayan
//      bir satir, denetleniyormus gibi gorunmemelidir; betik genisletilir.
//   3. ADR'ler: dizin <-> degisiklik gecmisi, gecmis <-> baslik karar listesi, KR-099
//      siniflandirmasi ve revize edilen paydas gereksinimine dayanan ADR'nin karari anmasi.
//
// Neden otomatik? 13.10.2026 modul sonu denetiminde 13 belgenin basligi gecmisin
// gerisindeydi ve SYG bolum 1 sayimlari iki degisiklik talebi sonrasinda bayatti. Yazili
// ama denetlenmeyen kurallar birkac gun icinde asindi (MAN.8 Ek A, O-3 ve A-06).
//
// Denetimin KENDISI once sinanir (requirement-traceability-check.mjs gibi): gercek bir
// belgenin basligi eskitilir ve gercek SYG'den bir satir silinir; denetim ikisini de
// yakalamalidir. Hicbir belge bulunamazsa veya hicbir satir eslesmezse denetim duser;
// bozuk bir ifade her seyi gecirip kapi calisiyor gibi gorunemez.
//
// Kapsam disi: docs/TSE_*, docs/TS_*, docs/NetGSM* (lisansli/harici) ve git'in yok
// saydigi dosyalar.
//
// Kullanim: node .github/scripts/doc-consistency-check.mjs [kok-dizin]

import { readFileSync, existsSync, readdirSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { join } from 'node:path';

const ROOT = process.argv[2] ?? '.';
const EXCLUDED = /^docs\/(TSE_|TS_|NetGSM)/;
const SYSTEM_DIR = 'docs/33061/TEC.3-sistem-yazilim-gereksinimleri/gereksinimler';
const STAKEHOLDER_DIR = 'docs/33061/TEC.2-paydas-ihtiyac-ve-gereksinimleri/paydas-gereksinimleri';
// Bu sayidan az belge denetlenirse kapsama bozulmus demektir (14.10.2026: 40'tan fazla).
const MIN_DATED_DOCS = 20;

// ------------------------------------------------------------------ 1. tarih kurali

/** Baslik bolgesindeki (ilk "## " basligindan onceki) "Son guncelleme" tarihi. */
export function headerDate(text) {
  const head = text.split(/^## /m)[0];
  return /^\*\*Son güncelleme:\*\*\s*(\d{4}-\d{2}-\d{2})/m.exec(head)?.[1] ?? null;
}

/** "Degisiklik Gecmisi" bolumundeki satirlarin tarihleri ("| 2026-10-14 | 1.2 | ..."). */
export function historyDates(text) {
  const lines = text.split('\n');
  let start = -1;
  let level = 0;
  lines.forEach((line, i) => {
    const m = /^(#{1,6})\s+.*(değişiklik|revizyon)\s+geçmişi/i.exec(line);
    if (m) { start = i; level = m[1].length; }
  });
  if (start < 0) return [];
  const dates = [];
  for (let i = start + 1; i < lines.length; i++) {
    const h = /^(#{1,6})\s/.exec(lines[i]);
    if (h && h[1].length <= level) break;
    const m = /^\|\s*(\d{4}-\d{2}-\d{2})\s*\|\s*\**v?\d+(?:\.\d+)*/.exec(lines[i]);
    if (m) dates.push(m[1]);
  }
  return dates;
}

/** Tarih kurali; belge denetim kapsamindaysa { checked: true, error? } dondurur. */
export function checkDates(path, text) {
  const header = headerDate(text);
  const dates = historyDates(text);
  if (!header || !dates.length) return { checked: false };
  const latest = dates.reduce((a, b) => (b > a ? b : a));
  if (header < latest) {
    return { checked: true, error: `${path}: "Son güncelleme" ${header}, değişiklik geçmişinin son satırı ${latest}` };
  }
  return { checked: true };
}

// ------------------------------------------------------------------ 2. SYG bolum 1 sayimlari

function section(text, number) {
  const m = new RegExp(`^## ${number}\\.[^\\n]*\\n([\\s\\S]*?)(?=^## )`, 'm').exec(text);
  return m ? m[1] : null;
}

/** SYG bolum 1 sayimlarini bolum 4 ve bolum 5'ten yeniden hesaplar; farklari dondurur. */
export function checkSummary(module, systemDoc, stakeholderDoc) {
  const errors = [];
  const summary = section(systemDoc, 1);
  if (!summary) return [`SYG-${module}: §1 Özet bölümü bulunamadı`];

  const rows = [...systemDoc.matchAll(new RegExp(`^\\| \\*\\*SYG-${module}-\\d{3}\\*\\* \\|(.*)$`, 'gm'))].map((m) => {
    const cells = m[1].split(' | ');
    return {
      type: (cells[1] ?? '').trim(),
      sources: (cells[2] ?? '').split(',').map((s) => s.trim()).filter(Boolean),
      method: (cells[3] ?? '').trim(),
      parameter: (cells[4] ?? '').replace(/\|\s*$/, '').trim(),
    };
  });
  if (!rows.length) return [`SYG-${module}: §4'te gereksinim satırı bulunamadı`];

  const stakeholderIds = new Set([...stakeholderDoc.matchAll(new RegExp(`^\\| \\*\\*((?:REQ|PG)-${module}-\\d+)\\*\\*`, 'gm'))].map((m) => m[1]));
  const covered = new Set(rows.flatMap((r) => r.sources).filter((s) => stakeholderIds.has(s)));
  const kpoCount = [...systemDoc.matchAll(new RegExp(`^\\| \\*\\*KPÖ-${module}-\\d+\\*\\*`, 'gm'))].length;
  const count = (key, value) => rows.filter((r) => r[key] === value).length;

  // "| Etiket | **81** |" veya "| Etiket | 56 / 56 |"; [ \t] satir sonunu asmaz.
  const listed = [...summary.matchAll(/^\|[ \t]*([^|\n]+?)[ \t]*\|[ \t]*\**[ \t]*(\d[\d \t/]*?)[ \t]*\**[ \t]*\|[ \t]*$/gm)];
  if (!listed.length) return [`SYG-${module}: §1 tablosunda sayım satırı bulunamadı`];

  const seenTypes = new Set();
  const seenMethods = new Set();
  for (const [, label, value] of listed) {
    let expected;
    let m;
    if (label === 'Sistem gereksinimi') expected = String(rows.length);
    else if (label === 'Karşılanan paydaş gereksinimi') expected = `${covered.size} / ${stakeholderIds.size}`;
    else if (label === 'Parametreye bağlı') expected = String(rows.filter((r) => r.parameter && r.parameter !== '—').length);
    else if (label === 'Kritik performans ölçütü') expected = String(kpoCount);
    else if ((m = /^Tür — (.+)$/.exec(label))) { seenTypes.add(m[1]); expected = String(count('type', m[1])); }
    else if ((m = /^Doğrulama — (.+)$/.exec(label))) { seenMethods.add(m[1]); expected = String(count('method', m[1])); }
    else { errors.push(`SYG-${module} §1: "${label}" satırı tanınmıyor; denetim betiğine eklenmeli`); continue; }
    if (value.replace(/\s+/g, ' ').trim() !== expected) {
      errors.push(`SYG-${module} §1: "${label}" = ${value.trim()}; §4'ten hesaplanan ${expected}`);
    }
  }
  for (const t of new Set(rows.map((r) => r.type))) {
    if (!seenTypes.has(t)) errors.push(`SYG-${module} §1: "Tür — ${t}" satırı yok (§4'te ${count('type', t)} madde)`);
  }
  for (const v of new Set(rows.map((r) => r.method))) {
    if (!seenMethods.has(v)) errors.push(`SYG-${module} §1: "Doğrulama — ${v}" satırı yok (§4'te ${count('method', v)} madde)`);
  }
  return errors;
}

// ------------------------------------------------------------------ 3. ADR'ler ve kararlar (#389)
//
// ADR'ler T3'te iki kez, T1'de ucuncu kez kararlarin gerisinde kaldi (TEC.5-9): dizin bes ADR'de
// bayatti, ADR-0022 KR-132'yi ve KR-131'i yansitmiyordu, KR-099 siniflandirmasi yapilmamisti.
// #212 maddeleri tek tek kapatti; kopyalar arasinda otomatik bag yoktu. Dort kural:
//   a. Dizin (docs/adr/README.md): her ADR'nin satiri var; "Son degisiklik" = gecmisin son satiri.
//   b. ADR gecmisinde anilan her KR, ADR basligindaki "Karar defteri karsiligi" satirinda da var
//      (KR-099 yordam kuralidir, karar degildir; sayilmaz).
//   c. KR-099 siniflandirmasi: "ADR-NNNN ... bu kararla revize edildi" diyen karar ya yeni bir
//      ADR'yle ("Degistirdigi: ADR-NNNN" basligi ve karar basligin listesinde) ya da ADR-NNNN
//      gecmisinde karari anan satirda "KR-099" gerekcesiyle kayitlidir.
//   d. "bu kararla revize edildi" diyen kararin revize ettigi PG kimligi bir ADR'nin "Ilgili
//      gereksinimler" basliginda varsa, o ADR karari anar (KR-132 -> ADR-0022 §3 boyle kaydi).
const ADR_HISTORY = /^\| (\d{4}-\d{2}-\d{2}) \| ([\d.]+) \|/;
const KR_SINCE_099 = (id) => Number(id.slice(3)) > 99; // siniflandirma kurali KR-099'la basladi

function headerLine(text, label) {
  return (text.match(new RegExp(`^\\*\\*${label}:\\*\\*.*$`, 'm')) || [''])[0];
}

// "PG-PERS-17, 21, 24–27" -> PG-PERS-17, PG-PERS-21, PG-PERS-24 ... PG-PERS-27
function pgIds(text) {
  const out = new Set();
  for (const m of text.matchAll(/PG-([A-Z]+)-([\d\s,–-]+)/g)) {
    for (const part of m[2].split(',')) {
      const r = part.trim().match(/^(\d+)(?:\s*[–-]\s*(\d+))?$/);
      if (!r) break;
      const last = r[2] ? Number(r[2]) : Number(r[1]);
      for (let i = Number(r[1]); i <= last; i++) out.add(`PG-${m[1]}-${String(i).padStart(2, '0')}`);
    }
  }
  return out;
}

// adrs: [{ id, file, text }]; register: karar defteri metni. Hata iletileri dondurur.
function checkAdrs(readme, adrs, register) {
  const errors = [];
  const readmeLines = readme.split('\n');
  for (const adr of adrs) {
    const history = adr.text.split('\n').filter((l) => ADR_HISTORY.test(l));
    const last = history.at(-1)?.match(ADR_HISTORY);
    // a. dizin
    const row = readmeLines.find((l) => l.includes(`](${adr.file})`));
    if (!row) {
      errors.push(`${adr.id}: dizinde (docs/adr/README.md) satırı yok`);
    } else if (last) {
      const cell = row.split('|')[4]?.trim();
      const expected = `${last[1]} (${last[2]})`;
      if (cell !== expected) errors.push(`${adr.id}: dizinde "Son değişiklik" ${cell}, değişiklik geçmişinin son satırı ${expected}`);
    }
    // b. baslik
    const header = headerLine(adr.text, 'Karar defteri karşılığı');
    const cited = new Set(history.join('\n').match(/KR-\d{3}/g) || []);
    cited.delete('KR-099');
    const missing = [...cited].filter((kr) => !header.includes(kr));
    if (missing.length) errors.push(`${adr.id}: değişiklik geçmişinde anılan ${missing.join(', ')} başlıktaki "Karar defteri karşılığı" satırında yok`);
  }
  const byId = new Map(adrs.map((a) => [a.id, a]));
  for (const line of register.split('\n').filter((l) => /^\| KR-\d{3} \|/.test(l))) {
    const kr = line.slice(2, 8);
    const decision = line.split('|')[4] || '';
    if (!/bu kararla revize/.test(decision)) continue;
    // c. KR-099 siniflandirmasi
    if (KR_SINCE_099(kr)) {
      for (const target of new Set(decision.match(/ADR-\d{4}/g) || [])) {
        const asNewAdr = adrs.some((a) => a.id !== target
          && headerLine(a.text, 'Değiştirdiği').includes(target)
          && headerLine(a.text, 'Karar defteri karşılığı').includes(kr));
        const inPlace = byId.get(target)?.text.split('\n')
          .some((l) => ADR_HISTORY.test(l) && l.includes(kr) && l.includes('KR-099'));
        if (!asNewAdr && !inPlace) {
          errors.push(`${kr}: ${target}'ü revize ediyor ama KR-099 sınıflandırması yok — yeni ADR ("Değiştirdiği: ${target}") ya da ${target} geçmişinde "KR-099" gerekçeli satır`);
        }
      }
    }
    // d. PG -> ADR bagi
    const revised = pgIds(decision);
    for (const adr of adrs) {
      const related = pgIds(headerLine(adr.text, 'İlgili gereksinimler'));
      const hit = [...revised].filter((pg) => related.has(pg));
      if (hit.length && !adr.text.includes(kr)) {
        errors.push(`${kr}: ${hit.join(', ')} revize edildi; ${adr.id} bu gereksinime dayanıyor ama kararı anmıyor (metin bayat olabilir)`);
      }
    }
  }
  return errors;
}

// ------------------------------------------------------------------ calistir

function markdownFiles() {
  const out = execFileSync('git', ['ls-files', '--cached', '--others', '--exclude-standard', '-z', 'docs'], { cwd: ROOT, encoding: 'utf8' });
  return out.split('\0').filter((p) => p.endsWith('.md') && !EXCLUDED.test(p) && existsSync(join(ROOT, p)));
}

// Satir sonlari LF'ye cevrilir; Windows kopyasinda CRLF ifadeleri bozmasin.
const read = (path) => readFileSync(path, 'utf8').replace(/\r\n/g, '\n');

let failures = 0;
const fail = (message) => { console.error(`✗ ${message}`); failures++; };

// --- 1. tarih kurali
const docs = markdownFiles().map((path) => ({ path, text: read(join(ROOT, path)) }));
const results = docs.map((d) => ({ ...d, ...checkDates(d.path, d.text) }));
const checked = results.filter((r) => r.checked);

// Once denetimin kendisi: kapsam bos olmamali ve eskitilmis bir baslik yakalanmali.
const probe = checked[0];
const staleProbe = probe && checkDates(probe.path, probe.text.replace(/^(\*\*Son güncelleme:\*\*\s*)\d{4}-\d{2}-\d{2}/m, '$11900-01-01'));
if (checked.length < MIN_DATED_DOCS) {
  fail(`tarih kuralı: yalnızca ${checked.length} belge denetlenebildi (en az ${MIN_DATED_DOCS} bekleniyor) — başlık veya geçmiş ifadesi bozulmuş olabilir`);
} else if (!staleProbe?.error) {
  fail(`tarih kuralı kendi sınamasını geçemedi — ${probe.path} başlığı 1900-01-01 yapıldığında yakalanmadı`);
} else {
  const stale = checked.filter((r) => r.error);
  if (stale.length) {
    fail(`tarih kuralı: ${stale.length} belgede "Son güncelleme" değişiklik geçmişinin gerisinde`);
    for (const r of stale) console.error(`    ${r.error}`);
  } else {
    console.log(`✓ Son güncelleme: ${checked.length} belgede başlık tarihi değişiklik geçmişinin son satırıyla tutarlı`);
  }
}

// --- 2. SYG bolum 1 sayimlari
const systemDir = join(ROOT, SYSTEM_DIR);
const modules = existsSync(systemDir)
  ? readdirSync(systemDir).map((f) => /^SYG-([A-Z]+)\.md$/.exec(f)?.[1]).filter(Boolean)
  : [];
if (!modules.length) console.log('Sistem gereksinimi belgesi yok; §1 sayımı denetlenmedi.');

for (const module of modules) {
  const systemDoc = read(join(systemDir, `SYG-${module}.md`));
  const stakeholderPath = join(ROOT, STAKEHOLDER_DIR, `PG-${module}.md`);
  const stakeholderDoc = existsSync(stakeholderPath) ? read(stakeholderPath) : '';

  // Once denetimin kendisi: bolum 4'ten ilk gereksinim silinirse toplam sayi farki yakalanmali.
  const broken = systemDoc.replace(new RegExp(`^\\| \\*\\*SYG-${module}-\\d{3}\\*\\* \\|.*\\n`, 'm'), '');
  const probeErrors = broken === systemDoc ? [] : checkSummary(module, broken, stakeholderDoc);
  if (!probeErrors.some((e) => e.includes('"Sistem gereksinimi"'))) {
    fail(`SYG-${module}: §1 denetimi kendi sınamasını geçemedi — silinen satır yakalanmadı`);
    continue;
  }

  const errors = checkSummary(module, systemDoc, stakeholderDoc);
  if (errors.length) {
    fail(`SYG-${module}: ${errors.length} sorun`);
    for (const e of errors) console.error(`    ${e}`);
  } else {
    console.log(`✓ SYG-${module}: §1 özet sayımları §4 ve §5 ile tutarlı`);
  }
}

// --- 3. ADR'ler ve kararlar (#389)
const ADR_DIR = 'docs/adr';
const adrFiles = existsSync(join(ROOT, ADR_DIR))
  ? readdirSync(join(ROOT, ADR_DIR)).filter((f) => /^ADR-\d{4}-.*\.md$/.test(f))
  : [];
if (!adrFiles.length) {
  fail('ADR kuralı: docs/adr altında ADR bulunamadı — yol veya ad kalıbı bozulmuş olabilir');
} else {
  const adrs = adrFiles.map((file) => ({ id: file.slice(0, 8), file, text: read(join(ROOT, ADR_DIR, file)) }));
  const readme = read(join(ROOT, ADR_DIR, 'README.md'));
  const register = read(join(ROOT, 'docs/karar-kayit-defteri.md'));

  // Once denetimin kendisi: dort kuralin her biri bozulmus bir ornegi yakalamali.
  const first = adrs.find((a) => headerLine(a.text, 'İlgili gereksinimler').includes('PG-')) ?? adrs[0];
  const firstPg = [...pgIds(headerLine(first.text, 'İlgili gereksinimler'))][0] ?? 'PG-XXXX-01';
  const fakeKr = (text) => `| KR-999 | 2099-01-01 | sınama | ${text} | - | - | - |`;
  const probes = [
    ['dizin', checkAdrs(readme.replace(`](${first.file})`, '](yok.md)'), adrs, register)],
    ['başlık', checkAdrs(readme, adrs.map((a) => (a === first
      ? { ...a, text: a.text + '\n| 2099-01-01 | 9.9 | sınama (`KR-998`) | - |' } : a)), register)],
    ['sınıflandırma', checkAdrs(readme, adrs, `${register}\n${fakeKr(`${first.id} §1 bu kararla revize edildi`)}`)],
    ['PG bağı', checkAdrs(readme, adrs, `${register}\n${fakeKr(`${firstPg} bu kararla revize edildi`)}`)],
  ];
  // Sayi degil ileti karsilastirilir: ornek ADR'de zaten bir bulgu varsa bozma onun yerine
  // yenisini koyar ve sayi degismez.
  const baseline = new Set(checkAdrs(readme, adrs, register));
  const blind = probes.filter(([, errs]) => !errs.some((e) => !baseline.has(e))).map(([name]) => name);
  if (blind.length) {
    fail(`ADR kuralı kendi sınamasını geçemedi — bozulan örnek yakalanmadı: ${blind.join(', ')}`);
  } else {
    const errors = checkAdrs(readme, adrs, register);
    if (errors.length) {
      fail(`ADR kuralı: ${errors.length} sorun (docs/adr/README.md kural 2-3, KR-099)`);
      for (const e of errors) console.error(`    ${e}`);
    } else {
      console.log(`✓ ADR: ${adrs.length} ADR dizinle, başlık karar listeleriyle ve karar defteriyle tutarlı`);
    }
  }
}

process.exit(failures ? 1 : 0);
