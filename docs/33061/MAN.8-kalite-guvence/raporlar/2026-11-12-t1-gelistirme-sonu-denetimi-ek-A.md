# 33061 T1 Geliştirme Sonu Süreç Denetimi — Bulgular A (MAN.1, MAN.2, MAN.4, MAN.5, MAN.6)

**Değerlendirme tarihi:** 2026-11-12
**Son güncelleme:** 2026-11-12
**Dönem:** T3 kabulü ve modül kapanışı (13.10.2026, `v0.2.0`, `main` @ `d354e94`) sonrası → bugün (`main` @ `388da21`). 89 commit, 90 birleşmiş PR (#220–#384). Kapsam: T3 düzeltici işleri (#207–#219), T1 gereksinim toplantısı (19.10), tasarım (ADR-0019…0022), geliştirme, UAT kurulumu ve ölçümü (09–10.11, #381). T1 için kabul adayı (`rc`) henüz yok.
**Dayanak:** TS ISO/IEC TS 33061 Madde 5.4.2, 5.4.3, 5.4.5, 5.4.6, 5.4.7 · TS ISO/IEC 33020 Madde 5.2.4 (PA 1.1, PA 2.1 a–g, PA 2.2 a–e), 5.3 ölçek
**Yöntem:**
- Öz değerlendirme; dereceler teşhistir. Salt okuma. Kaynaklar: depo, `git log`/`git tag`, `gh issue/pr/run/api`.
- UAT sunucusunda yalnızca okuma komutları. Sır dosyalarında yalnızca **değişken adları**. Veritabanından yalnızca sayılar. LOGO'ya bağlanılmadı.
- Depo dışı konumlarda yalnızca dosya ve klasör **adları** listelendi: `Masaüstü\HRMS`, UAT `/opt`, `/opt/dsg-hrms`, `docker volume ls`, yerel `docker volume ls`.
- "Planlandı" ve "yapılacak" kanıt sayılmadı. Ölçek: N · P · L · F; `−/+` 00-OLGUNLUK §2.
- Eşlik eden rapor: `MAN.5-konfigurasyon-yonetimi/raporlar/2026-11-12-t1-konfigurasyon-denetimi.md` (D-01…D-20, B-1…B-5).

> **Bekleyen, kanıt sayılmayanlar (12.11.2026):**
> - Durum raporu 3 ve aylık risk gözden geçirmesi: `KR-101` sınırı **13.11.2026** (yarın). Dönemde ikisi de yazılmadı.
> - T1 G2, kabul adayı, kabul ve G4 henüz yok. Bu denetim "geliştirme sonu"dur; modül kapanışı değildir.

---

## 0. Ortak gözlemler

| # | Gözlem | Kanıt |
|---|---|---|
| O-1 | **CI'a bağlanan T3 düzeltmeleri tuttu.** 90 PR'ın 90'ında `tur:`, `surec:` ve milestone var (#216). 89 commit'in 89'unda Dal Koruma Denetimi `success`. Başlık–geçmiş denetimi 48 belgeyi geçiriyor (#214). Değişiklik talebi onay denetimi 9 talepte işledi (#227). | `gh pr list --json labels,milestone`, `gh run list`, `node .github/scripts/doc-consistency-check.mjs` |
| O-2 | **Aşınma depodan sunucuya taşındı.** Kural yazılı ama denetimi olmayan yerler bu kez sunucudaki elle yapılan işler ve etiketsiz kurulumlar: yeni sır kopyası (A-01), bekleyen yeniden başlatma (A-11), sentetik yığın kalıntısı (A-05). T3'teki "kural var, denetleyen yok" örüntüsünün yeni biçimi. | Konfigürasyon denetimi D-09, D-14, D-15 |
| O-3 | **Defterler anında güncellendi, plan seti güncellenmedi.** Bilgi defteri dönemde 9, risk defteri 12 commit'le güncellendi; karar defterine 32 yeni kayıt girdi (KR-107…KR-138). Buna karşılık proje planı, öğe listesi ve YAKLAŞIM tabloları 13–15.10'dan beri değişmedi; T1 hiçbirine işlenmedi. | `git log -- <dosya>` |
| O-4 | **Yönetim süreçlerine T1'de iş açılmadı.** 15.10'dan sonra açılan 76 issue'nun hiçbiri `surec:MAN.1`, `MAN.2`, `MAN.4` veya `MAN.6` taşımıyor; tek MAN issue'su #252 (`MAN.5`, yanlış alarm). Yönetim işleri yalnızca modül kapanışında (#206–#219) üretiliyor. | `gh issue list --search "created:>=2026-10-15"` |

---

## 1. MAN.1 — Proje Planlama

**Amaç (özet):** Etkili ve uygulanabilir planlar üretmek; planlar proje boyunca **düzenli revize edilir**.

### 1.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Hedefler ve planlar tanımlanır | WBS 3.T1 kalemi ve iş paketleri issue olarak işledi (T1-1…T1-11). **Ancak plan T1 için revize edilmedi:** `proje-plani.md` 13.10'dan beri aynı; §5.2 hâlâ "A2 — T1, T2, T4, T5: İK gereksinim toplantısı bekliyor" diyor. T1'in sıradaki modül seçildiğine dair karar kaydı yok. T1 için hedef tarih yok. MAN.1 YAKLASIM §2.6 "G4 → sıradaki modül, kaynak ve altyapı durumu" tetikleyicisini tanımlıyor; T3 G4'ünden (13.10) sonra işletilmedi. | **L−** |
| **b)** Roller, sorumluluklar, yetkiler | RACI uygulandı: 9 değişiklik talebini İK onayladı (#270, #284, #321, #333, #344, #348, #352, #355, #374; CI denetimi #227). KVKK Sorumlusu kararları kayıtlı (`KR-109`, `KR-116`, `KR-125`). İK rolünün yetkileri karara bağlandı (`KR-128`). | **F** |
| **c)** Kaynaklar talep edilir ve taahhüt edilir | 13.10'da Üst Yönetimden üretim altyapısı (B2, B4) için hedef tarih istendi (durum raporu 2 §5 #1). **Kayıtlı cevap yok;** karar defterinde Üst Yönetim kararı yok. Bütçe hâlâ yok (T3 A-11). PostgreSQL 14 desteği bu ay bitiyor (A-02). | **L−** |
| **d)** Planlar yürürlüğe konur | T1 G1 19.10'da yapıldı (gereksinim toplantısı, `PG-PERS`). İlk kod PR'ından önce tasarım gözden geçirmesi kapısı konuldu ve işletildi (#267, 22.10). `A0` milestone'u hâlâ açık (T3 A-10). | **L+** |

**PA 1.1 = L** (13.10: L+). **Gerileme.** Plan T3'te revize edildi ve onaylandı; T1'de plan, işin gerisinde kaldı.

### 1.2 Temel uygulamalar

- **BP1:** ✔ BP1.4 WBS; ◐ BP1.5 süreçler (YAKLAŞIM'lar bayat, A-13).
- **BP2:** ✘ BP2.1 takvim; ✔ BP2.2 kapılar (G1, tasarım kapısı); ✘ BP2.3 bütçe; ✔ BP2.4 roller.
- **BP3:** ✔ BP3.1 onay (09.10, değişmedi); ◐ BP3.2 kaynak taahhüdü tarihsiz; ✔ BP3.3 uygulama.

### 1.3 PA 2.1 / PA 2.2

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L** (13.10: L+) | **(a)** Revizyon tetikleyicileri tanımlı ✔, G4 tetikleyicisi uygulanmadı ✘. **(c)** Plan, gerçekleşen sıraya göre ayarlanmadı ✘. Etiket ve milestone disiplini düzeldi (O-1) ✔. 6 issue'da `oncelik:` yok (#373, #377, #379, #381, #383, #385), CI bunu denetlemiyor (A-15). **(d)** RACI ✔. **(e)** Kaynak kararı tarihsiz. **(g)** Üst Yönetim arayüzü 13.10'dan beri işlemedi (A-02). |
| PA 2.2 | **L** (13.10: L) | **(a)/(b)** YAKLASIM ✔. **(c)** Başlık–geçmiş tutarlı (CI) ✔; içerik bayat: plan §5.2, YAKLASIM §7 #1 (A0). **(d)** PR onayı ✔. **(e)** Onay formu depoda (`KR-107`, #213) ✔. |

### 1.4 13.10 bulgularının durumu

| 13.10 bulgusu | Durum |
|---|---|
| A-05 etiket disiplini | ✅ Kapandı: #216; 90/90 PR. `oncelik:` CI dışında (A-15) |
| A-08 (MAN.1 payı) plan seti modül kapanışında | ⚠️ 13.10'da §5.2 güncellendi; T1 başlangıcında yeniden bayatladı (A-06) |
| A-10 `A0` milestone | ❌ Açık; issue'ya dönüşmedi |
| A-11 bütçe; B2/B4 hedef tarihi | ❌ Bütçe yok; hedef tarih istendi, cevap yok (A-02) |
| A-15 milestone kuralı | ✅ CONTRIBUTING §5.2 tek cümlelik kural; WBS §5 `SK` eşlemesi |

---

## 2. MAN.2 — Proje Değerlendirme ve Kontrol

**Amaç (özet):** Planların uyumunu ve uygulanabilirliğini değerlendirmek; durumu belirlemek; sapmaları düzeltmek. **Dönemsel olarak ve önemli olaylarda.**

### 2.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Performans ölçütleri veya değerlendirme sonuçları | Kalite güvence ölçütleri K1–K6 hedefleriyle tanımlandı; T3 başlangıç değerleri `MAN.8-…/kayitlar/2026-10-15-kg-olcutleri-t3-baslangic.md` (#217). T1 performans ölçümü (#381). **Eksik:** T1 döneminin ölçüt değerleri derlenmedi (rapor 3 bekliyor). | **L** |
| **b)** Rol ve sorumluluk yeterliliği | Durum raporu 2'de "rol ve kaynak yeterliliği" başlığı yok (YAKLASIM §2.5 madde 5; T3 A-12). T1'de ayrı değerlendirme yok. Tek kişilik geliştirme yalnızca R-05'te. | **P+** |
| **c)** Kaynak yeterliliği | UAT kapasitesi T1 yükünde ölçüldü: 2 çekirdek "dolu ama doymamış", yük ≈3,0 (#381 §3.2). Üretim altyapısı kararı bekliyor (A-02). | **L−** |
| **d)** Teknik ilerleme gözden geçirmeleri | T3 tasarım gözden geçirmesi (#247, 20.10); T1 tasarım gözden geçirmesi, ilk koddan önce (#267/#269, 22.10) ve ADR revizyonu (#271); kanıt denetimi uyarısı (#273); T1 doğrulama kanıtı (#379); UAT ölçümü (#381); bu denetim. | **L+** |
| **e)** Sapmalar incelenir | 9 değişiklik talebi etki analiziyle (PG/SYG/matris). `main` kırmızı olayları kök nedenle kapandı (#223, #242, #255, #286, #305); yanlış alarm #252 analiz edildi. Plan sapması (sıra, takvim) incelenmedi (A-06). | **L+** |
| **f)** Paydaşlar bilgilendirilir | Durum raporu 2 (13.10) yazıldı. **İletim kanıtı yok:** raporda yalnızca "Sunulan: Üst Yönetim"; Masaüstü'nde 13.10 tarihli bir set yok (06.10 setinin aksine). İstenen karar (hedef tarih) cevapsız. İK, T1 boyunca toplantı ve 9 talep onayıyla bilgilendirildi ✔. | **L−** |
| **g)** Düzeltici faaliyet | #207–#219'un 13'ü 13–16.10 arasında kapandı. T1 olayları issue'ya döndü (O-1). | **F** |
| **h)** Yeniden planlama | Modül içi yeniden planlama karar defterinde (`KR-115`, `KR-119`, `KR-124`). Proje düzeyinde yeniden planlama yapılmadı (A-06). | **L−** |
| **i)** Geçiş yetkisi | T1 G1 (19.10) ve tasarım kapısı (22.10) kayıtlı. G2 henüz yok. | **L+** |
| **j)** Hedefler başarılır | T1 geliştirmesi bitti: SYG-PERS 79 gereksinim, matris güncel (#369, #379), KPÖ-PERS-1…5 karşılandı (#381). Kabul henüz yok. Alınan dersler (T3) uygulanıyor: D-04 kontrol listesi var; D-01 sırası (İK denemesi önce) henüz sınanmadı. | **L** |

**PA 1.1 = L** (13.10: L). **Aynı bant.** Teknik gözden geçirme ve düzeltici faaliyet güçlü; yönetim raporlaması T1 boyunca durdu.

### 2.2 `KR-101` sıklığı

| Kural | Uygulama |
|---|---|
| Durum raporu: her modül kapanışında, en geç ayda bir | Rapor 1: 06.10. Rapor 2: 13.10 (T3 kapanışı). **Rapor 3 sınırı: 13.11.2026.** T1 boyunca (30 gün) rapor yok; kural henüz çiğnenmedi. |
| Rapor, risk gözden geçirmesiyle birlikte | Son gözden geçirme 13.10. Sınır aynı: 13.11. |

### 2.3 PA 2.1 / PA 2.2

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L** (13.10: L−) | **(a)** K1–K6 hedefli ✔ (13.10'daki "hedef yok" eksiği kapandı). **(c)** İkinci döngü 13.10'da gösterildi ✔; T1 içinde ara rapor yok. **(e)** Ölçütler hâlâ elle derleniyor. **(g)** Üst Yönetim arayüzü cevapsız kaldı. |
| PA 2.2 | **L** (13.10: L−) | **(a)/(b)** ✔. **(c)** `kayitlar/` dolu (kapanış, alınan dersler) ✔. YAKLASIM §2 bayat: "Dönemsel durum raporu henüz yok", "İlk rapor #127'dir" (A-13). **(e)** Rapor depoda ✔; iletim kaydı yok. |

### 2.4 13.10 bulgularının durumu

| 13.10 bulgusu | Durum |
|---|---|
| Vadesi gelen işler (rapor 2, G4, alınan dersler) | ✅ 13.10, #220 |
| A-12 rapora yeterlilik başlığı ve iletim satırı | ❌ Rapor 2'de yok; issue'ya dönüşmedi (A-09) |
| A-05 (MAN.2 payı) ölçütlerin `gh` ile derlenmesi | ❌ Açık |

---

## 3. MAN.4 — Risk Yönetimi

**Amaç (özet):** Riskleri **sürekli** belirlemek, analiz etmek, ele almak ve izlemek.

### 3.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Riskler belirlenir | **Eklenen:** R-31 (denetim kayıtlarının süper kullanıcıya karşı korunmaması), tasarım gözden geçirmesinden (B-01, 21.10). R-29'a dört yeni olay işlendi (#250, #255, #288, #305). R-25 kapsamı kan grubuyla genişletildi (`KR-125`). **Kayda girmeyen:** R-30'un gerçekleşen tekrarı (`.env.uat.bak-20261103`, 03.11); sentetik yığın kalıntısı; anahtar kopyası riskinin (sır kopyası artık özel nitelikli veri anahtarını koruyan dosyanın yanında) değerlendirilmesi. | **L** |
| **b)** Riskler analiz edilir | **Bayat:** (1) **R-09:** destek bu ay bitiyor; kayıt 13.10'daki "neredeyse kesin" ifadesinde kaldı, gerçekleşme durumu yazılmadı. (2) **R-31:** önlem 1 21.10'da UAT'de devreye girdi (`hrms_app`); yeniden puanlanmadı, durum "Açık". (3) **R-24:** T1 yük ölçümü (#381) işlenmedi. (4) **R-30:** puan gerekçesi "temizlik sonrası yeniden puanlanır" diyor; tekrar ettiği hâlde değişmedi. | **L−** |
| **c)** Ele alma seçenekleri | R-31 için rol ayrımı seçildi ve karara bağlandı (`KR-118`). Kabul yetkisi kuralı korunuyor. | **L+** |
| **d)** Önlem uygulanır | **Uygulanan ve etkili:** R-28 önlemleri (#208): sağlık taraması 93/93, olaylar otomatik kayda geçti; R-31 önlem 1 (`ApplicationRoleTests`, dağıtım betiği doğrulaması); R-29 önlem 4 (sabitleme). **Etkisiz:** R-30 önlem 1'in kalıcılığı; aynı türden kopya 21 gün sonra yeniden oluştu (A-01). | **L** |
| **e)** Riskler değerlendirilir | Olay tetikleyicisi işledi: defter dönemde 11 sürüm aldı (1.5…1.15). Dönemsel gözden geçirme 13.10'dan beri yok; sınır 13.11. | **L** |

**PA 1.1 = L** (13.10: L+). **Gerileme.** Olaya dayalı kayıt güçlü; analizlerin güncelliği ve önlem etkinliğinin izlenmesi zayıf.

### 3.2 Risk defterinin gerçeklikle karşılaştırılması (12.11.2026)

| Risk | Defterde | Gerçek | Güncel mi? |
|---|---|---|---|
| R-09 | 3/2/6, "kesinleşiyor; Üst Yönetim rapor 2'de bilgilendirilir" | Destek Kasım 2026'da bitiyor; Üst Yönetim kararı kayıtlı değil | ❌ Gerçekleşme ve karar durumu işlenmeli |
| R-18 | 3/3/9, ara dönem önlemi | UAT sertifikası 16.12'de bitiyor; yenileme penceresi 16.11'de açılıyor; iş yok | ⚠️ Yenileme işi açılmalı |
| R-24 | p95 4,42 sn (T3) | T1 liste p95 0,18 sn; yük ≈3,0 / 2 çekirdek (#381) | ⚠️ T1 ölçümü işlenmeli |
| R-25 | Kabul edildi; önlem 4 "sırlar kaynak ağacının dışında" | Sır kopyası `secrets/` altında (A-01) | ⚠️ Önlem 7'ye tekrar işlenmeli |
| R-28 | Açık; önlemler uygulandı | 93/93 tarama, olaylar kayda geçti | ⚠️ Etkinlik kanıtıyla yeniden puanlanabilir |
| R-30 | 3/2/6, Açık | Aynı tür kopya yeniden oluştu (03.11) | ❌ Tekrar ve yeni önlem (betik denetimi) işlenmeli |
| R-31 | 2/3/6, Açık | Önlem 1 UAT'de 21.10'dan beri etkin | ❌ Yeniden puanlanmalı |

### 3.3 PA 2.1 / PA 2.2

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L** (13.10: L+) | **(a)** YAKLASIM ✔. **(c)** Olay tetikleyicisi ✔; önlem etkinliğinin izlenmesi yok (R-30). PR şablonunda risk satırı var ✔ (#217). **(d)** ✔. **(g)** Risk profili 13.10'dan beri Üst Yönetime gitmedi. |
| PA 2.2 | **L** (13.10: L) | Defter başlığı ve geçmişi tutarlı ✔; bölüm sırası düzgün ✔. YAKLASIM §2.1 "30 kayıt" diyor (31 oldu) (A-13). |

### 3.4 13.10 bulgularının durumu

| 13.10 bulgusu | Durum |
|---|---|
| A-07 kapanış risk gözden geçirmesi | ✅ Defter 1.5 (13.10) |
| A-07b bölüm sırası | ✅ Defter 1.5 |
| A-13 PR şablonu satırları | ✅ Risk, CHANGELOG, bilgi defteri satırları var |

---

## 4. MAN.5 — Konfigürasyon Yönetimi

**Amaç (özet):** Sistem öğelerini ve konfigürasyonları yönetmek; ürün ile konfigürasyon tanımının tutarlılığını sağlamak.

> Madde madde sonuçlar: `MAN.5-…/raporlar/2026-11-12-t1-konfigurasyon-denetimi.md`.

### 4.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Öğeler belirlenir ve yönetilir | Öğe listesi 1.4 (14.10, #214) T3 boşluklarını kapattı. **T1'de güncellenmedi:** `docker/postgres/`, `docker/uat-ik-rolu-ata.sh`, `hrms_app` rolü, `PERSONAL_DATA_KEY_K1…K3` yuvaları, `/opt/acme/` yok (D-16, D-17). Örnek ortam dosyaları zorunlu değişkenleri içermiyor; KÖ-30 "alt küme" ifadesi bozuldu (D-07, D-08). | **L−** |
| **b)** Baseline'lar oluşturulur | Dönemde kabul yok, yeni baseline gerekmedi. `v0.2.0` değişmedi. UAT 21.10'dan beri etiketsiz geliştirme yapısı çalıştırıyor; bu yapı türü kurallarda tanımlı değil (A-03). | **L+** |
| **c)** Değişiklikler kontrol edilir | 89/89 commit PR'dan ve onaylı (D-19). 9 değişiklik talebi İK onaylı. **Kontrol dışı:** sunucudaki sır dosyası 03.11'de elle değişti; kayıt PR'ı ve kontrol adımı yok, kopya kaldı (D-09). | **L** |
| **d)** Durum bilgisi erişilebilir | Dağıtım kaydı sunucuyla **7/7**; `VERSION`, imaj etiketleri (`6dbe9d5`) ve 35/35 migration tutarlı (D-02…D-05). Depo tablosu biçimsel olarak bölünmüş (G-1). | **F** |
| **e)** Denetimler tamamlanır | `KR-110` T1'de tam denetim gerektirmedi (aday yok); bu denetim yapıldı. Etiketsiz iki kurulumda A6 kısa kontrolü yok; olsaydı D-09 03.11'den sonraki ilk kurulumda (09.11) yakalanırdı. | **L** |
| **f)** Sürümler ve teslimler kontrol edilir | İki kurulum `deploy-uat.sh` ile, etiket ve kayıtla yapıldı. 09.11 kurulumu onay kaydıyla (#381). Etiketsiz kurulumun onay ve kontrol adımı yazılı değil (A-03). | **L+** |

**PA 1.1 = L** (13.10: L+). **Gerileme.** Depo içi kontrol (CI) güçlü; sunucudaki elle değişiklikler ve öğe listesinin bakımı zayıf.

### 4.2 PA 2.1 / PA 2.2

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L** (13.10: L) | **(a)** Hedef yazılı ✔. **(c)** Telafi kontrolleri izleniyor ✔ (#208). Etiketsiz kurulumlar plan dışı (A-03). **(e)** `deploy-uat.sh` güçlü; `secrets/` ve örnek dosya denetimi betiğe alınmadı (13.10 Ö-9). |
| PA 2.2 | **L−** (13.10: L) | **(c)** Öğe listesi ve örnek dosyalar dört haftadır gerçekle uyuşmuyor (D-07, D-17). Dağıtım kaydı tablosu bölünmüş (G-1). **(d)** ✔. **(e)** ✔. |

### 4.3 13.10 bulgularının durumu

| 13.10 bulgusu | Durum |
|---|---|
| A-03 tespit edici kontrolün arızası | ✅ #208; 93 tarama, 6 kırmızı olay kayıtlı |
| A-04 denetim sıklığı | ✅ `KR-110`, kontrol listesi (#218). Etiketsiz kurulum kapsam dışı kaldı (A-03) |
| A-14 CHANGELOG tarih kuralı, dal koruma başlığı | ✅ Kontrol listesi A1; başlık 14.10 |
| Konfigürasyon denetimi B-1…B-4 | ✅ B-1, B-2, B-4; ⚠️ B-3 (yeniden başlatma yine bekliyor, KÖ-36 kuralı uygulanmıyor) |

---

## 5. MAN.6 — Bilgi Yönetimi

**Amaç (özet):** Bilgiyi üretmek, doğrulamak, **saklamak, yaymak ve imha etmek**.

### 5.1 Çıktılar (PA 1.1)

| Çıktı | Kanıt | Derece |
|---|---|---|
| **a)** Yönetilecek bilgi belirlenir | Defter T1'de **oluştuğu anda** güncellendi: BK-29 (20.10), BK-12/16 çalışma rolü parolası (21.10), anahtar yeri (03.11), `PRM-KVK-03` (07.11), döndürme yuvaları (08.11). **Kayıt dışı kalanlar:** UAT sır kopyası (gizli), UAT sahipsiz birim, iki Masaüstü görseli, bir gereksinim yedek dosyası (§5.3). | **L** |
| **b)** Gösterimler tanımlanır | Gizlilik sınıfları, şablonlar ✔. Kan grubu işleme koşulları defterde (§3.1, `KR-116`). | **L+** |
| **c)** Elde etme … imha | **Yapılan:** BK-20 ve BK-21 (Kimlik) dosyaları silindi (#215; Masaüstü'nde yok ✔). `src_old/` kararı (BK-06). Saklama süreleri onaylı (`KR-109`). #381 veritabanı kopyası silindi ✔. **Eksik:** BK-12 "yedek bırakılmaz" kuralı çiğnendi (A-01). Sentetik yığın birimi silinmedi (A-05). | **L** |
| **d)** Bilginin durumu | Başlık–geçmiş CI'da ✔ (48 belge). YAKLASIM §8 #2 ve #7 kapandığı hâlde "Açık" (A-13). | **L+** |
| **e)** Paydaşlara erişilebilir | Doldurulmuş formlar depoda (`KR-107`). Dağıtım kaydı tanımı hâlâ yok (YAKLASIM §8 #5); rapor 2 iletimi kanıtsız (A-09). | **L** |

**PA 1.1 = L** (13.10: L−). **İyileşme.** "Oluştuğu anda deftere" hedefi T1'de ilk kez işledi. F için kalan: sunucudaki elle işlerin (sır, birim) deftere ve imhaya bağlanması.

### 5.2 PA 2.1 / PA 2.2

| Öznitelik | Derece | Gerekçe |
|---|---|---|
| PA 2.1 | **L** (13.10: L−) | **(a)** Hedef yazılı ve uygulandı ✔. **(d)** KVKK Sorumlusu kararları kayıtlı ✔ (13.10'daki eksik kapandı). **(e)** Planlı yedek hâlâ yok (YAKLASIM §8 #6). İmha kuralı (BK-12) için tespit edici kontrol yok. |
| PA 2.2 | **L** (13.10: L−) | Defter ve YAKLASIM ✔; saklama süreleri onaylı ✔; başlık–geçmiş CI ✔. YAKLASIM §8 bayat. |

### 5.3 Bilgi kayıt defteri ↔ depo dışı konumlar (`KR-101`, 12.11.2026)

Yalnızca adlar listelendi; içerik açılmadı.

**Defterde var, yerinde var (uyumlu):**

| BK | Konum | Not |
|---|---|---|
| BK-05, BK-25 | `docs/TSE_*`, `docs/TS_*`, `docs/NetGSM/`, `claude/` | ✔ |
| BK-06 | `src_old/` (1 alt klasör) | ✔ AS sonuna kadar |
| BK-10, BK-11 | `dsg-hrms-uat_uat-postgres-data`, `dsg-hrms-uat_uat-api-logs` | ✔ |
| BK-12 | `secrets/.env.uat` (`600`; 23 değişken: `HRMS_APP_DB_PASSWORD`, `PERSONAL_DATA_KEY_K1` dahil); `tls/` | ⚠️ Yanında kayıt dışı kopya (aşağıda #1) |
| BK-13 | `/opt/dsg-hrms/yedek-*.sql.gz` | ✔ Yok; #381 kopyası silinmiş |
| BK-14, BK-18 | `deployments.log`, `sema.sql` | ✔ |
| BK-15, BK-16, BK-17 | `dsg-hrms-dev_postgres-data`, `dsg-hrms-dev_api-logs`; `docker/.env`; `…/Dsg.Hrms.Api/logs/` (26 dosya, 10.09–08.11) | ✔ (90 dosya sınırı içinde) |
| BK-20 | `DSG-HRMS_LOGO_Kart_Celiskileri_Listesi.xlsx` | ✔ Veri düzeltme listesi silinmiş |
| BK-21 | `…_Kullanici_Yonetimi_`, `…_Organizasyon_Yonetimi_`, `…_Personel_Yonetimi_`, `…_Rol_ve_Yetki_Yonetimi_Gereksinimleri.xlsx`, `…_Sistem_Yonetimi_Parametreleri.xlsx` | ✔ Kimlik dosyası silinmiş. T1 (Personel) içeriği depoda onaylı (`PG-PERS`); dosya "silinebilir" durumda |
| BK-22 | `DSG-HRMS_UAT_Izin_Listesi.txt` | ✔ |
| BK-23, BK-24, BK-26 | `T3_Kabul_IK_Inceleme\` (4), `Ust_Yonetim_Onay_Seti_2026-10\` (6) | ✔ Asıllar depoda; klasörler silinebilir |
| BK-27, BK-28 | `Rapor_Onerileri\` (1 görüntü), `duzen_logo.png`, `Personel.png` | ✔ |
| BK-29 | UAT veritabanında kişi, istihdam, kan grubu (şifreli), fotoğraflar | ✔ Sayılar (#381): 1.378 kişi, 873 fotoğraf, 153 kan grubu. Fotoğraflar BK-29 metninde açıkça anılmıyor |

**Yerinde var, defterde yok:**

| # | Konum (yalnızca ad) | Önerilen sınıf | Neden önemli |
|---|---|---|---|
| 1 | UAT `/opt/dsg-hrms/secrets/.env.uat.bak-20261103` | **Gizli** | 22 değişkenlik (sırlar dahil) bir kopya; BK-12 ihlali; #207'nin tekrarı (A-01) |
| 2 | UAT sahipsiz birim `e7b63157…` (PostgreSQL verisi, 135 MB, 10.11) | Kurum içi (rapora göre sentetik) | #381 §6 "silindi" diyor; sahibi ve imhası tanımsız (A-05) |
| 3 | UAT `/opt/acme/` (`certbot.log`, `expected-record.txt`, `hook.sh`) | Kurum içi | Runbook'ta anılıyor; KÖ-31 ve defterde yok |
| 4 | `Masaüstü\HRMS\MainScreen.jpeg` (25.10), `MainScreen2.jpeg` (03.11) | Kurum içi (içerik kontrol edilmedi) | Arayüz örnek tasarımı (#322); BK-28 yalnızca iki görseli sayıyor |
| 5 | `Masaüstü\HRMS\DSG-HRMS_Personel_Yonetimi_Gereksinimleri_yedek_2026-09-24.xlsx` | Kurum içi | BK-21 deseni (`*_Gereksinimleri.xlsx`) bu adı kapsamıyor; yedeğin yedeği |
| 6 | Yerel 66 sahipsiz Docker birimi (11.09–08.11) | Kurum içi (e2e sentetik; doğrulanmadı) | `docker/e2e/down.sh` `-v` kullanmıyor; BK-15 yalnızca adlandırılmış birimleri sayıyor |
| 7 | Kullanıcıların indirdiği personel listesi dosyaları (Excel/PDF, #330) | **Gizli** | Sistem dışına çıkan yeni küme; erişim kaydı var, saklama ve imha sorumluluğu defterde yazılı değil |

**Defterde var, yerinde yok:** yok.

**Gözlem:** `/root/.bash_history` 12.11.2026'da değişmiş. #207 "elle yazılmış sır için gözden geçirilir" diyordu; gözden geçirmenin kaydı bulunamadı. İçerik açılmadı.

### 5.4 13.10 bulgularının durumu

| 13.10 bulgusu | Durum |
|---|---|
| A-01 UAT sır kopyası | ⚠️ Kopya silindi (#207); **aynı türden yeni kopya** (A-01) |
| A-02 kayıt dışı kümeler | ✅ #215: BK-06, 16, 17, 18, 27, 28 |
| A-06 "Son güncelleme" CI | ✅ #214 |
| A-09 doküman haritası | ✅ Harita güncellendi (#214) |
| A-16 KVKK talebi; BK-20/21 imhası | ✅ `KR-109`; dosyalar silinmiş |
| A-17 imzalı kanıt | ✅ `KR-107`, #213 |

---

## 6. Derecelendirme özeti

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | 13.10 (PA 1.1/2.1/2.2 → Sv) | Eğilim |
|---|---|---|---|---|---|---|
| MAN.1 Proje planlama | L | L | L | 1 | L+ / L+ / L → 1 | ↓ |
| MAN.2 Değerlendirme ve kontrol | L | L | L | 1 | L / L− / L− → 1 | ↑ (2.1, 2.2) |
| MAN.4 Risk yönetimi | L | L | L | 1 | L+ / L+ / L → 1 | ↓ |
| MAN.5 Konfigürasyon yönetimi | L | L | L− | 1 | L+ / L / L → 1 | ↓ |
| MAN.6 Bilgi yönetimi | L | L | L | 1 | L− / L− / L− → 1 | ↑ |

> **Okuma notu:**
> - **Seviye 2'ye neden ulaşılmadı:** Hiçbir süreçte PA 1.1 = F değil. Engeller: MAN.1 plan T1'e işlenmedi; MAN.2 yeterlilik ve iletim; MAN.4 bayat analiz ve etkisiz önlem (R-30); MAN.5 öğe listesi ve sunucudaki elle değişiklik; MAN.6 sunucudaki kayıt dışı kümeler.
> - **Gerilemenin kaynağı** iş azlığı değil, kapsamın genişlemesi: T1, T3'ün kurduğu düzene yeni sırlar, yeni betikler ve etiketsiz kurulumlar ekledi; düzen bunları kendiliğinden kapsamadı.
> - **İyileşmenin kaynağı** CI'a bağlanan kurallar (O-1) ve "oluştuğu anda" güncellenen defterler (O-3).
> - Durum raporu 3 ve risk gözden geçirmesi (A-02, sınır 13.11) yazılınca MAN.2 (f) ve MAN.4 (b, e) birer kademe yükselebilir.

## 7. T3 düzeltici işlerinin etkinliği (bu ekin süreçleri)

| İş | Etkinlik | Kanıt |
|---|---|---|
| #207 UAT temizliği | ⚠️ **Kısmen.** Avahi/CUPS kapalı ve maskeli, dışarıya açık yalnız TCP 22/80/443 ✔. Ama yeni sır kopyası (03.11) ve yeni bekleyen yeniden başlatma (03.11) var; "D-12 genişletmesi" etiketsiz kurulumlarda uygulanmadı | D-09, D-10, D-14 |
| #208 otomatik kontrollerin izlenmesi | ✅ **Etkili.** 93/93 sağlık taraması; 6 kırmızı olay otomatik kayıt; #248 "doğrulanmadı"; #252 yanlış alarm yakalandı ve #254 ile giderildi; 89/89 commit denetlendi | D-19 |
| #213 kabul kanıtının korunması | ✅ **Etkili.** Doldurulmuş formlar depoda, özetleriyle (`KR-107`); konum kuralı yazılı | BK-24, BK-26 |
| #214 belge tutarlılığı | ⚠️ **Kısmen.** Tarih kuralı CI'da (48 belge) ✔; içerik bayatlığı (plan §5.2, YAKLAŞIM tabloları, öğe listesi) denetim dışında ve yeniden birikti | A-06, A-08, A-13 |
| #215 bilgi defteri eksikleri | ✅ **Etkili.** Altı küme eklendi, imha yapıldı, saklama onaylı; defter T1'de 5 kez anında güncellendi | §5.3 |
| #218 sürüm kontrol listesi, denetim sıklığı | ⚠️ **Kısmen.** Liste ve `KR-110` var; T1'de aday olmadığı için sınanmadı. Etiketsiz kurulum listede yok; A6 uygulanmadı | A-03 |

---

## 8. Bulgular ve düzeltici eylemler

"İş" sütunu: **Bu iş** = #385 PR'ında belge düzeltmesi olarak giderilebilir. **Ayrı iş** = sunucu işlemi, kod/betik değişikliği veya karar gerektirir; issue açılmalı.

| # | Önem | Bulgu → eylem | Süreç | İş |
|---|---|---|---|---|
| A-01 | **Yüksek** | UAT'de kayıt dışı sır kopyası `.env.uat.bak-20261103` (#207 tekrarı). Değer düzeyinde karşılaştırma, gerekirse döndürme, silme; `deploy-uat.sh` `secrets/` dizinini denetlesin (konf. Ö-1…Ö-3) | MAN.6 / MAN.5 | Ayrı iş |
| A-02 | **Yüksek** | PostgreSQL 14 desteği bu ay bitiyor; 13.10'da istenen üretim altyapısı hedef tarihi için Üst Yönetim kararı kayıtlı değil. Durum raporu 3 ve aylık risk gözden geçirmesi 13.11'e kadar yazılmalı; R-09 gerçekleşme durumu ve karar talebi tekrarlanmalı | MAN.2 / MAN.4 / MAN.1 | Bu iş (rapor 3, defter); karar Üst Yönetimde |
| A-03 | Orta | Etiketsiz geliştirme yapılarının UAT'ye kurulması için kural yok; `KR-110` ve kontrol listesi yalnız `rc`'yi kapsıyor; 21.10 ve 09.11 kurulumlarında A6 yok. Karar: her UAT kurulumunda A6 (betikte kendiliğinden) ve kayıt PR'ında satır | MAN.5 | Ayrı iş (karar + betik) |
| A-04 | Orta | Örnek ortam dosyaları zorunlu `HRMS_APP_DB_PASSWORD` ve `PERSONAL_DATA_KEY_*`'ı içermiyor; compose ↔ örnek karşılaştırması CI'a alınmalı (konf. Ö-4) | MAN.5 | Ayrı iş |
| A-05 | Orta | Sentetik yığın birimi UAT'de kaldı; #381 §6 yanlış. Birim silinir, rapor düzeltilir; `down -v` (UAT yığını ve `docker/e2e/down.sh`); yerel 66 birim temizlenir | MAN.6 / MAN.5 | Ayrı iş (rapor düzeltmesi bu işte) |
| A-06 | Orta | Plan T1 için revize edilmedi: §5.2 bayat, sıradaki modül kararı ve T1 hedef tarihi yok, G4 tetikleyicisi işletilmedi. Plan 0.3.2 (durum satırı) ve karar kaydı | MAN.1 | Bu iş (§5.2); karar Doğuş Uçanok |
| A-07 | Orta | Risk defteri bayat: R-30 tekrarı, R-31 yeniden puanı, R-24 T1 ölçümü, R-09, R-18 yenileme işi (§3.2) | MAN.4 | Bu iş |
| A-08 | Orta | Öğe listesi T1'de güncellenmedi: `docker/postgres/`, `uat-ik-rolu-ata.sh`, `hrms_app`, anahtar yuvaları, KÖ-52 yolu, `/opt/acme/` (konf. Ö-7) | MAN.5 | Bu iş |
| A-09 | Orta | Durum raporunda yeterlilik başlığı ve iletim satırı yok (13.10 A-12 issue'ya dönüşmedi). Rapor 3'te uygulanır; YAKLASIM §5'e şablon | MAN.2 | Bu iş |
| A-10 | Düşük | Bilgi defterine §5.3'teki 2–7 eklenir; BK-29'a fotoğraflar; BK-21 T1 dosyası için not | MAN.6 | Bu iş |
| A-11 | Düşük | UAT yeniden başlatması 03.11'den beri bekliyor; zaman belirlenip İK'ya duyurulur (konf. Ö-6) | MAN.5 | Ayrı iş (A-01 ile aynı issue) |
| A-12 | Düşük | Yerel imajlarda commit etiketi yok; KÖ-36 kuralı uygulanmıyor (konf. Ö-8) | MAN.5 | Ayrı iş (A-04 ile) |
| A-13 | Düşük | YAKLAŞIM bayatlıkları: MAN.2 §2 ("rapor henüz yok", "ilk rapor #127"), MAN.4 §2.1 ("30 kayıt"), MAN.6 §8 #2 ve #7 (kapandı), MAN.1 §7 #1 (A0) | Hepsi | Bu iş |
| A-14 | Düşük | `A0` milestone'u açık; bütçe yok (13.10 A-10, A-11 issue'ya dönüşmedi) | MAN.1 | Bu iş (milestone); bütçe ayrı karar |
| A-15 | Düşük | 6 issue'da `oncelik:` yok; CI yalnız `tur:` ve `surec:` denetliyor | MAN.1 / MAN.2 | Bu iş (etiketleme); CI kuralı isteğe bağlı ayrı iş |
| A-16 | Düşük | Depodaki dağıtım kaydı tablosu `v0.2.0` notuyla bölünmüş (konf. G-1, Ö-9) | MAN.5 | Bu iş |

**Sıra önerisi:** A-01 ve A-11 bir sonraki UAT kurulumundan ve T1 kabul adayından önce. A-02 13.11'e kadar. A-03, A-04, A-05 ilk `v0.3.0-rc.1`'den önce. Kalanlar bu PR'da.

## 9. Ana raporun doğrulaması için kritik iddialar

| İddia | Doğrulama |
|---|---|
| UAT'de kayıt dışı sır kopyası | `ssh -o BatchMode=yes root@192.168.3.202 'ls -la /opt/dsg-hrms/secrets'` |
| Sahipsiz sentetik birim | `ssh … 'docker volume ls -f dangling=true; docker volume inspect e7b631577634348667ad3bd4b94ff7ba612aa31b843f935fdb8585fa73d3f1c4 --format "{{.CreatedAt}}"'` |
| Örnek dosyalarda eksik değişkenler | `grep -oE '\$\{[A-Z0-9_]+' docker/compose.uat.yml \| sort -u` ↔ `grep -oE '^[A-Z0-9_]+=' docker/.env.uat.example` |
| Bekleyen yeniden başlatma | `ssh … 'cat /var/run/reboot-required.pkgs; uname -r; uptime -s'` |
| Dağıtım kaydı 7/7, UAT `6dbe9d5` | `ssh … 'cat /opt/dsg-hrms/deployments.log'`; `docker inspect dsg-hrms-uat-api --format '{{index .Config.Labels "org.opencontainers.image.revision"}}'` |
| Plan T1'e işlenmedi | `docs/33061/MAN.1-proje-planlama/proje-plani.md` §5.2; `git log -1 -- docs/33061/MAN.1-proje-planlama/proje-plani.md` |
| Üst Yönetim kararı yok | `docs/karar-kayit-defteri.md` (`KR-105`ten, 13.10, sonra Üst Yönetim kaynaklı karar yok); `durum raporu 2 §5` |
| Rapor 3 ve risk gözden geçirmesi yok | `ls docs/33061/MAN.2-proje-degerlendirme-ve-kontrol/raporlar/`; risk defteri §3 "Son gözden geçirme: 2026-10-13" |
| #208 etkili | `gh run list --workflow main-health-sweep.yml --limit 200`; #223, #242, #248, #255, #286, #305 |

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-11-12 | 1.0 | İlk sürüm (#385) | Bilgi İşlem |
