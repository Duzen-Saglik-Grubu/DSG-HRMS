# TEC.2 — Süreç Yaklaşımı

**Belge kimliği:** TEC.2-YAK
**Süreç:** TEC.2 — Paydaş İhtiyaç ve Gereksinimlerinin Tanımlanması
**Son güncelleme:** 2026-09-18
**Karşıladığı öznitelik maddeleri:** `PA 2.2 (a)` sürecin dokümante edilmiş bilgi
gereksinimleri belirlenir · `PA 2.2 (b)` bu bilginin kontrol gereksinimleri belirlenir

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Süreç çıktılarının
> kendisi değildir; çıktıların **kuralıdır**.

---

## 1. Sürecin amacı ve sınırı

Bu süreç, sistemin kullanıcıların ve diğer paydaşların ihtiyaç duyduğu yetenekleri
sağlayabilmesi için **paydaş gereksinimlerini** tanımlar (TS ISO/IEC TS 33061,
Madde 5, TEC.2).

**Kapsamdadır:** paydaşların belirlenmesi, ihtiyaçların toplanması, önceliklendirilmesi
ve **anlaşılır paydaş gereksinimlerine** dönüştürülmesi; mutabakatın alınması.

**Kapsamda değildir:** teknik çözümün tanımlanması. Paydaş gereksinimi *ne* istendiğini
söyler, *nasıl* yapılacağını değil. "Nasıl", TEC.3 (sistem gereksinimleri) ve TEC.5
(tasarım) süreçlerine aittir.

> Bu ayrım pratikte en sık kaybedilen şeydir. "İzin ekranında bir tarih seçici olsun"
> bir paydaş gereksinimi değildir; paydaş gereksinimi "çalışan izin talebini başlangıç
> ve bitiş tarihi vererek oluşturabilmelidir"dir. Çözümü gereksinime yazmak, daha iyi
> bir çözümün önünü kapatır.

---

## 2. Gereksinimler nasıl toplanır?

### 2.1 Modül başına toplantı (`KR-068`)

**Her modül, o modüle özel İK gereksinim toplantısıyla başlar.** Teknik ön koşulun
karşılanmış olması tek başına yeterli değildir; modülün geliştirmesine, gereksinimler
İK onayından geçmeden başlanmaz.

### 2.2 Toplantı öncesi hazırlık

Hazırlık **teşvik edilir**: geçmiş notlardan taslak gereksinim listesi çıkarmak, mevcut
sistemdeki veri kalitesini ölçmek, açık soruları belirlemek toplantıyı verimli kılar.

> **Ancak taslak, toplantı GİRDİSİDİR; onaylanmış gereksinim değildir.** Bu ayrım
> yazılı tutulmalıdır, çünkü iyi hazırlanmış bir taslak kolayca "zaten karar verilmiş"
> gibi görünür ve toplantı bir onay törenine dönüşür. Taslak, tartışmayı başlatmak
> içindir; bitirmek için değil.

Taslak listeler kişisel veri içerebilir (ör. mevcut sistemden örnek kayıtlar); bu
durumda depoya **girmez**, ilgili kişilere ayrıca iletilir.

### 2.3 Paydaş seçimi

Toplantıya kimin çağrılacağı `paydas-listesi.md` üzerinden belirlenir ve liste **her
toplantı öncesinde gözden geçirilir**. Paydaş atlanması, gereksinim eksikliğinin en
yaygın nedenidir.

---

## 3. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Çıktı | Dosya | Ne zaman |
|---|---|---|
| Paydaş listesi | `paydas-listesi.md` | Sürekli; her toplantı öncesi gözden geçirilir |
| Hayat döngüsü kavramları | `docs/mimari/vizyon-ve-kapsam.md` | A0'da üretildi; modülle güncellenir |
| **Toplantı kaydı** | `kayitlar/YYYY-AA-GG-<modül>-gereksinim-toplantisi.md` | Her toplantıdan sonra **48 saat içinde** |
| **Paydaş gereksinimleri** | `paydas-gereksinimleri/PG-<MODÜL>.md` | Toplantı kaydından türetilir |
| Geçerleme kriterleri | Her gereksinimin "Kabul kriteri" alanı | Gereksinimle **birlikte** |
| İzlenebilirlik | `docs/33061/izlenebilirlik-matrisi.md` | Gereksinim kesinleştiğinde |

Şablonlar: `kayitlar/SABLON-ik-gereksinim-toplantisi.md` ve
`paydas-gereksinimleri/SABLON-PG.md`.

### 3.1 Kimliklendirme

`PG-<MODÜL>-<no>` — modül kısaltması büyük harf, numara iki hane: `PG-KMLK-07`.

**Numara yeniden kullanılmaz.** Bir gereksinim iptal edilirse kaydı durur, durumu
`İptal` olur. Silinen bir numara, eski bir belgede ona yapılan atfı anlamsız kılar.

### 3.2 Kabul kriteri gereksinimin parçasıdır

Her paydaş gereksinimi, **nasıl doğrulanacağını** söyleyen bir kabul kriteri taşır.
Kriter yoksa gereksinim tamamlanmamıştır.

Gerekçe: TEC.11 (Geçerleme) *"paydaş gereksinimleri için geçerleme kriterleri
tanımlanır"* çıktısını ister ve bu kriterler **buradan** gelir. Kabul testini
modül bittikten sonra yazmak, testi kodun şekline uydurur — yani hiçbir şey ölçmez.

Kabul kriteri **ölçülebilir** olmalıdır: "hızlı açılmalı" değil, "liste 2 saniyede
gelmeli".

---

## 4. Bilginin kontrolü (`PA 2.2 b`)

| Kontrol | Kural |
|---|---|
| Sürüm | Her belge Git'te; değişiklik **Pull Request** ile yapılır |
| Onay | En az bir gözden geçiren onayı (CONTRIBUTING §5) |
| Kimlik | `PG-<MODÜL>-<no>`; numara yeniden kullanılmaz |
| Tarih ve sahip | Her belgede "Son güncelleme" ve değişiklik geçmişi |
| Değişiklik | Kesinleşmiş gereksinimin değişimi `tur:degisiklik-talebi` issue'su açar (`R-04`) |
| Kişisel veri | Kayıtlar karar ve gerekçe taşır; kişisel veri içeren ekler depoya girmez |
| Erişim | Depo kurum içi; gereksinim özetleri İK ile paylaşılır |

> **Mutabakat kaydı olmadan gereksinim kesinleşmez.** TEC.2'nin (g) çıktısı
> *"paydaşların ihtiyaçlarının gereksinimlere yeterince yansıdığı konusunda mutabakatı
> sağlanır"* der. Mutabakat, toplantı kaydındaki **katılımcı onayı** satırıyla kayda
> geçer. Sözlü mutabakat, kanıt üretmez.

---

## 5. Sürecin çıktıları ile bu belgenin eşlemesi

| TEC.2 çıktısı | Nerede karşılanır |
|---|---|
| a) Paydaşlar tanımlanır | `paydas-listesi.md` |
| b) Yetenek ve kullanım bağlamı tanımlanır | `vizyon-ve-kapsam.md` + toplantı kaydı §2 |
| c) Kısıtlar belirlenir | Toplantı kaydı §5 + `karar-kayit-defteri.md` |
| d) Paydaş ihtiyaçları tanımlanır | Toplantı kaydı §3 |
| e) İhtiyaçlar önceliklendirilir ve gereksinime dönüştürülür | `PG-<MODÜL>.md` |
| f) Kritik performans ölçütleri tanımlanır | `PG` kaydındaki "Ölçüt" alanı |
| g) Paydaş mutabakatı sağlanır | Toplantı kaydı §7 (katılımcı onayı) |
| h) Destekleyici sistem/hizmetler mevcut | Toplantı kaydı §6 |
| i) İzlenebilirlik kurulur | `izlenebilirlik-matrisi.md` |

---

## 6. Gözden geçirme

Bu yaklaşım belgesi, **her üçüncü modül toplantısından sonra** ve süreç gözden
geçirmelerinde ele alınır. Şablonlar pratikte işe yaramıyorsa değiştirilir —
uyulmayan bir şablon, olmayan bir şablondan kötüdür: uyum varmış gibi görünür.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-18 | 0.1 | İlk oluşturma — ilk İK gereksinim toplantısı öncesi (BULGU-02, BULGU-09) | Bilgi İşlem |
