// Degisiklik talebi onay kurallari (#210, #387).
//
// Kurallar TEK yerde durur: "PR izlenebilirlik denetimi" (birlestirmeden once) ve "Dal Koruma
// Denetimi" (birlestirmeden hemen sonra, main'de) bu modulu require ile yukler;
// change-request-approval-test.mjs ayni modulu sinar.
//
// #387 dersi: #210'daki kural yalnizca TARIHLI bir onay satiri ariyordu, onaylayanin ROLUNE
// bakmiyordu. T1'de paydas gereksinimini (PG) degistiren dort talep, RACI'de TEC.2'nin
// onaylayani (A) IK oldugu halde Bilgi Islem onayiyla gecti (#284, #321, #333, #355).
// #284'te IK onayi birlestirmeden 6 dakika SONRA yazildi; denetim, Bilgi Islem'in govdedeki
// satiriyla gecmisti.

'use strict';

const DATE = /\b(\d{2}\.\d{2}\.\d{4}|\d{4}-\d{2}-\d{2})\b/;
const APPROVAL = /payda[şs]\s+onay/i;
// Onay satiri: "<tarih> — <onaylayan rol> — <kanal>". Ayirici uzun/orta tire, bosluklu kisa
// tire veya virgul olabilir.
const SEPARATOR = /\s*[—–]\s*|\s+-\s+|,\s*/;
// Roller. "i" bayragi kullanilmaz: "İ" harfinin buyuk/kucuk eslemesi dile baglidir.
const ROLES = {
  'İK': /(?:^|[^\p{L}])(?:İK|IK|İk|Ik)(?![\p{L}])|[İI]nsan\s+[Kk]aynaklar/u,
  'Üst Yönetim': /[ÜU]st\s+[Yy][öo]netim/u,
};
const PG_ID = /\bPG-[A-Z]+-\d+\b/g;
const KR_ID = /\bKR-\d{3}\b/g;
const PG_DOC = /^docs\/33061\/TEC\.2-[^/]+\/paydas-gereksinimleri\//;

// Rol alanindan once gelen her seyi (baslik, kalin yazim, tarih) atar; ilk alani dondurur.
// "24.10.2026 — İK — yazili (... İK'nin da onayi var)" icin rol "İK"dir; kanal veya alinti
// icinde gecen rol adi, rol sayilmaz.
function roleOf(value) {
  const match = value.match(DATE);
  if (!match) return '';
  const rest = value.slice(match.index + match[0].length).replace(/^[\s*:—–,.-]+/, '');
  return (rest.split(SEPARATOR)[0] || '').replace(/[*_`]/g, '').trim();
}

function hasRole(role, name) {
  return ROLES[name].test(role);
}

// texts: issue govdesi ve yorumlari. Her tarihli ve rollu onay satiri icin { index, date, role }.
// index, satirin hangi metinden geldigini gosterir (Dal Koruma Denetimi zamani boyle eslestirir).
function approvals(texts) {
  const found = [];
  texts.forEach((text, index) => {
    const lines = (text || '').replace(/\r\n/g, '\n').split('\n');
    for (let i = 0; i < lines.length; i++) {
      if (!APPROVAL.test(lines[i])) continue;
      // Sablonun urettigi baslik ("### Paydas onayi") ise deger bir sonraki dolu satirdadir;
      // bos alan "_No response_" olarak gelir.
      let value = lines[i];
      if (/^\s*#{1,6}\s/.test(lines[i])) {
        value = lines.slice(i + 1).find((l) => l.trim() !== '') || '';
      }
      if (/_No response_/.test(value) || !DATE.test(value)) continue;
      const role = roleOf(value);
      // Rolu yazilmamis onay, kimin onayladigini gostermez; onay sayilmaz (#387).
      if (role.length < 2) continue;
      found.push({ index, date: value.match(DATE)[0], role });
    }
  });
  return found;
}

// Issue govdesinde "### <baslik>" ile baslayan alanin metni (sonraki ### basligina kadar).
function issueSection(body, heading) {
  const lines = (body || '').replace(/\r\n/g, '\n').split('\n');
  const start = lines.findIndex((l) => l.trim() === '### ' + heading);
  if (start < 0) return '';
  const rest = lines.slice(start + 1);
  const end = rest.findIndex((l) => /^###\s/.test(l));
  return (end < 0 ? rest : rest.slice(0, end)).join('\n').replace(/_No response_/g, '').trim();
}

// Karar defterinde "Kaynak" sutununda IK gecen kararlar: bu kararlari degistirmek IK'nin onayini
// ister (#321: IK karari KR-126 (4), Bilgi Islem onayli KR-130 ile tersine cevrildi).
function hrDecisions(registerText) {
  const ids = new Set();
  for (const line of (registerText || '').replace(/\r\n/g, '\n').split('\n')) {
    if (!/^\| KR-\d{3} \|/.test(line)) continue;
    const cells = line.split(/(?<!\\)\|/).map((c) => c.trim());
    // ['', Kimlik, Tarih, Konu, Karar, Gerekce, Kaynak, Durum, '']
    if (cells.length >= 8 && hasRole(cells[6], 'İK')) ids.add(cells[1]);
  }
  return ids;
}

// Talebin onayini vermesi gereken roller ve gerekceleri: [{ role, reasons }]. Bos dizi, herhangi
// bir rolun tarihli onayinin yettigi anlamina gelir.
// RACI §3.1: TEC.2 (paydas gereksinimi) onaylayani IK; TEC.3 (sistem gereksinimi) Bilgi Islem.
// Bu yuzden yalnizca SYG, ADR veya parametre degistiren talep Bilgi Islem onayiyla gecer.
function requiredRoles({ title, body, changedFiles = [], register = '' }) {
  const hr = [];
  const affected = issueSection(body, 'Etkilenen kayıt');
  // PG kimligi basliktan veya "Etkilenen kayit" alaninda satirin ILK ogesi olarak alinir. Satir
  // icinde baglam olarak gecen kimlik ("matris satir 36 (PG-PERS-36)") etkilenen sayilmaz: #374
  // yalnizca SYG degistiriyordu, ama matris satirini anarken PG kimligini yaziyordu.
  const leading = affected.split('\n')
    .map((l) => l.replace(/^[\s>*_`-]+/, ''))
    .filter((l) => /^PG-[A-Z]+-\d+/.test(l)).join('\n');
  const pgIds = [...new Set(((title || '') + '\n' + leading).match(PG_ID) || [])];
  if (pgIds.length) hr.push('etkilenen paydaş gereksinimi: ' + pgIds.join(', '));
  // En guclu isaret: PG degisikligi PG belgesini ayni PR'da gunceller (TEC.2 YAKLASIM §4.1 adim 4).
  const pgDocs = changedFiles.filter((f) => PG_DOC.test(f));
  if (pgDocs.length) hr.push('PR paydaş gereksinimi belgesini değiştiriyor: ' + pgDocs.join(', '));
  const hrIds = hrDecisions(register);
  const krIds = [...new Set(((title || '') + '\n' + affected).match(KR_ID) || [])].filter((id) => hrIds.has(id));
  if (krIds.length) hr.push('etkilenen İK kararı: ' + krIds.join(', '));
  const chosen = issueSection(body, 'Onaylaması gereken rol');
  if (/^İK\b/.test(chosen)) hr.push('talepte onaylaması gereken rol İK seçilmiş');

  const result = [];
  if (hr.length) result.push({ role: 'İK', reasons: hr });
  if (/^Üst Yönetim\b/.test(chosen)) {
    result.push({ role: 'Üst Yönetim', reasons: ['talepte onaylaması gereken rol Üst Yönetim seçilmiş'] });
  }
  return result;
}

// Eksik onaylar: gereken her rol icin o rolun tarihli onay satiri aranir. Rol gerekmiyorsa
// rollu herhangi bir tarihli onay yeter. Donen dizi bossa onay tamamdir.
function missingApprovals(texts, required = []) {
  const found = approvals(texts);
  if (required.length === 0) return found.length ? [] : ['tarihli ve rolü yazılı bir onay'];
  return required.filter(({ role }) => !found.some((a) => hasRole(a.role, role))).map(({ role }) => role);
}

function hasStakeholderApproval(texts, required = []) {
  return missingApprovals(texts, required).length === 0;
}

// PR govdesinde kapatilan issue numaralari.
function closingIssueNumbers(prBody) {
  return [...(prBody || '').matchAll(
    /\b(?:clos(?:e[sd]?)|fix(?:e[sd]?)?|resolv(?:e[sd]?))\s+#(\d+)/gi)].map((m) => Number(m[1]));
}

module.exports = {
  approvals,
  missingApprovals,
  hasStakeholderApproval,
  requiredRoles,
  hrDecisions,
  issueSection,
  closingIssueNumbers,
  hasRole,
};
