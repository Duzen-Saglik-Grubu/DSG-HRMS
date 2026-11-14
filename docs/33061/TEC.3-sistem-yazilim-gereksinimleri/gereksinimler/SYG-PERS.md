# SYG-PERS — T1 Personel Yönetimi Sistem/Yazılım Gereksinimleri

**Belge kimliği:** TEC.3-SYG-PERS
**Süreç:** TEC.3 — Sistem/Yazılım Gereksinimlerinin Tanımlanması
**Modül:** T1 Personel Yönetimi
**Kaynak:** `TEC.2/paydas-gereksinimleri/PG-PERS.md` (19.10.2026'da İK onaylı, 45 gereksinim; değişiklik talepleri #270, #284, #333, #355)
**Tasarım:** ADR-0019…ADR-0022 (`KR-121`…`KR-126`); T1 tasarım gözden geçirmesi (`TEC.5-tasarim-tanimlama/raporlar/2026-10-22-t1-tasarim-gozden-gecirme.md`)
**Son güncelleme:** 2026-11-14
**Sahibi:** Bilgi İşlem
**Onay durumu:** Bilgi İşlem (RACI TEC.3 A); değişiklikler PR onayıyla (`KR-096`). Baseline, T1'in kabul etiketiyle yazılır (`KR-097`, `KR-111`); o zamana kadar geliştirme sürüyor.

> **Paydaş gereksinimi *ne* istendiğini söyler; bu belge sistemin bunu *hangi
> davranışla* karşılayacağını söyler.** Her madde doğrulanabilir, tek anlamlıdır ve en
> az bir onaylı paydaş gereksinimine bağlıdır. Paydaş gereksinimine bağlanmayan
> sistem gereksinimi yazılmaz.

**Kapsama her PR'da otomatik denetlenir** (`YAKLASIM.md` §4): her paydaş gereksinimi
en az bir sistem gereksinimine bağlı olmalı; bu belgenin §8 tablosu ve izlenebilirlik
matrisinin §7 sütunu, §4 ile aynı eşlemeyi göstermelidir.

**Doğrulama kanıtı:** Her madde, T1 geliştirilirken onu anan bir testle (Analiz ve
İnceleme için TEC.9 doğrulama raporuyla) kanıtlanır. Baseline yazılana kadar eksik kanıt
CI'da uyarıdır; T1 kabulünde bütün maddeler kanıtlı olmalıdır (#272).

---

## 1. Özet

| | |
|---|---:|
| Sistem gereksinimi | **79** |
| Karşılanan paydaş gereksinimi | **45 / 45** |
| Tür — İşlevsel | 54 |
| Tür — Performans | 4 |
| Tür — Arayüz | 1 |
| Tür — İşlevsel olmayan | 12 |
| Tür — Kısıt | 8 |
| Doğrulama — Test | 74 |
| Doğrulama — İnceleme | 1 |
| Doğrulama — Analiz | 4 |
| Parametreye bağlı | 7 |
| Kritik performans ölçütü | 5 |

---

## 2. Sistem tanımı (TEC.3 çıktısı a)

### 2.1 Amaç

T1, personel verisini LOGO Bordro'dan HRMS'e taşır ve İK'nın günlük işini Excel'den
uygulamaya alır: personeli bulmak, kartını görmek, LOGO'da olmayan birkaç bilgiyi girmek ve
personelin hatalı bilgisini kayıtlı biçimde düzeltmeye göndermek. Personel kendi kartını
görür. T1 aynı zamanda HRMS'nin **başka kişilerin verisini** gösteren ilk modülüdür; bu
yüzden kapsam, alan görünürlüğü, özel nitelikli verinin şifrelenmesi ve erişim kaydı ilk
kez burada tam olarak kurulur.

### 2.2 Öğeler

Konumlar 14.11.2026'da koddan çıkarıldı (#389; T1 geliştirme sonu denetimi TEC.5-11). İlk sürümdeki
plan iki yerde koddan ayrılmıştı: şifreleme `Infrastructure/Security` yerine
`Personnel/Protection` altında kuruldu; dışa aktarma, uygulama servisleri ve yetki sözlüğü
tabloda yoktu. "Karşıladığı SYG-PERS" sütunu §2.5'teki dosya düzeyi tahsisten hesaplanır. Kodun
yeri değiştiğinde iki tablo aynı PR'da güncellenir; yolların varlığını CI denetler
(`doc-consistency-check.mjs` §4). Bir SYG birden çok öğeye dağılabilir.

| Kod | Öğe | Ne yapar | Konum | Karşıladığı SYG-PERS | ADR |
|---|---|---|---|---|---|
| `E` | Personel ekranları | Liste (kart ızgarası ve tablo), kişi kartı, kendi profilim, senkronizasyon durumu, veri kalitesi raporu, düzeltme talepleri | `src/frontend/dsg-hrms-web/src/features/personnel/`; ortak bileşenler `src/frontend/dsg-hrms-web/src/shared/` | 008–010, 017, 020, 022–027, 029–030, 032–034, 036–045, 048, 052, 056–057, 070–071, 073 | 0015; 0019 |
| `P` | Personel API'si | Liste, kart, fotoğraf, tarihçe, HRMS'te girilen alanlar, senkronizasyon, veri kalitesi | `src/backend/Dsg.Hrms.Api/Personnel/` — `/api/v1/personnel/` | 001, 008–010, 019–020, 022–024, 029, 032–033, 036, 038–040, 042–044, 051–052, 056–057, 066, 070–071, 074 | 0010; 0019 |
| `U` | Personel uygulama servisleri | Kart, liste, tarihçe, görev, evlilik, acil durum kişisi, veri kalitesi; HRMS alanlarının depoları | `src/backend/Dsg.Hrms.Application/Personnel/` (`Cards`, `Listing`, `History`, `Duties`, `Marriage`, `EmergencyContacts`, `Quality`); `src/backend/Dsg.Hrms.Infrastructure/Personnel/` | 016–020, 022, 024–027, 029, 031, 033–036, 038–044, 051–053, 055, 057–059, 063, 066, 070–071 | 0019; 0021 |
| `S` | Kapsamlı sorgu ve görünürlük politikası | `ICurrentUserScope`, `IPersonnelQuery`, `PersonFieldVisibility`, maske | `src/backend/Dsg.Hrms.Application/Personnel/Access/`; `src/backend/Dsg.Hrms.Infrastructure/Personnel/PersonnelQuery.cs`, `CurrentUserScope.cs` | 020, 022, 024, 028, 030–032, 034, 036, 038–043, 050–053, 063 | 0019 §1–§2, §6 |
| `M` | Personel alan modeli | Kişi genişlemesi, korumalı alanlar, fotoğraf, atama, görev, evlilik, acil durum; tablo yapılandırmaları | `src/backend/Dsg.Hrms.Domain/Personnel/`; `src/backend/Dsg.Hrms.Infrastructure/Data/Configurations/`; `personnel` şeması | 003, 006, 013–015, 017–019, 021, 030, 034–036, 038–043, 045–046, 052, 054, 067, 069 | 0021 §1–§3, §6 |
| `L` | LOGO senkronizasyon işi (genişler) | Yeni alanlar, fotoğraf özeti ve küçük hâli, tarih aralıklı atama, şema denetimi, uyarılar | `src/backend/Dsg.Hrms.Application/Personnel/Sync/`; `src/backend/Dsg.Hrms.Infrastructure/Logo/`; `src/backend/Dsg.Hrms.Infrastructure/Personnel/PersonnelSyncWorker.cs` | 001–008, 010–015, 018, 021, 070 | 0003; 0021 §2, §4–§5 |
| `T` | Düzeltme talebi | "İK'ya bildir" talebi, İK listesi ve kapatma | `src/backend/Dsg.Hrms.Application/Personnel/Corrections/`; `src/backend/Dsg.Hrms.Infrastructure/Personnel/CorrectionStore.cs`; `personnel.correction_request` | 045–049 | 0022 §3 |
| `A` | Dışa aktarma | Alan seçmeli Excel ve PDF, rapor yazıcıları | `src/backend/Dsg.Hrms.Application/Personnel/Export/`; `src/backend/Dsg.Hrms.Application/Common/Reports/`; `src/backend/Dsg.Hrms.Infrastructure/Reports/` | 032–033, 035, 039, 053, 059 | 0014; 0022 §4; 0023 |
| `B` | Uygulama içi bildirim ve ileti | Bildirim kutusu, okunmamış sayısı; "İK'ya bildir" e-postası | `src/backend/Dsg.Hrms.Domain/Notifications/`, `src/backend/Dsg.Hrms.Application/Notifications/`, `src/backend/Dsg.Hrms.Api/Notifications/`, `src/backend/Dsg.Hrms.Infrastructure/Notifications/`; `notification.inbox_item` | 012, 046–047 | 0012 §4; 0022 §3 |
| `V` | Referans veri çekirdeği | Ünvan, görev ve diğer tanımlar; yönetim ekranı | `src/backend/Dsg.Hrms.Domain/Reference/`, `src/backend/Dsg.Hrms.Application/Reference/`, `src/backend/Dsg.Hrms.Api/Reference/`; `src/frontend/dsg-hrms-web/src/features/reference/`; `reference.definition` | 072, 074 | 0022 §1 |
| `X` | Şifreleme | Özel nitelikli veri anahtarı, AES-256-GCM, yeniden şifreleme işi | `src/backend/Dsg.Hrms.Infrastructure/Personnel/Protection/`; `src/backend/Dsg.Hrms.Application/Personnel/Protection/`; `src/frontend/dsg-hrms-web/src/features/system/` | 049, 060–062 | 0020 |
| `D` | Denetim izi, erişim kaydı ve denetim kayıtları ekranı | Kişi bazlı liste ve dışa aktarma kaydı; dört kayıt türünü salt okunur listeleme | `src/backend/Dsg.Hrms.Application/Audit/`; `src/backend/Dsg.Hrms.Infrastructure/Audit/`; `src/backend/Dsg.Hrms.Api/Audit/`; `src/frontend/dsg-hrms-web/src/features/audit/` | 033, 058, 062, 064, 067, 069, 074 | 0009; 0019 §5; 0022 §2 |
| `R` | Parametre deposu (genişler) | T1 parametreleri ve ekranı | `src/backend/Dsg.Hrms.Application/Settings/`; `src/backend/Dsg.Hrms.Infrastructure/Settings/`; `system_parameter` | 007, 012, 028–029, 047, 068, 075 | 0022 §5 |
| `Y` | Yetki sözlüğü | T1 izinleri ve rollere dağılımı | `src/backend/Dsg.Hrms.Application/Identity/Authorization/Permissions.cs` | 008–009, 030, 032, 048, 050, 052, 055, 062, 069–070, 072 | 0007; 0019 §4 |

### 2.3 Dış arayüzler

| Arayüz | Yön | Protokol | Kısıt |
|---|---|---|---|
| **LOGO veritabanı** (MSSQL 2019) | Yalnızca okuma | TDS | **Yazma, güncelleme, silme kesinlikle yapılmaz** (`KR-003`, `KR-004`) — SYG-PERS-002. T1 beş tablo ekler (SYG-PERS-011) |
| **Kurum SMTP sunucusu** | Giden | SMTP + TLS | Yalnızca "İK'ya bildir" e-postası; içerikte kişisel veri yok (SYG-PERS-047) |
| **Tarayıcı** | Gelen | HTTPS | Yalnızca kurum içi erişim (`KR-067`) |

### 2.4 Sınırlar

**Kapsamdadır:** §4'teki 79 gereksinim. Bunlara, onaylı gereksinimlerin ön koşulu olan dört
yatay çekirdek dâhildir (`KR-115`, `KR-119`, ADR-0022): referans veri (SYG-PERS-072),
denetim kayıtları ekranı (SYG-PERS-069), uygulama içi bildirim (SYG-PERS-046) ve dışa
aktarma (SYG-PERS-032).

**Kapsam dışıdır:**

| Konu | Nerede |
|---|---|
| Personel ekleme, düzenleme, silme; ücret, bordro, SGK bildirimi | LOGO |
| Doğrudan ast kapsamı (PG-PERS-24), yönetici filtresi (PG-PERS-34), birim hiyerarşisi | T2 Organizasyon |
| Rol yönetim ekranı, satır bazlı kapsamın diğer türleri | T4 Rol ve Yetki |
| Bildirim tercihleri, kalıcı ileti kuyruğu, diğer bildirim türleri | Y1 Bildirim Merkezi |
| Kayıtların imhası (saklama süresi dolan kayıt) | Y2 Denetim ve Erişim Kayıtları |
| Ünvan ve görev dışındaki referans türleri | Y3 Referans Veri |
| "Belirli bir tarihte bir şubede kimler vardı" raporu | Raporlama modülü (AN-03) |
| Özlük belgeleri, izin, eğitim, performans | Y7, İ1, İ2, İ3, İ5 |

### 2.5 SYG → tasarım tahsisi (TEC.5 çıktısı b)

Her sistem gereksiniminin hangi öğe(ler)de ve hangi ana bileşende karşılandığı. Tablo
14.11.2026'da koddan çıkarıldı: kod yorumlarındaki ve testlerdeki `SYG-PERS-nnn` atıfları
(79 gereksinimin 74'ü), atıfsız olanlar için kodun ve kısıtı sınayan testin elle bulunması
(#389; T1 geliştirme sonu denetimi TEC.5-11). Kodun yeri değiştiğinde tablo aynı PR'da
güncellenir. Tablodaki her yolun var olduğunu ve §4'teki her gereksinimin burada bulunduğunu
CI denetler (`doc-consistency-check.mjs` §4). ADR sütunu izlenebilirlik matrisi §7'nin
"Tasarım" sütunundan gelir (paydaş gereksinimi düzeyinde; bölüm numarası matristedir).

- **Ana bileşen** gereksinimi karşılayan en fazla dört üretim dosyasıdır. Üretimde karşılığı
  olmayan bir kısıt ("istekte LOGO'ya bağlanılmaz", "süzgeç yoktur") için kısıtı sınayan test
  yazılır. Kod dışı kontrol ve performans ölçütü için kayıt gösterilir.
- **Öğe kodları** §2.2'dedir.
- **Yol kısaltmaları:** `Api/`, `App/`, `Dom/`, `Inf/` =
  `src/backend/Dsg.Hrms.{Api,Application,Domain,Infrastructure}/`; `T/` = `src/backend/tests/`;
  `F/` = `src/frontend/dsg-hrms-web/src/`; `FP/` = `F/features/personnel/`.

| SYG | Öğe | Ana bileşen | ADR |
|---|---|---|---|
| SYG-PERS-001 | `P` `L` | `T/Dsg.Hrms.Api.IntegrationTests/Personnel/PersonnelLogoIndependenceApiTests.cs`, `T/Dsg.Hrms.Architecture.Tests/LayerDependencyTests.cs` (kısıt: istek sırasında LOGO bağlantısı yok) | 0003; 0021 |
| SYG-PERS-002 | `L` | `Inf/Logo/LogoDbContext.cs`; `T/Dsg.Hrms.Api.IntegrationTests/Logo/LogoReadOnlyAccessTests.cs`, `T/Dsg.Hrms.Architecture.Tests/LayerDependencyTests.cs` | 0003 |
| SYG-PERS-003 | `M` `L` | `App/Personnel/Sync/ILogoPersonnelSource.cs`, `App/Personnel/Sync/PersonnelDetailsSynchronizer.cs`, `App/Personnel/Sync/PersonnelSyncService.cs`, `Dom/Personnel/Person.cs` | 0003; 0019; 0021; 0022 |
| SYG-PERS-004 | `L` | `App/Personnel/Sync/ILogoPersonnelSource.cs`, `Inf/Logo/LogoSchema.cs` | 0019; 0020; 0021 |
| SYG-PERS-005 | `L` | `App/Personnel/Sync/ILogoPersonnelSource.cs`, `App/Personnel/Sync/PersonnelDetailsSynchronizer.cs`, `Inf/Logo/LogoOptions.cs`, `Inf/Logo/LogoPersonnelSource.cs` | 0019; 0020; 0021 |
| SYG-PERS-006 | `M` `L` | `T/Dsg.Hrms.Architecture.Tests/PersonnelWriteBoundaryTests.cs` (kısıt: LOGO alanına yazma ucu yok; HRMS alanları ayrı tablolarda) | 0021 |
| SYG-PERS-007 | `L` `R` | `Inf/Personnel/PersonnelSyncWorker.cs`, `App/Personnel/Sync/PersonnelSyncOptions.cs`, `App/Settings/ParameterCatalog.cs` | 0003 |
| SYG-PERS-008 | `E` `P` `L` `Y` | `App/Personnel/Sync/IPersonnelSyncStatus.cs`, `Api/Personnel/PersonnelSyncRunsController.cs`, `FP/api/syncApi.ts`, `FP/pages/SyncRunsPage.tsx` | 0003 |
| SYG-PERS-009 | `E` `P` `Y` | `Api/Personnel/PersonnelSyncRunsController.cs` | 0003 |
| SYG-PERS-010 | `E` `P` `L` | `App/Personnel/Sync/IPersonnelSyncStatus.cs`, `Api/Personnel/SyncFreshnessController.cs`, `FP/api/personsApi.ts`, `FP/components/SyncFreshness.tsx` | 0003 |
| SYG-PERS-011 | `L` | `Inf/Logo/LogoSchema.cs` | 0003; 0021 |
| SYG-PERS-012 | `L` `B` `R` | `App/Personnel/Sync/PersonnelSyncAlerts.cs`, `App/Personnel/Sync/PersonnelSyncService.cs`, `App/Settings/ParameterCatalog.cs`, `Dom/Notifications/InboxItem.cs` | 0003; 0021 |
| SYG-PERS-013 | `M` `L` | `App/Personnel/Sync/IPersonnelSyncStore.cs`, `App/Personnel/Sync/PersonnelPhotoSynchronizer.cs`, `App/Personnel/Sync/PersonnelSyncService.cs`, `Dom/Personnel/PersonProfile.cs` | 0021 |
| SYG-PERS-014 | `M` `L` | `App/Personnel/Sync/ILogoPersonnelSource.cs`, `App/Personnel/Sync/IPersonnelSyncStore.cs`, `App/Personnel/Sync/IPhotoThumbnailer.cs`, `App/Personnel/Sync/PersonnelPhotoSynchronizer.cs` | 0021 |
| SYG-PERS-015 | `L` `M` | `App/Personnel/Sync/PersonnelSynchronizer.cs`, `Dom/Personnel/Person.cs`; `T/Dsg.Hrms.Api.IntegrationTests/Personnel/PersonListApiTests.cs` | 0005 |
| SYG-PERS-016 | `U` | `App/Personnel/Quality/DataQualityService.cs`, `App/Personnel/Quality/IDataQualityQuery.cs` | 0003 |
| SYG-PERS-017 | `E` `U` `M` | `App/Personnel/Cards/PersonCardService.cs`, `Dom/Personnel/Employment.cs`, `FP/components/PersonProfile.tsx`, `FP/display.ts` | 0005 |
| SYG-PERS-018 | `U` `M` `L` | `App/Personnel/History/EmploymentHistoryService.cs`, `App/Personnel/Sync/ILogoPersonnelSource.cs`, `App/Personnel/Sync/PersonnelDetailsSynchronizer.cs`, `App/Personnel/Sync/PersonnelSyncService.cs` | 0021 |
| SYG-PERS-019 | `P` `U` `M` | `App/Personnel/Duties/DutyService.cs`, `Dom/Personnel/EmploymentAssignment.cs`, `Dom/Personnel/EmploymentDuty.cs`, `Inf/Data/Configurations/EmploymentDutyConfiguration.cs` | 0021; 0022 |
| SYG-PERS-020 | `E` `P` `U` `S` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/History/EmploymentHistoryService.cs`, `Api/Personnel/EmploymentHistoryController.cs`, `FP/api/historyApi.ts` | 0021 |
| SYG-PERS-021 | `M` `L` | `App/Personnel/Sync/PersonnelDetailsSynchronizer.cs`, `Dom/Personnel/EmploymentDuty.cs` | 0021; 0022 |
| SYG-PERS-022 | `E` `P` `U` `S` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Listing/PersonListService.cs`, `Inf/Personnel/PersonnelQuery.cs`, `Api/Personnel/PersonsController.cs` | 0005; 0015; 0021; 0022 |
| SYG-PERS-023 | `E` `P` | `Api/Personnel/PersonsController.cs`, `FP/components/PersonCard.tsx`, `FP/listState.ts`, `FP/pages/PersonListPage.tsx` | 0015; 0021 |
| SYG-PERS-024 | `E` `P` `U` `S` | `App/Personnel/Listing/PersonListFilters.cs`, `App/Personnel/Listing/PersonListService.cs`, `Inf/Data/HrmsDbFunctions.cs`, `Inf/Personnel/PersonFilterOptionsQuery.cs` | 0019; 0021 |
| SYG-PERS-025 | `E` `U` | `App/Personnel/Listing/PersonListFilters.cs`, `FP/filterTags.ts`, `FP/listState.ts` | 0015; 0019; 0021 |
| SYG-PERS-026 | `E` `U` | `App/Personnel/Listing/PersonListService.cs`, `FP/components/FilterPanel.tsx`, `FP/listState.ts` | 0019; 0021 |
| SYG-PERS-027 | `E` `U` | `App/Personnel/Listing/PersonListService.cs`, `FP/listState.ts` | 0022 |
| SYG-PERS-028 | `S` `R` | `App/Settings/ParameterCatalog.cs`, `Inf/Personnel/PersonnelQuery.cs` | 0022 |
| SYG-PERS-029 | `E` `P` `U` `R` | `App/Personnel/Listing/PersonListService.cs`, `App/Settings/ParameterCatalog.cs`, `Api/Personnel/PersonsController.cs`, `FP/listState.ts` | 0010; 0022 |
| SYG-PERS-030 | `E` `S` `M` `Y` | `App/Personnel/Access/PersonFieldVisibility.cs`, `Dom/Personnel/Person.cs`, `Dom/Personnel/SearchText.cs`, `Inf/Personnel/PersonnelQuery.cs` | 0021 |
| SYG-PERS-031 | `U` `S` | `App/Personnel/Listing/PersonListService.cs`, `Inf/Personnel/PersonnelQuery.cs` | 0015; 0021 |
| SYG-PERS-032 | `E` `P` `S` `A` `Y` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Export/PersonExportFields.cs`, `App/Personnel/Export/PersonExportService.cs`, `Api/Personnel/PersonExportController.cs` | 0014; 0019; 0022; 0023 |
| SYG-PERS-033 | `E` `P` `U` `A` `D` | `App/Personnel/Export/PersonExportService.cs`, `App/Personnel/Quality/DataQualityService.cs`, `Dom/Audit/AccessLogEntry.cs`, `Api/Personnel/DataQualityController.cs` | 0014; 0022 |
| SYG-PERS-034 | `E` `U` `S` `M` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Cards/PersonCardService.cs`, `Dom/Personnel/Person.cs`, `FP/api/personsApi.ts` | 0021 |
| SYG-PERS-035 | `U` `M` `A` | `App/Personnel/Cards/PersonCardService.cs`, `App/Personnel/Export/PersonExportFields.cs`, `Dom/Personnel/Age.cs` | 0021 |
| SYG-PERS-036 | `E` `P` `U` `S` `M` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Cards/PersonCardService.cs`, `App/Personnel/Marriage/MarriageService.cs`, `Dom/Personnel/PersonMarriage.cs` | 0021 |
| SYG-PERS-037 | `E` | `FP/components/PersonProfile.tsx` | 0019 |
| SYG-PERS-038 | `E` `P` `U` `S` `M` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Cards/PersonCardService.cs`, `Dom/Personnel/EmploymentAssignment.cs`, `Inf/Personnel/PersonnelQuery.cs` | 0021; 0022 |
| SYG-PERS-039 | `E` `P` `U` `S` `M` `A` | `App/Personnel/Cards/PersonCardService.cs`, `App/Personnel/Export/PersonExportFields.cs`, `App/Personnel/Export/PersonExportService.cs`, `App/Personnel/Listing/PersonListFilters.cs` | 0021; 0022 |
| SYG-PERS-040 | `E` `P` `U` `S` `M` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Cards/PersonCardService.cs`, `App/Personnel/Listing/PersonListService.cs`, `Dom/Personnel/PersonPhoto.cs` | 0021 |
| SYG-PERS-041 | `E` `U` `S` `M` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Cards/PersonCardService.cs`, `Dom/Personnel/PersonPhoto.cs`, `Inf/Personnel/PersonnelQuery.cs` | 0007; 0019; 0021 |
| SYG-PERS-042 | `E` `P` `U` `S` `M` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Cards/PersonCardService.cs`, `App/Personnel/Duties/DutyService.cs`, `App/Personnel/History/EmploymentHistoryService.cs` | 0021; 0022 |
| SYG-PERS-043 | `E` `P` `U` `S` `M` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Cards/PersonCardService.cs`, `App/Personnel/EmergencyContacts/EmergencyContactService.cs`, `Dom/Personnel/EmergencyContact.cs` | 0019; 0021; 0022 |
| SYG-PERS-044 | `E` `P` `U` | `App/Personnel/Cards/PersonCardService.cs`, `App/Personnel/History/EmploymentHistoryService.cs`, `Api/Personnel/PersonsController.cs`, `F/app/navigation.ts` | 0007; 0019 |
| SYG-PERS-045 | `E` `M` `T` | `App/Personnel/Corrections/CorrectionService.cs`, `Dom/Personnel/CorrectionRequest.cs`, `Inf/Data/Configurations/CorrectionRequestConfiguration.cs`, `Inf/Personnel/CorrectionStore.cs` | 0022 |
| SYG-PERS-046 | `M` `T` `B` | `App/Notifications/Inbox/InboxService.cs`, `App/Personnel/Corrections/CorrectionService.cs`, `Dom/Notifications/InboxItem.cs`, `Inf/Data/Configurations/InboxItemConfiguration.cs` | 0022 |
| SYG-PERS-047 | `T` `B` `R` | `App/Personnel/Corrections/CorrectionService.cs`, `App/Settings/ParameterCatalog.cs`, `Dom/Notifications/NotificationDelivery.cs` | 0022 |
| SYG-PERS-048 | `E` `T` `Y` | `App/Personnel/Corrections/CorrectionService.cs`, `Dom/Personnel/CorrectionRequest.cs`, `Api/Personnel/CorrectionsController.cs`, `FP/pages/CorrectionsPage.tsx` | 0022 |
| SYG-PERS-049 | `T` `X` | `App/Personnel/Corrections/CorrectionService.cs`, `Inf/Personnel/Protection/PersonalDataProtector.cs` | 0019; 0020; 0021; 0022 |
| SYG-PERS-050 | `S` `Y` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Access/PersonnelViewer.cs`, `Inf/Personnel/PersonnelQuery.cs` | 0007; 0019 |
| SYG-PERS-051 | `P` `U` `S` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Duties/DutyService.cs`, `App/Personnel/EmergencyContacts/EmergencyContactService.cs`, `App/Personnel/History/EmploymentHistoryService.cs` | 0007; 0019 |
| SYG-PERS-052 | `E` `P` `U` `S` `M` `Y` | `App/Personnel/Access/IPersonnelQuery.cs`, `App/Personnel/Access/PersonFieldVisibility.cs`, `App/Personnel/Cards/PersonCardService.cs`, `Dom/Personnel/EmergencyContact.cs` | 0019; 0020; 0021; 0022 |
| SYG-PERS-053 | `U` `S` `A` | `App/Common/Security/Mask.cs`, `App/Personnel/Access/PersonFieldVisibility.cs`, `App/Personnel/Cards/PersonCardService.cs`, `App/Personnel/Export/PersonExportFields.cs` | 0019 |
| SYG-PERS-054 | `M` | `Dom/Personnel/PersonSensitive.cs`, `Inf/Data/Configurations/PersonSensitiveConfiguration.cs` | 0019; 0020; 0021; 0022 |
| SYG-PERS-055 | `U` `Y` | `App/Personnel/Quality/DataQualityService.cs` | 0003; 0014; 0019; 0022 |
| SYG-PERS-056 | `E` `P` | `T/Dsg.Hrms.Architecture.Tests/ManagerFilterAbsenceTests.cs`, `FP/pages/__tests__/PersonListPage.test.tsx` (kısıt: süzgeç yok) | 0007 |
| SYG-PERS-057 | `E` `P` `U` | `App/Personnel/Cards/PersonCardService.cs`, `App/Personnel/History/EmploymentHistoryService.cs`, `Api/Personnel/PersonsController.cs` | 0009; 0019; 0020; 0021 |
| SYG-PERS-058 | `U` `D` | `App/Audit/AuditLogService.cs`, `App/Common/Abstractions/IAccessLogger.cs`, `App/Personnel/Listing/PersonListService.cs` | 0009; 0019 |
| SYG-PERS-059 | `U` `A` | `App/Personnel/Export/PersonExportService.cs`, `App/Personnel/Listing/PersonListService.cs` | 0009; 0019; 0020; 0021 |
| SYG-PERS-060 | `X` | `Inf/Personnel/Protection/PersonalDataProtectionOptions.cs`, `Inf/Personnel/Protection/PersonalDataProtector.cs` | 0019; 0020; 0021 |
| SYG-PERS-061 | `X` | `Inf/InfrastructureRegistration.cs` (açılışta anahtar doğrulaması), `Inf/Personnel/Protection/PersonalDataProtectionOptions.cs` | 0019; 0020; 0021 |
| SYG-PERS-062 | `X` `D` `Y` | `App/Personnel/Protection/PersonalDataKeyService.cs`, `Dom/Audit/SecurityEventEntry.cs`, `Inf/Personnel/PersonalDataKeyStore.cs`, `Inf/Personnel/Protection/PersonalDataProtectionOptions.cs` | 0019; 0020; 0021 |
| SYG-PERS-063 | `U` `S` | `App/Personnel/Listing/PersonListFilters.cs`, `Inf/Personnel/PersonnelQuery.cs` | 0019; 0020; 0021 |
| SYG-PERS-064 | `D` | `docker/postgres/app-role.sql`; `T/Dsg.Hrms.Api.IntegrationTests/Data/ApplicationRoleTests.cs` | 0009; 0019; 0020; 0021 |
| SYG-PERS-065 | — | Kod dışı kurum kontrolü: gizlilik taahhütleri İK biriminde (`KR-139`); T1 kabulünde rol üyeleriyle karşılaştırılır | 0019; 0020; 0021 |
| SYG-PERS-066 | `P` `U` | `App/Personnel/EmergencyContacts/EmergencyContactService.cs`, `App/Personnel/Marriage/MarriageService.cs`, `Api/Personnel/EmergencyContactsController.cs` | 0019; 0021; 0022 |
| SYG-PERS-067 | `M` `D` | `T/Dsg.Hrms.Architecture.Tests/DataClassificationTests.cs` (her alanda sınıflandırma özniteliği) | 0009; 0019; 0020; 0021 |
| SYG-PERS-068 | `R` | `App/Settings/ParameterCatalog.cs` | 0009; 0022 |
| SYG-PERS-069 | `M` `D` `Y` | `App/Audit/AuditLogService.cs`, `Inf/Audit/AuditLogQuery.cs`, `Inf/Data/Configurations/AccessLogEntryConfiguration.cs`, `Inf/Data/Configurations/ChangeLogEntryConfiguration.cs` | 0009; 0014; 0019; 0022 |
| SYG-PERS-070 | `E` `P` `U` `L` `Y` | `App/Personnel/Quality/IDataQualityQuery.cs`, `App/Personnel/Sync/PersonnelSynchronizer.cs`, `Dom/Personnel/Sync/PersonnelSyncWarning.cs`, `Inf/Personnel/DataQualityQuery.cs` | 0003; 0014 |
| SYG-PERS-071 | `E` `P` `U` | `App/Personnel/Quality/DataQualityService.cs`, `App/Personnel/Quality/IDataQualityQuery.cs`, `Api/Personnel/DataQualityController.cs` | 0003 |
| SYG-PERS-072 | `V` `Y` | `App/Reference/ReferenceDefinitionService.cs`, `Dom/Reference/ReferenceDefinition.cs`, `Api/Reference/ReferenceDefinitionsController.cs`, `F/features/reference/api/referenceApi.ts` | 0019; 0020; 0021; 0022 |
| SYG-PERS-073 | `E` | `FP/pages/PersonListPage.tsx`, `F/shared/theme/theme.ts` | 0015 |
| SYG-PERS-074 | `P` `V` `D` | `Api/Personnel/`, `Api/Reference/`, `Api/Audit/` (yol önekleri); `T/Dsg.Hrms.Architecture.Tests/ModuleRouteTests.cs` | 0007; 0010; 0015; 0019; 0021; 0022 |
| SYG-PERS-075 | `R` | `App/Settings/ParameterCatalog.cs`, `Inf/Settings/SystemParameterEditor.cs`, `Api/Settings/SystemParametersController.cs`, `F/features/system/pages/ParametersPage.tsx` | 0009; 0010; 0022 |
| SYG-PERS-076 | — | Ölçüm: `docs/33061/TEC.9-dogrulama/raporlar/2026-11-10-t1-performans-olcumu.md` (KPÖ-PERS-1) | 0021 |
| SYG-PERS-077 | — | Ölçüm: `docs/33061/TEC.9-dogrulama/raporlar/2026-11-10-t1-performans-olcumu.md` (KPÖ-PERS-2) | 0021 |
| SYG-PERS-078 | — | Ölçüm: `docs/33061/TEC.9-dogrulama/raporlar/2026-11-10-t1-performans-olcumu.md` (KPÖ-PERS-3, 4) | 0021 |
| SYG-PERS-079 | — | Ölçüm: `docs/33061/TEC.9-dogrulama/raporlar/2026-11-10-t1-performans-olcumu.md` (KPÖ-PERS-5) | 0021 |

---

## 3. Durumlar ve kipler (TEC.3 BP2)

### 3.1 Kişinin çalışma durumu

| Durum | Tanım | Listede | Kart |
|---|---|---|---|
| **Aktif** | En az bir aktif sicil (SYG-PERS-022) | Varsayılan listede | Açılır |
| **Ayrılan** | Aktif sicili yok; görünür kalma süresi dolmadı | "Ayrılan" veya "Tümü" süzgeciyle | Açılır |
| **Ayrılan, süresi dolmuş** | Son çıkış tarihinin üzerinden `PRM-PRS-01` kadar yıl geçti | Görünmez | `404` (SYG-PERS-028) |

### 3.2 Tarih aralıklı kayıt (atama, görev)

`Açık` (bitişi yok) → `Kapalı` (değer değişti, görev değiştirildi veya istihdam bitti) ·
`Silindi` (yumuşak silme; yalnızca görev). Aynı istihdamda en fazla bir açık kayıt vardır
ve silinmemiş kayıtlar çakışmaz (SYG-PERS-018, 019, 021).

### 3.3 Düzeltme talebi

| Durum | Önerilen değer | Geçiş |
|---|---|---|
| **Yeni** | Saklanır (kan grubu şifreli) | Personel gönderir (SYG-PERS-045) |
| **Tamamlandı** | **Silinir**; alan ve tarihler kalır | İK kapatır (SYG-PERS-048, `KR-126`) |

### 3.4 Sistem kipleri

| Kip | Davranış | Gereksinim |
|---|---|---|
| LOGO erişilemez | Ekranlar son başarılı senkronizasyonun verisiyle çalışır | SYG-PERS-001 |
| LOGO şeması beklenenden farklı | Senkronizasyon başlamaz; Sistem Yöneticisi rolüne bildirim | SYG-PERS-011, 012 |
| Özel nitelikli senkronizasyon kapalı (yerel) | Kan grubu okunmaz, mevcut değer temizlenir | SYG-PERS-005 |
| Veri anahtarı tanımsız | Uygulama açılmaz | SYG-PERS-061 |
| Senkronizasyon çalışıyor | İkinci çalışma başlatılamaz | SYG-PERS-009 |
| T2 devrede değil | Kapsam yalnızca `All` ve `Self` | SYG-PERS-050 |

---

## 4. Gereksinimler (TEC.3 çıktısı b)

**Tür:** İşlevsel · Performans · Arayüz · İşlevsel olmayan (güvenlik, kullanılabilirlik,
dayanıklılık) · Kısıt (tasarım ve gerçekleştirme kısıtı).
**Doğrulama:** Test (otomatik, CI'da) · Gösterim (kabulde çalışırken gösterilir) ·
İnceleme (belge veya kayıt incelemesi) · Analiz (UAT'de ölçüm veya çalışma kaydından hesap).

**Ortak kural — tarih:** Bu belgedeki "gün", "bugün" ve tarih karşılaştırmaları
**Europe/Istanbul** takvim gününe göredir (ADR-0021 §2).

### 4.1 Veri kaynağı ve senkronizasyon

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-001** | Personel ekranları ve uçları yalnızca HRMS veritabanından okur; istek sırasında LOGO'ya bağlanılmaz. LOGO erişilemezken liste, kart ve kendi profilim son başarılı senkronizasyonun verisiyle çalışmaya devam eder (SYG-KMLK-011'in T1 ekranlarına genişlemesi). | İşlevsel olmayan | PG-PERS-01 | Test | — |
| **SYG-PERS-002** | T1'in eklediği bütün LOGO sorguları, T3'ün salt okunur bağlamından ve `ILogoPersonnelSource` arayüzünden geçer (SYG-KMLK-002, 003). LOGO ad alanı ve SQL Server sağlayıcısı yalnızca LOGO altyapısında kullanılabilir; mimari testi bunu denetler. | Kısıt | PG-PERS-02 | Test | — |
| **SYG-PERS-003** | T3 alanlarına ek olarak senkronize edilen alanlar yalnızca şunlardır. **Kişi:** cinsiyet, önceki soyadı, medeni hâl (`STATUS` 1 = Evli, diğer = Bekar), eğitim (tanımın adı), adres, kan grubu (SYG-PERS-005), fotoğraf (SYG-PERS-014). **İstihdam:** firma, şehir, şube, birim, ünvan kodu, çalışma durumu, statü, SGK meslek kodu ve adı. Okunan her tablo ve kolon `LogoSchema`'da tek listede durur (toplantı kaydı §6.1). | İşlevsel | PG-PERS-01, PG-PERS-19, PG-PERS-20, PG-PERS-21 | Test | — |
| **SYG-PERS-004** | Din bilgisi (`RELIGION`) ve sağlık tabloları (`LH_001_HEALTH`) okunan alan listesinde **yoktur**. Bunu, listeyi tarayan otomatik bir test doğrular; bu alanlardan birini listeye ekleyen değişiklik CI'da düşer. | Kısıt | PG-PERS-36 | Test | — |
| **SYG-PERS-005** | Kan grubu yalnızca ortam yapılandırması `Logo:SyncSpecialCategoryData` açıkken okunur. Ayar Sistem Yönetimi parametresi değildir ve ekrandan değiştirilemez. Ayar kapalıyken senkronizasyon kan grubunu LOGO'dan okumaz ve HRMS'teki mevcut değeri temizler; temizleme denetim izine "değişti" olarak yazılır. Ortamlar: CI sentetik veri, yerel kapalı, UAT ve üretim açık (`KR-125`). | Kısıt | PG-PERS-36 | Test | — |
| **SYG-PERS-006** | LOGO'dan gelen alanlar için hiçbir yazma ucu ve ekranda düzenleme denetimi yoktur. HRMS'te girilen alanlar (görev, evlilik bilgisi, acil durum kişisi; `KR-112`) ayrı tablolardadır: senkronizasyon bu tablolara, İK ekranları LOGO kaynaklı tablolara yazmaz. Böylece senkronizasyon İK'nın girdiğini ezemez. | İşlevsel | PG-PERS-03 | Test | — |
| **SYG-PERS-007** | Yeni alanlar T3'ün periyodik çalışmasında, aynı periyotla senkronize edilir (varsayılan 15 dakika). Ayrı bir zamanlayıcı kurulmaz. | İşlevsel | PG-PERS-04 | Test | PRM-ENT-07 |
| **SYG-PERS-008** | "Senkronizasyon durumu" ekranı son çalışmaları sayfalı listeler: başlangıç ve bitiş zamanı, süre, sonuç (başarılı / kısmen / başarısız), okunan, eklenen, güncellenen, pasifleşen ve uyarı üretilen kayıt sayıları. Ekran `personnel.sync.view` izniyle açılır. | İşlevsel | PG-PERS-05, PG-PERS-06 | Test | — |
| **SYG-PERS-009** | `personnel.sync.create` izni olan kullanıcı ekrandaki düğmeyle senkronizasyonu başlatır. Çalışma sürerken ikinci başlatma reddedilir ve ekranda "çalışıyor" görünür. Çalışma bitince sonucu ekrandaki listede görünür. T3'teki `identity.sync.*` izinleri `personnel.sync.*`'a taşınır. | İşlevsel | PG-PERS-05 | Test | — |
| **SYG-PERS-010** | Personel listesinde ve kişi kartında "Son senkronizasyon" olarak son **başarılı** çalışmanın bitiş zamanı (gün ve saat) gösterilir. Son çalışma başarısızsa yanında bir uyarı işareti görünür. | İşlevsel | PG-PERS-07 | Test | — |
| **SYG-PERS-011** | Şema denetimi T1'in okuduğu beş tabloyu kapsar: `LH_001_FAMILY`, `LH_001_PERIDINF`, `LH_001_TYPEDEF`, `L_CAPIDIV`, `L_CAPIDEPT`. Denetim CI'da ve üretimde günde bir kez çalışır; beklenen bir kolon yoksa veya türü değiştiyse senkronizasyon başlamaz ve hata kaydedilir (SYG-KMLK-012'nin genişlemesi). | İşlevsel olmayan | PG-PERS-08 | Test | — |
| **SYG-PERS-012** | Şema sapması bulunduğunda veya senkronizasyon başarısız olduğunda, parametrede tanımlı roldeki her aktif kullanıcıya uygulama içi bildirim yazılır; kullanıcının LOGO'daki kurumsal adresi varsa e-posta da gönderilir. E-postada kişisel veri yoktur; neden ve senkronizasyon ekranına bağlantı bulunur. Aynı sorun için açık (okunmamış) bildirim varken ne bildirim ne e-posta yazılır. Bu, T3'te Y1'e bırakılan bildirimi T1'in bildirim çekirdeğiyle karşılar (AN-06; #344, `KR-133`, `KR-134`). | İşlevsel | PG-PERS-08 | Test | PRM-BLD-01 |
| **SYG-PERS-013** | Bir senkronizasyon çalışmasında önce kişi ve istihdam adımı tamamlanır ve kaydedilir; T1'in ek alanları ve fotoğraflar sonra işlenir. Sonraki adımlarda oluşan hata ilk adımı geri almaz; çalışma "kısmen" olarak kaydedilir. Böylece hesabın istihdam bitince kapanma süresi (SYG-KMLK-078) fotoğraf işinden etkilenmez (AN-07). | İşlevsel olmayan | PG-PERS-41 | Test | — |
| **SYG-PERS-014** | Fotoğraf yalnızca LOGO'daki özeti (`HASHBYTES('SHA2_256', CAST(LDATA AS varbinary(max)))`) değiştiyse okunur; özeti değişmeyen fotoğraf ikinci çalışmada aktarılmaz. Okunan görüntü en fazla 400 piksele küçültülür ve JPEG olarak saklanır; orijinal saklanmaz. LOGO'da silinen fotoğraf HRMS'ten de kaldırılır. | İşlevsel | PG-PERS-23, PG-PERS-41 | Test | — |

### 4.2 Kişi, istihdam ve tarih aralıkları

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-015** | Aynı TCKN'ye sahip bütün siciller tek kişiye bağlanır; kişi listede bir kez görünür, kartında her sicil ayrı bir istihdam olarak yer alır (SYG-KMLK-001). | İşlevsel | PG-PERS-09 | Test | — |
| **SYG-PERS-016** | TCKN'si boş veya geçersiz LOGO kartı kişi olarak oluşturulmaz; kişi listesinde yoktur, veri kalitesi raporunda "TCKN yok" uyarısıyla yer alır (SYG-KMLK-007). | İşlevsel | PG-PERS-10 | Test | — |
| **SYG-PERS-017** | Birden fazla istihdamı olan kişinin kartında her istihdam bir sekmedir. Sekme başlığında sicil numarası ve durum (Aktif / Ayrılan) yazar. Sıra (`KR-130`): önce aktif istihdamlar işe giriş tarihi eskiden yeniye, sonra ayrılan istihdamlar çıkış tarihi yeniden eskiye; ilk sekme esas sicildir ve kartın başlığı bu sicilin bilgisini gösterir. Tek istihdamlı kişide sekme çubuğu gösterilmez. | İşlevsel | PG-PERS-11 | Test | — |
| **SYG-PERS-018** | Firma (#284), şube, birim, ünvan kodu, statü ve SGK meslek bilgisi istihdama bağlı ve tarih aralıklı tutulur (`valid_from` dahil, `valid_to` hariç). Senkronizasyon bir değişiklik gördüğünde açık kaydı o günle kapatır ve yeni kaydı açar. Aynı gün ikinci bir değişiklik görülürse o gün açılmış kayıt güncellenir; sıfır uzunlukta aralık oluşmaz. Geçmiş, T1'in devreye girdiği günden başlar; önceki tarihler için değer tahmin edilmez. İlk kez görülen ve zaten bitmiş bir istihdamda karttaki değer çıkış günündeki değer sayılır ve tek günlük kapalı bir kayıt açılır; bitmiş istihdamda sonradan yapılan LOGO düzeltmesi son kaydı yerinde günceller (AN-19). | İşlevsel | PG-PERS-12 | Test | — |
| **SYG-PERS-019** | Aynı istihdamda tarih aralıkları çakışamaz ve en fazla bir açık kayıt bulunur; boş veya ters aralık olamaz. Kural veritabanında `EXCLUDE USING gist` kısıtıyla zorlanır: LOGO kaynaklı atamada bütün kayıtlara, HRMS'te girilen görevde silinmemiş kayıtlara (`WHERE deleted_at IS NULL`); kısıt, uygulamanın çalışma rolüyle (`hrms_app`) yapılan eklemede de sınanır. Çakışma hatası (23P01) `409 Conflict` olarak Problem Details biçiminde döner, `500` dönmez. | Kısıt | PG-PERS-12, PG-PERS-43 | Test | — |
| **SYG-PERS-020** | Kişi kartının her istihdam sekmesinde firma, şube, birim, ünvan ve görev **tarihçesi** başlangıç ve bitiş tarihleriyle listelenir. API, bir tarih verildiğinde o tarihte geçerli firma, şube, birim, ünvan ve görevi döndürür; T1 öncesi bir tarih için "kayıt yok" döner. | İşlevsel | PG-PERS-12 | Test | — |
| **SYG-PERS-021** | İstihdam bittiğinde (çıkış tarihi senkronize edildiğinde) o istihdamın açık ataması ve açık görevi **çıkış tarihinin ertesi günü** ile kapanır. Çıkış tarihi kaydın başlangıcından önceyse (LOGO'ya geriye dönük girilmiş) kayıt başladığı günün ertesi kapanır; aralık boş olamaz (AN-19). | İşlevsel | PG-PERS-12, PG-PERS-43 | Test | — |
| **SYG-PERS-022** | Birden fazla sicili olan kişi (`KR-126`, `KR-130`): kişi, en az bir aktif sicili varsa aktiftir. Liste kartı, aktif sicillerden işe giriş tarihi en eski olanın bilgisini gösterir; aktif sicil yoksa çıkış tarihi en yeni olanınkini. Kartta "X sicil" işareti bulunur (X ≥ 2). İstihdam filtreleri kişinin herhangi bir sicilinde eşleşir; "Aktif" süzgeci yalnızca aktif sicillere bakar. Ayrılan kişinin görünür kalma süresi son çıkış tarihinden başlar. | İşlevsel | PG-PERS-09, PG-PERS-13, PG-PERS-16, PG-PERS-38 | Test | — |

### 4.3 Personel listesi

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-023** | Personel listesi kart ızgarası olarak açılır. Kartta fotoğraf (SYG-PERS-040), ad soyad, sicil numarası, şube / birim ve ünvan / görev bulunur. Adlar LOGO'daki gibi büyük harfle gösterilir; gösterim için dönüşüm yapılmaz. Görünüm değiştirici, aynı süzgeç ve sayfayla klasik tablo görünümüne geçer ve geri döner. | İşlevsel | PG-PERS-13 | Test | — |
| **SYG-PERS-024** | Kişi kartında gösterilen her alan için bir süzgeç vardır. Süzgeç türü alana göredir: **çoklu seçim** (firma, şehir, şube, birim, ünvan, görev, statü, çalışma durumu, SGK meslek, eğitim, medeni hâl, cinsiyet, kan grubu, evlilik yardımı); **metin, içinde geçen** (ad, soyad, önceki soyadı, sicil, TCKN, e-posta, telefon, adres, SGK meslek kodu); **tarih aralığı** (doğum, işe giriş, çıkış, evlilik tarihi); **sayı aralığı, yıl** (yaş, kıdem); **var / yok** (fotoğraf, acil durum kişisi). Yönetici süzgeci yoktur (SYG-PERS-056). | İşlevsel | PG-PERS-14 | Test | — |
| **SYG-PERS-025** | Çoklu seçimli bir süzgeçte seçilen değerlerden **herhangi birine** uyan kayıtlar gelir; farklı süzgeçler **birlikte** (VE) uygulanır. Seçili her süzgeç, liste üstünde kaldırılabilir bir etiket olarak görünür. "Temizle" bütün süzgeçleri kaldırır; çalışma durumu varsayılana (Aktif) döner. | İşlevsel | PG-PERS-14, PG-PERS-15 | Test | — |
| **SYG-PERS-026** | Süzgeç listesi, sıralama seçenekleri ve dışa aktarma alanları görünürlük politikasından gelir (SYG-PERS-052). Kullanıcının göremediği alanın süzgeci ve sıralaması sunulmaz; API'ye bu alanla süzgeçli veya sıralı istek gelirse `400` döner. Küme işlemlerinde (süzgeç, sıralama, dışa aktarma) "kişinin kendisi" kuralı uygulanmaz, yalnızca izin esas alınır. | İşlevsel olmayan | PG-PERS-14 | Test | — |
| **SYG-PERS-027** | Liste varsayılan olarak yalnızca aktif kişileri gösterir. Çalışma durumu süzgecinin değerleri: Aktif (varsayılan), Ayrılan, Tümü. | İşlevsel | PG-PERS-16 | Test | — |
| **SYG-PERS-028** | Ayrılan kişi, son çıkış tarihinden itibaren parametredeki süre (yıl) boyunca listede ve kartta görünür. Parametre boşsa süre sınırsızdır. Süre dolmuşsa kişi listede çıkmaz ve kartı `404` döner; bu kural İK ve Sistem Yöneticisi için de geçerlidir. Aktif sicili olan kişiye uygulanmaz. Kayıt silinmez (SYG-PERS-068). | İşlevsel | PG-PERS-38 | Test | PRM-PRS-01 |
| **SYG-PERS-029** | Liste sayfalanır; toplam kayıt sayısı görünür. Varsayılan sayfa boyutu parametreden gelir (varsayılan 24). Kullanıcı sayfa boyutunu seçebilir; seçenekler en büyük sayfa boyutu parametresini aşmaz. En büyük sayfa boyutu parametresi en fazla 100 olabilir (kodda tavan, ADR-0010 §4); bunu aşan istek `400` döner. | İşlevsel | PG-PERS-17 | Test | PRM-GRN-02, PRM-GRN-03 |
| **SYG-PERS-030** | Hızlı arama kutusu tek terimle ad, soyad, sicil ve TCKN'de "içinde geçen" arama yapar. Önceki soyadı yalnızca gösterildiği koşulda (kadın personel, LOGO'da dolu) aranır. TCKN ile arama yalnızca `personnel.person.view_national_id` izni olan kullanıcıya açıktır. Arama Türkçe karakterden bağımsızdır: "isik" IŞIK'ı, "ismail" İSMAİL'i bulur; "ışık" da aynı sonucu verir. | İşlevsel | PG-PERS-18 | Test | — |
| **SYG-PERS-031** | Liste varsayılan olarak ad, sonra soyad sırasıyla Türkçe alfabeye göre (ICU `tr-TR`) sıralanır. Tablo görünümünde sütun başlığına tıklayarak o sütuna göre artan / azalan sıralanır; kan grubuna göre sıralama yoktur. | İşlevsel | PG-PERS-13 | Test | — |
| **SYG-PERS-032** | `personnel.person.export` izni olan kullanıcı, listenin **süzgece uyan bütün kayıtlarını** (yalnızca görünen sayfayı değil) dışa aktarır. Alanlar seçilebilir; seçenekler görünürlük politikasından gelir. Excel düz listedir (ADR-0014 §6 kuralları); PDF'e fotoğraf alanı seçildiyse küçük fotoğraf eklenir. Satır sınırı 50.000'dir (ADR-0014 §5). İzni olmayan kullanıcıya düğme gösterilmez; API isteği `403` döner. | İşlevsel | PG-PERS-29, PG-PERS-42 | Test | — |
| **SYG-PERS-033** | Her dışa aktarma kaydedilir: kim, ne zaman, biçim, süzgeç (korumalı süzgecin değeri yazılmadan, SYG-PERS-059), kayıt sayısı, aktarılan alanların listesi ve aktarılan kişilerin kimlikleri. | İşlevsel | PG-PERS-30 | Test | — |

### 4.4 Kişi kartı ve kendi profilim

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-034** | Kartın "Genel Bilgiler" bölümünde fotoğraf, ad, soyad, cinsiyet, doğum tarihi, yaş, eğitim, medeni hâl ve kan grubu (görünürlüğe göre) bulunur. Önceki soyadı yalnızca kadın personelde ve LOGO'da doluysa gösterilir. | İşlevsel | PG-PERS-19 | Test | — |
| **SYG-PERS-035** | Yaş doğum tarihinden ve bugünden hesaplanır, saklanmaz. Kişi doğum gününde bir yaş büyür; 29 Şubat doğumlu kişi artık yıl olmayan yıllarda 1 Mart'ta büyür (AN-05). | İşlevsel | PG-PERS-22 | Test | — |
| **SYG-PERS-036** | Evlilik tarihi ve "evlilik yardımı aldı / almadı" bilgisi HRMS'te tutulur. Girme, değiştirme ve silme yalnızca `personnel.person.update` izniyle ve kişinin medeni hâli Evli iken yapılabilir. Medeni hâl Bekar'a dönerse girilmiş bilgi korunur ve görünmeye devam eder, ama düzenlenemez. Bu bilgiyi yalnızca `personnel.person.view_sensitive` izni olan görür; kişinin kendisi göremez. Değişiklik denetim izine yazılır. | İşlevsel | PG-PERS-19 | Test | — |
| **SYG-PERS-037** | "İletişim Bilgileri" bölümünde e-posta, telefon ve adres bulunur; kayıtlı olmayan alanda "Kayıtlı değil" yazar. Adres görünürlüğe tabidir (SYG-PERS-052). | İşlevsel | PG-PERS-20 | Test | — |
| **SYG-PERS-038** | Her istihdam sekmesinin "Çalışma Hayatı" bölümünde şehir, firma, şube, birim, ünvanı, görevi, çalışma durumu, statü, sicil numarası, SGK meslek kodu, SGK meslek, işe giriş tarihi, işten çıkış tarihi ve kıdem süresi bulunur. Ünvan "Ünvanı" etiketiyle, referans verideki karşılığıyla gösterilir; karşılığı olmayan kod boş görünür ("Bilinmiyor" yazmaz). Karşılığın adı değiştirildiğinde ekran kod değişikliği olmadan yeni adı gösterir. | İşlevsel | PG-PERS-21 | Test | — |
| **SYG-PERS-039** | Kıdem süresi her istihdam için hesaplanır, saklanmaz. Aktif istihdamda süre bugüne kadar sayılır, bugün sayılmaz. Ayrılan istihdamda çıkış günü **sayılır**: süre çıkış tarihinin ertesi gününe kadar hesaplanır; giriş 01.01.2020, çıkış 31.12.2020 ise tam bir yıl, ekranda "1 Yıl 0 Ay 0 Gün" (AN-04, İK teyidi 22.10.2026, `KR-127`). Süre takvime göre yıl, ay ve gün olarak bulunur: işe giriş tarihine önce tam yıllar, sonra tam aylar eklenir, kalan gün sayılır; eklenen ayda o gün yoksa ayın son günü alınır. Gösterim "X Yıl Y Ay Z Gün"dür; sayılar sıfırla doldurulmaz. Yıl sıfırsa yıl, yıl ve ay sıfırsa ay da yazılmaz ("3 Ay 5 Gün", "12 Gün"). | İşlevsel | PG-PERS-21 | Test | — |
| **SYG-PERS-040** | Fotoğrafı olmayan kişide ad ve soyadın baş harflerinden oluşan yer tutucu gösterilir. Personelin fotoğraf yüklemesi için hiçbir ekran ve uç yoktur. | İşlevsel | PG-PERS-23 | Test | — |
| **SYG-PERS-041** | Fotoğraf ucu, kart ile aynı kapsam ve görünürlük kurallarına tabidir; kapsam dışı veya bulunmayan kişinin fotoğrafı `404` döner. Yanıt, kaynak özetini ETag olarak ve `Cache-Control: private` başlığını taşır. Fotoğraf isteği ayrıca erişim kaydı üretmez; fotoğraf, kaydı yazılan kart veya liste görüntülemesinin parçasıdır. | İşlevsel olmayan | PG-PERS-23, PG-PERS-26 | Test | — |
| **SYG-PERS-042** | Görev, istihdama bağlı ve tarih aralıklıdır. `personnel.person.update` izni olan kullanıcı, referans verideki **aktif** görev tanımlarından birini ve başlangıç tarihini (varsayılan bugün) seçer; önceki açık görev yeni görevin başlangıç tarihiyle kapanır. Başlangıç tarihi işe giriş tarihinden önce veya çıkış tarihinden sonra olamaz (AN-16). Geçmişe dönük giriş, mevcut kayıtlarla çakışırsa reddedilir (SYG-PERS-019). Görev kaydı silinmez, yumuşak silinir. İzni olan kullanıcı kendi siciline de görev girer (#355, `KR-137`). Değişiklik denetim izine yazılır. İki sicilli kişide her sicilin görevi ayrıdır. | İşlevsel | PG-PERS-43 | Test | — |
| **SYG-PERS-043** | Bir kişiye birden fazla acil durum kişisi girilebilir: yakınlığı, adı, soyadı ve cep telefonu (dördü de zorunlu; telefon SYG-KMLK-009 kuralıyla doğrulanır). Yakınlık, referans verideki "Yakınlık" tanımlarından seçilir; yeni girişte yalnızca aktif tanım seçilebilir, pasife alınan tanım eski kayıtta adıyla görünür (#348, `KR-135`). Girme, değiştirme ve silme (yumuşak) yalnızca `personnel.person.update` izniyle yapılır. İzni olan kullanıcı kendi acil durum kişilerini de girer; izni olmayan kişi kendi kartında görür ama değiştiremez (#355, `KR-137`). Görünürlük: SYG-PERS-052. Değişiklik denetim izine yazılır. | İşlevsel | PG-PERS-44 | Test | — |
| **SYG-PERS-044** | Her kullanıcı, rolü ve izni ne olursa olsun, menüdeki "Profilim" ile kendi kartını açabilir. Kart, kişinin kendisi için geçerli görünürlük kurallarıyla gösterilir: adres, kan grubu ve acil durum kişileri görünür; evlilik bilgisi görünmez; TCKN maskelidir. | İşlevsel | PG-PERS-25 | Test | — |

### 4.5 "İK'ya bildir"

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-045** | Kişi yalnızca **kendi** kartında "İK'ya bildir" ile bir düzeltme talebi oluşturur: alan (telefon, e-posta, adres, eğitim, medeni hâl, kan grubu) ve doğru değer. Değer alanına göre doğrulanır: telefon SYG-KMLK-009 kuralıyla, e-posta biçim olarak, kan grubu sekiz değerli listeden, medeni hâl Evli / Bekar seçeneğinden; adres ve eğitim en fazla 500 karakter. Aynı alan için "Yeni" durumda bir talebi varken ikinci talep reddedilir ve kullanıcıya açık talebi olduğu söylenir (AN-08). | İşlevsel | PG-PERS-37 | Test | — |
| **SYG-PERS-046** | Talep oluştuğunda, aynı veritabanı işleminde, İK (`hr-identity-operations`) ve Sistem Yöneticisi (`system-administrator`) rolündeki her etkin kullanıcıya bir uygulama içi bildirim yazılır; iki role sahip kullanıcı tek bildirim alır (#333, `KR-132`). Menüde okunmamış bildirim sayısı görünür; bildirim açılınca okundu sayılır ve ilgili talebe götürür. | İşlevsel | PG-PERS-37 | Test | — |
| **SYG-PERS-047** | Parametrede bir İK adresi tanımlıysa bu adrese e-posta gönderilir; boşsa gönderilmez. Konu satırı sabittir: "Yeni düzeltme talebi". Konu ve gövdede kişinin adı, bildirilen alan ve önerilen değer **yer almaz**; gövde yalnızca HRMS'teki talebe bağlantı verir. E-posta bellek içi kuyrukla gider; kuyruk kaybında talep ve uygulama içi bildirim korunur. | İşlevsel | PG-PERS-37 | Test | PRM-PRS-02 |
| **SYG-PERS-048** | `personnel.correction.view` izni olan kullanıcı talepleri listeler (durum süzgeciyle; varsayılan "Yeni"): kişi, alan, önerilen değer, gönderim tarihi. `personnel.correction.update` izniyle talebi "Tamamlandı" olarak kapatır; kapatınca önerilen değer veritabanından **silinir**, talepte alan, durum ve tarihler kalır. Kapatma denetim izine yazılır. Talep kaydı (değersiz hâli) personel verisinin saklama süresine tabidir. | İşlevsel | PG-PERS-37 | Test | — |
| **SYG-PERS-049** | Önerilen kan grubu şifreli saklanır (SYG-PERS-060; AAD: talep tablosu, alan ve talebin kimliği). İK'nın önerilen kan grubunu görmesi, ilgili kişi için `SpecialCategoryView` erişim kaydı üretir. | İşlevsel olmayan | PG-PERS-36, PG-PERS-37 | Test | — |

### 4.6 Yetki ve kapsam

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-050** | Kişi okumaları tek bir kapsamlı sorgu bileşeninden geçer. Kapsam izinden türetilir: `personnel.person.view` izni olan kullanıcı bütün kişileri (`All`), olmayan yalnızca kendini (`Self`) görür. Kendi kaydını görmek izin gerektirmez. Kapsam türleri genişletilebilir; doğrudan ast kapsamı (PG-PERS-24) T2'de bu yapıya eklenir (AN-11). | İşlevsel | PG-PERS-24, PG-PERS-25, PG-PERS-35 | Test | — |
| **SYG-PERS-051** | Kapsam dışı bir kişinin kimliğiyle yapılan istek ile var olmayan kimlikle yapılan istek **aynı** yanıtı alır: `404`, aynı gövde. Kural kart, fotoğraf, tarihçe, HRMS'te girilen alanlar ve düzeltme talebi uçlarının hepsinde geçerlidir. | İşlevsel olmayan | PG-PERS-26, PG-PERS-27 | Test | — |
| **SYG-PERS-052** | Alan görünürlüğü tek politikadadır: tam TCKN `view_national_id`; adres, kan grubu ve acil durum kişileri `view_sensitive` veya kişinin kendisi; evlilik bilgisi yalnızca `view_sensitive`. Göremeyen kullanıcının **API yanıtında alan yer almaz** (maskeli TCKN hariç). Politika dört yerde uygulanır: yanıt, süzgeç, sıralama, dışa aktarma alanları. Her korumalı alan için bu dört yeri sınayan bir yetki sızıntısı testi bulunur. | İşlevsel olmayan | PG-PERS-19, PG-PERS-20, PG-PERS-31, PG-PERS-36, PG-PERS-44 | Test | — |
| **SYG-PERS-053** | Tam TCKN'yi göremeyen kullanıcı, kişinin kendisi dahil, TCKN'yi maskeli görür: ilk 3 ve son 2 hane açık, aradaki 6 hane `*` (ör. `123******89`). Maskeleme sunucuda yapılır; tam değer yanıtta yoktur. | İşlevsel | PG-PERS-31 | Test | — |
| **SYG-PERS-054** | Korumalı alanlar kişi tablosunda tutulmaz: adres ve kan grubu `person_sensitive`'de, evlilik bilgisi ve acil durum kişileri kendi tablolarındadır. Kimlik modülünün bu tiplere bağımlılığı mimari testiyle reddedilir; korumalı tiplere bağımlı olabilen sınıflar listelidir. | Kısıt | PG-PERS-20, PG-PERS-36, PG-PERS-44 | Test | — |
| **SYG-PERS-055** | T1 izinleri: `personnel.person.view`, `.view_national_id`, `.view_sensitive`, `.update`, `.export`; `personnel.correction.view`, `.update`; `personnel.quality.view`, `.export`; `personnel.sync.view`, `.create`; `reference.definition.view`, `.update`. Hepsi ve ayrıca `audit.log.view` ile `system.parameter.view`, `.update` İK rolüne ve Sistem Yöneticisine verilir (`KR-126`, `KR-128`). İK rolünün görünen adı "İnsan Kaynakları" olur; kodu değişmez. **İstisna (`KR-138`, #374):** `system.encryption.update` (özel nitelikli veri şifrelemesini yönetme, SYG-PERS-062) yalnızca Sistem Yöneticisine verilir; İK rolünde yoktur. | İşlevsel | PG-PERS-05, PG-PERS-29, PG-PERS-32, PG-PERS-35, PG-PERS-37 | Test | — |
| **SYG-PERS-056** | T1'de "Yöneticisi" süzgeci arayüzde ve API'de yoktur. Süzgeç T2 Organizasyon Yönetimi ile eklenir (AN-12). | İşlevsel | PG-PERS-34 | Test | — |

### 4.7 KVKK, şifreleme ve kayıtlar

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-057** | Her kart açılışı (kendi kartı ve "Profilim" dahil, AN-10) bir `View` erişim kaydı üretir: kim, kimin, ne zaman. Yanıtta kan grubu yer aldıysa ek olarak `SpecialCategoryView` kaydı üretilir. | İşlevsel | PG-PERS-28, PG-PERS-36 | Test | — |
| **SYG-PERS-058** | Liste görüntülemesinin erişim kaydında süzgeç ve toplam sayının yanında **ekranda gösterilen kişilerin kimlikleri** de yer alır (sayfa başına en fazla sayfa boyutu kadar). | İşlevsel | PG-PERS-28 | Test | — |
| **SYG-PERS-059** | Korumalı bir alana bakan süzgecin (kan grubu, adres, TCKN, evlilik, acil durum) **değeri** erişim kaydına ve dışa aktarma kaydına yazılmaz; yalnızca süzgecin kullanıldığı yazılır. Bir süzgecin korumalı olup olmadığı alanın sınıflandırmasından (`KR-120`) türetilir; ad listesine dayanılmaz. Kan grubu süzgeciyle listelenen her kişi için ayrıca `SpecialCategoryView` kaydı üretilir. | İşlevsel olmayan | PG-PERS-28, PG-PERS-36 | Test | — |
| **SYG-PERS-060** | Özel nitelikli değerler veritabanında AES-256-GCM ile, depo katmanında şifrelenir. Şifreli değer anahtar kimliği taşır (`k<n>:<nonce>:<şifreli metin>`); her değer için rastgele nonce kullanılır; AAD tablo adı, alan adı ve kayıt sahibinin `public_id`'sidir. Veri anahtarı sır parametre anahtarından ayrıdır. Testler: veritabanında düz metin yok; aynı değer iki kişide farklı şifreli metin üretir; başka satıra veya kolona kopyalanan şifreli değer çözülemez. | İşlevsel olmayan | PG-PERS-36 | Test | — |
| **SYG-PERS-061** | Veri anahtarı (`PersonalDataProtection:Keys`) tanımlı değilse veya biçimi geçersizse uygulama açılmaz; hata günlüğe anahtar değeri olmadan yazılır. | İşlevsel olmayan | PG-PERS-36 | Test | — |
| **SYG-PERS-062** | Sistem Yöneticisi (`system.encryption.update`, `KR-138`) bir yeniden şifreleme işi başlatabilir: iş, eski anahtarla şifreli bütün değerleri (kan grubu ve açık taleplerdeki önerilen kan grubu) güncel anahtarla yeniden yazar. Değer değişmediği için denetim izine "değişti" kaydı yazılmaz; işin kendisi tek bir güvenlik olayıdır: başlatan, kayıt sayısı, kaynak ve hedef anahtar kimliği. İş sonunda eski anahtarla şifreli kalan kayıt sayısını raporlar. | İşlevsel | PG-PERS-36 | Test | — |
| **SYG-PERS-063** | Kan grubu süzgeci uygulama belleğinde çalışır: kapsam ve diğer süzgeçler veritabanında uygulanır; kalan kayıtlarda kan grubu çözülür, süzülür, sonra sıralanır, sayfalanır ve toplam sayılır. Toplam ve sayfa içeriği, süzgecin veritabanında uygulandığı eşdeğer sonuçla aynıdır (test). Kan grubunun deterministik özeti (blind index) tutulmaz. | İşlevsel | PG-PERS-14, PG-PERS-36 | Test | — |
| **SYG-PERS-064** | Erişim kayıtları değiştirilemez ve silinemez: uygulamanın çalışma rolünün (`hrms_app`) erişim kaydı tablolarında `UPDATE` ve `DELETE` yetkisi yoktur (`KR-118`). | Kısıt | PG-PERS-28, PG-PERS-36 | Test | — |
| **SYG-PERS-065** | Kan grubunu görme yetkisi veren role (`personnel.person.view_sensitive`) bir kullanıcı atanmadan önce kullanıcının gizlilik taahhüdü alınır ve kaydı tutulur. Bu, sistem dışında yürütülen bir kurum kontrolüdür; T1 kabulünde rol üyelerinin taahhüt kayıtlarıyla karşılaştırılmasıyla doğrulanır (BK-29, §3.1; AN-18). | Kısıt | PG-PERS-36 | İnceleme | — |
| **SYG-PERS-066** | Denetim izine yazılanlar: HRMS'te girilen alanların (görev, evlilik bilgisi, acil durum kişisi) ve referans tanımlarının değişikliği, talep kapatma, senkronizasyonun kişi ve atama değişiklikleri. Kan grubunun değeri yazılmaz, yalnızca değiştiği görünür; diğer kişisel veriler sınıflandırmasına göre maskelenir. | İşlevsel | PG-PERS-19, PG-PERS-43, PG-PERS-44 | Test | — |
| **SYG-PERS-067** | T1'in eklediği her varlıkta kimlik ve yabancı anahtar dışındaki her skaler alan sınıflandırılır (`[PersonalData]`, `[SpecialCategoryData]`, `[Secret]` veya gerekçeli `[AuditVisible]`). Sınıflandırma testi `bool`, enum ve sayı alanlarını ve `Reference` ile bildirim ad alanlarını da kapsar; sınıflandırılmamış alan CI'da düşer. | Kısıt | PG-PERS-28, PG-PERS-36 | Test | — |
| **SYG-PERS-068** | Personel verisinin saklama süresi (yıl) bir Sistem Yönetimi parametresidir; koda gömülmez. T1'de bu parametreye göre hiçbir kayıt silinmez; imha Y2'dedir. | İşlevsel | PG-PERS-45 | Test | PRM-KVK-03 |
| **SYG-PERS-069** | "Denetim kayıtları" ekranı yalnızca `audit.log.view` izniyle açılır ve dört kayıt türünü (değişiklik, erişim, dışa aktarma, güvenlik olayı) salt okunur, sayfalı listeler. Süzgeçler: tarih aralığı, kayıt türü, işlemi yapan, verisi görüntülenen kişi. Maskeli değerler maskeli kalır. Ekranı görüntülemek de erişim kaydına yazılır. Ekran ve uçları kayıtları değiştiren bir işlem sunmaz. | İşlevsel | PG-PERS-28, PG-PERS-30 | Test | — |

### 4.8 Veri kalitesi

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-070** | "Veri kalitesi" raporu, aktif istihdamlarda şu uyarıları listeler: TCKN yok, doğum tarihi yok, iletişim bilgisi yok, geçersiz telefon biçimi, paylaşılan e-posta, ünvanı boş, görevi boş (açık görevi olmayan aktif istihdam). Rapor uyarı türüne göre süzülür; son senkronizasyonun sonucunu gösterir. Görmek `personnel.quality.view`, Excel'e aktarmak `personnel.quality.export` iznini gerektirir; aktarım SYG-PERS-033'e göre kaydedilir. | İşlevsel | PG-PERS-32 | Test | — |
| **SYG-PERS-071** | Raporda "Üye olamayacak kişiler" ayrı bir başlıktır: aktif istihdamı olan, hesabı bulunmayan ve üyelik koşulunu karşılamayan kişiler. Koşul T3'ten gelir (SYG-KMLK-013, 017): doğum tarihi yok, kurumsal e-postası yok, e-postası kabul edilen alan adlarında değil veya paylaşılan (AN-17). Her satırda nedeni yazar. | İşlevsel | PG-PERS-33 | Test | — |

### 4.9 Referans veri

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-072** | Referans veri ekranı ünvan, görev ve kan grubu tanımlarını listeler; `reference.definition.update` izniyle yeni tanım eklenir, adı değiştirilir, pasife alınır veya pasif tanım yeniden aktif edilir (#352, `KR-136`). Kod, tür içinde tekildir. Tanım silinmez; pasif tanım yeni girişte seçilemez ama geçmiş kayıtlarda adıyla görünür. Yeniden aktif edilen tanımın kodu ve adı değişmez. Ünvan türünün ilk değerleri: 1 Birim Operatörü, 2 Birim Teknik Sorumlusu, 3 Birim Sorumlusu, 4 Yardımcı Hizmetler. **Yakınlık** türünün ilk değerleri: Eşi, Annesi, Babası, Kardeşi; İK yeni yakınlık ekleyebilir (#348, `KR-135`). Yeni görev tanımı kod değişikliği olmadan seçilebilir hâle gelir. **Kan grubu** türünün tanımları LOGO kodlarının karşılıklarıdır (1 0 Rh+, 2 0 Rh−, 3 A Rh+, 4 A Rh−, 5 B Rh+, 6 B Rh−, 7 AB Rh+, 8 AB Rh−; 0 "Belirtilmemiş" değer yok sayılır) ve **ekrandan değiştirilemez**: bir adın yanlış düzenlenmesi herkesin sağlık verisini sessizce yanlış gösterirdi (`KR-129`). Değişiklikler denetim izine yazılır. | İşlevsel | PG-PERS-21, PG-PERS-36, PG-PERS-43 | Test | — |

### 4.10 Arayüz, API ve parametreler

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-073** | Personel listesi, kişi kartı, Profilim ve "İK'ya bildir" ekranları 360 piksel genişlikten itibaren yatay kaydırma olmadan kullanılır. Telefonda süzgeç çubuğu açılır bir panele dönüşür. Dokunma hedefleri en az 44 × 44 CSS pikseldir. | İşlevsel olmayan | PG-PERS-40 | Test | — |
| **SYG-PERS-074** | Personel işlemleri `/api/v1/personnel/`, referans veri `/api/v1/reference/`, denetim kayıtları `/api/v1/audit/` altında sunulur. Bütün uçlar OpenAPI sözleşmesinde tanımlıdır, listeleri sayfalıdır ve hataları RFC 9457 Problem Details biçiminde döndürür (ADR-0010). | Arayüz | PG-PERS-13, PG-PERS-17, PG-PERS-26 | Test | — |
| **SYG-PERS-075** | T1 parametreleri Sistem Yönetimi parametre ekranında türü ve aralığıyla görünür ve düzenlenir; değişiklik denetim izine yazılır. Varsayılan sayfa boyutu (sayı, 6–100, varsayılan 24), en büyük sayfa boyutu (sayı, 24–100, varsayılan 100; varsayılan sayfa boyutundan küçük olamaz), ayrılan personelin görünür kalma süresi (yıl, 1–50, boş = süresiz), "İK'ya bildir" e-posta adresi (e-posta, boş = gönderilmez), personel verisinin saklama süresi (yıl, 1–100, boş = karar bekliyor). | İşlevsel | PG-PERS-17, PG-PERS-37, PG-PERS-38, PG-PERS-45 | Test | PRM-GRN-02, PRM-GRN-03, PRM-PRS-01, PRM-PRS-02, PRM-KVK-03 |

### 4.11 Performans

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-076** | Personel listesinin bir sayfası (24 kart, fotoğraflar dahil) 20 eş zamanlı kullanıcı yükünde 95. yüzdelikte 2 saniyenin altında açılır. Yük ve ölçüm tanımı KPÖ-PERS-1'dedir (AN-15). | Performans | PG-PERS-39 | Analiz | — |
| **SYG-PERS-077** | Kişi kartı (fotoğraf dahil) aynı yükte 95. yüzdelikte 2 saniyenin altında açılır. | Performans | PG-PERS-39 | Analiz | — |
| **SYG-PERS-078** | Fotoğrafı değişmeyen bir senkronizasyon çalışması 2 dakikanın altında biter; kişi ve istihdam adımı ayrıca 60 saniyenin altında kalır (SYG-KMLK-078). Fotoğraf özeti sorgusunun LOGO sunucusundaki süresi UAT'de ölçülür ve ADR-0021 §5'e yazılır. | Performans | PG-PERS-41 | Analiz | — |
| **SYG-PERS-079** | Kan grubu süzgeciyle listelenen sayfa da aynı yükte 95. yüzdelikte 2 saniyenin altında açılır. | Performans | PG-PERS-39 | Analiz | — |

---

## 5. Kritik performans ölçütleri (TEC.3 çıktısı c)

| Kimlik | Ölçüt | Hedef | Nasıl ölçülür | İlgili SYG |
|---|---|---|---|---|
| **KPÖ-PERS-1** | Liste sayfası açılış süresi | p95 < 2 sn | UAT'de yük testi: 20 sanal kullanıcı, her biri 10 dakika boyunca istekler arasında 5 sn bekleyerek liste sayfası açar. Süre, liste isteğinin başlangıcından sayfadaki 24 küçük fotoğrafın son baytına kadardır | SYG-PERS-076, 014 |
| **KPÖ-PERS-2** | Kişi kartı açılış süresi | p95 < 2 sn | Aynı yük profili; kart isteği ve fotoğrafı | SYG-PERS-077 |
| **KPÖ-PERS-3** | Senkronizasyon süresi | Fotoğrafı değişmeyen çalışma < 2 dk; kişi ve istihdam adımı < 60 sn | Çalışma kaydındaki adım süreleri, UAT'de bir iş günü boyunca | SYG-PERS-078, 013 |
| **KPÖ-PERS-4** | Fotoğraf özeti sorgusunun LOGO'daki maliyeti | < 10 sn; okunan sayfa sayısı ölçülüp kaydedilir | UAT'de LOGO sunucusunda sorgu istatistiği (yalnızca süre ve okuma sayısı; veri okunmaz) | SYG-PERS-014, 078 |
| **KPÖ-PERS-5** | Kan grubu süzgeçli liste | p95 < 2 sn | KPÖ-PERS-1'in yük profili, kan grubu süzgeciyle | SYG-PERS-079, 063 |

**Neden bu beşi?** KPÖ-1 ve KPÖ-2, PG-PERS-39'un gerekçesini ölçer: yavaş açılan liste
İK'yı Excel'e döndürür. KPÖ-3, T1'in senkronizasyona eklediği işin T3'ün hesap kapanma
güvencesini bozmadığını gösterir. KPÖ-4, LOGO'nun kurumun kaynak sistemi olarak
yorulmadığının tek kanıtıdır; ölçüm kötüyse ADR-0021 §5'teki iki yedek yöntem uygulanır.
KPÖ-5, şifrelemenin bedelini (bellek içi süzgeç) ölçer.

> **Kimlikler toplantı taslağıyla aynı değildir (AN-30).** 19.10.2026 toplantı kaydındaki
> taslakta KPÖ-PERS-4 "telefon ekranında kullanılabilirlik", KPÖ-PERS-5 "'İK'ya bildir'
> e-postasının ulaşma süresi" idi. Buradaki KPÖ-PERS-4 ve 5 başka ölçütlerdir; toplantı
> kaydındaki kimliklerle karıştırılmaz.

---

## 6. Gereksinim analizi (TEC.3 çıktısı d, BP3)

Paydaş gereksinimleri tek tek ve birbirleriyle, T1 tasarım kararları ve tasarım gözden
geçirmesinin bulgularıyla birlikte karşılaştırıldı. Kapanmamış olanlar ⏳ ile işaretlidir.

| No | Bulgu | Çözüm | Durum |
|---|---|---|---|
| AN-01 | PG-PERS-14 "kartta gösterilen bilgilerin tamamı için filtre" der; fotoğraf, yaş, kıdem ve acil durum kişisi gibi alanlarda süzgecin ne olacağı yazmaz. | Süzgeç türü alana göre belirlendi: çoklu seçim, içinde geçen metin, tarih aralığı, yıl aralığı, var / yok (SYG-PERS-024). Fotoğraf ve acil durum kişisi için yalnızca var / yok süzgeci vardır. Anlam değişmez, yalnızca biçim netleşir. | ✅ |
| AN-02 | PG-PERS-18 TCKN ile aramayı herkese açar; PG-PERS-31 tam TCKN'yi yalnızca İK ve Sistem Yöneticisine gösterir. Tam TCKN'yi göremeyen kullanıcı TCKN ile arayarak kişinin varlığını doğrulayabilirdi (B-17). | TCKN ile arama `view_national_id`'ye bağlandı (SYG-PERS-030). T1'de başkalarını gören (`All`) kullanıcıların hepsinde bu izin var; uygulamada kimse kısıtlanmaz. | ✅ |
| AN-03 | PG-PERS-12 "geçen yıl hangi şubedeydi" sorusunu ister. LOGO yalnızca bugünkü değeri verir; geçmiş T1'in devreye girdiği günden başlar. "Bir tarihte bir şubede kimler vardı" listesi gereksinimde yoktur. | Kişi başına tarihçe ve tarihli sorgu (SYG-PERS-018, 020). Devreye almadan önceki dönem için "kayıt yok" döner. Toplu tarihli rapor kapsam dışıdır (§2.4). **T1 kabulünde İK'ya gösterilir:** geçmiş, devreye alma gününden başlar. | ✅ |
| AN-04 | PG-PERS-21 ayrılan istihdamda kıdemi "işten çıkış tarihine kadar" sayar; çıkış gününün sayılıp sayılmadığını söylemez. Örnek: giriş 01.01.2020, çıkış 31.12.2020. Çıkış günü sayılırsa "1 Yıl", sayılmazsa "11 Ay 30 Gün". | Çıkış günü sayılır (LOGO'daki çıkış tarihi son çalışma günüdür); hesap çıkış tarihinin ertesi gününe kadar yapılır. Aktif istihdamda bugün sayılmaz (gün henüz tamamlanmadı). **İK teyidi: 22.10.2026, Doğuş Uçanok bildirdi (`KR-127`).** SYG-PERS-039. | ✅ İK teyitli |
| AN-05 | PG-PERS-22 29 Şubat doğumlu kişinin artık yıl olmayan yıllarda ne zaman yaş aldığını söylemez. | 1 Mart'ta (SYG-PERS-035). Doğum günü o yıl takvimde yoksa henüz gelmemiş sayılır. | ✅ |
| AN-06 | PG-PERS-08 şema sapmasının sistem yöneticisine **bildirilmesini** ister; T3 bunu Y1'e bırakmıştı (SYG-KMLK-010, PG-PERS-08 gerekçesi). T1 uygulama içi bildirim çekirdeğini zaten kuruyor (ADR-0022 §3). | Bildirim T1'de, mevcut çekirdekle, `PRM-BLD-01`'deki role uygulama içi olarak yazılır (SYG-PERS-012). Yeni bir altyapı gerektirmez; ADR-0022 §3'e not düşüldü. Tercihler Y1'de kalır. **Onay: Doğuş Uçanok, 22.10.2026 (`KR-127`).** **Revizyon (05.11.2026, #344, `KR-134`):** e-posta da T1'de gönderilir; ilk kararda Y1'e bırakılmıştı. | ✅ |
| AN-07 | PG-PERS-41 fotoğraflı çalışmaya 2 dakika verir; SYG-KMLK-078 (baseline `v0.2.0`) tam çalışmayı 60 saniyeyle sınırlar ve buna hesabın kapanma süresini bağlar. Fotoğraf işi aynı çalışmaya girince T3 güvencesi bozulabilir. | Çalışma iki adıma ayrıldı: kişi ve istihdam önce tamamlanıp kaydedilir (SYG-KMLK-078 bu adımın süresidir), ek alanlar ve fotoğraf sonra (SYG-PERS-013, 078). | ✅ |
| AN-08 | PG-PERS-37 aynı alan için tekrarlanan talepleri sınırlamaz; aynı değer defalarca gönderilip İK'ya bildirim ve e-posta yağdırılabilir. | Aynı alan için "Yeni" durumda tek talep olabilir (SYG-PERS-045). Paydaşın anlamını değiştirmez: talep kaybolmaz, yalnızca tekrarı engellenir. | ✅ |
| AN-09 | PG-PERS-37 kan grubunun bildirilebilmesini ister; PG-PERS-36 kan grubunu şifreli saklamayı ve sınırlı görünürlüğü ister. Önerilen kan grubu da özel nitelikli veridir ve LOGO'dan yeniden üretilemez (B-03). | Önerilen değer şifrelenir, talep kapanınca silinir, İK'nın görmesi kayda geçer (SYG-PERS-048, 049; KVKK Sorumlusu kararı `KR-126`). | ✅ |
| AN-10 | PG-PERS-28 "bir personelin kişisel verisinin görüntülenmesi"ni kaydeder; kişinin kendi kartını görmesinin kayda girip girmediğini söylemez. | Kendi kartı da kaydedilir (SYG-PERS-057). Kural basit kalır ve "verilerimi kim gördü" sorusunun cevabı eksiksiz olur. | ✅ |
| AN-11 | PG-PERS-24 (doğrudan ast) T2'deki atamalara bağlıdır ve T1'de karşılanamaz. | T1 geçiş kuralını (PG-PERS-35) uygular; kapsam modeli T2'nin `DirectReports`'u ekleyebileceği biçimde kurulur (SYG-PERS-050). PG-PERS-24'ün kabul kriteri T2 kabulünde sınanır. **T1 kabulünde İK'ya gösterilir.** | ✅ |
| AN-12 | PG-PERS-34 yönetici süzgecinin T2 devreye girdiğinde "kod değişikliği olmadan" görünmesini ister; T2'nin kendisi bir kod değişikliğidir. | T1'de süzgeç yoktur (SYG-PERS-056). Süzgeç, T2'nin yönetici ilişkisiyle birlikte gelir; ayrıca bir açma işlemi gerektirmez. Kriterin "kod değişikliği olmadan" kısmı T2 kabulünde sınanır. | ✅ |
| AN-13 | PG-PERS-38 görünür kalma süresi dolan kaydın "gösterilmez" olmasını ister; kartın doğrudan adresle açılıp açılamayacağını ve çok sicilli kişide sürenin başlangıcını söylemez. | Süre dolunca kart herkes için `404` döner; süre son çıkış tarihinden başlar (SYG-PERS-022, 028; `KR-126`). Denetim kayıtlarındaki kimlikler korunur. | ✅ |
| AN-14 | PG-PERS-20, 36 ve 44 "kişinin kendisi görür" der. Bu, liste süzgeci ve dışa aktarma gibi küme işlemlerine uygulansaydı, kendi kan grubunu gören personel kan grubuna göre süzerek başkalarının değerini çıkarabilirdi (B-07). | "Kişinin kendisi" kuralı yalnızca tek kaydın gösterildiği yerdedir; küme işlemlerinde yalnızca izin geçerlidir (SYG-PERS-026). | ✅ |
| AN-15 | PG-PERS-39 "aynı anda 20 kullanıcı" der. T3'te benzer ifade iki farklı biçimde okunmuş ve ölçüm yeniden tanımlanmıştı (SYG-KMLK AN-25). | Yük profili baştan yazıldı: 20 sanal kullanıcı, istekler arası 5 sn, 10 dakika; süre fotoğraflar dahil (KPÖ-PERS-1). | ✅ |
| AN-16 | PG-PERS-43 görevin tarih aralıklı olmasını ister; aralığın istihdamın dışına taşıp taşamayacağını söylemez. | Görevin başlangıcı işe giriş tarihinden önce ve çıkış tarihinden sonra olamaz (SYG-PERS-042); istihdam bitince açık görev kapanır (SYG-PERS-021). | ✅ |
| AN-17 | PG-PERS-33 "üyelik akışını etkileyen kayıtları" tanımlamaz. | Tanım T3'ün üyelik koşulundan alındı: doğum tarihi, kurumsal e-posta, kabul edilen alan adı, tekil e-posta (SYG-PERS-071). | ✅ |
| AN-18 | PG-PERS-36'daki gizlilik taahhüdü bir yazılım davranışı değil, kurum kontrolüdür. | Doğrulaması İnceleme'dir: kabulde rol üyeleri taahhüt kayıtlarıyla karşılaştırılır (SYG-PERS-065). İK, taahhüt kayıtlarını tutar. | ✅ |
| AN-19 | **Türetilmiş davranış (24.10.2026, #285).** ADR-0021 §2 iki durumu tanımlamıyordu: (1) T1 devreye girdiğinde zaten bitmiş olan istihdamın nitelikleri; LOGO yalnızca bugünkü değeri verir ve "bugünden başlayan" kayıt çıkış tarihiyle çelişir. (2) LOGO'ya geriye dönük girilen ve kaydın başlangıcından önceki bir çıkış tarihi; ertesi gün kuralı boş veya ters aralık üretir. | (1) LOGO kartı çıkıştan sonra donar; karttaki değer çıkış günündeki değer sayılır ve [çıkış günü, ertesi gün) aralıklı tek kayıt açılır. Kartta ayrılan istihdamın son bilinen nitelikleri böylece görünür. Bitmiş istihdamda sonradan yapılan LOGO düzeltmesi son kaydı yerinde günceller. (2) Kayıt başladığı günün ertesi kapanır (SYG-PERS-018, 021). İkisi de paydaş gereksiniminin anlamını değiştirmez: değer uydurulmaz, yalnızca son bilinen değer saklanır. | ✅ |
| AN-20 | **Türetilmiş davranış (04.11.2026, #325).** SYG-PERS-010 "başarılı" çalışmayı ve süren çalışmanın uyarıya etkisini tanımlamaz. | Uyarılı tamamlanan çalışma başarılıdır: veri aktarıldı, uyarılar İK'nın LOGO'da düzelteceği kayıtlardır. T1 adımı başarısız olan ("kısmen") çalışma başarılı sayılmaz ve uyarı işareti çıkarır; sağlık denetimi de onu "bozulmuş" sayar. Uyarı son **biten** çalışmaya bakar; süren çalışma uyarıyı ne açar ne kapatır. Senkronizasyonun tanımlı olmadığı ortamda (LOGO bağlantısı yok) bilgi gösterilmez. Bilgi, kendi kartını gören izinsiz kullanıcıya da açıktır; yalnızca zaman ve durum döner, ayrıntı `personnel.sync.view` izniyle görülür. | ✅ |
| AN-21 | **Türetilmiş davranış (04.11.2026, #330).** SYG-PERS-032/033 satırın ne olduğunu, çok sicilli kişide hangi istihdamın aktarılacağını, maskeli TCKN'yi ve dosyadaki süzgeç özetini tanımlamaz. | Satır kişidir; istihdam alanları kartın esas istihdamından gelir (`KR-130`). Ad ve soyad her satırda vardır, seçilmez. Tam TCKN'yi göremeyen kullanıcı TCKN alanını seçebilir; dosyada TCKN maskelidir (SYG-PERS-053). Dosyanın ilk sayfasındaki süzgeç özeti kimlik yerine ad yazar; korumalı süzgecin değeri dosyaya da yazılmaz, yalnızca "(kullanıldı)". Kan grubu aktarılırsa değeri dosyaya giren her kişi için `SpecialCategoryView` kaydı üretilir (SYG-PERS-059 ile aynı ilke). Fotoğraf yalnızca PDF'e aktarılır; Excel isteği `400` alır. 50.000 satırı aşan istek `400` alır ve süzgeci daraltmanız istenir. | ✅ |
| AN-22 | **Türetilmiş davranış (05.11.2026, #335).** SYG-PERS-070/071 uyarıların hangi çalışmadan geldiğini, "aktif istihdamlarda" koşulunun kişi oluşturulmayan kartta nasıl sınandığını, kişi düzeyindeki uyarının hangi sicilde gösterileceğini ve raporda hangi kişisel verinin yer alacağını tanımlamaz. | **Kart düzeyindeki** uyarılar (TCKN yok, TCKN geçersiz, doğum tarihi yok, geçersiz telefon biçimi) veriyi aktaran son çalışmadan (başarılı, uyarılı veya kısmen tamamlanan) gelir; başarısız çalışmanın uyarısı kullanılmaz. Uyarı kaydı kartın aktif olup olmadığını da tutar; ayrılmış kartın uyarısı raporda yoktur. **Kişi düzeyindeki** uyarılar (iletişim bilgisi yok, paylaşılan e-posta) güncel veriden hesaplanır ve kişi başına bir kez, esas istihdamın sicilinde gösterilir (`KR-130`). **İstihdam düzeyindeki** uyarılar (ünvanı boş, görevi boş) her aktif istihdam için ayrıdır; "görevi boş", bitiş tarihi olmayan veya ileride olan görevi bulunmayan istihdamdır (SYG-PERS-018). Raporda ve dosyada TCKN, e-posta ve telefon **yer almaz**; İK kaydı LOGO'da sicil koduyla bulur. Rapor kapsam süzgeci uygulamaz: izinleri yalnızca tam kapsamlı rollerdedir (SYG-PERS-055). "Üye olamayacak kişiler"de doğum tarihi nedeni çıkmaz: doğum tarihi olmayan kart kişi olarak oluşturulmaz ve uyarılarda yer alır. Excel dosyasında uyarılar ve üye olamayacak kişiler ayrı sayfalardadır; tür süzgeci yalnızca uyarılara uygulanır. | ✅ |
| AN-23 | **Türetilmiş davranış (06.11.2026, #359).** SYG-PERS-020 tarihçenin kime, hangi kayıtla ve nasıl gösterileceğini; tarihli sorguda dönemin sınırını ve görüntülemenin erişim kaydını tanımlamaz. | Tarihçe ve tarihli sorgu tek uçtur: `GET …/employments/{sicil}/history`, isteğe bağlı `date`. Yetki ve kapsam kartla aynıdır: kapsam dışı kişi ve olmayan sicil `404` alır (SYG-PERS-051), kişi kendi tarihçesini izinsiz görür (SYG-PERS-044). Dönemler yeniden eskiye sıralanır; silinmiş görev kaydı yer almaz (SYG-PERS-042). Tarihli sorguda dönem [başlangıç, bitiş) aralığıdır: bitiş günü yeni dönemin ilk günüdür (ADR-0021 §2). T1 öncesi tarih boş liste döner, ekranda "Bu tarih için kayıt yok" yazar. Ekranda bitiş, dönemin son günü olarak gösterilir (sunucudaki bitişten bir gün önce); açık dönemde "Devam ediyor" yazar. Tarihçe kişisel veridir: her görüntüleme kişiye ait bir `View` erişim kaydı üretir (SYG-PERS-057 ile aynı ilke). | ✅ |
| AN-24 | **Türetilmiş davranış (07.11.2026, #361).** SYG-PERS-036 kayıt sayısını, alanların zorunluluğunu, tarih sınırlarını, silmeyi ve izni olan kişinin kendi kaydını tanımlamaz. | Kişi başına bir evlilik kaydı vardır; yeniden giriş kaydı değiştirir, silinmiş kayıt yeni girişi engellemez. Evlilik tarihi ve evlilik yardımı (Aldı / Almadı) ikisi de zorunludur. Tarih bugünden sonra ve doğum tarihinden önce olamaz (`422`). Medeni hâl Evli değilse giriş, değiştirme ve silme `422` alır; Bekar'a dönmüş kişide girilmiş bilgi görünür, ekranda nedeni yazılır. Kartın "evlilik" bölümü yalnızca `view_sensitive` izni olanın yanıtındadır; kayıt yoksa alanlar "Kayıtlı değil" yazar. Kural izne bakar: izni olan kullanıcı kendi kartında da görür (ADR-0019 §2), izni olmayan kişi kendi kartında görmez. Bölüm, kullanıcının bilgiyi düzenleyip düzenleyemeyeceğini (giriş izni ve medeni hâl Evli) de taşır. | ✅ |
| AN-25 | **Türetilmiş davranış (07.11.2026, #363).** SYG-PERS-069 dört türün nasıl birlikte listeleneceğini, "verisi görüntülenen kişi" süzgecinin her türde neyi eşleyeceğini, kişi süzgecinin seçenek kaynağını, ayrıntının gösterimini ve büyük tablolarda sayfalamanın sınırını tanımlamaz. | Dört tür tek listede, yeniden eskiye sıralanır; erişim kaydındaki `Export` "dışa aktarma", diğer erişim türleri "erişim" sayılır. **İşlemi yapan:** değişiklik ve erişimde kaydı yazan hesap, güvenlik olayında işlemi yapan oturumun hesabı; arka plan işleminin kaydında "Sistem". **Verisi görüntülenen kişi:** erişimde kaydın konusu olan kişi veya listede ya da dosyada verisi gösterilen kişiler (SYG-PERS-058), değişiklikte kişi kaydının kendisi, güvenlik olayında olayın konusu olan kişi. Kişiye bağlı diğer kayıtların (acil durum kişisi, görev, evlilik bilgisi) değişikliği bu süzgeçle değil kayıt türü ve kimliğiyle bulunur. Kişi süzgeçleri personel aramasını kullanır; personel görme izni olmayan kullanıcıya sunulmaz. Ayrıntı kayıtta nasıl duruyorsa öyle gösterilir: değişiklikte maskeli alanlar, erişimde maskeli süzgeçler, güvenlik olayında kısa kod; maske açılmaz. Ekran varsayılan olarak son 7 günü gösterir; tarih aralığı İstanbul günüdür ve iki uç da dahildir. Her kaynaktan sayfa × sayfa boyutu kadar en yeni kayıt okunup birleştirilir; bu derinlik 10.000 kaydı aşamaz, daha eski kayıtlar için tarih aralığı daraltılır. Ekranı görüntülemek, süzgeçleriyle bir `List` erişim kaydı üretir. | ✅ |
| AN-26 | **Türetilmiş davranış (08.11.2026, #371).** SYG-PERS-024 kan grubu, evlilik yardımı, evlilik tarihi, fotoğraf ve acil durum kişisi süzgeçlerini sayar; hangilerinin korumalı olduğunu, kayıt olmayan kişinin nasıl eşleşeceğini, kan grubu seçeneklerinin kaynağını ve SYG-PERS-059'daki "listelenen her kişi" ifadesinin listede ve dışa aktarmada kimleri kapsadığını tanımlamaz. | **Koruma:** kan grubu, evlilik yardımı, evlilik tarihi ve acil durum kişisi `view_sensitive` ister (ADR-0019 §2; kümede "kişinin kendisi" yok); fotoğraf listede herkese göründüğü için korumasızdır. **Eşleşme:** evlilik bilgisi olmayan kişi evlilik yardımı ve evlilik tarihi süzgeçlerinin hiçbir değerine uymaz; silinmiş evlilik bilgisi ve acil durum kişisi sayılmaz; çözülemeyen kan grubu hiçbir seçeneğe uymaz. **Seçenekler:** kan grubu referans verideki bütün tanımlardan (pasifler dahil), evlilik yardımı "Aldı / Almadı", fotoğraf ve acil durum kişisi "Var / Yok". **Kan grubu (SYG-PERS-063):** kapsam ve diğer süzgeçlerden kalan kişilerin değeri çözülür, süzülür; sayım, sıralama ve sayfalama bu kişi kümesi üzerinde veritabanında yapılır. Liste ve dışa aktarma aynı kümeyi kullanır. **Kayıt (SYG-PERS-059):** dört korumalı süzgecin değeri erişim ve dışa aktarma kaydında "(kullanıldı)"dır; fotoğraf süzgecinin değeri yazılır. Kan grubu süzgeci kullanıldığında listede **ekrandaki sayfada gösterilen** her kişi, dışa aktarmada **dosyaya giren** her kişi için bir `SpecialCategoryView` kaydı yazılır; dışa aktarmada kan grubu sütunu da seçildiyse kişi başına tek kayıt. | ✅ |
| AN-27 | **Türetilmiş davranış (08.11.2026, #375).** SYG-PERS-062 işin nerede başlatılacağını, işten önce neyin görüleceğini, çözülemeyen değerin ve silinmiş kaydın nasıl ele alınacağını, işin tekrar çalıştırılmasını ve yeni anahtarın ortama nasıl verileceğini tanımlamaz. | **Uç ve ekran:** `GET /api/v1/personnel/encryption` güncel anahtarı ve şifreli değerlerin anahtara göre sayılarını (tanımlı olmayan anahtarlar dahil) döner; değer, anahtar ve kişi dönmez. `POST /api/v1/personnel/encryption/re-encryption` işi çalıştırır. Ekran Sistem → Parametreler'de "Özel nitelikli veri şifrelemesi" bölümüdür; yalnızca bu izni olana görünür, iş onay penceresiyle başlar. **Kapsam:** kişilerin kan grubu ve önerilen kan grubu taşıyan düzeltme talepleri; silinmiş satırlar dahil, çünkü şifreli metin orada da durur. **Yazım:** değerler değişiklik izleyicisi dışında, eski şifreli metin koşuluyla yazılır; iş sırasında aynı satır başka bir yazımla değiştiyse atlanır. Bütün yazımlar ve güvenlik olayı tek veritabanı işlemindedir. **Çözülemeyen değer** (bozuk, tanımsız anahtar, başka yere ait) yeniden yazılmaz ve ayrıca sayılır. **Tekrar:** güncel anahtarla şifreli değer yeniden yazılmaz; işi tekrar çalıştırmak güvenlidir ve her çalıştırma bir olay yazar. **Olay ayrıntısı:** `<kaynaklar>><hedef> n=<yeniden yazılan> kalan=<eski anahtarla kalan> cozulemeyen=<sayı>`. **Anahtar yuvaları:** ortam `k1`–`k3` yuvalarını aktarır; boş yuva tanımsız sayılır, en az bir anahtar zorunludur; yeni yazımlar en büyük numaralı anahtarla yapılır. | ✅ |
| AN-28 | **Türetilmiş davranış (08.11.2026, #377).** SYG-PERS-073 "telefon"un hangi genişlik olduğunu, 44 piksel kuralının hangi cihazda uygulanacağını, sığmayan tablo görünümünün ne olacağını ve ölçümün nasıl yapılacağını tanımlamaz. | **Telefon:** 600 pikselin altı. Personel listesi orada kart görünümüyle açılır, görünüm seçimi sunulmaz; adresteki tablo seçimi korunur ve geniş ekranda geri gelir. Süzgeç paneli tam genişliktir. Kullanıcı bölümü (Profilim, çıkış) açılır menüdedir. **Dokunma hedefi:** kural dokunmatik ekranda (`pointer: coarse`) uygulanır ve temada tek yerdedir: düğme, simge düğmesi, geçiş düğmesi, metin ve seçim alanı, kaldırılabilen veya tıklanabilen etiket, sayfa boyutu seçicisi. Fareyle kullanılan ekranda boyutlar değişmez. Yalnızca bilgi veren etiket (durum, anahtar türü) dokunma hedefi değildir. **Sayfalama:** satır dar ekranda kırılır; düğmeler ekranın içinde kalır. **Ölçüm:** uçtan uca senaryo 360 × 740 dokunmatik ekranda her ekranı ekran durulana kadar ölçer: sayfa yatay kaymaz, hiçbir görünür öğe sağ kenarı aşmaz, görünür her dokunma hedefi en az 44 × 44 pikseldir. Metin alanında parmağın dokunduğu çerçeve ölçülür; tarih alanının ekran okuyucudan da gizli değeri ve onay kutusunun saydam girdisi dokunma hedefi sayılmaz. | ✅ |
| AN-29 | **İki gereksinimin uzlaştırılması (08.11.2026, #379).** SYG-PERS-006 "senkronizasyon HRMS'te girilen tablolara yazmaz" der; SYG-PERS-021 ise istihdam bitince açık görevin senkronizasyonla kapanmasını ister. İkisi birlikte uygulanmıştır (PR #298). | **Görev kaydında senkronizasyonun tek yazımı**, istihdam bitince açık görevin bitiş tarihini kapatmaktır (SYG-PERS-021). Görevin adı ve başlangıcı değişmez, kapalı görev yeniden açılmaz; SYG-PERS-006'nın amacı ("senkronizasyon İK'nın girdiğini ezemez") korunur. Evlilik bilgisi ve acil durum kişisine senkronizasyon hiç dokunmaz. Mimari testi bunu sınar: senkronizasyon sınıfları evlilik bilgisi ve acil durum kişisi tiplerine bağımlı değildir; görev tipine yalnızca T1 adımı (`PersonnelDetailsSynchronizer`), onun anlık görüntüsü ve veri deposu bağımlıdır. Personel modülünün yazma uçları gerekçeleriyle bir listededir; LOGO kaynaklı alana yazan uç yoktur. Yeniden şifreleme ucu (SYG-PERS-062) değeri değiştirmeden şifreli metni yeniler; senkronizasyonu tetikleme ucu LOGO kaynaklı tablolara yalnızca senkronizasyon üzerinden yazar. | ✅ |
| AN-30 | **KPÖ seti toplantı taslağından ayrıldı (T1 geliştirme sonu denetimi TEC.3-9, #392).** İK'ya 19.10.2026'da bilgi olarak sunulan taslakta KPÖ-PERS-4 "telefon ekranında kullanılabilirlik: 360 px'te yan kaydırma yok" ve KPÖ-PERS-5 "'İK'ya bildir' e-postasının ulaşma süresi: p95 < 60 sn" idi. §5'te aynı kimlikler "fotoğraf özeti maliyeti" ve "kan grubu süzgeçli liste" oldu. Ayrılmanın gerekçesi o gün kayda geçmedi. | **Telefon ölçütü** KPÖ olmaktan çıktı ama karşılanıyor: SYG-PERS-073 ve AN-28 (telefon 600 pikselin altı; kart görünümü; dokunma hedefi). 360 px'te yan kaydırma uçtan uca testte sınanır. **E-posta ulaşma süresi** düştü: e-postanın kullanıcıya ulaşması kurum SMTP sunucusuna ve alıcının posta kutusuna bağlıdır, HRMS'in ölçebileceği bir süre değildir. HRMS tarafında sınanan, talep kaydedildiği anda iletinin kuyruğa alınmasıdır (SYG-PERS-047). İki yeni ölçüt (fotoğraf özeti maliyeti, kan grubu süzgeçli liste) T1 tasarımının getirdiği iki riski ölçer (ADR-0021 §5, ADR-0020). Bu değerlendirme Bilgi İşlem'indir; T1 kabulünde İK'ya gösterilir (#398). | ⏳ İK teyidi |

**T1 tasarım gözden geçirmesinin test tanımı olmayan kuralları** (rapor §4) şu maddelerde,
doğrulama yöntemiyle yer alır:

| Kural | SYG-PERS |
|---|---|
| EXCLUDE kısıtı ve tarih kuralları | 018, 019, 021 |
| AAD ile değerin satırdan satıra taşınamaması | 060 |
| Anahtar döndürme | 062 |
| Bellek içi süzgeçte sayfalama ve toplam | 063 |
| Fotoğrafın değişmediğinde yeniden okunmaması | 014 |
| E-postada değer olmaması | 047 |
| Erişim kaydı içeriği | 033, 057, 058, 059 |
| Ayrılan personelin görünürlük süresi | 028 |
| Fotoğraf, dışa aktarma ve düzeltme talebi uçlarında `404` | 041, 051 |
| Kıdem hesabı | 039 |

**Paydaşa geri bildirim (BP3):** AN-04 onaylı bir kabul kriterinin (PG-PERS-21) uç
durumunu tek anlama indirir; İK 22.10.2026'da teyit etti (`KR-127`). AN-03 ve AN-11, kabul
kriterlerinin T1 içinde tam gösterilemeyecek kısmını söyler; bu ikisi T1 kabulünde İK'ya
ayrıca gösterilir.

AN-19…AN-30 geliştirme sırasında eklendi (14.11.2026'da değerlendirildi, #392; TEC.3-10):

- **T1 kabulünde İK'ya gösterilir** (kullanıcının gördüğü davranışı belirler): AN-19 (ayrılan
  istihdamın son nitelikleri), AN-21 (dışa aktarmada satır, esas istihdam, maskeli TCKN), AN-22
  (veri kalitesi raporundaki uyarıların kaynağı), AN-24 (evlilik bilgisi giriş kuralları), AN-25
  (denetim kayıtları ekranı), AN-26 (korumalı süzgeçler, kaydı olmayan kişinin eşleşmesi), AN-28
  (telefonda görünüm) ve AN-30 (KPÖ setinin taslaktan ayrılması).
- **Anlam değişmez** (teknik ayrıntı veya iki gereksinimin uzlaştırılması): AN-20, AN-23,
  AN-27, AN-29.

Diğer bulgular paydaş gereksinimlerinin anlamını değiştirmez.

---

## 7. Destekleyici sistemler (TEC.3 çıktısı e)

| Sistem | Ne için | Durum |
|---|---|---|
| LOGO salt okunur hesabı | Senkronizasyon, şema denetimi | ✅ Mevcut; yazma reddi CI'da sınanıyor (SYG-KMLK-003). T1 tablolarını okuma yetkisi UAT'de doğrulanacak |
| LOGO sunucusunda sorgu ölçümü | KPÖ-PERS-4 | ⏳ UAT'de, ilk fotoğraflı senkronizasyonda |
| Özel nitelikli veri anahtarı | SYG-PERS-060, 061 | Yerel `docker/.env`'de ✅ (#317). UAT `.env.uat`'te ⏳: kan grubunu içeren sürüm UAT'ye çıkmadan önce eklenir. Yeri BK-12, BK-16 ve BK-29'da |
| UAT sunucusu | Kabul ve ölçüm | ✅ Mevcut; uygulama `hrms_app` rolüyle çalışıyor (`KR-118`) |
| Kurum SMTP hesabı | "İK'ya bildir" e-postası | ✅ Mevcut (T3) |
| ClosedXML, PDFsharp / MigraDoc | Dışa aktarma | ✅ Projede mevcut (ADR-0014) |
| SkiaSharp | Fotoğraf küçültme | ⏳ Fotoğraf PR'ıyla eklenir; MIT lisansı, paket envanterine yazılır |
| Yük testi aracı | KPÖ-PERS-1, 2, 5 | ✅ T3 ölçümünde kullanılan yöntem (SYG-KMLK-077) |
| Gizlilik taahhüdü kayıtları | SYG-PERS-065 | ✅ Sistem Yöneticisi ve İK dâhil laboratuvarda çalışan tüm personelin taahhüdü var; İK biriminde saklanıyor (`KR-139` (8), 13.11.2026). Rol üyeleriyle karşılaştırma T1 kabulünde (AN-18) |

---

## 8. İzlenebilirlik (TEC.3 çıktısı f)

Aşağıdaki tablo §4'ün **tersidir**: paydaş gereksiniminden sistem gereksinimine.
`.github/scripts/requirement-traceability-check.mjs` her PR'da şunları denetler;
biri bozulursa PR birleştirilemez:

- Her paydaş gereksinimi en az bir sistem gereksinimine bağlı.
- §4'teki her kaynak kimliği `PG-PERS`'te var.
- Sistem gereksinimi kimlikleri tekil ve sıralı; metin içindeki atıflar boşa düşmüyor.
- Bu tablo ve `izlenebilirlik-matrisi.md` §7'nin "Sistem gereksinimi" sütunu §4 ile
  **aynı** eşlemeyi gösteriyor.

| Paydaş gereksinimi | Sistem gereksinimleri |
|---|---|
| `PG-PERS-01` | SYG-PERS-001, 003 |
| `PG-PERS-02` | SYG-PERS-002 |
| `PG-PERS-03` | SYG-PERS-006 |
| `PG-PERS-04` | SYG-PERS-007 |
| `PG-PERS-05` | SYG-PERS-008, 009, 055 |
| `PG-PERS-06` | SYG-PERS-008 |
| `PG-PERS-07` | SYG-PERS-010 |
| `PG-PERS-08` | SYG-PERS-011, 012 |
| `PG-PERS-09` | SYG-PERS-015, 022 |
| `PG-PERS-10` | SYG-PERS-016 |
| `PG-PERS-11` | SYG-PERS-017 |
| `PG-PERS-12` | SYG-PERS-018, 019, 020, 021 |
| `PG-PERS-13` | SYG-PERS-022, 023, 031, 074 |
| `PG-PERS-14` | SYG-PERS-024, 025, 026, 063 |
| `PG-PERS-15` | SYG-PERS-025 |
| `PG-PERS-16` | SYG-PERS-022, 027 |
| `PG-PERS-17` | SYG-PERS-029, 074, 075 |
| `PG-PERS-18` | SYG-PERS-030 |
| `PG-PERS-19` | SYG-PERS-003, 034, 036, 052, 066 |
| `PG-PERS-20` | SYG-PERS-003, 037, 052, 054 |
| `PG-PERS-21` | SYG-PERS-003, 038, 039, 072 |
| `PG-PERS-22` | SYG-PERS-035 |
| `PG-PERS-23` | SYG-PERS-014, 040, 041 |
| `PG-PERS-24` | SYG-PERS-050 |
| `PG-PERS-25` | SYG-PERS-044, 050 |
| `PG-PERS-26` | SYG-PERS-041, 051, 074 |
| `PG-PERS-27` | SYG-PERS-051 |
| `PG-PERS-28` | SYG-PERS-057, 058, 059, 064, 067, 069 |
| `PG-PERS-29` | SYG-PERS-032, 055 |
| `PG-PERS-30` | SYG-PERS-033, 069 |
| `PG-PERS-31` | SYG-PERS-052, 053 |
| `PG-PERS-32` | SYG-PERS-055, 070 |
| `PG-PERS-33` | SYG-PERS-071 |
| `PG-PERS-34` | SYG-PERS-056 |
| `PG-PERS-35` | SYG-PERS-050, 055 |
| `PG-PERS-36` | SYG-PERS-004, 005, 049, 052, 054, 057, 059, 060, 061, 062, 063, 064, 065, 067, 072 |
| `PG-PERS-37` | SYG-PERS-045, 046, 047, 048, 049, 055, 075 |
| `PG-PERS-38` | SYG-PERS-022, 028, 075 |
| `PG-PERS-39` | SYG-PERS-076, 077, 079 |
| `PG-PERS-40` | SYG-PERS-073 |
| `PG-PERS-41` | SYG-PERS-013, 014, 078 |
| `PG-PERS-42` | SYG-PERS-032 |
| `PG-PERS-43` | SYG-PERS-019, 021, 042, 066, 072 |
| `PG-PERS-44` | SYG-PERS-043, 052, 054, 066 |
| `PG-PERS-45` | SYG-PERS-068, 075 |

---

## 9. Değişiklik

Bu belge Pull Request ile değişir. Bir sistem gereksinimi değişikliği bir paydaş
gereksiniminin **anlamını** değiştiriyorsa önce `tur:degisiklik-talebi` issue'su açılır
ve İK'nın onayı alınır (TEC.2 YAKLAŞIM §4.1). Numara yeniden kullanılmaz; iptal edilen
madde `İptal` notuyla kalır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-22 | 0.1 | İlk oluşturma: PG-PERS'teki 45 onaylı gereksinimden ve T1 tasarım kararlarından (ADR-0019…0022) türetildi; 79 madde, 5 KPÖ, 18 analiz bulgusu (#274) | Bilgi İşlem |
| 2026-10-22 | 0.2 | AN-04 İK teyidiyle kapandı: ayrılan istihdamda kıdem çıkış gününü sayar (SYG-PERS-039); AN-06 onaylandı (`KR-127`, #274) | Bilgi İşlem |
| 2026-10-22 | 0.3 | SYG-PERS-039: örnek ekran metni "1 Yıl 0 Ay 0 Gün" olarak netleştirildi; yıl varsa üç birim de yazılır (PG-PERS-21; #276) | Bilgi İşlem |
| 2026-10-23 | 0.4 | SYG-PERS-055: denetim kayıtları ve parametre izinleri İK'da da (`KR-128`, #278) | Bilgi İşlem |
| 2026-10-23 | 0.5 | SYG-PERS-072: kan grubu tanımları referans veride, değiştirilemez; kaynak PG-PERS-36 (`KR-129`, #280) | Bilgi İşlem |
| 2026-10-24 | 0.6 | Değişiklik talebi #284: SYG-PERS-018 ve 020'ye firma; SYG-PERS-019 kısıt kapsamı (atama bütün kayıtlar, görev silinmemiş); AN-19 türetilmiş davranış: bitmiş istihdamın ilk kaydı ve geriye dönük çıkış (SYG-PERS-018, 021; #285) | Bilgi İşlem |
| 2026-11-03 | 0.7 | §7 ortam hazırlığı: özel nitelikli veri anahtarı yerelde eklendi, UAT'de sürümden önce; yeri BK-12, BK-16, BK-29 (#317). Gereksinim metni değişmedi | Bilgi İşlem |
| 2026-11-03 | 0.8 | Değişiklik talebi #321: SYG-PERS-022'de esas sicil, aktif sicillerden işe giriş tarihi en eski olan (`KR-130`; onay 03.11.2026) | Bilgi İşlem |
| 2026-11-03 | 0.9 | Değişiklik talebi #321: SYG-PERS-017'de kişi kartındaki sicil sekmelerinin sırası esas sicille uyumlu hâle getirildi; ilk sekme esas sicil (`KR-130`) | Bilgi İşlem |
| 2026-11-04 | 0.10 | §6: AN-20, SYG-PERS-010'daki "başarılı çalışma" ve uyarı tanımı (#325) | Bilgi İşlem |
| 2026-11-04 | 0.11 | §6: AN-21, dışa aktarmada satır, esas istihdam, maskeli TCKN ve süzgeç özeti (#330) | Bilgi İşlem |
| 2026-11-04 | 0.12 | Değişiklik talebi #333: SYG-PERS-046 bildirimi Sistem Yöneticisi rolüne de yazar (`KR-132`) | Bilgi İşlem |
| 2026-11-05 | 0.13 | §6: AN-22, veri kalitesi raporunda uyarıların kaynağı, aktiflik, sicil ve kişisel veri (#335) | Bilgi İşlem |
| 2026-11-05 | 0.14 | Değişiklik talebi #344: SYG-PERS-012 uyarısı e-postayla da gider; AN-06 revize edildi (`KR-133`, `KR-134`) | Bilgi İşlem |
| 2026-11-05 | 0.15 | Değişiklik talebi #348: SYG-PERS-043 yakınlığı referans veriden seçilir; SYG-PERS-072'ye "Yakınlık" türü eklendi (`KR-135`) | Bilgi İşlem |
| 2026-11-06 | 0.16 | Değişiklik talebi #352: SYG-PERS-072'de pasif tanım yeniden aktif edilir (`KR-136`) | Bilgi İşlem |
| 2026-11-06 | 0.17 | Değişiklik talebi #355: SYG-PERS-042 ve 043'te giriş izni olan kullanıcı kendi görevini ve acil durum kişilerini girer (`KR-137`) | Bilgi İşlem |
| 2026-11-06 | 0.18 | §6: AN-23, istihdam tarihçesinin ucu, kapsamı, dönem sınırı ve erişim kaydı (#359) | Bilgi İşlem |
| 2026-11-07 | 0.19 | §6: AN-24, evlilik bilgisinin kaydı, zorunlu alanları, tarih sınırları ve görünürlüğü (#361) | Bilgi İşlem |
| 2026-11-07 | 0.20 | §6: AN-25, denetim kayıtları ekranının birleşik listesi, kişi süzgeçleri, ayrıntı ve sayfalama sınırı (#363) | Bilgi İşlem |
| 2026-11-08 | 0.21 | §6: AN-26, kan grubu, evlilik, fotoğraf ve acil durum kişisi süzgeçlerinin koruması, eşleşmesi ve erişim kaydı (#371) | Bilgi İşlem |
| 2026-11-08 | 0.22 | §4: SYG-PERS-055 ve 062, `system.encryption.update` yalnızca Sistem Yöneticisinde (`KR-138`, #374); §6: AN-27, yeniden şifreleme işinin uç, kapsam, yazım ve anahtar yuvaları (#375) | Bilgi İşlem |
| 2026-11-08 | 0.23 | §6: AN-28, telefon genişliği, dokunmatik ekranda 44 piksel kuralı, telefonda kart görünümü ve ölçüm (#377) | Bilgi İşlem |
| 2026-11-08 | 0.24 | §6: AN-29, SYG-PERS-006 ile 021'in uzlaştırılması: senkronizasyonun görev kaydındaki tek yazımı (#379) | Bilgi İşlem |
| 2026-11-13 | 0.25 | §7: gizlilik taahhüdü kayıtlarının varlığı (`KR-139`); SYG-PERS-056 negatif testle doğrulandı (#388) | Bilgi İşlem |
| 2026-11-14 | 0.26 | §2.2 öğe konumları koddan (şifreleme `Personnel/Protection`; dışa aktarma, uygulama servisleri ve yetki sözlüğü öğe oldu; öğe kodları); §2.5 dosya düzeyi tahsis 79 gereksinim için; ikisi CI'da denetlenir (T1 geliştirme sonu denetimi TEC.5-11, #389) | Bilgi İşlem |
| 2026-11-14 | 0.27 | Başlıktaki "Kaynak" satırı: PG değişiklik talepleri #270, #284, #333, #355 (T1 geliştirme sonu denetimi TEC.2-8; #392) | Bilgi İşlem |
| 2026-11-14 | 0.28 | §5: KPÖ kimliklerinin toplantı taslağından farklı olduğu notu; §6: AN-30 (KPÖ seti taslaktan ayrıldı) ve AN-19…AN-30 için paydaşa geri bildirim kararı (T1 geliştirme sonu denetimi TEC.3-9, TEC.3-10; #392) | Bilgi İşlem |
