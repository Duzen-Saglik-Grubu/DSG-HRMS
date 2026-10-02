# Bulgular C — TEC.8, TEC.9, TEC.10, TEC.11, TEC.13

**Değerlendirme tarihi:** 2026-10-02
**Dönem:** 18.09.2026 temel değerlendirmesinden (MAN.8-SGR-2026-09-18) bugüne — T3 Kimlik Yönetimi geliştirme çevrimi (#62–#116)
**Dayanak:** TS ISO/IEC TS 33061 Madde 5.5.9–5.5.14 (çıktılar, BP'ler, bilgi öğeleri) · TS ISO/IEC 33020 Madde 5.2.4.2–5.2.4.3 (PA 2.1 a–g, PA 2.2 a–e)
**Yöntem:** Salt okuma. Depo (`main` @ `4199526`), GitHub issue/PR/Actions kayıtları (`gh`), CI günlükleri. "Planlandı / yapılacak / PR notunda söz verildi" kanıt sayılmadı. Ölçek: N/P/L/F (+/− yalnızca bant içi konumu gösterir).
**Bilinen durumlar:** (1) 30.09.2026 `git stash -u` olayı, hiç commit edilmemiş boş TEC.8/9/11/13 klasörlerini sildi; bu süreçlerin klasörü ve `YAKLASIM.md`'si yok — bu, kanıtın **hiç üretilmemiş** olduğu anlamına gelir, kayıp sayılmadı. (2) SYG-KMLK-077/078/079 UAT ölçümleri bugün (02.10) yapıldı ve ayrıca yazılıyor; **devam ediyor** olarak ele alındı, kanıt sayılmadı. (3) T3'ün İK kabulü (TEC.11) yapılmadı.


> **Düzeltme (ana rapor §1.3):** Bu ekte "UAT e-posta kanalı `Unhealthy` (SMTP 587 sertifikası)" diye geçen bulgu **günceli yansıtmıyor**. Sertifika 01.10.2026'da yenilendi ve UAT `/health/notifications` 02.10.2026'da `email: Healthy` döndü. Bulgunun geçerli kalan kısmı şudur: olay için ne issue ne de risk kaydı açıldı; sertifikanın bir sonraki bitişi (11.10.2026) de kayıtlı değil.

---

## 0. Özet tablo

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | 18.09 (1.1/2.1/2.2) | Ana engel |
|---|---|---|---|---|---|---|
| TEC.8 Entegrasyon | **L** | **L−** | **P+** | 1 | L / L / P+ | Yaklaşım ve entegrasyon raporu yok; SMTP arayüz anomalisi sahipsiz; entegre sürüm kimliksiz |
| TEC.9 Doğrulama | **L** | **L** | **P+** | 1 | L+ / L / L− | Doğrulama stratejisi ve raporu yok; gereksinim başına sonuç kaydı yok; SYG-060/061/062 doğrulanmamış ve takipsiz |
| TEC.10 Geçiş | **L−** | **P+** | **L−** | 1 | L− / L / L | Etiketsiz sürüm UAT'ye çıkıyor; dağıtım kaydı yok; runbook içi çelişkiler; kullanıcı eğitimi yok |
| TEC.11 Geçerleme | **P** | **P** | **P** | 0 | N / — / — | Kabul kriterleri var; geçerleme planı, prosedürü, kabul formu şablonu yok |
| TEC.13 Bakım | **P+** | **P** | **P+** | 0 | P− / P / P | Hata kayıtları güçlü; bakım stratejisi, eğilim/maliyet verisi, bakım raporu yok |

> **Derece düşüşleri gerileme değil, ölçütün yükselmesidir.** 18.09'da onaylı
> gereksinim yoktu; "her gereksinim doğrulandı mı?" sorusu sorulamıyordu. Bugün 56 REQ
> ve 79 SYG var; aynı yürütme bu ölçütle daha zayıf görünür. Buna karşılık **iki gerçek
> gerileme** var: ADR-0011 §7 ve proje planı §3 adım 8'in açık hükmüne rağmen
> **etiketsiz sürüm UAT'ye çıkarılması** (TEC.10 PA 2.1) ve ADR-0011'in
> "kapsam raporları her sürümde `TEC.9/raporlar/` altında saklanır" yükümlülüğünün T3
> çevriminde de karşılanmaması (TEC.9 PA 2.2).

---

## 1. TEC.8 — Entegrasyon

### 1.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Entegrasyon kısıtları (arayüzler dâhil) belirlendi | `SYG-KMLK.md` §2.3 dış arayüz tablosu (LOGO/TDS salt-okunur, NetGSM/HTTPS, SMTP+TLS, tarayıcı/HTTPS); ADR-0003, ADR-0012 §7 → `KR-083` gönderim kipleri; `KR-093` + runbook §10.4 (güvenilen vekil ağı `172.16.0.0/12`, `X-Forwarded-Proto`); PR #76 iki `DbContext` → `--context HrmsDbContext` kısıtı CI/betik/runbook'a işlendi | **F** |
| b | Birleştirilmiş arayüzlerin doğru çalışması için yaklaşım ve kontrol noktaları | **Yazılı entegrasyon stratejisi YOK** (`TEC.8/YAKLASIM.md` hiç commit edilmedi). Fiilî kontrol noktaları mevcut: CI işleri (`ci.yml` 9 iş, `api-contract-check.yml`), dağıtım betiği öz-denetimi (`deploy-uat.sh` 200/301/`Healthy`/5299 kapalı), arayüz sağlık uçları `/health/sync` (runbook §8.2), `/health/notifications` (§10.3) | **L−** |
| c | Entegrasyon için gereken destekleyici sistemler mevcut | Testcontainers PostgreSQL; **SQL Server Express** (LOGO salt-okunur kanıtı, PR #76); **Mailpit** (gerçek SMTP konuşması, PR #86); NetGSM sahte yanıtları; UAT sunucusu | **F** |
| d | Sistem entegre edildi | UAT'de üç konteyner; T3 UAT'de (bildirilen). Ancak entegre edilmiş sistemin **kimliği yok**: imajlar değişken `:uat` etiketiyle sunucuda derleniyor (`deploy-uat.sh`, `compose.uat.yml:50,113`), Git etiketi 0, GitHub Release 0 | **L** |
| e | Sistem öğeleri arası arayüzler sınandı | Entegrasyon testleri gerçek HTTP + PostgreSQL ile (PR #116: 252 test); OpenAPI sözleşme denetimi ve üretilen frontend tipleri denetimi her PR'da; `IdempotentSchemaScriptTests` dağıtımın kullandığı şema betiğini iki kez uyguluyor (PR #101 — betik UAT'yi düşürecekti, CI'da yakalandı) | **F** |
| f | Sistem ile dış çevre arasındaki arayüzler sınandı | **LOGO:** CI'da yazma reddi (`LogoReadOnlyAccessTests`, PR #76) + UAT'de canlı senkronizasyon (26.09, #79; canlı veride AN-17 bulgusu → `KR-079`, #77) ✔ · **NetGSM:** 12 sonuç kodu sahte yanıtla (PR #86) + canlı kimlik doğrulama `Healthy` (27.09, PR #86) + UAT'de gerçek SMS ulaştı (#103, 29.09) ✔ · **SMTP:** Mailpit ile CI ✔, **canlı arayüz `Unhealthy`** — Postfix 587 kendinden imzalı, süresi 07.03.2026'da dolmuş sertifika (PR #86, runbook §10.5, 27.09). PR #91 ve #108 notlarında "hâlâ çalışmıyor olabilir". **Ne issue ne risk kaydı var; çözüldüğüne dair kanıt yok** → e-posta kanalı uçtan uca doğrulanmadı | **L−** |
| g | Entegrasyon sonuçları ve anomaliler belirlendi | PR "Nasıl doğrulandı" tabloları (proje bazında test sayısı); Actions kayıtları (CI 113 çalışma / 9 başarısız); anomaliler #79, #93, #97, PR #101 betik hatası, CVE-2026-84782. **Entegrasyon raporu YOK**; SMTP anomalisi yalnızca PR metninde ve runbook'ta | **L−** |
| h | Entegre öğelerin izlenebilirliği | Matris §5 REQ → PR → test zinciri ✔; ancak "hangi öğe sürümü hangi entegre yapıya/dağıtıma girdi" bağı yok (etiket, imaj özeti, dağıtım kaydı yok) | **P+** |

**PA 1.1 = L** (8 çıktının 3'ü F, 4'ü L bandında, 1'i P+).

### 1.2 Temel uygulamalar
- **BP1 Hazırlık:** kısıtlar (5) ve destekleyici sistemler (3, 4) güçlü; **strateji (1) ve kriterler (2) yazılı değil** — CI ve betikte gömülü.
- **BP2 Entegrasyon:** sürekli entegrasyon her PR'da; dış arayüzler kısmen canlı sınandı (2/3 kanal).
- **BP3 Sonuçlar:** sonuçlar dağınık (PR metni, CI); izlenebilirlik kısmi; **baseline'a giren öğe yok** (BP3.3 karşılanmıyor).

### 1.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** beklenen sonuç: CI kapıları tanımlı; dış arayüz başarı ölçütü (ör. "`/health/notifications` iki kanal `Healthy`") yazılı değil.
- **2.1(b)** risk: **risk defteri 17.09'dan beri güncellenmedi** (sürüm 0.7) — T3 entegrasyon riskleri (SMTP sertifikası, sunucu saati #103, güvenilen vekil ağı aralığı) kayıtta yok. Web sertifikası `mail.duzen.com.tr` **11.10.2026'da bitiyor** (runbook §10.5) — 9 gün kaldı, izlenmiyor.
- **2.1(g)** taraflar arası arayüz: SMTP düzeltmesi "sunucu tarafında" (mail yöneticisi) — sahip, talep ve tarih yok.
- **2.2(a)(b)** süreç bilgi gereksinimi yalnızca doküman haritasında; süreç yaklaşımı yok.
- **2.2(d)** T3 PR'larının tamamında inceleme sayısı **0**; yazan = birleştiren (`dogusduzen`). Telafi kontrolleri MAN.5-DK'de yazılı ama ADR-0011 §6 "en az bir onay" kapısıyla çelişiyor.
- **2.2(e)** CI yapıtları 90 gün sonra siliniyor (`test-ve-kapsam`, `expires_at` 2026-12-31); kalıcı entegrasyon kaydı yok.

### 1.4 18.09'a göre değişim
**İyileşen:** (f) "dış arayüzler sınanmadı" → LOGO ve NetGSM canlı sınandı; (h) matris kuruldu (BULGU-01 kapandı); şema betiği CI'a alındı. **Değişmeyen:** rapor yok, yaklaşım yok (PA 2.2 P+ kaldı). **Yeni zayıflık:** SMTP anomalisi takipsiz; risk defteri T3 boyunca hiç güncellenmedi.

### 1.5 Düzeltici işler
| Öncelik | İş (issue boyutu) |
|---|---|
| **Yüksek** | `[HATA]` SMTP 587 sertifikası — e-posta kanalı UAT'de `Unhealthy`; sahibi (mail yöneticisi), hedef tarih, doğrulama komutu (runbook §10.5). İK kabulünün önkoşulu. Web sertifikası 11.10 bitişini risk defterine ekle |
| **Orta** | `TEC.8-entegrasyon/YAKLASIM.md`: entegrasyon sırası, arayüz bazında kontrol noktası ve başarı ölçütü (CI, dağıtım öz-denetimi, sağlık uçları), anomali kayıt yeri |
| **Orta** | T3 entegrasyon raporu `TEC.8/raporlar/2026-10-xx-t3-entegrasyon-raporu.md`: LOGO / NetGSM / SMTP / nginx-API için tarih, yöntem, sonuç, anomali, açık iş |
| **Orta** | Risk defteri T3 güncellemesi: SMTP sertifikası, sunucu saati (#103), tek kişi inceleme |
| **Düşük** | Dağıtılan imajın commit SHA ile etiketlenmesi (TEC.10 işiyle birlikte) |

---

## 2. TEC.9 — Doğrulama

### 2.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Gereksinim/tasarımı etkileyen doğrulama kısıtları | ADR-0011 (§1–§6); `SYG-KMLK.md` her maddede doğrulama yöntemi (Test 71 · Gösterim 3 · İnceleme 2 · Analiz 3); TEC.3 YAKLASIM §3: "Test yazılmış madde CI'da otomatik test olmadan tamamlanmış sayılmaz"; `IDateTimeProvider` tek saat kaynağı (#97) | **F** |
| b | Destekleyici sistemler mevcut | Testcontainers, SQL Server Express, Mailpit, CI; **ancak ADR-0011 §1'in öngördüğü uçtan uca test (Playwright, 5–8 senaryo) hiç kurulmadı** (`package.json`'da yok, `*.spec.ts` yok); performans (Analiz) için ölçüm aracı/betik depoda yok | **L** |
| c | Sistem doğrulandı | Son durum ~743 backend testi (PR #116: Domain 131, Application 278, Infrastructure 73, Mimari 9, Entegrasyon 252) + 230 frontend testi (PR #114); 18.09'da 168. **Açıklar:** SYG-KMLK-**060** (kimlik olayları denetim izi), **061** (kimlik günlüklerinde maskeleme), **062** (bildirim istisnası muafiyeti) — yöntem "Test", kaynakta ve testlerde **hiçbir atıf yok**, matris §5 satır 40–42 "sonraki iş / —". **PR #110'un "T3'ten kalanlar" listesinde bu üçü yok ve açık issue'ları yok** → kapsamdan sessizce düşme riski. 077–079 (Analiz) devam ediyor; 006, 064 (İnceleme) için inceleme kaydı yok; 065, 068, 073 (Gösterim) kabule bırakıldı | **L** |
| d | Düzeltici faaliyet için veri raporlandı | Testlerin yakaladığı hatalar issue'ya dönüştü: #93 (çift denetim izi), #97 (saat kaynağı — `main` 28.09 kırmızı), PR #101 betik hatası; ancak **#89 kararsız kapsam kapısı 27.09'dan beri açık** ve `main` 26.09'da bu yüzden kırmızı yandı (Domain %87,6 < %90) | **L** |
| e | Gereksinimi karşıladığına dair nesnel kanıt | KPÖ-KMLK-2 CI'da ölçüldü (medyan farkı 0,08 ms, `RegistrationTimingTests`, PR #88); LOGO `DENY` gerçek veritabanında; kontroller kırılarak kanıtlanmaya devam ediyor (PR #101 eski betikle düşüş, PR #76 1088/229). Testlerin 84 dosyası SYG kimliğine atıf yapıyor (69/79 SYG). **Ancak gereksinim başına "doğrulandı / tarih / çalışma no / commit" kaydı yok**; nesnel kanıt test adına indirgeniyor | **L** |
| f | Doğrulama sonuçları ve anomaliler belirlendi | PR tabloları ve Actions kayıtları. **Kayıt kusuru:** beş test projesi aynı `test-sonuclari.trx` adını kullanıyor, **yalnızca sonuncunun sonucu saklanıyor** (#89 gövdesinde tespit edilmiş, düzeltilmemiş); ham kapsam dosyaları saklanmıyor | **L−** |
| g | Doğrulanan öğelerin izlenebilirliği | Matris §5 "Doğrulama" sütunu 56 REQ satırında dolu (≥1 test sınıfı); "kısmen" işaretleri dürüst. Eksik: bağ **REQ** düzeyinde, **SYG** düzeyinde değil; doküman haritası §4'teki `TS-<MODÜL>-<no>` kimliği hiç kullanılmadı | **L** |

**PA 1.1 = L** (18.09: L+). Düşüşün nedeni ölçütün gereksinimle somutlaşması; SYG-060–062 boşluğu ve doğrulama raporunun yokluğu.

### 2.2 Temel uygulamalar
- **BP1.1 Doğrulama stratejisi:** ADR-0011 genel strateji olarak var; **T3'e özgü doğrulama planı yok** (hangi SYG hangi yöntemle, nerede, kim, giriş/çıkış ölçütü). ADR-0011 "Test stratejisi `TEC.9/YAKLASIM.md` içinde de özetlenecektir" yükümlülüğü karşılanmadı.
- **BP1.3** her doğrulama eylemi için amaç/koşul/uygunluk ölçütü: Test yöntemlilerde test kodunda örtük; Analiz/Gösterim/İnceleme yöntemlilerde **tanımlı değil** (ör. 077'nin yük profili, araç, örnek sayısı yazılı değil).
- **BP2** doğrulama prosedürleri: CI otomatik; elle olanlar için prosedür yok.
- **BP3.2** olay/problem kaydı ve takibi: iyi (#93, #97); #89 zayıf halka.
- **BP3.3 Paydaş mutabakatı** ("belirtilen gereksinimleri karşılıyor"): **yok** — G2 "Geliştirme tamam" kapısı (proje planı §3.1) için kayıt üretilmedi.
- **BP3.5** baseline: yok.

**Ek tutarsızlık:** SYG-KMLK-065 (360 px) yöntemi "Test", ancak PR #91 "jsdom ekran genişliğini ölçemiyor; UAT'de gösterilerek doğrulanacak" diyor — yöntem fiilen değişmiş, belge değişmemiş.

### 2.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** kapsam eşikleri ve kapılar tanımlı ✔; modül için "doğrulama tamam" ölçütü yazılı değil.
- **2.1(b)** risk: kararsız kapı (#89) 5 gündür "orta" öncelikte bekliyor — kapının güvenilirliği riski ele alınmadı.
- **2.1(c)** izleme: her PR'da ölçülüyor ✔; zaman içinde değerlendirme (kapsam eğilimi, kırmızı `main` sayısı: 26.09, 28.09, 30.09) yapılmıyor.
- **2.1(g)** ADR-0011 §6 "Kod gözden geçirme: en az bir onay" — T3 PR'larında inceleme kaydı **0**. Bu bir kalite kapısıdır ve fiilen uygulanmıyor; telafi kontrolü (MAN.5-DK) bu kapıyı kapsayacak biçimde güncellenmedi.
- **2.2(a)** gerekli bilgi tanımlı (doküman haritası: yaklaşım, kriterler, doğrulama raporu) ama süreç düzeyinde sahiplik/sıklık yok.
- **2.2(e)** saklama: **doğrulama raporu ve kapsam raporu `TEC.9/raporlar/` altında hiç yok** (ADR-0011 "Yükümlülükler"); CI yapıtı 90 günde siliniyor; trx üzerine yazılıyor.

### 2.4 18.09'a göre değişim
**İyileşen:** test sayısı 168 → ~973; test ↔ gereksinim bağı kuruldu (BULGU-01'in TEC.9(g) kısmı büyük ölçüde kapandı); kanıt-kırma kültürü sürüyor; hata→issue→regresyon testi döngüsü işliyor (#93, #97, #102, #103). **Değişmeyen:** rapor yok, yaklaşım yok. **Yeni:** kararsız kapı (#89), trx kaydı kusuru, SYG-060–062 takipsiz.

### 2.5 Düzeltici işler
| Öncelik | İş |
|---|---|
| **Yüksek** | SYG-KMLK-060, 061, 062 için görev issue'ları (veya gerekçeli kapsam kararı `KR-`): T3 G2 kapısı bunlar kapanmadan geçemez. PR #110 "kalanlar" listesinin neden eksik olduğu MAN.8'e not edilmeli (tamamlık denetimi otomatik değil) |
| **Yüksek** | T3 doğrulama raporu `TEC.9/raporlar/2026-10-xx-t3-dogrulama-raporu.md`: **79 SYG satırı** — yöntem, kanıt (test sınıfı / ölçüm / inceleme kaydı), CI çalışma no + commit, sonuç (Geçti / Kısmen / Bekliyor), anomali. Kapsam özeti ekli. G2 kapısının kaydı olarak kullanılır |
| **Orta** | `TEC.9-dogrulama/YAKLASIM.md`: ADR-0011 özeti + Analiz/Gösterim/İnceleme yöntemleri için prosedür ve uygunluk ölçütü; rapor sıklığı (her modül kapanışı); sahibi |
| **Orta** | #89 çözümü + CI'da proje başına ayrı trx adı ve ham kapsam dosyalarının yapıt olarak saklanması |
| **Orta** | İnceleme kapısı: ya tek kişi koşulunda yazılı telafi (ör. PR'da öz-inceleme kontrol listesi + haftalık örneklem incelemesi) ya ADR-0011 §6 revizyonu |
| **Düşük** | SYG-KMLK-065 doğrulama yöntemini "Gösterim" olarak düzelt (veya ölçülebilir otomatik test); ADR-0011'deki Playwright kararını uygula ya da gerekçeyle revize et; `TS-` kimlik kuralını uygula ya da haritadan çıkar |

---

## 3. TEC.10 — Geçiş

### 3.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Geçiş kısıtları belirlendi | `KR-066`, `KR-067`, `KR-093`; runbook §9–§12 (şifreleme anahtarı, JWT anahtarı zorunlu — API açılmaz, güvenilen vekil ağı, sunucu saati); #79 sır dosyasının iki okuyucusu | **F** |
| b | Destekleyici sistemler mevcut | UAT sunucusu, Let's Encrypt/DNS-01, sır dizini, NTP (#103, 29–30.09 ayarlandı). **Açık:** SSH parola girişi ve sunucu parolası 15.09'dan beri "Öneriliyor" (runbook §6) | **L** |
| c | Saha hazırlandı | Runbook (TEC.10-RB-001, 16 sürüm, son 02.10); §2 ilk kurulum, §8–§12 T3 eklemeleri | **F** |
| d | Kurulu sistem belirtilen işlevleri sunabiliyor | T3 UAT'de (bildirilen); PR #96 ve #99'da "UAT dağıtımı yapılamadı" (izin denetimi, eksik anahtarlar) → sonradan yapıldığına dair **yazılı kayıt yok**. E-posta kanalı `Unhealthy` (SMTP). SYG-060–062 yok | **L−** |
| e | Operatör/kullanıcı eğitimi | **Yok.** İK'nın UAT'de kullanacağı hesap işlemleri, davet ve parametre ekranları için kullanım notu/bilgilendirme kaydı yok. Üretim eğitimi "sırası gelmedi"; ama UAT kabulüne katılacak İK ve 2–3 personel (`KR-048`) için hazırlık sırası geldi | **N** |
| f | Geçiş sonuçları ve anomaliler | `deploy-uat.sh` öz-denetimi; anomaliler #79, #103 issue'ya döndü ✔; PR notlarındaki dağıtım engelleri. **Dağıtım kaydı, geçiş raporu yok** — `TEC.10/kayitlar/` ve `raporlar/` hiç yok | **L−** |
| g | Kurulu sistem etkin ve işletime hazır | UAT için evet (HTTPS, sağlık uçları); üretim sırası gelmedi | **L** |
| h | Geçişi yapılan öğelerin izlenebilirliği | **Zayıf.** UAT'de hangi commit'in çalıştığı hiçbir yerde kayıtlı değil; imaj etiketi değişken `:uat`; Git etiketi 0; GitHub Deployments 0 | **P** |

**PA 1.1 = L−** (18.09 ile aynı).

### 3.2 Temel uygulamalar
- **BP1.1 Sürüm yönetimi stratejisi:** CHANGELOG sürüm planı (`v0.2.0` = A2) ve ADR-0011 §7 ("yalnızca etiketlenmiş sürümler UAT'ye gider; kabul formu o sürüm numarasına yazılır") var — **uygulanmıyor**. `CHANGELOG.md` `[Yayımlanmamış]` bölümü A0 içeriğinde donmuş; T3'ün 21 PR'ı yok (BULGU-05 sürüyor).
- **BP1.3 / BP2.4 Eğitim ve kullanıcı belgesi:** yok.
- **BP1.4 Geçiş planı/takvimi:** runbook prosedürdür, plan değildir; T3 UAT geçişinin takvimi/sorumlusu yazılı değil.
- **BP2.5 Etkinleştirme ve kontrol:** dağıtım betiği otomatik kontrol yapıyor ✔ (örnek uygulama); çıktısı saklanmıyor.
- **BP3.2 Olay/problem takibi:** #79, #103 ✔; #73 (sır kopyası içeren yedek dizininin silinmesi, "en erken 29.09") **gecikmede ve açık**.
- **BP3.4 Baseline:** yok.

### 3.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)/(c)** proje planı §3 adım 8 "Sürüm etiketi (baseline) → UAT'ye dağıtım" planlanmış, **uygulanmadı**; G2 kapısı kaydı yok. Dağıtımlar planlı değil, PR'dan sonra fırsatçı (PR #96/#99 engelleri bunu gösteriyor).
- **2.1(b)** risk: R-18 izleniyor ✔; #73'teki sır kopyası ve SSH parola girişi risk olarak kayıtlı değil.
- **2.2(c)(d)** runbook PR ile değişiyor ve değişiklik geçmişi örnek düzeyde ✔; ancak **içerik çelişkileri** gözden geçirmeden geçmiş:
  - §1 "Amaç: İK kabul testi — **gerçek veri değil, maskelenmiş kopya**" ↔ §10.2 "UAT **gerçek LOGO verisiyle** çalışır" (aynı çelişki ADR-0011 §7 ve KR-024 bağlamında da sürüyor);
  - §4 doğrulama komutları `http://` ile 200 bekliyor — TLS'ten (17.09) beri 301 döner;
  - §6 "Kimlik doğrulama ⏳ — şu an uygulamada oturum yok" ve "ufw yalnızca 22 ve 80" — bayat (443 gerekli).
- **2.2(e)** dağıtım kanıtı (betik çıktısı, hangi sürüm, ne zaman, kim) saklanmıyor.

### 3.4 18.09'a göre değişim
**İyileşen:** runbook 0.2 → 1.6, T3 için tekrarlanabilir kurulum adımları; NTP sorunu bulundu ve çözüldü; dağıtımı bozacak hata CI'a taşındı (PR #101). **Gerileyen:** T3 ilk gerçek işlev teslimi olduğu hâlde etiketsiz dağıtıldı (ADR-0011 §7, plan adım 8 ihlali) → PA 2.1 L → P+; runbook çelişkileri → PA 2.2 L → L−.

### 3.5 Düzeltici işler
| Öncelik | İş |
|---|---|
| **Yüksek** | Kabul adayı sürümün etiketlenmesi (ör. `v0.2.0-rc.1`) ve UAT'ye **o etiketten** yeniden dağıtım; CHANGELOG'a T3 girişi. Kabul formu bu etikete yazılır |
| **Yüksek** | Dağıtım kaydı: `deploy-uat.sh` commit SHA'yı imaj etiketine ve sunucuda bir kayıt dosyasına yazsın; her dağıtım `TEC.10/kayitlar/YYYY-AA-GG-uat-dagitimi.md` (sürüm, şema, doğrulama çıktısı, anomaliler) üretsin. Bugünkü UAT sürümü geriye dönük tek kayıtla tespit edilsin |
| **Orta** | Runbook ve ADR-0011 §7 tutarlılık düzeltmesi (§1 veri niteliği, §4 HTTPS komutları, §6 bayat satırlar) |
| **Orta** | #73'ün kapatılması (sır kopyası içeren yedek dizini) ve SSH parola girişinin kapatılması |
| **Orta** | UAT kabulü için kısa kullanım notu (İK hesap işlemleri, davet, parametre ekranı; personel için üyelik/giriş) ve bilgilendirme kaydı — TEC.10(e) |
| **Düşük** | `TEC.10-gecis/YAKLASIM.md`: sürüm stratejisi, dağıtım sıklığı, geri dönüş (rollback) adımı — bugün geri dönüş yordamı yok |

---

## 4. TEC.11 — Geçerleme

### 4.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Paydaş gereksinimleri için geçerleme kriterleri tanımlandı | `PG-KMLK.md`: 56 REQ'nin her birinde **"Kabul kriteri"** sütunu, 23.09.2026 İK toplantısında onaylı (`TEC.2/kayitlar/2026-09-23-kimlik-gereksinim-toplantisi.md`); TEC.2 YAKLASIM §3.2 "kabul kriteri gereksinimin parçasıdır" | **F** |
| b | Paydaşların gerektirdiği hizmetlerin kullanılabilirliği teyit edildi | YOK | **N** |
| c | Geçerleme kısıtları belirlendi | `KR-048` (İK dışından 2–3 personel), `KR-083` AllowList kipi kabul testleri için, SYG-KMLK §6 "AN-01 kalan riski kabulde İK'ya ayrıca gösterilecek", Gösterim yöntemli SYG'ler (065, 068, 073). Bir geçerleme planında toplanmamış | **P+** |
| d | Sistem geçerlendi | YOK (kabul yapılmadı) | **N** |
| e | Destekleyici sistemler mevcut | UAT ve AllowList mekanizması var; **e-posta kanalı çalışmıyor** (SMTP) — üyelik/sıfırlama/davet akışlarının e-posta yarısı geçerlenemez; AllowList'in UAT'de yapılandırıldığına dair kayıt yok | **P+** |
| f | Geçerleme sonuçları ve anomaliler | YOK | **N** |
| g | Paydaş ihtiyaçlarının karşılandığına nesnel kanıt | YOK | **N** |
| h | Geçerlenen öğelerin izlenebilirliği | Matris §5 "Kabul" sütunu var, boş ve neden boş olduğu açıklanmış — yapı hazır, kanıt yok | **N** (yapı: P−) |

**PA 1.1 = P** (18.09: N). "Sırası gelmedi" gerekçesi **artık kısmen geçerli değil**: T3 geliştirmesi büyük ölçüde bitti ve UAT'de; kabul **öncesi** üretilmesi gereken geçerleme planı/prosedürü (BP1, BP2.1) sırası gelmiş ve yapılmamış işlerdir. b, d, f, g için "sırası gelmedi" kabul edilebilir.

### 4.2 Temel uygulamalar
- **BP1.1 Geçerleme stratejisi:** yalnızca ADR-0011 §8 ayrım tablosu ve proje planı §3 adım 9–10; **T3 kabul planı yok** (kapsam, katılımcılar, takvim, ortam, sürüm, giriş/çıkış ölçütleri, bulgu sınıflandırması, "kabul / koşullu kabul / ret" karar kuralı).
- **BP1.3** her eylem için amaç/koşul/uygunluk ölçütü: REQ kabul kriterleri var, ancak senaryo/adım düzeyine indirilmemiş.
- **BP2.1** geçerleme prosedürleri (kabul senaryoları): **yok**.
- **BP3.3** paydaş mutabakatı: **kabul formu şablonu yok** (`docs/sablonlar/` yalnızca ADR şablonu içeriyor); doküman haritası "İK imzalı kabul formları" bekliyor.

### 4.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** beklenen sonuç (G3 kapısı "kabul kriterleri karşılandı, form imzalandı") planda tanımlı ✔; T3 için ölçülebilir çıkış ölçütü (ör. "Zorunlu REQ'lerde açık kritik bulgu 0") yok.
- **2.1(c)(d)(e)** takvim, görevli İK kişileri ve 2–3 personelin adı/rolü, UAT erişim hesapları planlanmamış; RACI'de TEC.11 sorumluluğu genel düzeyde var (`roller-ve-sorumluluklar.md` satır 83).
- **2.1(g)** İK ile arayüz: kabul daveti/takvim mutabakatı kaydı yok.
- **2.2(a)(b)** kayıt biçimi (kabul raporu, imzalı form, bulgu listesi) haritada tanımlı ✔; şablon ve yaklaşım yok.

### 4.4 18.09'a göre değişim
N → P. Kazanım tamamen TEC.2'den gelen onaylı kabul kriterleri (a) ve dağınık kısıtlar (c). Geçerleme sürecinin kendisi henüz işletilmedi.

### 4.5 Düzeltici işler
| Öncelik | İş |
|---|---|
| **Yüksek** | `TEC.11-gecerleme/YAKLASIM.md` + T3 kabul planı: kapsam (56 REQ), katılımcılar (İK + `KR-048` 2–3 personel), UAT sürümü (etiket), AllowList alıcıları, takvim, giriş ölçütü (G2 kaydı, doğrulama raporu, SMTP `Healthy`), çıkış ölçütü ve karar kuralı, AN-01 kalan riskinin gösterimi |
| **Yüksek** | T3 kabul senaryoları (`TS-KMLK-nn` veya eşdeğeri): her REQ kabul kriteri → en az bir senaryo; Gösterim yöntemli SYG-065/068/073 senaryoya bağlanır; matris "Kabul" sütununa senaryo kimliği |
| **Yüksek** | Kabul formu şablonu `docs/sablonlar/` + kabul raporu şablonu (`raporlar/YYYY-AA-GG-kimlik-kabul-raporu.md`): sürüm etiketi, REQ bazında sonuç, bulgular, koşullar, imza |
| **Orta** | Kabul bulgu kaydı yöntemi: her bulgu `[HATA]` veya `[DEĞİŞİKLİK]` issue'su, `surec:TEC.11` etiketi (bugüne kadar 0 kez kullanıldı) |

---

## 5. TEC.13 — Bakım

### 5.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Bakım kısıtları belirlendi | `KR-065` taban imaj paketlerinin derlemede güncellenmesi; trivy kapısı; R-18 elle sertifika yenileme; runbook §12 saat eşitleme; §7.2 yenileme yordamı. Bakım stratejisi/destek modeli yok | **P+** |
| b | Bakım için destekleyici sistemler | `03-bug.yml` hata şablonu, `tur:hata` etiketi ("TEC.13"), CI kapıları, UAT | **L** |
| c | Onarılmış/revize öğeler sağlandı | #79 → PR #80 (aynı gün); #93 → PR #94; #97 → PR #96; #102 + #103 → PR #104 (aynı gün) + sunucuda NTP ayarı; CVE-2026-84782 → PR #108 içinde `apt-get upgrade` (main ~21 saat kırmızı kaldı). Onarımların ikisi ayrı, üçü özellik PR'larının içinde | **L** |
| d | Düzeltici/uyarlayıcı/iyileştirici değişiklik ihtiyacı raporlandı | 8 `[HATA]` issue'su; T3 dönemindekiler (#79, #89, #93, #97, #102, #103) **örnek nitelikte**: belirti, kök neden, "neden şimdiye kadar görülmedi", etki, düzeltme, regresyon testi. `[DEĞİŞİKLİK]` #77 canlı veriden doğdu. **Eksik:** CVE-2026-84782 (`main` kırmızı) ve SMTP sertifikası için issue yok | **L** |
| e | Arıza ve ömür verisi (maliyet dâhil) belirlendi | **YOK.** Hata sayısı, kaynağı (CI/UAT/yerel), kapanma süresi, eğilim, maliyet hiçbir yerde derlenmemiş. (Ham veri mevcut: T3 döneminde 6 hata, 5'i ≤1 günde kapandı, #89 5 gündür açık) | **N** |

**PA 1.1 = P+** (18.09: P−). Üretim yokken a/e'nin zayıf olması kısmen "sırası gelmedi"dir; ancak UAT'de bakım fiilen yapılıyor (NTP, CVE, sertifika) ve eğilim verisi sırası gelmiş bir çıktıdır.

### 5.2 Temel uygulamalar
- **BP1.1 Bakım stratejisi:** yok (doküman haritası: destek modeli, hata sınıflandırma, çözüm süreleri).
- **BP2.1** olay/problem raporlarının gözden geçirilmesi: issue bazında ✔; dönemsel gözden geçirme yok.
- **BP2.5 Önleyici bakım:** trivy kapısı ve `apt-get upgrade` ✔ (örnek); sertifika süreleri (UAT 16.12, mail web 11.10) izleme listesi yok.
- **BP4.1** kayıt ✔ · **BP4.2 eğilim** ✗ · **BP4.3 izlenebilirlik:** hata → PR bağı var, ancak etiketleme tutarsız: hatalar `surec:TEC.7` ile etiketleniyor; `surec:TEC.13` etiketi **hiç kullanılmadı** (0 issue, 0 PR), `tur:hata` etiketinin açıklaması ise "TEC.13" diyor · **BP4.5 müşteri memnuniyeti:** yok (sırası gelmedi).

### 5.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** çözüm süresi hedefi yok → kapanma süreleri ölçülemez, değerlendirilemez.
- **2.1(c)** #89 (orta) ve #73 (gecikmiş) izlenmiyor; önceliklendirme kuralı yazılı değil.
- **2.1(d)** "hatayı kim sınıflandırır / kim kapatır" yazılı değil (tek kişi — R-05).
- **2.2(a)(b)** `03-bug.yml` şablonu bilgi gereksinimini fiilen tanımlıyor ✔; bakım raporu yok; etiket kuralı tutarsız (yukarıda).
- **2.2(d)** hata düzeltmelerinin özellik PR'ına gömülmesi (#93, #97, CVE) inceleme ve izlenebilirliği zayıflatıyor.

### 5.4 18.09'a göre değişim
P− → P+ (PA 1.1). Hata kayıtlarının kalitesi belirgin biçimde arttı (kök neden + "neden görülmedi" + regresyon testi). Strateji ve rapor tarafı değişmedi.

### 5.5 Düzeltici işler
| Öncelik | İş |
|---|---|
| **Orta** | `TEC.13-bakim/YAKLASIM.md`: hata sınıfları ve öncelik → hedef çözüm süresi; `main` kırmızısı (güvenlik açığı dâhil) için `[HATA]` issue zorunluluğu; düzeltmenin ayrı PR'da yapılması kuralı; sertifika/sır süreleri izleme listesi |
| **Orta** | Etiket kuralı: `tur:hata` issue'larına `surec:TEC.13` (geliştirme sırasında bulunanlar için kural yazılı olarak `TEC.7`/`TEC.9` ise açıklamanın düzeltilmesi) |
| **Düşük** | İlk bakım/olay raporu `TEC.13/raporlar/2026-10-xx-t3-donemi-olay-raporu.md`: 6 hata, kaynak, kapanma süresi, kök neden sınıfı (saat/zaman 3, yapılandırma 1, DI kaydı 1, CI 1), CVE olayı |
| **Düşük** | CVE-2026-84782 ve SMTP sertifikası için geriye dönük kayıt (issue veya rapor satırı) |

---

## 6. Süreçler arası ortak bulgular

| No | Bulgu | Etkilenen | Önem |
|---|---|---|---|
| C-01 | **TEC.8/9/11/13 için `YAKLASIM.md`, `kayitlar/`, `raporlar/` hiç commit edilmedi** (30.09 olayı yalnızca boş yerel klasörleri sildi). BULGU-02 bu dört süreçte hiç ilerlemedi; T3 çevrimi "sürecin ilk kez işletildiği an" kuralına (18.09 öneri #4) rağmen geçti | PA 2.2(a)(b) ×4 | Yüksek |
| C-02 | **Baseline yok:** Git etiketi 0, Release 0, CHANGELOG T3'ü içermiyor. TEC.8(h), TEC.9 BP3.5, TEC.10(h), TEC.11 BP3.5 aynı eksikten etkileniyor | 4 süreç | Yüksek |
| C-03 | **Gereksinim tamamlık denetimi otomatik değil:** `requirement-traceability-check.mjs` REQ↔SYG eşlemesini denetliyor, ancak SYG → test/gerçekleştirme bağını denetlemiyor. Sonuç: SYG-060/061/062 hiçbir kapıya takılmadan "kalanlar" listesinden düştü | TEC.9, TEC.11 | Yüksek |
| C-04 | **Bağımsız inceleme yok:** T3'ün 21 PR'ında inceleme 0; ADR-0011 §6 ve TEC.3 YAKLASIM §4 "en az bir onay" diyor | TEC.9, PA 2.2(d) tümü | Orta |
| C-05 | **Kanıt saklama süresi:** CI yapıtları 90 gün; PR metinleri tek doğrulama kaydı. 31.12.2026'dan sonra T3 test sonuçlarının ham kanıtı kaybolur | PA 2.2(e) | Orta |
| C-06 | **Süreç etiketleri kullanılmıyor:** `surec:TEC.8` 0, `TEC.11` 0, `TEC.13` 0, `TEC.9` 1, `tur:test` 0; T3 işlerinin tamamı `surec:TEC.7`. Süreç bazında kanıt sorgulanamıyor | MAN.2/MAN.6 ile birlikte | Düşük |
| C-07 | **Risk defteri T3 boyunca güncellenmedi** (son 17.09) | TEC.8, TEC.10 PA 2.1(b) | Orta |

---

## 7. Önceliklendirilmiş düzeltici iş listesi (issue boyutunda)

**Yüksek — T3 kabulünden ÖNCE**

1. **[GÖREV] SYG-KMLK-060/061/062 kapsam boşluğu** — üç gereksinim için görev issue'su veya gerekçeli `KR-` kararı; matris satır 40–42 güncellenir. (TEC.9 c, C-03)
2. **[HATA] SMTP 587 sertifikası — UAT e-posta kanalı `Unhealthy`** — sahip, tarih, runbook §10.5 doğrulaması; web sertifikası 11.10.2026 bitişi risk defterine. (TEC.8 f, TEC.11 e)
3. **[GÖREV] T3 doğrulama raporu (79 SYG satırı) ve G2 kapı kaydı** — `TEC.9/raporlar/`; 077–079 ölçüm yazımı bu rapora bağlanır. (TEC.9 BP3, PA 2.2 e)
4. **[GÖREV] T3 kabul planı, kabul senaryoları ve kabul formu şablonu** — `TEC.11/YAKLASIM.md`, `kayitlar/`, `docs/sablonlar/`; matris "Kabul" sütununa senaryo kimliği. (TEC.11 BP1–BP3)
5. **[GÖREV] Kabul adayı sürüm etiketi ve etiketten UAT dağıtımı + dağıtım kaydı** — `deploy-uat.sh` SHA etiketi; `TEC.10/kayitlar/`; CHANGELOG T3 girişi. (TEC.10 h, C-02)

**Orta — T3 kabulü sırasında**

6. **[GÖREV] TEC.8 yaklaşımı ve T3 entegrasyon raporu** (arayüz bazında sonuç). (TEC.8 b, g)
7. **[GÖREV] TEC.9 yaklaşımı** — Analiz/Gösterim/İnceleme prosedürleri ve uygunluk ölçütleri. (TEC.9 BP1.3)
8. **[HATA] #89 kararsız kapsam kapısı + proje başına trx adı + ham kapsam yapıtı**. (TEC.9 f)
9. **[GÖREV] Runbook/ADR-0011 tutarlılık düzeltmesi** — veri niteliği (maskelenmiş ↔ gerçek LOGO), §4 HTTPS, §6 bayat satırlar. (TEC.10 PA 2.2)
10. **[GÖREV] #73 kapatılması + SSH parola girişinin kapatılması**. (TEC.10 BP3.2)
11. **[GÖREV] UAT kabul katılımcıları için kısa kullanım notu ve bilgilendirme kaydı**. (TEC.10 e)
12. **[GÖREV] Risk defteri T3 güncellemesi** (SMTP, saat, tek kişi inceleme, sertifika süreleri). (C-07)
13. **[DÜZELTİCİ] İnceleme kapısı** — tek kişi koşulunda yazılı telafi veya ADR-0011 §6 revizyonu. (C-04)
14. **[GÖREV] TEC.13 yaklaşımı ve etiket kuralı** — öncelik → çözüm süresi; `main` kırmızısı için zorunlu issue. (TEC.13 BP1, BP4.3)

**Düşük — fırsat bulundukça**

15. **[GÖREV] T3 dönemi olay/bakım raporu** (6 hata, kapanma süreleri, kök neden sınıfları, CVE olayı). (TEC.13 e)
16. **[GÖREV] SYG-KMLK-065 doğrulama yöntemi düzeltmesi; Playwright kararının uygulanması veya revizyonu; `TS-` kimlik kuralının netleştirilmesi**. (TEC.9 b, g)
17. **[GÖREV] CI kanıtlarının kalıcı arşivi** — modül kapanışında test/kapsam özetinin depoya alınması. (C-05)

---

## 8. Güçlü yönler (denge için)

1. **Hata kayıtları örnek nitelikte:** #93, #97, #102, #103 kök neden, "neden şimdiye kadar görülmedi", etki ve regresyon testi taşıyor; çoğu aynı gün kapandı.
2. **Kontroller kırılarak kanıtlanıyor:** PR #101'de dağıtım betiği hatası önce yeniden üretildi, sonra CI testine dönüştürüldü; PR #76'da LOGO `DENY` gerçek SQL Server'da sınandı.
3. **Dış arayüzlerde gerçek konuşma:** Mailpit ile gerçek SMTP, SQL Server Express ile gerçek LOGO yetki modeli; sağlık uçları ileti göndermeden canlı kimlik doğrulaması yapıyor.
4. **Canlı veriden gereksinim düzeltmesi:** UAT senkronizasyonunda bulunan ortak adres durumu (AN-17) değişiklik talebine (#77) ve karara (`KR-079`) dönüştü — doğrulama ile gereksinim süreci arasında çalışan bir geri besleme.
5. **İzlenebilirlik matrisi gerçekten PR'la birlikte güncellendi:** 2.0 sürümüne kadar 20 değişiklik, her biri bir PR'a bağlı; "kısmen" işaretleri dürüst.
6. **Runbook yaşayan belge:** 16 sürüm, her değişiklik bir issue'ya bağlı.

---

## Ek — Başlıca kanıt kaynakları

- `docs/33061/izlenebilirlik-matrisi.md` (2.0, 02.10.2026) §5 satır 40–42 (SYG-060/061/062)
- `docs/33061/TEC.3-sistem-yazilim-gereksinimleri/gereksinimler/SYG-KMLK.md` §1, §2.3, §4.8–4.12, §5, §7
- `docs/33061/TEC.2-paydas-ihtiyac-ve-gereksinimleri/paydas-gereksinimleri/PG-KMLK.md` ("Kabul kriteri" sütunu)
- `docs/33061/TEC.10-gecis/uat-ortami-kurulum-runbook.md` §1, §4, §6, §10.2, §10.5
- `docs/adr/ADR-0011-test-stratejisi.md` §1, §6, §7, "Yükümlülükler"
- `docs/33061/MAN.1-proje-planlama/proje-plani.md` §3 (adım 8–10), §3.1 (G2, G3)
- `docker/deploy-uat.sh`, `docker/compose.uat.yml` (imaj etiketi `:uat`)
- `.github/workflows/ci.yml` (trx adı `test-sonuclari.trx`, yapıt `test-ve-kapsam`)
- Issue'lar: #73, #79, #89, #93, #97, #102, #103; PR'lar: #76, #86, #88, #91, #96, #99, #101, #104, #108, #110, #114, #116
- Actions: `36272369709` (26.09, Domain %87,6), `36471125810` (28.09, `SessionApiTests` — #97), `36699066208` (30.09, trivy CVE-2026-84782); yapıt `test-ve-kapsam` son kullanım 2026-12-31
- `gh release list` boş; `git tag` boş; GitHub Deployments 0; `surec:TEC.8/11/13` etiket kullanımı 0
