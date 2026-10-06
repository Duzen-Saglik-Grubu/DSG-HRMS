# İş Kırılım Yapısı (WBS)

**Belge kimliği:** MAN.1-WBS
**Süreç:** MAN.1 — Proje Planlama
**Son güncelleme:** 2026-09-17
**33061 karşılığı:** *Work breakdown structure*

> **Kullanım:** Bu belge işin **yapısını** tanımlar; **durumunu** değil. Görevlerin
> güncel durumu GitHub Projects panosunda (Backlog / In Progress / Review / Done)
> izlenir. Bu belge, panonun neye göre kurulduğunu gösterir.

---

## 1. Numaralandırma

```
<Aşama>.<Grup>.<Kalem>          örnek: 3.Y5.4
```

Modül kimlikleri `docs/mimari/modul-listesi-ve-bagimliliklar.md` ile aynıdır
(T1–T5, Y1–Y10, İ1–İ20).

---

## 2. Üst düzey kırılım

```
DSG-HRMS
├── 1. Hazırlık ve Planlama          (A0)
├── 2. Teknik İskelet                (A1)
├── 3. Temel Modüller                (A2)
├── 4. Yatay Altyapı Modülleri       (A3)
├── 5. İş Modülleri                  (A4+)
├── 6. Veri Göçü ve Geçiş            (AS)
└── 7. Süreç Yönetimi ve Kanıt       (sürekli)
```

---

## 3. Kırılım ayrıntısı

> ### Her modülün başlangıç koşulu
>
> Aşağıdaki tablolar modüllerin **ne** içerdiğini gösterir; **ne zaman
> başlanabileceğini** değil. Bir modülün geliştirmesine başlanabilmesi için
> **İK ile o modüle özel gereksinim toplantısı yapılmış ve gereksinimler İK
> onayından geçmiş olmalıdır** (`KR-068`).
>
> Teknik ön koşulun (bir modülün başka bir modüle bağımlılığı) karşılanmış olması
> **yeterli değildir.** T1'in teknik bir ön koşulu yoktur; yine de kendi gereksinim
> toplantısı yapılmadan başlanmaz.
>
> **Hazırlık bunun dışındadır.** Geçmiş notlardan gereksinim taslağı üretmek, veri
> kalitesini ölçmek, açık soruları çıkarmak serbesttir ve teşvik edilir — toplantı
> bu girdilerle çok daha verimli geçer. Taslak, **toplantı girdisidir**; onaylanmış
> gereksinim değildir.

### 1. Hazırlık ve Planlama (A0)

| Kod | İş | Durum |
|---|---|---|
| 1.1 | Mevcut sistem ve veri envanteri analizi | ✅ Tamamlandı |
| 1.2 | LOGO erişimi ve salt-okunur yetki doğrulaması | ✅ Tamamlandı |
| 1.3 | Standartların temini ve olgunluk kriterlerinin çıkarılması | ✅ Tamamlandı |
| 1.4 | Doküman altyapısı ve 33061 kanıt klasörleri | ✅ Tamamlandı |
| 1.5 | Mimari kararlar (15 ADR) | ✅ Tamamlandı |
| 1.6 | Vizyon ve kapsam belgesi | ✅ Tamamlandı (onay bekliyor) |
| 1.7 | Modül listesi ve bağımlılık haritası | ✅ Tamamlandı |
| 1.8 | Proje planı, WBS, RACI | 🔄 Devam ediyor |
| 1.9 | Süreç yaklaşım belgeleri (15 süreç × `YAKLASIM.md`) | ⏳ Bekliyor |
| 1.10 | Vizyon-kapsam ve planın onaylanması | ⏳ Bekliyor |

### 2. Teknik İskelet (A1)

| Kod | İş |
|---|---|
| 2.1 | Solution yapısı (Domain / Application / Infrastructure / Api + test projeleri) |
| 2.2 | Merkezî paket sürüm yönetimi ve paket envanteri |
| 2.3 | Frontend iskeleti (Vite + React + TypeScript + MUI + yönlendirme) |
| 2.4 | Veritabanı bağlantısı, ilk migration, adlandırma kuralları (ADR-0004) |
| 2.5 | Yapılandırma ve sır yönetimi, açılışta doğrulama (ADR-0008) |
| 2.6 | Loglama, denetim ara katmanı, maskeleme altyapısı (ADR-0009) |
| 2.7 | Hata yönetimi ve Problem Details ara katmanı (ADR-0010) |
| 2.8 | OpenAPI üretimi + frontend tip üretimi |
| 2.9 | Sağlık kontrolü uç noktaları ve OpenTelemetry |
| 2.10 | Docker imajları ve Compose yığınları (dev + UAT) |
| 2.11 | GitHub kurulumu: dal koruma, issue/PR şablonları, etiketler, Projects |
| 2.12 | CI hattı ve **12 kalite kapısı** (ADR-0011 §6) |
| 2.13 | Mimari testi (katman ve modül bağımlılık kuralları) |
| 2.14 | Testcontainers ile entegrasyon test altyapısı |
| 2.15 | Ortak frontend bileşenleri (DataTable, PageHeader, ErrorState …) |
| 2.16 | Geliştirici kurulum kılavuzu (`README.md`) |

### 3. Temel Modüller (A2)

Sırayla yapılır; önceliklendirmeye tabi değildir. **Her modül kendi İK gereksinim
toplantısıyla başlar** (`KR-068`) — T1 dâhil.

| Kod | Modül | Ana işler |
|---|---|---|
| 3.T1 | Personel Yönetimi | LOGO yalıtım katmanı · senkronizasyon servisi · kişi/istihdam modeli · şema sapma denetimi · veri kalitesi raporu |
| 3.T2 | Organizasyon Yönetimi | Firma/şube/birim · tarih aralıklı yapı · hiyerarşi ve yönetici ataması · döngü kontrolü |
| 3.T3 | **Kimlik Yönetimi (Üyelik + Giriş)** ★ | Üyelik akışı · doğrulama kodu · kanal seçimi ve değiştirme · parola politikası · JWT + yenileme jetonu · hız sınırlama |
| 3.T4 | Rol ve Yetki Yönetimi | İzin tanımları · rol yönetimi · satır bazlı kapsam · yetki sızıntısı testleri |
| 3.T5 | Kullanıcı Yönetimi | Hesap yaşam döngüsü · istisna hesap açma · pasifleştirme |

> ★ = kullanıcıya teslim edilen ilk modül. T1 ve T2 onunla birlikte inşa edilir.
> T3'ün çalışması için **Y1'in gönderim altyapısı** (e-posta + SMS) da bu aşamada gelir.

> **§5'teki standart alt kırılım temel modüller için de geçerlidir** — özellikle
> `.1 Gereksinim toplantısı ve kayıt (TEC.2)` adımı. Bu adım yalnızca iş modülleri
> altında yazılmıştı; temel modüllerde de aynen uygulanır (`KR-068`).

### 4. Yatay Altyapı Modülleri (A3)

`KR-053` gereği iş modüllerinden **önce** yapılır.

| Kod | Modül | Ana işler |
|---|---|---|
| 4.Y1 | Bildirim Merkezi | Kanal soyutlaması · NetGSM · SMTP · uygulama içi · gönderim kaydı · kuyruk |
| 4.Y2 | Denetim ve Erişim Kayıtları | Değişiklik izi · erişim kaydı · maskeleme · saklama parametreleri |
| 4.Y3 | Referans Veri Yönetimi | Tanım listeleri · parametre yönetimi · sürümleme |
| 4.Y4 | Sistem Yönetimi | Senkronizasyon yönetimi · sistem sağlığı · parametreler |
| 4.Y5 | **İş Akışı (Workflow)** | Akış tanımı · kademe · onay/ret · durum makinesi · bildirim entegrasyonu |
| 4.Y6 | Delegasyon Yönetimi | Vekâlet tanımı · tarih aralıklı · onay yetkisi devri |
| 4.Y7 | Çalışan Belge Yönetimi | `IFileStorage` · NAS · ClamAV · yükleme/indirme denetimleri |
| 4.Y8 | Raporlama ve Dışa Aktarma | ClosedXML · PDFsharp · ortak şablon · `export` izni · dışa aktarma kaydı |
| 4.Y9 | Dashboard | Gösterge altyapısı; veri üreten modüllerle birlikte büyür |
| 4.Y10 | **Çalışma Takvimi Yönetimi** | Resmî tatil · dinî bayram · mesai şablonu · cumartesi düzeni · **izin gün hesabı motoru** |

> **4.Y5 ve 4.Y10 kritik yoldadır.** İzin Yönetimi (5.İ1) ikisine birden bağlıdır ve
> İK'nın en çok talep edeceği modül büyük olasılıkla odur.

### 5. İş Modülleri (A4+)

İK önceliklendirmesine göre; her modül bir MINOR sürüm üretir.

| Kod | Modül | Göç |
|---|---|---|
| 5.İ1 | İzin Yönetimi | ✅ |
| 5.İ2 | Eğitim Yönetimi | ✅ |
| 5.İ3 | Sertifika Yönetimi | ✅ |
| 5.İ4 | Yetkinlik Yönetimi | — |
| 5.İ5 | Performans Yönetimi | — |
| 5.İ6 | Terfi Yönetimi | — |
| 5.İ7 | Pozisyon Değişikliği Yönetimi | — |
| 5.İ8 | İşe Alım Yönetimi | — |
| 5.İ9 | Aday Havuzu Yönetimi | — |
| 5.İ10 | Mülakat Yönetimi | — |
| 5.İ11 | Oryantasyon Yönetimi | — |
| 5.İ12 | Deneme Süresi Yönetimi | — |
| 5.İ13 | Disiplin Süreci Yönetimi | — |
| 5.İ14 | Ödül ve Takdir Yönetimi | — |
| 5.İ15 | Varlık Yönetimi (Zimmet) | — |
| 5.İ16 | Kurum İçi Duyuru Yönetimi | — |
| 5.İ17 | İşten Çıkış Süreci Yönetimi | — |
| 5.İ18 | İşten Çıkış Görüşmesi Yönetimi | — |
| 5.İ19 | Tebrik Servisi | — |
| 5.İ20 | İSG ve İş Kazası Yönetimi | — |

**Her iş modülünün standart alt kırılımı:**

| Alt kod | İş |
|---|---|
| `.1` | Gereksinim toplantısı ve kayıt (TEC.2) |
| `.2` | Paydaş gereksinimleri (`PG-<MODÜL>`) ve kabul kriterleri |
| `.3` | Mevcut sistem veri profillemesi (göç kapsamındaysa) |
| `.4` | Sistem gereksinimleri (`REQ-<MODÜL>-nn`) — TEC.3 |
| `.5` | Veri modeli ve migration |
| `.6` | Backend: domain, uygulama, API |
| `.7` | Frontend: ekranlar ve akışlar |
| `.8` | Testler: birim, entegrasyon, **yetki sızıntısı**, **maskeleme** |
| `.9` | Veri göçü betiği ve mutabakat raporu (göç kapsamındaysa) |
| `.10` | UAT dağıtımı ve kabul testi (TEC.11) |
| `.11` | Kabul formu ve kapanış gözden geçirmesi (MAN.2) |

> `.8` içindeki **yetki sızıntısı** ve **maskeleme** testleri atlanamaz
> (ADR-0011 §2). Bunlar Definition of Done'un parçasıdır.

### 6. Veri Göçü ve Geçiş (AS)

| Kod | İş |
|---|---|
| 6.1 | Göç aracı geliştirilmesi (tekrar çalıştırılabilir / idempotent) |
| 6.2 | Mutabakat raporu üreteci (adet, alan bazlı karşılaştırma, eşleşmeyenler) |
| 6.3 | **1. deneme — kuru koşu** |
| 6.4 | **2. deneme — doğrulama ve düzeltmeler** |
| 6.5 | **3. deneme — gerçek kesim provası** |
| 6.6 | Eski veritabanının salt-okunur arşiv kopyası |
| 6.7 | Üretim ortamı kurulumu ve dağıtım |
| 6.8 | Kesim (cut-over) planı ve **geri dönüş planı** |
| 6.9 | Kullanıcı bilgilendirmesi ve kısa kullanım kılavuzu |
| 6.10 | Devreye alma ve gözetim dönemi |
| 6.11 | Eski sistemin salt-okunur moda alınması |

> **6.3–6.5 pazarlık konusu değildir.** `R-01` (puan 9) projenin en yüksek riskidir
> ve kabul kriteri **sıfır açıklanamayan farktır**.

### 7. Süreç Yönetimi ve Kanıt (sürekli)

| Kod | İş | Sıklık |
|---|---|---|
| 7.1 | Risk gözden geçirme | Modül kapanışı / aylık |
| 7.2 | Durum raporu (MAN.2) | Dönemsel |
| 7.3 | Öz değerlendirme tablosu (33061) | Modül kapanışı / 3 aylık |
| 7.4 | Konfigürasyon denetimi (MAN.5) | Dönemsel |
| 7.5 | Kalite güvence raporu (MAN.8) | Dönemsel |
| 7.6 | İzlenebilirlik matrisinin güncellenmesi | Her modül |
| 7.7 | Karar kayıt defteri ve ADR bakımı | Sürekli |
| 7.8 | Bağımlılık ve güvenlik güncellemeleri | Sürekli |

---

## 4. Kritik yol

```
1. Hazırlık ✅
      ↓
2. İskelet ────────────────────────────────┐
      ↓                                     │
3.T3 Kimlik ★ (+ T1 çekirdeği, KR-077)      │  T3 önce geliştirildi:
      ↓                                     │  diğer modüller girişe
3.T1 Personel · 3.T2 Organizasyon           │  bağımlı (KR-040).
      ↓                                     │
3.T4 Rol/Yetki → 3.T5 Kullanıcı             │
      ↓                                     │
4.Y1 Bildirim · 4.Y2 Denetim · 4.Y3 Referans│
      ↓                                     │
4.Y5 İş Akışı ← ← ← ← ← ← ← ← ← ← ← ← ← ← ←┘  ← 8 iş modülünün ön koşulu
      ↓
4.Y10 Çalışma Takvimi
      ↓
5.İ1 İzin Yönetimi  ← İK'nın en çok isteyeceği modül
      ↓
6. Veri Göçü ve Geçiş
```

**Kritik yoldaki darboğazlar:**

| Kalem | Neden kritik |
|---|---|
| 2.12 CI kalite kapıları | Kurulmadan hiçbir modül "tamamlandı" sayılamaz |
| 3.T4 Rol ve Yetki | Tüm modüllerin yetki modeli buna dayanır |
| 4.Y5 İş Akışı | 8 iş modülünün ön koşulu |
| 4.Y10 Çalışma Takvimi | İzin Yönetimi'nin ön koşulu |
| 6.1 Göç aracı | `R-01`; erken başlanmalı, sona bırakılmamalı |

> **6.1 (göç aracı) için not:** Göç, aşama sırasında sonda görünür ancak aracın
> geliştirilmesi ve kuru koşuları **İK onayı beklenen aralıklarda** (P6 ilkesi)
> erkenden başlatılmalıdır. Sona bırakılırsa `R-01` kontrol edilemez hâle gelir.

---

## 5. GitHub Projects eşlemesi

| WBS düzeyi | GitHub karşılığı |
|---|---|
| Aşama (1–7) | Milestone — **yedisinin de karşılığı vardır** |
| Modül (T/Y/İ) | Issue etiketi (`modul:izin`, `modul:kimlik` …) + Milestone |
| Alt kalem (`.1`–`.11`) | Issue |
| Görev | Issue içindeki kontrol listesi veya alt issue |

### 5.1 Milestone karşılıkları

| WBS kalemi | Milestone |
|---|---|
| 1. Hazırlık ve Planlama | `A0 — Hazırlık ve Planlama` |
| 2. Teknik İskelet | `A1 — Teknik İskelet` |
| 3. Temel Modüller | `A2 — Temel Modüller` |
| 4. Yatay Altyapı Modülleri | `A3 — Yatay Altyapı` |
| 5. İş Modülleri | `A4+` (modül grupları hâlinde açılır) |
| 6. Veri Göçü ve Geçiş | `AS — Geçiş ve Devreye Alma` |
| 7. Süreç Yönetimi ve Kanıt (sürekli) | `SK — Süreç ve Kalite (sürekli)` |

### 5.2 Hangi milestone? — tek cümlelik kural

> **Bir issue veya PR, işin ait olduğu WBS kalemine karşılık gelen milestone'a atanır;
> işin ne zaman yapıldığına göre değil.**

Bu kural, aşağıdaki üç yanlışı birden engeller:

1. **Aşama kapandı diye işi bir sonraki aşamaya yazmak.** A1 teslim edilen iskeletteki
   bir kusur, A1 kapandıktan sonra düzeltilse bile **A1'e** aittir. Aksi hâlde A1
   olduğundan temiz, sonraki aşama olduğundan yüklü görünür. (Kapalı bir milestone'a
   atama yapmak için milestone geçici olarak açılır ve iş bitince yeniden kapatılır.)
2. **Süreç işlerini bir ürün aşamasına yazmak.** Düzeltici faaliyetler, gözden
   geçirmeler ve öz değerlendirme **7. kaleme** aittir; bir aşamaya değil.
3. **Etiket ile milestone'un çelişmesi.** Süreç etiketi `surec:TEC.10` (Geçiş) olan bir
   iş, `A2 — Temel Modüller` altında duramaz.

> **Neden önemli:** Milestone, aşama ilerlemesinin **ölçüldüğü** yerdir
> (`PA 2.1 (c)`). Yanlış atama, ölçüyü sessizce bozar: 18.09.2026 gözden geçirmesinde
> A2 "12 kapalı" görünüyordu, oysa **tek bir temel modül işi yapılmamıştı**
> (BULGU-08).

**Pano sütunları:** `Backlog` → `In Progress` → `Review` → `Done`

**Etiket şeması (öneri):**
`tur:gereksinim` · `tur:tasarim` · `tur:gelistirme` · `tur:test` · `tur:dokuman` ·
`tur:hata` · `tur:degisiklik-talebi` · `tur:duzeltici-faaliyet` ·
`oncelik:yuksek/orta/dusuk` · `modul:<ad>` · `surec:TEC.x/MAN.x`

> `surec:` etiketi, bir issue'nun hangi 33061 sürecine kanıt ürettiğini gösterir;
> izlenebilirlik matrisinin otomatik üretilmesini kolaylaştırır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-07 | 0.1 | İlk oluşturma — 7 aşama, 35 modül, kritik yol | Bilgi İşlem |
| 2026-09-17 | 0.2 | Modül başlangıç koşulu eklendi: her modül kendi İK gereksinim toplantısıyla başlar (`KR-068`) | Bilgi İşlem |
| 2026-09-18 | 0.3 | §5 milestone eşlemesi tamamlandı ve atama kuralı yazıldı; WBS 7 için `SK` milestone'u açıldı (BULGU-08) | Bilgi İşlem |
| 2026-10-06 | 0.4 | §4 kritik yol gerçekleşen sıraya göre: T3 önce, T1 çekirdeği T3 içinde (`KR-040`, `KR-077`, #127) | Bilgi İşlem |
