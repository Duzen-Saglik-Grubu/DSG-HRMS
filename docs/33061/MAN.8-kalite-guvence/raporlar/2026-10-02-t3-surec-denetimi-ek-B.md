# Bulgular B — MAN.8, TEC.2, TEC.3, TEC.5, TEC.7

**Değerlendirme tarihi:** 2026-10-02
**Dönem:** 2026-09-18 (önceki gözden geçirme, `MAN.8-SGR-2026-09-18`) … 2026-10-02 (PR #116)
**Dayanak:** TS ISO/IEC TS 33061 Madde 5 (çıktılar ve BP'ler) · TS ISO/IEC 33020 Madde 5.2.4 (PA 2.1 a–g, PA 2.2 a–e) · Ölçek N/P/L/F (33020 Madde 5.3)
**Yöntem:** Salt okuma. Depo (`main`, temiz, 4199526), `gh issue/pr` (salt okuma), CI kayıtları. Kanıt yalnızca fiilen varsa sayıldı.
**Bağımsızlık:** Bu bir öz değerlendirme destek çalışmasıdır; 33020 anlamında bağımsız değerlendirme değildir.

---

## 0. Özet tablo

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | 18.09 durumu | Ana engel |
|---|---|---|---|---|---|---|
| MAN.8 Kalite güvence | **L−** | **P+** | **L−** | 1 | L / L / L | 18.09'dan beri süreç değerlendirmesi yok; PR incelemesi hiç yok (0/20); 6 bulgu takipsiz |
| TEC.2 Paydaş gereksinimleri | **L+** | **L** | **L** | 1 | P+ / P+ / L− | Kritik performans ölçütü PG düzeyinde yok; kesinleşmiş gereksinimin yorumu İK'ya dönmeden değişti |
| TEC.3 Sistem/yazılım gereksinimleri | **L+** | **L−** | **L−** | 1 | N / — / — | SYG için onay/baseline yok; §2.2 ve §7 bayat; kritik performans verisi 6 KPÖ'nün 1'inde |
| TEC.5 Tasarım tanımlama | **L** | **P+** | **P+** | 1 | L / L / L | Gereksinim → tasarım öğesi tahsisi yok; ADR kuralı (#2) ihlal ediliyor; YAKLASIM yok |
| TEC.7 Gerçekleştirme | **L** | **L−** | **L−** | 1 | L+ / L / L | Matris §5'te 26 "kısmen" satırı (≈16'sı bayat); 4 SYG gerçeklenmemiş; CHANGELOG/sürüm yok |

**Hiçbir süreç bugün Seviye 2 değil.** TEC.2 ve TEC.3 belirgin biçimde ilerledi (TEC.3: N → L+). MAN.8, TEC.5 ve TEC.7'de 18.09'a göre **gerileme** var: süreçler gerçek bir modülle ilk kez yük altına girince, A1'de görünmeyen zayıflıklar (inceleme yok, PR boyutu, tasarım belgelerinin koddan sonra yazılması, matris bakımı) ortaya çıktı.

---

## 1. İzlenebilirlik matrisi §5 — sayım (istenen özel kontrol)

Kaynak: `docs/33061/izlenebilirlik-matrisi.md` §5 (sürüm 2.0, 2026-10-02), 56 satır.

| Halka | Dolu | "kısmen" | `—` | Not |
|---|---:|---:|---:|---|
| Kaynak (REQ) → Sistem gereksinimi (SYG) | **56** | 0 | 0 | Her PR'da otomatik denetleniyor (`.github/scripts/requirement-traceability-check.mjs`, `pr-traceability-check.yml`) — **F** |
| Tasarım | 55 | — | **1** (#42) | ADR **bölüm** düzeyinde; SYG düzeyinde değil |
| Gerçekleştirme | 55 | **26** | **1** (#42) | 29 satır "tam" |
| Doğrulama (test) | 53 | — | **3** (#42, #44, #47) | |
| Kabul | 0 | — | **56** | Sırası gelmedi (T3 kabul edilmedi) |

### 1.1 "kısmen" işaretli 26 satır

`REQ-KMLK-001, 002, 003, 004, 005, 008, 009, 010, 011, 014, 016, 021, 022, 033, 034, 036, 037, 039, 040, 041, 043, 044, 045, 046, 050, 054`

Satırların içeriği SYG listesiyle tek tek karşılaştırıldığında iki gruba ayrılıyor:

**A) Gerçekten eksik halka taşıyan (10 satır):**

| Satır | Eksik SYG | Neden |
|---|---|---|
| REQ-KMLK-001 | SYG-079 | Teslim süresi ölçümü (UAT) yok |
| REQ-KMLK-016 | SYG-079 | Aynı |
| REQ-KMLK-021 | SYG-079 | "079 ölçümü UAT'de" |
| REQ-KMLK-022 | SYG-077 | Giriş p95 < 1 sn yük testi yok |
| REQ-KMLK-033 | SYG-049 (ölçüm) | "100–500 ms ölçümü UAT'de" (KPÖ-5) |
| REQ-KMLK-034 | SYG-078 | Tam senkronizasyon < 60 sn ölçümü yok |
| REQ-KMLK-037 | SYG-053 | **Kayıt eksiği:** SYG-053 PR #108'de gerçeklendi (satır 11'de yazılı) ama satır 37 güncellenmedi — hâlâ "053 (davet) sonraki iş" |
| REQ-KMLK-040 | **SYG-060** | Kimlik olaylarının denetim izine yazılması **gerçeklenmedi** — kodda/testte SYG-060 atfı yok, kimlik olay türü yok |
| REQ-KMLK-041 | **SYG-061** | "061 kimlik olayları işinde" — gerçeklenmedi / kanıtlanmadı |
| REQ-KMLK-044 | SYG-065 | SYG'de doğrulama yöntemi **Test**, matriste "ölçüm kabulde (Gösterim)", test sütunu `—` → çelişki (TEC.3 YAKLASIM §3.3: "Test yazılmış madde CI'da otomatik test olmadan tamamlanmış sayılmaz") |

**B) İçeriği tamamlanmış ama "kısmen" etiketi ve "sonraki iş" notları kalmış (≈16 satır, bayat):**
`002, 003, 004, 005, 008, 009, 010, 011, 014, 036, 039, 043, 045, 046, 050, 054`.
Örnek: satır 1 hâlâ "ekranlar sonraki iş" diyor (PR #91 ile yapıldı); satır 5 "SYG-076 (parametre ekranı) sonraki iş" diyor (PR #110 ile yapıldı); satır 11 aynı hücrede önce "051–053 sonraki işler", sonra "PR #108" yazıyor. Matris **yalnızca ekleme yapılan bir günlüğe** dönüşmüş; satır durumu okuyarak anlaşılamıyor.

### 1.2 Tamamen boş satır

**REQ-KMLK-042** (doğrulama/sıfırlama iletileri bildirim istisnasından muaf, SYG-062): Tasarım, Gerçekleştirme, Doğrulama `—`. Matris §3 kuralı: *"açıklamasız boş hücre yasaktır"* — gerekçe yazılmamış. Bildirim istisnası (#3, Y1) henüz yok; ama bu bir `yok (gerekçe: Y1'de, KR-056)` notu ve İK onaylı erteleme gerektirir. PR #110 "kalanlar" listesi SYG-060/061/062'yi **hiç saymıyor** → unutulma riski.

### 1.3 SYG düzeyinde kapsama

- Matrisin Gerçekleştirme sütununda adı geçmeyen SYG: **060, 061, 062, 077, 078** (+079 ve 049'un ölçüm kısmı).
- Test kodunda (`src/backend/tests`, `*.test.ts(x)`) atfı bulunmayan SYG: 006 (İnceleme — beklenir), **060, 061, 062**, 064 (İnceleme), 065, 068 (Gösterim), 071, 077 (Analiz).
- Kod ↔ gereksinim bağı güçlü: **184 dosyada 562 `REQ-/SYG-KMLK` atfı** (kod yorumları ve testler).
- Otomatik denetim yalnızca **REQ ↔ SYG** halkasını ve matrisin SYG sütununu kapsıyor; **SYG → gerçekleştirme/test** halkası denetlenmiyor. Bayatlama bu yüzden yakalanmadı.
- Matris §6 "İzlenebilirlik kapsaması" ölçütü T3 için **ölçülmedi**.

---

## 2. MAN.8 — Kalite güvence

### 2.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | KG yordamları tanımlanır ve **uygulanır** | Tanım: `CONTRIBUTING.md` §5 (PR kuralları), §6 (DoD), §7 (inceleme listesi); `.github/PULL_REQUEST_TEMPLATE.md`; ADR-0011 §6. **Uygulama zayıf:** §5 "İnceleme: en az 1 onay" → T3'teki **20 PR'ın 20'sinde inceleme = 0** (`gh pr view --json reviews`); §6 "Kod incelemesi yapıldı ve onaylandı" maddesi PR şablonunda **yok** (iki farklı DoD); §5 "Boyut < 400 satır" → T3 PR'ları +2.383 … +5.103 satır (#75, #84, #86, #88, #94, #99, #108, #110). DoD maddeleri PR'dan PR'a değişiyor: CONTRIBUTING'de "atlanamaz" denen **maskeleme testi** maddesi #101'den itibaren 6 PR'ın DoD'sinden düşmüş. `tamamlanma-tanimi.md`, `kod-gozden-gecirme-kontrol-listesi.md`, `MAN.8/YAKLASIM.md` yok (BULGU-07 açık). | **P+** |
| b | Değerlendirme ölçüt ve yöntemleri tanımlanır | ADR-0011 §6, kapsam eşikleri (%75 / Domain %90), CI 9 iş (derleme/test/kapsam, frontend, EF model, açıklı paket, trivy, gitleaks, OpenAPI sözleşme, PR izlenebilirlik + gereksinim izlenebilirlik, dal koruma denetimi). Süreç değerlendirmesi için ölçüt: `00-OLGUNLUK` §2–§6. Kapı sayısı tutarsızlığı (BULGU-10) kapatılmadı. | **L** |
| c | Ürün, hizmet ve **süreç** değerlendirmeleri yapılır | **Ürün:** CI her PR'da (örnek #65, #88, #110, #116 — tüm kontroller SUCCESS); gereksinim izlenebilirlik denetimi kendini sınıyor; LOGO yazma reddi CI'da (PR #76). İnsan incelemesi **yok**. **Süreç:** yalnızca 2026-09-18 raporu (`raporlar/` tek dosya). T3'te TEC.2 ve TEC.3 ilk kez işletildi; bu ilk çevrimin süreç değerlendirmesi yapılmadı. **Araç değerlendirmesi:** #89 (CI kapsam ölçümü kararsız) 27.09'dan beri açık. | **L−** |
| d | Sonuçlar ilgili paydaşlara iletilir | PR gövdeleri (Ne/Neden/Nasıl doğrulandı) ayrıntılı ve görünür; 18.09 raporu. **Öz değerlendirme tablosu (`00-OLGUNLUK` §6) 18.09'dan beri güncellenmedi** — TEC.3 hâlâ `N`, TEC.2 `P+` görünüyor (fiilî durumla çelişiyor). İK veya yönetime giden kalite raporu yok (`raporlar/` "dönemsel kalite raporu" hiç üretilmedi). | **P+** |
| e | Olaylar çözülür | #79, #93, #97, #102, #103 aynı gün açılıp kapandı (kök neden PR gövdelerinde). #89 açık. | **L+** |
| f | Önceliklendirilmiş problemler ele alınır | Düzeltici faaliyetler #66, #67 kapandı (dal adı kapısı PR #71); #72 **eğilim analizi** yapıyor ("aynı kök neden üç kez: #17, #44, #67") — BP5.6 için iyi kanıt, ancak "bir sonraki süreç gözden geçirmesine" ertelendi ve açık. **18.09 bulgularının takibi:** BULGU-01 (#54), 02 kısmen (#52, #65), 03, 08 (#50), 09 (#52, #63) kapandı; **BULGU-04, 05, 06, 07, 10, 11 için issue yok**, hiçbiri kapanmadı (CHANGELOG 2026-09-08'den beri değişmedi). | **L−** |

**PA 1.1 = L−** (18.09: L). Gerekçe: yordamın en önemli insan kontrolü (inceleme) hiç işlemiyor; süreç değerlendirmesi tek seferlik kaldı; bulgu takibi yarım.

### 2.2 BP kontrolü

| BP | Durum |
|---|---|
| BP1 Hazırlık (strateji, **bağımsızlık**) | Strateji yazılı değil (YAKLASIM yok). Bağımsızlık sağlanmadı — değerlendiren = yürüten = birleştiren (raporda açıkça kabul edilmiş; telafi kontrolü tanımlanmamış). **Zayıf** |
| BP2 Ürün değerlendirmesi | CI güçlü ✔; doğrulama/geçerlemenin yapıldığının izlenmesi (BP2.2) — matrisin test sütunu var ama SYG düzeyinde kapsama izlenmiyor |
| BP3 Süreç değerlendirmesi | 1 kez (18.09) |
| BP4 Kayıt ve raporlar | Rapor 1; dönemsel KG raporu 0 |
| BP5 Olay/problem | Güçlü; eğilim analizi (#72) var ama açık |

### 2.3 PA 2.1 / PA 2.2 boşlukları

- **PA 2.1 (a)/(c):** MAN.8'in kendi hedefleri (sıklık, kapsam) yalnızca plan §8'de "öz değerlendirme her modül kapanışında" olarak var; ara değerlendirme, PR inceleme oranı, bulgu kapanma süresi gibi hiçbir KG ölçütü izlenmiyor. **(d)** bağımsızlık/sorumluluk ayrımı tanımsız. → **P+**
- **PA 2.2 (a)** `MAN.8/YAKLASIM.md` yok; **(d)** raporun kendisi dâhil hiçbir belge incelemeden geçmiyor (PR #49 inceleme 0); **(e)** saklama tanımsız (BULGU-06). → **L−**

### 2.4 18.09'dan bu yana değişen

- ✔ Bulguların 5'i kapandı (matris, milestone, TEC.2 yaklaşımı, toplantı kaydı, ölçek).
- ✔ Yeni otomatik kapılar: dal adı (#71), gereksinim izlenebilirliği (PR #65).
- ✘ İkinci süreç değerlendirmesi yok; öz değerlendirme tablosu bayat.
- ✘ T3 ile birlikte PR boyutu ~10 kat büyüdü; inceleme hâlâ 0.

### 2.5 İyileştirme işleri

| Öncelik | İş (issue boyutunda) |
|---|---|
| **Yüksek** | **MAN.8-1:** Tek geliştiricili düzende inceleme kuralı için karar (KR) — ya telafi edici kontrol tanımla (ör. birleştirmeden önce PR'a kayıtlı öz inceleme yorumu + §7 kontrol listesi, belirli türlerde ikinci kişi/İK gözden geçirmesi), ya CONTRIBUTING §5 "en az 1 onay" ve §6 son maddesini değiştir. Uygulanmayan kural, uyum varmış görüntüsü verir. |
| **Yüksek** | **MAN.8-2:** T3 ara süreç değerlendirmesi (TEC.2/TEC.3'ün ilk çevrimi) ve `00-OLGUNLUK` §6 tablosunun güncellenmesi; T3 kabulünü beklemeden. |
| **Yüksek** | **MAN.8-3:** 18.09 raporundaki takipsiz 6 bulgu (04, 05, 06, 07, 10, 11) için birer issue (veya bilinçli kapatma kararı). |
| Orta | **MAN.8-4:** Tek DoD: PR şablonu ile CONTRIBUTING §6'yı birleştir; şablondaki maddelerin PR'da silinmesini (işaretsiz bırakıp gerekçe yazmak yerine) CI'da yakala. `tamamlanma-tanimi.md` + `MAN.8/YAKLASIM.md` (BULGU-07). |
| Orta | **MAN.8-5:** PR boyutu kuralını ya gerçekçi hâle getir (üretilen dosyalar/migration hariç, ör. < 1.500 satır) ya iş paketlerini böl; kuralı CI'da ölç ve uyar. |
| Orta | **MAN.8-6:** #89 kapsam ölçümü kararsızlığını kapat (kalite kapısının güvenilirliği). #72 envanterini tamamla. |
| Düşük | **MAN.8-7:** Aylık kısa KG raporu (açılan/kapanan hata, düzeltici faaliyet, kapı başarısızlıkları, inceleme oranı). |

---

## 3. TEC.2 — Paydaş ihtiyaç ve gereksinimleri

### 3.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Paydaşlar tanımlanır | `paydas-listesi.md` (P1–P11). Toplantı kaydı §1: "liste toplantı öncesi gözden geçirildi" — ancak liste 2026-09-07'den beri değişmemiş, gözden geçirmenin kaydı yok. P6 KVKK Sorumlusu, KVKK içerikli REQ-040/041 ve AN-16 (saklama süreleri bekliyor) için danışılmadı. | **L+** |
| b | Kullanım bağlamı ve hayat döngüsü kavramları | `vizyon-ve-kapsam.md`; toplantı kaydı §2 (564 kişi, seyrek kullanıcı, H2 hedefi). Senaryo seti açık değil (BP3.1). | **L+** |
| c | Kısıtlar | Toplantı kaydı §5 (KR-003/004, 018, 019, 067, 070, parametre ilkesi) | **F** |
| d | İhtiyaçlar tanımlanır | Toplantı kaydı §3, §8 (13 açık sorunun cevabı), PG-KMLK "Gerekçe/Kaynak" sütunu | **F** |
| e | Önceliklendirilir ve açık gereksinime dönüştürülür | `PG-KMLK.md`: 56 gereksinim, Öncelik (Zorunlu/Olmalı), kabul kriteri, durum | **F** |
| f | **Kritik performans ölçütleri** | Şablonda (`SABLON-PG.md`) "Ölçüt" alanı var; **PG-KMLK'de bu alan hiç yok**. Ölçütler yalnızca TEC.3'te (SYG §5, KPÖ-1…6) ve vizyon H2'de. Kabul kriterlerinin bir kısmı ölçülemez: REQ-044 "telefonda okunabilir", REQ-015 "tahmin edilebilir örüntü içermiyor", REQ-043. | **L−** |
| g | Paydaş mutabakatı | Toplantı kaydı §7 (3 katılımcı, 23.09); §11 toplantı sonrası İK teyidi (24.09, S-05 revizyonu). **Zayıflık:** kesinleşmiş metnin anlamını etkileyen iki yorum İK'ya dönmeden Bilgi İşlem kararıyla bağlandı: AN-23/`KR-087` (REQ-054 "sinyal yalnızca video oynarken" → "kullanıcı etkileşiminde de") ve AN-24/`KR-092` (S-02 kapsamı: "üyelikte parolasını belirleyenler dâhil"; karar sahibi "Doğuş Uçanok"). TEC.2 YAKLASIM §4 / TEC.3 YAKLASIM §4 bu tür değişiklikte `tur:degisiklik-talebi` ve İK onayı ister; yalnızca #77 bu yolu izledi. | **L+** |
| h | Destekleyici sistemler | Toplantı kaydı §6 (LOGO, NetGSM, SMTP, TLS; veri ön koşulu 12→4→2) | **F** |
| i | Paydaşa ve ihtiyaca izlenebilirlik | Her REQ satırında kaynak (İK kararı, S-xx, KR-xxx); toplantı kaydı depoda (BULGU-09 kapandı); REQ→SYG otomatik | **F** |

**PA 1.1 = L+** (18.09: P+). F olmamasının nedeni (f) ve (g)'deki zayıflık.

### 3.2 BP kontrolü

BP1 ✔ (YAKLASIM, paydaş listesi) · BP2 ✔ · BP3 kısmen (senaryo seti yok) · BP4 ✔ (kısıtlar, güvenlik nitelikleri) · BP5 kısmen (performans ölçütleri TEC.3'e kaymış; geri bildirim AN-01 için "kabulde gösterilecek") · BP6 kısmen (mutabakat ✔, izlenebilirlik ✔, **baseline yok** — PG-KMLK v1.2 sürümlü ama Git etiketi/baseline kaydı yok).

### 3.3 PA 2.1 / PA 2.2

- **PA 2.1 = L.** (a) hedef/koşul açık: KR-068, "kayıt 48 saat içinde" kuralı tutuldu (23.09 → 24.09) · (b) R-04 · (c) #62 gereksinim issue'su açık ve PR'larda `Refs` ile izleniyor · (g) İK↔Bİ arayüzü toplantı + §11 teyidiyle yönetildi. Zayıflık: (c) gereksinim değişikliklerinin izlenmesi tek kanaldan (#77) geçmiyor.
- **PA 2.2 = L.** (a)(b) `TEC.2/YAKLASIM.md` ✔. (c) Fiilî uygulama YAKLASIM'dan sapmış ve YAKLASIM güncellenmemiş (2026-09-18): kimlik `PG-KMLK-nn` yerine `REQ-KMLK-nnn` (PG-KMLK §3'te gerekçeli), "Ölçüt" alanı kullanılmadı. (d) PG ve toplantı kaydı PR #63 ile birleşti, inceleme 0; İK onayının özgün (doldurulmuş) dosyası depo dışında. (e) saklama tanımsız.

### 3.4 18.09'dan bu yana

Yaklaşım + iki şablon (#52/PR #53), ilk İK toplantısı ve kaydı, 56 onaylı gereksinim (PR #63), §11 teyit süreci, LOGO veri kalitesi ölçümleri (#58, #60). **En büyük ilerleme bu süreçte.**

### 3.5 İyileştirme işleri

| Öncelik | İş |
|---|---|
| **Yüksek** | **TEC.2-1:** AN-23/KR-087 ve AN-24/KR-092'yi İK'ya geri bildirim olarak götür; teyidi toplantı kaydına ek (§12) veya `tur:degisiklik-talebi` issue'su ile kayda al. |
| Orta | **TEC.2-2:** PG-KMLK'ye "Ölçüt" bağı: ya her ilgili REQ'e KPÖ atfı ekle ya da YAKLASIM §3/§5'i "kritik ölçütler SYG §5'te tutulur" diye değiştir. Ölçülemez kabul kriterlerini (REQ-015, 043, 044) SYG karşılığıyla ölçülebilir kıl. |
| Orta | **TEC.2-3:** YAKLASIM §3.1'i fiilî kimlik istisnasıyla hizala; `paydas-listesi.md`'ye gözden geçirme kaydı satırı ekle; KVKK sorumlusu (P6) danışmasını AN-16 için planla. |
| Düşük | **TEC.2-4:** T3 kabulünde PG-KMLK baseline etiketi (ör. `pg-kmlk-1.2`). |

---

## 4. TEC.3 — Sistem/yazılım gereksinimleri

### 4.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Sistem/öğe tanımı, arayüzler, işlevler, sınırlar | `SYG-KMLK.md` §2 (öğeler, dış arayüzler, sınırlar, kapsam dışı); §3 durumlar/kipler. **Bayat:** §2.2 API konumu hâlâ `/api/v1/kimlik/` (AN-21 ile `/api/v1/identity/` olmuştu). "System function model" çıktısı (`docs/mimari/fonksiyon-modeli.md`) yok. | **L** |
| b | İşlevsel, performans, arayüz, işlevsel olmayan gereksinimler ve kısıtlar | 79 SYG; tür dağılımı 46/3/4/14/12; her satırda kaynak, doğrulama yöntemi, parametre | **F** |
| c | Kritik performans ölçütleri | §5 KPÖ-KMLK-1…6, hedef ve ölçüm yöntemiyle ✔. "Critical performance data" çıktısı: yalnızca KPÖ-2 ölçüldü (medyan farkı 0,08 ms, KR-085/PR #88); KPÖ-1, 3, 4, 5 UAT ölçümü bekliyor. | **L+** |
| d | Gereksinimler analiz edilir | §6: 24 analiz bulgusu (AN-01…24), çözüm ve karar bağıyla; 23 ✅, AN-16 ⏳. Geri bildirim: yalnızca AN-01 İK'ya (kabulde) götürülecek; AN-23/24 götürülmedi (bkz. TEC.2 g). Analiz bulgularının 8'i (AN-17…24) geliştirme sırasında bulundu — analiz yaşayan bir süreç, bu olumlu. | **L+** |
| e | Destekleyici sistemler | §7 tablo. **Bayat:** LOGO otomatik testi "T3'te yazılacak" (PR #76'da yazıldı), NetGSM/SMTP kimlik bilgileri ⏳, parola listesi "seçilecek" (PR #88'de 143.672 kayıtlı liste seçildi). | **L** |
| f | Paydaş gereksinimlerine izlenebilirlik | 56/56 REQ bağlı; §8 ters tablo; her PR'da otomatik ve kendini sınayan denetim | **F** |

**Doğrulama kriterleri (Verification criteria) çıktısı:** SYG'de yöntem sütunu var (Test 71 / Gösterim 3 / İnceleme 2 / Analiz 3); kriterin kendisi gereksinim metninde ve KPÖ hedeflerinde. SYG başına kabul eşiği/test senaryosu kimliği (`TS-KMLK-nn`, doküman haritası §4.2) **üretilmedi**. Tutarsızlık: SYG-065 "Test" ama otomatik testi yok.

**PA 1.1 = L+** (18.09: N — sırası gelmemişti).

### 4.2 BP kontrolü

BP1 ✔ · BP2 ✔ (durum/kip §3, kısıtlar, risk ilişkili gereksinimler) · BP3 ✔/kısmen (geri bildirim eksik) · **BP4 kısmen:** BP4.1 "açık mutabakat" — SYG için onay kaydı yok (PG'de "Onay durumu: Onaylandı" alanı var, SYG'de yok; PR #65 inceleme 0); BP4.3 baseline yok.

### 4.3 PA 2.1 / PA 2.2

- **PA 2.1 = L−.** (a) "geliştirmeden önce" hedefi tutuldu (SYG 24–25.09, ilk kod PR #75 26.09) ✔. (c) Belge durumu izlenmiyor: §2.2, §7 bayat; "Son güncelleme 2026-10-01" iken PR #116 (SYG-063) sonrası değişiklik geçmişinde iz yok. (d) Sorumlu = yazan = onaylayan.
- **PA 2.2 = L−.** (a)(b) `TEC.3/YAKLASIM.md` ✔ (iyi yazılmış; otomatik denetim kontrol gereksinimi olarak tanımlı). (c) sürüm 1.0, kimlik kuralları ✔. (d) **onay yok** — ne SYG'de onay alanı ne PR incelemesi. (e) saklama tanımsız. YAKLASIM §4'teki "anlamı değiştiren değişiklikte önce değişiklik talebi" kuralı #77 dışında uygulanmadı.

### 4.4 18.09'dan bu yana

Süreç sıfırdan kuruldu ve işletildi: YAKLASIM, SYG-KMLK (79 madde, 24 AN), KPÖ'ler, otomatik izlenebilirlik kapısı, `SYG-` kimlik kararı (doküman haritası v0.4).

### 4.5 İyileştirme işleri

| Öncelik | İş |
|---|---|
| **Yüksek** | **TEC.3-1:** SYG-KMLK'ye "Onay durumu" alanı ve onay kaydı; T3 kabulünde SYG baseline etiketi. Onaylayıcı ve yöntem YAKLASIM §4'e yazılsın. |
| **Yüksek** | **TEC.3-2:** Bayat içeriği düzelt: §2.2 API yolu, §7 durumları, SYG-065 doğrulama yöntemi (Test ↔ Gösterim); değişiklik geçmişine PR #116'yı ekle. |
| Orta | **TEC.3-3:** Gereksinim denetim betiğine SYG belgesi iç tutarlılığı kontrolü (ör. `/api/v1/kimlik/` gibi AN ile iptal edilmiş ifadeler; §7'de ⏳ kalan ama matrise göre tamamlanan kalemler) — en azından "her PR'da SYG değişiklik geçmişi güncellendi mi" uyarısı. |
| Orta | **TEC.3-4:** KPÖ-1, 3, 4, 5 için UAT ölçüm işi (tek issue); sonuçlar SYG §5'e "ölçülen değer, tarih" sütunu olarak. |
| Düşük | **TEC.3-5:** Sistem işlev modeli (kısa: akış/durum diyagramı) veya doküman haritasından bu çıktının SYG §2–§3 ile karşılandığının yazılması. |

---

## 5. TEC.5 — Tasarım tanımlama

> **Not:** `docs/33061/TEC.5-tasarim-tanimlama/` klasörü hiç commit edilmedi (30.09.2026 `git stash -u` olayıyla yerelde de silindi). `YAKLASIM.md` ve `raporlar/` yok. Kanıt ADR'lerden, `docs/mimari/`'den, karar defterinden ve PR'lardan toplandı.

### 5.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Her öğenin tasarım özellikleri | 15 ADR; T3'te ADR-0003, 0006 (10 sürüm), 0007, 0008, 0012 güncellendi; `docs/mimari/izin-listesi.md` (PR #101); SYG §2.2 öğe–konum tablosu. T3 için veri modeli, oturum/üyelik akış ya da durum diyagramı **yok** (depoda hiç diyagram yok). | **L** |
| b | SYG'lerin öğelere **tahsisi** | Öğe düzeyinde SYG §2.2; iş paketi düzeyinde issue başlıkları (#74 "SYG-001…012", #83, #85 …); kodda SYG atıfları. Açık bir SYG → tasarım öğesi tahsis tablosu yok; matrisin Tasarım sütunu **REQ** düzeyinde ve yalnızca ADR bölümü veriyor. | **L−** |
| c | Tasarım etkinleştiricileri | ADR-0001, `paket-envanteri.md`, KR-084 (controller + FluentValidation) | **F** |
| d | Öğeler arası arayüzler | OpenAPI (`docs/api/openapi-v1.json`, `api-contract-check.yml` her PR'da), SYG §2.3; `entegrasyon-arayuzleri.md` hâlâ yok | **L+** |
| e | Alternatifler değerlendirilir | ADR'lerde "Değerlendirilen alternatifler"; T3 kararlarında gerekçe sütununda alternatif tartışması (KR-082, 084, 090, 091, 093). Yeni mimari kararlar ADR olarak değil KR + "gerçekleştirme notu" olarak kaydedildi. | **L** |
| f | Tasarım ürünleri geliştirilir | ADR güncellemeleri, izin listesi, migration'lar; ancak ADR değişiklikleri **gerçekleştirme PR'larında, kodla birlikte** yazıldı ("gerçekleştirme notu", ADR-0006 v0.5–1.0) — tasarım koddan önce değil, kodla/sonra belgeleniyor. | **L** |
| g | Destekleyici sistemler | Geliştirme ortamı, Testcontainers, UAT | **F** |
| h | Tasarım ↔ mimari varlık izlenebilirliği | Matris REQ → ADR §: 55/56 (REQ-042 boş). ADR'lerden SYG'ye geri atıf yalnızca 5 yerde. Tasarım öğesi → mimari varlık (ADR-0002 katmanları, modül) eşlemesi yok. | **P+** |

**PA 1.1 = L** (18.09: L).

### 5.2 BP kontrolü

BP1 strateji yok (YAKLASIM yok) · BP2 ✔ kısmen · BP3 ✔ · BP4 kısmen: BP4.1 gerekçe ✔ (ADR+KR güçlü), BP4.2 izlenebilirlik kısmen, BP4.3 tasarım durumu belirlenmiyor, BP4.4 baseline yok. Doküman haritasındaki "tasarım değerlendirme raporu" (`raporlar/…-tasarim-gozden-gecirme.md`) hiç üretilmedi.

### 5.3 PA 2.1 / PA 2.2

- **PA 2.1 = P+.** Tasarım faaliyeti planlanmıyor; ayrı tasarım işi/issue'su yok (yalnızca #56 A0 döneminden). Tasarım gözden geçirmesi yok. Kararların sahibi tek kişi.
- **PA 2.2 = P+.** (a) YAKLASIM yok. (b) ADR kuralları `docs/adr/README.md`'de var, ancak **kural #2 "ADR'ler değiştirilmez, değiştirilir" fiilen uygulanmıyor:** ADR-0006 v0.2'de karar içeriği yerinde değişti (§6 parola 12 → 6, §7 2FA yeniden yazıldı), ADR-0012 §7 yerinde değiştirildi (KR-083 "ADR-0012 §7 değiştirildi"); durum alanları "Kabul Edildi / 2026-09-06" kalmış. README dizini 2026-09-06'dan beri güncellenmedi. (d) ADR değişiklikleri incelemesiz birleşti.

### 5.4 18.09'dan bu yana

ADR'ler T3 kararlarıyla güncel tutuldu (5 ADR, +234/−32 satır); karar defterine KR-069…093 (25 kayıt) eklendi; izin listesi belgesi; matrisin Tasarım sütunu dolduruldu. Yeni ADR yok. YAKLASIM ve raporlar hâlâ yok.

### 5.5 İyileştirme işleri

| Öncelik | İş |
|---|---|
| **Yüksek** | **TEC.5-1:** ADR yönetişim kararı: README kural #2 ile yerinde güncelleme pratiğini uzlaştır (ör. "gerçekleştirme notu" eklemeleri izinli, **karar değişikliği** yeni ADR veya `Değiştirildi` durumu ister); ADR-0006 v0.2 ve ADR-0012 §7 için durum/geçmişi düzelt; README dizinini güncelle. |
| **Yüksek** | **TEC.5-2:** `TEC.5/YAKLASIM.md` (yeniden): tasarım ne zaman yazılır (kod PR'ından önce mi, aynı PR'da mı), hangi kararlar ADR ister, tasarım gözden geçirmesi ne zaman yapılır. |
| Orta | **TEC.5-3:** SYG → tasarım öğesi tahsis tablosu (SYG-KMLK §2.2'yi SYG aralıklarıyla genişlet ya da matrise "Tasarım öğesi" sütunu) ve REQ-042 tasarım hücresine gerekçe. |
| Orta | **TEC.5-4:** T3 kararlarından mimari nitelikte olanların (KR-082 bellek içi kuyruk, KR-086 istek başına oturum doğrulama, KR-089 istek başına izin okuma, KR-093 API'de HTTPS) ADR'ye alınıp alınmayacağına karar. |
| Düşük | **TEC.5-5:** T3 tasarım modeli: hesap/oturum durum diyagramı ve üyelik dizi diyagramı (Mermaid); `entegrasyon-arayuzleri.md` (LOGO, NetGSM, SMTP). |
| Düşük | **TEC.5-6:** T3 kabulü öncesi kısa tasarım gözden geçirme raporu. |

---

## 6. TEC.7 — Gerçekleştirme

> **Not:** `docs/33061/TEC.7-gerceklestirme/` klasörü hiç commit edilmedi; YAKLASIM yok. Kanıt kod, PR ve CI'dan.

### 6.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Gerçekleştirme kısıtları belirlenir | KR-058 (İngilizce tanımlayıcı; düzeltici #66, PR #68/#82), CONTRIBUTING §3, ADR-0002 mimari testleri, SYG "Kısıt" türü (12), KR-084 | **F** |
| b | Sistem öğesi gerçeklenir | T3: 20 PR (#75 … #116), 2026-09-26 … 10-02; 79 SYG'nin 69'u matriste gerçekleştirme halkasına sahip. **Gerçeklenmemiş:** SYG-060 (kimlik olayları denetim izi), SYG-061 (kimlik olaylarında maskeleme kanıtı), SYG-062 (bildirim istisnası muafiyeti); ölçüm bekleyen 077, 078, 079 (+049). PR #110'daki "kalanlar" listesi 060–062'yi saymıyor. | **L+** |
| c | Paketlenir veya saklanır | Docker imajları (trivy taraması her PR'da), UAT dağıtım betiği; ancak **Git etiketi 0, GitHub Release 0**, `CHANGELOG.md` 2026-09-08'den beri değişmedi (BULGU-05 açık; arada ~60 PR). | **L−** |
| d | Destekleyici sistemler | CI, Testcontainers, SQL Server Express (LOGO yazma reddi testi), UAT | **F** |
| e | İzlenebilirlik | İssue başlıkları SYG aralığı taşıyor; 184 kod/test dosyasında 562 REQ/SYG atfı; PR gövdelerinde "33061 izlenebilirliği" tablosu; matris §5. **Zayıflıklar:** §1'deki 26 "kısmen" satırı (≈16 bayat), satır 37'nin PR #108'de güncellenmemesi, satır 42'nin gerekçesiz boş kalması; matris §2.1 "Kod yolu + PR" ister, T3 satırları çoğunlukla yalnızca PR numarası veriyor. | **L** |

**PA 1.1 = L** (18.09: L+).

### 6.2 BP kontrolü

BP1 strateji yazılı değil (YAKLASIM yok; parçalar CONTRIBUTING'de) · BP2 ✔ (BP2.6 nesnel kanıt: PR "Nasıl doğrulandı" tabloları, test sayıları — güçlü) · BP3.1 anomaliler PR ve hata issue'larında ✔ · BP3.2 kısmen · BP3.3 baseline yok.

### 6.3 PA 2.1 / PA 2.2

- **PA 2.1 = L−.** (a)(c) İş paketleri issue olarak planlanıyor, sıradaki iş PR notlarında öneriliyor ✔; ama kalan iş listesi resmî değil (060–062 düşmüş). PR boyutu hedefi (<400) tutulmuyor; T3 PR'larının çoğu 2.400–5.100 satır. (d) Tek kişi yazar ve birleştirir; inceleme 0. Hatalara hızlı tepki ✔ (#97, #102, #103 aynı gün).
- **PA 2.2 = L−.** (a) YAKLASIM yok. (c) Git ✔, squash, dal adı ve başlık kapıları ✔. (d) inceleme/onay kaydı yok. "Gerçekleştirme raporu" karşılığı olan CHANGELOG bayat; PR özetleri ise ayrıntılı ve kalıcı (olumlu). DoD maddeleri PR'dan PR'a kayıyor.

### 6.4 18.09'dan bu yana

İlk gerçek modül kodlandı (T3'ün ~%90'ı); gereksinim ↔ kod bağı kuruldu (18.09'da "REQ kimliği yok" denen eksik kapandı); kod dosyası adları İngilizceye çevrildi (#81). Buna karşın CHANGELOG ve sürüm etiketi hâlâ yok; PR boyutu ve inceleme zayıflığı büyüdü.

### 6.5 İyileştirme işleri

| Öncelik | İş |
|---|---|
| **Yüksek** | **TEC.7-1:** Matris §5 temizliği: 26 "kısmen" satırını yeniden değerlendir (bayatları "tam" yap, notları birleştir), satır 37'ye PR #108'i ekle, satır 42'ye gerekçe yaz; §6 kapsama oranını T3 için ölç. |
| **Yüksek** | **TEC.7-2:** Gerçeklenmemiş SYG'ler için issue'lar: SYG-060 + 061 (kimlik olayları denetim izi ve maskeleme testi), SYG-062 (ya şimdi en küçük hâli ya İK onaylı değişiklik talebiyle Y1'e erteleme), SYG-065 otomatik testi. |
| Orta | **TEC.7-3:** Gereksinim denetim betiğini SYG → gerçekleştirme/test halkasına genişlet: matriste adı geçmeyen SYG'yi (gerekçesiz) ve "sonraki iş" notu kalan satırı uyarı olarak raporla. |
| Orta | **TEC.7-4:** `CHANGELOG.md`'yi T3 için güncelle; T3 kabulünde `v0.2.0-…` etiketi ve GitHub Release (BULGU-05). |
| Orta | **TEC.7-5:** `TEC.7/YAKLASIM.md` (yeniden): strateji, iş paketi büyüklüğü, DoD bağlantısı, matris bakım sorumluluğu. |
| Düşük | **TEC.7-6:** PR şablonundaki örnek gereksinim kimliği `REQ-KIMLIK-07` → `SYG-KMLK-nnn`; matris satırlarına kod yolu. |

---

## 7. Süreçler arası gözlemler

1. **Bakım kuralı var, bakım denetimi yok.** Matris §3 "PR'da güncellenir" kuralı büyük ölçüde işliyor (değişiklik geçmişinde 17 sürüm), ancak yalnızca **ekleme** yapılıyor; satır durumu güncellenmiyor. Otomatik kapı yalnızca REQ↔SYG'yi gördüğü için bayatlama sessiz kaldı. Bu, `00-OLGUNLUK` §7.1'in uyardığı "uyum varmış gibi görünme" riskidir.
2. **Tek kişi bağımlılığı artık kanıt sorunudur.** Yazan, analiz eden, karar veren (KR-092 "Doğuş Uçanok"), birleştiren ve değerlendiren aynı kişi. PA 2.2(d) ve MAN.8 BP1.2 için telafi edici bir kontrol tanımlanmalı; yoksa bu dört sürecin PA 2.x'i L'nin üstüne çıkamaz.
3. **Kayıp klasörler.** TEC.5, TEC.7, MAN.8'in YAKLASIM belgeleri hiç yazılmamıştı (BULGU-02); 30.09 olayı yalnızca boş klasörleri sildi, kanıt kaybı yok — ama PA 2.2(a) boşluğu sürüyor.
4. **İyi uygulamalar (korunmalı):** gereksinim izlenebilirlik kapısının kendini sınaması; analiz bulgularının (AN-xx) kararla (KR) ve issue ile bağlanması; PR gövdelerindeki nesnel doğrulama tabloları; toplantı sonrası çekincelerin İK ile yazılı teyidi (§10–§11); hata issue'larının aynı gün kök nedeniyle kapanması.

---

## 8. Öncelikli iş listesi (birleşik)

| # | Öncelik | İş | Süreç |
|---|---|---|---|
| 1 | Yüksek | Matris §5 temizliği + T3 kapsama ölçümü (TEC.7-1) | TEC.7, TEC.5, TEC.9 |
| 2 | Yüksek | SYG-060/061/062/065 için issue'lar; 062 için İK onaylı karar (TEC.7-2) | TEC.7, TEC.2 |
| 3 | Yüksek | Tek geliştiricide inceleme/onay için telafi edici kontrol kararı; CONTRIBUTING §5/§6 hizalama (MAN.8-1) | MAN.8, tümü PA 2.2(d) |
| 4 | Yüksek | T3 ara süreç değerlendirmesi + `00-OLGUNLUK` §6 güncellemesi; 18.09'dan kalan 6 bulgu için issue (MAN.8-2, -3) | MAN.8 |
| 5 | Yüksek | AN-23/KR-087 ve AN-24/KR-092'nin İK teyidi (TEC.2-1) | TEC.2, TEC.3 |
| 6 | Yüksek | SYG onay alanı ve baseline kuralı; bayat §2.2/§7 düzeltmesi (TEC.3-1, -2) | TEC.3 |
| 7 | Yüksek | ADR yönetişimi: kural #2 ↔ yerinde güncelleme; README dizini (TEC.5-1) | TEC.5 |
| 8 | Orta | Gereksinim denetimini SYG → gerçekleştirme/test halkasına genişlet (TEC.7-3) | TEC.7, MAN.8 |
| 9 | Orta | YAKLASIM: TEC.5, TEC.7, MAN.8 (TEC.5-2, TEC.7-5, MAN.8-4) | PA 2.2(a) |
| 10 | Orta | KPÖ-1/3/4/5 UAT ölçümü (TEC.3-4) | TEC.3, TEC.9 |
| 11 | Orta | CHANGELOG + T3 sürüm etiketi (TEC.7-4) | TEC.7, MAN.5 |
| 12 | Orta | Tek DoD, PR boyutu kuralı, #89 (MAN.8-4, -5, -6) | MAN.8 |
| 13 | Orta | PG-KMLK ölçüt bağı; YAKLASIM/paydaş listesi hizalama (TEC.2-2, -3) | TEC.2 |
| 14 | Düşük | Tasarım modeli/diyagramlar, entegrasyon arayüzleri, tasarım gözden geçirme raporu (TEC.5-5, -6) | TEC.5 |
| 15 | Düşük | PR şablonu örnek kimliği, matriste kod yolu, aylık KG raporu | TEC.7, MAN.8 |

---

## Ek — Başlıca kanıt konumları

- `docs/33061/MAN.8-kalite-guvence/raporlar/2026-09-18-surec-gozden-gecirme-raporu.md` (tek süreç değerlendirmesi)
- `docs/33061/00-OLGUNLUK-SEVIYESI-KRITERLERI.md` (son güncelleme 2026-09-18)
- `docs/33061/izlenebilirlik-matrisi.md` (v2.0, 2026-10-02)
- `docs/33061/TEC.2-paydas-ihtiyac-ve-gereksinimleri/{YAKLASIM.md, paydas-listesi.md, kayitlar/2026-09-23-kimlik-gereksinim-toplantisi.md, paydas-gereksinimleri/PG-KMLK.md}`
- `docs/33061/TEC.3-sistem-yazilim-gereksinimleri/{YAKLASIM.md, gereksinimler/SYG-KMLK.md}`
- `docs/adr/` (README son güncelleme 2026-09-06; ADR-0006 v1.0)
- `docs/karar-kayit-defteri.md` (KR-069…KR-093)
- `.github/PULL_REQUEST_TEMPLATE.md`, `CONTRIBUTING.md` §5–§7, `.github/scripts/requirement-traceability-check.mjs`, `.github/workflows/*.yml`
- GitHub: issue #62, #64, #72, #77, #89; PR #63, #65, #75–#116 (inceleme sayısı: hepsinde 0)
