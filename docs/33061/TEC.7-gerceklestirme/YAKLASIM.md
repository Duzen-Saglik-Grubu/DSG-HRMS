# TEC.7 — Süreç Yaklaşımı

**Belge kimliği:** TEC.7-YAK
**Süreç:** TEC.7 — Gerçekleştirme
**Son güncelleme:** 2026-10-05
**Karşıladığı öznitelik maddeleri:** `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir. `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir. Ayrıca `PA 2.1` için hedef, plan, sorumluluk ve kaynaklar (§1–§4).

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Ayrıntılı çalışma kuralları
> `CONTRIBUTING.md`'dedir; bu belge onları süreç adımlarına bağlar ve tekrar etmez.

---

## 1. Sürecin amacı ve sınırı

Gerçekleştirme, tasarımı karşılayan **sistem öğesini üretir**: kodu yazar, öğeyi
paketler ve gereksinime izlenebilir biçimde saklar (TS ISO/IEC TS 33061, Madde 5, TEC.7).

**Kapsamdadır:** backend (`src/backend/`), frontend (`src/frontend/dsg-hrms-web/`),
veritabanı migration'ları, Docker dosyaları, betikler ve iş akışları. Betikler ve iş
akışları da koddur (CONTRIBUTING §3.2).

**Kapsamda değildir:** tasarım kararı (TEC.5), öğelerin birlikte çalıştığının sınanması
(TEC.8), gereksinimi karşıladığının kanıtı (TEC.9). Birim ve entegrasyon testi
gerçekleştirmeyle aynı PR'da yazılır, ama kanıt olarak TEC.9'a aittir.

### 1.1 Hedefler

- Her değişiklik bir issue'ya bağlıdır ve PR ile `main`'e girer; `main`'e doğrudan yazılmaz.
- Birleştirilen her PR, CI kalite kapılarının tamamını geçmiştir (ADR-0011 §6).
- Her sistem gereksinimi en az bir testte anılır; CI bunu her PR'da denetler (#122).
- Kabule sunulan her sürüm açıklamalı bir etiket ve CHANGELOG bölümü taşır (`KR-097`).

---

## 2. Süreç nasıl işletilir?

### 2.1 İş paketinden birleştirmeye

| Adım | Ne yapılır | Denetim |
|---|---|---|
| 1. Issue | Modülün SYG'leri iş paketlerine bölünür; başlık SYG aralığını taşır (ör. #74 "SYG-KMLK-001…012") | — |
| 2. Dal | `<tür>/<issue-no>-<kısa-açıklama>` (ör. `ozellik/130-yaklasim-belgeleri`); trunk-based, kısa ömürlü | CI: dal adı ve bağlı issue (#67) |
| 3. Commit | Conventional Commits; gövdede "neden"; `Refs: #<issue>` | Yerel `commit-msg` kancası; `pre-push` `main`'e gönderimi engeller |
| 4. Kod ve test | Kodlama kuralları §2.2; testler aynı PR'da; kodda ve testte gereksinim kimliği anılır (ör. `(SYG-KMLK-060)`) | Derleme, test, kapsam, mimari testi |
| 5. PR | Başlık Conventional Commits; şablon doldurulur (Ne / Neden / Nasıl doğrulandı / Tamamlanma Tanımı); `Closes` veya `Refs`; milestone | CI: başlık, issue bağı, milestone, izlenebilirlik |
| 6. Belgeler | Matris "Gerçekleştirme" ve "Doğrulama" sütunları, karar defteri, gerekiyorsa ADR aynı PR'da | CI: SYG ↔ test bağı |
| 7. İnceleme | Onay, birleştirmeden önce PR'a "İnceledim ve onaylıyorum." yorumu olarak yazılır; yalnızca inceleyen yazar (`KR-096`) | `branch-protection-check.yml` `main`'de arar |
| 8. Birleştirme | Squash merge; dal silinir; issue kapanır | — |

`main` kırmızıysa yeni özellik birleştirilmez; düzeltme ayrı PR'da yapılır ve
`main-failure-check.yml`'in açtığı `[HATA]` issue'sunu kapatır (CONTRIBUTING §1.1, #129).

### 2.2 Kodlama kuralları

| Konu | Kural | Kaynak |
|---|---|---|
| Dil | Tanımlayıcılar ve kod dosyası adları İngilizce; yorumlar, XML belgeleri ve kullanıcı iletileri Türkçe | `KR-058`, CONTRIBUTING §3.2 |
| Katmanlar | Domain → Application → Infrastructure / Api; bağımlılık kuralları | ADR-0002 |
| API | Denetleyici (controller); istek doğrulaması tek filtreden FluentValidation | `KR-084`, ADR-0010 |
| Hata | Yalnızca tanımlı istisnalar (`400/403/404/409/422`); kapsam dışı kayıt `404` | CONTRIBUTING §3.5 |
| Günlük | Kişisel veri nesne olarak verilir (`{@Nesne}`); hassas alan `[PersonalData]` / `[Secret]` | CONTRIBUTING §3.3, ADR-0009 §4 |
| Erişim kaydı | Kişisel veri dışarı çıkıyorsa `IAccessLogger`; yazılamazsa veri sunulmaz | CONTRIBUTING §3.4, `KR-061` |
| Frontend | Modül bazlı yapı (`src/features/<modül>/`), API tipleri OpenAPI'den üretilir | ADR-0015 |
| Biçim ve analiz | `TreatWarningsAsErrors`, `AnalysisLevel latest-recommended`, `src/backend/.editorconfig`, `dotnet format`; ESLint, Prettier, tip denetimi | `src/backend/Directory.Build.props`, ADR-0011 §6 |

### 2.3 Paketleme ve saklama

- **Kaynak:** Git deposu; her birleştirme `main`'de tek bir squash commit'tir.
- **Sürüm:** Kabule sunulan sürüm `vX.Y.0-rc.N` açıklamalı etiketini alır (ör.
  `v0.2.0-rc.1`, `v0.2.0-rc.2`); kabulden sonra aynı commit `vX.Y.0` olur (`KR-097`).
- **Sürüm notu:** `CHANGELOG.md`'de etiketle eşleşen bölüm; her madde issue numarası taşır.
- **İmaj:** API ve web imajları Dockerfile'larından derlenir; CI'da `trivy` ile taranır.
  UAT'ye kurulum TEC.8 ve TEC.10'dadır.

---

## 3. Roller ve kaynaklar (`PA 2.1`)

| Rol | Sorumluluk |
|---|---|
| Bilgi İşlem (R1) | Kodu yazar, testini yazar, PR'ı açar, inceler, birleştirir (RACI: TEC.7 **A**/R) |
| İnceleyen (Doğuş Uçanok) | PR'ı inceler ve onay yorumunu yazar (`KR-096`) |
| Geliştirme yardımcısı (Claude) | Kod ve belge önerir; onay yorumu yazmaz (`KR-096` madde 3) |

RACI: `docs/33061/MAN.1-proje-planlama/roller-ve-sorumluluklar.md` §3.1. Yazan ile
inceleyenin aynı kişi olması R-23 olarak izlenir.

**Destekleyici sistemler:** GitHub (issue, PR, Actions), .NET 10 SDK (`global.json`),
Node 22, Testcontainers (PostgreSQL), Docker, yerel git kancaları (`.githooks/`).

---

## 4. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Kaynak kod ve testler | `src/` | Kod yorumları Türkçe; gereksinim kimliği anılır | PR sahibi |
| Commit geçmişi | Git, `main` | Conventional Commits, `Refs: #<issue>` | PR sahibi |
| İş paketi | GitHub issue | Şablon; `tur:`, `modul:`, `surec:` etiketleri (CONTRIBUTING §10) | Bilgi İşlem |
| PR özeti | GitHub PR | Şablon: Ne / Neden / Nasıl doğrulandı / Tamamlanma Tanımı | PR sahibi |
| İnceleme kaydı | PR yorumu; geçmiş onaylar `MAN.8-kalite-guvence/kayitlar/2026-10-04-pr-onay-kaydi.md` | "İnceledim ve onaylıyorum." | İnceleyen |
| Sürüm notu | `CHANGELOG.md` | Keep a Changelog; etiketle eşleşen bölüm | Bilgi İşlem |
| Sürüm etiketi | Git | Açıklamalı etiket (`KR-097`) | Bilgi İşlem |
| İzlenebilirlik | `../izlenebilirlik-matrisi.md` "Gerçekleştirme" sütunu | Kod yolu + PR numarası (matris §2.1) | PR sahibi |

Doküman haritası, gerçekleştirme raporunun karşılığı olarak `CHANGELOG.md` ve PR
özetlerini sayar; `raporlar/` altında ayrı bir gerçekleştirme raporu üretilmez.

---

## 5. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** Kod, yapılandırma ve belge yalnızca PR ile `main`'e girer
  (CONTRIBUTING §1). Doğrudan gönderim yerelde `pre-push` ile engellenir; sunucu tarafı
  dal koruma olmadığından `branch-protection-check.yml` sonradan tespit eder ve
  düzeltici faaliyet issue'su açar (`R-16`).
- **Onay:** Birleştirmeden önce yazılmış onay yorumu; yoksa aynı denetim issue açar (`KR-096`).
- **Kapılar:** ADR-0011 §6'daki kapılar uyarı değil engeldir; devre dışı bırakma PR'da
  gerekçelendirilir ve `duzeltici-faaliyet` issue'su açılır.
- **Sürüm:** Etiketler açıklamalıdır ve `CHANGELOG.md` bölümüyle eşleşir; kabul
  oturumundan sonra düzeltme gerekirse yeni `rc.N+1` verilir (`KR-097`).
- **Sır:** Koda sır yazılmaz; `gitleaks` her PR'da tarar (ADR-0008).
- **Kişisel veri:** Gerçek kişisel veri test verisi olarak kullanılmaz, depoya eklenmez
  (CONTRIBUTING §11).

---

## 6. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (TEC.7) | Karşılığı | Durum |
|---|---|---|
| a) Gerçekleştirme kısıtları belirlenir | §2.2; SYG "Kısıt" türü; mimari testleri | Var |
| b) Sistem öğesi gerçeklenir | `src/`, PR'lar | Var |
| c) Öğe paketlenir veya saklanır | Git, etiketler, `CHANGELOG.md`, imajlar (§2.3) | Var (`v0.1.0`, `v0.2.0-rc.1`, `v0.2.0-rc.2`) |
| d) Destekleyici sistemler mevcut | §3 | Var |
| e) Gerçekleştirmenin izlenebilirliği | Issue başlığı, kod ve testte SYG kimliği, PR, matris, CI'daki SYG ↔ test denetimi | Var; §7 madde 3 |

---

## 7. Açık noktalar

| No | Açık nokta | Bağlantı |
|---|---|---|
| 1 | ~~Modül sınırı otomatik korunmuyor~~ — **Kapandı (05.10.2026, #171):** ADR-0016 | ADR-0016 |
| 2 | **PR boyutu kuralı tutulmuyor.** CONTRIBUTING §5 "< 400 satır" der; T3'te sekiz PR +2.383 ile +5.103 satır arasındaydı (#75, #84, #86, #88, #94, #99, #108, #110). Kural ya gerçekçi hâle getirilmeli ya iş paketleri bölünmeli | Denetim MAN.8-5 |
| 3 | Matris §2.1 "kod yolu + PR" ister; T3 satırları çoğunlukla yalnızca PR numarası veriyor | Denetim TEC.7-6 |
| 4 | PR şablonundaki örnek kimlik hâlâ `REQ-KIMLIK-07`; doğrusu `SYG-KMLK-nnn` | Denetim TEC.7-6 |
| 5 | Tamamlanma Tanımı iki yerde (CONTRIBUTING §6 ve PR şablonu) ayrı listeler olarak duruyor | Denetim MAN.8-4 |

---

## 8. Gözden geçirme

Bu belge her modül sonu süreç denetiminde (MAN.8) ve CONTRIBUTING'de süreç adımını
değiştiren bir değişiklikte gözden geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
| 2026-10-05 | 1.1 | §8 madde 1 kapandı: ADR-0016 (#171) | Bilgi İşlem |
