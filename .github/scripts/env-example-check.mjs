// Ornek ortam dosyalari ve gecici yiginlar (MAN.5; #391, konfigurasyon denetimi D-07, D-15)
//
// Iki kural: (1) ornek ortam dosyalari compose ile birebir; (2) gecici yiginlar "down -v" ile
// kapatilir (ayrintisi asagida kendi bolumunde).
//
// Compose dosyasinin kullandigi her ${DEGISKEN}, eslesen ornek dosyada bir satir olarak
// bulunmalidir; ornekteki her degisken de compose'da kullanilmalidir.
//
// Neden otomatik? T1'de iki zorunlu sir (HRMS_APP_DB_PASSWORD, #244; PERSONAL_DATA_KEY_K1..K3,
// ADR-0020) compose'a eklendi ama .env.uat.example'a eklenmedi. Ornekten kurulan bir UAT
// yigini "HRMS_APP_DB_PASSWORD tanimli olmalidir" ile baslamaz, anahtar yoksa uygulama acilmazdi.
// Eksiklik ancak T1 gelistirme sonu denetiminde (12.11.2026, A-03) goruldu.
//
// Denetimin KENDISI once sinanir: ornekten bir satir silinir ve compose'a olmayan bir degisken
// eklenir; denetim ikisini de yakalamalidir.
//
// Kullanim: node .github/scripts/env-example-check.mjs [kok-dizin]

import { readFileSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = process.argv[2] ?? '.';
const PAIRS = [
  ['docker/compose.yml', 'docker/.env.example'],
  ['docker/compose.uat.yml', 'docker/.env.uat.example'],
];

const read = (path) => readFileSync(join(ROOT, path), 'utf8').replace(/\r\n/g, '\n');

// Yorum satirlari atlanir: aciklamadaki "${X}" ornegi kullanim sayilmaz.
function composeVariables(text) {
  const body = text.split('\n').filter((l) => !/^\s*#/.test(l)).join('\n');
  return new Set([...body.matchAll(/\$\{([A-Z][A-Z0-9_]*)/g)].map((m) => m[1]));
}

function exampleVariables(text) {
  return new Set([...text.matchAll(/^([A-Z][A-Z0-9_]*)=/gm)].map((m) => m[1]));
}

function compare(composeText, exampleText) {
  const used = composeVariables(composeText);
  const listed = exampleVariables(exampleText);
  return {
    missing: [...used].filter((v) => !listed.has(v)).sort(),
    unused: [...listed].filter((v) => !used.has(v)).sort(),
  };
}

let failures = 0;
for (const [composePath, examplePath] of PAIRS) {
  const composeText = read(composePath);
  const exampleText = read(examplePath);

  const first = [...exampleVariables(exampleText)][0];
  const probeMissing = compare(composeText, exampleText.replace(new RegExp(`^${first}=.*$`, 'm'), ''));
  const probeUnused = compare(composeText, `${exampleText}\nSINAMA_KULLANILMAYAN=\n`);
  if (!first || !probeMissing.missing.includes(first) || !probeUnused.unused.includes('SINAMA_KULLANILMAYAN')) {
    console.error(`✗ ${examplePath}: denetim kendi sınamasını geçemedi — silinen veya fazla satır yakalanmadı`);
    failures++;
    continue;
  }

  const { missing, unused } = compare(composeText, exampleText);
  if (missing.length || unused.length) {
    failures++;
    console.error(`✗ ${examplePath} ↔ ${composePath}`);
    if (missing.length) console.error(`    compose kullanıyor, örnekte yok: ${missing.join(', ')}`);
    if (unused.length) console.error(`    örnekte var, compose kullanmıyor: ${unused.join(', ')}`);
  } else {
    console.log(`✓ ${examplePath}: ${exampleVariables(exampleText).size} değişken, ${composePath} ile birebir`);
  }
}

// --- Gecici yiginlar birimleriyle kapatilir (#391, D-15) ---
// PostgreSQL imaji kendi VOLUME'unu tanimlar; "down" -v olmadan anonim birimi birakir. Yerelde
// 66, UAT'de bir sahipsiz birim bu yolla birikti. Kalici yiginlar (compose.yml yerel gelistirme,
// compose.uat.yml) kuralin disindadir: onlarda -v gercek veriyi silerdi.
const PERSISTENT = /compose\.(uat\.)?yml\b/;
function downWithoutVolumes(file, text) {
  return text.split('\n')
    .map((line, i) => ({ line, n: i + 1 }))
    .filter(({ line }) => !/^\s*#/.test(line) && /docker[ -]compose\b.*\bdown\b/.test(line))
    .filter(({ line }) => {
      const composeFile = (line.match(/-f\s+(\S+)/) || [])[1] || '';
      return !PERSISTENT.test(composeFile.split('/').pop()) && !/\s(-v|--volumes)\b/.test(line);
    })
    .map(({ n }) => `${file}:${n}`);
}

{
  const probe = downWithoutVolumes('ornek.sh', 'docker compose -f docker/compose.e2e.yml down --remove-orphans');
  const allowed = downWithoutVolumes('ornek.sh', 'docker compose -f compose.uat.yml down\ndocker compose -f docker/compose.e2e.yml down -v');
  if (probe.length !== 1 || allowed.length !== 0) {
    console.error('✗ Yığın kapatma kuralı kendi sınamasını geçemedi');
    failures++;
  } else {
    const { execFileSync } = await import('node:child_process');
    const files = execFileSync('git', ['ls-files', '-z', 'docker', 'tools', '.github/workflows'], { cwd: ROOT, encoding: 'utf8' })
      .split('\0').filter((f) => /\.(sh|ya?ml|ps1)$/.test(f));
    const found = files.flatMap((f) => downWithoutVolumes(f, read(f)));
    if (found.length) {
      failures++;
      console.error(`✗ Geçici yığın "-v" olmadan kapatılıyor (anonim birim kalır): ${found.join(', ')}`);
    } else {
      console.log(`✓ Geçici yığın kapatmaları birimleri de siliyor (${files.length} dosya tarandı)`);
    }
  }
}

process.exit(failures ? 1 : 0);
