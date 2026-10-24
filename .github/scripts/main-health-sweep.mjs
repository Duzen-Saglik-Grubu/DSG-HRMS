#!/usr/bin/env node
// main saglik taramasi (MAN.8, TEC.13, #208).
//
// 05.10.2026'da isler calistirici (runner) bulamadan iptal oldu; main dogrulanmadan
// kaldi ve bunu kayda gecirmesi gereken "main kirmizi denetimi" de ayni nedenle
// calismadi. Olay ancak 13.10'daki modul sonu denetiminde bulundu.
//
// Bu betik zamanlanmis olarak calisir ve iki seyi SONRADAN sinar:
//   1. main'in son commit'i, izlenen her is akisinda basariyla dogrulandi mi?
//      (Commit yeterince eskiyse; yeni commit'in calismalari surebilir.)
//   2. "main kirmizi denetimi"nin son calismalarindan biri basarisiz veya
//      tamamlanamamis mi? Denetimin kendisinin calismamasi da bir olaydir.
// Bulunan her sorun icin [HATA] issue'su acilir; ayni baslikta acik issue varsa
// yorum eklenir. Ayni sorun (anahtar) ikinci kez yazilmaz. "Dogrulanmadi" sorununda ayni
// is akisinin acik "main kirmizi" kaydi once aranir; varsa ona yazilir (#291).
//
// Kullanim (depo kokunde, gh CLI oturumu acik):
//   node .github/scripts/main-health-sweep.mjs [--dry-run]
// --dry-run: issue acmaz, yalnizca bulunanlari yazar.
// Gecmis bir olayi yeniden sinamak icin (yalnizca --dry-run ile):
//   --sha=<commit>  main'in son commit'i yerine bu commit sinanir
//   --until=<ISO>   kayit denetiminin penceresi bu anda biter (simdi yerine)

import { execFileSync } from 'node:child_process';
import { findOpenIssue, unverifiedTitle } from './health-sweep-issues.mjs';

const arg = (name) => process.argv.find((a) => a.startsWith(`--${name}=`))?.split('=').slice(1).join('=');
const DRY_RUN = process.argv.includes('--dry-run');
const REPLAY_SHA = arg('sha');
const UNTIL = arg('until') ? Date.parse(arg('until')) : Date.now();
if ((REPLAY_SHA || arg('until')) && !DRY_RUN) {
  console.error('--sha ve --until yalnizca --dry-run ile kullanilir.');
  process.exit(2);
}
const REPO = process.env.GITHUB_REPOSITORY
  ?? execFileSync('gh', ['repo', 'view', '--json', 'nameWithOwner', '-q', '.nameWithOwner'], { encoding: 'utf8' }).trim();

// main'e her gonderimde calisan ve main'in dogrulandigini gosteren is akislari.
const MONITORED = [
  { file: 'ci.yml', name: 'CI' },
  { file: 'api-contract-check.yml', name: 'API sozlesme denetimi' },
  { file: 'branch-protection-check.yml', name: 'Dal Koruma Denetimi' },
];
const RECORDER = { file: 'main-failure-check.yml', name: 'main kirmizi denetimi' };

// Yeni commit'in calismalari surebilir; bu sureden genc commit sinanmaz.
const GRACE_MINUTES = 120;
// Zamanlama 6 saatte bir; pencereler cakisir, ayni sorun anahtarla tekillestirilir.
const LOOKBACK_HOURS = 26;
const BAD = new Set(['failure', 'cancelled', 'timed_out', 'startup_failure']);

const api = (path) => JSON.parse(execFileSync('gh', ['api', path], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }));

/** @type {{ title: string, key: string, detail: string, workflowName?: string }[]} */
const problems = [];

// 1. main'in son commit'i dogrulandi mi?
const head = api(`repos/${REPO}/commits/${REPLAY_SHA ?? 'main'}`);
const sha = head.sha;
const ageMinutes = (UNTIL - Date.parse(head.commit.committer.date)) / 60000;
if (ageMinutes >= GRACE_MINUTES) {
  for (const wf of MONITORED) {
    const runs = api(`repos/${REPO}/actions/workflows/${wf.file}/runs?branch=main&head_sha=${sha}&per_page=20`).workflow_runs;
    if (runs.some((r) => r.conclusion === 'success')) continue;
    const seen = runs.map((r) => `${r.status}/${r.conclusion ?? '-'} ${r.html_url}`).join('; ') || 'hiç çalışma yok';
    problems.push({
      title: unverifiedTitle(wf.name),
      workflowName: wf.name,
      key: `${wf.file}@${sha}`,
      detail: `Son \`main\` commit'i \`${sha.substring(0, 7)}\` (${Math.round(ageMinutes / 60)} saat önce) **${wf.name}** iş akışında başarıyla doğrulanmadı. Çalışmalar: ${seen}`,
    });
  }
}

// 2. Kayit tutan denetimin kendisi calisti mi?
const since = new Date(UNTIL - LOOKBACK_HOURS * 3600000).toISOString();
const until = new Date(UNTIL).toISOString();
const recorderRuns = api(`repos/${REPO}/actions/workflows/${RECORDER.file}/runs?created=${encodeURIComponent(`${since}..${until}`)}&per_page=100`).workflow_runs;
for (const r of recorderRuns.filter((x) => BAD.has(x.conclusion))) {
  problems.push({
    title: `[HATA] Denetim çalışmadı: ${RECORDER.name}`,
    key: `${RECORDER.file}#${r.id}`,
    detail: `**${RECORDER.name}** ${r.created_at} tarihli çalışmada tamamlanamadı (\`${r.conclusion}\`): ${r.html_url}. Bu çalışmanın kayda geçirmesi gereken \`main\` sonucu kaydedilmemiş olabilir; ilgili \`main\` çalışmaları elle incelenir.`,
  });
}

if (problems.length === 0) {
  console.log(`Sorun yok. main @ ${sha.substring(0, 7)} (${Math.round(ageMinutes)} dk önce).`);
  process.exit(0);
}

for (const p of problems) console.log(`${p.title}\n  ${p.detail}\n  anahtar: ${p.key}`);
if (DRY_RUN) process.exit(0);

const marker = (key) => `<!-- tarama-anahtari: ${key} -->`;
const openIssues = api(`repos/${REPO}/issues?state=open&labels=tur:hata&per_page=100`);

for (const p of problems) {
  const body = `${p.detail}\n\n${marker(p.key)}`;
  const existing = findOpenIssue(p, openIssues);
  if (existing) {
    const comments = api(`repos/${REPO}/issues/${existing.number}/comments?per_page=100`);
    const already = [existing.body ?? '', ...comments.map((c) => c.body ?? '')].some((b) => b.includes(marker(p.key)));
    if (already) continue;
    execFileSync('gh', ['issue', 'comment', String(existing.number), '--repo', REPO, '--body', body]);
    console.log(`Yorum eklendi: #${existing.number}`);
    continue;
  }
  const full = [
    '## Belirti', '', body, '',
    '## Kural (CONTRIBUTING §1.1)', '',
    '- Doğrulanmamış `main` kırmızı sayılır; yeni özellik birleştirilmez.',
    '- Neden bulunur; gerekiyorsa iş akışı yeniden çalıştırılır ve sonucu bu issue\'ya yazılır.', '',
    '## Yapılması gerekenler', '',
    '- [ ] Neden bulundu',
    '- [ ] `main` doğrulandı (yeniden çalıştırma veya düzeltme PR\'ı)',
    '- [ ] Olay TEC.13 olay kayıtlarına yazıldı',
  ].join('\n');
  const url = execFileSync('gh', ['issue', 'create', '--repo', REPO, '--title', p.title, '--body', full,
    '--label', 'tur:hata', '--label', 'oncelik:yuksek', '--label', 'surec:TEC.13'], { encoding: 'utf8' }).trim();
  console.log(`Kayıt açıldı: ${url}`);
  // Ayni calismada ayni baslikli ikinci sorun yeni issue acmasin, yorum olarak eklensin.
  openIssues.push({ number: Number(url.split('/').pop()), title: p.title, body: full });
}
