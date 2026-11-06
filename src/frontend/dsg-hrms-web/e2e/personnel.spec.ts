import { expect, test, type Download, type Locator } from '@playwright/test';
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

  // Fotograf (SYG-PERS-040): tohumda yalnizca Deniz'in sentetik fotografi var; digerlerinde bas harfler.
  const photo = page
    .getByRole('article', { name: 'Deniz Davetli' })
    .getByRole('img', { name: 'Deniz Davetli fotoğrafı' });
  await expectPhotoLoaded(photo);
  await expect(
    page
      .getByRole('article', { name: 'Yusuf Yonetici' })
      .getByRole('img', { name: 'Yusuf Yonetici fotoğrafı' }),
  ).toHaveText('YY');

  // Ust cubuktaki arama liste acikken de suzer ve suzgec geri alinmaz (#322).
  await page.getByRole('search').getByRole('textbox').fill('davetli');
  await page.getByRole('search').getByRole('textbox').press('Enter');
  await expect(page.getByLabel(/Ad, soyad, sicil/)).toHaveValue('davetli');
  await page.waitForTimeout(600);
  await expect(page.getByRole('article')).toHaveCount(1);

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
  await expectPhotoLoaded(page.getByRole('img', { name: 'Deniz Davetli fotoğrafı' }));
  await page.getByRole('link', { name: 'Listeye dön' }).click();
  await expect(page).toHaveURL(/view=table/);
  await expect(page).toHaveURL(/q=davetl/);

  // Karta gidip donunce fotograf kaybolmaz (#315).
  await page.getByRole('button', { name: 'Kart görünümü' }).click();
  await expectPhotoLoaded(
    page
      .getByRole('article', { name: 'Deniz Davetli' })
      .getByRole('img', { name: 'Deniz Davetli fotoğrafı' }),
  );

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

test('3. IK-ya bildir: personel kendi kartindan bildirir, IK talebi kapatir (SYG-PERS-045, 048)', async ({
  page,
}) => {
  await signIn(page, people.unprivileged.email, passwords.first);
  await page.getByRole('link', { name: 'Profilim' }).click();
  await page.getByRole('button', { name: "İK'ya bildir" }).click();

  const dialog = page.getByRole('dialog', { name: "İK'ya bildir" });
  await dialog.getByRole('combobox', { name: /Düzeltilecek bilgi/ }).click();
  await page.getByRole('option', { name: 'Adres' }).click();
  await dialog.getByRole('textbox', { name: /Doğru bilgi/ }).fill('Ornek Mah. 7. Sok. No: 3');
  await dialog.getByRole('button', { name: 'Gönder' }).click();
  await expect(page.getByText("Talebiniz İK'ya iletildi.", { exact: false })).toBeVisible();

  // Ayni alan icin ikinci talep acilamaz (AN-08).
  await page.getByRole('button', { name: "İK'ya bildir" }).click();
  await dialog.getByRole('combobox', { name: /Düzeltilecek bilgi/ }).click();
  await expect(page.getByRole('option', { name: /Adres/ })).toHaveAttribute(
    'aria-disabled',
    'true',
  );
  await page.keyboard.press('Escape');
  await dialog.getByRole('button', { name: 'Vazgeç' }).click();
  await signOut(page, people.unprivileged.fullName);

  // Sistem Yoneticisi talepleri gorur ve kapatir; kapatinca onerilen deger silinir.
  await signIn(page, people.admin.email, passwords.first);
  await page.getByRole('link', { name: 'Düzeltme talepleri' }).click();
  const row = page.getByRole('row', { name: /Zeynep Izinsiz/ });
  await expect(row).toContainText('Ornek Mah. 7. Sok. No: 3');
  await row.getByRole('button', { name: 'Tamamlandı olarak kapat' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Tamamlandı olarak kapat' }).click();
  await expect(page.getByText('Zeynep Izinsiz için talep kapatıldı.')).toBeVisible();

  await page.getByRole('button', { name: 'Tamamlandı', exact: true }).click();
  await expect(page.getByRole('row', { name: /Zeynep Izinsiz/ })).toContainText(
    'Talep kapatıldığı için silindi',
  );
  await signOut(page, people.admin.fullName);
});

test('4. disa aktarma: yonetici suzgece uyan listeyi Excel ve PDF olarak indirir (SYG-PERS-032)', async ({
  page,
}) => {
  await signIn(page, people.admin.email, passwords.first);
  await page.getByRole('link', { name: 'Personel listesi', exact: true }).click();
  await expect(page.getByRole('article', { name: 'Deniz Davetli' })).toBeVisible();

  await page.getByRole('button', { name: 'Dışa aktar' }).click();
  const dialog = page.getByRole('dialog', { name: 'Personel listesini dışa aktar' });
  await expect(dialog.getByText(/Süzgece uyan \d+ kişi aktarılacak\./)).toBeVisible();
  // Fotograf yalnizca PDF'te secilebilir.
  await expect(dialog.getByRole('checkbox', { name: /Fotoğraf/ })).toBeDisabled();
  await dialog.getByRole('checkbox', { name: 'TCKN' }).check();

  const excel = page.waitForEvent('download');
  await dialog.getByRole('button', { name: 'İndir' }).click();
  const excelFile = await excel;
  expect(excelFile.suggestedFilename()).toMatch(/^personel-listesi-\d{8}-\d{4}\.xlsx$/);
  const xlsx = await readDownload(excelFile);
  expect(xlsx.subarray(0, 2).toString('latin1')).toBe('PK'); // xlsx bir zip arsividir
  await expect(dialog).toBeHidden();

  // PDF: secim hatirlanir; fotograf eklenebilir.
  await page.getByRole('button', { name: 'Dışa aktar' }).click();
  await expect(dialog.getByRole('checkbox', { name: 'TCKN' })).toBeChecked();
  await dialog.getByRole('button', { name: 'PDF' }).click();
  await dialog.getByRole('checkbox', { name: /Fotoğraf/ }).check();
  const pdf = page.waitForEvent('download');
  await dialog.getByRole('button', { name: 'İndir' }).click();
  const pdfFile = await pdf;
  expect(pdfFile.suggestedFilename()).toMatch(/\.pdf$/);
  expect((await readDownload(pdfFile)).subarray(0, 4).toString('latin1')).toBe('%PDF');
  await signOut(page, people.admin.fullName);
});

test('5. veri kalitesi: yonetici uyarilari turle suzer ve raporu Excel olarak indirir (SYG-PERS-070, 071)', async ({
  page,
}) => {
  await signIn(page, people.admin.email, passwords.first);
  await page.getByRole('link', { name: 'Veri kalitesi', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Veri kalitesi' })).toBeVisible();

  // Tohumdaki istihdamlarin unvani yok: tur suzgeci sayisiyla gorunur.
  const titleFilter = page.getByRole('button', { name: /^Ünvanı boş · [1-9]\d*$/ });
  await titleFilter.click();
  await expect(titleFilter).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByRole('note')).toContainText("Ünvanı LOGO'da girin.");
  await expect(page.getByRole('row').nth(1)).toContainText('Ünvanı boş');

  await page.getByRole('tab', { name: /^Üye olamayacak kişiler/ }).click();
  await expect(page.getByRole('tabpanel')).toBeVisible();

  const excel = page.waitForEvent('download');
  await page.getByRole('button', { name: "Excel'e aktar" }).click();
  const file = await excel;
  expect(file.suggestedFilename()).toMatch(/^veri-kalitesi-\d{8}-\d{4}\.xlsx$/);
  expect((await readDownload(file)).subarray(0, 2).toString('latin1')).toBe('PK');
  await signOut(page, people.admin.fullName);
});

test('6. senkronizasyon: yonetici calismalari sonuclariyla gorur (SYG-PERS-008, 009)', async ({
  page,
}) => {
  await signIn(page, people.admin.email, passwords.first);
  await page.getByRole('link', { name: 'Senkronizasyon', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Senkronizasyon' })).toBeVisible();

  // Bu ortamda LOGO tanimli degil: onceki calismalar gorunur, elle baslatma pasiftir.
  await expect(page.getByText(/bu ortamda tanımlı değil/)).toBeVisible();
  await expect(page.getByRole('button', { name: 'Şimdi senkronize et' })).toBeDisabled();

  const rows = page.getByRole('row');
  await expect(rows.nth(1)).toContainText('Başarısız');
  await expect(rows.nth(1)).toContainText("LOGO'ya bağlanılamadı");
  await expect(rows.nth(1)).toContainText('Elle');
  await expect(rows.nth(2)).toContainText('Başarılı');
  await expect(rows.nth(2)).toContainText('2 kişi · 2 sicil');
  await signOut(page, people.admin.fullName);
});

test('7. acil durum kisisi: yonetici girer, personel kendi profilinde gorur ama degistiremez (SYG-PERS-043)', async ({
  page,
}) => {
  await signIn(page, people.admin.email, passwords.first);
  await page.getByRole('link', { name: 'Personel listesi', exact: true }).click();
  await page.getByLabel(/Ad, soyad, sicil/).fill('izinsiz');
  await page.getByRole('button', { name: 'Tablo görünümü' }).click();
  await page
    .getByRole('row', { name: /Zeynep/ })
    .getByRole('link', { name: 'Zeynep' })
    .click();

  const section = page.getByRole('region', { name: 'Acil Durum Kişileri' });
  await expect(section).toContainText('Kayıtlı değil');
  await section.getByRole('button', { name: 'Acil durum kişisi ekle' }).click();

  const dialog = page.getByRole('dialog', { name: 'Acil durum kişisi ekle' });
  // Yakinlik referans verideki ilk tanimlardan secilir (KR-135).
  await dialog.getByRole('combobox', { name: /Yakınlığı/ }).click();
  await page.getByRole('option', { name: 'Annesi' }).click();
  await dialog.getByRole('textbox', { name: /^Adı/ }).fill('Fatma');
  await dialog.getByRole('textbox', { name: /Soyadı/ }).fill('Izinsiz');
  // Gecersiz telefon alanin altinda reddedilir (SYG-KMLK-009).
  await dialog.getByRole('textbox', { name: /Cep telefonu/ }).fill('212 111 22 33');
  await dialog.getByRole('button', { name: 'Kaydet' }).click();
  await expect(dialog.getByText(/5 ile başlayan 10 haneli/)).toBeVisible();
  await dialog.getByRole('textbox', { name: /Cep telefonu/ }).fill('0532 111 22 33');
  await dialog.getByRole('button', { name: 'Kaydet' }).click();
  await expect(section).toContainText('Fatma Izinsiz');
  await expect(section).toContainText('Annesi · 532 111 22 33');
  await signOut(page, people.admin.fullName);

  // Kisinin kendisi gorur; ekleme, duzenleme ve silme dugmesi yoktur (PG-PERS-44).
  await signIn(page, people.unprivileged.email, passwords.first);
  await page.getByRole('link', { name: 'Profilim' }).click();
  const own = page.getByRole('region', { name: 'Acil Durum Kişileri' });
  await expect(own).toContainText('Fatma Izinsiz');
  await expect(own.getByRole('button')).toHaveCount(0);
  await signOut(page, people.unprivileged.fullName);
});

test('8. referans veri: yonetici yakinlik ekler, pasife alir ve yeniden aktif eder; acil durum formu yeni listeyi kullanir (SYG-PERS-072)', async ({
  page,
}) => {
  await signIn(page, people.admin.email, passwords.first);
  await page.getByRole('link', { name: 'Referans veri', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Referans veri' })).toBeVisible();
  await page.getByRole('tab', { name: 'Yakınlık' }).click();
  await expect(page.getByRole('row', { name: /Y001 Eşi/ })).toBeVisible();

  // Yeni tanim: kod bos birakilir, sunucu siradakini verir (KR-135).
  await page.getByRole('button', { name: 'Yeni yakınlık' }).click();
  const dialog = page.getByRole('dialog', { name: 'Yeni yakınlık tanımı' });
  await dialog.getByRole('textbox', { name: /^Ad/ }).fill('Teyzesi');
  await dialog.getByRole('button', { name: 'Kaydet' }).click();
  await expect(page.getByText('“Teyzesi” eklendi (kod Y005).')).toBeVisible();

  // Pasife alinan tanim listede kalir ama yeni secimde gorunmez.
  await page.getByRole('button', { name: 'Kardeşi tanımını pasife al' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Pasife al' }).click();
  await expect(page.getByRole('row', { name: /Kardeşi/ })).toContainText('Pasif');

  // Kan grubu sistem tanimidir; sekmede degistirme dugmesi yoktur (KR-129).
  await page.getByRole('tab', { name: 'Kan grubu' }).click();
  await expect(page.getByText(/ekrandan değiştirilemez/)).toBeVisible();
  await expect(page.getByRole('tabpanel').getByRole('button')).toHaveCount(0);

  await page.getByRole('link', { name: 'Personel listesi', exact: true }).click();
  await page.getByLabel(/Ad, soyad, sicil/).fill('izinsiz');
  await page.getByRole('button', { name: 'Tablo görünümü' }).click();
  await page
    .getByRole('row', { name: /Zeynep/ })
    .getByRole('link', { name: 'Zeynep' })
    .click();
  await page
    .getByRole('region', { name: 'Acil Durum Kişileri' })
    .getByRole('button', { name: 'Acil durum kişisi ekle' })
    .click();
  await page
    .getByRole('dialog', { name: 'Acil durum kişisi ekle' })
    .getByRole('combobox', { name: /Yakınlığı/ })
    .click();
  await expect(page.getByRole('option', { name: 'Teyzesi' })).toBeVisible();
  await expect(page.getByRole('option', { name: 'Kardeşi' })).toHaveCount(0);
  await page.keyboard.press('Escape');
  await page.keyboard.press('Escape');

  // Yanlislikla pasife alinan tanim yeniden aktif edilir (KR-136).
  await page.getByRole('link', { name: 'Referans veri', exact: true }).click();
  await page.getByRole('tab', { name: 'Yakınlık' }).click();
  await page.getByRole('button', { name: 'Kardeşi tanımını yeniden aktif et' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Yeniden aktif et' }).click();
  await expect(page.getByText('“Kardeşi” yeniden aktif edildi.')).toBeVisible();
  await expect(page.getByRole('row', { name: /Kardeşi/ })).toContainText('Aktif');
  await signOut(page, people.admin.fullName);
});

async function readDownload(download: Download): Promise<Buffer> {
  const stream = await download.createReadStream();
  const chunks: Buffer[] = [];
  for await (const chunk of stream) {
    chunks.push(chunk as Buffer);
  }
  return Buffer.concat(chunks);
}

/** Fotograf goruntusu gercekten yuklendi: bas harflere dusmedi. */
async function expectPhotoLoaded(photo: Locator) {
  await expect(photo).toHaveAttribute('src', /^data:image\/jpeg;base64,/);
  await expect(photo).toHaveJSProperty('complete', true);
  expect(await photo.evaluate((img: HTMLImageElement) => img.naturalWidth)).toBe(160);
}
