#!/usr/bin/env node
// CI kanit ozeti (TEC.9, #136).
//
// GitHub Actions yapitlari 90 gun saklanir; sonra ham test ve kapsam kaniti kaybolur.
// Bu betik bir CI calismasinin yapitlarini indirir ve test sayilarini ve kapsami
// depoya alinacak KALICI bir Markdown ozetine cevirir. Modul kapanisinda ve kabul
// adayi etiketlendiginde calistirilir (TEC.9 YAKLASIM §6).
//
// Kullanim (depo kokunde, gh CLI oturumu acik):
//   node .github/scripts/ci-evidence-summary.mjs <calisma-no> <cikti.md> [baslik]
//
// Yapit yoksa (suresi dolmus veya o calismada yuklenmemis) ilgili bolum "yapit yok"
// olarak yazilir; eksik kanit sessizce atlanmaz.

import { execFileSync } from 'node:child_process';
import { existsSync, mkdtempSync, readdirSync, readFileSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { basename, join } from 'node:path';

const [runId, output, title = 'CI kanıt özeti'] = process.argv.slice(2);
if (!runId || !output) {
  console.error('Kullanim: node .github/scripts/ci-evidence-summary.mjs <calisma-no> <cikti.md> [baslik]');
  process.exit(2);
}

const gh = (...args) => execFileSync('gh', args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] });

const run = JSON.parse(gh('run', 'view', runId, '--json', 'headSha,createdAt,conclusion,url,displayTitle,headBranch'));
const work = mkdtempSync(join(tmpdir(), 'ci-kanit-'));

/** Yapiti indirir; yoksa null doner. */
function download(name) {
  const dir = join(work, name);
  try {
    gh('run', 'download', runId, '-n', name, '-D', dir);
    return dir;
  } catch {
    return null;
  }
}

function files(dir, predicate) {
  const out = [];
  for (const entry of readdirSync(dir)) {
    const path = join(dir, entry);
    if (statSync(path).isDirectory()) out.push(...files(path, predicate));
    else if (predicate(entry)) out.push(path);
  }
  return out;
}

const attr = (xml, element, name) => {
  const tag = xml.match(new RegExp(`<${element}\\b[^>]*>`));
  const value = tag?.[0].match(new RegExp(`\\b${name}="([^"]*)"`));
  return value ? value[1] : null;
};

const percent = (rate) => (rate === null ? '—' : `%${(Number(rate) * 100).toFixed(1).replace('.', ',')}`);

const lines = [
  `# ${title}`,
  '',
  '| | |',
  '|---|---|',
  `| Çalışma | [${runId}](${run.url}) — ${run.conclusion} |`,
  `| Commit | \`${run.headSha.substring(0, 7)}\` (${run.headBranch}) — ${run.displayTitle} |`,
  `| Tarih | ${run.createdAt} |`,
  `| Üretildi | ${new Date().toISOString()} — \`.github/scripts/ci-evidence-summary.mjs\` |`,
  '',
  '> Ham yapıtlar GitHub Actions\'ta 90 gün saklanır. Bu özet kalıcı kanıttır (TEC.9, #136).',
  '',
];

// --- Backend: test-ve-kapsam
const backend = download('test-ve-kapsam');
lines.push('## Backend', '');
if (!backend) {
  lines.push('**Yapıt yok:** `test-ve-kapsam` bu çalışmada bulunamadı (süresi dolmuş veya yüklenmemiş).', '');
} else {
  lines.push('| Test projesi | Toplam | Geçti | Başarısız | Çalışmadı |', '|---|---:|---:|---:|---:|');
  let total = 0;
  let passed = 0;
  let failed = 0;
  for (const trx of files(backend, (n) => n.endsWith('.trx')).sort()) {
    const xml = readFileSync(trx, 'utf8');
    const storage = xml.match(/storage="([^"]*)"/)?.[1] ?? trx;
    const project = basename(storage).replace(/\.dll$/i, '');
    const t = Number(attr(xml, 'Counters', 'total'));
    const p = Number(attr(xml, 'Counters', 'passed'));
    const f = Number(attr(xml, 'Counters', 'failed')) + Number(attr(xml, 'Counters', 'error'));
    const notRun = t - Number(attr(xml, 'Counters', 'executed'));
    total += t;
    passed += p;
    failed += f;
    lines.push(`| ${project} | ${t} | ${p} | ${f} | ${notRun} |`);
  }
  lines.push(`| **Toplam** | **${total}** | **${passed}** | **${failed}** | |`, '');

  const report = join(backend, 'coverage-report', 'Cobertura.xml');
  if (existsSync(report)) {
    const xml = readFileSync(report, 'utf8');
    lines.push(
      `**Kapsam (birleşik):** satır ${percent(attr(xml, 'coverage', 'line-rate'))} (${attr(xml, 'coverage', 'lines-covered')}/${attr(xml, 'coverage', 'lines-valid')}), dal ${percent(attr(xml, 'coverage', 'branch-rate'))}`,
      '',
      '| Derleme | Satır | Dal |',
      '|---|---:|---:|',
    );
    for (const m of xml.matchAll(/<package name="([^"]*)" line-rate="([^"]*)" branch-rate="([^"]*)"/g)) {
      lines.push(`| ${m[1]} | ${percent(m[2])} | ${percent(m[3])} |`);
    }
    lines.push('');
  } else {
    lines.push('**Kapsam raporu yok** (`coverage-report/Cobertura.xml`).', '');
  }
}

// --- On yuz: on-yuz-test-ve-kapsam
const frontend = download('on-yuz-test-ve-kapsam');
lines.push('## Ön yüz', '');
if (!frontend) {
  lines.push('**Yapıt yok:** `on-yuz-test-ve-kapsam` bu çalışmada bulunamadı. Yapıt CI\'a 04.10.2026\'da eklendi (#136); daha eski çalışmalarda yoktur.', '');
} else {
  const junit = files(frontend, (n) => n === 'junit.xml')[0];
  if (junit) {
    const xml = readFileSync(junit, 'utf8');
    const t = Number(attr(xml, 'testsuites', 'tests'));
    const f = Number(attr(xml, 'testsuites', 'failures')) + Number(attr(xml, 'testsuites', 'errors') ?? 0);
    const s = Number(attr(xml, 'testsuites', 'skipped') ?? 0);
    lines.push(`**Testler:** ${t} toplam, ${t - f - s} geçti, ${f} başarısız, ${s} atlandı`, '');
  } else {
    lines.push('**Test sonucu yok** (`junit.xml`).', '');
  }
  const cobertura = files(frontend, (n) => n === 'cobertura-coverage.xml')[0];
  if (cobertura) {
    const xml = readFileSync(cobertura, 'utf8');
    lines.push(`**Kapsam:** satır ${percent(attr(xml, 'coverage', 'line-rate'))}, dal ${percent(attr(xml, 'coverage', 'branch-rate'))}`, '');
  } else {
    lines.push('**Kapsam raporu yok** (`cobertura-coverage.xml`).', '');
  }
}

writeFileSync(output, `${lines.join('\n').trimEnd()}\n`);
rmSync(work, { recursive: true, force: true });
console.log(`Yazildi: ${output}`);
