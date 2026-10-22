# ADR-0021 — T1 Veri Modeli Genişlemesi

**Durum:** **Kabul Edildi**
**Tarih:** 2026-10-22
**Karar defteri karşılığı:** `KR-123`, `KR-126`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)
**Genişlettiği:** ADR-0003 (LOGO entegrasyonu), ADR-0004 §3, §5, §7, §10; ADR-0005 §3, §5
**Ayrıldığı yer:** ADR-0013 §2 (fotoğraflar veritabanında; gerekçe §5)
**İlgili gereksinimler:** PG-PERS-12, 19–23, 32, 36, 38, 41, 43, 44, 45; `KR-112`, `KR-114`
**İlgili iş:** #245 maddeler 5, 7, 12, 13

---

## Bağlam

T3, kişi ve istihdamı yalnızca kendi gereksinimlerinin gerektirdiği alanlarla kurdu
(`KR-077`). T1 bu çekirdeği üç yönde genişletir:

1. **LOGO'dan gelen yeni alanlar:**
   - fotoğraf, cinsiyet, önceki soyadı, medeni hâl, eğitim, kan grubu ve adres;
   - şehir, şube, birim, ünvan, statü ve SGK mesleği.
   Eşleme toplantı kaydı §6.1'dedir.
2. **HRMS'te tutulan alanlar** (`KR-112`): görev, evlilik tarihi ve evlilik yardımı, acil
   durum kişileri.
3. **Tarih aralıklı nitelikler:** Şube, birim ve görev değişince geçmiş sorulabilmelidir
   (PG-PERS-12; görev sicile bağlı ve tarih aralıklı, `KR-116`).

Aynı zamanda iki ilke korunmalıdır:
- LOGO'dan gelen alan HRMS'te düzenlenmez (`KR-003`);
- senkronizasyon HRMS'te girilen alanın üzerine yazmaz.

## Karar

### 1. Kaynağa göre ayrım

Her alanın **tek bir kaynağı** vardır ve bu kaynak tablo düzeyinde ayrılır.
Senkronizasyon yalnızca LOGO kaynaklı tablolara yazar. İK ekranları yalnızca HRMS
kaynaklı tablolara yazar. Bir satırda iki kaynağın alanı birlikte durmaz; aksi hâlde
senkronizasyon İK'nın girdiğini ezerdi.

| Tablo | Kaynak | İçerik |
|---|---|---|
| `person` (genişler) | LOGO | Cinsiyet, önceki soyadı, medeni hâl, eğitim (tanımın adı) |
| `person_sensitive` (yeni, kişi başına bir satır) | LOGO | **Korumalı alanlar:** adres, kan grubu (şifreli, ADR-0020) |
| `person_photo` (yeni) | LOGO | Fotoğrafın küçük hâli ve kaynak özeti (§5) |
| `employment_assignment` (yeni, tarih aralıklı) | LOGO | Şube, birim, ünvan kodu, statü, SGK meslek kodu ve adı |
| `employment_duty` (yeni, tarih aralıklı) | HRMS | Görev (referans veri tanımı) |
| `person_marriage` (yeni) | HRMS | Evlilik tarihi, evlilik yardımı alındı mı |
| `emergency_contact` (yeni, birden çok) | HRMS | Yakınlığı, ad, soyad, cep telefonu |
| `branch`, `department` (yeni) | LOGO | Şube (şehir dahil) ve birim tanımları, firma altında |

ADR-0005 §3, görev ve ünvanı aynı `employment_assignment` tablosunda öngörüyordu. Görev
artık HRMS'te girildiği için ayrı tabloya alınır; ünvan LOGO'dan geldiği için atamada kalır.
"Çalışma şekli" de ADR-0005 §3'te atamada sayılıyordu. LOGO'da verisi olmadığı için
(`WORKTYPE`, 588 aktif kartın tamamında 0) değişiklik talebiyle PG-PERS-12'den çıkarıldı
(#270, `KR-126`); modelde yer almaz (revizyon 1.2, B-05).

**Korumalı alanların ayrı tabloda tutulması** (revizyon 1.2, B-11): Adres ve kan grubu
`person` tablosunda olsaydı, `Person`'ı okuyan her kod bunları da yüklerdi; Kimlik modülünün
hesap listesi ve oturum deposu dahil. Ayrı tabloda dururlarsa yalnızca kapsamlı sorgu
bileşeni ve senkronizasyon onlara erişir. Mimari testi bunu tip düzeyinde denetler
(ADR-0019 §1, §3). Evlilik bilgisi ve acil durum kişileri zaten ayrı tablolardadır.

**`branch` ve `department` ile ADR-0005'teki `organization_unit`** (revizyon 1.2, B-16): T1
LOGO'daki iki düz tanımı kurar. ADR-0005 §5'teki `organization_unit`, birimleri hiyerarşiyle
birbirine bağlayan T2 yapısıdır. T2, `department`'ı ya bu yapıya taşır ya da onunla eşler;
karar T2 tasarımına aittir. T1 `organization_unit` tablosunu kurmaz.

### 2. Tarih aralıkları

- `employment_assignment` ve `employment_duty`, ADR-0004 §7'ye uyar:
  - `valid_from` / `valid_to` (dışlayan üst sınır) kullanılır;
  - aynı istihdamda çakışan aralık `EXCLUDE USING gist` ile engellenir;
  - açık uçlu kayıt en fazla bir tanedir.
  Bunun için `btree_gist` eklentisi migration ile kurulur. Eklenti PostgreSQL 13'ten beri
  güvenilir (trusted) sayılır; veritabanında `CREATE` yetkisi olan rol kurabilir. Migration'ı
  uygulayan sahip rolde bu yetki vardır; çalışma rolü `hrms_app` eklentiyi kurmaz, yalnızca
  kısıtlı tablolara yazar (`KR-118`; revizyon 1.2, B-19).
- **LOGO yalnızca bugünkü değeri verir.** Senkronizasyon, değer değiştiğinde açık kaydı
  değişikliği gördüğü günle kapatır ve yeni kaydı açar. Geçmiş, T1'in devreye girdiği günden
  başlar. Önceki dönemler için geçmiş yoktur ve uydurulmaz.
- Görev değişikliğini İK girer. Önceki görev yeni görevin başlangıç tarihiyle kapanır.

**Tarih aralığı kuralları** (revizyon 1.2, B-06). Her biri bir entegrasyon testiyle sınanır.

| Durum | Kural |
|---|---|
| Gün sınırı | **Europe/Istanbul** takvim günü. "Değişikliği gördüğü gün" UTC'ye göre değil, İstanbul saatine göre belirlenir |
| Aynı gün ikinci değişiklik | Aynı gün açılmış açık kayıt **güncellenir**, yeni kayıt açılmaz. Böylece sıfır uzunlukta aralık oluşmaz; günün son değeri geçerli olur |
| Yumuşak silme ve çakışma | `EXCLUDE` kısıtı yalnızca silinmemiş kayıtlara uygulanır (`WHERE deleted_at IS NULL`); silinmiş görev, yerine girilen görevle çakışmaz |
| Geçmişe dönük görev girişi | İK, başlangıcı geçmişte olan bir görev girebilir. Mevcut kayıtlarla çakışırsa giriş reddedilir; mevcut kaydı kısaltmak için önce o kayıt düzeltilir |
| İstihdam bitince | Açık görev ve açık atama, istihdamın **çıkış tarihinin ertesi günü** kapanır (dışlayan üst sınır) |
| Çakışma hatası | Veritabanının çakışma hatası (23P01) `409 Conflict` olarak, Problem Details biçiminde döner; `500` dönmez |
| T1 öncesi bir tarih | Sorgu o tarih için kayıt bulamaz ve "kayıt yok" döner. Değer tahmin edilmez |

### 3. Organizasyon tanımları

LOGO'da **birim şubeye değil firmaya bağlıdır** (`L_CAPIDEPT` firma ile birleşir; toplantı
kaydı §6.1). ADR-0005 §5'teki "firma → şube → birim" hiyerarşisi LOGO verisinde yoktur.
T1 bu nedenle şubeyi ve birimi **ayrı ayrı firmaya bağlı** tanımlar olarak kurar. Birimleri
birbirine bağlayan hiyerarşi T2 Organizasyon Yönetimi'nde kurulur. ADR-0005 §5'e bu yönde bir
gerçekleştirme notu düşülür.

### 4. LOGO okuma kuralları

- **Okunan alan listesi tektir ve testle korunur.** `LogoSchema` okunan her tablo ve kolonu
  listeler. Bir test, din (`RELIGION`) ve sağlık tablolarının (`LH_001_HEALTH`) listede
  olmadığını doğrular (PG-PERS-36). Kan grubu (`BLOODGROUP`) listededir ve `KR-113`'e
  dayanır.
- Yeni tablolar şema denetimine eklenir: `LH_001_FAMILY`, `LH_001_PERIDINF`,
  `LH_001_TYPEDEF`, `L_CAPIDIV` ve `L_CAPIDEPT` (PG-PERS-08).
- Aile/resim tablosunda her kartın tek bir satırı vardır (yakınlık = 0). Satır bu koşulla
  seçilir (toplantı kaydı §11.9).
- LOGO'ya yazma yoktur; bütün yeni sorgular aynı salt okunur bağlamdan geçer (ADR-0003 §1).

### 5. Fotoğraf

- **Değişiklik tespiti sunucuda yapılır.** LOGO, SQL Server 2019'dur (15.0). Senkronizasyon
  her çalışmada yalnızca `HASHBYTES('SHA2_256', CAST(LDATA AS varbinary(max)))` özetini okur;
  `LDATA` eski `image` türündedir, bu yüzden `CAST` gerekir. Görüntünün kendisi yalnızca özet
  değiştiyse okunur (PG-PERS-41). Böylece her 15 dakikada yaklaşık 271 MB ağdan taşınmaz.
- **LOGO sunucusundaki yük** (revizyon 1.2, B-13): Özet, sunucunun bütün görüntüleri okumasını
  gerektirir; aile/resim tablosunda değiştirilme tarihi yoktur (toplantı kaydı §11.9 ve T1
  tasarım gözden geçirmesi BG-03). Yük **UAT'de ölçülür**: süre ve okunan sayfa sayısı. Ölçüm
  kabul edilemezse sırasıyla şunlar uygulanır:
  1. Özetten önce `DATALENGTH` karşılaştırılır; boyutu değişmeyen fotoğrafın özeti yalnızca
     günde bir kez alınır.
  2. Fotoğraf özeti 15 dakikalık çalışmadan ayrılıp günde bir kez gece alınır.
  Seçilen yöntem ve ölçüm bu ADR'ye gerçekleştirme notu olarak yazılır.
- **HRMS orijinali saklamaz.** Görüntü senkronizasyonda en fazla 400 piksele küçültülür ve
  JPEG olarak `person_photo` tablosunda tutulur (tahmini ≈30 KB, toplam ≈15 MB). Liste
  sayfası bu küçük hâli kullanır. 24 orijinal fotoğraf yaklaşık 6,7 MB eder;
  KPÖ-PERS-1'in (p95 < 2 sn) karşılanamamasının ana nedeni olurdu.
- **ADR-0013 §2'den ayrılış:** Dosyalar veritabanında saklanmaz kuralı, kullanıcının
  yüklediği belgeler (virüs taraması, büyük boyut, yedek şişmesi) içindir. Fotoğraf ise
  LOGO'nun kopyasıdır: kaybolursa yeniden senkronize edilir, küçüktür ve senkronizasyonla
  aynı işlemde yazılır. NAS altyapısı da henüz kurulmadı.
- **Küçültme kütüphanesi:** SkiaSharp (MIT). Linux konteyneri için bağımlılıksız yerel
  paket kullanılır. ImageSharp'ın lisansı ciroya bağlı olduğu için kullanılmaz (`KR-025`
  ile aynı ilke). Paket envanterine eklenir.
- Fotoğraf ucu, kart ile aynı kapsam ve görünürlük kurallarına tabidir (ADR-0019); kapsam
  dışı kişinin fotoğrafı `404` döner. Tarayıcı önbelleği için ETag olarak kaynak özeti
  kullanılır. Yanıt `Cache-Control: private` taşır; ortak önbelleklerde (vekil) saklanmaz.
  Fotoğraf ayrıca erişim kaydı üretmez: fotoğraf, erişim kaydı yazılan kart veya liste
  görüntülemesinin parçasıdır (revizyon 1.2, B-18).

### 6. Veritabanı kuralları (#246'dan)

- **Enum kolonları** iş tablolarında tamsayı ve `CHECK` kısıtlıdır. T1'in yeni enum
  kolonları kısıtla gelir. Mevcut 8 kolonun eksik kısıtları aynı migration'da eklenir.
  `DatabaseConventionTests`'e bir kural testi eklenir: tamsayı enum kolonunun `CHECK`
  kısıtı yoksa test düşer (ADR-0004 §3 notu).
- **Yumuşak silme** ilk kez kullanılır: acil durum kişisi ve görev kaydı silinmez,
  `ISoftDeletable` ile işaretlenir (ADR-0004 §5). Evlilik bilgisi de aynı kuralla silinir.
- **Sınıflandırma:** Her yeni alan `KR-120`'ye göre sınıflandırılır:
  - `[PersonalData]`: adres, telefon, önceki soyadı, evlilik tarihi, evlilik yardımı,
    acil durum kişisinin adı, soyadı, yakınlığı ve telefonu, cinsiyet, medeni hâl;
  - `[SpecialCategoryData]`: kan grubu ve düzeltme talebinde önerilen kan grubu;
  - `[AuditVisible]` (gerekçesiyle): şube, birim, ünvan, statü, SGK meslek kodu ve adı,
    eğitim, görev.
- **Sınıflandırma testinin kapsamı genişler** (revizyon 1.2, B-12). `DataClassificationTests`
  yalnızca metin ve tarih alanlarını değil, kimlik ve yabancı anahtarlar dışındaki **bütün
  skaler alanları** tarar: `bool`, enum ve sayılar dahil. Kapsamına `Dsg.Hrms.Domain.Personnel`'e
  ek olarak `Reference` ve bildirim ad alanlarının T1'de eklenen varlıkları (`inbox_item`,
  düzeltme talebi) da girer.
- **Karşılaştırma kuralı:** Şube, birim, ünvan ve görev adları ile önceki soyadı Türkçe ICU
  kuralını kullanır (ADR-0004 §10).

### 7. Türkçe karaktersiz arama

- Kişi için bir **arama anahtarı** kolonu tutulur: ad, soyad, önceki soyadı ve sicil, Türkçe
  harflerden arındırılmış küçük harfle yazılır (ç→c, ğ→g, ı→i, İ→i, ö→o, ş→s, ü→u).
  Kolonu uygulama yazar.
- **Önceki soyadı** anahtara yalnızca ekranda gösterildiği koşulda girer: kadın personelde ve
  LOGO'da doluysa (PG-PERS-19). Gösterilmeyen bir değerle bulunabilmek, değerin varlığını ele
  verirdi (revizyon 1.2, B-17).
- **Dizin:** Arama "içinde geçen" biçimdedir; B-tree dizini bu aramada kullanılmaz. T1
  ölçeğinde (≈1.400 kişi) sıralı tarama yeterlidir. Ölçüm KPÖ-PERS-1'i karşılamazsa `pg_trgm`
  dizini eklenir (revizyon 1.2, B-17).
- TCKN ile arama yalnızca tam TCKN'yi görme izni olana açıktır (ADR-0019 §2).
- Arama terimi aynı kuralla dönüştürülür. "isik" IŞIK'ı, "ismail" İSMAİL'i bulur; "ışık"
  yazmak da aynı sonucu verir. Bu, ADR-0004 §10'un "açıkça normalize edilmiş kolon" ilkesidir.
- **Sıralama** Türkçe ICU kuralıyla ad ve soyad kolonlarından yapılır (#261).

> **Karar (22.10.2026, İK):** Adlar ekranda **LOGO'daki gibi tamamı büyük harfle** gösterilir;
> gösterim için dönüşüm yapılmaz.

## Gerekçe

- Kaynağa göre tablo ayrımı, iki kaynaklı verinin en sık hatasını (senkronizasyonun elle
  girileni ezmesi) yapısal olarak önler.
- Tarih aralıkları LOGO'nun bugünkü değerinden üretilir; geçmişi uydurmak yerine geçmişin
  nereden başladığı açıkça kayda geçer.
- Fotoğrafın özetle izlenmesi ve küçük hâlinin saklanması, LOGO yükünü (PG-PERS-41) ve
  liste performansını (PG-PERS-39) birlikte çözer.
- Arama anahtarı, Türkçe ICU kuralının çözemediği "Türkçe karaktersiz yazım" sorununu
  çözer; sıralama yine ICU ile doğru kalır.

## Değerlendirilen alternatifler

| Alternatif | Artıları | Eksileri | Neden seçilmedi |
|---|---|---|---|
| Görev ve ünvan aynı atama tablosunda | ADR-0005 §3 ile aynı | Senkronizasyon İK'nın girdiği görevi ezer | İki kaynak bir satırda |
| Fotoğraf NAS'ta (ADR-0013) | Kurala uygun | NAS kurulmadı; LOGO kopyası için virüs taraması gereksiz | Altyapı yok, fayda yok |
| Orijinal fotoğrafı saklamak | Kalite | ≈137–271 MB; liste yavaş | KPÖ-PERS-1 |
| Her çalışmada fotoğrafı okuyup karşılaştırmak | Basit | Her 15 dakikada ≈271 MB LOGO okuması | PG-PERS-41 |
| ImageSharp | Yaygın | Ciroya bağlı lisans | `KR-025` ilkesi |
| `unaccent` eklentisi | Hazır | Türkçe ı/İ dönüşümünü doğru yapmaz; eklenti bağımlılığı | Uygulama dönüşümü daha öngörülebilir |
| Şube altında birim (ADR-0005 §5) | Hiyerarşi | LOGO verisinde yok | T2'de kurulur |

## Sonuçlar

**Olumlu:**
- Her alanın tek kaynağı vardır; senkronizasyon ile İK girişi çakışmaz.
- "Geçen yıl hangi şubedeydi" sorusu T1'in devreye girdiği günden itibaren cevaplanır.
- Liste sayfası küçük fotoğraflarla hızlı açılır.

**Olumsuz / kabul edilen ödünler:**
- Geçmiş, T1'in devreye girdiği günden başlar.
- Fotoğraf kalitesi en fazla 400 piksel ile sınırlıdır; PDF dışa aktarmada da bu boyut kullanılır.
- SkiaSharp yerel kütüphane getirir; konteyner taramasında izlenir.

**Bu kararın getirdiği yükümlülükler:**
- ADR-0005 §3 ve §5'e gerçekleştirme notu düşülür (görev ayrı tabloda; birim firmaya bağlı).
- Paket envanterine SkiaSharp eklenir.
- `LogoSchema`, okunan alan listesi testi ve şema denetimi yeni tablolarla aynı PR'da gelir.
- `ApplicationRoleTests`'e `hrms_app` ile `EXCLUDE` kısıtlı tabloya ekleme ve çakışma
  senaryosu eklenir (B-19); tarih aralığı kurallarının her biri için entegrasyon testi yazılır (B-06).
- Fotoğraf özetinin LOGO yükü UAT'de ölçülür ve sonucu bu ADR'ye yazılır (B-13).

## Geri dönüş maliyeti

Orta. Tablo ayrımı ve tarih aralıkları migration'la değiştirilebilir; fotoğraflar LOGO'dan
yeniden üretilebilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-22 | 1.0 | İlk sürüm: T1 tasarım kararları (#245 maddeler 5, 7, 12, 13) | Bilgi İşlem |
| 2026-10-22 | 1.1 | §7: ad gösterimi LOGO'daki gibi büyük harf (İK, 22.10.2026, #265) | Bilgi İşlem |
| 2026-10-22 | 1.2 | Revizyon (#267, #268): korumalı alanlar `person_sensitive` tablosunda (B-11); çalışma şekli kapsam dışı (#270, B-05); `branch`/`department` ile `organization_unit` (B-16); tarih aralığı kuralları (B-06); `btree_gist` yetkisi (B-19); `HASHBYTES` için `CAST` ve LOGO yükü ölçümü (B-13); fotoğraf ucu önbellek ve kayıt (B-18); sınıflandırma kapsamı (B-12); arama anahtarı ve dizin (B-17) | Bilgi İşlem |
