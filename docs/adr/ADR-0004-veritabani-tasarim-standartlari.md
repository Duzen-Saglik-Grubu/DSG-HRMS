# ADR-0004 — Veritabanı Tasarım Standartları

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-002`, `KR-023`, `KR-035`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)

---

## Bağlam

Mevcut sistemin veritabanı şeması temel alınmayacaktır (`KR-002`). Yeni şema sıfırdan,
ilişkisel bütünlüğü olan ve tarih farkındalıklı bir yapıda kurulacaktır. 10–15 yıllık
kullanım hedefi, baştan konulmazsa sonradan eklenemeyecek kararların şimdi alınmasını
gerektiriyor.

## Karar

### 1. Adlandırma

| Konu | Kural | Örnek |
|---|---|---|
| Genel biçim | `snake_case`, İngilizce | `employment_period` |
| Tablo adı | **Tekil** | `person`, not `persons` |
| Birincil anahtar | `id` | |
| Yabancı anahtar | `<tekil_tablo>_id` | `person_id` |
| Ara tablo | `<a>_<b>` | `role_permission` |
| Dizin | `ix_<tablo>_<kolonlar>` | `ix_person_national_id` |
| Tekillik kısıtı | `uq_<tablo>_<kolonlar>` | `uq_person_national_id` |
| Kontrol kısıtı | `ck_<tablo>_<kural>` | `ck_leave_end_after_start` |
| Şema | Modül başına ayrı şema | `identity`, `organization`, `personnel`, `leave`, `training`, `audit` |

EF Core tarafında bu dönüşüm **otomatik** yapılacaktır (`UseSnakeCaseNamingConvention`);
C# tarafında `PascalCase` kullanılır. Ad eşlemesi elle yazılmaz.

### 2. Anahtarlar

- Birincil anahtar: **`bigint` (`GENERATED ALWAYS AS IDENTITY`)**.
- Dışa açık kimlik gereken varlıklarda ayrıca **`uuid` (`public_id`)** tutulur ve
  API'de bu kullanılır. Böylece kayıt sayısı, oluşturma sırası gibi bilgiler dışarı
  sızmaz.
- Doğal anahtar (TCKN, sicil kodu) **birincil anahtar olarak kullanılmaz**; tekillik
  kısıtı olarak tanımlanır.

### 3. Veri tipleri

| Kullanım | Tip | Not |
|---|---|---|
| Tarih (doğum, işe giriş, izin günü) | `date` | Saat bilgisi taşımaz; zaman dilimi sorunu doğurmaz |
| Zaman damgası (oluşturma, log) | `timestamptz` | **Daima UTC** saklanır |
| Metin | `text` | PostgreSQL'de `varchar(n)` performans avantajı sağlamaz; uzunluk kısıtı `CHECK` ile |
| Para | `numeric(19,4)` | Kayan noktalı tip kullanılmaz |
| Sayısal kod / durum | Küçük tamsayı + `CHECK`, C# tarafında `enum` | Metin durum kodu kullanılmaz |
| Yapılandırılmamış ek veri | `jsonb` | Yalnızca gerçekten şemasız veri için; iş kuralı taşıyan alan JSON'a konmaz |

**Zaman dilimi kuralı:** Veritabanında her şey UTC'dir. Kullanıcıya gösterim
`Europe/Istanbul` saatine göre **sunum katmanında** çevrilir. İzin günü gibi takvim
kavramları `date` olduğu için bu çevrimden etkilenmez.

### 4. Zorunlu ortak kolonlar

Her iş tablosunda:

| Kolon | Tip | Açıklama |
|---|---|---|
| `created_at` | `timestamptz` | Oluşturma anı (UTC) |
| `created_by` | `bigint` | Oluşturan kullanıcı |
| `updated_at` | `timestamptz` | Son güncelleme |
| `updated_by` | `bigint` | Son güncelleyen |
| `row_version` | `xmin` (PostgreSQL sistem kolonu) | Eşzamanlılık denetimi |

Bu kolonlar EF Core `SaveChanges` ara katmanında **otomatik** doldurulur; her serviste
elle yazılmaz.

### 5. Silme politikası

- İş kayıtları **fiziksel olarak silinmez**; `deleted_at` ve `deleted_by` ile
  işaretlenir (soft delete). Sorgular varsayılan olarak silinmişleri hariç tutar
  (EF Core global query filter).
- Referans/tanım verileri (izin türü, rol) silinmez, **pasife alınır** (`is_active`).
- Gerçek fiziksel silme yalnızca KVKK imha süreci kapsamında ve kayıt altına alınarak
  yapılır (`KR-023`).

### 6. Referans bütünlüğü

- Tüm ilişkiler **yabancı anahtar kısıtı** ile tanımlanır. "Uygulama katmanı kontrol
  eder" gerekçesiyle kısıt atlanmaz.
- Silme davranışı varsayılan olarak `RESTRICT`; `CASCADE` yalnızca gerçekten sahiplik
  ilişkisi olan yerlerde ve açık gerekçeyle kullanılır.
- Alan düzeyi kurallar `CHECK` kısıtı ile veritabanında da tanımlanır (örn. izin bitiş
  tarihi başlangıçtan önce olamaz). Doğrulama hem uygulamada hem veritabanında olur:
  uygulama iyi hata mesajı için, veritabanı son savunma hattı için.

### 7. Tarih aralıklı (temporal) tablolar

Zaman içinde değişen ve geçmişi sorgulanabilir olması gereken veriler için standart
kolon çifti:

| Kolon | Anlam |
|---|---|
| `valid_from` | `date`, dâhil |
| `valid_to` | `date`, **dışlayan** üst sınır; açık uçlu kayıtlarda `null` |

Kurallar:
- Aynı varlık için çakışan aralık olamaz — PostgreSQL `EXCLUDE` kısıtı ile veritabanı
  seviyesinde engellenir (`daterange` + `gist` dizini).
- Açık uçlu kayıt (`valid_to IS NULL`) varlık başına en fazla bir tane olabilir.
- "Bugün geçerli olan" sorgusu için kısmi dizin tanımlanır.

Hangi verilerin tarih aralıklı tutulacağı ADR-0005'te belirlenmiştir.

### 8. Göç (migration) yönetimi

- Şema değişiklikleri **yalnızca EF Core migration** ile yapılır; üretimde elle SQL
  çalıştırılmaz.
- Her migration **geri alınabilir** olmalıdır (`Down` yazılır ve gözden geçirilir).
- **Yıkıcı değişiklikler** (kolon silme, tip daraltma) tek adımda yapılmaz;
  ekle → doldur → geçiş → temizle şeklinde çok sürümlü uygulanır.
- CI, "bekleyen model değişikliği var mı" kontrolü yapar: kod ile migration'lar
  uyumsuzsa yapı kırılır.
- Referans verileri (izin türleri, roller, iller) migration içinde **seed** edilir;
  elle giriş beklenmez.

### 9. Performans

- Tüm yabancı anahtarlar dizinlenir.
- Sık kullanılan filtre ve sıralama kolonları için dizin tanımlanır; her dizin
  gerekçesiyle birlikte migration açıklamasına yazılır.
- Listeleme uç noktalarında **sayfalama zorunludur**; sayfasız liste dönen uç nokta
  yazılmaz.
- N+1 sorgu üreten kod, kod gözden geçirme kontrol listesinde ayrı bir maddedir.

### 10. Karakter kümesi ve karşılaştırma

- Veritabanı `UTF8` kodlamasıyla oluşturulur.
- Türkçe sıralama ve büyük/küçük harf duyarsız arama için **ICU collation**
  (`tr-TR-x-icu`) kullanılır. Bu, `i/İ` ve `ı/I` sorununu doğru çözer.
- Arama alanlarında `citext` yerine, açıkça normalize edilmiş (`lower()`) kolonlar ve
  ifade dizinleri tercih edilir.

## Gerekçe

- `snake_case` PostgreSQL'in doğal biçimidir; tırnaklama gerektirmez, elle yazılan
  sorguları okunur kılar.
- `date` / `timestamptz` ayrımı, İK verisinde en sık görülen hata kaynağını (izin
  gününün zaman dilimi çevriminde kayması) baştan ortadan kaldırır.
- Soft delete, İK verisinde geçmişe dönük denetim ihtiyacı nedeniyle zorunludur.
- Tarih aralıklı tablolar için `EXCLUDE` kısıtı, uygulamada gözden kaçan çakışmaları
  veritabanı düzeyinde engeller — 10–15 yıllık veride bu tür bozulmalar birikir.
- ICU collation, Türkçe karakterlerde doğru sıralama ve arama için gereklidir; varsayılan
  collation ile "İnsan" ve "insan" beklenmedik biçimde sıralanır.

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| `PascalCase` tablo adları | PostgreSQL'de her sorguda çift tırnak gerektirir |
| `uuid` birincil anahtar | Dizin boyutu ve ekleme performansı; `bigint` + ayrı `public_id` daha iyi denge |
| Fiziksel silme | Denetim izi ve geçmiş raporlama kaybı |
| Veritabanı kısıtı yerine yalnız uygulama doğrulaması | Tek savunma hattı; doğrudan veritabanı erişimiyle bozulabilir |
| PostgreSQL sistem sürümlü (system-versioned) tablolar | PostgreSQL'de yerleşik değil; eklenti bağımlılığı doğurur |

## Sonuçlar

**Olumlu:** Tutarlı, denetlenebilir ve geçmişi sorgulanabilir bir şema; veri bozulmasına
karşı iki katmanlı koruma.

**Olumsuz / kabul edilen ödünler:** Tarih aralıklı tablolar sorguları karmaşıklaştırır;
"bugünkü hâli" için görünüm (view) veya yardımcı sorgu yöntemleri tanımlanacaktır.
Soft delete, her sorguda filtre gerektirir — global query filter ile otomatikleştirilir.

**Yükümlülükler:** Bu standartlar kod gözden geçirme kontrol listesine madde madde
yazılacaktır (MAN.8).

## Geri dönüş maliyeti

**Yüksek.** Adlandırma, anahtar ve tarihlilik kararları şemanın temelidir; sonradan
değiştirmek veri göçü gerektirir. Bu nedenle iskelet aşamasında kesinleştirilmiştir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
