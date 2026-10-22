# TEC.5 — Süreç Yaklaşımı

**Belge kimliği:** TEC.5-YAK
**Süreç:** TEC.5 — Tasarım Tanımlama
**Son güncelleme:** 2026-10-22
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
| Öğeler ve gereksinim tahsisi | Öğe → konum ve SYG aralığı tablosu; madde madde SYG → öğe ve bileşen tahsisi | `SYG-<MODÜL>.md` §2.2 ve §2.5 |
| Durum ve akış diyagramları | Hesap ve oturum durumları, üyelik akışı (Mermaid, koddan çıkarılmış ve kanıtlı) | `docs/mimari/<modül>-tasarim-diyagramlari.md` (T3: `kimlik-tasarim-diyagramlari.md`) |
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

### 2.5 Tasarım gözden geçirmesi (#234)

Her modülün tasarımı **ilk kod PR'ından önce** bir kez gözden geçirilir. İlk örnek T3 için
yapıldı ve T1'e girdi oldu (`raporlar/2026-10-20-tasarim-gozden-gecirme.md`).

| Adım | Ne yapılır |
|---|---|
| 1. Kapsam | Bilgi İşlem, modülün ADR'lerini, SYG §2.2–§2.5'i ve ilgili kodu belirler |
| 2. Bağımsız inceleme | Ayrı bir alt ajan ADR'leri ve kodu okur. Önceki denetim raporlarını ve Bilgi İşlem görüşünü görmeden çalışır. Bulgular raporda değiştirilmeden ayrı bölümde gösterilir (Doğuş Uçanok'un kararı, 15.10.2026) |
| 3. Tahsis ve diyagram | SYG → öğe tahsisi ve durum/akış diyagramları güncellenir |
| 4. Değerlendirme | Bilgi İşlem bulguları doğrular. Her bulgu ya bir issue'ya bağlanır ya da gerekçeyle kabul edilir (MAN.8 YAKLAŞIM §2.3) |

**Uygulamalar:** T3 (`raporlar/2026-10-20-tasarim-gozden-gecirme.md`), T1 (`raporlar/2026-10-22-t1-tasarim-gozden-gecirme.md`; dört yüksek önemli bulgu, ADR revizyonu #268).

**Bağımsızlık sınırı:** Alt ajan, geliştirme yardımcısıyla aynı model ailesindendir.
İnceleme kurum dışı veya insan bağımsızlığı sağlamaz; kalan risk R-23'tür.

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
| Tasarım gözden geçirme raporu | `raporlar/YYYY-AA-GG-tasarim-gozden-gecirme.md` | Kapsam, yöntem ve bağımsızlık sınırı, Bilgi İşlem bulguları (BG), bağımsız inceleme bulguları (B), sonraki modüle girdi (§2.5). İlki: `2026-10-20` | Bilgi İşlem; bağımsız bölüm alt ajan |
| Durum ve akış diyagramları | `docs/mimari/<modül>-tasarim-diyagramlari.md` | Mermaid; her geçişin altında dosya ve satır kanıtı | Bilgi İşlem |

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
| a) Her öğenin tasarım özellikleri tanımlanır | ADR'ler, gerçekleştirme notları, `docs/mimari/` (T3 diyagramları dahil) | Var (diyagramlar #234) |
| b) Gereksinimler öğelere dağıtılır | SYG §2.2 (öğe → SYG aralığı) ve §2.5 (SYG → öğe ve bileşen) | Var (#234) |
| c) Tasarımı mümkün kılan araç ve kararlar | ADR-0001, `docs/mimari/paket-envanteri.md`, `KR-084` | Var |
| d) Öğeler arası arayüzler tanımlanır | OpenAPI + sözleşme denetimi; SYG §2.3 | Var |
| e) Tasarım alternatifleri değerlendirilir | ADR "Değerlendirilen alternatifler"; `KR-` gerekçe sütunu | Var |
| f) Tasarım ürünleri geliştirilir | ADR güncellemeleri, migration'lar, izin listesi | Var; kodla aynı PR'da |
| g) Destekleyici sistemler mevcut | §4 | Var |
| h) Tasarım ile mimari öğeler arasında izlenebilirlik | Matris "Tasarım" sütunu (REQ → ADR §); SYG düzeyinde SYG §2.5 | Var (#234); matristeki eksik ADR atıfları #246 |

---

## 8. Açık noktalar

| No | Açık nokta | Bağlantı |
|---|---|---|
| 1 | ~~Modül yalıtım testi fiilen boş geçiyor~~ — **Kapandı (05.10.2026, #171):** test kodun gerçek düzenine uyarlandı; modüller arası bağımlılıklar gerekçeli bir tabloda bildiriliyor ve ölçülen kümeyle birebir karşılaştırılıyor (ADR-0016, `KR-100`) | ADR-0016 |
| 2 | ~~SYG → tasarım öğesi tahsis tablosu yok~~ — **Kapandı (20.10.2026, #234):** SYG-KMLK §2.2 ve §2.5 | Denetim TEC.5-3 |
| 3 | ~~Tasarım gözden geçirme raporu hiç üretilmedi~~ — **Kapandı (20.10.2026, #234):** ilk rapor yazıldı; adım §2.5'te tanımlandı. Bulgular #244, #245, #246 | Denetim TEC.5-6 |
| 4 | ~~Depoda diyagram yok (hesap/oturum durumu, üyelik akışı)~~ — **Kapandı (20.10.2026, #234):** `docs/mimari/kimlik-tasarim-diyagramlari.md`. ~~`docs/mimari/entegrasyon-arayuzleri.md` yok~~ — bu kısım **kapandı (15.10.2026):** ayrı belge yazılmaz; entegrasyon arayüzleri OpenAPI belgesi, SYG §2.3 ve TEC.8 entegrasyon raporundadır (doküman haritası, TEC.5 ve TEC.8) | Denetim TEC.5-5 → #234; belge kısmı `KR-111` |
| 5 | ~~`KR-093` (API'de HTTPS zorunluluğu) hiçbir ADR'de geçmiyor; T3'ün mimari nitelikli kararlarının ADR'ye alınıp alınmayacağı kararlaştırılmadı~~ — **Kapandı (15.10.2026):** kural §2.3 (adım 2) ve §2.4'tedir; `KR-082` (ADR-0012 §7), `KR-086` (ADR-0006 §8) ve `KR-089` (ADR-0007 §1) gerçekleştirme notları zaten vardı; `KR-093` ADR-0010 §5'e gerçekleştirme notu olarak yazıldı | Denetim TEC.5-4 → `KR-111`, #217 |

---

## 9. Gözden geçirme

Bu belge her modül sonu süreç denetiminde (MAN.8) ve yeni bir ADR yazıldığında gözden
geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
| 2026-10-05 | 1.1 | §8 madde 1 kapandı: ADR-0016 (#171) | Bilgi İşlem |
| 2026-10-15 | 1.2 | §7 ve §8: madde 2, 3 ve 4'ün diyagram kısmı #234'e bağlandı; madde 4'ün entegrasyon belgesi kısmı ve madde 5 kapandı (`KR-111`, #217) | Bilgi İşlem |
| 2026-10-20 | 1.3 | §1.1, §5: SYG §2.5 tahsis ve diyagram belgesi; §2.5 tasarım gözden geçirmesi adımı; §7 a, b, h ve §8 madde 2, 3, 4 kapandı (#234) | Bilgi İşlem |
| 2026-10-22 | 1.4 | §2.5: T1 tasarım gözden geçirmesi uygulandı (#267) | Bilgi İşlem |
