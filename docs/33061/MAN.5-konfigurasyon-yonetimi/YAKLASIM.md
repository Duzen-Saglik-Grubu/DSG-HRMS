# MAN.5 — Süreç Yaklaşımı

**Belge kimliği:** MAN.5-YAK
**Süreç:** MAN.5 — Konfigürasyon Yönetimi
**Son güncelleme:** 2026-10-15
**Karşıladığı öznitelik maddeleri:** `PA 2.1`: amaç, planlama, sorumluluk, kaynak ve arayüzler (§1–§4). `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir (§5). `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir (§6).

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Ayrıntılı kurallar
> `CONTRIBUTING.md`'dedir; bu belge onları süreç adımlarına bağlar.

---

## 1. Sürecin amacı ve sınırı

Konfigürasyon yönetimi, sistemi oluşturan öğeleri ve onların sürümlerini yaşam döngüsü boyunca kontrol altında tutar. Temel soru şudur: **"UAT'de (ileride üretimde) çalışan şey tam olarak hangi kod, hangi ayar ve hangi belgedir?"** (TS ISO/IEC TS 33061, MAN.5).

| Bu süreç | Komşu süreç |
|---|---|
| Neyin kontrol altında olduğu, baseline, sürüm etiketi, değişiklik yolu | Dağıtımın kendisi ve UAT işletimi: TEC.10 |
| Belgelerin de sürüm kontrolünde tutulması | Bilginin saklanması, gizliliği ve imhası: MAN.6 |
| Kontrolün işlediğinin denetimi (konfigürasyon denetimi) | Süreçlerin standarda göre denetimi: MAN.8 |

**Hedef (`PA 2.1 a`):** Kabule sunulan her sürüm için etiket, CHANGELOG bölümü, dağıtım kaydı ve CI kanıtı birbiriyle tutarlıdır; konfigürasyon denetiminde uygunsuzluk çıkmaz.

---

## 2. Süreç nasıl işletilir?

### 2.1 Her değişiklikte (sürekli)

1. Her değişiklik bir issue'ya bağlıdır. Dal adı `<tür>/<issue-no>-<kısa-açıklama>` biçimindedir (`CONTRIBUTING.md` §3).
2. Commit mesajı ve PR başlığı Conventional Commits biçimindedir (§4, §5.1). Yerelde `commit-msg` kancası, CI'da `pr-traceability-check.yml` denetler.
3. PR; şablon, issue bağlantısı ve milestone ile açılır. CI kalite kapıları geçmelidir.
4. İnceleyen, birleştirmeden önce PR'a onay yorumunu yazar (`KR-096`). Birleştirme squash merge ile yapılır; dal silinir.
5. **Belgeler de aynı yoldan geçer.** `main`'e doğrudan yazılmaz (§1).

### 2.2 `main` dalının korunması (`KR-054`, `KR-055`)

Depo özeldir ve GitHub Free planındadır; sunucu tarafı dal koruma kullanılamaz (`R-16`). Yerine üç katmanlı telafi düzeni vardır. Ayrıntı: `dal-koruma-telafi-kontrolleri.md`.

| Katman | Araç | Türü |
|---|---|---|
| İstemci | `.githooks/pre-push` (`main`'e gönderim engeli), `.githooks/commit-msg` | Önleyici, atlanabilir |
| Sunucu | `branch-protection-check.yml`: `main`'e gelen her commit PR ile mi geldi, PR'da birleştirmeden önce onay yorumu var mı? Yoksa `[DÜZELTİCİ]` issue'su açılır. Denetim çökerse 3 kez dener, yine olmazsa "denetim yapılamadı" issue'su açılır (#129) | Tespit edici, atlatılamaz |
| Sunucu | `main-failure-check.yml`: `main` kırmızıysa `[HATA] main kırmızı` issue'su açılır; kırmızıyken özellik birleştirilmez (`CONTRIBUTING.md` §1.1) | Tespit edici |

### 2.3 Sürüm ve baseline (`KR-097`, `CONTRIBUTING.md` §9)

1. **Kabul adayı:** Modül doğrulamayı (G2, TEC.9) geçince Bilgi İşlem bir sürüm hazırlık PR'ı açar. Bu PR `CHANGELOG.md`'ye sürüm bölümünü yazar. Birleşen commit `vX.Y.0-rc.N` olarak **açıklamalı (annotated)** etiketlenir.
2. **Dağıtım:** UAT'ye yalnızca `docker/deploy-uat.sh` ile kurulur. Betik şunları yapar:
   - Commit edilmemiş değişiklik ve GitHub'a gönderilmemiş commit varsa durur.
   - Kaynağı `git archive` ile commit'ten aktarır.
   - İmajlara sürümü ve commit'i OCI etiketi olarak yazar.
   - Çalışan imajın commit'ini doğrular.
   - Dağıtımı sunucuda `deployments.log`'a ve depoda `TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`'ye yazar. Depodaki satır PR ile commit edilir.
3. **Kabul:** İK kabul formu bu etikete atıf yapar (TEC.11). Kabulden sonra aynı commit `vX.Y.0` olarak etiketlenir. Kabulden sonra düzeltme gerekirse `rc.N+1` verilir.
4. Bugünkü etiketler (14.10.2026): `v0.1.0` (A1, geriye dönük, `065a7c3`); `v0.2.0-rc.1` (`6cfeef0`), `rc.2` (`69bafad`), `rc.3` (`fa3e194`), `rc.4` (`353eae7`), `rc.5` (`149f6ee`); `v0.2.0` (`149f6ee`, T3 Kimlik Yönetimi, İK kabulü 13.10.2026, `KR-105`). Yedisi de açıklamalı ve `origin`'de; her birinin CHANGELOG bölümü var.
5. **Kabul etiketi ve CHANGELOG:** `vX.Y.0` kabul edilen adayın commit'ine konduğu için o commit'teki `CHANGELOG.md`'de `[X.Y.0]` bölümü henüz yoktur; bölüm kabulden sonraki commit'te yazılır (T3: `d354e94`). Bu, "kabulden sonra aynı commit etiketlenir" kuralının doğal sonucudur, uygunsuzluk değildir (konfigürasyon denetimi 13.10.2026, G-4).

### 2.4 Konfigürasyon öğeleri ve konfigürasyon denetimi

- **Öğe listesi:** `konfigurasyon-ogeleri.md`, depo içi ve depo dışı öğeleri `KÖ-01`…`KÖ-52` kimlikleriyle sayar (sonradan eklenen ara öğeler `KÖ-08a`, `KÖ-14a`; yerel geliştirme ortamı `KÖ-36`). Her öğenin yeri ve değişiklik yolu oradadır. Baseline üç biçimde oluşur:
  - depodaki öğeler için açıklamalı Git etiketi;
  - dağıtılan yapı için dağıtım kaydı;
  - depo dışı öğeler (sunucu ayarları, sertifikalar, dış hizmet hesapları) için konfigürasyon denetimi.
- **Ne zaman denetlenir (`KR-110`):** Tam denetim, İK kabul oturumuna sunulacak **son adayda** ve **her modül kapanışında** yapılır. Ara adaylarda kurulum sonrası kısa kontrol yapılır ve kayıt PR'ına yazılır (sürüm kontrol listesi A6, `TEC.10-gecis/surum-kontrol-listesi.md`). Denetim "kabule sunulan yapı, kayıtlarda yazan yapı mı?" sorusunu cevaplar.
- **Yapılan denetimler:**

  | Rapor | Denetlenen | Sonuç |
  |---|---|---|
  | `raporlar/2026-10-05-ilk-konfigurasyon-denetimi.md` | `v0.2.0-rc.2` | 12 madde: 05.10'da 10'u uygun, 2'si (D-11, D-12) sunucu erişimi gerektirdiği için sınanamadı; ikisi 06.10'da sınandı ve uygun (rapor 1.1–1.2). Uygunsuzluk yok |
  | `raporlar/2026-10-13-t3-konfigurasyon-denetimi.md` | `v0.2.0` (`149f6ee`), T3 modül kapanışı | 20 madde: 14 uygun, 4 kısmen uygun, 1 sınanamadı (D-18), 1 uygunsuzluk (D-19, `main` kırmızı denetiminin iptali). Bulgular B-1…B-4; B-1 ve UAT'deki B-3 maddeleri 13.10'da giderildi (#207), öğe listesi B-2/B-3'e göre güncellendi (#214) |

  `v0.2.0-rc.3`, `rc.4` ve `rc.5` için ayrı denetim yapılmadı; kabule giren `rc.5` yapısı modül kapanış denetiminde `v0.2.0` olarak denetlendi. Sıklık kuralı bu yüzden `KR-110` ile netleştirildi (#218).

---

## 3. Roller (`PA 2.1 d`)

RACI'de MAN.5'in sorumlusu ve hesap vereni Bilgi İşlem'dir; İK bilgilendirilir (`roller-ve-sorumluluklar.md`).

| Rol | Görev |
|---|---|
| Bilgi İşlem | Dal, PR, sürüm etiketi, CHANGELOG, dağıtım, öğe listesi, konfigürasyon denetimi |
| İnceleyen (Doğuş Uçanok) | Birleştirmeden önce onay yorumu (`KR-096`). Onay yorumunu yalnızca inceleyen yazar |
| İK | Kabul formunda sürüm etiketini onaylar (TEC.11) |
| CI (GitHub Actions) | Biçim, izlenebilirlik, dal koruma ve `main` durum denetimleri |

---

## 4. Kaynaklar ve arayüzler (`PA 2.1 e, f`)

| Kaynak / arayüz | Kullanım |
|---|---|
| Git + GitHub (özel depo, Free plan) | Sürüm kontrolü, issue, PR, etiket |
| GitHub Actions | Telafi denetimleri ve kalite kapıları |
| UAT sunucusu | Dağıtılan yapı; `deployments.log` |
| TEC.9 | G2 kapısı ve CI kanıt özeti, etiketin ön koşuludur |
| TEC.10 / TEC.11 | Dağıtım kaydı / kabul formundaki sürüm atfı |
| MAN.8 | Telafi denetimlerinin açtığı `[DÜZELTİCİ]` issue'larının takibi |

---

## 5. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Çalışma kuralları | `CONTRIBUTING.md` §1–§5, §9 | Kural ve gerekçe | Bilgi İşlem |
| Telafi kontrolleri | `dal-koruma-telafi-kontrolleri.md` | Kısıt, kontrol, karar, açık iş | Bilgi İşlem |
| Konfigürasyon öğeleri | `konfigurasyon-ogeleri.md` | `KÖ-NN`: yer, sahibi, değişiklik yolu | Bilgi İşlem |
| Değişiklik kaydı | GitHub issue + PR | Şablon; issue bağlantısı; onay yorumu | PR sahibi, inceleyen |
| Baseline | Açıklamalı Git etiketi | `vX.Y.Z[-rc.N]`; açıklama ilgili CHANGELOG bölümüne adıyla atıf yapar (ör. "CHANGELOG [0.2.0]"). Açıklama ASCII yazılır ve başlığın birebir kopyası değildir (konfigürasyon denetimi 13.10.2026, G-3) | Bilgi İşlem |
| Sürüm notu | `CHANGELOG.md` | Keep a Changelog; her etikete bir bölüm | Bilgi İşlem (sürüm hazırlık PR'ı) |
| Dağıtım kaydı | `TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`, sunucuda `deployments.log` | Zaman (UTC), sürüm, commit, dağıtan | `deploy-uat.sh` |
| Konfigürasyon denetimi | `raporlar/<tarih>-*konfigurasyon-denetimi.md` | `D-NN` maddeleri: yöntem, sonuç | Bilgi İşlem |
| Kontrol ihlali | GitHub issue (`[DÜZELTİCİ]`, `tur:duzeltici-faaliyet`, `surec:MAN.5`) | Otomatik: commit, yazar, tarih | CI |

---

## 6. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** Bu belge, öğe listesi ve raporlar yalnızca PR ile değişir. Her belgenin bir değişiklik geçmişi vardır.
- **Etiket taşınmaz:** Düzeltme gerekirse var olan etiket başka commit'e taşınmaz; yeni `rc.N+1` verilir (`KR-097`; örnek: `v0.2.0-rc.1` → `rc.2`, #160).
- **Etiket ↔ CHANGELOG ↔ dağıtım tutarlılığı:** Her konfigürasyon denetiminde sınanır (ilk denetim, D-01…D-05).
- **Sırlar:** Depo dışındaki sır dosyasının (`KÖ-30`) içeriği depoya girmez; yalnızca yeri ve değişken listesi kayıtlıdır. `gitleaks` CI'da denetler.
- **Raporların değişmezliği:** Denetim raporundaki bir madde sonradan sınanırsa (örn. D-11, D-12) rapor silinmez; yeni sürüm eklenir.

---

## 7. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (MAN.5) | Karşılığı |
|---|---|
| a) Kontrol gerektiren öğeler belirlenir ve yönetilir | `konfigurasyon-ogeleri.md` (§2.4) |
| b) Baseline'lar oluşturulur | Açıklamalı etiketler, `KR-097` (§2.3) |
| c) Değişiklikler kontrol edilir | Dal → PR → CI → onay → squash; telafi denetimleri (§2.1, §2.2) |
| d) Konfigürasyon durum bilgisi erişilebilir | Git geçmişi, CHANGELOG, dağıtım kaydı, GitHub Projects |
| e) Konfigürasyon denetimleri tamamlanır | `raporlar/` (§2.4) |
| f) Sürümler ve teslimler kontrol edilir ve onaylanır | G2 → rc etiketi → `deploy-uat.sh` → İK kabul formu (§2.3) |

---

## 8. Açık noktalar

Durum sütunu 14.10.2026'da T3 modül sonu denetimlerine göre güncellendi (#214).

| # | Konu | Durum |
|---|---|---|
| 1 | Sunucu tarafı dal koruma yok; kontrol olayı önlemez, sonradan yakalar | Bilinçli kabul (`KR-055`, `R-16`) |
| 2 | Telafi kontrollerinin dönemsel örneklenmesi (`dal-koruma-telafi-kontrolleri.md` §6 iş 3) konfigürasyon denetiminin maddeleri arasında yok | ✅ Kapandı: 13.10.2026 denetiminde D-19 olarak ilk kez yapıldı; `main` kırmızı denetiminin 05.10'daki iptalini buldu (B-4). Kontrollerin kendisinin izlenmesi #208 ile eklendi (zamanlanmış `main` sağlık taraması) |
| 3 | İlk denetimin D-11 (NTP) ve D-12 (sır dosyası izni) maddeleri sınanmadı | ✅ Kapandı: 06.10.2026'da sınandı (ilk denetim raporu 1.1–1.2); 13.10'da da uygun (D-13, D-14) |
| 4 | `00-DOKUMAN-HARITASI.md` sürüm kontrolü için "GitHub Releases" diyor; GitHub Release hiç oluşturulmadı. Uygulama: CHANGELOG + açıklamalı etiket | ✅ Kapandı: harita düzeltildi (#214) |
| 5 | CHANGELOG yalnızca sürüm hazırlık PR'ında güncellenir; unutulursa onu yalnızca konfigürasyon denetimi yakalar (D-01) | İzleniyor. 13.10 denetiminde 7 etiketin 7'sinin bölümü var |
| 6 | Örnek ortam dosyası ile compose değişkenlerinin uyumu (D-04/D-08) elle denetleniyor | Açık: 13.10 denetiminde de elle yapıldı (G-2, Ö-9) |
| 7 | 04.10.2026'dan önceki UAT dağıtımlarının commit kaydı yok | Geriye dönük kapatılamaz; kayıtta yazılı |
| 8 | Kabul adayları `rc.3`, `rc.4`, `rc.5` için konfigürasyon denetimi yapılmadı (§2.4 kuralı) | Açık: kural ya da uygulaması #218'de |

---

## 9. Gözden geçirme

Bu belge her konfigürasyon denetiminde ve her modül sonu süreç denetiminde (MAN.8) gözden geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
| 2026-10-14 | 1.1 | T3 modül sonu gözden geçirmesi: §2.3 etiket listesi (`rc.3`…`rc.5`, `v0.2.0`) ve kabul etiketinde CHANGELOG bölümünün sonraki commit'te yazılması; §2.4 yapılan denetimler; §5 etiket açıklaması kuralı uygulamaya göre; §8 durumları güncellendi, madde 8 eklendi (#214) | Bilgi İşlem |
| 2026-10-15 | 1.2 | §2.4: denetim sıklığı `KR-110`; sürüm kontrol listesi (#218) | Bilgi İşlem |
