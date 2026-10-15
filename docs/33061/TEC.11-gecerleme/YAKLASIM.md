# TEC.11 — Süreç Yaklaşımı

**Belge kimliği:** TEC.11-YAK
**Süreç:** TEC.11 — Geçerleme
**Son güncelleme:** 2026-10-15
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

   Plan İK'ya sunulmadan önce Bilgi İşlem **her senaryoyu ekranda bir kez dener** ve her
   beklenen sonucu onaylı gereksinimin kabul ölçütüyle karşılaştırır. T3'te plan metninden
   yazılan iki adım ekranda uygulanamadı, bir beklenen sonuç gereksinimle çelişti (#196; alınan
   dersler D-03). İK planı gözden geçirir ve takvimi belirler.
1a. **Ön deneme (ilk kabul adayından önce):** G2'den sonra İK, UAT'de sistemi serbestçe dener;
   Bilgi İşlem kabul senaryolarını prova eder. İlk kabul adayı bunlardan **sonra** etiketlenir.
   T3'te bu denemeler adaylar etiketlendikten sonra yapıldı ve beş aday çıktı: bir hata
   düzeltmesi (rc.2), iki değişiklik talebi (rc.3, rc.4) ve bir prova bulgusu (rc.5) (D-01).
2. **Giriş ölçütleri kontrol edilir:**
   - G2 geçti;
   - kabul adayı sürüm etiketlendi ve UAT'ye bu etiketten kuruldu;
   - katılımcılar İK tarafından belirlendi (§2.1); listede olmayanların e-posta ve
     telefonları UAT izin listesine eklendi (önceki kabullerin katılımcıları listede kalır, `KR-106`);
   - katılımcılara kısa kullanım notu verildi;
   - doldurulmuş kabul formunun depoya alınma yolu hazır: Markdown kaydı ve üst verisi
     temizlenmiş Word kopyası `kayitlar/` altında (`KR-107`; T3'te saklama yeri kabulden önce
     belirlenmemişti, D-06).
3. **Kabul oturumu (`BP2`):**
   - Katılımcılar senaryoları UAT'de kendileri uygular; Bilgi İşlem gözlemler ve yönlendirmez.
   - Kullanıcı arayüz ve teknik doğrulama gerektiren gereksinimler için Bilgi İşlem kanıtı gösterir (**kanıt gösterimi**). İK bu kanıtı inceler.
4. **Sonuçlar (`BP3`):**
   - Her senaryo "geçti", "geçmedi" veya "koşullu" olarak işaretlenir.
   - Bulunan her sorun bir `[HATA]` issue'su olur, senaryo kimliğiyle.
   - Gözlemler (kullanım güçlüğü vb.) ayrıca yazılır.
5. **Karar (G3):** İK, kabul ölçütüne göre kararını kabul formuna yazar: kabul, koşullu kabul veya ret. Form, sürüm etiketini taşır. Form oturumda doldurulur ve depoya alınır; ıslak imza kalite belgeleriyle birlikte proje sonunda toplanır (`KR-107`; T3: `KR-105` (4)).
6. **Kabul raporu:** Bilgi İşlem kabul raporunu yazar; izlenebilirlik matrisinin "Kabul" sütununu doldurur.

### 2.1 Katılımcı seçimi (#219)

**Katılımcıları İK seçer** (Doğuş Uçanok'un kararı, 15.10.2026, #219). Sabit bir oran veya birim
kuralı yoktur, çünkü modülün ekranları farklı kullanıcılara hitap eder:

- **Yalnızca İK'nın kullanacağı ekranlar** İK kullanıcılarıyla denenir; İK dışından personel
  bu senaryolara katılmaz.
- **Personelin kullanacağı ekranlar** için İK, gerektiğinde Bilgi İşlem dışındaki birimlerden
  personeli de katılımcı yapar (`KR-048`).
- Kabul planı her senaryonun kimin ekranı olduğunu ve istenen katılımcı özelliklerini yazar (örn.
  T3'te: hesabı olmayan, cep telefonu kayıtlı olan ve olmayan personel); İK seçimini buna göre
  yapar.
- Bilgi İşlem seçimi kabul raporuna yazar.

> T3'te personel katılımcıların dördü de Bilgi İşlem'dendi (kabul raporu §2; alınan dersler
> D-08). Değerlendirme: oran kuralı İK'ya özgü ekranlarda uygun değil; seçim İK'ya bırakılır,
> kabul planı her senaryonun kullanıcısını açıkça belirtir.

---

## 3. Geçerleme yöntemleri (`BP1.4`)

| Yöntem | Ne zaman | Kim uygular | Kanıt |
|---|---|---|---|
| **Senaryo** | Kullanıcının ekranda gördüğü davranış | İK ve katılımcı personel | Senaryo sonucu (form) |
| **Kanıt gösterimi** | Kullanıcı arayüzünden gözlenemeyen davranış: veritabanında özet saklama, günlükte maskeleme, hız sınırları, LOGO senkronizasyonu | Bilgi İşlem gösterir, İK inceler | Doğrulama raporu satırı, test veya ölçüm |

**Kanıt gösterimi kuralı (#219):** Ekranda gözlenebilen bir davranış kanıt gösterimiyle kabul
edilmez; senaryoyla denenir. Bir gereksinimin işlevi başka bir modülle gelecekse (örn.
REQ-KMLK-042 bildirim istisnası → Y1; REQ-KMLK-054 video → Eğitim Yönetimi) gereksinim
bugünkü davranışın kanıt gösterimiyle kabul edilebilir; izlenebilirlik matrisine "ilgili
modülün kabulünde senaryo olarak yeniden denenir" notu düşülür. T3'te 56 gereksinimin 15'i
yalnızca kanıt gösterimiyle kabul edildi (T3 modül sonu denetimi YB-05); kabul planı, kanıt
gösterimine bırakılan her gereksinimin neden ekrandan denenemediğini yazar.
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
| UAT izin listesi: yalnızca listedeki adreslere e-posta ve SMS gider (`KR-083`) | Var. Yeni katılımcılar eklenir; önceki kabullerin katılımcıları listede kalır (`KR-106`). Saklama koşulu KVKK Sorumlusu tarafından onaylandı (BK-22, `KR-109`) |
| Etiketli sürüm ve dağıtım kaydı | Var (#125): UAT'ye yalnızca etiketten `deploy-uat.sh` ile kurulur; kayıt `TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`. T3 kabulü `v0.2.0-rc.5` kurulumunda yapıldı |
| Kabul formu şablonu | `docs/sablonlar/kabul-formu.md` |

---

## 6. Üretilen bilgi ve kontrolü (`PA 2.2 a, b`)

| Bilgi | Yer | Kim | Kontrol |
|---|---|---|---|
| Kabul planı | `TEC.11-gecerleme/<MODÜL>-kabul-plani.md` | Bilgi İşlem | PR ile; İK gözden geçirir |
| Katılımcı notu | `TEC.11-gecerleme/<MODÜL>-kullanim-notu.md` | Bilgi İşlem | PR ile; İK gözden geçirir. Adım adım yönlendirme içermez |
| Kabul formu | `kayitlar/<tarih>-<modül>-kabul-formu.md` ve oturumda doldurulan formun kopyası `kayitlar/<tarih>-<modül>-kabul-formu.docx` (üst verisi temizlenmiş; aslının ve kopyanın SHA-256 özeti `.md` kayıtta). Taranmış imzalı nüsha proje sonunda kalite belgesi olarak depoya konur (`KR-107`) | İK | Sürüm etiketi formda yazılı |
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
| g) Nesnel kanıt | Kabul formu (depo kaydı ve doldurulmuş formun kopyası; imzalı nüsha proje sonunda, `KR-107`), kabul raporu, `KR-105` |
| h) İzlenebilirlik | Matris "Kabul" sütunu |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-04 | 1.0 | İlk oluşturma (#124) | Bilgi İşlem |
| 2026-10-04 | 1.1 | §6: katılımcı notu eklendi (#154) | Bilgi İşlem |
| 2026-10-14 | 1.2 | §6: kabul formu kopyası depoda; imzalı nüsha proje sonunda (`KR-107`, #213) | Bilgi İşlem |
| 2026-10-14 | 1.3 | T3 modül sonu gözden geçirmesi: §2 adım 5 imza zamanı (`KR-107`); §5 etiketli sürüm ve dağıtım kaydı kuruldu (#125); §7 (g) (#214) | Bilgi İşlem |
| 2026-10-15 | 1.4 | §2: senaryoların ekranda denenmesi, ön deneme (1a), giriş ölçütleri; §2.1 katılımcı seçimi İK'nın (oran kuralı yok); §3 kanıt gösterimi kuralı; §5 izin listesi (#219) | Bilgi İşlem |
