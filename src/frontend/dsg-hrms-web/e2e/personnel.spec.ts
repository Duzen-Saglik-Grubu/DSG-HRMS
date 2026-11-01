import { expect, test } from '@playwright/test';
import { signIn, signOut } from './support/flows';
import { passwords, people } from './support/people';

/**
 * Personel listesi uctan uca (PG-PERS-13, 16, 18; ADR-0019 §1). Veriler SENTETIKTIR
 * (docker/e2e/seed.sql).
 *
 * Hesaplar `identity.spec.ts` senaryolarinda acilir (6: yonetici, 7: izinsiz kullanici); dosyalar
 * tek calisanla alfabetik sirada kosar, bu dosya ondan sonra gelir.
 */
test.describe.configure({ mode: 'serial' });

test('1. yonetici: liste kart izgarasiyla acilir, arar ve tabloya gecer (SYG-PERS-023, 030)', async ({
  page,
}) => {
  await signIn(page, people.admin.email, passwords.first);
  await page.getByRole('link', { name: 'Personel listesi', exact: true }).click();

  await expect(page.getByRole('article', { name: 'Deniz Davetli' })).toBeVisible();
  await expect(page.getByRole('article', { name: 'Yusuf Yonetici' })).toBeVisible();

  // Turkce karakterden bagimsiz, yazma durunca aranir.
  await page.getByLabel(/Ad, soyad, sicil/).fill('davetlı');
  await expect(page.getByRole('article')).toHaveCount(1);
  await expect(page.getByRole('article', { name: 'Deniz Davetli' })).toBeVisible();

  await page.getByRole('button', { name: 'Tablo görünümü' }).click();
  await expect(page.getByRole('row', { name: /Deniz/ })).toBeVisible();
  await expect(page).toHaveURL(/view=table/);
  await expect(page).toHaveURL(/q=davetl/);

  // Ayrilan personel yok: bos durum ne yapilabilecegini soyler (SYG-PERS-027).
  await page.getByRole('button', { name: 'Ayrılan' }).click();
  await expect(page.getByText('Bu ölçütlere uyan personel yok')).toBeVisible();
  await page.getByRole('button', { name: 'Temizle' }).click();
  await expect(page.getByRole('row', { name: /Deniz/ })).toBeVisible();

  // Kisi karti: tablodan acilir, geri donuste liste ayni arama ve gorunumle gelir (#311).
  await page.getByRole('row', { name: /Deniz/ }).getByRole('link', { name: 'Deniz' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Deniz Davetli' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Genel Bilgiler' })).toBeVisible();
  await page.getByRole('link', { name: 'Listeye dön' }).click();
  await expect(page).toHaveURL(/view=table/);
  await expect(page).toHaveURL(/q=davetl/);

  await signOut(page, people.admin.fullName);
});

test('2. rolu olmayan personel listeyi gormez; adresi yazsa da profiline gider (#311)', async ({
  page,
}) => {
  await signIn(page, people.unprivileged.email, passwords.first);
  await expect(page.getByRole('button', { name: people.unprivileged.fullName })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Personel listesi', exact: true })).toHaveCount(0);

  await page.goto('/personnel');
  await expect(page).toHaveURL(/\/profile$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Zeynep Izinsiz' })).toBeVisible();

  // Profilim herkese acik (SYG-PERS-044); TCKN kisinin kendisine de maskeli.
  await page.getByRole('link', { name: 'Ana Sayfa' }).click();
  await page.getByRole('link', { name: 'Profilim' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Zeynep Izinsiz' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Genel Bilgiler' })).toContainText('*');

  // Kullanici kartinda adin altinda e-posta; Profilim kullanici menusunden de acilir.
  await page.getByRole('link', { name: 'Ana Sayfa' }).click();
  const userCard = page.getByRole('button', { name: people.unprivileged.fullName });
  await expect(userCard).toContainText(people.unprivileged.email);
  await userCard.click();
  await page.getByRole('menuitem', { name: 'Profilim' }).click();
  await expect(page).toHaveURL(/\/profile$/);
});
