# TEC.11 — Süreç Yaklaşımı

**Belge kimliği:** TEC.11-YAK
**Süreç:** TEC.11 — Geçerleme
**Son güncelleme:** 2026-10-04
**Karşıladığı öznitelik maddeleri:** `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir. `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir.

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir.

---

## 1. Sürecin amacı ve sınırı

Geçerleme, sistemin **gerçek kullanımında** paydaş gereksinimlerini karşıladığına dair nesnel kanıt üretir. Kabulü **paydaş** verir (TS ISO/IEC TS 33061, TEC.11).

| | Doğrulama (TEC.9) | Geçerleme (bu süreç) |
|---|---|---|
| Soru | "Ürünü doğru mu yaptık?" | "Doğru ürünü mü yaptık?" |
| Ölçüt | Sistem gereksinimleri (`SYG-`) | Paydaş gereksinimlerinin kabul kriterleri (`REQ-`, TEC.2) |
| Kim | Bilgi İşlem, CI | **İK** ve İK dışından 2–3 personel (`KR-048`) |
| Nerede | Geliştirme ortamı, CI | **UAT**, etiketli sürüm (`KR-024`, ADR-0011 §7) |
| Kanıt | Doğrulama raporu | **İmzalı kabul formu**, kabul raporu |

Geçerleme, doğrulama bittikten sonra başlar: **G2 kapısı** geçilmeden kabul yapılmaz.

---

## 2. Süreç nasıl işletilir? (her modül için)

1. **Hazırlık (`BP1`):** Bilgi İşlem modülün **kabul planını** yazar (`<MODÜL>-kabul-plani.md`). Planda şunlar yer alır:
   - kapsam (onaylı paydaş gereksinimleri);
   - kabul senaryoları ve her senaryonun hangi gereksinimleri kapsadığı;
   - katılımcılar, ortam ve sürüm;
   - giriş ölçütleri ve kabul ölçütü.

   İK planı gözden geçirir ve takvimi belirler.
2. **Giriş ölçütleri kontrol edilir:**
   - G2 geçti;
   - kabul adayı sürüm etiketlendi ve UAT'ye bu etiketten kuruldu;
   - katılımcıların e-posta ve telefonları UAT izin listesine eklendi;
   - katılımcılara kısa kullanım notu verildi.
3. **Kabul oturumu (`BP2`):**
   - Katılımcılar senaryoları UAT'de kendileri uygular; Bilgi İşlem gözlemler ve yönlendirmez.
   - Kullanıcı arayüz ve teknik doğrulama gerektiren gereksinimler için Bilgi İşlem kanıtı gösterir (**kanıt gösterimi**). İK bu kanıtı inceler.
4. **Sonuçlar (`BP3`):**
   - Her senaryo "geçti", "geçmedi" veya "koşullu" olarak işaretlenir.
   - Bulunan her sorun bir `[HATA]` issue'su olur, senaryo kimliğiyle.
   - Gözlemler (kullanım güçlüğü vb.) ayrıca yazılır.
5. **Karar (G3):** İK, kabul ölçütüne göre kabul formunu imzalar: kabul, koşullu kabul veya ret. Form, sürüm etiketini taşır.
6. **Kabul raporu:** Bilgi İşlem kabul raporunu yazar; izlenebilirlik matrisinin "Kabul" sütununu doldurur.

---

## 3. Geçerleme yöntemleri (`BP1.4`)

| Yöntem | Ne zaman | Kim uygular | Kanıt |
|---|---|---|---|
| **Senaryo** | Kullanıcının ekranda gördüğü davranış | İK ve katılımcı personel | Senaryo sonucu (form) |
| **Kanıt gösterimi** | Kullanıcı arayüzünden gözlenemeyen davranış: veritabanında özet saklama, günlükte maskeleme, hız sınırları, LOGO senkronizasyonu | Bilgi İşlem gösterir, İK inceler | Doğrulama raporu satırı, test veya ölçüm |
| **Gözlem** | Kullanım kolaylığı | Bilgi İşlem gözlemler | Gözlem notları |

---

## 4. Kabul ölçütü

- **Zorunlu gereksinimlerin tamamı** karşılanmalı. Karşılanmayan bir Zorunlu gereksinim ret sebebidir; İK gerekçeyle koşullu kabul verebilir. Koşul ve tarihi forma yazılır.
- **"Olmalı" gereksinimler:** Karşılanmayanlar forma yazılır ve iş listesine alınır; kabulü engellemez.
- **Kabul kriterini tam karşılamayan bilinen durumlar** (örn. SYG-KMLK AN-01) kabulde İK'ya **ayrıca gösterilir** ve forma yazılır.

---

## 5. Destekleyici sistemler (`BP1.5–6`)

| Sistem | Durum |
|---|---|
| UAT ortamı (gerçek LOGO verisiyle) | Var (TEC.10 runbook) |
| UAT izin listesi: yalnızca listedeki adreslere e-posta ve SMS gider (`KR-083`) | Var; katılımcılar her kabulde eklenir |
| Etiketli sürüm ve dağıtım kaydı | #125 ile kurulacak |
| Kabul formu şablonu | `docs/sablonlar/kabul-formu.md` |

---

## 6. Üretilen bilgi ve kontrolü (`PA 2.2 a, b`)

| Bilgi | Yer | Kim | Kontrol |
|---|---|---|---|
| Kabul planı | `TEC.11-gecerleme/<MODÜL>-kabul-plani.md` | Bilgi İşlem | PR ile; İK gözden geçirir |
| Katılımcı notu | `TEC.11-gecerleme/<MODÜL>-kullanim-notu.md` | Bilgi İşlem | PR ile; İK gözden geçirir. Adım adım yönlendirme içermez |
| İmzalı kabul formu | `kayitlar/<tarih>-<modül>-kabul-formu.md` (taranmış imzalı nüsha depo dışında, MAN.6 kaydında) | İK | Sürüm etiketi formda yazılı |
| Kabul raporu | `raporlar/<tarih>-<modül>-kabul-raporu.md` | Bilgi İşlem | PR ile |
| Kabulde bulunan sorunlar | GitHub issue (`[HATA]`, `surec:TEC.11`) | Bulan | Senaryo kimliğiyle |
| İzlenebilirlik | `../izlenebilirlik-matrisi.md` "Kabul" sütunu | Bilgi İşlem | PR ile |

**Kişisel veri:** UAT gerçek veriyle çalışır. Kabul kayıtlarına ekran görüntüsü alınacaksa kişisel veri görünmez kılınır.

---

## 7. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (TEC.11) | Karşılığı |
|---|---|
| a) Geçerleme kriterleri tanımlanır | PG kabul kriterleri; kabul planı senaryoları |
| b) Paydaşın ihtiyaç duyduğu hizmetlerin varlığı teyit edilir | Kabul senaryoları (UAT) |
| c) Geçerleme kısıtları belirlenir | §3, §5; kabul planı "kısıtlar" |
| d) Sistem geçerlenir | Kabul oturumu |
| e) Destekleyici sistemler mevcut | §5 |
| f) Sonuçlar ve anomaliler belirlenir | Kabul formu, `[HATA]` issue'ları |
| g) Nesnel kanıt | İmzalı kabul formu, kabul raporu |
| h) İzlenebilirlik | Matris "Kabul" sütunu |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-04 | 1.0 | İlk oluşturma (#124) | Bilgi İşlem |
| 2026-10-04 | 1.1 | §6: katılımcı notu eklendi (#154) | Bilgi İşlem |
