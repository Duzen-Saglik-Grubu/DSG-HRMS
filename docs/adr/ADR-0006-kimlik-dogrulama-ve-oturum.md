# ADR-0006 — Kimlik Doğrulama, Üyelik ve Oturum Yönetimi

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-013`, `KR-014`, `KR-015`, `KR-016`, `KR-017`, `KR-018`, `KR-019`, `KR-020`, `KR-042`
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
| Saklama | Veritabanında **hash'lenmiş** olarak (düz metin saklanmaz) |
| Loglama | Kod **hiçbir yere loglanmaz** (SMS/e-posta içeriği dâhil) |
| Tek kullanımlık | Doğrulanan kod anında geçersiz kılınır |

### 4. Kanal seçilebilirliği kuralları

| Durum | E-posta ile doğrulama | SMS ile doğrulama |
|---|---|---|
| Kurumsal e-posta kayıtlı ve kişiye özel | ✅ | — |
| E-posta **kurumsal alan adı dışında** | ❌ Sunulmaz (`KR-019`) | ✅ |
| E-posta **birden fazla kişiye tanımlı** | ❌ Sunulmaz (`KR-018`) | ✅ |
| E-posta kayıtlı değil | ❌ | ✅ |
| Cep telefonu kayıtlı değil | — | ❌ |
| Hiçbiri kullanılamıyor | İstisna akışı (İK elle açar, gerekçe girer, tam loglanır) | |

**Kabul edilen kurumsal alan adları** (`@duzen.com.tr`, `@zeytinim.com`, `@labpt.com.tr`)
koda gömülmez; **yönetilebilir parametre** olarak tutulur (`KR-019`).

Kurumsal adresi olmayan personel için SMS **zorunlu kılınmaz** (`KR-020`); kalıcı çözüm
İK'nın LOGO'da adres tanımlamasıdır.

### 5. Hız sınırlama ve kilitleme

| Kapsam | Sınır |
|---|---|
| TCKN başına üyelik denemesi | 5 deneme / saat |
| IP başına üyelik denemesi | 20 deneme / saat |
| Kod gönderimi (kişi başına) | 3 gönderim / 15 dakika |
| Giriş denemesi (hesap başına) | 5 başarısız deneme → 15 dakika kilit |

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
- Zorunlu periyodik parola değişimi **varsayılan olarak uygulanmaz** (güncel güvenlik
  rehberleri bunu önermiyor; kullanıcıyı zayıf kalıplara itiyor). Sistem Yönetimi
  parametresiyle açılabilir.
- Parola sıfırlama, üyelik akışıyla aynı doğrulama mekanizmasını kullanır.

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
  e-postası olmayan 2 kişi için karar bekleniyor (toplantı kaydı §10.1, `R-08`).

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
