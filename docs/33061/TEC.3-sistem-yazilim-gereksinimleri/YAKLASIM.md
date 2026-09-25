# TEC.3 — Süreç Yaklaşımı

**Belge kimliği:** TEC.3-YAK
**Süreç:** TEC.3 — Sistem/Yazılım Gereksinimlerinin Tanımlanması
**Son güncelleme:** 2026-09-25
**Karşıladığı öznitelik maddeleri:** `PA 2.2 (a)` sürecin dokümante edilmiş bilgi
gereksinimleri belirlenir · `PA 2.2 (b)` bu bilginin kontrol gereksinimleri belirlenir

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Süreç çıktılarının
> kendisi değildir; çıktıların **kuralıdır**.

---

## 1. Sürecin amacı ve sınırı

Bu süreç, onaylı paydaş gereksinimlerini sistemin **doğrulanabilir davranışlarına**
dönüştürür (TS ISO/IEC TS 33061, Madde 5, TEC.3).

**Kapsamdadır:** sistemin ve öğelerinin tanımı, arayüzleri ve sınırları; işlevsel,
performans, arayüz ve işlevsel olmayan gereksinimler ile tasarım kısıtları; kritik
performans ölçütleri; gereksinimlerin analizi; paydaş gereksinimlerine izlenebilirlik.

**Kapsamda değildir:** mimari ve ayrıntılı tasarım (TEC.5), kod (TEC.7). Sistem
gereksinimi "sistem ne yapar"ı paydaş gereksiniminden daha kesin söyler, ama "hangi
sınıf, hangi tablo"yu söylemez.

### 1.1 Sistem ve yazılım gereksinimleri neden tek belgede?

33061'de TEC.3 tek bir süreçtir ve ikisini birlikte kapsar. DSG-HRMS'nin donanım veya
elle işletilen bir öğesi yoktur; tüm öğeler yazılımdır. Ayrı iki belge, aynı
gereksinimi iki kez yazmak olurdu. Sisteme yazılım dışı bir öğe eklenirse bu karar
yeniden değerlendirilir.

---

## 2. Süreç nasıl işletilir?

### 2.1 Başlangıç koşulu

Modülün paydaş gereksinimleri **İK onayından geçmiş ve depoda olmalıdır**
(`KR-068`, TEC.2 YAKLASIM §2.1). Onaylanmamış taslaktan sistem gereksinimi yazılmaz.

### 2.2 Adımlar

| Adım | Ne yapılır | 33061 |
|---|---|---|
| 1. Hazırlık | Sistemin öğeleri, dış arayüzleri ve sınırları yazılır; kapsam dışı olanlar sahibi olan modülle birlikte listelenir | BP1 |
| 2. Tanımlama | Her paydaş gereksinimi bir veya daha fazla sistem gereksinimine dönüştürülür; durumlar ve kipler tanımlanır | BP2 |
| 3. Analiz | Paydaş gereksinimleri tek tek ve **birbirleriyle** karşılaştırılır: belirsizlik, gerilim, ölçülemezlik aranır | BP3 |
| 4. Geri bildirim | Bir paydaş gereksiniminin anlamını etkileyen bulgu paydaşa götürülür; teyit edilmeden kesinleşmiş sayılmaz | BP3 |
| 5. Yönetim | Belge PR ile birleştirilir; izlenebilirlik matrisi aynı PR'da güncellenir | BP4 |

### 2.3 İyi bir sistem gereksinimi

- **Tek anlamlı:** "hızlı", "güvenli", "kolay" gibi sözcükler tek başına kullanılmaz;
  ölçüt veya davranış yazılır.
- **Doğrulanabilir:** her maddenin bir doğrulama yöntemi vardır ve bu yöntem gerçekten
  uygulanabilir.
- **Kaynaklı:** her madde en az bir paydaş gereksinimine bağlıdır. Kaynağı olmayan
  madde onaylanmamış kapsamdır ve yazılmaz.
- **Çözümden bağımsız, kısıt dışında:** gerçekleştirme ancak bir karar veya ADR bunu
  zorunlu kılıyorsa gereksinime girer (ör. LOGO'ya yalnızca okuma arayüzüyle erişim).
  Bu maddeler türü **Kısıt** olarak işaretlenir.

---

## 3. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Çıktı | Dosya | Ne zaman |
|---|---|---|
| **Sistem/yazılım gereksinimleri** | `gereksinimler/SYG-<MODÜL>.md` | Paydaş gereksinimleri onaylandıktan sonra, geliştirmeden önce |
| İzlenebilirlik | `docs/33061/izlenebilirlik-matrisi.md` — "Sistem gereksinimi" sütunu | SYG belgesiyle **aynı PR'da** |
| Analiz bulguları | SYG belgesi §6 | SYG belgesiyle birlikte |

### 3.1 Kimliklendirme

`SYG-<MODÜL>-<no>` — **S**istem/**Y**azılım **G**ereksinimi; numara üç hane:
`SYG-KMLK-013`. Numara yeniden kullanılmaz; iptal edilen madde `İptal` notuyla kalır.

> **Neden `SYG-`?**
>
> - **`REQ-` olamaz:** Belge haritası ilk yazıldığında sistem gereksinimleri için
>   `REQ-<MODÜL>-<no>` öngörülmüştü. T3 paydaş gereksinimleri İK'ya `REQ-KMLK-nnn`
>   kimlikleriyle sunuldu ve **bu kimliklerle onaylandı** (PG-KMLK §3). Aynı önek iki
>   farklı belgede aynı kimliği üretirdi.
> - **`SG-` yetmez:** Yalnızca sistem gereksinimlerini kapsıyormuş gibi okunur; oysa
>   süreç ikisini birlikte kapsar (§1.1).
> - **`SG-YG-` seçilmedi:** İki ayrı kategori varmış gibi okunur. Ayrıca projedeki diğer
>   kimliklerin (`PG-KMLK-01`, `TS-KMLK-01`, `KR-001`) "tek önek + modül + numara"
>   düzenini bozar.
>
> Karar: 25.09.2026, PR #65 incelemesi.

### 3.2 Belgenin bölümleri

Her SYG belgesi TEC.3 çıktılarına karşılık gelen şu bölümleri taşır:

| Bölüm | İçerik | TEC.3 çıktısı |
|---|---|---|
| §2 Sistem tanımı | Öğeler, dış arayüzler, sınırlar, kapsam dışı | a |
| §3 Durumlar ve kipler | Hesap, nesne ve sistem durumları | BP2 |
| §4 Gereksinimler | Kimlik, metin, tür, kaynak, doğrulama yöntemi, parametre | b |
| §5 Kritik performans ölçütleri | Ölçüt, hedef, nasıl ölçüldüğü | c |
| §6 Analiz | Bulgu, çözüm, durum | d |
| §7 Destekleyici sistemler | Dış hesaplar, ortamlar, veri kaynakları ve durumları | e |
| §8 İzlenebilirlik | Paydaş → sistem gereksinimi ters tablosu | f |

### 3.3 Sütunlar

| Sütun | Değerler |
|---|---|
| Tür | İşlevsel · Performans · Arayüz · İşlevsel olmayan · Kısıt |
| Kaynak | Tam paydaş gereksinimi kimliği/kimlikleri (ör. `REQ-KMLK-003, REQ-KMLK-014`) |
| Doğrulama | Test (otomatik, CI'da) · Gösterim (kabulde) · İnceleme · Analiz (ölçüm) |
| Parametre | Y4 kataloğundaki parametre kimliği; yoksa `—` |

Doğrulama yöntemi, TEC.9'da yazılacak testin **türünü** belirler. "Test" yazılmış bir
madde CI'da otomatik test olmadan tamamlanmış sayılmaz.

---

## 4. Bilginin kontrolü (`PA 2.2 b`)

| Kontrol | Kural |
|---|---|
| Sürüm | Her belge Git'te; değişiklik **Pull Request** ile yapılır |
| Onay | En az bir gözden geçiren onayı (CONTRIBUTING §5) |
| Kimlik | `SYG-<MODÜL>-<no>`; numara yeniden kullanılmaz |
| **Kapsama** | **Otomatik:** `.github/scripts/gereksinim-izlenebilirlik-denetimi.mjs`, PR izlenebilirlik denetimi içinde her PR'da çalışır |
| Değişiklik | Paydaş gereksiniminin anlamını değiştiren sistem gereksinimi değişikliği önce `tur:degisiklik-talebi` issue'su ve İK onayı gerektirir |
| Tarih ve sahip | Her belgede "Son güncelleme" ve değişiklik geçmişi |

**Otomatik denetim neyi yakalar?** Bağsız kalan paydaş gereksinimi; paydaş belgesinde
olmayan kaynak kimliği; tekrar eden veya sırası bozuk kimlik; metin içinde karşılıksız
atıf; SYG §8 tablosu veya izlenebilirlik matrisi ile §4 arasındaki farklılık. Denetim
çalışmadan önce **kendini sınar**: bir paydaş gereksinimi bağsız bırakılmış gibi
yapılır ve bunu yakalaması beklenir. Böylece hiçbir satırı eşleştiremeyen bozuk bir
ifade "her şey yolunda" diyemez.

> **Neden otomatik?** Aynı eşleme üç yerde durur: SYG §4, SYG §8 ve izlenebilirlik
> matrisi. Elle tutulan üç kopya ilk değişiklikte ayrılır ve izlenebilirlik varmış
> gibi görünmeye devam eder. İlk çalıştırmada denetim, belgenin kendisinde iki sıra
> hatası yakaladı.

---

## 5. Sürecin çıktıları ile bu belgenin eşlemesi

| TEC.3 çıktısı | Nerede karşılanır |
|---|---|
| a) Sistem ve öğeleri, arayüzleri, işlevleri, sınırları tanımlanır | `SYG-<MODÜL>.md` §2 |
| b) İşlevsel, performans, arayüz, işlevsel olmayan gereksinimler ve tasarım kısıtları tanımlanır | `SYG-<MODÜL>.md` §4 (Tür sütunu) |
| c) Kritik performans ölçütleri tanımlanır | `SYG-<MODÜL>.md` §5 |
| d) Gereksinimler analiz edilir | `SYG-<MODÜL>.md` §6 |
| e) Destekleyici sistemler mevcuttur | `SYG-<MODÜL>.md` §7 |
| f) Paydaş gereksinimlerine izlenebilirlik kurulur | `SYG-<MODÜL>.md` §8 + `izlenebilirlik-matrisi.md` + otomatik denetim |

---

## 6. Gözden geçirme

Bu yaklaşım belgesi, TEC.2 yaklaşım belgesiyle birlikte **her üçüncü modülden sonra**
ve süreç gözden geçirmelerinde ele alınır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-24 | 0.1 | İlk oluşturma — ilk SYG belgesiyle (T3) birlikte | Bilgi İşlem |
