# ADR-0021 — T1 Veri Modeli Genişlemesi

**Durum:** **Kabul Edildi**
**Tarih:** 2026-10-22
**Karar defteri karşılığı:** `KR-123`
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
| `person` (genişler) | LOGO | Cinsiyet, önceki soyadı, medeni hâl, eğitim (tanımın adı), adres, kan grubu (şifreli, ADR-0020) |
| `person_photo` (yeni) | LOGO | Fotoğrafın küçük hâli ve kaynak özeti (§5) |
| `employment_assignment` (yeni, tarih aralıklı) | LOGO | Şube, birim, ünvan kodu, statü, SGK meslek kodu ve adı |
| `employment_duty` (yeni, tarih aralıklı) | HRMS | Görev (referans veri tanımı) |
| `person_marriage` (yeni) | HRMS | Evlilik tarihi, evlilik yardımı alındı mı |
| `emergency_contact` (yeni, birden çok) | HRMS | Yakınlığı, ad, soyad, cep telefonu |
| `branch`, `department` (yeni) | LOGO | Şube (şehir dahil) ve birim tanımları, firma altında |

ADR-0005 §3, görev ve ünvanı aynı `employment_assignment` tablosunda öngörüyordu. Görev
artık HRMS'te girildiği için ayrı tabloya alınır; ünvan LOGO'dan geldiği için
atamada kalır.

### 2. Tarih aralıkları

- `employment_assignment` ve `employment_duty`, ADR-0004 §7'ye uyar:
  - `valid_from` / `valid_to` (dışlayan üst sınır) kullanılır;
  - aynı istihdamda çakışan aralık `EXCLUDE USING gist` ile engellenir;
  - açık uçlu kayıt en fazla bir tanedir.
  Bunun için `btree_gist` eklentisi migration ile kurulur. Eklenti PostgreSQL 13'ten beri
  güvenilir sayılır ve tablo sahibi tarafından kurulabilir.
- **LOGO yalnızca bugünkü değeri verir.** Senkronizasyon, değer değiştiğinde açık kaydı
  değişikliği gördüğü günle kapatır ve yeni kaydı açar. Geçmiş, T1'in devreye girdiği günden
  başlar. Önceki dönemler için geçmiş yoktur ve uydurulmaz.
- Görev değişikliğini İK girer. Önceki görev yeni görevin başlangıç tarihiyle kapanır.

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
  her çalışmada yalnızca `HASHBYTES('SHA2_256', LDATA)` özetini okur. Görüntünün kendisi
  yalnızca özet değiştiyse okunur (PG-PERS-41). Böylece her 15 dakikada yaklaşık 271 MB
  okunmaz.
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
- Fotoğraf ucu, kart ile aynı kapsam ve görünürlük kurallarına tabidir (ADR-0019). Tarayıcı
  önbelleği için ETag olarak kaynak özeti kullanılır.

### 6. Veritabanı kuralları (#246'dan)

- **Enum kolonları** iş tablolarında tamsayı ve `CHECK` kısıtlıdır. T1'in yeni enum
  kolonları kısıtla gelir. Mevcut 8 kolonun eksik kısıtları aynı migration'da eklenir.
  `DatabaseConventionTests`'e bir kural testi eklenir: tamsayı enum kolonunun `CHECK`
  kısıtı yoksa test düşer (ADR-0004 §3 notu).
- **Yumuşak silme** ilk kez kullanılır: acil durum kişisi ve görev kaydı silinmez,
  `ISoftDeletable` ile işaretlenir (ADR-0004 §5). Evlilik bilgisi de aynı kuralla silinir.
- **Sınıflandırma:** Her yeni metin ve tarih alanı `KR-120`'ye göre sınıflandırılır:
  - adres, telefon ve önceki soyadı `[PersonalData]` ile işaretlenir;
  - kan grubu `[SpecialCategoryData]` ile işaretlenir;
  - şube, birim ve ünvan `[AuditVisible]` ile, gerekçesiyle işaretlenir.
- **Karşılaştırma kuralı:** Şube, birim, ünvan ve görev adları ile önceki soyadı Türkçe ICU
  kuralını kullanır (ADR-0004 §10).

### 7. Türkçe karaktersiz arama

- Kişi için bir **arama anahtarı** kolonu tutulur: ad, soyad, önceki soyadı ve sicil, Türkçe
  harflerden arındırılmış küçük harfle yazılır (ç→c, ğ→g, ı→i, İ→i, ö→o, ş→s, ü→u).
  Kolonu uygulama yazar ve üzerinde bir ifade dizini bulunur.
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

## Geri dönüş maliyeti

Orta. Tablo ayrımı ve tarih aralıkları migration'la değiştirilebilir; fotoğraflar LOGO'dan
yeniden üretilebilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-22 | 1.0 | İlk sürüm: T1 tasarım kararları (#245 maddeler 5, 7, 12, 13) | Bilgi İşlem |
| 2026-10-22 | 1.1 | §7: ad gösterimi LOGO'daki gibi büyük harf (İK, 22.10.2026, #265) | Bilgi İşlem |
