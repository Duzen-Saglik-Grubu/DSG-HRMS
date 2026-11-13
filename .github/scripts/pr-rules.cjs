// PR govdesi ve inceleme kurallari (#216, #390).
//
// Kurallar TEK yerde durur: "PR izlenebilirlik denetimi" ve "Dal Koruma Denetimi" is akislari
// bu modulu require ile yukler; pr-discipline-test.mjs ayni modulu sinar. Onceden kurallar is
// akisinin icindeydi ve test onlari metinden kesip calistiriyordu.
//
// #390 dersi: bir kural, uymayan ornekle sinanmadikca calistigi varsayilamaz. Boyut kurali
// "boyut gerekcesi" ifadesini govdenin HERHANGI bir yerinde ariyordu; sablonun kendi DoD satiri bu
// ifadeyi tasidigi icin kural hicbir zaman dusmedi (#364'te 1.713 satir vardi). Bu yuzden her
// kural, sablonun bos haliyle de sinanir.

'use strict';

// --- Atlanamaz tamamlanma maddeleri (#216) ---
const MANDATORY_DOD = [
  { name: 'Yetki sizintisi testi', pattern: /yetki\s+s[ıi]z[ıi]nt[ıi]s[ıi]/i },
  { name: 'Maskeleme testi', pattern: /maskeleme\s+testi/i },
];

function missingMandatoryDod(prBody) {
  const lines = (prBody || '').split('\n');
  return MANDATORY_DOD.filter(({ pattern }) => !lines.some((line) => {
    if (!pattern.test(line)) return false;
    if (/^\s*-\s*\[[xX]\]/.test(line)) return true;
    // "- [ ] ~~Madde~~ — gerekce": ustu cizili ve gerekcesi var.
    const struck = line.match(/^\s*-\s*\[ \]\s*~~[^~]+~~\s*[—–-]+\s*(.*)$/);
    return Boolean(struck && struck[1].trim().length >= 5);
  })).map((d) => d.name);
}

// --- PR boyutu (#216, #390) ---
// Uretilen dosyalar boyut sayimina girmez: degisikligi insan yazmadi.
const GENERATED = [
  /(^|\/)src\/shared\/api\/generated\//,
  /^docs\/api\/openapi-v1\.json$/,
  /\/Migrations\/[^/]+\.Designer\.cs$/,
  /ModelSnapshot\.cs$/,
  /(^|\/)package-lock\.json$/,
];
const SIZE_LIMIT = 1500;

function reviewableLines(files) {
  return (files || [])
    .filter((f) => !GENERATED.some((p) => p.test(f.filename)))
    .reduce((sum, f) => sum + (f.additions || 0) + (f.deletions || 0), 0);
}

// Gerekce, kendi satirinda "Boyut gerekcesi:" ile baslayan ve en az 20 karakter aciklama
// tasiyan bir paragraftir. Liste maddesi (sablonun DoD satiri dahil) gerekce sayilmaz.
function hasSizeJustification(prBody) {
  return lf(prBody).split('\n').some((line) => {
    if (/^\s*[-*]\s/.test(line)) return false;
    const m = line.match(/^\s*(?:\*\*)?boyut\s+gerek[çc]esi\s*:\s*(?:\*\*)?\s*(.*)$/i);
    return Boolean(m && m[1].replace(/[*_`]/g, '').trim().length >= 20);
  });
}

function needsSizeJustification(files, prBody) {
  return reviewableLines(files) > SIZE_LIMIT && !hasSizeJustification(prBody);
}

// --- Sablon yapisi (#390) ---
// T1'de 17 PR'da tamamlanma tanimi 8-16 maddeye inmisti (sablonda 26); 73 PR'in 45'inde
// "Neden?" veya "Notlar" yoktu. Govde sablonla karsilastirilir: basliklar ayni sirada,
// sablondaki her DoD maddesi govdede (isaretli, isaretsiz ya da gerekceyle ustu cizili).
const REQUIRED_SECTIONS = ['Ne değişti?', 'Neden?', 'Nasıl doğrulandı?'];

const normalize = (text) => text
  .replace(/<!--[\s\S]*?-->/g, '')
  .replace(/[*~`]/g, '')
  .replace(/\s+/g, ' ')
  .trim()
  .toLocaleLowerCase('tr');

// GitHub'dan gelen govde CRLF tasiyabilir; satir kurallari LF ile calisir.
const lf = (text) => (text || '').replace(/\r\n/g, '\n');

const stripComments = (text) => lf(text).replace(/<!--[\s\S]*?-->/g, '');

function headingsOf(markdown) {
  return stripComments(markdown).split('\n')
    .filter((l) => /^## \S/.test(l))
    .map((l) => l.slice(3).trim());
}

// "- [ ] ..." maddeleri; girintili devam satirlari maddeye eklenir.
function checklistItems(markdown) {
  const items = [];
  let current = null;
  for (const line of stripComments(markdown).split('\n')) {
    const m = line.match(/^- \[([ xX])\] (.*)$/);
    if (m) {
      current = { checked: m[1] !== ' ', text: m[2] };
      items.push(current);
    } else if (current && /^\s{2,}\S/.test(line)) {
      current.text += ' ' + line.trim();
    } else {
      current = null;
    }
  }
  return items;
}

function sectionBody(markdown, heading) {
  const lines = stripComments(markdown).split('\n');
  const start = lines.findIndex((l) => l.trim() === `## ${heading}`);
  if (start < 0) return null;
  const rest = lines.slice(start + 1);
  const end = rest.findIndex((l) => /^## \S/.test(l) || l.trim() === '---');
  return (end < 0 ? rest : rest.slice(0, end)).join('\n').trim();
}

function templateProblems(prBody, rawTemplate) {
  const body = lf(prBody);
  const template = lf(rawTemplate);
  const problems = [];

  // 1. Basliklar: sablondakilerin hepsi, ayni sirada.
  const expected = headingsOf(template);
  const actual = headingsOf(body);
  const missing = expected.filter((h) => !actual.includes(h));
  if (missing.length > 0) {
    problems.push(`Sablon basliklari eksik: ${missing.map((h) => `"## ${h}"`).join(', ')}`);
  } else {
    const order = actual.filter((h) => expected.includes(h));
    if (order.join('|') !== expected.join('|')) {
      problems.push('Sablon basliklarinin sirasi degismis.');
    }
  }

  // 2. Zorunlu bolumler bos degil.
  for (const heading of REQUIRED_SECTIONS) {
    const text = sectionBody(body, heading);
    if (text !== null && text.replace(/[\s-]/g, '').length < 10) {
      problems.push(`"## ${heading}" bolumu bos.`);
    }
  }

  // 3. DoD: sablondaki her madde govdede, metni AYNEN (ardindan ":" veya " — " ile aciklama
  // gelebilir); maddeler silinmez ve kisaltilmaz (CONTRIBUTING §6). #351-#364'te metinler
  // kisaltilmisti ("guncellendi: PG-PERS-28").
  const templateItems = checklistItems(template).map((i) => normalize(i.text));
  const bodyItems = checklistItems(body);
  const bodyKeys = bodyItems.map((i) => normalize(i.text));
  const absent = templateItems.filter((key) => !bodyKeys.some((k) => k.startsWith(key)));
  if (absent.length > 0) {
    problems.push(
      `Tamamlanma tanimindan ${absent.length} madde silinmis (sablonda ${templateItems.length}): ` +
      absent.map((k) => `"${k.slice(0, 60)}…"`).join('; '));
  }

  // 4. Ustu cizili madde gerekcesiz olamaz.
  for (const item of bodyItems) {
    if (item.checked || !item.text.trim().startsWith('~~')) continue;
    const m = item.text.match(/^~~[\s\S]+~~\s*[—–-]+\s*(.*)$/);
    if (!m || m[1].trim().length < 5) {
      problems.push(`Ustu cizili madde gerekcesiz: "${normalize(item.text).slice(0, 50)}…"`);
    }
  }

  // 5. 33061 izlenebilirlik tablosu dolu.
  const traceRows = (sectionBody(body, '33061 izlenebilirliği') || '').split('\n')
    .filter((l) => /^\|/.test(l) && !/^\|\s*(Alan|---)/.test(l));
  for (const row of traceRows) {
    const cells = row.split('|').map((c) => c.trim());
    if (!cells[2]) problems.push(`33061 izlenebilirlik tablosunda bos alan: "${cells[1]}"`);
  }
  return problems;
}

// --- Buyuk PR'da inceleme notu (#390, KR-140) ---
// Boyut gerekcesi gereken PR'in onay yorumu neyin incelendigini "Incelenen:" satiriyla yazar.
function hasReviewNote(commentBody) {
  return /^\s*(?:\*\*)?[İI]ncelenen\s*:\s*(?:\*\*)?\s*\S.{9,}/mi.test(commentBody || '');
}

function needsReviewNote(files) {
  return reviewableLines(files) > SIZE_LIMIT;
}

module.exports = {
  SIZE_LIMIT,
  missingMandatoryDod,
  reviewableLines,
  hasSizeJustification,
  needsSizeJustification,
  templateProblems,
  hasReviewNote,
  needsReviewNote,
};
