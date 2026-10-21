-- DSG-HRMS: uygulamanin calisma rolu (#244, tasarim gozden gecirmesi B-01; ADR-0009 §2, KR-118).
--
-- NEDEN: Denetim izi, erisim kaydi ve guvenlik olaylari degistirilemez olmalidir (KR-060,
-- KR-094). Bunu saglayan tetikleyiciyi tablonun SAHIBI veya super kullanici kapatabilir.
-- Uygulama bu yuzden sahip olmayan, super kullanici olmayan bir rolle baglanir:
--   * veri semalarinda okuma, ekleme, guncelleme ve silme;
--   * audit semasinda YALNIZCA okuma ve ekleme (UPDATE, DELETE, TRUNCATE yok);
--   * sema degistirme (DDL) yok; tetikleyiciyi kapatamaz.
-- Migration'lar sahip rolle (POSTGRES_USER) uygulanir; bu betik ondan SONRA calisir.
--
-- KULLANIM: sahip rolle, psql degiskenleriyle (betik idempotenttir; her dagitimda calisir):
--   psql -v app_role=hrms_app -v app_password=... -f app-role.sql
-- Parola komut satirina yazilmamalidir; dagitim betigi onu standart girdiden \set ile verir.
--
-- Yeni bir SEMA ekleyen migration'dan sonra betik yeniden calistirilmalidir; mevcut
-- semalardaki yeni tablolar varsayilan yetkilerle kendiliginden kapsanir.

\set ON_ERROR_STOP on

-- Rol: giris yapabilir; super kullanici, rol/veritabani olusturma ve cogaltma yetkisi yok.
SELECT format('CREATE ROLE %I LOGIN', :'app_role')
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'app_role') \gexec
ALTER ROLE :"app_role" WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS
  PASSWORD :'app_password';

SELECT format('GRANT CONNECT ON DATABASE %I TO %I', current_database(), :'app_role') \gexec

-- Uygulama semalari: sistem semalari ve migration gecmisinin durdugu public disindaki
-- her sema. Uygulama public'e hic erismez. Yetkiler tek islemde verilir: yarida kalan
-- calisma, audit'te fazla yetkiyle birakmaz.
BEGIN;

CREATE TEMP TABLE app_schemas_tx ON COMMIT DROP AS
  SELECT nspname FROM pg_namespace
   WHERE nspname NOT LIKE 'pg\_%' AND nspname NOT IN ('information_schema', 'public');

SELECT format('GRANT USAGE ON SCHEMA %I TO %I', nspname, :'app_role') FROM app_schemas_tx \gexec

-- Veri semalari: okuma ve yazma.
SELECT format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO %I', nspname, :'app_role')
  FROM app_schemas_tx WHERE nspname <> 'audit' \gexec
SELECT format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I', nspname, :'app_role')
  FROM app_schemas_tx WHERE nspname <> 'audit' \gexec

-- Kimlik sutunlarinin dizileri.
SELECT format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA %I TO %I', nspname, :'app_role') FROM app_schemas_tx \gexec
SELECT format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT ON SEQUENCES TO %I', nspname, :'app_role')
  FROM app_schemas_tx \gexec

-- Denetim kayitlari: yalnizca okuma ve ekleme. Onceden verilmis olabilecek fazla yetki geri alinir.
SELECT format('REVOKE ALL ON ALL TABLES IN SCHEMA audit FROM %I', :'app_role')
  WHERE EXISTS (SELECT 1 FROM app_schemas_tx WHERE nspname = 'audit') \gexec
SELECT format('GRANT SELECT, INSERT ON ALL TABLES IN SCHEMA audit TO %I', :'app_role')
  WHERE EXISTS (SELECT 1 FROM app_schemas_tx WHERE nspname = 'audit') \gexec
SELECT format('ALTER DEFAULT PRIVILEGES IN SCHEMA audit REVOKE ALL ON TABLES FROM %I', :'app_role')
  WHERE EXISTS (SELECT 1 FROM app_schemas_tx WHERE nspname = 'audit') \gexec
SELECT format('ALTER DEFAULT PRIVILEGES IN SCHEMA audit GRANT SELECT, INSERT ON TABLES TO %I', :'app_role')
  WHERE EXISTS (SELECT 1 FROM app_schemas_tx WHERE nspname = 'audit') \gexec

COMMIT;
