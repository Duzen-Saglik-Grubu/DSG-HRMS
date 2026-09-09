# ADR-0001 — Teknoloji Yığını

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-001`, `KR-005`, `KR-006`, `KR-025`, `KR-026`, `KR-057`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)

---

## Bağlam

DSG-HRMS'in **10–15 yıl** kullanılması hedefleniyor. Bu süre, teknoloji seçimlerinde
"bugün en hızlısı" yerine "yarın da desteklenecek olanı" tercih etmeyi gerektiriyor.
Kurum tarafından konulan seçim ölçütleri:

- Uzun süreli destek (LTS)
- Yüksek bakım kolaylığı ve okunabilirlik
- Güçlü topluluk desteği
- **Düşük tedarikçi bağımlılığı** — lisans değişikliğiyle kilitlenme riski olmamalı
- Temiz mimariyle uyum, test edilebilirlik, gözlemlenebilirlik
- Microsoft ekosistemiyle uyum (kurumda mevcut yetkinlik)
- Linux üzerinde çalışabilme (üretim ortamı Linux olacak)

## Karar

Aşağıdaki yığın kullanılacaktır.

### Backend

| Bileşen | Seçim | Not |
|---|---|---|
| Çalışma zamanı | **.NET 10 (LTS)** | Kasım 2025 LTS sürümü; 3 yıl destek |
| Web çerçevesi | **ASP.NET Core Web API** | |
| ORM | **Entity Framework Core 10** + **Npgsql** sağlayıcısı | |
| Nesne eşleme | **Mapperly** (MIT, kaynak üreteci) | AutoMapper **kullanılmayacak** |
| Doğrulama | **FluentValidation** | |
| Loglama | **Serilog** + yapılandırılmış log | |
| Gözlemlenebilirlik | **OpenTelemetry** (izleme, ölçüm) + `/health` uç noktaları | |
| Zamanlanmış işler | **Hosted Service** (LOGO senkronizasyonu vb.) | Harici zamanlayıcı bağımlılığı yok |
| Dayanıklılık | **Microsoft.Extensions.Http.Resilience** | Yeniden deneme, zaman aşımı, devre kesici |
| Excel | **ClosedXML** (MIT) | |
| PDF | **PDFsharp / MigraDoc 6.x** (MIT) | QuestPDF **kullanılmayacak** |
| Birim test | **xUnit** + **Shouldly** + **NSubstitute** | FluentAssertions **kullanılmayacak** (`KR-057`) |
| Mimari denetimi | **NetArchTest.Rules** | Katman ve modül kurallarını otomatik denetler (ADR-0002) |
| Entegrasyon test | **Testcontainers** (gerçek PostgreSQL) | In-memory sağlayıcı kullanılmayacak |

**Kullanılmayacaklar ve gerekçeleri:**

- **AutoMapper** — ticari lisansa geçti. "Düşük tedarikçi bağımlılığı" ilkesine aykırı.
- **MediatR** — ticari lisansa geçti. Sağladığı fayda kendi servis katmanımızla karşılanır.
- **QuestPDF** — Community lisansı ciro eşiğine bağlı; koşullu lisans istenmiyor.
- **EF Core In-Memory sağlayıcısı** — gerçek veritabanı davranışını taklit etmediği için
  yanlış güven verir.
- **FluentAssertions (8.x ve üzeri)** — koşullu ticari lisansa geçti. Yerine **Shouldly**
  (BSD-3-Clause) kullanılacaktır (`KR-057`). Bu, AutoMapper ve QuestPDF ile aynı ilkedir.

### Frontend

| Bileşen | Seçim | Not |
|---|---|---|
| Çalışma zamanı | **Node.js 24 (LTS)** | |
| Kütüphane | **React 19** | |
| Dil | **TypeScript** (`strict` açık) | |
| Derleyici / sunucu | **Vite** | |
| Yönlendirme | **React Router** | Tek sayfa uygulama (SPA) |
| Sunucu durumu | **TanStack Query** | Önbellek, yeniden deneme, eşzamanlılık |
| İstemci durumu | **Zustand** (yalnızca gerektiğinde) | Genel durum yönetimi zorunlu değil |
| HTTP istemcisi | **Axios** | Ortak hata ve oturum ara katmanı için |
| Form | **React Hook Form** + **Zod** | Şema doğrulaması; backend kurallarıyla hizalı |
| Arayüz | **Material UI (MUI)** | |
| Bildirim | **Notistack** | |
| Tarih | **Day.js** | |
| Çoklu dil | **i18next** + **react-i18next** | Başlangıç TR (`KR-039`) |
| Birim test | **Vitest** + **React Testing Library** | |
| Uçtan uca test | **Playwright** | Kritik akışlar |
| Kod kalitesi | **ESLint** + **Prettier** | |

### Veritabanı ve altyapı

| Bileşen | Seçim |
|---|---|
| Veritabanı | **PostgreSQL** (güncel kararlı sürüm, ayrı sunucu — `KR-035`) |
| Kaynak sistem | LOGO Bordro / MSSQL — **salt okunur** (`KR-003`) |
| Paketleme | **Docker** + Docker Compose |
| Sürekli tümleştirme | **GitHub Actions** |
| Virüs tarama | **ClamAV** (konteyner) |
| Üretim işletim sistemi | **Linux** |

### Sürüm sabitleme

Tüm paketlerin **kesin sürümleri** iskelet oluşturulurken sabitlenecek
(`Directory.Packages.props` ile merkezî sürüm yönetimi; frontend'de `package-lock.json`).
Bu ADR yalnızca ana sürümleri ve seçimleri belirler; sürüm envanteri
`docs/mimari/paket-envanteri.md` altında tutulur ve MAN.5 kapsamında konfigürasyon
öğesidir.

## Gerekçe

- **.NET 10 LTS** üç yıl destekli; kurumda mevcut .NET yetkinliği var; Linux'ta birinci
  sınıf çalışıyor. .NET 9 STS olduğu için tercih edilmedi.
- **Mapperly**, derleme zamanında eşleme kodu üretir: çalışma zamanı maliyeti yoktur,
  hatalı eşleme **derleme hatası** olarak çıkar (AutoMapper'da çalışma zamanında sessizce
  `null` üretebilir) ve üretilen kod okunabilir/hata ayıklanabilir.
- **PostgreSQL**, mevcut sistemde de kullanılıyor; ekip aşina, lisans maliyeti yok,
  tarih aralıklı veri ve JSON desteği güçlü.
- **TanStack Query**, sunucu durumunu istemci durumundan ayırarak Redux benzeri ağır
  durum yönetimi ihtiyacını ortadan kaldırıyor; İK uygulaması ağırlıklı olarak
  "sunucudan oku, göster, güncelle" örüntüsünde.
- **MUI**, hazır ve erişilebilir bileşen seti ile arayüz kalitesini yüksek tutarken
  geliştirme süresini kısaltıyor; kurumun tercihi de bu yönde.

## Değerlendirilen alternatifler

| Alternatif | Artıları | Eksileri | Neden seçilmedi |
|---|---|---|---|
| .NET 9 | Daha yeni özellikler | **STS**, kısa destek | 10–15 yıllık hedefe uygun değil |
| Java / Spring Boot | Güçlü ekosistem, LTS | Kurumda yetkinlik yok | Bakım maliyeti ve öğrenme eğrisi |
| Node.js backend | Tek dil (TS) | Büyük iş kuralı katmanında tip güvenliği ve olgunluk açısından .NET'in gerisinde | Ekip yetkinliği ve ekosistem uyumu |
| AutoMapper | Yaygın, bilinen | Ticari lisans, çalışma zamanı maliyeti, sessiz hatalar | Lisans ve kalite riski |
| Angular | Bütünleşik çerçeve | Daha dik öğrenme eğrisi, daha ağır | React'te ekip yetkinliği ve topluluk büyüklüğü |
| Redux Toolkit | Olgun | İK uygulaması için gereğinden ağır | TanStack Query yeterli (KISS, YAGNI) |
| QuestPDF | En iyi tablo/düzen API'si | Koşullu ticari lisans | Kurum koşullu lisans istemiyor |
| FluentAssertions 8.x | Çok yaygın, okunabilir iddialar | **Koşullu ticari lisans** | Aynı ilke; Shouldly aynı okunabilirliği ücretsiz sağlıyor |
| FluentAssertions 7.x (son ücretsiz sürüm) | Ücretsiz (Apache-2.0) | Sürüm dondurulmuş; güvenlik ve uyumluluk güncellemesi almayacak | 10–15 yıllık ufukta terk edilmiş bağımlılık kabul edilemez |
| AwesomeAssertions (FluentAssertions 7 çatallaması) | Ücretsiz, geçiş kolay | Genç proje, tek kaynaklı bakım | Shouldly daha köklü ve topluluğu geniş |

## Sonuçlar

**Olumlu:**
- Tüm bileşenler ücretsiz ve izin verici lisanslı (MIT / Apache 2.0 / açık kaynak).
- Tedarikçi kilitlenmesi riski minimum; hiçbir bileşen ciro eşiğine bağlı değil.
- Linux ve konteyner uyumu baştan sağlanmış.

**Olumsuz / kabul edilen ödünler:**
- PDFsharp/MigraDoc, QuestPDF'e göre daha düşük seviyeli bir API sunar; karmaşık tablo
  düzenleri için daha fazla kod yazılacaktır. Türkçe karakterler için font açıkça
  gömülecek ve Linux'ta font çözücü tanımlanacaktır.
- Mapperly ekipte AutoMapper kadar bilinmiyor; kısa bir alışma dönemi olacaktır.

**Yükümlülükler:**
- Paket sürümleri sabitlenecek ve `docs/mimari/paket-envanteri.md` güncel tutulacak.
- Bağımlılık güncellemeleri Dependabot ile izlenecek; güvenlik güncellemeleri öncelikli.
- Yeni bir paket eklenmeden önce lisansı kontrol edilecek; koşullu ticari lisanslı
  paket eklenmeyecektir.

## Geri dönüş maliyeti

- Kütüphane düzeyinde (Mapperly, PDF, Excel): **Düşük** — soyutlama arkasında kullanıldığı
  için tek noktadan değiştirilebilir.
- Çalışma zamanı ve veritabanı düzeyinde: **Yüksek** — bu seçimler projenin temelidir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
| 2026-09-09 | 0.2 | FluentAssertions lisans değişikliği nedeniyle Shouldly ile değiştirildi (`KR-057`) | Bilgi İşlem |
