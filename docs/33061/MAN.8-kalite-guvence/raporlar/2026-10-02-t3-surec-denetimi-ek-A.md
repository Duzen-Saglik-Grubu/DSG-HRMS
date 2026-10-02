# 33061 Süreç Gözden Geçirmesi — Bulgular A (MAN.1, MAN.2, MAN.4, MAN.5, MAN.6)

**Değerlendirme tarihi:** 2026-10-02
**Dönem:** 18.09.2026 gözden geçirmesi (MAN.8-SGR-2026-09-18) sonrası — T3 Kimlik Yönetimi geliştirmesi (#62–#116, 24.09–02.10.2026)
**Dayanak:** TS ISO/IEC TS 33061 Madde 5.4.2, 5.4.3, 5.4.5, 5.4.6, 5.4.7 · TS ISO/IEC 33020 Madde 5.2.4.2 (PA 2.1 a–g), 5.2.4.3 (PA 2.2 a–e), 5.3 ölçek
**Yöntem:** Salt okuma. Depo (`main` @ 4199526), `git log`, `gh issue/pr/run/api/project` çıktıları. "Planlandı / yapılacak" ifadeleri kanıt sayılmadı. Ölçek: N (%0–15) · P (>15–50) · L (>50–85) · F (>85–100); `−/+` alt ayrımı 00-OLGUNLUK §2'ye göre.

---

## 0. Ortak gözlemler (beş sürecin hepsini etkileyen)

| # | Gözlem | Kanıt |
|---|---|---|
| O-1 | **T3 boyunca beş MAN sürecinden hiçbiri iş kalemi üretmedi.** 18.09 sonrası açılan 33 PR'ın 20'si `modul:kimlik`; `surec:MAN.2` ve `surec:MAN.4` etiketli **hiç** issue yok (projenin tamamında); `surec:MAN.1` son issue #50 (18.09), `surec:MAN.6` tek issue #54 (18.09). | `gh issue list --state all` |
| O-2 | **Planlama ve yönetim belgelerinin "Son güncelleme" tarihleri T3'ün gerisinde:** `proje-plani.md` 2026-09-07 (son commit 5bdd7f4, 08.09), `roller-ve-sorumluluklar.md` 2026-09-07, `CHANGELOG.md` 08.09 (o tarihten bu yana **56 PR** birleşti), `risk-kayit-defteri.md` 22.09, `00-OLGUNLUK…` 18.09, `is-kirilim-yapisi.md` başlıkta 17.09 / geçmişte 0.3 = 18.09 (kendi içinde çelişkili). Buna karşılık `karar-kayit-defteri.md` (02.10), `izlenebilirlik-matrisi.md` (02.10) ve UAT runbook (02.10) her PR'la güncellenmiş. | `git log -1 -- <dosya>` |
| O-3 | **30.09.2026 `git stash -u` olayı:** TEC.5, TEC.7, TEC.8, TEC.9, TEC.11, TEC.13, MAN.2, MAN.6 klasörleri ve `kayitlar/`/`raporlar/` alt klasörleri silindi. Klasörler boştu ve hiç commit edilmemişti; **olayın kendisi hiçbir yerde kayıtlı değil** (issue, düzeltici faaliyet, karar, risk yok). `git stash list` ve reflog'da iz yok. Doküman haritası (§2.2) hâlâ bu klasörleri "kanıt yeri" olarak gösteriyor. | `ls docs/33061`, `00-DOKUMAN-HARITASI.md` |
| O-4 | **Hiçbir PR'da kayıtlı gözden geçirme (review) yok:** 56 birleştirilmiş PR'ın tamamında `reviews=[]`; yazar ve birleştiren aynı kişi (`dogusduzen`). 00-OLGUNLUK §5(d) "en az bir gözden geçiren onayı zorunludur", A1 kapanışı §3 "Kod gözden geçirme ✅ Etkin — en az bir onay" diyor. Bu iddia GitHub kayıtlarıyla **doğrulanamıyor**; PA 2.2(d) için kanıt yalnızca PR şablonundaki öz-kontrol listesidir. | `gh pr list --json reviews` |
| O-5 | **Hiçbir sürecin `YAKLASIM.md` belgesi yok** (yalnız TEC.2 ve TEC.3'te var — iyi bir şablon oluştu). WBS 1.9 hâlâ "⏳ Bekliyor". | `docs/33061/*/YAKLASIM.md` |

---

## 1. MAN.1 — Proje Planlama

**Amaç (özet):** Etkili ve uygulanabilir planlar üretmek ve koordine etmek; planlar proje boyunca **düzenli revize edilir**.

### 1.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Hedefler ve planlar tanımlanır | `proje-plani.md` v0.1 (H1–H7, aşama planı, kısıtlar), `is-kirilim-yapisi.md` v0.3, `vizyon-ve-kapsam.md` v0.1. **Ancak plan 07.09'dan beri revize edilmedi** ve gerçeklikle çelişiyor: (1) WBS §4 kritik yol `T1 → T2 → T3` diyor, fiilen T3 önce yapıldı (`KR-077`, 24.09) — plan/WBS güncellenmedi; (2) plan §3'te sistem gereksinimi `REQ-…`, fiilen `SYG-…` (harita v0.4, 24.09); (3) plan §6.2 UAT "Kurulacak", §6.3 B3/B8 "Bekliyor/Kurulacak" — UAT 16.09'da kuruldu (#37), GitHub 08.09'da (#1); (4) plan §6.2 UAT verisi "maskelenmiş", fiilen UAT **gerçek LOGO verisiyle** çalışıyor (runbook §10, `KR-083`); (5) T3'ün kalan çevrim adımları (9–12: UAT kabul, kabul formu, kapanış, `v0.2.0`) için ne issue ne pano kalemi var (pano: 116 öğe, yalnız 6'sı açık). | **L−** |
| **b)** Roller, sorumluluklar, hesap verebilirlik, yetkiler tanımlanır | `roller-ve-sorumluluklar.md` RACI (süreç ve karar bazlı). **Teyit edilmedi** (§7 dört açık iş). RACI'nin fiilen uygulanmadığı örnek: "Riskin kabulü (puan ≥ 6) — A: Üst Yönetim" kuralına karşın `KR-078` (25.09) kalan riski **Bilgi İşlem** kaynağıyla kabul etti. | **L** |
| **c)** Kaynaklar resmen talep edilir ve taahhüt edilir | **YOK.** Plan "taslak — üst yönetim onayı bekliyor" (25 gündür); vizyon-kapsam "İK onayı bekliyor"; `A0 — Hazırlık ve Planlama` milestone'u hâlâ **açık**; B2 (PostgreSQL), B4 (üretim), B5 (NAS yedek), B9 (yedekleme/RPO-RTO) "Bekliyor". Bütçe kalemi (33061 çıktısı *Project budget*) yok — plan §6.4 yalnızca "ücretsiz araçlar" der. | **P−** |
| **d)** Planlar yürürlüğe konur (activated) | A1 yürütüldü ve kapandı (MAN-004, 12.09); T3 `KR-068` kapısı (İK toplantısı 23.09, PG-KMLK onayı) geçilerek başlatıldı ve 20 PR ile geliştirildi. Ancak MAN.1.BP3.1 *"projenin onayını al"* karşılanmadan; *Project planning record* (`MAN.1/kayitlar/` plan revizyonları) **YOK**. | **L** |

**PA 1.1 = L−** (18.09: L) — gerileme: plan güncelliğini yitirdi, (c) hâlâ yok.

### 1.2 Temel uygulamalar (BP)

- **BP1** (projeyi tanımla): hedef, kısıt, kapsam, yaşam döngüsü, WBS var ✔. BP1.5 "projede uygulanacak süreçleri tanımla ve sürdür" — 13 süreçte YAKLASIM yok ✘.
- **BP2** (planla): BP2.1 takvim/iş tahmini **yok** (plan §5 "tarih değil sıra" — bilinçli, ama A2 milestone'unun bile hedef tarihi yok, T3 için süre tahmini yok); BP2.2 karar kapıları G1–G4 tanımlı ✔; BP2.3 maliyet/bütçe **yok** ✘; BP2.4 roller ✔; BP2.5 altyapı ✔ (güncel değil); BP2.7 planın duyurulması — kanıt yok.
- **BP3** (etkinleştir): BP3.1 onay ✘; BP3.2 kaynak taahhüdü ✘; BP3.3 uygulama ✔.

### 1.3 PA 2.1 / PA 2.2 boşlukları

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L−** (18.09: L) | (a) hedefler yazılı ✔ · (b) `R-15` planlama riskini kapsıyor ✔ · **(c) izleme ve ayarlama zayıf:** plan, sıra değişikliği (`KR-077`) ve UAT/veri gerçekliği karşısında ayarlanmadı · (d) RACI örnek nitelikte ✔ ama uygulanmadığı bir örnek var (`KR-078`) · (e) kaynak: UAT sağlandı, üretim/yedek bekliyor · (f) yetkinlik tablosu 07.09'dan beri güncellenmedi (T3'te kullanılan ASP.NET Identity/JWT, Testcontainers vb. hâlâ "⏳ Yeni / 🔄") · (g) İK arayüzü 23.09 toplantısıyla işledi ✔. |
| PA 2.2 | **P+** (18.09: L) | (a)/(b) MAN.1 YAKLASIM yok · (c) Git sürüm kontrolü ✔ ama "Son güncelleme" alanları çelişkili (WBS) · **(d) plan ve RACI hiç onaylanmadı** — 18.09 derecesi "sürümlü ve onaylı" gerekçesiyle L verilmişti; onay fiilen yok, derece düzeltildi · (e) `kayitlar/` (plan revizyon kaydı) yok. |

### 1.4 18.09'dan bu yana değişim

- ✅ **İyileşen:** WBS §5.1–5.2 milestone eşlemesi ve atama kuralı yazıldı (#50, PR #51); `SK` milestone'u açıldı; modül başlangıç koşulu yazıya döküldü (`KR-068`, PR #47).
- ⚠️ **Kısmi gerileme:** Milestone kuralı ihlal edildi — #79/PR #80 `surec:TEC.10` etiketli UAT hatası `A2 — Temel Modüller` altında (WBS §5.2 kural 3). 7 issue ve 41 PR öncelik etiketi taşımıyor.
- ❌ **Kapanmayan:** BULGU-11 (plan ve RACI onayı) — hiçbir ilerleme yok.
- ❌ **Yeni:** Plan, T3 sırasında alınan sıra kararıyla (`KR-077`) çelişir hâle geldi; revizyon yapılmadı.

### 1.5 Düzeltici eylemler

| Öncelik | Eylem (issue olacak büyüklükte) |
|---|---|
| **Yüksek** | Proje planı v0.2 revizyonu: `KR-077` sıra değişikliği (T3 önce), `SYG-` kimliği, UAT'nin gerçek veriyle çalışması, B3/B8 durumları; değişiklik `MAN.1/kayitlar/2026-10-xx-plan-revizyonu.md` olarak kaydedilsin. |
| **Yüksek** | Plan + vizyon-kapsam + RACI için üst yönetim/İK onayının alınması ve onay kaydının (tarih, onaylayan) belge başlığına ve `kayitlar/`'a işlenmesi; ardından `A0` milestone'unun kapatılması. |
| **Orta** | T3 çevriminin kalan adımları (UAT kabul testi, kabul formu, kapanış gözden geçirmesi, `v0.2.0`/T3 baseline) için issue açılıp A2'ye atanması — panoda "bitti" görünen modülün kalan işi görünür olsun. |
| **Orta** | `MAN.1/YAKLASIM.md`: plan revizyon tetikleyicileri (her modül kapanışı, sıra değişikliği kararı), onay yolu, kayıt biçimi. |
| **Düşük** | Yetkinlik tablosunun (RACI §4.1) T3 sonrası güncellenmesi; WBS 1.8/1.9 durumlarının ve başlık tarihinin düzeltilmesi. |
| **Düşük** | Bütçe/maliyet kalemi: "lisans maliyeti 0, TSE dokümanı, sunucu kaynakları" şeklinde bile olsa yazılı hâle getirilmesi (33061 çıktısı *Project budget*). |

---

## 2. MAN.2 — Proje Değerlendirme ve Kontrol

**Amaç (özet):** Planların uyumlu ve uygulanabilir olduğunu değerlendirmek, durumu belirlemek, sapmaları düzeltmek; **dönemsel ve önemli olaylarda** değerlendirme.

**Klasör durumu:** `docs/33061/MAN.2-proje-degerlendirme-ve-kontrol/` **depoda yok** (hiç commit edilmemişti; 30.09'da yerelde de silindi). Süreç kanıtı yalnızca başka yerlerde.

### 2.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Performans ölçütleri veya değerlendirme sonuçları mevcut | Plan §8.1'deki 5 ölçütün (modül çevrim süresi, açık/kapanan risk, test kapsamı, kabul bulgusu, değişiklik talebi) **hiçbiri ölçülüp kaydedilmedi.** Dağınık ham veri var: PR gövdelerinde test sayıları (ör. PR #116: 743 test), CI çalışmaları, risk özeti tablosu. T3 çevrim süresi hesaplanabilir (toplantı 23.09 → son geliştirme PR'ı 02.10) ama hiçbir yerde hesaplanmamış. | **P−** |
| **b)** Rol/sorumluluk yeterliliği değerlendirilir | YOK. | **N** |
| **c)** Kaynak yeterliliği değerlendirilir | YOK (tek geliştirici `R-05` ve kaynak taahhüdü hiç değerlendirilmedi). | **N** |
| **d)** Teknik ilerleme gözden geçirmeleri yapılır | A1 kapanış değerlendirmesi (12.09) ve 18.09 süreç gözden geçirmesi. T3 boyunca ara gözden geçirme **yok**. | **P** |
| **e)** Plandan sapmalar incelenir ve analiz edilir | Ürün düzeyinde güçlü: #97, #102, #103 (saat farkı kök nedeni, runbook §12), #93 (çift denetim izi), #89 (kararsız kapsam ölçümü, açık). Proje düzeyinde sapma (sıra değişikliği `KR-077`) karar olarak kayıtlı, ama plana etkisi analiz edilmedi. | **L−** |
| **f)** Etkilenen paydaşlar proje durumundan haberdar edilir | **Dönemsel durum raporu hiç üretilmedi** (plan §8 ve §9: üst yönetime yazılı durum raporu). İK ile toplantı (23.09) var; üst yönetime kanıtlı bildirim yok. | **P−** |
| **g)** Hedef tutmadığında düzeltici faaliyet tanımlanır ve yönlendirilir | #66, #67 (25.09, düzeltici faaliyet, aynı gün kapandı, CI kapısı eklendi — PR #68–#71), #72 ve #89 açık izleniyor. | **L** |
| **h)** Gerektiğinde yeniden planlama başlatılır | `KR-077` fiilî bir yeniden planlamadır; plan/WBS'e yansıtılmadı, MAN.2 kaydı yok. | **P** |
| **i)** Bir sonraki kilometre taşına geçiş yetkilendirilir | T3 başlangıcı G1 kapısı (İK onayı 23.09, `PG-KMLK.md` "Onaylandı") ile yetkilendirildi ✔; A1→A2 geçişi A1 kapanışıyla ✔. Ancak A1 kapanışı "üst yönetim bilgisine sunulacak" durumunda kaldı. | **L** |
| **j)** Proje hedefleri başarılır | Henüz modül kabulü yok (T3 G3 kapısı geçilmedi) — kısmen "sırası gelmedi". | **N** (sırası gelmedi) |

**PA 1.1 = P** (18.09: P+) — gerileme: taahhüt edilmiş durum raporu dönemi ikinci kez geçti; T3 için ara gözden geçirme yapılmadı.

### 2.2 Temel uygulamalar

- **BP1** (değerlendirme ve kontrol stratejisi) — YOK (YAKLASIM yok; plan §8 tablosu tek dayanak).
- **BP2** (değerlendir) — BP2.6 "ölçülmüş başarı ve milestone tamamlanması": milestone sayıları var (A2: 43 kapalı/1 açık) ama analiz yok; BP2.10 "durum ve bulguları kaydet" ✘.
- **BP3** (kontrol et) — BP3.1 düzeltici eylemler ✔; BP3.2 yeniden planlama ✘ (kayıtsız); BP3.4 geçiş yetkisi ✔ (G1).

### 2.3 PA 2.1 / PA 2.2 boşlukları

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **P** (18.09: P+) | (a) ölçütler tanımlı ama hedef değerleri yok · (c) izleme sıklığı "dönemsel" — dönem tanımsız, uygulanmadı · (d) RACI'de A/R Bilgi İşlem ✔ · (e) kaynak/araç yok (ölçüm betiği, rapor şablonu) · (g) üst yönetim arayüzü işlemiyor. |
| PA 2.2 | **P** (18.09: L) | Klasör ve YAKLASIM yok; `raporlar/`, `kayitlar/` yok; doküman haritasının gösterdiği kanıt yeri fiziksel olarak mevcut değil (O-3). 18.09'daki L, A1 kapanış belgesinin kontrollü olmasına dayanıyordu; T3 döneminde yeni MAN.2 belgesi üretilmedi. |

### 2.4 18.09'dan bu yana değişim

- ❌ BULGU-04 (durum raporu ve ölçütler) **kapanmadı**; rapor 18.09'da "A2 başlarken" taahhüt edilmişti — A2 başladı (24.09), rapor yok.
- ❌ Klasör kayboldu (O-3).
- ✅ Düzeltici faaliyet döngüsü işlemeye devam etti (#66, #67).

### 2.5 Düzeltici eylemler

| Öncelik | Eylem |
|---|---|
| **Yüksek** | İlk dönemsel durum raporu: `MAN.2-proje-degerlendirme-ve-kontrol/raporlar/2026-10-durum-raporu.md` — T3 ilerlemesi, §8.1'deki 5 ölçütün gerçek değerleri, açık riskler, plan sapması (`KR-077`), üst yönetime iletim kaydı. |
| **Yüksek** | `MAN.2/YAKLASIM.md`: "dönemsel"in tanımı (ör. ayda bir + her modül kapanışı), ölçüt formülleri ve veri kaynağı (`gh` sorgusu), eskalasyon eşiği, dağıtım listesi. |
| **Orta** | T3 kapanışında `kayitlar/2026-xx-xx-kimlik-kapanis-gozden-gecirmesi.md` + alınan dersler (saat farkı hataları, stash olayı, kararsız kapsam). |
| **Orta** | Ölçütlerin otomatik üretimi: `gh` ile milestone/etiket sayımı yapan küçük bir betik (çevrim süresi, değişiklik talebi sayısı, düzeltici faaliyet sayısı). |
| **Düşük** | Rol ve kaynak yeterliliğinin (b, c) durum raporunda ayrı başlıkla değerlendirilmesi (`R-05` dâhil). |

---

## 3. MAN.4 — Risk Yönetimi

**Amaç (özet):** Riskleri **sürekli** belirlemek, analiz etmek, ele almak ve izlemek.

### 3.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Riskler belirlenir | `risk-kayit-defteri.md` 18 risk (R-01…R-18). **T3 geliştirmesi (24.09–02.10) boyunca yeni risk eklenmedi**, oysa dönemde risk niteliğinde olaylar yaşandı: sunucu saati sapması (#97, #103 — kimlik doğrulamasını kıran), UAT'nin gerçek LOGO verisi ve gerçek SMTP/NetGSM ile çalışması (`KR-083`), kararsız kapsam ölçümü (#89), kayıtsız çalışma dosyası kaybı (O-3), dal koruma tespit denetiminin 28.09'da çökmesi (bkz. MAN.5). `KR-078` açıkça "kabul edilen kalan risk" tanımlıyor — risk defterinde **yok**. | **L** |
| **b)** Riskler analiz edilir | Her risk O/E/puan ile ✔. **Tutarsızlık:** R-01 kaydında `2/3/6`, §3 özet tablosunda "Puan 9" altında; 6 puanlılar sayısı buna göre yanlış (5 yerine 6 olmalı). | **L+** |
| **c)** Risk ele alma seçenekleri belirlenir, önceliklendirilir, seçilir | Her riskte önlem; puan ≥ 6 aktif takip eşiği ✔. | **F** |
| **d)** Uygun önlem uygulanır | R-12, R-13 veri düzeltmesiyle kapandı (21–22.09) ✔. **T3'te uygulanan önlemler deftere işlenmedi:** R-14 önlem (1) kanal değiştirme ve (2) gönderim kaydı PR #86/#88/#91 ile gerçekleştirildi; R-03 maskeleme/erişim kaydı önlemleri ilerledi; durum hâlâ "Açık", önlem metni "…sunulacak / kaydedilecek" gelecek kipinde. | **L** |
| **e)** Riskler durum değişikliği ve önlem ilerlemesi açısından değerlendirilir | Son gözden geçirme **22.09**. R-08 önlemi (3) "İK istisna akışı tasarlanacak" `KR-076` (23.09: iletişimsiz 2 personel sisteme alınmayacak) ile çelişiyor, güncellenmedi. `KR-069` gerekçesinde "12 aktif personelin hiçbir iletişim kanalı yok (`R-12`)" yazıyor — doğrusu R-08 ve güncel değer 2. Aylık gözden geçirme sınırı (22.10) henüz geçmedi; ancak "her modül kapanışında" tetikleyicisi T3 kapanışında beklenecek. | **L−** |

**PA 1.1 = L+** (18.09: **F**) — **gerileme.** Defter, T3'ün yoğun on gününde işin doğal akışında güncellenmeyi bıraktı; 18.09 raporunun bu süreci ayıran özelliği ("iş yapılırken kayıt oluştu") T3'te sürmedi. Seviye 2 için PA 1.1 = F şartı artık karşılanmıyor.

### 3.2 Temel uygulamalar

- **BP1** (risk yönetimi stratejisi, bağlam) — ölçek ve eşik defterin §1'inde ✔; ayrı strateji/YAKLASIM yok.
- **BP2.1** kabul eşikleri ve koşulları — RACI §3.2 "puan ≥ 6 riskin kabulü: A = Üst Yönetim" ✔ tanımlı; **uygulanmadı** (`KR-078`, kabul eden Bilgi İşlem). Ayrıca "Kabul edildi" durumu (R-16) §1'deki durum değerleri listesinde (`Açık/İzleniyor/Kapandı/Gerçekleşti`) tanımlı değil.
- **BP2.3** risk profilinin dönemsel olarak paydaşlara sunulması — **YOK** (`MAN.4/raporlar/` yok, durum raporu yok).
- **BP3/BP4** ✔ (yukarıdaki istisnalarla).
- **BP5.3** yeni risklerin sürekli izlenmesi — T3 döneminde ✘ (bkz. a).

### 3.3 PA 2.1 / PA 2.2 boşlukları

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L−** (18.09: L) | (c) gözden geçirme sıklığı tanımlı ama T3 olayları tetikleyici sayılmadı · (d) kabul yetkisi kuralı çiğnendi (`KR-078`) · (g) üst yönetim/İK'ya risk iletişimi kanıtı yok · risk raporu taahhüdü (plan §8) hâlâ karşılanmıyor. |
| PA 2.2 | **L−** (18.09: L) | (a)/(b) YAKLASIM yok · (c) yapısal kusurlar: R-17 ve R-18 kayıtları §3 özetinin **arkasına** düşmüş (bölüm sırası bozuk), özet tablo ile kayıt değerleri çelişiyor (R-01) · (d) değişiklikler PR'dan geçiyor ✔ ama kayıtlı inceleme yok (O-4) · (e) saklama süresi tanımsız. |

### 3.4 18.09'dan bu yana değişim

- ✅ 21.09 ve 22.09'da LOGO yeniden ölçümüyle R-13 ve R-12 kapandı, R-08 güncellendi (PR #59, #61) — iyi uygulama.
- ❌ 22.09'dan sonra **hiç güncelleme yok** (T3'ün tamamı bu dönemde).
- ❌ Defter içi tutarsızlıklar (R-01 puanı, bölüm sırası) eklendi/fark edilmedi.

### 3.5 Düzeltici eylemler

| Öncelik | Eylem |
|---|---|
| **Yüksek** | T3 risk gözden geçirmesi: R-03, R-08 (`KR-076` ile), R-14 (uygulanan önlemler, PR #86/#88/#91) güncellensin; yeni riskler eklensin: `KR-078` kalan risk (eşleşme ifşası), sunucu saati sapması, UAT'de gerçek veri + gerçek gönderim, yerel çalışma dosyası kaybı. |
| **Yüksek** | `KR-078` risk kabulünün RACI'ye uygun hâle getirilmesi: ya üst yönetim onayı alınsın ya da puanı < 6 olarak gerekçelendirilip deftere işlensin. |
| **Orta** | Defter tutarlılık düzeltmesi: R-01 puanı (6 mı 9 mu — gerekçesiyle), §3 özet sayıları, R-17/R-18'in §2'ye taşınması, "Kabul edildi" durum değerinin §1'e eklenmesi. |
| **Orta** | İlk dönemsel risk raporu `MAN.4/raporlar/2026-10-risk-raporu.md` (veya MAN.2 durum raporu içinde ayrı bölüm — plan §8 hangisini söylüyorsa). |
| **Düşük** | `MAN.4/YAKLASIM.md`: tetikleyiciler ("her `tur:hata` yüksek öncelikli issue'da risk etkisi sorulur"), kabul yetkisi, raporlama. PR şablonuna "Risk etkisi" satırı eklenmesi. |

---

## 4. MAN.5 — Konfigürasyon Yönetimi

**Amaç (özet):** Sistem öğelerini ve konfigürasyonları yaşam döngüsü boyunca yönetmek ve kontrol etmek; ürün ile konfigürasyon tanımı arasındaki tutarlılığı sağlamak.

### 4.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Konfigürasyon yönetimi gerektiren öğeler belirlenir ve yönetilir | Git 478 izlenen dosya; `.gitignore` neyin dışarıda kaldığını gerekçeli yazıyor. **`konfigurasyon-ogeleri.md` YOK** (BULGU-05 açık). Depo dışında kalan ama ürünün parçası olan öğeler (UAT `secrets/.env.uat`, sunucudaki `dsg-renew-tls.sh`, NTP ayarı, TLS sertifikası) yalnızca runbook'ta anlatılıyor; konfigürasyon öğesi olarak tanımlı değil. | **P+** |
| **b)** Konfigürasyon baseline'ları oluşturulur | **Git etiketi 0, GitHub Release 0.** Plan §5: A1'in çıktısı `v0.1.0`; A1 15.09'da kapandı (milestone kapalı) — etiket **üretilmedi**. 18.09'da "sırası gelmedi" sayılmıştı; A1 için bu gerekçe geçerli değil. | **P−** |
| **c)** Konfigürasyon altındaki değişiklikler kontrol edilir | Güçlü ve gelişti: zorunlu PR + issue bağlantısı (PR izlenebilirlik denetimi, 69 çalışma), PR başlığı denetimi (#44), **dal adı denetimi** (#67, PR #71, 25.09), squash merge, `main`'e gelen her commit için tespit edici denetim. **Zayıflıklar:** (1) 56 PR'da kayıtlı inceleme yok (O-4); (2) Dal Koruma Denetimi **28.09.2026'da 387c4d2 (PR #94) için GitHub API 500 hatasıyla çöktü** (`retries: 0`), yeniden çalıştırılmadı, kayıt/issue açılmadı — o commit denetlenmemiş durumda; (3) `dal-koruma-telafi-kontrolleri.md` 26.09'da (#82) değişti ama "Son güncelleme 2026-09-09" ve değişiklik geçmişi güncellenmedi. | **L** |
| **d)** Konfigürasyon durum bilgisi erişilebilir | Git geçmişi, GitHub Projects (116 öğe, 110 Done), milestone'lar, izlenebilirlik matrisi (PR ↔ SYG). **UAT'de hangi commit'in çalıştığı bilinmiyor:** runbook §3.1 dağıtımı geliştirici çalışma kopyasından `tar` ile yapıyor (commit edilmemiş değişiklik de gidebilir), imajlar `:uat` sabit etiketiyle derleniyor, dağıtım kaydı tutulmuyor. | **L−** |
| **e)** Gerekli konfigürasyon denetimleri tamamlanır | **YOK.** `MAN.5/raporlar/` yok; dal-koruma belgesi §6.3 "telafi kontrollerinin dönemsel örneklenmesi 🔄 Sürekli" — hiç örnekleme kaydı yok (28.09 çökmesi bu yüzden fark edilmedi). | **N** |
| **f)** Sistem sürümleri ve teslimleri kontrol edilir ve onaylanır | T3 parçaları UAT'ye dağıtılıyor (runbook v0.5–1.6: senkronizasyon etkinleştirme 26.09, ilk sistem yöneticisi 29.09, saat eşitleme 30.09, HTTPS doğrulaması 02.10) ve UAT **gerçek LOGO verisiyle** çalışıyor. Sürüm talebi, sürüm onayı, sürüm kimliği **yok**. 18.09'da "sırası gelmedi" sayılan bu çıktının sırası, gerçek veriyle çalışan ortama dağıtımla **geldi**. | **P−** |

**PA 1.1 = P+** (18.09: L−) — **gerileme.** Değişiklik kontrolü (c) güçlenmesine karşın, baseline (b), denetim (e) ve sürüm kontrolü (f) T3'ün UAT'ye girmesiyle artık "sırası gelmedi" sayılamıyor.

### 4.2 Temel uygulamalar

- **BP1** strateji, saklama/arşiv/geri getirme yordamı — YAKLASIM yok; CONTRIBUTING §1–§4 dallanma/commit kuralları kısmen karşılıyor.
- **BP2** tanımlama — BP2.1 öğe seçimi ✘, BP2.3 baseline tanımı yalnızca plan §5.1'de (SemVer) ✔/uygulanmadı.
- **BP3** değişiklik yönetimi — ✔ (`tur:degisiklik-talebi` #77 örnek).
- **BP4** sürüm kontrolü — ✘.
- **BP5** durum muhasebesi / değerlendirme — kısmen (Git); CM denetimi ✘.

### 4.3 PA 2.1 / PA 2.2 boşlukları

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L−** (18.09: L) | (c) kontroller CI'da planlı ve izleniyor ✔; ama kontrolün kendisinin arızası (28.09) izlenmiyor · (e) araçlar yeterli · (b) R-16 kabul edilmiş ve telafi kontrolleri kayıtlı ✔. |
| PA 2.2 | **L−** (18.09: L) | **`CHANGELOG.md` 08.09'dan beri değişmedi** — 56 PR, T3'ün tamamı yok; "Yayımlanmamış" bölümü yalnız A0 içeriğini sayıyor (BULGU-05 açık, büyüdü) · belge başlığı/geçmiş güncellenmeden yapılan değişiklik (dal-koruma) · kayıtlı inceleme yok (O-4). |

### 4.4 18.09'dan bu yana değişim

- ✅ Dal adı denetimi (#67/PR #71), tanımlayıcıların İngilizceleştirilmesi (#66/PR #68–#70), kod dosya adları (#81/PR #82) — değişiklik kontrolü güçlendi.
- ❌ BULGU-05'in üç maddesinin hiçbiri kapanmadı (öğe listesi, denetim, CHANGELOG).
- ❌ Yeni: kontrolsüz UAT dağıtımı (gerçek veriyle), tespit edici kontrolün sessiz arızası.

### 4.5 Düzeltici eylemler

| Öncelik | Eylem |
|---|---|
| **Yüksek** | UAT dağıtımının bir commit'e/etikete bağlanması: dağıtım `git archive <etiket>` (veya temiz çalışma kopyası denetimi) ile yapılsın, imaj etiketi commit kısa kimliğini taşısın, `deploy-uat.sh` her dağıtımda `commit + tarih + yapan` satırını bir dağıtım kaydına yazsın. |
| **Yüksek** | `CHANGELOG.md`'nin A1 ve T3 içeriğiyle güncellenmesi; A1 için geriye dönük `v0.1.0` etiketi (A1 kapanış commit'i 065a7c3 veya 15665d2 — gerekçesiyle) ve T3 UAT'ye sunulurken bir ön sürüm etiketi (ör. `v0.2.0-rc.1`). |
| **Yüksek** | 28.09 Dal Koruma Denetimi çökmesi (run 36471125839) için kayıt: 387c4d2'nin PR #94 ile geldiğinin elle doğrulanması; iş akışına yeniden deneme (`retries`) ve "denetim yapılamadı" durumunda da issue açma eklenmesi. |
| **Orta** | `MAN.5/konfigurasyon-ogeleri.md`: kod, belgeler, CI, Compose/nginx, sunucu tarafı öğeler (sır dosyası konumu, TLS, NTP, yenileme kancası), veritabanı şeması (migration). |
| **Orta** | İlk konfigürasyon denetimi `MAN.5/raporlar/2026-10-xx-konfigurasyon-denetimi.md`: telafi kontrollerinin örneklenmesi (dal-koruma §6.3), UAT'de çalışan sürüm ↔ depo karşılaştırması. |
| **Düşük** | `MAN.5/YAKLASIM.md` (baseline tanımı, sürüm onay yolu, CHANGELOG'u kimin ne zaman güncellediği — ör. PR şablonuna "CHANGELOG güncellendi" maddesi). `dal-koruma-telafi-kontrolleri.md` başlık/geçmiş düzeltmesi. |

---

## 5. MAN.6 — Bilgi Yönetimi

**Amaç (özet):** Bilgiyi üretmek, almak, doğrulamak, dönüştürmek, **saklamak, geri getirmek, yaymak ve imha etmek**.

**Klasör durumu:** `docs/33061/MAN.6-bilgi-yonetimi/` **depoda yok** (hiç commit edilmemişti; 30.09'da yerelde de silindi). Doküman haritası kendisinin MAN.6 kapsamında olduğunu söylüyor.

### 5.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Yönetilecek bilgi belirlenir | `00-DOKUMAN-HARITASI.md` v0.4 (24.09) süreç süreç kanıt yerleri ✔. **Haritanın gösterdiği klasörlerin 8'i fiziksel olarak yok** (TEC.5, 7, 8, 9, 11, 13, MAN.2, MAN.6) ve harita bunu belirtmiyor. Depo dışında tutulan bilgiler haritada yok: `claude/` soru-cevap arşivi (`.gitignore`: "mesajlaşmada paylaşılan parolaları içerir"), Masaüstü\HRMS kişisel veri dosyaları, 23.09 toplantısının "doldurulmuş özgün dosyası Bilgi İşlem'de saklanmaktadır" (TEC.2 kaydı). | **L−** |
| **b)** Bilgi gösterimleri tanımlanır | Kimlik şeması (`PG-`/`REQ-KMLK-`, `SYG-`, `KR-`, `ADR-`, `R-`, `TS-`), tarih biçimi, dil kuralı, şablonlar (`sablonlar/`, TEC.2 toplantı ve PG şablonları) ✔. `REQ`→`SYG` çakışması 24.09'da kontrollü çözüldü ✔. | **L+** |
| **c)** Bilgi elde edilir, geliştirilir, dönüştürülür, saklanır, doğrulanır, sunulur ve **imha edilir** | Elde etme/geliştirme/saklama Git'te ✔. **Saklama süresi ve imha tanımsız** (`KR-023` KVKK görüşü bekliyor; SYG-KMLK AN-16 "karar bekliyor"). **Yedekleme yok:** plan B5/B9 bekliyor; runbook §2/§198 "UAT verisi maskelenmiş test verisidir; düzenli yedeklenmez" derken aynı runbook §10 "UAT gerçek LOGO verisiyle çalışır" diyor — gerçek kişisel veri taşıyan ortam yedeksiz ve belge kendi içinde çelişkili. **Bilgi kaybı olayı kayıtsız** (O-3). Parola içeren depo dışı arşiv (`claude/`) için erişim/imha kuralı yok. | **P** |
| **d)** Bilginin durumu belirlenir | "Son güncelleme" + değişiklik geçmişi kuralı (harita §4.4) çoğu belgede ✔; karar defteri, izlenebilirlik matrisi, runbook örnek nitelikte güncel. **İhlaller:** dal-koruma belgesi (26.09 değişti, başlık 09.09), WBS (başlık 17.09, geçmiş 18.09), plan durum sütunları gerçekliği yansıtmıyor (MAN.1). | **L−** |
| **e)** Bilgi belirlenmiş paydaşlara erişilebilir | Depo özel; erişim Bilgi İşlem'de. İK'ya, üst yönetime, KVKK sorumlusuna hangi bilginin hangi kanalla sunulduğu tanımsız; dağıtım kaydı yok (ör. A1 kapanışı "üst yönetim bilgisine sunulacak" — sunulduğuna dair kayıt yok). | **P** |

**PA 1.1 = P+** (18.09: L−) — **gerileme.** Kayıtsız bilgi kaybı olayı, gerçek veriyle çalışan yedeksiz ortam ve depo dışı bilgi kümelerinin büyümesi (c) ve (e)'yi aşağı çekti.

### 5.2 Temel uygulamalar

- **BP1.1** strateji — YOK (YAKLASIM yok). **BP1.2** yönetilecek öğeler — harita ✔ (eksik ve kısmen yanlış). **BP1.3** yetki/sorumluluk — RACI'de MAN.6 A/R Bilgi İşlem; saklama süresi A = KVKK sorumlusu (rolü resmen bildirilmedi, RACI §7.3). **BP1.4** içerik/biçim ✔. **BP1.5** bakım eylemleri — kısmen (harita §4).
- **BP2.2** depolama kayıtlarının tutulması — **bilgi kayıt defteri YOK** (BULGU-06 açık). **BP2.4** arşivleme ✘. **BP2.5** geçersiz bilginin imhası ✘ (ör. #73 "KR-058 taşıma yedeklerinin temizlenmesi" 25.09'dan beri açık).

### 5.3 PA 2.1 / PA 2.2 boşlukları

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **P** (18.09: P+) | Hedef (ör. "her belge PR'la güncel") yazılı değil; izleme yok — stash olayı hiçbir kontrolden geçmedi; sorumluluk RACI'de var, KVKK rolü bildirilmedi; kaynak (NAS/yedek) taahhüt edilmedi. |
| PA 2.2 | **P+** (18.09: L−) | (a)/(b) YAKLASIM ve klasör yok; (c) Git kontrolü ✔; (d) kayıtlı inceleme yok (O-4); **(e) saklama/yedek/imha düzeni yok** — PA 2.2(e)'nin doğrudan istediği. |

### 5.4 18.09'dan bu yana değişim

- ✅ **En büyük iyileşme:** `izlenebilirlik-matrisi.md` kuruldu (#54, PR #55, 19.09; `surec:MAN.6` etiketli) ve T3 boyunca **her PR'la** güncellendi (v0.1 → v2.0, 20 sürüm). Bilgi yönetiminin "işin doğal akışında kayıt" ilkesinin en iyi örneği.
- ✅ TEC.2 toplantı kaydı depoda (BULGU-09 kısmen kapandı); harita v0.4 güncellendi.
- ❌ BULGU-06 (bilgi kayıt defteri, saklama, yedek) kapanmadı.
- ❌ Yeni: kayıtsız bilgi kaybı (O-3); UAT'nin veri sınıfı belgelerde çelişkili.

### 5.5 Düzeltici eylemler

| Öncelik | Eylem |
|---|---|
| **Yüksek** | 30.09 stash olayının kaydı: düzeltici faaliyet issue'su (`tur:duzeltici-faaliyet`, `surec:MAN.6`) — ne kayboldu, neden (commit edilmemiş boş klasörler), önlem (klasörler `.gitkeep`/`YAKLASIM.md` ile commit edilsin; `stash -u` yerine dal kullanımı kuralı CONTRIBUTING'e). |
| **Yüksek** | Silinen 8 süreç klasörünün en azından `YAKLASIM.md` iskeletiyle yeniden oluşturulup commit edilmesi (MAN.2, MAN.6 öncelikli) — harita ile depo eşleşsin. |
| **Yüksek** | UAT veri sınıfının netleştirilmesi: runbook §1 ve §198, plan §6.2, ADR-0011 §UAT "maskelenmiş" ifadeleri `KR-083` gerçeğiyle hizalansın; gerçek veri taşıyan UAT için yedekleme kararı (yedek alınacak mı, nerede, ne kadar saklanacak) yazılsın. |
| **Orta** | `MAN.6/bilgi-kayit-defteri.md`: her bilgi kümesi için sahip, konum (depo / NAS / Masaüstü\HRMS / `claude/` / UAT DB), gizlilik sınıfı, saklama süresi (KVKK görüşü gelene kadar "geçici"), imha yöntemi. Depo dışı kümeler dâhil. |
| **Orta** | `claude/` soru-cevap arşivindeki parolaların döndürülmesi/temizlenmesi ve arşivin erişim kuralı (sır niteliğinde bilgi düz metin tutulmamalı — ADR-0008 ilkesi). |
| **Düşük** | `MAN.6/YAKLASIM.md`; KVKK sorumlusunun rolünün resmen bildirilmesi (RACI §7.3) ve `KR-023` saklama görüşünün istenmesi için tarihli talep kaydı. |

---

## 6. Derecelendirme özeti

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | 18.09 (PA1.1/2.1/2.2 → Sv) | Eğilim |
|---|---|---|---|---|---|---|
| MAN.1 Proje planlama | L− | L− | P+ | 1 | L / L / L → 1 | ↓ |
| MAN.2 Değerlendirme ve kontrol | P | P | P | 0 | P+ / P+ / L → 0 | ↓ |
| MAN.4 Risk yönetimi | L+ | L− | L− | **1** | **F** / L / L → **2** | ↓↓ (Seviye 2 kaybı) |
| MAN.5 Konfigürasyon yönetimi | P+ | L− | L− | **0** | L− / L / L → 1 | ↓↓ (Seviye 1 kaybı) |
| MAN.6 Bilgi yönetimi | P+ | P | P+ | **0** | L− / P+ / L− → 1 | ↓ (Seviye 1 kaybı) |

> **Okuma notu:** Gerilemenin bir kısmı **ölçüt sıkılaşmasıdır**, bir kısmı **gerçek gerilemedir.**
> - Ölçüt sıkılaşması: MAN.5(b)(f) 18.09'da "sırası gelmedi" sayıldı; T3'ün gerçek veriyle UAT'ye girmesiyle sırası geldi. MAN.1 PA 2.2'de 18.09 "onaylı" demişti; onay fiilen hiç olmamıştı.
> - Gerçek gerileme: risk defteri 22.09'dan sonra durdu (MAN.4); CHANGELOG 25 gün / 56 PR geride; durum raporu ikinci kez gecikti; kayıtsız bilgi kaybı.
> - Teknik süreçlerde (TEC.2/3/7/9) aynı dönemde güçlü ilerleme var; yönetim süreçleri bu temponun **arkasında kaldı**. Sebep tek kişilik kaynaktır (`R-05`, `KS8`) ve MAN süreçlerinin işin akışına (PR şablonu, CI) bağlanmamış olmasıdır — karar defteri ve izlenebilirlik matrisi bağlandığı için güncel kaldı.

## 7. Öncelikli düzeltici eylemler (birleşik sıralama)

| # | Öncelik | Eylem | Süreç |
|---|---|---|---|
| 1 | Yüksek | T3 risk gözden geçirmesi + `KR-078` risk kabulünün RACI'ye uygun hâle getirilmesi | MAN.4 |
| 2 | Yüksek | UAT dağıtımının commit/etikete bağlanması ve dağıtım kaydı | MAN.5 |
| 3 | Yüksek | CHANGELOG güncellemesi; `v0.1.0` (A1) etiketi; T3 ön sürüm etiketi | MAN.5 |
| 4 | Yüksek | İlk dönemsel durum raporu (§8.1 ölçütleri gerçek değerleriyle) | MAN.2 |
| 5 | Yüksek | Proje planı v0.2 revizyonu (`KR-077`, UAT gerçeği, SYG) + onay talebi ve kaydı | MAN.1 |
| 6 | Yüksek | 30.09 stash olayı için düzeltici faaliyet; 8 süreç klasörünün commit edilmesi | MAN.6 |
| 7 | Yüksek | UAT veri sınıfı çelişkisinin giderilmesi ve yedekleme kararı | MAN.6 / TEC.10 |
| 8 | Yüksek | 28.09 Dal Koruma Denetimi çökmesinin kaydı ve iş akışına yeniden deneme | MAN.5 |
| 9 | Orta | `bilgi-kayit-defteri.md` (depo dışı kümeler dâhil); `claude/` parolalarının temizlenmesi | MAN.6 |
| 10 | Orta | `konfigurasyon-ogeleri.md` + ilk konfigürasyon denetimi raporu | MAN.5 |
| 11 | Orta | T3 kalan çevrim adımları için issue'lar (UAT kabul, kabul formu, kapanış, sürüm) | MAN.1 / MAN.2 |
| 12 | Orta | Risk defteri tutarlılık düzeltmesi (R-01, özet, bölüm sırası, durum değeri) | MAN.4 |
| 13 | Orta | MAN.1/2/4/5/6 `YAKLASIM.md` belgeleri (TEC.2/TEC.3 şablonuyla) | Hepsi |
| 14 | Düşük | PR şablonuna "Risk etkisi / CHANGELOG / plan etkisi" maddeleri — MAN süreçlerini PR akışına bağlamak | MAN.1/4/5 |
| 15 | Düşük | Tek geliştirici ortamında PA 2.2(d) için gözden geçirme kanıtının ne olacağının yazılı kararı (öz-kontrol listesi + dönemsel MAN.8 örneklemesi) — 00-OLGUNLUK §5(d) ve A1 kapanışındaki "en az bir onay" iddiasının düzeltilmesi | MAN.5/MAN.6/MAN.8 |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-02 | 0.1 | İlk oluşturma — MAN.1, MAN.2, MAN.4, MAN.5, MAN.6 bulguları (T3 dönemi) | Denetim alt ajanı (salt okuma) |
