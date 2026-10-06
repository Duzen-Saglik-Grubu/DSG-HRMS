# Roller, Sorumluluklar ve Yetkinlikler

**Belge kimliği:** MAN.1-RS
**Süreç:** MAN.1 — Proje Planlama
**Son güncelleme:** 2026-10-06
**33061 karşılığı:** *Roles, responsibilities, accountabilities, and authorities*
**Karşıladığı öznitelik maddeleri:** PA 2.1 (d) sorumluluk ve yetki tanımı, PA 2.1 (f) yetkinlik

---

## 1. Amaç

TS ISO/IEC 33020 PA 2.1'in iki maddesi doğrudan bu belgeyle karşılanır:

> **(d)** Süreci yürütmeye ilişkin sorumluluk ve yetkiler belirlenir, atanır ve duyurulur.
> **(f)** Süreci yürüten kişi(ler) uygun eğitim, öğretim veya deneyim temelinde yetkindir.

Denetimde sorulan soru "kim yapıyor" değil, **"kim sorumlu, kim karar veriyor ve bu
kişi yetkin mi"** olur. Bu belge o soruların yazılı cevabıdır.

---

## 2. Roller

### R1 — Proje Sorumlusu / Geliştirici

| | |
|---|---|
| **Kim** | Bilgi İşlem Birim Sorumlusu |
| **Sorumluluk** | Teknik tasarım, geliştirme, test, dağıtım, işletme, dokümantasyon, 33061 kanıtlarının üretilmesi |
| **Yetki** | Teknik kararlar (mimari, teknoloji, kütüphane, veri modeli); dal koruma ve sürüm etiketleme |
| **Hesap verdiği** | Üst Yönetim |
| **Yetkinlik dayanağı** | ASP.NET Core ve .NET ekosisteminde deneyim; mevcut HRMS ve LOGO entegrasyonuna hâkimiyet; PostgreSQL ve MSSQL deneyimi; kurum süreçlerine hâkimiyet |
| **Yetkinlik boşluğu** | .NET 10, React 19, Mapperly, Testcontainers ve 33061 süreç yönetimi görece yeni. Kapatma yöntemi: resmî dokümantasyon, ADR'lerle karar disiplini, iskelet aşamasında örnek uygulamalarla pekiştirme |

### R2 — İş Sahibi (Süreç Sahibi)

| | |
|---|---|
| **Kim** | İnsan Kaynakları Birimi |
| **Sorumluluk** | Gereksinimlerin sağlanması; iş kurallarının doğrulanması; kabul testlerinin yapılması; kabul formunun imzalanması |
| **Yetki** | **Modül kapsamı** ve **kabul kararı** — bir modülün "tamam" olduğuna İK karar verir |
| **Hesap verdiği** | Üst Yönetim |
| **Yetkinlik dayanağı** | İK süreçlerine ve mevzuata hâkimiyet; mevcut sistemin fiilî kullanıcısı |

### R3 — Proje Sahibi (Sponsor)

| | |
|---|---|
| **Kim** | Üst Yönetim |
| **Sorumluluk** | Kaynak tahsisi; kapsam ve takvim beklentisinin belirlenmesi |
| **Yetki** | **Proje onayı** (MAN.1.BP3); kapsam değişikliği onayı; projenin durdurulması |
| **Hesap verdiği** | — |

### R4 — Danışılan Uzmanlar

| Rol | Kim | Sorumluluk | Yetki |
|---|---|---|---|
| Birim sorumlusu / teknik sorumlusu | P4 | Hiyerarşi, onay akışı ve ekip süreçlerinin doğrulanması | Kendi biriminin çalışma düzeni |
| KVKK Sorumlusu | P6 | Kişisel veri uyumu, saklama süreleri | Uyum kararları |
| Mali İşler / Bordro | P7 | LOGO bütünlüğü, mesai ve ücret etkileri | LOGO erişim kuralları |
| İSG Uzmanı (iç kaynak) | P11 | İSG ve iş kazası süreç gereksinimleri | Mevzuata uygunluk |

---

## 3. RACI matrisi

**R** = Yapan · **A** = Onaylayan (hesap veren) · **C** = Danışılan · **I** = Bilgilendirilen

Her satırda **tam bir A** bulunur — kimin karar verdiği belirsiz kalmaz.

### 3.1 Süreç bazlı

| 33061 Süreci / Faaliyet | R1 Bilgi İşlem | R2 İK | R3 Üst Yön. | P4 | P6 KVKK | P7 Mali | P11 İSG |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| **TEC.2** Paydaş gereksinimleri | R | **A** | I | C | C | C | C |
| **TEC.3** Sistem gereksinimleri | **A**/R | C | I | — | C | — | — |
| **TEC.5** Tasarım ve ADR | **A**/R | I | I | — | C | — | — |
| **TEC.7** Geliştirme | **A**/R | I | I | — | — | — | — |
| **TEC.8** Entegrasyon | **A**/R | I | — | — | — | C | — |
| **TEC.9** Doğrulama (test) | **A**/R | I | I | — | — | — | — |
| **TEC.10** Geçiş ve veri göçü | R | **A** | I | I | C | C | — |
| **TEC.11** Geçerleme (kabul) | R | **A** | I | C | — | — | C |
| **TEC.13** Bakım | **A**/R | C | I | — | — | — | — |
| **MAN.1** Proje planlama | R | C | **A** | — | — | — | — |
| **MAN.2** İzleme ve kontrol | **A**/R | C | I | — | — | — | — |
| **MAN.4** Risk yönetimi | **A**/R | C | I | — | C | — | C |
| **MAN.5** Konfigürasyon yönetimi | **A**/R | I | — | — | — | — | — |
| **MAN.6** Bilgi yönetimi | **A**/R | C | I | — | C | — | — |
| **MAN.8** Kalite güvence | **A**/R | C | I | — | — | — | — |

### 3.2 Karar bazlı

| Karar | R1 | R2 İK | R3 Üst Yön. | Not |
|---|:--:|:--:|:--:|---|
| Teknoloji ve mimari seçimi | **A** | I | I | ADR olarak kayda geçer |
| Modül kapsamı | C | **A** | I | Gereksinim toplantısında kilitlenir |
| **Modül önceliklendirmesi** | C | **A** | I | Bağımlılık haritası dikkate alınarak |
| Modül kabulü | R | **A** | I | Kabul formu imzası |
| Proje kapsamı ve takvim | C | C | **A** | Vizyon-kapsam onayı |
| Kaynak tahsisi | C | — | **A** | |
| Üretime dağıtım | R | C | **A** | Kesim kararı |
| Veri göçünün kabulü | R | **A** | I | Sıfır açıklanamayan fark |
| Saklama süreleri | C | C | — | **A: KVKK Sorumlusu** |
| Değişiklik talebinin kabulü | C | **A** | I | Kapsam etkisi büyükse R3'e çıkar |
| Riskin kabul edilmesi | R | C | **A** | Puan ≥ 6 olan riskler için |

---

## 4. Yetkinlik yönetimi (PA 2.1 f)

### 4.1 Gerekli yetkinlikler ve durum

| Alan | Gereken | Durum | Kapatma yöntemi |
|---|---|---|---|
| .NET / ASP.NET Core | Yüksek | ✅ Mevcut | — |
| .NET 10 yenilikleri | Orta | ✅ Mevcut | İskelet ve T3'te uygulandı |
| EF Core + PostgreSQL | Yüksek | ✅ Mevcut | — |
| React + TypeScript | Orta–Yüksek | ✅ Mevcut | — |
| React 19 / TanStack Query | Orta | ✅ Mevcut | T3 ekranlarında uygulandı |
| Testcontainers | Orta | ✅ Mevcut | PostgreSQL ve SQL Server ile entegrasyon testleri (T3) |
| Mapperly | Düşük | ⏳ Kullanılmadı | Paket ekli; T3'te eşleme elle yazıldı. İlk gerektiğinde değerlendirilir |
| Kimlik doğrulama ve oturum (JWT, yenileme jetonu, parola özetleme) | Yüksek | ✅ Mevcut | T3 (ADR-0006) |
| Uçtan uca test (Playwright) | Orta | ✅ Mevcut | T3 sonunda kuruldu (#146) |
| Docker / Linux işletim | Orta | ✅ Mevcut | — |
| LOGO veri yapısı | Yüksek | ✅ Mevcut | Analiz belgeleriyle pekiştirildi |
| KVKK gereklilikleri | Orta | 🔄 Geliştiriliyor | KVKK Sorumlusundan görüş (P6) |
| **TS ISO/IEC TS 33061 / 33020** | Yüksek | 🔄 Geliştiriliyor | Standartlar temin edildi ve incelendi; kriterler belgelendi |
| İş hukuku (izin, kıdem) | Orta | ⏳ İK'dan alınacak | İzin modülü gereksinim toplantısı |

### 4.2 Yetkinlik kaydı

Yetkinlik durumu **her modül kapanışında** gözden geçirilir. Yeni bir teknoloji veya
mevzuat alanı gerektiğinde:

1. Boşluk bu tabloya eklenir.
2. Kapatma yöntemi belirlenir (dokümantasyon, örnek uygulama, dış görüş).
3. Kapatıldığında durum güncellenir.

> Bu tablo, denetimde PA 2.1 (f) için **doğrudan kanıttır**. Boşluğun kayıtlı olması
> zayıflık değil, **süreç olgunluğu göstergesidir**; kayıtsız boşluk zayıflıktır.

---

## 5. Sorumlulukların duyurulması (PA 2.1 d)

Sorumluluk ve yetkiler şu kanallarla duyurulur ve kanıtlanır:

| Kanal | Ne gösterir |
|---|---|
| Bu belge (depoda, sürümlenmiş) | Rol ve yetki tanımlarının yazılı olması |
| Dal koruma telafi kontrolleri ve PR onay yorumu | `main`'e PR dışı yazmanın ve onaysız birleştirmenin tespiti (`KR-055`, `KR-096`); sunucu tarafı dal koruma Free planda yok |
| GitHub Issue/PR atamaları | Hangi işin kimde olduğu |
| Kabul formları | Kabul kararının kim tarafından verildiği |
| Vizyon-kapsam ve plan onayı | Proje onayının kim tarafından verildiği |
| Karar kayıt defteri (`KR-NNN`) | Her kararın kaynağı |

---

## 6. Tek kişiye bağımlılık — açık bir zayıflık

Projenin geliştirme tarafı **tek kişiye** dayanmaktadır (`KS8`, `R-05`). Bu, planın
gizlemediği bir gerçektir ve üst yönetime raporlanan bir konudur.

**Etkisi azaltan önlemler:**

| Önlem | Nasıl çalışır |
|---|---|
| ADR'ler (15 adet) | Kararların *neden* alındığı yazılı; yeni kişi bağlamı okuyarak edinir |
| Karar kayıt defteri (53 karar) | Kararların tek ve resmî kaydı |
| Test kapsamı eşikleri | Yeni kişi değişiklik yaparken güvenlik ağı bulur |
| Mimari testi | Yapısal kurallar kodla zorlanır, sözlü bilgiye ihtiyaç kalmaz |
| Kurulum kılavuzu (`README.md`) | Ortamın sıfırdan kurulabilmesi |
| Doküman haritası | Hangi bilginin nerede olduğu |

> **Bu önlemler bağımlılığı ortadan kaldırmaz, devir süresini kısaltır.** Kaynak
> artırımı planın yetkisi dışındadır; `R-05` bu nedenle "İzleniyor" durumundadır ve
> dönemsel durum raporlarında üst yönetime hatırlatılır.

---

## 7. Bu belgeden doğan açık işler

| # | İş | Sorumlu |
|---|---|---|
| 1 | RACI matrisinin İK ve üst yönetimle teyidi | Bilgi İşlem |
| 2 | Birim sorumlusu / İK uzmanı sayılarının netleştirilmesi | İK |
| 3 | KVKK Sorumlusunun projedeki rolünün resmen bildirilmesi | Üst Yönetim |
| 4 | Yetkinlik tablosundaki 🔄 ve ⏳ maddelerin iskelet aşamasında kapatılması | Bilgi İşlem |

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-07 | 0.1 | İlk oluşturma — roller, RACI, yetkinlik yönetimi | Bilgi İşlem |
| 2026-10-06 | 0.2 | §4.1 yetkinlik tablosu T3'e göre güncellendi; §5 dal koruma satırı gerçeğe göre düzeltildi. Üst Yönetim onayına sunuldu (#127) | Bilgi İşlem |
