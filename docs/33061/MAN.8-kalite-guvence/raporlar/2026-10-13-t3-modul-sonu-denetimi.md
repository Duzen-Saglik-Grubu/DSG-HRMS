# 33061 Süreç Denetim Raporu — T3 Modül Sonu

**Belge kimliği:** MAN.8-SGR-2026-10-13
**Süreç:** MAN.8 — Kalite Güvence (süreç değerlendirmesi)
**Değerlendirme tarihi:** 2026-10-13
**Değerlendirilen dönem:** 02.10.2026 denetiminden (MAN.8-SGR-2026-10-02, `main` @ `4199526`) T3'ün kabulüne ve `v0.2.0` etiketine kadar (`main` @ `d354e94`). Bu dönemde T3 kabul hazırlığı, beş kabul adayı (rc.1–rc.5), iki değişiklik talebi (#185, #190) ve İK kabulü (13.10.2026, `KR-105`) yer aldı.
**Dayanak:** TS ISO/IEC TS 33061, Madde 5 (süreç amaçları, çıktılar, temel uygulamalar, bilgi öğeleri) · TS ISO/IEC 33020, Madde 5.2.4 (PA 1.1, PA 2.1 a–g, PA 2.2 a–e) ve Madde 5.3 (ölçek)
**Değerlendiren:** Bilgi İşlem
**Ekler:** [Ek A — MAN.1, 2, 4, 5, 6](2026-10-13-t3-modul-sonu-denetimi-ek-A.md) · [Ek B — MAN.8, TEC.2, 3, 5, 7](2026-10-13-t3-modul-sonu-denetimi-ek-B.md) · [Ek C — TEC.8, 9, 10, 11, 13](2026-10-13-t3-modul-sonu-denetimi-ek-C.md) · Konfigürasyon denetimi: `MAN.5-konfigurasyon-yonetimi/raporlar/2026-10-13-t3-konfigurasyon-denetimi.md`

---

## 1. Amaç, sınırlar ve yöntem

### 1.1 Amaç

Modül sonu 33061 denetim kuralının ikinci uygulamasıdır; ilk uygulama 02.10.2026'da T3'ün geliştirmesi bittiğinde yapıldı. Bu denetim, T3 kabul edilip kapanırken iki soruya cevap arar:

1. 02.10'da bulunan eksikler giderildi mi?
2. Kabule giden son on günde 15 süreç standardın ölçütlerine göre **fiilen** nasıl işletildi?

### 1.2 Bu bir öz değerlendirmedir

Değerlendiren ile süreci yürüten aynı birimdir. Dereceler bir **teşhistir**, beyan değildir (02.10 raporu §1.2 ile aynı sınır).

### 1.3 Yöntem

- **Süreç grupları:** 15 süreç 02.10'daki gibi üç gruba ayrıldı (Ek A, B, C). Konfigürasyon denetimi ayrı rapor olarak yapıldı.
- **Kanıt:** Her çıktı için dosya, issue/PR numarası, CI çalışması veya tarih arandı. "Planlandı" kanıt sayılmadı. İnceleme salt okumadır; UAT sunucusunda yalnızca okuma komutları çalıştırıldı, sır ve kişisel veri açılmadı.
- **Aynı gün üretilen belgeler:** Bu denetimle birlikte yazılan kapanış gözden geçirmesi, alınan dersler ve durum raporu 2 eklerde kanıt **sayılmadı**; yalnızca birleştirildikten sonraki denetimde sayılır.
- **Kritik iddiaların ayrıca doğrulanması:** Eklerdeki şu iddialar ana rapordan önce doğrudan kontrol edildi ve doğrulandı:
  - UAT'de `/opt/dsg-hrms/secrets/.env.uat.yedek-20260929` var (27.09.2026).
  - 05.10.2026 20:34–20:55 UTC arasında `main-failure-check` 7 kez başarısız oldu; işi adım çalıştırmadan iptal edildi (`cancelled`, 0 adım) ve "main kırmızı" issue'su açılmadı.
  - T3 doğrulama raporu `fb58fbe`'yi ve 79 SYG'yi kapsıyor.
  - #185'in uygulama PR'ı (#186) 10.10 12:16 UTC'de birleşti; İK onayını kaydeden PR (#188) 13:05'te açıldı.

### 1.4 Seviye kuralı

02.10 raporu §1.4 ile aynıdır:
- **Seviye 2:** PA 1.1 = F, PA 2.1 ≥ L ve PA 2.2 ≥ L.
- **Seviye 1:** PA 1.1 ≥ L.
- Aksi hâlde **Seviye 0.**

---

## 2. Yönetici özeti

- **15 sürecin hepsi Seviye 1.** 02.10'da beş süreç (MAN.2, MAN.5, MAN.6, TEC.11, TEC.13) Seviye 0'daydı; hepsi Seviye 1'e çıktı. Seviye 2'de süreç yok, çünkü hiçbirinde PA 1.1 = F değil.
- **02.10 iş planı tamamlandı.** 18 düzeltici işin (#119–#136) 18'i de kapandı. 10 bulgudan 9'u kapandı; T3-09 (PR boyutu ve tamamlanma tanımı) açık kaldı ve #216'ya alındı.
- **T3 koşulsuz kabul edildi** (`KR-105`); izlenebilirlik kaynaktan kabule %100.
- **Engel artık bakım.**
  - Yazılan kurallar birkaç gün içinde kopyalarda bayatlıyor (ADR-0006 §5, SYG §1 sayımları, "Son güncelleme" alanları).
  - Kanıt kabul edilen adaya kadar sürdürülmüyor: doğrulama raporu rc.2'de, entegrasyon raporu rc.2'de kaldı.
  - Otomatik kontroller sessizce çalışmayabiliyor (05.10 olayı).
  - Gereksinim zinciri yalnızca ileri yönde denetleniyor: kodda olup SYG'de olmayan davranış (#138) ve kapsam dışı öğe (#198) yakalanmadı.
- **Yeni düzeltici iş planı: 13 iş** (#207–#219), §6.

---

## 3. Derecelendirme tablosu

| Süreç | 02.10 PA 1.1 / 2.1 / 2.2 → Sv | 13.10 PA 1.1 / 2.1 / 2.2 → Sv | Ek |
|---|---|---|---|
| MAN.1 Proje planlama | L− / L− / P+ → 1 | L+ / L+ / L → **1** | A |
| MAN.2 Proje değerlendirme ve kontrol | P / P / P → 0 | L / L− / L− → **1** | A |
| MAN.4 Risk yönetimi | L+ / L− / L− → 1 | L+ / L+ / L → **1** | A |
| MAN.5 Konfigürasyon yönetimi | P+ / L− / L− → 0 | L+ / L / L → **1** | A |
| MAN.6 Bilgi yönetimi | P+ / P / P+ → 0 | L− / L− / L− → **1** | A |
| MAN.8 Kalite güvence | L− / P+ / L− → 1 | L+ / L / L → **1** | B |
| TEC.2 Paydaş ihtiyaç ve gereksinimleri | L+ / L / L → 1 | L+ / L / L → **1** | B |
| TEC.3 Sistem/yazılım gereksinimleri | L+ / L− / L− → 1 | L+ / L / L → **1** | B |
| TEC.5 Tasarım tanımlama | L / P+ / P+ → 1 | L / L− / L− → **1** | B |
| TEC.7 Gerçekleştirme | L / L− / L− → 1 | L+ / L / L → **1** | B |
| TEC.8 Entegrasyon | L / L− / P+ → 1 | L+ / L / L → **1** | C |
| TEC.9 Doğrulama | L / L / P+ → 1 | L+ / L / L → **1** | C |
| TEC.10 Geçiş | L− / P+ / L− → 1 | L+ / L+ / L → **1** | C |
| TEC.11 Geçerleme | P / P / P → 0 | L+ / L / L− → **1** | C |
| TEC.13 Bakım | P+ / P / P+ → 0 | L− / P+ / L− → **1** | C |

**Seviye 2 için en yakın süreçler:** MAN.1, MAN.4, MAN.5, MAN.8, TEC.3, TEC.7, TEC.9, TEC.10. PA 1.1'i L+'tan F'ye çıkarmak için gereken, çıktıların **sürekliliğidir**: aynı kayıtların sonraki modülde kendiliğinden, eksiksiz ve güncel üretilmesi.

---

## 4. Bulgular

Bulguların ayrıntısı eklerdedir. Burada önem sırasıyla birleştirilmiş hâlleri var; her biri §6'daki bir işe bağlıdır.

### M-01 — UAT sunucusunda kayıt dışı eski sır kopyası ve gereksiz servisler · **Yüksek**
`/opt/dsg-hrms/secrets/.env.uat.yedek-20260929` ne bilgi kayıt defterinde ne konfigürasyon öğelerinde var; ilk konfigürasyon denetimi (05.10) yakalamadı. Sunucuda masaüstü kurulumundan kalan Avahi (UDP 5353, dışa açık) ve CUPS (yerel) çalışıyor; yeniden başlatma bekleniyor. *(Ek A A-01; MAN.5 B-1, B-3)* → **#207**

### M-02 — Otomatik kontroller sessizce çalışmayabiliyor · **Yüksek**
05.10'da `main` kırmızıydı; bunu yakalamak için kurulan `main-failure-check` adım çalıştırmadan iptal oldu ve issue açmadı. Dal Koruma Denetimi iki PR'da koşmadı. Olay kayda geçmedi. *(MAN.5 B-4; Ek A A-03; Ek C YB-08)* → **#208**

### M-03 — Doğrulama kanıtı kabul edilen sürümü kapsamıyor · **Yüksek**
Doğrulama raporu ve G2, 79 SYG ve `fb58fbe` için. Kabul edilen `v0.2.0` 81 SYG içeriyor; G2'den sonra gelen iki değişiklik talebi ve iki hata düzeltmesi raporda yok. *(Ek C YB-01, YB-13)* → **#209**

### M-04 — Değişiklik talebi yordamı yazılı değil · **Yüksek**
Yol büyük ölçüde denetimliydi: issue, etki analizi, PG/SYG/matris güncellemesi; kabulde T-03 ve T-04 teyidi. Ama #185'te uygulama İK onayı kayda geçmeden birleşti; onaylar ikinci elden kaydedildi; #190'ın kapsamı PR sırasında genişledi. *(Ek B TEC.2-5)* → **#210**

### M-05 — Gereksinim zinciri yalnızca ileri yönde denetleniyor · **Yüksek**
Her SYG'nin testi CI'da denetleniyor. Ama koddaki her öğenin onaylı bir SYG'ye dayandığı denetlenmiyor: #198 (T5 parametresi 16 gün T3'te), #138 (SYG'de olmayan kural). *(Ek B TEC.7-7, TEC.3-6; Ek C YB-02)* → **#211**

### M-06 — Kabul kanıtı imzasız ve tek kopya · **Yüksek**
İmzalar onaylı kalite belgeleriyle sonradan toplanacak (Doğuş Uçanok'un kararı). O zamana kadar kanıt, depo kaydı ve Masaüstü'ndeki tek Word dosyasıdır. Bu PR'da formun SHA-256 özeti depo kaydına yazıldı. TEC.11 (g) bu yüzden L− derecelendi; imzalı nüsha BK-24'e konursa F olur. *(Ek C YB-04; Ek A A-17)* → **#213**

### M-07 — ADR'ler kararların gerisinde · **Orta**
ADR-0006 §5 #185'ten sonraki sınırları yansıtmıyor; KR-104'ün ADR sınıflandırması yapılmadı; ADR-0011 §7 UAT için "maskelenmiş kopya" diyor. *(Ek B TEC.5-7; Ek C YB-11)* → **#212**

### M-08 — Belge tutarlılığı ve kayıt dışı bilgi kümeleri · **Orta**
13 belgede "Son güncelleme" bayat; SYG §1 sayımları, doküman haritası, YAKLAŞIM tabloları, entegrasyon raporu ve konfigürasyon öğeleri listesi güncel değil. Bilgi kayıt defterinde `src_old/`, `docker/.env`, yerel günlükler ve birkaç Masaüstü klasörü yok. *(Ek A A-02, A-06, A-08, A-09; Ek B TEC.3-7; Ek C YB-07; MAN.5 B-2, B-3)* → **#214, #215**

### M-09 — Issue ve PR disiplini denetlenmiyor · **Orta**
04.10'dan sonraki 23 issue'nun 13'ünde `tur:` etiketi yok; tamamlanma tanımının "atlanamaz" iki maddesi örneklenen PR'larda yok; #191 +5.319 satır. Onay yorumu 36/36 PR'da birleştirmeden önce var (02.10 bulgusu T3-06 kapandı). *(Ek A A-05; Ek B MAN.8-9, MAN.8-10, TEC.7-8, TEC.7-9; Ek C YB-09)* → **#216**

### M-10 — Denetim önerileri issue'ya dönüşmeden kalıyor · **Orta**
02.10 Ek B'deki yaklaşık 13 öneri yalnızca YAKLAŞIM "açık noktalar" tablolarında duruyor. Kalite güvence ölçütü tanımlı değil. *(Ek B MAN.8-8, MAN.8-11)* → **#217**

### M-11 — Sürüm adımları tek listede değil · **Orta**
rc.3'ün CI kanıt özeti geç üretildi (#194); rc.3–rc.5 için konfigürasyon denetimi yapılmadı; CI özetlerinde uçtan uca sonuç yok. *(Ek A A-04, A-14; Ek C YB-03, YB-12)* → **#218**

### M-12 — Kabulün temsil gücü · **Orta**
56 gereksinimin 15'i yalnızca Bilgi İşlem kanıt gösterimiyle kabul edildi; personel katılımcıların dördü de Bilgi İşlem'den; senaryo başına gözlem notu yok; `KR-106` izin listesindeki iletişim verisini saklıyor. *(Ek C YB-05, YB-06, YB-14)* → **#219**

---

## 5. 02.10 bulgularının durumu

| Bulgu | Durum | Kanıt |
|---|---|---|
| T3-01 SYG-060/061/062 | ✅ Kapandı | #119 → PR #140 |
| T3-02 SYG-077 | ✅ Kapandı | #120 → PR #141, #143 (p95 0,279 sn) |
| T3-03 Kabul planı yok | ✅ Kapandı | #124; kabul 13.10.2026 |
| T3-04 Sürüm ve dağıtım kaydı | ✅ Kapandı | #125; rc.1–rc.5, `v0.2.0`, dağıtım kaydı sunucuyla 5/5 |
| T3-05 Yönetim süreçleri işletilmedi | ✅ Kapandı | #126, #127, #131, #132; plan onayı `KR-103` |
| T3-06 Kayıtlı inceleme yok | ✅ Kapandı | #128, `KR-096`; 36/36 PR |
| T3-07 Olaylar kayda geçmedi | ⚠️ Kural kuruldu, 05.10 olayı yine kayda geçmedi | #129 → M-02, #208 |
| T3-08 Yaklaşım belgeleri | ✅ Kapandı | #130 |
| T3-09 Belge tutarlılığı, PR boyutu ve DoD | ⚠️ Kısmen: #133 kapandı; PR boyutu ve DoD açık | M-08, M-09 |
| T3-10 İK'ya dönülmemiş yorumlar | ✅ Kapandı | #135; T-01, T-02 kabulde uygun |

---

## 6. Düzeltici iş planı

| # | İş | Bulgu | Öncelik | Issue |
|---|---|---|---|---|
| 1 | UAT: eski sır kopyası, Avahi/CUPS, yeniden başlatma, D-12 genişletme | M-01 | Yüksek | #207 |
| 2 | Otomatik kontrollerin izlenmesi; 05.10 olay kaydı; R-28 | M-02 | Yüksek | #208 |
| 3 | Doğrulama raporu 2.0, G2'nin yeniden kaydı, SYG-077 ölçümü | M-03 | Yüksek | #209 |
| 4 | Değişiklik talebi yordamı | M-04 | Yüksek | #210 |
| 5 | Geri yönde kapsam testi; #138 kuralının SYG'ye işlenmesi | M-05 | Yüksek | #211 |
| 6 | ADR-0006, KR-104 sınıflandırması, ADR-0011 §7 | M-07 | Orta | #212 |
| 7 | Kabul kanıtı: ikinci kopya, imza hedef tarihi, BK-24 konumu | M-06 | Yüksek | #213 |
| 8 | Belge tutarlılığı toplu düzeltme ve betik denetimi | M-08 | Orta | #214 |
| 9 | Bilgi kayıt defteri eksikleri ve saklama kararları | M-08 | Orta | #215 |
| 10 | Etiket, DoD, PR boyutu ve onay kaydı denetimleri | M-09 | Orta | #216 |
| 11 | 02.10 önerilerinin izlenmesi; kalite güvence ölçütleri | M-10 | Orta | #217 |
| 12 | Sürüm kontrol listesi; konfigürasyon denetimi sıklığı | M-11 | Orta | #218 |
| 13 | Kabul süreci iyileştirmeleri | M-12 | Orta | #219 |

**Sıra önerisi:** 1, 2 ve 7 bir sonraki modüle başlamadan; 3, 4, 5 sıradaki modülün G2'sinden önce; kalanlar sürekli iyileştirme (`SK`).

**Aynı PR'da yapılanlar:**
- Risk defteri gözden geçirildi; R-28–R-30 eklendi.
- Öz değerlendirme tablosu güncellendi.
- Kabul formu kaydına bütünlük özeti yazıldı ve imza ifadesi düzeltildi.

---

## 7. Güçlü yönler

- 02.10'daki 18 düzeltici işin tamamı 10.10'a kadar kapandı; denetimden işe dönüş çalışıyor.
- İzlenebilirlik CI'da denetleniyor; kaynaktan kabule %100.
- Sürüm etiketleri, CHANGELOG ve UAT dağıtım kaydı sunucuyla birebir (7/7 etiket, 5/5 dağıtım).
- Değişiklik talepleri kayıtlı ve İK'ya kabulde ayrıca teyit ettirildi.
- Kabul öncesi Bilgi İşlem provası dört bulguyu oturumdan önce kapattı; oturumda bulgu çıkmadı.

---

## 8. Sonraki denetim

Sıradaki modülün (T1, T2, T4 veya T5) sonunda. O denetimde ilk bakılacaklar:
- §6'daki işlerin durumu;
- alınan derslerin (`MAN.2-…/kayitlar/2026-10-13-t3-alinan-dersler.md`) uygulanması;
- Seviye 2'ye en yakın sekiz süreçte çıktıların sürekliliği.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-13 | 1.0 | İlk sürüm (#206) | Bilgi İşlem |
