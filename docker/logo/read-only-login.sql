-- ============================================================================
-- LOGO salt-okunur oturumu (KR-003, KR-004, ADR-0003 §1)
-- ============================================================================
--
-- HRMS'in LOGO Bordro veritabanina baglandigi oturum. Yalnizca OKUR; yazma,
-- guncelleme, silme, sema degistirme ve saklı yordam calistirma yetkileri DENY
-- ile acikca reddedilir.
--
-- NEDEN DENY (yalnizca db_datareader degil)?
--   db_datareader yazma yetkisi VERMEZ, ancak yetki baska bir yoldan (baska bir
--   rol uyeligi, public rolune verilen bir yetki) gelebilir. DENY, GRANT'ten
--   ustundur: oturuma sonradan hangi rol verilirse verilsin yazamaz.
--
-- CALISTIRAN: LOGO veritabani yoneticisi (sysadmin). HRMS bu betigi CALISTIRMAZ.
--
-- KULLANIM (sqlcmd degiskenleri):
--   sqlcmd -S <sunucu> -E -i read-only-login.sql ^
--          -v LogoDatabase="BORDRO" ReaderLogin="hrms_logo_reader" ReaderPassword="..."
--
-- DOGRULAMA (hicbir sey yazmadan):
--   SELECT HAS_PERMS_BY_NAME('LH_001_PERSON', 'OBJECT', 'UPDATE');  -- 0 olmali
--   HRMS de her senkronizasyondan once ayni denetimi yapar ve yazma yetkisi
--   gorurse calismayi reddeder.
--
-- CI'da bu dosyanin KENDISI calistirilarak sinanir (LogoReadOnlyAccessTests):
-- yazma denemelerinin veritabani tarafindan reddedildigi otomatik olarak kanitlanir
-- (SYG-KMLK-003).
--
-- Canli ortamda 2026-09-03'te uygulanmistir (KR-004); bu dosya ayni yetkileri
-- tanimlar.
-- ============================================================================

CREATE LOGIN [$(ReaderLogin)] WITH PASSWORD = N'$(ReaderPassword)', CHECK_POLICY = ON, DEFAULT_DATABASE = [$(LogoDatabase)];
GO

USE [$(LogoDatabase)];
GO

CREATE USER [$(ReaderLogin)] FOR LOGIN [$(ReaderLogin)];
GO

ALTER ROLE [db_datareader] ADD MEMBER [$(ReaderLogin)];
GO

DENY INSERT, UPDATE, DELETE, ALTER, EXECUTE TO [$(ReaderLogin)];
GO
