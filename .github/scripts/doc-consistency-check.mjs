// Belge tutarliligi denetimi (MAN.6 / TEC.3; #214, denetim A-06 ve TEC.3-7)
//
// Iki kural denetlenir:
//   1. "Son guncelleme" basligi, degisiklik gecmisinin en yeni satirindan eski olamaz
//      (harita bolum 4.4, MAN.6 YAKLASIM bolum 2.1). docs/ altindaki her .md dosyasinda, hem
//      basinda "**Son guncelleme:** YYYY-AA-GG" hem sonunda "Degisiklik Gecmisi"
//      tablosu ("| YYYY-AA-GG | surum | ...") olan belgeler denetlenir.
//   2. SYG-*.md bolum 1 ozet sayimlari bolum 4 tablosundan (ve bolum 5 KPO tablosundan) yeniden
//      hesaplananla ayni. bolum 1'de taninmayan bir satir varsa denetim duser: sayilmayan
//      bir satir, denetleniyormus gibi gorunmemelidir; betik genisletilir.
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

process.exit(failures ? 1 : 0);
