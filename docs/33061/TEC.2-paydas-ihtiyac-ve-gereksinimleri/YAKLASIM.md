# TEC.2 — Süreç Yaklaşımı

**Belge kimliği:** TEC.2-YAK
**Süreç:** TEC.2 — Paydaş İhtiyaç ve Gereksinimlerinin Tanımlanması
**Son güncelleme:** 2026-10-15
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

**Kritik performans ölçütleri (KPÖ) bilgi olarak sunulur.** Bilgi İşlem, modülün KPÖ
taslaklarını (yanıt süresi, eş zamanlı kullanıcı, teslim süresi gibi eşikler) toplantıdan
önce hazırlar. Taslaklar toplantıda İK'ya **bilgi olarak** sunulur ve toplantı kaydının
"Kritik performans ölçütleri" bölümüne İK'nın görüşüyle birlikte yazılır. KPÖ'ler PG'de
değil, SYG §5'te tutulur (§5 çıktı f; `KR-111`). Gerekçe: T3'te KPÖ-KMLK-1, UAT'de
karşılanmayınca Bilgi İşlem kararıyla yeniden tanımlandı (AN-25, `KR-095`) ve İK'ya hiç
gitmemişti (13.10 denetimi TEC.3-8).

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

**İstisna:** T3 paydaş gereksinimleri istisnaen `REQ-KMLK-<no>` kimliğiyle onaylandı
(`REQ-KMLK-001` … `REQ-KMLK-056`). Liste bu belgeden önce hazırlanmıştı ve İK'ya bu
kimliklerle sunuldu; imzalı toplantı kaydıyla depo arasındaki bağ kimliğin sabit kalmasına
dayandığı için kimlikler değiştirilmez (PG-KMLK §3). Sonraki modüller `PG-<MODÜL>-<no>`
biçimini kullanır.

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
| Değişiklik | Kesinleşmiş gereksinimin değişimi `tur:degisiklik-talebi` issue'su açar (`R-04`) ve §4.1'deki yordamla işlenir. **Otomatik:** değişiklik talebini kapatan PR, issue'da tarihli paydaş onayı yoksa birleştirilemez |
| Kişisel veri | Kayıtlar karar ve gerekçe taşır; kişisel veri içeren ekler depoya girmez |
| Erişim | Depo kurum içi; gereksinim özetleri İK ile paylaşılır |

> **Mutabakat kaydı olmadan gereksinim kesinleşmez.** TEC.2'nin (g) çıktısı
> *"paydaşların ihtiyaçlarının gereksinimlere yeterince yansıdığı konusunda mutabakatı
> sağlanır"* der. Mutabakat, toplantı kaydındaki **katılımcı onayı** satırıyla kayda
> geçer. Sözlü mutabakat, kanıt üretmez.

### 4.1 Değişiklik talebi yordamı (#210)

Onaylı bir paydaş gereksinimini değiştiren her istek bu yolu izler. Yordam T3'teki üç
değişiklik talebinin (#77, #185, #190) deneyiminden yazıldı: yol büyük ölçüde denetimliydi,
ama #185'te uygulama İK onayı kayda geçmeden birleşti ve UAT'ye kuruldu; onaylar ikinci
elden kaydedildi; #190'ın kapsamı PR sırasında genişledi (T3 modül sonu denetimi M-04).

| # | Adım | Kanıt |
|---|---|---|
| 1 | Değişiklik talebi şablonuyla issue açılır (`tur:degisiklik-talebi`, `surec:TEC.2`): ne değişsin, neden, etkilenen gereksinimler (PG, SYG, ADR, parametre), tahmini etki | Issue |
| 2 | Etki analizi issue'ya yazılır: hangi PG ve SYG maddeleri değişir, hangi testler, kabul planı etkileniyor mu | Issue yorumu |
| 3 | **Paydaş onayı** issue'ya yazılır: `Paydaş onayı: GG.AA.YYYY — <onaylayan rol> — <kanal>`. Kanal toplantı kaydı, e-posta veya yazılı form olabilir; yazılı bir kanıt varsa bilgi kayıt defterinde numarası verilir. Onayı Bilgi İşlem iletiyorsa "(Doğuş Uçanok bildirdi)" eklenir | Issue gövdesi veya yorumu |
| 4 | PG, SYG, izlenebilirlik matrisi ve gerekiyorsa ADR **aynı PR'da** güncellenir. PG değişiklik geçmişine talep numarası ve onay tarihi yazılır | PR |
| 5 | Uygulama PR'ı **3. adımdan sonra** birleşir. **Otomatik:** PR izlenebilirlik denetimi, kapatılan issue değişiklik talebiyse tarihli onay satırını arar; yoksa PR birleştirilemez | CI |
| 6 | Kapsam PR sırasında genişlerse issue'ya **"Kapsam değişikliği"** başlıklı bir yorum yazılır ve yeni kapsam için 3. adım yinelenir | Issue yorumu |
| 7 | Değişiklik, kabul planında İK'nın teyit edeceği yorumlar arasına (`T-xx`) eklenir ve kabulde gösterilir | Kabul planı §6.1, kabul formu |

**Onay olmadan yapılabilecekler:** Etki analizi, taslak PR ve UAT dışındaki ortamlarda
deneme. UAT'ye kurulum ve `main`'e birleştirme onaydan sonradır.

---

## 5. Sürecin çıktıları ile bu belgenin eşlemesi

| TEC.2 çıktısı | Nerede karşılanır |
|---|---|
| a) Paydaşlar tanımlanır | `paydas-listesi.md` |
| b) Yetenek ve kullanım bağlamı tanımlanır | `vizyon-ve-kapsam.md` + toplantı kaydı §2 |
| c) Kısıtlar belirlenir | Toplantı kaydı §5 + `karar-kayit-defteri.md` |
| d) Paydaş ihtiyaçları tanımlanır | Toplantı kaydı §3 |
| e) İhtiyaçlar önceliklendirilir ve gereksinime dönüştürülür | `PG-<MODÜL>.md` |
| f) Kritik performans ölçütleri tanımlanır | `SYG-<MODÜL>.md` §5 (KPÖ tablosu); taslaklar İK toplantısında bilgi olarak sunulur ve toplantı kaydına yazılır (§2.2) |
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
| 2026-10-14 | 0.2 | §4: değişiklik talebi otomatik denetimi; §4.1 değişiklik talebi yordamı (#210) | Bilgi İşlem |
| 2026-10-15 | 0.3 | §2.2: KPÖ taslakları toplantıda bilgi olarak sunulur; §3.1: T3'ün `REQ-KMLK` kimlik istisnası; §5 (f): KPÖ'ler SYG §5'te, PG şablonundaki "Ölçüt" alanı kaldırıldı (`KR-111`, #217) | Bilgi İşlem |
