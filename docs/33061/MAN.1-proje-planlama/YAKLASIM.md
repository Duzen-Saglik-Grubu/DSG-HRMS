# MAN.1 — Süreç Yaklaşımı

**Belge kimliği:** MAN.1-YAK
**Süreç:** MAN.1 — Proje Planlama
**Son güncelleme:** 2026-10-05
**Karşıladığı öznitelik maddeleri:** `PA 2.1`: hedefler, planlama, sorumluluklar, kaynaklar ve arayüzler. `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir. `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir.

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Planın kendisi
> `proje-plani.md`'dedir; bu belge planın nasıl kurulduğunu, güncellendiğini ve
> onaylandığını anlatır.

---

## 1. Sürecin amacı ve sınırı

Proje planlama, projenin hedeflerini, kapsamını, işin yapısını, rollerini ve kaynaklarını
tanımlar ve planı yürürlüğe koyar. Plan proje boyunca revize edilir (TS ISO/IEC TS 33061, MAN.1).

**Kapsamdadır:** hedefler ve kısıtlar, hayat döngüsü ve karar kapıları, iş kırılım yapısı (WBS),
aşama ve modül sırası, roller ve yetkiler (RACI), insan kaynağı ve altyapı ihtiyaçları,
planın onayı ve revizyonu.

**Kapsamda değildir:**

| Konu | Süreç |
|---|---|
| Planın izlenmesi, durum raporu, sapma analizi | MAN.2 |
| Risklerin kaydı ve ele alınması | MAN.4 |
| Sürüm etiketi ve baseline | MAN.5 |

---

## 2. Süreç nasıl işletilir?

### 2.1 Plan seti

Plan tek belge değildir. Beş belge birlikte planı oluşturur:

| Belge | Ne söyler |
|---|---|
| `proje-plani.md` | Hedefler (H1–H7), hayat döngüsü ve G1–G4 kapıları, aşama planı, kaynaklar, kısıtlar, izleme |
| `is-kirilim-yapisi.md` | İşin yapısı (7 kalem, 35 modül), kritik yol, milestone eşlemesi |
| `roller-ve-sorumluluklar.md` | Roller, RACI, yetkinlik tablosu |
| `docs/mimari/vizyon-ve-kapsam.md` | İş hedefleri, kapsam ve kapsam dışı |
| `docs/mimari/modul-listesi-ve-bagimliliklar.md` | Modül bağımlılıkları ve seçim rehberi |

### 2.2 Tarih değil, sıra

Aşama planı (A0 → A1 → A2 → A3 → A4+ → AS) **tarih değil sıra** üzerinden kurulur
(`proje-plani.md` §5). Nedenleri:

- Modül önceliğine İK karar verir (RACI §3.2). Bir sonraki modül önceden bilinmez.
- Tek geliştirme kanalı vardır (`KS8`, `R-05`).

Bu yüzden GitHub milestone'larının bitiş tarihi yoktur. Sıra şu ilkelerle belirlenir:
temel modüller önce (`KR-040`), yatay modüllere öncelik (`KR-053`), zincirli modüllerde
ilk halkadan başlama (`KR-052`), ön koşulu tamamlanmamış modül seçilmez (seçim rehberi).
Süre tahmini, ilk 3–4 modülün çevrim süresinden üretilecek ve üst yönetime sunulacaktır
(`proje-plani.md` §8.1).

### 2.3 Modül çevrimi ve başlangıç kapısı

Her modül aynı 12 adımlı çevrimi izler (`proje-plani.md` §3). Kapılar:

| Kapı | Karar veren | Kayıt |
|---|---|---|
| G1 — Gereksinim kilidi | İK | Toplantı kaydı ve onaylı `PG-<MODÜL>.md` (TEC.2) |
| G2 — Geliştirme tamam | Bilgi İşlem | Doğrulama raporundaki G2 kaydı (TEC.9) |
| G3 — Kabul | İK | İmzalı kabul formu (TEC.11) |
| G4 — Kapanış | Bilgi İşlem | Kapanış gözden geçirmesi (MAN.2) |

**Bir modül, o modüle özel İK gereksinim toplantısı yapılmadan ve gereksinimler İK
onayından geçmeden başlamaz** (`KR-068`). Teknik ön koşulun hazır olması yetmez.
Bir modülün başka bir modülün çekirdeğine ihtiyacı varsa yalnızca onaylı gereksinimlerin
gerektirdiği kadarı kurulur (`KR-077`: T3 kapsamında T1 çekirdeği).

### 2.4 Sürüm planı

Kabul edilen her modül bir MINOR sürüm üretir; numara kabul sırasına göre verilir
(`KR-097`). İK kabulüne sunulan sürüm `vX.Y.0-rc.N` etiketi alır. Bugünkü durum:
`v0.1.0` (A1), `v0.2.0-rc.1` ve `v0.2.0-rc.2` (T3 kabul adayları). Etiketleme MAN.5'e aittir.

### 2.5 Planın işe çevrilmesi

- Her WBS kalemi bir milestone'a karşılık gelir (`is-kirilim-yapisi.md` §5.1).
- İş, ait olduğu WBS kaleminin milestone'una atanır; ne zaman yapıldığına göre değil (§5.2).
- Her issue `tur:`, `surec:` ve `oncelik:` etiketi taşır (CONTRIBUTING §10).
- İK onayı beklenirken modülden bağımsız enine işler yapılır (`proje-plani.md` §4.1).

### 2.6 Plan revizyonu

Plan şu durumlarda gözden geçirilir ve gerekirse revize edilir:

| Tetikleyici | Örnek |
|---|---|
| Modül kapanışı (G4) | Sıradaki modül, kaynak ve altyapı durumu |
| Sırayı veya kapsamı değiştiren karar | `KR-077` (T3'ün T1'den önce yapılması) |
| Aşama kapanışı | A1 kapanış değerlendirmesi |
| Sürüm veya süreç kuralı değişikliği | `KR-097` (plan v0.2) |

Revizyon PR ile yapılır. Belgenin başlığındaki sürüm ve tarih ile değişiklik geçmişi
güncellenir. Revizyonun nedeni ve etkisi `kayitlar/YYYY-AA-GG-plan-revizyonu.md` olarak
kayda geçer (doküman haritası: "Planlama kaydı").

### 2.7 Onay

Proje planı ve kapsam **Üst Yönetim** onayıyla yürürlüğe girer (RACI §3.1, MAN.1 satırı;
§3.2 "Proje kapsamı ve takvim"). RACI'nin İK ve Üst Yönetimle teyidi de bu adıma bağlıdır.
Onay tarihi ve onaylayan, belgenin başlığına ve `kayitlar/`'a yazılır.

---

## 3. Roller

| Rol | Görev |
|---|---|
| Bilgi İşlem (Doğuş Uçanok) | Planı hazırlar ve revize eder (R). Değişiklikleri inceler ve onaylar (`KR-096`) |
| Geliştirme yardımcısı (Claude) | Taslak ve revizyon önerisi hazırlar. Onay vermez |
| İK Birimi | Modül önceliği ve modül kapsamı (A). Plana danışılır (C) |
| Üst Yönetim | Planı, kapsamı ve kaynak tahsisini onaylar (A) |

---

## 4. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Proje planı | `proje-plani.md` | Sürümlü Markdown; başlıkta sürüm, onay durumu, sahip | Bilgi İşlem |
| İş kırılım yapısı | `is-kirilim-yapisi.md` | Numaralı kalemler; durum tutmaz, yapı tutar | Bilgi İşlem |
| Roller ve RACI | `roller-ve-sorumluluklar.md` | Her satırda tek A | Bilgi İşlem |
| Aşama kapanış değerlendirmesi | `A<n>-asama-kapanis-degerlendirmesi.md` | Her aşama için bir belge | Bilgi İşlem |
| Plan revizyon kaydı | `kayitlar/YYYY-AA-GG-plan-revizyonu.md` | Neden, ne değişti, etkisi, onay | Bilgi İşlem |
| Planlama kararları | `docs/karar-kayit-defteri.md` | `KR-NNN` satırı | Kararı veren |
| Milestone ve iş kalemleri | GitHub milestone, issue, Projects panosu | Etiket ve milestone zorunlu | PR/issue sahibi |

---

## 5. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** Plan belgeleri yalnızca PR ile değişir. PR'ı inceleyen, birleştirmeden
  önce onay yorumu yazar; CI bunu denetler (`KR-096`).
- **Sürüm ve tarih:** Her revizyonda başlıktaki sürüm, "Son güncelleme" ve değişiklik geçmişi
  birlikte güncellenir. Biri güncellenip diğeri unutulursa belge kendi içinde çelişir.
- **Onay durumu:** Başlıkta yazılıdır. Onaysız plan "taslak" olarak işaretli kalır.
- **Karar izi:** Planı değiştiren her karar önce `KR-NNN` olarak yazılır, sonra plana yansıtılır.
- **Saklama:** Git geçmişi tüm sürümleri saklar. Saklama ve imha kuralı MAN.6'da tanımlanacak (#131).

---

## 6. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (MAN.1) | Karşılığı |
|---|---|
| a) Hedefler ve planlar tanımlanır | `proje-plani.md`, `is-kirilim-yapisi.md`, §2.1–§2.4 |
| b) Roller, sorumluluklar ve yetkiler tanımlanır | `roller-ve-sorumluluklar.md`, §3 |
| c) Kaynaklar talep edilir ve taahhüt edilir | `proje-plani.md` §6, §2.7 onayı (açık, bkz. §7) |
| d) Planlar yürürlüğe konur | G1 kapısı (`KR-068`), milestone ve issue'lar, §2.5 |

---

## 7. Açık noktalar

T3 sonu süreç denetiminde (`MAN.8-kalite-guvence/raporlar/2026-10-02-t3-surec-denetimi.md`,
Ek A §1) görülen ve 05.10.2026'da hâlâ açık olan konular:

| # | Açık nokta | İzlendiği yer |
|---|---|---|
| 1 | Plan ve RACI 07.09.2026'dan beri onaysız; `A0 — Hazırlık ve Planlama` milestone'u açık | #127 |
| 2 | Plan v0.2 revizyonu yapılmadı: `KR-077` sırası, `SYG-` kimliği (plan §3 hâlâ `REQ-` diyor), UAT'nin kurulmuş olması ve gerçek veriyle çalışması (`KR-083`, `R-25`), B3/B8 durumları. WBS §4 kritik yolu da T1 → T2 → T3 diyor | #127 |
| 3 | `proje-plani.md` başlığı "Sürüm 0.1, 2026-09-07" diyor; değişiklik geçmişinde 0.2 (2026-10-04) var | #127 |
| 4 | `kayitlar/` boş: hiçbir plan revizyonu kayda geçmedi | — |
| 5 | Bütçe kalemi yok; plan yalnızca araçların ücretsiz olduğunu söylüyor | — |
| 6 | RACI yetkinlik tablosu (§4.1) 07.09.2026'dan beri güncellenmedi; "her modül kapanışında" kuralı T3 için henüz işlemedi | T3 kapanışı |
| 7 | WBS §5.2 süreç işlerini `SK` milestone'una atar; CONTRIBUTING §5 ise yalnızca A0/A1/A2/A3/AS sayar. Denetim işleri (#125–#133) `A2` altında açıldı. İki kural birleştirilmeli | — |

---

## 8. Gözden geçirme

Bu belge her modül sonu süreç denetiminde (MAN.8) ve plan revizyonu yapıldığında gözden geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
