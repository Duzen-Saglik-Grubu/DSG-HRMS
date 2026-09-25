# Katkı Rehberi

**Son güncelleme:** 2026-09-25
**İlgili süreçler:** TEC.7 (Gerçekleştirme), TEC.9 (Doğrulama), MAN.5 (Konfigürasyon Yönetimi), MAN.8 (Kalite Güvence)

Bu belge, depoda çalışma kurallarını tanımlar. Kurallar aynı zamanda
**TS ISO/IEC TS 33061 Seviye 2 kanıtıdır**: PA 2.2 (c) ve (d) maddeleri, bilginin
kontrol edilmesini ve onaylanmasını ister. Buradaki akış tam olarak bunu sağlar.

---

## 0. İlk kurulum — git kancaları

Depoyu klonladıktan sonra **bir kez** çalıştırın:

```bash
git config core.hooksPath .githooks
```

Bu, iki kancayı etkinleştirir:

| Kanca | İşlevi |
|---|---|
| `pre-push` | `main` dalına doğrudan gönderimi engeller |
| `commit-msg` | Commit mesajının Conventional Commits biçimine uymasını denetler |

> **Neden gerekli?** GitHub Free planında **özel** depolarda sunucu tarafı dal
> koruma kullanılamamaktadır (`R-16`). Bu kancalar telafi edici kontroldür.
> Kancayı kurmadan çalışırsanız `main`'e yanlışlıkla gönderim yapabilirsiniz;
> bu durum CI tarafından tespit edilir ve otomatik olarak bir düzeltici faaliyet
> issue'su açılır. Ayrıntı:
> [`dal-koruma-telafi-kontrolleri.md`](docs/33061/MAN.5-konfigurasyon-yonetimi/dal-koruma-telafi-kontrolleri.md)

---

## 1. Temel kural

> **`main` dalına doğrudan yazılmaz.** Kod, yapılandırma ve **dokümanlar dâhil**
> her değişiklik bir dal üzerinde yapılır ve Pull Request ile birleştirilir.

Gerekçe: Değişikliğin *neyi*, *neden* değiştirdiği ve *kim tarafından* onaylandığı
kaydedilmiş olur. Dokümanların da bu akıştan geçmesi bilinçli bir karardır — 33061
açısından bir doküman değişikliği de kontrol edilmesi gereken bir değişikliktir.

---

## 2. İş akışı

```
1. Issue açılır (veya mevcut issue seçilir)
2. Issue'dan dal oluşturulur
3. Geliştirme yapılır, commit'lenir
4. Pull Request açılır (issue'ya bağlanır)
5. CI kalite kapıları çalışır
6. Kod incelemesi yapılır
7. Squash merge ile main'e alınır
8. Dal silinir
9. Issue otomatik kapanır
```

**Her değişiklik bir issue'ya bağlıdır.** Issue'suz PR açılmaz; izlenebilirlik zinciri
(gereksinim → issue → PR → test → kabul) bu bağ üzerinden kurulur.

---

## 3. Dal adlandırma

```
<tür>/<issue-no>-<kısa-açıklama>
```

| Tür | Kullanım | Örnek |
|---|---|---|
| `ozellik` | Yeni yetenek | `ozellik/42-uyelik-dogrulama-kodu` |
| `hata` | Hata düzeltmesi | `hata/57-izin-gun-hesabi` |
| `dokuman` | Yalnızca doküman | `dokuman/12-adr-0016-eklendi` |
| `bakim` | Bağımlılık, altyapı, teknik borç | `bakim/63-paket-guncellemeleri` |
| `duzeltici` | MAN.8 düzeltici faaliyet | `duzeltici/71-kapsam-esigi-dususu` |

Türkçe karakter ve boşluk kullanılmaz.

### 3.1 Dallanma stratejisi

**Trunk-based (GitHub Flow).** Tek uzun ömürlü dal vardır: `main`.

- Dallar **kısa ömürlüdür** — hedef: 1–3 gün, en fazla bir hafta.
- Uzun süren dallar birleştirme çatışması ve gözden geçirilemez PR üretir.
- Büyük bir modül tek PR'da gelmez; anlamlı parçalara bölünür.
- `develop` veya `release` dalı **kullanılmaz** — tek geliştirme kanalı için gereksiz
  karmaşıklık üretir.

---

## 3.2 Kodlama dili (`KR-058`)

| Ne | Dil |
|---|---|
| Sınıf, arayüz, metot, özellik, değişken adları | **İngilizce** |
| Ad alanı (namespace) ve klasör adları | **İngilizce** |
| Veritabanı tablo ve kolon adları | **İngilizce** (`snake_case`) |
| Yapılandırma anahtarları | **İngilizce** (`Database:Hrms`) |
| Test metodu adları | **İngilizce** |
| Kabuk betiği değişkenleri ve fonksiyonları, ortam değişkeni adları | **İngilizce** (`DOMAIN`, `TLS_ENABLED`) |
| İş akışı (`.github/workflows`) iş kimlikleri, matris anahtarları, satır içi betik tanımlayıcıları | **İngilizce** |
| Çeviri (i18n) anahtarları | **İngilizce** (`error.notFound`) — değerleri Türkçe |
| **Kod içi yorumlar** | **Türkçe** |
| **XML belgeleri** (`<summary>`, `<remarks>`) | **Türkçe** |
| Hata mesajları (kullanıcıya görünen) | **Türkçe** |
| Dokümanlar | **Türkçe** (teknik terimler hariç) |

**Gerekçe:** .NET ekosisteminin tamamı İngilizcedir — çerçeve tipleri, ezilen metot
imzaları, paket API'leri. Karışık dil, ezme (override) kurallarında doğrudan çelişki
üretir: `CA1725`, ezilen metotlarda parametre adlarının taban imzayla aynı olmasını
zorunlu kılar. Ayrıca ADR'lerde tanımlanan arayüzler (`ICurrentUser`, `IFileStorage`,
`ILogoPersonnelSource`) ve ADR-0004'teki kolon adları (`created_at`, `deleted_by`)
zaten İngilizcedir.

**Yorumların Türkçe olması bilinçlidir.** Yorum, kodun *neden* öyle yazıldığını
anlatır; bu açıklamanın ekibin ana dilinde olması anlaşılırlığı artırır.

**Kural yalnızca C# ve TypeScript için değildir.** CI betikleri, iş akışlarının
içindeki betikler, kabuk betikleri ve Dockerfile'lar da koddur. Bu dosyaların
"altyapı yapılandırması" gibi okunup kuralın dışında kalması; betiklerde, iş
akışlarında, ön yüzde ve çeviri anahtarlarında farklı tarihlerde tekrarlanan bir
uygunsuzluk üretti (düzeltici faaliyet #66).

**Kapsam dışı olanlar:**
- **Dosya adları.** `baslik-bicimi-testi.mjs` gibi adlar belgelerde ve runbook'ta
  atıf alır; KR-058 dosya adlarını saymaz.
- **Belge klasörleri** (`docs/` altı). Belgeler Türkçedir.
- **Kullanıcıya görünen değerler:** çeviri değerleri, hata iletileri, betik çıktıları.

```csharp
/// <summary>
/// Denetim alanlarini otomatik doldurur (ADR-0004 §4).
/// </summary>
public sealed class AuditFieldsInterceptor : SaveChangesInterceptor
{
    // Olusturma bilgisi degistirilemez: denetim izinin guvenilirligi
    // bunun degismezligine dayanir.
    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
}
```

---

## 3.3 Günlük (log) kaydı ve kişisel veri

Kişisel verinin günlüğe düz metin olarak düşmesi **geri alınamaz** bir KVKK
ihlalidir: dosya diske yazıldıktan sonra yedeklere, kapsayıcı günlüklerine ve
izleme sistemine yayılır.

**Kural:** Kişisel veri günlüğe **daima nesne olarak** verilir.

```csharp
// DOĞRU — maskeleme ilkesi devreye girer
logger.LogInformation("Personel bulundu {@Person}", person);

// YANLIŞ — ham metin maskelenemez, düz metin diske düşer
logger.LogInformation("TCKN: {NationalId}", person.NationalId);

// Zorunlu hâllerde açıkça maskelenir
logger.LogInformation("TCKN: {NationalId}", Mask.NationalId(nationalId));
```

**Neden bu kural var:** `MaskingDestructuringPolicy` yalnızca nesne ayrıştırma
(`{@Nesne}`) yolunu kapsar. Doğrudan yazılan bir metin, Serilog için sıradan bir
değerdir ve otomatik olarak korunamaz. Bu, altyapının bilinen ve kabul edilen
sınırıdır; kod incelemesinde denetlenir (§7).

| Veri | Davranış | Örnek çıktı |
|---|---|---|
| T.C. Kimlik No | Kısmen maskelenir | `123*****901` |
| Telefon | Kısmen maskelenir | `532*****67` |
| E-posta | Yerel bölüm maskelenir, alan adı kalır | `ah***@duzen.com.tr` |
| IBAN | Kısmen maskelenir | `TR***1326` |
| Parola, doğrulama kodu, jeton | **Hiç yazılmaz** | `***` |
| SMS / e-posta gövdesi | **Hiç yazılmaz** | `***` |

Yeni bir DTO veya varlık yazarken hassas alanlar `[PersonalData(...)]` veya
`[Secret]` ile işaretlenir. İşaretleme unutulursa ad benzerliği ikinci savunma
hattı olarak devreye girer (`MaskRules`) — ancak buna **güvenilmez**, öznitelik
asıl kuraldır.

Ayrıntı: ADR-0009 §4.

---

## 3.4 Erişim kaydı — ne zaman çağrılır?

Değişiklik kaydı otomatiktir; **erişim kaydı değildir.** Bir sorgunun kişisel veri
döndürüp döndürmediğini yalnızca kullanım senaryosu bilir, bu yüzden çağrı açıkça
yapılır (`IAccessLogger`).

**Kişisel veri dışarı çıkıyorsa kaydedilir:**

| Durum | Çağrı |
|---|---|
| Tek kişinin özlük kartı açıldı | `AccessRecord.View("Person", id)` |
| Sağlık raporu, engellilik bilgisi görüntülendi | `AccessRecord.SpecialCategoryView(...)` |
| Personel listesi getirildi | `AccessRecord.List("Person", count, filters)` |
| Excel / PDF indirildi | `AccessRecord.Export("Person", count, filters)` |
| Toplu rapor üretildi | `AccessRecord.Report(reportName, count, filters)` |
| Özlük dosyası eki indirildi | `AccessRecord.FileDownload(...)` |

**Kaydedilmez:** referans veri (il, ilçe, unvan listesi), kendi profilini görüntüleme,
kişisel veri içermeyen sayaç ve grafikler. Her şeyi kaydetmek, kaydın kendisini
kullanılamaz hâle getirir.

**Dışa aktarmada kayıt sayısı zorunludur.** Bir sızıntı incelemesinin ilk sorusu
"kaç kişinin verisi dışarı çıktı" sorusudur.

**Filtre değerleri maskelenir**, ancak bu otomatik maskelemeye güvenilerek serbest
metin gönderilmez: filtreler `ad → değer` sözlüğü olarak verilir, karar **ada** göre
alınır (`MaskRules`).

**Fail-closed:** Kayıt yazılamazsa çağrı hata fırlatır ve **veri sunulmaz** (`KR-061`).

Ayrıntı: ADR-0009 §3.

---

## 3.5 Hata fırlatma — hangi istisna, hangi durum kodu?

Hata yanıtı **tek yerden** üretilir (`ProblemDetailsExceptionHandler`). Servis kodu
yalnızca doğru istisnayı fırlatır; HTTP ayrıntısıyla ilgilenmez.

| İstisna | Kod | Ne zaman |
|---|---|---|
| `ValidationException` (FluentValidation) | `400` | Veri **biçimsel** olarak geçersiz |
| `ForbiddenException` | `403` | Kullanıcı bu eylemi **hiçbir** kayıtta yapamaz (rol) |
| `NotFoundException` | `404` | Kayıt yok **veya kapsam dışı** |
| `ConflictException` | `409` | Kaydın o anki durumu isteği reddediyor |
| `BusinessRuleException` | `422` | Biçim doğru, **iş kuralı** ihlal edilmiş |

**`403` / `404` ayrımı kritiktir.** Kapsam dışı bir kayıt için `403` dönmek, kaydın
**var olduğunu** sızdırır — kullanıcı kimlik deneyerek hangi kayıtların mevcut
olduğunu çıkarabilir. Bu yüzden kapsam dışı erişim `NotFoundException` fırlatır ve
mesajda kayıt kimliği **yer almaz** (ADR-0007 §4).

**`400` / `422` ayrımı** frontend için anlamlıdır: ilkinde kullanıcı alanı düzeltir,
ikincisinde kendisine açıklama gösterilir.

**Kullanıcıya gösterilecek mesaj yalnızca bu istisnalarda taşınır.** Diğer tüm
istisnalar "beklenmeyen" sayılır: kullanıcı genel bir mesaj ve `traceId` görür,
ayrıntının tamamı sunucu günlüğüne yazılır (`KR-062`).

```csharp
// DOĞRU
if (balance < requestedDays)
{
    throw new BusinessRuleException("Yıllık izin bakiyeniz yetersiz.");
}

// YANLIŞ — teknik ayrıntı kullanıcıya gider
throw new InvalidOperationException($"leave_balance={balance} < {requestedDays}");
```

Ayrıntı: ADR-0010 §5–§7.

---

## 4. Commit mesajları

**Conventional Commits** biçimi kullanılır:

```
<tür>(<kapsam>): <özet>

[gövde — neden değişti]

Refs: #<issue-no>
```

| Tür | Anlamı |
|---|---|
| `feat` | Yeni yetenek |
| `fix` | Hata düzeltmesi |
| `docs` | Doküman |
| `test` | Test ekleme/düzeltme |
| `refactor` | Davranışı değiştirmeyen iyileştirme |
| `perf` | Performans |
| `build` | Derleme, paket, Docker |
| `ci` | CI/CD yapılandırması |
| `chore` | Diğer bakım işleri |

**Kapsam**, modül kısaltmasıdır: `kimlik`, `izin`, `organizasyon`, `personel` …

**Örnek:**
```
feat(kimlik): uyelik dogrulama kodu gonderimi

Dogrulama kodu 6 hane, 5 dakika gecerli ve hash'lenmis saklaniyor.
Kod hicbir log kaydina yazilmiyor (ADR-0009 §4).

Refs: #42
```

**Kurallar:**
- Özet satırı **en fazla 72 karakter**, küçük harfle başlar, sonunda nokta yok.
- Gövdede **ne** değil **neden** anlatılır; *ne* zaten koddadır.
- Kırıcı değişiklikte `!` kullanılır: `feat(api)!: ...`

---

## 5. Pull Request kuralları

| Kural | Değer |
|---|---|
| **Başlık** | **Conventional Commits** — issue biçimi (`[GÖREV] …`) **kullanılmaz**. Bkz. §5.1 |
| Şablon | `.github/PULL_REQUEST_TEMPLATE.md` doldurulur |
| Issue bağlantısı | **Zorunlu** — `Closes #42` (görev/hata) veya `Refs #42` (gereksinim) |
| Milestone | **Zorunlu** — PR ve issue aynı aşamaya (A0/A1/A2/A3/AS) bağlanır |
| Boyut | Hedef **< 400 satır** değişiklik; büyükse bölünür |
| CI | Tüm kalite kapıları geçmeli |
| İnceleme | En az **1 onay** |
| Birleştirme | **Squash merge** — `main` geçmişi okunabilir kalır |
| Dal | Birleştirme sonrası **silinir** |

### 5.1 PR başlığı

PR başlığı, **commit mesajlarıyla aynı biçimi** kullanır (§4):

```
<tür>(<kapsam>): <özet>
```

| | |
|---|---|
| ✅ Doğru | `feat(uat): TLS (HTTPS) devreye alındı` |
| ✅ Doğru | `fix(frontend): izleme kimliği güvenli olmayan bağlamda da üretiliyor` |
| ❌ Yanlış | `[GÖREV] UAT ortamında TLS kurulumu` — bu **issue** biçimidir |

**Neden bu biçim zorunlu:** Birleştirme **squash merge** ile yapılır ve GitHub,
squash commit'inin konu satırını **PR başlığından** üretir. Uyumsuz bir PR başlığı,
Conventional Commits'e uymayan bir commit olarak `main` geçmişine girer. Yerel
`commit-msg` kancası bunu **yakalayamaz**; o yalnızca yerel commit'lere bakar.
Denetlenebilecek tek yer CI'dır.

Başlık biçimi **CI tarafından denetlenir**
([`pr-izlenebilirlik-denetimi.yml`](.github/workflows/pr-izlenebilirlik-denetimi.yml)).

> Bu kural #44 düzeltici faaliyetinin sonucudur. Kural yazılı olmadığı için 20 PR
> boyunca yalnızca alışkanlıkla korunmuş, 21.'sinde kırılmıştı.

### 5.2 `Closes` ve `Refs` ayrımı

| Anahtar kelime | Etkisi | Ne zaman |
|---|---|---|
| `Closes #42` | Issue **kapanır**, resmî bağlantı (*Linked pull requests*) oluşur | Görev ve hata issue'ları |
| `Refs #42` | Yalnızca metinsel bağ; issue **açık kalır** | Gereksinim issue'ları — modül kabul edilene kadar açık kalmalıdır |

> **Dikkat:** `Refs` kullanıldığında issue kapanmaz **ve** pano otomasyonu onu
> "Done"a taşımaz. Görev issue'sunda yanlışlıkla `Refs` yazılırsa issue "Review"da
> asılı kalır. Bu durum bir kez yaşandı (#17); artık CI denetimi ve bu tablo var.

Her iki alan da **CI tarafından denetlenir**
([`pr-izlenebilirlik-denetimi.yml`](.github/workflows/pr-izlenebilirlik-denetimi.yml)):
milestone veya issue bağlantısı eksikse kontrol başarısız olur.

### 5.3 Taslak (draft) PR

İş yarımken PR **taslak** olarak açılabilir. Bu, CI'ın erken çalışmasını ve geri
bildirimin erken alınmasını sağlar. Taslak PR birleştirilemez.

---

## 6. Tamamlanma Tanımı (Definition of Done)

Bir iş, aşağıdakilerin **tamamı** sağlanmadan "bitti" sayılmaz:

- [ ] Kod yazıldı ve kodlama standartlarına uygun
- [ ] **Birim testleri** yazıldı; kapsam eşikleri sağlanıyor (genel %75, Domain %90)
- [ ] **Entegrasyon testleri** yazıldı (gerçek PostgreSQL — Testcontainers)
- [ ] **Yetki sızıntısı testi** yazıldı (kapsam dışı kayıt `404` dönüyor) — *atlanamaz*
- [ ] **Maskeleme testi** yazıldı (kişisel veri log'a düz metin düşmüyor) — *atlanamaz*
- [ ] Mimari kuralları ihlal edilmedi (mimari testi geçiyor)
- [ ] Denetim izi ve gerekiyorsa erişim kaydı üretiliyor (ADR-0009)
- [ ] API değişikliği varsa OpenAPI güncel; frontend tipleri yeniden üretildi
- [ ] Veritabanı değişikliği varsa migration yazıldı ve **geri alınabilir**
- [ ] İlgili dokümanlar güncellendi (ADR, karar defteri, doküman haritası)
- [ ] Yeni bir mimari karar alındıysa **ADR yazıldı**
- [ ] Sır sızıntısı yok (`gitleaks` temiz)
- [ ] CI'daki tüm kalite kapıları geçti
- [ ] Kod incelemesi yapıldı ve onaylandı

> Bu liste, ADR-0011 §2 ve §6'nın operasyonel karşılığıdır ve MAN.8 kapsamında
> kalite kriteri sayılır.

---

## 7. Kod inceleme kontrol listesi

İnceleyen kişi şunlara bakar:

**Doğruluk**
- [ ] İş kuralı gereksinimle örtüşüyor mu?
- [ ] Sınır durumlar ele alınmış mı? (boş liste, ilk/son gün, çakışan tarih aralığı)
- [ ] Hata durumları anlamlı mesajla dönüyor mu?

**Güvenlik ve KVKK**
- [ ] Yetki kontrolü **veri katmanında** mı yapılıyor? (ADR-0007 §3)
- [ ] Kapsam dışı erişimde `404` mü dönüyor? (§3.5)
- [ ] Hata mesajı kullanıcıya yönelik mi, teknik ayrıntı taşıyor mu?
- [ ] Kişisel veri log'a veya hata mesajına sızıyor mu?
- [ ] Hassas alanlar `[PersonalData]` / `[Secret]` ile işaretlenmiş mi? (§3.3)
- [ ] Kişisel veri günlüğe **nesne olarak** mı veriliyor? (`{@Nesne}`, ham metin değil)
- [ ] Kişisel veri dışarı çıkıyorsa **erişim kaydı** yazılıyor mu? (§3.4)
- [ ] Dışa aktarmada **kayıt sayısı** kaydediliyor mu?
- [ ] Kullanıcı girdisi doğrulanıyor mu? (FluentValidation)
- [ ] Sır, bağlantı dizesi veya anahtar koda yazılmış mı?

**Veri**
- [ ] Yabancı anahtar ve `CHECK` kısıtları tanımlı mı?
- [ ] Tarih alanları `date` mi, zaman damgaları `timestamptz` (UTC) mi?
- [ ] Soft delete filtresi uygulanıyor mu?
- [ ] Migration geri alınabilir mi? Yıkıcı değişiklik çok adımlı mı?

**Performans**
- [ ] Listeleme sayfalanmış mı?
- [ ] **N+1 sorgu** var mı?
- [ ] Gerekli dizinler tanımlı mı?

**Bakım kolaylığı**
- [ ] Kod, çevresindeki kodla aynı üslupta mı?
- [ ] Katman ve modül sınırları korunuyor mu?
- [ ] Gereksiz soyutlama var mı? (KISS, YAGNI)
- [ ] Tekrar eden mantık var mı? (DRY)

---

## 8. Doküman kuralları

Ayrıntı: `docs/00-DOKUMAN-HARITASI.md` §4

- Teknik terimler dışında **Türkçe** yazılır.
- Kimliklendirme: `PG-`, `REQ-`, `ADR-`, `KR-`, `R-`, `TS-`
- Tarih biçimi: `YYYY-AA-GG`
- Her belgede "Son güncelleme" ve sonunda değişiklik geçmişi bulunur.
- **Sır, bağlantı dizesi veya gerçek kişisel veri örneği yazılmaz.**
- Yeni bir mimari karar `docs/sablonlar/ADR-sablonu.md` ile ADR olarak yazılır.

---

## 9. Sürümleme ve baseline

**Semantic Versioning** (`MAJOR.MINOR.PATCH`).

| Artış | Ne zaman |
|---|---|
| `PATCH` | Hata düzeltmesi |
| `MINOR` | Yeni modül veya yetenek |
| `MAJOR` | `v1.0.0` = üretime geçiş; sonrasında kırıcı değişiklik |

Kabul edilen her modül bir **etiket (tag)** ve bir **baseline** üretir (MAN.5).
Sürüm notları `CHANGELOG.md` dosyasında tutulur.

---

## 10. Etiketler ve issue türleri

| Ön ek | Kullanım |
|---|---|
| `tur:` | gereksinim, tasarim, gelistirme, test, dokuman, hata, degisiklik-talebi, duzeltici-faaliyet |
| `modul:` | kimlik, personel, organizasyon, izin, egitim … |
| `surec:` | TEC.2, TEC.3, TEC.5, TEC.7, TEC.8, TEC.9, TEC.10, TEC.11, TEC.13, MAN.1, MAN.2, MAN.4, MAN.5, MAN.6, MAN.8 |
| `oncelik:` | yuksek, orta, dusuk |
| `durum:` | engellendi, bilgi-bekliyor |

> **`surec:` etiketi**, bir issue'nun hangi 33061 sürecine kanıt ürettiğini gösterir.
> İzlenebilirlik matrisinin üretilmesini kolaylaştırır; boş bırakılmaz.

---

## 11. Yapılmayacaklar

- ❌ `main`'e doğrudan push
- ❌ Issue'suz PR
- ❌ CI kapılarını atlayarak birleştirme
- ❌ Sır, parola veya bağlantı dizesini depoya yazma
- ❌ `--force` ile `main` geçmişini değiştirme
- ❌ Testsiz yeni yetenek
- ❌ Gerçek kişisel veriyi test verisi olarak kullanma
- ❌ Kişisel veri içeren dosyayı (Excel, döküm) depoya ekleme

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-07 | 0.1 | İlk oluşturma | Bilgi İşlem |
| 2026-09-10 | 0.2 | §3.2 kodlama dili (KR-058) ve §3.3 günlük kaydı / kişisel veri kuralları eklendi; §7 kontrol listesi genişletildi | Bilgi İşlem |
| 2026-09-10 | 0.3 | §3.4 erişim kaydı kuralları eklendi; §7 kontrol listesi genişletildi | Bilgi İşlem |
| 2026-09-10 | 0.4 | §3.5 hata fırlatma kuralları eklendi; §7 kontrol listesi genişletildi | Bilgi İşlem |
| 2026-09-10 | 0.5 | §5 milestone zorunluluğu ve `Closes`/`Refs` ayrımı eklendi (düzeltici faaliyet #17) — o gün §5.1, bugün §5.2 | Bilgi İşlem |
| 2026-09-17 | 0.6 | §5.1 **PR başlığı** kuralı eklendi; alt bölümler yeniden numaralandı (düzeltici faaliyet #44) | Bilgi İşlem |
| 2026-09-25 | 0.7 | §3.2 kodlama dilinin kabuk betiklerini, iş akışlarını ve çeviri anahtarlarını da kapsadığı açıkça yazıldı; kapsam dışı olanlar tanımlandı (düzeltici faaliyet #66) | Bilgi İşlem |
