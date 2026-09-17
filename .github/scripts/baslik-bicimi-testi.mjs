import { readFileSync } from 'node:fs';

// Regex, is akisi dosyasindan AYNEN okunur. Kopyalayip yeniden yazmak, sinanan
// ifadenin calisandan sessizce ayrismasina yol acardi.
const y = readFileSync('.github/workflows/pr-izlenebilirlik-denetimi.yml', 'utf8');
const satirlar = y.split('\n');
const turler = satirlar.find((s) => s.includes('const TURLER ='));
const basi = satirlar.findIndex((s) => s.includes('const BASLIK_BICIMI ='));
const regex = satirlar.slice(basi, basi + 2).join('\n');

const kod = (turler + '\n' + regex).replace(/^\s+/gm, '') + '\nreturn BASLIK_BICIMI;';
const BASLIK_BICIMI = new Function(kod)();

const ornekler = [
  ['feat(uat): TLS (HTTPS) devreye alındı', true],
  ['fix(frontend): izleme kimliği güvenli olmayan bağlamda da üretiliyor', true],
  ['ci(github): PR izlenebilirlik denetimi', true],
  ['docs(33061): A1 aşama kapanış değerlendirmesi', true],
  ['feat(api)!: kirici degisiklik', true],
  ['feat: kapsamsiz da gecerli', true],
  ['[GÖREV] UAT ortamında TLS kurulumu', false],
  ['[HATA] bir sey bozuk', false],
  ['TLS kurulumu', false],
  ['feat(uat) TLS devreye alindi', false],
  ['Feat(uat): buyuk harf', false],
  ['feat(uat):', false],
  ['yenilik(uat): gecersiz tur', false],
];

let hata = 0;
for (const [baslik, beklenen] of ornekler) {
  const sonuc = BASLIK_BICIMI.test(baslik);
  const uygun = sonuc === beklenen;
  if (!uygun) hata++;
  console.log(`  ${uygun ? 'OK    ' : 'YANLIS'} ${sonuc ? 'gecti' : 'dustu'}  ${JSON.stringify(baslik)}`);
}
console.log(hata === 0 ? '\nTum ornekler beklendigi gibi.' : `\n${hata} ornek beklenenden farkli!`);
process.exit(hata ? 1 : 0);
