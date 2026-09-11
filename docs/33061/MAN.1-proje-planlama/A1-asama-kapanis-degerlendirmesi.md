# A1 — Teknik İskelet · Aşama Kapanış Değerlendirmesi

**Belge kimliği:** MAN-004
**Son güncelleme:** 2026-09-11
**İlgili süreçler:** MAN.1 (Proje Planlama), MAN.2 (Proje Değerlendirme ve Kontrol), MAN.8 (Kalite Güvence)
**Durum:** Bilgi İşlem değerlendirmesi — üst yönetim bilgisine sunulacak

> Bir aşama "bitti" demek, kanıtı gösterilmeden yapılamaz. Bu belge A1 aşamasının
> **ne ürettiğini, neyi kanıtladığını ve neyi devrettiğini** kayıt altına alır.

---

## 1. Özet

| | |
|---|---|
| **Aşama** | A1 — Teknik İskelet |
| **Amaç** | İş modüllerinin üzerine kurulacağı teknik tabanın hazırlanması |
| **WBS kalemleri** | 16 / 16 tamamlandı |
| **Birleştirilen PR** | 16 |
| **Üretilen karar** | `KR-057` … `KR-065` (9 yeni karar) |
| **Otomatik test** | 168 (116 backend, 52 frontend) |
| **CI kalite kapısı** | 8 kapı, tamamı etkin ve geçiyor |

**Sonuç: A1 tamamlanmıştır.** İlk iş modülü geliştirmesi için teknik engel
kalmamıştır.

---

## 2. WBS kalemleri ve kanıtları

| WBS | Kalem | Durum | Kanıt |
|---|---|---|---|
| 2.1 | Solution yapısı ve katman projeleri | ✅ | PR #6 |
| 2.2 | Merkezî paket sürüm yönetimi ve envanter | ✅ | PR #6 · `docs/mimari/paket-envanteri.md` |
| 2.3 | Frontend iskeleti | ✅ | PR #26 |
| 2.4 | Veritabanı altyapısı ve ilk migration | ✅ | PR #8 |
| 2.5 | Yapılandırma ve sır yönetimi | ✅ | PR #8 |
| 2.6 | Loglama, denetim izi, erişim kaydı, maskeleme | ✅ | PR #10, #12, #14 |
| 2.7 | Hata yönetimi (Problem Details) | ✅ | PR #16 |
| 2.8 | OpenAPI üretimi ve frontend tip üretimi | ✅ | PR #20, #26 |
| 2.9 | Sağlık kontrolleri ve OpenTelemetry | ✅ | PR #22 |
| 2.10 | Docker imajları ve Compose yığınları | ✅ | PR #30 |
| 2.11 | GitHub kurulumu ve akış | ✅ | PR #2, #18 |
| 2.12 | CI hattı ve kalite kapıları | ✅ | PR #24, #30 |
| 2.13 | Mimari testleri | ✅ | PR #6 |
| 2.14 | Testcontainers ile entegrasyon test altyapısı | ✅ | PR #8 |
| 2.15 | Ortak frontend bileşenleri | ✅ | PR #32 |
| 2.16 | Geliştirici kurulum kılavuzu | ✅ | PR #30 (`README.md`) |

---

## 3. Kalite kapılarının durumu (ADR-0011 §6)

| Kapı | Durum | Not |
|---|---|---|
| Derleme — uyarı yok | ✅ Etkin | `TreatWarningsAsErrors` |
| Statik analiz | ✅ Etkin | .NET analiz kuralları + ESLint (uyarıya izin yok) |
| Biçim | ✅ Etkin | `dotnet format` + Prettier |
| Birim + entegrasyon testleri | ✅ Etkin | 168 test |
| Kapsam eşiği | ⚠️ Kısmen | Genel %75 ve frontend %70 etkin; **Domain %90 ölçülemiyor** (§6) |
| Mimari testi | ✅ Etkin | Katman ve modül kuralları |
| Sır taraması | ✅ Etkin | gitleaks (tüm geçmiş) |
| Bağımlılık güvenliği | ✅ Etkin | NuGet audit + geçişli tarama |
| Konteyner taraması | ✅ Etkin | trivy — iki imaj |
| Migration kontrolü | ✅ Etkin | Bekleyen model değişikliği |
| Kod gözden geçirme | ✅ Etkin | En az bir onay (telafi edici kontrol — `KR-054`) |

---

## 4. Kanıtlanmış kontroller

A1'in ayırt edici yanı, kontrollerin **iddia edilmekle kalmayıp ölçülmüş** olmasıdır.
Her biri kasıtlı ihlalle sınandı:

| Kontrol | Sınama | Sonuç |
|---|---|---|
| Mimari kuralları | Domain'e `System.Text.Json` bağımlılığı eklendi | ❌ Test kırıldı → kaldırıldı ✅ |
| LOGO salt-okunurluk | `UPDATE` denendi | `The UPDATE permission was denied` ✅ |
| Açılışta yapılandırma doğrulaması | Bağlantı dizesi olmadan başlatıldı | Uygulama açılmadı, mesaj yol gösterdi ✅ |
| Denetim izi değiştirilemezliği | Tetikleyici migration'dan çıkarıldı | ❌ 2 test kırıldı → geri kondu ✅ |
| Hata ayrıntısı sızıntısı | `exception.ToString()` yanıta konuldu | ❌ 2 test kırıldı → geri alındı ✅ |
| Kapsam eşiği | Eşik %95'e çıkarıldı | ❌ Çıkış kodu 1 ✅ |
| PR izlenebilirlik denetimi | Milestone kaldırıldı | ❌ Kapı kırıldı → geri kondu ✅ |
| Sağlık kontrolü ayrımı | Veritabanı erişilemez yapıldı | `ready` 503, `live` 200 ✅ |
| Konteyner güvenliği | trivy taraması | 36 yüksek açık bulundu → düzeltildi ✅ |

---

## 5. Alınan kararlar

| Karar | Konu |
|---|---|
| `KR-057` | İddia kütüphanesi Shouldly (FluentAssertions lisans değişikliği) |
| `KR-058` | Kodlama dili: tanımlayıcılar İngilizce, yorumlar Türkçe |
| `KR-059` | Günlükte iki katmanlı maskeleme, fail-closed davranış |
| `KR-060` | Denetim izinin değiştirilemezliği (uygulama + veritabanı) |
| `KR-061` | Erişim kaydında fail-closed davranış ve kapsam |
| `KR-062` | Hata ayrıntısı hiçbir ortamda yanıta konmaz |
| `KR-063` | Sağlık kontrollerinin ayrımı; izlerde kişisel veri temizliği |
| `KR-064` | Frontend araç sürümlerinin sabitlenmesi |
| `KR-065` | Konteyner imajı kuralları ve UAT yığını ayrımı |

**Lisans ilkesinin (`KR-025`) uygulandığı durumlar:** FluentAssertions, gitleaks
GitHub Action, MUI X `DateRangePicker`, MUI X `DataGrid`. Dördünde de ücretsiz
alternatif kuruldu; hiçbir koşullu ticari lisanslı bileşen kullanılmadı.

---

## 6. Açık konular ve devreden işler

### 6.1 Domain kapsam eşiği ölçülemiyor

Domain katmanı şu an yalnızca otomatik özelliklerden oluşuyor; ölçülecek satır yok.
Kapsam denetimi bunu **açıkça atlıyor** ve raporluyor. İlk iş kuralı eklendiğinde
%90 eşiği kendiliğinden devreye girecek.

**Eylem:** İlk modülle birlikte doğrulanacak. Ayrı bir iş kalemi gerekmiyor.

### 6.2 Geriye kalan doğrulama boşlukları

| Konu | Neden şimdi yapılmadı | Ne zaman |
|---|---|---|
| Uçtan uca testler (Playwright) | Test edilecek kullanıcı akışı yok | A2, ilk modülle |
| `401` / `429` hata eşlemeleri | Fırlatan kod yok | Kimlik Yönetimi modülü |
| LOGO ve NAS sağlık kontrolleri | Bağlantı bileşenleri yazılmadı | İlgili modüllerle |
| `PermissionGate` bileşeni | Yetkilendirme sözleşmesi belirsiz | T4 Yetkilendirme |
| Üretim dağıtımı (Linux, TLS) | Sunucu hazırlanıyor | A3 |

### 6.3 UAT ortamı

Compose yığını hazır (`docker/compose.uat.yml`); **sunucu bekleniyor.** Linux sunucu
hazırlandığında Kimlik Yönetimi modülüyle birlikte devreye alınacak. UAT'nin boş bir
iskeletle İK'ya açılması, beklenti yönetimi açısından uygun görülmedi.

### 6.4 İK'ya iletilen veri düzeltme listesi

LOGO verisi 2026-09-11 tarihinde yeniden ölçüldü. **Kritik bulgu ağırlaştı:**

| Bulgu | 2026-09-09 | 2026-09-11 |
|---|---|---|
| Aktif personel | 583 | 584 |
| Kurumsal e-posta yok | 38 | 38 |
| Cep telefonu yok | 13 | 13 |
| **Hiçbir iletişim bilgisi yok** | 12 | **12** |
| Telefon biçimi geçersiz | 1 | **3** |
| **Aynı e-posta birden fazla kişide** | 4 | **37** |
| Kurumsal olmayan alan adı | 58 | 58 |

> **Aynı e-posta bulgusu 4'ten 37 kişiye çıktı.** 13 farklı adres paylaşılıyor; bir
> adres **10 kişiye** tanımlı. Bu, `R-12` riskinin öngörülenden büyük olduğunu
> gösteriyor: ortak posta kutusuna giden doğrulama kodu ile bir kişi başkasının adına
> üye olabilir.

**Eylem:** Güncel liste İK'ya iletildi. Kimlik Yönetimi modülü devreye alınmadan önce
en az "hiçbir iletişim bilgisi yok" (12 kişi) ve "aynı e-posta" (37 kişi) listelerinin
kapatılması gerekiyor.

---

## 7. Risk kayıt defteri gözden geçirmesi

| Risk | Değişiklik |
|---|---|
| `R-03` KVKK yükümlülüğü | **Azaldı.** Maskeleme, denetim izi, erişim kaydı ve izlerde veri temizliği kuruldu ve testlerle doğrulandı |
| `R-05` Bilgi tekelliği | **Azaldı.** Kararlar ve gerekçeler ADR + karar defterinde yazılı; kod yorumları gerekçe taşıyor |
| `R-12` Aynı e-posta | **Arttı.** 4 → 37 kişi. Önlem aynı (kanal sunulmaz), ancak veri düzeltme aciliyeti yükseldi |
| `R-16` Dal koruma | **Azaldı.** Telafi edici kontrollere PR izlenebilirlik denetimi eklendi (#18) |
| `R-02` LOGO şema değişikliği | **Değişmedi.** Şema doğrulama denetimi LOGO entegrasyon modülüyle gelecek |

**Yeni risk önerisi yok.** A1 sırasında karşılaşılan sorunların tamamı (araç sürümleri,
konteyner açıkları, nginx başlık kalıtımı) **tespit edildikleri anda çözüldü**;
kalıcı risk bırakmadılar.

---

## 8. A1'de öğrenilenler

**İşe yarayan**

1. **Kontrolleri kasıtlı ihlalle sınamak.** Dokuz kontrolün her biri, gerçekten
   çalıştığı ölçülerek kabul edildi. "Hep yeşil" bir kapı hiçbir şey ifade etmez.
2. **Gerekçenin kodda yazılı olması.** Bir ayarın *neden* öyle olduğunu açıklayan
   yorumlar, altı ay sonra aynı tartışmayı tekrar yapmayı önleyecek.
3. **Küçük PR'lar.** Ortalama PR boyutu gözden geçirilebilir kaldı; her biri tek bir
   konuyu kapattı.
4. **Uygulamayı gerçekten çalıştırmak.** Vite vekili ve nginx başlıkları hataları
   yalnızca çalıştırınca görüldü — testler ikisini de yakalamamıştı.

**İşe yaramayan / düzeltilen**

1. **Sıralama hatası.** Frontend iskeleti (2.3) atlandı ve CI'ya geçildi; kullanıcı
   fark etti. Bağımlılık sıralaması doğruydu, geri dönmeyi atlamak hataydı.
2. **Kural yazılı değilse uygulanmıyor.** Milestone ve issue bağlantısı alışkanlıkla
   yapılıyordu; yazılı olmadığı için unutuldu. Düzeltici faaliyet (#17) ile hem
   yazıldı hem CI'ya taşındı.
3. **Varsayılan araç seçimleri sorgulanmalı.** Vite şablonunun getirdiği TypeScript 6,
   ESLint 10 ve oxlint, ekosistem ve ADR uyumu açısından değiştirildi.

---

## 9. 33061 öz değerlendirme güncellemesi

| Süreç | A0 sonu | A1 sonu | Kanıt |
|---|---|---|---|
| MAN.1 Proje Planlama | Kurulmuş | **İşletiliyor** | Plan, WBS, bu kapanış belgesi |
| MAN.5 Konfigürasyon Yönetimi | Kurulmuş | **İşletiliyor** | Sürüm sabitleme, paket envanteri, OpenAPI konfigürasyon öğesi, telafi kontrolleri |
| MAN.8 Kalite Güvence | Kısmen | **İşletiliyor** | 8 CI kapısı, düzeltici faaliyet kaydı (#17) |
| TEC.5 Tasarım | İşletiliyor | **İşletiliyor** | 16 ADR, OpenAPI sözleşmesi |
| TEC.7 Gerçekleştirme | — | **İşletiliyor** | 16 PR, kodlama standartları |
| TEC.9 Doğrulama | — | **İşletiliyor** | 168 test, kapsam eşikleri, kasıtlı ihlal sınamaları |
| TEC.2 Paydaş Gereksinimleri | Kısmen | **Kısmen** | Vizyon-kapsam hazır; modül gereksinimleri İK toplantılarıyla toplanacak |
| TEC.11 Geçerleme | — | **Başlamadı** | UAT sunucusu bekleniyor |
| MAN.4 Risk Yönetimi | İşletiliyor | **İşletiliyor** | Bu belgede gözden geçirme yapıldı |

> **Not:** Bu bir **öz değerlendirmedir**, resmî derecelendirme değildir. Belgelenmiş
> Seviye 2 kriterleri belgelendirme kuruluşundan alındığında yeniden değerlendirilecektir.

---

## 10. A2'ye geçiş

**Sıradaki aşama:** A2 — Temel Modüller. İlk modül **T3 Kimlik Yönetimi** (giriş ve
üyelik).

**Hazırlık durumu**

| Konu | Durum |
|---|---|
| Gereksinim taslağı | ✅ Hazır — 46 gereksinim, 12 açık soru, 9 ekran (İK toplantısına sunulacak) |
| Teknik ön koşullar | ✅ Tamam — kimlik, günlük, denetim, hata yönetimi, frontend iskeleti |
| LOGO veri kalitesi | ⚠️ İK çalışması sürüyor — 12 + 37 kişilik kritik listeler açık |
| NetGSM SMS | ✅ Hazır — kullanıcı tanımlı, standart servis (`KR-042`) |
| UAT ortamı | ⚠️ Sunucu bekleniyor |

**İlk adım:** İK gereksinim toplantısı. Toplantı çıktısı gereksinim belgesine
işlendikten sonra modül geliştirmesi başlayacaktır (TEC.2 → TEC.3 → TEC.7).

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-11 | 0.1 | İlk oluşturma — A1 kapanış değerlendirmesi | Bilgi İşlem |
