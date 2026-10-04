-- Uctan uca test yigininin SENTETIK verisi (#146, ADR-0011 §5).
--
-- Gercek personel verisi KULLANILMAZ. TCKN'ler algoritmaya uygun ama uydurmadir
-- (2000000xxxx). Adresler var olmayan e2e.* adresleridir ve iletiler yalnizca Mailpit'e
-- gider; hicbir ileti disari cikmaz.
--
-- Hesaplar burada OLUSTURULMAZ: parola ozeti uygulamanin isidir. Testler hesaplari
-- uyelik akisiyla (Mailpit'ten okunan kodla) kendileri acar.

INSERT INTO organization.company (logo_firm_number, name, created_at, public_id)
VALUES (1, 'Duzen E2E Laboratuvar', now(), gen_random_uuid())
ON CONFLICT DO NOTHING;

INSERT INTO personnel.person (national_id, first_name, last_name, birth_date, email, is_email_shared, mobile_phone, created_at, public_id)
VALUES
  -- 1: uyelik senaryosu (hesabi yok)
  ('20000000114', 'Ayse', 'Uyelik', '1985-04-12', 'e2e.uyelik@duzen.com.tr', false, null, now(), gen_random_uuid()),
  -- 2: ilk sistem yoneticisi (AccessControl__BootstrapAdministrators)
  ('20000000282', 'Yusuf', 'Yonetici', '1980-01-15', 'e2e.yonetici@duzen.com.tr', false, null, now(), gen_random_uuid()),
  -- 3: siradan personel (giris, cikis, parola degistirme, sifirlama)
  ('20000000350', 'Mehmet', 'Personel', '1990-06-30', 'e2e.personel@duzen.com.tr', false, null, now(), gen_random_uuid()),
  -- 4: IK davetiyle parola belirleyecek kisi (hesabi yok)
  ('20000000428', 'Deniz', 'Davetli', '1992-11-03', 'e2e.davet@duzen.com.tr', false, null, now(), gen_random_uuid()),
  -- 5: yetki senaryosu (izinsiz kullanici)
  ('20000000596', 'Zeynep', 'Izinsiz', '1988-03-21', 'e2e.izinsiz@duzen.com.tr', false, null, now(), gen_random_uuid())
ON CONFLICT (national_id) DO NOTHING;

INSERT INTO personnel.employment (person_id, registry_code, company_id, hire_date, is_active, logo_ref, created_at, public_id)
SELECT p.id, 'E2E' || right(p.national_id, 3), c.id, '2020-01-01', true, 900000 + row_number() OVER (ORDER BY p.national_id), now(), gen_random_uuid()
FROM personnel.person p
CROSS JOIN organization.company c
WHERE p.national_id LIKE '2000000%' AND c.logo_firm_number = 1
ON CONFLICT (registry_code) DO NOTHING;
