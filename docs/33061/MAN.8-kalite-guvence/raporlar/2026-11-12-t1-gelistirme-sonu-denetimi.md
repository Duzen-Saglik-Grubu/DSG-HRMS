# 33061 Süreç Denetim Raporu — T1 Geliştirme Sonu

**Belge kimliği:** MAN.8-SGR-2026-11-12
**Süreç:** MAN.8 — Kalite Güvence (süreç değerlendirmesi)
**Değerlendirme tarihi:** 2026-11-12
**Değerlendirilen dönem:** T3 kabulünden (13.10.2026, `v0.2.0`, `main` @ `d354e94`) T1 geliştirmesinin sonuna (`main` @ `388da21`). Bu dönemde T1'in gereksinim toplantısı (19.10), tasarımı ve gözden geçirmesi (ADR-0019…0022), 87 PR, 9 değişiklik talebi, UAT kurulumu (09.11) ve UAT performans ölçümü (10.11) yer aldı.
**Dayanak:** TS ISO/IEC TS 33061, Madde 5 (süreç amaçları, çıktılar, temel uygulamalar, bilgi öğeleri) · TS ISO/IEC 33020, Madde 5.2.4 (PA 1.1, PA 2.1 a–g, PA 2.2 a–e) ve Madde 5.3 (ölçek)
**Değerlendiren:** Bilgi İşlem
**Ekler:** [Ek A — MAN.1, 2, 4, 5, 6](2026-11-12-t1-gelistirme-sonu-denetimi-ek-A.md) · [Ek B — MAN.8, TEC.2, 3, 5, 7](2026-11-12-t1-gelistirme-sonu-denetimi-ek-B.md) · [Ek C — TEC.8, 9, 10, 11, 13](2026-11-12-t1-gelistirme-sonu-denetimi-ek-C.md) · Konfigürasyon denetimi: `MAN.5-konfigurasyon-yonetimi/raporlar/2026-11-12-t1-konfigurasyon-denetimi.md`

---

## 1. Amaç, sınırlar ve yöntem

### 1.1 Amaç

Modül sonu 33061 denetim kuralının üçüncü uygulamasıdır. T3'te iki kez uygulandı: geliştirme bitince (02.10) ve kabulle kapanırken (13.10). Bu denetim T1'in geliştirmesi bitince yapıldı; T1 kabul edildiğinde tekrarlanır. Üç soruya cevap arar:

1. 13.10'daki 13 düzeltici iş (#207–#219) etkili oldu mu, T1 boyunca sürdü mü?
2. T1 boyunca 15 süreç standardın ölçütlerine göre **fiilen** nasıl işletildi?
3. T1 kabulüne geçmeden neyin eksik olduğu biliniyor mu?

### 1.2 Bu bir öz değerlendirmedir

Değerlendiren ile süreci yürüten aynı birimdir. Dereceler bir **teşhistir**, beyan değildir.

### 1.3 Yöntem

- **Süreç grupları:** 15 süreç T3'teki gibi üç gruba ayrıldı (Ek A, B, C). Her ek, ölçütleri standart belgelerinden okuyarak ayrı ayrı hazırlandı; ana rapor ekleri birleştirir.
- **Kanıt:** Her çıktı için dosya, issue/PR numarası, CI çalışması veya tarih arandı. "Planlandı" kanıt sayılmadı. UAT sunucusunda yalnızca okuma komutları çalıştırıldı; sır ve kişisel veri açılmadı. LOGO'ya bağlanılmadı.
- **Aynı PR'da üretilen belgeler** (durum raporu 3, risk gözden geçirmesi 1.16) dereceye kanıt **sayılmadı**; bir sonraki denetimde sayılır.
- **Kritik iddialar ana rapordan önce doğrudan doğrulandı:**
  - UAT'de `/opt/dsg-hrms/secrets/.env.uat.bak-20261103` var (03.11.2026; içeriği 21.10 sürümü). Dosya içeriği açılmadı.
  - 10.11'deki sentetik yük yığınından UAT'de 135 MB'lık anonim bir PostgreSQL birimi kaldı (10.11 00:29:47). Performans raporu "silindi" diyordu. Birim bu denetimde silindi.
  - 21.10–09.11 arasında UAT'de çalışan `e488898`, `main`'in atası değil (PR #258'in dal commit'i); kodu `f312510` ile birebir aynı.
  - PR #287 11:29:10Z'de birleşti; #284'teki İK onayı 11:34:57Z'de yazıldı.
  - CI'daki boyut denetimi `/boyut\s+gerek[çc]esi/i` arıyor; şablonun DoD satırı bu ifadeyi taşıdığı için her PR'da geçiyor.
  - UAT TLS sertifikası 16.12.2026'da bitiyor. Dependabot #5 (minimatch, orta) 04.11'den beri açık.
  - K4 ve K5 (a) bu raporda PR ve CI çalışma listelerinden hesaplandı: 78/87 ve 0.

### 1.4 Seviye kuralı

- **Seviye 2:** PA 1.1 = F, PA 2.1 ≥ L ve PA 2.2 ≥ L.
- **Seviye 1:** PA 1.1 ≥ L.
- Aksi hâlde **Seviye 0.**

---

## 2. Yönetici özeti

- **15 sürecin hepsi Seviye 1'de kaldı.** Dokuz süreç ilerledi, dördü geriledi (MAN.1, MAN.4, MAN.5, TEC.10), ikisi yerinde. Seviye 2'de süreç yok; en yakını TEC.3 (L+ / L+ / L+).
- **13.10 iş planı kapandı, ama etkisi karışık.** 13 işin 13'ü ortalama bir günde kapandı (K2 medyan 0,80 gün). Altısı etkili oldu (#208, #209, #211, #213, #215, #217). Altısında kök neden kaldı (#207, #210, #212, #214, #216, #218): sır kopyası yeniden oluştu, ADR'ler üçüncü kez bayatladı, içerik bayatlığı tarih denetiminin dışında kaldı. #219 T1 kabulünde sınanacak.
- **Engel aynı: sürdürmek.**
  - Düzeltmeler maddeye bağlı kalıyor; kök neden ikinci modülde yeniden çıkıyor.
  - Otomatik denetimler biçimi sınıyor, anlamı sınamıyor: onay satırı var mı (ama onaylayan İK mı), boyut gerekçesi var mı (ama şablonun kendi metni mi).
  - Yönetim çıktıları (plan, risk, durum raporu) dönem içinde değil, denetimde güncelleniyor.
- **Bir risk gerçekleşti:** PostgreSQL 14 desteği 12.11.2026'da bitti (R-09); Üst Yönetim kararı yok.
- **T1 kabule hazır değil:** kabul aşamasının işleri planlanmadı (#388).
- **Yeni düzeltici iş planı: 10 iş** (#386–#395), §6. Bulguların bir kısmı bu PR'da giderildi (§4 sonu).

---

## 3. Derecelendirme tablosu

| Süreç | 13.10 PA 1.1 / 2.1 / 2.2 → Sv | 12.11 PA 1.1 / 2.1 / 2.2 → Sv | Eğilim | Ek |
|---|---|---|---|---|
| MAN.1 Proje planlama | L+ / L+ / L → 1 | L / L / L → **1** | ↓ | A |
| MAN.2 Proje değerlendirme ve kontrol | L / L− / L− → 1 | L / L / L → **1** | ↑ | A |
| MAN.4 Risk yönetimi | L+ / L+ / L → 1 | L / L / L → **1** | ↓ | A |
| MAN.5 Konfigürasyon yönetimi | L+ / L / L → 1 | L / L / L− → **1** | ↓ | A |
| MAN.6 Bilgi yönetimi | L− / L− / L− → 1 | L / L / L → **1** | ↑ | A |
| MAN.8 Kalite güvence | L+ / L / L → 1 | L+ / L+ / L → **1** | ↑ | B |
| TEC.2 Paydaş ihtiyaç ve gereksinimleri | L+ / L / L → 1 | L+ / L / L+ → **1** | ↑ | B |
| TEC.3 Sistem/yazılım gereksinimleri | L+ / L / L → 1 | L+ / L+ / L+ → **1** | ↑ | B |
| TEC.5 Tasarım tanımlama | L / L− / L− → 1 | L+ / L / L− → **1** | ↑ | B |
| TEC.7 Gerçekleştirme | L+ / L / L → 1 | L+ / L / L → **1** | = | B |
| TEC.8 Entegrasyon | L+ / L / L → 1 | L+ / L / L → **1** | = | C |
| TEC.9 Doğrulama | L+ / L / L → 1 | L+ / L+ / L → **1** | ↑ | C |
| TEC.10 Geçiş | L+ / L+ / L → 1 | L+ / L / L → **1** | ↓ | C |
| TEC.11 Geçerleme | L+ / L / L− → 1 | L+ / L / L → **1** | ↑ | C |
| TEC.13 Bakım | L− / P+ / L− → 1 | L / L− / L− → **1** | ↑ | C |

**Gerileyenlerin ortak nedeni:** T1, T3'te kurulan düzene yeni sırlar (anahtar yuvaları, çalışma rolü), yeni betikler ve etiketsiz UAT kurulumları ekledi. Düzen bunları kendiliğinden kapsamadı. Plan ve risk defteri dönem içinde güncellenmedi.

**Seviye 2 için en yakın:** TEC.3, MAN.8, TEC.9. PA 1.1'i F'ye çıkarmak için yine **süreklilik** gerekiyor: aynı çıktının sonraki modülde kendiliğinden ve güncel üretilmesi.

---

## 4. Bulgular

Bulguların ayrıntısı eklerdedir. Burada önem sırasıyla birleştirilmiş hâlleri var; her biri §6'daki bir işe bağlıdır.

### M-01 — UAT'de yeniden kayıt dışı sır kopyası; bekleyen yeniden başlatma ve TLS · **Yüksek**
`/opt/dsg-hrms/secrets/.env.uat.bak-20261103` ne bilgi kayıt defterinde ne konfigürasyon öğelerinde var; #207'nin kapattığı açık 03.11'de yeniden oluştu. Çekirdek güncellemesi 03.11'den beri yeniden başlatma bekliyor. TLS sertifikası 16.12.2026'da bitiyor; yenileme penceresi 16.11'de açılıyor. *(Ek A A-01, A-11; Ek C YB-13; R-30, R-18)* → **#386**

### M-02 — Paydaş gereksinimi değişikliklerinde İK onayı eksik · **Yüksek**
Dokuz talebin dokuzunda tarihli onay satırı birleştirmeden önce var; CI bunu denetliyor. Ama #333 ve #355 yalnızca Bilgi İşlem onaylı; #284'te İK onayı birleştirmeden 6 dakika sonra yazıldı; #321 bir İK kararını (`KR-126` (4)) Bilgi İşlem onayıyla tersine çevirdi. RACI'de TEC.2'nin onaylayanı İK'dır. *(Ek B TEC.2-7)* → **#387**

### M-03 — T1 kabul aşaması planlanmadı · **Yüksek**
Açık tek T1 işi bu denetimdi. Doğrulama raporu ve G2, entegrasyon raporu, kabul planı, İK ön denemesi için issue yok. Proje planı §5.2 hâlâ "T1 gereksinim toplantısı bekliyor" diyor. Kan grubu 09.11'den beri UAT'de; görme izni olan mevcut rol üyelerinin gizlilik taahhüdü (SYG-PERS-065) depoda kayıtlı değil. SYG-PERS-056 bugün sınanabilir ama sınanmıyor. *(Ek A A-06; Ek C YB-01, YB-02, YB-11, YB-12)* → **#388**

### M-04 — ADR'ler üçüncü kez kararların gerisinde · **Yüksek**
ADR dizini 24.10'dan beri güncellenmedi. ADR-0022 §3 `KR-132`'yi, ADR-0022 §4 ve ADR-0001 `KR-131`'i yansıtmıyor. `KR-130` ve `KR-131` için "neden yeni ADR değil" sınıflandırması yok. SYG-PERS §2.2/§2.5 tahsisi koddan ayrılmış. *(Ek B TEC.5-9, TEC.5-10, TEC.5-11)* → **#389**

### M-05 — Yönetim çıktıları denetime kadar bayat kaldı · **Yüksek** — *bu PR'da giderildi*
R-09 bu ay gerçekleşti ama defterde "kesinleşiyor" diyordu; R-24, R-30, R-31 güncellenmemişti. Durum raporu 13.10'dan beri yoktu (`KR-101` sınırı 13.11); K1–K6 dönemde ölçülmedi; durum raporu 2'de rol/kaynak yeterliliği ve iletim satırı yoktu. *(Ek A A-02, A-07, A-09; Ek B MAN.8-15)* → **bu PR:** durum raporu 3, risk defteri 1.16, K değerleri

### M-06 — CI ve şablon denetimleri biçimi sınıyor, anlamı sınamıyor · **Orta**
#320–#349 arasındaki 17 PR'da tamamlanma tanımı 8–16 maddeye indi (şablonda 26). Boyut denetimi şablonun kendi metniyle geçiyor; #364 üretilenler hariç 1.713 satırdı. 1.500 satırı aşan 7 PR'ın onayında neyin incelendiği yazılı değil. Hata kayıtlarında "neden yakalanmadı" yanıtsız. *(Ek B MAN.8-12, MAN.8-13, MAN.8-14, MAN.8-16)* → **#390**

### M-07 — Dağıtım betiği ve ortam dosyaları yeni yapılandırmayı kapsamıyor · **Orta**
`.env.uat.example`'da compose'un zorunlu tuttuğu `HRMS_APP_DB_PASSWORD` ve anahtar yuvaları yok. Betik `secrets/` dizinini, commit'in `main`'de olduğunu ve canlı arayüz denetimlerini kaydetmiyor; A6 kısa kontrolü etiketsiz kurulumlarda yapılmadı. Geçici yığınlar `-v` olmadan kapatıldığı için birim kaldı (UAT'deki bu PR'da silindi; yerelde 66). *(Ek A A-03, A-04, A-05, A-12; Ek C YB-04, YB-10)* → **#391**

### M-08 — İçerik bayatlığı tarih denetiminin dışında · **Orta**
Konfigürasyon öğeleri listesi 14.10'dan beri aynı; bilgi kayıt defterinde birkaç küme eksik; altı YAKLAŞIM tablosu, PG-PERS §2 sayımı, SYG-PERS başlığı bayat; KPÖ setinin toplantı taslağından ayrılması ve AN'lerin paydaşa dönülmesi yazılı değil. *(Ek A A-08, A-10, A-13, A-14; Ek B TEC.2-8, TEC.3-9, TEC.3-10, MAN.8-17; Ek C YB-06)* → **#392**

### M-09 — Bakım eğilim verisi ve Dependabot kuralı yok · **Orta**
`TEC.13-bakim/raporlar/` boş (T3 YB-10'un tekrarı). Dependabot #5 kayıtsız. Dört hata kaydında `surec:TEC.13`, altı issue'da öncelik etiketi yoktu (bu PR'da düzeltildi). *(Ek C YB-07, YB-08, YB-09; Ek A A-15)* → **#393**

### M-10 — UAT için geri dönüş yordamı yok · **Orta**
T1 kurulumu şema, rol ve şifreli veri getirdi; geri dönüş yolu yazılı değil. 13.10 önerisi issue'ya dönüşmemişti. *(Ek C YB-03)* → **#394**

### M-11 — Geri yön kapsamı dar; hatalar onaydan sonra bulunuyor · **Orta**
Geri yön kapsam testi yalnızca parametre ve LOGO alanı için var. T1'de 5 ürün hatası birleştikten sonra bulundu (K6 = 5); #306 arama anahtarlarının ≈%69'unu etkiliyordu. *(Ek B TEC.7-11, TEC.7-12)* → **#395**

### M-12 — İmza ertelemesi tek taraflı karar · **Gözlem**
`KR-107` (imzaların proje sonuna ertelenmesi) kanıtı üreten birim tarafından alındı. Bütünlük riski özetle giderildiği için TEC.11 düşürülmedi. *(Ek C YB-14)* → durum raporu 3 §6: Üst Yönetim teyidi istendi

**Bu PR'da yapılan düzeltmeler:**
- UAT'deki sentetik birim silindi; performans raporu 0.3 ile düzeltildi (A-05).
- Dağıtım kaydı tablosu birleştirildi; notlar tablonun üstüne alındı ve `e488898` notu eklendi (A-16, YB-04, YB-05).
- Altı issue'ya öncelik, dört hata kaydına `surec:TEC.13` etiketi (A-15, YB-08).
- Risk defteri 1.16, durum raporu 3, öz değerlendirme tablosu 0.5 (M-05).

---

## 5. 13.10 bulgularının durumu

| Bulgu | İş | Durum | Kanıt |
|---|---|---|---|
| M-01 UAT sır kopyası, gereksiz servisler | #207 | ⚠️ Kısmen: Avahi/CUPS maskeli, dışarıya yalnızca 22/80/443; ama yeni sır kopyası ve yeni bekleyen yeniden başlatma | → M-01, #386 |
| M-02 Otomatik kontroller | #208 | ✅ Etkili: 6 `main` kırmızısının 6'sı otomatik kayıtlı; sağlık taraması 93/93 | Ek A, Ek C |
| M-03 Doğrulama kanıtı kabul edilen sürümde | #209 | ✅ T3 için; T1'de henüz sınanmadı | → M-03 |
| M-04 Değişiklik talebi yordamı | #210 | ⚠️ Biçim etkili (9/9); amaç kısmen (İK onayı) | → M-02, #387 |
| M-05 Geri yönde kapsam | #211 | ✅ Etkili; izin ve uç noktaya genişlemedi | → M-11, #395 |
| M-06 Kabul kanıtı | #213 | ✅ Özet doğrulandı, kopya depoda; imza `KR-107` ile ertelendi | → M-12 |
| M-07 ADR güncelliği | #212 | ⚠️ Üç maddesi kapandı; kök neden kaldı | → M-04, #389 |
| M-08 Belge tutarlılığı, bilgi defteri | #214, #215 | ⚠️ #215 etkili; #214 tarihleri kapsıyor, içeriği kapsamıyor | → M-08, #392 |
| M-09 Issue/PR disiplini | #216 | ⚠️ Etiket (77/77, 89/89) ve atlanamaz maddeler (89/89) etkili; boyut ve DoD kırpılması değil | → M-06, #390 |
| M-10 Önerilerin izlenmesi, KG ölçütleri | #217 | ✅ 28/28 öneri takipte; K1–K6 tanımlı ama dönemde ölçülmedi | → M-05 |
| M-11 Sürüm kontrol listesi | #218 | ⚠️ Liste var; T1'de aday çıkmadığı için sınanmadı; etiketsiz kurulumlar kapsam dışı | → M-07, #391 |
| M-12 Kabul süreci | #219 | ⏳ Belgede tamam; T1 kabulünde sınanacak | → M-03, #388 |

**T3 alınan dersleri:** "İK UAT denemesi ve Bilgi İşlem provası ilk kabul adayından önce" ve "sürüm kontrol listesi" T1 kabulünde uygulanacak; bugün için plan yok (#388).

---

## 6. Düzeltici iş planı

| # | İş | Bulgu | Öncelik | Issue |
|---|---|---|---|---|
| 1 | UAT: sır kopyası, yeniden başlatma, TLS yenilemesi | M-01 | Yüksek | #386 |
| 2 | Değişiklik taleplerinde İK onayı; geriye dönük teyit | M-02 | Yüksek | #387 |
| 3 | T1 kabul aşamasının planlanması; SYG-PERS-056 ve 065 | M-03 | Yüksek | #388 |
| 4 | ADR güncelliği ve CI denetimi | M-04 | Yüksek | #389 |
| 5 | PR şablonu ve CI denetimleri | M-06 | Orta | #390 |
| 6 | Dağıtım betiği ve ortam dosyaları | M-07 | Orta | #391 |
| 7 | Belge içerik tutarlılığı | M-08 | Orta | #392 |
| 8 | Bakım eğilim raporu, Dependabot kuralı | M-09 | Orta | #393 |
| 9 | UAT geri dönüş yordamı | M-10 | Orta | #394 |
| 10 | Geri yön kapsamı, kaçan hatalar | M-11 | Orta | #395 |

**Sıra önerisi:**
- #386 TLS bitişinden (16.12) önce.
- #387, #388, #389 T1'in ilk kabul adayından önce.
- #391 ve #394 bir sonraki UAT kurulumundan önce.
- Kalanlar sürekli iyileştirme.

---

## 7. Güçlü yönler

- **İzlenebilirlik:** 79 SYG'nin 77'si testle veya ölçümle kanıtlı; CI her PR'da denetliyor. Türetilmiş davranışlar 29 AN ile kayıtlı.
- **Disiplin:** 89/89 PR'da onay yorumu birleştirmeden önce; etiket ve atlanamaz maddeler 89/89; kırmızıyla birleştirme 0 (K5).
- **Otomatik kontroller artık çalışıyor:** altı `main` kırmızısının altısı otomatik kayda girdi (#208).
- **UAT kurulumu runbook'a göre yapıldı:** kopya alındı, betik doğrulamaları geçti, dağıtım kaydı sunucuyla 7/7.
- **Ölçüm:** beş performans göstergesi gerçek donanımda ölçüldü; yük testi gerçek kişilerin erişim kaydına iz bırakmadan sentetik yığında yapıldı.
- **Değişiklik talepleri kayıtlı:** dokuzunun dokuzu issue, etki analizi ve tarihli onayla.

---

## 8. Sonraki denetim

T1 kabul edildiğinde (modül sonu). O denetimde ilk bakılacaklar:
- §6'daki işlerin durumu ve etkinliği: kök neden mi giderildi, madde mi kapandı?
- T1 kabul kanıtlarının kabul edilen adayı kapsaması (#209'un T1'de sınanması);
- dönem içinde güncellenen yönetim çıktıları: plan, risk defteri, durum raporu 4;
- Seviye 2'ye en yakın üç süreçte (TEC.3, MAN.8, TEC.9) çıktıların sürekliliği.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-11-12 | 1.0 | İlk sürüm (#385) | Bilgi İşlem |
