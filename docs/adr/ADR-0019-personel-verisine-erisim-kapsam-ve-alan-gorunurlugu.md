# ADR-0019 — Personel Verisine Erişim: Kapsam ve Alan Görünürlüğü

**Durum:** **Kabul Edildi**
**Tarih:** 2026-10-22
**Karar defteri karşılığı:** `KR-121`
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
  Bunun yerine bir **mimari testi**, `Person` tablosuna doğrudan erişimi (`Set<Person>()`)
  yalnızca izin verilen sınıflarda kabul eder: senkronizasyon, üyelik, hesap işlemleri ve
  kapsamlı sorgu bileşeni. Listeye yeni sınıf eklemek PR'da görünür bir karardır.

### 2. Alan düzeyi: tek politika, üç kullanım yeri

- `PersonFieldVisibility` politikası korumalı her alan için kimin görebileceğini tanımlar:

  | Alan | Görebilen |
  |---|---|
  | Tam TCKN | `personnel.person.view_national_id` izni (İK, Sistem Yöneticisi). Diğerleri maskeli görür, kişinin kendisi dahil (S-01) |
  | Adres | `personnel.person.view_sensitive` izni veya kişinin kendisi |
  | Kan grubu | `personnel.person.view_sensitive` izni veya kişinin kendisi |
  | Acil durum kişileri | `personnel.person.view_sensitive` izni veya kişinin kendisi |
  | Diğer kart alanları | Kaydı görebilen herkes |

- Aynı politika **üç yerde** kullanılır ve başka yerde yeniden yazılmaz:
  1. **API yanıtı:** Göremeyen kullanıcının yanıtında alan **yer almaz** (maskeli TCKN
     hariç). Ekranda gizlemek yetmez.
  2. **Filtre listesi:** Liste ekranı, kullanıcının görebildiği alanların filtrelerini alır;
     göremediği alana göre filtreli istek `400` ile reddedilir.
  3. **Dışa aktarma alan listesi:** Seçilebilir alanlar politikadan gelir (PG-PERS-42).
- **Test:** Her korumalı alan için "yetkisi olmayan kullanıcının API yanıtında, filtresinde
  ve dışa aktarma seçeneklerinde yok" testi yazılır. Bu, T1'in yetki sızıntısı testidir.

### 3. Kimlik modülünün kişi sorguları

Kimlik modülü `Person`'ı doğrudan okumaya devam eder, ama **yalnızca kendi gereksiniminin
alanlarını** okur: ad, soyad, kurumsal e-posta, cep telefonu, TCKN ile doğum tarihi
(eşleştirme) ve istihdam. Bir mimari testi Kimlik'in `Person`'ın korumalı alanlarına
(adres, kan grubu, acil durum, evlilik bilgisi) dokunmasını reddeder. Böylece hesap
işlemleri listesi T1'in alan politikasını atlayan bir yol olmaz. Kimlik'in Personnel'in
okuma arayüzüne taşınması, T4'te rol ve kapsam yeniden düzenlenirken değerlendirilir.

### 4. İzinler ve rol

| İzin | Anlamı |
|---|---|
| `personnel.person.view` | Tüm personeli görme (kapsam `All`) |
| `personnel.person.view_national_id` | Tam TCKN |
| `personnel.person.view_sensitive` | Adres, kan grubu, acil durum kişileri |
| `personnel.person.update` | HRMS'te tutulan alanları girme (görev, evlilik bilgisi, acil durum kişisi; `KR-112`) |
| `personnel.person.export` | Dışa aktarma (ADR-0014 §4) |
| `personnel.sync.view`, `personnel.sync.create` | Senkronizasyon durumu ve elle başlatma (T3'teki `identity.sync.*` buraya taşınır; B-10) |

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
  cevaplanır (B-07'nin ikinci kısmı). Bedeli kayıt hacmidir: 24 kartlık bir sayfa için
  24 kimlik.
- **Dışa aktarma:** Süzgeç, kayıt sayısı ve **aktarılan alanların listesi** yazılır
  (ADR-0014 §4).

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
- Kimlik modülü hâlâ `Person` tablosunu doğrudan okur; sınır testle korunur, kod
  düzeyinde ayrılmaz.

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
