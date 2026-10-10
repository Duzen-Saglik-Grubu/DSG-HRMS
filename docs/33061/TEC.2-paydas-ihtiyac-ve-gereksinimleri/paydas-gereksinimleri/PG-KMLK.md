# Paydaş Gereksinimleri — T3 Kimlik Yönetimi

**Belge kimliği:** TEC.2-PG-KMLK
**Süreç:** TEC.2 — Paydaş İhtiyaç ve Gereksinimlerinin Tanımlanması
**Son güncelleme:** 2026-09-28
**Kaynak toplantı:** `kayitlar/2026-09-23-kimlik-gereksinim-toplantisi.md`
**Onay durumu:** **Onaylandı — 2026-09-23** (İK gereksinim toplantısı)

> **Bu belge, T3 geliştirmesinin başlangıç koşuludur** (`KR-068`). Burada yazmayan bir
> davranış geliştirilmez; burada yazan bir davranış değiştirilecekse
> `tur:degisiklik-talebi` issue'su açılır (`R-04`).

---

## 1. Kapsam

Personelin sisteme kendi başına üye olması, kurumsal e-posta adresi ve parolasıyla
giriş yapması, parolasını yönetmesi ve oturumunun güvenli biçimde yönetilmesi.

**Kapsam dışı:** rol ve yetki tanımı (T4), kullanıcı listesi ve hesap yönetimi (T5),
Active Directory ile tek oturum açma, mobil uygulama, parolasız giriş. Ayrıntı:
toplantı kaydı §4.

## 2. Özet

| | |
|---|---:|
| İK'ya sunulan gereksinim | 53 |
| Olduğu gibi onaylanan | 52 |
| Değişiklikle onaylanan | 1 (REQ-KMLK-026) |
| Açık soru cevabıyla netleşen | 2 (REQ-KMLK-011, REQ-KMLK-022) |
| Toplantı sonucu eklenen | 3 (REQ-KMLK-054, 055, 056) |
| **Toplam onaylı gereksinim** | **56** |

## 3. Kimlik numaraları hakkında

Gereksinimler İK'ya `REQ-KMLK-nnn` kimlikleriyle sunuldu ve **bu kimliklerle
onaylandı.** `TEC.2/YAKLASIM.md` §3.1 paydaş gereksinimleri için `PG-<MODÜL>-nn`
biçimini öngörür; ancak liste yaklaşım belgesinden önce hazırlanmıştı.

Kimlikler **bilinçli olarak değiştirilmedi**: imzalı toplantı kaydıyla depo arasındaki
bağ, kimliğin sabit kalmasına dayanır. Numara yeniden kullanılmaz ve değiştirilmez
(`YAKLASIM.md` §3.1).

> **Not:** Bu listenin bir bölümü paydaş düzeyinde değil **sistem düzeyinde**
> gereksinimdir (ör. kodun hash'li saklanması, jeton ömürleri). TEC.3 sistem
> gereksinimleri yazılırken bu ayrım yapılacak ve her sistem gereksinimi buradaki
> kimliğe bağlanacaktır.

---

## 4. Gereksinimler

### Üyelik

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **REQ-KMLK-001** | Personel, giriş ekranındaki "Üye Ol" bağlantısıyla kendi hesabını kendisi oluşturabilir. | İK iş yükünü artırmamak; 584 aktif personel için elle hesap açmak sürdürülebilir değil. (ADR-0006 §1) | Zorunlu | Personel İK ile temas etmeden hesap açabiliyor. | Onaylandı |
| **REQ-KMLK-002** | Üyelik doğrulaması TCKN + doğum tarihi + kurumsal e-posta bilgileriyle yapılır. | Kimliğin LOGO kaydıyla eşleştirilmesi gerekir. (ADR-0006 §1) | Zorunlu | Üç bilgi de doğruysa akış devam eder; biri yanlışsa kod gönderilmez. | Onaylandı |
| **REQ-KMLK-003** | Eşleşme LOGO anlık görüntüsü üzerinden yapılır; yalnızca AKTİF istihdamı olan personel üye olabilir. | Ayrılmış personelin hesap açması engellenmelidir. (KR-013) | Zorunlu | Çıkış tarihi dolu personel üye olamıyor. | Onaylandı |
| **REQ-KMLK-004** | Bilgiler eşleşse de eşleşmese de kullanıcıya AYNI ekran ve AYNI mesaj gösterilir; eşleşme yoksa hiçbir kod gönderilmez. | Aksi hâlde "bu TCKN bu kurumda çalışıyor mu" bilgisi dışarı sızar (kimlik sıralama saldırısı). (KR-016) | Zorunlu | Var olmayan bir TCKN ile var olan bir TCKN arasında ekran, mesaj ve yanıt süresi farkı yok. | Onaylandı |
| **REQ-KMLK-005** | Doğrulama kanalı kullanıcıya seçtirilir: e-posta veya SMS. Hangi kanalların kullanılacağı Sistem Yönetimi parametresidir (PRM-KML-02). | Kişinin eline hangi kanalın ulaştığını en iyi kendisi bilir. (ADR-0006 §1) | Zorunlu | Kanal seçim ekranı gösteriliyor; yalnızca kullanılabilir kanallar sunuluyor. | Onaylandı |
| **REQ-KMLK-006** | SMS seçilip kod ulaşmazsa kullanıcı aynı ekrandan e-posta kanalına geçebilir. | SMS operatör kaynaklı gecikebilir/engellenebilir (R-14). Kullanıcı akışın ortasında kilitlenmemeli. | Zorunlu | Kanal değiştirildiğinde önceki kod geçersiz olur, yeni kod gönderilir. | Onaylandı |
| **REQ-KMLK-007** | Kurumsal e-posta adresi bulunmayan personel için e-posta kanalı sunulmaz. | Gönderilecek adres yok. (ADR-0006 §4) | Zorunlu | Adresi olmayan kişiye yalnızca SMS seçeneği çıkıyor. | Onaylandı |
| **REQ-KMLK-008** | Kurumsal alan adı dışındaki e-posta adreslerine (gmail, hotmail vb.) doğrulama kodu gönderilmez; kabul edilen alan adları Sistem Yönetimi parametresidir (PRM-KML-01). | Kod, kurum denetiminde olmayan bir posta kutusuna gider; personel ayrıldıktan sonra da o adrese erişimi sürer. (KR-019) | Zorunlu | Kişisel adresi olan kişiye e-posta kanalı sunulmuyor. | Onaylandı |
| **REQ-KMLK-009** | Aynı e-posta adresi birden fazla kişiye tanımlıysa o adrese doğrulama kodu gönderilmez. | Ortak posta kutusuna giden kod ile bir kişi başkasının adına üye olup onun özlük verisine erişebilir. (KR-018) LOGO verisinde paylaşılan adres KALMADI (22.09.2026 ölçümü: 0; önceki ölçümlerde 1 adres 4 kişiye tanımlıydı). Kural yine de kalıcıdır: yarın açılacak bir kayıt aynı durumu üretebilir. | Zorunlu | Paylaşılan adres kullanan kişiye e-posta kanalı sunulmuyor. | Onaylandı |
| **REQ-KMLK-010** | Cep telefonu tanımlı olmayan personel için SMS kanalı sunulmaz. | Gönderilecek numara yok. LOGO verisinde 10 aktif personelin telefonu yok (22.09.2026 ölçümü; önceki: 12, 13). | Zorunlu | Numarası olmayan kişiye yalnızca e-posta seçeneği çıkıyor. | Onaylandı |
| **REQ-KMLK-011** | İK, personele sistem üzerinden **tek kullanımlık parola oluşturma bağlantısı** gönderebilir (PRM-HSP-01). Bağlantı yalnızca personelin **LOGO'da tanımlı kurumsal e-posta adresine** otomatik olarak gider; kurumsal e-posta tanımlı değilse bağlantı gönderilemez. Bağlantının geçerlilik süresi parametredir, varsayılan **3 saat** (PRM-HSP-04). Personel parolasını bağlantı üzerinden kendisi oluşturur. Gerekçe zorunludur ve işlem denetim izine yazılır. | İK kararı (23.09.2026, S-04; S-05 24.09.2026'da revize edildi). İK hiçbir aşamada ne adres girer ne parolayı bilir. Adresin yalnızca LOGO'dan okunması, bağlantının kurum dışına veya başka bir personele gitmesini yapısal olarak imkânsız kılar (KR-018, KR-019). Toplantı kaydı §11.2. | Zorunlu | Kurumsal e-postası olmayan personel için bağlantı gönder seçeneği çalışmıyor; bağlantı yalnızca bir kez kullanılabiliyor; 3 saat sonra geçersiz; İK gerekçe girmeden işlem yapamıyor; işlem denetim izinde görünüyor. | Onaylandı (açık soru cevabıyla netleşti) |
| **REQ-KMLK-012** | Kabul edilen kurumsal alan adları koda gömülmez; Sistem Yönetimi parametresinden değiştirilebilir (parametre: PRM-KML-01). | Yeni bir şirket/alan adı eklendiğinde yazılım değişikliği gerekmemeli. (KR-019) | Zorunlu | Sistem yöneticisi alan adı ekleyip çıkarabiliyor. | Onaylandı |
| **REQ-KMLK-013** | TCKN’si bulunmayan LOGO kartı için sistemde Kişi kaydı oluşturulmaz; kayıt uyarı listesine düşer. | TCKN kimlik eşleştirmenin anahtarıdır. (KR-043) | Zorunlu | TCKN’siz kart uyarı listesinde görünüyor, hesap açılamıyor. | Onaylandı |
| **REQ-KMLK-014** | Bir kişinin birden fazla sicil numarası olsa da TEK hesabı olur. | LOGO’da 140 kişinin birden fazla sicili var; 12’sinde aynı anda aktif. (KR-014) | Zorunlu | Aynı TCKN ile ikinci bir hesap açılamıyor; kişi tüm sicillerini tek hesapta görüyor. | Onaylandı |

### Doğrulama Kodu

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **REQ-KMLK-015** | Doğrulama kodu 6 hanedir (parametre: PRM-KML-09) ve kriptografik güvenli üreteçle üretilir. | Tahmin edilebilir kod, hesabın ele geçirilmesini mümkün kılar. (ADR-0006 §3) | Zorunlu | Kodlar tahmin edilebilir bir örüntü içermiyor. | Onaylandı |
| **REQ-KMLK-016** | Kod 5 dakika geçerlidir (parametre: PRM-KML-10). | Uzun ömürlü kod, ele geçirilme penceresini büyütür. (ADR-0006 §3) | Zorunlu | Süre dolduktan sonra kod kabul edilmiyor; kullanıcıya süre bitimi anlaşılır biçimde bildiriliyor. | Onaylandı |
| **REQ-KMLK-017** | Kod en fazla 3 kez yanlış girilebilir; aşılırsa kod iptal edilir ve yeniden istenmesi gerekir. Deneme sayısı Sistem Yönetimi parametresidir (PRM-KML-16). | Kaba kuvvet denemesini engeller. (ADR-0006 §3) | Zorunlu | 4. yanlış denemede kod iptal oluyor. | Onaylandı |
| **REQ-KMLK-018** | Kod veritabanında şifrelenmiş (hash) saklanır; düz metin tutulmaz ve hiçbir kayda yazılmaz. | Veritabanına veya günlüğe erişen biri kodu kullanamamalıdır. (KR-059) | Zorunlu | Günlük ve denetim kayıtlarında kod bulunmuyor; testle doğrulanıyor. | Onaylandı |
| **REQ-KMLK-019** | Doğrulanan kod anında geçersiz kılınır (tek kullanımlık). | Aynı kodun ikinci kez kullanılması engellenmelidir. (ADR-0006 §3) | Zorunlu | Kullanılmış kod tekrar kabul edilmiyor. | Onaylandı |
| **REQ-KMLK-020** | Kod ekranında kalan süre görünür ve "kodu tekrar gönder" seçeneği bulunur. | Kullanıcı ne kadar süresi kaldığını bilmeli; kod gelmediğinde çaresiz kalmamalı. (ADR-0015 §6) | Zorunlu | Geri sayım görünüyor; tekrar gönderme hız sınırına tabi. | Onaylandı |
| **REQ-KMLK-021** | SMS gönderimleri NetGSM standart servisi üzerinden, DUZEN başlığıyla yapılır; hesap bilgileri ve gönderici başlığı Sistem Yönetimi parametreleridir (PRM-ENT-01, PRM-ENT-02, PRM-ENT-03). | Kurumun diğer uygulamaları da bu servisi kullanıyor; SMS 1 dakika içinde ulaşıyor. (KR-042) | Zorunlu | SMS gönderiliyor ve gönderici adı DUZEN görünüyor. | Onaylandı |

### Giriş

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **REQ-KMLK-022** | Kullanıcı **kurumsal e-posta adresi** ve parolasıyla giriş yapar. | İK kararı (23.09.2026, S-01). Sicil numarası birden fazla olabildiği için giriş kimliği olarak uygun değildi. (ADR-0006 §8) Sonuç: kurumsal e-posta adresi kişiye TEKİL olmalıdır — paylaşılan adres yasağı (KR-018) artık bir giriş kısıtıdır da. Telefonu olup e-postası olmayan 2 kişiye İK kurumsal e-posta tanımlayacak (24.09.2026, toplantı kaydı §11.1). | Zorunlu | Doğru e-posta ve parolayla giriş yapılabiliyor; sicil numarası veya TCKN ile giriş yapılamıyor. | Onaylandı (açık soru cevabıyla netleşti) |
| **REQ-KMLK-023** | Hatalı girişte "kullanıcı adı veya parola hatalı" denir; hangisinin yanlış olduğu söylenmez. | Hangi kullanıcının var olduğunu sızdırmamak için. (KR-016) | Zorunlu | Var olmayan kullanıcı ile yanlış parola aynı mesajı alıyor. | Onaylandı |
| **REQ-KMLK-024** | 5 başarısız giriş denemesinden sonra hesap 15 dakika kilitlenir (parametreler: PRM-KML-03, PRM-KML-04). | Kaba kuvvet denemesini engeller. (ADR-0006 §5) | Zorunlu | Kilitlenme sonrası doğru parola da 15 dakika kabul edilmiyor. | Onaylandı |
| **REQ-KMLK-025** | Giriş sonrası ikinci doğrulama adımı (2FA) GELİŞTİRİLİR; varsayılan olarak KAPALIDIR ve Sistem Yönetimi parametresiyle açılır. | İK kararı (21.09.2026). Önceki karar (KR-017) 2FA'yı kapsam dışı bırakıyordu; sistem yalnızca kurum içinden erişilebildiği için aciliyeti düşüktü. Şimdi geliştirilip kapalı tutulması, ileride internete açılma veya denetim talebi doğduğunda kod değişikliği beklenmemesini sağlar. Parametre: PRM-KML-08. | Zorunlu | Parametre kapalıyken giriş tek adımdır; açıldığında ikinci adım kod değişikliği olmadan devreye giriyor. | Onaylandı |
| **REQ-KMLK-026** | Oturum 15 dakikalık erişim jetonu ve 8 saatlik yenileme jetonu ile yönetilir; **30 dakika** hareketsizlikte oturum düşer (parametreler: PRM-KML-11, PRM-KML-12, PRM-KML-13). Meşru uzun süreli etkinliklerde (ör. eğitim videosu izlenmesi) oturum düşmez — bkz. REQ-KMLK-054. | İK kararı (23.09.2026): hareketsizlik varsayılanı 60 değil **30 dakika**. İK notu: "Eğitim modülünde kullanıcı video süresince 30 dk'dan fazla hareketsiz kalabilir; bu ve benzer durumlarda oturum düşmemelidir." (ADR-0006 §8) | Zorunlu | 30 dakika işlem yapmayan kullanıcının oturumu kapanıyor; aynı sürede video izleyen kullanıcının oturumu kapanmıyor. | Değişiklikle onaylandı |
| **REQ-KMLK-027** | Çıkış yapıldığında oturum sunucu tarafında da sonlandırılır. | Yalnızca tarayıcıdan silmek yeterli değildir. (ADR-0006 §8) | Zorunlu | Çıkış sonrası eski jeton kabul edilmiyor. | Onaylandı |
| **REQ-KMLK-049** | Bir kullanıcı yeni bir tarayıcıdan giriş yaptığında önceki oturumu sonlandırılır; önceki tarayıcı sayfayı yenilediğinde otomatik olarak giriş ekranına döner (parametre: PRM-KML-14). | İK talebi (15.09.2026). Aynı hesabın birden fazla yerde açık kalması, ortak kullanılan bilgisayarlarda (laboratuvar, sekreterlik) kapatılmayı unutulmuş bir oturumun başkası tarafından kullanılmasına yol açar. Tek aktif oturum kuralı bu riski ortadan kaldırır. Açık soru S-06 bu gereksinimle cevaplanmıştır. | Zorunlu | A tarayıcısında oturum açıkken B tarayıcısından giriş yapılır; A tarayıcısında sayfa yenilendiğinde kullanıcı giriş ekranına düşer ve bilgilendirilir. | Onaylandı |
| **REQ-KMLK-050** | Oturumun başka bir yerden sonlandırıldığı kullanıcıya açıkça bildirilir ("Hesabınıza başka bir cihazdan giriş yapıldı"). | Sebebi söylenmeyen ani bir çıkış, kullanıcıya sistemin bozuk olduğunu düşündürür; ayrıca hesabının izinsiz kullanıldığını fark etmesini de engeller. | Zorunlu | Oturumu düşen kullanıcı giriş ekranında nedeni belirten bir mesaj görüyor. | Onaylandı |
| **REQ-KMLK-051** | 2FA açıkken, parola doğrulandıktan sonra kullanıcıdan doğrulama kodu istenir; kod üyelik akışıyla AYNI altyapıyı kullanır (PRM-KML-02, PRM-KML-09, PRM-KML-10, PRM-KML-16). | İkinci bir kod mekanizması, ikinci bir güvenlik açığı yüzeyidir. Tek altyapı hem bakımı kolaylaştırır hem de kuralların (hash'li saklama, tek kullanımlık, hız sınırı) otomatik olarak geçerli olmasını sağlar. | Zorunlu | 2FA kodu da hash'li saklanıyor, tek kullanımlık ve süreli; aynı hız sınırlarına tabi. | Onaylandı |
| **REQ-KMLK-052** | 2FA açıkken hiçbir doğrulama kanalı bulunmayan kullanıcı sisteme giremez hâle gelir; bu durumdaki kullanıcılar parametre açılmadan ÖNCE listelenir ve uyarı verilir (PRM-KML-08, PRM-KML-15). | 2 aktif personelin ne e-postası ne telefonu var (R-08, 22.09.2026 ölçümü; önceki: 4, 12). Parametre düşünmeden açılırsa bu kişiler kilitlenir ve nedeni anlaşılmaz. Uyarı, kararın sonucunu açmadan önce göstermek içindir. | Zorunlu | Parametre açılmak istendiğinde etkilenecek kullanıcı sayısı ekranda gösteriliyor ve onay isteniyor. | Onaylandı |
| **REQ-KMLK-053** | 2FA'nın açık ve kapalı hâli (PRM-KML-08) ayrı ayrı test edilir. | Varsayılanı kapalı olan bir işlev, test edilmezse ilk açıldığı gün bozuk çıkar. Parametrenin iki durumu da davranıştır. (ADR-0011) | Zorunlu | Her iki durum için de otomatik test var ve geçiyor. | Onaylandı |
| **REQ-KMLK-054** | Meşru uzun süreli etkinlik sırasında (ör. eğitim videosu oynatılırken) sayfa, sunucuya **etkinlik sinyali** gönderir ve hareketsizlik süresi sıfırlanır. Sinyal yalnızca etkinlik GERÇEKTEN sürerken gönderilir (video oynuyor ve sekme görünür); toplam oturum süresi (PRM-KML-12) her durumda geçerlidir. | İK notu (23.09.2026, REQ-KMLK-026). Hareketsizlik kuralını kapatmak yerine meşru etkinliği tanımak gerekir: kural kapatılırsa açık unutulan her oturum 8 saat açık kalır. Mekanizma T3'te kurulur; eğitim modülü (İ2) kullanır. | Zorunlu | 40 dakikalık video izleyen kullanıcının oturumu düşmüyor; video duraklatılıp 30 dakika beklendiğinde düşüyor; sekme arka plandayken sinyal gönderilmiyor. | Toplantı sonucu eklendi |

### Parola

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **REQ-KMLK-028** | Parola en az 6 karakter olmalıdır; karmaşıklık (büyük/küçük harf, rakam, simge) zorunluluğu varsayılan olarak DAYATILMAZ. İki değer de Sistem Yönetimi parametresidir. | İK kararı (21.09.2026): kullanıcıların önemli bölümü seyrek kullanıcı; 8+ karakter parola oluşturmakta zorlanıyor ve parola unutma vakaları artıyor. Değer parametre olduğu için ihtiyaç doğduğunda kod değişikliği olmadan artırılabilir. Parametreler: PRM-KML-05, PRM-KML-06. Not: 6 karakter, güncel güvenlik önerilerinin (en az 8) altındadır; bu bilinçli bir ödündür ve REQ-KMLK-029, REQ-KMLK-024 ile REQ-KMLK-049 telafi edici kontrollerdir. | Zorunlu | Parametredeki değerden kısa parola kabul edilmiyor; parametre değiştirildiğinde yeni kural kod değişikliği olmadan geçerli oluyor. | Onaylandı |
| **REQ-KMLK-029** | Yaygın sızıntı listesinde bulunan parolalar kabul edilmez; kontrol çevrimdışı yapılır. | En sık ele geçirilme yolu, başka sitede sızmış parolanın tekrar kullanılmasıdır. Dış servise sorgu yapılmaz. (ADR-0006 §6) BU KONTROL, en az uzunluğun 6 karaktere indirilmesinden sonra parola güvenliğinin ASIL dayanağıdır; devre dışı bırakılamaz. | Zorunlu | Bilinen sızmış bir parola reddediliyor; internet bağlantısı gerekmiyor. | Onaylandı |
| **REQ-KMLK-030** | Zorunlu periyodik parola değişimi varsayılan olarak uygulanmaz (parametre: PRM-KML-07). | Güncel güvenlik rehberleri önermiyor; kullanıcıyı zayıf kalıplara itiyor. (ADR-0006 §6) | Zorunlu | Sistem parola değişimi dayatmıyor. | Onaylandı |
| **REQ-KMLK-031** | Parola sıfırlama, üyelikle aynı doğrulama akışını kullanır (kanal seçimi + 6 haneli kod); aynı parametrelere tabidir (PRM-KML-02, PRM-KML-09, PRM-KML-10). | Tek bir doğrulama mekanizması; ikinci bir akış ikinci bir güvenlik açığı yüzeyidir. (ADR-0006 §6) | Zorunlu | Parolasını unutan kullanıcı İK’ya başvurmadan sıfırlayabiliyor. | Onaylandı |
| **REQ-KMLK-032** | Oturum içinde parola değiştirirken mevcut parola sorulur. | Açık bırakılmış bir oturumu bulan kişi parolayı değiştirip hesabı ele geçirememelidir. | Zorunlu | Mevcut parola girilmeden değişiklik yapılamıyor. | Onaylandı |
| **REQ-KMLK-033** | Parola geri döndürülebilir biçimde saklanmaz ve hiçbir kayda yazılmaz. | Parola, kurum içinde bile kimse tarafından görülememelidir. (KR-059) | Zorunlu | Veritabanında ve günlüklerde parola bulunmuyor; testle doğrulanıyor. | Onaylandı |
| **REQ-KMLK-055** | İlk girişte parola değiştirme zorunluluğu Sistem Yönetimi parametresiyle yönetilir; **varsayılan olarak kapalıdır** (PRM-KML-20). | İK kararı (23.09.2026, S-02): üyelik sırasında kullanıcının kendi belirlediği parola yeterli. | Olmalı | Parametre kapalıyken ilk girişte parola değişikliği istenmiyor; açıldığında isteniyor. | Toplantı sonucu eklendi |

### Hesap Yaşam Döngüsü

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **REQ-KMLK-034** | Personelin tüm aktif istihdamları sona erdiğinde hesabı otomatik pasife düşer ve açık oturumları sonlandırılır (PRM-HSP-02). | Ayrılan personelin erişimi elle kapatmaya bırakılamaz; unutulur. (KR-015) | Zorunlu | LOGO’da çıkış işlenen personelin hesabı en geç bir sonraki senkronizasyonda kapanıyor. | Onaylandı |
| **REQ-KMLK-035** | Personel yeniden işe girdiğinde mevcut hesabı yeniden aktifleşir; yeni hesap açılmaz. | Geçmiş izin ve eğitim kayıtlarıyla bağ korunmalıdır. (KR-014) | Zorunlu | Aynı kişi ikinci kez üye olmak zorunda kalmıyor. | Onaylandı |
| **REQ-KMLK-036** | İK, bir hesabı elle pasife alabilir; gerekçe zorunludur. | Disiplin süreci, uzun süreli izin gibi durumlar için. Gerekçesiz kapatma sonradan açıklanamaz. | Zorunlu | Gerekçe girilmeden işlem tamamlanmıyor; denetim izinde görünüyor. | Onaylandı |
| **REQ-KMLK-037** | Hesap durum değişiklikleri (açılma, pasifleşme, kilitlenme) denetim izine yazılır. | KVKK ve iç denetim gereği. (ADR-0009 §2) | Zorunlu | Her durum değişikliği kim/ne zaman bilgisiyle kayıtlı. | Onaylandı |

### Güvenlik

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **REQ-KMLK-038** | Üyelik denemeleri TCKN başına saatte ~~5~~ **10**, IP başına saatte 20 ile sınırlıdır; iki sınır da Sistem Yönetimi parametresidir (PRM-KML-18, PRM-KML-19). | Toplu TCKN denemesiyle personel listesi çıkarılmasını engeller. (ADR-0006 §5) | Zorunlu | Sınır aşıldığında istek reddediliyor ve olay kaydediliyor. | Onaylandı; **değişiklik talebi #185** (İK teyidi kabulde, T3 kabul planı §6.1 T-03) |
| **REQ-KMLK-039** | Kod gönderimi kişi başına 15 dakikada en fazla ~~3~~ **10** kez yapılabilir; sınır Sistem Yönetimi parametresidir (PRM-KML-17). | SMS maliyeti ve kullanıcıyı rahatsız etme riskine karşı. (ADR-0006 §5) | Zorunlu | ~~Dördüncü~~ **On birinci** istek reddediliyor. | Onaylandı; **değişiklik talebi #185** (İK teyidi kabulde, T3 kabul planı §6.1 T-03) |
| **REQ-KMLK-040** | Tüm kimlik olayları (giriş, başarısız giriş, kilitlenme, kod gönderimi, parola değişimi) denetim izine yazılır. | Bir güvenlik olayı incelenirken "ne oldu" sorusunun cevabı kayıtlardan çıkmalıdır. (ADR-0009 §2) | Zorunlu | Olaylar kullanıcı, zaman, IP ve takip numarasıyla kayıtlı. | Onaylandı |
| **REQ-KMLK-041** | Kişisel veriler günlük kayıtlarında maskelenir (TCKN 123*****901, telefon 532*****67, e-posta ab***@duzen.com.tr). | Günlük dosyası yedeklere ve kapsayıcı günlüklerine yayılır; düz metin kişisel veri geri alınamaz bir KVKK ihlalidir. (KR-059) | Zorunlu | Günlükte düz metin kişisel veri bulunmuyor; testle doğrulanıyor. | Onaylandı |
| **REQ-KMLK-042** | Doğrulama kodu ve parola sıfırlama iletileri bildirim istisnasından MUAFTIR (PRM-BLD-03). | İstisna kapsamındaki kişi kod alamazsa sisteme hiç giremez. (KR-056) | Zorunlu | Bildirim istisnası tanımlı kişi de doğrulama kodunu alıyor. | Onaylandı |

### Kullanılabilirlik

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **REQ-KMLK-043** | Tüm ekranlar ve hata mesajları Türkçedir; teknik terim kullanılmaz. | Kullanıcıların çoğunluğu düşük teknik seviyeli ve seyrek kullanıcı. (ADR-0015 §6, KR-039) | Zorunlu | Hata mesajı ne yapılacağını söylüyor ("Bir hata oluştu" demiyor). | Onaylandı |
| **REQ-KMLK-044** | Giriş ve üyelik ekranları telefon ve tablette de kullanılabilir. | Yönetici sahada olabilir; personel bilgisayara erişemeyebilir. (ADR-0015 §8) | Zorunlu | Ekranlar telefonda okunabilir ve kullanılabilir. | Onaylandı |
| **REQ-KMLK-045** | Ekranlar klavye ile kullanılabilir; form alanlarının etiketi vardır. | Erişilebilirlik ve hızlı veri girişi. (ADR-0015 §7) | Olmalı | Sekme sırası mantıklı; etiketsiz alan yok. | Onaylandı |
| **REQ-KMLK-046** | Hata ekranında kullanıcıya bir takip numarası gösterilir. | Destek talebinde bu numara iletilince kayıt doğrudan bulunur. (ADR-0009 §2) | Olmalı | Hata ekranında takip numarası görünüyor. | Onaylandı |
| **REQ-KMLK-056** | Üyelik ve giriş ekranlarında "sorun yaşarsanız" başvurulacak birim olarak **Bilgi İşlem** gösterilir; iletişim bilgisi Sistem Yönetimi parametresidir (PRM-GRN-04). | İK kararı (23.09.2026, S-10). Metin koda gömülmez; kişi veya numara değiştiğinde yeni sürüm gerekmez. | Olmalı | Üyelik ekranında Bilgi İşlem iletişim bilgisi görünüyor; parametre değiştirildiğinde ekran güncelleniyor. | Toplantı sonucu eklendi |

### Görsel Tasarım

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **REQ-KMLK-047** | Giriş, üye ol, doğrulama ve parola ekranları ekranı iki bölüme ayıran (split layout) bir düzende tasarlanır: bir bölümde form alanları, diğer bölümde kurumsal görsel ve/veya bilgilendirici içerik yer alır. | İK talebi (12.09.2026). Form tek başına ortada durduğunda ekran kurumsal kimlik taşımaz ve kullanıcıya doğru sisteme girdiğini göstermez. Bilgilendirici bölüm ayrıca duyuru ve yardım metni için yer açar. Görsel içerik Claude tarafından üretilecektir (İK kararı 23.09.2026, S-13). | Zorunlu | Masaüstünde ekran iki bölüme ayrılıyor; dar ekranda (telefon) bölümler alt alta geçiyor ve form üstte kalıyor. | Onaylandı |
| **REQ-KMLK-048** | Giriş ve doğrulama ekranlarında kurumsal logo kullanılır; logo, Sistem Yönetimi parametresinden yüklenir (PRM-GRN-01); kurulumda varsayılan olarak depodaki assets/duzen_logo.png dosyası kullanılır. | İK talebi (12.09.2026). Logonun tek bir kaynaktan gelmesi, kurumsal kimlik güncellendiğinde tek yerden değiştirilerek tüm ekranların güncellenmesini sağlar. İK talebi (20.09.2026) ile logo, Sistem Yönetimi parametresi hâline getirildi; değiştirmek için yeni sürüm gerekmez. | Zorunlu | Logo giriş ekranında görünüyor; farklı ekran boyutlarında bozulmuyor ve alternatif metni (alt) tanımlı. | Onaylandı |

---

## 5. İzlenebilirlik

Her gereksinim `docs/33061/izlenebilirlik-matrisi.md` §5'te bir satır olarak açılmıştır.
Sistem gereksinimi, tasarım, kod ve test halkaları, işin yapıldığı PR'da doldurulur
(matris §3). **Bağlanmamış gereksinim tamamlanmamış sayılır.**

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-24 | 1.0 | İlk sürüm — 23.09.2026 İK toplantısında onaylanan 56 gereksinim | Bilgi İşlem |
| 2026-09-24 | 1.1 | REQ-KMLK-011: bağlantı yalnızca LOGO'daki kurumsal e-postaya, geçerlilik 3 saat (S-05 revize); REQ-KMLK-022 gerekçesine 2 kişiye e-posta tanımı eklendi | Bilgi İşlem |
| 2026-09-28 | 1.2 | REQ-KMLK-047 gerekçe notu toplantı kaydıyla uyumlu hâle getirildi: S-13 cevabı "Claude tarafından en uygun görsel üretilsin" (aktarım hatası; gereksinim metni değişmedi, #90) | Bilgi İşlem |
| 2026-10-10 | 1.3 | **Değişiklik talebi #185:** REQ-KMLK-038 TCKN başına saatte 5 → 10; REQ-KMLK-039 15 dakikada 3 → 10 (parametre aralığı 1–15). Neden: UAT'de İK ile yapılan üyelik denemelerinde sınırlar çok çabuk engelledi (Doğuş Uçanok, 10.10.2026). RACI'ye göre kabul İK'dadır; teyit kabul oturumunda alınır | Bilgi İşlem |
