# TEC.5 — Süreç Yaklaşımı

**Belge kimliği:** TEC.5-YAK
**Süreç:** TEC.5 — Tasarım Tanımlama
**Son güncelleme:** 2026-10-05
**Karşıladığı öznitelik maddeleri:** `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir. `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir. Ayrıca `PA 2.1` için hedef, sorumluluk, kaynak ve arayüzler (§1, §3, §4).

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Tasarım kararlarının
> kendisi ADR'lerde ve karar defterindedir; bu belge onların nasıl üretildiğini ve
> korunduğunu anlatır.

---

## 1. Sürecin amacı ve sınırı

Tasarım tanımlama, sistem öğelerinin **tasarım özelliklerini, gerekçesini ve
arayüzlerini** tanımlar ve sistem gereksinimlerini bu öğelere dağıtır
(TS ISO/IEC TS 33061, Madde 5, TEC.5).

**Kapsamdadır:** katman ve modül yapısı, veri modeli kuralları, öğeler arası ve dış
arayüzler, teknoloji ve kütüphane seçimi, güvenlik ve KVKK etkili tasarım kararları,
gereksinim yorumları.

**Kapsamda değildir:** gereksinimin kendisi (TEC.3) ve kodun yazılması (TEC.7).
Bir metodun adı ya da bir bileşenin yerleşimi tasarım kararı değildir; kodda ve kod
yorumunda kalır (`docs/adr/README.md`, "Ne zaman ADR yazılır?").

### 1.1 Tasarım bilgisi nerede durur?

| Düzey | Örnek | Yer |
|---|---|---|
| Mimari karar ve gerekçesi | Modüler monolit, LOGO'ya salt okuma, JWT + yenileme jetonu | `docs/adr/ADR-NNNN-*.md` |
| Kurumsal veya teknik karar ("ne") | `KR-084` denetleyici + FluentValidation, `KR-094` kimlik olayları tablosu | `docs/karar-kayit-defteri.md` |
| Gereksinim yorumu | AN-01…AN-24 (ör. AN-05: kilit sayacı girilen e-postaya bağlı) | `SYG-<MODÜL>.md` §6 |
| Öğeler ve gereksinim tahsisi | Öğe → konum tablosu | `SYG-<MODÜL>.md` §2.2 |
| Arayüz | HTTP API sözleşmesi; LOGO, NetGSM, SMTP, tarayıcı | `docs/api/openapi-v1.json`; `SYG-<MODÜL>.md` §2.3 |
| Yardımcı mimari belgeler | Modül listesi, izin listesi, paket envanteri | `docs/mimari/` |

### 1.2 Hedefler

- Birleştirilen her iş paketinin tasarım kararı, kodla **aynı PR'da** kayda geçmiş olur.
- Mimari kurallar elle değil, **testle** korunur (ADR-0002, §4).
- Bir kararı tersine çeviren değişiklik eski kaydı silmez; yeni ADR açar (`KR-099`).

---

## 2. Süreç nasıl işletilir?

### 2.1 Proje başında (A0)

On beş ADR, 2026-09-06'da proje genelindeki kararlar için yazıldı (ADR-0001…0015).
Modüller bu kararların üzerine kurulur. README'deki "Birbirini etkileyen kararlar" ve
"Gözden geçirme tetikleyicileri" tabloları, bir ADR değişmeden önce okunur.

### 2.2 Modül başında

SYG belgesi yazılırken (TEC.3) iki tasarım girdisi de üretilir:

- **§2.2 Öğeler:** modülün hangi öğelerden oluştuğu ve her birinin katmandaki yeri
  (ör. "Kimlik API'si → `Dsg.Hrms.Api`, `/api/v1/identity/`"). Gereksinimler bu
  düzeyde öğelere dağıtılır.
- **§2.3 Dış arayüzler:** yön, protokol ve kısıt (ör. LOGO yalnızca okuma, `KR-003`).

### 2.3 Her iş paketinde

| Adım | Ne yapılır |
|---|---|
| 1. İş paketi | Issue başlığı karşıladığı SYG aralığını taşır (ör. #74 "SYG-KMLK-001…012", #119 "SYG-KMLK-060/061/062") |
| 2. Karar | Geri dönüşü pahalı, alışılmışın dışında ya da güvenlik/KVKK etkili bir karar varsa yazılır: karar defterine `KR-` satırı ("ne"), ilgili ADR'ye **gerçekleştirme notu** ("neden"). Örnek: ADR-0006 v0.5–v1.0 (`KR-085`, `KR-086`, `KR-087`, `KR-088`, `KR-090`, `KR-092`) |
| 3. Yorum | Gereksinim metninde açık kalan nokta SYG §6'ya `AN-` olarak yazılır. Paydaş gereksiniminin anlamını etkiliyorsa İK'ya götürülür (TEC.3 YAKLASIM §4; örnek #135, AN-23 ve AN-24) |
| 4. Arayüz | API değiştiyse OpenAPI belgesi koddan yeniden üretilir ve frontend tipleri yenilenir (ADR-0010 §1) |
| 5. İzlenebilirlik | İzlenebilirlik matrisinin "Tasarım" sütunu aynı PR'da doldurulur (ADR bölümü ve `KR-`) |
| 6. Gözden geçirme | PR incelemesi tasarımı da kapsar; onay, birleştirmeden önce PR'a yorum olarak yazılır (`KR-096`) |

**Fiilî durum:** Tasarım koddan **önce ayrı bir PR'da** yazılmaz; kararla kod aynı
PR'da gelir. Tek geliştiricili düzende bu, tasarımın koddan kopmamasını sağlar. Bedeli
şudur: değerlendirilen alternatifler kod yazıldıktan sonra kaydedilir ve tasarımı
koddan bağımsız gözden geçiren bir adım yoktur (§8).

### 2.4 ADR değişikliği (`KR-099`)

| Değişiklik | Nasıl yapılır |
|---|---|
| Kararı genişleten veya netleştiren (gerçekleştirme notu, alt kural, ölçülen değer) | ADR'nin içinde; değişiklik geçmişine tarih, sürüm ve `KR-NNN` yazılır |
| Kararı tersine çeviren veya yerine başka çözüm koyan | Yeni ADR; eskisi silinmez, durumu `Yerini aldı: ADR-NNNN` olur |
| Hangisi olduğu belirsiz | Yeni ADR |

Her durumda README dizini (durum ve "Son değişiklik" sütunu) aynı PR'da güncellenir.

---

## 3. Roller ve kaynaklar (`PA 2.1`)

| Rol | Sorumluluk |
|---|---|
| Bilgi İşlem (R1) | Tasarım kararlarını alır, yazar ve onaylar (RACI: TEC.5 **A**/R) |
| KVKK Sorumlusu (P6) | Kişisel veri etkili kararlarda danışılır (RACI: C) |
| İK (R2) | Gereksinim yorumu paydaş gereksiniminin anlamını etkilediğinde teyit eder; tasarım sonucundan bilgilendirilir (RACI: I) |

RACI: `docs/33061/MAN.1-proje-planlama/roller-ve-sorumluluklar.md` §3.1. Şablon:
`docs/sablonlar/ADR-sablonu.md`. Tek kişiye bağımlılık ve bağımsız olmayan inceleme
riskleri R-05 ve R-23 olarak izlenir.

---

## 4. Destekleyici sistemler ve otomatik kontroller

| Kontrol | Neyi korur | Yer |
|---|---|---|
| Mimari testleri (NetArchTest) | Katman bağımlılıkları, LOGO erişiminin `Logo` klasöründe kalması, kimlik uç noktalarının yolu | `src/backend/tests/Dsg.Hrms.Architecture.Tests/` |
| API sözleşme denetimi | Koddan üretilen OpenAPI belgesi depodakiyle aynı | `.github/workflows/api-contract-check.yml` |
| Üretilen API tipleri | Frontend tipleri OpenAPI belgesiyle aynı | `ci.yml`, "Uretilen API tipleri guncel mi" |
| Migration denetimi | EF modeli ile migration'lar arasında bekleyen değişiklik yok (ADR-0004) | `ci.yml`, `migration-check` |

---

## 5. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Tasarım gerekçesi | `docs/adr/ADR-NNNN-<kisa-baslik>.md` | Şablon bölümleri: Bağlam, Karar, Gerekçe, Değerlendirilen alternatifler, Sonuçlar, Geri dönüş maliyeti, Değişiklik Geçmişi | Bilgi İşlem |
| ADR dizini | `docs/adr/README.md` | Numara, durum, son değişiklik, konu; etkileşim ve tetikleyici tabloları | Bilgi İşlem |
| Karar kaydı | `docs/karar-kayit-defteri.md` | `KR-NNN`, tarih, karar, ayrıntı, gerekçe, karar veren, durum | Karar veren |
| Gereksinim yorumları | `SYG-<MODÜL>.md` §6 | `AN-nn`: bulgu, çözüm, durum | Bilgi İşlem |
| Öğeler ve dış arayüzler | `SYG-<MODÜL>.md` §2.2, §2.3 | Tablo | Bilgi İşlem |
| API sözleşmesi | `docs/api/openapi-v1.json` | Koddan üretilir; elle düzenlenmez | PR sahibi |
| Veri modeli | `src/backend/Dsg.Hrms.Infrastructure/Data/Configurations/`, `Migrations/` | EF Core yapılandırması ve migration'lar (ADR-0004 kuralları) | PR sahibi |
| Mimari belgeler | `docs/mimari/` | Modül listesi, izin listesi, paket envanteri, vizyon | Bilgi İşlem |
| İzlenebilirlik | `../izlenebilirlik-matrisi.md` "Tasarım" sütunu | `ADR-nnnn` §, `KR-` | PR sahibi |
| Tasarım gözden geçirme raporu | `raporlar/YYYY-AA-GG-tasarim-gozden-gecirme.md` | Doküman haritasında öngörülü; **henüz üretilmedi** (§8) | Bilgi İşlem |

---

## 6. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** ADR, karar defteri, SYG ve `docs/mimari/` yalnızca PR ile değişir;
  PR onayı birleştirmeden önce yorum olarak yazılır ve `main`'de denetlenir (`KR-096`).
- **Kimlik:** `ADR-NNNN` ve `KR-NNN` numaraları yeniden kullanılmaz.
- **Geçmişin korunması:** ADR değişiklik kuralı §2.4 (`KR-099`). Karar defterinde
  yürürlükten kalkan karar silinmez, durumu değişir.
- **Sözleşmenin tek kaynağı:** OpenAPI belgesi koddan üretilir; fark CI'da PR'ı durdurur.
- **Mimari kuralların korunması:** kural ihlali mimari testini kırar; PR birleştirilemez.
- **Tarih ve sürüm:** her ADR'de ve README'de değişiklik geçmişi bulunur.

---

## 7. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (TEC.5) | Karşılığı | Durum |
|---|---|---|
| a) Her öğenin tasarım özellikleri tanımlanır | ADR'ler, gerçekleştirme notları, `docs/mimari/` | Var; diyagram yok |
| b) Gereksinimler öğelere dağıtılır | SYG §2.2 (öğe düzeyi), issue başlıklarındaki SYG aralıkları | Kısmen; SYG → öğe tablosu yok |
| c) Tasarımı mümkün kılan araç ve kararlar | ADR-0001, `docs/mimari/paket-envanteri.md`, `KR-084` | Var |
| d) Öğeler arası arayüzler tanımlanır | OpenAPI + sözleşme denetimi; SYG §2.3 | Var |
| e) Tasarım alternatifleri değerlendirilir | ADR "Değerlendirilen alternatifler"; `KR-` gerekçe sütunu | Var |
| f) Tasarım ürünleri geliştirilir | ADR güncellemeleri, migration'lar, izin listesi | Var; kodla aynı PR'da |
| g) Destekleyici sistemler mevcut | §4 | Var |
| h) Tasarım ile mimari öğeler arasında izlenebilirlik | Matris "Tasarım" sütunu (REQ → ADR §) | Kısmen; SYG düzeyinde değil |

---

## 8. Açık noktalar

| No | Açık nokta | Bağlantı |
|---|---|---|
| 1 | **Modül yalıtım testi fiilen boş geçiyor.** `Modules_should_not_depend_on_each_others_internals` modülleri `<Katman>.Modules.<Modül>` ad alanında arar; kod ise `Dsg.Hrms.Application.Identity` gibi `Modules` olmadan yerleşmiş. ADR-0002'nin `Modules/<Modül>/Abstractions/` düzeni uygulanmadı ve bu sapma ADR'de yazılı değil. Ya ADR-0002'ye gerçekleştirme notu ya teste yeni ad alanı kuralı gerekir | ADR-0002, `LayerDependencyTests.cs` |
| 2 | SYG → tasarım öğesi tahsis tablosu yok; matrisin "Tasarım" sütunu REQ düzeyinde | Denetim TEC.5-3 |
| 3 | Tasarım gözden geçirme raporu hiç üretilmedi; tasarımı koddan bağımsız gözden geçiren adım yok | Denetim TEC.5-6 |
| 4 | Depoda diyagram yok (hesap/oturum durumu, üyelik akışı); `docs/mimari/entegrasyon-arayuzleri.md` yok | Denetim TEC.5-5 |
| 5 | `KR-093` (API'de HTTPS zorunluluğu) hiçbir ADR'de geçmiyor; T3'ün mimari nitelikli kararlarının ADR'ye alınıp alınmayacağı kararlaştırılmadı | Denetim TEC.5-4 |

---

## 9. Gözden geçirme

Bu belge her modül sonu süreç denetiminde (MAN.8) ve yeni bir ADR yazıldığında gözden
geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
