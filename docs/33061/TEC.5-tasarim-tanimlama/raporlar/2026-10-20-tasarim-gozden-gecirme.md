# Tasarım Gözden Geçirme Raporu — T3 Kimlik Yönetimi (T1 öncesi)

**Belge kimliği:** TEC.5-RAP-2026-10-20
**Süreç:** TEC.5 — Tasarım Tanımlama
**Tarih:** 2026-10-20
**Kapsam:** T3 Kimlik Yönetimi tasarımı, `v0.2.0` sonrası `main` (`f7560bd`); T1 Personel
Yönetimi tasarımına girdi
**Hazırlayan:** Bilgi İşlem; bağımsız inceleme bölümü (§4) ayrı bir alt ajan tarafından
**İlgili iş:** #234 (denetim önerileri TEC.5-3, TEC.5-5, TEC.5-6, TEC.5-8)

> **Neden şimdi?** T3, tasarımı koddan bağımsız gözden geçiren bir adım olmadan kabul
> edildi (13.10 modül sonu denetimi, TEC.5-6). T1 tasarımı başlamadan bu adım bir kez
> yapılır ve sonucu T1'e girdi olur. Rapor, gözden geçirmenin **ilk** örneğidir; TEC.5
> YAKLAŞIM §5'te öngörülüyordu.

---

## 1. Özet

| | |
|---|---|
| Bilgi İşlem bulguları (BG) | 10 — 4'ü bu PR'da kapandı, 3'ü bilinen kabul, 3'ü issue'da |
| Bağımsız inceleme bulguları (B) | 12 — Yüksek 2, Orta 6, Düşük 4; 3'ü Bilgi İşlem tarafından yeniden doğrulandı |
| T1 tasarımına girdi | 10 madde → #245 |
| Açılan işler | #244 (T1 öncesi altyapı düzeltmeleri), #245 (T1 tasarım ADR'leri), #246 (belge kaymaları) |
| Yeni risk | R-31 — denetim kayıtlarının değiştirilemezliğinin veritabanı süper kullanıcısına karşı korunmaması |
| Üretilen tasarım ürünleri | `docs/mimari/kimlik-tasarim-diyagramlari.md` (6 diyagram); SYG-KMLK §2.2 genişletildi ve §2.5 SYG → öğe tahsisi eklendi |

**Sonuç:** T3 tasarımı ADR'lerle büyük ölçüde tutarlı ve T1 genişlemesine uygun bir
kişi/istihdam temeli kuruyor. Ancak üç altyapı eksiği T1'in yeni kişisel veri alanlarından
**önce** kapatılmalı: veritabanı rolleri (B-01), maskeleme sınıflandırması (B-02) ve
varsayılan yetkilendirme politikası (B-04). Bunlar #244'te.

---

## 2. Yöntem ve bağımsızlık sınırı

| Adım | Kim | Ne yapıldı |
|---|---|---|
| 1 | Bilgi İşlem | Kapsam ve sorular belirlendi; üç inceleme paralel başlatıldı |
| 2 | Alt ajan A (bağımsız inceleme) | ADR'ler ve kod okundu; önceki iç denetim raporları (MAN.8) **okunmadan**, Bilgi İşlem görüşü verilmeden bulgu çıkarıldı (§4) |
| 3 | Alt ajan B | SYG-KMLK'nin 81 maddesi tasarım öğelerine ve kod bileşenlerine tahsis edildi; kod aramasıyla doğrulandı (SYG-KMLK §2.5) |
| 4 | Alt ajan C | Hesap, bayrak, oturum ve üyelik akışı koddan çıkarıldı; her geçiş dosya ve satırla kanıtlandı (diyagram belgesi) |
| 5 | Bilgi İşlem | Çıktılar okundu; bulgulardan üçü (B-01, B-04, B-05) kodda yeniden doğrulandı; kendi bulguları (§3) ve işler yazıldı |

**Bağımsızlık sınırı (R-23):** Doğuş Uçanok'un 15.10.2026 kararı gereği inceleme bir alt
ajanla yapıldı. Bu inceleme **kurum dışı veya insan bağımsızlığı sağlamaz**:
- Alt ajan, kodu ve belgeleri üreten geliştirme yardımcısıyla aynı model ailesindendir.
- Alt ajan aynı depoyu okur.
- Kapsamı ve sorularını Bilgi İşlem belirledi.

Sağladığı bağımsızlık şudur: alt ajan önceki denetim bulgularını ve Bilgi İşlem'in görüşünü
görmeden çalıştı ve bulguları değiştirilmeden §4'te ayrı gösterilir. Kalan risk R-23'te
izlenir.

**Kapsam dışı kalanlar:** ADR-0011, 0013 ve 0015 ile frontend bileşenlerinin tamamı alt
ajan A tarafından okunmadı (§4 "Okunanlar"). Çalışma zamanı denetimi (UAT veritabanı
yetkileri) yapılmadı; B-01 yapılandırma dosyasından doğrulandı.

---

## 3. Bilgi İşlem bulguları (BG)

| No | Bulgu | Kaynak | Sonuç |
|---|---|---|---|
| BG-01 | Üyelikte yanıt süresi eşitlemesi bir alt sınırdır (1 sn); iş 1 sn'yi aşarsa fark kalır | Diyagram çıkarımı | Bilinen; kanal sayısı farkıyla birlikte `KR-078` / R-19 kapsamında kabul. Diyagram belgesinde yazılı |
| BG-02 | İleti kuyruğu doluysa kod kaydedilir ama gönderilmez; kullanıcıya hata dönmez | Diyagram çıkarımı | #246 (Y1 mi, T1 bildirim çekirdeği mi kararı) |
| BG-03 | Kuyruk bellektedir; yeniden başlatmada bekleyen ileti kaybolur | Diyagram çıkarımı | Bilinen; `KR-082`. Diyagram belgesinde yazılı |
| BG-04 | `UserAccount.LockedUntil` süre dolunca temizlenmez; kilidin tek doğru kaynağı `LoginThrottle` | Diyagram çıkarımı | #246; diyagram belgesinde not |
| BG-05 | SYG-KMLK §2.2'de dört öğe yok: kimlik alan modeli ve depoları, eylem yetkisi altyapısı, sistem API'si, çapraz kesen altyapı; konumların bir kısmı tek katman gösteriyor | Tahsis | **Bu PR'da kapandı:** §2.2 genişletildi |
| BG-06 | SYG → öğe tahsis tablosu yoktu (TEC.5 çıktı b) | 02.10 denetimi TEC.5-3 | **Bu PR'da kapandı:** SYG-KMLK §2.5 |
| BG-07 | Matrisin "Tasarım" sütununda 2FA satırları ADR-0017'yi, yetki satırları ADR-0007'yi göstermiyor | Tahsis | #246 |
| BG-08 | PG-PERS-08 "T3'te karşılandı" diyor; şema sapması denetimi var ama bildirim Y1'e bırakıldı (SYG-KMLK-010) | Tahsis + B-08 | **Bu PR'da kapandı:** PG-PERS-08 gerekçesi düzeltildi |
| BG-09 | Kan grubu işleme koşulu 3 ("erişim kayıtları değiştirilemez", `KR-116`) bugün süper kullanıcıya karşı sağlanmıyor (B-01) | B-01 ile karşılaştırma | R-31 açıldı; #244. Koşul, #244 kapanınca tam sağlanır; kan grubu T1'de gelmeden önce |
| BG-10 | SYG-KMLK §3.1 "Kilitli"yi hesap durumu sayıyor; kodda kilit girilen e-postaya bağlı bir sayaçtır ve hesabın durumu değildir | Diyagram çıkarımı | **Bu PR'da kapandı:** §3.1'e gerçekleştirme notu; davranış gereksinimi (SYG-KMLK-033) değişmedi |

---

## 4. Bağımsız inceleme bulguları (B)

> Bu bölüm alt ajan A'nın çıktısıdır; dili sadeleştirildi, içeriği değiştirilmedi. "Bilgi
> İşlem" sütunu Bilgi İşlem'in eklediği tek sütundur.

**Okunanlar:**
- **ADR'ler:** README, 0002, 0003, 0004, 0005, 0006, 0007, 0008 (§5, §7), 0009, 0010 (§3, §4), 0012 (§4–6), 0014 (§2, §4), 0016, 0018.
- **Belgeler:** Karar defterinde KR-070…KR-116, SYG-KMLK §2.2–2.4, PG-PERS.
- **Backend:**
  - `Api` katmanı: `Program.cs` ve `Identity`;
  - `Application`: `Common/Security`, `Identity/Authorization`, `Personnel/Sync`;
  - `Domain`: `Personnel` ve `Common`;
  - `Infrastructure`: `Logo`, `Data` (bağlam, yapılandırmalar, denetim izi ara katmanı), `Audit/AccessLogger`, `Settings/AesGcmSecretProtector`;
  - migration'larda tetikleyici ve collation araması.
- **Testler:** mimari testlerin tamamı.
- **Ortam:** `docker/compose*.yml`. Sır dosyaları açılmadı.

| No | Önem | Bulgu | Kanıt | Öneri | Bilgi İşlem |
|---|---|---|---|---|---|
| B-01 | Yüksek | Denetim kayıtlarının değiştirilemezliği yalnızca tetikleyiciye dayanıyor; uygulama veritabanına süper kullanıcıyla bağlanıyor. Süper kullanıcı tetikleyiciyi kapatabilir veya silebilir; ADR-0009 §2'nin "değiştirilemez" iddiası UAT'de teknik olarak sağlanmıyor | `docker/compose.uat.yml:57`; `Migrations/20260910145923_DenetimIzi.cs:65-88` | Migration ve çalışma rolleri ayrılsın; çalışma rolü sahip olmasın, `audit`'te yalnızca INSERT/SELECT; rolün süper kullanıcı olmadığını test doğrulasın | **Doğrulandı.** #244, R-31 |
| B-02 | Yüksek (T1 için) | Değişiklik kaydında maskeleme ada dayalı, hatada açık bırakan bir yasak listesi. T1 alanları (kan grubu, adres, acil durum telefonu) düz metin düşer; özel nitelikli veri için "yalnızca değişti" kipi yok | `Domain/Personnel/Person.cs:14-19`; `Application/Common/Security/MaskRules.cs`; `AuditTrailInterceptor.cs:243-262` | Sınıflandırma Domain'e; özel nitelikli için "değişti" kipi; sınıflandırılmamış kolonu yakalayan mimari testi | #244 |
| B-03 | Orta | ADR-0009 §5 kolon şifrelemesi için altyapı yok. `AesGcmSecretProtector` yalnızca sır parametreler için; tek anahtar, anahtar kimliği ve döndürme yok, anahtar isteğe bağlı | `Infrastructure/Settings/AesGcmSecretProtector.cs:23-38` | T1 öncesi ADR: ayrı veri anahtarı, anahtar kimliği, zorunlu açılış doğrulaması, döndürme | #245 |
| B-04 | Orta | Kimlik doğrulama açıkça istenirse çalışıyor; varsayılan politika yok, özniteliği unutulan uç nokta anonim kalır; yakalayan test yok | `Api/Identity/Authentication.cs:104` | `FallbackPolicy = RequireAuthenticatedUser`; anonim uçlar açık işaretli; mimari testi | **Doğrulandı.** #244 |
| B-05 | Orta | ADR-0004 §10 Türkçe ICU collation uygulanmamış; ad sıralaması ve i/İ araması yanlış olabilir | `docker/compose.yml:27`, `compose.uat.yml:31`; migration'larda collation yok | Ad ve soyad kolonlarına `tr-TR-x-icu`; T1 listesinden önce test | **Doğrulandı.** #244 |
| B-06 | Orta | ADR-0007 §2–3 kapsam altyapısı (`ICurrentUserScope`, veri katmanında filtre, 404) yok; `KR-089` T4'e bırakmıştı. T3'te açık değil, T1'de ilk günden gerekli | `Permissions.cs` yorumu | Bkz. T1 girdisi 1 | #245 |
| B-07 | Orta | Erişim kaydında serbest arama değeri maskelenmiyor; rakamla aranan TCKN düz metin yazılır. Liste erişimi yalnızca sayıyı tutuyor | `AccountAdministration.cs:153`; `AccessLogger.cs:68-80` | Serbest arama maskelensin; liste için gösterilen kişilerin kaydı ADR-0009'da kararlaştırılsın | #244 |
| B-08 | Orta | Senkronizasyon hatasında ve şema sapmasında yöneticiye bildirim yok; ADR-0003 §4 ve ADR-0009 §8 güncellenmemiş; PG-PERS-08 "T3'te karşılandı" diyor | `PersonnelSyncService.cs:98-103`; `SYG-KMLK.md:164` | ADR'lere "Y1'e ertelendi" notu; PG-PERS-08 düzeltilsin | PG-PERS-08 bu PR'da (BG-08); ADR notu #246 |
| B-09 | Düşük | ADR-0004 sapmaları: enum alanları metin, tekillik dizinleri `ix_`, `ISoftDeletable` kullanılmamış | `PersonnelSyncRunConfiguration.cs:24-26` vb. | Gerçekleştirme notu veya uyum | #246 |
| B-10 | Düşük | ADR-0002 Kural 4 ihlal ediliyor ve test edilmiyor; Api katmanı modül ölçümünde yok; `SyncRunsController` Kimlik altında | `Api/Identity/Authentication.cs:6`; `LayerDependencyTests.cs:241` | Api katmanı ölçüme; senkronizasyon uçları T1'de `personnel` altına | #246 |
| B-11 | Düşük | LOGO yalıtım testi yalnızca adı sayılmış üç tipi koruyor; ikinci kilit `ExecuteUpdate/ExecuteSqlRaw` yollarını kapsamıyor (asıl koruma veritabanındaki DENY) | `LayerDependencyTests.cs:112-133`; `LogoDbContext.cs:46-57` | Kural `SqlServer` paketini ve `Logo` ad alanının tamamını kapsasın | #246 |
| B-12 | Düşük | ADR-0003 §2'deki `FindByNationalIdAsync` kodda yok; ADR-0005 `gender` ve şube/birim birleştirmeleri T1'e kaldı | `ILogoPersonnelSource.cs:16-34` | ADR'lere "T1'de" / "değişti" notu | #246 |

**ADR'si olmayan önemli kararlar** (alt ajan A):
- izinlerin her istekte okunması;
- Kimlik modülünün `Person` tablosunu doğrudan sorgulaması (T1'in alan görünürlüğünü atlayabilecek yan kapı);
- erişim kaydının ayrı işlemle yazılması.

Bu kararlar #246'da işlenecek.

**Güçlü yönler** (alt ajan A):
- LOGO'ya karşı üç katmanlı salt okuma koruması; her çalışmadan önce yetki denetimi yapılıyor ve CI'da gerçek betikle doğrulanıyor.
- Modül sınırı testi iki yönlü çalışıyor.
- Oturum her istekte kayıtla doğrulanıyor; jeton yalnızca bellekte, yenileme jetonu HttpOnly çerezde.
- Eşleşme gizliliği tüm üyelik akışına yayılmış.
- Denetim izi aynı işlemde yazılıyor.
- Sırlar AES-GCM ile ve amaca bağlı şifreleniyor.
- Kişi/istihdam ayrımı ölçüme dayanıyor ve T1 genişlemesine uygun.

---

## 5. T1 tasarımına girdi

Ayrıntı #245'tedir. Özetle T1 SYG'si ve kodu başlamadan şu ADR kararları gerekir:

1. **Kapsam ve geçiş kuralı** (ADR-0007 genişletmesi; PG-PERS-25…27, 35).
2. **Alan düzeyinde görünürlük:** API yanıtı, filtre ve dışa aktarmada tek politika (`KR-116` madde 3).
3. **Kolon şifreleme** (ADR-0009 §5; kan grubu): anahtar yönetimi; denetim izine yalnızca "değişti" yazılması; şifreli alanda filtre kararı.
4. **ADR-0018 / R-25:** gerçek kan grubunun yerel ortama ve UAT'ye taşınması.
5. **Veri modeli:** görev istihdama bağlı ve tarih aralıklı; acil durum kişileri; evlilik bilgisi.
6. **Referans veri çekirdeği** (`KR-115`): ayrı ad alanı ve ADR-0016 bağımlılığı.
7. **ADR-0003:**
   - yeni LOGO tabloları;
   - okunan alan listesi testi;
   - fotoğrafta değişiklik algılama ve saklama yeri.
8. **ADR-0014:** alan seçmeli ve fotoğraflı dışa aktarma.
9. **ADR-0012 / Y1:** "İK'ya bildir" için bildirim çekirdeği.
10. **ADR-0010 §4:** sayfa boyutu üst sınırı parametreye bağlanır.

Önce yapılacaklar: #244 (B-01, B-02, B-04, B-05, B-07).

---

## 6. Bu PR'da üretilen tasarım ürünleri

| Ürün | Yer | 33061 TEC.5 çıktısı |
|---|---|---|
| Hesap, bayrak, oturum ve üyelik diyagramları (Mermaid, kanıtlı) | `docs/mimari/kimlik-tasarim-diyagramlari.md` | a) öğelerin tasarım özellikleri |
| Genişletilmiş öğe tablosu (12 öğe, konum ve SYG aralığıyla) | SYG-KMLK §2.2 | a, b |
| SYG → öğe ve bileşen tahsisi (81 madde) | SYG-KMLK §2.5 | b) gereksinimlerin öğelere dağıtılması; h) izlenebilirlik |
| Modül listesi → ADR-0016 ve `AllowedModuleDependencies` atfı | `docs/mimari/modul-listesi-ve-bagimliliklar.md` | Denetim TEC.5-8 |
| Bu rapor | `TEC.5-tasarim-tanimlama/raporlar/` | Tasarım gözden geçirmesi (TEC.5-6) |

---

## 7. Sonraki gözden geçirme

TEC.5 YAKLAŞIM §2.5'e göre her modülün tasarımı, ilk kod PR'ından önce bir kez gözden
geçirilir; sıradaki gözden geçirme T1 ADR'leri (#245) yazıldıktan sonra, T1'in ilk kod
PR'ından önce yapılır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-20 | 1.0 | İlk sürüm (#234) | Bilgi İşlem |
