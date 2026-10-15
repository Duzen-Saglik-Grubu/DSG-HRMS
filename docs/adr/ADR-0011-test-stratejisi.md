# ADR-0011 — Test Stratejisi ve Kalite Kapıları

**Durum:** Kabul Edildi; §7 Veri sütunu → ADR-0018
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-024`, `KR-111` (§6 tanımı)
**İlgili süreç:** TEC.9 (Doğrulama), MAN.8 (Kalite Güvence)

---

## Bağlam

Kurumun gereksinimi: *"Hem frontend hem backend tarafında her modül için mutlaka unit
test ve integration testlerimiz olacak."*

Bu hedef doğrudur, ancak "her şeyi test et" olarak uygulanırsa iki bilinen sonuç doğar:
ya anlamsız testler yazılır (kapsam yüzdesi için `getter` testleri), ya da hedef
karşılanamaz ve zamanla terk edilir.

Ayrıca TEC.9 (Doğrulama) Seviye 2 kapsamındadır: denetimde sorulan soru "test yaptınız
mı" değil, **"tanımlı bir yaklaşımınız ve uygulanan bir eşiğiniz var mı"** olacaktır.

## Karar

### 1. Test piramidi ve hedefler

| Katman | Kapsam | Hedef | Araç |
|---|---|---|---|
| **Domain birim testi** | İş kuralları: izin hakedişi, kıdem, çalışma takvimi, döngü kontrolü, tarih aralığı çakışması | **≥ %90 satır kapsamı** | xUnit + Shouldly |
| **Application birim testi** | Kullanım senaryoları, doğrulama, yetki kararları | ≥ %80 | xUnit + NSubstitute |
| **API entegrasyon testi** | Uç noktalar, gerçek veritabanı, kimlik doğrulama, yetki | Her modülün ana akışları | `WebApplicationFactory` + **Testcontainers** |
| **Frontend birim testi** | Bileşenler, kancalar (hooks), form doğrulama | ≥ %70 | Vitest + React Testing Library |
| **Uçtan uca test** | Kritik kullanıcı akışları | 5–8 senaryo | Playwright |

**Genel kapsam eşiği: %75.** Domain katmanı için ayrıca %90 eşiği uygulanır.

> Kapsam yüzdesi bir **taban**tır, hedef değil. Yüksek kapsamlı ama zayıf test,
> düşük kapsamlı iyi testten kötüdür. Kod gözden geçirmede testin *neyi* doğruladığına
> bakılır.

### 2. Zorunlu test türleri — her modülde

Bir modül, aşağıdakiler yazılmadan "tamamlandı" sayılmaz (MAN.8 Definition of Done):

1. **İş kuralı birim testleri** — sınır durumlar ve hata durumları dâhil.
2. **API entegrasyon testleri** — ana akış + doğrulama hataları.
3. **Yetki sızıntısı testi** — kapsam dışı bir kaydın kimliğiyle yapılan istek `404`
   dönmelidir (ADR-0007 §3). **Bu test atlanamaz.**
4. **Maskeleme testi** — modülün ürettiği log ve denetim kayıtlarında kişisel veri
   düz metin olarak bulunmamalıdır (ADR-0009 §4).
5. **Frontend bileşen testleri** — form doğrulama ve hata gösterimi.

### 3. Entegrasyon testlerinde gerçek veritabanı

**EF Core In-Memory sağlayıcısı kullanılmayacaktır.** Gerekçe: gerçek veritabanı
davranışını taklit etmez — kısıtları, `EXCLUDE` kurallarını, eşzamanlılık denetimini,
Türkçe sıralamayı ve işlem semantiğini uygulamaz. Geçen bir test, üretimde başarısız
olan koda güven verir.

Bunun yerine **Testcontainers** ile her test koşumunda gerçek bir PostgreSQL
konteyneri ayağa kaldırılır. Migration'lar da bu veritabanı üzerinde çalışır; böylece
**migration'ların kendisi de test edilmiş olur**.

### 4. Dış sistemler test edilirken

| Sistem | Test yaklaşımı |
|---|---|
| **LOGO / MSSQL** | Sahte (fake) `ILogoPersonnelSource` uygulaması. Ayrıca **şema sapma testi** ayrı bir kategori olarak, gerçek LOGO'ya karşı ve **yalnızca okuma** yaparak çalışır |
| **NetGSM (SMS)** | Sahte HTTP yanıtları; tüm hata kodları kapsanır. **Gerçek SMS gönderilmez** |
| **SMTP** | Sahte gönderici; gerçek e-posta gönderilmez |
| **NAS / dosya** | Geçici dizin üzerinde çalışan uygulama |
| **ClamAV** | Test konteyneri veya sahte tarayıcı |

**Kural:** Hiçbir otomatik test gerçek bir personele SMS veya e-posta göndermez.

### 5. Test verisi

- Testlerde **gerçek personel verisi kullanılmaz**; üretici (builder) desenleriyle
  sentetik veri üretilir.
- Testler birbirinden bağımsızdır ve **sırasız** çalışabilir; ortak duruma dayanmaz.
- Her test kendi verisini kurar ve temizler.

### 6. Kalite kapıları (CI)

**Tanım (15.10.2026, `KR-111`):** Kalite kapısı, PR'da veya `main`'de çalışan, başarısızlığı
birleştirmeyi durduran ya da `[HATA]`/`[DÜZELTİCİ]` issue'su açan otomatik denetim ve
`KR-096` onay yorumudur. Kapıların **tek listesi bu bölümdür**; başka belgeler kapı sayısı
vermez, buraya atıf yapar (18.09 gözden geçirmesi BULGU-10: üç belge 8, 11 ve 12 kapı
sayıyordu).

> **Sunucu tarafı zorlama yoktur.** GitHub Free planında özel depoda dal koruma kuralı
> tanımlanamaz (`KR-054`, `KR-055`; risk `R-16` kabul edildi). "Birleştirmeyi durdurur",
> sunucunun uyguladığı bir engel değil, **uzlaşmadır**: kırmızı kapısı olan PR birleştirilmez
> (CONTRIBUTING §5). Uzlaşmanın tutulduğu sonradan, tespit edici olarak denetlenir (B
> tablosu) ve atlatılan kapı issue olarak kayda geçer. Kırmızıyla birleştirilen PR sayısı
> kalite güvence ölçütü K5'tir (MAN.8 YAKLAŞIM §2.5).

**A. PR'da çalışanlar — başarısızlık birleştirmeyi durdurur.** `ci.yml` ve
`api-contract-check.yml` `main`'e gelen her commit'te de çalışır.

| # | Kapı | Kural | Yer |
|---|---|---|---|
| A1 | Derleme ve statik analiz | Uyarı yok (`TreatWarningsAsErrors`, .NET analiz kuralları); NuGet güvenlik denetimi derlemede | `ci.yml` · `build-and-test` |
| A2 | Biçim (backend) | `dotnet format` temiz | `ci.yml` · `build-and-test` |
| A3 | Backend testleri | Birim, entegrasyon ve mimari testlerinin tamamı geçer; mimari testi katman ve modül sınırlarını denetler (ADR-0002, ADR-0016) | `ci.yml` · `build-and-test` |
| A4 | Kapsam eşiği (backend) | Genel %75, Domain %90 | `ci.yml` · `build-and-test` |
| A5 | Ön yüz bağımlılık güvenliği | `npm audit`: yüksek ve üstü açık yok | `ci.yml` · `frontend` |
| A6 | Biçim (ön yüz) | Prettier temiz | `ci.yml` · `frontend` |
| A7 | Kurumsal logo tek kaynak | Web imajındaki logo depodaki asıl dosyayla aynı | `ci.yml` · `frontend` |
| A8 | Lint | ESLint; uyarı da kabul edilmez (`--max-warnings 0`) | `ci.yml` · `frontend` |
| A9 | Tip denetimi | TypeScript hatasız | `ci.yml` · `frontend` |
| A10 | Ön yüz testleri ve kapsam | Tamamı geçer; kapsam ≥ %70 | `ci.yml` · `frontend` |
| A11 | Ön yüz derleme | Üretim derlemesi başarılı | `ci.yml` · `frontend` |
| A12 | Üretilen API tipleri | OpenAPI belgesinden yeniden üretilen tipler depodakiyle aynı | `ci.yml` · `frontend` |
| A13 | Migration kontrolü | Bekleyen model değişikliği yok | `ci.yml` · `migration-check` |
| A14 | Bağımlılık güvenliği (backend) | Geçişli bağımlılıklar dâhil açıklı paket yok | `ci.yml` · `dependency-security` |
| A15 | Konteyner taraması | `trivy`: API ve web imajında yaması yayımlanmış kritik veya yüksek açık yok | `ci.yml` · `container-scan` |
| A16 | Sır taraması | `gitleaks` temiz (ADR-0008 §3) | `ci.yml` · `secret-scan` |
| A17 | Uçtan uca testler | Playwright senaryolarının tamamı geçer (sentetik veri) | `ci.yml` · `e2e` |
| A18 | API sözleşmesi | Koddan üretilen OpenAPI belgesi depodakiyle aynı (ADR-0010 §1) | `api-contract-check.yml` |
| A19 | Denetim kurallarının öz sınaması | Başlık, dal adı, değişiklik talebi onayı ve PR disiplini kuralları örnek girdilerle sınanır | `pr-traceability-check.yml` |
| A20 | Gereksinim izlenebilirliği | PG → SYG → test zinciri; SYG §8 tablosu ve izlenebilirlik matrisi SYG §4 ile aynı eşlemeyi gösterir | `pr-traceability-check.yml` · `requirement-traceability-check.mjs` |
| A21 | Belge tutarlılığı | "Son güncelleme" geçmişin son satırından eski değil; SYG §1 sayımları §4 ve §5 ile aynı | `pr-traceability-check.yml` · `doc-consistency-check.mjs` |
| A22 | PR başlığı | Conventional Commits (CONTRIBUTING §5.1) | `pr-traceability-check.yml` · Denetle |
| A23 | Milestone | PR'a milestone atanmış | `pr-traceability-check.yml` · Denetle |
| A24 | Issue bağlantısı | `Closes #` veya `Refs #` var ve doldurulmuş | `pr-traceability-check.yml` · Denetle |
| A25 | Dal adı | CONTRIBUTING §3 önekleri; bağlı issue numarası | `pr-traceability-check.yml` · Denetle |
| A26 | Değişiklik talebi onayı | Kapatılan issue değişiklik talebiyse tarihli paydaş onayı kayıtlı (TEC.2 YAKLAŞIM §4.1) | `pr-traceability-check.yml` · Denetle |
| A27 | Etiketler | PR'da `tur:` ve `surec:`; kapatılan issue'da `tur:` | `pr-traceability-check.yml` · Denetle |
| A28 | Atlanamaz tamamlanma maddeleri | "Yetki sızıntısı testi" ve "Maskeleme testi" işaretli ya da üstü çizili ve gerekçeli | `pr-traceability-check.yml` · Denetle |
| A29 | PR boyutu | Üretilen dosyalar hariç 1.500 satırı aşan PR'da `Boyut gerekçesi:` var | `pr-traceability-check.yml` · Denetle |
| A30 | Gereksinim belgesinde geçmiş satırı | PG, SYG veya izlenebilirlik matrisi değiştiyse o belgenin değişiklik geçmişine yeni satır eklendi (#235) | `pr-traceability-check.yml` · Denetle |

**B. `main`'de çalışanlar — tespit edici; başarısızlık issue açar.**

| # | Kapı | Neyi yakalar | Sonuç | Yer |
|---|---|---|---|---|
| B1 | Dal Koruma Denetimi | PR olmadan gelen commit; birleştirmeden önce onay yorumu olmayan PR; denetimin kendisinin çalışamaması | `[DÜZELTİCİ]` issue'su | `branch-protection-check.yml` (`main`'e her gönderim) |
| B2 | `main` kırmızı denetimi | `main`'de CI, API sözleşme denetimi veya Dal Koruma Denetiminin başarısız, iptal veya zaman aşımıyla bitmesi | `[HATA] main kırmızı` issue'su; kırmızıyken özellik birleştirilmez (CONTRIBUTING §1.1) | `main-failure-check.yml` |
| B3 | `main` sağlık taraması | `main`'in son commit'inin izlenen iş akışlarında doğrulanmaması; B2'nin kendisinin çalışmaması | `[HATA]` issue'su | `main-health-sweep.yml` (6 saatte bir) |

**C. İnsan kapısı.**

| # | Kapı | Kural |
|---|---|---|
| C1 | Kod gözden geçirme | En az bir onay. Tek kişilik düzende inceleyen, birleştirmeden önce PR'a onay yorumu yazar; B1 bunu `main`'de denetler (`KR-096`) |

Kapılar **uyarı değil, engeldir.** Geçici olarak devre dışı bırakılması gerekirse
gerekçesi PR'da yazılır ve `duzeltici-faaliyet` etiketli bir issue açılır (MAN.8). Yeni
bir kapı eklendiğinde veya kaldırıldığında bu liste aynı PR'da güncellenir.

### 7. Ortamlar

| Ortam | Amaç | Veri |
|---|---|---|
| **Geliştirme** | Günlük geliştirme | ~~Sentetik~~ Gerçek (→ ADR-0018) |
| **UAT** | İK ile kabul testi (`KR-024`) | ~~Gerçek veriden maskelenmiş kopya~~ Gerçek, maskelenmez (→ ADR-0018) |
| **Üretim** | Canlı kullanım | Gerçek |

> **Yerini aldı (14.10.2026, ADR-0018, `KR-108`):** Yerel geliştirme ortamı ve UAT gerçek
> personel verisiyle çalışır; CI ve otomatik testler sentetik veriyle çalışmaya devam eder.
> Gerekçe, korumalar ve kabul edilen risk (R-25) ADR-0018'dedir.

UAT ayrı bir Docker Compose yığını ve ayrı veritabanıdır. Yalnızca **etiketlenmiş
sürümler** UAT'ye gider; kabul formu o sürüm numarasına yazılır (TEC.11).

### 8. Kabul testi (UAT) ile doğrulamanın ayrımı

| | Doğrulama (TEC.9) | Geçerleme (TEC.11) |
|---|---|---|
| Sorusu | "Ürünü doğru mu yaptık?" | "Doğru ürünü mü yaptık?" |
| Kim yapar | Geliştirme / CI | **İK birimi** |
| Nerede | Geliştirme ortamı, CI | UAT ortamı |
| Kanıt | Test ve kapsam raporları | İmzalı kabul formu |

İkisi ayrı süreçtir ve ayrı kanıt üretir; biri diğerinin yerine geçmez.

## Gerekçe

- Katman bazlı farklı eşikler, çabayı en çok hata çıkan yere (iş kuralları) yönlendirir.
- İzin hakediş hesabı, mevzuata bağlı ve sınır durumları çok olan bir alandır; %90 eşiği
  buraya bilinçli olarak konmuştur.
- Yetki sızıntısı ve maskeleme testlerinin zorunlu kılınması, KVKK risklerini (`R-03`)
  yapısal olarak kontrol altına alır.
- Testcontainers, "testte geçti üretimde patladı" sınıfı hataları ortadan kaldırır.
- Kalite kapılarının engelleyici olması, 33061 PA 2.1 (c) — *"performans izlenir,
  ölçülür ve gerektiğinde düzeltilir"* — maddesinin somut kanıtıdır.

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| %100 kapsam hedefi | Anlamsız testler üretir veya terk edilir |
| Kapsam eşiği koymamak | Ölçülemeyen kalite; 33061 açısından kanıt yok |
| EF Core In-Memory | Gerçek davranışı taklit etmiyor, yanlış güven veriyor |
| Yalnızca uçtan uca test | Yavaş, kırılgan, hata yerini göstermez |
| Kalite kapılarını uyarı seviyesinde tutmak | Zamanla göz ardı edilir |

## Sonuçlar

**Olumlu:** Ölçülebilir kalite; regresyona karşı koruma; denetimde gösterilebilir kanıt.

**Olumsuz / kabul edilen ödünler:**
- Testcontainers, CI koşum süresini uzatır (her koşumda konteyner ayağa kalkar).
  Kabul edilebilir bir maliyettir; gerekirse test veritabanı yeniden kullanılarak
  optimize edilir.
- Kalite kapıları bazen acil bir düzeltmeyi yavaşlatır. Bu bilinçli bir ödündür.

**Yükümlülükler:**
- Test stratejisi `docs/33061/TEC.9-dogrulama/YAKLASIM.md` içinde de özetlenecektir.
- Kapsam raporları her sürümde `TEC.9/raporlar/` altında saklanacaktır.

## Geri dönüş maliyeti

**Düşük.** Eşikler ve kapılar yapılandırmadır; ayarlanabilir. Test altyapısından
dönmek ise kalite kaybı demektir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
| 2026-09-09 | 0.2 | İddia kütüphanesi FluentAssertions yerine Shouldly (`KR-057`) | Bilgi İşlem |
| 2026-10-04 | 0.3 | §6 kod gözden geçirme kapısının tek kişilik düzende kanıtı (`KR-096`, #128) | Bilgi İşlem |
| 2026-10-14 | 0.4 | §7: ortamların Veri sütunu ADR-0018'e taşındı (`KR-108`, `KR-099`, #212) | Bilgi İşlem |
| 2026-10-15 | 0.5 | §6: kalite kapısı tanımı ve iş akışlarındaki gerçek kapıların tek listesi; sunucu tarafı zorlamanın olmadığı yazıldı (`KR-111`, `KR-099`, BULGU-10, #217) | Bilgi İşlem |
| 2026-10-15 | 0.6 | §6: A30 gereksinim belgesinde değişiklik geçmişi satırı (#235) | Bilgi İşlem |
