# Proje Durum Raporu — 2

**Belge kimliği:** MAN.2-DR-2026-10-13
**Süreç:** MAN.2 — Proje Değerlendirme ve Kontrol
**Dönem:** 06.10.2026 – 13.10.2026
**Hazırlayan:** Bilgi İşlem · **Sunulan:** Üst Yönetim
**Sıklık:** Her modül kapanışında ve en geç ayda bir (`KR-101`). Bu rapor T3 kapanışında yazıldı. Önceki: `2026-10-06-durum-raporu.md`.

---

## 1. Özet

- **İlk kullanıcı modülü teslim edildi.** İK, T3 Kimlik Yönetimi'ni 13.10.2026'da **koşulsuz kabul etti** (`KR-105`). Kabul edilen sürüm `v0.2.0` olarak etiketlendi; modül kapandı (G4).
- **Üst Yönetim kararları yürürlükte.** 06.10'da sunulan dört karar 09.10'da verildi (`KR-103`): plan ve RACI onaylandı, R-25 kabul edildi, üretim altyapısına kaynak ayrıldı, başarı ölçütleri teyit edildi.
- **Modül sonu süreç denetimi yapıldı.** 15 sürecin hepsi Seviye 1'de; 02.10'da beşi Seviye 0'daydı. Seviye 2'de henüz süreç yok. 13 düzeltici iş açıldı (#207–#219).
- **Bir risk yükseldi:** R-09. Mevcut sistemin PostgreSQL 14 desteği Kasım 2026'da bitiyor ve üretim altyapısı için hedef tarih yok (§5).

## 2. Aşama ve modül durumu

| Aşama | Durum | Sürüm | Not |
|---|---|---|---|
| A0 — Hazırlık ve planlama | ✅ Tamamlandı | — | Plan 0.3 ve RACI 0.2 onaylı (`KR-103`) |
| A1 — Teknik iskelet | ✅ Tamamlandı (12.09.2026) | `v0.1.0` | |
| A2 — Temel modüller | 🔄 Sürüyor | `v0.2.0` | Aşağıda |
| A3 — Yatay altyapı | ⏳ Başlamadı | — | 3 gereksinim kaydı bekliyor (#3, #42, #189) |
| A4+ — İş modülleri | ⏳ Başlamadı | — | |
| AS — Geçiş | ⏳ Başlamadı | `v1.0.0` | Üretim altyapısı için hedef tarih yok (§5) |

**A2 modülleri:**

| Modül | Durum | Kapılar |
|---|---|---|
| T3 Kimlik Yönetimi | ✅ **Kabul edildi ve kapandı** — `v0.2.0` | G1 ✅ 23.09 · G2 ✅ 04.10 · G3 ✅ 13.10 · G4 ✅ 13.10 |
| T1 Personel, T2 Organizasyon, T4 Rol ve Yetki, T5 Kullanıcı | İK gereksinim toplantısı bekliyor (`KR-068`) | — |

## 3. Ölçütler (plan §8.1)

| Ölçüt | Değer | Yorum |
|---|---|---|
| Modül çevrim süresi | T3: gereksinim onayı → kabul **20 gün** (G1 23.09 → G2 04.10 → G3 13.10) | İlk veri noktası. Plan, 3–4 modülden sonra tahmin vermeyi öngörüyor |
| Açık / kapalı risk | 30 kayıt: **10 açık**, 11 izleniyor, 3 kabul edildi, 6 kapalı. Puanı 6 ve üzeri: 10 | §4 |
| Test kapsamı (`v0.2.0`) | Backend **%92,9** (eşik %75), Domain **%98,9** (eşik %90), ön yüz **%87,6** | 796 backend + 247 ön yüz testi, tamamı geçti |
| Kabul testinde çıkan bulgu | **0** | Oturumdan önceki Bilgi İşlem provasında 4 bulgu; hepsi oturumdan önce kapandı |
| Değişiklik talebi | **3** (#77, #185, #190) | Üçü de İK onaylı; kapsamı büyütmediler, davranışı ayarladılar (R-04) |

**Ek göstergeler:**
- Toplam 101 birleştirilmiş PR; bu dönemde 15.
- Toplam 16 hata kaydı; hepsi kapalı.
- 106 kayıtlı karar (`KR-001`…`KR-106`).
- Beş kabul adayı (rc.1–rc.5); nedenleri kabul raporu §3.1'de.

## 4. Riskler (puanı 6 ve üzeri)

Risk defteri T3 kapanışında gözden geçirildi (sürüm 1.5). Üç yeni risk eklendi: R-28 (otomatik kontrollerin sessizce çalışmaması), R-29 (bağımlılık açıkları), R-30 (kayıt dışı gizli bilgi kopyaları).

| Risk | Puan | Durum | Not |
|---|---|---|---|
| R-18 TLS sertifikasının elle yenilenmesi | 9 | Açık | Bitiş 16.12.2026; kalıcı önlem Y4 modülünde (#42) |
| **R-09 PostgreSQL 14 desteğinin bitmesi** | **6** (4'ten yükseldi) | Açık | **Üst Yönetim'in bilgisine (§5)** |
| **R-30 Kayıt dışı gizli bilgi kopyaları** | **6** (yeni) | Açık | UAT'de eski bir sır dosyası kopyası bulundu; giderilecek (#207, #215) |
| R-01 Veri göçü | 6 | Açık | Göç aşamasında (AS) |
| R-03 KVKK yükümlülüğü | 6 | Açık | |
| R-04 Kapsam kayması | 6 | Açık | 3 değişiklik talebi, hepsi onaylı |
| R-05 Tek kişiye bağımlılık | 6 | İzleniyor | |
| R-06 Mevzuat değişikliği | 6 | Açık | İzin modülünde ele alınacak |
| R-15 Kapsam büyüklüğü | 6 | Açık | İlk çevrim süresi: 20 gün |
| R-25 UAT'nin gerçek kişisel veriyle çalışması | 6 | Kabul edildi | Üst Yönetim, 09.10.2026 (`KR-103`) |

**Puanı düşen risk:** R-20 (e-posta sunucusu sertifikası) 6'dan 3'e indi; sertifika 06.10'da yenilendi, bitiş 27.03.2027.

## 5. Üst Yönetim'in bilgisine ve kararına sunulanlar

| # | Konu | Neden | İstenen |
|---|---|---|---|
| 1 | **Üretim altyapısı için hedef tarih** (B2 PostgreSQL sunucusu, B4) | Mevcut sistemin PostgreSQL 14 desteği Kasım 2026'da bitiyor. 09.10'da kaynak ayrıldı ama tarih belirlenmedi. Geçiş (AS) bu tarihten sonraya kalırsa mevcut sistem desteksiz sürümde çalışır (R-09) | Yeni PostgreSQL kurulumu için hedef tarih |
| 2 | **Islak imzalar** | T3 kabul formu ve Üst Yönetim onay formu henüz imzasız; imzalar onaylı kalite belgeleriyle birlikte toplanacak | Bilgi; imza takvimi kalite belgeleriyle birlikte belirlenir (#213) |

## 6. Sonraki dönem (en geç 13.11.2026)

1. **Sıradaki modül:** İK gereksinim toplantısı (T1, T2, T4 veya T5) ve onaylı gereksinim listesi (G1).
2. **Öncelikli düzeltici işler, sıradaki modüle başlamadan:** UAT sunucusu (#207), otomatik kontrollerin izlenmesi (#208), kabul kanıtının korunması (#213).
3. **Sıradaki modülün G2'sinden önce:** doğrulama raporunun kabul edilen sürüme güncellenmesi (#209), değişiklik talebi yordamı (#210), geri yönde kapsam denetimi (#211).
4. **T3'ten alınan dersler** sıradaki modülde uygulanır: İK UAT denemesi ve Bilgi İşlem provası ilk kabul adayından önce; sürüm kontrol listesi.
5. **Bu raporun üçüncüsü:** sıradaki modülün kapanışında veya en geç bir ay sonra.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-13 | 1.0 | İkinci durum raporu; T3 kapanışı (#206) | Bilgi İşlem |
