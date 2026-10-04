import { expect, type APIRequestContext, type Page } from '@playwright/test';
import { codeIn, nextMessageText } from './mailpit';
import type { Person } from './people';

/**
 * Kimlik bilgileri adimini doldurur (uyelik ve parola sifirlama ayni ekrani kullanir).
 * Dogum tarihi alani gun, ay ve yil bolumlerinden olusur; rakamlar sirayla yazilir.
 */
export async function fillIdentity(page: Page, person: Person): Promise<void> {
  await page.getByLabel('T.C. Kimlik Numarası').fill(person.nationalId);
  await page.getByRole('group', { name: 'Doğum tarihi' }).getByRole('spinbutton').first().click();
  await page.keyboard.type(person.birthDigits);
  await page.getByLabel('Kurumsal e-posta adresi').fill(person.email);
  await page.getByRole('button', { name: 'Devam et' }).click();
}

/** Kanal adiminda e-postayi secer, kodu Mailpit'ten okuyup girer. */
export async function verifyByEmail(page: Page, person: Person): Promise<void> {
  await page.getByLabel('E-posta ile').check();
  const since = new Date(Date.now() - 1000);
  await page.getByRole('button', { name: 'Kodu gönder' }).click();
  const code = codeIn(await nextMessageText(person.email, since));
  await page.getByRole('textbox', { name: 'Doğrulama kodu' }).fill(code);
  await page.getByRole('button', { name: 'Doğrula' }).click();
}

/** Yeni parolayi iki kez yazar ve gonderir. */
export async function choosePassword(page: Page, password: string, submit: string): Promise<void> {
  await page.getByLabel('Parola', { exact: true }).fill(password);
  await page.getByLabel('Parola (tekrar)').fill(password);
  await page.getByRole('button', { name: submit }).click();
}

export async function signIn(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('Kurumsal e-posta adresi').fill(email);
  await page.getByLabel('Parola', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Giriş yap' }).click();
}

export async function signOut(page: Page, fullName: string): Promise<void> {
  await page.getByRole('button', { name: fullName }).click();
  await page.getByRole('menuitem', { name: 'Çıkış yap' }).click();
  await expect(page).toHaveURL(/\/login/);
}

/**
 * Hesabi API uzerinden acar (uyelik akisinin ekransiz hali). Ekranin kendisi ayri
 * senaryoda sinanir; burada yalnizca baska senaryolarin on kosulu kurulur.
 */
export async function registerViaApi(
  request: APIRequestContext,
  person: Person,
  password: string,
): Promise<void> {
  const base = '/api/v1/identity/registrations';
  const start = await request.post(base, {
    data: { nationalId: person.nationalId, birthDate: person.birthIso, email: person.email },
  });
  expect(start.ok()).toBeTruthy();
  const { registrationId } = (await start.json()) as { registrationId: string };

  const since = new Date(Date.now() - 1000);
  expect(
    (await request.post(`${base}/${registrationId}/code`, { data: { channel: 'email' } })).ok(),
  ).toBeTruthy();
  const code = codeIn(await nextMessageText(person.email, since));

  const verification = await request.post(`${base}/${registrationId}/verification`, {
    data: { code },
  });
  expect(((await verification.json()) as { result: string }).result).toBe('verified');

  const account = await request.post(`${base}/${registrationId}/account`, { data: { password } });
  expect(account.status()).toBe(201);
}
