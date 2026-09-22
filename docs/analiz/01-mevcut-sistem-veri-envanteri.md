# Mevcut Sistem Veri Envanteri ve Ön Bulgular

**Belge kimliği:** ANL-001
**Son güncelleme:** 2026-09-07
**Kapsam:** LOGO Bordro veritabanı (MSSQL) ve mevcut HRMS veritabanı (PostgreSQL)
**İlgili süreçler:** TEC.2 (paydaş gereksinimleri), TEC.10 (geçiş / veri göçü), MAN.4 (risk)

> **Amaç:** Yeni sistemin veri modelini ve göç planını sağlam temele oturtmak için mevcut
> iki veri kaynağının hacim ve kalite fotoğrafını çekmek. Bu belge **ön envanterdir**;
> her modülün ayrıntılı veri profillemesi, o modül geliştirilirken ilgili modül klasöründe
> yapılacaktır.
>
> **Erişim notu:** Her iki kaynağa da **yalnızca okuma** amaçlı erişilmiştir. LOGO
> veritabanına hiçbir yazma/güncelleme/silme işlemi yapılmamıştır ve yapılamaz
> (bkz. §1.1 yetki doğrulaması).

---

## 1. LOGO Bordro veritabanı (MSSQL)

**Sunucu sürümü:** Microsoft SQL Server 2019 Standard Edition (Windows Server 2022)
**Veritabanı:** BORDRO

### 1.1 Salt-okunur erişim doğrulaması

Proje için ayrı bir SQL oturum açma kaydı (`hrms_logo_reader`) oluşturulmuş, `db_datareader`
rolü verilmiş ve `INSERT/UPDATE/DELETE/ALTER/EXECUTE` yetkileri açıkça reddedilmiştir (DENY).

**Doğrulama testi (2026-09-03):** Hiçbir satırı etkilemeyecek bir `UPDATE` denemesi
yapılmış, veritabanı beklendiği gibi şu hatayı döndürmüştür:

```
The UPDATE permission was denied on the object 'LH_001_PERSON',
database 'BORDRO', schema 'dbo'.
```

**Sonuç:** LOGO tarafında yazma işlemi teknik olarak imkânsızdır. Bu, bir taahhüt değil
uygulanmış bir kontroldür ve düzenli olarak yeniden doğrulanacaktır (bkz. TEC.9).

### 1.2 Organizasyon hacmi

| Varlık | Adet | Kaynak tablo |
|---|---:|---|
| Firma | 7 | `L_CAPIFIRM` |
| Şube | 32 | `L_CAPIDIV` |
| Birim | 128 | `L_CAPIDEPT` |
| Personel kartı (toplam) | 1.533 | `LH_001_PERSON` |
| — Aktif çalışan (`TYP=1`) | 587 | |
| — İşten ayrılan | 946 | |

**Aktif personelin firmalara dağılımı:**

| Firma | Aktif personel |
|---|---:|
| DÜZEN A.Ş. | 267 |
| DÜZEN LTD | 218 |
| POLİKLİNİK A.Ş. | 38 |
| NORWEST | 35 |
| KASIMLAR | 19 |
| LABPT | 10 |
| RADYOLOJİ A.Ş. | 0 |
| **Toplam** | **587** |

> **Bulgu:** Sistem çok firmalı (multi-company) olarak tasarlanmak zorundadır. Yetkilendirme
> kapsamı, izin kuralları ve raporlamada firma boyutu birinci sınıf bir kavramdır.
> RADYOLOJİ A.Ş.'de aktif personel bulunmamaktadır; firma tanımının korunup korunmayacağı
> İK ile teyit edilecektir.

### 1.3 Tablo yapısı bulgusu — tek personel tablosu

`LH_%_PERSON` desenine uyan **yalnızca bir tablo** vardır: `LH_001_PERSON`. Yani yedi firmanın
tamamının personeli tek tabloda tutulmakta, firma ayrımı `FIRMNR` alanı ile yapılmaktadır.

> **Sonuç:** Tablo adları firma numarasına göre çoğalmadığı için, LOGO erişiminde Entity
> Framework Core ile sabit varlık eşlemesi (entity mapping) yapılabilir. Bu bulgu
> `ADR-0003 LOGO Entegrasyon Stratejisi` kararının dayanağıdır.

### 1.4 Kimlik verisi kalitesi — kritik bulgular

| Ölçüm | Değer |
|---|---:|
| TCKN dolu kart sayısı | 1.531 |
| **TCKN boş kart sayısı** | **2** |
| Tekil TCKN (kişi) sayısı | 1.360 |
| **Birden fazla sicili olan kişi sayısı** | **140** |
| **Aynı anda birden fazla AKTİF sicili olan kişi sayısı** | **12** |

**Yorum:**

1. **1.531 kart ↔ 1.360 kişi.** Kart (sicil) ile kişi aynı şey değildir. 140 kişi LOGO'da
   birden fazla sicil kaydına sahiptir. Bu, İK biriminin bildirdiği iki durumun sayısal
   karşılığıdır: (a) emekli olup işe devam eden personele yeni sicil açılması,
   (b) aynı kişinin farklı görevler için ayrı sicillerde tanımlanması.

2. **12 kişinin eş zamanlı birden fazla aktif sicili vardır.** Bu, veri modelinde
   `Kişi (Person)` ve `İstihdam (Employment)` ayrımının **zorunlu** olduğunu kanıtlar;
   tek tablolu bir personel modeli bu 12 kişiyi doğru temsil edemez.

3. **Kullanıcı hesabı kişi bazlıdır.** Bir kişinin kaç sicili olursa olsun tek hesabı olur;
   hesap, kişinin tüm aktif istihdamlarını kapsar. (Karar: `KR-014`)

4. **TCKN'si boş kartlar incelendi ve kapsam dışı bırakıldı.** Toplam iki kayıt vardır:
   biri **işten ayrılmış** bir personele aittir (TCKN'nin sonradan elde edilmesi pratik
   değildir), diğeri (`0001000` sicili) **gerçek bir personel kaydı değildir**.
   Karar (`KR-034`): veri kalitesi kontrolleri yalnızca **aktif personel** üzerinde
   çalışır ve `0001000` sicili tüm kontrollerin dışında tutulur.
   Bu kural uygulandığında **TCKN'si boş aktif personel kalmamaktadır.** → Risk `R-07` kapandı

### 1.5 Üyelik akışı için kimlik ve iletişim verisi tamlığı

Üyelik akışında kişi TCKN, doğum tarihi ve kurumsal e-posta ile eşleştirilecek; doğrulama
kodu LOGO'da kayıtlı kurumsal e-posta veya cep telefonuna gönderilecektir.

**Ölçüm kapsamı (`KR-034`):** aktif personel, `0001000` sicili hariç →
**564 kişi / 584 sicil kaydı**
**Ölçüm tarihi:** 2026-09-22 (önceki ölçümler: 2026-09-21, 2026-09-09)

> **İK düzeltme çalışması sürüyor.** Tablo, İK biriminin LOGO üzerinde yaptığı
> iletişim bilgisi tamamlamasının ardından **iki kez** yeniden ölçülmüştür.
> Eksik sütunu kişi (TCKN) bazındadır.

| Alan | Eksik (22.09) | 21.09 | 09.09 | Kaynak |
|---|---:|---:|---:|---|
| **TCKN** | **0** ✅ | 0 | 0 | `LH_001_PERSON.TTFNO` |
| **Doğum tarihi** | **0** ✅ | 0 | 0 | `LH_001_PERSON.BIRTHDATE` |
| Kurumsal e-posta | **4** | 5 | 38 | `LH_001_CONTACT` (`TYP=6`) |
| Cep telefonu | **10** | 12 | 13 | `LH_001_CONTACT` (`TYP=3`) |

**Ek kontroller:**

| Kontrol | 22.09 | 21.09 | 09.09 |
|---|---:|---:|---:|
| Telefon biçimi geçersiz (normalize edilince 10 haneli ve `5` ile başlamıyor) | **0** ✅ | 0 | 1 |
| **Ne e-posta ne telefon — hiçbir iletişim bilgisi yok** | **2** ⚠️ | 4 | 12 |

> **İyi haber:** Aktif personelin tamamında TCKN ve doğum tarihi doludur. Üyelik akışının
> kimlik eşleştirme adımı bu iki alan açısından sorunsuz çalışacaktır.

**⚠️ En kritik bulgu — 2 personel hiçbir kanaldan doğrulanamaz.** (09.09'da 12, 21.09'da 4)

4 kişide e-posta, 10 kişide telefon eksiktir; bu iki kümenin **kesişimi 2 kişidir**:

| Durum | Kişi | 21.09 | 09.09 | Sonuç |
|---|---:|---:|---:|---|
| Yalnız e-posta eksik (telefonu var) | 2 | 1 | 26 | SMS ile doğrulanabilir |
| Yalnız telefon eksik (e-postası var) | 8 | 8 | 1 | E-posta ile doğrulanabilir |
| **İkisi de eksik** | **2** | **4** | **12** | **Hiçbir kanaldan doğrulanamaz** |

Bu 2 kişi, sistem devreye alındığında **kendi başına üye olamaz.** İki seçenek vardır:

1. İK, devreye alma öncesinde bu kişilerin iletişim bilgilerini LOGO'da tamamlar
   (**tercih edilen**), veya
2. Bu kişiler için İK'nın gerekçe girerek elle hesap açtığı **istisna akışı** kullanılır
   (Kullanıcı Yönetimi modülü).

→ Risk `R-08`

> Kurumsal adresi olmayanlar için SMS doğrulaması **zorunlu kılınmayacaktır** (`KR-020`);
> kalıcı çözüm İK'nın LOGO üzerinde bilgileri tamamlamasıdır.

### 1.5.1 Telefon alanının doğrulanması

SMS gönderiminde kullanılacak alanın gerçekten **cep telefonu** olduğu teyit edilmiştir.
`LH_001_CONTACT.TYP = 3` alanındaki numaraların ön ekleri tamamen Türkiye cep
operatörü aralığındadır (`53x`, `54x`, `50x`, `55x`). Sabit hat ön eki görülmemiştir.

Bazı kayıtlar başında `0` ile girilmiştir (`053…`, `054…`); bu, ADR-0012 §8'de
tanımlanan **numara normalizasyonunun** neden gerekli olduğunu göstermektedir.

### 1.6 E-posta adresi kalitesi — güvenlik bulguları

Üyelik doğrulama kodu e-posta ile gönderileceği için, e-posta adreslerinin **kişiye özel**
ve **kurum denetiminde** olması bir güvenlik gereksinimidir. Aktif personel üzerinde yapılan
ölçüm (2026-09-04):

**Alan adı dağılımı:**

| Alan adı | Kayıt | Önceki | Değerlendirme |
|---|---:|---:|---|
| `@duzen.com.tr` | 522 | 478 | Kurumsal ✅ |
| `@zeytinim.com` | 54 | 5 | Kurumsal ✅ |
| `@labpt.com.tr` | 3 | 3 | Kurumsal ✅ |
| `@gmail.com` | **0** | 50 | Kurumsal değil |
| `@hotmail.com` | **0** | 5 | Kurumsal değil |
| `@icloud.com` | **0** | 3 | Kurumsal değil |
| `@yahoo.com` | **0** | 2 | Kurumsal değil |

> Dağılım 2026-09-22 ölçümüne aittir (öncekiler: 2026-09-21, 2026-09-04). LOGO canlı bir sistem olduğu
> için toplam sayılar günden güne birkaç kayıt oynayabilir (bkz. §1.7).

**Bulgu 1 — Kurumsal olmayan adres: 0 aktif personel** (2026-09-22 ölçümünde de 0;
ilk ölçümde 59). İK, kişisel adreslerin tamamını kurumsal adreslerle değiştirmiştir.
→ Risk `R-13` **kapandı**

> **Teknik kontrol kaldırılmaz.** Bugün kişisel adres bulunmaması, yarın da
> bulunmayacağı anlamına gelmez: LOGO canlı bir sistemdir ve yeni kayıt her gün
> açılabilir. Kurumsal olmayan adrese doğrulama kodu göndermeme kuralı
> (`REQ-KMLK-008`, `KR-019`) kalıcı bir kontroldür.

**Bulgu 2 — Paylaşılan e-posta adresi: 0 personel, 0 adres** (2026-09-22 ölçümü;
21.09'da 4 personel / 1 adres, ilk ölçümde 4 personel / 1 adres).
İK, paylaşılan adres kullanan personele kendilerine ait adres tanımlamıştır.
→ Risk `R-12` **kapandı**

> **Teknik kontrol kaldırılmaz.** `R-13`'te olduğu gibi: bugün paylaşılan adres
> bulunmaması, yarın da bulunmayacağı anlamına gelmez. Paylaşılan bir adrese doğrulama
> kodu göndermeme kuralı (`REQ-KMLK-009`, `KR-018`) kalıcı bir kontroldür.

**Aşağıdaki paragraf, bulgunun ilk tespit edildiği hâliyle korunmuştur:**
İki e-posta adresi, farklı TCKN'li birden fazla aktif personele tanımlıdır. Bu, İK
biriminin öngördüğü durumu doğrular: kurumsal adresi olmayan personele başka bir kişinin
(örneğin sorumlusunun) adresi tanımlanmış olabilir. Kontrol yalnızca belirli bir görev
grubuyla sınırlandırılmamış, **tüm aktif personel** üzerinde çalıştırılmıştır.

> **Güvenlik sonucu:** Bu adrese gönderilen doğrulama kodunu alan kişi, **başkasının adına
> hesap açıp o kişinin özlük verilerine erişebilir.** Tasarım kararı: paylaşılan bir adrese
> doğrulama kodu gönderilmeyecek, kayıt istisna akışına yönlendirilecektir. → Risk `R-12`

**Bulgu 3 — Kurumsal alan adı listesi yapılandırılabilir olmalı.**
Kabul edilen alan adları bugün `@duzen.com.tr`, `@zeytinim.com`, `@labpt.com.tr`'dir. Grup
yapısı değişebileceği için bu liste koda gömülmeyecek, yönetilebilir bir parametre olarak
tutulacaktır.

### 1.7 Canlı veri hareketliliği üzerine bir gözlem

2026-09-03 tarihindeki ölçümde aktif personel sayısı **587**, 2026-09-04 tarihinde **585**
olarak bulunmuştur. Aradaki bir gün içinde LOGO'da iki personelin durumu değişmiştir.

> Bu, `ADR-0003` ile alınan **anlık görüntü (snapshot) tabanlı senkronizasyon** kararının
> pratik gerekçesidir: raporlar ve ekranlar canlı sorguya bağlı olsaydı, aynı raporun iki
> ayrı çalıştırmasında farklı sonuç vermesi kaçınılmaz olurdu. Yeni sistemde her senkronizasyon
> tarihli bir anlık görüntü üretecek ve raporlar hangi ana ait olduğunu gösterecektir.

### 1.8 LOGO sorgularında dikkat edilecek teknik nokta

Organizasyon tablolarında (`L_CAPIDIV`, `L_CAPIDEPT`, `L_CAPIFIRM`) `NR` alanı **firma
içinde tekildir, veritabanı genelinde değildir.** Birleştirmelerde yalnızca `NR` üzerinden
eşleşme yapılırsa aynı personel birden fazla kez listelenir.

**Doğru birleştirme koşulu:**

```sql
JOIN L_CAPIDIV  e ON e.NR = p.LOCNR  AND e.FIRMNR = p.FIRMNR
JOIN L_CAPIFIRM f ON f.NR = e.FIRMNR AND f.NR     = p.FIRMNR
JOIN L_CAPIDEPT d ON d.NR = p.DEPTNR AND d.FIRMNR = f.NR
```

Test sırasında bu koşul eksik bırakıldığında 40 kayıtlık bir liste 131 kayıt olarak
üretilmiştir. Firma sayısı yedi olduğu için hata büyük ölçekte oluşmaktadır.

> **Karar:** LOGO erişim katmanında bu birleştirme kuralı tek bir yerde tanımlanacak,
> her sorguda tekrarlanmayacaktır. Kayıt çoğalmasını yakalayan bir entegrasyon testi
> yazılacaktır (senkronizasyon sonrası kişi sayısı = kaynak kart sayısı kontrolü).

---

## 2. Mevcut HRMS veritabanı (PostgreSQL)

**Sunucu sürümü:** PostgreSQL 14.24 (Ubuntu 22.04)
**Veritabanı:** `human_resources_management`
**Şema:** `public` — **74 tablo**

> **Uyarı:** PostgreSQL 14, üretici tarafından **Kasım 2026'da** destek dışına çıkacaktır.
> Bu, mevcut sistemin yenilenmesi için ayrıca bir gerekçedir. Yeni sistem PostgreSQL'in
> güncel ve uzun destekli bir sürümü üzerine kurulacaktır. → Risk `R-09`

### 2.1 Veri hacmi (satır sayıları)

**Yüksek hacimli tablolar:**

| Tablo | Satır | Değerlendirme |
|---|---:|---|
| `personel_egitimleri_personel_sorulari_ve_verilen_cevaplar` | 44.525 | Eğitim sınav cevapları |
| `log` | 19.327 | Sistem log kaydı |
| `egitimler_bildirim_servisi_log` | 9.734 | Bildirim servisi logu |
| `personel_anketleri_sorular` | 7.998 | Anket soruları |
| **`yillik_izin`** | **7.494** | **Kritik göç verisi** |
| `giris_tokenleri` | 6.966 | Oturum jetonları (göç edilmeyecek) |
| **`personel_egitimleri`** | **2.508** | **Kritik göç verisi** |
| `bildirimler` | 1.906 | |
| `personel_egitimleri_degerlendirmeler` | 1.585 | |
| `egitimler_bildirim_servisi` | 851 | |
| `yillik_izin_2023_kalan_izin_gunleri` | 841 | 2023 devir bakiyeleri |
| `egitimler_bildirim_servisi_hata` | 700 | |
| **`personel_sertifikalari`** | **603** | **Kritik göç verisi** |
| `personel_anketleri` | 566 | |
| `personel_ise_giris_tarihleri` | 515 | |
| `kurum_egitimleri` | 167 | |
| `kurum_anketleri` | 108 | |
| `egitimler` | 43 | Eğitim tanımları |
| `ise_alim_gorusmeler` | 42 | |
| `ucretsiz_izin` | 30 | |

**Düşük hacimli / tanım tabloları (seçilmiş):**

| Tablo | Satır |
|---|---:|
| `yillik_izin_turleri` | 7 |
| `yillik_izin_kurallar` | 3 |
| `dini_izin_gunleri` | 5 |
| `is_goremez_izin` | 7 |
| `is_goremez_izin_turleri` | 2 |
| `talepler` | 8 |
| `talep_turleri` | 3 |
| `is_kazalari` | 2 |
| `isg_toplanti_tutanaklari` | 2 |
| `ise_alim_is_ilanlari` | 12 |
| `ise_alim_pozisyonlar` | 13 |
| `sirket_faaliyetleri` | 9 |
| `ayarlar` | 2 |
| `personel` | 1 |

### 2.2 Kritik bulgular

**a) `personel` tablosunda yalnızca 1 satır var.**
Bu, personel verisinin mevcut sistemde tutulmadığını, canlı olarak LOGO'dan okunduğunu
doğrular. Dolayısıyla göç sırasında personel eşleştirmesi **LOGO sicil numarası / TCKN**
üzerinden yapılacaktır. Eşleşmeyen kayıtlar mutabakat raporunda ayrıca listelenecektir.

**b) Boş veya neredeyse boş modüller var.**

| Modül | Durum |
|---|---|
| Performans değerlendirme | `personel_performans_degerlendirmeleri` = **0 satır**; tanım tablolarında 1 kayıt. Modül yapılmış ama **kullanılmamış**. |
| Anket cevapları | `personel_anketleri_sorular_cevaplari` = **0 satır**, `anketler_sorular_cevaplar` = **0 satır** |
| İş kazası / İSG | 2–3 satır. Neredeyse kullanılmamış. |
| İşe alım | 12 ilan, 42 görüşme, 5 karar. Sınırlı kullanım. |
| Devamsızlık takibi | 1 satır |

> **Karar (2026-09-07):** Bu tablodaki modüllerin **verileri göç kapsamı dışındadır**
> (`KR-046`). Ancak bu, modüllerin geliştirilmeyeceği anlamına **gelmez**: Performans
> Yönetimi, İşe Alım Yönetimi ve ilgili modüller yeni sistemde geliştirilecek, yalnızca
> **boş veriyle** başlayacaklardır (`KR-045`).
>
> Anket ve İSG/İş Kazası ise güncel modül listesinde yer almamaktadır; kapsam durumları
> İK ile teyit edilecektir (`docs/mimari/vizyon-ve-kapsam.md` §7.4).
>
> **Ayrım önemlidir:** *geliştirme kapsamı* ile *veri göçü kapsamı* iki ayrı karardır.
> Az kullanılmış veriyi taşımak göç riskini ve maliyetini artırır, karşılığında değer
> üretmez; modülün kendisi ise ileriye dönük bir ihtiyaçtır.

**c) Göçte önceliklendirilecek veri kümesi.**
İK biriminin "olmazsa olmaz" olarak bildirdiği veriler ve hacimleri:

| Öncelik | Veri | Yaklaşık hacim |
|---|---|---:|
| 1 | Yıllık izin kayıtları | 7.494 |
| 1 | Yıllık izin devir bakiyeleri (2023) | 841 |
| 1 | Ücretsiz izin | 30 |
| 1 | İş göremezlik izinleri | 7 |
| 2 | Personel eğitimleri | 2.508 |
| 2 | Eğitim tanımları ve kurum eğitimleri | 210 |
| 2 | Eğitim değerlendirmeleri | 1.585 |
| 3 | Personel sertifikaları | 603 |

**d) Göç edilmeyecek veriler.**
`giris_tokenleri` (oturum jetonları), `log`, `hata`, `egitimler_bildirim_servisi_log` ve
benzeri işletim/teknik log tabloları yeni sisteme taşınmayacaktır. Bunlar geçmiş denetim
ihtiyacı için mevcut veritabanının salt-okunur arşiv kopyasında saklanacaktır.
→ Karar `ADR` ile kayıt altına alınacak, İK onayı alınacaktır.

---

## 3. Yeni sisteme etkisi (özet)

| Bulgu | Tasarım kararına etkisi |
|---|---|
| 7 firma, 32 şube, 128 birim | Çok firmalı organizasyon modeli; yetki kapsamında firma/şube boyutu |
| 140 kişi çoklu sicil, 12'si eş zamanlı aktif | `Kişi` ve `İstihdam` varlıklarının ayrılması zorunlu |
| Tek `LH_001_PERSON` tablosu | LOGO erişiminde EF Core ile sabit eşleme uygulanabilir |
| 41 personelde e-posta eksik | Üyelik akışına istisna süreci ve İK ön hazırlık işi eklenmeli |
| Mevcut sistemde personel verisi yok | Göç eşleştirmesi TCKN/sicil üzerinden yapılacak |
| Performans/anket modülleri kullanılmamış | Kapsam kararı İK'ya sorulmalı; varsayılan olarak kapsam dışı |
| PostgreSQL 14 destek sonu 2026-11 | Yeni sistemde güncel PostgreSQL sürümü |

---

## 4. Bu belgeden doğan işler

| No | İş | Sorumlu | İlgili süreç | Durum |
|---|---|---|---|---|
| 1 | TCKN'si boş kartların tamamlanması | İK | TEC.10 | **Kapandı:** aktif personelde eksik TCKN yok (`KR-034`) |
| 2 | Doğum tarihi boş kartların tamamlanması | İK | TEC.10 | **Kapandı:** aktif personelde eksik doğum tarihi yok |
| 3 | **Hiçbir iletişim bilgisi olmayan 12 personelin bilgilerinin tamamlanması** | İK | TEC.10 | **Açık — en yüksek öncelik** |
| 4 | Kurumsal e-postası olmayan 38 personelin LOGO'da tamamlanması | İK | TEC.10 | Açık |
| 5 | Cep telefonu olmayan 13 personelin LOGO'da tamamlanması | İK | TEC.10 | Açık |
| 6 | Telefon biçimi geçersiz olan 1 personelin numarasının düzeltilmesi | İK | TEC.10 | Açık |
| 7 | Paylaşılan e-posta adreslerinin kişiye özel hâle getirilmesi (4 personel, 2 adres) | İK | TEC.10 | Açık |
| 8 | Kurumsal olmayan adres kullanan 58 personele kurumsal adres tanımlanması | İK | TEC.10 | Açık |
| 9 | RADYOLOJİ A.Ş. firmasının kapsamda olup olmadığının teyidi | İK | TEC.2 | Açık |
| 10 | Performans, anket, İSG, işe alım modüllerinin kapsam kararı | İK | TEC.2 | **Kapandı:** modüller geliştirilecek, **verileri göç edilmeyecek** (`KR-045`, `KR-046`). İSG kapsama alındı (`KR-051`); anket teyit edilecek |
| 11 | Log/jeton tablolarının göç dışı bırakılmasının onayı | İK | TEC.10 | **Kapandı:** onaylandı, aktarılmayacak (`KR-033`) |

> **3–8 numaralı işler için ayrıntılı personel listesi hazırlanmıştır.** Liste kişisel veri
> içerdiği için depoya **eklenmemiş**, İK toplantısında kullanılmak üzere ayrı bir Excel
> dosyası olarak teslim edilmiştir. Bu belgede yalnızca özet sayılar yer alır.
>
> **Listeler birbiriyle kesişir.** 3 numaralı listedeki 12 kişi, 4 ve 5 numaralı
> listelerde de görünür. Öncelik 3 numaralı listededir: o kişiler tamamlanmadan
> sisteme hiçbir şekilde giremezler.
>
> Sayılar canlı LOGO verisinden alınmıştır ve günden güne birkaç kayıt oynayabilir
> (bkz. §1.7). Ölçüm tarihi: **2026-09-09**.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-03 | 0.1 | İlk envanter ve ön bulgular | Bilgi İşlem |
