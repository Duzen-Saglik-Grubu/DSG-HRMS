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
  await page.getByRole('link', { name: 'Personel' }).click();

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

  await signOut(page, people.admin.fullName);
});

test('2. rolu olmayan personel baglantiyi gormez, listede yalnizca kendini gorur (ADR-0019 §1)', async ({
  page,
}) => {
  await signIn(page, people.unprivileged.email, passwords.first);
  await expect(page.getByRole('button', { name: people.unprivileged.fullName })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Personel' })).toHaveCount(0);

  await page.goto('/personnel');
  await expect(page.getByRole('article', { name: 'Zeynep Izinsiz' })).toBeVisible();
  await expect(page.getByRole('article')).toHaveCount(1);
});
