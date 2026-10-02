# 33061 Süreç Denetim Raporu — T3 Kimlik Yönetimi

**Belge kimliği:** MAN.8-SGR-2026-10-02
**Süreç:** MAN.8 — Kalite Güvence (`MAN.8.BP3`: süreç değerlendirmelerinin yapılması)
**Değerlendirme tarihi:** 2026-10-02
**Değerlendirilen dönem:** 18.09.2026 gözden geçirmesi (MAN.8-SGR-2026-09-18) sonrası. Kapsam, T3 Kimlik Yönetimi geliştirme çevrimidir: #62–#116, 24.09–02.10.2026, `main` @ `4199526`.
**Dayanak:** TS ISO/IEC TS 33061, Madde 5 (süreç amaçları, çıktılar, temel uygulamalar, bilgi öğeleri) · TS ISO/IEC 33020, Madde 5.2.4 (PA 1.1, PA 2.1 a–g, PA 2.2 a–e) ve Madde 5.3 (ölçek)
**Değerlendiren:** Bilgi İşlem
**Ekler:** [Ek A — MAN.1, 2, 4, 5, 6](2026-10-02-t3-surec-denetimi-ek-A.md) · [Ek B — MAN.8, TEC.2, 3, 5, 7](2026-10-02-t3-surec-denetimi-ek-B.md) · [Ek C — TEC.8, 9, 10, 11, 13](2026-10-02-t3-surec-denetimi-ek-C.md)

---

## 1. Amaç, sınırlar ve yöntem

### 1.1 Amaç

Bu denetim, "her modül tamamlandığında 33061 süreçlerinin doğru işletildiği denetlenir" kuralının ilk uygulamasıdır (Doğuş Uçanok, 02.10.2026). Sorduğu soru şudur: T3 geliştirilirken kapsamdaki 15 süreç, standardın kendi ölçütlerine göre **fiilen** nasıl işletildi?

### 1.2 Bu bir öz değerlendirmedir

Değerlendiren ile süreci yürüten aynı birimdir. Bu yüzden rapor, TS ISO/IEC 33020 anlamında bağımsız bir değerlendirme değildir. Dereceler bir **teşhistir**, beyan değildir. Bu sınır 18.09 raporuyla aynıdır.

### 1.3 Yöntem

- **Süreç grupları:** 15 süreç üç gruba ayrıldı ve her grup ayrı ayrı incelendi. Ayrıntılı bulgular eklerdedir.
- **İnceleme yöntemi:** İnceleme salt okumadır. Her çıktı (a, b, c…) için kanıt aranmıştır: dosya, issue/PR numarası, CI çalışması veya tarih.
  - "Planlandı", "yapılacak" veya "PR notunda söz verildi" kanıt sayılmamıştır.
  - Ölçek N/P/L/F'dir. `+` ve `−` işaretleri yalnızca bant içindeki konumu gösterir.
- **Kritik iddiaların ayrıca doğrulanması:** Eklerdeki kritik iddialar ana rapordan önce ayrıca kontrol edilmiştir:
  - SYG-KMLK-060/061/062'nin durumu: doğrulandı (§4, T3-01).
  - Ek C'deki "UAT e-posta kanalı `Unhealthy`" bulgusu: **güncel değil**. SMTP sertifikası 01.10.2026'da yenilendi; UAT `/health/notifications` 02.10.2026'da `email: Healthy` döndü. Bulgunun geçerli kalan kısmı (olayın kaydı yok) T3-07'ye alındı.

### 1.4 Seviye kuralı

18.09 raporu §1.3 ile aynıdır:
- **Seviye 2:** PA 1.1 = F, PA 2.1 ≥ L ve PA 2.2 ≥ L.
- **Seviye 1:** PA 1.1 ≥ L.
- Aksi hâlde **Seviye 0.**

---

## 2. Yönetici özeti

**T3'ün ürün tarafı güçlü, süreç tarafı geride kaldı.**

- **Ürün tarafı:**
  - 56 onaylı paydaş gereksiniminin tamamı sistem gereksinimine bağlı. Bu bağ her PR'da otomatik denetleniyor.
  - 973 otomatik test var (backend 743, frontend 230). Hatalar kök nedeniyle ve regresyon testiyle kapatıldı.
  - Karar kayıt defteri ile izlenebilirlik matrisi her PR'la güncellendi.
- **Süreç tarafı:**
  - T3 boyunca **yönetim süreçleri iş üretmedi.** Risk defteri, plan, durum raporu, CHANGELOG ve sürüm etiketi güncellenmedi.
  - **TEC.5, 7, 8, 9, 11 ve 13'ün yaklaşım belgesi hâlâ yok.**

**Seviye 2'ye ulaşan süreç yok.** 18.09'da Seviye 2 olan MAN.4 Seviye 1'e düştü. MAN.5 ve MAN.6 Seviye 0'a düştü. TEC.2 ve TEC.3 belirgin biçimde ilerledi.

**T3 henüz "tamamlandı" sayılamaz.** Bunun üç nedeni var:
1. Üç sistem gereksinimi (SYG-KMLK-060, 061, 062) doğrulanmadı ve hiçbir iş kalemiyle izlenmiyor.
2. SYG-KMLK-077 (giriş yanıt süresi) UAT ölçümünde karşılanmadı.
3. İK kabulü (TEC.11) için plan yok.

Bunlar giderilmeden T3 kabulüne gidilmemelidir (§6, Yüksek öncelik).

**Düşüşlerin bir kısmı ölçütün yükselmesinden geliyor.** 18.09'da birçok çıktı "sırası gelmedi" sayılabiliyordu. T3 gerçek LOGO verisiyle UAT'ye çıkınca baseline, sürüm kontrolü, doğrulama raporu ve geçerleme planı artık zorunlu hâle geldi. Gerçek gerileme ise yönetim süreçlerinin işletilmemesidir.

---

## 3. Derecelendirme tablosu

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | 18.09 (PA 1.1/2.1/2.2 → Seviye) | Eğilim |
|---|---|---|---|---|---|---|
| MAN.1 Proje planlama | L− | L− | P+ | 1 | L/L/L → 1 | ↓ |
| MAN.2 Proje değerlendirme ve kontrol | P | P | P | 0 | P+/P+/L → 0 | ↓ |
| MAN.4 Risk yönetimi | L+ | L− | L− | 1 | F/L/L → **2** | ↓↓ |
| MAN.5 Konfigürasyon yönetimi | P+ | L− | L− | 0 | L−/L/L → 1 | ↓↓ |
| MAN.6 Bilgi yönetimi | P+ | P | P+ | 0 | L−/P+/L− → 1 | ↓↓ |
| MAN.8 Kalite güvence | L− | P+ | L− | 1 | L/L/L → 1 | ↓ |
| TEC.2 Paydaş ihtiyaç ve gereksinimleri | L+ | L | L | 1 | P+/P+/L− → 0 | ↑↑ |
| TEC.3 Sistem/yazılım gereksinimleri | L+ | L− | L− | 1 | N/—/— → 0 | ↑↑ |
| TEC.5 Tasarım tanımlama | L | P+ | P+ | 1 | L/L/L → 1 | ↓ |
| TEC.7 Gerçekleştirme | L | L− | L− | 1 | L+/L/L → 1 | ↓ |
| TEC.8 Entegrasyon | L | L− | P+ | 1 | L/L/P+ → 1 | ↓ |
| TEC.9 Doğrulama | L | L | P+ | 1 | L+/L/L− → 1 | ↓ |
| TEC.10 Geçiş | L− | P+ | L− | 1 | L−/L/L → 1 | ↓ |
| TEC.11 Geçerleme | P | P | P | 0 | N/—/— → 0 | ↑ |
| TEC.13 Bakım | P+ | P | P+ | 0 | P−/P/P → 0 | ↑ |

**Seviye dağılımı:**
- **Seviye 2:** yok (18.09'da 1 süreç).
- **Seviye 1:** 10 süreç (18.09'da 9).
- **Seviye 0:** 5 süreç (18.09'da 5).

Bu tablo `00-OLGUNLUK-SEVIYESI-KRITERLERI.md` §6'ya işlenmiştir.

---

## 4. Bulgular

18.09 bulgularının (BULGU-01…11) durumu §5'tedir. Bu bölüm T3 çevriminde ortaya çıkan bulguları verir.

### T3-01 — Üç sistem gereksinimi doğrulanmadı ve izlenmiyor · **Yüksek**

| Gereksinim | Durum |
|---|---|
| **SYG-KMLK-060** Kimlik olaylarının denetim izine yazılması | Olaylar uygulama günlüğüne yazılıyor. Ancak gereksinimin saydığı olayların (üyelik başlatma, kod doğrulama, giriş başarılı/başarısız, oturum sonlandırma, davet…) denetim izinde kullanıcı, IP ve izleme kimliğiyle tutulduğunu gösteren bir test yok. |
| **SYG-KMLK-061** Günlüklerde TCKN, telefon ve e-posta maskelemesi | Maskeleme altyapısı A1'den beri var (`KR-059`). Kimlik akışlarının günlüklerinde maskelemeyi doğrulayan bir test yok. |
| **SYG-KMLK-062** İşlemsel iletilerin bildirim istisnasından muaf olması | `PRM-BLD-03` katalogda var ama hiçbir kodda okunmuyor. Bildirim istisnası mekanizması henüz yok; bu yüzden gereksinim bugün "kendiliğinden" sağlanıyor ama doğrulanmamış. |

**Neden yakalanmadı?** İzlenebilirlik denetim betiği yalnızca paydaş gereksinimi ile sistem gereksinimi arasındaki bağı denetliyor; sistem gereksinimi ile test arasındaki bağı denetlemiyor. Bu üç madde PR #110'daki "kalanlar" listesine de girmedi. Matriste "sonraki iş" veya "—" olarak kaldılar.

### T3-02 — SYG-KMLK-077 karşılanmıyor · **Yüksek**

Ayrıntı: [UAT performans ölçüm raporu](../../TEC.9-dogrulama/raporlar/2026-10-02-t3-performans-olcumu.md).

- **Gerçekçi yük:** Mesai başında 50 kullanıcı 60 saniyeye yayılarak giriş yapıyor; p95 1,021 sn.
  - Bu sürenin tamamına yakını, giriş ucuna yanlışlıkla uygulanmış 1 sn'lik en kısa yanıt süresinden geliyor.
- **50 isteğin aynı anda gelmesi:** p95 4,7–7,4 sn.
  - Sunucu 2 çekirdekli ve saniyede ≈11 giriş işleyebiliyor.
  - Bu durumda gereksinim, SYG-KMLK-049'un parola özeti alt sınırıyla çelişiyor.
- **Ne gerekiyor:** Bir hata düzeltmesi ve gereksinimin anlamı için bir karar.
- **Diğer ölçümler:** SYG-KMLK-078 karşılanıyor (p95 0,43 sn). SYG-KMLK-079 SMS için karşılanıyor (p95 0,50 sn). E-posta için örnek yetersiz.

### T3-03 — T3 geçerlemesi (İK kabulü) için plan yok · **Yüksek**

- **Var olan:** İK onaylı kabul kriterleri var (PG-KMLK, TEC.11 (a) = F).
- **Eksik olan:**
  - Geçerleme planı, kabul senaryoları ve kabul formu şablonu.
  - UAT'de kabul katılımcılarının izin listesi.
  - AN-01'in (kabul kriterini tam karşılamayan kesim) İK'ya nasıl gösterileceği.
- **Matris:** "Kabul" sütunu 56/56 boş. T3 kabul edilmediği için bu beklenen bir durum, ama plan olmadan bu sütun dolmaz.

### T3-04 — Sürüm, baseline ve dağıtım kaydı yok · **Yüksek**

- **Etiket ve sürüm notu:** Git etiketi ve GitHub Release yok. A1'in planlanan `v0.1.0` etiketi hiç oluşturulmadı. `CHANGELOG.md` 08.09'dan beri değişmedi; arada 56 PR birleşti.
- **UAT dağıtımı:**
  - UAT'ye çalışma kopyası aktarılıyor ve imajlar değişken `:uat` etiketini taşıyor.
  - Hangi commit'in kurulu olduğu yalnızca bu oturumun notlarında var; depoda kayıt yok.
  - Bu durum ADR-0011 §7'ye ve proje planı §3'e aykırıdır.
- **Gereksinim baseline'ı:** SYG-KMLK'nin onay alanı ve onay kaydı yok.

### T3-05 — Yönetim süreçleri T3 boyunca işletilmedi · **Yüksek**

- **İş kalemi:** `surec:MAN.2` ve `surec:MAN.4` etiketli hiç issue yok.
- **Bayat belgeler:** Risk defteri 22.09'dan beri değişmedi. Proje planı ve RACI 07.09 tarihli ve onaysız.
- **Risk kaydına girmeyenler:**
  - T3'te ortaya çıkan riskler: SMTP sertifikası, sunucu saati (#103), güvenilen vekil ağı, tek kişilik inceleme.
  - `KR-078`'deki kalan risk kabulü. Bu kabulü Bilgi İşlem yapmış; RACI'ye göre yetki Üst Yönetim'dedir.
- **Durum raporu:** İlk durum raporu "A2 başlarken" diye taahhüt edilmişti, yazılmadı. Plandaki beş ölçütün hiçbiri ölçülmedi.

### T3-06 — Kayıtlı bağımsız inceleme yok · **Orta**

- **Bulgu:** T3'teki 21 PR'ın hiçbirinde GitHub inceleme kaydı yok. PR'ı açan ve birleştiren aynı hesap olduğu için GitHub onaya izin vermiyor.
- **Pratikte:** Her PR'ı Doğuş Uçanok inceleyip onayladı. Ancak bu onay yalnızca oturum içi yazışmada duruyor.
- **Belgelerle çelişki:** ADR-0011 §6, CONTRIBUTING ve 00-OLGUNLUK §5(d) "en az bir onay" diyor.
- **Gereken:** Tek kişilik düzen için yazılı bir telafi edici kontrol. Örneğin onay PR'a yorum olarak yazılır ve birleştirme bu yorumdan sonra yapılır.

### T3-07 — Olaylar kayda geçmedi · **Orta**

Aşağıdaki olayların hiçbiri için issue, düzeltici faaliyet veya risk kaydı açılmadı:

| Olay | Kaydedilmesi gereken |
|---|---|
| 30.09 `git stash -u` olayı | Boş süreç klasörlerinin silinmesi; bu PR'da düzeltici faaliyet olarak kaydedildi (§5, BULGU-02) |
| 28.09 Dal Koruma Denetimi | GitHub API 500 hatasıyla çöktü (çalışma 36471125839) |
| 30.09 `main` kırmızı | CVE-2026-84782 nedeniyle ≈21 saat |
| SMTP 587 sertifika sorunu | Olay ve 11.10.2026 sertifika bitişi |

### T3-08 — Yaklaşım belgeleri ve süreç klasörleri · **Orta**

- **Yaklaşım belgeleri:** Yalnızca TEC.2 ve TEC.3'ün `YAKLASIM.md`'si var. İkisi iyi bir şablon oluşturuyor.
- **Süreç klasörleri:**
  - Sekiz sürecin klasörü 30.09'da silinmişti. **Bu PR'da yeniden oluşturuldu** ve `.gitkeep` ile izlenmeye alındı.
  - Doküman haritası artık fiziksel klasörlerle uyumlu.

### T3-09 — Belge tutarlılığı · **Düşük–Orta**

- **UAT runbook'u:** §1 "maskelenmiş kopya" diyor, §10.2 "gerçek LOGO verisi" diyor. Yedekleme kararı da yazılmamış.
- **SYG-KMLK:** §2.2'de API yolu hâlâ `/api/v1/kimlik/` olarak geçiyor. §7'deki bazı durumlar tamamlanmış işleri hâlâ bekliyor gösteriyor.
- **İzlenebilirlik matrisi §5:**
  - ≈16 satır tamamlanmış olduğu hâlde "kısmen" etiketini taşıyor.
  - Satır 037'nin işi PR #108'de yapıldı ama satır güncellenmedi.
  - Satır 042'de gerekçe yok.
- **ADR'ler:**
  - ADR-0006 ve ADR-0012'de kararlar yerinde değiştirildi. README kural #2 ise "ADR değiştirilmez, yerine yenisi yazılır" diyor.
  - ADR dizini 06.09'dan beri güncellenmedi.
- **PR boyutu ve Tamamlanma Tanımı:**
  - PR'lar +2.400 ile +5.100 satır arasında; kural 400'ün altını istiyor.
  - "Atlanamaz" denen maskeleme testi maddesi #101'den sonra altı PR'da düşmüş.

### T3-10 — İK'ya dönülmemiş yorumlar · **Düşük**

AN-23/`KR-087` (etkinlik sinyalinin kapsamı) ve AN-24/`KR-092` (parola değişim süresi, ilk giriş kapsamı) Bilgi İşlem kararıyla netleştirildi. TEC.2 YAKLASIM §4 şu kuralı koyar: bir paydaş gereksiniminin **anlamını** değiştiren karar İK onayı ister. İkisi de anlamı daraltmaktan çok netleştiriyor. Yine de kabul toplantısında İK'ya teyit ettirilmelidir.

---

## 5. 18.09 bulgularının durumu

| Bulgu | Konu | Durum |
|---|---|---|
| BULGU-01 | İzlenebilirlik matrisi yok | ✅ Kapandı (#54). Matris 20 sürümde güncellendi; REQ↔SYG halkası otomatik denetleniyor |
| BULGU-02 | Yaklaşım belgeleri yok | ⚠️ Kısmen: TEC.2 ve TEC.3 yazıldı (#52, #65); 13 süreç hâlâ eksik. Klasör kaybı bu PR'da düzeltildi |
| BULGU-03 | Öz değerlendirme güncellenmedi, iki ölçek | ✅ Ölçek düzeldi. Tablo 18.09'dan beri ilk kez bu raporla güncellendi |
| BULGU-04 | MAN.2 çıktıları üretilmiyor | ❌ Açık; issue yok |
| BULGU-05 | Konfigürasyon öğeleri, denetim, CHANGELOG | ❌ Açık, büyüdü (T3-04); issue yok |
| BULGU-06 | Bilgi kayıt defteri, saklama düzeni | ❌ Açık; issue yok. #73 (sır kopyası içeren yedek) 25.09'dan beri açık |
| BULGU-07 | Kalite yordamları, süreç değerlendirmesi | ⚠️ Bu rapor ikinci süreç değerlendirmesidir; yordam belgesi yok |
| BULGU-08 | Milestone–WBS uyumsuzluğu | ✅ Kapandı (#50) |
| BULGU-09 | Paydaş kayıtları sürüm kontrolünde değil | ✅ Kapandı (#52, #63) |
| BULGU-10 | Kalite kapısı sayısı tutarsız | ❌ Açık |
| BULGU-11 | Plan ve RACI onayı | ❌ Açık; ilerleme yok |

**Kök neden:** Bulguların issue'ya dönüştürülmemesi. 18.09 raporu "öneriler" listesiyle bitti, ama öneriler iş kalemine çevrilmedi. Bu yüzden T3'ün yoğun akışında unutuldular. Bu raporun düzeltici işleri **issue olarak** açılacaktır (§6).

---

## 6. Düzeltici iş planı

Her satır bir issue'dur. "Yüksek" öncelikli işler **T3 kabulünden önce** tamamlanır.

| # | İş | Bulgu | Öncelik |
|---|---|---|---|
| 1 | SYG-KMLK-060, 061 ve 062'nin gerçekleştirilmesi veya doğrulanması; matris satırlarının güncellenmesi | T3-01 | Yüksek |
| 2 | Giriş ucundan 1 sn'lik alt sınırın kaldırılması; SYG-KMLK-077'nin anlamı için karar (AN-25); ölçümün tekrarı | T3-02 | Yüksek |
| 3 | SYG-KMLK-079 e-posta ölçümünün tamamlanması (izin listesi adresine 20 gönderim) | T3-02 | Yüksek |
| 4 | Gereksinim denetim betiğinin genişletilmesi: her SYG'nin en az bir teste veya gerekçeye bağlı olması CI'da denetlenir | T3-01 | Yüksek |
| 5 | T3 doğrulama raporu (79 SYG satırı: yöntem, sonuç, kanıt) ve G2 kapı kaydı; TEC.9 `YAKLASIM.md` | T3-01, T3-02 | Yüksek |
| 6 | T3 kabul planı, kabul senaryoları ve kabul formu şablonu; TEC.11 `YAKLASIM.md` | T3-03 | Yüksek |
| 7 | Sürüm etiketleri (A1 `v0.1.0`, T3 kabul adayı), CHANGELOG; `deploy-uat.sh` commit SHA'sını imaja ve dağıtım kaydına yazar | T3-04 | Yüksek |
| 8 | T3 risk gözden geçirmesi; `KR-078` risk kabulünün RACI'ye uygun hâle getirilmesi; yeni riskler | T3-05 | Yüksek |
| 9 | İlk durum raporu (ölçütlerin gerçek değerleriyle); proje planı v0.2 ve RACI'nin onaya sunulması | T3-05, BULGU-04, 11 | Yüksek |
| 10 | Tek kişilik inceleme için telafi edici kontrol kararı; CONTRIBUTING ile PR şablonunun birleştirilmesi | T3-06 | Orta |
| 11 | Olay kayıtları: stash olayı, denetim çökmesi, CVE kırmızısı, SMTP; `main` kırmızısı için zorunlu issue kuralı | T3-07 | Orta |
| 12 | MAN.1, 2, 4, 5, 6, 8 ve TEC.5, 7, 8, 13 `YAKLASIM.md` belgeleri | T3-08, BULGU-02 | Orta |
| 13 | MAN.6 bilgi kayıt defteri (depo dışı bilgiler dâhil), saklama ve imha; #73'ün kapatılması | BULGU-06 | Orta |
| 14 | Konfigürasyon öğeleri listesi ve ilk konfigürasyon denetimi | BULGU-05 | Orta |
| 15 | Belge tutarlılığı: runbook veri sınıfı ve yedekleme, SYG §2.2/§7, matris §5 temizliği, ADR yönetişimi | T3-09 | Orta |
| 16 | TEC.8 T3 entegrasyon raporu (LOGO, NetGSM, SMTP, nginx) | Ek C | Orta |
| 17 | AN-23 ve AN-24'ün kabul toplantısında İK'ya teyidi | T3-10 | Düşük |
| 18 | #89 (kapsam kapısı kararsızlığı) ve CI kanıtlarının kalıcı arşivi | Ek C | Düşük |

**Sıralama önerisi:**
1. **Önce 1–4:** T3'ün ürün tarafını kapatır.
2. **Sonra 5–9:** Kabul öncesi süreç kanıtını üretir.
3. **Son olarak 10–18:** T4 başlamadan önce veya T4 ile birlikte yapılır.

---

## 7. Güçlü yönler

- **İzlenebilirlik:** REQ → SYG halkası 56/56 bağlı ve CI'da denetleniyor. Matris her PR'la güncellendi.
- **Karar disiplini:** 93 karar kaydı tutuldu. Her biri gerekçeli ve tarihli; çoğu PR'a bağlı.
- **Analiz:** SYG-KMLK'deki 24 analiz bulgusu belirsizliği tek anlama indirdi. Paydaş kararı gereken yerlerde karar beklendi (ör. AN-24).
- **Hata döngüsü:** Hata issue'larında (#93, #97, #102, #103) kök neden, "neden şimdiye kadar görülmedi" sorusu ve regresyon testi var.
- **Dış arayüzler:** Gerçek konuşmayla sınandılar. LOGO'nun yazma reddi CI'da kanıtlanıyor. SMTP Mailpit ile, NetGSM sonuç kodlarıyla sınanıyor.
- **Ölçüme dayalı doğrulama:** T3'ün performans ölçütleri bu denetimle UAT'de ilk kez ölçüldü. Karşılanmayan gereksinim açıkça raporlandı.

---

## 8. Sonraki denetim

- **T3 kabulünden sonra:** Kısa bir ara denetim yapılır ve §6'daki Yüksek öncelikli işlerin kapanışı doğrulanır.
- **T4 tamamlandığında:** Tam denetim yapılır (modül sonu kuralı).
- **Hedef:** Bir sonraki tam denetimde en az MAN.4, TEC.2, TEC.3 ve TEC.9'un Seviye 2'ye çıkması.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-02 | 1.0 | İlk sürüm: T3 sonu süreç denetimi (#117) | Bilgi İşlem |
