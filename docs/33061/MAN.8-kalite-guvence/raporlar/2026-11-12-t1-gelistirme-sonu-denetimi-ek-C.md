# Bulgular C — TEC.8, TEC.9, TEC.10, TEC.11, TEC.13 (T1 geliştirme sonu)

**Değerlendirme tarihi:** 2026-11-12
**Dönem:** T3 kabulünden (13.10.2026, `v0.2.0` = `149f6ee`, `main` @ `d354e94`) bugüne (`main` @ `388da21`). 89 commit, 90 birleşen PR (#204–#384). T1 Personel Yönetimi geliştirildi ve 09.11.2026'da UAT'ye kuruldu (`v0.2.0-89-g6dbe9d5`, #381). T1 henüz kabule sunulmadı; kabul adayı etiketi yok.
**Dayanak:** TS ISO/IEC TS 33061 Madde 5.5.9–5.5.12 ve 5.5.14 (çıktılar, BP'ler, bilgi öğeleri) · TS ISO/IEC 33020 Madde 5.2.4 (PA 1.1, PA 2.1 a–g, PA 2.2 a–e) ve 5.3 (ölçek)
**Yöntem:** Salt okuma. Depo, GitHub issue/PR/Actions kayıtları ve Dependabot uyarıları (`gh`). UAT sunucusu salt okunur denetlendi (12.11.2026 20:16 UTC: `deployments.log`, `VERSION`, konteyner OCI etiketleri, `/health/sync`, `/health/notifications`, TLS bitişi). LOGO'ya bağlanılmadı; sır dosyası ve kişisel veri okunmadı. "Planlandı / yapılacak" kanıt sayılmadı. Ölçek: N/P/L/F (+/− yalnızca bant içi konumu gösterir).
**Bilinen durumlar:** (1) T1 kabul aşamasına geçmedi. Bu yüzden modül sonu çıktıları (T1 doğrulama raporu ve G2, entegrasyon raporu, kabul planı ve oturumu, CI kanıt özeti) **bulgu değil "bekliyor"** sayıldı. Derece, dönemde fiilen yürütülen uygulamalara ve sürecin son tam işletimine (T3) dayanır. Bekleyen çıktıların **planlanmamış** olması ise bulgudur (YB-01). (2) İmzalar `KR-107` gereği proje sonunda toplanacak; bu, 13.10 Ek C'nin TEC.11 (g) için koyduğu koşulu değiştirdi (§4.1). (3) Bu ek kişisel veri içermez. Kişiler rol adıyla anılır. (4) Bu ekteki YB numaraları yenidir; 13.10 bulgularına "13.10 YB-xx" diye atıf yapılır.

---

## 0. Özet tablo

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | 13.10 (1.1/2.1/2.2 → Seviye) | Ana engel (Seviye 2 için) |
|---|---|---|---|---|---|---|
| TEC.8 Entegrasyon | **L+** | **L** | **L** | 1 | L+ / L / L → 1 | Canlı arayüz denetimleri hâlâ kayda girmiyor; T1 entegrasyon raporu planlanmadı |
| TEC.9 Doğrulama | **L+** | **L+** | **L** | 1 | L+ / L / L → 1 | T1 doğrulama raporu ve G2 bekliyor, planı yok; 77/79 SYG; yük testi betikleri depoda yok |
| TEC.10 Geçiş | **L+** | **L** | **L** | 1 | L+ / L+ / L → 1 | Geri dönüş yordamı yok; `main` dışı commit UAT'de çalıştı; SYG-PERS-065 ön koşulu kayıtsız |
| TEC.11 Geçerleme | **L+** | **L** | **L** | 1 | L+ / L / L− → 1 | T1 kabul planı, takvimi ve katılımcıları yok; T3 kabulü imzasız (`KR-107`) |
| TEC.13 Bakım | **L** | **L−** | **L−** | 1 | L− / P+ / L− → 1 | Bakım/eğilim raporu yok; YAKLAŞIM 05.10'dan beri bayat; etiket kuralı 4/17 ihlal |

> **Genel tablo:** T3 kapanışının beş sürece düşen düzeltici işleri (#208, #209, #213,
> #218, #219) kapandı ve belgede uygulandı. En **etkili** olan #208: dönemdeki altı `main`
> kırmızı olayının altısı da otomatik kayda girdi ve nedeniyle kapandı. TEC.13'ün PA 1.1'i
> L−'den L'ye, TEC.11'in PA 2.2'si L−'den L'ye çıktı. **Seviye 2'ye ulaşan süreç yok.**
> Yeni ortak örüntü şu: 13.10 Ek C'deki **Orta/Düşük** önerilerin #207–#219'a alınmayanları
> (geri dönüş yordamı, dağıtım sonrası canlı denetim kaydı, bakım raporu, 2FA uçtan uca
> senaryosu) bir ay boyunca hiç ele alınmadı. Ayrıca T1'in kabul aşaması için **hiçbir
> issue açık değil**; T3'teki "kayıtlar kabul edilen sürüme kadar sürdürülmedi" dersine
> karşı önlem henüz planda yok.

---

## 1. TEC.8 — Entegrasyon

### 1.1 Çıktılar

| # | Çıktı (kısa) | Kanıt (13.10 sonrası) | 13.10 | 12.11 |
|---|---|---|---|---|
| a | Entegrasyon kısıtları | YAKLAŞIM §2 (1.1, 14.10). LOGO satırı T1 genişlemesini yansıtmıyor: SYG-PERS §2.3 "T1 beş tablo ekler" diyor; YAKLAŞIM'ın kaynağı yalnız SYG-KMLK §2.3. Fotoğraf okumasının LOGO maliyeti (KPÖ-PERS-4) ve şifreli kan grubu yeni kısıtlar | F | **L+** |
| b | Yaklaşım ve kontrol noktaları | YAKLAŞIM §3 dört kontrol noktası değişmeden uygulanıyor. §3.5 "rapor kabul edilen sürümü kapsar" kuralı eklendi (#214) | F | **F** |
| c | Destekleyici sistemler | Uçtan uca yığın T1 için genişledi: `personnel.spec.ts` 15 senaryo; son `main` CI'ında 22/22 (7 kimlik + 15 personel). #319 ile e2e yığınına veri anahtarı eklendi | F | **F** |
| d | Sistem entegre edildi | UAT'de T1: `VERSION` = `v0.2.0-89-g6dbe9d5`; API ve web OCI `revision` = `6dbe9d5…`; üç konteyner `healthy` (12.11, sunucuda) | F | **F** |
| e | Öğeler arası arayüzler sınandı | Son `main` CI (43297135920): entegrasyon testleri 449, mimari 111, ön yüz 382, uçtan uca 22; API sözleşme denetimi 90/90 yeşil | F | **F** |
| f | Dış çevreyle arayüzler sınandı | **LOGO:** T1 senkronizasyonu kurulumdan 10.11 akşamına 89 çalışma, hepsi "uyarılarla tamamlandı" (veri kalitesi); LOGO sorgu maliyeti ölçüldü (0,36 sn; performans raporu §5). **Bugün:** `/health/sync` ve `/health/notifications` `Healthy`. **Zayıflık:** 09.11 kurulumunun kaydında yalnız betik öz denetimleri var; YAKLAŞIM §3.4'teki canlı denetimlerin sonucu yazılmadı. 13.10'daki "betik sağlık sonucunu kaydetsin" önerisi issue'ya dönüşmedi (YB-10) | L+ | **L+** |
| g | Sonuçlar ve anomaliler | T3 raporu 1.2 ile `v0.2.0`'a bağlandı (#214; 13.10 YB-07 kapandı). T1 entegrasyon raporu **bekliyor** (§3.5: kabul edilen sürümde). Dönem anomalileri issue'da: #315 (fotoğraf kaybı, EXIF), #319 (e2e yığını). Ayrı bir entegrasyon kaydına toplanmadı | L | **L** |
| h | Entegre öğelerin izlenebilirliği | Commit → OCI `revision` → dağıtım kaydı zinciri tutuyor. **Ama** 21.10–09.11 arasında UAT'de çalışan `e488898` `main`'in atası değil (PR #258'in dal commit'i; YB-04) | L+ | **L+** |

**PA 1.1 = L+** (13.10: L+). Altı çıktı F. F'yi engelleyen: canlı arayüz denetiminin kaydı ve LOGO kısıtlarının güncellenmemesi.

### 1.2 Temel uygulamalar
- **BP1 Hazırlık:** Strateji ve kontrol noktaları yazılı. BP1.5 (kısıtların gereksinime işlenmesi) SYG-PERS'te var, YAKLAŞIM'a yansımadı.
- **BP2 Entegrasyon:** Her PR'da sürekli. T1'de LOGO'dan bağımsızlık testi eklendi (SYG-PERS-001, #379).
- **BP3 Sonuçlar:** Baseline yok (T1 kabul edilmedi). Anomaliler issue'da, rapor bekliyor.

### 1.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** "Modül kapanışında rapor" hedefi yazılı. T1 için iş açılmadı (YB-01).
- **2.1(c)** Dağıtım sonrası canlı denetim hâlâ elle ve kayıtsız. 13.10'da Orta öneriydi; bir ay boyunca ele alınmadı.
- **2.1(e)** UAT kurulumu bir yazılım ajanınca yapıldı ("Claude (Doğuş Uçanok onayıyla)"). Onay kaydı #381'de; rol tanımı (YAKLAŞIM §4) bunu öngörmüyor. Düşük önem.
- **2.2(c)** YAKLAŞIM §2 LOGO satırı ve kaynak listesi T1'e göre güncellenmedi (YB-06).
- **2.2(d)** İnceleme kanıtı yine `KR-096` onay yorumu; bağımsızlık sınırı sürüyor.

### 1.4 13.10'a göre değişim

| 13.10 bulgusu / işi | Durum |
|---|---|
| YB-07 entegrasyon raporu `rc.2`'de | ✅ #214 → rapor 1.2 (`v0.2.0`) |
| `deploy-uat.sh` sağlık sonucunu kaydetsin (Orta) | ❌ Issue açılmadı; betikte değişiklik yok (YB-10) |
| Uçtan uca 2FA senaryosu (Düşük) | ❌ `identity.spec.ts` değişmedi |

---

## 2. TEC.9 — Doğrulama

### 2.1 Çıktılar

| # | Çıktı (kısa) | Kanıt (13.10 sonrası) | 13.10 | 12.11 |
|---|---|---|---|---|
| a | Doğrulama kısıtları | YAKLAŞIM 1.3 (14.10): §2.2 madde 4 "G2'den sonraki değişiklik rapora satır olur; G2 yeniden kaydedilir" (#209) | F | **F** |
| b | Destekleyici sistemler | Uçtan uca 22 senaryo; UAT'de ayrı sentetik yük yığını. **Ancak** yük betikleri (`personnel-load.mjs`, `seed-gen.mjs`) depoya alınmadı (rapor §6); ölçüm yalnız tarifle yeniden üretilebilir (YB-11) | F | **L+** |
| c | Sistem doğrulandı | İzlenebilirlik denetimi: **77/79** SYG-PERS kanıtlı (uyarı kipi, #272). Kalan: 056 (Test; matriste T2'ye bağlı) ve 065 (İnceleme; kabulde). KPÖ-PERS-1…5 UAT'de ölçüldü, beşi de karşılanıyor (`2026-11-10-t1-performans-olcumu.md`). T1 doğrulama raporu ve G2 **bekliyor** | L | **L+** |
| d | Düzeltici faaliyet verisi | Kaçaklar kök nedenle issue'da (#306, #315, #338, #357, #373). Kararsız test #242/#295 kök nedenle kapandı. Örneklenen 7 hata düzeltme PR'ının 6'sında test dosyası değişti (#319'unki altyapı betiği) | L+ | **L+** |
| e | Nesnel kanıt | CI çalışmaları, performans raporu, matris 2.65. T1 için kalıcı CI kanıt özeti **bekliyor** (yalnız kabul adayı etiketinde gerekir; etiket yok) | L+ | **L+** |
| f | Sonuçlar ve anomaliler | `ci-evidence-summary.mjs` artık uçtan uca sonucu da yazıyor; rc.1 özeti üretildi (#218, 15.10). T1 kaçakları issue'da | L+ | **L+** |
| g | İzlenebilirlik | CI geri yön denetimi (#211) ve SYG → test denetimi işliyor. SYG-PERS-056 bugün sınanabilir ("süzgeç yok") ama T2'ye bırakıldı; baseline'da denetim hata verecek (YB-12) | L+ | **L+** |

**PA 1.1 = L+** (13.10: L+). F için T1 doğrulama raporu, G2 ve 79/79 gerekir.

**CI ölçeğinin değişimi** (`v0.2.0` → `388da21`): backend testleri 796 → 1.253; ön yüz 247 → 382; uçtan uca 7 → 22.

### 2.2 Temel uygulamalar
- **BP1.3–1.4:** Analiz yöntemi T1'de iyi uygulandı: gerçek kayıtlara iz bırakmamak için ayrı sentetik yığın, en kötü durum verisi, sınırlar yazılı.
- **BP2:** Yük ölçümü ve iş günü senkronizasyon ölçümü ayrı ayrı. KPÖ-PERS-4 için LOGO'da tek sorgu, salt okunur oturumla ve onayla.
- **BP3.3 Paydaş mutabakatı (G2):** T1 için bekliyor. #209 kuralı T3'e geriye dönük uygulandı (rapor 2.0); T1'de henüz sınanmadı.
- **BP3.5 Baseline:** Yok (bekliyor).

### 2.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** G2 ölçütü yazılı. T1 G2'si için tarih ve iş yok (YB-01).
- **2.1(c)** #272 ile izlenebilirlik denetimi geliştirme süresince uyarı veriyor; kalan iki SYG görünür ✔.
- **2.2(c)** Performans raporu 0.2 (taslak numarası). Kabulden önce 1.0'a bağlanmalı.
- **2.2(e)** Yük betikleri saklanmadı (YB-11).

### 2.4 13.10'a göre değişim

| 13.10 bulgusu / işi | Durum |
|---|---|
| YB-01 doğrulama raporu `v0.2.0`'ı kapsamıyor | ✅ #209 → rapor 2.0, G2 yeniden kaydı, YAKLAŞIM §2.2(4) |
| YB-02 SYG-076 regresyon testi | ✅ #211 (geri yön kapsam testi) |
| YB-03 CI özetinde uçtan uca yok; rc.1 özeti yok | ✅ #218 (`uctan-uca-sonucu`; `2026-10-15-v0.2.0-rc.1-ci-kanit-ozeti.md`) |
| YB-13 SYG-077 yeniden ölçümü | ✅ #209 (T3 performans raporu güncellendi) |

---

## 3. TEC.10 — Geçiş

### 3.1 Çıktılar

| # | Çıktı (kısa) | Kanıt (13.10 sonrası) | 13.10 | 12.11 |
|---|---|---|---|---|
| a | Geçiş kısıtları | `KR-110` (denetim sıklığı), `KR-118` (`hrms_app` rolü), `KR-122` (veri anahtarı) runbook'a işlendi | F | **F** |
| b | Destekleyici sistemler | Sır dosyasına veri anahtarı ve `hrms_app` parolası; #207 ile sunucu temizliği. **TLS bitişi 16.12.2026** (bugün 34 gün); yenileme elle (R-18), iş açık değil (YB-13) | L+ | **L+** |
| c | Saha hazırlandı | Runbook 1.11–1.14: §2.2.1 ve §3.3a çalışma rolü, §5.1 anahtar döndürme, §11.2 İK rolü betiği (#383) | F | **F** |
| d | Kurulu sistem işlevlerini sunabiliyor | 09.11 kurulumu: betik öz denetimleri geçti, uygulama kısıtlı rolle bağlı. 12.11'de üç konteyner `healthy`, iki sağlık ucu `Healthy` | F | **F** |
| e | Operatör ve kullanıcı hazırlığı | Operatör: runbook. İK hesap ve rol hazırlığı #383. T1 kullanım notu **bekliyor** (kabul öncesi) | L− | **L** |
| f | Sonuçlar ve anomaliler | Dağıtım kaydı 7 satır, **sunucu `deployments.log` ile birebir aynı**. T1 kurulumunun ön yedeği, silinmesi ve ilk senkronizasyonu performans raporu §2 ve §6'da. **Biçim kusuru:** 13.10 notu tablonun ortasına girdi; sonraki iki satır başlıksız kaldı (YB-05) | L | **L+** |
| g | Etkin ve işletime hazır | UAT için evet | L+ | **L+** |
| h | İzlenebilirlik | 09.11 satırı `main` commit'i. **Ama** 21.10 satırı `e488898`: PR #258'in dal commit'i, `main`'de yok, hiçbir dal veya etiket göstermiyor. Kod içeriği `f312510` ile aynı (`src`, `docker` farkı boş); fark yalnız kayıt satırı. UAT bu commit'le 19 gün çalıştı (YB-04) | L+ | **L** |

**PA 1.1 = L+** (13.10: L+).

### 3.2 Temel uygulamalar
- **BP1.1 Sürüm stratejisi:** `KR-097` ve sürüm kontrol listesi (#218). Ara kurulumlar etiketsiz; runbook §3.0 buna izin veriyor.
- **BP1.4 Geçiş bilgisi:** T1 kurulumu mesai dışında, ön yedekle ve gerekçesiyle yapıldı ✔ (örnek uygulama).
- **BP2.5 Etkinleştirme:** Betik `revision`, rol ve port denetimi yapıyor ✔. `main`'in atası olma koşulunu denetlemiyor.
- **Geri dönüş yordamı:** Hâlâ yok (YAKLAŞIM §8). T1 kurulumu şema, yeni rol ve şifreli veri getirdi; geri dönüş yine sınanmadı (YB-03).

### 3.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(b)** **SYG-PERS-065 ön koşulu:** SYG-PERS §7 "gizlilik taahhüdü kayıtları — UAT'de kan grubu açılmadan önce" diyor. UAT'de kan grubu 09.11'den beri okunuyor (153 kişi). Sistem Yöneticisi rolü `view_sensitive` iznini taşıyor (izin listesi "Tümü"). Mevcut rol üyelerinin taahhüt kaydına dair depoda iz yok. #383 yalnız **yeni** İK atamalarını `--taahhut-alindi` onayına bağladı (YB-02).
- **2.1(c)** Depodaki dağıtım kaydı gecikmeli işleniyor: 09.11 satırı 12.11'de (#382). Sunucu kaydı anlık ✔.
- **2.2(c)** TEC.11 YAKLAŞIM §5 "UAT'ye yalnızca etiketten kurulur" diyor; runbook §3.0 ve iki etiketsiz kurulumla çelişiyor (YB-06).

### 3.4 13.10'a göre değişim

| 13.10 bulgusu / işi | Durum |
|---|---|
| YB-11 ADR-0011 §7 | ✅ #212; ADR-0018 (ortamların verisi) |
| YB-12 `v0.2.0` dağıtım kaydında yok | ✅ #218 notu (biçimi tabloyu bozdu, YB-05) |
| Geri dönüş yordamı (Orta) | ❌ Issue açılmadı (YB-03) |
| YAKLAŞIM §8 güncellemesi (Düşük) | ✅ #214 (1.1) |

---

## 4. TEC.11 — Geçerleme

### 4.1 Çıktılar

| # | Çıktı (kısa) | Kanıt (13.10 sonrası) | 13.10 | 12.11 |
|---|---|---|---|---|
| a | Geçerleme kriterleri | T1 için PG-PERS kabul kriterleri var (#239, #241). T1 kabul planı (senaryolar) **bekliyor** | F | **F** |
| b | Hizmetlerin varlığı teyit edildi | T3 kabulü (13.10) dışında yeni geçerleme yok. T1 bekliyor | L+ | **L+** |
| c | Geçerleme kısıtları | YAKLAŞIM 1.4: kanıt gösterimi kuralı, ön deneme (1a), katılımcı seçimi (#219) | F | **F** |
| d | Sistem geçerlendi | T3: değişmedi. T1: bekliyor | L+ | **L+** |
| e | Destekleyici sistemler | UAT T1'le hazır; İK hesapları için rol betiği (#383); BK-22 saklama koşulu onaylı (`KR-109`) | F | **F** |
| f | Sonuçlar ve anomaliler | Kabul formu şablonuna gözlem sütunu eklendi (#219). T1'de henüz uygulanmadı | L | **L** |
| g | Paydaş onayının nesnel kanıtı | Depoda doldurulmuş Word formun üst verisi temizlenmiş kopyası; **SHA-256 doğrulandı** (`f13849…f369`, kayıttaki değerle aynı). Aslının özeti de kayıtlı. İmza sütunu "İmza bekleniyor". Ö-3 kapandı (konum: depo). İmzalar `KR-107` ile proje sonuna | L− | **L** |
| h | İzlenebilirlik | Matris "Kabul" sütunu T3 için 56/56; T1 satırları bekliyor | F | **F** |

**PA 1.1 = L+** (13.10: L+).

**İmza ertelemesi ve 13.10 koşulu:** 13.10 Ek C, "imza toplanmazsa (g) P+'ya düşürülür" demişti. Gerekçe tek kopya ve bütünlük özeti yokluğuydu. Bugün iki risk de giderildi: kopya depoda, özetler kayıtlı ve doğrulanabilir. Bu yüzden (g) düşürülmedi; L'ye çıktı. **Ancak** `KR-107` kararını kanıtı üreten birim tek başına verdi (karar sahibi Bilgi İşlem, #213). Kararın Üst Yönetim veya Kalite Uzmanı tarafından teyidi kayıtlı değil (YB-14).

### 4.2 Temel uygulamalar
- **BP1.1 Strateji:** YAKLAŞIM 1.4. D-01 sırası (G2 → İK ön deneme → prova → ilk aday) yazılı ✔.
- **BP1.3 Amaç, koşul, ölçüt:** T1 için bekliyor.
- **BP3.3 Mutabakat:** T3 imzasız (bilinen durum).

### 4.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** T1 kabul planı, takvim ve katılımcı seçimi için iş yok (YB-01). Plan, İK'ya sunulmadan önce Bilgi İşlem'in ekranda denemesini gerektiriyor (D-03); süre ister.
- **2.1(d)** Katılımcı kuralı, 13.10 önerisindeki "en az biri Bilgi İşlem dışından" yerine "İK seçer" oldu (#219). Gerekçe yazılı; etkisi T1 kabulünde görülecek.
- **2.1(g)** İK'nın UAT'de T1'i serbest denemesi için hesaplar hazırlanıyor (#383; 4 aktif kişinin hesabı yok).
- **2.2(c)** YAKLAŞIM §5 bayat (YB-06).

### 4.4 13.10'a göre değişim

| 13.10 bulgusu / işi | Durum |
|---|---|
| YB-04 kabul kanıtı korunması | ✅ #213: özet, depo kopyası, ifade düzeltmesi. İmza `KR-107` ile ertelendi |
| YB-05 kanıt gösterimi ve katılımcı kuralı | ✅ #219 (YAKLAŞIM §2.1, §3) |
| YB-06 gözlem sütunu | ✅ #219 (şablon) |
| YB-14 `KR-106` ve BK-22 | ✅ `KR-109` (KVKK Sorumlusu onayı) |

---

## 5. TEC.13 — Bakım

### 5.1 Çıktılar

| # | Çıktı (kısa) | Kanıt (13.10 sonrası) | 13.10 | 12.11 |
|---|---|---|---|---|
| a | Bakım kısıtları | YAKLAŞIM 1.0 (05.10) değişmedi; yeni kısıtlar (R-28, R-29, `KR-122` anahtar döndürme) başka belgelerde | L | **L** |
| b | Destekleyici sistemler | `main-failure-check` iptal/zaman aşımını da yakalıyor; `main-health-sweep` 6 saatte bir (93/93 çalıştı); yinelenen kayıt önlendi (#291). Dependabot uyarıları açık ama süreçte yeri yok | L | **L+** |
| c | Onarılmış öğeler | 17 `[HATA]`'nın 17'si kapandı; ürün hatalarının hepsi aynı gün. Örneklenen 7 düzeltme PR'ının 6'sında test. #373 ayrı PR yerine özellik PR'ında (#372) düzeltildi; hata o PR'ın yeni e2e senaryosunda bulunmuştu | L+ | **L+** |
| d | Değişiklik ihtiyacı raporlandı | 17 `[HATA]`, 9 `[DEĞİŞİKLİK]`, 4 düzeltici faaliyet (#244, #252, #272, #291). `main` kırmızı kayıtlarının hepsinde neden, etki, önlem yazılı | L | **L+** |
| e | Arıza ve ömür verisi | Bakım raporu **yine yok** (`raporlar/` boş; 13.10 YB-10 issue'ya dönüşmedi). Ham veri var: R-29'da açık sayımı, issue'larda süreler. Analiz yok | P− | **P** |

**PA 1.1 = L** (13.10: L−).

**Ham veri (13.10–12.11, rapor için hazır):**

| Sınıf | Kayıtlar | Kapanma |
|---|---|---|
| `main` kırmızı / doğrulanmadı | #223, #242 (+#248), #255, #286 (+#290), #305; Dal Koruma yanlış alarmı #253 (+#252) | 32 dk – 13,8 sa |
| — kök neden: yeni yayımlanan açık | #223, #255, #286, #305 (4/6) | — |
| — kök neden: kararsız test | #242 (palindrom kod), #295 | aynı gün |
| Güvenlik açığı (kırmızıdan önce) | #250, #288 | 17 dk – 13,5 sa |
| Ürün hatası (doğrulama kaçağı) | #306, #315, #338, #357 | 0,5 – 2,8 sa |
| Ürün hatası (PR CI'ında yakalandı) | #373 | 55 dk |
| Test altyapısı | #319 | 5,5 sa |

`main` CI: 90 çalışmanın 84'ü başarılı; 6 başarısızlığın 6'sı da kayda girdi. Dependabot: 4 uyarı; 3'ü (vite) bir günde kapandı; **#5 (minimatch, orta, geliştirme bağımlılığı) 04.11'den beri açık ve kayıtsız** (YB-09).

### 5.2 Temel uygulamalar
- **BP2.1 Olay gözden geçirmesi:** Issue bazında ✔. Dönemsel gözden geçirme yok.
- **BP2.5 Önleyici bakım:** CI taramaları ✔; sertifika yenilemesi için hatırlatma yok (YB-13).
- **BP4.1 Kayıt:** ✔ (#208 sonrası boşluk görülmedi).
- **BP4.2 Eğilim:** ✗. "Kod değişmeden kırmızı" örüntüsü R-29'da sayılıyor (altı olay) ama önlem (3) "düzenli bağımlılık güncellemesi değerlendirilecek" hâlâ karar bekliyor.
- **BP4.3 İzlenebilirlik:** Etiket kuralı 17 kaydın 4'ünde ihlal: #295 (`surec:TEC.9`), #306 ve #338 (`surec:TEC.7`), #319 (`surec:TEC.8`). #373'te öncelik etiketi yok (YB-08).

### 5.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** Hedef çözüm süresi hâlâ yok (YAKLAŞIM §7).
- **2.1(c)** İzleme otomatik ve etkili (#208) ✔. Orta önemli açıkların izlenmesi tanımsız.
- **2.2(a)(b)** YAKLAŞIM 1.0 bayat: §2.4 "CI zamanlanmış çalışmaz" ve §7 "zamanlanmış tarama yok" diyor; `main-health-sweep` 14.10'dan beri var. §8 "her modül sonu denetiminde gözden geçirilir" kuralı T3 kapanışında uygulanmadı (YB-06).
- **2.2(e)** Olay kayıtları ✔ (O-5 eklendi). Bakım raporu yok.

### 5.4 13.10'a göre değişim

| 13.10 bulgusu / işi | Durum |
|---|---|
| YB-08 05.10 olayı, izleyiciyi izleyen denetim | ✅ #208: O-5, yeniden deneme, zamanlanmış tarama, R-28 |
| YB-09 `[HATA]` etiket kuralı | ⚠️ #216 `tur:` etiketini denetliyor; `surec:TEC.13` kuralı denetlenmiyor, 4/17 ihlal |
| YB-10 bakım raporu | ❌ Issue açılmadı (YB-07) |

---

## 6. T3 düzeltici işlerinin etkinliği (bu beş süreç)

| İş | Ne yapıldı | Etkinlik |
|---|---|---|
| #208 otomatik kontroller | İptal/zaman aşımı kaydı, 6 saatlik tarama, R-28, O-5 | **Etkili.** 6/6 `main` kırmızısı otomatik kayda girdi; tarama 93/93 çalıştı; yinelenen kayıt kusuru #291 ile giderildi |
| #209 doğrulama raporu kabul edilen sürüm | Rapor 2.0, G2 yeniden kaydı, YAKLAŞIM §2.2(4) | **Belgede etkili, T1'de sınanmadı.** Kural T1 G2'sinde uygulanacak |
| #213 kabul kanıtı | SHA-256, depo kopyası, Ö-3 kapandı, `KR-107` | **Kısmen etkili.** Bütünlük ve kopya sorunu çözüldü; imza proje sonuna ertelendi; karar tek taraflı (YB-14) |
| #218 sürüm kontrol listesi | `surum-kontrol-listesi.md`, `KR-110`, uçtan uca özet, rc.1 özeti, `v0.2.0` notu | **Kısmen etkili.** Araçlar hazır; liste T1'de henüz kullanılmadı. Not dağıtım kaydı tablosunu bozdu (YB-05) |
| #219 kabul iyileştirmeleri | YAKLAŞIM 1.4, şablon gözlem sütunu, `KR-109` | **Belgede tamam, uygulamada bekliyor.** Katılımcı kuralı öneriden zayıf (İK seçer) |

---

## 7. Yeni bulgular (12.11) — önceliklendirilmiş

| No | Bulgu | Süreç / öznitelik | Önem | Önerilen iş | Nerede giderilir |
|---|---|---|---|---|---|
| YB-01 | T1 kabul aşaması planlanmadı: doğrulama raporu ve G2, entegrasyon raporu, kabul planı, kullanım notu, İK ön denemesi için açık issue yok (açık tek T1 işi #385) | TEC.8/9/11 PA 2.1(a) | **Orta** | Sürüm kontrol listesi A1–A11 ve YAKLAŞIM'lara göre T1 kapanış işleri, sırası (D-01) ve hedef tarihi | Ayrı iş (issue'lar) |
| YB-02 | SYG-PERS-065: UAT'de kan grubu 09.11'den beri açık; `view_sensitive` taşıyan mevcut Sistem Yöneticisi üyelerinin gizlilik taahhüdü kaydına dair iz yok | TEC.10 PA 2.1(b); KVKK | **Orta** (kayıt yoksa Yüksek) | Taahhüt kayıtlarının varlığını doğrula; yoksa al ve kaydet; SYG-PERS §7 satırını güncelle | Ayrı iş |
| YB-03 | Geri dönüş yordamı hâlâ yok; 13.10 önerisi issue'ya dönüşmedi; T1 kurulumu şema, rol ve şifreli veri getirdi | TEC.10 BP3; PA 2.1(c) | **Orta** | Runbook'a geri dönüş bölümü; bir kez UAT'de sınanır | Ayrı iş |
| YB-07 | Bakım/eğilim raporu yok (13.10 YB-10 tekrarı); ham veri hazır (§5.1) | TEC.13 e; BP4.2 | **Orta** | `TEC.13-bakim/raporlar/` T3+T1 dönemi raporu; hedef çözüm süreleri | Ayrı iş |
| YB-04 | 21.10–09.11 UAT'de `main`'de olmayan commit (`e488898`) çalıştı; betik ata denetimi yapmıyor | TEC.10 h; MAN.5 | Düşük | Dağıtım kaydına açıklama notu; `deploy-uat.sh`'e "commit `origin/main`'in atası mı" denetimi | Not: bu işte · Betik: ayrı iş |
| YB-05 | Dağıtım kaydında #218 notu tabloyu böldü; 21.10 ve 09.11 satırları başlıksız | TEC.10 f; PA 2.2(c) | Düşük | Notu tablonun altına taşı | Bu işte (belge düzeltmesi) |
| YB-06 | Bayat yaklaşım belgeleri: TEC.13 1.0 (zamanlanmış tarama, Dependabot yok); TEC.11 §5 "yalnızca etiketten"; TEC.8 §2 LOGO satırı ve kaynak (SYG-PERS §2.3 yok) | PA 2.2(c) ×3 | Düşük–Orta | Üç YAKLAŞIM'ın güncellenmesi | Bu işte (belge düzeltmesi) |
| YB-08 | `[HATA]` etiket kuralı 4/17 kayıtta ihlal; #373'te öncelik yok; kural CI'da değil | TEC.13 BP4.3; PA 2.2(b) | Düşük | Etiketleri düzelt; `pr-traceability-check`'e kural ya da YAKLAŞIM'dan çıkar | Etiketler: bu işte · Kural: ayrı iş |
| YB-09 | Dependabot #5 (orta) 04.11'den beri açık, kayıtsız; orta önemli açıkların ele alınışı tanımsız | TEC.13 BP2.5 | Düşük | Kural (YAKLAŞIM §2.4) ve uyarının kapatılması/gerekçesi | Ayrı iş (küçük) |
| YB-10 | Dağıtım sonrası canlı arayüz denetimleri kayda girmiyor (13.10 Orta öneri, issue yok) | TEC.8 f; PA 2.1(c) | Düşük | `deploy-uat.sh` `/health/sync` ve `/health/notifications` sonucunu kayda yazsın | Ayrı iş |
| YB-11 | Yük testi betikleri depoya alınmadı; performans raporu 0.2 | TEC.9 b; PA 2.2(e) | Düşük | Betikleri `tests/perf/` altına al (yalnız sentetik yığına bağlanır) veya gerekçeli karar | Ayrı iş |
| YB-12 | SYG-PERS-056 bugün sınanabilir ama T2'ye bırakıldı; 065 için inceleme kaydı yöntemi yazılı değil; baseline'da denetim hata verir | TEC.9 c, g | Düşük | 056 için negatif test; 065 için doğrulama raporunda inceleme satırı | Ayrı iş (YB-01 içinde) |
| YB-13 | UAT TLS sertifikası 16.12.2026'da bitiyor; elle yenileme; T1 kabul takvimiyle çakışabilir; iş yok | TEC.10 b; R-18 | Düşük | Yenileme için tarihli issue (ör. 01.12) | Ayrı iş |
| YB-14 | `KR-107` (imza ertelemesi) kanıtı üreten birimce tek başına alındı; üst onay kaydı yok | TEC.11 g; MAN.8 bağımsızlık | Gözlem | Üst Yönetim veya Kalite Uzmanı teyidi | Ana raporda karar |

**Seviye 2 için ne gerekiyor?** PA 2.1 ve PA 2.2 beş süreçte de L bandında ya da yakınında. Sınır yine PA 1.1 = F. T1 için bunun yolu belli: YB-01'deki kapanış işleri kabul edilen sürüme kadar sürdürülürse TEC.8, TEC.9 ve TEC.11'in PA 1.1'i F'ye çıkabilir. TEC.10 için YB-03 ve YB-04, TEC.13 için YB-07 ve YB-06 gerekir.

---

## 8. Güçlü yönler (denge için)

1. **İzleyiciyi izleyen denetim çalışıyor.** #208 sonrası altı `main` kırmızısının hepsi kendiliğinden kayda girdi; kök nedenler (yeni açık, kararsız test, API gecikmesi) issue'da yazılı.
2. **Analiz yöntemi örnek nitelikte.** KPÖ-PERS ölçümleri gerçek kişilere iz bırakmayan ayrı yığında, en kötü durum verisiyle ve sınırları yazılarak yapıldı.
3. **UAT kurulumu disiplinli.** Ön yedek, mesai dışı kurulum, kısıtlı veritabanı rolü, kurulumdan sonra yedeğin silinmesi; sunucu kaydı depo kaydıyla birebir.
4. **Uçtan uca kapsam üç kat arttı** (7 → 22) ve telefon genişliği dahil T1 ekranlarını kapsıyor.
5. **Kişisel veriye dokunan işlemler onaya bağlandı:** İK rol betiği varsayılan kipte yalnız rapor verir; atama taahhüt onayı ve denetim izi ister (#383).

---

## Ek — Başlıca kanıt kaynakları

- `docs/33061/TEC.8-entegrasyon/YAKLASIM.md` (1.1), `raporlar/2026-10-05-t3-entegrasyon-raporu.md` (1.2)
- `docs/33061/TEC.9-dogrulama/YAKLASIM.md` (1.3), `raporlar/2026-10-04-t3-dogrulama-raporu.md` (2.0), `raporlar/2026-11-10-t1-performans-olcumu.md` (0.2), `kayitlar/2026-10-15-v0.2.0-rc.1-ci-kanit-ozeti.md`
- `docs/33061/TEC.10-gecis/YAKLASIM.md` (1.1), `surum-kontrol-listesi.md` (1.0), `kayitlar/uat-dagitim-kaydi.md`, `uat-ortami-kurulum-runbook.md` (1.14)
- `docs/33061/TEC.11-gecerleme/YAKLASIM.md` (1.4), `kayitlar/2026-10-13-t3-kabul-formu.md` ve `.docx`
- `docs/33061/TEC.13-bakim/YAKLASIM.md` (1.0), `kayitlar/2026-10-04-t3-olay-kayitlari.md` (1.3)
- `docs/33061/TEC.3-sistem-yazilim-gereksinimleri/gereksinimler/SYG-PERS.md` §2.3, §7, AN-12, AN-18; `docs/mimari/izin-listesi.md`; `docs/karar-kayit-defteri.md` `KR-107`, `KR-109`, `KR-110`, `KR-118`, `KR-122`, `KR-125`, `KR-128`; risk defteri R-18, R-28, R-29; `CHANGELOG.md` `[Yayımlanmamış]` "Güvenlik"
- Issue'lar: #207–#219, #223, #242, #248, #250, #252, #253, #255, #272, #286, #288, #290, #291, #295, #305, #306, #315, #319, #338, #357, #373, #379, #381, #383. PR'lar: #258, #316, #320, #339, #358, #372, #382, #384
- Actions: son `main` CI 43297135920 (`388da21`); dönemdeki başarısız `main` CI'ları 38530336839, 39443078292, 39563560670, 39564223843, 40023480889, 41331436361
- Dependabot uyarıları #1–#3 (vite, kapalı), #5 (minimatch, açık); #4 geri çekilmiş
- UAT (salt okunur, 12.11.2026 20:16 UTC): `deployments.log` 7 satır (depo kaydıyla aynı); `VERSION` `v0.2.0-89-g6dbe9d5 6dbe9d5…`; `dsg-hrms-uat-api` ve `-web` OCI `revision` `6dbe9d5…`, `healthy`; `/health/sync` ve `/health/notifications` `Healthy`; TLS `notAfter` 16.12.2026

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-11-12 | 1.0 | İlk sürüm (#385) | Bilgi İşlem |
