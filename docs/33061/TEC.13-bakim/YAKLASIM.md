# TEC.13 — Süreç Yaklaşımı

**Belge kimliği:** TEC.13-YAK
**Süreç:** TEC.13 — Bakım
**Son güncelleme:** 2026-10-05
**Karşıladığı öznitelik maddeleri:** `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir. `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir.

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir.

---

## 1. Sürecin amacı ve sınırı

Bakım, sistemin hizmet verebilir durumda kalmasını sağlar: arızaları giderir, ortam ve bağımlılıklardaki değişikliklere uyarlar, sorunları oluşmadan önler (TS ISO/IEC TS 33061, TEC.13).

Üretim ortamı henüz yoktur. Bakım bugün **geliştirme, CI ve UAT** üzerinde yapılır. Bakımın üç türü fiilen işletilmektedir:

| Tür | Ne | Örnek |
|---|---|---|
| **Düzeltici** | Bulunan hatanın giderilmesi | #97, #103, #137, #138 |
| **Önleyici** | Bağımlılık ve taban imaj açıklarının kapatılması; sertifika ve saat gibi ortam koşullarının izlenmesi | CVE-2026-84782 (PR #108), #149, R-18, R-20, R-21 |
| **Uyarlayıcı** | Kabul edilmiş bir kuralın gerçek veriye göre değiştirilmesi | #77 (`[DEĞİŞİKLİK]`, MAN.2 akışı) |

**Kapsamda değildir:** yeni yetenek eklemek. Yeni yetenek gereksinim olarak TEC.2'den başlar.

---

## 2. Süreç nasıl işletilir?

### 2.1 Hata bulunduğunda

1. **Kayıt açılır:** `[HATA]` issue'su, `03-bug.yml` şablonuyla. Şablon `tur:hata` ve `surec:TEC.13` etiketlerini koyar; ortam ve şiddet (Kritik, Yüksek, Orta, Düşük) zorunludur. Kişisel veri yazılmaz; `traceId` yeterlidir.
2. **Kayıt doldurulur.** İyi bir hata kaydında şunlar bulunur (örnek: #97, #103, #137, #138):
   - belirti ve nasıl görüldüğü;
   - kök neden;
   - neden şimdiye kadar görülmediği;
   - etki (UAT, üretim, tasarım);
   - düzeltme;
   - regresyon testi.
3. **Öncelik verilir:** `oncelik:yuksek`, `oncelik:orta` veya `oncelik:dusuk`.
4. **Düzeltme ayrı PR ile yapılır** (örn. #137 → PR #139, #138 → PR #159, #149 → PR #150). PR issue'yu kapatır. Regresyon testi aynı PR'dadır ve eski kodda düştüğü gösterilir (TEC.9 §2.1).
5. **Kabul adayını etkiliyorsa** yeni aday etiketlenir ve UAT'ye kurulur (TEC.10 §2.2). Örnek: #138 düzeltmesi `v0.2.0-rc.2` ile geldi; `v0.2.0-rc.1`'in yerini aldı (#160). Düzeltme CHANGELOG'un "Düzeltildi" bölümüne yazılır.
6. **Risk niteliğindeyse** risk defterine de işlenir. Örnek: SMTP sertifikası → R-20, sunucu saati → R-21.

### 2.2 `main` kırmızıyken (CONTRIBUTING §1.1)

- `main`'de CI veya API sözleşme denetimi düşerse `main-failure-check.yml` otomatik olarak `[HATA] main kırmızı: …` issue'su açar (`tur:hata`, `oncelik:yuksek`, `surec:TEC.13`). Güvenlik açığı nedeniyle düşen tarama da buna dahildir.
- `main` kırmızıyken yeni özellik birleştirilmez. Düzeltme ayrı PR ile gelir; ilgisiz bir özellik PR'ının içine konmaz.
- Neden, etki ve tekrarı önleyen önlem issue'ya yazılır.

Bu kural, O-2 olayından doğdu: `main` 30.09.2026'da CVE-2026-84782 nedeniyle yaklaşık 21 saat kırmızı kaldı ve düzeltme PR #108'in içinde geldi (`kayitlar/2026-10-04-t3-olay-kayitlari.md`).

### 2.3 Olaylar

Hata olmayan ama sistemi etkileyen olaylar (denetimin çökmesi, klasör kaybı, dış sunucu sertifikası) da kayda geçer. 04.10.2026'dan önceki dört olay geriye dönük olarak `kayitlar/2026-10-04-t3-olay-kayitlari.md`'de (O-1…O-4) tutulur. Bundan sonra olaylar `[HATA]` issue'su olarak açılır. Dal koruma denetimi yapılamazsa `[DÜZELTİCİ]` issue'su otomatik açılır.

### 2.4 Önleyici bakım (her PR'da ve `main`'de)

| Denetim | Nerede | Etki |
|---|---|---|
| .NET paketlerinde bilinen açık | `ci.yml`, `dotnet list package --vulnerable --include-transitive` | Açık varsa CI düşer |
| Ön yüz paketlerinde yüksek ve üstü açık | `ci.yml`, `npm audit --audit-level=high` (#149) | Açık varsa CI düşer |
| Konteyner imajında kritik ve yüksek açık | `ci.yml`, trivy (yaması olan açıklar) | Açık varsa CI düşer |
| Taban imaj paketleri | Dockerfile'larda derlemede güncelleme (`KR-065`) | Yeni yamalar her derlemede alınır |

CI zamanlanmış çalışmaz; yalnızca PR'da ve `main`'e gönderimde çalışır. Ortam koşulları elle izlenir: UAT sertifikası (runbook §7, R-18), SMTP sertifikası (runbook §10.5, R-20), sunucu saati (runbook §12, R-21).

---

## 3. Roller

| Rol | Sorumluluk |
|---|---|
| **Bilgi İşlem** (Doğuş Uçanok) | Hatayı kaydeder, sınıflandırır, düzeltir ve kapatır; önleyici denetimleri işletir (RACI: A/R) |
| **İK** | UAT ve kabulde bulduğu sorunları bildirir; etkiye ilişkin görüş verir (RACI: C) |
| **Kurum e-posta ve DNS yöneticileri** | SMTP sertifikası ve TLS yenilemesinde dış adımlar |

Tek kişilik düzende hatayı kaydeden, düzelten ve kapatan aynı kişidir (R-05). Düzeltme PR'ı yine inceleme onayından geçer (`KR-096`).

---

## 4. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Hata kaydı | GitHub issue (`[HATA]`, `tur:hata`, `surec:TEC.13`) | `03-bug.yml` alanları ve §2.1'deki başlıklar | Bulan; Bilgi İşlem tamamlar |
| `main` kırmızı kaydı | GitHub issue (otomatik) | Çalışma bağlantısı; neden, etki, önlem elle eklenir | `main-failure-check.yml`, Bilgi İşlem |
| Düzeltme | PR (issue'yu kapatır) | Regresyon testiyle | Bilgi İşlem |
| Olay kaydı | `kayitlar/<tarih>-<konu>-olay-kayitlari.md` veya issue | Ne oldu, etki, düzeltme, önlem | Bilgi İşlem |
| Düzeltici faaliyet | GitHub issue (`[DÜZELTİCİ]`) | `05-corrective-action.yml` | Bilgi İşlem, CI |
| Sürüme giren düzeltmeler | `CHANGELOG.md` "Düzeltildi" ve "Güvenlik" | Issue numarasıyla | Bilgi İşlem |
| Ortam riskleri | `MAN.4-risk-yonetimi/risk-kayit-defteri.md` | Risk kaydı | Bilgi İşlem |

---

## 5. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** Düzeltmeler ve bu belge yalnızca PR ile değişir. Her düzeltme bir issue'ya bağlıdır (CONTRIBUTING §2).
- **İzlenebilirlik:** hata → PR → regresyon testi → sürüm etiketi → CHANGELOG. Etiket kuralı: tüm `[HATA]` kayıtları `surec:TEC.13` taşır; şablon ve `main-failure-check.yml` bu etiketi kendiliğinden koyar. #137'den önceki kayıtların çoğu `surec:TEC.7` ile açılmıştı; geriye dönük değiştirilmedi.
- **Kayıtların değişmezliği:** Kapanan issue silinmez. Geriye dönük kayıt, olayın o tarihte kaydedilmediğini açıkça yazar (O-1…O-4).
- **Kişisel veri:** Hata kaydına ve ekran görüntüsüne kişisel veri girmez (şablon uyarısı).

---

## 6. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (TEC.13) | Karşılığı |
|---|---|
| a) Bakım kısıtları belirlenir | `KR-065`, `KR-067`, `KR-098`; runbook §7, §10.5, §12; §2.4 |
| b) Destekleyici sistemler mevcut | Hata şablonu ve etiketleri, CI kapıları, `main-failure-check.yml`, UAT |
| c) Onarılan veya değiştirilen öğeler sağlanır | Düzeltme PR'ları; gerekirse yeni kabul adayı (`rc.N+1`) |
| d) Düzeltici, uyarlayıcı ve iyileştirici değişiklik ihtiyacı raporlanır | `[HATA]`, `[DEĞİŞİKLİK]`, `[DÜZELTİCİ]` issue'ları |
| e) Arıza ve ömür verisi belirlenir | **Yok** (§7) |

---

## 7. Açık noktalar

| Konu | Durum |
|---|---|
| **Hedef çözüm süreleri** | Yazılı değil. Şiddet ve öncelik var, ama öncelik başına hedef süre yok; kapanma süreleri bu yüzden değerlendirilemez. T3 denetimine göre T3 dönemindeki 6 hatanın 5'i bir gün içinde kapandı |
| **Eğilim ve maliyet verisi, bakım raporu** | Yok. `raporlar/` boş; hata sayısı, kaynağı, kapanma süresi ve kök neden sınıfları derlenmedi |
| **Destek modeli** | Üretim için yok: kullanıcı sorunu nasıl bildirir, kim ilk yanıtı verir, mesai dışı ne olur |
| **Sertifika ve sır süreleri izleme listesi** | Tek bir listede değil; bitiş tarihleri runbook §1 ve risk defterinde (R-18, R-20). Sistem içi uyarı Y4'te gelecek (#42) |
| **Zamanlanmış güvenlik taraması** | Yok. Yeni yayımlanan bir açık, ancak bir sonraki PR'da veya `main` gönderiminde görülür |
| **Müşteri memnuniyeti** | Ölçülmüyor; üretim kullanıcısı yok |

---

## 8. Gözden geçirme

Bu belge her modül sonu süreç denetiminde (MAN.8) ve üretime geçişten önce gözden geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
