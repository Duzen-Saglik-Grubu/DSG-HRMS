# ADR-0023 — PDF Raporlarının SkiaSharp ile Üretilmesi

**Durum:** **Kabul Edildi**
**Tarih:** 2026-11-04 (karar) · 2026-11-14 (ADR olarak kayda geçti, #389)
**Karar defteri karşılığı:** `KR-131`; sınıflandırma `KR-141`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)
**Değiştirdiği:** ADR-0014 §1 tablosunun PDF satırı ve ADR-0001 teknoloji tablosunun PDF satırı: "PDFsharp / MigraDoc 6.x (MIT)". ADR-0014'ün geri kalanı (Excel için ClosedXML, QuestPDF reddi, raporlama mimarisi, zorunlu üst bilgi, `export` izni ve dışa aktarma kaydı) yürürlüktedir.
**İlgili iş:** #330 (dışa aktarma), #389 (ADR olarak kayda geçme)

---

## Bağlam

ADR-0001 ve ADR-0014, PDF için PDFsharp/MigraDoc 6.x'i seçmişti; gerekçe MIT lisansı ve
Linux uyumuydu. T1'de dışa aktarma yazılırken (#330) paketin **6.5.0 sürümünden (14.10.2026)
itibaren AGPL-3.0** lisanslı olduğu görüldü. AGPL, ağ üzerinden sunulan uygulamanın kaynak
kodunu açma yükümlülüğü getirir; kurumun "tamamen ücretsiz ve koşulsuz lisans" koşuluna
(`KR-025`, ADR-0001) uymaz. 6.5.0 öncesine sabitlemek, güvenlik güncellemesi alamayan bir
pakete bağlanmak demekti.

Aynı inceleme, ClosedXML'in yazı tipi ölçüm bağımlılığı SixLabors.Fonts'un 2.x'ten itibaren
ciroya bağlı lisansa geçtiğini de gösterdi.

Karar 04.11.2026'da `KR-131` ile alındı ve ADR-0014 §1'e yerinde işlendi. `KR-099` (2),
yerine başka bir çözüm koyan değişikliğin yeni ADR ile yapılmasını ister; T1 geliştirme sonu
denetimi bunun atlandığını buldu (TEC.5-10). Bu ADR, kararı kendi gerekçesiyle kayda geçirir
(`KR-141`).

## Karar

**PDF raporları SkiaSharp ile çizilir; PDFsharp/MigraDoc kullanılmaz. ClosedXML'in yazı tipi
bağımlılığı SixLabors.Fonts 1.x'e sabitlenir.**

- PDF, projede fotoğraf küçültme için zaten bulunan **SkiaSharp** (MIT) ile çizilir
  (`SkiaSharp` ve `SkiaSharp.NativeAssets.Linux.NoDependencies`). Yeni bir paket eklenmez.
- Yazıcı, ADR-0014 §2'deki soyutlamanın arkasındadır: `IReportWriter` →
  `PdfReportWriter` (`Dsg.Hrms.Infrastructure/Reports`). Tablo, satır kaydırma, sayfalama
  ve zorunlu üst bilgi (ADR-0014 §3) bu ortak yazıcıda bir kez çizilir.
- Türkçe karakter için **Noto Sans** (OFL; normal ve kalın) uygulamaya gömülür; Linux'ta
  sistem yazı tipine bağlı kalınmaz.
- **SixLabors.Fonts 1.0.1**'e (Apache-2.0) sabitlenir (`Directory.Packages.props`); 2.x ve
  sonrası eklenmez.

## Gerekçe

- **Lisans:** SkiaSharp MIT, SixLabors.Fonts 1.x Apache-2.0; ikisi de koşulsuzdur (`KR-025`).
- **Bağımlılık sayısı:** SkiaSharp, fotoğraf küçük hâli için zaten projededir (ADR-0021 §5).
  PDF için ikinci bir grafik yığını eklenmez; güncellenecek ve taranacak paket sayısı artmaz.
- **Linux:** Bağımlılıksız yerel paket, konteynerde ek sistem kütüphanesi istemez.
- **Geri dönüş:** Yazıcı `IReportWriter`'ın arkasında olduğu için kütüphane değişimi tek
  sınıfı etkiler.

## Değerlendirilen alternatifler

| Alternatif | Artıları | Eksileri | Neden seçilmedi |
|---|---|---|---|
| PDFsharp / MigraDoc ≥ 6.5 | Belge düzeni API'si; tablo hazır | AGPL-3.0 | Lisans koşulu (`KR-025`) |
| PDFsharp / MigraDoc < 6.5'e sabitlemek | Kod değişmez | Güvenlik güncellemesi alamaz | Bakımı olmayan pakete bağlanmak |
| QuestPDF | Zengin düzen API'si | Ciroya bağlı Community lisansı | `KR-025` |
| HTML → PDF (headless tarayıcı) | HTML şablonu | Chromium bağımlılığı, konteyner boyutu, bellek | ADR-0014'teki gerekçe |
| SkiaSharp | MIT; projede zaten var; Linux bağımlılıksız | Düzen elle çizilir; alt kümeleme yok | **Seçildi** |

## Sonuçlar

**Olumlu:**
- Lisans riski kalmadı; yeni grafik yığını eklenmedi.
- Rapor biçimi (ADR-0014 §2–§3) değişmedi; kararın etkisi tek yazıcı sınıfında.

**Olumsuz / kabul edilen ödünler:**
- SkiaSharp bir belge düzeni kütüphanesi değildir; tablo, satır kaydırma ve sayfalama elle
  çizilir. Bu maliyet `PdfReportWriter`'da bir kez ödendi.
- Bağımlılıksız yerel paket yazı tipini alt kümelemez; her PDF'e yaklaşık **0,65 MB** sabit ek
  gelir (ADR-0014 §1 revizyon notu).

**Bu kararın getirdiği yükümlülükler:**
- SixLabors.Fonts'un 1.x sabitlemesi `Directory.Packages.props`'taki açıklamayla korunur;
  Dependabot'un 2.x önerisi kabul edilmez.
- Yeni bir PDF raporu, ortak yazıcıyı kullanır; kendi çizimini yapmaz.

## Geri dönüş maliyeti

**Düşük.** `IReportWriter` soyutlaması sayesinde başka bir PDF kütüphanesine geçiş yalnızca
`PdfReportWriter`'ı etkiler.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-11-14 | 1.0 | İlk oluşturma: `KR-131` (04.11.2026) ADR olarak kayda geçti; ADR-0014 §1'deki yerinde revizyon bu ADR'ye bağlandı (`KR-141`, #389) | Bilgi İşlem |
