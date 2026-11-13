# Proje Durum Raporu — 3

**Belge kimliği:** MAN.2-DR-2026-11-12
**Süreç:** MAN.2 — Proje Değerlendirme ve Kontrol
**Dönem:** 13.10.2026 – 12.11.2026
**Hazırlayan:** Bilgi İşlem · **Sunulan:** Üst Yönetim · **İletim:** Doğuş Uçanok iletir; iletim tarihi ve cevap §7'ye işlenir
**Sıklık:** Her modül kapanışında ve en geç ayda bir (`KR-101`). Bu rapor T1 geliştirmesinin sonunda, aylık sınırdan (13.11) bir gün önce yazıldı. Önceki: `2026-10-13-durum-raporu.md`.

---

## 1. Özet

- **T1 Personel Yönetimi'nin geliştirmesi tamamlandı.** 45 paydaş gereksinimi ve 79 sistem gereksinimi kodlandı; 77'si testle veya ölçümle kanıtlı. T1, 09.11.2026'da UAT'ye kuruldu; beş performans göstergesinin hepsi hedefin çok altında. Sırada İK kabulü var.
- **Bir risk gerçekleşti: R-09.** Mevcut sistemin PostgreSQL 14 desteği **12.11.2026'da bitti**. Üretim altyapısı için hedef tarih hâlâ yok (§6).
- **Geliştirme sonu süreç denetimi yapıldı.** 15 sürecin hepsi Seviye 1'de kaldı; dokuzu ilerledi, dördü geriledi. 13.10 denetiminin 13 düzeltici işi kapandı; yeni 10 düzeltici iş açıldı (#386–#395).
- **UAT TLS sertifikası 16.12.2026'da bitiyor.** Yenileme elle yapılıyor ve T1 kabul takvimiyle çakışabilir (#386).

## 2. Aşama ve modül durumu

| Aşama | Durum | Sürüm | Not |
|---|---|---|---|
| A0 — Hazırlık ve planlama | ✅ Tamamlandı | — | Milestone hâlâ açık (#392) |
| A1 — Teknik iskelet | ✅ Tamamlandı (12.09.2026) | `v0.1.0` | |
| A2 — Temel modüller | 🔄 Sürüyor | `v0.2.0` | Aşağıda |
| A3 — Yatay altyapı | ⏳ Başlamadı | — | 3 gereksinim kaydı bekliyor (#3, #42, #189) |
| A4+ — İş modülleri | ⏳ Başlamadı | — | |
| AS — Geçiş | ⏳ Başlamadı | `v1.0.0` | Üretim altyapısı için hedef tarih yok (§6) |

**A2 modülleri:**

| Modül | Durum | Kapılar |
|---|---|---|
| T3 Kimlik Yönetimi | ✅ Kabul edildi ve kapandı — `v0.2.0` | G1 23.09 · G2 04.10 · G3 13.10 · G4 13.10 |
| T1 Personel Yönetimi | 🔄 **Geliştirme tamamlandı; UAT'de** (`v0.2.0-89-g6dbe9d5`). Kabul hazırlığı planlanacak (#388) | G1 ✅ 19.10 · G2 ⏳ · G3 ⏳ · G4 ⏳ |
| T2 Organizasyon, T4 Rol ve Yetki, T5 Kullanıcı | İK gereksinim toplantısı bekliyor (`KR-068`) | — |

## 3. Ölçütler (plan §8.1)

| Ölçüt | Değer | Yorum |
|---|---|---|
| Modül çevrim süresi | T1: gereksinim onayı → geliştirme sonu **24 gün** (19.10 → 12.11); kabul henüz yok | T3: onay → kabul 20 gün. T1, T3'ün yaklaşık iki katı gereksinim taşıyor (45 PG, 79 SYG) |
| Açık / kapalı risk | 32 kayıt: **10 açık**, 13 izleniyor, 3 kabul edildi, 6 kapalı. Puanı 6 ve üzeri: 10 | §5 |
| Test kapsamı (`main` @ `388da21`) | Arka uç **%91,3** (eşik %75), Domain **%98,7** (eşik %90), ön yüz satır **%84,2** | 1.253 arka uç + 382 ön yüz testi ve 22 uçtan uca senaryo, tamamı geçiyor |
| Performans (UAT) | Liste p95 **0,18 sn**, kart **0,045 sn**, kan grubu süzgeçli liste **0,25 sn** (hedef < 2 sn); senkronizasyon p95 **1,16 sn** (hedef < 2 dk) | `TEC.9-dogrulama/raporlar/2026-11-10-t1-performans-olcumu.md` |
| Kabul testinde çıkan bulgu | — | T1 kabulü henüz yapılmadı |
| Değişiklik talebi | **9** (#270, #284, #321, #333, #344, #348, #352, #355, #374) | Dördünde İK onayı yok veya birleştirmeden sonra kaydedildi (#387) |

**Kalite güvence ölçütleri (MAN.8, dönem içinde ilk kez ölçüldü):**

| # | Ölçüt | Değer | Hedef |
|---|---|---|---|
| K2 | 13.10 denetim işlerinin kapanma süresi | Medyan 0,80 gün, en uzun 2,84 gün; 13/13 kapalı | Medyan ≤ 7 gün ✅ |
| K3 | `main` CI başarı oranı | %93,1 (81/87); 5 kırmızı olayın 5'i otomatik kayıtlı | ≥ %90 ✅ |
| K4 | PR'ın ilk CI çalıştırmasında geçme oranı | %89,7 (78/87) | ≥ %90 ⚠️ |
| K5 | Kırmızıyla birleştirme / birleştirmeden önce onay | 0 / 89 PR'ın 89'u | 0 / %100 ✅ |
| K6 | Kaçan hata (birleştikten sonra insan tarafından bulunan ürün hatası) | T1: **5** (#306, #315, #338, #357, #373); T3 kabulünden sonraki 30 gün: 0 | 0 ⚠️ (#395) |

**Ek göstergeler:**
- Toplam 190 birleştirilmiş PR; bu dönemde 87.
- Toplam 33 hata kaydı; bu dönemde 17, hepsi kapalı.
- 138 kayıtlı karar (`KR-001`…`KR-138`); bu dönemde 32.

## 4. Rol ve kaynak yeterliliği

- **Geliştirme ve belgelendirme** Bilgi İşlem Birim Sorumlusu ile yapay zekâ destekli geliştirme aracı (Claude) tarafından yürütülüyor. T1 hızında yeterli. Ancak R-05 (tek kişiye bağımlılık) sürüyor: bütün onaylar ve UAT işletimi tek kişide.
- **İK katkısı:** gereksinim toplantısı (19.10) ve değişiklik talepleri. Dört talepte İK onayı kayda geçmedi (#387); kabulde İK'nın zamanı gerekecek.
- **Altyapı:** UAT sunucusu (2 çekirdek) T1 yükünü taşıyor (R-24). Üretim sunucusu için kaynak ayrıldı (`KR-103`), tarih yok.
- **Bağımsız inceleme:** PR'ları kodu yazan taraftan farklı bir kişi (Doğuş Uçanok) onaylıyor; 89/89. Büyük PR'larda neyin incelendiği yazılmıyor (#390).

## 5. Riskler (puanı 6 ve üzeri)

Risk defteri bu raporla gözden geçirildi (sürüm 1.16). Yeni risk: **R-32** (özel nitelikli veri anahtarının kaybı veya sızması, puan 3). R-31 6'dan 3'e düştü.

| Risk | Puan | Durum | Not |
|---|---|---|---|
| R-18 TLS sertifikasının elle yenilenmesi | 9 | Açık | **Bitiş 16.12.2026**; yenileme #386; kalıcı önlem A3'te (#42) |
| **R-09 PostgreSQL 14 desteğinin bitmesi** | 6 | **Gerçekleşti (12.11.2026)** | **Üst Yönetim kararı bekleniyor (§6)** |
| **R-30 Kayıt dışı gizli bilgi kopyaları** | 6 | Açık — **yeniden gerçekleşti** | UAT'de 03.11 tarihli sır kopyası bulundu (#386, #391) |
| R-01 Veri göçü | 6 | Açık | Göç aşamasında (AS) |
| R-03 KVKK yükümlülüğü | 6 | Açık | Kan grubu UAT'de şifreli (`KR-125`); gizlilik taahhüdü kayıtları doğrulanacak (#388) |
| R-04 Kapsam kayması | 6 | Açık | 9 değişiklik talebi; dördünde İK onayı eksik (#387) |
| R-05 Tek kişiye bağımlılık | 6 | İzleniyor | §4 |
| R-06 Mevzuat değişikliği | 6 | Açık | İzin modülünde ele alınacak |
| R-15 Kapsam büyüklüğü | 6 | Açık | T1 çevrimi 24 gün (kabul hariç) |
| R-25 UAT'nin gerçek kişisel veriyle çalışması | 6 | Kabul edildi | Üst Yönetim, 09.10.2026 (`KR-103`) |

**Puanı düşen risk:** R-31 (denetim kayıtlarının korunması) 6'dan 3'e indi; çalışma rolü ayrımı UAT'de etkin.

## 6. Üst Yönetim'in bilgisine ve kararına sunulanlar

| # | Konu | Neden | İstenen |
|---|---|---|---|
| 1 | **Üretim altyapısı için hedef tarih** (yeni PostgreSQL sunucusu) | **R-09 gerçekleşti:** mevcut sistem 12.11.2026'dan beri desteksiz PostgreSQL 14'te çalışıyor. 13.10'da tarih istenmişti; kayıtlı bir karar yok. Geçiş (AS) bu tarihe bağlı | Hedef tarih; tarih verilemiyorsa desteksiz dönemin kabulü (risk kabulü, puan 6 → Üst Yönetim yetkisi) |
| 2 | **İmzaların proje sonuna ertelenmesi** (`KR-107`) | Kararı kanıtı üreten birim (Bilgi İşlem) tek başına aldı; T3 kabul formu ve Üst Yönetim onay formu imzasız. Bütünlük riski özetle giderildi | Teyit veya imza takvimi |
| 3 | **Bütçe** | Planda bütçe kalemi yok; A0 açık maddesi (#392) | Bilgi; gerekirse bütçe kararı |
| 4 | **T1 kabul takvimi** | Geliştirme bitti; İK'nın kabul için zaman ayırması gerekiyor | Bilgi; tarih kabul planıyla (#388) önerilecek |

## 7. Sonraki dönem (en geç 12.12.2026)

1. **T1 kabul hazırlığı** (#388): plan ve hedef tarih, doğrulama raporu ve G2, kabul planı ve senaryolar, İK ön denemesi, ilk kabul adayı.
2. **Yüksek öncelikli düzeltici işler:** UAT sır kopyası, yeniden başlatma ve TLS (#386, 16.12'den önce); değişiklik taleplerinde İK onayı (#387) ve ADR güncelliği (#389) kabul adayından önce.
3. **Sıradaki modül kararı:** T2, T4 veya T5 için İK gereksinim toplantısı (`KR-068`).
4. **Bu raporun dördüncüsü:** T1 kabulünde veya en geç 12.12.2026'da.

**İletim kaydı:** _(Doğuş Uçanok doldurur: iletim tarihi, kime, cevap ve tarihi.)_

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-11-12 | 1.0 | Üçüncü durum raporu; T1 geliştirme sonu (#385) | Bilgi İşlem |
