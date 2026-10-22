# SYG-PERS — T1 Personel Yönetimi Sistem/Yazılım Gereksinimleri

**Belge kimliği:** TEC.3-SYG-PERS
**Süreç:** TEC.3 — Sistem/Yazılım Gereksinimlerinin Tanımlanması
**Modül:** T1 Personel Yönetimi
**Kaynak:** `TEC.2/paydas-gereksinimleri/PG-PERS.md` (19.10.2026'da İK onaylı, 45 gereksinim; değişiklik talebi #270)
**Tasarım:** ADR-0019…ADR-0022 (`KR-121`…`KR-126`); T1 tasarım gözden geçirmesi (`TEC.5-tasarim-tanimlama/raporlar/2026-10-22-t1-tasarim-gozden-gecirme.md`)
**Son güncelleme:** 2026-10-22
**Sahibi:** Bilgi İşlem
**Onay durumu:** Bilgi İşlem (RACI TEC.3 A); değişiklikler PR onayıyla (`KR-096`). Baseline, T1'in kabul etiketiyle yazılır (`KR-097`, `KR-111`); o zamana kadar geliştirme sürüyor.

> **Paydaş gereksinimi *ne* istendiğini söyler; bu belge sistemin bunu *hangi
> davranışla* karşılayacağını söyler.** Her madde doğrulanabilir, tek anlamlıdır ve en
> az bir onaylı paydaş gereksinimine bağlıdır. Paydaş gereksinimine bağlanmayan
> sistem gereksinimi yazılmaz.

**Kapsama her PR'da otomatik denetlenir** (`YAKLASIM.md` §4): her paydaş gereksinimi
en az bir sistem gereksinimine bağlı olmalı; bu belgenin §8 tablosu ve izlenebilirlik
matrisinin §7 sütunu, §4 ile aynı eşlemeyi göstermelidir.

**Doğrulama kanıtı:** Her madde, T1 geliştirilirken onu anan bir testle (Analiz ve
İnceleme için TEC.9 doğrulama raporuyla) kanıtlanır. Baseline yazılana kadar eksik kanıt
CI'da uyarıdır; T1 kabulünde bütün maddeler kanıtlı olmalıdır (#272).

---

## 1. Özet

| | |
|---|---:|
| Sistem gereksinimi | **79** |
| Karşılanan paydaş gereksinimi | **45 / 45** |
| Tür — İşlevsel | 54 |
| Tür — Performans | 4 |
| Tür — Arayüz | 1 |
| Tür — İşlevsel olmayan | 12 |
| Tür — Kısıt | 8 |
| Doğrulama — Test | 74 |
| Doğrulama — İnceleme | 1 |
| Doğrulama — Analiz | 4 |
| Parametreye bağlı | 7 |
| Kritik performans ölçütü | 5 |

---

## 2. Sistem tanımı (TEC.3 çıktısı a)

### 2.1 Amaç

T1, personel verisini LOGO Bordro'dan HRMS'e taşır ve İK'nın günlük işini Excel'den
uygulamaya alır: personeli bulmak, kartını görmek, LOGO'da olmayan birkaç bilgiyi girmek ve
personelin hatalı bilgisini kayıtlı biçimde düzeltmeye göndermek. Personel kendi kartını
görür. T1 aynı zamanda HRMS'nin **başka kişilerin verisini** gösteren ilk modülüdür; bu
yüzden kapsam, alan görünürlüğü, özel nitelikli verinin şifrelenmesi ve erişim kaydı ilk
kez burada tam olarak kurulur.

### 2.2 Öğeler

Konumlar ADR-0002 ve ADR-0016'ya göre planlanmıştır; kod yazıldıkça kesinleşir ve tablo
aynı PR'da güncellenir. Bir SYG birden çok öğeye dağılabilir.

| Öğe | Ne yapar | Konum | Karşıladığı SYG-PERS | ADR |
|---|---|---|---|---|
| Personel ekranları | Liste (kart ızgarası ve tablo), kişi kartı, kendi profilim, senkronizasyon durumu, veri kalitesi raporu, düzeltme talepleri | `src/frontend/dsg-hrms-web/src/features/personnel/` | 008–010, 017, 020, 023–025, 027–032, 034, 036–040, 042–046, 048, 056, 070–071, 073 | 0015; 0019 |
| Personel API'si | Liste, kart, fotoğraf, tarihçe, dışa aktarma, HRMS'te girilen alanlar, düzeltme talebi, senkronizasyon | `Dsg.Hrms.Api/Personnel` — `/api/v1/personnel/` | 008–010, 020, 024, 026, 029–033, 036, 041–045, 048, 051, 070, 074 | 0010; 0019 |
| Kapsamlı sorgu ve görünürlük politikası | `ICurrentUserScope`, `IPersonnelQuery`, `PersonFieldVisibility` | `Dsg.Hrms.Application/Personnel`, `Dsg.Hrms.Infrastructure/Personnel` | 022, 024–028, 030–031, 050–054, 063 | 0019 §1–§2, §6 |
| Personel alan modeli | Kişi genişlemesi, korumalı alanlar, fotoğraf, atama, görev, evlilik, acil durum, şube ve birim | `Dsg.Hrms.Domain/Personnel`; `personnel` şeması | 003, 006, 015–016, 018–019, 021, 035–036, 039, 042–043, 054, 067 | 0021 §1–§3, §6 |
| LOGO senkronizasyon işi (genişler) | Yeni alanlar, fotoğraf özeti, tarih aralıklı atama, şema denetimi | `Dsg.Hrms.Application/Personnel/Sync`, `Dsg.Hrms.Infrastructure/Logo` | 001–005, 007, 011, 013–014, 018, 021 | 0003; 0021 §2, §4–§5 |
| Şifreleme | Özel nitelikli veri anahtarı, AES-256-GCM, yeniden şifreleme işi | `Dsg.Hrms.Infrastructure/Security` | 005, 049, 060–062 | 0020 |
| Referans veri çekirdeği | Ünvan ve görev tanımları, yönetim ekranı | `Reference` ad alanı; `reference.definition` | 038, 042, 072 | 0022 §1 |
| Uygulama içi bildirim çekirdeği | Bildirim kutusu, okunmamış sayısı | `notification.inbox_item` | 012, 046 | 0022 §3 |
| Düzeltme talebi | "İK'ya bildir" talebi, İK listesi ve kapatma | `personnel.correction_request` | 045–049 | 0022 §3 |
| Denetim kayıtları ekranı | Dört kayıt türünü salt okunur listeleme | `Dsg.Hrms.Api/Audit`, `features/audit/` | 069 | 0022 §2 |
| Denetim izi ve erişim kaydı (genişler) | Kişi bazlı liste ve dışa aktarma kaydı, korumalı süzgeç | `Dsg.Hrms.Infrastructure/Audit` | 033, 057–059, 064, 066 | 0009; 0019 §5 |
| Parametre deposu (genişler) | T1 parametreleri | `system_parameter`; `Dsg.Hrms.Application/Settings` | 007, 012, 028–029, 047, 068, 075 | 0022 §5 |
| İleti kuyruğu (mevcut) | "İK'ya bildir" e-postası | `Dsg.Hrms.Infrastructure/Notifications` | 047 | 0012 §4 |

### 2.3 Dış arayüzler

| Arayüz | Yön | Protokol | Kısıt |
|---|---|---|---|
| **LOGO veritabanı** (MSSQL 2019) | Yalnızca okuma | TDS | **Yazma, güncelleme, silme kesinlikle yapılmaz** (`KR-003`, `KR-004`) — SYG-PERS-002. T1 beş tablo ekler (SYG-PERS-011) |
| **Kurum SMTP sunucusu** | Giden | SMTP + TLS | Yalnızca "İK'ya bildir" e-postası; içerikte kişisel veri yok (SYG-PERS-047) |
| **Tarayıcı** | Gelen | HTTPS | Yalnızca kurum içi erişim (`KR-067`) |

### 2.4 Sınırlar

**Kapsamdadır:** §4'teki 79 gereksinim. Bunlara, onaylı gereksinimlerin ön koşulu olan dört
yatay çekirdek dâhildir (`KR-115`, `KR-119`, ADR-0022): referans veri (SYG-PERS-072),
denetim kayıtları ekranı (SYG-PERS-069), uygulama içi bildirim (SYG-PERS-046) ve dışa
aktarma (SYG-PERS-032).

**Kapsam dışıdır:**

| Konu | Nerede |
|---|---|
| Personel ekleme, düzenleme, silme; ücret, bordro, SGK bildirimi | LOGO |
| Doğrudan ast kapsamı (PG-PERS-24), yönetici filtresi (PG-PERS-34), birim hiyerarşisi | T2 Organizasyon |
| Rol yönetim ekranı, satır bazlı kapsamın diğer türleri | T4 Rol ve Yetki |
| Bildirim tercihleri, kalıcı ileti kuyruğu, diğer bildirim türleri | Y1 Bildirim Merkezi |
| Kayıtların imhası (saklama süresi dolan kayıt) | Y2 Denetim ve Erişim Kayıtları |
| Ünvan ve görev dışındaki referans türleri | Y3 Referans Veri |
| "Belirli bir tarihte bir şubede kimler vardı" raporu | Raporlama modülü (AN-03) |
| Özlük belgeleri, izin, eğitim, performans | Y7, İ1, İ2, İ3, İ5 |

### 2.5 SYG → tasarım tahsisi (TEC.5 çıktısı b)

Her SYG'nin tasarım kararı izlenebilirlik matrisi §7'nin "Tasarım" sütunundadır ve §2.2'deki
öğe tablosu ADR bölümünü gösterir. Dosya düzeyindeki tahsis (SYG-KMLK §2.5 gibi), kod
yazıldıkça bu bölüme eklenir; T1 kabulünden önce tamamlanır.

---

## 3. Durumlar ve kipler (TEC.3 BP2)

### 3.1 Kişinin çalışma durumu

| Durum | Tanım | Listede | Kart |
|---|---|---|---|
| **Aktif** | En az bir aktif sicil (SYG-PERS-022) | Varsayılan listede | Açılır |
| **Ayrılan** | Aktif sicili yok; görünür kalma süresi dolmadı | "Ayrılan" veya "Tümü" süzgeciyle | Açılır |
| **Ayrılan, süresi dolmuş** | Son çıkış tarihinin üzerinden `PRM-PRS-01` kadar yıl geçti | Görünmez | `404` (SYG-PERS-028) |

### 3.2 Tarih aralıklı kayıt (atama, görev)

`Açık` (bitişi yok) → `Kapalı` (değer değişti, görev değiştirildi veya istihdam bitti) ·
`Silindi` (yumuşak silme; yalnızca görev). Aynı istihdamda en fazla bir açık kayıt vardır
ve silinmemiş kayıtlar çakışmaz (SYG-PERS-018, 019, 021).

### 3.3 Düzeltme talebi

| Durum | Önerilen değer | Geçiş |
|---|---|---|
| **Yeni** | Saklanır (kan grubu şifreli) | Personel gönderir (SYG-PERS-045) |
| **Tamamlandı** | **Silinir**; alan ve tarihler kalır | İK kapatır (SYG-PERS-048, `KR-126`) |

### 3.4 Sistem kipleri

| Kip | Davranış | Gereksinim |
|---|---|---|
| LOGO erişilemez | Ekranlar son başarılı senkronizasyonun verisiyle çalışır | SYG-PERS-001 |
| LOGO şeması beklenenden farklı | Senkronizasyon başlamaz; Sistem Yöneticisi rolüne bildirim | SYG-PERS-011, 012 |
| Özel nitelikli senkronizasyon kapalı (yerel) | Kan grubu okunmaz, mevcut değer temizlenir | SYG-PERS-005 |
| Veri anahtarı tanımsız | Uygulama açılmaz | SYG-PERS-061 |
| Senkronizasyon çalışıyor | İkinci çalışma başlatılamaz | SYG-PERS-009 |
| T2 devrede değil | Kapsam yalnızca `All` ve `Self` | SYG-PERS-050 |

---

## 4. Gereksinimler (TEC.3 çıktısı b)

**Tür:** İşlevsel · Performans · Arayüz · İşlevsel olmayan (güvenlik, kullanılabilirlik,
dayanıklılık) · Kısıt (tasarım ve gerçekleştirme kısıtı).
**Doğrulama:** Test (otomatik, CI'da) · Gösterim (kabulde çalışırken gösterilir) ·
İnceleme (belge veya kayıt incelemesi) · Analiz (UAT'de ölçüm veya çalışma kaydından hesap).

**Ortak kural — tarih:** Bu belgedeki "gün", "bugün" ve tarih karşılaştırmaları
**Europe/Istanbul** takvim gününe göredir (ADR-0021 §2).

### 4.1 Veri kaynağı ve senkronizasyon

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-001** | Personel ekranları ve uçları yalnızca HRMS veritabanından okur; istek sırasında LOGO'ya bağlanılmaz. LOGO erişilemezken liste, kart ve kendi profilim son başarılı senkronizasyonun verisiyle çalışmaya devam eder (SYG-KMLK-011'in T1 ekranlarına genişlemesi). | İşlevsel olmayan | PG-PERS-01 | Test | — |
| **SYG-PERS-002** | T1'in eklediği bütün LOGO sorguları, T3'ün salt okunur bağlamından ve `ILogoPersonnelSource` arayüzünden geçer (SYG-KMLK-002, 003). LOGO ad alanı ve SQL Server sağlayıcısı yalnızca LOGO altyapısında kullanılabilir; mimari testi bunu denetler. | Kısıt | PG-PERS-02 | Test | — |
| **SYG-PERS-003** | T3 alanlarına ek olarak senkronize edilen alanlar yalnızca şunlardır. **Kişi:** cinsiyet, önceki soyadı, medeni hâl (`STATUS` 1 = Evli, diğer = Bekar), eğitim (tanımın adı), adres, kan grubu (SYG-PERS-005), fotoğraf (SYG-PERS-014). **İstihdam:** firma, şehir, şube, birim, ünvan kodu, çalışma durumu, statü, SGK meslek kodu ve adı. Okunan her tablo ve kolon `LogoSchema`'da tek listede durur (toplantı kaydı §6.1). | İşlevsel | PG-PERS-01, PG-PERS-19, PG-PERS-20, PG-PERS-21 | Test | — |
| **SYG-PERS-004** | Din bilgisi (`RELIGION`) ve sağlık tabloları (`LH_001_HEALTH`) okunan alan listesinde **yoktur**. Bunu, listeyi tarayan otomatik bir test doğrular; bu alanlardan birini listeye ekleyen değişiklik CI'da düşer. | Kısıt | PG-PERS-36 | Test | — |
| **SYG-PERS-005** | Kan grubu yalnızca ortam yapılandırması `Logo:SyncSpecialCategoryData` açıkken okunur. Ayar Sistem Yönetimi parametresi değildir ve ekrandan değiştirilemez. Ayar kapalıyken senkronizasyon kan grubunu LOGO'dan okumaz ve HRMS'teki mevcut değeri temizler; temizleme denetim izine "değişti" olarak yazılır. Ortamlar: CI sentetik veri, yerel kapalı, UAT ve üretim açık (`KR-125`). | Kısıt | PG-PERS-36 | Test | — |
| **SYG-PERS-006** | LOGO'dan gelen alanlar için hiçbir yazma ucu ve ekranda düzenleme denetimi yoktur. HRMS'te girilen alanlar (görev, evlilik bilgisi, acil durum kişisi; `KR-112`) ayrı tablolardadır: senkronizasyon bu tablolara, İK ekranları LOGO kaynaklı tablolara yazmaz. Böylece senkronizasyon İK'nın girdiğini ezemez. | İşlevsel | PG-PERS-03 | Test | — |
| **SYG-PERS-007** | Yeni alanlar T3'ün periyodik çalışmasında, aynı periyotla senkronize edilir (varsayılan 15 dakika). Ayrı bir zamanlayıcı kurulmaz. | İşlevsel | PG-PERS-04 | Test | PRM-ENT-07 |
| **SYG-PERS-008** | "Senkronizasyon durumu" ekranı son çalışmaları sayfalı listeler: başlangıç ve bitiş zamanı, süre, sonuç (başarılı / kısmen / başarısız), okunan, eklenen, güncellenen, pasifleşen ve uyarı üretilen kayıt sayıları. Ekran `personnel.sync.view` izniyle açılır. | İşlevsel | PG-PERS-05, PG-PERS-06 | Test | — |
| **SYG-PERS-009** | `personnel.sync.create` izni olan kullanıcı ekrandaki düğmeyle senkronizasyonu başlatır. Çalışma sürerken ikinci başlatma reddedilir ve ekranda "çalışıyor" görünür. Çalışma bitince sonucu ekrandaki listede görünür. T3'teki `identity.sync.*` izinleri `personnel.sync.*`'a taşınır. | İşlevsel | PG-PERS-05 | Test | — |
| **SYG-PERS-010** | Personel listesinde ve kişi kartında "Son senkronizasyon" olarak son **başarılı** çalışmanın bitiş zamanı (gün ve saat) gösterilir. Son çalışma başarısızsa yanında bir uyarı işareti görünür. | İşlevsel | PG-PERS-07 | Test | — |
| **SYG-PERS-011** | Şema denetimi T1'in okuduğu beş tabloyu kapsar: `LH_001_FAMILY`, `LH_001_PERIDINF`, `LH_001_TYPEDEF`, `L_CAPIDIV`, `L_CAPIDEPT`. Denetim CI'da ve üretimde günde bir kez çalışır; beklenen bir kolon yoksa veya türü değiştiyse senkronizasyon başlamaz ve hata kaydedilir (SYG-KMLK-012'nin genişlemesi). | İşlevsel olmayan | PG-PERS-08 | Test | — |
| **SYG-PERS-012** | Şema sapması bulunduğunda veya senkronizasyon başarısız olduğunda, parametrede tanımlı roldeki her kullanıcıya uygulama içi bildirim yazılır. Aynı sorun için açık (okunmamış) bildirim varken yenisi yazılmaz. Bu, T3'te Y1'e bırakılan bildirimi T1'in bildirim çekirdeğiyle karşılar (AN-06). | İşlevsel | PG-PERS-08 | Test | PRM-BLD-01 |
| **SYG-PERS-013** | Bir senkronizasyon çalışmasında önce kişi ve istihdam adımı tamamlanır ve kaydedilir; T1'in ek alanları ve fotoğraflar sonra işlenir. Sonraki adımlarda oluşan hata ilk adımı geri almaz; çalışma "kısmen" olarak kaydedilir. Böylece hesabın istihdam bitince kapanma süresi (SYG-KMLK-078) fotoğraf işinden etkilenmez (AN-07). | İşlevsel olmayan | PG-PERS-41 | Test | — |
| **SYG-PERS-014** | Fotoğraf yalnızca LOGO'daki özeti (`HASHBYTES('SHA2_256', CAST(LDATA AS varbinary(max)))`) değiştiyse okunur; özeti değişmeyen fotoğraf ikinci çalışmada aktarılmaz. Okunan görüntü en fazla 400 piksele küçültülür ve JPEG olarak saklanır; orijinal saklanmaz. LOGO'da silinen fotoğraf HRMS'ten de kaldırılır. | İşlevsel | PG-PERS-23, PG-PERS-41 | Test | — |

### 4.2 Kişi, istihdam ve tarih aralıkları

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-015** | Aynı TCKN'ye sahip bütün siciller tek kişiye bağlanır; kişi listede bir kez görünür, kartında her sicil ayrı bir istihdam olarak yer alır (SYG-KMLK-001). | İşlevsel | PG-PERS-09 | Test | — |
| **SYG-PERS-016** | TCKN'si boş veya geçersiz LOGO kartı kişi olarak oluşturulmaz; kişi listesinde yoktur, veri kalitesi raporunda "TCKN yok" uyarısıyla yer alır (SYG-KMLK-007). | İşlevsel | PG-PERS-10 | Test | — |
| **SYG-PERS-017** | Birden fazla istihdamı olan kişinin kartında her istihdam bir sekmedir. Sekme başlığında sicil numarası ve durum (Aktif / Ayrılan) yazar. Sıra: önce aktif istihdamlar, kendi içinde işe giriş tarihi yeniden eskiye. Tek istihdamlı kişide sekme çubuğu gösterilmez. | İşlevsel | PG-PERS-11 | Test | — |
| **SYG-PERS-018** | Şube, birim, ünvan kodu, statü ve SGK meslek bilgisi istihdama bağlı ve tarih aralıklı tutulur (`valid_from` dahil, `valid_to` hariç). Senkronizasyon bir değişiklik gördüğünde açık kaydı o günle kapatır ve yeni kaydı açar. Aynı gün ikinci bir değişiklik görülürse o gün açılmış kayıt güncellenir; sıfır uzunlukta aralık oluşmaz. Geçmiş, T1'in devreye girdiği günden başlar; önceki tarihler için değer tahmin edilmez. | İşlevsel | PG-PERS-12 | Test | — |
| **SYG-PERS-019** | Aynı istihdamda silinmemiş tarih aralıkları çakışamaz ve en fazla bir açık kayıt bulunur. Kural veritabanında `EXCLUDE USING gist … WHERE deleted_at IS NULL` kısıtıyla zorlanır; kısıt, uygulamanın çalışma rolüyle (`hrms_app`) yapılan eklemede de sınanır. Çakışma hatası (23P01) `409 Conflict` olarak Problem Details biçiminde döner, `500` dönmez. | Kısıt | PG-PERS-12, PG-PERS-43 | Test | — |
| **SYG-PERS-020** | Kişi kartının her istihdam sekmesinde şube, birim, ünvan ve görev **tarihçesi** başlangıç ve bitiş tarihleriyle listelenir. API, bir tarih verildiğinde o tarihte geçerli şube, birim, ünvan ve görevi döndürür; T1 öncesi bir tarih için "kayıt yok" döner. | İşlevsel | PG-PERS-12 | Test | — |
| **SYG-PERS-021** | İstihdam bittiğinde (çıkış tarihi senkronize edildiğinde) o istihdamın açık ataması ve açık görevi **çıkış tarihinin ertesi günü** ile kapanır. | İşlevsel | PG-PERS-12, PG-PERS-43 | Test | — |
| **SYG-PERS-022** | Birden fazla sicili olan kişi (`KR-126`): kişi, en az bir aktif sicili varsa aktiftir. Liste kartı, aktif sicillerden işe giriş tarihi en yeni olanın bilgisini gösterir; aktif sicil yoksa çıkış tarihi en yeni olanınkini. Kartta "X sicil" işareti bulunur (X ≥ 2). İstihdam filtreleri kişinin herhangi bir sicilinde eşleşir; "Aktif" süzgeci yalnızca aktif sicillere bakar. Ayrılan kişinin görünür kalma süresi son çıkış tarihinden başlar. | İşlevsel | PG-PERS-09, PG-PERS-13, PG-PERS-16, PG-PERS-38 | Test | — |

### 4.3 Personel listesi

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-023** | Personel listesi kart ızgarası olarak açılır. Kartta fotoğraf (SYG-PERS-040), ad soyad, sicil numarası, şube / birim ve ünvan / görev bulunur. Adlar LOGO'daki gibi büyük harfle gösterilir; gösterim için dönüşüm yapılmaz. Görünüm değiştirici, aynı süzgeç ve sayfayla klasik tablo görünümüne geçer ve geri döner. | İşlevsel | PG-PERS-13 | Test | — |
| **SYG-PERS-024** | Kişi kartında gösterilen her alan için bir süzgeç vardır. Süzgeç türü alana göredir: **çoklu seçim** (firma, şehir, şube, birim, ünvan, görev, statü, çalışma durumu, SGK meslek, eğitim, medeni hâl, cinsiyet, kan grubu, evlilik yardımı); **metin, içinde geçen** (ad, soyad, önceki soyadı, sicil, TCKN, e-posta, telefon, adres, SGK meslek kodu); **tarih aralığı** (doğum, işe giriş, çıkış, evlilik tarihi); **sayı aralığı, yıl** (yaş, kıdem); **var / yok** (fotoğraf, acil durum kişisi). Yönetici süzgeci yoktur (SYG-PERS-056). | İşlevsel | PG-PERS-14 | Test | — |
| **SYG-PERS-025** | Çoklu seçimli bir süzgeçte seçilen değerlerden **herhangi birine** uyan kayıtlar gelir; farklı süzgeçler **birlikte** (VE) uygulanır. Seçili her süzgeç, liste üstünde kaldırılabilir bir etiket olarak görünür. "Temizle" bütün süzgeçleri kaldırır; çalışma durumu varsayılana (Aktif) döner. | İşlevsel | PG-PERS-14, PG-PERS-15 | Test | — |
| **SYG-PERS-026** | Süzgeç listesi, sıralama seçenekleri ve dışa aktarma alanları görünürlük politikasından gelir (SYG-PERS-052). Kullanıcının göremediği alanın süzgeci ve sıralaması sunulmaz; API'ye bu alanla süzgeçli veya sıralı istek gelirse `400` döner. Küme işlemlerinde (süzgeç, sıralama, dışa aktarma) "kişinin kendisi" kuralı uygulanmaz, yalnızca izin esas alınır. | İşlevsel olmayan | PG-PERS-14 | Test | — |
| **SYG-PERS-027** | Liste varsayılan olarak yalnızca aktif kişileri gösterir. Çalışma durumu süzgecinin değerleri: Aktif (varsayılan), Ayrılan, Tümü. | İşlevsel | PG-PERS-16 | Test | — |
| **SYG-PERS-028** | Ayrılan kişi, son çıkış tarihinden itibaren parametredeki süre (yıl) boyunca listede ve kartta görünür. Parametre boşsa süre sınırsızdır. Süre dolmuşsa kişi listede çıkmaz ve kartı `404` döner; bu kural İK ve Sistem Yöneticisi için de geçerlidir. Aktif sicili olan kişiye uygulanmaz. Kayıt silinmez (SYG-PERS-068). | İşlevsel | PG-PERS-38 | Test | PRM-PRS-01 |
| **SYG-PERS-029** | Liste sayfalanır; toplam kayıt sayısı görünür. Varsayılan sayfa boyutu parametreden gelir (varsayılan 24). Kullanıcı sayfa boyutunu seçebilir; seçenekler en büyük sayfa boyutu parametresini aşmaz. En büyük sayfa boyutu parametresi en fazla 100 olabilir (kodda tavan, ADR-0010 §4); bunu aşan istek `400` döner. | İşlevsel | PG-PERS-17 | Test | PRM-GRN-02, PRM-GRN-03 |
| **SYG-PERS-030** | Hızlı arama kutusu tek terimle ad, soyad, sicil ve TCKN'de "içinde geçen" arama yapar. Önceki soyadı yalnızca gösterildiği koşulda (kadın personel, LOGO'da dolu) aranır. TCKN ile arama yalnızca `personnel.person.view_national_id` izni olan kullanıcıya açıktır. Arama Türkçe karakterden bağımsızdır: "isik" IŞIK'ı, "ismail" İSMAİL'i bulur; "ışık" da aynı sonucu verir. | İşlevsel | PG-PERS-18 | Test | — |
| **SYG-PERS-031** | Liste varsayılan olarak ad, sonra soyad sırasıyla Türkçe alfabeye göre (ICU `tr-TR`) sıralanır. Tablo görünümünde sütun başlığına tıklayarak o sütuna göre artan / azalan sıralanır; kan grubuna göre sıralama yoktur. | İşlevsel | PG-PERS-13 | Test | — |
| **SYG-PERS-032** | `personnel.person.export` izni olan kullanıcı, listenin **süzgece uyan bütün kayıtlarını** (yalnızca görünen sayfayı değil) dışa aktarır. Alanlar seçilebilir; seçenekler görünürlük politikasından gelir. Excel düz listedir (ADR-0014 §6 kuralları); PDF'e fotoğraf alanı seçildiyse küçük fotoğraf eklenir. Satır sınırı 50.000'dir (ADR-0014 §5). İzni olmayan kullanıcıya düğme gösterilmez; API isteği `403` döner. | İşlevsel | PG-PERS-29, PG-PERS-42 | Test | — |
| **SYG-PERS-033** | Her dışa aktarma kaydedilir: kim, ne zaman, biçim, süzgeç (korumalı süzgecin değeri yazılmadan, SYG-PERS-059), kayıt sayısı, aktarılan alanların listesi ve aktarılan kişilerin kimlikleri. | İşlevsel | PG-PERS-30 | Test | — |

### 4.4 Kişi kartı ve kendi profilim

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-034** | Kartın "Genel Bilgiler" bölümünde fotoğraf, ad, soyad, cinsiyet, doğum tarihi, yaş, eğitim, medeni hâl ve kan grubu (görünürlüğe göre) bulunur. Önceki soyadı yalnızca kadın personelde ve LOGO'da doluysa gösterilir. | İşlevsel | PG-PERS-19 | Test | — |
| **SYG-PERS-035** | Yaş doğum tarihinden ve bugünden hesaplanır, saklanmaz. Kişi doğum gününde bir yaş büyür; 29 Şubat doğumlu kişi artık yıl olmayan yıllarda 1 Mart'ta büyür (AN-05). | İşlevsel | PG-PERS-22 | Test | — |
| **SYG-PERS-036** | Evlilik tarihi ve "evlilik yardımı aldı / almadı" bilgisi HRMS'te tutulur. Girme, değiştirme ve silme yalnızca `personnel.person.update` izniyle ve kişinin medeni hâli Evli iken yapılabilir. Medeni hâl Bekar'a dönerse girilmiş bilgi korunur ve görünmeye devam eder, ama düzenlenemez. Bu bilgiyi yalnızca `personnel.person.view_sensitive` izni olan görür; kişinin kendisi göremez. Değişiklik denetim izine yazılır. | İşlevsel | PG-PERS-19 | Test | — |
| **SYG-PERS-037** | "İletişim Bilgileri" bölümünde e-posta, telefon ve adres bulunur; kayıtlı olmayan alanda "Kayıtlı değil" yazar. Adres görünürlüğe tabidir (SYG-PERS-052). | İşlevsel | PG-PERS-20 | Test | — |
| **SYG-PERS-038** | Her istihdam sekmesinin "Çalışma Hayatı" bölümünde şehir, firma, şube, birim, ünvanı, görevi, çalışma durumu, statü, sicil numarası, SGK meslek kodu, SGK meslek, işe giriş tarihi, işten çıkış tarihi ve kıdem süresi bulunur. Ünvan "Ünvanı" etiketiyle, referans verideki karşılığıyla gösterilir; karşılığı olmayan kod boş görünür ("Bilinmiyor" yazmaz). Karşılığın adı değiştirildiğinde ekran kod değişikliği olmadan yeni adı gösterir. | İşlevsel | PG-PERS-21 | Test | — |
| **SYG-PERS-039** | Kıdem süresi her istihdam için hesaplanır, saklanmaz. Aktif istihdamda süre bugüne kadar sayılır, bugün sayılmaz. Ayrılan istihdamda çıkış günü **sayılır**: süre çıkış tarihinin ertesi gününe kadar hesaplanır; giriş 01.01.2020, çıkış 31.12.2020 ise tam bir yıl, ekranda "1 Yıl 0 Ay 0 Gün" (AN-04, İK teyidi 22.10.2026, `KR-127`). Süre takvime göre yıl, ay ve gün olarak bulunur: işe giriş tarihine önce tam yıllar, sonra tam aylar eklenir, kalan gün sayılır; eklenen ayda o gün yoksa ayın son günü alınır. Gösterim "X Yıl Y Ay Z Gün"dür; sayılar sıfırla doldurulmaz. Yıl sıfırsa yıl, yıl ve ay sıfırsa ay da yazılmaz ("3 Ay 5 Gün", "12 Gün"). | İşlevsel | PG-PERS-21 | Test | — |
| **SYG-PERS-040** | Fotoğrafı olmayan kişide ad ve soyadın baş harflerinden oluşan yer tutucu gösterilir. Personelin fotoğraf yüklemesi için hiçbir ekran ve uç yoktur. | İşlevsel | PG-PERS-23 | Test | — |
| **SYG-PERS-041** | Fotoğraf ucu, kart ile aynı kapsam ve görünürlük kurallarına tabidir; kapsam dışı veya bulunmayan kişinin fotoğrafı `404` döner. Yanıt, kaynak özetini ETag olarak ve `Cache-Control: private` başlığını taşır. Fotoğraf isteği ayrıca erişim kaydı üretmez; fotoğraf, kaydı yazılan kart veya liste görüntülemesinin parçasıdır. | İşlevsel olmayan | PG-PERS-23, PG-PERS-26 | Test | — |
| **SYG-PERS-042** | Görev, istihdama bağlı ve tarih aralıklıdır. `personnel.person.update` izni olan kullanıcı, referans verideki **aktif** görev tanımlarından birini ve başlangıç tarihini (varsayılan bugün) seçer; önceki açık görev yeni görevin başlangıç tarihiyle kapanır. Başlangıç tarihi işe giriş tarihinden önce veya çıkış tarihinden sonra olamaz (AN-16). Geçmişe dönük giriş, mevcut kayıtlarla çakışırsa reddedilir (SYG-PERS-019). Görev kaydı silinmez, yumuşak silinir. Değişiklik denetim izine yazılır. İki sicilli kişide her sicilin görevi ayrıdır. | İşlevsel | PG-PERS-43 | Test | — |
| **SYG-PERS-043** | Bir kişiye birden fazla acil durum kişisi girilebilir: yakınlığı, adı, soyadı ve cep telefonu (dördü de zorunlu; telefon SYG-KMLK-009 kuralıyla doğrulanır). Girme, değiştirme ve silme (yumuşak) yalnızca `personnel.person.update` izniyle yapılır; kişinin kendisi görür ama değiştiremez. Görünürlük: SYG-PERS-052. Değişiklik denetim izine yazılır. | İşlevsel | PG-PERS-44 | Test | — |
| **SYG-PERS-044** | Her kullanıcı, rolü ve izni ne olursa olsun, menüdeki "Profilim" ile kendi kartını açabilir. Kart, kişinin kendisi için geçerli görünürlük kurallarıyla gösterilir: adres, kan grubu ve acil durum kişileri görünür; evlilik bilgisi görünmez; TCKN maskelidir. | İşlevsel | PG-PERS-25 | Test | — |

### 4.5 "İK'ya bildir"

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-045** | Kişi yalnızca **kendi** kartında "İK'ya bildir" ile bir düzeltme talebi oluşturur: alan (telefon, e-posta, adres, eğitim, medeni hâl, kan grubu) ve doğru değer. Değer alanına göre doğrulanır: telefon SYG-KMLK-009 kuralıyla, e-posta biçim olarak, kan grubu sekiz değerli listeden, medeni hâl Evli / Bekar seçeneğinden; adres ve eğitim en fazla 500 karakter. Aynı alan için "Yeni" durumda bir talebi varken ikinci talep reddedilir ve kullanıcıya açık talebi olduğu söylenir (AN-08). | İşlevsel | PG-PERS-37 | Test | — |
| **SYG-PERS-046** | Talep oluştuğunda, aynı veritabanı işleminde, İK rolündeki (`hr-identity-operations`) her kullanıcıya bir uygulama içi bildirim yazılır. Menüde okunmamış bildirim sayısı görünür; bildirim açılınca okundu sayılır ve ilgili talebe götürür. | İşlevsel | PG-PERS-37 | Test | — |
| **SYG-PERS-047** | Parametrede bir İK adresi tanımlıysa bu adrese e-posta gönderilir; boşsa gönderilmez. Konu satırı sabittir: "Yeni düzeltme talebi". Konu ve gövdede kişinin adı, bildirilen alan ve önerilen değer **yer almaz**; gövde yalnızca HRMS'teki talebe bağlantı verir. E-posta bellek içi kuyrukla gider; kuyruk kaybında talep ve uygulama içi bildirim korunur. | İşlevsel | PG-PERS-37 | Test | PRM-PRS-02 |
| **SYG-PERS-048** | `personnel.correction.view` izni olan kullanıcı talepleri listeler (durum süzgeciyle; varsayılan "Yeni"): kişi, alan, önerilen değer, gönderim tarihi. `personnel.correction.update` izniyle talebi "Tamamlandı" olarak kapatır; kapatınca önerilen değer veritabanından **silinir**, talepte alan, durum ve tarihler kalır. Kapatma denetim izine yazılır. Talep kaydı (değersiz hâli) personel verisinin saklama süresine tabidir. | İşlevsel | PG-PERS-37 | Test | — |
| **SYG-PERS-049** | Önerilen kan grubu şifreli saklanır (SYG-PERS-060; AAD: talep tablosu, alan ve talebin kimliği). İK'nın önerilen kan grubunu görmesi, ilgili kişi için `SpecialCategoryView` erişim kaydı üretir. | İşlevsel olmayan | PG-PERS-36, PG-PERS-37 | Test | — |

### 4.6 Yetki ve kapsam

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-050** | Kişi okumaları tek bir kapsamlı sorgu bileşeninden geçer. Kapsam izinden türetilir: `personnel.person.view` izni olan kullanıcı bütün kişileri (`All`), olmayan yalnızca kendini (`Self`) görür. Kendi kaydını görmek izin gerektirmez. Kapsam türleri genişletilebilir; doğrudan ast kapsamı (PG-PERS-24) T2'de bu yapıya eklenir (AN-11). | İşlevsel | PG-PERS-24, PG-PERS-25, PG-PERS-35 | Test | — |
| **SYG-PERS-051** | Kapsam dışı bir kişinin kimliğiyle yapılan istek ile var olmayan kimlikle yapılan istek **aynı** yanıtı alır: `404`, aynı gövde. Kural kart, fotoğraf, tarihçe, HRMS'te girilen alanlar ve düzeltme talebi uçlarının hepsinde geçerlidir. | İşlevsel olmayan | PG-PERS-26, PG-PERS-27 | Test | — |
| **SYG-PERS-052** | Alan görünürlüğü tek politikadadır: tam TCKN `view_national_id`; adres, kan grubu ve acil durum kişileri `view_sensitive` veya kişinin kendisi; evlilik bilgisi yalnızca `view_sensitive`. Göremeyen kullanıcının **API yanıtında alan yer almaz** (maskeli TCKN hariç). Politika dört yerde uygulanır: yanıt, süzgeç, sıralama, dışa aktarma alanları. Her korumalı alan için bu dört yeri sınayan bir yetki sızıntısı testi bulunur. | İşlevsel olmayan | PG-PERS-19, PG-PERS-20, PG-PERS-31, PG-PERS-36, PG-PERS-44 | Test | — |
| **SYG-PERS-053** | Tam TCKN'yi göremeyen kullanıcı, kişinin kendisi dahil, TCKN'yi maskeli görür: ilk 3 ve son 2 hane açık, aradaki 6 hane `*` (ör. `123******89`). Maskeleme sunucuda yapılır; tam değer yanıtta yoktur. | İşlevsel | PG-PERS-31 | Test | — |
| **SYG-PERS-054** | Korumalı alanlar kişi tablosunda tutulmaz: adres ve kan grubu `person_sensitive`'de, evlilik bilgisi ve acil durum kişileri kendi tablolarındadır. Kimlik modülünün bu tiplere bağımlılığı mimari testiyle reddedilir; korumalı tiplere bağımlı olabilen sınıflar listelidir. | Kısıt | PG-PERS-20, PG-PERS-36, PG-PERS-44 | Test | — |
| **SYG-PERS-055** | T1 izinleri: `personnel.person.view`, `.view_national_id`, `.view_sensitive`, `.update`, `.export`; `personnel.correction.view`, `.update`; `personnel.quality.view`, `.export`; `personnel.sync.view`, `.create`; `reference.definition.view`, `.update`. Hepsi İK rolüne ve Sistem Yöneticisine verilir (`KR-126`). `audit.log.view` yalnızca Sistem Yöneticisindedir. İK rolünün görünen adı "İnsan Kaynakları" olur; kodu değişmez. | İşlevsel | PG-PERS-05, PG-PERS-29, PG-PERS-32, PG-PERS-35, PG-PERS-37 | Test | — |
| **SYG-PERS-056** | T1'de "Yöneticisi" süzgeci arayüzde ve API'de yoktur. Süzgeç T2 Organizasyon Yönetimi ile eklenir (AN-12). | İşlevsel | PG-PERS-34 | Test | — |

### 4.7 KVKK, şifreleme ve kayıtlar

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-057** | Her kart açılışı (kendi kartı ve "Profilim" dahil, AN-10) bir `View` erişim kaydı üretir: kim, kimin, ne zaman. Yanıtta kan grubu yer aldıysa ek olarak `SpecialCategoryView` kaydı üretilir. | İşlevsel | PG-PERS-28, PG-PERS-36 | Test | — |
| **SYG-PERS-058** | Liste görüntülemesinin erişim kaydında süzgeç ve toplam sayının yanında **ekranda gösterilen kişilerin kimlikleri** de yer alır (sayfa başına en fazla sayfa boyutu kadar). | İşlevsel | PG-PERS-28 | Test | — |
| **SYG-PERS-059** | Korumalı bir alana bakan süzgecin (kan grubu, adres, TCKN, evlilik, acil durum) **değeri** erişim kaydına ve dışa aktarma kaydına yazılmaz; yalnızca süzgecin kullanıldığı yazılır. Bir süzgecin korumalı olup olmadığı alanın sınıflandırmasından (`KR-120`) türetilir; ad listesine dayanılmaz. Kan grubu süzgeciyle listelenen her kişi için ayrıca `SpecialCategoryView` kaydı üretilir. | İşlevsel olmayan | PG-PERS-28, PG-PERS-36 | Test | — |
| **SYG-PERS-060** | Özel nitelikli değerler veritabanında AES-256-GCM ile, depo katmanında şifrelenir. Şifreli değer anahtar kimliği taşır (`k<n>:<nonce>:<şifreli metin>`); her değer için rastgele nonce kullanılır; AAD tablo adı, alan adı ve kayıt sahibinin `public_id`'sidir. Veri anahtarı sır parametre anahtarından ayrıdır. Testler: veritabanında düz metin yok; aynı değer iki kişide farklı şifreli metin üretir; başka satıra veya kolona kopyalanan şifreli değer çözülemez. | İşlevsel olmayan | PG-PERS-36 | Test | — |
| **SYG-PERS-061** | Veri anahtarı (`PersonalDataProtection:Keys`) tanımlı değilse veya biçimi geçersizse uygulama açılmaz; hata günlüğe anahtar değeri olmadan yazılır. | İşlevsel olmayan | PG-PERS-36 | Test | — |
| **SYG-PERS-062** | Sistem Yöneticisi bir yeniden şifreleme işi başlatabilir: iş, eski anahtarla şifreli bütün değerleri (kan grubu ve açık taleplerdeki önerilen kan grubu) güncel anahtarla yeniden yazar. Değer değişmediği için denetim izine "değişti" kaydı yazılmaz; işin kendisi tek bir güvenlik olayıdır: başlatan, kayıt sayısı, kaynak ve hedef anahtar kimliği. İş sonunda eski anahtarla şifreli kalan kayıt sayısını raporlar. | İşlevsel | PG-PERS-36 | Test | — |
| **SYG-PERS-063** | Kan grubu süzgeci uygulama belleğinde çalışır: kapsam ve diğer süzgeçler veritabanında uygulanır; kalan kayıtlarda kan grubu çözülür, süzülür, sonra sıralanır, sayfalanır ve toplam sayılır. Toplam ve sayfa içeriği, süzgecin veritabanında uygulandığı eşdeğer sonuçla aynıdır (test). Kan grubunun deterministik özeti (blind index) tutulmaz. | İşlevsel | PG-PERS-14, PG-PERS-36 | Test | — |
| **SYG-PERS-064** | Erişim kayıtları değiştirilemez ve silinemez: uygulamanın çalışma rolünün (`hrms_app`) erişim kaydı tablolarında `UPDATE` ve `DELETE` yetkisi yoktur (`KR-118`). | Kısıt | PG-PERS-28, PG-PERS-36 | Test | — |
| **SYG-PERS-065** | Kan grubunu görme yetkisi veren role (`personnel.person.view_sensitive`) bir kullanıcı atanmadan önce kullanıcının gizlilik taahhüdü alınır ve kaydı tutulur. Bu, sistem dışında yürütülen bir kurum kontrolüdür; T1 kabulünde rol üyelerinin taahhüt kayıtlarıyla karşılaştırılmasıyla doğrulanır (BK-29, §3.1; AN-18). | Kısıt | PG-PERS-36 | İnceleme | — |
| **SYG-PERS-066** | Denetim izine yazılanlar: HRMS'te girilen alanların (görev, evlilik bilgisi, acil durum kişisi) ve referans tanımlarının değişikliği, talep kapatma, senkronizasyonun kişi ve atama değişiklikleri. Kan grubunun değeri yazılmaz, yalnızca değiştiği görünür; diğer kişisel veriler sınıflandırmasına göre maskelenir. | İşlevsel | PG-PERS-19, PG-PERS-43, PG-PERS-44 | Test | — |
| **SYG-PERS-067** | T1'in eklediği her varlıkta kimlik ve yabancı anahtar dışındaki her skaler alan sınıflandırılır (`[PersonalData]`, `[SpecialCategoryData]`, `[Secret]` veya gerekçeli `[AuditVisible]`). Sınıflandırma testi `bool`, enum ve sayı alanlarını ve `Reference` ile bildirim ad alanlarını da kapsar; sınıflandırılmamış alan CI'da düşer. | Kısıt | PG-PERS-28, PG-PERS-36 | Test | — |
| **SYG-PERS-068** | Personel verisinin saklama süresi (yıl) bir Sistem Yönetimi parametresidir; koda gömülmez. T1'de bu parametreye göre hiçbir kayıt silinmez; imha Y2'dedir. | İşlevsel | PG-PERS-45 | Test | PRM-KVK-03 |
| **SYG-PERS-069** | "Denetim kayıtları" ekranı yalnızca `audit.log.view` izniyle açılır ve dört kayıt türünü (değişiklik, erişim, dışa aktarma, güvenlik olayı) salt okunur, sayfalı listeler. Süzgeçler: tarih aralığı, kayıt türü, işlemi yapan, verisi görüntülenen kişi. Maskeli değerler maskeli kalır. Ekranı görüntülemek de erişim kaydına yazılır. Ekran ve uçları kayıtları değiştiren bir işlem sunmaz. | İşlevsel | PG-PERS-28, PG-PERS-30 | Test | — |

### 4.8 Veri kalitesi

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-070** | "Veri kalitesi" raporu, aktif istihdamlarda şu uyarıları listeler: TCKN yok, doğum tarihi yok, iletişim bilgisi yok, geçersiz telefon biçimi, paylaşılan e-posta, ünvanı boş, görevi boş (açık görevi olmayan aktif istihdam). Rapor uyarı türüne göre süzülür; son senkronizasyonun sonucunu gösterir. Görmek `personnel.quality.view`, Excel'e aktarmak `personnel.quality.export` iznini gerektirir; aktarım SYG-PERS-033'e göre kaydedilir. | İşlevsel | PG-PERS-32 | Test | — |
| **SYG-PERS-071** | Raporda "Üye olamayacak kişiler" ayrı bir başlıktır: aktif istihdamı olan, hesabı bulunmayan ve üyelik koşulunu karşılamayan kişiler. Koşul T3'ten gelir (SYG-KMLK-013, 017): doğum tarihi yok, kurumsal e-postası yok, e-postası kabul edilen alan adlarında değil veya paylaşılan (AN-17). Her satırda nedeni yazar. | İşlevsel | PG-PERS-33 | Test | — |

### 4.9 Referans veri

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-072** | Referans veri ekranı ünvan ve görev tanımlarını listeler; `reference.definition.update` izniyle yeni tanım eklenir, adı değiştirilir veya pasife alınır. Kod, tür içinde tekildir. Tanım silinmez; pasif tanım yeni girişte seçilemez ama geçmiş kayıtlarda adıyla görünür. Ünvan türünün ilk değerleri: 1 Birim Operatörü, 2 Birim Teknik Sorumlusu, 3 Birim Sorumlusu, 4 Yardımcı Hizmetler. Yeni görev tanımı kod değişikliği olmadan seçilebilir hâle gelir. Değişiklikler denetim izine yazılır. | İşlevsel | PG-PERS-21, PG-PERS-43 | Test | — |

### 4.10 Arayüz, API ve parametreler

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-073** | Personel listesi, kişi kartı, Profilim ve "İK'ya bildir" ekranları 360 piksel genişlikten itibaren yatay kaydırma olmadan kullanılır. Telefonda süzgeç çubuğu açılır bir panele dönüşür. Dokunma hedefleri en az 44 × 44 CSS pikseldir. | İşlevsel olmayan | PG-PERS-40 | Test | — |
| **SYG-PERS-074** | Personel işlemleri `/api/v1/personnel/`, referans veri `/api/v1/reference/`, denetim kayıtları `/api/v1/audit/` altında sunulur. Bütün uçlar OpenAPI sözleşmesinde tanımlıdır, listeleri sayfalıdır ve hataları RFC 9457 Problem Details biçiminde döndürür (ADR-0010). | Arayüz | PG-PERS-13, PG-PERS-17, PG-PERS-26 | Test | — |
| **SYG-PERS-075** | T1 parametreleri Sistem Yönetimi parametre ekranında türü ve aralığıyla görünür ve düzenlenir; değişiklik denetim izine yazılır. Varsayılan sayfa boyutu (sayı, 6–100, varsayılan 24), en büyük sayfa boyutu (sayı, 24–100, varsayılan 100; varsayılan sayfa boyutundan küçük olamaz), ayrılan personelin görünür kalma süresi (yıl, 1–50, boş = süresiz), "İK'ya bildir" e-posta adresi (e-posta, boş = gönderilmez), personel verisinin saklama süresi (yıl, 1–100, boş = karar bekliyor). | İşlevsel | PG-PERS-17, PG-PERS-37, PG-PERS-38, PG-PERS-45 | Test | PRM-GRN-02, PRM-GRN-03, PRM-PRS-01, PRM-PRS-02, PRM-KVK-03 |

### 4.11 Performans

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-PERS-076** | Personel listesinin bir sayfası (24 kart, fotoğraflar dahil) 20 eş zamanlı kullanıcı yükünde 95. yüzdelikte 2 saniyenin altında açılır. Yük ve ölçüm tanımı KPÖ-PERS-1'dedir (AN-15). | Performans | PG-PERS-39 | Analiz | — |
| **SYG-PERS-077** | Kişi kartı (fotoğraf dahil) aynı yükte 95. yüzdelikte 2 saniyenin altında açılır. | Performans | PG-PERS-39 | Analiz | — |
| **SYG-PERS-078** | Fotoğrafı değişmeyen bir senkronizasyon çalışması 2 dakikanın altında biter; kişi ve istihdam adımı ayrıca 60 saniyenin altında kalır (SYG-KMLK-078). Fotoğraf özeti sorgusunun LOGO sunucusundaki süresi UAT'de ölçülür ve ADR-0021 §5'e yazılır. | Performans | PG-PERS-41 | Analiz | — |
| **SYG-PERS-079** | Kan grubu süzgeciyle listelenen sayfa da aynı yükte 95. yüzdelikte 2 saniyenin altında açılır. | Performans | PG-PERS-39 | Analiz | — |

---

## 5. Kritik performans ölçütleri (TEC.3 çıktısı c)

| Kimlik | Ölçüt | Hedef | Nasıl ölçülür | İlgili SYG |
|---|---|---|---|---|
| **KPÖ-PERS-1** | Liste sayfası açılış süresi | p95 < 2 sn | UAT'de yük testi: 20 sanal kullanıcı, her biri 10 dakika boyunca istekler arasında 5 sn bekleyerek liste sayfası açar. Süre, liste isteğinin başlangıcından sayfadaki 24 küçük fotoğrafın son baytına kadardır | SYG-PERS-076, 014 |
| **KPÖ-PERS-2** | Kişi kartı açılış süresi | p95 < 2 sn | Aynı yük profili; kart isteği ve fotoğrafı | SYG-PERS-077 |
| **KPÖ-PERS-3** | Senkronizasyon süresi | Fotoğrafı değişmeyen çalışma < 2 dk; kişi ve istihdam adımı < 60 sn | Çalışma kaydındaki adım süreleri, UAT'de bir iş günü boyunca | SYG-PERS-078, 013 |
| **KPÖ-PERS-4** | Fotoğraf özeti sorgusunun LOGO'daki maliyeti | < 10 sn; okunan sayfa sayısı ölçülüp kaydedilir | UAT'de LOGO sunucusunda sorgu istatistiği (yalnızca süre ve okuma sayısı; veri okunmaz) | SYG-PERS-014, 078 |
| **KPÖ-PERS-5** | Kan grubu süzgeçli liste | p95 < 2 sn | KPÖ-PERS-1'in yük profili, kan grubu süzgeciyle | SYG-PERS-079, 063 |

**Neden bu beşi?** KPÖ-1 ve KPÖ-2, PG-PERS-39'un gerekçesini ölçer: yavaş açılan liste
İK'yı Excel'e döndürür. KPÖ-3, T1'in senkronizasyona eklediği işin T3'ün hesap kapanma
güvencesini bozmadığını gösterir. KPÖ-4, LOGO'nun kurumun kaynak sistemi olarak
yorulmadığının tek kanıtıdır; ölçüm kötüyse ADR-0021 §5'teki iki yedek yöntem uygulanır.
KPÖ-5, şifrelemenin bedelini (bellek içi süzgeç) ölçer.

---

## 6. Gereksinim analizi (TEC.3 çıktısı d, BP3)

Paydaş gereksinimleri tek tek ve birbirleriyle, T1 tasarım kararları ve tasarım gözden
geçirmesinin bulgularıyla birlikte karşılaştırıldı. Kapanmamış olanlar ⏳ ile işaretlidir.

| No | Bulgu | Çözüm | Durum |
|---|---|---|---|
| AN-01 | PG-PERS-14 "kartta gösterilen bilgilerin tamamı için filtre" der; fotoğraf, yaş, kıdem ve acil durum kişisi gibi alanlarda süzgecin ne olacağı yazmaz. | Süzgeç türü alana göre belirlendi: çoklu seçim, içinde geçen metin, tarih aralığı, yıl aralığı, var / yok (SYG-PERS-024). Fotoğraf ve acil durum kişisi için yalnızca var / yok süzgeci vardır. Anlam değişmez, yalnızca biçim netleşir. | ✅ |
| AN-02 | PG-PERS-18 TCKN ile aramayı herkese açar; PG-PERS-31 tam TCKN'yi yalnızca İK ve Sistem Yöneticisine gösterir. Tam TCKN'yi göremeyen kullanıcı TCKN ile arayarak kişinin varlığını doğrulayabilirdi (B-17). | TCKN ile arama `view_national_id`'ye bağlandı (SYG-PERS-030). T1'de başkalarını gören (`All`) kullanıcıların hepsinde bu izin var; uygulamada kimse kısıtlanmaz. | ✅ |
| AN-03 | PG-PERS-12 "geçen yıl hangi şubedeydi" sorusunu ister. LOGO yalnızca bugünkü değeri verir; geçmiş T1'in devreye girdiği günden başlar. "Bir tarihte bir şubede kimler vardı" listesi gereksinimde yoktur. | Kişi başına tarihçe ve tarihli sorgu (SYG-PERS-018, 020). Devreye almadan önceki dönem için "kayıt yok" döner. Toplu tarihli rapor kapsam dışıdır (§2.4). **T1 kabulünde İK'ya gösterilir:** geçmiş, devreye alma gününden başlar. | ✅ |
| AN-04 | PG-PERS-21 ayrılan istihdamda kıdemi "işten çıkış tarihine kadar" sayar; çıkış gününün sayılıp sayılmadığını söylemez. Örnek: giriş 01.01.2020, çıkış 31.12.2020. Çıkış günü sayılırsa "1 Yıl", sayılmazsa "11 Ay 30 Gün". | Çıkış günü sayılır (LOGO'daki çıkış tarihi son çalışma günüdür); hesap çıkış tarihinin ertesi gününe kadar yapılır. Aktif istihdamda bugün sayılmaz (gün henüz tamamlanmadı). **İK teyidi: 22.10.2026, Doğuş Uçanok bildirdi (`KR-127`).** SYG-PERS-039. | ✅ İK teyitli |
| AN-05 | PG-PERS-22 29 Şubat doğumlu kişinin artık yıl olmayan yıllarda ne zaman yaş aldığını söylemez. | 1 Mart'ta (SYG-PERS-035). Doğum günü o yıl takvimde yoksa henüz gelmemiş sayılır. | ✅ |
| AN-06 | PG-PERS-08 şema sapmasının sistem yöneticisine **bildirilmesini** ister; T3 bunu Y1'e bırakmıştı (SYG-KMLK-010, PG-PERS-08 gerekçesi). T1 uygulama içi bildirim çekirdeğini zaten kuruyor (ADR-0022 §3). | Bildirim T1'de, mevcut çekirdekle, `PRM-BLD-01`'deki role uygulama içi olarak yazılır (SYG-PERS-012). Yeni bir altyapı gerektirmez; ADR-0022 §3'e not düşüldü. E-posta ve tercihler Y1'de kalır. **Onay: Doğuş Uçanok, 22.10.2026 (`KR-127`).** | ✅ |
| AN-07 | PG-PERS-41 fotoğraflı çalışmaya 2 dakika verir; SYG-KMLK-078 (baseline `v0.2.0`) tam çalışmayı 60 saniyeyle sınırlar ve buna hesabın kapanma süresini bağlar. Fotoğraf işi aynı çalışmaya girince T3 güvencesi bozulabilir. | Çalışma iki adıma ayrıldı: kişi ve istihdam önce tamamlanıp kaydedilir (SYG-KMLK-078 bu adımın süresidir), ek alanlar ve fotoğraf sonra (SYG-PERS-013, 078). | ✅ |
| AN-08 | PG-PERS-37 aynı alan için tekrarlanan talepleri sınırlamaz; aynı değer defalarca gönderilip İK'ya bildirim ve e-posta yağdırılabilir. | Aynı alan için "Yeni" durumda tek talep olabilir (SYG-PERS-045). Paydaşın anlamını değiştirmez: talep kaybolmaz, yalnızca tekrarı engellenir. | ✅ |
| AN-09 | PG-PERS-37 kan grubunun bildirilebilmesini ister; PG-PERS-36 kan grubunu şifreli saklamayı ve sınırlı görünürlüğü ister. Önerilen kan grubu da özel nitelikli veridir ve LOGO'dan yeniden üretilemez (B-03). | Önerilen değer şifrelenir, talep kapanınca silinir, İK'nın görmesi kayda geçer (SYG-PERS-048, 049; KVKK Sorumlusu kararı `KR-126`). | ✅ |
| AN-10 | PG-PERS-28 "bir personelin kişisel verisinin görüntülenmesi"ni kaydeder; kişinin kendi kartını görmesinin kayda girip girmediğini söylemez. | Kendi kartı da kaydedilir (SYG-PERS-057). Kural basit kalır ve "verilerimi kim gördü" sorusunun cevabı eksiksiz olur. | ✅ |
| AN-11 | PG-PERS-24 (doğrudan ast) T2'deki atamalara bağlıdır ve T1'de karşılanamaz. | T1 geçiş kuralını (PG-PERS-35) uygular; kapsam modeli T2'nin `DirectReports`'u ekleyebileceği biçimde kurulur (SYG-PERS-050). PG-PERS-24'ün kabul kriteri T2 kabulünde sınanır. **T1 kabulünde İK'ya gösterilir.** | ✅ |
| AN-12 | PG-PERS-34 yönetici süzgecinin T2 devreye girdiğinde "kod değişikliği olmadan" görünmesini ister; T2'nin kendisi bir kod değişikliğidir. | T1'de süzgeç yoktur (SYG-PERS-056). Süzgeç, T2'nin yönetici ilişkisiyle birlikte gelir; ayrıca bir açma işlemi gerektirmez. Kriterin "kod değişikliği olmadan" kısmı T2 kabulünde sınanır. | ✅ |
| AN-13 | PG-PERS-38 görünür kalma süresi dolan kaydın "gösterilmez" olmasını ister; kartın doğrudan adresle açılıp açılamayacağını ve çok sicilli kişide sürenin başlangıcını söylemez. | Süre dolunca kart herkes için `404` döner; süre son çıkış tarihinden başlar (SYG-PERS-022, 028; `KR-126`). Denetim kayıtlarındaki kimlikler korunur. | ✅ |
| AN-14 | PG-PERS-20, 36 ve 44 "kişinin kendisi görür" der. Bu, liste süzgeci ve dışa aktarma gibi küme işlemlerine uygulansaydı, kendi kan grubunu gören personel kan grubuna göre süzerek başkalarının değerini çıkarabilirdi (B-07). | "Kişinin kendisi" kuralı yalnızca tek kaydın gösterildiği yerdedir; küme işlemlerinde yalnızca izin geçerlidir (SYG-PERS-026). | ✅ |
| AN-15 | PG-PERS-39 "aynı anda 20 kullanıcı" der. T3'te benzer ifade iki farklı biçimde okunmuş ve ölçüm yeniden tanımlanmıştı (SYG-KMLK AN-25). | Yük profili baştan yazıldı: 20 sanal kullanıcı, istekler arası 5 sn, 10 dakika; süre fotoğraflar dahil (KPÖ-PERS-1). | ✅ |
| AN-16 | PG-PERS-43 görevin tarih aralıklı olmasını ister; aralığın istihdamın dışına taşıp taşamayacağını söylemez. | Görevin başlangıcı işe giriş tarihinden önce ve çıkış tarihinden sonra olamaz (SYG-PERS-042); istihdam bitince açık görev kapanır (SYG-PERS-021). | ✅ |
| AN-17 | PG-PERS-33 "üyelik akışını etkileyen kayıtları" tanımlamaz. | Tanım T3'ün üyelik koşulundan alındı: doğum tarihi, kurumsal e-posta, kabul edilen alan adı, tekil e-posta (SYG-PERS-071). | ✅ |
| AN-18 | PG-PERS-36'daki gizlilik taahhüdü bir yazılım davranışı değil, kurum kontrolüdür. | Doğrulaması İnceleme'dir: kabulde rol üyeleri taahhüt kayıtlarıyla karşılaştırılır (SYG-PERS-065). İK, taahhüt kayıtlarını tutar. | ✅ |

**T1 tasarım gözden geçirmesinin test tanımı olmayan kuralları** (rapor §4) şu maddelerde,
doğrulama yöntemiyle yer alır:

| Kural | SYG-PERS |
|---|---|
| EXCLUDE kısıtı ve tarih kuralları | 018, 019, 021 |
| AAD ile değerin satırdan satıra taşınamaması | 060 |
| Anahtar döndürme | 062 |
| Bellek içi süzgeçte sayfalama ve toplam | 063 |
| Fotoğrafın değişmediğinde yeniden okunmaması | 014 |
| E-postada değer olmaması | 047 |
| Erişim kaydı içeriği | 033, 057, 058, 059 |
| Ayrılan personelin görünürlük süresi | 028 |
| Fotoğraf, dışa aktarma ve düzeltme talebi uçlarında `404` | 041, 051 |
| Kıdem hesabı | 039 |

**Paydaşa geri bildirim (BP3):** AN-04 onaylı bir kabul kriterinin (PG-PERS-21) uç
durumunu tek anlama indirir; İK 22.10.2026'da teyit etti (`KR-127`). AN-03 ve AN-11, kabul
kriterlerinin T1 içinde tam gösterilemeyecek kısmını söyler; bu ikisi T1 kabulünde İK'ya
ayrıca gösterilir. Diğer bulgular paydaş gereksinimlerinin anlamını değiştirmez.

---

## 7. Destekleyici sistemler (TEC.3 çıktısı e)

| Sistem | Ne için | Durum |
|---|---|---|
| LOGO salt okunur hesabı | Senkronizasyon, şema denetimi | ✅ Mevcut; yazma reddi CI'da sınanıyor (SYG-KMLK-003). T1 tablolarını okuma yetkisi UAT'de doğrulanacak |
| LOGO sunucusunda sorgu ölçümü | KPÖ-PERS-4 | ⏳ UAT'de, ilk fotoğraflı senkronizasyonda |
| Özel nitelikli veri anahtarı | SYG-PERS-060, 061 | ⏳ UAT `.env.uat` ve yerel `docker/.env`'e kan grubu PR'ıyla eklenir; BK-29'a yeri işlenir |
| UAT sunucusu | Kabul ve ölçüm | ✅ Mevcut; uygulama `hrms_app` rolüyle çalışıyor (`KR-118`) |
| Kurum SMTP hesabı | "İK'ya bildir" e-postası | ✅ Mevcut (T3) |
| ClosedXML, PDFsharp / MigraDoc | Dışa aktarma | ✅ Projede mevcut (ADR-0014) |
| SkiaSharp | Fotoğraf küçültme | ⏳ Fotoğraf PR'ıyla eklenir; MIT lisansı, paket envanterine yazılır |
| Yük testi aracı | KPÖ-PERS-1, 2, 5 | ✅ T3 ölçümünde kullanılan yöntem (SYG-KMLK-077) |
| Gizlilik taahhüdü kayıtları | SYG-PERS-065 | ⏳ İK tutar; UAT'de kan grubu açılmadan önce |

---

## 8. İzlenebilirlik (TEC.3 çıktısı f)

Aşağıdaki tablo §4'ün **tersidir**: paydaş gereksiniminden sistem gereksinimine.
`.github/scripts/requirement-traceability-check.mjs` her PR'da şunları denetler;
biri bozulursa PR birleştirilemez:

- Her paydaş gereksinimi en az bir sistem gereksinimine bağlı.
- §4'teki her kaynak kimliği `PG-PERS`'te var.
- Sistem gereksinimi kimlikleri tekil ve sıralı; metin içindeki atıflar boşa düşmüyor.
- Bu tablo ve `izlenebilirlik-matrisi.md` §7'nin "Sistem gereksinimi" sütunu §4 ile
  **aynı** eşlemeyi gösteriyor.

| Paydaş gereksinimi | Sistem gereksinimleri |
|---|---|
| `PG-PERS-01` | SYG-PERS-001, 003 |
| `PG-PERS-02` | SYG-PERS-002 |
| `PG-PERS-03` | SYG-PERS-006 |
| `PG-PERS-04` | SYG-PERS-007 |
| `PG-PERS-05` | SYG-PERS-008, 009, 055 |
| `PG-PERS-06` | SYG-PERS-008 |
| `PG-PERS-07` | SYG-PERS-010 |
| `PG-PERS-08` | SYG-PERS-011, 012 |
| `PG-PERS-09` | SYG-PERS-015, 022 |
| `PG-PERS-10` | SYG-PERS-016 |
| `PG-PERS-11` | SYG-PERS-017 |
| `PG-PERS-12` | SYG-PERS-018, 019, 020, 021 |
| `PG-PERS-13` | SYG-PERS-022, 023, 031, 074 |
| `PG-PERS-14` | SYG-PERS-024, 025, 026, 063 |
| `PG-PERS-15` | SYG-PERS-025 |
| `PG-PERS-16` | SYG-PERS-022, 027 |
| `PG-PERS-17` | SYG-PERS-029, 074, 075 |
| `PG-PERS-18` | SYG-PERS-030 |
| `PG-PERS-19` | SYG-PERS-003, 034, 036, 052, 066 |
| `PG-PERS-20` | SYG-PERS-003, 037, 052, 054 |
| `PG-PERS-21` | SYG-PERS-003, 038, 039, 072 |
| `PG-PERS-22` | SYG-PERS-035 |
| `PG-PERS-23` | SYG-PERS-014, 040, 041 |
| `PG-PERS-24` | SYG-PERS-050 |
| `PG-PERS-25` | SYG-PERS-044, 050 |
| `PG-PERS-26` | SYG-PERS-041, 051, 074 |
| `PG-PERS-27` | SYG-PERS-051 |
| `PG-PERS-28` | SYG-PERS-057, 058, 059, 064, 067, 069 |
| `PG-PERS-29` | SYG-PERS-032, 055 |
| `PG-PERS-30` | SYG-PERS-033, 069 |
| `PG-PERS-31` | SYG-PERS-052, 053 |
| `PG-PERS-32` | SYG-PERS-055, 070 |
| `PG-PERS-33` | SYG-PERS-071 |
| `PG-PERS-34` | SYG-PERS-056 |
| `PG-PERS-35` | SYG-PERS-050, 055 |
| `PG-PERS-36` | SYG-PERS-004, 005, 049, 052, 054, 057, 059, 060, 061, 062, 063, 064, 065, 067 |
| `PG-PERS-37` | SYG-PERS-045, 046, 047, 048, 049, 055, 075 |
| `PG-PERS-38` | SYG-PERS-022, 028, 075 |
| `PG-PERS-39` | SYG-PERS-076, 077, 079 |
| `PG-PERS-40` | SYG-PERS-073 |
| `PG-PERS-41` | SYG-PERS-013, 014, 078 |
| `PG-PERS-42` | SYG-PERS-032 |
| `PG-PERS-43` | SYG-PERS-019, 021, 042, 066, 072 |
| `PG-PERS-44` | SYG-PERS-043, 052, 054, 066 |
| `PG-PERS-45` | SYG-PERS-068, 075 |

---

## 9. Değişiklik

Bu belge Pull Request ile değişir. Bir sistem gereksinimi değişikliği bir paydaş
gereksiniminin **anlamını** değiştiriyorsa önce `tur:degisiklik-talebi` issue'su açılır
ve İK'nın onayı alınır (TEC.2 YAKLAŞIM §4.1). Numara yeniden kullanılmaz; iptal edilen
madde `İptal` notuyla kalır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-22 | 0.1 | İlk oluşturma: PG-PERS'teki 45 onaylı gereksinimden ve T1 tasarım kararlarından (ADR-0019…0022) türetildi; 79 madde, 5 KPÖ, 18 analiz bulgusu (#274) | Bilgi İşlem |
| 2026-10-22 | 0.2 | AN-04 İK teyidiyle kapandı: ayrılan istihdamda kıdem çıkış gününü sayar (SYG-PERS-039); AN-06 onaylandı (`KR-127`, #274) | Bilgi İşlem |
| 2026-10-22 | 0.3 | SYG-PERS-039: örnek ekran metni "1 Yıl 0 Ay 0 Gün" olarak netleştirildi; yıl varsa üç birim de yazılır (PG-PERS-21; #276) | Bilgi İşlem |
