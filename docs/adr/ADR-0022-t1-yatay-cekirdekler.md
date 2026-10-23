# ADR-0022 — T1'de Kurulan Yatay Çekirdekler

**Durum:** **Kabul Edildi**
**Tarih:** 2026-10-22
**Karar defteri karşılığı:** `KR-124`, `KR-126`; kapsam kararları `KR-115` (Y3), `KR-119` (Y2)
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)
**Genişlettiği:** ADR-0010 §4 (sayfalama), ADR-0012 §4 (uygulama içi bildirim), ADR-0014 (dışa aktarma), ADR-0016 (modül tablosu)
**İlgili gereksinimler:** PG-PERS-17, 21, 28, 30, 37, 38, 42, 43, 45
**İlgili iş:** #245 maddeler 6, 8, 9, 10, 11

---

## Bağlam

T1'in onaylı gereksinimleri, henüz kurulmamış dört yatay modülün bir parçasına ihtiyaç
duyar:

| İhtiyaç | Asıl modül | Gereksinim |
|---|---|---|
| Ünvan ve görev tanımları | Y3 Referans Veri | PG-PERS-21, 43 (`KR-115`) |
| Denetim ve erişim kayıtlarını görme | Y2 Denetim ve Erişim Kayıtları | PG-PERS-28, 30 (`KR-119`) |
| "İK'ya bildir": uygulama içi bildirim ve e-posta | Y1 Bildirim Merkezi | PG-PERS-37 |
| Dışa aktarma (alan seçmeli, fotoğraflı PDF) | Raporlama ve Dışa Aktarma | PG-PERS-42 |

`KR-077`'deki ilke geçerlidir: modül yalnızca kendi onaylı gereksinimlerinin gerektirdiği
çekirdeği kurar; asıl modül onu kendi gereksinim toplantısından sonra genişletir.
Onaylanmamış gereksinime dayanan hiçbir şey yazılmaz (`KR-068`).

## Karar

### 1. Referans veri çekirdeği (`KR-115`)

- Ayrı bir modül ad alanında kurulur: `Reference`. Y3 bu ad alanını genişletir.
  ADR-0016 tablosuna `Personnel -> Reference` bağımlılığı gerekçesiyle eklenir.
- Tek tablo: `reference.definition` (tür, kod, ad, sıra, `is_active`). T1'de iki tür vardır:
  - **Ünvan:** LOGO'daki 1–4 kodlarının karşılıkları. Karşılığı olmayan kod ekranda boş
    görünür (PG-PERS-21).
  - **Görev:** İK'nın tanımladığı görevler (PG-PERS-43).
  - **Kan grubu** (23.10.2026, `KR-129`): LOGO kodlarının karşılıkları (1 0 Rh+ … 8 AB Rh−; 0
    "Belirtilmemiş" değer yok sayılır). Bu tür **sistem tanımıdır**: ekrandan adı değiştirilemez ve
    pasife alınamaz, yalnızca migration ile değişir. Bir adın yanlış düzenlenmesi herkesin sağlık
    verisini sessizce yanlış gösterirdi.
- Tanım silinmez, pasife alınır (ADR-0004 §5). Pasif tanım yeni girişte seçilemez, ama
  geçmiş kayıtlarda adıyla görünür.
- **Basit yönetim ekranı:** liste, ekleme, ad değiştirme, pasife alma. İzinler
  `reference.definition.view` ve `reference.definition.update`; İK ve Sistem Yöneticisi.
  Değişiklikler denetim izine yazılır.

### 2. Denetim kayıtları görüntüleme çekirdeği (`KR-119`)

- Salt okunur, sayfalı bir liste. Dört kayıt türünü kapsar: değişiklik, erişim, dışa
  aktarma, güvenlik olayı. Süzgeçler: tarih aralığı, kayıt türü, işlemi yapan, verisi
  görüntülenen kişi.
- İzin `audit.log.view`; Sistem Yöneticisi ve İK (**`KR-128`**, 23.10.2026; ilk sürümde yalnızca Sistem Yöneticisiydi, `KR-119`).
- Ekrana bakmak da erişim kaydına yazılır. Maskeli değerler maskeli kalır; maskeyi açma
  yoktur.
- Kayıtlara yalnızca okuma yetkisiyle erişilir (`KR-118`). Ekran, kayıtları değiştirecek bir
  yol açmaz.

### 3. "İK'ya bildir" ve uygulama içi bildirim çekirdeği

- **Düzeltme talebi** (`personnel.correction_request`):
  - Talebin içeriği: kişi, alan (telefon, e-posta, adres, eğitim, medeni hâl, kan grubu),
    önerilen değer, durum (Yeni / Tamamlandı), tarihler.
  - Önerilen değer kişisel veridir ve sınıflandırılır (`KR-120`). Kan grubu için önerilen
    değer şifrelenir (ADR-0020).
  - **Önerilen değer, talep kapatılınca silinir** (revizyon 1.2; KVKK Sorumlusu kararı,
    `KR-126`; B-03). Talepte yalnızca hangi alanın bildirildiği, durum ve tarihler kalır.
    Değer yalnızca İK'nın LOGO'yu düzeltmesi için tutulur; düzeltme yapıldıktan sonra
    saklamanın bir amacı yoktur. Kapatma denetim izine yazılır.
  - Talep kaydının kendisi (değersiz hâli) personel verisinin saklama süresine tabidir
    (PG-PERS-45; revizyon 1.2, B-18).
  - Talepleri görme ve kapatma `personnel.correction.view` ve `personnel.correction.update`
    izinleriyle yapılır; ikisi de İK'da ve Sistem Yöneticisindedir (ADR-0019 §4, B-14).
    Önerilen kan grubunu görmek `SpecialCategoryView` kaydı üretir (ADR-0019 §5).
- **Uygulama içi bildirim** (`notification.inbox_item`): alıcı kullanıcı, tür, ilişkili kayıt
  bağlantısı, okundu/okunmadı. Talep oluşunca **İK rolündeki her kullanıcıya** bir bildirim
  yazılır (S-12). Menüde okunmamış sayısı görünür. Bu, ADR-0012 §4'ün en küçük hâlidir;
  Y1 tercihleri, kanalları ve diğer bildirim türlerini sonra ekler.
- **E-posta:** Sistem Yönetimi parametresindeki İK adresine gider (S-12; yeni parametre).
  E-postada **önerilen değer yazmaz**, yalnızca HRMS'teki talebe bağlantı verir. Böylece
  kişisel veri, hele kan grubu, e-postada dolaşmaz (toplantı kaydı §11.3). **Konu satırı**
  sabittir: "Yeni düzeltme talebi". Kişinin adı ve bildirilen alan konu satırına da yazılmaz
  (revizyon 1.2, B-18). Bunu bir test doğrular.
- **Kalıcı gönderim:** Talep ve bildirim kaydı veritabanına aynı işlemde yazılır. E-posta
  bugünkü bellek içi kuyrukla gider. Kuyruk kaybında İK talebi yine uygulama içinde görür;
  kaybolan yalnızca e-postadır (ADR-0012 §4 notu, BG-02). Kalıcı kuyruk Y1'dedir.
- **İkinci bildirim türü** (SYG-PERS AN-06, #274): Şema sapması ve başarısız senkronizasyon,
  `PRM-BLD-01`'deki roldeki kullanıcılara aynı çekirdekle uygulama içi bildirim olarak yazılır
  (PG-PERS-08). T3 bu bildirimi Y1'e bırakmıştı; çekirdek T1'de kurulduğu için yeni altyapı
  gerekmez. E-posta ve tercihler Y1'de kalır.

### 4. Dışa aktarma (ADR-0014)

- Seçilebilir alanlar ADR-0019'daki görünürlük politikasından gelir. Kullanıcı yalnızca
  görebildiği alanları seçer.
- Excel düz listedir. PDF'e fotoğraf alanı seçilirse küçük fotoğraf (ADR-0021 §5) eklenir.
- Erişim kaydına süzgeç, kayıt sayısı, **aktarılan alanların listesi** ve **aktarılan
  kişilerin kimlikleri** yazılır (ADR-0019 §5; revizyon 1.2, B-09).
- Kütüphaneler ADR-0014 §1'deki gibidir: ClosedXML ve PDFsharp/MigraDoc, Türkçe karakter
  için gömülü yazı tipiyle.

### 5. Sistem Yönetimi parametreleri

| Parametre | Varsayılan | Gereksinim |
|---|---|---|
| Kart ızgarası varsayılan sayfa boyutu | 24 | PG-PERS-17 (S-07) |
| En büyük sayfa boyutu | 100 (kodda tavan) | PG-PERS-17; ADR-0010 §4 |
| Ayrılan personelin görünür kalma süresi (yıl) | Boş = süresiz | PG-PERS-38 (`KR-114`) |
| Personel verisinin saklama süresi (yıl) | Değer KVKK Sorumlusu kararıyla girilir; imha Y2'de | PG-PERS-45 (`KR-116`) |
| "İK'ya bildir" e-posta adresi | Boş = e-posta gönderilmez | PG-PERS-37 (S-12) |

- **Sayfa boyutu:** Parametre, kodda kalan 100 tavanını aşamaz. Tavan kötüye kullanımı
  (tek istekte bütün listeyi çekmeyi) önleyen bir güvenlik sınırıdır (ADR-0010 §4 notu).
- **Özel nitelikli verinin senkronizasyonu bu tabloda değildir** (revizyon 1.2, B-04): Bu
  ayar ortam yapılandırmasıdır (`Logo:SyncSpecialCategoryData`) ve ekrandan
  değiştirilemez; ayrıntı ADR-0020 §6. İlk sürümde tabloda parametre olarak yer alıyordu.
- **Görünür kalma süresi** ayrılan kişinin **son çıkış tarihinden** başlar (ADR-0019 §6).
- Parametre değişiklikleri denetim izine yazılır (mevcut altyapı).

## Gerekçe

- Her çekirdek, onaylı bir T1 gereksinimine bağlıdır ve asıl modülün ad alanında kurulur.
  Asıl modül geldiğinde çekirdek taşınmaz, genişletilir.
- Uygulama içi bildirimin veritabanına yazılması, bellek içi kuyruğun kayıp riskini İK
  talepleri için ortadan kaldırır; Y1'in bütün altyapısını beklemeye gerek kalmaz.
- E-postada değer yazmamak, kan grubu gibi değerlerin kurum dışına çıkabilecek bir kanala
  hiç girmemesini sağlar.

## Değerlendirilen alternatifler

| Alternatif | Artıları | Eksileri | Neden seçilmedi |
|---|---|---|---|
| T1'i Y1, Y2, Y3'ü bekletmek | Modüller sırayla | T1 A3'e kadar ertelenir | `KR-115`, `KR-119` kararları |
| Ünvan ve görev listesini kodda sabitlemek | Basit | İK yeni görev ekleyemez; "kod değişikliği olmadan" kriteri karşılanmaz | PG-PERS-21, 43 |
| Yalnızca e-posta, uygulama içi bildirim yok | Basit | PG-PERS-37 uygulama içi bildirim istiyor; bellek kuyruğunda kayıp | Gereksinim |
| E-postada önerilen değeri yazmak | İK'ya kolaylık | Kişisel ve özel nitelikli veri e-postada | KVKK |

## Sonuçlar

**Olumlu:**
- T1, A3'teki modülleri beklemeden onaylı gereksinimlerinin tamamını karşılar.
- Y1, Y2 ve Y3 için ilk çalışan parçalar ve ad alanları hazır olur.

**Olumsuz / kabul edilen ödünler:**
- Y1, Y2 ve Y3 kendi gereksinim toplantılarında bu çekirdeklerin biçimini değiştirebilir.
  Değişiklik o modülün işidir.
- "İK'ya bildir" e-postası bellek içi kuyrukla gider ve kaybolabilir; talep kaybolmaz.

**Bu kararın getirdiği yükümlülükler:**
- ADR-0016 modül tablosuna `Reference` ve gerekli bağımlılıklar eklenir.
- İzin listesine `reference.*`, `audit.log.view`, `personnel.correction.*` ve
  `personnel.quality.*` izinleri eklenir (ADR-0019 §4).
- Parametre kataloğuna yeni parametreler eklenir. Parametre ekranı mevcut altyapıyla
  gösterir.

## Geri dönüş maliyeti

Düşük–orta. Çekirdekler küçüktür ve kendi ad alanlarındadır.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-22 | 1.0 | İlk sürüm: T1 tasarım kararları (#245 maddeler 6, 8, 9, 10, 11) | Bilgi İşlem |
| 2026-10-22 | 1.1 | Revizyon (#267, #268): önerilen değer talep kapanınca silinir, talebin saklama süresi ve izinleri (B-03, B-14, B-18); e-posta konu satırı (B-18); dışa aktarmada kişi kimlikleri (B-09); özel nitelikli senkronizasyon parametre değil (B-04); görünür kalma süresinin başlangıcı (B-15) | Bilgi İşlem |
| 2026-10-22 | 1.2 | §3: şema sapması ve başarısız senkronizasyon için ikinci bildirim türü (SYG-PERS AN-06, #274) | Bilgi İşlem |
| 2026-10-23 | 1.3 | §2: `audit.log.view` İK'da da (`KR-128`, #278) | Bilgi İşlem |
| 2026-10-23 | 1.4 | §1: üçüncü referans türü kan grubu, değiştirilemez sistem tanımı (`KR-129`, #280) | Bilgi İşlem |
