# MAN.4 — Süreç Yaklaşımı

**Belge kimliği:** MAN.4-YAK
**Süreç:** MAN.4 — Risk Yönetimi
**Son güncelleme:** 2026-10-05
**Karşıladığı öznitelik maddeleri:** `PA 2.1`: hedefler, gözden geçirme sıklığı, sorumluluklar ve yetkiler, arayüzler. `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir. `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir.

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Ölçek, eşik ve kabul kuralı
> `risk-kayit-defteri.md` §1'dedir; bu belge onları süreç adımlarına bağlar.

---

## 1. Sürecin amacı ve sınırı

Risk yönetimi, projenin risklerini **sürekli** belirler, analiz eder, ele alır ve izler
(TS ISO/IEC TS 33061, MAN.4).

**Kapsamdadır:** proje, ürün, altyapı, güvenlik, KVKK ve süreç riskleri; risk kabulü;
riskin paydaşlara iletilmesi.

**Kapsamda değildir:** gerçekleşmiş bir olayın giderilmesi. Olay `[HATA]` issue'su ve
olay kaydıyla yönetilir (TEC.13). Risk niteliğindeki olay ayrıca risk defterine işlenir (§2.5).

---

## 2. Süreç nasıl işletilir?

### 2.1 Tek kaynak

Riskin tek kaydı `risk-kayit-defteri.md`'dir. Başka belgede anılan risk (ADR, karar,
denetim raporu) defterdeki `R-NN` numarasına atıf yapar. 05.10.2026'da defterde 26 kayıt
vardır (R-01…R-26): 20 açık veya izlenen, 6 kapalı.

### 2.2 Belirleme: riskler nereden gelir?

| Kaynak | Örnek |
|---|---|
| Planlama ve kapsam | `R-04` kapsam kayması, `R-15` kapsam büyüklüğü |
| Veri ölçümü (LOGO) | `R-07`, `R-08`, `R-12`, `R-13` |
| Kurulum ve altyapı | `R-16` (GitHub kurulumu), `R-17`, `R-18` (UAT TLS) |
| Karardaki kalan risk | `R-19` (`KR-078`) |
| Gerçekleşen olay | `R-21` (sunucu saati, #103), `R-26` (`git stash -u` olayı) |
| Süreç denetimi | `R-23` (bağımsız olmayan inceleme), `R-25` (UAT'de gerçek veri) |

Bir karar "kalan risk kabul edilir" diyorsa, aynı PR'da defterde bir kayıt açılır.

### 2.3 Analiz

Her risk için olasılık (O) ve etki (E) 1–3 arasında verilir; **puan = O × E**.
Puan, kabul yetkisini belirlediği için gerekçesiyle yazılır (örnek: `R-19` "Puan gerekçesi" satırı).
**Puanı 6 ve üzeri olan risk aktif takiptedir** ve her durum raporunda ayrıca ele alınır.

### 2.4 Ele alma ve kabul

- Her riskin bir **sahibi** ve numaralı **önlemleri** vardır. Uygulanan önlem, kaydın içinde tarihle işaretlenir.
- Durum değerleri: `Açık`, `İzleniyor`, `Kapandı`, `Gerçekleşti`, `Kabul edildi`.
- **Kabul yetkisi (RACI §3.2):**

| Puan | Kabul eden | Örnek |
|---|---|---|
| 6 ve üzeri | Üst Yönetim | `R-25`: kabul için Üst Yönetime sunulacak |
| 6'nın altı | Bilgi İşlem | `R-19`: Bilgi İşlem, 25.09.2026 (`KR-078`) |

Kabul edenin adı ve tarih kayda yazılır.

### 2.5 Gözden geçirme

Defter şu durumlarda gözden geçirilir (`risk-kayit-defteri.md` §1):

| Tetikleyici | Ne yapılır |
|---|---|
| Modül kapanışı | Tüm açık riskler; modülde ortaya çıkan yeni riskler |
| Ayda bir | Puanı 6 ve üzeri riskler; tarihli önlemler (örn. `R-20` sertifika bitişi 27.03.2027) |
| Risk niteliğinde olay (kesinti, güvenlik bulgusu, ortam arızası) | Olay kaydıyla birlikte ilgili risk güncellenir veya yeni risk açılır |

Son gözden geçirme 04.10.2026'dır (#126). Aylık sınır bu tarihten sayılır.

Her gözden geçirmede aynı PR'da şunlar birlikte güncellenir:

1. ilgili risk kayıtları;
2. §3 risk özeti tablosu ve aktif takip listesi;
3. "Son gözden geçirme" satırı;
4. değişiklik geçmişi.

Özetin unutulması daha önce yaşandı (16.09.2026, `R-17`; 04.10.2026, `R-01`). Kayıt eklemek
yetmez; özet de aynı anda düzeltilir.

### 2.6 İletişim

- **Üst Yönetim:** puanı 6 ve üzeri riskler ve kabul bekleyen riskler durum raporunda ayrı başlıkla sunulur (MAN.2 §2.5). Ayrı bir risk raporu gerekirse `raporlar/YYYY-AA-risk-raporu.md` olarak yazılır.
- **İK:** kendi kabulünü ilgilendiren kalan riskler kabulde gösterilir (`R-19`, T3 kabul planı).
- **Danışılanlar:** KVKK riskleri KVKK Sorumlusuna, İSG riskleri İSG Uzmanına danışılır (RACI §3.1).

---

## 3. Roller

| Rol | Görev |
|---|---|
| Bilgi İşlem (Doğuş Uçanok) | Defterin sahibi; riskleri belirler, analiz eder, izler (RACI: A/R). Puanı 6'nın altındaki riski kabul eder. Değişiklikleri onaylar (`KR-096`) |
| Geliştirme yardımcısı (Claude) | Olay ve kararlardan risk adayı çıkarır, kayıt taslağı yazar. Puanı ve kabulü onaylamaz |
| Üst Yönetim | Puanı 6 ve üzeri riskin kabulü |
| İK, KVKK Sorumlusu, İSG Uzmanı | Danışılır (C) |

---

## 4. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Risk kaydı | `risk-kayit-defteri.md` §2 | Kategori, açıklama, O/E/puan, sahip, önlem, durum | Bilgi İşlem |
| Risk özeti | `risk-kayit-defteri.md` §3 | Puana göre sayılar, aktif takip, kabul edilenler, gözden geçirme geçmişi | Bilgi İşlem |
| Risk kabulü | Risk kaydının "Durum" satırı + `KR-NNN` | Kabul eden, tarih, puan gerekçesi | Kabul yetkilisi |
| Olay kaydı | `TEC.13-bakim/kayitlar/` | Ne oldu, etki, önlem; ilgili `R-NN` | Bilgi İşlem |
| Risk iletişimi | MAN.2 durum raporu; gerekirse `raporlar/` | Puanı 6 ve üzeri riskler | Bilgi İşlem |
| Risk işi | GitHub issue, `surec:MAN.4` | Şablonlu issue | Bilgi İşlem |

---

## 5. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** Defter yalnızca PR ile değişir. İnceleme onayı birleştirmeden önce yazılır (`KR-096`).
- **Numara yeniden kullanılmaz.** Kapanan risk silinmez; durumu `Kapandı` olur ve gerekçesi kalır.
- **Puan geçmişi korunur.** Puan değişirse eski değer ve tarih kayıtta kalır (örn. `R-02`: "6'dan düşürüldü").
- **Tarih ve sahip:** Başlıkta "Son güncelleme", sonda değişiklik geçmişi.
- **Kişisel veri:** Kayıtlar kişi sayısı taşır, kişi bilgisi taşımaz (örn. `R-08`: "2 kişi").
- **Saklama:** Git geçmişi tüm sürümleri saklar. Saklama ve imha kuralı MAN.6'da tanımlanacak (#131).

---

## 6. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (MAN.4) | Karşılığı |
|---|---|
| a) Riskler belirlenir | §2.2, defter §2 |
| b) Riskler analiz edilir | §2.3, O/E/puan |
| c) Ele alma seçenekleri belirlenir ve seçilir | §2.4, önlem satırları, kabul kuralı |
| d) Uygun önlem uygulanır | Önlemlerin tarihli işaretlenmesi, ilgili PR'lar |
| e) Riskler durum ve önlem ilerlemesi açısından değerlendirilir | §2.5 gözden geçirme, §2.6 iletişim |

---

## 7. Açık noktalar

| # | Açık nokta | İzlendiği yer |
|---|---|---|
| 1 | `R-25` (puan 6) kabul için Üst Yönetime sunulmadı | #127 |
| 2 | Risk profili hiçbir zaman Üst Yönetime iletilmedi; durum raporu yok | #127 |
| 3 | Defterin bölüm sırası bozuk: R-17…R-26 §3 özetinin arkasında, R-11 R-13'ten sonra | — |
| 4 | T3 boyunca (24.09–02.10.2026) deftere hiç risk eklenmedi; olay tetikleyicisi 04.10.2026'da yazıldı. Yeni kuralın işlediği henüz görülmedi | Sonraki modül sonu denetimi |
| 5 | PR şablonunda "risk etkisi" sorusu yok; risk adayı yakalamak gözden geçirene bağlı | — |

---

## 8. Gözden geçirme

Bu belge her modül sonu süreç denetiminde (MAN.8) gözden geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
| 2026-10-06 | 1.1 | §4 örnek: R-20 yeni bitiş tarihi (#179) | Bilgi İşlem |
