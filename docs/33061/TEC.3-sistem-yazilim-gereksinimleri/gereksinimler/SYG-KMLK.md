# SYG-KMLK — T3 Kimlik Yönetimi Sistem/Yazılım Gereksinimleri

**Belge kimliği:** TEC.3-SYG-KMLK
**Süreç:** TEC.3 — Sistem/Yazılım Gereksinimlerinin Tanımlanması
**Modül:** T3 Kimlik Yönetimi
**Kaynak:** `TEC.2/paydas-gereksinimleri/PG-KMLK.md` (23.09.2026'da İK onaylı, 56 gereksinim)
**Son güncelleme:** 2026-10-03
**Sahibi:** Bilgi İşlem

> **Paydaş gereksinimi *ne* istendiğini söyler; bu belge sistemin bunu *hangi
> davranışla* karşılayacağını söyler.** Her madde doğrulanabilir, tek anlamlıdır ve en
> az bir onaylı paydaş gereksinimine bağlıdır. Paydaş gereksinimine bağlanmayan
> sistem gereksinimi yazılmaz; paydaş gereksinimi olmadan eklenen davranış, onaylanmamış
> kapsamdır.

> **Sistem mi, yazılım mı?** TS ISO/IEC TS 33061'de TEC.3 tek bir süreçtir ve ikisini
> birlikte kapsar. DSG-HRMS'nin donanım veya elle işletilen bir öğesi yoktur; sistemin
> tüm öğeleri yazılımdır. Bu nedenle sistem ve yazılım gereksinimleri **tek belgede**
> tutulur. Donanım veya altyapı öğesi eklenirse (ör. ayrı bir ileti sunucusu) bu ayrım
> yeniden değerlendirilir.

**Kapsama her PR'da otomatik denetlenir** (`YAKLASIM.md` §4): her paydaş gereksinimi
en az bir sistem gereksinimine bağlı olmalı; bu belgenin §8 tablosu ve izlenebilirlik
matrisinin §5 sütunu, §4 ile aynı eşlemeyi göstermelidir.

---

## 1. Özet

| | |
|---|---:|
| Sistem gereksinimi | **81** |
| Karşılanan paydaş gereksinimi | **56 / 56** |
| Tür — İşlevsel | 46 |
| Tür — Performans | 3 |
| Tür — Arayüz | 4 |
| Tür — İşlevsel olmayan | 14 |
| Tür — Kısıt | 12 |
| Doğrulama — Test | 71 |
| Doğrulama — Gösterim | 3 |
| Doğrulama — İnceleme | 2 |
| Doğrulama — Analiz | 3 |
| Parametreye bağlı | 31 |
| Kritik performans ölçütü | 6 |

---

## 2. Sistem tanımı (TEC.3 çıktısı a)

### 2.1 Amaç

T3, personelin HRMS'ye **kendi hesabını açması, güvenle giriş yapması ve hesabının
istihdam durumuna göre otomatik yönetilmesi** için gereken kimlik altyapısını kurar.
Sonraki tüm modüller bu altyapının üzerinde çalışır.

### 2.2 Öğeler

| Öğe | Ne yapar | Konum (ADR-0002, ADR-0015) |
|---|---|---|
| Kimlik ekranları | Giriş, üyelik, kod doğrulama, parola, İK hesap işlemleri, parametre | `src/frontend/dsg-hrms-web/src/features/identity/` |
| Kimlik API'si | Üyelik, oturum, parola, davet, hesap işlemleri, senkronizasyon tetikleme | `Dsg.Hrms.Api` — `/api/v1/identity/` (AN-21) |
| Kimlik uygulama hizmetleri | İş kuralları: eşleştirme, kanal seçimi, kod, oturum, kilit, parola politikası | `Dsg.Hrms.Application` |
| Kişi / istihdam modeli | Kişi (TCKN tekil) ve istihdam (sicil tekil) kayıtları — `KR-077` sınırında | `Dsg.Hrms.Domain` |
| LOGO senkronizasyon işi | Periyodik ve elle tetiklenen salt-okunur aktarım | `Dsg.Hrms.Infrastructure` (arka plan hizmeti) |
| İleti kuyruğu | Doğrulama kodu, sıfırlama ve davet iletilerinin arka planda gönderimi | `Dsg.Hrms.Infrastructure` |
| Parametre deposu | T3 parametrelerinin değeri, türü, aralığı | PostgreSQL |
| Denetim izi | Mevcut altyapı (ADR-0009) — kimlik olayları eklenir | PostgreSQL `audit` şeması |

### 2.3 Dış arayüzler

| Arayüz | Yön | Protokol | Kısıt |
|---|---|---|---|
| **LOGO veritabanı** (MSSQL) | Yalnızca okuma | TDS | **Yazma, güncelleme, silme kesinlikle yapılmaz** (`KR-003`, `KR-004`) — SYG-KMLK-002, 003 |
| **NetGSM** | Giden | HTTPS | Standart SMS servisi; hata kodları `analiz/02` |
| **Kurum SMTP sunucusu** | Giden | SMTP + TLS | Gönderen hesap `PRM-ENT-05` |
| **Tarayıcı** | Gelen | HTTPS | Yalnızca kurum içi erişim (`KR-067`); jeton çerezi `Secure` |

### 2.4 Sınırlar

**Kapsamdadır:** §4'teki 81 gereksinim. Bunlara, onaylı gereksinimlerin **ön koşulu**
olan dört altyapı dâhildir: kişi/istihdam modeli ve senkronizasyon (`KR-077`), eylem
yetkisi altyapısı (SYG-KMLK-074), parametre deposu (SYG-KMLK-075, 076) ve İK hesap
işlemleri ekranı (SYG-KMLK-073).

**Kapsam dışıdır:**

| Konu | Nerede |
|---|---|
| Şube, birim, görev, fotoğraf ve diğer personel alanları; personel ekranları | T1 Personel Yönetimi |
| Organizasyon yapısı, "Yöneticisi" ilişkisi | T2 Organizasyon |
| Rol yönetim ekranı, satır bazlı kapsam (`DirectReports` vb.) | T4 Rol ve Yetki |
| Kapsamlı kullanıcı yönetimi | T5 Kullanıcı Yönetimi |
| Bildirim merkezi, bildirim istisnası (#3) | Y1 |
| Parametre ekranının tamamı | Y4 Sistem Yönetimi |

> **Kapsam dışı öğelerin en küçük hâlleri neden T3'te?** Üç onaylı gereksinim
> (REQ-KMLK-011, 012, 036) T3'ün kabulünde **gösterilmesi gereken** işlemler
> tanımlıyor: İK davet gönderir, sistem yöneticisi alan adı ekler, İK hesabı pasife
> alır. Bunlar, sahibi olan modül gelene kadar beklerse T3 kabul edilemez. Bu nedenle
> her biri yalnızca T3'ün ihtiyacı kadar kurulur ve sahibi olan modül bunu
> **genişletir, yeniden yazmaz.**

---

## 3. Durumlar ve kipler (TEC.3 BP2)

### 3.1 Hesap durumları

| Durum | Anlamı | Giriş yapılabilir mi? |
|---|---|---|
| *Yok* | Kişi var, hesap açılmamış | — |
| **Aktif** | Hesap kullanımda | Evet |
| **Kilitli** | Başarısız giriş sınırı aşıldı; süre sonunda kendiliğinden kalkar | Hayır |
| **Pasif** | Tüm istihdamlar bitti veya elle pasife alındı | Hayır |

| Geçiş | Tetikleyen | Gereksinim |
|---|---|---|
| Yok → Aktif | Üyelik veya davet bağlantısıyla parola oluşturma | SYG-KMLK-013, 051 |
| Aktif → Kilitli | Başarısız giriş sınırı | SYG-KMLK-033 |
| Kilitli → Aktif | Kilit süresinin dolması | SYG-KMLK-033 |
| Aktif → Pasif | Tüm istihdamların bitmesi (senkronizasyon) veya elle | SYG-KMLK-054, 057 |
| Pasif → Aktif | Yeni aktif istihdam (senkronizasyon) veya elle | SYG-KMLK-056, 057 |

Her geçiş denetim izine yazılır (SYG-KMLK-058).

### 3.2 Doğrulama kodunun durumları

`Üretildi` → `Doğrulandı` (tek kullanım) · `Süresi doldu` · `Deneme sınırı aşıldı` ·
`Geçersiz kılındı` (kanal değişimi veya yeni kod). `Üretildi` dışındaki her durum
**sondur**; kod bir daha kabul edilmez (SYG-KMLK-023, 024, 027, 019).

### 3.3 Sistem kipleri

| Kip | Davranış | Gereksinim |
|---|---|---|
| LOGO erişilemez | Sistem son başarılı anlık görüntüyle çalışır; senkronizasyon "başarısız" kaydeder | SYG-KMLK-010, 011 |
| LOGO şeması beklenenden farklı | Senkronizasyon başlamaz | SYG-KMLK-012 |
| İki adımlı doğrulama kapalı / açık | Giriş tek adım / iki adım | SYG-KMLK-034, 036 |
| Senkronizasyon çalışıyor | İkinci çalışma başlatılamaz | SYG-KMLK-004 |

---

## 4. Gereksinimler (TEC.3 çıktısı b)

**Tür:** İşlevsel · Performans · Arayüz · İşlevsel olmayan (güvenlik, kullanılabilirlik,
dayanıklılık) · Kısıt (tasarım ve gerçekleştirme kısıtı).
**Doğrulama:** Test (otomatik, CI'da) · Gösterim (kabulde çalışırken gösterilir) ·
İnceleme (belge veya kod incelemesi) · Analiz (UAT'de ölçüm veya çalışma kaydından hesap).

### 4.1 Veri ve senkronizasyon (KR-077 kapsamı)

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-001** | Sistem **kişi** ve **istihdam** kayıtlarını ayrı tutar. Kişi TCKN ile, istihdam sicil numarasıyla tekildir. Bir kişinin birden fazla eş zamanlı **aktif** istihdamı olabilir; model bunu kısıtlamaz. | İşlevsel | REQ-KMLK-003, REQ-KMLK-014 | Test | — |
| **SYG-KMLK-002** | LOGO'ya erişim yalnızca `ILogoPersonnelSource` arayüzü üzerinden yapılır. Bu erişimde kullanılan veritabanı bağlamı izlemesizdir (`NoTracking`) ve **kaydetme çağrısında istisna fırlatır.** Veritabanı oturumu `INSERT/UPDATE/DELETE/ALTER/EXECUTE` yetkilerinden `DENY` ile yoksundur. | Kısıt | REQ-KMLK-003 | Test | — |
| **SYG-KMLK-003** | LOGO'ya yazma denemesinin **veritabanı tarafından reddedildiği**, CI'da çalışan otomatik bir testle kanıtlanır. (Bugüne kadar bu yalnızca elle doğrulanmıştı — izlenebilirlik matrisi §4.1.) | Kısıt | REQ-KMLK-003 | Test | — |
| **SYG-KMLK-004** | Senkronizasyon periyodik olarak ve elle tetiklenerek çalışır. Her çalışmada kaynaktaki tam liste okunur, HRMS'teki kayıtlarla karşılaştırılır ve **yalnızca fark** uygulanır. Aynı anda iki senkronizasyon çalışamaz. | İşlevsel | REQ-KMLK-003, REQ-KMLK-034 | Test | PRM-ENT-07 |
| **SYG-KMLK-005** | Her senkronizasyon çalışması kalıcı bir kayıt üretir: başlangıç ve bitiş zamanı, sonuç (başarılı / kısmen / başarısız), okunan, eklenen, güncellenen, pasifleşen ve uyarı üretilen kayıt sayıları. | İşlevsel | REQ-KMLK-034, REQ-KMLK-040 | Test | — |
| **SYG-KMLK-006** | **T3 kapsamında senkronize edilen alanlar** yalnızca şunlardır — kişi: TCKN, ad, soyad, doğum tarihi, kurumsal e-posta, cep telefonu; istihdam: sicil numarası, firma, işe giriş tarihi, işten çıkış tarihi, aktiflik. Şube, birim, görev, fotoğraf ve diğer alanlar **T1 kapsamındadır** ve bu çalışmada senkronize edilmez. | Kısıt | REQ-KMLK-002, REQ-KMLK-003 | İnceleme | — |
| **SYG-KMLK-007** | TCKN'si boş veya geçersiz olan LOGO kartı için kişi kaydı **oluşturulmaz**; kart, gerekçesiyle birlikte veri kalitesi uyarı listesine alınır. | İşlevsel | REQ-KMLK-013 | Test | — |
| **SYG-KMLK-008** | Aynı kurumsal e-posta adresi, **aktif istihdamı olan** birden fazla kişiye (farklı TCKN) tanımlıysa, bu kişilerin tümü uyarı listesine alınır ve bu adres **ne giriş kimliği ne doğrulama hedefi** olarak kullanılabilir. Ayrılmış personelin kartında kalan adres paylaşım sayılmaz (`KR-079`). | İşlevsel | REQ-KMLK-009, REQ-KMLK-022 | Test | — |
| **SYG-KMLK-009** | E-posta ve telefon senkronizasyonda normalleştirilir. E-posta: baştaki ve sondaki boşluklar ile satır sonu karakterleri kaldırılır, küçük harfe çevrilir. Telefon: boşluk, CR/LF ve sekme karakterleri kaldırılır, baştaki `0` veya `90` atılır; sonuç 10 hane değilse veya `5` ile başlamıyorsa **geçersiz** sayılır ve uyarı üretilir. | İşlevsel | REQ-KMLK-008, REQ-KMLK-010 | Test | — |
| **SYG-KMLK-010** | Senkronizasyon hatası sessiz kalmaz: hata kaydedilir, çalışma kaydı "başarısız" olarak işaretlenir ve sistem sağlığı uç noktası bunu gösterir. Sistem yöneticisi rolüne bildirim, Y1 Bildirim Merkezi devreye girdiğinde eklenir. | İşlevsel olmayan | REQ-KMLK-040 | Test | PRM-BLD-01 |
| **SYG-KMLK-011** | LOGO erişilemez olduğunda sistem **çalışmaya devam eder**; üyelik eşleştirmesi ve hesap işlemleri son başarılı anlık görüntüye göre yapılır. | İşlevsel olmayan | REQ-KMLK-003 | Test | — |
| **SYG-KMLK-012** | Beklenen LOGO tablo, kolon ve veri tiplerinin varlığı (şema sapması) CI'da entegrasyon testi olarak ve üretimde günde bir kez denetlenir; sapma bulunduğunda senkronizasyon başlamaz ve hata kaydedilir. | İşlevsel olmayan | REQ-KMLK-003 | Test | — |

### 4.2 Üyelik

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-013** | Üyelik; TCKN, doğum tarihi ve kurumsal e-posta ile başlatılır. Üç bilgi de, son anlık görüntüde **en az bir aktif istihdamı olan** tek bir kişiyle eşleşmelidir. E-posta karşılaştırması büyük/küçük harf duyarsızdır. | İşlevsel | REQ-KMLK-001, REQ-KMLK-002, REQ-KMLK-003 | Test | — |
| **SYG-KMLK-014** | TCKN, 11 hane ve resmî sağlama algoritmasıyla hem istemcide hem sunucuda doğrulanır. Geçersiz TCKN eşleştirmeye gönderilmez; kullanıcıya biçim hatası gösterilir (bu, kişinin varlığı hakkında bilgi vermez). | İşlevsel | REQ-KMLK-002 | Test | — |
| **SYG-KMLK-015** | Eşleşme olsa da olmasa da sunucu **aynı durum kodunu ve aynı yanıt gövdesini** döndürür. Kod gönderimi istek içinde değil, **arka plan kuyruğunda** yapılır; böylece gönderim süresi yanıt süresine yansımaz. Yanıt süresi ayrıca sabit bir alt sınıra tamamlanır. | İşlevsel olmayan | REQ-KMLK-004 | Test | — |
| **SYG-KMLK-016** | Eşleşme yoksa **hiçbir kod gönderilmez** ve kanal seçimi ekranında iki kanal da gösterilir. Kod ekranında her durumda şu anlamda bir ileti yer alır: "Bilgileriniz kayıtlarımızla eşleşiyorsa kod birkaç dakika içinde gelir; gelmezse bilgilerinizi kontrol edin veya Bilgi İşlem'e başvurun." | İşlevsel | REQ-KMLK-004, REQ-KMLK-005 | Test | — |
| **SYG-KMLK-017** | Eşleşme varsa yalnızca kullanılabilir kanallar sunulur: e-posta kanalı için adresin kabul edilen kurumsal alan adlarından birinde ve kişiye tekil olması; SMS kanalı için geçerli bir cep telefonunun bulunması gerekir. Kullanılabilir kanal yoksa akış, eşleşme yokmuş gibi davranır. | İşlevsel | REQ-KMLK-005, REQ-KMLK-007, REQ-KMLK-008, REQ-KMLK-009, REQ-KMLK-010, REQ-KMLK-012 | Test | PRM-KML-01, PRM-KML-02 |
| **SYG-KMLK-018** | Kodun gönderileceği hedef (e-posta adresi veya telefon numarası), **maskeli biçimde dahi** ekranda gösterilmez. | İşlevsel olmayan | REQ-KMLK-004 | Test | — |
| **SYG-KMLK-019** | Kullanıcı kanal değiştirdiğinde önceki kod geçersiz kılınır ve seçilen kanala yeni kod gönderilir. Kanal değiştirme de kod gönderim hız sınırına tabidir. | İşlevsel | REQ-KMLK-006 | Test | PRM-KML-17 |
| **SYG-KMLK-020** | Kişinin zaten aktif bir hesabı varsa akış **aynı biçimde** ilerler; doğrulama başarıyla tamamlandıktan sonra kullanıcıya hesabının bulunduğu bildirilir ve parola sıfırlamaya yönlendirilir. İkinci hesap oluşmaz. | İşlevsel | REQ-KMLK-004, REQ-KMLK-014 | Test | — |
| **SYG-KMLK-021** | Bir kişiye en fazla bir hesap bağlanabilir; bu, veritabanında tekillik kısıtıyla zorlanır. | Kısıt | REQ-KMLK-014 | Test | — |

### 4.3 Doğrulama kodu ve iletim

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-022** | Doğrulama kodu kriptografik olarak güvenli rastgele sayı üreteciyle üretilen, parametredeki uzunlukta bir sayıdır (varsayılan 6 hane). | İşlevsel | REQ-KMLK-015 | Test | PRM-KML-09 |
| **SYG-KMLK-023** | Kod, üretildikten sonra parametredeki süre boyunca geçerlidir (varsayılan 5 dakika); süre dolduğunda kabul edilmez ve kullanıcıya bu durum açıkça bildirilir. | İşlevsel | REQ-KMLK-016 | Test | PRM-KML-10 |
| **SYG-KMLK-024** | Yanlış deneme sayısı parametredeki sınırı aştığında (varsayılan 3) kod iptal edilir; yeni kod istenmesi gerekir. | İşlevsel | REQ-KMLK-017 | Test | PRM-KML-16 |
| **SYG-KMLK-025** | Kod veritabanında **anahtarlı özet (HMAC-SHA256)** olarak saklanır; anahtar sır yönetiminden okunur. Düz özet yeterli değildir: 6 haneli bir kodun 1.000.000 olasılığı saniyeler içinde denenebilir. | Kısıt | REQ-KMLK-018 | Test | — |
| **SYG-KMLK-026** | Kod ve kodu içeren ileti gövdesi hiçbir günlük, denetim kaydı veya hata iletisine yazılmaz. | İşlevsel olmayan | REQ-KMLK-018, REQ-KMLK-041 | Test | — |
| **SYG-KMLK-027** | Doğrulanan kod aynı işlemde geçersiz kılınır; eş zamanlı iki doğrulama isteğinden yalnızca biri başarılı olabilir. | İşlevsel | REQ-KMLK-019 | Test | — |
| **SYG-KMLK-028** | Kod ekranı, kalan geçerlilik süresini geri sayım olarak gösterir ve "kodu tekrar gönder" seçeneği sunar; tekrar gönderim hız sınırına tabidir. | İşlevsel | REQ-KMLK-020 | Gösterim | PRM-KML-17 |
| **SYG-KMLK-029** | SMS, NetGSM standart servisiyle ve parametredeki gönderici başlığıyla gönderilir. NetGSM hata kodları `analiz/02` tablosuna göre yeniden denenir veya kalıcı hata sayılır. | Arayüz | REQ-KMLK-021 | Test | PRM-ENT-01, PRM-ENT-02, PRM-ENT-03 |
| **SYG-KMLK-030** | E-posta, kurum SMTP sunucusu üzerinden, Türkçe ve kurum adını taşıyan bir şablonla gönderilir. İleti, kod veya bağlantı dışında kişisel veri içermez. | Arayüz | REQ-KMLK-005, REQ-KMLK-011 | Test | PRM-ENT-04, PRM-ENT-05, PRM-ENT-06 |

### 4.4 Giriş ve oturum

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-031** | Giriş kimliği **kurumsal e-posta adresidir**; karşılaştırma büyük/küçük harf duyarsızdır. Sicil numarası veya TCKN ile giriş kabul edilmez. | İşlevsel | REQ-KMLK-022 | Test | — |
| **SYG-KMLK-032** | Hatalı girişte, kullanıcının var olup olmadığından bağımsız olarak aynı ileti ve aynı durum kodu döndürülür. Var olmayan kullanıcı için de parola doğrulama işlemi yapılır, böylece yanıt süreleri ayırt edilemez. | İşlevsel olmayan | REQ-KMLK-023 | Test | — |
| **SYG-KMLK-033** | Başarısız giriş sayısı sınırı aştığında (varsayılan 5) giriş, parametredeki süre boyunca (varsayılan 15 dakika) reddedilir; bu sürede doğru parola da kabul edilmez. Sayaç, hesabın var olup olmadığından bağımsız olarak **girilen e-posta adresine** göre tutulur. | İşlevsel | REQ-KMLK-024 | Test | PRM-KML-03, PRM-KML-04 |
| **SYG-KMLK-034** | İki adımlı doğrulama parametreyle kullanıma açılıp kapatılır (varsayılan **kapalı**). Parametre açıkken, **2FA'yı kendi hesabında açmış** kullanıcıdan parola doğrulandıktan sonra kod adımı istenir (#190); kod, üyelikle aynı altyapıyı ve aynı kuralları (uzunluk, süre, deneme sınırı, özetli saklama, tek kullanım) kullanır. | İşlevsel | REQ-KMLK-025, REQ-KMLK-051 | Test | PRM-KML-08 |
| **SYG-KMLK-035** | İki adımlı doğrulama parametresi açılmadan önce, 2FA'yı kendi hesabında açmış aktif hesap sahiplerinin sayısı hesaplanıp yöneticiye gösterilir; sayı sıfırdan büyükse onay istenir (#190). | İşlevsel | REQ-KMLK-052 | Test | PRM-KML-15 |
| **SYG-KMLK-036** | İki adımlı doğrulamanın sistem ve kullanıcı düzeyindeki açık ve kapalı hâllerinin her birleşimi ayrı otomatik testlerle doğrulanır (#190). | Kısıt | REQ-KMLK-053 | Test | — |
| **SYG-KMLK-037** | Oturum, kısa ömürlü bir erişim jetonu (varsayılan 15 dakika, tarayıcı belleğinde) ve bir yenileme jetonuyla (`HttpOnly`, `Secure`, `SameSite=Strict` çerez) yönetilir. Oturumun toplam süresi parametredeki üst sınırı (varsayılan 8 saat) aşamaz. | Kısıt | REQ-KMLK-026 | Test | PRM-KML-11, PRM-KML-12 |
| **SYG-KMLK-038** | Hareketsizlik süresi (varsayılan **30 dakika**) **kullanıcı etkileşimine** göre ölçülür. Arka plan istekleri (sağlık kontrolü, veri yenileme, bildirim yoklaması) etkinlik sayılmaz; yenileme jetonu, son kullanıcı etkileşiminden bu yana süre dolmuşsa kabul edilmez. | İşlevsel | REQ-KMLK-026, REQ-KMLK-054 | Test | PRM-KML-13 |
| **SYG-KMLK-039** | Sistem, meşru uzun süreli etkinlik için bir **etkinlik sinyali** uç noktası sunar. Sinyal yalnızca doğrulanmış oturumdan kabul edilir, hareketsizlik sayacını sıfırlar, **toplam oturum süresini uzatmaz** ve dakikada en fazla 2 kez kabul edilir. İstemci sinyali yalnızca medya oynarken ve sekme görünürken gönderir. | İşlevsel | REQ-KMLK-054 | Test | — |
| **SYG-KMLK-040** | Yenileme jetonu her kullanımda yenilenir ve eskisi geçersizleşir. Kullanılmış bir jeton tekrar sunulursa kullanıcının **tüm oturumları** sonlandırılır ve olay kaydedilir. | İşlevsel olmayan | REQ-KMLK-026 | Test | — |
| **SYG-KMLK-041** | Tek aktif oturum kuralı açıkken (varsayılan açık), başarılı bir giriş kullanıcının önceki tüm yenileme jetonlarını iptal eder. | İşlevsel | REQ-KMLK-049 | Test | PRM-KML-14 |
| **SYG-KMLK-042** | Oturumu başka bir girişle sonlandırılan istemci, bir sonraki yenilemede makine tarafından okunabilir bir neden kodu alır ve kullanıcıya "Hesabınıza başka bir cihazdan giriş yapıldı" iletisi gösterilir. | İşlevsel | REQ-KMLK-050 | Test | — |
| **SYG-KMLK-043** | Çıkışta yenileme jetonu sunucu tarafında iptal edilir; iptal edilmiş jetonla yenileme yapılamaz. | İşlevsel | REQ-KMLK-027 | Test | — |

### 4.5 Parola

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-044** | Parola en az parametredeki uzunlukta (varsayılan 6), en fazla 128 karakter olabilir. Karmaşıklık zorunluluğu parametreyle açılır (varsayılan kapalı). Unicode karakterler kabul edilir ve karşılaştırmadan önce NFKC biçimine normalleştirilir. | İşlevsel | REQ-KMLK-028 | Test | PRM-KML-05, PRM-KML-06 |
| **SYG-KMLK-045** | Parola, uygulamaya gömülü **yaygın parola listesinde** bulunursa reddedilir. Liste en az 100.000 kayıt içerir, açık lisanslı bir kaynaktan alınır ve kuruma özgü sözcüklerle genişletilir (kurum ve grup şirketi adları; kullanıcının adı, soyadı ve e-posta adresinin yerel kısmı). Denetim çevrimdışıdır ve **devre dışı bırakılamaz.** | İşlevsel olmayan | REQ-KMLK-029 | Test | — |
| **SYG-KMLK-046** | Zorunlu periyodik parola değişimi parametreyle açılır (varsayılan kapalı). Açıkken, parolası parametredeki süreden (varsayılan **90 gün**) eski olan kullanıcı girişte parolasını değiştirmeden uygulamayı kullanamaz. | İşlevsel | REQ-KMLK-030 | Test | PRM-KML-07, PRM-KML-21 |
| **SYG-KMLK-047** | Parola sıfırlama, üyelikle aynı eşleştirme, kanal ve kod akışını kullanır; ayrı bir doğrulama mekanizması bulunmaz. | İşlevsel | REQ-KMLK-031 | Test | PRM-KML-02, PRM-KML-09, PRM-KML-10 |
| **SYG-KMLK-048** | Oturum içinde parola değişikliği mevcut parolanın girilmesini gerektirir. Değişiklikten sonra kullanıcının **diğer tüm oturumları** sonlandırılır. | İşlevsel | REQ-KMLK-032 | Test | — |
| **SYG-KMLK-049** | Parola, uyarlanabilir maliyetli bir özet işleviyle (PBKDF2-HMAC-SHA512, en az 100.000 yineleme veya eşdeğeri) saklanır; özetleme süresi sunucuda 100–500 ms aralığında tutulur. Parola hiçbir kayda yazılmaz. | Kısıt | REQ-KMLK-033 | Test | — |
| **SYG-KMLK-050** | İlk girişte parola değiştirme zorunluluğu parametreyle açılır (varsayılan kapalı). Açıkken, daha önce hiç giriş yapmamış her hesap, parolasını üyelikte kendisi belirlemiş olsa da, ilk girişte parolasını değiştirmeden uygulamayı kullanamaz. | İşlevsel | REQ-KMLK-055 | Test | PRM-KML-20 |

### 4.6 İK destekli davet

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-051** | İK, bir personele tek kullanımlık parola oluşturma bağlantısı gönderebilir. Bağlantı yalnızca kişinin **LOGO'dan senkronize edilmiş**, kabul edilen kurumsal alan adlarından birindeki ve kişiye tekil e-posta adresine gider; böyle bir adres yoksa işlem reddedilir. İK adres giremez. | İşlevsel | REQ-KMLK-011 | Test | PRM-HSP-01, PRM-KML-01 |
| **SYG-KMLK-052** | Bağlantı, en az 256 bit rastgelelikte bir jeton taşır; jeton sunucuda özet olarak saklanır, tek kullanımlıktır ve parametredeki süre sonunda geçersizleşir (varsayılan **3 saat**). Yeni bağlantı gönderildiğinde öncekiler geçersizleşir. | Kısıt | REQ-KMLK-011 | Test | PRM-HSP-04 |
| **SYG-KMLK-053** | Bağlantı gönderimi gerekçe olmadan yapılamaz; gönderen, alıcı kişi, gerekçe ve zaman denetim izine yazılır. | İşlevsel | REQ-KMLK-011, REQ-KMLK-037 | Test | — |

### 4.7 Hesap yaşam döngüsü

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-054** | Senkronizasyonda kişinin **tüm** istihdamları sona ermişse hesabı pasife alınır ve açık oturumları ile yenileme jetonları iptal edilir. | İşlevsel | REQ-KMLK-034 | Test | PRM-HSP-02 |
| **SYG-KMLK-055** | Pasif hesapla giriş yapılamaz ve pasif hesabın sahibi hiçbir kaydı görüntüleyemez. | İşlevsel | REQ-KMLK-034 | Test | — |
| **SYG-KMLK-056** | Pasif hesabı olan kişi için senkronizasyonda yeni bir aktif istihdam görülürse **mevcut hesap** yeniden aktifleşir; yeni hesap oluşmaz. | İşlevsel | REQ-KMLK-035 | Test | — |
| **SYG-KMLK-057** | Yetkili kullanıcı bir hesabı gerekçe girerek elle pasife alabilir ve yeniden aktifleştirebilir. Gerekçe girilmeden işlem tamamlanmaz. | İşlevsel | REQ-KMLK-036 | Test | — |
| **SYG-KMLK-058** | Hesap durumundaki her değişiklik (oluşma, pasifleşme, yeniden aktifleşme, kilitlenme, kilit kalkması) önceki ve sonraki durumla birlikte denetim izine yazılır. | İşlevsel | REQ-KMLK-037 | Test | — |

### 4.8 Güvenlik, denetim ve KVKK

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-059** | Hız sınırları: üyelik denemesi TCKN başına saatte ve IP başına saatte, kod gönderimi kişi başına 15 dakikada parametredeki sayıları aşamaz. Aşımda istek `429` ile reddedilir ve olay kaydedilir. | İşlevsel olmayan | REQ-KMLK-038, REQ-KMLK-039 | Test | PRM-KML-17, PRM-KML-18, PRM-KML-19 |
| **SYG-KMLK-060** | Şu kimlik olayları denetim izine yazılır: üyelik başlatma, kod gönderimi, kod doğrulama (başarılı/başarısız), hesap oluşturma, giriş (başarılı/başarısız), kilitlenme, parola değişimi ve sıfırlama, oturum sonlandırma, jeton yeniden kullanımı, davet bağlantısı. Her kayıt kullanıcı, zaman, IP ve izleme kimliği taşır. | İşlevsel | REQ-KMLK-040 | Test | — |
| **SYG-KMLK-061** | TCKN, telefon ve e-posta günlük kayıtlarında mevcut maskeleme altyapısıyla maskelenir (`KR-059`). | İşlevsel olmayan | REQ-KMLK-041 | Test | — |
| **SYG-KMLK-062** | Doğrulama kodu, parola sıfırlama ve davet iletileri bildirim istisnasından muaftır. | İşlevsel | REQ-KMLK-042 | Test | PRM-BLD-03 |
| **SYG-KMLK-063** | Kimlik uç noktaları yalnızca HTTPS üzerinden hizmet verir; yenileme jetonu çerezi `Secure` işaretlidir ve düz HTTP'de gönderilmez. | Kısıt | REQ-KMLK-026 | Test | — |

### 4.9 Kullanıcı arayüzü

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-064** | Tüm ekran metinleri ve hata iletileri Türkçedir, teknik terim içermez ve kullanıcıya ne yapması gerektiğini söyler. | İşlevsel olmayan | REQ-KMLK-043 | İnceleme | — |
| **SYG-KMLK-065** | Giriş, üyelik ve parola ekranları 360 piksel genişlikten itibaren yatay kaydırma gerektirmeden kullanılabilir. | İşlevsel olmayan | REQ-KMLK-044 | Test | — |
| **SYG-KMLK-066** | Ekranlar yalnızca klavyeyle eksiksiz kullanılabilir; her form alanının erişilebilir bir etiketi vardır; odak sırası görsel sırayla aynıdır. | İşlevsel olmayan | REQ-KMLK-045 | Test | — |
| **SYG-KMLK-067** | Beklenmeyen hata ekranında kullanıcıya izleme kimliği gösterilir. | İşlevsel | REQ-KMLK-046 | Test | — |
| **SYG-KMLK-068** | Giriş, üyelik, doğrulama ve parola ekranları iki bölümlü düzendedir: bir bölümde form, diğerinde kurumsal görsel. 900 pikselin altında bölümler alt alta geçer ve form üstte kalır. Görsel, İK kararı gereği (S-13) geliştirme kapsamında Claude tarafından üretilir. | İşlevsel | REQ-KMLK-047 | Gösterim | — |
| **SYG-KMLK-069** | Kurumsal logo parametreden yüklenir; yüklenmemişse `assets/duzen_logo.png` kullanılır. Logonun alternatif metni tanımlıdır. | İşlevsel | REQ-KMLK-048 | Test | PRM-GRN-01 |
| **SYG-KMLK-070** | Üyelik ve giriş ekranlarında destek birimi olarak Bilgi İşlem ve parametredeki iletişim bilgisi gösterilir. | İşlevsel | REQ-KMLK-056 | Test | PRM-GRN-04 |

### 4.10 Programlama arayüzü (API)

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-071** | Kimlik işlemleri `/api/v1/identity/` altında sunulur, OpenAPI sözleşmesinde tanımlıdır ve hataları RFC 9457 Problem Details biçiminde döndürür (ADR-0010). | Arayüz | REQ-KMLK-043 | Test | — |
| **SYG-KMLK-072** | Senkronizasyonun elle tetiklenmesi ve son çalışma bilgisinin okunması için yetki gerektiren uç noktalar sunulur; yetkisiz çağrı `403` döndürür. | Arayüz | REQ-KMLK-003 | Test | — |

### 4.11 T3'ün ön koşulu olan en küçük altyapılar (§2.4)

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-073** | T3, İK için **en küçük bir hesap işlemleri ekranı** sunar: kişiyi sicil numarası, ad veya soyadla arama; hesap durumunu (yok / aktif / pasif / kilitli) görme; davet bağlantısı gönderme (SYG-KMLK-051); hesabı elle pasife alma ve yeniden aktifleştirme (SYG-KMLK-057). Ekran kişisel veri olarak yalnızca ad, soyad, sicil ve firma gösterir. Kapsamlı kullanıcı yönetimi T5 kapsamındadır. | İşlevsel | REQ-KMLK-011, REQ-KMLK-036 | Gösterim | PRM-HSP-01 |
| **SYG-KMLK-074** | T3, ADR-0007'deki eylem yetkisi altyapısını (kullanıcı–rol–izin, sabit izinler, `[HasPermission]`) kurar ve yalnızca T3'ün izinlerini tanımlar: `identity.account.view`, `identity.account.update`, `identity.invite.create`, `identity.sync.view`, `identity.sync.create`, `system.parameter.view`, `system.parameter.update`. İki rol hazır gelir: **Sistem Yöneticisi** ve **İK Kimlik İşlemleri**. İlk sistem yöneticisi kurulum yapılandırmasıyla atanır. Rol yönetim ekranı ve satır bazlı kapsam **T4 kapsamındadır**; o zamana kadar İK rolünün kapsamı tüm personeldir. | Kısıt | REQ-KMLK-011, REQ-KMLK-036 | Test | — |
| **SYG-KMLK-075** | T3'ün kullandığı parametreler koda gömülmez; veritabanındaki parametre deposunda, Y4 kataloğundaki kimlik, tür, geçerli aralık ve varsayılan değerle tutulur. Değişiklik **yeniden dağıtım gerektirmeden** en geç 1 dakika içinde etkili olur ve eski/yeni değerle denetim izine yazılır. Parola ve API anahtarı niteliğindeki parametreler şifreli saklanır. | Kısıt | REQ-KMLK-005, REQ-KMLK-012, REQ-KMLK-021, REQ-KMLK-028 | Test | PRM-KML-01, PRM-KML-05, PRM-ENT-02, PRM-ENT-06 |
| **SYG-KMLK-076** | T3, sistem yöneticisi için **yalnızca T3 parametrelerini** listeleyen ve düzenleyen en küçük bir parametre ekranı sunar. Değer, türüne ve aralığına göre doğrulanır; geçersiz değer kaydedilmez. Sır niteliğindeki parametreler **yalnızca yazılabilir**: değer ekranda ve API yanıtında hiçbir zaman gösterilmez (`KR-071`). Logo dosyası bu ekrandan yüklenir. Parametre ekranının tamamı Y4 kapsamındadır ve bu ekranı genişletir. | İşlevsel | REQ-KMLK-012, REQ-KMLK-028, REQ-KMLK-048, REQ-KMLK-056 | Test | PRM-GRN-01, PRM-GRN-04 |

### 4.12 Performans

| Kimlik | Gereksinim | Tür | Kaynak | Doğrulama | Parametre |
|---|---|---|---|---|---|
| **SYG-KMLK-077** | Giriş isteğinin yanıt süresi, girişleri mesai başına yayılan 50 kullanıcıda (60 saniyede 50 giriş) 95. yüzdelikte 1 saniyenin altındadır. 50 giriş isteği **aynı anda** geldiğinde 95. yüzdelik 5 saniyenin altında kalır. Süreler parola özetlemesi dahildir. | Performans | REQ-KMLK-022 | Analiz | — |
| **SYG-KMLK-078** | Tam bir LOGO senkronizasyonu (bugünkü hacim ≈1.540 kart) 60 saniyenin altında tamamlanır. Böylece istihdamı biten kişinin hesabı en geç periyot + 1 dakika içinde kapanır. | Performans | REQ-KMLK-034 | Analiz | PRM-ENT-07 |
| **SYG-KMLK-079** | Doğrulama kodu, e-posta ve SMS için istekten itibaren 95. yüzdelikte 60 saniye içinde alıcının sunucusuna / operatöre teslim edilir. Teslim edilemeyen ileti yeniden denenir ve sonucu kaydedilir. | Performans | REQ-KMLK-001, REQ-KMLK-016, REQ-KMLK-021 | Analiz | — |
| **SYG-KMLK-080** | Parametre açıkken kullanıcı, "Hesap güvenliği" ekranından kendi 2FA tercihini açar ve kapatır; tercih varsayılan olarak **kapalıdır**. Açmak için mevcut parola ve seçilen kanala gönderilen kodun doğrulanması gerekir; kod yalnızca o kişiye ve bu amaca aittir. Doğrulama kanalı olmayan kullanıcı 2FA'yı açamaz. Kapatmak için mevcut parola gerekir. Açma ve kapatma denetim izine ve güvenlik olaylarına yazılır. Parametre kapalıyken tercih saklanır ama uygulanmaz (#190). | İşlevsel | REQ-KMLK-025, REQ-KMLK-051, REQ-KMLK-052 | Test | PRM-KML-08 |
| **SYG-KMLK-081** | Doğrulama kodunu alamayan kişi için (e-posta ve telefon erişimi yok) **kurtarma:** `identity.account.update` iznine sahip İK kullanıcısı, "Hesap işlemleri" ekranından kişinin 2FA tercihini **gerekçe girerek** kapatır. Gerekçe hesapta saklanır; işlem denetim izine ve güvenlik olaylarına yazılır. Kişi bundan sonra parolasıyla girer, isterse 2FA'yı yeniden açar. Kullanıcı kendi hesabına bu işlemi uygulayamaz (#190). | İşlevsel | REQ-KMLK-037, REQ-KMLK-052 | Test | — |

---

## 5. Kritik performans ölçütleri (TEC.3 çıktısı c)

| Kimlik | Ölçüt | Hedef | Nasıl ölçülür | İlgili SYG |
|---|---|---|---|---|
| **KPÖ-KMLK-1** | Giriş yanıt süresi | p95 < 1 sn (60 sn'ye yayılan 50 giriş); p95 < 5 sn (aynı anda 50 giriş) | UAT'de yük testi | SYG-KMLK-077, 049 |
| **KPÖ-KMLK-2** | Üyelik eşleştirmesinde yanıt süresi farkı | Eşleşen/eşleşmeyen medyan farkı < 20 ms (1.000 istek) | Otomatik test | SYG-KMLK-015 |
| **KPÖ-KMLK-3** | Tam senkronizasyon süresi | < 60 sn (≈1.540 kart) | Çalışma kaydındaki süre | SYG-KMLK-078, 005 |
| **KPÖ-KMLK-4** | Doğrulama kodu ulaşma süresi | E-posta ve SMS için p95 < 60 sn | Gönderim kaydı / NetGSM raporu | SYG-KMLK-079 |
| **KPÖ-KMLK-5** | Parola özetleme süresi | 100–500 ms | Birim testi (ölçüm) | SYG-KMLK-049 |
| **KPÖ-KMLK-6** | Üyelik oranı (vizyon H2) | Devreye almadan 2 ay sonra aktif personelin ≥ %90'ı | Hesap / aktif kişi oranı | SYG-KMLK-013 |

**Neden bu altısı?** KPÖ-1 ve KPÖ-4 kullanıcının sistemi kullanıp kullanmayacağını
belirler: kod bir dakikada gelmiyorsa personel İK'yı arar ve self-servis amacı
(REQ-KMLK-001) boşa çıkar. KPÖ-2 bir güvenlik gereksiniminin (REQ-KMLK-004) **tek
ölçülebilir kanıtıdır**. KPÖ-5 hem güvenliğin hem giriş süresinin sınırıdır: çok hızlı
özetleme kaba kuvvete, çok yavaş özetleme hizmet aksamasına açıktır. KPÖ-6 T3'ün vizyon
hedefine (H2) katkısını ölçer ve **T3 kabulünden sonra** ölçülür.

---

## 6. Gereksinim analizi (TEC.3 çıktısı d, BP3)

Paydaş gereksinimleri tek tek ve birbirleriyle karşılaştırılarak analiz edildi. Aşağıdaki
bulgular, paydaş gereksinimlerinin **metninde açık kalan** veya **birbirleriyle gerilimli**
noktalardır. Her birinin nasıl ele alındığı ve hangi sistem gereksinimine yansıdığı
yazılıdır; kapanmamış olanlar ⏳ ile işaretlidir.

| No | Bulgu | Çözüm | Durum |
|---|---|---|---|
| AN-01 | **REQ-KMLK-004 ile REQ-KMLK-010 arasında gerilim.** 004 eşleşme olsa da olmasa da "aynı ekran" ister; 010 cep telefonu olmayan kişiye "yalnızca e-posta seçeneği" gösterilmesini ister. Telefonu olmayan kişi için eşleşme varsa ekran tek kanal, yoksa iki kanal gösterir; bu fark, üç bilginin (TCKN, doğum tarihi, e-posta) **doğru olduğunu** ele verir. 22.09.2026 ölçümüne göre telefonu olmayan aktif personel **10 kişidir.** | **REQ-KMLK-010 olduğu gibi uygulanır** (SYG-KMLK-016, 017). Gerekçe: farkı görebilmek için saldırganın üç bilgiyi de zaten doğru bilmesi gerekir; öğrendiği tek şey kişinin kurumda çalıştığıdır ve kod yine kişinin kendi kurumsal adresine gider. Buna karşılık, iki kanalı her zaman göstermek 10 kişiyi hiç gelmeyecek bir SMS'i beklemeye bırakır. **Kalan risk kabul edildi (`KR-078`, 25.09.2026).** Bilgisini yanlış yazan kişinin gelmeyecek kodu beklemesi, kod ekranındaki yönlendirme iletisiyle hafifletilir (SYG-KMLK-016). | ✅ Karar verildi (`KR-078`) |
| AN-02 | REQ-KMLK-004 "aynı yanıt süresi" ister, ama kod yalnızca eşleşmede gönderilir; eşzamanlı gönderim eşleşmeyi süre farkından ele verir. | Gönderim arka plan kuyruğunda yapılır; yanıt süresi sabit alt sınıra tamamlanır (SYG-KMLK-015). Ölçüt: KPÖ-2. | ✅ |
| AN-03 | REQ-KMLK-004 kodun gönderileceği hedefin gösterilip gösterilmeyeceğini söylemez. Maskeli hedef (ör. `532*****67`) bile eşleşmeyi ele verir. | Hedef hiçbir biçimde gösterilmez (SYG-KMLK-018). | ✅ |
| AN-04 | Zaten hesabı olan kişinin yeniden üye olmaya çalışması REQ-KMLK-014'te tanımlı değil. "Hesabınız var" demek, REQ-KMLK-004'ü ihlal eder. | Akış aynı ilerler; bilgi **doğrulamadan sonra** verilir ve sıfırlamaya yönlendirilir (SYG-KMLK-020). | ✅ |
| AN-05 | REQ-KMLK-024 "hesap kilitlenir" der; var olmayan hesap kilitlenmezse kilit iletisi hesabın varlığını ele verir (REQ-KMLK-023 ile gerilim). | Sayaç hesaba değil **girilen e-postaya** bağlanır (SYG-KMLK-033); var olmayan kullanıcı için de parola doğrulaması yapılır (SYG-KMLK-032). | ✅ |
| AN-06 | REQ-KMLK-018 "şifrelenmiş (hash)" der. 6 haneli kodun düz özeti 1.000.000 denemede kırılır; özet tek başına koruma sağlamaz. | Anahtarlı özet (HMAC-SHA256), anahtar sır yönetiminde (SYG-KMLK-025). | ✅ |
| AN-07 | REQ-KMLK-026'daki "8 saatlik yenileme jetonu" kayan mı, mutlak mı? REQ-KMLK-054 sinyali oturumu sonsuza kadar uzatabilir mi? | 8 saat **mutlak üst sınırdır**; etkinlik sinyali yalnızca hareketsizlik sayacını sıfırlar (SYG-KMLK-037, 039). ADR-0006 §8 ile tutarlı. | ✅ |
| AN-08 | REQ-KMLK-026 "hareketsizlik" tanımlamaz. Arka plan istekleri (bildirim yoklaması vb.) etkinlik sayılırsa oturum hiç düşmez. | Hareketsizlik **kullanıcı etkileşimine** göre ölçülür (SYG-KMLK-038). | ✅ |
| AN-09 | REQ-KMLK-003 "aktif istihdam" der; REQ-KMLK-014 birden fazla sicil kabul eder. Bir sicili aktif, diğeri kapalı olan kişi üye olabilir mi? | **En az bir** aktif istihdam yeterlidir (SYG-KMLK-013); hesap ancak **tümü** bittiğinde pasifleşir (SYG-KMLK-054). | ✅ |
| AN-10 | REQ-KMLK-002 üyelikte kurumsal e-postanın LOGO ile eşleşmesini ister. Bu durumda eşleşen her kişinin e-posta kanalı vardır; REQ-KMLK-007 (e-postası olmayan kişiye yalnızca SMS) üyelikte **ortaya çıkmaz.** | Çelişki değildir; 007, iki adımlı doğrulama ve parola sıfırlama akışlarında geçerliliğini korur. E-postası olmayan kişi zaten üye olamaz; çözüm İK'nın LOGO'da adres tanımlamasıdır (`KR-073`, toplantı kaydı §11.1). | ✅ |
| AN-11 | Paylaşılan e-posta (REQ-KMLK-009) giriş kimliği de olursa (REQ-KMLK-022) iki kişi aynı kimlikle giriş yapmaya çalışır. | Paylaşılan adres ne giriş kimliği ne doğrulama hedefi olur; kişiler uyarı listesine alınır (SYG-KMLK-008). 22.09.2026 ölçümünde paylaşılan adres **0**. | ✅ |
| AN-12 | REQ-KMLK-011 ve 036 "İK" der; rol yönetimi T4'te. REQ-KMLK-012 "sistem yöneticisi alan adı ekleyebilir" der; parametre ekranı Y4'te. | En küçük yetki altyapısı, parametre deposu ve ekranları T3'te kurulur (SYG-KMLK-073–076, §2.4). | ✅ |
| AN-13 | REQ-KMLK-029 listenin kaynağını ve büyüklüğünü söylemez. | En az 100.000 kayıtlı, açık lisanslı liste + kuruma özgü sözcükler (SYG-KMLK-045). Lisans `KR-025`'e göre denetlenir. | ✅ |
| AN-14 | REQ-KMLK-044 "telefon ve tablet" ölçülebilir değil. | 360 piksel genişlikten itibaren yatay kaydırmasız (SYG-KMLK-065). | ✅ |
| AN-15 | REQ-KMLK-034 "en geç bir sonraki senkronizasyonda" der; süre periyoda bağlıdır. | Varsayılan periyotla (`PRM-ENT-07`, 15 dk) en geç 15 dakika + çalışma süresi (SYG-KMLK-004, 054). | ✅ |
| AN-17 | **Canlı doğrulamada bulundu (26.09.2026):** Bir adres 3 kişiye tanımlıydı: 2'si ayrılmış, 1'i aktif. REQ-KMLK-009 ayrılmış personeli ayırt etmediği için aktif kişi e-postasıyla giriş yapamayacaktı. | Paylaşım tespitinde yalnızca aktif istihdamı olan kişiler sayılır: ayrılmış kişi üye olamaz ve giriş yapamaz, paylaşım riski doğurmaz (SYG-KMLK-008, `KR-079`, #77). İK ayrılanların kartındaki adresi sildi. | ✅ |
| AN-18 | Kişinin kartları arasındaki ad/soyad farkı her zaman hata değildir: evlilik sonrası eşin soyadı veya iki soyad kullanılabilir (İK, 26.09.2026). | Aktif sicildeki ad esas alınır (esas kart kuralı). Uyarı yalnızca iki **aktif** kart arasında fark varsa üretilir (`KR-079`, #77). | ✅ |
| AN-19 | REQ-KMLK-035 "yeniden işe girişte hesap aktifleşir", REQ-KMLK-036 "İK hesabı elle pasife alabilir" der. Senkronizasyon 15 dakikada bir çalıştığından, elle pasif hesabı her çalışmada aktifleştirseydi elle pasife alma anlamsız kalırdı. | Yalnızca istihdam bitimiyle pasifleşen hesap otomatik aktifleşir (SYG-KMLK-056). İstihdamı süren kişinin elle pasif hesabı ancak elle aktifleşir (SYG-KMLK-057). Elle pasif hesabın sahibi ayrılırsa neden "istihdam bitti"ye döner ve yeniden işe girişte hesap aktifleşir (`KR-080`, #83). | ✅ |
| AN-20 | ADR-0012 §7 geliştirme ve UAT'de "içeriği log'a yazan" göndericiler öngörür; SYG-KMLK-026 kodun hiçbir günlüğe yazılmamasını ister. UAT gerçek LOGO verisiyle çalıştığından gerçek personele de kod gitmemelidir. | Gönderim kipleri: `LogOnly` (varsayılan, içerik yazılmaz), `AllowList` (UAT ve geliştirme), `Send` (üretim). ADR-0012 §7 değiştirildi (`KR-083`, #85). | ✅ |
| AN-21 | SYG-KMLK-071 ilk yazımda `/api/v1/kimlik/` diyordu. ADR-0010 §2 modül ve kaynak adlarını İngilizce kebab-case ister (`/api/v1/leave/leave-requests`); `KR-058` kod tanımlayıcılarını İngilizce tutar. | Yol `/api/v1/identity/` olarak düzeltildi. Paydaş gereksinimi (REQ-KMLK-043: ekran ve iletiler Türkçe) etkilenmez; yol kullanıcıya görünmez (#87). | ✅ |
| AN-22 | SYG-KMLK-040 "kullanılmış jeton tekrar sunulursa tüm oturumlar kapanır" der; SYG-KMLK-041 ise yeni girişte önceki oturumu kapatır. Kapatılan eski sekme elindeki jetonla tekrar denerse, SYG-040'ın harfi yeni cihazdaki meşru oturumu da kapatırdı. | Yeniden kullanım yalnızca jetonun oturumu **hâlâ açıkken** çalınma sayılır ve tüm oturumlar kapanır. Oturum zaten kapalıysa istek kapanma nedeniyle reddedilir; diğer oturumlara dokunulmaz (`KR-086`, #92). | ✅ |
| AN-23 | SYG-KMLK-038 hareketsizliği **kullanıcı etkileşimine** göre ölçer; sunucu etkileşimi yalnızca etkinlik sinyali ucundan (SYG-KMLK-039) öğrenebilir. SYG-KMLK-039'un son cümlesi ("istemci sinyali yalnızca medya oynarken ve sekme görünürken gönderir") harfiyen uygulansaydı, etkileşimle çalışan kullanıcının oturumu 30 dakikada kapanırdı. | Cümle, **etkileşim olmadan** gönderilen sinyali sınırlar. İstemci, görünür sekmede kullanıcı etkileşimi (tıklama, tuş, kaydırma, dokunma) olduğunda ve görünür sekmede medya oynarken sinyal gönderir; dakikada en fazla 1. Görünmeyen sekme, fare hareketi ve arka plan istekleri sinyal göndermez (`KR-087`, #95). İK teyidi kabulde alınır: T3 kabul planı §6.1, T-01 (#135). | ✅ |
| AN-24 | SYG-KMLK-046 değişimin **ne sıklıkta** isteneceğini söylemez; Y4 taslak kataloğunda da süre parametresi yoktur. SYG-KMLK-050 "ilk giriş"in kapsamını söylemez: S-02 kararı "üyelikte belirlenen parola yeterli" gerekçesiyle parametreyi varsayılan kapalı tutar; açıldığında kimlerin değiştireceği belirsizdir. | (1) Süre yeni parametredir: `PRM-KML-21`, varsayılan 90 gün (30–365). (2) Parametre açıkken hiç giriş yapmamış **her** hesap değiştirir; parolasını üyelikte kendisi belirleyenler de dahildir. Kural açılmadan önce giriş yapmış hesaplar etkilenmez. (3) İkisi de girişte değerlendirilir ve oturumda tutulur. Değişim yapılana kadar yalnızca parola değiştirme ve oturum uçları kullanılabilir; bu kural sunucuda uygulanır (Doğuş Uçanok, 01.10.2026; `KR-092`, #113). İK teyidi kabulde alınır: T3 kabul planı §6.1, T-02 (#135). | ✅ |
| AN-25 | SYG-KMLK-077 "50 eş zamanlı kullanıcı" der. UAT ölçümünde (02.10.2026) iki ayrı sorun çıktı. (1) Giriş ucuna, üyelik için tasarlanmış 1 saniyelik en kısa yanıt süresi uygulanmıştı (PR #94); bu durumda hedef hiçbir donanımda karşılanamaz. (2) "Eş zamanlı" sözcüğü 50 isteğin **aynı anda** gelmesi olarak okunursa, 2 çekirdekli sunucuda SYG-KMLK-049'un parola özeti alt sınırı (100 ms) korunarak hedef karşılanamaz: 50 × ≈180 ms / 2 çekirdek ≈ 4,5 sn. | (1) Alt sınır giriş ucundan kaldırıldı. Var olmayan kullanıcı için de özet hesaplandığından (SYG-KMLK-032) süreler zaten ayırt edilemez. (2) "Eş zamanlı kullanıcı", girişleri mesai başına yayılan 50 kullanıcı (60 saniyede 50 giriş) olarak tanımlandı. 50 isteğin aynı anda geldiği durum için ayrıca p95 < 5 sn üst sınırı kondu (Doğuş Uçanok, 03.10.2026; `KR-095`, #120). | ✅ |
| AN-16 | Denetim izi ve erişim kaydı saklama süreleri (`PRM-KVK-01`, `02`) karar bekliyor. | T3'ü engellemez: kayıtlar üretilir, silme işi karar verildiğinde eklenir. | ⏳ KVKK kararı bekliyor |

**Paydaşa geri bildirim (BP3):** AN-01, onaylı bir kabul kriterinin (REQ-KMLK-004)
küçük bir kesim için tam karşılanamayacağı anlamına gelir. Bilgi İşlem 25.09.2026'da
onaylı metni koruyup kalan riski kabul etti (`KR-078`). Bu durum **T3 kabulünde İK'ya
ayrıca gösterilecektir**: kabul kriterini tam karşılamayan bir kesim, kabulde
söylenmeden geçilmemelidir. Diğer bulgular paydaş gereksinimlerinin anlamını
değiştirmez, yalnızca belirsiz kalan noktayı tek anlama indirir.

---

## 7. Destekleyici sistemler (TEC.3 çıktısı e)

| Sistem | Ne için | Durum |
|---|---|---|
| LOGO salt-okunur veritabanı hesabı | Senkronizasyon | ✅ Mevcut; yazma reddi CI'da her çalışmada sınanıyor (SYG-KMLK-003, PR #76) |
| UAT sunucusu ve TLS | Kabul ortamı | ✅ Mevcut (`KR-067`); sertifika 16.12.2026'ya kadar geçerli |
| PostgreSQL, CI kapıları | Veri, doğrulama | ✅ Mevcut (A1) |
| NetGSM hesabı | SMS | ✅ UAT'ye tanımlı; gönderim izin listesi kipinde (`KR-083`) |
| Kurum SMTP hesabı | E-posta | ✅ UAT'ye tanımlı; sunucu sertifikası 27.03.2027'ye kadar geçerli (R-20) |
| Yaygın parola listesi | SYG-KMLK-045 | ✅ SecLists (MIT lisansı), ≈144 bin kayıt, uygulamaya gömülü (PR #88; kaynak: `Infrastructure/Identity/Passwords/README.md`) |
| Kurumsal görsel | SYG-KMLK-068 | ✅ S-13 kararı gereği kodla üretildi (PR #91) |

---

## 8. İzlenebilirlik (TEC.3 çıktısı f)

Aşağıdaki tablo §4'ün **tersidir**: paydaş gereksiniminden sistem gereksinimine.
`.github/scripts/requirement-traceability-check.mjs` her PR'da şunları denetler;
biri bozulursa PR birleştirilemez:

- Her paydaş gereksinimi en az bir sistem gereksinimine bağlı (bu çıktının kendisi).
- §4'teki her kaynak kimliği `PG-KMLK`'de var.
- Sistem gereksinimi kimlikleri tekil ve sıralı; metin içindeki atıflar boşa düşmüyor.
- Bu tablo ve `izlenebilirlik-matrisi.md` §5'in "Sistem gereksinimi" sütunu §4 ile
  **aynı** eşlemeyi gösteriyor.

Parametre kimlikleri, Y4 taslak kataloğuyla (41 parametre) yazım sırasında
karşılaştırıldı; katalog Y4 toplantısıyla depoya girdiğinde bu denetime eklenecek.

| Paydaş gereksinimi | Karşılayan sistem gereksinimleri |
|---|---|
| `REQ-KMLK-001` | SYG-KMLK-013, 079 |
| `REQ-KMLK-002` | SYG-KMLK-006, 013, 014 |
| `REQ-KMLK-003` | SYG-KMLK-001, 002, 003, 004, 006, 011, 012, 013, 072 |
| `REQ-KMLK-004` | SYG-KMLK-015, 016, 018, 020 |
| `REQ-KMLK-005` | SYG-KMLK-016, 017, 030, 075 |
| `REQ-KMLK-006` | SYG-KMLK-019 |
| `REQ-KMLK-007` | SYG-KMLK-017 |
| `REQ-KMLK-008` | SYG-KMLK-009, 017 |
| `REQ-KMLK-009` | SYG-KMLK-008, 017 |
| `REQ-KMLK-010` | SYG-KMLK-009, 017 |
| `REQ-KMLK-011` | SYG-KMLK-030, 051, 052, 053, 073, 074 |
| `REQ-KMLK-012` | SYG-KMLK-017, 075, 076 |
| `REQ-KMLK-013` | SYG-KMLK-007 |
| `REQ-KMLK-014` | SYG-KMLK-001, 020, 021 |
| `REQ-KMLK-015` | SYG-KMLK-022 |
| `REQ-KMLK-016` | SYG-KMLK-023, 079 |
| `REQ-KMLK-017` | SYG-KMLK-024 |
| `REQ-KMLK-018` | SYG-KMLK-025, 026 |
| `REQ-KMLK-019` | SYG-KMLK-027 |
| `REQ-KMLK-020` | SYG-KMLK-028 |
| `REQ-KMLK-021` | SYG-KMLK-029, 075, 079 |
| `REQ-KMLK-022` | SYG-KMLK-008, 031, 077 |
| `REQ-KMLK-023` | SYG-KMLK-032 |
| `REQ-KMLK-024` | SYG-KMLK-033 |
| `REQ-KMLK-025` | SYG-KMLK-034, SYG-KMLK-080 |
| `REQ-KMLK-026` | SYG-KMLK-037, 038, 040, 063 |
| `REQ-KMLK-027` | SYG-KMLK-043 |
| `REQ-KMLK-049` | SYG-KMLK-041 |
| `REQ-KMLK-050` | SYG-KMLK-042 |
| `REQ-KMLK-051` | SYG-KMLK-034, SYG-KMLK-080 |
| `REQ-KMLK-052` | SYG-KMLK-035, SYG-KMLK-080, SYG-KMLK-081 |
| `REQ-KMLK-053` | SYG-KMLK-036 |
| `REQ-KMLK-054` | SYG-KMLK-038, 039 |
| `REQ-KMLK-028` | SYG-KMLK-044, 075, 076 |
| `REQ-KMLK-029` | SYG-KMLK-045 |
| `REQ-KMLK-030` | SYG-KMLK-046 |
| `REQ-KMLK-031` | SYG-KMLK-047 |
| `REQ-KMLK-032` | SYG-KMLK-048 |
| `REQ-KMLK-033` | SYG-KMLK-049 |
| `REQ-KMLK-055` | SYG-KMLK-050 |
| `REQ-KMLK-034` | SYG-KMLK-004, 005, 054, 055, 078 |
| `REQ-KMLK-035` | SYG-KMLK-056 |
| `REQ-KMLK-036` | SYG-KMLK-057, 073, 074 |
| `REQ-KMLK-037` | SYG-KMLK-053, 058, 081 |
| `REQ-KMLK-038` | SYG-KMLK-059 |
| `REQ-KMLK-039` | SYG-KMLK-059 |
| `REQ-KMLK-040` | SYG-KMLK-005, 010, 060 |
| `REQ-KMLK-041` | SYG-KMLK-026, 061 |
| `REQ-KMLK-042` | SYG-KMLK-062 |
| `REQ-KMLK-043` | SYG-KMLK-064, 071 |
| `REQ-KMLK-044` | SYG-KMLK-065 |
| `REQ-KMLK-045` | SYG-KMLK-066 |
| `REQ-KMLK-046` | SYG-KMLK-067 |
| `REQ-KMLK-056` | SYG-KMLK-070, 076 |
| `REQ-KMLK-047` | SYG-KMLK-068 |
| `REQ-KMLK-048` | SYG-KMLK-069, 076 |

---

## 9. Değişiklik

Bu belge Pull Request ile değişir. Bir sistem gereksinimi değişikliği bir paydaş
gereksiniminin **anlamını** değiştiriyorsa önce `tur:degisiklik-talebi` issue'su açılır
ve İK'nın onayı alınır (TEC.2 YAKLASIM §4). Numara yeniden kullanılmaz; iptal edilen
madde `İptal` notuyla kalır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-24 | 0.1 | İlk oluşturma — PG-KMLK'deki 56 onaylı gereksinimden türetildi | Bilgi İşlem |
| 2026-09-25 | 0.2 | Önek `SG-` → `SYG-` (PR #65 incelemesi); AN-01 karara bağlandı (`KR-078`); SYG-KMLK-016'ya yönlendirme iletisi eklendi | Bilgi İşlem |
| 2026-09-26 | 0.3 | SYG-KMLK-008: ortak adres tespitinde yalnızca aktif istihdamlı kişiler; AN-17, AN-18 eklendi (`KR-079`, #77) | Bilgi İşlem |
| 2026-09-26 | 0.4 | AN-19 eklendi: elle pasife alma ile yeniden işe girişte aktifleşmenin birlikte uygulanması (`KR-080`, #83) | Bilgi İşlem |
| 2026-09-27 | 0.5 | AN-20 eklendi: ADR-0012 §7 ile SYG-KMLK-026 çelişkisi, gönderim kipleri (`KR-083`, #85) | Bilgi İşlem |
| 2026-09-27 | 0.6 | SYG-KMLK-071: yol `/api/v1/identity/`; AN-21 eklendi (#87) | Bilgi İşlem |
| 2026-09-28 | 0.7 | SYG-KMLK-068: görseli üreten taraf S-13 kararıyla uyumlu hâle getirildi (#90) | Bilgi İşlem |
| 2026-09-28 | 0.8 | AN-22 eklendi: kapanmış oturumun jeton tekrarı (`KR-086`, #92) | Bilgi İşlem |
| 2026-09-28 | 0.9 | AN-23 eklendi: etkinlik sinyalinin kapsamı (`KR-087`, #95); §7 kurumsal görsel durumu güncellendi | Bilgi İşlem |
| 2026-10-01 | 1.0 | SYG-KMLK-046 ve 050 netleştirildi; `PRM-KML-21` eklendi; AN-24 (`KR-092`, #113) | Bilgi İşlem |
| 2026-10-03 | 1.1 | SYG-KMLK-077 ve KPÖ-KMLK-1 netleştirildi; AN-25 (`KR-095`, #120) | Bilgi İşlem |
| 2026-10-04 | 1.2 | AN-23 ve AN-24: İK teyidinin kabulde alınacağı yazıldı (#135) | Bilgi İşlem |
| 2026-10-04 | 1.3 | §2.2 API yolu `/api/v1/identity/`; §7 destekleyici sistemlerin durumu güncellendi (#133) | Bilgi İşlem |
| 2026-10-06 | 1.4 | §7: SMTP sertifikasının yeni bitişi (#179) | Bilgi İşlem |
| 2026-10-10 | 1.5 | Değişiklik talebi #190: SYG-KMLK-034, 035, 036 güncellendi; SYG-KMLK-080 (kullanıcının 2FA tercihi) ve SYG-KMLK-081 (İK'nın 2FA'yı kurtarma için kapatması) eklendi | Bilgi İşlem |
