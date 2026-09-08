# ADR-0013 — Dosya Saklama ve Güvenlik Taraması

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-027`, `KR-023`
**İlgili süreç:** TEC.5 (Tasarım), TEC.8 (Entegrasyon)
**İlgili riskler:** `R-03`

---

## Bağlam

Sistem dosya saklayacaktır: özlük dosyası ekleri, sağlık raporları, sertifikalar,
eğitim materyalleri, iş kazası tutanakları. Bu dosyaların bir bölümü **özel nitelikli
kişisel veri** içerir.

Kurum kararı: dosyalar **NAS** üzerinde saklanacak, virüs taraması ve erişim logu
mutlaka olacaktır (`KR-027`).

## Karar

### 1. Soyutlama

```csharp
public interface IFileStorage
{
    Task<StoredFileRef> SaveAsync(Stream content, FileMetadata meta, CancellationToken ct);
    Task<Stream>        OpenAsync(StoredFileRef reference, CancellationToken ct);
    Task                DeleteAsync(StoredFileRef reference, CancellationToken ct);
}
```

Uygulama katmanı NAS'ı, dosya yolunu veya SMB bağlamasını **bilmez**. Bugün
`NasFileStorage`, yarın gerekirse `MinioFileStorage` — tek sınıf değişir.

### 2. Dosyalar veritabanında saklanmaz

İkili içerik veritabanına konmaz. Veritabanında yalnızca **üst veri** tutulur:

| Alan | İçerik |
|---|---|
| `id`, `public_id` | Kimlik |
| `original_name` | Kullanıcının yüklediği ad |
| `content_type` | Doğrulanmış MIME türü |
| `size_bytes` | Boyut |
| `storage_key` | Depodaki konum (uygulamanın ürettiği, kullanıcı adından bağımsız) |
| `checksum_sha256` | Bütünlük doğrulaması |
| `scan_status`, `scanned_at` | Virüs tarama sonucu |
| `owner_person_id`, `category` | Kime ait, hangi türde |
| `uploaded_by`, `uploaded_at` | Denetim |

**Gerekçe:** Veritabanı yedekleri şişer, geri yükleme süresi uzar, bellek kullanımı
artar. Ayrıca NAS zaten yedeklenen bir kaynaktır.

### 3. Dosya adı ve dizin düzeni

- Kullanıcının verdiği ad **depoda kullanılmaz**; yalnızca üst veride saklanır ve
  indirme sırasında gösterilir.
- Depo anahtarı uygulama tarafından üretilir: `<yıl>/<ay>/<uuid>` biçiminde.
- Bu, yol geçişi (path traversal) saldırısını, ad çakışmasını ve dosya adından bilgi
  sızmasını (`ahmet-yilmaz-saglik-raporu.pdf`) birlikte önler.

### 4. Yükleme denetimleri

Sırayla uygulanır; herhangi biri başarısız olursa dosya kabul edilmez:

1. **Boyut sınırı** — tür bazında yapılandırılabilir (varsayılan 20 MB).
2. **Uzantı beyaz listesi** — yalnızca izin verilen türler (`pdf`, `jpg`, `png`,
   `docx`, `xlsx`). Kara liste değil, **beyaz liste**.
3. **İçerik türü doğrulaması** — dosyanın gerçek imzası (magic bytes) kontrol edilir;
   uzantıya güvenilmez.
4. **Virüs taraması** — **ClamAV** (konteyner olarak). Tarama **yükleme anında**
   yapılır; temiz değilse dosya kabul edilmez ve olay kaydedilir.
5. **Karma (checksum) hesaplama** — `SHA-256`, bütünlük doğrulaması için.

ClamAV erişilemezse dosya **kabul edilmez** — taranmamış dosya depoya girmez.

### 5. İndirme denetimleri

- İndirme **her zaman uygulama üzerinden** yapılır; NAS yoluna doğrudan erişim
  verilmez. Böylece yetki kontrolü atlanamaz.
- Yetki kontrolü ADR-0007 kurallarına tabidir; kapsam dışı dosya `404` döner.
- **Her indirme erişim kaydına yazılır** (ADR-0009 §3).
- Yanıtta `Content-Disposition: attachment` ve doğrulanmış içerik türü kullanılır;
  tarayıcıda çalıştırılabilir içerik olarak yorumlanması engellenir
  (`X-Content-Type-Options: nosniff`).

### 6. Erişilebilirlik ve hata davranışı

NAS ağ üzerinden bağlanır; kesinti olabilir.

- Dosya işlemleri **zaman aşımlıdır**; NAS yanıt vermezse istek belirsiz süre beklemez.
- NAS erişilemezse **yalnızca dosya işlemleri** başarısız olur; sistemin geri kalanı
  çalışmaya devam eder.
- NAS erişimi `/health/ready` kontrolüne dâhildir.

### 7. Silme ve saklama

- Kullanıcı silme işlemi **üst veriyi işaretler** (soft delete); fiziksel dosya
  hemen silinmez.
- Fiziksel silme, saklama süresi dolduğunda (`KR-023`) ve kayıt altına alınarak yapılır.
- Yetim dosya (üst verisi olmayan fiziksel dosya) tespiti için periyodik mutabakat
  görevi çalışır ve rapor üretir.

### 8. Yedekleme

> **Uygulama yedeği dosyaları kapsamaz.** NAS'ın kendi yedekleme düzeni olmalıdır ve
> bu, uygulama dışında bir sorumluluktur.

Geri yükleme senaryosunda veritabanı ile NAS'ın **aynı ana** ait olması gerekir; aksi
halde yetim üst veri veya yetim dosya oluşur. Bu, felaket kurtarma planında ele
alınacaktır (TEC.10).

## Gerekçe

- Soyutlama, NAS'a bağımlılığı tek sınıfa hapseder — 10–15 yıllık ufukta depolama
  çözümü değişebilir.
- Depo anahtarının uygulama tarafından üretilmesi, üç ayrı güvenlik sorununu tek
  kararla çözer.
- Yükleme anında tarama, "sonra tararız" yaklaşımının aksine kirli dosyanın depoya hiç
  girmemesini sağlar.
- İndirmenin uygulama üzerinden olması, yetki kontrolünün atlanamamasını garanti eder;
  paylaşılan bir NAS yoluna doğrudan erişim verilseydi bu mümkün olmazdı.

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| Dosyaları veritabanında (`bytea`) saklamak | Yedek boyutu, geri yükleme süresi, bellek kullanımı |
| MinIO / S3 uyumlu nesne deposu | Kurumda NAS mevcut; ek sistem kurulumu gereksiz. Soyutlama sayesinde ileride mümkün |
| NAS paylaşımına doğrudan kullanıcı erişimi | Yetki kontrolü atlanır, erişim logu tutulamaz |
| Kullanıcının dosya adını depoda kullanmak | Yol geçişi, ad çakışması, ad üzerinden bilgi sızması |
| Uzantı kara listesi | Eksik kalır; beyaz liste güvenli varsayılandır |
| Taramayı arka planda yapmak | Kirli dosya bir süre depoda ve erişilebilir kalır |

## Sonuçlar

**Olumlu:** Yetki atlanamaz, kirli dosya depoya girmez, depolama çözümü değiştirilebilir,
her erişim izlenebilir.

**Olumsuz / kabul edilen ödünler:**
- Tüm indirmeler uygulama üzerinden geçtiği için sunucu bant genişliği kullanılır.
  Bu ölçekte sorun değildir.
- ClamAV erişilemezse yükleme durur. Bilinçli bir tercihtir: güvenlik, kullanılabilirliğin
  önündedir.

**Yükümlülükler:**
- NAS yedekleme düzeninin doğrulanması açık bir iştir (proje dışı sorumluluk).
- Yetim dosya mutabakat görevi yazılacaktır.
- Dosya erişim yetkisi sızıntı testleri modül testlerine dâhildir (ADR-0011 §2).

## Geri dönüş maliyeti

**Düşük.** `IFileStorage` arkasında farklı bir depolama uygulaması yazmak yeterlidir;
mevcut dosyaların taşınması ayrı bir göç işidir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
