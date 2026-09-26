# ADR-0003 — LOGO Entegrasyon Stratejisi

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-003`, `KR-004`, `KR-007`, `KR-008`, `KR-043`
**İlgili süreç:** TEC.5 (Tasarım), TEC.8 (Entegrasyon)
**İlgili riskler:** `R-02`

---

## Bağlam

Personel ana verisi (kimlik, iletişim, işe giriş/çıkış, firma, şube, birim, görev) kurumun
LOGO Bordro sisteminde tutulmaktadır ve orası **kaynak sistemdir**. Bordro işlemleri
LOGO'da kalmaya devam edecektir; HRMS'in LOGO'dan alacağı tek kapsam personel ana verisidir.

Mevcut HRMS bu veriyi her ekranda **canlı** olarak LOGO'dan okumaktadır. Bu yaklaşımın
tespit edilen sorunları:

- LOGO veya MSSQL erişilemediğinde HRMS tamamen durur.
- Liste, arama ve sayfalama yükü LOGO veritabanına düşer.
- HRMS kayıtları (izin, eğitim) LOGO satırlarına yabancı anahtar veremez; referans
  bütünlüğü sağlanamaz.
- Canlı veri gün içinde değişir; aynı rapor iki kez çalıştırıldığında farklı sonuç verir.
  (Ölçüm: 2026-09-03'te 587 aktif personel, 2026-09-04'te 585.)

## Karar

### 1. Erişim yalnızca okumadır ve bu teknik olarak zorlanmıştır

LOGO veritabanına hiçbir yazma, güncelleme veya silme işlemi yapılmayacaktır. Bu bir
taahhüt değil, **uygulanmış bir kontroldür**:

- Proje için ayrı bir SQL oturum açma kaydı tanımlanmıştır (`db_datareader`).
- `INSERT`, `UPDATE`, `DELETE`, `ALTER`, `EXECUTE` yetkileri `DENY` ile reddedilmiştir.
- Doğrulama testi (2026-09-03) yazma denemesinin reddedildiğini teyit etmiştir.
- Ek olarak, uygulama tarafında LOGO için kullanılan `DbContext`
  **`SaveChanges` çağrısında istisna fırlatacaktır** ve varsayılan olarak `NoTracking`
  çalışacaktır — ikinci bir kilit.
- **Üçüncü kilit (2026-09-26):** Senkronizasyon **her çalışmadan önce** oturumun yazma
  yetkisi olmadığını `HAS_PERMS_BY_NAME` ile denetler; bu denetim hiçbir şey yazmaz.
  Yazma yetkisi görürse çalışmayı reddeder ve kritik düzeyde günlüğe yazar. `DENY` bir
  gün yanlışlıkla kaldırılırsa uygulama bunu fark eder.

**Kanıt:** Salt-okunur oturumu oluşturan betik depodadır
(`docker/logo/salt-okunur-oturum.sql`). CI, bu betiğin **kendisini** SQL Server
Express üzerinde çalıştırıp yazma denemelerinin veritabanı tarafından reddedildiğini
her PR'da doğrular (`LogoReadOnlyAccessTests`, SYG-KMLK-003). Yetki denetiminin
gerçekten yetki tespit edebildiği, yazma yetkili bir kontrol oturumuyla ayrıca sınanır.

**Araç notu:** Projede iki `DbContext` bulunduğundan `dotnet ef` komutlarına
`--context HrmsDbContext` verilir. LOGO şeması HRMS tarafından yönetilmez; LOGO
bağlamı için migration üretilmez.

### 2. Yalıtım: tek bir bileşen, tek bir arayüz

LOGO'ya erişen tüm kod `Dsg.Hrms.Infrastructure/Logo/` altında toplanır. Uygulama
katmanı yalnızca şu arayüzü bilir:

```csharp
public interface ILogoPersonnelSource
{
    Task<IReadOnlyList<LogoPersonnelRecord>> GetAllAsync(CancellationToken ct);
    Task<LogoPersonnelRecord?> FindByNationalIdAsync(string nationalId, CancellationToken ct);
    Task<LogoSchemaCheckResult> VerifySchemaAsync(CancellationToken ct);
}
```

Bu, **yolsuzluk önleyici katmandır** (anti-corruption layer): LOGO'nun tablo adları,
kolon kodları ve alan anlamları uygulamanın geri kalanına sızmaz. Kaynak sistem yarın
değişirse (başka bir bordro yazılımı) yalnızca bu klasör değişir.

### 3. Erişim yöntemi: Entity Framework Core, ayrı bir salt-okunur bağlam

LOGO erişimi ham SQL yerine EF Core ile yapılacaktır. Bu, tablo adlarının firma
numarasına göre çoğalmadığının doğrulanmasına dayanır: `LH_%_PERSON` desenine uyan
**tek tablo** vardır (`LH_001_PERSON`); yedi firmanın personeli bu tablodadır ve firma
ayrımı `FIRMNR` alanı ile yapılır.

**Birleştirme kuralı — kritik:** `L_CAPIDIV`, `L_CAPIDEPT` ve `L_CAPIFIRM` tablolarında
`NR` alanı **firma içinde tekildir, veritabanı genelinde değildir.** Birleştirmelerde
firma eşleşmesi de zorunludur:

```sql
JOIN L_CAPIDIV  e ON e.NR = p.LOCNR  AND e.FIRMNR = p.FIRMNR
JOIN L_CAPIFIRM f ON f.NR = e.FIRMNR AND f.NR     = p.FIRMNR
JOIN L_CAPIDEPT d ON d.NR = p.DEPTNR AND d.FIRMNR = f.NR
```

Bu koşul eksik bırakıldığında kayıtlar çoğalır (test sırasında 40 kayıtlık bir liste
131 kayıt olarak üretilmiştir). Kural, eşleme yapılandırmasında **tek bir yerde**
tanımlanacak ve her sorguda tekrarlanmayacaktır.

### 4. Senkronizasyon: anlık görüntü (snapshot)

Personel ana verisi HRMS veritabanına kopyalanır; ekranlar ve raporlar **HRMS'ten** okur.

| Konu | Karar |
|---|---|
| Periyot | **15 dakikada bir**, arka plan servisi (Hosted Service) ile |
| Manuel tetikleme | **Var** — yetkili kullanıcı "şimdi senkronize et" diyebilir |
| Yöntem | Kaynaktan tam liste okunur, HRMS'teki kayıtlarla karşılaştırılır, fark uygulanır |
| Kimlik eşleştirme | **TCKN** (kişi) ve **sicil numarası** (istihdam) |
| Tarihçe | Her senkronizasyon tarihli bir çalışma kaydı üretir: okunan, eklenen, güncellenen, uyarı üretilen kayıt sayıları |
| Hata davranışı | Senkronizasyon hatası **sessiz kalmaz**; kayıt altına alınır ve yöneticiye bildirim üretir |
| LOGO erişilemezse | Sistem **çalışmaya devam eder**; son anlık görüntü kullanılır, ekranda "son güncelleme" bilgisi gösterilir |

### 5. Veri kalitesi kuralları

| Durum | Davranış |
|---|---|
| **TCKN'si olmayan kart** | Kişi olarak **oluşturulmaz**, uyarı listesine alınır (`KR-043`) |
| Aynı TCKN'li birden fazla kart | Tek `Kişi`, birden fazla `İstihdam` olarak eşlenir (bkz. ADR-0005) |
| Organizasyon referansı eşleşmeyen kayıt | Uyarı üretilir, kayıt düşürülmez |
| Eksik iletişim bilgisi | Uyarı listesine alınır; İK'ya raporlanır |

Uyarılar, yönetim ekranında **veri kalitesi raporu** olarak sunulur ve İK'nın LOGO
üzerinde düzeltme yapabilmesi için kullanılır.

### 6. Şema sapması denetimi

`VerifySchemaAsync`, beklenen tablo, kolon ve veri tiplerinin varlığını kontrol eder.
Bu denetim hem bir **entegrasyon testi** olarak CI'da, hem de üretimde **günlük** olarak
çalışır. LOGO sürüm yükseltmesi bir şeyi değiştirdiyse, kullanıcı fark etmeden önce
uyarı üretilir (`R-02`).

## Gerekçe

- Snapshot yaklaşımı, dört sorunu birden çözer: kesinti dayanıklılığı, performans,
  referans bütünlüğü ve rapor tutarlılığı.
- 15 dakikalık gecikme İK süreçleri için kabul edilebilirdir; acil durumda manuel
  yenileme mevcuttur.
- Yalıtım katmanı, kaynak sistem değişikliğini tek klasörlük bir işe indirger — bu,
  10–15 yıllık ufukta somut bir kazançtır.

## Değerlendirilen alternatifler

| Alternatif | Artıları | Eksileri | Neden seçilmedi |
|---|---|---|---|
| Canlı sorgu (mevcut sistem) | Her zaman güncel | Kesintide sistem durur, performans, referans bütünlüğü yok, rapor tutarsız | Dört ayrı sorun üretiyor |
| LOGO'da view oluşturmak | Şema değişikliğine karşı tampon | View silinir/güncellenmez, kaynak sistemde nesne yaratır | Kurum LOGO'da nesne oluşturulmasını istemiyor |
| Ham SQL ile erişim | Firma bazlı tablo adlarına esneklik | Bakımı zor, tip güvenliği yok | Tablo adlarının sabit olduğu doğrulandı; EF Core uygun |
| Olay tabanlı senkronizasyon (tetikleyici/CDC) | Anlık güncelleme | LOGO'da değişiklik gerektirir | `KR-003` ile çelişir |

## Sonuçlar

**Olumlu:**
- LOGO kesintisinde HRMS ayakta kalır.
- Raporlar tekrarlanabilir ve tarihlidir.
- İzin/eğitim kayıtları HRMS'teki personel kaydına gerçek yabancı anahtarla bağlanır.
- Kaynak sistem değişikliği tek bileşenlik iş olur.

**Olumsuz / kabul edilen ödünler:**
- Veri en fazla 15 dakika gecikmelidir. Ekranlarda "son senkronizasyon" bilgisi
  gösterilerek bu şeffaf kılınır.
- HRMS'te personel verisinin bir kopyası tutulur; KVKK kapsamında bu kopya da
  korunmalıdır (ADR-0009).

**Yükümlülükler:**
- Şema sapma denetimi yazılacak ve günlük çalıştırılacak.
- Senkronizasyon kayıt çoğalmasını yakalayan bir doğrulama içerecek (senkronizasyon
  sonrası kişi sayısı, kaynaktaki tekil TCKN sayısına eşit olmalı).
- Salt-okunur yetkinin korunduğu düzenli olarak yeniden doğrulanacak.

## Geri dönüş maliyeti

**Düşük–Orta.** Yalıtım katmanı sayesinde senkronizasyon periyodu, yöntemi veya kaynak
sistem değiştirilebilir. Canlı sorguya dönmek istenirse arayüz aynı kalır, uygulaması
değişir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
| 2026-09-26 | 0.2 | §1 üçüncü kilit (her çalışmadan önce yetki denetimi), CI kanıtı ve `--context` notu eklendi (#74) | Bilgi İşlem |
