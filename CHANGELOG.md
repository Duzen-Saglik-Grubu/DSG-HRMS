# Değişiklik Günlüğü

Bu dosya, DSG-HRMS'in sürüm geçmişini tutar.
Biçim [Keep a Changelog](https://keepachangelog.com/tr/1.1.0/) yaklaşımına,
sürümleme [Semantic Versioning](https://semver.org/lang/tr/) kurallarına dayanır.

**Sürüm türleri:** `Eklendi` · `Değiştirildi` · `Kullanımdan kaldırıldı` ·
`Kaldırıldı` · `Düzeltildi` · `Güvenlik`

> Her kabul edilen modül bir **MINOR sürüm** ve bir **baseline** üretir (MAN.5, `KR-097`).
> Kabule sunulan sürüm `-rc.N` ön sürüm etiketi alır; İK kabulünden sonra aynı commit
> ön eksiz etiketlenir. Sürüm etiketi, kabul formunun atıf yaptığı konfigürasyon öğesidir.

---

## [Yayımlanmamış]

### Güvenlik
- **System.Security.Cryptography.Pkcs 10.0.13'e sabitlendi (#223).** MailKit ve SQL Server sürücüsü üzerinden dolaylı gelen 10.0.0 sürümünde 13.10.2026'da iki hizmet engelleme (DoS) açığı yayımlandı: CVE-2026-84008 (yüksek) ve CVE-2026-70162 (orta). `main` 14.10.2026'da bu yüzden kırmızıya düştü; kayıt otomatik açıldı.
- **zod 4.6.7, vitest 5.0.3 ve undici 8.12.3 (#288).** 24.10.2026'da yayımlanan açıklar kapatıldı: zod'da büyük koleksiyon doğrulamasında bellek tüketimi (yüksek), vitest'te geliştirme sunucusunda kod enjeksiyonu (orta, yalnızca geliştirme bağımlılığı). Aynı gün `undici`'de (test ortamındaki `jsdom` üzerinden) üç yüksek önemli açık da kapatıldı.
- **follow-redirects 1.16.1 (#250) ve vite 8.3.4 (#255).** Ön yüzün dolaylı ve geliştirme bağımlılıklarında yayımlanan yüksek önemli açıklar kapatıldı. Açıklar tarayıcıya giden pakette değildi.
- **Varsayılan yetki politikası oturum ister (#257, `KR-117`).** Yetki işareti taşımayan bir uç nokta artık anonim kalmaz. Oturumsuz erişilebilen uçların listesi mimari testinde sabittir.
- **Uygulama veritabanına kısıtlı rolle bağlanır (#258, `KR-118`).** Uygulama denetim, erişim ve güvenlik kayıtlarını yalnızca okuyup ekleyebilir. Bu kayıtları değiştiremez, silemez ve koruma tetikleyicisini kapatamaz.
- **Erişim kaydında arama metni maskelenir (#259).** Arama kutusuna yazılan TCKN, telefon ve e-posta erişim kaydına düz metin yazılmaz.

### Eklendi
- **Kişi kartı ve "Profilim" API'si (#276, T1).** Rolü olmayan personel yalnızca kendi kartını görür; İK ve Sistem Yöneticisi tüm personeli görür. Kapsam dışındaki kişi ile var olmayan kişi aynı yanıtı alır. Tam TCKN'yi göremeyen kullanıcı, kendi kartında da `123******01` biçiminde maskeli görür. Kartta yaş ve her istihdamın kıdemi hesaplanır; ayrılan personelde çıkış günü sayılır. Her kart açılışı erişim kaydına yazılır. Ekranlar sonraki sürümlerde gelecek.

- **LOGO'dan T1 kişi alanları ve kurum tanımları (#280).** Cinsiyet, önceki soyadı (yalnızca kadın personelde), medeni hâl, eğitim ve adres senkronize edilir; şube (şehir dahil) ve birim tanımları HRMS'e alınır. Din ve sağlık alanları okunmaz. Senkronizasyon önce kişi ve istihdamı kaydeder; T1 adımı başarısız olursa bu değişiklikler kalır ve çalışma "kısmen tamamlandı" olarak kaydedilir. Henüz ekranda gösterilmiyor.
- **Kişi kartında genel bilgiler ve adres (#282).** Kart API'si cinsiyet, önceki soyadı, medeni hâl ve eğitimi döndürür. Adres yalnızca İK'ya, Sistem Yöneticisine ve kişinin kendisine gider; diğer kullanıcıların yanıtında bu bölüm hiç yer almaz.
- **Tarih aralıklı istihdam nitelikleri (#285, değişiklik talebi #284).** Firma, şube, birim, ünvan, statü ve SGK meslek bilgisi tarihleriyle tutulur. Değer değişince eski kayıt o gün kapanır; istihdam bitince açık kayıt çıkışın ertesi günü kapanır. Çakışan aralıkları veritabanı reddeder. Kartın her istihdam sekmesi güncel nitelikleri gösterir. Geçmiş, bu sürümün kurulduğu günden başlar.

### Değişti
- **İK rolünün adı "İnsan Kaynakları" oldu ve T1 izinleri eklendi (#276).** İK artık senkronizasyonu elle de başlatabilir (`KR-126`); denetim kayıtlarını ve sistem parametrelerini de görür ve parametreleri değiştirebilir (#278, `KR-128`). Senkronizasyon izinlerinin kodu `identity.sync.*` iken `personnel.sync.*` oldu.
- **Ad ve soyad Türkçe kurala göre sıralanır ve aranır (#244).** Hesap işlemleri ekranında Ç, İ, Ö, Ş ve Ü alfabedeki yerlerinde sıralanır. "ışık" araması IŞIK'ı, "ismail" araması İSMAİL'i bulur. Aramada ASCII `I`, Türkçedeki gibi `ı` sayılır; bu yüzden `ISMAIL` yazmak İSMAİL'i bulmaz.

---

## [0.2.0] — 2026-10-13 · T3 Kimlik Yönetimi

İK'nın 13.10.2026'da **kabul ettiği** sürüm (G3, `KR-105`). Kod `v0.2.0-rc.5` ile aynı commit'tir (`149f6ee`); değişiklikler aşağıdaki aday bölümlerindedir. Kabul formu: `docs/33061/TEC.11-gecerleme/kayitlar/2026-10-13-t3-kabul-formu.md`.

---

## [0.2.0-rc.5] — 2026-10-12 · T3 Kimlik Yönetimi kabul adayı (5)

`v0.2.0-rc.4`'ün yerine İK kabulüne sunulan sürüm. Bilgi İşlem'in kabul senaryoları provasında çıkan düzeltmeyi içerir; kabul planındaki düzeltmeler (#196) yalnızca belgede yapıldı.

### Düzeltildi
- **Parametre ekranında T5'e ait bir parametre görünüyordu (#198).** "İK'nın kilitli hesabı açabilmesi" (PRM-HSP-03) hiçbir işleve bağlı olmadan listeleniyordu. Kilit açma T5 Kullanıcı Yönetimi kapsamında; parametre o modülle birlikte gelecek (SYG-KMLK-076).

---

## [0.2.0-rc.4] — 2026-10-11 · T3 Kimlik Yönetimi kabul adayı (4)

`v0.2.0-rc.3`'ün yerine İK kabulüne sunulan sürüm.

### Değiştirildi
- **İki adımlı doğrulama kullanıcı tercihine bağlı (#190, `KR-104`).**
  - Sistem parametresi (PRM-KML-08) 2FA'yı yalnızca kullanıma açar; her kullanıcı "Hesap güvenliği" ekranından kendi hesabında açar veya kapatır. Varsayılan kapalı.
  - Açmak için mevcut parola ve seçilen kanala gelen kodun doğrulanması gerekir; kapatmak için mevcut parola.
  - Parametre açılırken uyarı, 2FA'yı kendisi açmış kişi sayısını gösterir.
  - Onaylı gereksinimlerde değişiklik talebi; İK onaylı.

### Eklendi
- "Hesap güvenliği" ekranı ve uçları (`/api/v1/identity/account/two-factor`).
- **Kurtarma:** İK, doğrulama kodunu alamayan kişinin 2FA'sını "Hesap işlemleri" ekranından gerekçe girerek kapatır (`POST /api/v1/identity/accounts/{personId}/two-factor/reset`).

---

## [0.2.0-rc.3] — 2026-10-10 · T3 Kimlik Yönetimi kabul adayı (3)

`v0.2.0-rc.2`'nin yerine İK kabulüne sunulan sürüm.

### Değiştirildi
- Kod gönderim sınırının (PRM-KML-17, kişi başına / 15 dakika) varsayılanı 3 → **10**, ayarlanabilir aralık 1–10 → **1–15**; üyelik deneme sınırının (PRM-KML-18, TCKN başına / saat) varsayılanı 5 → **10**. UAT'de İK ile yapılan üyelik denemelerinde sınırlar çok çabuk engelliyordu. Onaylı gereksinimlerde (REQ-KMLK-038, 039) değişiklik talebidir; İK teyidi kabulde (#185)
- Ekran testlerinde yavaş CI makinesi için süre payı (#182)

---

## [0.2.0-rc.2] — 2026-10-04 · T3 Kimlik Yönetimi kabul adayı (2)

`v0.2.0-rc.1`'in yerine İK kabulüne sunulan sürüm (#160).

### Düzeltildi
- Kullanıcı kendi hesabını pasife alamaz; aktif kalan son sistem yöneticisinin hesabı da pasife alınamaz. Önceden tek yönetici kendi hesabını pasife alıp sistemi yönetilemez bırakabiliyordu (#138)

---

## [0.2.0-rc.1] — 2026-10-04 · T3 Kimlik Yönetimi kabul adayı

İK kabulüne (TEC.11) sunulan sürüm. Kabul planı: `docs/33061/TEC.11-gecerleme/T3-kabul-plani.md`.

### Eklendi
- **Personel verisi:** Kişi ve istihdam modeli; LOGO'dan salt okunur personel senkronizasyonu (#75, #76, #78)
- **Üyelik:** T.C. Kimlik Numarası, doğum tarihi ve kurumsal e-postayla LOGO kaydına karşı kimlik doğrulama; e-posta veya SMS ile doğrulama kodu; parola politikası (#84, #86, #88, #91)
- **Giriş ve oturum:** Kurumsal e-postayla giriş; tek aktif oturum; hareketsizlik ve toplam süre sınırı; hatalı girişte hesap kilidi (#94, #96)
- **Parola:** Parola sıfırlama, oturum içinde değişiklik, periyodik ve ilk girişte zorunlu değişim (parametreyle) (#98, #113)
- **İK hesap işlemleri:** Personel arama, hesabı pasife alma ve yeniden aktifleştirme (gerekçe zorunlu), parola oluşturma bağlantısı gönderme (#100, #105, #108)
- **Sistem yönetimi:** Parametre ekranı, kurumsal logo, iki adımlı doğrulama ve açılmadan önce etki uyarısı (#109, #111)
- **Denetim:** Kimlik olaylarının değiştirilemez güvenlik olayı kaydı; bildirim istisnası kancası (#119)
- **UAT ortamı:** Kurulum runbook'u, dağıtım betiği ve TLS (#38, #43). Dağıtım izlenebilir: yalnızca commit edilmiş ve GitHub'a gönderilmiş kod dağıtılır, imaj commit'i etiketinde taşır, her dağıtım kayda geçer (#125)
- **Kalite:** Uçtan uca testler (Playwright); her sistem gereksiniminin doğrulama kanıtı, PR inceleme onayı ve ön yüz bağımlılık güvenliği CI'da denetlenir (#122, #128, #146, #149)

### Düzeltildi
- Sayfa yenilemede oturumun kapanması ve sunucu saati farkı (#102)
- Davet bağlantısının jetonu açılışta kayboluyordu (#137)
- Girişteki en kısa yanıt süresi kaldırıldı; giriş artık gereksiz yere beklemiyor (#120)
- Kullanıcı iletileri: kısa parola iletisi gereken karakter sayısını söylüyor; teknik terimler çıkarıldı; doğrulama iletileri her durumda Türkçe (#152)

### Güvenlik
- API yalnızca HTTPS üzerinden gelen isteğe hizmet verir (#115)
- Günlüklerde kişisel veri, doğrulama kodu ve parola maskelenir; gerçek günlük dosyasıyla test edilir (#119)
- UAT'ye kaynak aktarımı `git archive` ile yapılır; geliştirici makinesindeki yerel ortam dosyaları sunucuya artık kopyalanmaz (#125)

---

## [0.1.0] — 2026-09-12 · A1 Teknik iskelet

Etiket 04.10.2026'da geriye dönük olarak A1 kapanış commit'ine (`065a7c3`) konmuştur (#125).

### Eklendi
- Proje doküman altyapısı ve 33061 kanıt klasör düzeni; 15 Mimari Karar Kaydı (ADR-0001 … ADR-0015)
- Vizyon ve kapsam belgesi, modül listesi ve bağımlılık haritası, proje planı, iş kırılım yapısı ve RACI matrisi
- Karar ve risk kayıt defterleri, paydaş listesi; mevcut sistem veri envanteri ve NetGSM incelemesi
- GitHub akış altyapısı: issue ve PR şablonları, katkı rehberi, CODEOWNERS, dal koruma telafi kontrolleri
- Katmanlı solution yapısı ve mimari testleri (#6)
- Veritabanı altyapısı, yapılandırma ve sır yönetimi (#8)
- Günlük altyapısı ve kişisel veri maskeleme; denetim izi ve değiştirilemezlik koruması; erişim kaydı (#10, #12, #14)
- Merkezi hata yönetimi (Problem Details), OpenAPI sözleşmesi, sağlık kontrolleri ve gözlemlenebilirlik (#16, #20, #22)
- CI kalite kapıları ve PR izlenebilirlik denetimi (#18, #24)
- Ön yüz iskeleti, OpenAPI'den tip üretimi ve ortak bileşenler (#26, #32)
- Docker imajları ve Compose yığınları (geliştirme ve UAT) (#30)

---

## Sürüm planı

Modül başına bir MINOR sürüm (`KR-097`). Numaralar kabul sırasına göre verilir.

| Sürüm | İçerik | Durum |
|---|---|---|
| `v0.1.0` | Teknik iskelet (A1) | Etiketlendi (geriye dönük) |
| `v0.2.0` | T3 Kimlik Yönetimi — ilk kullanıcı teslimi | **Kabul edildi** (13.10.2026); `v0.2.0-rc.5` ile aynı commit |
| `v0.3.0` … | A2'nin kalan modülleri (T1, T2, T4, T5), ardından yatay altyapı (A3) ve iş modülleri (A4+); her biri kabul edildiğinde | Planlandı |
| `v1.0.0` | Üretime geçiş (AS) | Planlandı |
