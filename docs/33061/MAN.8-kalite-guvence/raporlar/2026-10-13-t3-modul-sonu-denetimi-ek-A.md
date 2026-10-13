# 33061 Modül Sonu Süreç Denetimi — Bulgular A (MAN.1, MAN.2, MAN.4, MAN.5, MAN.6)

**Değerlendirme tarihi:** 2026-10-13
**Dönem:** 02.10.2026 T3 süreç denetimi (MAN.8-SGR-2026-10-02, `main` @ `4199526`) sonrası. Kapsam: düzeltici iş planı, T3 kabul adayları ve İK kabulü. PR #118–#205, 45 commit, 02.10–13.10.2026, `main` @ `d354e94`. T3, İK tarafından 13.10.2026'da kabul edildi (G3, `KR-105`). Kabul edilen commit `149f6ee` `v0.2.0` olarak etiketlendi.
**Dayanak:** TS ISO/IEC TS 33061 Madde 5.4.2, 5.4.3, 5.4.5, 5.4.6, 5.4.7 · TS ISO/IEC 33020 Madde 5.2.4.2 (PA 2.1 a–g), 5.2.4.3 (PA 2.2 a–e), 5.3 ölçek
**Yöntem:**
- Salt okuma. Kaynaklar: depo, `git log`/`git tag -n2`, `gh issue/pr/run/api` çıktıları.
- Depo dışı konumlarda yalnızca dosya ve klasör **adları** listelendi:
  - `Masaüstü\HRMS`;
  - UAT `/opt/dsg-hrms`, `/opt/dsg-hrms/secrets`, `docker volume ls`.
  - Hiçbir dosyanın içeriği açılmadı.
- "Planlandı", "yapılacak" ve çalışma kopyasında duran, birleşmemiş taslaklar kanıt sayılmadı.
- Ölçek: N (%0–15) · P (>15–50) · L (>50–85) · F (>85–100). `−/+` alt ayrımı 00-OLGUNLUK §2'ye göredir.

> **Bekleyen, kanıt sayılmayanlar (13.10.2026):**
> - Modül kapanışı nedeniyle (`KR-101`) şu an vadesi gelmiş işler var: ikinci durum raporu, T3 kapanış risk gözden geçirmesi, kapanış gözden geçirmesi (G4) ve alınan dersler.
> - Kapanış gözden geçirmesi ve alınan dersler için `MAN.2-…/kayitlar/` altında iki taslak dosya çalışma kopyasında duruyor. Taslaklar commit edilmemiş ve birleşmemiş.
> - `MAN.5-…/raporlar/2026-10-13-…` modül sonu konfigürasyon denetimi ayrı olarak yazılıyor.
> - Bu dört iş bu ekte "bekliyor" olarak gösterildi; derecelere katkı yapmadı.

---

## 0. Ortak gözlemler (beş sürecin hepsini etkileyen)

| # | Gözlem | Kanıt |
|---|---|---|
| O-1 | **Yönetim süreçleri bu dönemde iş üretti.** 02.10 denetiminin 18 düzeltici işi issue'ya çevrildi (#119–#136, 03.10). Hepsi 03–10.10 arasında kapandı. 02.10'daki kök neden ("bulgular issue'ya dönüştürülmüyor") giderildi. MAN etiketli işler: `surec:MAN.2` #127, `surec:MAN.4` #126, `surec:MAN.5` #125/#132/#179, `surec:MAN.6` #130/#131/#174/#177. `surec:MAN.1` etiketli issue yine yok; plan revizyonu #127 (MAN.2) altında yapıldı. | `gh issue list --search "created:>=2026-10-02"` |
| O-2 | **Etiket disiplini 04.10'dan sonra çöktü.** Bu tarihten sonra açılan 23 issue'nun yalnızca 5'i `tur:` + `surec:` + `oncelik:` üçlüsünü taşıyor (#146, #149, #152, #175, #189). Etiketsiz olanlar: #154, #157, #160. Yalnızca `surec:` taşıyanlar: #187, #192, #196, #198, #200, #202, #204. #153–#191 arasındaki PR'ların hiçbiri etiket taşımıyor. Sonuç olarak MAN.2'nin etiketle saydığı ölçütler eksik sayılıyor: #198 `[HATA]` başlıklı ama `tur:hata` yok, #196 `[DÜZELTİCİ]` başlıklı ama `tur:duzeltici-faaliyet` yok. Kural: CONTRIBUTING §10, MAN.1 YAKLASIM §2.5. | `gh issue list`, `gh pr list --json labels` |
| O-3 | **"Son güncelleme" ile değişiklik geçmişi 13 belgede uyuşmuyor.** Başlık tarihi, geçmişteki son satırdan eski. Bu ekin kapsamındakiler: `bilgi-kayit-defteri.md` (başlık 05.10, geçmiş 1.3 = 13.10), `karar-kayit-defteri.md` (04.10 / 13.10), `dal-koruma-telafi-kontrolleri.md` (09.09 / 04.10; **02.10'da bulgu olarak yazılmıştı, düzelmedi**), `konfigurasyon-ogeleri.md`, MAN.2/MAN.4/MAN.6 `YAKLASIM.md`. Kapsam dışında kalanlar: `izlenebilirlik-matrisi.md`, `PG-KMLK.md`, `SYG-KMLK.md`, `adr/README.md`, `CONTRIBUTING.md`, `paket-envanteri.md`. Kuralı (harita §4.4, MAN.6 YAKLASIM §2.1) denetleyen bir araç yok. | Başlık–geçmiş karşılaştırma betiği (salt okuma) |
| O-4 | **Yaklaşım belgeleri 05.10'da yazıldı (#130), modül sonunda gözden geçirilmedi.** "Bugünkü durum" ve "Açık noktalar" tabloları kapanmış işleri açık gösteriyor. Örnekler: MAN.1 §7 #1 (onay 09.10'da alındı), MAN.2 §2.2 ("G3: `v0.2.0-rc.2` kabul bekliyor"), MAN.4 §2.4 ("R-25: kabul için Üst Yönetime sunulacak"), MAN.5 §2.3/4 ("`v0.2.0` T3 kabulünü bekliyor"), MAN.6 §8 #1 (Ö-1, 06.10'da kapandı). Aynı durum RACI §7 iş 1'de de var: RACI teyidi `KR-103` ile yapıldı ama iş hâlâ açık görünüyor. Her YAKLASIM'ın kendi §8/§9'u "her modül sonu süreç denetiminde gözden geçirilir" diyor; bu denetim o gözden geçirmenin tetikleyicisidir. | `docs/33061/MAN.*/YAKLASIM.md` |
| O-5 | **Kayıtlı inceleme sorunu (02.10 O-4) çözüldü.** `KR-096` (#128, PR #148) ile onay yorumu birleştirmeden önce yazılıyor ve CI bunu denetliyor. Bu dönemde birleşen PR'larda düzeltici issue açılmadı. Örnek olarak denetimin çalışmadığı iki PR (#169, #172) elle kontrol edildi: ikisinde de onay yorumu birleştirmeden önce yazılmış (bkz. MAN.5 A-03). | `gh run list --workflow branch-protection-check.yml`, `gh pr view 169/172 --json comments` |

---

## 1. MAN.1 — Proje Planlama

**Amaç (özet):** Etkili ve uygulanabilir planlar üretmek ve koordine etmek; planlar proje boyunca **düzenli revize edilir**.

### 1.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Hedefler ve planlar tanımlanır | **Plan revize edildi:** `proje-plani.md` 0.3 (06.10, #127, PR #181) `KR-077` sırasını, `SYG-` kimliğini, UAT'nin gerçek veriyle çalışmasını, B2/B3/B8/B9 durumlarını ve durum raporu sıklığını (`KR-101`) yansıtıyor. Plan setinin diğer belgeleri: WBS 0.4 (§4 kritik yol: T3 önce), RACI 0.2. **Kalan zayıflıklar:** (1) BP2.1'in istediği çizelge veya iş tahmini yok ("tarih değil sıra" bilinçli bir karar; yine de B2/B4 için bile hedef tarih yok). (2) Plan §5.2 "T3 … İK kabulü bekliyor, `v0.2.0-rc.2`" diyor; 13.10 kabulü henüz işlenmedi (G4 ile beklenir). | **L+** |
| **b)** Roller, sorumluluklar, hesap verebilirlik, yetkiler tanımlanır | RACI 0.2 Üst Yönetim tarafından onaylandı (`KR-103`, 09.10). Bu dönemde RACI fiilen uygulandı: (1) puanı 6 olan R-25'i Üst Yönetim kabul etti; (2) puanı 2 olan R-19'u Bilgi İşlem kabul etti ve puan gerekçesi yazıldı; (3) G3 kararını İK verdi (`KR-105`); (4) değişiklik taleplerini İK onayladı (#185, #190). 02.10'daki `KR-078` yetki ihlali giderildi. | **F** |
| **c)** Kaynaklar resmen talep edilir ve taahhüt edilir | **Talep:** Onay formu karar 3, 06.10. **Taahhüt:** "Kaynak ayrıldı; mevcut kaynaklar kullanılacak, yeni sistem için güncel bir PostgreSQL kurulacak" (`KR-103`, 09.10). **Eksikler:** Hedef tarih yazılmadı. B5/B9 (yedekleme, RPO/RTO) için karar yok. 33061 çıktısı *Project budget* hâlâ yok; planda "bütçe" veya "maliyet" kelimesi geçmiyor. | **L** |
| **d)** Planlar yürürlüğe konur (activated) | MAN.1.BP3.1 karşılandı: proje onayı Elvan Laleli Şahin (Yönetim Kurulu Üyesi), 09.10.2026. Onay kaydı: `kayitlar/2026-10-06-plan-ve-raci-onay-talebi.md` 1.1 (*Project planning record*). Plan başlıklarına onay işlendi (PR #184). Kalan tek eksik: `A0 — Hazırlık ve Planlama` milestone'u açık işi kalmadığı hâlde hâlâ açık. | **F** |

**PA 1.1 = L+** (02.10: L−). **İyileşme.** Plan revize edildi, onaylandı ve uygulandı. F için kalan: bütçe ve tarihli kaynak taahhüdü (c).

### 1.2 Temel uygulamalar (BP)

- **BP1:**
  - ✔ Proje tanımı (hedef, kısıt, kapsam, yaşam döngüsü, WBS).
  - ✔ BP1.5: uygulanacak süreçler, 15 YAKLASIM ile (#130).
- **BP2:**
  - ✘ BP2.1 takvim.
  - ✔ BP2.2 kapılar; G1–G3 T3'te işletildi.
  - ✘ BP2.3 bütçe.
  - ✔ BP2.4 roller.
  - ✔ BP2.5 altyapı.
  - ✔ BP2.7 planın duyurulması: Word onay seti Üst Yönetime iletildi (BK-26).
- **BP3:**
  - ✔ BP3.1 onay.
  - ◐ BP3.2 kaynak taahhüdü: tarihsiz.
  - ✔ BP3.3 uygulama.

### 1.3 PA 2.1 / PA 2.2

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L+** (02.10: L−) | **(a)** YAKLASIM §2.6 revizyon tetikleyicilerini tanımlıyor ✔. **(b)** `R-15`, `R-05` ✔. **(c)** Plan, sapma karşısında revize edildi (0.3) ✔. Ancak iş kalemlerinin etiketlenmesi bozuldu (O-2). Milestone kuralında belirsizlik var: WBS §5.2 süreç işini `SK`'ya atıyor, CONTRIBUTING §5 bunu saymıyor. Fiilen #174/#177/#179 `SK` altında, #125–#133 `A2` altında açıldı (YAKLASIM §7 #7, issue yok). **(d)** RACI onaylı ve uygulanıyor ✔. **(e)** Kaynak kararı var ama tarihsiz. **(f)** Yetkinlik tablosu güncellendi (RACI 0.2, 06.10) ✔. **(g)** Üst Yönetim arayüzü çalıştı: talep 06.10 → karar 09.10 ✔. |
| PA 2.2 | **L** (02.10: P+) | **(a)/(b)** MAN.1 YAKLASIM ✔. **(c)** Plan belgelerinin başlık ve geçmişi tutarlı ✔. YAKLASIM §2.4 ve §7 ise bayat (O-4). **(d)** Plan seti onaylı ✔; PR onayları (`KR-096`) ✔. **(e)** Onayın asıl nüshası (doldurulmuş Word formu, imza hücreleri boş) yalnızca `Masaüstü\HRMS` altında duruyor (BK-26). Kalıcı konumu yok (Ö-3), yedeği yok (MAN.6 §8 #6). Plan revizyonu için YAKLASIM §2.6'nın öngördüğü `kayitlar/…-plan-revizyonu.md` yazılmadı. Değişiklik özeti onay talebi §1'de duruyor; bu kabul edilebilir ama adlandırma kuralına uymuyor. |

### 1.4 02.10 bulgularının durumu

| 02.10 bulgusu / eylemi | Durum |
|---|---|
| Plan v0.2 revizyonu (`KR-077`, SYG, UAT gerçeği, B3/B8) | ✅ Kapandı: plan 0.3, WBS 0.4 (06.10, PR #181) |
| Plan, vizyon-kapsam ve RACI onayı; ardından A0'ın kapatılması | ⚠️ Kısmen. Plan, RACI ve WBS onaylandı (`KR-103`). **A0 milestone'u hâlâ açık.** Vizyon ve kapsam belgesinin İK onayı bu dönemde ele alınmadı. |
| T3 kalan çevrim adımları için issue'lar | ✅ Kapandı: #123 (G2), #124 (kabul planı), #125 (sürüm), #204 (G3) |
| MAN.1 YAKLASIM | ✅ Kapandı: #130 (05.10). Modül sonunda gözden geçirilmedi (O-4) |
| Yetkinlik tablosu güncellemesi | ✅ Kapandı: RACI 0.2 §4.1 |
| Bütçe/maliyet kalemi | ❌ Açık; issue yok |
| BULGU-11 (plan ve RACI onayı) | ✅ Kapandı (`KR-103`) |
| Milestone kuralı ihlali / öncelik etiketi eksikliği | ❌ Büyüdü (O-2) |

### 1.5 Yeni bulgular ve düzeltici eylemler

| Öncelik | Bulgu → eylem (issue olacak büyüklükte) |
|---|---|
| **Orta** | **A-05:** Etiket disiplini çöktü (O-2). Üç adım: (1) Issue şablonlarına varsayılan `tur:` etiketi eklenir. (2) `pr-traceability-check.yml` bağlı issue'nun `tur:`, `surec:`, `oncelik:` etiketlerini denetler. (3) #146 sonrası issue'lar geriye dönük etiketlenir. |
| **Orta** | **A-08 (MAN.1 payı):** Modül kapanışında plan seti güncellenir: §5.2 T3 = `v0.2.0`, kabul 13.10. MAN.1 YAKLASIM §2.4 ve §7 güncellenir; RACI §7 iş 1 kapatılır. |
| **Düşük** | **A-10:** `A0` milestone'u kapatılır (onay 09.10). |
| **Düşük** | **A-11:** Plan §6'ya maliyet ve bütçe alt bölümü eklenir. İçerik "lisans 0, TSE belgeleri, sunucu kaynakları" düzeyinde olabilir. B2/B4 için hedef tarih ikinci durum raporunda Üst Yönetimden istenir. |
| **Düşük** | **A-15:** Süreç işlerinin milestone'u için karar alınır: `SK` mı, denetimi tetikleyen aşama mı? Karar WBS §5.2 ve CONTRIBUTING §5'e birlikte yazılır (önerilen karar metni §8.3'te). |

---

## 2. MAN.2 — Proje Değerlendirme ve Kontrol

**Amaç (özet):** Planların uyumlu ve uygulanabilir olduğunu değerlendirmek, durumu belirlemek, sapmaları düzeltmek. Değerlendirme **dönemsel olarak ve önemli olaylarda** yapılır.

### 2.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Performans ölçütleri veya değerlendirme sonuçları mevcut | Durum raporu 1 (`raporlar/2026-10-06-durum-raporu.md` §3) plan §8.1'deki beş ölçütün gerçek değerini veriyor: çevrim 11 gün, risk 26 kayıt (20 açık, 6 kapalı), kapsam %92,9 / %98,9 / %88,5, değişiklik talebi 1. Ek göstergeler: 87 PR, 14 hata. **Eksik:** Ölçütlerin hedef değeri yok (YAKLASIM §3). Etiketle sayılan göstergeler O-2 nedeniyle eksik sayılıyor. | **L** |
| **b)** Rol ve sorumlulukların yeterliliği değerlendirilir | YAKLASIM §2.5 madde 5 "rol ve kaynak yeterliliği" başlığını istiyor; durum raporunda bu başlık **yok**. Tek kişilik geliştirme ve inceleme yalnızca risk tablosunda tek satırla geçiyor (R-05 "İzleniyor"). R-23 rapora girmemiş. | **P+** |
| **c)** Kaynak yeterliliği değerlendirilir | Üretim altyapısı ihtiyacı (B2, B4, B5, B9) ve R-09 bağlantısı karar 3 olarak sunuldu ve karara bağlandı (`KR-103`). UAT kapasitesi (R-24) ayrıca ölçüldü. Değerlendirme ayrı bir başlık hâlinde değil. | **L−** |
| **d)** Teknik ilerleme gözden geçirmeleri yapılır | (1) 02.10 süreç denetimi. (2) G2 doğrulama raporu, 04.10 (#123). (3) İlk konfigürasyon denetimi, 05.10 (#132). (4) Entegrasyon raporu, 05.10 (#134). (5) Kabul provası bulguları, 11.10 (#196). (6) G3 kabul oturumu, 13.10. Kapanış gözden geçirmesi (G4) **bekliyor**. | **L+** |
| **e)** Plandan sapmalar incelenir ve analiz edilir | **Sıra sapması:** `KR-077` plan 0.3'te analiz edildi ✔. **Kapsam sapmaları:** #185 ve #190 etkilenen onaylı gereksinimleri (REQ-KMLK-038/039, 052) adıyla sayıyor ✔. **Performans sapması:** SYG-KMLK-077 (#120, `KR-095`) ✔. | **L+** |
| **f)** Etkilenen paydaşlar proje durumundan haberdar edilir | Durum raporu 1, onay setiyle birlikte Word olarak Üst Yönetime iletildi (`Masaüstü\HRMS\Ust_Yonetim_Onay_Seti_2026-10\01_Durum_Raporu.docx`, 06.10). Kararlar üç günde döndü (`KR-103`). **Eksik:** YAKLASIM §2.5 "iletim tarihi ve alıcı rapora yazılır" diyor; raporda yalnız "Sunulan: Üst Yönetim" var. İletim, onay kaydı §4'ten çıkarılabiliyor. | **L** |
| **g)** Hedef tutmadığında düzeltici faaliyet tanımlanır ve yönlendirilir | 02.10 denetiminin 18 işi issue'ya çevrildi ve hepsi kapandı. Bunlara #128 ve #129 düzeltici faaliyetleri de dahil. #196 (kabul planı prova bulguları) aynı gün kapandı. Kapanış hızı ve kaydı örnek nitelikte. | **F** |
| **h)** Gerektiğinde yeniden planlama başlatılır | Plan 0.3 revizyonu yapıldı (`KR-077`). Kabul adayı düzeltmeleri `rc.N+1` ile yeniden planlandı (`KR-097`: rc.3, rc.4, rc.5). Değişiklik talepleri kabul adayına alındı. | **L+** |
| **i)** Bir sonraki kilometre taşına geçiş yetkilendirilir | G2 GEÇTİ (04.10, doğrulama raporu §5). G3 kabul edildi (13.10, kabul formu, `KR-105`, Elvan Laleli Şahin). Proje onayı alındı (`KR-103`). G4 **bekliyor**. | **F** |
| **j)** Proje hedefleri başarılır | İlk kullanıcı modülü koşulsuz kabul edildi: 56/56 onaylı gereksinim, 11/11 senaryo, `[HATA]` kaydı yok. Proje düzeyinde hedefler (H1–H7) henüz ölçülebilir aşamada değil. Alınan dersler **bekliyor**. | **L** |

**PA 1.1 = L** (02.10: P). **Belirgin iyileşme.** İlk durum raporu yazıldı ve karar üretti. Düzeltici faaliyet döngüsü tam işledi. F için kalanlar: (b)/(c) yeterlilik değerlendirmesinin ayrı başlık hâline gelmesi, ölçütlere hedef değer verilmesi, ikinci raporla birlikte düzenin sürdüğünün gösterilmesi.

### 2.2 Temel uygulamalar

- **BP1** (değerlendirme ve kontrol stratejisi): ✔ YAKLASIM (#130), `KR-101` sıklığı.
- **BP2:**
  - ✔ BP2.6 ilerlemenin ölçülmesi.
  - ✔ BP2.7 gözden geçirme ve denetimler.
  - ✔ BP2.10 durum ve bulguların kaydı.
  - ◐ BP2.4 ve BP2.5 yeterlilik.
  - ✘ BP2.11 süreç yürütmesinin izlenmesi: etiket bozulması (O-2) fark edilmedi.
- **BP3:**
  - ✔ BP3.1 düzeltici eylemler.
  - ✔ BP3.2 yeniden planlama.
  - ✔ BP3.4 geçiş yetkisi (G2, G3).

### 2.3 `KR-101` durum raporu sıklığı

| Kural | Uygulama |
|---|---|
| "Her modül kapanışında ve en geç ayda bir" (`KR-101`, 05.10) | **Rapor 1:** 06.10.2026, dönem 07.09–06.10. **Aylık sınır:** 06.11.2026. **Modül kapanışı:** T3 G3 13.10.2026'da geçti. Bu nedenle **rapor 2'nin vadesi bugün geldi.** Rapor ana denetçi tarafından yazılacak; bu ekte **bekliyor** sayıldı. |
| Rapor risk gözden geçirmesiyle birlikte yapılır (YAKLASIM §2.5) | T3 kapanış risk gözden geçirmesi de **bekliyor** (§3.6, öneriler §8.1). |

Kural bugüne kadar çiğnenmedi. Ancak dönemde tek rapor olduğu için **düzenin sürdüğü henüz gösterilemiyor.** PA 2.1(c) bu nedenle L−'de tutuldu.

### 2.4 PA 2.1 / PA 2.2

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L−** (02.10: P) | **(a)** Süreç ve sıklık tanımlı ✔, ölçütlerin hedef değeri yok. **(c)** İlk rapor taahhüt edilen dönemde yazıldı ✔; ikinci döngü henüz gösterilmedi. Süreç yürütmesinin izlenmesi etiket bozulmasını yakalamadı (O-2). **(d)** Roller ✔. **(e)** Ölçüt derleme aracı yok, değerler elle toplanıyor (YAKLASIM §8 #6). **(g)** Üst Yönetim arayüzü işledi ✔. |
| PA 2.2 | **L−** (02.10: P) | **(a)/(b)** YAKLASIM ✔, `raporlar/` ve `kayitlar/` var ✔. **(c)** Rapor kimlikli ve değişmezlik kuralı yazılı ✔. YAKLASIM §2.2 tablosu ve §8 bayat (O-4); başlık tarihi geçmişin gerisinde (O-3). **(d)** PR onayı ✔. **(e)** Rapor depoda ✔. `kayitlar/` (kapanış, alınan dersler) henüz boş: taslaklar birleşmedi. |

### 2.5 02.10 bulgularının durumu

| 02.10 bulgusu / eylemi | Durum |
|---|---|
| BULGU-04: durum raporu ve ölçütler | ✅ Kapandı: rapor 1 (06.10, #127, PR #181). Ölçütler gerçek değerle verildi |
| MAN.2 YAKLASIM ("dönemsel"in tanımı) | ✅ Kapandı: #130; `KR-101` (#177) |
| MAN.2 klasörünün kaybolması (O-3) | ✅ Kapandı: klasörler `.gitkeep` ile izleniyor (#118) |
| T3 kapanış gözden geçirmesi ve alınan dersler | ⏳ Bekliyor: vadesi geldi (G3, 13.10); taslaklar birleşmedi |
| Ölçütlerin otomatik üretimi | ❌ Açık; issue yok (YAKLASIM §8 #6) |
| Rol ve kaynak yeterliliğinin raporda ayrı başlıkla değerlendirilmesi | ❌ Açık: rapor 1'de başlık yok |
| A1 kapanışının üst yönetime iletimi | ✅ Kapandı sayıldı: rapor 1 §2'de A1 durumu Üst Yönetime sunuldu |

### 2.6 Yeni bulgular ve düzeltici eylemler

| Öncelik | Bulgu → eylem |
|---|---|
| **Yüksek** | **Vadesi gelen işler (`KR-101`):** durum raporu 2, G4 kapanış gözden geçirmesi, alınan dersler. Bu ekin bulgusu değil, modül kapanışının zorunlu adımıdır; T4 (veya sıradaki modül) G1 toplantısından önce birleşmelidir. |
| **Düşük** | **A-12:** Rapor 2'de iki ekleme yapılır: (1) "Rol ve kaynak yeterliliği" başlığı (R-05, R-23, B2/B4 hedef tarihi); (2) üst bilgiye "İletim: tarih, alıcı, kanal" satırı. Rapor şablonu bu iki alanla YAKLASIM §5'e eklenir. |
| **Düşük** | **A-05 (MAN.2 payı):** Ölçütlerin `gh` sorgusuyla derlenmesi. Etiket denetimi (A-05) gelmeden bu sayılar güvenilir değildir. |

---

## 3. MAN.4 — Risk Yönetimi

**Amaç (özet):** Riskleri **sürekli** belirlemek, analiz etmek, ele almak ve izlemek.

### 3.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Riskler belirlenir | **Eklenenler:** T3 gözden geçirmesinde (04.10, #126, PR #164) R-19…R-26 eklendi. `KR-104`'ün kalan riski aynı gün R-27 olarak eklendi (10.10, #190). 06.10'da bulunan Webmin, R-25 önlem 6'ya işlendi (#177, #179). **Kayda girmeyenler:** (1) #171 (05.10): mimari testi hiçbir tipi denetlemeden geçiyordu, yani bir kalite kapısı sessizce etkisizdi. (2) 05.10: Dal Koruma Denetimi iki commit'te hiç çalışmadı, çünkü runner atanamadı (MAN.5 A-03). (3) #149 (04.10) ve #175 (06.10): iki yüksek önemli bağımlılık açığı. 30.09 CVE olayıyla birlikte bu, tekrarlayan bir örüntü. (4) UAT'de kayıt dışı sır kopyası (MAN.6 A-01). (5) `KR-106`: izin listesi kabulden sonra daraltılmıyor, bu da R-25'in maruziyetini uzatıyor. | **L** |
| **b)** Riskler analiz edilir | **Gerekçeli puanlar:** Puan gerekçesi satırı R-19 ve R-27'de var ✔. R-01 tutarsızlığı düzeltildi ✔. **Bayat analizler:** (1) **R-09:** PostgreSQL 14 desteği Kasım 2026'da, yani üç hafta sonra bitiyor. Üretim için hedef tarih yok (`KR-103` karar 3). Kesimin bu tarihten sonraya kalması artık neredeyse kesin; O = 2 ve "kalıntı risk düşük" ifadesi gerçeği yansıtmıyor. (2) **R-20:** Açıklama hâlâ "geçerliliği **11.10.2026**'da bitiyor" diyor. Sertifika 06.10'da yenilendi (bitiş 27.03.2027); puan 2/3/6 yeniden değerlendirilmedi. (3) **R-19:** Puan gerekçesi hız sınırına dayanıyor (SYG-KMLK-059). Üyelik deneme sınırı #185 ile 5'ten 10'a çıktı; gerekçe yeniden değerlendirilmedi. | **L** |
| **c)** Risk ele alma seçenekleri belirlenir, önceliklendirilir, seçilir | Kabul yetkisi kuralı §1'e yazıldı ✔ ve **doğru uygulandı**: R-25 Üst Yönetim (`KR-103`), R-19 Bilgi İşlem (puan 2), R-27 kurtarma önlemiyle seçildi (#190). | **F** |
| **d)** Uygun önlem uygulanır | **Uygulananlar:** R-25 önlemleri: SSH anahtarı ve `root` parolası değişikliği (`KR-102`, #174), Webmin kaldırıldı (#179). R-20 sertifikası yenilendi (#179). R-27 için İK kurtarma işlemi (#190) eklendi; kabulde T-04 "Uygun" bulundu. R-19 kalan riski kabulde İK'ya gösterildi (AN-01 "Uygun", 13.10). **Uygulanmayan:** R-08 kaydı kendisi "ölçüm 22.09'dan beri yenilenmedi; **kabul öncesinde yenilenmeli**" diyor. Kabul 13.10'da yapıldı; yeniden ölçüm kaydı yok (veri envanteri son değişiklik 24.09). | **L+** |
| **e)** Riskler durum değişikliği ve önlem ilerlemesi açısından değerlendirilir | **Yapılanlar:** Resmî gözden geçirme 04.10'da yapıldı. Olay tetikleyicisi çalıştı: 06.10 Webmin ve SMTP, 10.10 R-25 kabulü ve R-27. Risk profili ilk kez Üst Yönetime sunuldu (durum raporu §4; BP2.3 karşılandı). **Bekleyen ve eksik olanlar:** Modül kapanış gözden geçirmesi **bekliyor**. Defterin yapısal kusuru sürüyor: R-17…R-27 §3 özetinin arkasında, R-11 R-13'ten sonra (YAKLASIM §7 #3, issue yok). | **L+** |

**PA 1.1 = L+** (02.10: L+). **Aynı bant, içerik güçlendi:** kabul yetkisi düzeldi, profil üst yönetime ulaştı, olay tetikleyicisi işledi. **F (ve Seviye 2) için kalanlar:** yeni risk adaylarının kayda girmemesi (a) ve bayat puanlar (b). 02.10 raporu §8'deki "MAN.4 bir sonraki tam denetimde Seviye 2" hedefi bu denetimde **gerçekleşmedi**.

### 3.2 Temel uygulamalar

- **BP1:** ✔ Strateji (YAKLASIM, #130), bağlam ve eşik (§1).
- **BP2:**
  - ✔ BP2.1 kabul eşikleri; uygulandı.
  - ✔ BP2.2 risk profili.
  - ✔ BP2.3 profilin paydaşlara sunulması: durum raporu §4.
- **BP3:** ◐ BP3.2 olasılık/etki tahmini: R-09, R-19, R-20 bayat.
- **BP4:**
  - ✔ BP4.2.
  - ✔ BP4.3 kabul edilen riskin izlenmesi: R-25 önlemleri izleniyor.
  - ◐ R-25 önlem 1 metni `KR-106` ile çelişiyor.
- **BP5:**
  - ✔ BP5.1 olay tetikleyicisi.
  - ◐ BP5.3 yeni risklerin sürekli izlenmesi: (a)'daki beş aday.

### 3.3 Risk defterinin gerçeklikle karşılaştırılması (R-19…R-27)

| Risk | Defterde | Gerçek (13.10.2026) | Güncel mi? |
|---|---|---|---|
| R-19 | Kabul edildi (Bilgi İşlem, 25.09); "kalan risk T3 kabulünde İK'ya gösterilir" | Kabul formunda AN-01 "Evet / Uygun" (13.10) ✔. Hız sınırı #185 ile gevşedi | ⚠️ İK'ya gösterim işlenmedi; puan gerekçesi #185'e göre yeniden değerlendirilmedi |
| R-20 | Durum İzleniyor, bitiş 27.03.2027 | Doğru. Açıklama satırında "11.10.2026'da bitiyor" kaldı | ⚠️ Açıklama ve puan bayat |
| R-21 | İzleniyor; önlem (3) "NTP konfigürasyon öğeleri listesine alınacak (#132)" | KÖ-32 olarak listede; D-11 06.10'da sınandı | ⚠️ Önlem (3) gelecek kipte kaldı; uygulandı olarak işaretlenmeli |
| R-22 | İzleniyor | Değişiklik yok | ✔ |
| R-23 | İzleniyor; `KR-096` | Onay denetimi çalışıyor. Denetimin kendisi 05.10'da iki kez hiç koşmadı | ⚠️ "Denetim çalışmayabilir" zayıflığı eklenmeli (A-03) |
| R-24 | İzleniyor; p95 4,42 sn | Değişiklik yok | ✔ |
| R-25 | Kabul edildi (Üst Yönetim, 09.10); önlem 1: "liste … oturumdan sonra daraltılır" | `KR-106` (13.10): katılımcılar listede kalır. UAT'de kayıt dışı sır kopyası bulundu (A-01) | ❌ Önlem 1 `KR-106` ile çelişiyor; önlem 4 ("sırlar kaynak ağacının dışında") geçerli ama eski sır kopyası önlemin kapsamında değil |
| R-26 | İzleniyor | Olay kaydı yazıldı (#129, `TEC.13-bakim/kayitlar/2026-10-04-t3-olay-kayitlari.md`) | ⚠️ Önlem (4) "olay kaydı #129'da yazılır" uygulandı olarak işaretlenmeli |
| R-27 | İzleniyor; kurtarma işlemi eklendi (#190) | Önlem (2) yürürlükte; kabulde T-04 "Uygun", KG-07 otomatik testle gösterildi (13.10) | ⚠️ Kabul kanıtı işlenmeli; puan 1/2/2 uygun |
| R-09 | 2/2/4, "İzleniyor — kalıntı risk düşük" | Destek bitişine yaklaşık 3 hafta kaldı; üretim için hedef tarih yok | ❌ Olasılık 3'e çıkmalı (puan 6, aktif takip) |
| R-08 | "Kabul öncesinde yenilenmeli" | Yenilenmedi | ❌ Önlem gecikti; tarih verilmeli (devreye almadan önce) |

### 3.4 PA 2.1 / PA 2.2

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L+** (02.10: L−) | **(a)** YAKLASIM hedef ve tetikleyicileri tanımlıyor ✔. **(c)** Olay tetikleyicisi işledi ✔. Ancak #171, #175 ve 05.10 runner arızası tetikleyici sayılmadı. **(d)** Kabul yetkisi doğru uygulandı ✔ (02.10'daki ihlal kapandı). **(g)** Üst Yönetim (rapor §4) ve İK'yla (kabul formu AN-01) risk iletişimi kanıtlı ✔. PR şablonunda "risk etkisi" sorusu hâlâ yok (YAKLASIM §7 #5). |
| PA 2.2 | **L** (02.10: L−) | **(a)/(b)** YAKLASIM ✔. **(c)** Bölüm sırası bozuk. YAKLASIM §2.1 "26 kayıt" diyor (27 oldu), §2.4 "R-25 sunulacak" diyor (kabul edildi); başlık tarihi bayat (O-3). **(d)** PR onayları ✔. **(e)** Git ✔. "Son gözden geçirme" satırı ve geçmiş tutarlı ✔. |

### 3.5 02.10 bulgularının durumu

| 02.10 bulgusu / eylemi | Durum |
|---|---|
| T3 risk gözden geçirmesi; R-03, R-08, R-14 güncellemesi; yeni riskler | ✅ Kapandı: #126, PR #164 (04–05.10) |
| `KR-078` risk kabulünün RACI'ye uygun hâle getirilmesi | ✅ Kapandı: R-19 puan 2 ve gerekçe, Bilgi İşlem kabulü; puanı Doğuş Uçanok 05.10'da onayladı |
| R-01 puan tutarsızlığı, özet sayıları, "Kabul edildi" durum değeri | ✅ Kapandı |
| R-17/R-18'in (ve sonrakilerin) §2'ye taşınması (bölüm sırası) | ❌ Açık, büyüdü: artık R-17…R-27 özetin arkasında |
| Risk profilinin dönemsel raporu | ✅ Kapandı: durum raporu §4 |
| MAN.4 YAKLASIM | ✅ Kapandı: #130 |
| PR şablonuna "risk etkisi" satırı | ❌ Açık; issue yok (şablon 25.09'dan beri değişmedi) |

### 3.6 Yeni bulgular ve düzeltici eylemler

| Öncelik | Bulgu → eylem |
|---|---|
| **Yüksek** | **A-07:** T3 kapanış risk gözden geçirmesi, modül kapanışı tetikleyicisiyle **vadesi geldi**. Kapsamı: §3.3'teki güncellemeler; R-09'un yeniden puanlanması (Üst Yönetim gündemine); R-08 yeniden ölçüm tarihi; üç yeni risk adayının kaydı (öneriler §8.1). Durum raporu 2 ile aynı PR'da yapılır (`KR-101`). |
| **Orta** | **A-07b:** Defterin bölüm sırası düzeltilir. R-11 yerine taşınır; R-17…R-27 §2'ye alınır; §3 özet sona gelir. Yalnızca taşıma yapılır, içerik değişmez; ayrı PR ile yapılırsa farkı okunur kalır. |
| **Düşük** | **A-13:** PR şablonuna üç satır eklenir: "Risk etkisi (yeni/değişen `R-NN`)", "CHANGELOG" ve "Bilgi kayıt defteri (depo dışı yeni küme)". |

---

## 4. MAN.5 — Konfigürasyon Yönetimi

**Amaç (özet):** Sistem öğelerini ve konfigürasyonları yaşam döngüsü boyunca yönetmek ve kontrol etmek; ürün ile konfigürasyon tanımı arasındaki tutarlılığı sağlamak.

> **Sınır:** Bu bölüm yalnızca süreç düzeyindedir. Etiket, CHANGELOG, dağıtım ve sunucu
> eşleşmesinin madde madde denetimi ayrı konfigürasyon denetimi raporundadır
> (`MAN.5-konfigurasyon-yonetimi/raporlar/2026-10-13-…`, yazılıyor; bu ekte kanıt sayılmadı).

### 4.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Konfigürasyon yönetimi gerektiren öğeler belirlenir ve yönetilir | `konfigurasyon-ogeleri.md` (#132, 05.10) depo içi ve dışı öğeleri sayıyor (KÖ-01…52). 06.10'da iki kez güncellendi: KÖ-34 SSH, KÖ-35 Webmin, KÖ-43 sertifika. **Eksikler:** (1) UAT'de `secrets/.env.uat.yedek-20260929` var (29.09 tarihli); öğe listesinde ve bilgi defterinde yok. (2) KÖ-22 kaynak kopyası `src`/`docker` sayıyor; aynı betiğin yazdığı `sema.sql`, `global.json`, `assets` anılmıyor (küçük). | **L+** |
| **b)** Konfigürasyon baseline'ları oluşturulur | `KR-097` (04.10) uygulandı. Yedi açıklamalı etiket var: `v0.1.0` (065a7c3, geriye dönük), `v0.2.0-rc.1`…`rc.5`, `v0.2.0`. `v0.2.0` kabulden sonra `rc.5` ile aynı commit'e (149f6ee) konuldu; etiket açıklaması kabul formuna ve `KR-105`'e atıf yapıyor. CHANGELOG'da her etiket için bir bölüm var (7/7). | **F** |
| **c)** Konfigürasyon altındaki değişiklikler kontrol edilir | **Kontroller:** Dal → PR → CI → onay yorumu (`KR-096`, CI denetimli) → squash. Değişiklik talepleri İK onayıyla geldi (#185, #190). **Zayıflık:** 05.10'da iki commit (3f6833d / PR #169, 90a25a9 / PR #172) için Dal Koruma Denetimi **hiç çalışmadı**: "job was not acquired by Runner", çalışmalar 37369395394 ve 37370762481. #129 ile eklenen yeniden deneme ve "denetim yapılamadı" issue'su iş adımının içinde olduğu için devreye girmedi. `main-failure-check.yml` yalnızca "CI" ve "API sozlesme denetimi" iş akışlarını izliyor. Sonuç: kayıt da issue da yok. Bu, 02.10'daki "tespit edici kontrolün sessiz arızası" bulgusunun yeni bir biçimidir. İki PR'da onay yorumunun birleştirmeden önce yazıldığı bu denetimde elle doğrulandı. | **L+** |
| **d)** Konfigürasyon durum bilgisi erişilebilir | Depodaki dağıtım kaydı (`TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`) ile sunucudaki `deployments.log` **5/5 satır eşleşiyor** (rc.1…rc.5, tam commit kimliği). Sunucuda `VERSION` dosyası var. CHANGELOG güncel. Her aday için CI kanıt özeti var (rc.2, rc.3, rc.4, rc.5). rc.3 özeti sonradan, #194 ile tamamlandı. | **F** |
| **e)** Gerekli konfigürasyon denetimleri tamamlanır | İlk denetim 05.10'da yapıldı (`v0.2.0-rc.2`, D-01…D-12). D-11 ve D-12 06.10'da sınandı. **Kural:** YAKLASIM §2.4 "her kabul adayı etiketinde ve her modül kapanışında" denetim istiyor. **rc.3, rc.4 ve rc.5 için denetim yok.** İK'ya sunulan aday rc.5'ti, yani kabule giren yapı denetlenmedi. D-12 yalnızca `.env.uat` dosyasının iznine baktı; aynı dizindeki eski kopyayı yakalamadı. Modül kapanış denetimi **bekliyor**. | **P+** |
| **f)** Sistem sürümleri ve teslimleri kontrol edilir ve onaylanır | Zincir kesintisiz işledi: G2 → sürüm hazırlık PR'ı → `rc.N` etiketi → `deploy-uat.sh` (temiz çalışma kopyası, `git archive`, OCI etiketi, kayıt) → İK kabul formu etikete atıf yapıyor → `v0.2.0`. | **F** |

**PA 1.1 = L+** (02.10: P+). **Belirgin iyileşme:** baseline, sürüm ve durum muhasebesi kuruldu ve işledi. F için kalanlar: denetim kuralının uygulanması (e) ve tespit edici kontrolün altyapı arızasına karşı izlenmesi (c).

### 4.2 Temel uygulamalar

- **BP1:** ✔ Strateji (YAKLASIM). BP1.2 saklama ve geri getirme yordamı: Git ve etiket ✔.
- **BP2:** ✔ Öğe seçimi, baseline tanımı (`KR-097`).
- **BP3:** ✔ Değişiklik talebi ve onay (#185, #190).
- **BP4:** ✔ Sürüm kontrolü ve dağıtım takibi.
- **BP5 (durum muhasebesi):** ✔.
- **BP5 (değerlendirme):** ◐ BP5.1 denetim ihtiyacı belirlendi ama planlanan olaylarda (rc.3–5) yapılmadı.

### 4.3 Etiketlerin `KR-097` ve CHANGELOG ile karşılaştırılması

| Kural (`KR-097`) | Sonuç |
|---|---|
| (1) Kabul edilen modül → MINOR sürüm | ✔ T3 → `v0.2.0` |
| (2) Kabul adayı `vX.Y.0-rc.N`; UAT bu etiketten kurulur; kabul formu etikete atıf yapar | ✔ rc.1–rc.5. Dağıtım kaydı 5/5. Kabul formu "`v0.2.0-rc.5` (`149f6ee`)" diyor |
| (3) Kabulden sonra aynı commit `vX.Y.0` | ✔ `v0.2.0` → 149f6ee, etiket tarihi 13.10 |
| (4) A1 → geriye dönük `v0.1.0` (065a7c3) | ✔ |
| (5) Etiketler açıklamalı; CHANGELOG bölümüyle eşleşir | ✔ 7/7 açıklamalı, 7/7 bölüm var. **İki küçük sapma:** (a) `[0.2.0-rc.4]` bölümünün tarihi 2026-10-10; commit ve etiket 11.10.2026 00:48/00:55 (+03:00). (b) MAN.5 YAKLASIM §5 "etiket açıklaması CHANGELOG başlığıyla aynı" diyor; etiket açıklamaları Türkçe karakter içermiyor ve tarih taşımıyor ("v0.2.0 - T3 Kimlik Yonetimi" ile "[0.2.0] — 2026-10-13 · T3 Kimlik Yönetimi"). Kural ya gevşetilmeli ya uygulanmalı |
| GitHub Release | Yok. Doküman haritası §MAN.5 hâlâ "CHANGELOG + GitHub Releases" diyor (YAKLASIM §8 #4, issue yok) |

### 4.4 PA 2.1 / PA 2.2

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L** (02.10: L−) | **(a)** Hedef yazılı ✔: "kabule sunulan her sürüm için etiket, CHANGELOG, dağıtım kaydı ve CI kanıtı tutarlı". **(c)** Denetim sıklığı planlandı ama uygulanmadı (rc.3–5). Kontrolün altyapı arızası izlenmiyor (05.10). **(d)** Roller ✔. **(e)** `deploy-uat.sh` güçlü bir araç ✔. **(g)** TEC.9, TEC.10, TEC.11 arayüzleri işledi ✔. |
| PA 2.2 | **L** (02.10: L−) | **(a)/(b)** YAKLASIM ✔. **(c)** `dal-koruma-telafi-kontrolleri.md` başlığı 09.09'da kaldı; 02.10'da yazılmıştı, düzelmedi. `konfigurasyon-ogeleri.md` başlığı bayat (O-3). YAKLASIM §2.3/4 bayat (O-4). §4.3'teki iki küçük CHANGELOG/etiket sapması var. **(d)** ✔. **(e)** ✔. |

### 4.5 02.10 bulgularının durumu

| 02.10 bulgusu / eylemi | Durum |
|---|---|
| UAT dağıtımının commit/etikete bağlanması ve dağıtım kaydı | ✅ Kapandı: #125, PR #156. Kayıt sunucuyla 5/5 eşleşiyor |
| CHANGELOG, `v0.1.0`, T3 ön sürüm etiketi | ✅ Kapandı |
| 28.09 Dal Koruma Denetimi çökmesinin kaydı ve yeniden deneme | ⚠️ Kısmen. Olay kaydı (#129) ve `retries`/"denetim yapılamadı" issue'su eklendi. **05.10'da iki kez runner atanamadı ve bu yeni koruma devreye girmedi** (A-03) |
| `konfigurasyon-ogeleri.md` ve ilk konfigürasyon denetimi | ✅ Kapandı (#132). Sonraki denetimler kurala göre yapılmadı (A-04) |
| MAN.5 YAKLASIM; `dal-koruma-telafi-kontrolleri.md` başlık düzeltmesi | ⚠️ YAKLASIM ✅ (#130). Başlık düzeltmesi ❌ açık |
| BULGU-05 (öğeler, denetim, CHANGELOG) | ✅ Kapandı sayıldı; denetim sıklığı yeni bulgu olarak A-04'te |

### 4.6 Yeni bulgular ve düzeltici eylemler

| Öncelik | Bulgu → eylem |
|---|---|
| **Orta** | **A-03:** Tespit edici kontrolün altyapı arızası kayıt dışı kalıyor. Eylemler: (1) `main-failure-check.yml` `workflow_run` listesine "main Dal Koruma Denetimi" eklenir. "Sonuç `failure` veya `cancelled`" durumunda issue açılır. (2) Haftalık zamanlanmış toplu denetim eklenir: `main`'deki son N commit'i yeniden denetler. (3) 3f6833d ve 90a25a9 için elle doğrulama sonucu olay kaydına yazılır (iki PR'da da onay yorumu birleştirmeden önce var). |
| **Orta** | **A-04:** Konfigürasyon denetimi sıklığı uygulanmadı (rc.3, rc.4, rc.5). İki yol var: (a) Kural korunur; kabule girecek her aday için kısa bir denetim (D-01…D-05) sürüm hazırlık PR'ına eklenir. (b) Kural "İK kabul oturumuna sunulan son aday ve modül kapanışı" olarak değiştirilir (karar önerisi §8.3). Her iki yolda D-12 kapsamı dizine genişletilir: `secrets/` altında yalnızca `.env.uat` bulunur. |
| **Düşük** | **A-14:** CHANGELOG tarihi kuralı CONTRIBUTING §9'a yazılır: tarih, etiketin yerel (+03:00) tarihidir. rc.4 tarihi düzeltilir. YAKLASIM §5'teki "etiket açıklaması CHANGELOG başlığıyla aynı" ifadesi uygulamaya göre netleştirilir. Doküman haritasındaki "GitHub Releases" ifadesi kaldırılır. `dal-koruma-telafi-kontrolleri.md` başlığı düzeltilir. |

---

## 5. MAN.6 — Bilgi Yönetimi

**Amaç (özet):** Bilgiyi üretmek, almak, doğrulamak, dönüştürmek, **saklamak, geri getirmek, yaymak ve imha etmek**.

### 5.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Yönetilecek bilgi belirlenir | `bilgi-kayit-defteri.md` (#131, 05.10) BK-01…BK-26 kümelerini sayıyor. Defter dört kez güncellendi: 06.10, 10.10, 13.10 (BK-22/23/24/26). **Depo dışı konumlarla karşılaştırma (`KR-101`, ilk uygulama) defterde olmayan kümeler buldu (§5.3).** Bunların içinde **gizli** sınıfta olanlar var: UAT eski sır kopyası, `src_old/` (`.gitignore` uyarısına göre düz metin bağlantı dizeleri), yerel `docker/.env`, yerel API günlükleri. Doküman haritası 24.09'dan beri güncellenmedi (YAKLASIM §8 #4, issue yok); bilgi kayıt defterini ve YAKLASIM belgelerini göstermiyor. | **L−** |
| **b)** Bilgi gösterimleri tanımlanır | Kimlik şeması, gizlilik sınıfları (Genel / Kurum içi / Gizli), şablonlar ✔. Kabul formu şablonu (`sablonlar/kabul-formu.md`) kullanıldı ✔. | **L+** |
| **c)** Bilgi elde edilir, geliştirilir, dönüştürülür, saklanır, doğrulanır, sunulur ve **imha edilir** | **Yapılanlar:** #73 kapandı (04.10). Eski volume'ler ve sır kopyası içeren dizin silindi; `/root` altında iz kalmadığı bu denetimde görüldü. Yazışma arşivinden 24 parola temizlendi (Ö-1, 06.10). UAT yedek kuralı yazıldı (`KR-098`). Word dönüşümüyle Üst Yönetim ve İK setleri üretildi. **Eksikler:** (1) BK-12 "bir sır değiştirildiğinde eski değer dosyada tutulmaz" diyor, ama `secrets/` altında 29.09 tarihli bir sır yedeği duruyor. (2) BK-20'nin imha koşulu ("yeniden ölçümden sonra silinir") 22.09 ölçümüyle doldu; `…Personel_Veri_Duzeltme_Listesi.xlsx` hâlâ duruyor. (3) Saklama süreleri hâlâ "öneri". KVKK Sorumlusundan tarihli bir talep kaydı yok (Ö-2); RACI §7 iş 3 açık. | **L−** |
| **d)** Bilginin durumu belirlenir | Belge başı ve sonu kuralı çoğu belgede işliyor. **13 belgede başlık ile geçmiş uyuşmuyor; defterin kendisi de bunlardan biri** (O-3). Yaklaşım belgelerinin açık nokta tabloları bayat (O-4). | **L−** |
| **e)** Bilgi belirlenmiş paydaşlara erişilebilir | Üst Yönetime onay seti (06.10) ve İK'ya kabul inceleme seti (12.10) Word olarak sunuldu ve kullanıldı. Kararlar ve kabul bu belgelerle alındı ✔. **Eksikler:** Dağıtım kaydı tanımı yok (YAKLASIM §8 #5); hangi setin kime, ne zaman gittiği yalnızca onay kaydında yazılı. Asıl kanıt niteliğindeki doldurulmuş kabul formu ve onay formu yalnızca geliştirici masaüstünde duruyor; kalıcı konumu (Ö-3; "T3 kabul oturumundan önce" belirlenecekti) ve yedeği yok. | **L** |

**PA 1.1 = L−** (02.10: P+). **İyileşme:** defter kuruldu, sır kopyası ve parolalar temizlendi, saklama ve yedek kuralı yazıldı. Ancak `KR-101`'in istediği ilk karşılaştırma gizli sınıfta kayıt dışı kümeler buldu.

### 5.2 Temel uygulamalar

- **BP1:**
  - ✔ BP1.1 strateji (YAKLASIM).
  - ◐ BP1.2 öğeler (§5.3 eksikleri).
  - ◐ BP1.3 yetki: KVKK rolü bildirilmedi.
  - ✔ BP1.4.
  - ◐ BP1.5 bakım: harita güncellenmedi.
- **BP2:**
  - ✔ BP2.1.
  - ◐ BP2.2 depolama kayıtları ve durum.
  - ✔ BP2.3 dağıtım.
  - ✘ BP2.4 arşivleme: imzalı formların kalıcı konumu yok.
  - ◐ BP2.5 imha: #73 ✔, BK-20 ✘, sır yedeği ✘.

### 5.3 Bilgi kayıt defteri ↔ depo dışı konumlar (`KR-101`, 13.10.2026)

Yalnızca adlar listelendi; içerik açılmadı.

**Defterde var, yerinde var (uyumlu):**

| BK | Konum | Not |
|---|---|---|
| BK-10, BK-11 | UAT volume'leri `dsg-hrms-uat_uat-postgres-data`, `dsg-hrms-uat_uat-api-logs` | ✔ İki volume var, başka volume yok |
| BK-12 | `/opt/dsg-hrms/secrets/.env.uat` (`600`) | ✔ |
| BK-13 | `/opt/dsg-hrms/yedek-*.sql.gz` | ✔ Yok (beklenen: kopya yalnızca riskli işlem sürince durur) |
| BK-14 | `/opt/dsg-hrms/deployments.log` | ✔ Depodaki kayıtla 5/5 |
| BK-20 | `Masaüstü\HRMS\DSG-HRMS_LOGO_*.xlsx` (2 dosya) | ⚠️ Veri düzeltme listesi 22.09 tarihli; imha koşulu doldu |
| BK-21 | `DSG-HRMS_*_Gereksinimleri.xlsx` (5) ve `…_Sistem_Yonetimi_Parametreleri.xlsx` | ⚠️ Kimlik gereksinimleri dosyasının imha koşulu doldu (T3 onaylı ve kabul edildi) |
| BK-22 | `DSG-HRMS_UAT_Izin_Listesi.txt` | ✔ |
| BK-23, BK-24 | `T3_Kabul_IK_Inceleme\` (4 belge) | ⚠️ Kabul kararı verildi (13.10). Form BK-24'ün kalıcı yerine taşınmadı (Ö-3) |
| BK-26 | `Ust_Yonetim_Onay_Seti_2026-10\` (6 belge) | ✔. Kalıcı konum Ö-3'e bağlı |
| BK-05, BK-25 | `docs/TSE_*`, `docs/TS_*`, `docs/NetGSM/`, `claude/` | ✔ |

**Yerinde var, defterde yok:**

| # | Konum (yalnızca ad) | Önerilen sınıf | Neden önemli |
|---|---|---|---|
| 1 | UAT `/opt/dsg-hrms/secrets/.env.uat.yedek-20260929` | **Gizli** | Sır dosyasının eski kopyası. BK-12 kuralına aykırı; #73'ün kapattığı türden bir kopya. İlk konfigürasyon denetimi (D-12) yalnızca `.env.uat` dosyasına baktı |
| 2 | Depo kökü `src_old/` (eski HRMS kaynak kodu; `.gitignore`'da) | **Gizli** | `.gitignore` uyarısı: "src_old içinde açık metin kimlik bilgileri var (veritabanı bağlantı dizeleri…)" |
| 3 | `docker/.env` (yerel geliştirme ortam dosyası; `.gitignore`'da) | **Gizli** | Yerel ortam gerçek LOGO verisi ve gerçek gönderimle çalışıyor; dosya bağlantı bilgisi taşıması beklenen sır dosyasıdır. BK-15 yalnızca volume'leri sayıyor |
| 4 | `src/backend/Dsg.Hrms.Api/logs/` (13 günlük dosyası, 10.09–11.10) | **Gizli** (doğrulanana kadar) | Yerel ortam gerçek veriyle çalışıyor. Bir kısmı kimlik akışı maskeleme testinden (#119, 03.10) önceye ait. Saklama sınırı tanımsız |
| 5 | `Masaüstü\HRMS\Rapor_Onerileri\` (1 görüntü dosyası) | Kurum içi (içerik kontrol edilmedi) | İK'dan gelen rapor istekleri; TEC.2 girdisi niteliğinde |
| 6 | `Masaüstü\HRMS\duzen_logo.png`, `Personel.png` | Belirlenecek (içerik kontrol edilmedi) | Kurumsal logo (KÖ-51 kaynağı olabilir) ve adı kişisel veri çağrıştıran bir görüntü |
| 7 | UAT `/opt/dsg-hrms/sema.sql` | Kurum içi | `deploy-uat.sh` her dağıtımda yazıyor. KÖ-22'nin parçası sayılabilir; açıkça anılmıyor |
| 8 | UAT `/opt/dsg-hrms/tls/` (özel anahtar dahil) | **Gizli** | KÖ-31'de var, bilgi defterinde yok. Özel anahtar gizli sınıftadır |

**Defterde var, yerinde yok:** yok.

**Gözlem (bulgu değil):** UAT'de `root` kullanıcısının kabuk geçmişi (`/root/.bash_history`, 09.10) var. Elle yapılan sunucu işlerinde komut satırına yazılmış bir sır bu dosyada kalabilir. İçerik açılmadı; bir sonraki sunucu işinde Bilgi İşlem'in kontrol etmesi önerilir.

### 5.4 PA 2.1 / PA 2.2

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L−** (02.10: P) | **(a)** "Oluştuğu anda deftere" hedefi yazılı ✔ (YAKLASIM §2.2), uygulanmadı (§5.3). **(c)** İzleme düzeni `KR-101` ile kuruldu; ilk uygulaması bu denetimdir. Defter dönemde dört kez güncellendi ✔. **(d)** KVKK Sorumlusunun rolü resmen bildirilmedi (RACI §7 iş 3). **(e)** İmzalı kanıtlar için saklama ve yedek kaynağı yok (plan B5/B9; YAKLASIM §8 #6). |
| PA 2.2 | **L−** (02.10: P+) | **(a)/(b)** YAKLASIM ✔, defter ✔. **(c)** Başlık–geçmiş uyuşmazlığı (O-3); harita 24.09'da kaldı. **(d)** PR onayı ✔. **(e)** Saklama ve imha süreleri "öneri" durumunda, onay yok. Kanıt niteliğindeki iki imzalı belge tek kopya olarak masaüstünde. |

### 5.5 02.10 bulgularının durumu

| 02.10 bulgusu / eylemi | Durum |
|---|---|
| 30.09 stash olayı için düzeltici faaliyet; 8 süreç klasörü | ✅ Kapandı: #118 (klasörler, `.gitkeep`), #129 (olay kaydı), R-26 |
| UAT veri sınıfı çelişkisi ve yedekleme kararı | ✅ Kapandı: `KR-098` (#133) |
| `bilgi-kayit-defteri.md` (depo dışı kümeler dahil) | ⚠️ Kısmen: defter kuruldu (#131). İlk karşılaştırma 8 eksik küme buldu (§5.3) |
| `claude/` parolalarının temizlenmesi | ✅ Kapandı: Ö-1 (06.10), `KR-102` |
| #73 (sır kopyası içeren yedek) | ✅ Kapandı (04.10). **Aynı türden yeni bir kopya bulundu** (§5.3 #1) |
| MAN.6 YAKLASIM; KVKK rolü; `KR-023` saklama görüşü talebi | ⚠️ YAKLASIM ✅ (#130). KVKK rolü ve talep ❌ açık |
| BULGU-06 | ⚠️ Kısmen kapandı (yukarıdaki satırlar) |

### 5.6 Yeni bulgular ve düzeltici eylemler

| Öncelik | Bulgu → eylem |
|---|---|
| **Yüksek** | **A-01:** UAT'deki `secrets/.env.uat.yedek-20260929` kopyası. Adımlar: (1) Kopyadaki değerlerin güncel `.env.uat` ile aynı olup olmadığı içerik yazışmaya girmeden karşılaştırılır. Kopyada artık kullanılmayan bir sır varsa ve o sır başka bir yerde hâlâ geçerliyse döndürülür. (2) Dosya silinir; silme issue'da kayda geçer (#73 örneği). (3) D-12 "`secrets/` dizininde yalnızca `.env.uat`" olarak genişletilir. (4) Runbook'taki sır değiştirme adımına "yedek dosya bırakılmaz" uyarısı eklenir. |
| **Orta** | **A-02:** §5.3'teki 2–8 numaralı kümeler deftere işlenir (önerilen satırlar §8.2). Ayrıca iki karar gerekir: `src_old/` içindeki bağlantı dizelerinde geçen parolalar döndürülecek mi, ve `src_old/` ne zaman imha edilecek (öneri: geçiş aşaması AS bittiğinde)? |
| **Orta** | **A-06:** "Son güncelleme" denetimi CI'a alınır. Bir PR'da değişen `.md` dosyasında başlık tarihi, değişiklik geçmişindeki en yeni tarihten eski olamaz. Mevcut 13 belge toplu düzeltilir. |
| **Orta** | **A-09:** Doküman haritası güncellenir: bilgi kayıt defteri, YAKLASIM'lar, `kayitlar/` ve `raporlar/` düzeni. Var olmayan `tamamlanma-tanimi.md` ve `kod-gozden-gecirme-kontrol-listesi.md` dosyaları ya oluşturulur ya haritadan çıkarılır. "GitHub Releases" ifadesi kaldırılır. |
| **Orta** | **A-17:** İmzalı ve doldurulmuş kanıtların (T3 kabul formu, Üst Yönetim onay formu) kalıcı konumu ve yedeği belirlenir (Ö-3). Sıradaki modülün kabulünden önce yapılmalıdır, çünkü T3 için öngörülen tarih kaçtı. |
| **Düşük** | **A-16:** KVKK Sorumlusuna saklama ve imha süreleri için tarihli yazılı talep gönderilir (Ö-2, `KR-023`). Talep kaydı `MAN.6/kayitlar/` altına yazılır. RACI §7 iş 3 bir sonraki durum raporunda Üst Yönetime hatırlatılır. BK-20 ve BK-21 (Kimlik) için imha koşulu doldu; silinir veya saklama gerekçesi yazılır. |

---

## 6. Derecelendirme özeti

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | 02.10 (PA 1.1/2.1/2.2 → Sv) | Eğilim |
|---|---|---|---|---|---|---|
| MAN.1 Proje planlama | L+ | L+ | L | 1 | L− / L− / P+ → 1 | ↑ |
| MAN.2 Değerlendirme ve kontrol | L | L− | L− | **1** | P / P / P → 0 | ↑↑ (Seviye 1'e çıktı) |
| MAN.4 Risk yönetimi | L+ | L+ | L | 1 | L+ / L− / L− → 1 | ↑ (Seviye 2 hedefi tutmadı) |
| MAN.5 Konfigürasyon yönetimi | L+ | L | L | **1** | P+ / L− / L− → 0 | ↑↑ (Seviye 1'e çıktı) |
| MAN.6 Bilgi yönetimi | L− | L− | L− | **1** | P+ / P / P+ → 0 | ↑↑ (Seviye 1'e çıktı) |

> **Okuma notu:**
> - **İyileşmenin kaynağı:** 02.10 bulgularının issue'ya çevrilip kapatılması. Ayrıca yönetim süreçleri, karar defteri ve matrisin daha önce yaptığı gibi işin akışına bağlandı. Örnekler: sürüm hazırlık PR'ı, dağıtım betiği, onay seti, `KR-096` denetimi.
> - **Seviye 2'ye neden ulaşılmadı:** Hiçbir süreçte PA 1.1 = F değil. Her birinde F'yi engelleyen somut bir eksik var:
>   - MAN.1: bütçe ve tarihli kaynak taahhüdü.
>   - MAN.2: yeterlilik değerlendirmesi ve ikinci döngü.
>   - MAN.4: kayıt dışı yeni riskler ve bayat puanlar.
>   - MAN.5: kurala göre yapılmayan konfigürasyon denetimleri.
>   - MAN.6: kayıt dışı gizli kümeler.
> - **Ortak zayıflık:** Kural yazılıyor, ama kuralın uygulanıp uygulanmadığını hiçbir şey denetlemiyor. Örnekler: etiketler (O-2), başlık tarihleri (O-3), yaklaşım belgesi gözden geçirmesi (O-4), konfigürasyon denetimi sıklığı (A-04), defterin "oluştuğu anda" güncellenmesi (A-02). CI'a bağlanan kurallar (onay yorumu, dal adı, izlenebilirlik) tutuyor; bağlanmayanlar 8–10 gün içinde aşınıyor.
> - **Bekleyen dört iş:** durum raporu 2, G4 kapanış gözden geçirmesi, alınan dersler, kapanış risk gözden geçirmesi. Bu dört iş birleşince MAN.2 (b, d, j) ve MAN.4 (e) birer kademe yükselebilir. Dereceler bugünkü kanıta göre verildi.

## 7. Öncelikli düzeltici eylemler (birleşik sıralama)

| # | Öncelik | Eylem | Bulgu | Süreç |
|---|---|---|---|---|
| 1 | Yüksek | UAT'deki eski sır kopyasının kontrolü, gerekirse sır döndürme, silme; D-12 kapsamının genişletilmesi | A-01 | MAN.6 / MAN.5 |
| 2 | Yüksek | T3 kapanış paketi (`KR-101`): durum raporu 2 + kapanış risk gözden geçirmesi (§8.1) + G4 kapanış + alınan dersler | §2.6, A-07 | MAN.2 / MAN.4 |
| 3 | Orta | Dal Koruma Denetimi arızasının izlenmesi (`main-failure-check` + haftalık toplu denetim); 05.10 olay kaydı | A-03 | MAN.5 |
| 4 | Orta | Konfigürasyon denetimi sıklığı: kural kararı veya aday başına kısa denetim | A-04 | MAN.5 |
| 5 | Orta | Bilgi kayıt defterine kayıt dışı kümelerin eklenmesi; `src_old/` kararı | A-02 | MAN.6 |
| 6 | Orta | Etiket denetimi CI'da; geriye dönük etiketleme | A-05 | MAN.1 / MAN.2 |
| 7 | Orta | "Son güncelleme" denetimi CI'da; 13 belgenin düzeltilmesi | A-06 | MAN.6 |
| 8 | Orta | Modül kapanışında YAKLASIM ve plan seti gözden geçirmesi (MAN.1/2/4/5/6, RACI §7) | A-08 | Hepsi |
| 9 | Orta | Doküman haritasının güncellenmesi | A-09 | MAN.6 |
| 10 | Orta | İmzalı kanıtların kalıcı konumu ve yedeği (Ö-3) | A-17 | MAN.6 / TEC.11 |
| 11 | Orta | Risk defteri bölüm sırasının düzeltilmesi (yalnız taşıma) | A-07b | MAN.4 |
| 12 | Düşük | `A0` milestone'unun kapatılması | A-10 | MAN.1 |
| 13 | Düşük | Bütçe alt bölümü; B2/B4 hedef tarihi talebi | A-11 | MAN.1 |
| 14 | Düşük | Durum raporuna yeterlilik başlığı ve iletim kaydı | A-12 | MAN.2 |
| 15 | Düşük | PR şablonuna risk, CHANGELOG ve bilgi defteri satırları | A-13 | MAN.4 / 5 / 6 |
| 16 | Düşük | CHANGELOG tarih kuralı; etiket açıklaması kuralı; dal koruma belgesi başlığı | A-14 | MAN.5 |
| 17 | Düşük | Süreç işlerinin milestone kuralı | A-15 | MAN.1 |
| 18 | Düşük | KVKK saklama talebi; BK-20/BK-21 imhası | A-16 | MAN.6 |

## 8. Ortak kayıtlar için önerilen güncellemeler

Bu bölüm **öneridir**; kayıtlara bu denetimde işlenmedi. Kayıtlar PR ile ve Doğuş Uçanok'un onayıyla değişir (`KR-096`).

### 8.1 Risk kayıt defteri (`MAN.4-risk-yonetimi/risk-kayit-defteri.md`)

| Kayıt | Önerilen değişiklik (metin) |
|---|---|
| R-08 / Önlem sonu | "**Güncelleme (2026-10-13):** Kabul öncesi yeniden ölçüm yapılmadı. Ölçüm, üretime geçiş (AS) kararından önce ve en geç sıradaki modülün kabulünde yenilenir." |
| R-09 / O-E-Puan | "**3** / 2 / **6** *(2026-10-13'te 2/2/4'ten yükseltildi: destek bitişi Kasım 2026; üretim altyapısı için hedef tarih yok, `KR-103` madde 4; onay formunda karar 3)*" |
| R-09 / Durum | "Açık — kesimin destek bitişinden sonraya kalması kesinleşiyor; Üst Yönetim durum raporu 2'de bilgilendirilir" |
| R-19 / Önlem sonu | "**Güncelleme (2026-10-13):** Kalan risk T3 kabulünde İK'ya gösterildi ve uygun bulundu (kabul formu, AN-01; `KR-105`). Üyelik deneme sınırı #185 ile 5'ten 10'a çıktı; olasılık 1'de tutuldu, çünkü farkı görmek için üç bilginin birden doğru bilinmesi gerekir ve IP sınırı (PRM-KML-19) değişmedi." (Puan kararını Bilgi İşlem verir.) |
| R-20 / Açıklama | "`mail.duzen.com.tr` sertifikası 06.10.2026'da yenilendi; geçerlilik **27.03.2027**'de bitiyor. Yenilenmezse…" (eski "11.10.2026" cümlesi geçmiş bilgisi olarak parantez içine alınır) |
| R-20 / O-E-Puan | Öneri: "**1** / 3 / **3** *(2026-10-13'te 2/3/6'dan düşürüldü: sertifika 5,5 ay geçerli; yenileme tarihi takvimde)*". Puan düşürülmezse gerekçesi yazılır |
| R-21 / Önlem 3 | "(3) NTP eşitlemesi konfigürasyon öğeleri listesinde (KÖ-32) ve konfigürasyon denetiminde (D-11, 06.10.2026 uygun)." |
| R-23 / Önlem 1 sonu | "05.10.2026'da denetim iki commit'te runner atanamadığı için hiç çalışmadı ve kayıt oluşmadı; izleme önlemi A-03 (R-28)." |
| R-25 / Önlem 1 | "(1) UAT'de ileti gönderimi `AllowList` kipindedir; yalnızca listedeki adreslere gider (`KR-083`). Liste kurum çalışanı kabul katılımcılarıyla sınırlıdır; katılımcılar sonraki kabuller için listede kalır (`KR-106`, 13.10.2026)." |
| R-25 / Önlem 7 (yeni) | "(7) 13.10.2026 modül sonu denetiminde `secrets/` altında 29.09.2026 tarihli eski bir sır kopyası bulundu (A-01); silindi / döndürüldü: …(tarih, issue)." |
| R-26 / Önlem 4 | "(4) Olay kaydı yazıldı: `TEC.13-bakim/kayitlar/2026-10-04-t3-olay-kayitlari.md` (#129)." |
| R-27 / Durum | "İzleniyor — kurtarma işlemi yürürlükte; İK kabulde T-04'ü uygun buldu, KG-07 otomatik testle gösterildi (13.10.2026, `KR-105`)." |
| **R-28 (yeni)** | **Otomatik kontrollerin sessizce etkisiz kalması.** Kategori: Kalite Güvencesi / MAN.5. Açıklama: "Bir kalite kapısı veya tespit edici denetim çalışmadığında ya da hiçbir şeyi denetlemeden geçtiğinde kayıt oluşmuyor. Gerçekleşenler: 28.09.2026 (Dal Koruma Denetimi: GitHub API bozuk yanıt, `Unexpected end of JSON input`, O-1), 05.10.2026 (iki çalışmada runner atanamadı), #171 (mimari testi hiçbir tipi denetlemiyordu)." O/E/Puan: 2 / 2 / **4**. Önlem: "(1) `main-failure-check` Dal Koruma Denetimi'ni de izler (A-03). (2) Haftalık toplu yeniden denetim. (3) Testler 'boş küme' durumunda başarısız olur (#171 düzeltmesindeki kural)." Durum: Açık |
| **R-29 (yeni)** | **Bağımlılıklarda tekrarlayan güvenlik açıkları.** Kategori: Güvenlik / Bakım (TEC.13). Açıklama: "30.09 (CVE, `main` ≈21 saat kırmızı), 04.10 (#149) ve 06.10 (#175): iki haftada üç yüksek önemli açık. Her biri birleştirmeyi durdurur ve plansız iş üretir." O/E/Puan: 3 / 1 / **3**. Önlem: "Güvenlik taraması her PR'da (ADR-0011 §6); `main` kırmızı kuralı (CONTRIBUTING §1.1); haftalık bağımlılık güncellemesi değerlendirilir." Durum: İzleniyor |
| **R-30 (yeni)** | **Depo dışında kayıt dışı gizli bilgi kopyaları.** Kategori: KVKK / Güvenlik (MAN.6). Açıklama: "13.10.2026 karşılaştırmasında bilgi kayıt defterinde olmayan gizli kümeler bulundu: UAT'de eski sır kopyası, `src_old/` (düz metin bağlantı dizeleri), yerel `docker/.env`, yerel API günlükleri." O/E/Puan: 3 / 2 / **6**; temizlik ve kayıt sonrasında yeniden puanlanır. Önlem: "(1) A-01 ve A-02. (2) Her modül sonu denetiminde karşılaştırma (`KR-101`). (3) PR şablonunda 'depo dışı yeni küme' satırı." Durum: Açık |
| §3 Risk özeti | Puanlara göre sayılar yeni değerlerle güncellenir. Aktif takip listesine R-09 ve R-30 eklenir; R-20 öneri kabul edilirse çıkarılır. "Son gözden geçirme: 2026-10-13 — T3 modül kapanışı" satırı yazılır |
| Bölüm sırası | R-11, R-10'dan sonraya taşınır; R-17…R-30 §2'ye, §3 risk özetinin önüne alınır (ayrı PR, içerik değişmez) |

### 8.2 Bilgi kayıt defteri (`MAN.6-bilgi-yonetimi/bilgi-kayit-defteri.md`)

| Kayıt | Önerilen değişiklik (metin) |
|---|---|
| Başlık | "**Son güncelleme:** 2026-10-13" (geçmiş 1.3 ile eşitlenir) |
| BK-06 (yeni) | `| BK-06 | Eski HRMS kaynak kodu | Geliştirici makinesi, depo kökü src_old/ (.gitignore'da) | Bilgi İşlem | **Gizli**: düz metin bağlantı dizeleri içerir | **Öneri:** Geçiş (AS) tamamlanana kadar; göç kuralları için başvuru kaynağı | **Öneri:** AS kapanışında silinir; içindeki parolaların hâlâ geçerli olup olmadığı kontrol edilir, geçerliyse döndürülür |` |
| BK-12 | Konum: "`/opt/dsg-hrms/secrets/.env.uat`; TLS özel anahtarı `/opt/dsg-hrms/tls/` (KÖ-31)". İmha sütununa: "Sır değiştirilirken yedek dosya bırakılmaz. 13.10.2026'da bırakılmış bir kopya bulundu ve silindi (A-01, issue …)" |
| BK-16 (yeni) | `| BK-16 | Yerel geliştirme ortam dosyası | Geliştirici makinesi, docker/.env (.gitignore'da) | Bilgi İşlem | **Gizli**: bağlantı bilgileri ve sırlar | Geliştirme süresince | Geliştirici değiştiğinde veya makine el değiştirdiğinde silinir |` |
| BK-17 (yeni) | `| BK-17 | Yerel API günlükleri | Geliştirici makinesi, src/backend/Dsg.Hrms.Api/logs/ (.gitignore'da) | Bilgi İşlem | **Gizli** (yerel ortam gerçek veriyle çalışır; maskeleme doğrulanana kadar) | **Öneri:** 30 gün | **Öneri:** 30 günden eski dosyalar silinir; yerel yapılandırmada dosya sayısı sınırı |` |
| BK-18 (yeni) | `| BK-18 | UAT şema betiği | /opt/dsg-hrms/sema.sql | Bilgi İşlem | Kurum içi: yalnızca şema | Her dağıtımda üzerine yazılır (deploy-uat.sh) | Gerekmez |` |
| BK-20 | Saklama sütununa: "22.09.2026 ölçümüyle imha koşulu doldu; `…Veri_Duzeltme_Listesi.xlsx` silinir veya saklama gerekçesi yazılır (13.10.2026 denetimi)" |
| BK-21 | "T3 (Kimlik) gereksinimleri depoda onaylı ve kabul edildi (13.10.2026); `DSG-HRMS_Kimlik_Yonetimi_Gereksinimleri.xlsx` silinebilir" |
| BK-23 | Saklama: "Kabul kararı verildi (13.10.2026, `KR-105`). Klasör, form BK-24'ün kalıcı konumuna taşınınca silinir" |
| BK-27 (yeni) | `| BK-27 | İK'dan gelen rapor istekleri | Masaüstü\HRMS\Rapor_Onerileri\ | Bilgi İşlem; kaynağı İK | Kurum içi | İlgili modüllerin (raporlama) gereksinimleri onaylanana kadar | Onaylı içerik depoya alınınca silinebilir |` |
| BK-28 (yeni) | `| BK-28 | Görsel dosyalar | Masaüstü\HRMS\duzen_logo.png, Personel.png | Bilgi İşlem | **Belirlenecek** (içerik kontrol edilip sınıflanır) | — | — |` |
| §5 Ö-3 | Neden sütununa: "T3 tarihi kaçtı; sıradaki modülün kabulünden önce belirlenir. Bugün iki imzalı kanıt (BK-24 T3 formu, BK-26 onay formu) tek kopya ve yedeksiz" |
| Değişiklik geçmişi | `| 2026-10-13 | 1.4 | Modül sonu karşılaştırması (KR-101): BK-06, BK-16, BK-17, BK-18, BK-27, BK-28 eklendi; BK-12, BK-20, BK-21, BK-23 güncellendi (MAN.8 Ek A, A-01/A-02) | Bilgi İşlem |` |

### 8.3 Karar kayıt defteri (`docs/karar-kayit-defteri.md`)

Aşağıdaki kayıtlar yalnızca **karar seçeneği** olarak önerilir. Kararı Doğuş Uçanok verir; numaralar yazım anındaki sıraya göre değişebilir.

| Önerilen kayıt | Karar (öneri metni) | Gerekçe |
|---|---|---|
| KR-107 — Konfigürasyon denetiminin sıklığı | "(1) Konfigürasyon denetimi, İK kabul oturumuna sunulan aday için ve her modül kapanışında yapılır. (2) Ara adaylarda (oturuma girmeyen `rc.N`) sürüm hazırlık PR'ı D-01…D-05 maddelerini (etiket, CHANGELOG, dağıtım kaydı, CI kanıtı, ortam değişkenleri) kontrol listesi olarak taşır. (3) D-12, `secrets/` dizininde yalnızca `.env.uat` bulunduğunu da sınar." | MAN.5 YAKLASIM §2.4 her adayda denetim istiyordu; rc.3–rc.5 için yapılmadı (A-04). Kural uygulanabilir düzeye indirilir, eski sır kopyası türü (A-01) kapsama alınır |
| KR-108 — Süreç işlerinin milestone'u | "Belirli bir aşamanın kapanışına bağlı olmayan süreç işleri (denetim düzeltmeleri, yaklaşım belgeleri, kayıt güncellemeleri) `SK — Süreç ve Kalite (sürekli)` milestone'una atanır. Bir modülün kapısına (G2/G3/G4) bağlı işler o modülün aşamasına atanır. CONTRIBUTING §5 ve WBS §5.2 birlikte güncellenir." | İki belge çelişiyordu; fiilen #125–#133 A2'ye, #174/#177/#179 SK'ya atandı (A-15) |
| KR-109 — Kuralların CI'a bağlanması | "Belge başlığındaki 'Son güncelleme' ve issue etiketleri (`tur:`, `surec:`, `oncelik:`) CI'da denetlenir; denetlenemeyen bir kural CONTRIBUTING'de 'elle denetlenir' olarak işaretlenir (#72 envanteriyle birlikte)." | CI'a bağlanan kurallar tuttu, bağlanmayanlar 8–10 günde aşındı (O-2, O-3) |

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-13 | 0.1 | İlk oluşturma: T3 modül sonu denetimi, MAN.1, MAN.2, MAN.4, MAN.5, MAN.6 bulguları (02.10–13.10.2026 dönemi) | Denetim alt ajanı (salt okuma) |
