import { createRequire } from 'node:module';
import { readFileSync } from 'node:fs';

// Degisiklik talebi onay kurallari (#210, #387). Testin sinadigi kod, iki is akisinin require ile
// yukledigi modulun KENDISIDIR; kopyasi degil.
const require = createRequire(import.meta.url);
const cr = require('./change-request-rules.cjs');

const register = readFileSync('docs/karar-kayit-defteri.md', 'utf8');
const field = (heading, value) => `### ${heading}\n\n${value}\n\n`;
const issue = ({ affected = '_No response_', role = '_No response_', approval = '_No response_' } = {}) =>
  field('Ne değişsin?', 'Bir şey') +
  field('Etkilenen kayıt', affected) +
  field('Onaylaması gereken rol', role) +
  field('Paydaş onayı', approval) +
  field('Karar (değerlendirme sonrası doldurulur)', '_No response_');

let failures = 0;
function check(name, actual, expected) {
  const ok = JSON.stringify(actual) === JSON.stringify(expected);
  if (!ok) failures++;
  console.log(`  ${ok ? 'OK    ' : 'YANLIS'} ${name}${ok ? '' : ` (beklenen ${JSON.stringify(expected)}, gelen ${JSON.stringify(actual)})`}`);
}

console.log('Tarihli ve rollu onay (#210, #387):');
const any = (texts) => cr.hasStakeholderApproval(texts);
check('Sablon alani dolu (GG.AA.YYYY)', any([issue({ approval: '10.10.2026 — İK birim yöneticisi — e-posta' })]), true);
check('Sablon alani dolu (YYYY-AA-GG, virgul)', any([issue({ approval: '2026-10-10, İK, toplantı' })]), true);
check('Sablon alani bos', any([issue()]), false);
check('Sablon alani tarihsiz', any([issue({ approval: 'İK onayladı' })]), false);
check('Onay sonradan yorumla', any([issue(), 'Paydaş onayı: 10.10.2026 — İK — e-posta (BK-27)']), true);
check('Yorum kalin yazimla', any(['**Paydaş onayı:** 13.10.2026 — İK birim yöneticisi — kabul oturumu']), true);
check('ASCII yazim', any(['Paydas onayi: 14.10.2026 - IK - e-posta']), true);
check('Onaysiz, baska yerde tarih var', any(['Talep 10.10.2026 tarihinde geldi.', 'İK değerlendirecek.']), false);
check('Onay kelimesi var, tarih yok', any(['Paydaş onayı bekleniyor']), false);
check('Bos govde ve yorum yok', any([null]), false);
check('NEGATIF: tarih var, rol yok', any(['Paydaş onayı: 10.10.2026']), false);
check('CRLF govde', any(['### Paydaş onayı\r\n\r\n10.10.2026 — İK — e-posta\r\n']), true);

console.log('\nRol ayristirma:');
const roleOf = (line) => cr.approvals([line]).map((a) => a.role);
check('Rol ikinci alandir', roleOf('Paydaş onayı: 24.10.2026 — İK — yazılı (Doğuş Uçanok bildirdi)'), ['İK']);
// #284 govdesi: kanaldaki alinti "IK" gecse de rol Bilgi Islem'dir.
check('NEGATIF: alintidaki IK rol sayilmaz',
  cr.missingApprovals(['Paydaş onayı: 24.10.2026 — Bilgi İşlem Birim Sorumlusu (Doğuş Uçanok) — yazılı ("İK\'nın da onayı var")'],
    [{ role: 'İK', reasons: ['x'] }]), ['İK']);
check('NEGATIF: "Bilgi İşlem" IK sayilmaz', cr.hasRole('Bilgi İşlem', 'İK'), false);
check('"İK (Doğuş Uçanok bildirdi)" IK sayilir', cr.hasRole('İK (Doğuş Uçanok bildirdi)', 'İK'), true);
check('"İnsan Kaynakları Müdürü" IK sayilir', cr.hasRole('İnsan Kaynakları Müdürü', 'İK'), true);

console.log('\nGereken rol (RACI §3.1):');
const roles = (args) => cr.requiredRoles({ register, ...args }).map((r) => r.role);
check('Baslikta PG kimligi -> IK', roles({ title: '[DEĞİŞİKLİK] PG-PERS-37: bildirim', body: issue() }), ['İK']);
check('Etkilenen alaninda PG satiri -> IK',
  roles({ title: '[DEĞİŞİKLİK] x', body: issue({ affected: '- PG-PERS-37 (alıcılar)' }) }), ['İK']);
check('PR PG belgesini degistiriyor -> IK', roles({
  title: '[DEĞİŞİKLİK] x', body: issue(),
  changedFiles: ['docs/33061/TEC.2-paydas-ihtiyac-ve-gereksinimleri/paydas-gereksinimleri/PG-PERS.md'],
}), ['İK']);
// #321: IK karari KR-126'yi degistiren talep.
check('Etkilenen IK karari (KR-126) -> IK',
  roles({ title: '[DEĞİŞİKLİK] esas sicil', body: issue({ affected: '- `KR-126` madde 4' }) }), ['İK']);
check('Bilgi Islem karari (KR-001) -> rol gerekmez',
  roles({ title: '[DEĞİŞİKLİK] x', body: issue({ affected: '- KR-001' }) }), []);
// #374: PG kimligi yalnizca matris satirinin baglaminda geciyor; talep yalnizca SYG degistiriyor.
check('NEGATIF: satir icindeki baglam PG kimligi sayilmaz', roles({
  title: '[DEĞİŞİKLİK] SYG-PERS-055/062: izin',
  body: issue({ affected: '- SYG-PERS-055: izin listesi\n- İzlenebilirlik matrisi §7 satır 36 (PG-PERS-36)' }),
}), []);
check('Yalnizca SYG -> rol gerekmez', roles({ title: '[DEĞİŞİKLİK] SYG-PERS-043: yakınlık', body: issue() }), []);
check('Sablonda IK secildi -> IK', roles({
  title: '[DEĞİŞİKLİK] x', body: issue({ role: 'İK — paydaş gereksinimi (PG) veya İK kararı değişiyor' }),
}), ['İK']);
check('Sablonda Ust Yonetim secildi', roles({
  title: '[DEĞİŞİKLİK] x', body: issue({ role: 'Üst Yönetim — kapsam veya takvim değişikliği (büyük etki)' }),
}), ['Üst Yönetim']);

console.log('\nEksik onay:');
const hr = [{ role: 'İK', reasons: ['PG'] }];
// #333, #355: PG degisikligi yalnizca Bilgi Islem onayiyla.
check('NEGATIF: PG degisikligi Bilgi Islem onayiyla',
  cr.missingApprovals([issue({ approval: '04.11.2026 — Bilgi İşlem Birim Sorumlusu (Doğuş Uçanok) — yazılı' })], hr), ['İK']);
check('PG degisikligi IK onayiyla',
  cr.missingApprovals([issue(), 'Paydaş onayı: 24.10.2026 — İK — yazılı (Doğuş Uçanok bildirdi)'], hr), []);
check('IK ve Ust Yonetim gerekli, yalnizca IK var',
  cr.missingApprovals(['Paydaş onayı: 24.10.2026 — İK — toplantı'], [...hr, { role: 'Üst Yönetim', reasons: ['x'] }]), ['Üst Yönetim']);
// #284: IK onayi birlestirmeden SONRA yazildi. Dal Koruma Denetimi yorumlari zamanina gore suzer;
// birlestirme anindaki metinlerle onay eksiktir.
const body284 = issue({ approval: '24.10.2026 — Bilgi İşlem Birim Sorumlusu (Doğuş Uçanok) — yazılı' });
const comments284 = [
  { at: '2026-10-24T11:34:57Z', body: 'Paydaş onayı: 24.10.2026 — İK — yazılı (Doğuş Uçanok bildirdi)' },
];
const mergedAt = new Date('2026-10-24T11:29:10Z');
const before = comments284.filter((c) => new Date(c.at) <= mergedAt).map((c) => c.body);
check('NEGATIF: #284 birlestirme anında IK onayi yok', cr.missingApprovals([body284, ...before], hr), ['İK']);
check('#284 bugun IK onayi var', cr.missingApprovals([body284, ...comments284.map((c) => c.body)], hr), []);

console.log('\nYardimcilar:');
check('Kapatilan issue numaralari', cr.closingIssueNumbers('Closes #285\nCloses #284\nRefs #10'), [285, 284]);
check('Karar defterinde IK kaynakli karar var (KR-126)', cr.hrDecisions(register).has('KR-126'), true);
check('NEGATIF: Bilgi Islem karari IK sayilmaz (KR-130)', cr.hrDecisions(register).has('KR-130'), false);

console.log(failures === 0 ? '\nTum ornekler beklendigi gibi.' : `\n${failures} ornek beklenenden farkli!`);
process.exit(failures ? 1 : 0);
