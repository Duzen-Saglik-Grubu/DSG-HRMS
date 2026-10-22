# Tasarım Gözden Geçirme Raporu — T1 Personel Yönetimi (ilk kod PR'ından önce)

**Belge kimliği:** TEC.5-RAP-2026-10-22
**Süreç:** TEC.5 — Tasarım Tanımlama (YAKLAŞIM §2.5)
**Tarih:** 2026-10-22
**Kapsam:**
- ADR-0019…0022 (`KR-121`…`KR-124`) ve teyitleri (`KR-125`, #266);
- PG-PERS (45 madde) ve toplantı kaydı;
- mevcut kodun bu tasarıma hazırlığı.

**Hazırlayan:** Bilgi İşlem; bağımsız inceleme bölümü (§4) ayrı bir alt ajan tarafından
**İlgili işler:** #267 (bu rapor), #268 (ADR revizyonu)

> **Neden şimdi?** TEC.5 YAKLAŞIM §2.5'e göre her modülün tasarımı ilk kod PR'ından önce
> bir kez gözden geçirilir. Bu, kuralın ikinci uygulamasıdır; ilki T3 için yapıldı
> (`2026-10-20-tasarim-gozden-gecirme.md`).

---

## 1. Özet

| | |
|---|---|
| Bağımsız inceleme bulguları (B) | 19: Yüksek 4, Orta 11, Düşük 4 |
| Bilgi İşlem bulguları (BG) | 5. Üçü bağımsız bulguları ölçümle doğruluyor, biri aynı bulguya bağımsız olarak varıyor |
| Bilgi İşlem'in yeniden doğruladığı bulgular | B-01, B-02, B-03, B-04, B-05, B-11, B-13 |
| Karar gerekenler | 4: B-03 (KVKK), B-05 (İK), B-14, B-15 (İK) |
| Bağlanan iş | #268. Bütün bulgular SYG-PERS'ten önce ADR'lere işlenir |

**Sonuç:** Tasarımın ana yönleri sağlam:
- her alanın tek kaynağı var;
- görünürlük tek bir politikada toplanıyor;
- blind index bilinçli olarak reddedildi;
- yatay çekirdekler dar kapsamlı.

Ancak **dört yüksek önemli bulgu**, tasarım koda geçmeden düzeltilmelidir. Bunlardan biri
kan grubunun erişim kaydından sızması (B-01). Diğer üçü şifreleme tasarımının uygulanamayan
iki parçası (B-02, B-03) ve bir ayarın iki ADR'de farklı tanımlanması (B-04). SYG-PERS, #268
kapanmadan yazılmaz.

---

## 2. Yöntem ve bağımsızlık sınırı

| Adım | Kim | Ne yapıldı |
|---|---|---|
| 1 | Bilgi İşlem | Kapsam ve sorular belirlendi |
| 2 | Alt ajan (bağımsız inceleme) | ADR'ler, PG-PERS, toplantı kaydı, karar defteri ve ilgili kod okundu. Önceki tasarım gözden geçirme ve MAN.8 raporları **okunmadan**, Bilgi İşlem görüşü verilmeden bulgu çıkarıldı (§4) |
| 3 | Bilgi İşlem | Kendi bulguları çıkarıldı (§3). Yüksek önemli bulgular kodda ve LOGO meta verisinde doğrulandı; her bulguya değerlendirme ve öneri yazıldı |

**Bağımsızlık sınırı (R-23):** Alt ajan, ADR'leri yazan geliştirme yardımcısıyla aynı model
ailesindendir; kurum dışı veya insan bağımsızlığı sağlamaz. Kapsamı Bilgi İşlem belirledi.
Sağladığı bağımsızlık iki noktadadır: alt ajan önceki raporları ve Bilgi İşlem görüşünü
görmeden çalıştı, bulguları da §4'te değiştirilmeden gösterilir.

---

## 3. Bilgi İşlem bulguları (BG)

| No | Bulgu | Kaynak | Sonuç |
|---|---|---|---|
| BG-01 | `Logo:SyncSpecialCategoryData` ADR-0020 §6'da yapılandırma ayarı, ADR-0022 §5'te Sistem Yönetimi parametresi | Belge karşılaştırması (alt ajandan önce bulundu) | B-04 ile aynı; #268 |
| BG-02 | LOGO'da "çalışma şekli" için `LH_001_PERSON.WORKTYPE` kolonu var ama **kullanılmıyor**: 588 aktif kartın tamamında 0. `CONTRACTTYPE`'ta da 587 kart 0, 1 kart 1 (22.10.2026, yalnızca sayım) | LOGO meta verisi ve sayım | B-05'i doğruluyor: PG-PERS-12'deki "çalışma şekli"nin LOGO'da verisi yok |
| BG-03 | Aile/resim tablosunda **değiştirilme tarihi yok**; `LDATA` eski `image` türünde, bu yüzden `HASHBYTES` için `varbinary(max)`'a `CAST` gerekir. Personel kartında `CAPIBLOCK_MODIFIEDDT` var; fotoğraf değişikliğinin bu tarihi güncelleyip güncellemediği bilinmiyor | LOGO meta verisi | B-13'ü doğruluyor; UAT'de ölçülür |
| BG-04 | ADR-0020 §3'teki döndürme yordamı ("eski değerler sonraki senkronizasyonda yeniden yazılır") çalışmaz: senkronizasyon yalnızca değişen alanı yazar | Kod (`Person.cs`) | B-03 doğru; #268 |
| BG-05 | ADR-0019 §1'deki "doğrudan `Set<Person>()` erişimini sınırlayan mimari testi" mevcut araçla (NetArchTest, tip düzeyi) yazılamaz | Test projesi bağımlılıkları | B-11 doğru; #268 |

---

## 4. Bağımsız inceleme bulguları (B)

> Bu bölüm alt ajanın çıktısıdır. Dili sadeleştirildi, içeriği değiştirilmedi. "Bilgi İşlem
> değerlendirmesi" sütununu Bilgi İşlem ekledi.

**Okunanlar:**
- **ADR'ler:** 0019–0022 tamamı. 0004, 0007, 0009 ve 0014 tamamı. 0005 §3–§5, 0010 §4, 0012 §4, 0016 ve README dizini.
- **Gereksinim ve kayıtlar:** PG-PERS (45 madde), toplantı kaydı §3.3–§13, karar defterinde KR-112…KR-125.
- **Kod:**
  - `Person.cs`, `LogoPersonnelSource.cs`, `AccessLogger.cs`, `MaskRules.cs`, `AesGcmSecretProtector.cs`;
  - `DataClassificationTests.cs`, `ApplicationRoleTests.cs`;
  - `Set<Person>()` kullanımları, `izin-listesi.md` ve API `Dockerfile`'ı.

| No | Önem | Bulgu | Kanıt | Bilgi İşlem değerlendirmesi ve öneri |
|---|---|---|---|---|
| B-01 | Yüksek | Erişim kaydındaki süzgeç değeri adına göre maskeleniyor; `bloodType` veya `address` listede yok. ADR-0019 §5 liste kaydına kişi kimliklerini de yazıyor. Sonuç: "kan grubu = 0 Rh−" ve 24 kişi aynı kayıtta, 10 yıl boyunca | `AccessLogger.cs:69-83`; `MaskRules.cs`; ADR-0019 §5 | **Doğrulandı. Katılıyorum.** Korumalı alan süzgeçlerinin değeri erişim kaydına yazılmaz, yalnızca süzgecin kullanıldığı yazılır. Süzgeç adları alan sınıflandırmasından türetilir. Bunu doğrulayan test eklenir |
| B-02 | Yüksek | AAD'nin `public_id`'ye bağlanması EF değer dönüştürücüsüyle yapılamaz; dönüştürücü satırın başka alanını görmez | ADR-0020 §1; `Entity.cs` | **Doğrulandı. Katılıyorum.** Öneri: Özel nitelikli değerler ayrı bir tabloda tutulur ve şifreleme depo katmanında yapılır. AAD = tablo + alan + kişinin `public_id`'si. Bu, B-11'i de çözer |
| B-03 | Yüksek | Döndürme ve anahtar kaybı yordamı uygulanamıyor. (1) Değişmeyen alan yeniden yazılmaz. (2) Düzeltme talebindeki kan grubu önerisi HRMS'te girilmiştir, LOGO'dan üretilemez | ADR-0020 §3; ADR-0022 §3 | **Doğrulandı (BG-04).** Açık bir yeniden şifreleme işi tanımlanır; bu iş denetim izine "değişti" gürültüsü yazmaz. **Karar gerekli (KVKK):** önerilen kan grubu talep kapandığında silinsin mi? Önerim: evet, talep kapanınca önerilen değer silinir, yalnızca alan ve durum kalır |
| B-04 | Yüksek | `Logo:SyncSpecialCategoryData` iki yerde farklı tanımlı. Ekrandan değiştirilebilirse yerelde açılabilir (`KR-125`); kapatılınca mevcut değerlere ne olacağı tanımsız | ADR-0020 §6; ADR-0022 §5 | **Katılıyorum (BG-01).** Ortam yapılandırmasıdır, ekrandan değiştirilemez. Kapatılınca alan bir sonraki senkronizasyonda temizlenir |
| B-05 | Orta | PG-PERS-12 "çalışma şekli"ni tarih aralıklı ister; ADR-0021 ve LOGO eşlemesi bu alanı anmıyor. T1 öncesi tarih sorulduğunda ne döneceği tanımsız | PG-PERS-12; ADR-0021 | **Doğrulandı (BG-02):** LOGO'da veri yok. **Karar gerekli (İK):** (a) değişiklik talebiyle kapsamdan çıkar, (b) HRMS'te girilen alan olur, (c) LOGO'da doldurulur. T1 öncesi tarih için "kayıt yok" döner |
| B-06 | Orta | Tarih aralığı kuralları eksik: yumuşak silme ile EXCLUDE, aynı gün iki değişiklik, gün sınırının saat dilimi, geçmişe dönük görev, istihdam bitince açık görev, 23P01 → 500 | ADR-0021 §2, §6; ADR-0004 §7 | **Katılıyorum.** Öneri: `EXCLUDE … WHERE deleted_at IS NULL`; gün sınırı Europe/Istanbul; aynı gün ikinci değişiklik açık kaydı günceller; geçmişe dönük görev çakışmıyorsa kabul edilir; istihdam bitince açık görev çıkış tarihinde kapanır; çakışma `409` döner. Her kural entegrasyon testiyle sınanır |
| B-07 | Orta | Görünürlük politikası sıralamaya uygulanmıyor; "kişinin kendisi" kayıt düzeyinde, filtre ve dışa aktarma küme düzeyinde | ADR-0019 §2 | **Katılıyorum.** Sıralama politikanın dördüncü kullanım yeri olur. Küme işlemlerinde (filtre, sıralama, dışa aktarma) yalnızca izin esas alınır |
| B-08 | Orta | Kan grubuna göre filtre ve düzeltme talebinde önerilen kan grubunu görmek `SpecialCategoryView` kaydı üretmiyor | ADR-0019 §5; ADR-0022 §3 | **Katılıyorum.** Bu iki durumda kişi başına `SpecialCategoryView` kaydı üretilir |
| B-09 | Orta | Dışa aktarma kaydında kişi kimlikleri yok | ADR-0019 §5; ADR-0022 §4 | **Katılıyorum.** Aktarılan kişilerin kimlikleri yazılır |
| B-10 | Orta | Bellek içi filtrede sayfalama ve toplamın sırası belirtilmemiş | ADR-0020 §4 | **Katılıyorum.** Kan grubu süzgeci varsa süzme, sıralama, sayfalama ve sayma bellekte yapılır; toplam testle doğrulanır |
| B-11 | Orta | Yan kapı testi NetArchTest ile tasarlandığı gibi kurulamaz; Kimlik `Person`'ı bütün olarak yükler | ADR-0019 §1, §3; `SessionStore.cs`, `AccountAdministrationStore.cs` | **Doğrulandı (BG-05). Katılıyorum.** Öneri: korumalı alanlar `Person`'da değil ayrı tablolarda tutulur (adres, kan grubu; evlilik ve acil durum zaten ayrı). Kimlik modülünün bu tiplere bağımlılığı tip düzeyindeki mimari testiyle yasaklanır |
| B-12 | Orta | Sınıflandırma testi yalnızca `string`/`DateOnly` alanlarını ve `Personnel` ad alanını tarıyor; `bool`, enum ve yeni ad alanları dışarıda | `DataClassificationTests.cs` | **Katılıyorum.** Test bütün skaler alanları (kimlik ve yabancı anahtar hariç) ve `Reference` ile bildirim ad alanlarını kapsar |
| B-13 | Orta | `HASHBYTES` LOGO sunucusunda yine bütün `LDATA`'yı her 15 dakikada okur | ADR-0021 §5; PG-PERS-41 | **Doğrulandı (BG-03).** UAT'de ölçülür. Ölçüm kötüyse önerilen sıra: (1) `DATALENGTH` ön kontrolü, (2) fotoğraf özetinin günde bir kez alınması |
| B-14 | Orta | Eksik izinler: düzeltme taleplerini listeleme ve kapatma, veri kalitesi raporu ve onun dışa aktarımı. `personnel.sync.create`'in kimde olacağı yazılmamış | ADR-0019 §4; `izin-listesi.md` | **Katılıyorum.** Öneri: `personnel.correction.view/update` ve `personnel.quality.view/export` İK'ya verilir; `personnel.sync.create` bugünkü gibi yalnızca Sistem Yöneticisinde kalır. **Teyidin gerekli** |
| B-15 | Orta | Çok sicilli kişide (13 kişide aynı anda iki aktif sicil var) liste kartında hangi şube ve ünvanın görüneceği, filtrenin hangi istihdama bakacağı, aktif/ayrılan ayrımı ve PG-PERS-38'deki sürenin başlangıcı tanımsız | PG-PERS-09, 13, 16, 38 | **Katılıyorum. Karar gerekli (İK).** Öneri: <br>• Kart, aktif istihdamlardan işe giriş tarihi en yeni olanı gösterir ve "2 sicil" işareti taşır. <br>• Filtre herhangi bir istihdamda eşleşir; "aktif" süzgeci yalnızca aktif istihdamlara bakar. <br>• Kişi, en az bir aktif istihdamı varsa aktiftir. <br>• Görünürlük süresi son çıkış tarihinden başlar |
| B-16 | Düşük | Belge tutarsızlıkları: `view_sensitive` açıklamasında evlilik yok; yeni eylem adları ADR-0007 sözlüğünde yok; ADR-0009 Sonuçlar "şifreli alan filtre olamaz" diyor; `branch`/`department` ile `organization_unit` | Belirtilen bölümler | **Katılıyorum.** #268'de düzeltilir |
| B-17 | Düşük | Arama anahtarı erkeklerde de önceki soyadını içeriyor; tam TCKN ile arama `view_national_id`'ye bağlı değil; "içinde geçen" aramada B-tree dizini işlemez | ADR-0021 §7 | **Katılıyorum.** Önceki soyadı yalnızca gösterildiği koşulda anahtara girer; TCKN araması izne bağlanır; dizin `pg_trgm` veya ön ek araması olarak yeniden değerlendirilir |
| B-18 | Düşük | Fotoğraf ucunda önbellek başlıkları ve erişim kaydı; düzeltme talebinin saklama süresi; e-posta konu satırının içeriği tanımsız | ADR-0021 §5; ADR-0022 §3 | **Katılıyorum.** `Cache-Control: private`; konu satırı yalnızca "Yeni düzeltme talebi"; saklama süresi PG-PERS-45 parametresine bağlanır |
| B-19 | Düşük | `btree_gist` için "tablo sahibi kurabilir" ifadesi yanlış; `hrms_app`'in EXCLUDE kısıtlı tabloya eklemesi test edilmemiş | ADR-0021 §2 | **Katılıyorum.** İfade düzeltilir; `ApplicationRoleTests`'e ekleme ve çakışma senaryosu eklenir |

**Uygulanabilir görünenler (alt ajan):**
- SkiaSharp çalışır: API imajı Ubuntu Noble (glibc) tabanlı. Yine de konteynerde bir duman testi önerilir.
- SQL Server 2019'da `HASHBYTES` girdi sınırı sorun değil.

**Test tanımı olmayan kurallar (alt ajan):**
- EXCLUDE kısıtı ve tarih kuralları;
- AAD ile değerin satırdan satıra taşınamaması;
- anahtar döndürme;
- bellek içi filtrede sayfalama ve toplam;
- fotoğrafın değişmediğinde yeniden okunmaması;
- e-postada değer olmaması;
- erişim kaydı içeriği;
- ayrılan personelin görünürlük süresi;
- fotoğraf, dışa aktarma ve düzeltme talebi uçlarında `404`;
- kıdem hesabı.

Bunlar SYG-PERS'te doğrulama yöntemiyle birlikte yazılır.

**Güçlü yönler (alt ajan):**
- Her alanın tek kaynağı tablo düzeyinde ayrılmış; senkronizasyon İK girişini ezemez.
- Görünürlük tek politikada toplanmış ve yanıta, filtreye, dışa aktarmaya uygulanıyor.
- Blind index'in reddi, ayrı ve zorunlu anahtar, değersiz e-posta, yerelde kan grubu olmaması.
- Yatay çekirdeklerin dar kapsamı ve kararların izlenebilirliği.

---

## 5. Karar gerekenler

| # | Konu | Kim | Öneri |
|---|---|---|---|
| 1 | B-03: Düzeltme talebinde önerilen kan grubu, talep kapanınca silinsin mi? | KVKK Sorumlusu | Evet |
| 2 | B-05: "Çalışma şekli" (LOGO'da verisi yok) | İK | Değişiklik talebiyle PG-PERS-12'den çıkarılsın veya HRMS'te girilsin |
| 3 | B-14: İzin dağılımı | Bilgi İşlem | Düzeltme talepleri ve veri kalitesi raporu İK'da; elle senkronizasyon yalnızca Sistem Yöneticisinde |
| 4 | B-15: Çok sicilli kişinin listede gösterimi ve aktif/ayrılan ayrımı | İK | §4'teki öneri |

---

## 6. Sonraki adım

#268: Kararlar alındıktan sonra bulguların tamamı ADR-0019…0022'ye işlenir. Ardından
SYG-PERS yazılır. Test tanımı olmayan kurallar orada doğrulama yöntemiyle yer alır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-22 | 1.0 | İlk sürüm (#267) | Bilgi İşlem |
