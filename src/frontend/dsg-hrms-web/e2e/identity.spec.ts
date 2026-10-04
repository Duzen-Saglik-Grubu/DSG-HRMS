import { expect, test } from '@playwright/test';
import { inviteLinkIn, nextMessageText } from './support/mailpit';
import {
  choosePassword,
  fillIdentity,
  registerViaApi,
  signIn,
  signOut,
  verifyByEmail,
} from './support/flows';
import { passwords, people } from './support/people';

/**
 * Kimlik akislari uctan uca (#146, ADR-0011 §1): gercek tarayici, gercek API, gercek
 * veritabani. E-postalar Mailpit'ten okunur. Veriler SENTETIKTIR (docker/e2e/seed.sql).
 *
 * Senaryolar sirayla calisir: uyeligin actigi hesap sonraki senaryolarda kullanilir.
 */
test.describe.configure({ mode: 'serial' });

const member = people.member;

test('1. uyelik: kimlik, e-posta kodu ve parolayla hesap acilir (SYG-KMLK-013)', async ({
  page,
}) => {
  await page.goto('/register');
  await fillIdentity(page, member);
  await verifyByEmail(page, member);
  await choosePassword(page, passwords.first, 'Hesabı oluştur');

  await expect(page.getByRole('heading', { name: 'Hesabınız oluşturuldu' })).toBeVisible();
});

test('2. giris ve cikis: oturum acilir, cikista giris ekranina donulur (SYG-KMLK-031, 043)', async ({
  page,
}) => {
  await signIn(page, member.email, passwords.first);
  await expect(page.getByRole('button', { name: member.fullName })).toBeVisible();

  await signOut(page, member.fullName);
  await expect(page.getByText('Çıkış yaptınız.')).toBeVisible();

  // Oturum sunucuda da kapandi: korumali sayfa giris ekranina yonlendirir.
  await page.goto('/account/password');
  await expect(page).toHaveURL(/\/login/);
});

test('3. hatali giris: hesabin varligini ele vermeyen ayni ileti (SYG-KMLK-032)', async ({
  page,
}) => {
  await signIn(page, member.email, 'Yanlis parola 1');
  const wrongPassword = await page.getByRole('alert').innerText();

  await signIn(page, 'yok.boyle@duzen.com.tr', passwords.first);
  const unknownUser = await page.getByRole('alert').innerText();

  expect(wrongPassword).toBe(unknownUser);
  await expect(page).toHaveURL(/\/login/);
});

test('4. oturum icinde parola degistirme (SYG-KMLK-048)', async ({ page }) => {
  await signIn(page, member.email, passwords.first);
  await page.getByRole('button', { name: member.fullName }).click();
  await page.getByRole('menuitem', { name: 'Parolamı değiştir' }).click();

  await page.getByLabel('Mevcut parola').fill(passwords.first);
  await page.getByLabel('Yeni parola', { exact: true }).fill(passwords.second);
  await page.getByLabel('Yeni parola (tekrar)').fill(passwords.second);
  await page.getByRole('button', { name: 'Parolayı değiştir' }).click();
  await expect(page.getByRole('status')).toContainText('Parolanız değiştirildi');

  await signOut(page, member.fullName);
  await signIn(page, member.email, passwords.second);
  await expect(page.getByRole('button', { name: member.fullName })).toBeVisible();
  await signOut(page, member.fullName);
});

test('5. parola sifirlama: dogrulanmis kimlikle yeni parola (SYG-KMLK-047)', async ({ page }) => {
  await page.goto('/login');
  await page.getByRole('link', { name: 'Parolamı unuttum' }).click();
  await fillIdentity(page, member);
  await verifyByEmail(page, member);
  await choosePassword(page, passwords.third, 'Parolayı değiştir');
  await expect(page.getByRole('heading', { name: 'Parolanız değiştirildi' })).toBeVisible();

  await signIn(page, member.email, passwords.third);
  await expect(page.getByRole('button', { name: member.fullName })).toBeVisible();
});

test('6. IK davet baglantisi: yonetici gonderir, kisi parolasini belirler (SYG-KMLK-051, #137)', async ({
  page,
  request,
}) => {
  await registerViaApi(request, people.admin, passwords.first);
  await signIn(page, people.admin.email, passwords.first);
  await page.getByRole('button', { name: people.admin.fullName }).click();
  await page.getByRole('menuitem', { name: 'Hesap işlemleri' }).click();

  await page.getByLabel('Sicil numarası, ad veya soyad').fill('Davetli');
  const row = page.getByRole('row', { name: new RegExp(people.invitee.fullName) });
  await row.getByRole('button', { name: 'Bağlantı gönder' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel(/Gerekçe/).fill('Uctan uca test');
  const since = new Date(Date.now() - 1000);
  await dialog.getByRole('button', { name: 'Bağlantı gönder' }).click();
  await expect(page.getByText(/bağlantı gönderildi/)).toBeVisible();
  await signOut(page, people.admin.fullName);

  // Baglanti e-postadaki haliyle, yeni bir sekmede acilir (#137: jeton kaybolmamali).
  const link = inviteLinkIn(await nextMessageText(people.invitee.email, since));
  const invitePage = await page.context().newPage();
  await invitePage.goto(link);
  await expect(invitePage.getByText(/Merhaba Deniz/)).toBeVisible();
  await expect(invitePage).toHaveURL(/\/invite$/); // jeton adresten kaldirildi

  await choosePassword(invitePage, passwords.second, 'Parolayı kaydet');
  await expect(invitePage.getByRole('status')).toContainText('Parolanız kaydedildi');

  await signIn(invitePage, people.invitee.email, passwords.second);
  await expect(invitePage.getByRole('button', { name: people.invitee.fullName })).toBeVisible();
});

test('7. yetki: izni olmayan kullanici hesap islemlerini goremez (SYG-KMLK-072)', async ({
  page,
  request,
}) => {
  await registerViaApi(request, people.unprivileged, passwords.first);
  await signIn(page, people.unprivileged.email, passwords.first);

  await page.getByRole('button', { name: people.unprivileged.fullName }).click();
  await expect(page.getByRole('menuitem', { name: 'Parolamı değiştir' })).toBeVisible();
  await expect(page.getByRole('menuitem', { name: 'Hesap işlemleri' })).toHaveCount(0);
  await page.keyboard.press('Escape');

  // Adres elle yazilsa da sunucu reddeder ve ekran bunu soyler.
  await page.goto('/accounts');
  await expect(page.getByText('Bu işlem için yetkiniz bulunmuyor.')).toBeVisible();
});
