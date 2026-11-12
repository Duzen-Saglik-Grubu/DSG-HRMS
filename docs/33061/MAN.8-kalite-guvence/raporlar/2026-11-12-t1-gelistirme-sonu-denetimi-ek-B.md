# Bulgular B — MAN.8, TEC.2, TEC.3, TEC.5, TEC.7

**Değerlendirme tarihi:** 2026-11-12
**Dönem:** 2026-10-13 (T3 kabulü `KR-105`, `v0.2.0`; `main` @ `d354e94`) … 2026-11-12 (`main` @ `388da21`, PR #384). Bu dönemde T1 Personel Yönetimi gereksinimden geliştirme sonuna kadar ilerledi: İK toplantısı (19.10), PG-PERS (45), ADR-0019…0022, T1 tasarım gözden geçirmesi (22.10), SYG-PERS (79 SYG, AN-1…AN-29), dokuz değişiklik talebi.
**Dayanak:** TS ISO/IEC TS 33061 Madde 5 (çıktılar ve BP'ler) · TS ISO/IEC 33020 Madde 5.2.4 (PA 2.1 a–g, PA 2.2 a–e) · Ölçek N/P/L/F (33020 Madde 5.3)
**Yöntem:** Salt okuma. Depo (`dokuman/385-t1-surec-denetimi`, temiz), `git log d354e94..388da21` (89 commit = 89 PR, #220–#384), `gh issue/pr/run` (salt okuma), `node .github/scripts/requirement-traceability-check.mjs` ve `doc-consistency-check.mjs` (12.11.2026: ikisi de ✓, çıkış 0). PR gövdeleri, yorumları ve dosya listeleri 89 PR'ın tamamı için okundu. Kanıt yalnızca fiilen varsa sayıldı; "planlandı" kanıt sayılmadı.
**Bağımsızlık:** Bu bir öz değerlendirme destek çalışmasıdır; 33020 anlamında bağımsız değerlendirme değildir. Ek, değerlendirilen PR'ların çoğunu yazan geliştirme yardımcısının (Claude) ayrı bir alt ajanı tarafından hazırlandı. Dereceler teşhistir.
**Bulgu numaraları:** 13.10 Ek B'nin numaralarından devam eder (MAN.8-12, TEC.2-7, TEC.3-9, TEC.5-9, TEC.7-11).

---

## 0. Özet tablo

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | 13.10 durumu | Ana engel |
|---|---|---|---|---|---|---|
| MAN.8 Kalite güvence | **L+** | **L+** | **L** | 1 | L+ / L / L | İki CI denetimi (DoD, PR boyutu) delikli; DoD 17 PR'da kırpıldı ve uygunsuzluk kaydı açılmadı; K1–K6 T1 boyunca ölçülmedi |
| TEC.2 Paydaş gereksinimleri | **L+** | **L** | **L+** | 1 | L+ / L / L | Dört PG değişikliğinin üçü İK onayı kayda geçmeden birleşti; bir İK kararı (`KR-126` 4) yalnızca Bilgi İşlem onayıyla tersine çevrildi |
| TEC.3 Sistem/yazılım gereksinimleri | **L+** | **L+** | **L+** | 1 | L+ / L / L | F'ye en yakın süreç. KPÖ seti İK'ya gösterilen taslaktan gerekçesiz ayrıldı; AN-19…29 paydaşa dönülmedi |
| TEC.5 Tasarım tanımlama | **L+** | **L** | **L−** | 1 | L / L− / L− | ADR dizini beş ADR'de yine bayat; iki ADR eski kararı gösteriyor; `KR-099` (2) iki kez sınıflandırmasız; T1 diyagramı yok |
| TEC.7 Gerçekleştirme | **L+** | **L** | **L** | 1 | L+ / L / L | Beş ürün hatası birleştirmeden sonra insan gözüyle bulundu; geri yön kapsamı izin ve uç noktaya genişlemedi; DoD kayması |

**Hiçbir süreç Seviye 2 değil.** Beşinin dördünde 13.10'a göre ilerleme var; TEC.7 yerinde.

**İlerlemenin kaynağı:** 13.10 iş planının beş sürece düşen işleri (#210, #211, #212, #216, #217) üç gün içinde kapandı ve çoğu **CI denetimine** dönüştü. T1 bu denetimlerin altında geliştirildi:
- 9/9 değişiklik talebinde tarihli onay birleştirmeden önce kayıtlı (CI).
- 89/89 PR'da onay yorumu birleştirmeden önce (CI).
- 77/77 issue ve 89/89 PR'da `tur:` ve `surec:` etiketi (CI).
- T1 tasarımı ilk kod PR'ından önce bağımsız alt ajanla gözden geçirildi; 19 bulgu SYG'den önce ADR'lere işlendi.
- On türetilmiş davranış aynı PR'da SYG'ye `AN-` olarak girdi.

**Neden yine Seviye 2 değil?** Denetimler **neyi aradıklarını** buluyor, **amacı** değil:
- Onay denetimi bir tarih arıyor, **kimin** onayladığına bakmıyor (TEC.2-7).
- Boyut denetimi "Boyut gerekçesi" sözcüğünü arıyor; şablonun kendi DoD satırı bu sözcüğü içeriyor (MAN.8-13).
- DoD denetimi yalnızca iki maddeye bakıyor; 17 PR'da öteki maddeler silindi (MAN.8-12).
- Belge tutarlılık denetimi SYG sayımlarına ve başlık tarihlerine bakıyor; ADR dizinine ve PG §2 özetine bakmıyor (TEC.5-9, TEC.2-8).

---

## 1. Değişiklik talepleri — #210 yordamının uygulanması (istenen özel kontrol)

TEC.2 YAKLAŞIM §4.1 (#210): onay `Paydaş onayı: GG.AA.YYYY — <rol> — <kanal>` satırıyla issue'ya yazılır; uygulama PR'ı ondan sonra birleşir (CI). RACI'de TEC.2'nin **A**'sı İK'dır (`roller-ve-sorumluluklar.md` §3.1); TEC.3 YAKLAŞIM §4, PG'nin anlamını değiştiren SYG değişikliği için "İK onayı" ister.

### 1.1 Dokuz talep

| Talep | Değişen | Onay satırı (rol) | Kaynak | Uygulama PR'ı | Değerlendirme |
|---|---|---|---|---|---|
| #270 | PG-PERS-12 (çalışma şekli çıktı) | 22.10 — **İK** (Bİ bildirdi) | Tasarım gözden geçirmesi B-05 | #271 | ✔ |
| #284 | PG-PERS-12 (firma eklendi) | 24.10 — **Bİ**; İK yorumu 11:34:57Z | PR #283 onay yorumu | #287, 11:29:10Z | ⚠ İK onayı birleştirmeden **6 dk sonra** |
| #321 | SYG-PERS-017, 022 (esas sicil: en yeni → en eski) | 03.11 — **Bİ** | PR #318 onay yorumu | #323 | ✘ `KR-126` (4) bir **İK kararıydı**; 11 kişinin esas sicili değişti |
| #333 | PG-PERS-37 (bildirim Sistem Yöneticisine de) | 04.11 — **Bİ** | PR #331 onay yorumu | #334 | ✘ PG değişikliği, İK onayı yok |
| #344 | SYG-PERS-012 (uyarı e-postayla da) | 05.11 — **Bİ** | PR #343 onay yorumu | #345 | ✔ (SYG; A = Bİ) |
| #348 | SYG-PERS-043 (yakınlık referans veriden) | 05.11 — **Bİ** | PR #347 onay yorumu | #349 | ✔ |
| #352 | SYG-PERS-072 (yeniden aktif etme) | 06.11 — **Bİ** | PR #351 inceleme yorumu | #351 (aynı PR) | ✔; kapsam PR sırasında genişledi, ayrı talep açıldı |
| #355 | PG-PERS-44, SYG-PERS-042/043 | 06.11 — **Bİ** | PR #354 inceleme yorumu | #356 | ✘ PG değişikliği, İK onayı yok |
| #374 | SYG-PERS-055/062 (yeni izin) | 08.11 — **Bİ** | Oturumdaki karar sorusu | #376 | ✔ |

### 1.2 Değerlendirme

**Yordam mekanik olarak işledi.** Dokuz talebin dokuzunda:
- etiket, etki analizi ve tarihli onay satırı var;
- PG/SYG geçmişine talep numarası yazıldı (PG-PERS 1.4–1.8, SYG-PERS 0.6–0.22);
- ilgili ADR ve matris aynı PR'da güncellendi (istisna: ADR-0022 §3, bkz. TEC.5-9).

13.10'daki "#185'te uygulama onaydan önce birleşti" sorunu bu biçimiyle tekrarlanmadı.

**Ama onayın sahibi değişti.** Dört PG değişikliğinden yalnızca biri (#270) birleştirmeden önce İK onayı taşıyor. #333 ve #355'te İK onayı hiç yok. #321, tasarım gözden geçirmesinde "Karar gerekli (İK)" diye işaretlenmiş ve İK'nın verdiği bir kararı (`KR-126` 4) tersine çevirdi; onaylayan Bilgi İşlem Birim Sorumlusu. CI onaylayan rolü denetlemiyor; satırda tarih olması yetiyor.

**Örüntü:** Dokuz talebin sekizi, inceleyenin PR onay yorumunda istediği bir değişiklikten doğdu. Talep eden ve onaylayan aynı kişi. Bu meşru bir kanaldır; ama PG İK'nındır ve TEC.2(g) paydaş mutabakatını ister. Telafi, #210'un 7. adımıdır: değişiklikler T1 kabul planında `T-xx` olarak İK'ya gösterilir. T1 kabul planı henüz yok.

---

## 2. MAN.8 — Kalite güvence

### 2.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | KG yordamları tanımlanır ve uygulanır | **Tanım:** YAKLAŞIM 1.1 (15.10), PR şablonu tek DoD (`KR-111`), CONTRIBUTING §5–§6. **Uygulama:** 89/89 PR'da onay yorumu birleştirmeden önce (7–487 sn önce, medyan 30 sn). Etiket, atlanamaz DoD, boyut, değişiklik talebi onayı ve geçmiş satırı kuralları CI'da (#216, #210, #235). **Zayıflık:** DoD listesi 17 PR'da kırpıldı (MAN.8-12); boyut denetimi şablon metniyle geçiyor (MAN.8-13); büyük PR'da inceleme notu 0/7 (MAN.8-14). | **L** |
| b | Ölçüt ve yöntemler tanımlanır | K1–K6 tanımlı, hedefli (YAKLAŞIM §2.5, `KR-111`); T3 başlangıç değerleri (`kayitlar/2026-10-15-kg-olcutleri-t3-baslangic.md`). Modül sonu yöntemi §2.3. Tasarım gözden geçirme yöntemi TEC.5 §2.5. | **F** |
| c | Değerlendirmeler yapılır | **Ürün:** CI her PR'da; SYG → test kanıtı CI'da, baseline'sız modülde uyarı (#273). **Süreç:** bu denetim; T3 ve T1 tasarım gözden geçirmeleri. **Araç:** `pr-discipline-test.mjs`, `change-request-approval-test.mjs` kuralları kendini sınıyor. Ancak boyut kuralının sınaması şablon metnini içermiyor (MAN.8-13). | **L+** |
| d | Sonuçlar iletilir | 13.10 raporu ve iş planı issue'larda. **K1–K6 T1 dönemi boyunca hiçbir yerde raporlanmadı**: 13.10'dan sonra durum raporu yok (`KR-101` sınırı 13.11). | **L** |
| e | Olaylar çözülür | `main` kırmızısı: 5 olay (14.10, 20.10, 21.10, 23.10, 01.11), 5'i de otomatik issue (#223, #242, #255, #286, #305); dördü aynı gün, biri ertesi gün (≈14 saat) kapandı. T1'de insan tarafından bulunan 5 ürün hatası issue'lu ve kapalı (#306, #315, #338, #357, #373). | **F** |
| f | Önceliklendirilmiş problemler ele alınır | 13.10'un 13 düzeltici işi 13/13 kapandı; medyan 0,8 gün, en uzun 2,8 gün (K2). **Eksik:** 17 PR'daki DoD kayması fark edildi ve #351'den itibaren düzeldi, ama issue'su yok (YAKLAŞIM §2.4). Kaçan 5 hatanın 1'inde "neden yakalanmadı" kısmen yanıtlı (#357). #72 hâlâ açık. | **L+** |

**PA 1.1 = L+** (13.10: L+). Yordam güçlendi ve sayıyla ölçülebilir hâle geldi. F'nin önünde (a)'daki iki delikli denetim ve (d)'deki ölçümsüz dönem var.

### 2.2 BP kontrolü

| BP | Durum |
|---|---|
| BP1 Hazırlık, bağımsızlık | Strateji ✔. Bağımsızlık: T1 tasarım gözden geçirmesinde ayrı alt ajan, bulguları değiştirilmeden raporda (§4). İnsan veya kurum dışı bağımsızlık yok (R-23). **Kısmen** |
| BP2 Ürün değerlendirmesi | ✔ CI ve onay. BP2.2: SYG → test kanıtı CI'da (77/79, kalan ikisi planlı) |
| BP3 Süreç değerlendirmesi | ✔ BP3.2: kurallar kendini sınıyor; ama sınama örnekleri gerçek PR gövdesini temsil etmiyor (MAN.8-13) |
| BP4 Kayıt ve raporlar | Raporlar ✔; dönem ölçümü ✘ (MAN.8-15) |
| BP5 Olay/problem | BP5.1 kayıt ✔. BP5.4 kök neden: teknik neden yazılıyor, "neden yakalanmadı" yazılmıyor. BP5.6 eğilim: K6 T1'de ölçülmedi. BP5.9 kapanış ✔ |

### 2.3 PA 2.1 / PA 2.2

- **PA 2.1 = L+** (13.10: L). (a) Hedefler sayısal (K1–K6) ✔. (c) İş planı izlendi ve üç günde kapandı ✔; dönem içi ölçüm yok ✘. (d) Roller ✔. (e)(f) GitHub Actions ✔. (g) TEC.9/TEC.13 arayüzü otomatik issue'larla ✔.
- **PA 2.2 = L** (13.10: L). (a)(b) YAKLAŞIM §5–§6 ✔. (c) ✔. (d) Onay yorumu var; büyük PR'da içerik notu yok; denetim hâlâ yalnızca sözcüğü arıyor. 13.10 MAN.8-9 (3), onay kaydında #114/#118'in "onay birleştirmeden sonra" notu, yapılmadı. (e) ✔.

### 2.4 13.10 bulgularının durumu (MAN.8)

| Bulgu / iş | Durum |
|---|---|
| MAN.8-8 Bulguların issue'ya çevrilmesi (#217) | ✅ 28/28 takipte; issue (#234, #235) veya gerekçeli kapatma (`KR-111`) |
| MAN.8-9 Onay kaydının anlamı | ⚠ Kural yazıldı (CONTRIBUTING §5: büyük PR'da neyin incelendiği); uygulama 0/7; denetim değişmedi; #114/#118 notu yok |
| MAN.8-10 Etiket disiplini (#216) | ✅ 77/77 issue, 89/89 PR; CI |
| MAN.8-11 KG ölçütleri (#217) | ⚠ Tanım ve T3 değerleri ✔; T1 dönemi ölçülmedi |

### 2.5 KG ölçütleri — T1 dönemi (bu denetimde hesaplandı, 12.11.2026)

| # | Değer | Hedef | Yöntem |
|---|---|---|---|
| K1 | 13.10 bulguları 12/12 → 13 issue; 13.10 Ek B önerileri §2.4–§6.4 tablolarında | %100 | Issue gövdesi "Denetim maddesi" |
| K2 | #207–#219: medyan **0,80 gün**, en uzun **2,84 gün**, 13/13 kapalı | ≤ 7 gün | `closedAt − createdAt` |
| K3 | `main` CI **81/87 (%93,1)**; 5 olay, 5'i otomatik kayıtlı | ≥ %90 | `gh run list --workflow ci.yml --branch main` |
| K5 (b) | **89/89** onay yorumu birleştirmeden önce | %100 | PR yorumları |
| K6 | T1: **5** kaçan hata (#306, #315, #338, #357, #373); T3 kabul sonrası 30 gün: **0** ürün hatası | 0 | `tur:hata` gövdeleri |

K4 ve K5 (a) bu ekte hesaplanmadı (ana rapor veya durum raporu).

### 2.6 Yeni bulgular

| Kimlik | Önem | Bulgu ve kanıt | Öneri | Nasıl |
|---|---|---|---|---|
| **MAN.8-12** | Orta | **DoD kayması uygunsuzluk sayılmadı.** #320–#349 arasındaki 17 PR'da DoD 8–16 maddeye indi (şablon 26); "Madde silinmez" (CONTRIBUTING §6). T1'in 73 PR'ının 45'inde "Neden?" veya "Notlar" başlığı yok. #351'den beri 17/17 tam; ama issue açılmadı | `[DÜZELTİCİ]` issue; CI şablondaki yedi başlığı ve DoD madde sayısını denetler (silinen madde → hata) | Ayrı iş |
| **MAN.8-13** | Orta | **Boyut denetimi şablonla geçiyor.** `/boyut\s+gerek[çc]esi/` şablonun DoD satırını da eşliyor. #364: üretilenler hariç **1.713** satır; gövde "1.500'ün altında" diyerek satırın üstünü çizdi; CI geçti. 8 PR > 1.500: biri kuraldan önce (#220), 6'sında gerçek gerekçe, #364'te yanlış beyan | Denetim "Boyut gerekçesi:"yi DoD listesi dışında, satır başında arar; `pr-discipline-test.mjs`'e şablon metniyle bir örnek eklenir | Ayrı iş |
| **MAN.8-14** | Orta | **Büyük PR'da inceleme notu yok.** CONTRIBUTING §5 neyin incelendiğini ister. 1.500'ü aşan 7 PR'ın onay yorumlarının 7'si genel ifade ("Tüm düzenlemeleri uygun buluyorum") | Ya kural uygulanır (sabit biçim, CI) ya da gerekçeli `KR-` ile kaldırılır | Ayrı iş |
| **MAN.8-15** | Orta | **K1–K6 dönem içinde ölçülmedi.** Hedef "her durum raporunda" (YAKLAŞIM §1). Son durum raporu 13.10; sonraki en geç 13.11 | §2.5'teki değerler ana rapora ve durum raporu §3'e alınır; K4, K5 (a) tamamlanır | Bu işte |
| **MAN.8-16** | Düşük | **Kaçan hata kök nedeni yarım.** 5 hatanın gövdesinde teknik neden var, "neden şimdiye kadar yakalanmadı" yok (yalnızca #357 kısmen). Hepsi PR onayından **sonra** yerel incelemede bulundu | Hata şablonuna zorunlu "Neden yakalanmadı?" alanı; onay öncesi yerel deneme DoD maddesi | Ayrı iş |
| **MAN.8-17** | Düşük | 13.10 MAN.8-9 (3): onay kaydında #114/#118 "onay birleştirmeden sonra" notu yok | Kayda not | Bu işte |

---

## 3. TEC.2 — Paydaş ihtiyaç ve gereksinimleri

### 3.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Paydaşlar tanımlanır | Toplantıya dört İK uzmanı ve KVKK Sorumlusu katıldı; çağrılıp katılamayan yok (kayıt §1). Paydaş listesi #236'da güncellendi. Kan grubu kararlarında P6 danışıldı (`KR-113`, `KR-116`) | **F** |
| b | Kullanım bağlamı | Kayıt §2; 16.10 tarihli taslak toplantıda satır satır görüşüldü | **F** |
| c | Kısıtlar | Kayıt §5 | **F** |
| d | İhtiyaçlar | 41 taslak + 3 toplantıda eklenen + 1 teyitle (PG-PERS-45); elenenler gerekçeli (§4) | **F** |
| e | Öncelik ve PG'ye dönüşüm | PG-PERS 45 madde, öncelik ve kabul kriteriyle; teyitler ayrı kayıtta (§12, §13) | **F** |
| f | Kritik performans ölçütleri | **İlerleme:** KPÖ taslakları toplantıda İK'ya sunuldu (§10; 13.10 TEC.3-8 kapandı). Ama SYG-PERS §5'teki set taslaktan ayrıldı (TEC.3-9) | **L+** |
| g | Paydaş mutabakatı | Toplantıda beş katılımcının beşi "Evet" (§7) ✔. **Sonrası:** dört PG değişikliğinin üçü İK onayı birleştirmeden önce kayıtlı değil (§1.2) | **L** |
| h | Destekleyici sistemler | Taslak dosyası, UAT | **F** |
| i | İzlenebilirlik | PG → SYG 45/45 CI ✔; her değişiklik issue'ya ve PG geçmişine bağlı ✔ | **F** |

**PA 1.1 = L+** (13.10: L+). (f) belirgin biçimde iyileşti; (g) geriledi.

### 3.2 BP kontrolü

BP1 ✔ · BP2 ✔ · BP3 kısmen (senaryo seti yok; kabul planı henüz yok) · BP4 ✔ · BP5 ✔ BP5.3 geri bildirim toplantı sonrası iki teyitle (20.10, 22.10) · BP6.1 açık mutabakat: toplantıda ✔, değişikliklerde kısmen.

### 3.3 PA 2.1 / PA 2.2

- **PA 2.1 = L** (13.10: L). (a)(c) Toplantı planlandığı gibi yapıldı; kayıt 48 saat içinde ✔. (d) RACI'de A = İK; değişikliklerde fiilen A = Bilgi İşlem ✘. (g) İK ↔ Bİ arayüzü toplantıda güçlü; sonrasında Bİ üzerinden.
- **PA 2.2 = L+** (13.10: L). (a)(b) YAKLAŞIM §4.1 yazılı ve CI'da ✔. (c) PG geçmişi 1.0–1.8, üstü çizili eski metin ✔. PG §2 özeti ve SYG başlığı bayat (TEC.2-8). (d) ✔. (e) ✔.

### 3.4 13.10 bulgularının durumu (TEC.2)

| İş | Durum |
|---|---|
| TEC.2-5 Değişiklik talebi yordamı (#210) | ✅ Yazılı, CI'da; 9/9 talep onaylı birleşti. ⚠ Rol denetlenmiyor (TEC.2-7) |
| TEC.2-6 PG-KMLK bakımı, #62 | ✅ PG-KMLK 1.7 (#214); #62 kapalı |
| TEC.2-2 PG'ye ölçüt bağı | ✅ `KR-111`: KPÖ SYG'de, toplantıda bilgi olarak sunulur; T1'de uygulandı |
| TEC.2-3 YAKLAŞIM ve paydaş listesi | ✅ #236 |

### 3.5 Yeni bulgular

| Kimlik | Önem | Bulgu ve kanıt | Öneri | Nasıl |
|---|---|---|---|---|
| **TEC.2-7** | **Yüksek** | **PG değişikliği İK onayı olmadan.** #333 (PG-PERS-37) ve #355 (PG-PERS-44) yalnızca Bİ onaylı; #284'te İK onayı birleştirmeden 6 dk sonra; #321 bir İK kararını (`KR-126` 4) Bİ onayıyla tersine çevirdi. CI rolü denetlemiyor | (1) Talep PG'ye veya bir İK kararına dokunuyorsa onay satırındaki rol "İK" olmalı; CI bunu denetler. (2) #284, #321, #333, #355 için İK'dan geriye dönük yazılı teyit. (3) Dokuz talep T1 kabul planına `T-xx` olarak girer | Ayrı iş |
| **TEC.2-8** | Orta | **PG ve SYG özetleri bayat.** PG-PERS §2 "Onaydan sonraki değişiklik talebi: 2" diyor; gerçek 4 (#270, #284, #333, #355). SYG-PERS başlığı "Kaynak … değişiklik talebi #270" diyor. 13.10'da PG-KMLK'de görülen sorunun tekrarı | Belge düzeltmesi; `doc-consistency-check.mjs` PG §2 sayısını geçmişteki "Değişiklik talebi #" satırlarıyla karşılaştırır | Bu işte (belge); önlem ayrı iş |

---

## 4. TEC.3 — Sistem/yazılım gereksinimleri

### 4.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Sistem/öğe tanımı | SYG-PERS §2 (amaç, öğeler, dış arayüzler, sınırlar), §3 durumlar ve kipler (BP2.2) ✔. Öğe konumları güncellenmedi (TEC.5-11) | **L+** |
| b | Gereksinimler ve kısıtlar | 79 SYG; tür ve doğrulama yöntemi her satırda. Türetilmiş davranış kuralı (#211) **on kez** uygulandı: AN-19…AN-28 aynı PR'da | **F** |
| c | Kritik performans ölçütleri | KPÖ-PERS-1…5 yük profiliyle tanımlı (AN-15, T3 dersi). UAT ölçümü #381 (TEC.9 raporu 10.11). Taslaktan ayrılış kayıtsız (TEC.3-9) | **L+** |
| d | Analiz | 29 AN; tasarım gözden geçirmesinin test tanımsız kuralları SYG'ye eşlendi ✔. BP3.3: AN-04 İK'ya teyit ettirildi (`KR-127`); AN-19…29 için geri bildirim cümlesi yok (TEC.3-10) | **L+** |
| e | Destekleyici sistemler | §7 güncel (0.7) | **F** |
| f | İzlenebilirlik | 45/45; §8, matris §7 ve §4 tutarlı (CI); 77/79 SYG test kanıtlı, kalan ikisi planlı (SYG-PERS-056 T2'ye bağlı, 065 İnceleme) | **F** |

**PA 1.1 = L+** (13.10: L+). Beş süreç içinde F'ye en yakın olanıdır. F için (c) ve (d)'deki iki küçük kayıt eksiği kapanmalıdır.

### 4.2 BP kontrolü

BP1 ✔ · BP2 ✔ (BP2.4 risk ilişkili: kan grubu, AAD, yeniden şifreleme SYG'de) · BP3 kısmen (BP3.3) · BP4.1 Bİ onayı PR ile, "Onay durumu" satırı ✔ · BP4.2 ✔ · BP4.3 baseline kabulde (planlı).

### 4.3 PA 2.1 / PA 2.2

- **PA 2.1 = L+** (13.10: L). (a) Hedef: SYG koddan önce; kodla birlikte kanıt (#272) ✔. (c) Belge her değişiklikte izlendi: 24 geçmiş satırı 17 günde (0.1–0.24) ✔. (d) A = Bİ, uygulandı ✔.
- **PA 2.2 = L+** (13.10: L). (a)(b) YAKLAŞIM §4 ✔: kapsama, kanıt, geçmiş satırı ve sayım tutarlılığı otomatik. (c) Başlıktaki "Kaynak" satırı bayat (TEC.2-8). (d)(e) ✔.

### 4.4 13.10 bulgularının durumu (TEC.3)

| İş | Durum |
|---|---|
| TEC.3-6 #138 kuralı SYG'de (#211) | ✅ SYG-KMLK-057, AN-26; TEC.3 §4 "Türetilmiş davranış" kuralı. T1'de on kez uygulandı |
| TEC.3-7 İç tutarlılık denetimi (#214, #235) | ✅ SYG §1 sayımları ve başlık tarihleri CI'da; geçmiş satırı CI'da. ⚠ PG §2 özeti ve ADR dizini kapsam dışı |
| TEC.3-8 KPÖ İK'ya | ✅ T1 toplantısı §10 |
| TEC.3-1 SYG onay alanı (02.10) | ✅ "Onay durumu" satırı (`KR-111`) |

### 4.5 Yeni bulgular

| Kimlik | Önem | Bulgu ve kanıt | Öneri | Nasıl |
|---|---|---|---|---|
| **TEC.3-9** | Orta | **KPÖ seti taslaktan gerekçesiz ayrıldı.** İK'ya sunulan taslakta KPÖ-PERS-4 "telefon, 360 px" ve KPÖ-PERS-5 "e-posta ulaşma p95 < 60 sn" idi. SYG-PERS §5'te aynı kimlikler "fotoğraf özeti maliyeti" ve "kan grubu süzgeçli liste". E-posta süresi ölçütü düştü. Hiçbir `AN-` bunu anlatmıyor | SYG-PERS §6'ya AN: neden değişti, düşen ölçüt nerede karşılanıyor. Kimlikler tarihsel kaydı çağrıştırmayacak biçimde not düşülür | Bu işte (belge) |
| **TEC.3-10** | Düşük | **AN-19…29 paydaşa dönülmedi.** §6 sonundaki "Paydaşa geri bildirim" paragrafı 0.1'den beri değişmedi; on yeni AN için "anlam değişmez" ya da "İK'ya gösterilir" kararı yok (BP3.3) | Paragraf güncellenir; İK'ya gösterilecek AN'ler T1 kabul planına alınır | Bu işte (belge) |

---

## 5. TEC.5 — Tasarım tanımlama

### 5.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Öğelerin tasarım özellikleri | ADR-0019…0022; gerçekleştirme notları (ADR-0020 1.4, 1.5; ADR-0021 1.4–1.6) ✔. **T1 için diyagram yok** (T3'te `kimlik-tasarim-diyagramlari.md` var) | **L** |
| b | SYG'lerin öğelere tahsisi | **İlerleme:** SYG-PERS §2.2 öğe → SYG aralığı → ADR tablosu; matris §7 "Tasarım" sütunu satır başına ADR bölümü. **Eksik:** §2.2 konumları "kod yazıldıkça" güncellenmedi (`Infrastructure/Security` yok; şifreleme `Infrastructure/Personnel/Protection`'da; `Reports` tabloda yok). Dosya düzeyi tahsis (§2.5) "kabulden önce" bekliyor | **L** |
| c | Tasarım etkinleştiricileri | ✔ | **F** |
| d | Arayüzler | OpenAPI + sözleşme denetimi; LOGO alan kapsamı testle (`LogoFieldScopeTests`) ✔ | **F** |
| e | Alternatifler | ADR'lerde alternatif tabloları; blind index reddi gerekçeli; `KR-131`'de PDF kütüphanesi alternatifleri ✔ | **F** |
| f | Tasarım ürünleri | **Tasarım gözden geçirmesi, T1 tasarımına dayanan ilk kod PR'ından (#277, 23.10) önce yapıldı** (22.10): bağımsız alt ajan 19 bulgu, Bİ 5 bulgu; dört karar sahibine soruldu; hepsi #268/PR #271'de ADR'lere işlendi; SYG ancak ondan sonra yazıldı ✔. 13.10'un TEC.5-6'sı kapandı | **F** |
| g | Destekleyici sistemler | ✔ | **F** |
| h | Tasarım ↔ mimari varlık | ADR-0016 bağımlılık tablosu + `LayerDependencyTests` ✔; matris "Tasarım" sütunu ✔; diyagram ve dosya düzeyi tahsis yok | **L** |

**PA 1.1 = L+** (13.10: L). Tasarım gözden geçirmesi ve tahsis tablosu gerçek ilerlemedir.

### 5.2 ADR güncelliği ve `KR-099` (istenen özel kontrol; #212'nin etkinliği)

| Kontrol | Sonuç |
|---|---|
| #212'nin üç maddesi | ✔ ADR-0006 §5 düzeldi; KR-104 için ADR-0017 yazıldı; ADR-0011 §7 için ADR-0018 (#230) |
| ADR değişiklik geçmişleri | ✔ ADR-0019 1.0–1.4, 0020 1.0–1.5, 0021 1.0–1.6, 0022 1.0–1.7; her satırda `KR-` ve issue |
| ADR dizini (README) | ✘ "Son güncelleme 2026-10-24". Beş ADR bayat: ADR-0014 dizinde 0.2 (gerçek 0.3), 0019 1.3 (1.4), 0020 1.3 (1.5), 0021 1.3 (1.6), 0022 1.4 (1.7). ADR-0014 özeti hâlâ "ClosedXML + PDFsharp". TEC.5 §2.4: "README dizini aynı PR'da güncellenir" |
| Eski kararı gösteren metin | ✘ ADR-0022 §3: bildirim "İK rolündeki her kullanıcıya" (`KR-132` ile Sistem Yöneticisi de; ADR'de `KR-132` yok). ADR-0022 §4 ve ADR-0001 teknoloji tablosu: "PDFsharp/MigraDoc" (`KR-131` ile SkiaSharp) |
| Başlıktaki "Karar defteri karşılığı" | ✘ ADR-0019'da `KR-128`, ADR-0020'de `KR-129` ve `KR-138`, ADR-0022'de `KR-128`, `129`, `133`, `134`, `135` yok (gövde ve geçmişte var). 13.10'da ADR-0006 için görülen kusurun aynısı |
| `KR-099` (2) sınıflandırması | ⚠ `KR-131` PDF kütüphanesini değiştirdi ("yerine başka çözüm koyan"); ADR-0014 içinde "Revizyon" notu ile işlendi. `KR-130` İK kararı `KR-126` (4)'ü tersine çevirdi; ADR-0019 §6 yerinde güncellendi. İkisinde de "neden yeni ADR değil" gerekçesi yok. `KR-099` (3): "belirsizse yeni ADR" |
| Önleyici CI adımı | ✘ 13.10 TEC.5-7 "README Son değişiklik ↔ ADR geçmişi" denetimini önerdi; `doc-consistency-check.mjs` ADR dizinini taramıyor |

**Sonuç:** #212 kendi üç maddesini kapattı; ama kök neden (dizin ve kopyaların elle bakımı) kalmış. Aynı kusur üçüncü kez görüldü (02.10, 13.10, 12.11).

### 5.3 BP ve PA 2.1 / PA 2.2

BP1 ✔ · BP2 ✔ · BP3 ✔ · BP4.1 gerekçe ✔; BP4.2 tahsis kısmen; **BP4.3 tasarım durumu ✘** (dizin bayat); BP4.4 baseline kabulde.

- **PA 2.1 = L** (13.10: L−). (a) YAKLAŞIM §1.2 ✔. (c) Gözden geçirme planlandığı gibi yapıldı ✔; `KR-099` hedefi yine kaçtı ✘. (d) Bağımsız alt ajan rolü tanımlı (Doğuş Uçanok'un kararı, 15.10) ✔.
- **PA 2.2 = L−** (13.10: L−). (a)(b) ✔. (c) Dizin, başlıklar, iki ADR metni bayat ✘. (d) PR onaylı ✔. (e) ✔.

### 5.4 13.10 bulgularının durumu (TEC.5)

| İş | Durum |
|---|---|
| TEC.5-7 `KR-099` (#212) | ⚠ Üç madde kapandı; önleyici CI adımı yok; dizin yeniden bayat |
| TEC.5-8 Modül listesi ↔ ADR-0016 | ✅ |
| TEC.5-3 SYG → öğe tahsisi (#234) | ⚠ Öğe düzeyi ✔ (SYG-PERS §2.2); dosya düzeyi ve konumların güncellenmesi ✘ |
| TEC.5-5 Diyagramlar (#234) | ⚠ T3 için ✔; T1 için ✘ |
| TEC.5-6 Tasarım gözden geçirmesi (#234) | ✅ T3 (20.10), T1 (22.10) |

### 5.5 Yeni bulgular

| Kimlik | Önem | Bulgu ve kanıt | Öneri | Nasıl |
|---|---|---|---|---|
| **TEC.5-9** | **Yüksek** | **ADR'ler kararların gerisinde (üçüncü kez).** §5.2: beş ADR dizinde bayat; ADR-0022 §3–§4 ve ADR-0001 eski kararı gösteriyor; başlık KR listeleri eksik. 13.10'da önerilen CI adımı yapılmadı | (1) Belge düzeltmesi. (2) `doc-consistency-check.mjs`: README "Son değişiklik" = ADR geçmişinin son satırı; geçmişte anılan her `KR-` başlıkta | Bu işte (belge) + ayrı iş (CI) |
| **TEC.5-10** | Orta | **`KR-099` (2) sınıflandırması yapılmadı.** `KR-131` ve `KR-130` yerinde revizyon; gerekçe yok | Her biri için karar: yeni ADR veya ADR notuna "neden genişletme" gerekçesi | Ayrı iş |
| **TEC.5-11** | Orta | **T1 tahsis ve diyagramı eksik.** SYG-PERS §2.2 konumları planlandığı gibi kaldı (en az iki sapma); §2.5 dosya düzeyi tahsis boş; T1 için durum/akış diyagramı yok (TEC.5 §2.5 adım 3) | T1 kabulünden önce: §2.2 konumları, §2.5 tahsis, `personel-tasarim-diyagramlari.md` (tarih aralıklı kayıt, düzeltme talebi durumları, senkronizasyon adımları) | Ayrı iş |

---

## 6. TEC.7 — Gerçekleştirme

### 6.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Gerçekleştirme kısıtları | ADR-0016 + mimari test; `KR-117` (oturumsuz uç yok), `KR-118` (sahip olmayan rol), `KR-120` (alan sınıflandırması zorunlu, testli) ✔ | **F** |
| b | Öğe gerçeklenir | T1: 79 SYG'nin 77'si kanıtlı; 73 PR. **Ancak** 5 ürün hatası CI ve onaydan sonra insan gözüyle bulundu (#306 i/İ araması anahtarların ≈%69'unu etkiliyordu; #315 fotoğraf kayboluyor; #338; #357; #373) | **L+** |
| c | Paketlenir veya saklanır | T1 için henüz etiket yok (geliştirme sonu; kabul adayı sonraki adım). CHANGELOG `[Yayımlanmamış]` güncel ✔ | **L+** |
| d | Destekleyici sistemler | Playwright, Testcontainers, UAT performans ölçümü ✔ | **F** |
| e | İzlenebilirlik | Matris §7 "Gerçekleştirme" sütunu satır başına PR ve test sınıfı (2.24–2.65) ✔. Geri yön: parametre (#211) ve LOGO alanı testli; izin ve uç nokta değil (TEC.7-11) | **L+** |

**PA 1.1 = L+** (13.10: L+).

### 6.2 PR disiplini (istenen özel kontrol; #216'nın etkinliği)

**Evren:** 89 PR'ın tamamı (#220–#384). **Gövde örneklemi:** #240, #271, #277, #283, #287, #304, #318, #328, #331, #336, #351, #364, #376, #384 (14 PR).

| Kural | Sonuç |
|---|---|
| Başlık, dal adı, milestone, issue bağlantısı | 89/89 ✔ (CI) |
| `Closes`/`Refs` | 89/89 ✔; `Refs` + ikinci PR'da `Closes` düzeni (#328/#329, #331/#332, #336/#337) ✔ |
| Etiketler (`tur:` + `surec:`) | 89/89 PR, 77/77 issue ✔ (CI; 13.10'da 13/23 issue eksikti) |
| Atlanamaz iki DoD maddesi | 89/89 işaretli veya üstü çizili ve gerekçeli ✔ (CI; 13.10'da 0/11). Örnek: #283, #318'de maskeleme "alan yanıttan çıkarılıyor" gerekçesiyle üstü çizili |
| Şablon başlıkları ve DoD bütünlüğü | ✘ T1'in 73 PR'ının 45'inde "Neden?" veya "Notlar" yok; #320–#349 (17 PR) DoD 8–16 maddeye kırpılmış. #351'den beri 17/17 tam (MAN.8-12) |
| PR boyutu (üretilenler hariç) | ≤ 400: **49/89**; medyan 275; > 1.500: **8** (#220, #277, #281, #304, #314, #328, #331, #364). #220 kuraldan önce; 6'sında gerçek gerekçe; #364'te yanlış beyan (MAN.8-13) |
| Onay yorumu birleştirmeden önce (`KR-096`) | 89/89 ✔; aynı hesap açıyor, onaylıyor, birleştiriyor (beklenen) |
| Kodlama standardı (`KR-058`) | Örneklemde Türkçe tanımlayıcı yok (`src/` sınıf/değişken adı taraması); yorumlar Türkçe ✔ |

### 6.3 Geri yön kapsamı (#211'in etkinliği)

- `ParameterCatalogScopeTests`: her `PRM-` bir SYG'nin Parametre sütununda ve kodda; T1 parametreleri (`PRM-BLD-01`, `PRM-KVK-03` istisnası) testten geçiyor ✔.
- T1'de yeni: `LogoFieldScopeTests`, LOGO'dan yalnızca SYG'deki alanların okunduğunu ve din/sağlık alanlarının hiç okunmadığını sınıyor ✔. İyi bir genişleme.
- 13.10 TEC.7-7'nin ikinci adımı (izin → SYG, uç nokta → SYG) yapılmadı. T1 yeni izinler ve uçlar ekledi; `KR-128` ve `KR-138` gibi izin kararları SYG'ye elle işlendi, otomatik denetim yok.

### 6.4 PA 2.1 / PA 2.2 ve 13.10 durumu

- **PA 2.1 = L** (13.10: L). (a) Boyut hedefi ölçülüyor ✔. (c) İş paketleri issue'lu; büyük özellikler arka uç/ekran olarak bölündü ✔. Boyut ve DoD denetimleri delikli ✘. Hatalar onaydan sonra bulunuyor ✘.
- **PA 2.2 = L** (13.10: L). (a)(b) ✔. (c) Matris ve CHANGELOG ✔. (d) Onay ✔; DoD kaydı 17 PR'da eksik ✘. (e) ✔.

| 13.10 işi | Durum |
|---|---|
| TEC.7-7 Geri yön (#211) | ⚠ Parametre ✔, LOGO alanı ✔ (yeni); izin, uç nokta ✘ |
| TEC.7-8 DoD CI'da (#216) | ⚠ İki madde ✔; öteki maddeler silinebiliyor |
| TEC.7-9 PR boyutu (#216) | ⚠ Kural ve CI ✔; regex şablonla geçiyor |
| TEC.7-10, TEC.7-6 | ✅ #217 (`KR-111`) |

### 6.5 Yeni bulgular

| Kimlik | Önem | Bulgu ve kanıt | Öneri | Nasıl |
|---|---|---|---|---|
| **TEC.7-11** | Orta | **Geri yön kapsamı izin ve uç noktaya genişlemedi.** §6.3 | Mimari teste: `Permissions` sınıfındaki her izin bir SYG'de ve `izin-listesi.md`'de; OpenAPI'deki her yol bir SYG'de anılıyor | Ayrı iş |
| **TEC.7-12** | Orta | **Onaydan sonra bulunan hatalar.** 5 ürün hatası, onaylanıp birleştirilen PR'lardan sonra yerel incelemede bulundu (K6). Biri (#306) arama anahtarlarının ≈%69'unu etkiliyordu ve veritabanının `tr_TR` yerel ayarına bağlıydı | Her hatada "neden yakalanmadı" yanıtlanır (ör. #306: test veritabanının yerel ayarı üretimle aynı mı?). Ekran içeren PR'da onaydan önce yerel deneme maddesi (MAN.8-16 ile birlikte) | Ayrı iş |

---

## 7. Süreçler arası gözlemler

1. **Düzeltici işler hızlı ve otomatik; ama denetimler "sözcük" arıyor.** 13.10'un önerileri üç günde CI kuralı oldu. Bu dönemin bulgularının çoğu, kuralın biçimini sağlayıp amacını kaçıran durumlar: tarih var ama İK yok (TEC.2-7); "Boyut gerekçesi" var ama gerekçe yok (MAN.8-13); iki madde var ama liste kırpık (MAN.8-12).
2. **Kopyaların bakımı hâlâ elle.** SYG sayımları ve başlık tarihleri artık CI'da ve hiç kaymadı. CI'ın görmediği kopyalar yine kaydı: ADR dizini, ADR başlıkları, PG §2 özeti, SYG başlığı (TEC.5-9, TEC.2-8). Çare aynı: ucuz bir tutarlılık denetimi.
3. **İnceleme kanalı değişiklik kanalına dönüştü.** Dokuz talebin sekizi PR onay yorumundan doğdu. Bu, inceleyenin ürünü gerçekten denediğini gösterir (5 hata da böyle bulundu). Ama onay yorumu aynı anda hem "kod uygun" hem "gereksinim değişsin" anlamı taşıyor ve PG'nin sahibi İK devreden çıkıyor.
4. **Bağımsızlık.** T1 tasarım gözden geçirmesindeki alt ajan, 19 bulgunun 4'ü yüksek önemli olmak üzere, gerçek bir ikinci göz sağladı. Bu yöntem PR incelemesine ve kaçan hataların kök neden analizine de uygulanabilir. İnsan veya kurum dışı bağımsızlık yine yok (R-23).
5. **İyi uygulamalar (korunmalı):**
   - tasarım gözden geçirmesinin koddan önce, bulguların SYG'den önce kapanması;
   - türetilmiş davranışın aynı PR'da `AN-` olması (on örnek);
   - her değişiklik talebinin PG/SYG geçmişine, ADR'ye ve matrise aynı PR'da işlenmesi;
   - `main` kırmızısının otomatik kaydı (5/5);
   - büyük özelliğin arka uç ve ekran olarak iki PR'a bölünmesi.

---

## 8. Öncelikli iş listesi (birleşik)

| # | Öncelik | İş | Bulgu | Nasıl |
|---|---|---|---|---|
| 1 | Yüksek | Değişiklik talebinde onaylayan rol denetimi; #284, #321, #333, #355 için İK'dan geriye dönük teyit; dokuz talebin T1 kabul planına alınması | TEC.2-7 | Ayrı iş |
| 2 | Yüksek | ADR dizini, ADR-0022 §3–§4, ADR-0001, başlık KR listeleri; dizin ↔ geçmiş CI denetimi | TEC.5-9 | Belge bu işte; CI ayrı iş |
| 3 | Orta | Boyut regex'i; şablon başlıkları ve DoD bütünlüğü CI'da; 17 PR için `[DÜZELTİCİ]` kaydı | MAN.8-12, MAN.8-13 | Ayrı iş |
| 4 | Orta | Geri yön kapsamı: izin ve uç nokta | TEC.7-11 | Ayrı iş |
| 5 | Orta | Onaydan sonra bulunan hatalar: "neden yakalanmadı" alanı, onay öncesi yerel deneme | TEC.7-12, MAN.8-16 | Ayrı iş |
| 6 | Orta | `KR-131`, `KR-130` için `KR-099` sınıflandırması | TEC.5-10 | Ayrı iş |
| 7 | Orta | T1 tahsis (§2.2 konumları, §2.5) ve diyagramlar, kabulden önce | TEC.5-11 | Ayrı iş |
| 8 | Orta | Büyük PR'da inceleme notu: uygula veya `KR-` ile kaldır | MAN.8-14 | Ayrı iş |
| 9 | Orta | K1–K6'nın ana rapora ve durum raporuna alınması | MAN.8-15 | Bu işte |
| 10 | Orta | PG-PERS §2, SYG-PERS başlığı; KPÖ ayrılışı için AN; AN-19…29 geri bildirimi | TEC.2-8, TEC.3-9, TEC.3-10 | Bu işte (belge) |
| 11 | Düşük | Onay kaydına #114/#118 notu | MAN.8-17 | Bu işte |

---

## Ek — Başlıca kanıt konumları

- `docs/33061/MAN.8-kalite-guvence/{YAKLASIM.md, kayitlar/2026-10-15-kg-olcutleri-t3-baslangic.md, kayitlar/2026-10-04-pr-onay-kaydi.md}`
- `docs/33061/TEC.2-paydas-ihtiyac-ve-gereksinimleri/{YAKLASIM.md §4.1, kayitlar/2026-10-19-personel-gereksinim-toplantisi.md, paydas-gereksinimleri/PG-PERS.md (1.0–1.8)}`
- `docs/33061/TEC.3-sistem-yazilim-gereksinimleri/{YAKLASIM.md §4, gereksinimler/SYG-PERS.md (0.1–0.24)}`
- `docs/33061/TEC.5-tasarim-tanimlama/{YAKLASIM.md §2.4–§2.5, raporlar/2026-10-22-t1-tasarim-gozden-gecirme.md}`
- `docs/adr/README.md` (0.14), `ADR-0001`, `ADR-0014` (0.3), `ADR-0019` (1.4), `ADR-0020` (1.5), `ADR-0021` (1.6), `ADR-0022` (1.7)
- `docs/karar-kayit-defteri.md` (`KR-111`…`KR-138`); `docs/33061/MAN.1-proje-planlama/roller-ve-sorumluluklar.md` §3.1
- `docs/33061/izlenebilirlik-matrisi.md` (2.65, §7)
- `.github/workflows/pr-traceability-check.yml` (madde 6–10), `.github/scripts/{pr-discipline-test.mjs, change-request-approval-test.mjs, doc-consistency-check.mjs, requirement-traceability-check.mjs}`, `.github/PULL_REQUEST_TEMPLATE.md`
- `src/backend/tests/Dsg.Hrms.Architecture.Tests/ParameterCatalogScopeTests.cs`, `src/backend/tests/Dsg.Hrms.Infrastructure.Tests/Logo/LogoFieldScopeTests.cs`
- GitHub: issue #207–#219, #270, #284, #321, #333, #344, #348, #352, #355, #374, #306, #315, #338, #357, #373; PR #220–#384 (onay yorumu 89/89)

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-11-12 | 1.0 | İlk sürüm (#385) | Bilgi İşlem |
