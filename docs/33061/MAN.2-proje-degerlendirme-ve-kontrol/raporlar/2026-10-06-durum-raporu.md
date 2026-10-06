# Proje Durum Raporu — 1

**Belge kimliği:** MAN.2-DR-2026-10-06
**Süreç:** MAN.2 — Proje Değerlendirme ve Kontrol
**Dönem:** 07.09.2026 (proje planı) – 06.10.2026
**Hazırlayan:** Bilgi İşlem · **Sunulan:** Üst Yönetim
**Sıklık:** Her modül kapanışında ve en geç ayda bir (`KR-101`). Bu, projenin ilk durum raporudur (#127).

---

## 1. Özet

- **Teknik iskelet (A1) tamamlandı** ve `v0.1.0` olarak etiketlendi (12.09.2026).
- **İlk kullanıcı modülü T3 Kimlik Yönetimi'nin geliştirmesi tamamlandı.** İK'nın 23.09.2026'da onayladığı 56 gereksinimin geliştirmesi 11 günde bitti. Geliştirme tamam kapısı (G2) 04.10.2026'da geçildi.
- **T3, İK kabulüne hazır.** Kabul adayı `v0.2.0-rc.2` test ortamında (UAT) çalışıyor. Kabul oturumu için İK'nın katılımcıları belirlemesi bekleniyor.
- **Süreç denetimi yapıldı.** T3 sonunda TS ISO/IEC TS 33061'e göre bir süreç denetimi yapıldı (02.10.2026). Denetimden çıkan 18 düzeltici işten 17'si tamamlandı; sonuncusu bu rapordur.
- **Üst Yönetim'den dört karar bekleniyor (§5):** proje planı ve RACI onayı, bir risk kabulü, üretim altyapısı ve başarı ölçütleri.

## 2. Aşama ve modül durumu

| Aşama | Durum | Sürüm | Not |
|---|---|---|---|
| A0 — Hazırlık ve planlama | ✅ Tamamlandı | — | Vizyon, kapsam, plan, 15 mimari karar kaydı (ADR). Plan onayı bekliyor (§5) |
| A1 — Teknik iskelet | ✅ Tamamlandı (12.09.2026) | `v0.1.0` | 36 iş kaydı kapandı |
| A2 — Temel modüller | 🔄 Sürüyor | — | Aşağıda |
| A3 — Yatay altyapı | ⏳ Başlamadı | — | 2 gereksinim kaydı bekliyor (#3, #42) |
| A4+ — İş modülleri | ⏳ Başlamadı | — | |
| AS — Geçiş | ⏳ Başlamadı | `v1.0.0` | |

**A2 modülleri:**

| Modül | Durum | Kapılar |
|---|---|---|
| T3 Kimlik Yönetimi | **Kabul bekliyor** — `v0.2.0-rc.2` UAT'de | G1 ✅ 23.09 · G2 ✅ 04.10 · G3 ⏳ · G4 ⏳ |
| T1 Personel, T2 Organizasyon, T4 Rol ve Yetki, T5 Kullanıcı | İK gereksinim toplantısı bekliyor (`KR-068`) | — |

> **Sıra değişikliği:** Plan T1 → T2 → T3 sırasını gösteriyordu. T3 önce geliştirildi, çünkü
> diğer bütün modüller giriş ve yetkilendirmeye bağımlı (`KR-040`). T3'ün ihtiyaç duyduğu
> personel çekirdeği T3 içinde kuruldu (`KR-077`). Plan 0.3 bu sırayı yansıtıyor.

## 3. Ölçütler (plan §8.1)

| Ölçüt | Değer | Yorum |
|---|---|---|
| Modül çevrim süresi | T3: gereksinim onayı (23.09) → geliştirme tamam (04.10) **11 gün**; kabul henüz yapılmadı | Tahmin için ilk veri. Plan, 3–4 modülden sonra anlamlı bir tahmin verileceğini söylüyor |
| Açık / kapalı risk | 26 kayıt: **20 açık veya izleniyor**, 6 kapalı. Puanı 6 ve üzeri: 9 | §4 |
| Test kapsamı | Backend **%92,9** (eşik %75), Domain **%98,9** (eşik %90), ön yüz **%88,5** | `v0.2.0-rc.2`; 770 backend + 233 ön yüz testi, tamamı geçti |
| Kabul testinde çıkan bulgu | Henüz kabul yapılmadı | |
| Değişiklik talebi | **1** (#77, onaylı kurala yönelik ayrıntı) | Kapsam kayması görülmüyor (R-04) |

**Ek göstergeler:**
- 87 birleştirilmiş PR.
- 14 hata kaydı; hepsi kapandı.
- 6 düzeltici faaliyet; hepsi kapandı.
- 102 kayıtlı karar (`KR-001`…`KR-102`).

## 4. Riskler (puanı 6 ve üzeri)

| Risk | Puan | Durum | Not |
|---|---|---|---|
| R-18 TLS sertifikasının elle yenilenmesi | 9 | Açık | Bitiş 16.12.2026; kalıcı önlem Y4 modülünde (#42) |
| R-01 Veri göçü | 6 | Açık | Göç aşamasında (AS) |
| R-03 KVKK yükümlülüğü | 6 | Açık | T3'te maskeleme, erişim kaydı ve denetim izi uygulandı |
| R-04 Kapsam kayması | 6 | Açık | 1 değişiklik talebi |
| R-05 Tek kişiye bağımlılık | 6 | İzleniyor | Kararlar, ADR'ler ve süreç belgeleri yazılı |
| R-06 Mevzuat değişikliği | 6 | Açık | İzin modülünde ele alınacak |
| R-15 Kapsam büyüklüğü | 6 | Açık | Çevrim süresi ölçülmeye başlandı (§3) |
| R-20 E-posta sunucusu sertifikası | 6 | İzleniyor | 06.10.2026'da yenilendi; bitiş 27.03.2027 |
| **R-25 UAT'nin gerçek kişisel veriyle çalışması** | 6 | Açık | **Üst Yönetim'in kabul kararı gerekiyor (§5)** |

## 5. Üst Yönetim'den beklenen kararlar

| # | Karar | Neden | Belge |
|---|---|---|---|
| 1 | **Proje planı (0.3) ve roller ile sorumluluklar (RACI, 0.2) onayı** | Plan, onaylanmadan yürürlüğe girmez (33061 MAN.1). İlk sürüm 07.09.2026'dan beri onay bekliyor | `MAN.1-proje-planlama/kayitlar/2026-10-06-plan-ve-raci-onay-talebi.md` |
| 2 | **R-25 risk kabulü:** Test ortamının gerçek personel verisiyle çalışması | Puan 6 ve üzeri riskleri RACI'ye göre Üst Yönetim kabul eder. Kabul testlerinin gerçek personelle yapılabilmesi için gerekli; önlemler risk defterinde | `MAN.4-risk-yonetimi/risk-kayit-defteri.md` R-25 |
| 3 | **Üretim altyapısı:** üretim sunucusu ve PostgreSQL sunucusu (B2, B4), yedekleme düzeni (B5, B9) | Geçiş aşamasının (AS) ön koşulu. Kesim tarihi Kasım 2026'dan sonraya kalırsa mevcut sistem desteksiz PostgreSQL sürümünde çalışır (R-09) | Plan §6.3 |
| 4 | **Başarı ölçütlerinin teyidi** (plan §1) | Hedefler ölçülebilir yazıldı; Üst Yönetim'le teyit edilmedi | Plan §1 |

## 6. Sonraki dönem (en geç 06.11.2026)

1. **T3 kabulü (G3):** İK katılımcıları belirler; kabul oturumu yapılır; kabul edilirse `v0.2.0` etiketlenir.
2. **T3 kapanışı (G4):** Kapanış gözden geçirmesi, alınan dersler, modül sonu süreç denetimi.
3. **Sıradaki modül:** İK gereksinim toplantısı (T1, T2, T4 veya T5).
4. **Bu raporun ikincisi:** T3 kapanışında veya en geç bir ay sonra.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-06 | 1.0 | İlk durum raporu (#127) | Bilgi İşlem |
