// Kurumsal logonun web imajindaki kopyasi depodaki asil dosyayla ayni mi (REQ-KMLK-048, #90).
//
// Web imaji yalnizca src/frontend/dsg-hrms-web klasorunden derlenir; depo kokundeki
// assets/ klasorune erisemez. Bu yuzden logo public/brand/ altina kopyalanir. Bu denetim
// iki dosyanin ayrismasini engeller: logo degisirse ikisi birlikte guncellenmelidir.
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';

const pairs = [['assets/duzen_logo.png', 'src/frontend/dsg-hrms-web/public/brand/duzen_logo.png']];
const hash = (path) => createHash('sha256').update(readFileSync(path)).digest('hex');

let failed = false;
for (const [original, copy] of pairs) {
  if (hash(original) !== hash(copy)) {
    console.error(`::error::${copy} ile ${original} farkli. Logoyu degistirdiyseniz kopyayi da guncelleyin.`);
    failed = true;
  } else {
    console.log(`✓ ${copy} = ${original}`);
  }
}

process.exit(failed ? 1 : 0);
