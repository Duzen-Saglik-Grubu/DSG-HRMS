# İzlenebilirlik Matrisi

**Belge kimliği:** 33061-IZM
**Son güncelleme:** 2026-09-18
**Hizmet ettiği süreçler:** TEC.2, TEC.3, TEC.5, TEC.7, TEC.8, TEC.9, TEC.10, TEC.11, TEC.13
**Sahibi:** Bilgi İşlem

> **Bu belge, "bu kod hangi gereksinimi karşılıyor?" ve "bu gereksinim nerede
> doğrulandı?" sorularının cevabıdır.** Sekiz sürecin çıktı listesinde izlenebilirlik
> maddesi bulunur; hiçbiri bu matris olmadan karşılanamaz.

---

## 1. Neden var?

TS ISO/IEC TS 33061, aşağıdaki süreç çıktılarında izlenebilirlik ister:

| Süreç | Çıktı | Ne istiyor |
|---|---|---|
| TEC.2 | (i) | Paydaş gereksiniminin, paydaşa ve ihtiyacına kadar izlenebilmesi |
| TEC.3 | (f) | Sistem gereksiniminin paydaş gereksinimine bağlanması |
| TEC.5 | (h) | Tasarım özelliklerinin mimari öğelere bağlanması |
| TEC.7 | (e) | Gerçekleştirmenin izlenebilir olması |
| TEC.8 | (h) | Entegre edilen öğelerin izlenebilir olması |
| TEC.9 | (g) | Doğrulanan öğelerin izlenebilir olması |
| TEC.10 | (h) | Geçişi yapılan öğelerin izlenebilir olması |
| TEC.11 | (h) | Geçerlenen öğelerin izlenebilir olması |

18.09.2026 süreç gözden geçirmesi, bu matrisin **hiç oluşturulmamış** olmasını en
yüksek önemli bulgu (BULGU-01) olarak kaydetti: tek başına bu eksik, sekiz sürecin
`PA 1.1 = F` almasını yapısal olarak engelliyordu.

---

## 2. Zincir

```
Paydaş ihtiyacı  →  PG-<MODÜL>-nn  →  REQ-<MODÜL>-nn  →  Tasarım  →  Kod / PR  →  Test  →  Kabul
   (toplantı)        (TEC.2)          (TEC.3)         (TEC.5)     (TEC.7)    (TEC.9)  (TEC.11)
```

Her halka bir öncekine **kimlikle** bağlanır. Bir halka kopuksa, kopukluk matriste
görünür — gizlenmez.

### 2.1 Sütunlar

| Sütun | Ne yazılır | Kaynak |
|---|---|---|
| **Kaynak** | Paydaş gereksinimi (`PG-…`) veya kurumsal karar (`KR-…`) | TEC.2 / karar kayıt defteri |
| **Sistem gereksinimi** | `REQ-<MODÜL>-nn` | TEC.3 |
| **Tasarım** | `ADR-nnnn` veya `docs/mimari/` altındaki belge | TEC.5 |
| **Gerçekleştirme** | Kod yolu + birleştiren PR numarası | TEC.7 |
| **Doğrulama** | Test dosyası veya CI kapısı | TEC.9 |
| **Kabul** | Kabul formu / kabul raporu | TEC.11 |

### 2.2 Kaynak neden iki türlü olabilir?

Zincirin başı çoğunlukla bir **paydaş gereksinimidir**. Ancak bazı kalemler paydaştan
değil, **kurumsal bir karardan** doğar: "LOGO'ya yazma yapılmayacak" gibi. Bunlar
`KR-` kimliğiyle girer ve aynı zinciri izler.

Bu ayrım bilinçlidir: kurumsal kısıtı paydaş gereksinimi gibi göstermek, gereksinimin
kaynağını yanlış kaydeder — ki izlenebilirliğin asıl amacı **kaynağı** doğru
tutmaktır.

---

## 3. Bakım kuralı

> **Matris, değişikliğin yapıldığı PR'da güncellenir.** Ayrı bir iş olarak
> bırakılmaz.

Gerekçe: ayrı bırakılan izlenebilirlik kaydı **çürür**. Bir hafta sonra kimse hangi
kodun hangi gereksinimden geldiğini hatırlamaz; matris "sonra doldurulacak" satırlarla
dolar ve kanıt değerini kaybeder. Denetimde değerli olan, bağın **iş yapılırken**
kurulmuş olmasıdır.

| Ne zaman | Kim | Ne yapar |
|---|---|---|
| Paydaş gereksinimi onaylandığında | Bilgi İşlem | Satırı açar; `PG` ve kaynak dolu, gerisi boş |
| Sistem gereksinimi yazıldığında | Bilgi İşlem | `REQ` sütununu doldurur |
| Kod birleştirildiğinde | PR sahibi | `Gerçekleştirme` ve `Doğrulama` sütunlarını doldurur |
| Modül kabul edildiğinde | Bilgi İşlem | `Kabul` sütununu doldurur |

**Boş hücre yasak değildir; açıklamasız boş hücre yasaktır.** Sırası gelmemiş bir
halka `—` ile, uygulanmayan bir halka `yok (gerekçe)` ile işaretlenir.

---

## 4. A1 — Teknik İskelet

> Bu bölüm, matrisin **çalıştığını gösterir**. A1'de paydaş gereksinimi üretilmedi
> (modüller başlamamıştı); zincirlerin başı kurumsal kararlardır. Buna rağmen karar →
> tasarım → kod → test zinciri **eksiksiz izlenebilmektedir**.

| # | Kaynak | Sistem gereksinimi | Tasarım | Gerçekleştirme | Doğrulama | Kabul |
|---|---|---|---|---|---|---|
| 1 | `KR-060` Denetim izi değiştirilemez | — (A2'de) | `ADR-0009` §2 | `src/backend/.../Audit/` · PR #12 | `AuditImmutabilityTests.cs`, `AuditTrailTests.cs` | — |
| 2 | `KR-061` Erişim kaydı fail-closed | — | `ADR-0009` §3 | PR #14 | `AccessLogTests.cs` | — |
| 3 | `KR-059` Günlükte iki katmanlı maskeleme | — | `ADR-0009` §4 | `MaskingDestructuringPolicy` · PR #10 | `MaskingDestructuringPolicyTests.cs`, `MaskTests.cs`, `MaskRulesTests.cs` | — |
| 4 | `KR-062` Hata ayrıntısı hiçbir ortamda sızmaz | — | `ADR-0010` §5 | Problem Details ara katmanı · PR #16 | `ProblemDetailsTests.cs`, `problemDetails.test.ts` | — |
| 5 | `KR-063` Sağlık kontrollerinin ayrımı | — | `ADR-0011` | `/health/live`, `/health/ready` · PR #22 | `HealthCheckTests.cs`, `PersonalDataScrubbingProcessorTests.cs` | — |
| 6 | `ADR-0002` Katman bağımlılık kuralları | — | `ADR-0002` | Katman projeleri · PR #8 | `LayerDependencyTests.cs` (NetArchTest) | — |
| 7 | `ADR-0004` Veritabanı adlandırma standartları | — | `ADR-0004` | EF yapılandırması · PR #8 | `DatabaseConventionTests.cs` | — |
| 8 | `KR-067` UAT ve üretimde TLS zorunlu | — | Runbook §7 | `nginx/tls.conf`, `tls-yenile.sh` · PR #43 | Dağıtım betiğinin doğrulaması (`uat-dagit.sh`) + elle ölçüm | — |

**Kabul sütunu neden boş?** TEC.11 geçerleme, **kabul edilecek bir modül** gerektirir.
A1 teknik iskelettir; İK kabulüne sunulan bir işlev üretmedi. İlk kabul T3 ile
gelecektir.

### 4.1 Zinciri tamamlanmamış kalemler

| Kaynak | Eksik halka | Neden |
|---|---|---|
| `KR-003`, `KR-004` LOGO salt-okunur | Kod ve test | LOGO yalıtım katmanı **T1**'de yazılacak. Kısıt bugün veritabanı düzeyinde `DENY` ile uygulanıyor ve elle doğrulandı; **depoda kanıt üreten bir test yok**. T1'de otomatik doğrulamaya bağlanacak. |
| `KR-056` Bildirim istisnası (#3) | Tasarım sonrası tüm halkalar | Y1 Bildirim Merkezi'nde uygulanacak (A3) |
| `KR-068` Modül başlangıç koşulu | Kod/test | Süreç kuralı; kodda karşılığı yok — `yok (süreç kuralı)` |

> Bu tablo bilinçli olarak tutuluyor: **kopuk halkayı göstermek, matrisi dolu
> göstermekten daha değerlidir.** Bir denetçi, eksiğin bilindiğini ve nereye
> bağlandığını görmek ister.

---

## 5. T3 — Kimlik Yönetimi

> İlk gerçek modül çevrimi. Satırlar, İK gereksinim toplantısı sonrası açılacak
> (`TEC.2/YAKLASIM.md` §3) ve modül ilerledikçe **aynı PR'larda** doldurulacak.

| # | Kaynak | Sistem gereksinimi | Tasarım | Gerçekleştirme | Doğrulama | Kabul |
|---|---|---|---|---|---|---|
| | *(toplantı sonrası açılacak)* | | | | | |

---

## 6. Kapsama ölçütü

Aşağıdaki oran, her modül kapanışında ölçülür ve öz değerlendirmeye girer:

```
İzlenebilirlik kapsaması = zinciri eksiksiz kalem / toplam kalem
```

Bir kalem, **kaynağından testine kadar** her halkası dolu (veya gerekçeli `yok`) ise
"eksiksiz" sayılır.

**A1 ölçümü:** 8 kalemden 8'i eksiksiz (kabul halkası hariç — sırası gelmedi) → **%100**.
LOGO kalemi zinciri tamamlanmamış olduğu için §4.1'de ayrıca listelendi ve bu orana
dâhil edilmedi; T1'de dâhil olacak.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-18 | 0.1 | İlk oluşturma — yapı, bakım kuralı ve A1 kalemleri (BULGU-01) | Bilgi İşlem |
