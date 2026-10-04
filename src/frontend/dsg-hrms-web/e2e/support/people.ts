/** Uctan uca yiginin sentetik kisileri (`docker/e2e/seed.sql`). Gercek veri DEGILDIR. */
export interface Person {
  nationalId: string;
  birthDigits: string; // GGAAYYYY
  birthIso: string;
  email: string;
  fullName: string;
}

export const people = {
  member: {
    nationalId: '20000000114',
    birthDigits: '12041985',
    birthIso: '1985-04-12',
    email: 'e2e.uyelik@duzen.com.tr',
    fullName: 'Ayse Uyelik',
  },
  admin: {
    nationalId: '20000000282',
    birthDigits: '15011980',
    birthIso: '1980-01-15',
    email: 'e2e.yonetici@duzen.com.tr',
    fullName: 'Yusuf Yonetici',
  },
  invitee: {
    nationalId: '20000000428',
    birthDigits: '03111992',
    birthIso: '1992-11-03',
    email: 'e2e.davet@duzen.com.tr',
    fullName: 'Deniz Davetli',
  },
  unprivileged: {
    nationalId: '20000000596',
    birthDigits: '21031988',
    birthIso: '1988-03-21',
    email: 'e2e.izinsiz@duzen.com.tr',
    fullName: 'Zeynep Izinsiz',
  },
} satisfies Record<string, Person>;

/** Parola kurallarina uyan, kisisel sozcuk icermeyen parolalar. */
export const passwords = {
  first: 'Mavi deniz 42 kez',
  second: 'Turuncu gunes 17 kez',
  third: 'Yesil orman 93 kez',
};
