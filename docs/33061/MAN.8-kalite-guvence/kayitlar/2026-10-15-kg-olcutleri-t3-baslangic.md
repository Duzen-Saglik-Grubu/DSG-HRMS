# Kalite Güvence Ölçütleri — T3 Başlangıç Değerleri

**Belge kimliği:** MAN.8-KYT-2026-10-15-KGO
**Süreç:** MAN.8 — Kalite Güvence (`PA 2.1 a`: performans hedefleri; `PA 2.1 d`: izleme)
**Son güncelleme:** 2026-10-15
**Dönem:** T3 Kimlik Yönetimi — 23.09.2026 00:00 … 13.10.2026 23:59 (UTC+3); gereksinim toplantısından kabule
**Ölçüt tanımları:** `../YAKLASIM.md` §2.5 (K1–K6)
**Hazırlayan:** Bilgi İşlem

## 1. Amaç

K1–K6, 15.10.2026'da tanımlandı (`KR-111`, #217). Bu kayıt, T3 dönemi için ilk değerleri
verir; sonraki durum raporları ve modül sonu denetimleri bu değerlerle karşılaştırılır.
Değerler, 13.10 denetimine hazırlık triyajındaki tahminlerden **kopyalanmadı**; 15.10.2026'da
salt okuma ile yeniden hesaplandı.

## 2. Yöntem

- **Kaynak:** GitHub (`gh issue list`, `gh pr list`, `gh run list`, `gh api`) ve depo; 15.10.2026
  ~11:00'de alındı. Hiçbir kayıt değiştirilmedi.
- **Pencere:** Zamanlar UTC olarak okunup UTC+3 pencereye göre süzüldü.
- **PR evreni:** Dönemde birleştirilen **73 PR** (#63 … #221).
- **CI evreni:** `ci.yml` (iş akışı adı `CI`). `main` çalışmaları `push` olayıyla gelir; dönemde
  **73 çalışma**, 73 farklı commit.

## 3. Değerler

| # | Ölçüt | T3 değeri | Hedef | Triyaj tahmini |
|---|---|---|---|---|
| K1 | Denetim bulgusu takip oranı | 02.10 Ek B önerileri: **15/28 (%54)**. 02.10 iş planı: **18/18**. 13.10 bulguları: **12/12** | %100 | 15/28 |
| K2 | Denetim issue'su kapanma süresi | 02.10 denetimi (#119–#136, 18 issue): medyan **2,23 gün**, en uzun **7,15 gün** (#127); 18/18 kapandı | Medyan ≤ 7 gün | ~2,2 / ~7,1 gün |
| K3 | `main` CI başarı oranı | **66/73 (%90,4)**; **4 kırmızı olay**, yeşile dönüş 13,0–21,3 saat | ≥ %90; ≤ 24 saat; her olay kayıtlı | 66/73; 4 olay |
| K4 | PR'da ilk çalıştırmada geçme | **68/73 (%93,2)** | ≥ %90 | 68/73 |
| K5 | Birleştirme disiplini | (a) Kırmızıyla birleştirilen PR: **0/73**. (b) Birleştirmeden önce onay yorumu: **38/38** (`KR-096` sonrası, #148 … #221) | 0; %100 | 0; 48/48 |
| K6 | Kaçan hata | **5** (#102, #103, #137, #138, #198); kabul sonrası: **0** (15.10 itibarıyla) | Kabul sonrası 30 günde 0 | 3 |

### 3.1 K1 — Denetim bulgusu takip oranı

- **02.10 Ek B önerileri (28):** MAN.8-1…7, TEC.2-1…4, TEC.3-1…5, TEC.5-1…6, TEC.7-1…6. Her
  önerinin 13.10 durumu, 13.10 denetimi Ek B'nin "02.10 önerilerinin durumu" tablolarından
  okundu. Kapanan (✅) veya açık kalan her parçası issue'ya bağlı (⚠️) öneri "takipte"
  sayıldı; açık parçası issue'suz olan öneri (❌ veya "issue yok") takipsiz sayıldı.
  Takipsiz 13 öneri: MAN.8-4, 5, 7; TEC.2-2, 3; TEC.3-1, 3, 5; TEC.5-3, 4, 5, 6; TEC.7-6.
  MAN.8-3'ün açık kalan parçası BULGU-10'du; ayrıca takipsizdi ve 13.10 denetimi onu ayrı
  saydı (M-10).
- **15.10 sonrası (bilgi):** 13 önerinin tamamı #217 ile ele alındı: issue (#234, #235),
  gerekçeli kapatma (`KR-111`) veya #217'nin kendisinde yapılan iş. Böylece 28/28.
- **02.10 iş planı (18 iş):** Raporun §6'sındaki her satır, 03.10'da gövdesinde
  `Denetim maddesi N` satırı olan bir issue oldu (#119 … #136).
- **13.10 bulguları (M-01 … M-12):** 12 bulgu 13 işe dönüştü; 13 issue (#207 … #219) rapor
  PR'ı (#220) birleşmeden önce açıldı.
- **Komut:** `gh issue list --state all --search 'in:body "Denetim maddesi"' --json number`
  31 issue döndürür: #119 … #136 ve #207 … #219.

### 3.2 K2 — Denetim issue'su kapanma süresi

- **Seçim:** Gövdesinde `Denetim maddesi` geçen issue'lar (K1'deki komut). Süre `closedAt − createdAt`.
- **02.10 denetimi (#119 … #136):** 18/18 kapandı. En kısa 0,15 gün (#119), medyan 2,23 gün,
  en uzun 7,15 gün (#127, ilk durum raporu ve plan onayı; Üst Yönetim onayını bekledi).
- **13.10 denetimi (#207 … #219, bilgi, dönem dışı):** 15.10 itibarıyla 10/13 kapandı; medyan
  0,72 gün, en uzun 1,54 gün. Açık: #217, #218, #219.

### 3.3 K3 — `main` CI başarı oranı ve kırmızı olaylar

**Komut:** `gh run list --workflow ci.yml --branch main --limit 1000 --json conclusion,createdAt,updatedAt,headSha,event`.
Dönemde 73 çalışmanın 66'sı başarılı, 7'si başarısız. Ardışık başarısızlar bir olay sayıldı.

| Olay | İlk başarısız çalışma (UTC) | Neden (başarısız adım) | Yeşile dönüş | Süre | Kayıt |
|---|---|---|---|---|---|
| 26.09 | 21:16, `2b667da` | Domain kapsamı %87,6 < %90 | `b8be775`, 27.09 15:48 | 18,5 saat | #89 (27.09, kapsam ölçümü kararsızlığı) |
| 28.09 | 19:16, `387c4d2` | `SessionApiTests.Activity_keeps_the_session_alive…` | `a593884`, 29.09 08:14 | 13,0 saat | #97 (aynı gün, aynı test) |
| 30.09 | 09:54, `ade18ff` | `trivy` (API imajı) | `ef9975f`, 01.10 07:09 | 21,3 saat | Geriye dönük: olay kaydı O-2 (04.10, #129) |
| 05.10 | 20:19, `2592fab` (+3 commit) | `trivy` başarısız, diğer işler iptal | `e8dfc87`, 06.10 10:01 | 13,7 saat | Geriye dönük: olay kaydı O-5 (13.10, #208) |

- **Yeşile dönüş:** İlk başarısız çalışmanın başlangıcından ilk başarılı çalışmanın bitişine.
  Dört olayın dördü de 24 saatin altında.
- **Kayıt:** Olay anında ilişkili bir issue'su olan: 2/4. Diğer ikisi geriye dönük kaydedildi.
  Otomatik `[HATA] main kırmızı` kuralı 05.10'da yürürlüğe girdi (#129). O gün kuralın kendisi
  çalışmadı (#208).

### 3.4 K4 — PR'da ilk çalıştırmada geçme

- **Yöntem:** Her PR için PR'ın dalında (`headRefName`), PR'ın açılışından en çok 1 saat önce
  ile birleştirme anı arasında başlayan `ci.yml` `pull_request` çalışmaları alındı. En erken
  çalışmanın sonucuna bakıldı. Yeniden çalıştırılmış 4 çalışmanın ilk denemesi
  `gh api …/actions/runs/<id>/attempts/1` ile ayrıca okundu; hiçbiri sonucu değiştirmedi.
- **Sonuç:** 68/73. İlk çalışması başarısız olanlar: #96, #108, #141, #173, #180. Beşinde de
  ikinci çalışma başarılıydı.

### 3.5 K5 — Birleştirme disiplini

- **(a) Kırmızıyla birleştirme:** Her PR'ın birleştirmeden önce başlayan son `ci.yml`
  çalışması hem başarılı hem de birleştirmeden önce bitmiş: **73/73**. Kırmızıyla birleştirilen
  PR: **0**.
- **(b) Onay yorumu:** Her PR'ın yorumları `gh api repos/{owner}/{repo}/issues/<no>/comments`
  ile okundu. Kural Dal Koruma Denetimiyle aynıdır: birleştirmeden önce yazılmış ve
  `onaylıyorum` içeren yorum.
  - `KR-096`'dan sonra (#148 … #221): **38/38**. Yazar: inceleyen hesap.
  - `KR-096`'dan önceki 35 PR'da (#63 … #147) PR yorumu yok. Bu 35 PR'ın tamamı geriye
    dönük onay kaydında var (`2026-10-04-pr-onay-kaydi.md`).
- **Triyajla fark:** Triyajdaki 48/48, #148'den 15.10'a kadar sayılmıştı. Dönem sonrasındaki
  10 PR (#222 … #232) de birleştirmeden önce onaylı: 38 + 10 = 48.

### 3.6 K6 — Kaçan hata

- **Kaynak:** `gh issue list --state all --label tur:hata`. Dönemde açılan 14 `tur:hata`
  issue'sunun gövdesi okundu ve sınıflandı.

| Issue | Bulunduğu yer | Sınıf |
|---|---|---|
| #102 | UAT ve yerel kullanım, Bilgi İşlem bildirimi (29.09): yenilemede oturum kapanıyor | **Kaçan** |
| #103 | UAT denemesi, Bilgi İşlem bildirimi (29.09): sunucu saati geride, kod ekranı | **Kaçan** |
| #137 | UAT denemesi, Doğuş Uçanok (03.10): davet bağlantısı geçersiz görünüyor | **Kaçan** |
| #138 | UAT denemesi, Doğuş Uçanok (03.10): yönetici kendi hesabını pasife alabiliyor | **Kaçan** |
| #198 | Kabul öncesi yerel deneme, Doğuş Uçanok (12.10): T5 parametresi T3 ekranında | **Kaçan** |
| #93 | PR #92'nin testi | Birleşmeden önce yakalandı |
| #97 | CI testi (PR #96 ve `main`) | CI yakaladı |
| #120 | Planlı UAT performans ölçümü (TEC.9) | Doğrulama yakaladı |
| #79 | UAT dağıtımı | Dağıtım betiği; ürün hatası değil |
| #89, #182 | CI | Test ve ölçüm altyapısı |
| #149, #175 | CI bağımlılık taraması | Bağımlılık açığı |
| #171 | Mimari testi incelemesi | Test altyapısı |

- **Sonuç:** **5** kaçan hata. Hepsi düzeltildi ve kapandı.
- **"Neden yakalanmadı?":** Beş issue'nun gövdesinde ve yorumlarında bu soru ayrı bir başlıkla
  yanıtlanmamış. Gövde ve yorumlarda "yakalan", "görülmedi", "kök neden" aranarak sınandı.
  #138'in gövdesinde neden de yazılı değil. Önlem bazı durumlarda sonraki işlerde geldi:
  - #137 → tarayıcı akışları için uçtan uca testler (#146, PR #147);
  - #138 ve #198 → onaylı SYG'de olmayan davranış için geri yön denetimi (#211, PR #226).

  K6 hedefinin bu yarısı T3'te **karşılanmadı**. Bundan sonra issue'da aranır.
- **Kabul sonrası 30 gün (13.10 … 12.11):** 15.10 itibarıyla **0**. Dönemden sonra açılan tek
  `tur:hata` issue'su #223'tür; o da `main` CI kırmızısıdır, ürün hatası değildir.
- **Triyajla fark:** Triyaj yalnızca 02.10 denetiminden sonra bulunanları saydı (#137, #138,
  #198). 29.09'daki ilk UAT denemesinde Bilgi İşlem'in bulduğu #102 ve #103 de tanıma uyar.
  Bu yüzden değer 5'tir.

## 4. Değerlendirme

- **Hedefi tutturan:** K2, K3 (oran ve süre), K4, K5.
- **Hedefin altında kalan:**
  - **K1:** Ek B önerilerinin %54'ü 13.10'a kadar takipsiz kaldı. Düzeltme #217'dir.
  - **K3 kayıt:** Dört olayın ikisi olay anında kayda geçmedi. Düzeltme #129 ve #208'dir.
- **K6:** Kaçan beş hatanın hiçbirinin issue'sunda "neden yakalanmadı" yanıtı yok. Kabul
  sonrası hedef henüz ölçülemez; ilk kesin değer 12.11.2026'dan sonraki durum raporundadır.
- **Sonraki ölçüm:** Bir sonraki durum raporu (en geç 13.11.2026, `KR-101`) §3'te K1–K6'yı
  dönem değerleriyle verir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-15 | 1.0 | İlk oluşturma (`KR-111`, #217) | Bilgi İşlem |
