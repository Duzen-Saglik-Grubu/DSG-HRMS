# ADR-0011 — Test Stratejisi ve Kalite Kapıları

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-024`
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

Pull Request, aşağıdakilerin tamamı geçmeden birleştirilemez:

| Kapı | Kural |
|---|---|
| Derleme | Uyarı yok (`TreatWarningsAsErrors`) |
| Birim + entegrasyon testleri | Tamamı geçmeli |
| Kapsam eşiği | Genel %75, Domain %90 |
| Mimari testi | Katman ve modül bağımlılık kuralları (ADR-0002 §"Bağımlılık kuralları") |
| Statik analiz | .NET analiz kuralları + ESLint; hata seviyesinde bulgu yok |
| Sır taraması | `gitleaks` temiz (ADR-0008 §3) |
| Bağımlılık güvenliği | Bilinen kritik açıklı paket yok |
| Konteyner taraması | `trivy` — kritik açık yok |
| Migration kontrolü | Bekleyen model değişikliği yok |
| Biçim | `dotnet format` ve Prettier temiz |
| Kod gözden geçirme | En az bir onay. Tek kişilik düzende PR'a birleştirmeden önce yazılan onay yorumu; `main`'de denetlenir (`KR-096`) |

Kapılar **uyarı değil, engeldir.** Geçici olarak devre dışı bırakılması gerekirse
gerekçesi PR'da yazılır ve `duzeltici-faaliyet` etiketli bir issue açılır (MAN.8).

### 7. Ortamlar

| Ortam | Amaç | Veri |
|---|---|---|
| **Geliştirme** | Günlük geliştirme | Sentetik |
| **UAT** | İK ile kabul testi (`KR-024`) | Gerçek veriden **maskelenmiş** kopya |
| **Üretim** | Canlı kullanım | Gerçek |

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
