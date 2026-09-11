# Paket Envanteri

**Belge kimliği:** MIM-003
**Son güncelleme:** 2026-09-11
**İlgili süreç:** TEC.5 (Tasarım), MAN.5 (Konfigürasyon Yönetimi)
**İlgili kararlar:** `KR-001`, `KR-005`, `KR-006`, `KR-025`, `KR-026`, `KR-057`

> **Bu belge bir konfigürasyon öğesidir.** Kullanılan her paket, sürümü ve **lisansı**
> burada kayıtlıdır. Yeni paket eklenmeden önce lisansı kontrol edilir ve bu belgeye
> işlenir.

---

## 1. Lisans kuralı

Kurum kararı gereği **yalnızca ücretsiz ve koşulsuz lisanslı** paketler kullanılır
(`KR-025`). Aşağıdakiler **kabul edilmez**:

- Ciro eşiğine bağlı lisanslar (örn. "yıllık geliri X'in altındaki şirketler için ücretsiz")
- Kullanıcı veya geliştirici sayısına bağlı lisanslar
- Değerlendirme/deneme süresi olan lisanslar

Gerekçe: bu tür lisanslar sonradan maliyet ve hukuki belirsizlik üretir; kurumun
"düşük tedarikçi bağımlılığı" ilkesine aykırıdır.

**Uygulama:** `src/backend/Directory.Packages.props` dosyasının başında bu kural
yazılıdır. Kod incelemesinde yeni paket eklenen her PR'da lisans sorulur.

---

## 2. Reddedilen paketler

Bu tablo, **neden kullanılmadıklarını** kayıt altına alır. Amacı, ileride birisinin
"neden kullanmamışlar" diye sorup yeniden eklemesini önlemektir (`R-05`).

| Paket | Alternatifi | Ret gerekçesi | Karar |
|---|---|---|---|
| **AutoMapper** | Riok.Mapperly | 2025'te ticari lisansa geçti (ciro eşiği) | `KR-005` |
| **MediatR** | Kendi servis katmanımız | Ticari lisansa geçti | `KR-006` |
| **QuestPDF** | PdfSharp / MigraDoc | Community lisansı ciro eşiğine bağlı | `KR-025` |
| **FluentAssertions** | Shouldly | **8.x sürümü koşullu ticari lisansa geçti** | `KR-057` |
| **EPPlus** | ClosedXML | 5.x'ten itibaren ticari lisans | `KR-026` |
| **EF Core In-Memory** | Testcontainers | Gerçek veritabanı davranışını taklit etmiyor | ADR-0011 §3 |
| **MUI X `DateRangePicker`** | İki adet `DatePicker` | Yalnızca ücretli (Pro) pakette; iki ücretsiz `DatePicker` ile aynı işlev elde edildi | `KR-025` |
| **MUI X `DataGrid`** | MUI `Table` üzerine kendi `DataTable` | Sunucu tarafı özelliklerin bir kısmı Pro pakette; lisans belirsizliği yerine tam denetim tercih edildi | `KR-025` |

---

## 3. Backend paketleri

Sürümler `src/backend/Directory.Packages.props` dosyasında **merkezî olarak** tanımlıdır.
Proje dosyalarında sürüm yazılmaz.

### 3.1 Çalışma zamanı

| Bileşen | Sürüm | Lisans |
|---|---|---|
| .NET SDK | 10.0.400 | MIT |
| .NET Runtime / ASP.NET Core | 10.0.11 (LTS) | MIT |

### 3.2 Veri erişimi

| Paket | Sürüm | Lisans | Amaç |
|---|---|---|---|
| Microsoft.EntityFrameworkCore | 10.0.12 | MIT | ORM |
| Microsoft.EntityFrameworkCore.Design | 10.0.12 | MIT | Migration araçları |
| Microsoft.EntityFrameworkCore.Relational | 10.0.12 | MIT | İlişkisel sağlayıcı temeli |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | PostgreSQL License | PostgreSQL sağlayıcısı |
| EFCore.NamingConventions | 10.0.1 | Apache-2.0 | `snake_case` dönüşümü (ADR-0004 §1) |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.12 | MIT | LOGO Bordro **salt-okunur** erişimi (ADR-0003) |

### 3.3 Uygulama katmanı

| Paket | Sürüm | Lisans | Amaç |
|---|---|---|---|
| Riok.Mapperly | 4.3.1 | Apache-2.0 | Nesne eşleme (kaynak üreteci) |
| FluentValidation | 12.1.1 | Apache-2.0 | Doğrulama |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | Apache-2.0 | DI entegrasyonu |
| Microsoft.Extensions.* (DI, Logging, Options) | 10.0.12 | MIT | Soyutlamalar |

### 3.4 Loglama ve gözlemlenebilirlik

| Paket | Sürüm | Lisans | Amaç |
|---|---|---|---|
| Serilog | 4.4.0 | Apache-2.0 | Günlük çekirdeği (Infrastructure katmanı) |
| Serilog.AspNetCore | 10.0.0 | Apache-2.0 | Yapılandırılmış loglama |
| Serilog.Sinks.Console | 6.1.1 | Apache-2.0 | Konsol çıktısı |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 | Dosya çıktısı |
| OpenTelemetry.Extensions.Hosting | 1.18.0 | Apache-2.0 | İzleme ve ölçüm |
| OpenTelemetry.Instrumentation.AspNetCore | 1.12.0 | Apache-2.0 | HTTP sunucu izleme |
| OpenTelemetry.Instrumentation.Http | 1.12.0 | Apache-2.0 | HTTP istemci izleme |
| OpenTelemetry.Instrumentation.Runtime | 1.18.0 | Apache-2.0 | Çalışma zamanı ölçümleri (GC, iş parçacığı) |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.18.0 | Apache-2.0 | OTLP dışa aktarımı |

### 3.5 API

| Paket | Sürüm | Lisans | Amaç |
|---|---|---|---|
| Microsoft.AspNetCore.OpenApi | 10.0.11 | MIT | OpenAPI belgesi (ADR-0010) |
| Microsoft.Extensions.ApiDescription.Server | 10.0.12 | MIT | **Derleme zamanı** OpenAPI belgesi üretimi |
| Microsoft.OpenApi | 2.12.0 | MIT | OpenAPI nesne modeli — sürüm **araç uyumu için sabitlendi** |
| Swashbuckle.AspNetCore | 10.2.3 | MIT | Swagger arayüzü |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.11 | MIT | JWT doğrulama (ADR-0006) |
| AspNetCore.HealthChecks.NpgSql | 9.0.0 | Apache-2.0 | Veritabanı hazır olma kontrolü |

> **Not:** `Microsoft.Extensions.Diagnostics.HealthChecks` .NET 10'da paylaşılan
> çerçeveye dâhildir; açıkça referans verilmez (NU1510).

### 3.6 Dış sistem entegrasyonu

| Paket | Sürüm | Lisans | Amaç |
|---|---|---|---|
| Microsoft.Extensions.Http.Resilience | 10.10.0 | MIT | Yeniden deneme, zaman aşımı, devre kesici (ADR-0012 §5) |

### 3.7 Raporlama

| Paket | Sürüm | Lisans | Amaç |
|---|---|---|---|
| ClosedXML | 0.105.0 | MIT | Excel üretimi (ADR-0014) |
| PdfSharp | 6.2.1 | MIT | PDF üretimi |
| PdfSharp.MigraDoc | 6.2.1 | MIT | PDF belge düzeni |

### 3.8 Test

| Paket | Sürüm | Lisans | Amaç |
|---|---|---|---|
| Microsoft.NET.Test.Sdk | 18.10.0 | MIT | Test altyapısı |
| xunit | 2.9.3 | Apache-2.0 | Test çerçevesi |
| xunit.runner.visualstudio | 3.1.5 | Apache-2.0 | Test koşucusu |
| **Shouldly** | 4.3.0 | **BSD-3-Clause** | İddia (assertion) kütüphanesi — `KR-057` |
| NSubstitute | 6.2.0 | BSD-3-Clause | Sahte nesne (mock) |
| coverlet.collector | 10.0.1 | MIT | Kod kapsamı ölçümü |
| NetArchTest.Rules | 1.3.2 | MIT | Mimari kural denetimi (ADR-0002) |
| Testcontainers.PostgreSql | 4.15.0 | MIT | Gerçek PostgreSQL ile test (ADR-0011 §3) |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.11 | MIT | API entegrasyon testi |

---

## 4. Frontend paketleri

Sürümler `src/frontend/dsg-hrms-web/package.json` dosyasında; `package-lock.json`
**depoda tutulur** ve CI `npm ci` ile birebir aynı sürümleri kurar.

### 4.1 Çalışma zamanı

| Paket | Sürüm | Lisans | Amaç |
|---|---|---|---|
| react / react-dom | 19.2 | MIT | Arayüz kütüphanesi |
| /material · /icons-material | 9.4 | MIT | Bileşen kütüphanesi ve tema |
| /react · /styled | 11.14 | MIT | MUI stil motoru |
| react-router | 8.3 | MIT | Yönlendirme, kod bölme |
| /react-query | 5.102 | MIT | Sunucu durumu (ADR-0015 §2) |
| axios | 1.20 | MIT | HTTP istemcisi ve ara katmanlar |
| i18next · react-i18next | 26.4 / 17.0 | MIT | Çoklu dil (ADR-0015 §9) |
| react-hook-form | 7.87 | MIT | Form yönetimi |
| @mui/x-date-pickers | 9.13 | MIT | Tarih seçimi (**community** sürüm) |
| date-fns | 4.4 | MIT | Tarih işlemleri ve Türkçe yerelleştirme |
| zod · @hookform/resolvers | 4.6 / 5.9 | MIT | Şema doğrulama |

### 4.2 Geliştirme ve test

| Paket | Sürüm | Lisans | Amaç |
|---|---|---|---|
| vite · /plugin-react | 8.3 / 6.1 | MIT | Derleme ve geliştirme sunucusu |
| typescript | **5.9** | Apache-2.0 | Tip sistemi — sürüm gerekçesi §4.3 |
| eslint · typescript-eslint | **9.39** / 8.70 | MIT | Statik analiz — sürüm gerekçesi §4.3 |
| eslint-plugin-react-hooks · react-refresh | 7.1 / 0.5 | MIT | React kuralları |
| eslint-config-prettier | 10.1 | MIT | Biçim kurallarını ESLint'ten ayırır |
| prettier | 3.9 | MIT | Biçimlendirme |
| vitest · /coverage-v8 | 5.0 | MIT | Test ve kapsam |
| jsdom | 30.0 | MIT | Tarayıcı ortamı benzetimi |
| -library/react · jest-dom · user-event | 16.3 / 7.0 / 14.6 | MIT | Bileşen testleri |
| openapi-typescript | 7.13 | MIT | **OpenAPI'den tip üretimi** (ADR-0010 §1) |
| globals · /* | — | MIT | Tip tanımları |

### 4.3 Sürüm sabitleme gerekçeleri

Vite şablonu **TypeScript 6.0** ve **ESLint 10** ile geldi; ikisi de araç
ekosistemi tarafından henüz desteklenmiyor:

| Paket | Şablon | Seçilen | Neden |
|---|---|---|---|
| TypeScript | 6.0 | **5.9** | `openapi-typescript` yalnızca TS 5.x destekliyor |
| ESLint | 10.x | **9.39** | `eslint-plugin-*` paketleri henüz ESLint 10 desteklemiyor |

Bu, `KR-001`'deki **LTS ve olgunluk** ilkesinin uygulanmasıdır: en yeni sürüm değil,
ekosistemin desteklediği sürüm seçilir. Sürümler ekosistem yetiştiğinde yükseltilecektir.

> **Not:** Vite şablonunun varsayılan linter'ı artık `oxlint`. ADR-0015 ESLint +
> Prettier öngördüğü ve eklenti ekosistemi (özellik izolasyonu kuralı) ESLint'te
> olduğu için `oxlint` kaldırıldı.

---

## 4.4 Konteyner taban imajları

Taban imajlar da **konfigürasyon öğesidir**: sürümleri burada kayıtlıdır ve CI
her PR'da `trivy` ile taranır (ADR-0011 §6).

| İmaj | Sürüm | Lisans | Amaç |
|---|---|---|---|
| `mcr.microsoft.com/dotnet/sdk` | 10.0-noble | MIT | API derleme aşaması |
| `mcr.microsoft.com/dotnet/aspnet` | 10.0-noble | MIT | API çalışma zamanı |
| `node` | 22-alpine | MIT | Frontend derleme aşaması |
| `nginx` | 1.29-alpine | BSD-2-Clause | Statik dosya sunumu |
| `postgres` | 17-alpine | PostgreSQL License | Geliştirme ve UAT veritabanı |

> **Taban imajdaki paketler imaj derlenirken güncellenir** (`KR-065`). Resmî etiketler
> yayımlandıkları andaki paket sürümleriyle dondurulur; ilk taramada web imajında
> **36 yüksek önemli açık** bulundu ve hepsinin yaması mevcuttu.

---

## 5. Güvenlik açığı denetimi

`Directory.Build.props` içinde NuGet denetimi **açık** ve **hata seviyesindedir**:

```xml
<NuGetAudit>true</NuGetAudit>
<NuGetAuditMode>all</NuGetAuditMode>
<NuGetAuditLevel>low</NuGetAuditLevel>
```

Bilinen güvenlik açığı olan bir paket (doğrudan veya geçişli) eklendiğinde **derleme
kırılır**. Bu, ADR-0011 §6'daki "bağımlılık güvenliği" kalite kapısının ilk
savunma hattıdır; CI'da ayrıca taranır.

Ayrıca **geçişli sürüm sabitleme** açıktır
(`CentralPackageTransitivePinningEnabled`): dolaylı bağımlılıkların sürümü de bu
belgede kayıtlı sürümlere sabitlenir.

---

## 6. Paket ekleme kuralı

Yeni bir paket eklenmeden önce:

1. **Lisansı kontrol edilir.** Koşullu ticari lisans varsa eklenmez.
2. **Bakım durumu kontrol edilir.** Son sürüm tarihi ve açık sorun sayısı bakılır.
3. `Directory.Packages.props` içine **kesin sürümle** eklenir.
4. Bu belgeye **lisansı ve amacıyla** işlenir.
5. Gerçekten gerekli mi diye sorulur — çerçevede karşılığı varsa paket eklenmez (KISS).

Reddedilen bir paket varsa §2 tablosuna gerekçesiyle yazılır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-09 | 0.1 | İlk oluşturma — backend paketleri ve lisansları | Bilgi İşlem |
| 2026-09-10 | 0.2 | Serilog çekirdek paketi eklendi (Infrastructure katmanı günlük yapılandırması) | Bilgi İşlem |
| 2026-09-10 | 0.3 | OpenAPI belge üretimi paketleri eklendi (ApiDescription.Server, Microsoft.OpenApi) | Bilgi İşlem |
| 2026-09-10 | 0.4 | OpenTelemetry çalışma zamanı ölçümü ve OTLP dışa aktarım paketleri eklendi | Bilgi İşlem |
| 2026-09-11 | 0.5 | Frontend paketleri eklendi (§4); TypeScript ve ESLint sürüm sabitleme gerekçeleri yazıldı | Bilgi İşlem |
| 2026-09-11 | 0.6 | §4.4 konteyner taban imajları eklendi | Bilgi İşlem |
| 2026-09-11 | 0.7 | Tarih seçimi paketleri eklendi; MUI X Pro bileşenleri reddedilenler tablosuna işlendi | Bilgi İşlem |
