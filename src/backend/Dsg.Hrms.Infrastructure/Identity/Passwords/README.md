# Yaygın parola listesi (SYG-KMLK-045)

`common-passwords.txt.gz`: üyelikte ve parola değişikliğinde reddedilen yaygın
parolalar. Uygulamaya gömülüdür; denetim çevrimdışıdır ve dış servise sorgu yapılmaz
(ADR-0006 §6).

## Kaynak ve lisans

| Dosya | Kaynak |
|---|---|
| `100k-most-used-passwords-NCSC.txt` | SecLists, `Passwords/Common-Credentials/` |
| `xato-net-10-million-passwords-100000.txt` | SecLists, `Passwords/Common-Credentials/` |
| `Pwdb_top-100000.txt` | SecLists, `Passwords/Common-Credentials/` |

SecLists — <https://github.com/danielmiessler/SecLists> — **MIT lisansı**,
Copyright (c) 2018 Daniel Miessler. Lisans metni:
<https://github.com/danielmiessler/SecLists/blob/master/LICENSE>.

## Üretim

İndirme tarihi: 27.09.2026. Üç liste birleştirildi. Her kayıt NFKC biçimine getirilip
küçük harfe çevrildi. 6 karakterden kısa kayıtlar çıkarıldı, çünkü en az parola
uzunluğu 6'dır (`PRM-KML-05`, en küçük değer 6). Tekrarlar ayıklandı ve sonuç sıralandı.

| | |
|---|---|
| Okunan satır | 299.843 |
| **Tekil kayıt** | **143.672** (SYG-KMLK-045: en az 100.000) |
| SHA-256 (`.gz`) | `7d100575284b90b352a6b3f58103bc600b145f1ac7c4e8bbd07bc8c019cf8110` |

```js
// node, üç dosyanın bulunduğu klasörde
const fs = require('fs'); const set = new Set();
for (const f of ['100k-most-used-passwords-NCSC.txt', 'xato-net-10-million-passwords-100000.txt', 'Pwdb_top-100000.txt'])
  for (let l of fs.readFileSync(f, 'utf8').split(/\r?\n/)) {
    l = l.normalize('NFKC').toLowerCase();
    if ([...l].length >= 6 && [...l].length <= 128 && !/[\x00-\x1f]/.test(l)) set.add(l);
  }
fs.writeFileSync('common-passwords.txt', [...set].sort().join('\n') + '\n');
// gzip -9 common-passwords.txt
```

Kuruma ve kişiye özgü sözcükler bu dosyada değil, koddadır: `PasswordPolicy.OrganizationWords`
ve kişinin adı, soyadı, e-posta adresinin yerel kısmı.
