# ADR-0019 — Personel Verisine Erişim: Kapsam ve Alan Görünürlüğü

**Durum:** **Kabul Edildi**
**Tarih:** 2026-10-22
**Karar defteri karşılığı:** `KR-121`, `KR-125`, `KR-126`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)
**Genişlettiği:** ADR-0007 §2–§3 (kapsam ve uygulama noktası), ADR-0009 §3 (erişim kaydı), ADR-0014 §4 (dışa aktarma)
**İlgili gereksinimler:** PG-PERS-14, 20, 24–27, 31, 35, 36, 42, 44; `KR-116` madde 3
**İlgili iş:** #245 maddeler 1, 2, 8, 13 ve B-07'nin ikinci kısmı

---

## Bağlam

T1 ilk kez **başka kişilerin** verisini gösteren ekranlar getiriyor: personel listesi,
kişi kartı, dışa aktarma. T3'te kişiye özel okuma ucu yoktu; ADR-0007'nin kapsam
altyapısı (`ICurrentUserScope`, veri katmanında süzgeç, kapsam dışı kayda `404`) bu yüzden
hiç kurulmadı (`KR-089`; tasarım gözden geçirmesi B-06).

T1'in onaylı gereksinimleri iki ayrı soruyu cevaplamayı gerektiriyor:

1. **Kim, kimin kaydını görür?** (satır düzeyi). T2 ve T4 gelene kadar geçiş kuralı:
   İK ve Sistem Yöneticisi herkesi, diğer personel yalnızca kendini görür (PG-PERS-35).
   Doğrudan ast kuralı (PG-PERS-24) T2'deki atamalarla gelir.
2. **Gördüğü kaydın hangi alanlarını görür?** (alan düzeyi). Tam TCKN yalnızca İK ve Sistem
   Yöneticisi görür (PG-PERS-31). Adres, kan grubu ve acil durum kişisini yalnızca İK,
   Sistem Yöneticisi ve kişinin kendisi görür (PG-PERS-20, 36, 44). Kullanıcının göremediği
   alan ne filtrede ne dışa aktarmada seçenek olarak sunulur (PG-PERS-14, 42; `KR-116`).

Bir de yan kapı var: Kimlik modülü `Person` tablosunu doğrudan sorguluyor (hesap işlemleri
listesi, üyelik). Alan politikası yalnızca Personnel'in sorgularına uygulanırsa bu yol
politikayı atlayabilir (ADR-0016 notu, #246).

## Karar

### 1. Satır düzeyi: tek giriş noktası, açık kapsam

- `ICurrentUserScope` kurulur. Bugünkü değerleri `All` ve `Self`; T2 `DirectReports`'u,
  T4 diğerlerini ekler (ADR-0007 §2).
- Kapsam **izinden** türetilir: `personnel.person.view` izni olan kullanıcı `All`,
  olmayan `Self` kapsamındadır. Kendi kaydını görmek izin gerektirmez (ADR-0007 §5).
- Kişi okumaları tek bir **kapsamlı sorgu** bileşeninden geçer (`IPersonnelQuery`,
  Infrastructure). Bileşen sorguya kapsam süzgecini ekler; kapsam dışı kayıt "bulunamadı"
  olarak döner ve uç `404` verir (ADR-0007 §4).
- **EF genel sorgu süzgeci (global query filter) kullanılmaz:** Senkronizasyon ve üyelik
  bütün kişileri görmek zorundadır ve süzgeç `IgnoreQueryFilters` ile sessizce atlanabilir.
- **Korumalı alanlar `Person` tablosunda tutulmaz** (revizyon 1.2, B-11). Adres ve kan grubu
  ayrı bir tabloda (`person_sensitive`), evlilik bilgisi ve acil durum kişileri kendi
  tablolarında tutulur (ADR-0021 §1). `Person`'ı okuyan her kod, korumalı alanları
  **yüklemez**; Kimlik modülü dahil. Korumalı tablolara yalnızca kapsamlı sorgu bileşeni ve
  senkronizasyon erişir. Bunu tip düzeyindeki mimari testi denetler: korumalı varlık tiplerine
  bağımlı olabilen sınıflar listelidir ve liste PR'da görünür bir karardır. Önceki "doğrudan
  `Set<Person>()` erişimini sınırlayan test" tasarımı NetArchTest ile kurulamazdı; çünkü araç
  tip düzeyinde çalışır, çağrıyı ayırt edemez.

### 2. Alan düzeyi: tek politika, üç kullanım yeri

- `PersonFieldVisibility` politikası korumalı her alan için kimin görebileceğini tanımlar:

  | Alan | Görebilen |
  |---|---|
  | Tam TCKN | `personnel.person.view_national_id` izni (İK, Sistem Yöneticisi). Diğerleri maskeli görür, kişinin kendisi dahil (S-01) |
  | Adres | `personnel.person.view_sensitive` izni veya kişinin kendisi |
  | Kan grubu | `personnel.person.view_sensitive` izni veya kişinin kendisi |
  | Acil durum kişileri | `personnel.person.view_sensitive` izni veya kişinin kendisi |
  | Evlilik tarihi ve evlilik yardımı | Yalnızca `personnel.person.view_sensitive` izni (İK, Sistem Yöneticisi); **kişinin kendisi dahil değil** (İK teyidi 22.10.2026) |
  | Diğer kart alanları | Kaydı görebilen herkes |

- Aynı politika **dört yerde** kullanılır ve başka yerde yeniden yazılmaz:
  1. **API yanıtı:** Göremeyen kullanıcının yanıtında alan **yer almaz** (maskeli TCKN
     hariç). Ekranda gizlemek yetmez.
  2. **Filtre listesi:** Liste ekranı, kullanıcının görebildiği alanların filtrelerini alır;
     göremediği alana göre filtreli istek `400` ile reddedilir.
  3. **Sıralama:** Göremediği alana göre sıralama istenemez (`400`). Aksi hâlde sıra,
     değerlerin kendisini ele verir (revizyon 1.2, B-07).
  4. **Dışa aktarma alan listesi:** Seçilebilir alanlar politikadan gelir (PG-PERS-42).
- **Kayıt düzeyi ve küme düzeyi:** "Kişinin kendisi" kuralı yalnızca tek kaydın gösterildiği
  yerde (kart, kendi profilim) geçerlidir. Küme işlemlerinde (liste filtresi, sıralama, dışa
  aktarma) **yalnızca izin** esas alınır. Böylece kendi kan grubunu gören personel, kan
  grubuna göre filtre veya sıralama kullanamaz (B-07).
- **Arama:** Hızlı aramada TCKN ile arama yalnızca `personnel.person.view_national_id` izni
  olan kullanıcıya açıktır. Göremediği bir TCKN ile arayan kullanıcı, kişinin varlığını
  doğrulayamamalıdır (revizyon 1.2, B-17).
- **Test:** Her korumalı alan için ayrı bir test yazılır: yetkisi olmayan kullanıcının API
  yanıtında, filtresinde, sıralamasında ve dışa aktarma seçeneklerinde alan yoktur. Bu,
  T1'in yetki sızıntısı testidir.

### 3. Kimlik modülünün kişi sorguları

Kimlik modülü `Person`'ı doğrudan okumaya devam eder: ad, soyad, kurumsal e-posta, cep
telefonu, TCKN ile doğum tarihi (eşleştirme) ve istihdam. Korumalı alanlar `Person`'da
olmadığı için (§1) Kimlik bunları yükleyemez. Mimari testi, Kimlik modülünün korumalı varlık
tiplerine (`PersonSensitive`, `PersonMarriage`, `EmergencyContact`) bağımlılığını reddeder.
Böylece hesap işlemleri listesi ve oturum deposu T1'in alan politikasını atlayan bir yol
olmaz (B-11). Kimlik'in Personnel'in okuma arayüzüne taşınması, T4'te rol ve kapsam yeniden
düzenlenirken değerlendirilir.

### 4. İzinler ve rol

| İzin | Anlamı | İK | Sistem Yöneticisi |
|---|---|---|---|
| `personnel.person.view` | Tüm personeli görme (kapsam `All`) | ✔ | ✔ |
| `personnel.person.view_national_id` | Tam TCKN | ✔ | ✔ |
| `personnel.person.view_sensitive` | Adres, kan grubu, acil durum kişileri, evlilik bilgisi | ✔ | ✔ |
| `personnel.person.update` | HRMS'te tutulan alanları girme (görev, evlilik bilgisi, acil durum kişisi; `KR-112`) | ✔ | ✔ |
| `personnel.person.export` | Dışa aktarma (ADR-0014 §4) | ✔ | ✔ |
| `personnel.correction.view`, `personnel.correction.update` | "İK'ya bildir" taleplerini görme ve kapatma (PG-PERS-37) | ✔ | ✔ |
| `personnel.quality.view`, `personnel.quality.export` | Veri kalitesi raporu ve dışa aktarımı (PG-PERS-32) | ✔ | ✔ |
| `personnel.sync.view`, `personnel.sync.create` | Senkronizasyon durumu ve elle başlatma (T3'teki `identity.sync.*` buraya taşınır; B-10) | ✔ | ✔ |

İzin dağılımı Doğuş Uçanok tarafından 22.10.2026'da onaylandı (`KR-126`; B-14). Senkronizasyonu
elle başlatma T3'te yalnızca Sistem Yöneticisindeydi; artık İK'da da var.

> **Karar (23.10.2026, `KR-128`):** Denetim kayıtlarını görme (`audit.log.view`) ve sistem
> parametreleri (`system.parameter.*`) de İK'ya verildi; bugün İK ve Sistem Yöneticisi aynı
> izinlere sahiptir. T4'te izinler istenen rollere, roller istenen kullanıcılara atanabilecek.

**Eylem adları:** `view_national_id` ve `view_sensitive`, ADR-0007 §1'deki eylem sözlüğüne
(`view`, `create`, `update`, `delete`, `report`, `export`, `approve`) **alan düzeyi görme**
eylemleri olarak eklenir. İzin listesi belgesi aynı PR'da güncellenir (B-16).

T1 izinleri mevcut İK rolüne (`hr-identity-operations`) verilir. Rolün görünen adı
"İnsan Kaynakları" olur, kodu değişmez. Sistem Yöneticisi bütün izinlere sahiptir. Yeni bir
rol açılmaz: rol yönetimi T4'te gelir ve o zamana kadar yönetilemeyen bir rol daha
eklemek, atama yükünü artırır.

### 5. Erişim kaydı

- **Kart görüntüleme:** Her kart açılışı bir `View` kaydı üretir (PG-PERS-28). Kan grubu
  yanıtta yer aldıysa ek olarak `SpecialCategoryView` kaydı da üretilir (ADR-0009 §3).
- **Liste görüntüleme:** Liste kaydı bugün yalnızca süzgeci ve sayıyı tutuyor. T1'de liste
  kaydına **ekranda gösterilen kişilerin kimlikleri** de yazılır (sayfa başına en fazla
  sayfa boyutu kadar). Böylece "verilerimi kim gördü" sorusu liste görüntülemesi için de
  cevaplanır (B-07'nin ikinci kısmı; `KR-125`). Bedeli kayıt hacmidir: 24 kartlık bir sayfa
  için 24 kimlik.
- **Dışa aktarma:** Süzgeç, kayıt sayısı, **aktarılan alanların listesi** ve **aktarılan
  kişilerin kimlikleri** yazılır (ADR-0014 §4; revizyon 1.2, B-09). Kişiye göre "verilerim
  dışarı çıktı mı" sorusu en riskli işlemde de cevaplanır.
- **Korumalı alan süzgeçlerinin değeri yazılmaz** (revizyon 1.2, B-01). Bir süzgeç korumalı
  bir alana bakıyorsa (kan grubu, adres; §2'deki tablo), erişim kaydına yalnızca süzgecin
  **kullanıldığı** yazılır, değeri yazılmaz. Süzgeç değeri kişi kimlikleriyle aynı kayıtta
  durursa ("kan grubu = 0 Rh−" ve 24 kişi), kayıt kan grubunun kendisini ele verir; üstelik 10
  yıl saklanır. Süzgecin korumalı olup olmadığı alanın sınıflandırmasından türetilir
  (`KR-120`); erişim kaydı süzgeç adına bakan ad listesine güvenmez. Bunu doğrulayan test
  yazılır.
- **Özel nitelikli veri, küme işleminde de kayda geçer** (revizyon 1.2, B-08). Kan grubu
  süzgeciyle listelenen her kişi için, ayrıca İK'nın düzeltme talebinde önerilen kan
  grubunu görmesi için kişi başına bir `SpecialCategoryView` kaydı üretilir.

### 6. Birden fazla sicili olan kişi

İK kararı (22.10.2026, `KR-126`; B-15). Bugün 13 kişinin aynı anda iki aktif sicili var.

| Konu | Kural |
|---|---|
| Liste kartı | Kişi bir kez görünür. Kartta **en son işe girdiği sicil** gösterilir: aktif sicillerden işe giriş tarihi en yeni olan; aktif sicil yoksa çıkış tarihi en yeni olan. Kartta **"X sicil"** işareti bulunur; X, kişinin sicil sayısıdır |
| Filtre | Şube, birim ve ünvan gibi istihdam filtreleri kişinin **herhangi bir** sicilinde eşleşir; "aktif" süzgeci yalnızca aktif sicillere bakar |
| Aktif / ayrılan | Kişinin **en az bir aktif sicili** varsa kişi aktiftir |
| Görünür kalma süresi | Ayrılan kişi için süre (PG-PERS-38) **son çıkış tarihinden** başlar |

## Gerekçe

- Tek giriş noktası ve tek politika, kuralın her ekran ve uçta yeniden yazılmasını
  önler. Yeniden yazılan kural er geç bir yerde eksik kalır.
- Politikanın filtre ve dışa aktarmaya da uygulanması, `KR-116` madde 3'ün kendisidir:
  göremediği alana göre filtreleyebilen kullanıcı, gizli değeri sorgu sonucundan çıkarır.
- Genel sorgu süzgeci, senkronizasyonun her kişiyi görmesi gerektiği için istisnalarla
  dolardı; açık bileşen ve mimari testi aynı güvenceyi görünür biçimde verir.
- Kapsamın izinden türetilmesi, T4 rol yönetimi geldiğinde veri modelini değiştirmeden
  genişletilebilir.

## Değerlendirilen alternatifler

| Alternatif | Artıları | Eksileri | Neden seçilmedi |
|---|---|---|---|
| EF genel sorgu süzgeci | Unutulması zor | Senkronizasyon ve üyelik için istisna gerekir; `IgnoreQueryFilters` sessiz atlama | Atlama yolu görünmez kalır |
| Alan gizlemeyi yalnızca ön yüzde yapmak | Basit | API doğrudan çağrılınca veri döner | ADR-0007 §3 kritik kuralıyla çelişir |
| Alan başına ayrı uç nokta (ör. `/persons/{id}/sensitive`) | Yetki uç düzeyinde | Filtre ve dışa aktarmada politika yine gerekir; ön yüz karmaşıklaşır | Sorunu çözmez, böler |
| Yeni "İK" rolü | Temiz ayrım | Rol yönetimi T4'te; atama elle yapılır | Bugün yönetim yükü getirir |
| Liste görüntülemesinde kişi kimliği tutmamak | Daha az kayıt | "Verilerimi kim gördü" sorusu listeler için cevapsız kalır | KVKK başvurusunda eksik kanıt |

## Sonuçlar

**Olumlu:**
- Kapsam ve alan kuralları tek yerde; yetki sızıntısı testi her korumalı alanı kapsar.
- Kimlik modülü yan kapı olmaktan çıkar.
- Liste görüntülemeleri de kişi bazında izlenebilir.

**Olumsuz / kabul edilen ödünler:**
- Erişim kaydı hacmi artar (liste başına sayfa boyutu kadar kimlik).
- Kimlik modülü hâlâ `Person` tablosunu doğrudan okur; korumalı alanlar ayrı tablolarda
  olduğu için bu okuma onlara ulaşmaz, sınır tip düzeyindeki testle korunur.

**Bu kararın getirdiği yükümlülükler:**
- `IPersonnelQuery`, `ICurrentUserScope` ve `PersonFieldVisibility` T1'in ilk kod
  parçasında kurulur; ekranlar onların üzerine yazılır.
- İzin listesi (`docs/mimari/izin-listesi.md`) ve izin migration'ı aynı PR'da güncellenir.
- T2 `DirectReports`'u, T4 rol yönetimini bu yapının üzerine ekler.

## Geri dönüş maliyeti

Orta. Kapsam ve politika tek yerde olduğu için değiştirilebilir. Asıl maliyet, ekranların
ve testlerin bu bileşenlere dayanmasıdır.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-22 | 1.0 | İlk sürüm: T1 tasarım kararları (#245 maddeler 1, 2, 8, 13) | Bilgi İşlem |
| 2026-10-22 | 1.1 | §2: evlilik bilgisi yalnızca İK ve Sistem Yöneticisi; §4 ve §5 kararları onaylandı (22.10.2026, #265) | Bilgi İşlem |
| 2026-10-22 | 1.2 | Revizyon (T1 tasarım gözden geçirmesi #267, #268): korumalı alanlar ayrı tablolarda ve tip düzeyinde test (B-11); sıralama ve küme işlemleri (B-07); TCKN araması (B-17); izin tablosu ve dağılım (B-14, B-16); erişim kaydında süzgeç değeri yazılmaz, dışa aktarmada kişi kimlikleri, küme işleminde `SpecialCategoryView` (B-01, B-08, B-09); §6 çok sicilli kişi (B-15) | Bilgi İşlem |
| 2026-10-23 | 1.3 | §4: denetim kayıtları ve parametre izinleri İK'da da (`KR-128`, #278) | Bilgi İşlem |
