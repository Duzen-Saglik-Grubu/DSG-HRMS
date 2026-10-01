# ADR-0006 — Kimlik Doğrulama, Üyelik ve Oturum Yönetimi

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-013`, `KR-014`, `KR-015`, `KR-016`, `KR-017` (yürürlükten kalktı), `KR-018`, `KR-019`, `KR-020`, `KR-042`, `KR-069`, `KR-070`, `KR-073`, `KR-074`, `KR-075`, `KR-076`, `KR-078`, `KR-086`, `KR-087`, `KR-088`, `KR-090`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)
**İlgili riskler:** `R-08`, `R-12`, `R-13`, `R-14`

---

## Bağlam

Kurumda **Active Directory / Entra ID bulunmamaktadır** (`KR-013`); dolayısıyla dizin
tabanlı çoklu oturum açma (SSO) mümkün değildir. Uygulama kendi kullanıcı yönetimini
yapmak zorundadır.

Uygulama **yalnızca yerel ağda** çalışacak, internete açılmayacaktır. Kullanıcı kitlesi
≈584 aktif personeldir ve personel kendi izin talebini kendisi gireceği için tüm
personel kullanıcıdır.

Kimlik kaynağı LOGO'dur: kişi LOGO'da kayıtlı bilgilerle doğrulanır.

## Karar

### 1. Üyelik akışı

```
1. Giriş ekranı → "Üye Ol"
2. Kişi girer: TCKN + Doğum tarihi + Kurumsal e-posta
3. Sistem LOGO anlık görüntüsünde eşleşme arar
4. Kanal seçimi ekranı: "E-posta ile doğrula" / "SMS ile doğrula"
5. Seçilen kanala 6 haneli doğrulama kodu gönderilir
6. Kod doğru girilirse → parola belirleme → hesap oluşur
```

**Adım 3–4 arasındaki kritik davranış (`KR-016`):** Bilgiler eşleşse de eşleşmese de
kullanıcıya **aynı ekran ve aynı mesaj** gösterilir. Eşleşme yoksa kanal seçimi ekranı
yine görünür, ancak hiçbir kod gönderilmez. Böylece "bu TCKN bu kurumda çalışıyor mu"
bilgisi dışarı sızmaz (kimlik sıralama / enumeration saldırısı).

> **Gerçekleştirme (27.09.2026, `KR-085`, #87).** Gizlilik yalnızca ilk ekranda değil
> **tüm akışta** korunur. Eşleşmeyen deneme gerçek bir kod varmış gibi işler: kod
> isteği aynı yanıtı verir, girilen kod "yanlış" sayılır, süre ve deneme sınırı gerçek
> kodla aynıdır. Hız sınırları TCKN'nin anahtarlı özetine ve IP'ye göre sayılır. Yanıtlar
> en az 1 saniye sürer. Uç noktalar: `POST /api/v1/identity/registrations`,
> `…/{id}/code`, `…/{id}/verification`, `…/{id}/account`.

### 2. Kanal değiştirme

Kullanıcı SMS'i seçip kod eline ulaşmazsa, **aynı ekrandan e-posta ile doğrulamaya
geçebilir.** Geçiş otomatik değildir, kullanıcının tercihidir.

Kurallar:
- Kanal değiştirildiğinde **önceki kod geçersiz kılınır**, yeni kod üretilir.
- Kanal değiştirme denemeleri de hız sınırlamasına tabidir.
- Bu, SMS'in kara liste nedeniyle ulaşmadığı durumun (`R-14`) telafisidir.

### 3. Doğrulama kodu kuralları

| Konu | Değer |
|---|---|
| Uzunluk | 6 hane, kriptografik olarak güvenli üretici ile |
| Geçerlilik süresi | **5 dakika** |
| Yanlış deneme sınırı | **3** — aşılırsa kod iptal edilir, yeniden istenmesi gerekir |
| Saklama | Veritabanında **anahtarlı özet (HMAC-SHA256)** olarak; düz metin saklanmaz. Düz özet yetmez: 6 haneli kodun 1.000.000 olasılığı saniyeler içinde denenir (`SYG-KMLK-025`) |
| Loglama | Kod **hiçbir yere loglanmaz** (SMS/e-posta içeriği dâhil) |
| Tek kullanımlık | Doğrulanan kod anında geçersiz kılınır |

### 4. Kanal seçilebilirliği kuralları

> **24.09.2026 güncellemesi (`KR-073`, `KR-075`, `KR-076`):** Giriş kimliği kurumsal
> e-posta olduğundan ve üyelikte girilen e-postanın LOGO'dakiyle eşleşmesi
> gerektiğinden, **kurumsal e-postası olmayan kişi üye olamaz.** Aşağıdaki tablo buna
> göre yeniden yazıldı; önceki tablodaki "yalnızca SMS" satırları üyelikte artık ortaya
> çıkmaz.

| Durum | Üyelik | E-posta kanalı | SMS kanalı |
|---|---|---|---|
| Kurumsal e-posta kişiye tekil, cep telefonu geçerli | ✅ | ✅ | ✅ |
| Kurumsal e-posta kişiye tekil, cep telefonu yok/geçersiz | ✅ | ✅ | ❌ |
| E-posta **kurumsal alan adı dışında** (`KR-019`) | ❌ | — | — |
| E-posta **birden fazla kişiye tanımlı** (`KR-018`) | ❌ Uyarı listesine düşer; İK LOGO'da düzeltir | — | — |
| E-posta kayıtlı değil | ❌ İK LOGO'da tanımlar | — | — |
| Hiçbir iletişim bilgisi yok | ❌ Sisteme alınmaz (`KR-076`) | — | — |

**Üyelikte zorlanan personel için İK destekli davet (`KR-075`):** İK, sistemden tek
kullanımlık parola oluşturma bağlantısı gönderir. Bağlantı **yalnızca LOGO'daki
kurumsal e-postaya** gider; İK adres girmez, parolayı bilmez. Geçerlilik 3 saat
(parametre). Önceki "İK elle hesap açar" istisna akışının yerini alır.

**Kabul edilen kurumsal alan adları** (`@duzen.com.tr`, `@zeytinim.com`, `@labpt.com.tr`)
koda gömülmez; **yönetilebilir parametre** olarak tutulur (`KR-019`).

`KR-020` (kurumsal adresi olmayana SMS zorunlu kılınmaz) `KR-073` ile **konusuz
kalmıştır**: kurumsal adresi olmayan kişi zaten üye olamaz; kalıcı çözüm İK'nın
LOGO'da adres tanımlamasıdır. SMS kanalı iki adımlı doğrulama ve parola sıfırlamada
geçerliliğini korur.

Eşleşmenin sızmaması (`KR-016`) ile kanal gösterimi arasındaki gerilim ve çözümü:
`SYG-KMLK.md` §6, AN-01 ve `KR-078`.

> **Gerçekleştirme (30.09.2026, `KR-090`, #107).** İK daveti hesap işlemleri ekranından gönderilir
> (`POST /api/v1/identity/accounts/{personId}/invitations`, `identity.invite.create`). Bağlantı
> `/invite#token=…` biçimindedir. Jeton 256 bit, özeti saklanır, 3 saat geçerlidir
> (PRM-HSP-04). Yeni bağlantı öncekileri geçersiz kılar. Bağlantı hesap yoksa oluşturur, varsa
> parolayı yeniler.

### 5. Hız sınırlama ve kilitleme

| Kapsam | Sınır |
|---|---|
| TCKN başına üyelik denemesi | 5 deneme / saat |
| IP başına üyelik denemesi | 20 deneme / saat |
| Kod gönderimi (kişi başına) | 3 gönderim / 15 dakika |
| Giriş denemesi (**girilen e-posta** başına — hesap var olmasa da; `SYG-KMLK-033`) | 5 başarısız deneme → 15 dakika kilit |

Kilitlenme ve sınır aşımı olayları denetim kaydına yazılır (ADR-0009).

> **Not:** NetGSM standart servisinde aynı numaraya 1 dakikada 20'den fazla görev
> oluşturulamaz (hata `85`). Yukarıdaki sınırlar bunun çok altındadır.

### 6. Parola politikası

- En az **6 karakter** (`KR-070`, 21.09.2026). Karmaşıklık kuralı (büyük/küçük/rakam/simge)
  **dayatılmaz**. İki değer de Sistem Yönetimi parametresidir.

  > **Bu değer bilinçli bir ödündür.** ADR'nin ilk sürümü 12 karakter öngörüyordu;
  > uzunluk karmaşıklıktan etkilidir ve bu teknik değerlendirme **değişmemiştir**.
  > Ancak kullanıcıların önemli bölümü seyrek kullanıcıdır ve uzun parola, parola
  > unutma ile hesap kilitlenme vakalarını artırmaktadır. Karar, aşağıdaki telafi
  > edici kontrollerle birlikte alınmıştır ve bu kontroller **devre dışı
  > bırakılamaz**: sızmış parola kontrolü, hesap kilitleme (§5) ve tek aktif oturum.
  > Değer parametre olduğu için ihtiyaç doğduğunda artırılabilir.
- **Sızmış parola kontrolü:** yaygın sızıntı listesi uygulama içinde çevrimdışı
  tutulur; listede olan parola kabul edilmez. Dış servise sorgu yapılmaz.
  **En az uzunluk 6'ya indirildikten sonra bu kontrol, parola güvenliğinin asıl
  dayanağıdır** (`KR-070`).
- Saklama: **ASP.NET Core Identity varsayılanı** (PBKDF2, yüksek yineleme sayısı) veya
  eşdeğeri. Parola hiçbir koşulda geri döndürülebilir biçimde saklanmaz.
  *Gerçekleştirme (#87):* PBKDF2-HMAC-SHA512, 210.000 yineleme (OWASP önerisi); yineleme
  sayısı özetin içinde saklanır. Yaygın parola listesi SecLists'ten (MIT) üç listenin
  birleşimidir: 143.672 kayıt (`Infrastructure/Identity/Passwords/README.md`).
- Zorunlu periyodik parola değişimi **varsayılan olarak uygulanmaz** (güncel güvenlik
  rehberleri bunu önermiyor; kullanıcıyı zayıf kalıplara itiyor). Sistem Yönetimi
  parametresiyle açılabilir.
  *Gerçekleştirme (01.10.2026, `KR-092`, #113):* Süre `PRM-KML-21` parametresidir (varsayılan
  90 gün). Zorunluluk (periyodik veya ilk giriş, `PRM-KML-20`) girişte değerlendirilir ve
  oturumda tutulur. Parola değişene kadar yalnızca parola değiştirme ve oturum uçları
  kullanılabilir; diğer uçlar `403 password-change-required` döner.
- Parola sıfırlama, üyelik akışıyla aynı doğrulama mekanizmasını kullanır.
  *Gerçekleştirme (29.09.2026, `KR-088`, #98):* Sıfırlama bir üyelik denemesidir; yalnızca
  amacı ve kod iletisinin metni farklıdır (`/api/v1/identity/password-resets`). Sıfırlama
  hesabın tüm oturumlarını kapatır ve giriş kilidini kaldırır. Oturum içinde değişiklik
  (`/api/v1/identity/account/password`) mevcut parolayı ister; yanlış deneme giriş kilidine
  sayılır. Değişiklikten sonra bu oturum açık kalır, diğerleri kapanır.

### 7. İki aşamalı doğrulama (2FA)

2FA **geliştirilecek**, ancak Sistem Yönetimi parametresiyle yönetilecek ve
**varsayılan değeri KAPALI** olacaktır (`KR-069`, 21.09.2026).

> **Bu, `KR-017`'nin yerini alır.** Önceki karar 2FA'yı tümüyle kapsam dışı
> bırakıyordu. Gerekçesi — uygulamanın yalnızca yerel ağda çalışması — hâlâ geçerli;
> değişen şey, işlevi **hiç geliştirmemenin** ileride yeni sürüm beklemek anlamına
> gelmesidir. Geliştirip kapalı tutmak, kararı kod değişikliği olmadan tersine
> çevirmeyi mümkün kılar.

**Uygulama kuralları:**

- 2FA açıkken, parola doğrulandıktan sonra doğrulama kodu istenir. Kod, üyelik
  akışıyla **aynı altyapıyı** kullanır (§3): aynı uzunluk, aynı süre, aynı deneme
  sınırı, hash'li saklama, tek kullanımlık. İkinci bir kod mekanizması kurulmaz.
- Parametrenin **açık ve kapalı hâli ayrı ayrı test edilir.** Varsayılanı kapalı olan
  bir işlev test edilmezse ilk açıldığı gün bozuk çıkar.
- Parametre açılmadan önce, hiçbir doğrulama kanalı bulunmayan personel listelenir ve
  uyarı verilir. Aksi hâlde bu kişiler (2026-09-22 ölçümüyle 2 aktif personel, `R-08`) sisteme **giremez**
  hâle gelir ve nedeni anlaşılmaz.

### 8. Oturum yönetimi

| Konu | Karar |
|---|---|
| Yöntem | **JWT erişim jetonu** + **yenileme jetonu** |
| Erişim jetonu ömrü | 15 dakika |
| Giriş kimliği | **Kurumsal e-posta adresi** (`KR-073`) — sicil birden fazla olabildiği için uygun değil |
| Yenileme jetonu ömrü | 8 saat (mesai günü); hareketsizlikte **30 dakika** sonra düşer (`KR-074`; önceki değer 1 saat) |
| Meşru uzun etkinlik | Eğitim videosu gibi etkinlik sürerken sayfa **etkinlik sinyali** gönderir; hareketsizlik sayacı sıfırlanır. 8 saatlik üst sınır her durumda geçerlidir (`KR-074`) |
| Erişim jetonu saklama | Tarayıcı belleğinde (JavaScript değişkeni) — **`localStorage` kullanılmaz** |
| Yenileme jetonu saklama | **`HttpOnly`, `Secure`, `SameSite=Strict` çerez** |
| Yenileme jetonu döndürme | Her kullanımda yeni jeton üretilir; eskisi geçersizleşir (rotation) |
| Yeniden kullanım tespiti | Kullanılmış bir yenileme jetonu tekrar gelirse **tüm oturumlar sonlandırılır** ve olay kaydedilir |
| Çıkış | Yenileme jetonu sunucu tarafında iptal edilir |

> **Gerçekleştirme (28.09.2026, `KR-086`, #92).** Uç noktalar `/api/v1/identity/sessions`
> altındadır: giriş, iki adımlı doğrulama, `refresh`, `activity`, `current` (çıkış).
> Yenileme jetonu `hrms_refresh` çerezinde, yalnızca bu yola gönderilir; veritabanında
> SHA-256 özeti tutulur. Erişim jetonu (HMAC-SHA256, `Identity__JwtSigningKey`) her istekte
> oturum kaydıyla da doğrulanır. Oturum sona erdiğinde yanıtın hata türü nedeni taşır
> (`session-ended/signed-in-elsewhere`, `idle-timeout`, `expired`, `token-reuse`,
> `account-changed`, `logged-out`).
>
> **İstemci (28.09.2026, `KR-087`, #95).** Erişim jetonu sekmenin belleğindedir; sayfa
> açılışında oturum yenileme çereziyle geri getirilir. Jeton, süresi dolmadan 30 saniye önce
> veya bir istek `401` aldığında yenilenir; sekme içinde aynı anda gelen istekler tek
> yenilemeyi bekler, sekmeler arasında yenilemeler `navigator.locks` ile sıraya girer (aksi
> hâlde aynı çerezi gönderen iki sekme jeton tekrarına düşerdi). Etkinlik sinyali görünür
> sekmedeki kullanıcı etkileşimi ve oynayan medya için dakikada en fazla bir kez gider
> (AN-23); hareketsizlik dolmadan 2 dakika önce uyarı gösterilir. Sekmeler giriş, çıkış ve son
> etkileşimi `BroadcastChannel` ile birbirine bildirir; jeton iletilmez.

**`localStorage` kullanılmama gerekçesi:** XSS açığı durumunda `localStorage`'daki jeton
JavaScript ile okunabilir. `HttpOnly` çerez okunamaz.

### 9. Hesabın yaşam döngüsü

| Olay | Davranış |
|---|---|
| Tüm aktif istihdamlar sona erdi | Hesap senkronizasyonda **otomatik pasife düşer** (`KR-015`); açık oturumlar sonlandırılır |
| Kişi yeniden işe girdi | Yeni istihdam eklenir; **mevcut hesap yeniden aktifleşir**, yeni hesap açılmaz |
| İK tarafından pasife alma | Elle mümkün; gerekçe zorunlu, kayıt altına alınır |

## Gerekçe

- Dizin hizmeti olmadığı için kendi kimlik yönetimimiz zorunludur; ancak LOGO'yu kimlik
  kaynağı olarak kullanmak, ayrı bir kullanıcı listesi yönetme yükünü ortadan kaldırır.
- Doğrulama kodunun **kayıtlı** iletişim bilgisine gitmesi, kimliği kanıtlayan asıl
  adımdır. TCKN ve doğum tarihi kurum içinde bilinebilir bilgilerdir; tek başlarına
  yeterli değildir.
- Aynı mesaj/aynı ekran kuralı, düşük maliyetli ama etkili bir bilgi sızıntısı önlemidir.
- Yenileme jetonu döndürme + yeniden kullanım tespiti, jeton çalınması durumunda
  saldırıyı tespit edilebilir kılar.

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| Keycloak / harici kimlik sağlayıcı | Ek bir sistemin kurulumu, yedeklenmesi ve bakımı; 584 kullanıcı için orantısız |
| Yalnızca çerez tabanlı oturum (JWT'siz) | SPA + API mimarisinde jeton yaklaşımı daha uygun; ileride mobil istemci gerekirse hazır |
| Erişim jetonunu `localStorage`'da tutmak | XSS'e açık |
| Doğrulama kodunu kullanıcının girdiği e-postaya göndermek | Kimlik doğrulamaz; herkes kendi adresini girerek başkasının adına hesap açabilir |
| Zorunlu periyodik parola değişimi | Güncel rehberler önermiyor; zayıf kalıplara itiyor |

## Sonuçlar

**Olumlu:** Dış bağımlılık yok; kimlik kaynağı tek (LOGO); bilinen saldırı yüzeyleri
(enumeration, XSS, jeton çalınması, kaba kuvvet) baştan ele alınmış.

**Olumsuz / kabul edilen ödünler:**
- 2FA varsayılan olarak kapalıdır; bu, ağın yerel olmasına dayanan bir varsayımdır.
  Varsayım değişirse **parametre açılır** — geliştirme gerekmez. Karar
  yeniden değerlendirilecektir.
- Kurumsal e-postası olmayan personel **giriş yapamaz**: giriş kimliği e-postadır
  (`KR-073`). Ölçüm: 09.09.2026'da 38, 22.09.2026'da **4** kişi; bunların 2'sinin
  hiçbir iletişim bilgisi yoktur ve sisteme alınmayacaktır (`KR-076`). Telefonu olup
  e-postası olmayan 2 kişiye İK kurumsal e-posta tanımlayacaktır (24.09.2026, `R-08`).

**Yükümlülükler:**
- Hız sınırlama, kilitleme ve kod hijyeni kuralları birim testleriyle kapsanacaktır.
- Kimlik doğrulama akışının tamamı için uçtan uca (Playwright) testi yazılacaktır.
- Kanal seçilebilirliği kuralları (§4) yapılandırılabilir tutulacaktır.

## Geri dönüş maliyeti

**Orta.** Oturum yöntemi ve parola politikası değiştirilebilir. Üyelik akışının
kimlik kaynağı (LOGO) değişirse ADR-0003'teki yalıtım katmanı sayesinde etki sınırlıdır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
| 2026-09-21 | 0.2 | §6 parola uzunluğu 12 → 6 (`KR-070`); §7 yeniden yazıldı: 2FA geliştirilecek, varsayılan kapalı (`KR-069`, `KR-017` yürürlükten kalktı) | Bilgi İşlem |
| 2026-09-24 | 0.3 | §8: giriş kimliği kurumsal e-posta (`KR-073`), hareketsizlik 30 dk ve meşru uzun etkinlik (`KR-074`); ödünler bölümündeki bayat "39 personel" güncellendi | Bilgi İşlem |
| 2026-09-24 | 0.4 | §3 kod saklama: anahtarlı özet; §5 kilit sayacı girilen e-postaya bağlı; §4 kanal tablosu `KR-073`, `KR-075`, `KR-076` ile yeniden yazıldı (İK destekli davet, `KR-020` konusuz) | Bilgi İşlem |
| 2026-09-27 | 0.5 | §1 ve §6: gerçekleştirme notları (eşleşme gizliliğinin tüm akışa yayılması, parola özeti ve liste; `KR-085`, #87) | Bilgi İşlem |
| 2026-09-28 | 0.6 | §8: gerçekleştirme notu (uç noktalar, çerez, her istekte oturum doğrulaması; `KR-086`, #92) | Bilgi İşlem |
| 2026-09-28 | 0.7 | §8: istemci tarafı gerçekleştirme notu (bellekte jeton, sekmeler arası yenileme, etkinlik sinyali; `KR-087`, #95) | Bilgi İşlem |
| 2026-09-29 | 0.8 | §6: parola sıfırlama ve oturum içinde değişiklik gerçekleştirme notu (`KR-088`, #98) | Bilgi İşlem |
| 2026-09-30 | 0.9 | §4: İK davet bağlantısı gerçekleştirme notu (`KR-090`, #107) | Bilgi İşlem |
| 2026-10-01 | 1.0 | §6: zorunlu parola değişimi gerçekleştirme notu (`KR-092`, #113) | Bilgi İşlem |
