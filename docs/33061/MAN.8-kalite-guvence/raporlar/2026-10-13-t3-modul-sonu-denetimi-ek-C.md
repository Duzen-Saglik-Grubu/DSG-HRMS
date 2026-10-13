# Bulgular C — TEC.8, TEC.9, TEC.10, TEC.11, TEC.13 (T3 modül sonu)

**Değerlendirme tarihi:** 2026-10-13
**Dönem:** 02.10.2026 T3 süreç denetiminden (MAN.8-SGR-2026-10-02, `main` @ `4199526`) bugüne (`main` @ `d354e94`). 45 commit; #117–#205. T3 Kimlik Yönetimi İK tarafından 13.10.2026'da kabul edildi (`KR-105`); `v0.2.0` etiketi `149f6ee`'de (`v0.2.0-rc.5` ile aynı commit).
**Dayanak:** TS ISO/IEC TS 33061 Madde 5.5.9–5.5.14 (çıktılar, BP'ler, bilgi öğeleri) · TS ISO/IEC 33020 Madde 5.2.4.2–5.2.4.3 (PA 2.1 a–g, PA 2.2 a–e)
**Yöntem:** Salt okuma. Depo, GitHub issue/PR/Actions kayıtları (`gh`), CI günlükleri. UAT sunucusu salt okunur olarak denetlendi (13.10.2026 18:12 UTC: `deployments.log`, `VERSION`, konteyner OCI etiketleri, `/health/notifications`, TLS bitişi, `sshd -T`). "Planlandı / yapılacak / PR notunda söz verildi" kanıt sayılmadı. Ölçek: N/P/L/F (+/− yalnızca bant içi konumu gösterir).
**Bilinen durumlar:** (1) İmzalar, kullanıcının 13.10.2026 kararıyla, onaylı kalite belgeleriyle birlikte daha sonra toplanacak. Etkisi TEC.11 (g) ve PA 2.2 (e) altında değerlendirildi; imza **olmayan** kanıt olarak ele alındı. (2) `v0.2.0` için UAT'ye yeniden kurulum yapılmadı; aynı commit (`149f6ee`) 12.10.2026'dan beri çalışıyor. Bu, sunucuda doğrulandı. (3) Bu ek kişisel veri içermez. Katılımcılar rol adıyla anılır.

---

## 0. Özet tablo

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | 02.10 (1.1/2.1/2.2 → Seviye) | Ana engel (Seviye 2 için) |
|---|---|---|---|---|---|---|
| TEC.8 Entegrasyon | **L+** | **L** | **L** | 1 | L / L− / P+ → 1 | Entegrasyon raporu `rc.2`'de kaldı; canlı arayüz denetimleri kayda girmiyor |
| TEC.9 Doğrulama | **L+** | **L** | **L** | 1 | L / L / P+ → 1 | Doğrulama raporu ve G2 kaydı kabul edilen sürümü (81 SYG) kapsamıyor; #198 doğrulama kaçağı |
| TEC.10 Geçiş | **L+** | **L+** | **L** | 1 | L− / P+ / L− → 1 | Geri dönüş yordamı yok; yaklaşım belgesi ve ADR-0011 §7 bayat |
| TEC.11 Geçerleme | **L+** | **L** | **L−** | **1** | P / P / P → 0 | Paydaş onayının nesnel kanıtı imzasız ve tek kopya; katılımcı temsili |
| TEC.13 Bakım | **L−** | **P+** | **L−** | **1** | P+ / P / P+ → 0 | Arıza/eğilim verisi yok; 05.10 CI olayı kayda girmedi; etiket kuralı ihlal ediliyor |

> **Genel tablo:** 02.10'daki Yüksek öncelikli işlerin bu beş süreçteki **tamamı**
> issue'ya dönüştü ve kapandı (§6). Beş sürecin beşi de artık yaklaşım belgesine,
> kayıt klasörüne ve en az bir dönem içi kayda sahip. TEC.11 ve TEC.13 Seviye 0'dan
> 1'e çıktı. **Seviye 2'ye ulaşan süreç yok.** Hiçbir süreçte PA 1.1 = F değil. Ortak
> neden şu: kayıtlar **ara bir noktada** (04–05.10) üretildi, ama kabul edilen sürüme
> (`rc.5`, 12.10) kadar sürdürülmedi. Modül kapanışındaki iki değişiklik talebi (#185,
> #190) doğrulama, entegrasyon ve olay kayıtlarına geri yansıtılmadı.

---

## 1. TEC.8 — Entegrasyon

### 1.1 Çıktılar

| # | Çıktı (kısa) | Kanıt (02.10 sonrası) | 02.10 | 13.10 |
|---|---|---|---|---|
| a | Entegrasyon kısıtları (arayüzler dâhil) belirlendi | `TEC.8-entegrasyon/YAKLASIM.md` §2: beş arayüzün yönü, protokolü, kısıtı ve kaynağı tek tabloda (#130, PR #172, 05.10). SYG-KMLK §2.3'e bağlı | F | **F** |
| b | Yaklaşım ve kontrol noktaları | YAKLASIM §3: dört kontrol noktası (PR'da CI, kabul adayı etiketi, dağıtım betiği öz denetimi, dağıtım sonrası canlı denetim) ve her birinin beklenen sonucu. §9'da açık noktalar dürüstçe yazılı | L− | **F** |
| c | Destekleyici sistemler | 02.10'dakilere ek olarak uçtan uca yığın (`docker/compose.e2e.yml`, Playwright, #146, PR #147) | F | **F** |
| d | Sistem entegre edildi | Entegre yapının kimliği var: açıklamalı etiketler `v0.2.0-rc.1…rc.5`, `v0.2.0`; imajda `org.opencontainers.image.version/.revision`. **Sunucuda doğrulandı (13.10):** API ve web konteynerinin `revision` etiketi `149f6ee21c3f…`, `VERSION` = `v0.2.0-rc.5` | L | **F** |
| e | Öğeler arası arayüzler sınandı | CI'da gerçek PostgreSQL ile entegrasyon testleri (rc.5: 286), sözleşme denetimi, 7 uçtan uca senaryo (nginx + API + PostgreSQL + Mailpit). Not: uçtan uca senaryolar 2FA akışını içermiyor (PR #191 "Nasıl doğrulandı") | F | **F** |
| f | Dış çevreyle arayüzler sınandı | **SMTP:** 01.10'da düzeldi, sertifika 06.10'da yenilendi (bitiş 27.03.2027, #179, PR #180). **Bugün (13.10):** `/health/notifications` → `email: Healthy`, `sms: Healthy`; UAT TLS bitişi 16.12.2026. **Zayıflık:** YAKLASIM §3.4'teki canlı denetimlerin (`/health/sync`, `/health/notifications`, vekil ağı, SMTP sertifikası) rc.3, rc.4 ve rc.5 dağıtımlarında yapıldığına dair **kayıt yok**. Dağıtım betiği bunları denetlemiyor (YAKLASIM §9 madde 1) | L− | **L+** |
| g | Sonuçlar ve anomaliler belirlendi | `raporlar/2026-10-05-t3-entegrasyon-raporu.md` (#134, PR #168): beş arayüz, yedi anomali (A-1…A-7), kaynak ve düzeltme. **Ancak:** (1) Rapor `rc.2`'yi (`69bafad`) entegre yapı olarak kaydediyor. Kabul edilen `rc.5`'e kadar gelen değişiklikler rapora girmedi: yeni 2FA uçları, migration `20261010203210_UserTwoFactorPreference`, OpenAPI değişikliği (#190) ve parametre kataloğu değişikliği (#198). (2) İç tutarsızlık var: §1 "Sonuç" paragrafı SMTP yenilemesini hâlâ açık iş sayıyor; §2.3 "Bugün" satırı bitişi 11.10.2026 gösteriyor. Oysa aynı satırın "Açık iş" hücresi yenilemenin kapandığını yazıyor (1.1). (3) Girişte "yaklaşım #130 kapsamında yazılacak" ifadesi bayat | L− | **L** |
| h | Entegre öğelerin izlenebilirliği | Etiket → commit → OCI `revision` → dağıtım kaydı zinciri kuruldu ve canlıda tutuyor. Entegrasyon raporu §4 ise kabul edilen yapıya (`v0.2.0` / `rc.5`) değil `rc.2`'ye bağlı | P+ | **L+** |

**PA 1.1 = L+** (02.10: L). Sekiz çıktının beşi F. F'ye çıkmayı engelleyen şey, modül kapanışındaki entegre yapının (`rc.5`) raporlanmamış olması ve canlı arayüz denetimlerinin kayda geçmemesi.

### 1.2 Temel uygulamalar
- **BP1 Hazırlık:** Strateji (1) ve kriterler (2) artık yazılı (YAKLASIM §3). Kısıtlar (5) ve destekleyici sistemler (3, 4) güçlü.
- **BP2 Entegrasyon:** Her PR'da sürekli entegrasyon yapılıyor. Dış arayüzlerin üçü de canlıda `Healthy`.
- **BP3 Sonuçlar:** Rapor var. **Baseline'a giren öğe var** (`v0.2.0` etiketi, BP3.3 / BP3.5). Raporun kapsadığı yapı ile baseline yapısı farklı.

### 1.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** Hedefler yazılı (YAKLASIM §1.1). "Her modül kapanışında rapor" hedefi **ara bir sürüm** için karşılandı.
- **2.1(b)** R-20, R-21 ve R-22 risk defterinde (#126, PR #164) ✔.
- **2.1(c)** İzleme zayıf. Dağıtım sonrası canlı denetimler (§3.4) elle yapılıyor ve sonucu hiçbir yere yazılmıyor. 05.10 akşamı `main`'deki dört commit'in CI'ı tamamlanmadı (§5, YB-08) ve bu entegrasyon kontrol noktası boşluğu fark edilmedi.
- **2.1(g)** Mail yöneticisiyle arayüz bu kez kayıtlı: sertifika yenilemesi #179'da kayıtlı ✔.
- **2.2(a)(b)** YAKLASIM §6–§7 ✔.
- **2.2(c)** Rapor PR ile ve değişiklik geçmişiyle güncellendi (1.1). Ancak güncelleme yalnızca ilgili satıra yapıldı; özet ve "Bugün" satırları tutarsız kaldı.
- **2.2(d)** İnceleme kanıtı yalnızca `KR-096`'ya göre yazılan onay yorumu. Yazan ve onaylayan aynı hesap.
- **2.2(e)** Kalıcı özet (TEC.9 `kayitlar/`) ✔.

### 1.4 02.10'a göre değişim ve bulguların durumu

| 02.10 bulgusu / işi | Durum |
|---|---|
| SMTP 587 sertifikası, e-posta kanalı (Ek C §7 iş 2) | ✅ Kapandı. 01.10 düzeltme, 06.10 yenileme (#179), R-20. 13.10'da canlıda `Healthy` |
| TEC.8 YAKLASIM (iş 6, T3-08) | ✅ #130 → PR #172 |
| T3 entegrasyon raporu (iş 6, ana rapor iş 16) | ⚠️ Yazıldı (#134 → PR #168). `rc.2`'de kaldı (YB-07) |
| Risk defteri güncellemesi (iş 12, C-07) | ✅ #126 → PR #164 (R-20/21/22). R-25 ve R-27 sonradan eklendi |
| Dağıtılan imajın SHA ile etiketlenmesi | ✅ #125 → PR #156 |

**İyileşen:** b, d, h. Yaklaşım belgesi ve rapor yazıldı. **Değişmeyen:** Canlı arayüz denetimi kayda geçmiyor (02.10'da "PR metninde" idi, bugün "hiçbir yerde"). **Yeni:** YB-07.

### 1.5 Düzeltici işler
| Öncelik | İş (issue boyutu) |
|---|---|
| **Orta** | **YB-07** Entegrasyon raporu 1.2: §4'ü `v0.2.0` (`149f6ee`) olarak güncelle. rc.3–rc.5 değişikliklerinin arayüz etkisini ekle: 2FA uçları (kod gönderimi SMS/e-posta kanalını kullanır), migration, OpenAPI. §1 "Sonuç" ve §2.3 "Bugün" satırlarını 1.1 ile tutarlı yap. 13.10 canlı denetim sonucunu yaz |
| **Orta** | `deploy-uat.sh`: `/health/sync` ve `/health/notifications` sonucunu `deployments.log` satırına (veya ayrı bir sütuna) yazsın. `Unhealthy` dağıtımı durdurmasın, ama kayda girsin (YAKLASIM §9 madde 1'i kapatır) |
| **Düşük** | Uçtan uca senaryolara 2FA açma ve iki adımlı giriş senaryosu (`KR-104`) |

---

## 2. TEC.9 — Doğrulama

### 2.1 Çıktılar

| # | Çıktı (kısa) | Kanıt (02.10 sonrası) | 02.10 | 13.10 |
|---|---|---|---|---|
| a | Doğrulama kısıtları | YAKLASIM §3: dört yöntem için prosedür, uygunluk ölçütü ve kayıt yeri (#123, PR #144) | F | **F** |
| b | Destekleyici sistemler | Playwright + atılabilir uçtan uca yığın, 7 senaryo, her PR'da (#146, PR #147). Ölçüm yöntemi performans raporunda | L | **F** |
| c | Sistem doğrulandı | `raporlar/2026-10-04-t3-dogrulama-raporu.md` (1.6): **79/79 SYG** için yöntem, kanıt ve sonuç. SYG-060/061/062 gerçekleştirildi ve sınandı (#119, PR #140). SYG-077 düzeltildi ve yeniden ölçüldü (#120, PR #141). 079 e-posta ölçümü tamamlandı (#121, PR #143). **Ancak** rapor `fb58fbe`'yi (04.10) doğruluyor. Kabul edilen `v0.2.0` ise **81 SYG** içeriyor: 080 ve 081 eklendi, 034–036 değişti (#190, PR #191). Bu değişikliklerin doğrulama raporunda satırı yok. SYG-076, raporda "✅ Geçti" iken T5'e ait bir parametreyi listeliyordu. Hata 12.10'da kabul provasında bulundu (#198) | L | **L** |
| d | Düzeltici faaliyet için veri raporlandı | #89 kapandı (PR #145, 04.10). Doğrulama kaçakları issue'ya dönüştü: #137, #171 (mimari testi boş geçiyordu), #182 (kararsız ön yüz testi), #198. **#198, regresyon testi olmadan kapandı** (PR #199'da test dosyası değişikliği yok; yalnızca vaka sayısı 284 → 282 azaldı) | L | **L+** |
| e | Gereksinimi karşıladığına dair nesnel kanıt | SYG başına "yöntem / kanıt / sonuç" tablosu var (rapor §7). CI çalışmasının kalıcı özetleri var (`kayitlar/`). Ancak §7'deki sonuçlar CI çalışması 37197387185'e dayanıyor ve bu çalışmanın kalıcı özeti yok. Kalıcı özetler etiketli commit'lere ait | L | **L+** |
| f | Sonuçlar ve anomaliler belirlendi | `trx` üzerine yazma kusuru giderildi (`LogFilePrefix`, `ci.yml:65–74`). Kalıcı CI özetleri: rc.2 (04.10), **rc.3 (gecikmeli: 10.10 etiketi, 11.10'da #194 ile üretildi)**, rc.4, rc.5. **rc.1'in özeti yok.** Özetlerde **uçtan uca test sonucu yok** (yalnızca backend ve ön yüz birim/bileşen testleri) | L− | **L+** |
| g | Doğrulanan öğelerin izlenebilirliği | CI "her SYG en az bir testte anılır" kuralını denetliyor (#122, PR #142). SYG-080/081 testlerde anılıyor (ör. `AccountsApiTests`, `SessionApiTests`). Matris "Doğrulama" sütunu dolu. Doküman haritasındaki `TS-<MODÜL>-<no>` kimliği hâlâ kullanılmıyor (kabulde KS/KG kullanıldı) | L | **L+** |

**PA 1.1 = L+** (02.10: L).

**CI kanıt özetlerinin denetimi:**

| Sürüm | Commit | CI çalışması | Backend | Ön yüz | Satır kapsamı (birleşik / Domain) | Üretim |
|---|---|---|---|---|---|---|
| rc.1 | `6cfeef0` | — | — | — | — | **Özet yok** |
| rc.2 | `69bafad` | 37229959287 | 770/770 | 233/233 (elle aktarıldı) | %92,9 / %98,9 | 04.10, zamanında |
| rc.3 | `fa3e194` | 38051374806 | 771/771 | 233/233 | %92,9 (6705/7216) / %98,9 | **11.10, gecikmeli (#194)** |
| rc.4 | `353eae7` | 38089012238 | 798/798 | 247/247 | %92,9 (6927/7453) / %98,9 | 11.10 (etiketten ≈17 saat sonra) |
| rc.5 = v0.2.0 | `149f6ee` | 38299593371 | 796/796 | 247/247 | %92,9 (6925/7451) / %98,9 | 12.10, zamanında |

Sayılar tutarlı. rc.3'ün kapsam satır sayıları rc.2 ile aynı (6705/7216); arada yalnızca varsayılan değer değişikliği (#185) ve mimari testi düzeltmesi var, bu yüzden makul. rc.5'te Application testlerinin 284 → 282 azalması PR #199'da açıklanmış. Kapsam eşikleri (genel %75, Domain %90) her adayda karşılanıyor. Dal kapsamında en düşük değer Api %77,0.

### 2.2 Temel uygulamalar
- **BP1.1 Strateji:** YAKLASIM ✔. ADR-0011 "TEC.9 YAKLASIM'da özetlenecek" yükümlülüğü karşılandı.
- **BP1.3 Amaç, koşul, ölçüt:** Analiz, İnceleme ve Gösterim için tanımlı ✔ (02.10'da yoktu).
- **BP3.2 Olay takibi:** İyi. Ancak #198'de "regresyon testi aynı PR'da" kuralı (YAKLASIM §2.1 madde 4) uygulanmadı.
- **BP3.3 Paydaş mutabakatı (G2):** 04.10'da "GEÇTİ" olarak kayıtlı ✔. Ancak G2 yalnızca `fb58fbe` için verildi. G2 sonrasında gelen iki değişiklik talebi yeni uç, yeni sütun ve yeni SYG getirdi. G2'nin yeniden değerlendirilmesi için ne kural ne kayıt var.
- **BP3.5 Baseline:** `v0.2.0` ✔.

### 2.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** G2 ölçütü tanımlı ✔. "Kabul adayı değişirse doğrulama raporu ne olur?" sorusu tanımlı değil.
- **2.1(c)** İzleme ve düzeltme işliyor: rc.3 özetinin atlandığı fark edildi ve #194 ile giderildi ✔ (örnek davranış). Ancak rc.1 boşluğu fark edilmedi. Doğrulama raporu ise 05.10'dan beri güncellenmedi.
- **2.1(g)** `KR-096` onay yorumu CI'da denetleniyor ✔. Bağımsızlık sınırlı: yazan ve onaylayan aynı hesap.
- **2.2(c)** Raporun değişiklik geçmişi örnek nitelikte (1.0–1.6, her biri bir issue'ya bağlı).
- **2.2(e)** Kalıcı özetler ✔. Ancak (1) uçtan uca test sonuçları kalıcı değil (90 gün sonra kaybolur); (2) rc.1 özeti yok; (3) G2'nin dayandığı çalışma 37197387185'in özeti yok.

### 2.4 02.10'a göre değişim ve bulguların durumu

| 02.10 bulgusu / işi | Durum |
|---|---|
| T3-01: SYG-060/061/062 (iş 1) | ✅ #119 → PR #140; raporda "Geçti" |
| T3-02: SYG-077 karşılanmıyor (ana rapor iş 2, 3) | ✅ #120 → PR #141 (p95 0,279 sn); 079 e-posta #121 |
| C-03: SYG → test denetimi (ana rapor iş 4) | ✅ #122 → PR #142 |
| Doğrulama raporu ve G2 (iş 3) | ⚠️ Yazıldı (#123 → PR #144). Kabul edilen sürüme güncellenmedi (YB-01) |
| TEC.9 YAKLASIM (iş 7) | ✅ |
| #89, trx adı, ham kapsam (iş 8) | ✅ #89 → PR #145; `LogFilePrefix` |
| İnceleme kapısı (iş 13, C-04) | ✅ Kısmen: `KR-096`, #128. 65 PR geriye dönük kayda geçti. Bağımsızlık sınırı sürüyor |
| SYG-065 yöntemi, Playwright, `TS-` kimliği (iş 16) | ⚠️ Playwright ✅. SYG-065 yöntemi hâlâ "Test"; gerçek tarayıcı ölçümüyle desteklenmiş ama belge düzeltilmedi. `TS-` kuralı haritada duruyor ama kullanılmıyor |
| CI kanıtlarının kalıcı arşivi (iş 17, C-05) | ✅ #136; ⚠️ rc.1 yok, uçtan uca sonuç yok |

### 2.5 Düzeltici işler
| Öncelik | İş |
|---|---|
| **Yüksek** | **YB-01** Doğrulama raporu 2.0 (`v0.2.0` için): SYG-080 ve 081 satırları; 034–036 yeni metne göre; SYG-076 satırına #198 notu; kanıtı `2026-10-12-v0.2.0-rc.5-ci-kanit-ozeti.md`'ye bağla. G2'yi `rc.5` için yeniden kaydet. TEC.9 YAKLASIM §2.2'ye kural ekle: "G2 sonrasında kabul edilen değişiklik talebi rapora satır ekler; G2 kabul adayı için yeniden kaydedilir" |
| **Orta** | **YB-02** SYG-076 için regresyon testi: katalogdaki her parametrenin T3'e ait olduğunu denetleyen test (modül etiketi veya izin listesi). #198'in regresyon testi olmadan kapandığını TEC.13 kaydına yaz |
| **Orta** | **YB-03** `ci-evidence-summary.mjs`: uçtan uca test sonuçlarını özetle (Playwright JUnit/JSON yapıtı). rc.1 (`6cfeef0`) özetini yapıtlar silinmeden üret ya da "yok" kaydı düş |
| **Düşük** | SYG-077'nin #191 sonrasında (giriş yolu 2FA tercihini okuyor) kısa bir yeniden ölçümü veya "etkisiz" gerekçesi. SYG-065 yöntem satırının düzeltilmesi; `TS-` kuralının haritadan çıkarılması ya da KS/KG ile eşlenmesi |

---

## 3. TEC.10 — Geçiş

### 3.1 Çıktılar

| # | Çıktı (kısa) | Kanıt (02.10 sonrası) | 02.10 | 13.10 |
|---|---|---|---|---|
| a | Geçiş kısıtları | YAKLASIM §3 (#130): sekiz kısıt ve kaynağı. `KR-097` (etiketli sürüm), `KR-098` (UAT yedeği), `KR-102` (SSH) | F | **F** |
| b | Destekleyici sistemler | NTP, TLS (bitiş 16.12.2026, sunucuda doğrulandı), sır dizini, SSH anahtarı (#174). SSH parola girişi **açık**: `sshd -T` → `passwordauthentication yes`, `permitrootlogin yes`. Bu, bilinçli bir karar (`KR-102`); risk Üst Yönetim tarafından kabul edildi (R-25, `KR-103`). Webmin kaldırıldı (#179) | L | **L+** |
| c | Saha hazırlandı | Runbook 1.7–1.10: sürüm seçimi, `git archive`, imaj etiketi, dağıtım kaydı, veri sınıfı, yedek kuralı, HTTPS doğrulama komutları | F | **F** |
| d | Kurulu sistem işlevlerini sunabiliyor | `v0.2.0-rc.5` dağıtım betiği öz denetiminden geçti (12.10 19:51 UTC). İK kabulü bu kurulumda yapıldı. 13.10'da konteynerler `healthy`, iki bildirim kanalı `Healthy` | L− | **F** |
| e | Operatör ve kullanıcı hazırlığı | Operatör: runbook. Kullanıcı: `TEC.11-gecerleme/T3-kullanim-notu.md` (#154). Notun katılımcılara verildiğinin kanıtı yalnızca beyan ("Doğuş Uçanok bildirdi, 13.10"). Üretim eğitimi ve kılavuz yok; sırası gelmedi | N | **L−** |
| f | Sonuçlar ve anomaliler | `kayitlar/uat-dagitim-kaydi.md`: 5 satır (rc.1–rc.5). **Sunucudaki `deployments.log` ile satır satır aynı** (zaman, sürüm, tam SHA, dağıtan). UAT hesap temizliği kaydı (`2026-10-10-uat-hesap-temizligi.md`, #187): kapsam, deneme ve geri alma yöntemi, silinmeyenler, yedek kararı. Geçiş raporu yok (UAT için gerekli değil) | L− | **L** |
| g | Etkin ve işletime hazır | UAT için evet. Üretim için sırası gelmedi | L | **L+** |
| h | Geçişi yapılan öğelerin izlenebilirliği | Etiket → dağıtım kaydı → OCI etiketi → kabul formu (`rc.5`, `149f6ee`). Zincir canlıda doğrulandı. **Boşluklar:** `v0.2.0` dağıtım kaydında anılmıyor (sunucuda `VERSION` = `rc.5`). `v0.2.0` etiketinin ağacında kabul formu ve CHANGELOG `[0.2.0]` bölümü yok; ikisi de sonraki commit'te (`d354e94`). Etiket iletisi ise kabul formuna atıf yapıyor | P | **L+** |

**PA 1.1 = L+** (02.10: L−). Dağıtım kaydı ile sunucu kaydı arasında tutarsızlık bulunmadı.

### 3.2 Temel uygulamalar
- **BP1.1 Sürüm stratejisi:** `KR-097` beş adayda da uygulandı. Her aday için CHANGELOG bölümü var ✔ (02.10'daki en büyük gerileme kapandı).
- **BP1.4 Geçiş planı:** Kabul planı §3–§4 sürüm ve takvim bağını taşıyor ✔.
- **BP2.5 Etkinleştirme ve kontrol:** Betik `revision` eşleşmesini denetliyor ✔.
- **BP3.2 Olay takibi:** #73 kapandı (04.10).
- **BP3.4 Baseline:** `v0.2.0` ✔.
- **Geri dönüş yordamı:** Hâlâ yok (YAKLASIM §8). rc.1 → rc.5 arasında beş kurulum yapıldı, ama hiçbirinde geri dönüş gerekmedi; yordam sınanmadı.

### 3.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)(c)** Proje planı §3 adım 8 (etiket → UAT) uygulanıyor ✔. Depodaki dağıtım kaydı PR ile ve gecikmeli işleniyor: rc.4 dağıtımı 10.10 21:56'da yapıldı, kaydı 11.10'da işlendi (#192). Sunucu kaydı ise anlık.
- **2.1(b)** R-25 Üst Yönetim kabulü (`KR-103`) ✔. SSH parola girişi kararı yazılı ✔.
- **2.1(g)** Hesap temizliği İK'nın bildirimine dayanıyor; İK tarafında kayıt yok (düşük önem).
- **2.2(c)** Runbook çelişkileri giderildi (1.8, 1.9) ✔. Ancak **ADR-0011 §7 hâlâ "UAT: gerçek veriden maskelenmiş kopya"** diyor. Bu ifade runbook §1, R-25 ve `KR-103` ile çelişiyor. `KR-099`'a göre kararı tersine çeviren değişiklik yeni ADR ister; yazılmadı.
- **2.2(c)** YAKLASIM §8 bayat: runbook §4/§6'nın bayat olduğunu yazıyor (06.10'da düzeltildi). SSH satırı `KR-102`'ye atıf yapmıyor.

### 3.4 02.10'a göre değişim ve bulguların durumu

| 02.10 bulgusu / işi | Durum |
|---|---|
| T3-04: etiketsiz UAT dağıtımı, dağıtım kaydı yok (iş 5) | ✅ #125 → PR #156; rc.1–rc.5 kayıtlı ve canlıda doğrulandı |
| CHANGELOG T3 girişi | ✅ `[0.2.0-rc.1]…[0.2.0]` |
| Runbook ve ADR-0011 §7 tutarlılığı (iş 9) | ⚠️ Runbook ✅ (#133, #177). ADR-0011 §7 ❌ |
| #73 ve SSH parola girişi (iş 10) | ✅ #73 kapandı. SSH: karar ile kapandı (`KR-102`, risk kabulü `KR-103`) |
| Kullanım notu (iş 11) | ✅ #154 |
| TEC.10 YAKLASIM, geri dönüş adımı | ⚠️ YAKLASIM ✅ (#130). Geri dönüş yordamı ❌ |

### 3.5 Düzeltici işler
| Öncelik | İş |
|---|---|
| **Orta** | **YB-11** ADR-0011 §7 için yeni ADR (veya `KR-099`'a göre gerekçeli ek): UAT veri sınıfı "gerçek veri, R-25, `KR-103`" |
| **Orta** | Geri dönüş yordamı (runbook): önceki etiketten kurulum, şema betiğinin geri almadığı durum ve doğrulama. Bir kez UAT'de sınanır ve kayda geçer |
| **Düşük** | **YB-12** Dağıtım kaydına not: "`v0.2.0` = `149f6ee`, 12.10.2026 19:51:46Z kurulumu; yeniden kurulum yapılmadı (`KR-105`)". MAN.5 ilk konfigürasyon denetimi G-1 maddesinin `v0.2.0` için tekrarı |
| **Düşük** | TEC.10 YAKLASIM §8 güncellemesi (runbook satırı kaldırılır; SSH → `KR-102`) |

---

## 4. TEC.11 — Geçerleme

### 4.1 Çıktılar

| # | Çıktı (kısa) | Kanıt (02.10 sonrası) | 02.10 | 13.10 |
|---|---|---|---|---|
| a | Geçerleme kriterleri | PG-KMLK kabul kriterleri + `T3-kabul-plani.md` (0.1–0.17): 11 senaryo (KS), 10 kanıt gösterimi (KG), §8 karar kuralı. Değişiklik talepleri (#185, #190) plana T-03 ve T-04 olarak işlendi | F | **F** |
| b | Paydaşın ihtiyaç duyduğu hizmetlerin varlığı teyit edildi | 13.10.2026 kabul oturumu: KS-01…KS-11 "Geçti", İK kullanıcısı ve dört personel kendi bilgileriyle uyguladı. **Sınır:** dört personel katılımcının **dördü de Bilgi İşlem'den**. Plan (`KR-048`) "İK dışından" diyor ve bu harfiyen karşılandı. Ancak katılımcılar geliştiren birimin çalışanları; son kullanıcıyı temsil gücü düşük. Kabul raporu §2 bunu kendisi yazıyor | N | **L+** |
| c | Geçerleme kısıtları | Plan §2–§3: izin listesi, cihazlar, katılımcı özellikleri, AN-01. Ekrandan denenemeyenler gerekçesiyle KG'ye taşındı (#196) | P+ | **F** |
| d | Sistem geçerlendi | Kabul oturumu (13.10), karar Kabul (`KR-105`). **56 REQ'nin 15'i yalnızca kanıt gösterimiyle kabul edildi** (003, 013, 015, 018, 019, 033, 034, 035, 038, 039, 040, 041, 042, 053, 054). Bunlarda Bilgi İşlem otomatik testi veya veritabanı görünümünü gösterdi; İK inceledi. Yani bu 15 kalemde geçerleme fiilen doğrulama kanıtının paydaşa sunulmasıdır. REQ-042'nin işlevi (bildirim istisnası) henüz yok (KG-06: "istisnanın kendisi Y1'de"). REQ-054 ekranda denenemedi. İkisi de matriste "Kabul edildi"; yalnızca 054'te erteleme notu var | N | **L+** |
| e | Destekleyici sistemler | UAT, izin listesi (BK-22), iki kanal `Healthy`. Kabul formu şablonu `docs/sablonlar/kabul-formu.md` | P+ | **F** |
| f | Sonuçlar ve anomaliler | Form §2–§5: senaryo başına sonuç, KG incelemesi, AN-01, T-01…T-04, gözlem. Prova bulguları kayıtlı (#196, #198). **Zayıflık:** Plan §2 Bilgi İşlem'in "gözlem notu tuttuğunu" ve §5 "takılırsa not düşülür" diyor. Formda senaryo başına gözlem yok; tek gözlem İK'nın genel değerlendirmesi. Adım sonucu, süre ve cihaz bilgisi yok | N | **L** |
| g | Paydaş ihtiyaçlarının karşılandığına dair nesnel kanıt | Kanıtlar: depo kaydı (`kayitlar/2026-10-13-t3-kabul-formu.md`), `KR-105`, kabul raporu, CI özeti. **Ama paydaşın onayı (ratification) bağımsız olarak doğrulanabilir değil:** (1) Form imzasız. İmzalar kalite belgeleriyle birlikte daha sonra toplanacak (kullanıcı kararı, 13.10). (2) Asıl nüsha tek kopya, bir geliştirici makinesinin Masaüstü'nde (`BK-23`); saklama yeri belirlenmemiş (`BK-24`, `Ö-3`: "kabulden önce belirlenmeliydi"). (3) Depo kaydını, kabul edilen sistemi geliştiren Bilgi İşlem yazdı; Word dosyasının bütünlük özeti (hash) kayıtlı değil. (4) Formun imza sütununda **"İmzalı nüsha: BK-24" yazıyor, oysa imzalı nüsha yok.** Bu ifade olgu değil, gelecek beyanı | N | **L−** |
| h | Geçerlenen öğelerin izlenebilirliği | Matris §5 "Kabul" sütunu 56/56 dolu; her hücrede KS/KG kimliği, tarih, sürüm ve form yolu var (2.12). §6 T3 ölçümü %100 | N | **F** |

**PA 1.1 = L+** (02.10: P). Süreç ilk kez uçtan uca işletildi. F'yi engelleyenler: (g) imzasız ve tek kopya kanıt; (b/d) temsil ve kanıt gösterimi oranı.

**İmza ertelemesinin "nesnel kanıt"a etkisi:** TS 33061 TEC.11 amacı "geçerleme paydaşlarca onaylanır" diyor; BP3.3 "paydaş mutabakatı alınır" diyor. Mutabakatın **olduğu** tartışmalı değil: KR-105, katılımcı listesi ve oturumda doldurulan form tutarlı. Sorun, mutabakatın **dış gözle doğrulanabilirliği**. Bugün bunu taşıyan tek şey Bilgi İşlem'in kendi kaydı. Bu yüzden (g) **L−** derecelendirildi. İmzalı ve taranmış nüsha BK-24'e konduğunda ve depo kaydı ona bağlandığında (g) F olur. **Bir sonraki denetime kadar imza toplanmazsa (g) P+'ya düşürülmelidir.** Bu, kararın kendisine itiraz değil; bekleme süresinin kanıt riskidir (tek kopya, bütünlük özeti yok).

### 4.2 Temel uygulamalar
- **BP1.1 Strateji:** YAKLASIM ✔ ve kabul planı ✔.
- **BP1.3 Amaç, koşul, ölçüt:** Senaryo düzeyinde "Beklenen" ve "Kapsadığı gereksinimler" ✔.
- **BP2.1 Prosedürler:** KS-01…KS-11 ✔. Prova (11–12.10) planın kendisini düzeltti (#196): KS-05'teki deneme sayısı onaylı gereksinime göre düzeltildi. Örnek uygulama.
- **BP3.2 Olay kaydı:** Kabulde `[HATA]` yok. Provada bulunanlar issue oldu ✔.
- **BP3.3 Paydaş mutabakatı:** Var, imzasız (yukarıda).
- **BP3.5 Baseline:** `v0.2.0` ✔.

### 4.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** G3 karar kuralı ve giriş ölçütleri yazılı; giriş ölçütlerinin her biri tarihle işaretlenmiş ✔.
- **2.1(c)** Plan 17 sürümde izlenip düzeltildi; aday değişimleri (rc.1 → rc.5) plana yansıdı ✔. Güçlü.
- **2.1(d)** Katılımcı seçimi İK'nın. Formda rol ve unvan var ✔.
- **2.1(g)** İK arayüzü: Değişiklik taleplerinin İK onayı (#185, #190; 10.10) yalnızca "Doğuş Uçanok bildirdi" diye kayıtlı. İK tarafında kayıt yok. Bu boşluk kabulde T-03/T-04 "Uygun" ile **sonradan teyit edildi** ✔. **`KR-106`:** Plan §3 "katılımcılar oturumdan sonra izin listesinden çıkarılır" diyordu. Bu hüküm, Bilgi İşlem kararıyla uygulanmadı. Liste kişisel iletişim verisi içeriyor (BK-22, "Gizli"); kararda İK'nın veya veri sorumlusunun görüşü yok.
- **2.2(a)(b)** YAKLASIM §6 ✔. Ancak bayat: §5 "Etiketli sürüm ve dağıtım kaydı: #125 ile kurulacak". §2 adım 5 ve §6 "imzalı kabul formu" diyor; imzanın ertelendiği belgeye işlenmedi.
- **2.2(d)** Planın İK tarafından gözden geçirildiğine dair (YAKLASIM §6 "İK gözden geçirir") kayıt yok.
- **2.2(e)** Asıl nüshanın yeri belirsiz (`Ö-3`), tek kopya, Masaüstü.

### 4.4 02.10'a göre değişim ve bulguların durumu

| 02.10 bulgusu / işi | Durum |
|---|---|
| T3-03: kabul planı yok (iş 4) | ✅ #124 → PR #151 |
| Kabul senaryoları ve matris "Kabul" sütunu | ✅ KS/KG; 56/56 |
| Kabul formu şablonu ve kabul raporu şablonu | ✅ `docs/sablonlar/kabul-formu.md`; rapor yazıldı |
| `surec:TEC.11` etiketi hiç kullanılmamış | ✅ 3 issue, 3 PR |
| T3-10: AN-23 ve AN-24 İK teyidi | ✅ #135 → T-01/T-02, kabulde "Uygun" |
| E-posta kanalı (e) | ✅ |

**P → L+ (PA 1.1); Seviye 0 → 1.** Bu beş süreç içinde en büyük ilerleme TEC.11'de.

### 4.5 Düzeltici işler
| Öncelik | İş |
|---|---|
| **Yüksek** | **YB-04** Kabul kanıtının korunması: (1) Word formun SHA-256 özetini depo kaydına yaz. (2) İkinci kopyayı kurum içi güvenli konuma koy; `Ö-3`'ü kapat. (3) Formdaki "İmzalı nüsha: BK-24" ifadesini "İmza bekleniyor — `KR-105` (4), `Ö-3`" olarak düzelt. (4) İmza toplama için hedef tarihli issue aç (kalite belgeleri onayına bağlı) |
| **Orta** | **YB-05** Matris REQ-KMLK-042 "Kabul" hücresine not: "İstisna mekanizması Y1'de; T3 kabulü, iletilerin muafiyet amacı taşıdığının gösterimidir; Y1 kabulünde senaryo olarak yeniden denenir". TEC.11 YAKLASIM §3'e kural: kanıt gösterimi yalnızca arayüzden gözlenemeyen davranış için kullanılır ve kabul raporunda oranı yazılır |
| **Orta** | **YB-05** Katılımcı seçim kuralı (YAKLASIM §2): personel katılımcılardan en az biri Bilgi İşlem dışındaki bir kullanıcı biriminden. Kabul raporu §6 önerisinin kurala dönüşmesi |
| **Orta** | **YB-14** `KR-106` için İK onayı ve BK-22 saklama süresinin somutlaştırılması (kişisel iletişim verisi) |
| **Düşük** | **YB-06** Kabul formu şablonuna "gözlem / takıldığı adım / cihaz" sütunu. YAKLASIM §5 ve §6'nın güncellenmesi |

---

## 5. TEC.13 — Bakım

### 5.1 Çıktılar

| # | Çıktı (kısa) | Kanıt (02.10 sonrası) | 02.10 | 13.10 |
|---|---|---|---|---|
| a | Bakım kısıtları | YAKLASIM §1, §2.4 (#130): bakım türleri, önleyici denetimler, `KR-065/067/098`. Üretim için bakım stratejisi ve destek modeli yok (YAKLASIM §7; sırası kısmen gelmedi) | P+ | **L** |
| b | Destekleyici sistemler | `03-bug.yml` (`tur:hata`, `surec:TEC.13`), `main-failure-check.yml` (#129, PR #165), `npm audit` kapısı (#149). **Ancak:** `main-failure-check` 05.10'da kendi işini yapamadı (aşağıda YB-08) | L | **L** |
| c | Onarılmış/revize öğeler sağlandı | Ayrı PR'la ve çoğu aynı gün: #137 → PR #139, #138 → PR #159, #149 → PR #150, #171 → PR #173, #175 → PR #176, #182 → PR #183, #198 → PR #199. Düzeltmeler yeni kabul adayıyla UAT'ye taşındı (rc.2, rc.5). #198'de regresyon testi yok | L | **L+** |
| d | Değişiklik ihtiyacı raporlandı | 02.10–13.10 arasında 8 `[HATA]`, 2 `[DEĞİŞİKLİK]` (#185, #190), 3 `[DÜZELTİCİ]` kaydı. Kayıtların çoğu belirti, neden ve etki taşıyor (#171, #175, #182, #198). Geriye dönük olay kayıtları O-1…O-4 (`kayitlar/2026-10-04-t3-olay-kayitlari.md`). **Kayda girmeyen olay:** 05.10.2026 20:19–20:55 UTC arasında `main`'e gelen dört commit (`2592fab`, `3f6833d`, `c6a9c86`, `90a25a9`). Bu commit'lerin CI çalışmaları (37369047969, 37369395541, 37370158991, 37370762263) işler adım çalıştırmadan iptal edildiği için "failure" ile bitti. Dal koruma denetimi üç commit'te çalışmadı. `main-failure-check` yedi kez tetiklendi (`c6a9c86` için 2, `90a25a9` için 5); yedisinde de işi adım çalıştırmadan iptal edildi ve issue açılmadı. Sonraki yeşil `main` 06.10 09:52 (`e8dfc87`). Değişen içerik yalnızca belgeydi, ürün riski düşük. Ancak bu, T3-07'deki "olay kayda geçmedi" bulgusunun yeni kuralın ilk tetiklenmesinde tekrarıdır | L | **L** |
| e | Arıza ve ömür verisi (maliyet dâhil) | Hâlâ derlenmedi (YAKLASIM §7 açıkça yazıyor). O-1…O-4'te süre bilgisi var (ör. O-2: ≈21 saat). Hata sayısı, kaynağı, kapanma süresi ve kaçak analizi yok. `raporlar/` boş | N | **P−** |

**PA 1.1 = L−** (02.10: P+). Seviye 1'e geçiş, (a)–(d)'nin L bandına girmesinden geliyor.

**Ham veri (rapor için hazır):** 02.10–13.10 arasındaki `[HATA]` kayıtları #120, #137, #138, #149, #171, #175, #182, #198. Hepsi açıldığı gün ya da ertesi gün kapandı. Bulunduğu yer: UAT kullanımı 2 (#137, #138), belge yazımı 1 (#171), CI kapısı 3 (#149, #175, #182), UAT ölçümü 1 (#120), kabul provası 1 (#198). **Doğrulama kaçağı** sayılabilecekler (testler geçerken hata ürünün içindeydi): #137, #138, #171, #198.

### 5.2 Temel uygulamalar
- **BP1.1 Strateji:** YAKLASIM, UAT dönemi için yeterli. Üretim için yok.
- **BP2.1 Olay raporlarının gözden geçirilmesi:** Issue bazında ✔. Dönemsel gözden geçirme yok.
- **BP2.5 Önleyici bakım:** CI'da üç açık taraması ✔. Sertifika yenilemesi (#179) ✔. Zamanlanmış tarama yok (YAKLASIM §7).
- **BP4.1 Kayıt:** ✔ (YB-08 hariç).
- **BP4.2 Eğilim:** ✗.
- **BP4.3 İzlenebilirlik:** Hata → PR → CHANGELOG "Düzeltildi" ✔. **Etiket kuralı ihlal ediliyor:** YAKLASIM §5 "tüm `[HATA]` kayıtları `surec:TEC.13` taşır" diyor. 05.10 sonrasında açılan `[HATA]`'ların üçü bu etikete sahip değil: #171 (`surec:TEC.5`), #182 (`surec:TEC.9`), #198 (`surec:TEC.7`, `tur:hata` da yok). #185 ve #190'da süreç etiketi yok.

### 5.3 PA 2.1 / PA 2.2 boşlukları
- **2.1(a)** Hedef çözüm süresi yok (YAKLASIM §7) → (c) "değerlendirilir" yapılamıyor.
- **2.1(b)** Ortam riskleri R-18, R-20 ve R-21 ✔.
- **2.1(c)** Otomatik kayıt mekanizması kendi başarısızlığını izlemiyor (YB-08). Ayrıca #198'de "regresyon testi" kuralının atlanması hiçbir denetime takılmadı.
- **2.1(d)** Roller yazılı ✔ (tek kişi, R-05).
- **2.2(a)(b)** YAKLASIM ✔. Etiket kuralı uygulanmıyor (yukarıda).
- **2.2(e)** Olay kayıtları depoda ✔. Bakım raporu yok.

### 5.4 02.10'a göre değişim ve bulguların durumu

| 02.10 bulgusu / işi | Durum |
|---|---|
| T3-07: olaylar kayda geçmedi (ana rapor iş 11) | ⚠️ O-1…O-4 geriye dönük kaydedildi; `main` kırmızı kuralı kuruldu (#129). **05.10 olayı yine kayda girmedi** (YB-08) |
| TEC.13 YAKLASIM ve etiket kuralı (iş 14) | ⚠️ YAKLASIM ✅. Etiket kuralı yazıldı ama uygulanmıyor (YB-09) |
| T3 dönemi bakım/olay raporu (iş 15) | ❌ Yapılmadı (YB-10) |
| CVE ve SMTP için geriye dönük kayıt (Ek C §5.5) | ✅ O-2, O-4 |
| Düzeltmenin ayrı PR'da yapılması | ✅ 7/7 düzeltme ayrı PR |

### 5.5 Düzeltici işler
| Öncelik | İş |
|---|---|
| **Orta** | **YB-08** O-5 olay kaydı (05.10 iptal edilen `main` çalışmaları). Nedenin araştırılması (Actions kotası veya kesinti). Önlem: (1) `main`'de iptal edilen CI yeniden çalıştırılır; (2) "son `main` commit'inin CI'ı yeşil mi" denetimi ya zamanlanmış bir iş akışıyla ya da sonraki PR'ın kapısında yapılır. Böylece `main-failure-check`'in kendisi düştüğünde boşluk kalmaz |
| **Orta** | **YB-10** `TEC.13/raporlar/2026-10-xx-t3-bakim-raporu.md`: 02.10–13.10 dönemindeki 8 hata (kaynak, kapanma süresi, kök neden sınıfı), doğrulama kaçakları (#137, #138, #171, #198), O-1…O-5. Öncelik başına hedef çözüm süresi önerisi |
| **Düşük** | **YB-09** `pr-traceability-check`'e kural: başlığı `[HATA]` olan issue'yu kapatan PR'da issue'nun `tur:hata` + `surec:TEC.13` taşıması ve test dosyası değişikliği (yoksa gerekçe). #171, #182 ve #198'in etiketleri düzeltilir |

---

## 6. 02.10 Ek C §7 iş listesinin kapanış durumu (bu beş süreç)

| # | İş | Issue / PR | Durum |
|---|---|---|---|
| 1 | SYG-060/061/062 | #119 / #140 | ✅ |
| 2 | SMTP sertifikası | #179 / #180 (+01.10 sunucu düzeltmesi) | ✅ |
| 3 | Doğrulama raporu ve G2 | #123 / #144 | ⚠️ `rc.5`'e güncellenmedi (YB-01) |
| 4 | Kabul planı, senaryolar, form şablonu | #124 / #151 | ✅ |
| 5 | Etiket, etiketten dağıtım, dağıtım kaydı | #125 / #156 | ✅ |
| 6 | TEC.8 yaklaşımı ve entegrasyon raporu | #130, #134 / #172, #168 | ⚠️ Rapor `rc.2`'de (YB-07) |
| 7 | TEC.9 yaklaşımı | #123 | ✅ |
| 8 | #89, trx, ham kapsam | #89 / #145 | ✅ |
| 9 | Runbook ve ADR-0011 tutarlılığı | #133, #177 | ⚠️ ADR-0011 §7 açık (YB-11) |
| 10 | #73, SSH parola girişi | #73; #174, `KR-102` | ✅ (SSH karar ile) |
| 11 | Kullanım notu | #154 | ✅ |
| 12 | Risk defteri T3 | #126 / #164 | ✅ |
| 13 | İnceleme kapısı | #128 / #148, `KR-096` | ✅ (bağımsızlık sınırı sürüyor) |
| 14 | TEC.13 yaklaşımı ve etiket kuralı | #130 | ⚠️ Kural uygulanmıyor (YB-09) |
| 15 | T3 olay/bakım raporu | — | ❌ (YB-10) |
| 16 | SYG-065 yöntemi, Playwright, `TS-` | #146 | ⚠️ Yalnızca Playwright |
| 17 | CI kanıtlarının kalıcı arşivi | #136 / #167 | ✅ (rc.1 ve uçtan uca eksik, YB-03) |

**17 işin 11'i tam, 5'i kısmen kapandı, 1'i açık.** Yüksek öncelikli beş işin beşi de issue'ya dönüştü ve kabulden önce kapandı. Bu, 18.09 → 02.10 dönemindeki "bulgular issue'ya çevrilmedi" kök nedeninin giderildiğini gösteriyor.

---

## 7. Yeni bulgular (13.10) — önceliklendirilmiş

| No | Bulgu | Süreç / öznitelik | Önem | Önerilen iş (issue boyutu) |
|---|---|---|---|---|
| YB-01 | Doğrulama raporu ve G2 kaydı `fb58fbe`'yi (79 SYG) kapsıyor; kabul edilen `v0.2.0` 81 SYG içeriyor, 034–036 değişti; G2 yeniden değerlendirilmedi | TEC.9 c, BP3.3; PA 2.1(a) | **Yüksek** | Rapor 2.0 + G2'nin `rc.5` için kaydı + YAKLASIM §2.2 kuralı (§2.5) |
| YB-04 | Kabul kanıtı imzasız, tek kopya, bütünlük özeti yok; formda imzalı nüsha varmış gibi ifade | TEC.11 g; PA 2.2(e) | **Yüksek** | SHA-256, ikinci kopya, ifade düzeltmesi, imza issue'su (§4.5) |
| YB-02 | #198: SYG-076 doğrulanmış sayılırken hatalıydı; düzeltme regresyon testi olmadan kapandı | TEC.9 c–d; TEC.13 c | Orta | Katalog kapsam testi + kural ihlali kaydı |
| YB-03 | CI kanıt özetlerinde uçtan uca sonuç yok; rc.1 özeti yok; rc.3 gecikmeli | TEC.9 f; PA 2.2(e) | Orta | `ci-evidence-summary.mjs` genişletme; rc.1 özeti |
| YB-05 | Kabulde 15/56 REQ yalnızca kanıt gösterimiyle; REQ-042 işlevsiz kabul; personel katılımcıların tamamı Bilgi İşlem'den | TEC.11 b, d | Orta | Matris notu; YAKLASIM katılımcı ve kanıt gösterimi kuralı |
| YB-07 | Entegrasyon raporu `rc.2`'de kaldı; iç tutarsızlık; canlı arayüz denetimi kayda girmiyor | TEC.8 f, g, h | Orta | Rapor 1.2; `deploy-uat.sh` sağlık çıktısını kaydeder |
| YB-08 | 05.10 `main` CI çalışmaları iptal; `main-failure-check` da iptal; olay kaydı yok | TEC.13 b, d; TEC.8 PA 2.1(c) | Orta | O-5 kaydı; izleyiciyi izleyen denetim |
| YB-14 | `KR-106`: kişisel iletişim verisi içeren izin listesi plana aykırı olarak korunuyor; karar tek taraflı | TEC.11 PA 2.1(g); MAN.6 | Orta | İK onayı; BK-22 saklama süresi |
| YB-06 | Kabul formunda senaryo başına gözlem yok (plan istiyordu) | TEC.11 f | Düşük | Şablona gözlem sütunu |
| YB-09 | `[HATA]` etiket kuralı, kural yazıldıktan (05.10) sonra açılan 4 kaydın 3'ünde uygulanmadı | TEC.13 BP4.3; PA 2.2(b) | Düşük | CI kuralı; etiket düzeltmesi |
| YB-10 | Bakım/eğilim raporu yok (02.10 iş 15) | TEC.13 e | Düşük | T3 bakım raporu |
| YB-11 | Bayat belgeler: ADR-0011 §7 ve §8; TEC.10 YAKLASIM §8; TEC.11 YAKLASIM §5–§6; entegrasyon raporu girişi; matris başlığı "Son güncelleme 2026-10-03" (içerik 2.12, 13.10) | PA 2.2(c) ×4 | Düşük–Orta | Tek "belge tutarlılığı" issue'su + ADR-0011 için yeni ADR |
| YB-12 | `v0.2.0` dağıtım kaydında yok; etiket ağacında kabul formu yok | TEC.10 h; MAN.5 | Düşük | Dağıtım kaydına not; konfigürasyon denetimi G-1 |
| YB-13 | SYG-077, giriş yolunu değiştiren #191'den sonra yeniden ölçülmedi | TEC.9 c | Düşük | Kısa ölçüm veya etkisizlik gerekçesi |

**Seviye 2 için ne gerekiyor?** Beş sürecin de PA 2.1 ve PA 2.2'si L bandında ya da ona yakın. Sınır PA 1.1 = F. Bunun için YB-01, YB-04, YB-07 ve YB-10 kapanmalı. Ayrıca bir sonraki modülde (T4) kayıtların **kabul edilen sürüme kadar** sürdürülmesi gerekiyor: doğrulama raporu, entegrasyon raporu ve CI özeti son adayla eşleşmeli.

---

## 8. Güçlü yönler (denge için)

1. **Bulgu → issue → kapanış döngüsü çalıştı.** 02.10 raporunun ertesi günü (03.10) 20 issue açıldı (#119–#138). Yüksek öncelikli işlerin tamamı kabulden önce kapandı.
2. **Sürüm disiplini oturdu.** Beş kabul adayı, her biri etiketli, CHANGELOG bölümlü ve kalıcı CI özetli; UAT'de çalışan commit, kayıtla birebir aynı (13.10'da canlı doğrulandı).
3. **Kabul provası işe yaradı.** Bilgi İşlem provası hem planı onaylı gereksinime göre düzeltti (KS-05, #196) hem de ürün hatası buldu (#198). Kabul oturumu bu yüzden hatasız geçti.
4. **Değişiklik talepleri kurallı işlendi.** #185 ve #190 önce PG, SYG, matris, karar defteri ve kabul planına işlendi, sonra kodlandı. Kabulde İK'ya ayrıca teyit ettirildi (T-03, T-04).
5. **Öz düzeltme görünür.** rc.3 CI özetinin atlandığı #194 ile, mimari testinin boş geçtiği #171 ile kayda geçirildi. Doğrulama raporu da geriye dönük not aldı (1.6).
6. **Hesap temizliği kaydı örnek nitelikte:** deneme işlem + geri alma, sayı karşılaştırması, silinmeyenlerin gerekçesi.

---

## Ek — Başlıca kanıt kaynakları

- `docs/33061/TEC.8-entegrasyon/YAKLASIM.md` (1.0), `raporlar/2026-10-05-t3-entegrasyon-raporu.md` (1.1)
- `docs/33061/TEC.9-dogrulama/YAKLASIM.md` (1.2), `raporlar/2026-10-04-t3-dogrulama-raporu.md` (1.6), `raporlar/2026-10-02-t3-performans-olcumu.md`, `kayitlar/2026-10-04|11|11|12-v0.2.0-rc.{2,3,4,5}-ci-kanit-ozeti.md`
- `docs/33061/TEC.10-gecis/YAKLASIM.md` (1.0), `kayitlar/uat-dagitim-kaydi.md`, `kayitlar/2026-10-10-uat-hesap-temizligi.md`, `uat-ortami-kurulum-runbook.md` (1.10)
- `docs/33061/TEC.11-gecerleme/YAKLASIM.md` (1.1), `T3-kabul-plani.md` (0.17), `T3-kullanim-notu.md`, `kayitlar/2026-10-13-t3-kabul-formu.md`, `raporlar/2026-10-13-t3-kabul-raporu.md`
- `docs/33061/TEC.13-bakim/YAKLASIM.md` (1.0), `kayitlar/2026-10-04-t3-olay-kayitlari.md` (1.2)
- `docs/33061/izlenebilirlik-matrisi.md` (2.12) §5 "Kabul", §6; `docs/karar-kayit-defteri.md` `KR-096…KR-106`; `docs/33061/MAN.6-bilgi-yonetimi/bilgi-kayit-defteri.md` BK-22…BK-24, Ö-3; `docs/adr/ADR-0011-test-stratejisi.md` §7–§8; `CHANGELOG.md` `[0.2.0]…[0.2.0-rc.1]`
- Etiketler: `v0.2.0-rc.1` `6cfeef0`, `rc.2` `69bafad`, `rc.3` `fa3e194`, `rc.4` `353eae7`, `rc.5` = `v0.2.0` `149f6ee`
- Issue'lar: #119–#138, #146, #149, #154, #157, #160, #171, #174, #175, #179, #182, #185, #187, #190, #192, #194, #196, #198, #200, #202, #204. PR'lar: #140–#145, #147, #151, #156, #159, #164–#168, #172, #173, #176, #180, #191, #199, #201, #203, #205
- Actions: 37197387185 (G2), 37229959287 (rc.2), 38051374806 (rc.3), 38089012238 (rc.4), 38299593371 (rc.5); 05.10 iptal edilen `main` çalışmaları 37369047969, 37369395541, 37370158991, 37370762263; `main kirmizi denetimi` 37370628312, 37370630134, 37370971834, 37371140179, 37372019789, 37372229941, 37372670226
- UAT (salt okunur, 13.10.2026 18:12 UTC): `deployments.log` 5 satır (rc.1–rc.5, depo kaydıyla aynı); `VERSION` `v0.2.0-rc.5 149f6ee…`; `dsg-hrms-uat-api` ve `-web` OCI `revision` `149f6ee…`, `healthy`; `/health/notifications` email ve sms `Healthy`; TLS `notAfter` 16.12.2026; `sshd -T`: `passwordauthentication yes` (`KR-102`)

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-13 | 1.0 | İlk sürüm: T3 modül sonu süreç denetimi, Ek C | Bilgi İşlem |
