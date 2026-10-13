# T3 Kimlik Yönetimi — Kapanış Gözden Geçirmesi (G4)

**Belge kimliği:** MAN.2-KGG-2026-10-13-T3
**Süreç:** MAN.2 — Proje Değerlendirme ve Kontrol (YAKLAŞIM §2.4)
**Modül:** T3 Kimlik Yönetimi · **Sürüm:** `v0.2.0` (`149f6ee`)
**Hazırlayan:** Bilgi İşlem · **Tarih:** 2026-10-13
**Karar veren (G4):** Doğuş Uçanok, Bilgi İşlem Birim Sorumlusu (RACI: A/R). Karar bu belgenin PR onayıyla yürürlüğe girer (`KR-096`).

---

## 1. Kapılar

| Kapı | Tarih | Kanıt |
|---|---|---|
| G1 — Gereksinim onayı | 23.09.2026 | `TEC.2-…/kayitlar/2026-09-23-kimlik-gereksinim-toplantisi.md`; PG-KMLK 56 gereksinim |
| G2 — Geliştirme tamam | 04.10.2026 | `TEC.9-dogrulama/raporlar/2026-10-04-t3-dogrulama-raporu.md` §5 |
| G3 — Kabul | 13.10.2026 | `TEC.11-gecerleme/kayitlar/2026-10-13-t3-kabul-formu.md`; `KR-105` |
| **G4 — Kapanış** | **13.10.2026** | Bu belge; alınan dersler (`2026-10-13-t3-alinan-dersler.md`); modül sonu süreç ve konfigürasyon denetimleri |

---

## 2. Plan ile gerçekleşen

Proje planı takvimi **tarih değil sıra** üzerine kurar (plan §5); bu yüzden karşılaştırma
sıra, kapsam ve kapılar üzerinden yapılır.

| Konu | Plan | Gerçekleşen | Sapma ve gerekçe |
|---|---|---|---|
| Modül sırası | T1 → T2 → T3 | T3 önce | Diğer modüller giriş ve yetkilendirmeye bağımlı (`KR-040`); personel çekirdeği T3'te kuruldu (`KR-077`). Plan 0.3'te işlendi ve Üst Yönetim'ce onaylandı (`KR-103`) |
| Kapsam | PG-KMLK: 56 onaylı gereksinim | 56 / 56 karşılandı ve kabul edildi | Yok |
| Kapsam değişikliği | — | 3 değişiklik talebi: #77 (onaylı kurala ayrıntı), #185 (deneme ve kod sınırları 10), #190 (2FA kullanıcı tercihine bağlı ve İK kurtarma işlemi). Üçü de İK onaylı | #77 canlı veri doğrulamasından (AN-17), #185 ve #190 UAT'de İK ile yapılan denemelerden doğdu. Kapsamı büyütmediler, davranışı ayarladılar (R-04) |
| Kapsam dışı işlev | — | T5'e ait PRM-HSP-03 parametresi #84'ten beri T3 kataloğundaydı; işlevi yoktu | Provada bulundu, rc.5'te çıkarıldı (#198). Ders: §4 ve alınan dersler D-02 |
| Sürüm | Modül başına bir MINOR (`KR-097`) | `v0.2.0`; beş kabul adayı (rc.1–rc.5) | Adaylar ve nedenleri: kabul raporu §3.1 |
| Kabul | G3 | Koşulsuz kabul; kabulde hata kaydı yok | Yok |

---

## 3. Ölçütler (plan §8.1)

| Ölçüt | Değer |
|---|---|
| Modül çevrim süresi (gereksinim → kabul) | **20 gün**: G1 23.09 → G2 04.10 (11 gün) → G3 13.10 (9 gün) |
| Kabul testinde çıkan bulgu | **0** (oturumda). Oturumdan önceki Bilgi İşlem provasında 4 bulgu; hepsi oturumdan önce kapandı (#196, #198) |
| Değişiklik talebi | **3** (#77, #185, #190) |
| Test kapsamı (`v0.2.0`) | Backend satır **%92,9** (eşik %75), dal %85,4; Domain %98,9 (eşik %90); ön yüz satır %87,6. 796 backend + 247 ön yüz testi, tamamı geçti |
| Hata kaydı | Proje toplamı 16 `[HATA]` (A1 dahil); hepsi kapalı. T3 kabul hazırlığı döneminde (03–13.10) açılanlar: #138, #175, #182, #198 |
| Düzeltici faaliyet | Proje toplamı 7 (A1 dahil); hepsi kapalı. T3 döneminde: #66, #67, #128, #129, #196 |
| İzlenebilirlik kapsaması | 56 / 56 kalem kaynaktan kabule eksiksiz, **%100** (matris §6) |

---

## 4. Değerlendirme

**İyi gidenler**
- Onaylı gereksinimlerin tamamı tek modül çevriminde karşılandı; kabul koşulsuz geldi.
- İzlenebilirlik CI'da denetleniyor; eksik SYG satırlarını birleştirmeden önce yakaladı.
- Parametrik yapı İK tarafından olumlu karşılandı (kabul formu §5); üç değişiklik talebinden
  ikisi kod değil varsayılan değer ve davranış ayarıydı.

**Sapmalar**
- Kabul adayı beş kez yenilendi. Biri hata düzeltmesi (rc.2), ikisi değişiklik talebi
  (rc.3, rc.4), biri prova bulgusu (rc.5).
- Bir T5 parametresi T3 kataloğunda iki haftadan uzun süre fark edilmeden kaldı (27.09 → 12.10, #198).
- rc.3'ün CI kanıt özeti etiket anında çıkarılmadı; sonradan üretildi (#194).
- İmzalı kabul formlarının saklanacağı yer kabulden önce belirlenmedi (MAN.6 Ö-3).

Ayrıntı ve önlemler: `2026-10-13-t3-alinan-dersler.md`.

---

## 5. Açık kalanlar (T3 kapandıktan sonra izlenir)

| Konu | Sahibi | Kayıt |
|---|---|---|
| Islak imzalar (kabul formu, Üst Yönetim onay formu) onaylı kalite belgeleriyle toplanacak | Doğuş Uçanok | BK-24, Ö-3 |
| REQ-KMLK-054'ün ekranda denenmesi | Eğitim Yönetimi kabulü | Kabul raporu §4 |
| İK'nın kilitli hesabı açması | T5 Kullanıcı Yönetimi | 23.09 toplantısı S-03 |
| Varsayılan ayarlara sıfırlama | Y4 Sistem Yönetimi | #189 |

---

## 6. G4 kararı

T3 Kimlik Yönetimi **kapanmıştır.** Kapsam karşılandı, kabul alındı, baseline `v0.2.0`
etiketlendi, açık kalanlar sahipleriyle kayıtlıdır. Modül sonu süreç denetimi ve
konfigürasyon denetimi bu kapanışla birlikte yapıldı (MAN.8 ve MAN.5 raporları).

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-13 | 1.0 | İlk sürüm | Bilgi İşlem |
