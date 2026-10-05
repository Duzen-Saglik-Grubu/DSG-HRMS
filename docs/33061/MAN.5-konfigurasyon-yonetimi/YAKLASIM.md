# MAN.5 — Süreç Yaklaşımı

**Belge kimliği:** MAN.5-YAK
**Süreç:** MAN.5 — Konfigürasyon Yönetimi
**Son güncelleme:** 2026-10-05
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
4. Bugünkü etiketler: `v0.1.0` (A1, geriye dönük), `v0.2.0-rc.1`, `v0.2.0-rc.2`. `v0.2.0` T3 kabulünü bekliyor.

### 2.4 Konfigürasyon öğeleri ve konfigürasyon denetimi

- **Öğe listesi:** `konfigurasyon-ogeleri.md`, depo içi ve depo dışı öğeleri `KÖ-01`…`KÖ-52` kimlikleriyle sayar. Her öğenin yeri ve değişiklik yolu oradadır. Baseline üç biçimde oluşur:
  - depodaki öğeler için açıklamalı Git etiketi;
  - dağıtılan yapı için dağıtım kaydı;
  - depo dışı öğeler (sunucu ayarları, sertifikalar, dış hizmet hesapları) için konfigürasyon denetimi.
- **Ne zaman denetlenir:** Her kabul adayı etiketinde ve her modül kapanışında. Denetim "kabule sunulan yapı, kayıtlarda yazan yapı mı?" sorusunu cevaplar.
- **İlk denetim:** `raporlar/2026-10-05-ilk-konfigurasyon-denetimi.md` (`v0.2.0-rc.2`). 12 madde denetlendi: 10'u uygun, 2'si sunucu erişimi gerektirdiği için sınanamadı, uygunsuzluk yok.

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
| Baseline | Açıklamalı Git etiketi | `vX.Y.Z[-rc.N]`; açıklama CHANGELOG başlığıyla aynı | Bilgi İşlem |
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

| # | Konu | Durum |
|---|---|---|
| 1 | Sunucu tarafı dal koruma yok; kontrol olayı önlemez, sonradan yakalar | Bilinçli kabul (`KR-055`, `R-16`) |
| 2 | Telafi kontrollerinin dönemsel örneklenmesi (`dal-koruma-telafi-kontrolleri.md` §6 iş 3) konfigürasyon denetiminin maddeleri arasında yok | Bir sonraki denetime madde olarak eklenmeli |
| 3 | İlk denetimin D-11 (NTP) ve D-12 (sır dosyası izni) maddeleri sınanmadı | Bir sonraki sunucu işinde |
| 4 | `00-DOKUMAN-HARITASI.md` sürüm kontrolü için "GitHub Releases" diyor; GitHub Release hiç oluşturulmadı. Uygulama: CHANGELOG + açıklamalı etiket | Harita bu belgeye göre düzeltilmeli |
| 5 | CHANGELOG yalnızca sürüm hazırlık PR'ında güncellenir; unutulursa onu yalnızca konfigürasyon denetimi yakalar (D-01) | İzleniyor |
| 6 | Örnek ortam dosyası ile compose değişkenlerinin uyumu (D-04) elle denetleniyor | CI'a alınması düşünülebilir |
| 7 | 04.10.2026'dan önceki UAT dağıtımlarının commit kaydı yok | Geriye dönük kapatılamaz; kayıtta yazılı |

---

## 9. Gözden geçirme

Bu belge her konfigürasyon denetiminde ve her modül sonu süreç denetiminde (MAN.8) gözden geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
