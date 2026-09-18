# 33061 Süreç Gözden Geçirme Raporu — A0 ve A1

**Belge kimliği:** MAN.8-SGR-2026-09-18
**Süreç:** MAN.8 — Kalite Güvence (`MAN.8.BP3` — süreç değerlendirmelerinin yapılması)
**Değerlendirme tarihi:** 2026-09-18
**Değerlendirilen dönem:** Proje başlangıcı … A1 Teknik İskelet kapanışı (2026-09-15) ve sonrası
**Dayanak:** TS ISO/IEC TS 33061 (süreç boyutu) · TS ISO/IEC 33020 (ölçüm çerçevesi)
**Değerlendiren:** Bilgi İşlem

---

## 1. Amaç ve sınırlar

Bu rapor, A0 ve A1 boyunca 33061 süreçlerinin **fiilen** nasıl işletildiğini, standardın
kendi ölçütlerine karşı kanıta dayalı olarak ortaya koyar.

### 1.1 Bu bir öz değerlendirmedir

**Değerlendiren ile süreci yürüten aynı kişidir.** TS ISO/IEC 33020 anlamında
**bağımsız bir değerlendirme değildir** ve resmî bir yetenek seviyesi beyanı
oluşturmaz. Amacı, belgelendirme öncesinde eksikleri **zamanında** görmektir.

Bu sınır, raporun değerini azaltmaz ama okunma biçimini belirler: aşağıdaki dereceler
bir **hedef** değil, bir **teşhistir**.

### 1.2 Yöntem

1. Kapsamdaki 15 sürecin **süreç çıktıları (process outcomes)** standardın Madde 5'inden
   okundu — hafızadan veya türetilmiş belgeden değil.
2. Her çıktı için depodaki, GitHub'daki ve çalışan sistemdeki kanıt arandı.
3. Kanıt **fiilen mevcut** olduğunda sayıldı. "Planlandı", "yapılacak", "şu belgede
   söz verildi" sayılmadı.
4. Derecelendirme TS ISO/IEC 33020 Madde 5.3 ölçeğiyle yapıldı.

### 1.3 Ölçek ve seviye kuralı (33020 Madde 5.3 ve Tablo 1)

| Kod | Anlam | Başarı |
|---|---|---|
| `N` | Sağlanmadı | %0 – %15 |
| `P` | Kısmen sağlandı | >%15 – %50 |
| `L` | Büyük ölçüde sağlandı | >%50 – %85 |
| `F` | Tam sağlandı | >%85 – %100 |

**Seviye 2 için:** PA 1.1 = `F` **ve** PA 2.1 ≥ `L` **ve** PA 2.2 ≥ `L`.
**Seviye 1 için:** PA 1.1 ≥ `L`.

> Ölçek ve seviye tablosu bu gözden geçirmede standardın metninden **yeniden
> doğrulandı**; `00-OLGUNLUK-SEVIYESI-KRITERLERI.md` §1–§2'deki aktarım doğrudur.
> PA 2.1'in yedi çıktısı (a–g) ve PA 2.2'nin beş çıktısı (a–e) da birebir uyuşmaktadır.

### 1.4 "Henüz başlamadı" ile "eksik" ayrımı

Bir süreç hiç yürütülmediyse, biçimsel bir değerlendirmede PA 1.1 `N` olur. Ancak
**neden** `N` olduğu önemlidir:

- **Sırası gelmedi:** TEC.11 Geçerleme, kabul edilecek bir modül olmadığı için
  yürütülmedi. Bu bir eksiklik değil, plana uygunluktur.
- **Sırası geldi, yapılmadı:** MAN.2'nin dönemsel durum raporu planda taahhüt edildi,
  dönem geçti, rapor üretilmedi. Bu bir eksikliktir.

Tablolarda bu ayrım ayrıca belirtilmiştir.

---

## 2. Yönetici özeti

**Ana sonuç: kapsamdaki 15 süreçten yalnızca biri (MAN.4 Risk Yönetimi) bugün
Seviye 2 ölçütlerini karşılamaktadır.**

Bunun tek bir baskın nedeni var ve teknik değil, **kayıt** ile ilgilidir:

> **Seviye 2'nin önündeki en büyük tek engel `PA 1.1 = F` koşuludur.** Sekiz sürecin
> çıktı listesinde **"izlenebilirlik (traceability)"** maddesi bulunur ve
> `docs/33061/izlenebilirlik-matrisi.md` **hiç oluşturulmamıştır.** Tek başına bu
> eksik, sekiz sürecin `F` almasını yapısal olarak imkânsız kılar.

İkinci sistemik neden, **15 sürecin yaklaşım belgelerinin (`YAKLASIM.md`) hiç
yazılmamış olmasıdır** (İş Kırılım Yapısı 1.9, "⏳ Bekliyor"). Bu, PA 2.2'nin (a) ve
(b) maddelerini süreç bazında zayıflatır.

**Buna karşılık yürütmenin kendisi güçlüdür.** Kod, test, kalite kapıları, karar
disiplini ve düzeltici faaliyet kültürü, Seviye 2'nin *ruhunu* fazlasıyla karşılar.
Eksik olan, yapılan işin **süreç kanıtına dönüştürülmesidir**.

| Gösterge | Değer |
|---|---|
| Değerlendirilen süreç | 15 |
| Bugün Seviye 2 | **1** (MAN.4) |
| Bugün Seviye 1 | 7 (TEC.5, TEC.7, TEC.8, TEC.9, TEC.10, MAN.1, MAN.5, MAN.8 → 8) |
| Seviye 0 / henüz başlamadı | 6 |
| Açılan bulgu | 11 (2 yüksek, 6 orta, 3 düşük) |

---

## 3. Süreç bazında değerlendirme

### 3.1 MAN.4 — Risk Yönetimi

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **F** | Beş çıktının tamamı karşılanıyor |
| PA 2.1 | **L** | Sistematik; dönemsel risk raporu eksik |
| PA 2.2 | **L** | Sürümlü ve onaylı; saklama süresi tanımsız |
| **Seviye** | **2** | |

**Çıktı kanıtları:** (a) 18 risk tanımlı · (b) tamamı O/E/puan ile analiz edilmiş ·
(c) her risk için önlem seçilmiş · (d) önlem **uygulanmış** — `R-17` TLS ile kapandı,
`R-12` yeniden ölçüldü · (e) durum değişiklikleri izlenmiş — `R-02` puanı düşürüldü,
`R-07`/`R-10`/`R-11`/`R-17` kapandı, değişiklik geçmişi 7 sürüm.

**Bu süreç neden diğerlerinden ayrışıyor:** risk defteri, işin **doğal akışında**
güncellendi. Kanıt üretmek için ayrıca çalışılmadı; iş yapılırken kayıt oluştu. Diğer
süreçlerde eksik olan tam olarak budur.

**Zayıflık:** Proje planı §8, "dönemsel risk raporu" taahhüt ediyor;
`MAN.4/raporlar/` boş. Risk defterinin kendisi bu işlevi büyük ölçüde görüyor, bu
yüzden PA 2.1 `L` (F değil).

---

### 3.2 MAN.1 — Proje Planlama

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **L** | Dört çıktının üçü tam; (c) eksik |
| PA 2.1 | **L** | Rol, yetkinlik ve kaynak tanımı güçlü; izleme kaydı zayıf |
| PA 2.2 | **L** | Sürümlü ve onaylı |
| **Seviye** | **1** | |

**Kanıtlar:** (a) `proje-plani.md`, `is-kirilim-yapisi.md` ✔ · (b)
`roller-ve-sorumluluklar.md` RACI ✔ · (d) planlar **etkinleştirildi** — A0 ve A1
yürütüldü, A1 kapanış değerlendirmesi üretildi ✔

**(c) "Kaynaklar resmen talep edilir ve taahhüt edilir" — karşılanmıyor.** Plan
kaynakları listeliyor ancak İş Kırılım Yapısı 1.10'a göre **vizyon-kapsam ve planın
onayı hâlâ "⏳ Bekliyor"**. Onaylanmamış bir plan, "resmî taahhüt" kanıtı üretmez.
`roller-ve-sorumluluklar.md` §7'deki dört açık iş (RACI teyidi, KVKK sorumlusunun
bildirimi vb.) da aynı boşluğu gösteriyor. → `BULGU-11`

**PA 2.1 özel notu:** (d) sorumluluk ve (f) yetkinlik maddeleri, projede
**örnek gösterilecek** güçtedir: `roller-ve-sorumluluklar.md` doğrudan PA madde
numaralarına atıf yaparak yazılmış, yetkinlik boşlukları ve kapatma yöntemi açıkça
listelenmiş, tek kişiye bağımlılık ayrı bir başlıkta zayıflık olarak kabul edilmiş.

---

### 3.3 MAN.2 — Proje Değerlendirme ve Kontrol

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **P+** | On çıktıdan dördü karşılanıyor |
| PA 2.1 | **P+** | İzleme var, ölçüm yok |
| PA 2.2 | **L** | Üretilen kayıtlar kontrollü |
| **Seviye** | **0** | |

**Karşılanan:** (d) teknik ilerleme gözden geçirmesi — A1 kapanış değerlendirmesi ✔ ·
(e) sapmalar incelendi — LOGO Excel hatası, `crypto.randomUUID` arızası ✔ ·
(g) düzeltici faaliyet tanımlandı ve yürütüldü — #17, #44, #46 ✔ · (i) bir sonraki
aşamaya geçiş yetkilendirildi — A1 kapanışı ve onayı ✔

**Karşılanmayan:** (a) **performans ölçütleri mevcut değil.** Proje planı §8.1 beş
ölçüt tanımlıyor (modül çevrim süresi, açık/kapanan risk sayısı, test kapsamı yüzdesi,
kabul bulgusu sayısı, değişiklik talebi sayısı). Hiçbiri **ölçülüp kaydedilmemiş**. ·
(b) ve (c) rol ve kaynak yeterliliği hiç değerlendirilmemiş · (f) paydaş
bilgilendirmesi sözlü/mesaj yoluyla; **dönemsel durum raporu üretilmemiş** —
`MAN.2/raporlar/` boş.

> Bu, planda **taahhüt edilmiş** ve dönemi **geçmiş** bir çıktıdır; "sırası gelmedi"
> kapsamında değildir. → `BULGU-04`

---

### 3.4 MAN.5 — Konfigürasyon Yönetimi

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **L−** | Değişiklik kontrolü güçlü; tanımlama ve denetim eksik |
| PA 2.1 | **L** | |
| PA 2.2 | **L** | |
| **Seviye** | **1** | |

**Güçlü:** (c) değişiklikler kontrol ediliyor — dal koruma, zorunlu PR, 8 CI kapısı,
squash merge; telafi kontrolleri ayrı belgede ✔ · (d) konfigürasyon durumu erişilebilir
— Git geçmişi, GitHub Projects (47 öğe) ✔

**Eksik:**
- (a) **`konfigurasyon-ogeleri.md` yok.** Doküman haritası bu belgeyi "neyin kontrol
  altında olduğu" çıktısı olarak sayıyor. Pratikte Git her şeyi izliyor, ancak
  *hangi öğelerin konfigürasyon öğesi sayıldığı* yazılı değil.
- (e) **Konfigürasyon denetimi hiç yapılmadı.** `MAN.5/raporlar/` boş.
- (b) ve (f) **baseline ve sürüm yayımı yok** — Git etiketi 0, GitHub Release 0.
  Bunlar plana göre *kabul edilen modülle* üretilir; henüz kabul edilmiş modül yok,
  dolayısıyla **"sırası gelmedi"** sayıldı ve derece düşürülmedi.
- `CHANGELOG.md` **ilk commit'ten beri hiç güncellenmedi** — aradan 23 PR geçti.
  Doküman haritası bunu MAN.5'in "sürüm kontrolü" ve TEC.7'nin "gerçekleştirme
  raporu" çıktısı olarak sayıyor. → `BULGU-05`

---

### 3.5 MAN.6 — Bilgi Yönetimi

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **L−** | Tanımlama güçlü; saklama ve erişim tanımsız |
| PA 2.1 | **P+** | |
| PA 2.2 | **L−** | |
| **Seviye** | **1** | |

**Güçlü:** (a) yönetilecek bilgi tanımlı — `00-DOKUMAN-HARITASI.md` süreç süreç
tablolanmış ✔ · (b) gösterim tanımlı — kimliklendirme şeması (`PG-`, `REQ-`, `ADR-`,
`R-`, `TS-`), tarih biçimi, dil kuralı ✔ · (d) durum tanımlı — her belgede
"Son güncelleme" ve değişiklik geçmişi ✔

**Eksik:** (c) bilginin **imhası/saklama süresi** tanımsız · (e) **`bilgi-kayit-defteri.md`
yok** — hangi bilginin sahibi kim, nerede, ne kadar saklanacak sorusu cevapsız.
Yedekleme düzeni de yazılı değil. → `BULGU-06`

**Ek gözlem:** Paydaşla yapılan soru-cevap alışverişinin kayıtları
`TEC.2/kayitlar/` altında tutulmuyor ve **sürüm kontrolü dışında**. Gereksinimlerin
kaynağı bu alışveriş olduğu için, izlenebilirlik zincirinin ilk halkası kayıt altında
değil. → `BULGU-09`

---

### 3.6 MAN.8 — Kalite Güvence

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **L** | Ürün değerlendirmesi güçlü; süreç değerlendirmesi bugüne kadar yoktu |
| PA 2.1 | **L** | |
| PA 2.2 | **L** | |
| **Seviye** | **1** | |

**Güçlü:** (b) ölçüt ve yöntemler tanımlı — ADR-0011 §6 kalite kapıları, PR
şablonundaki Tamamlanma Tanımı ✔ · (c) **ürün değerlendirmesi her PR'da fiilen
yapılıyor** — 8 CI kapısı, 23 PR ✔ · (d) sonuçlar paydaşa iletiliyor — PR kontrol
listesi görünür ✔ · (e) olaylar çözüldü — #27, #39 ✔ · (f) önceliklendirilmiş
problemler ele alındı — #17, #44 düzeltici faaliyetleri ✔

**Eksik:**
- (a) **Kalite güvence yordamları ayrı belge olarak yok.** Doküman haritası
  `tamamlanma-tanimi.md` ve `kod-gozden-gecirme-kontrol-listesi.md` belgelerini
  sayıyor; ikisi de yok. İçerik PR şablonunda gömülü — işlevsel olarak çalışıyor,
  ancak sürecin yordamı tek bir yerde tanımlı değil. → `BULGU-07`
- **`MAN.8.BP3` — süreç değerlendirmesi bugüne kadar hiç yapılmamıştı.** Bu rapor
  ilkidir. Ürün değerlendirmesi (BP2) her gün işliyordu; süreç değerlendirmesi hiç.

> Bu eksiğin bugün kapanıyor olması, MAN.8'in **çalıştığının** kanıtıdır
> (`00-OLGUNLUK` §7.4: *zayıflık gizlenmez, kaydedilir*).

---

### 3.7 TEC.2 — Paydaş İhtiyaç ve Gereksinimleri

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **P+** | Dokuz çıktıdan dördü |
| PA 2.1 | **P+** | |
| PA 2.2 | **L−** | |
| **Seviye** | **0** | |

**Karşılanan:** (a) paydaşlar tanımlı — `paydas-listesi.md` ✔ · (b) hayat döngüsü
kavramları ve kullanım bağlamı — `vizyon-ve-kapsam.md` ✔ · (c) kısıtlar tanımlı —
plan §7, karar kayıt defteri (LOGO salt-okunur, eski koda dayanmama vb.) ✔ ·
(h) destekleyici sistemler mevcut ✔

**Karşılanmayan:** (d)/(e) **paydaş gereksinimleri depoda yok** — `PG-<MODÜL>.md`
dosyası bulunmuyor · (f) kritik performans ölçütleri tanımsız · (g) **paydaş mutabakatı
alınmadı** — İK gereksinim toplantıları henüz yapılmadı · (i) **izlenebilirlik yok**.

> (d), (e) ve (g) için durum **"sırası gelmedi"**: `KR-068` gereği her modül kendi İK
> gereksinim toplantısıyla başlar ve ilk modül (T3) henüz başlamadı. T3 Kimlik Yönetimi
> için hazırlanan gereksinim taslağı bir **toplantı girdisidir**, onaylanmış gereksinim
> değildir ve bilinçli olarak repo dışında tutulmaktadır.
>
> (f) ve (i) ise "sırası gelmedi" kapsamında **değildir**; şimdiden tanımlanabilirdi.

---

### 3.8 TEC.5 — Tasarım Tanımlama

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **L** | Sekiz çıktıdan altısı |
| PA 2.1 | **L** | |
| PA 2.2 | **L** | |
| **Seviye** | **1** | |

**Güçlü:** (a) tasarım özellikleri tanımlı — 15 ADR + `docs/mimari/` ✔ ·
(c) tasarım etkinleştiricileri seçilmiş — ADR-0001 teknoloji yığını ✔ · (d) arayüzler
tanımlı — OpenAPI sözleşmesi, derleme zamanında üretiliyor ✔ · (e) **alternatifler
değerlendirilmiş** — her ADR "Değerlendirilen alternatifler" bölümü taşıyor ✔ ·
(f) tasarım ürünleri geliştirilmiş ✔ · (g) destekleyici sistemler ✔

**Eksik:** (b) **gereksinimler tasarım öğelerine tahsis edilmemiş** — tahsis edilecek
sistem gereksinimi henüz yok (TEC.3 başlamadı) · (h) **izlenebilirlik yok** ·
`docs/mimari/fonksiyon-modeli.md` ve `entegrasyon-arayuzleri.md` doküman haritasında
sayılıyor ancak mevcut değil.

---

### 3.9 TEC.7 — Gerçekleştirme

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **L+** | Beş çıktıdan dördü |
| PA 2.1 | **L** | |
| PA 2.2 | **L** | |
| **Seviye** | **1** | |

**Güçlü:** (a) gerçekleştirme kısıtları tanımlı — kodlama standartları, katman
kuralları, `KR-058` ✔ · (b) sistem öğesi gerçeklendi — 142 kaynak dosya, çalışan
iskelet ✔ · (c) paketlendi — Docker imajları, UAT'de çalışıyor ✔ · (d) destekleyici
sistemler ✔

**Eksik:** (e) **izlenebilirlik kısmi.** PR ↔ issue bağı **güçlü ve otomatik
denetleniyor** (#17 düzeltici faaliyeti ve CI kapısı). Ancak gereksinim ↔ kod bağı
yok — çünkü gereksinim kimliği (`REQ-`) henüz üretilmedi.

---

### 3.10 TEC.8 — Entegrasyon

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **L** | Sekiz çıktıdan beşi |
| PA 2.1 | **L** | |
| PA 2.2 | **P+** | Entegrasyon raporu üretilmiyor |
| **Seviye** | **1** | |

**Güçlü:** (b) yaklaşım ve kontrol noktaları tanımlı — CI hattı, Compose yığınları ✔ ·
(d) sistem entegre — UAT'de üç konteyner çalışıyor ✔ · (e) öğeler arası arayüzler
sınanıyor — sağlık kontrolleri, Testcontainers ile gerçek PostgreSQL ✔ ·
(g) sonuçlar ve anomaliler belirlendi — CI kayıtları, #27 ve #39 ✔

**Eksik:** (f) **dış çevreyle arayüzler sınanmadı** — LOGO ve NetGSM entegrasyonu
henüz yok (sırası gelmedi) · (h) izlenebilirlik yok · `TEC.8/raporlar/` boş.

---

### 3.11 TEC.9 — Doğrulama

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **L+** | Yedi çıktıdan altısı |
| PA 2.1 | **L** | |
| PA 2.2 | **L−** | Doğrulama raporu üretilmiyor |
| **Seviye** | **1** | |

**Bu, projenin en güçlü süreçlerinden biridir.**

(a) doğrulama kısıtları tanımlı — ADR-0011 ✔ · (b) destekleyici sistemler —
Testcontainers ile **gerçek PostgreSQL**; EF In-Memory bilinçli olarak yasaklandı ✔ ·
(c) doğrulandı — **168 test** (backend ~113, frontend 55; bu gözden geçirmede frontend
tarafı çalıştırılarak 55 olarak **doğrulandı**) ✔ · (d) düzeltici faaliyet verisi
üretiliyor — CI kayıtları ✔ · (f) sonuç ve anomaliler ✔

**(e) "nesnel kanıt" maddesi örnek niteliğinde karşılanıyor:** kontroller **bilerek
kırılarak** kanıtlandı — mimari testleri, LOGO salt-okunur reddi, denetim izi
değiştirilemezliği, hata ayrıntısı sızıntısı, kapsam eşiği, PR izlenebilirlik kapısı,
sağlık kontrolü ayrımı ve en son PR başlığı kapısı. Bir kapının yalnızca yeşil yandığını
görmek, onun bir şey ölçtüğünü kanıtlamaz; bu projede kapılar **düşerken de** görüldü.

**Eksik:** (g) izlenebilirlik — test ↔ gereksinim bağı yok · `TEC.9/raporlar/` boş;
doküman haritası "test sonuç ve kapsam raporları" çıktısını burada bekliyor.

---

### 3.12 TEC.10 — Geçiş

| PA | Derece | Gerekçe |
|---|---|---|
| PA 1.1 | **L−** | Sekiz çıktıdan dördü; kalanların çoğu sırası gelmedi |
| PA 2.1 | **L** | |
| PA 2.2 | **L** | |
| **Seviye** | **1** | |

**Güçlü:** (b) destekleyici sistemler ✔ · (c) **saha hazırlandı** — UAT sunucusu
kuruldu · (d)/(g) sistem kurulu ve **çalışır durumda** — HTTPS ile yayında, doğrulaması
ölçülerek yapıldı · (f) sonuçlar ve anomaliler belirlendi — runbook, dağıtım betiğinin
otomatik doğrulaması (API'nin dışarı açıldığını **kendi kontrolü yakaladı**) ✔

**Eksik / sırası gelmedi:** (e) kullanıcı eğitimi yapılmadı · veri göçü başlamadı ·
üretim ortamına geçiş yok · (h) izlenebilirlik yok.

---

### 3.13 Başlamamış süreçler

| Süreç | PA 1.1 | Neden |
|---|---|---|
| TEC.3 Sistem/Yazılım Gereksinimleri | `N` | **Sırası gelmedi** — paydaş gereksinimi olmadan sistem gereksinimi türetilemez |
| TEC.11 Geçerleme | `N` | **Sırası gelmedi** — kabul edilecek modül yok |
| TEC.13 Bakım | `P−` | Üretim yok; yalnızca hata issue'ları işletildi (#27, #39) |

Bu üçü için derece **bugün için doğrudur ve sorun değildir**; T3 modülü tamamlandığında
üçü de kanıt üretmeye başlar.

---

## 4. Derecelendirme tablosu

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | Ana engel |
|---|---|---|---|---|---|
| MAN.4 Risk Yönetimi | **F** | L | L | **2** | — |
| MAN.1 Proje Planlama | L | L | L | 1 | Planın onaylanmamış olması |
| MAN.5 Konfigürasyon Yönetimi | L− | L | L | 1 | Konfigürasyon öğeleri belgesi, denetim |
| MAN.6 Bilgi Yönetimi | L− | P+ | L− | 1 | Bilgi kayıt defteri, saklama |
| MAN.8 Kalite Güvence | L | L | L | 1 | Yordam belgeleri; süreç değerlendirmesi |
| TEC.5 Tasarım Tanımlama | L | L | L | 1 | İzlenebilirlik |
| TEC.7 Gerçekleştirme | L+ | L | L | 1 | İzlenebilirlik |
| TEC.8 Entegrasyon | L | L | P+ | 1 | İzlenebilirlik, rapor |
| TEC.9 Doğrulama | L+ | L | L− | 1 | İzlenebilirlik, rapor |
| TEC.10 Geçiş | L− | L | L | 1 | İzlenebilirlik; göç ve eğitim sırası gelmedi |
| MAN.2 Değerlendirme ve Kontrol | P+ | P+ | L | 0 | Ölçüt ölçülmüyor, durum raporu yok |
| TEC.2 Paydaş Gereksinimleri | P+ | P+ | L− | 0 | Gereksinim ve mutabakat (sırası gelmedi) |
| TEC.13 Bakım | P− | P | P | 0 | Üretim yok |
| TEC.3 Sistem Gereksinimleri | N | — | — | 0 | Sırası gelmedi |
| TEC.11 Geçerleme | N | — | — | 0 | Sırası gelmedi |

---

## 5. Bulgular

Önem ölçütü: **Yüksek** = birden fazla sürecin seviyesini engelliyor ·
**Orta** = tek sürecin çıktısını engelliyor · **Düşük** = tutarlılık/derinlik.

### BULGU-01 — İzlenebilirlik matrisi hiç oluşturulmadı · **Yüksek**

`docs/33061/izlenebilirlik-matrisi.md` doküman haritasında **dokuz sürece** hizmet eden
ortak kanıt olarak tanımlı; dosya yok.

Standart, TEC.2(i), TEC.3(f), TEC.5(h), TEC.7(e), TEC.8(h), TEC.9(g), TEC.10(h) ve
TEC.11(h) çıktılarında izlenebilirlik istiyor. **Bu tek eksik, sekiz sürecin PA 1.1'inin
`F` olmasını yapısal olarak engelliyor** — yani Seviye 2'nin önündeki en büyük engel.

**Kapatma:** T3 modülü başlarken matris kurulur ve **modülle birlikte doldurulur**.
Geriye dönük doldurmak hem pahalıdır hem inandırıcı değildir.

### BULGU-02 — 15 sürecin yaklaşım belgesi (`YAKLASIM.md`) yazılmadı · **Yüksek**

İş Kırılım Yapısı 1.9 bu işi "⏳ Bekliyor" olarak gösteriyor. Doküman haritası her süreç
için `YAKLASIM.md` bekliyor; **hiçbiri yok**.

Etkisi PA 2.2 (a) ve (b): *"sürecin dokümante edilmiş bilgi gereksinimleri belirlenir"*
ve *"kontrol gereksinimleri belirlenir"*.

**Hafifletici:** `00-DOKUMAN-HARITASI.md` bu iki maddeyi **genel düzeyde** karşılıyor —
hangi sürecin hangi belgeyi üreteceği ve §4'teki doküman kuralları yazılı. Bu nedenle
PA 2.2 dereceleri `N` değil `L` civarında kaldı. Ancak süreç bazında yaklaşım
(sıklık, eşik, sorumluluk) tanımsız.

### BULGU-03 — Öz değerlendirme güncellenmedi; iki farklı ölçek kullanılıyor · **Orta**

`00-OLGUNLUK-SEVIYESI-KRITERLERI.md` §6 tablosu **2026-09-04'ten beri güncellenmedi**;
hâlâ "İskelet kurulmadı" yazıyor. Proje planı §8 bu tablonun "her modül kapanışında, en
geç 3 ayda bir" güncellenmesini öngörüyor ve A1 kapanışında (15.09) güncellenmedi.

Bunun yerine A1 kapanış belgesi §9'da **farklı bir sözlük** kullanıldı:
"Kurulmuş / İşletiliyor / Kısmen / Başlamadı". Bu sözlük 33020'de yoktur ve N/P/L/F ile
birleştirilemez; dolayısıyla **yetenek seviyesine yuvarlanamaz**.

**Kapatma:** Bu rapor ile §6 tablosu güncellendi. Bundan sonra yalnızca N/P/L/F
kullanılacak; aşama kapanış belgeleri kendi ölçeğini üretmeyecek, §6'ya atıf yapacak.

### BULGU-04 — MAN.2'nin planlanan çıktıları üretilmiyor · **Orta**

Dönemsel durum raporu yok (`MAN.2/raporlar/` boş) ve plan §8.1'deki beş ölçütün hiçbiri
ölçülmemiş. Plan bunları taahhüt ediyor ve dönem geçti.

**Kapatma:** A2 başlarken ilk durum raporu; ölçütler her aşama kapanışında ölçülür.
Ölçüt sayısı gerçekçi değilse **plan revize edilir** — ölçülmeyecek bir ölçütü planda
tutmak, planın kendisini güvenilmez kılar.

### BULGU-05 — MAN.5: konfigürasyon öğeleri belgesi, denetim ve CHANGELOG · **Orta**

- `konfigurasyon-ogeleri.md` yok → MAN.5(a) zayıf
- Konfigürasyon denetimi hiç yapılmadı → MAN.5(e) karşılanmıyor
- `CHANGELOG.md` ilk commit'ten beri hiç güncellenmedi; aradan **23 PR** geçti

### BULGU-06 — MAN.6: bilgi kayıt defteri ve saklama düzeni yok · **Orta**

`bilgi-kayit-defteri.md` yok; saklama süresi, imha ve yedekleme yazılı değil.
PA 2.2(e) doğrudan bunu istiyor: *bilgi, sürecin planlandığı gibi yürütüldüğüne güven
verecek ölçüde saklanır*.

### BULGU-07 — MAN.8: kalite yordamları ayrı belge değil; süreç değerlendirmesi yoktu · **Orta**

`tamamlanma-tanimi.md` ve `kod-gozden-gecirme-kontrol-listesi.md` yok; içerik PR
şablonunda gömülü. İşliyor, ancak MAN.8(a) "yordamlar tanımlanır ve uygulanır"
maddesinin *tanımlanır* kısmı zayıf.

`MAN.8.BP3` (süreç değerlendirmesi) bu rapora kadar hiç yürütülmedi.

### BULGU-08 — Milestone ataması İş Kırılım Yapısı ile uyumsuz · **Orta**

A1 kapandıktan sonra açılan UAT (#37), TLS (#41) ve süreç işleri (#44, #46, #48)
**"A2 — Temel Modüller"** milestone'una atandı. Oysa:

- A2, İş Kırılım Yapısı'na göre **T1–T5 modülleridir**; bu işlerin hiçbiri modül değil.
- Aynı issue'ların süreç etiketi **`surec:TEC.10` (Geçiş)**.
- **"AS — Geçiş ve Devreye Alma"** milestone'u mevcut ve **boş**.

Sonuç: aşama ilerlemesi ölçümü bozuluyor — A2 "12 kapalı" görünüyor ama tek bir temel
modül işi yapılmadı. PA 2.1(c) *"performans ölçülür ve değerlendirilir"* maddesini
doğrudan zayıflatır.

**Kapatma:** Geçiş nitelikli işler `AS`'ye taşınır; süreç/kalite işleri için ayrı bir
milestone tanımlanır (ör. "Süreç ve Kalite — sürekli"). Aşama milestone'u, WBS'teki
aşamadan başka bir anlam taşımamalıdır.

### BULGU-09 — Paydaş etkileşim kayıtları süreç klasöründe ve sürüm kontrolünde değil · **Düşük**

Gereksinimlerin kaynağı olan soru-cevap alışverişi `TEC.2/kayitlar/` altında tutulmuyor
ve sürüm kontrolü dışında. İzlenebilirlik zincirinin ilk halkası kayıt altında değil.

**Kapatma:** Her İK toplantısı `kayitlar/YYYY-AA-GG-<konu>.md` üretecek. Kişisel veri
içeren ekler depoya girmez; kayıt, kararı ve gerekçeyi taşır.

### BULGU-10 — Kalite kapısı sayısı belgeler arasında tutarsız · **Düşük**

ADR-0011 §6: **11 kapı** · İş Kırılım Yapısı 2.12: **12 kapı** · A1 kapanışı:
**8 kapı**. Üçü de "kapı" diyor ama farklı şeyleri sayıyor (kural / iş / CI job).

**Kapatma:** Tek bir tanım seçilir ("kapı = PR'ı engelleyen kural") ve üç belge
hizalanır.

### BULGU-11 — Planın ve RACI'nin onayı alınmadı · **Düşük**

İş Kırılım Yapısı 1.10 ve `roller-ve-sorumluluklar.md` §7'deki açık işler duruyor.
MAN.1(c) *"kaynaklar resmen talep edilir ve taahhüt edilir"* bu nedenle karşılanmıyor;
MAN.1'in PA 1.1'i `F` olamıyor.

---

## 6. Güçlü yönler

Bu rapor eksiklere odaklandı; dengeyi kurmak için aşağıdakiler de kanıtlıdır.

1. **Kontroller kırılarak kanıtlanıyor.** Sekizden fazla kontrol, bilerek ihlal
   edilerek sınandı. Bu, PA 1.1'in "nesnel kanıt" maddesini karşılamanın en güçlü
   biçimidir ve çoğu projede bulunmaz.
2. **Düzeltici faaliyet kültürü işliyor.** #17, #44 ve bu rapor; üçü de *tespit → kök
   neden → yazılı kural → otomatik kapı → kanıt* döngüsünü tamamladı.
3. **Karar disiplini.** 68 karar kaydı ve 16 ADR, her biri gerekçesi ve reddedilen
   alternatifiyle. TEC.5'in "tasarım gerekçesi" çıktısı fazlasıyla karşılanıyor.
4. **Risk yönetimi gerçekten işliyor.** Riskler ölçülüyor, düzeltiliyor ve
   kapanıyor — hatalı bir ölçüm (R-12) tespit edilip düzeltilebildi.
5. **Doküman değişiklikleri de PR'dan geçiyor.** PA 2.2(d) — *gözden geçirilir ve
   onaylanır* — bu sayede istisnasız karşılanıyor.
6. **Zayıflık gizlenmiyor.** Tek kişiye bağımlılık, dal koruma kısıtı ve UAT'nin
   şifresiz yayını; üçü de kayıt altına alınmış zayıflıklar.

---

## 7. Öneriler ve sıralama

**T3 Kimlik Yönetimi başlamadan önce (ön koşul):**

| # | İş | Kapattığı bulgu |
|---|---|---|
| 1 | İzlenebilirlik matrisinin kurulması ve T3 ile birlikte doldurulması | BULGU-01 |
| 2 | Milestone düzeninin WBS ile hizalanması | BULGU-08 |
| 3 | TEC.2 için `YAKLASIM.md` ve toplantı kaydı şablonu | BULGU-02, BULGU-09 |

**T3 süresince, işin doğal akışında:**

| # | İş | Kapattığı bulgu |
|---|---|---|
| 4 | Kalan 14 `YAKLASIM.md` — sürecin ilk kez işletildiği anda yazılır | BULGU-02 |
| 5 | İlk dönemsel durum raporu ve §8.1 ölçütlerinin ölçülmesi | BULGU-04 |
| 6 | `bilgi-kayit-defteri.md` ve saklama/yedekleme düzeni | BULGU-06 |
| 7 | `konfigurasyon-ogeleri.md`; `CHANGELOG` güncelliği; T3 kabulünde ilk baseline etiketi | BULGU-05 |

**Fırsat bulundukça:**

| # | İş | Kapattığı bulgu |
|---|---|---|
| 8 | `tamamlanma-tanimi.md` ve kod gözden geçirme kontrol listesi | BULGU-07 |
| 9 | Kalite kapısı sayısının üç belgede hizalanması | BULGU-10 |
| 10 | Plan ve RACI onayının alınması | BULGU-11 |

> **Sıralama ilkesi:** Eksiklerin çoğu, T3 çevrimi sırasında **doğal olarak** üretilecek
> kanıtlardır. Bugün toplu hâlde belge yazmak yerine, süreci işletirken kayıt üretmek
> hem ucuzdur hem değerlendirmede "sistematik yaklaşım" olarak görünür
> (`00-OLGUNLUK` §7.1). Bu yüzden yalnızca üç iş "ön koşul" sayılmıştır.

---

## 8. Sonraki gözden geçirme

**T3 Kimlik Yönetimi kabulünde.** O noktada 15 sürecin tamamı bir tam çevrim
tamamlamış olacak ve dereceler ilk kez gerçek veriyle ölçülebilecektir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-18 | 0.1 | İlk süreç değerlendirmesi — A0 ve A1 dönemi; 15 süreç, 11 bulgu | Bilgi İşlem |
