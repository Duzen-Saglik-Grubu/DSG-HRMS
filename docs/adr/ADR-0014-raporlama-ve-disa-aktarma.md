# ADR-0014 — Raporlama ve Dışa Aktarma

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-025`, `KR-026`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)
**İlgili riskler:** `R-03`

---

## Bağlam

İK biriminin vazgeçmeyeceği yetenek **Excel'e aktarmadır**; resmî evrak niteliğindeki
çıktılar için de **PDF** gerekir. Kurumun koşulu: kullanılan paketler tamamen ücretsiz
olmalı ve **Linux'ta çalışmalıdır**.

Ayrıca dışa aktarma, KVKK açısından ayrı bir risktir: veriyi ekranda görmek ile
kurum dışına çıkarabilecek bir dosyaya dönüştürmek aynı şey değildir.

## Karar

### 1. Kütüphaneler

| Biçim | Kütüphane | Lisans |
|---|---|---|
| Excel (`.xlsx`) | **ClosedXML** | MIT |
| PDF | **PDFsharp / MigraDoc 6.x** | MIT |

**QuestPDF kullanılmayacaktır** (`KR-025`): Community lisansı ciro eşiğine bağlıdır;
kurum koşullu lisanslı paket istemiyor.

**Türkçe karakter:** PDF üretiminde font **açıkça gömülecek** (örn. DejaVu Sans veya
Noto Sans) ve Linux için font çözücü tanımlanacaktır. Bu, PDFsharp'ta atlanırsa Türkçe
karakterlerin bozulmasına yol açan bilinen bir noktadır; iskelette bir kez çözülüp
ortak bir yardımcı sınıfa alınacaktır.

### 2. Raporlama mimarisi

```
Uygulama katmanı
   └── IReportGenerator<TRequest>
            ├── ExcelReportGenerator   (ClosedXML)
            └── PdfReportGenerator     (PDFsharp/MigraDoc)
```

- Rapor **verisi** ile rapor **biçimi** ayrılır. Aynı rapor sorgusu hem Excel hem PDF
  üretebilir.
- Rapor tanımları (sütunlar, başlıklar, gruplamalar) veri erişiminden bağımsızdır.
- Ortak bir **kurumsal şablon** kullanılır: başlık, kurum adı, rapor adı, üretim
  tarihi, üreten kullanıcı, sayfa numarası, filtre özeti.

### 3. Her raporda zorunlu üst bilgi

Bu, hem kullanıcı hem denetim açısından gereklidir:

| Alan | Neden |
|---|---|
| Rapor adı | Ne olduğunu belirtir |
| Üretim tarihi ve saati | Verinin hangi ana ait olduğu |
| **Veri anlık görüntüsü tarihi** | LOGO senkronizasyonunun son çalışma anı (ADR-0003) |
| Üreten kullanıcı | Sorumluluk |
| Uygulanan filtreler | Raporun kapsamı — "neden 320 kişi çıktı" sorusunun cevabı |
| Kayıt sayısı | Bütünlük kontrolü |

**Veri anlık görüntüsü tarihi özellikle önemlidir:** personel verisi 15 dakikada bir
senkronize edildiği için, raporun hangi ana ait olduğu belirtilmezse iki farklı
zamanda alınan rapor arasındaki fark açıklanamaz.

### 4. Yetki ve kayıt

- Dışa aktarma için **ayrı izin** gerekir: `<modül>.<kaynak>.export` (ADR-0007 §1).
  Görüntüleme yetkisi dışa aktarma yetkisi anlamına gelmez.
- Rapor verisi, kullanıcının **kapsamına göre filtrelenir** (ADR-0007 §2). Rapor,
  yetki kontrolünü atlayan bir arka kapı değildir.
- **Her dışa aktarma erişim kaydına yazılır** (ADR-0009 §3): kim, ne zaman, hangi
  rapor, hangi filtre, **kaç kayıt**. Kayıt sayısının tutulması, olası bir sızıntı
  incelemesinin ilk sorusudur.

### 5. Büyük raporlar

- Rapor üretimi **eşzamanlı (senkron)** yapılır ve satır sınırı uygulanır
  (varsayılan **50.000 satır**). Sınır aşılırsa kullanıcıdan filtre daraltması istenir.
- Excel üretiminde bellek dostu yazma kullanılır; tüm veri belleğe alınmaz.
- Gerekirse ileride arka planda üretim + hazır olunca bildirim modeline geçilebilir;
  bugünkü hacim (≈584 aktif personel) bunu gerektirmiyor (YAGNI).

### 6. Excel çıktı kuralları

| Konu | Kural |
|---|---|
| Başlık satırı | Dondurulur (freeze pane) |
| Filtre | Otomatik filtre açık |
| Sütun genişliği | İçeriğe göre otomatik |
| TCKN, sicil, telefon | **Metin biçiminde** — Excel'in baştaki sıfırı silmesi ve bilimsel gösterime çevirmesi engellenir |
| Tarih | Türkçe tarih biçimi (`GG.AA.YYYY`) |
| Sayı | Türkçe ondalık ayırıcı |
| Kişisel veri uyarısı | Kişisel veri içeren raporlarda ilk sayfada **KVKK uyarısı** bulunur |

> Son madde bilinçli bir tercihtir: dosya kurum dışına çıktığında, onu açan kişi
> içeriğin KVKK kapsamında olduğunu görür.

### 7. Yazdırma

Ekrandan doğrudan yazdırma için ayrı bir CSS yazdırma stili tanımlanır; İK'nın basit
listeleri PDF üretmeden yazdırabilmesi sağlanır.

## Gerekçe

- ClosedXML ve PDFsharp, MIT lisanslı ve Linux uyumludur; kurumun koşullu lisans
  istememesi kararıyla uyumludur.
- Veri/biçim ayrımı, aynı raporu iki biçimde üretmeyi ve yeni biçim eklemeyi kolaylaştırır.
- `export` izninin ve dışa aktarma kaydının ayrılması, KVKK açısından somut ve
  denetlenebilir bir kontroldür.
- Rapor üst bilgisi, İK'nın "bu rapor ne zamanki veriye ait" sorusunu kalıcı olarak
  çözer.

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| QuestPDF | Koşullu ticari lisans (`KR-025`) |
| EPPlus | 5.x sürümünden itibaren ticari lisans |
| HTML → PDF (headless tarayıcı) | Ağır bağımlılık (Chromium), konteyner boyutu, bellek kullanımı |
| Rapor sunucusu (JasperReports vb.) | Ek sistem, ek bakım; ölçek gerektirmiyor |
| Dışa aktarmayı `view` izniyle vermek | KVKK açısından dışa aktarma ayrı bir risk |
| Sınırsız satır | Bellek ve zaman aşımı sorunları |

## Sonuçlar

**Olumlu:** Lisans riski yok, Linux uyumlu, dışa aktarma denetlenebilir, raporlar
kendi kendini açıklıyor.

**Olumsuz / kabul edilen ödünler:**
- PDFsharp/MigraDoc, karmaşık tablo düzenlerinde QuestPDF'e göre daha fazla kod
  gerektirir. Ortak yardımcı sınıflarla bu maliyet bir kez ödenecektir.
- Eşzamanlı üretim, çok büyük raporlarda kullanıcıyı bekletir; satır sınırı bu riski
  yönetir.

**Yükümlülükler:**
- Türkçe font gömme ve Linux font çözücü, iskelette çözülüp ortak sınıfa alınacaktır.
- Rapor üst bilgisi ortak şablona konacak; her raporda tekrar yazılmayacaktır.
- Dışa aktarma kaydı, kod gözden geçirme kontrol listesinde madde olacaktır.

## Geri dönüş maliyeti

**Düşük.** `IReportGenerator` soyutlaması sayesinde kütüphane değişimi tek uygulamayı
etkiler.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
