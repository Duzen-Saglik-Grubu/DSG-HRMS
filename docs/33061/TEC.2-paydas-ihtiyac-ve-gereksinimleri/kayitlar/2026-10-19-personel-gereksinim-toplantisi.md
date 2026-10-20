# İK Gereksinim Toplantısı — T1 Personel Yönetimi

**Belge kimliği:** TEC.2-KAYIT-2026-10-19-PERS
**Süreç:** TEC.2 — Paydaş İhtiyaç ve Gereksinimlerinin Tanımlanması
**Tarih:** 2026-10-19, 14:30–17:00
**Yer / biçim:** Yüz yüze
**Kaydı tutan:** Doğuş Uçanok (Bilgi İşlem)
**Kayıt tarihi:** 2026-10-20 (toplantıdan sonra 48 saat içinde, `YAKLASIM.md` §3)

> **Toplantı girdisi:** Toplantıya 16.10.2026 tarihli gereksinim taslağı getirildi. Taslakta
> 41 gereksinim, 12 açık soru, 6 ekran, LOGO alan eşlemesi ve KPÖ taslağı vardı. Taslak
> toplantıda satır satır görüşüldü; İK Onayı, İK Notu ve İK Cevabı sütunları toplantı
> sırasında dolduruldu. Doldurulmuş dosyanın özgün hâli Bilgi İşlem'de saklanır. Dosya
> kişisel veri içermez, ancak depoya alınmadı: bu kayıt içeriğini eksiksiz aktarır ve tek
> doğruluk kaynağı depodur (T3'teki uygulamayla aynı).

---

## 1. Katılımcılar

| Ad | Rol | Paydaş kodu |
|---|---|---|
| Doğuş Uçanok | Bilgi İşlem Birim Sorumlusu; KVKK Sorumlusu | P2, P6 |
| Gamze Demir | İnsan Kaynakları Uzmanı | P1 |
| Ahmet Erdoğan | İnsan Kaynakları Uzmanı | P1 |
| Hilal Nur Başer | İnsan Kaynakları Uzmanı | P1 |
| Soner Yıldırım | İnsan Kaynakları Uzmanı | P1 |

**Çağrılıp katılamayanlar:** Yok.

> Paydaş listesi toplantıdan önce gözden geçirildi. Listenin §4'ü Personel Yönetimi için
> P1'e ek olarak P7 Mali İşler ile P6 KVKK'nın görüşünü öngörür. P6 toplantıdaydı. P7
> toplantıda yoktu; değerlendirmesi §11.8'de.

---

## 2. Kapsam ve kullanım bağlamı *(TEC.2 çıktısı b)*

**Modül:** T1 Personel Yönetimi

**Kimler kullanacak:** İK ve Sistem Yöneticisi bütün personeli görür. Diğer personel
yalnızca kendi kartını görür (PG-PERS-35). Bu kural T2 ve T4 devreye girene kadar
geçerlidir. Ölçüm (16.10.2026): 585 aktif kart ve 564 aktif kişi; LOGO'da toplam 1.547
kart ve 1.371 kişi.

**Hangi durumda kullanılacak:**
- İK günlük işlerinde personeli bulur, kartına bakar, filtreleyip dışa aktarır.
- Personel kendi kartını görür; hatalı bilgisi varsa İK'ya bildirir.
- İK veri kalitesi raporuna bakarak düzeltmeleri LOGO'da yapar.

**Mevcut durumda nasıl yapılıyor:** Personel bilgisi yalnızca LOGO Bordro'da görülebiliyor.
Personelin kendi bilgisine erişmesi ve düzeltme bildirmesi için bir kanal yok.

**T3 ile ilişkisi:** T3 Kimlik Yönetimi önce geliştirildi ve 13.10.2026'da kabul edildi
(`v0.2.0`). Personel çekirdeği T3 kapsamında kuruldu (`KR-077`): kişi/istihdam modeli,
LOGO senkronizasyonu, çalışma kaydı ve şema denetimi. T1 bu çekirdeği genişletir ve
ekranlarını ekler.

---

## 3. Görüşülen gereksinimler *(TEC.2 çıktısı d)*

| Sonuç | Sayı |
|---|---:|
| Olduğu gibi onaylanan | **34** |
| Değişiklikle onaylanan | **7** |
| Reddedilen | 0 |
| **Sunulan toplam** | **41** |
| Toplantıda İK'nın eklediği | **2** (REQ-PERS-042, 043) |
| Açık soru cevabından doğan | **1** (S-08 → PG-PERS-44) |
| **Onaylı toplam** | **44** |

Olduğu gibi onaylanan 34 gereksinimin 5'i açık soru cevaplarıyla netleşti (§8).

### 3.1 Değişiklikle onaylananlar

İK notları aşağıda aynen aktarılmıştır.

| Taslak | İK Notu | PG karşılığı |
|---|---|---|
| REQ-PERS-013 Liste kart ızgarası | *"Kart üzerinde fotoğraf, ad soyad, sicil, şube/birim ve ünvanı/görevi görünsün."* | PG-PERS-13 |
| REQ-PERS-014 Filtre alanları | *"Personel bilgilerinin tamamı için filtreleme yapılabilmeli. Filtre seçme alanlarında çoklu seçim mutlaka olmalı."* | PG-PERS-14 |
| REQ-PERS-019 Genel Bilgiler | *"Medeni Hali Evli olan personellerde, evlilik tarihi girilebilsin, bir de evlilik yardımı almıştır/almamıştır işaretlenebilsin. (LOGO'da bu veri bulunmuyor, bu veriyi İK rolü yada Sistem Yöneticisi girebilecek.) LOGO'dan cinsiyeti kadın olan kişilerde önceki soyadı bilgisi dolu ise ekran da gösterilsin."* | PG-PERS-19 |
| REQ-PERS-021 Çalışma Hayatı | *"1 Birim Operatörü, 2 Birim Teknik Sorumlusu, 3 Birim Sorumlusu, 4 Yardımcı Hizmetler, diğer Boş, kısmını Görevi değil Ünvanı etiketi ile gösterilecek. Ayrıca buradaki 1,2,3,4 gibi tanım karşılıklarını Y3-Referans Veri Yönetimi'nden alacak. İşe giriş tarihi ile hesaplama yaparak kıdem süresini de ekranda göstersin."* | PG-PERS-21 |
| REQ-PERS-036 Özel nitelikli veri | *"Kan grubu bilgisi gösterilmesi şeklinde karar alındı. Sadece İK, Sistem Yöneticisi ve kişinin kendisi görebilecek. KVKK kapsamında bir sorun yaratmıyor."* | PG-PERS-36, `KR-113` |
| REQ-PERS-037 İK'ya bildir | *"Personel telefon, e-posta, adres dışında eğitim, medeni hal, kan grubu bilgilerini de bildirebilsin."* | PG-PERS-37 |
| REQ-PERS-038 Ayrılan personel | *"Süresiz bir şekilde saklanabilmesi şeklinde karar alındı, ancak süre Sistem Yönetimi parametresi olarak tutulsun, KVKK kapsamında bir sorun yaratmıyor."* | PG-PERS-38, `KR-114` |

### 3.2 Toplantıda eklenenler

| Taslak | Gereksinim (İK'nın yazdığı biçimde) | PG karşılığı |
|---|---|---|
| REQ-PERS-042 | *"Dışa aktarma (Excel/PDF) yapılırken hangi alanların dışarıya aktarılabileceği seçilebilir olmalıdır, ayrıca personel resmi ile de dışa aktarım sağlanabilmelidir. Excel'e düz liste, PDF'e ise fotoğraf alanının dışa aktarımı seçildi ise resimli aktarım yapılabilir."* | PG-PERS-42 |
| REQ-PERS-043 | *"Kişi kartında gözükecek ve HRMS içerisinden girilebilecek Görevi alanı eklenecek. Görevi seçimi tanım karşılıklarını Y3-Referans Veri Yönetimi'nden alacak. Örn: Yazılım Uzmanı, İnsan Kaynakları Uzmanı, Biyolog, Donanım Uzmanı vs."* | PG-PERS-43 |

İki satırda öncelik ve kabul kriteri sütunları boş kaldı. Bunları Bilgi İşlem PG'ye yazdı
(§11.6).

### 3.3 Ekranlar

| Ekran | İK Notu | Etkisi |
|---|---|---|
| E-01 Personel listesi | *"Personel bilgilerinin tamamı için filtreleme yapılabilmeli. Filtre seçme alanlarında çoklu seçim mutlaka olmalı."* | PG-PERS-14 |
| E-02 Kişi kartı | *"Uygun"* | — |
| E-03 Senkronizasyon durumu | *"Uygun"* | — |
| E-04 Veri kalitesi raporu | *"Ünvanı ve görevi boş kayıtlarda uyarı listesine eklensin."* | PG-PERS-32 |
| E-05 Kendi profilim | *"Personel telefon, e-posta, adres dışında eğitim, medeni hal, kan grubu bilgilerini de bildirebilsin."* | PG-PERS-37 |
| E-06 İK'ya bildirilen düzeltmeler | *"Uygun"* | — |

Onaylı gereksinimlerin tamamı: `paydas-gereksinimleri/PG-PERS.md`.

---

## 4. Tartışılan ve elenen talepler

| Talep | Neden kapsam dışı | Sonra ele alınacak mı? |
|---|---|---|
| Ücret / maaş bilgisinin kartta gösterilmesi | İK cevabı (S-06): *"hiçbir şekilde gösterilmeyecek, kapsam dışı tutulacak."* | Hayır |
| Personelin kendi fotoğrafını yüklemesi | İK cevabı (S-04). LOGO ile HRMS arasında ikinci doğruluk kaynağı doğardı | Hayır |
| Personelin kendi bilgisini HRMS'te düzeltmesi | Personel düzeltmeyi yalnızca "İK'ya bildir" ile talep eder; İK düzeltmeyi LOGO'da yapar (PG-PERS-03, 37) | Hayır |
| Din ve sağlık bilgileri | Özel nitelikli veri; personel yönetiminin ihtiyacı yok (PG-PERS-36). Kan grubu bu kuralın dışında tutuldu (`KR-113`) | Hayır |
| Yönetici bilgisinin LOGO'dan alınması | Yönetici atamaları T2'de yapılır (PG-PERS-34) | T2 Organizasyon Yönetimi |
| Personel ekleme, düzenleme ve silme | Tek doğruluk kaynağı LOGO'dur (`KR-003`) | Hayır (LOGO Bordro) |
| Ücret, bordro ve SGK bildirimleri | HRMS bordro sistemi değildir | Hayır (LOGO Bordro) |
| Özlük belgeleri | Dosya saklama, virüs taraması ve erişim denetimi ayrı altyapı gerektirir (ADR-0013) | Y7 Çalışan Belge Yönetimi |
| İzin bakiyesi ve talepleri | Ayrı modül | İ1 İzin Yönetimi |
| Eğitim, sertifika ve performans geçmişi | Her biri kendi modülünde | İ2, İ3, İ5 |

---

## 5. Kısıtlar *(TEC.2 çıktısı c)*

| Kısıt | Kaynak | İlgili kayıt |
|---|---|---|
| LOGO'ya hiçbir koşulda yazma yapılmaz | Kurumsal | `KR-003`, `KR-004` |
| LOGO'dan gelen alanlar HRMS'te düzenlenmez; LOGO'da olmayan birkaç alan HRMS'te tutulur | İK kararı | `KR-112` |
| Kan grubu özel nitelikli veridir: şifreli saklanır, yalnızca İK, Sistem Yöneticisi ve kişinin kendisi görür | KVKK md. 6; İK ve KVKK Sorumlusu kararı | `KR-113`, ADR-0009 §5 |
| Kişisel verinin görüntülenmesi ve dışa aktarılması kayda yazılır; kayıtlar 10 yıl saklanır | KVKK | ADR-0009 §3, ADR-0014, `KR-109` |
| Kapsam dışı kayıt 404 döner; kapsam veri katmanında uygulanır | Güvenlik | ADR-0007 §3–4 |
| Sistemde kullanılan değerler koda gömülmez, parametre olur | İK talebi | Y4 parametre kataloğu |
| Sistem yalnızca kurum içinden erişilebilir | Kurumsal | `KR-067` |

---

## 6. Destekleyici sistem ve hizmetler *(TEC.2 çıktısı h)*

| Bağımlılık | Gerekli mi | Durum |
|---|---|---|
| LOGO Bordro (salt okunur) | Evet: personel verisi | ✅ Erişim var, salt okunur yetki doğrulandı (T3) |
| Kurum SMTP sunucusu | Evet: "İK'ya bildir" e-postası | ✅ Mevcut (T3) |
| Uygulama içi bildirim | Evet: "İK'ya bildir" (PG-PERS-37) | ❗ Y1 Bildirim Merkezi henüz yok. T1'de gereken en az biçim tasarımda belirlenir |
| Y3 Referans Veri Yönetimi | Evet: ünvan ve görev tanımları (PG-PERS-21, 43) | ❗ Henüz yok. Bkz. §11.1 |
| T2 Organizasyon Yönetimi | Yönetici filtresi ve ast görünürlüğü (PG-PERS-24, 34) | ❗ Henüz yok. Geçiş kuralı PG-PERS-35 |
| T4 Rol ve Yetki Yönetimi | Rol tanımı ve satır bazlı kapsam | ❗ Henüz yok. Geçiş kuralı PG-PERS-35 |

### 6.1 LOGO alan eşlemesi

Eşleme toplantı taslağıyla İK'ya sunuldu (16.10.2026). Doluluk sayıları 585 aktif kart
üzerinden yapılan sayımlardır; kişisel veri okunmadı.

| Alan | LOGO kaynağı | Kural | Doluluk |
|---|---|---|---|
| Fotoğraf | `LH_001_FAMILY.LDATA` | Yoksa baş harfli yer tutucu | 488 / 585 |
| Medeni hâl | `LH_001_FAMILY.STATUS` | 1 = Evli, diğer = Bekar | 585 / 585 |
| Eğitim | `LH_001_TYPEDEF` (TYP = -5), `EDUCATION` ile | Tanımın adı | 585 / 585 |
| Önceki soyadı | `LH_001_PERIDINF.EXSURNAME` | Yalnızca kadın personelde ve doluysa | 135 kartta dolu (20.10.2026) |
| Kan grubu | `LH_001_PERIDINF.BLOODGROUP` | Şifreli saklanır; görünürlüğü sınırlı | 89 / 585 (20.10.2026) |
| Telefon / e-posta / adres | `LH_001_CONTACT` (TYP 3 / 6 / 1) | Boşsa "Kayıtlı değil" | 577 / 579 / 511 |
| Firma | `L_CAPIFIRM.NAME` | — | 585 / 585 (6 firma) |
| Şube, şehir | `L_CAPIDIV.NAME`, `L_CAPIDIV.CITY` | — | 585 / 585 |
| Birim | `L_CAPIDEPT.NAME` | — | 585 / 585 |
| Ünvanı | `LH_001_PERSON.TITLE` | 1–4 karşılıkları Y3'ten; diğer = boş | 572 / 585 |
| Çalışma durumu | `LH_001_PERSON.TYP` | 1 = Aktif, diğer = Ayrılan | Çıkış tarihiyle tam tutarlı |
| Statü | `LH_001_PERSON.SPECODE` | Olduğu gibi | 580 / 585 |
| SGK meslek kodu / meslek | `LH_001_TYPEDEF` (TYP = -44), `JOBREF` ile; `EXP` / `DESC_` | — | 583 / 585 |
| Görevi, evlilik tarihi, evlilik yardımı, acil durum kişisi | **LOGO'da yok**, HRMS'te girilir | `KR-112` | — |
| Yönetici | **LOGO'dan alınmaz** | T2'de atanır | — |
| Din, sağlık | **LOGO'dan alınmaz** | PG-PERS-36 | — |

---

## 7. Mutabakat *(TEC.2 çıktısı g)*

**Üretilen paydaş gereksinimleri:** `PG-PERS-01` … `PG-PERS-44` (44 gereksinim), bkz.
`paydas-gereksinimleri/PG-PERS.md`.

| Katılımcı | İhtiyaçların doğru yansıdığını onaylıyor mu | Tarih |
|---|---|---|
| Doğuş Uçanok | Evet | 2026-10-19 |
| Gamze Demir | Evet | 2026-10-19 |
| Ahmet Erdoğan | Evet | 2026-10-19 |
| Hilal Nur Başer | Evet | 2026-10-19 |
| Soner Yıldırım | Evet | 2026-10-19 |

**Çekince veya şerh (toplantıda):** Yok.

> Toplantıdan **sonra** Bilgi İşlem'in yaptığı değerlendirme §11'dedir. Bu değerlendirme
> toplantı mutabakatının parçası değildir.

---

## 8. Açık soruların cevapları

12 sorunun tamamı cevaplandı.

| # | Soru | İK cevabı | Gereksinime etkisi |
|---|---|---|---|
| S-01 | TCKN tam mı, maskeli mi gösterilsin? | **İK ve Sistem Yöneticisi tam görsün**; sorumlular dahil diğer herkes maskeli görsün. | PG-PERS-31 netleşti |
| S-02 | Ev adresini kimler görsün? | **İK, Sistem Yöneticisi ve kişinin kendisi** dışında, sorumlular dahil kimse görmesin. | PG-PERS-20 netleşti |
| S-03 | Ayrılan personel ne kadar süre görünür kalsın? | **Varsayılan süresiz**; süre Sistem Yönetimi parametresinden alınacak. "KVKK kapsamında bir sorun yaratmıyor." | PG-PERS-38 değişti, `KR-114` |
| S-04 | Kişi kendi fotoğrafını yükleyebilsin mi? | **Yükleyemesin.** | PG-PERS-23 netleşti |
| S-05 | Yönetici filtresi | T2 ile **birlikte** açılacak (21.09.2026 kararının yinelenmesi). | Değişiklik yok (PG-PERS-34) |
| S-06 | Ücret / maaş gösterilsin mi? | **Hayır**, hiçbir şekilde; kapsam dışı. | §4 |
| S-07 | Kart ızgarasında sayfa başına kaç kişi? | Varsayılan **24**, Sistem Yönetimi parametresinden. | PG-PERS-17 netleşti |
| S-08 | Acil durum kişisi kapsama alınsın mı? | **Alınacak**, LOGO'dan değil HRMS'ten girilecek; yalnızca İK ve Sistem Yöneticisi girer; yalnızca İK, Sistem Yöneticisi ve kişinin kendisi görür (yakınlığı, adı, soyadı, cep telefonu vb.). | PG-PERS-44 eklendi, `KR-112` |
| S-09 | Tablo görünümü de olsun mu? | **Evet.** | PG-PERS-13 |
| S-10 | Görev bilgisi (LOGO'daki 1–4 kodlu alan) | "Ünvanı" etiketiyle gösterilecek; karşılıklar **Y3**'ten; "Bilinmiyor" değil **boş**; veri kalitesi raporuna "ünvanı boş" ve "görevi boş" uyarıları. | PG-PERS-21, 32 |
| S-11 | T2 ve T4'e kadar kim kimi görsün? | İK ve Sistem Yöneticisi herkesi, diğer personel yalnızca kendini görür. | PG-PERS-35 teyit edildi |
| S-12 | "İK'ya bildir" kime gitsin? | Uygulama içi bildirim **İK rolündeki herkese**; e-posta **Sistem Yönetimi parametresinde tanımlı İK adresine**. | PG-PERS-37 netleşti |

---

## 9. Sonraki adımlar

| # | İş | Sorumlu |
|---|---|---|
| 1 | Onaylı gereksinimlerin depoya alınması (`PG-PERS.md`) ve izlenebilirlik matrisinde satırların açılması | Bilgi İşlem (#239) |
| 2 | §11'deki değerlendirmelerin İK ile teyidi | Bilgi İşlem + İK |
| 3 | T1 öncesi tasarım gözden geçirmesi, SYG → öğe tahsisi ve diyagramlar | Bilgi İşlem (#234) |
| 4 | T1 sistem gereksinimleri (SYG-PERS, KPÖ'ler §5'te) ve geliştirmeye başlanması | Bilgi İşlem |
| 5 | Ünvanı boş 13 kartın ve fotoğrafı olmayan kartların LOGO'da tamamlanması | İK |

---

## 10. Kritik performans ölçütleri — bilgi *(TEC.2 çıktısı f)*

KPÖ taslakları toplantı dosyasının "KPO (Bilgi)" sayfasında bilgi olarak sunuldu (TEC.2
YAKLAŞIM §2.2). Ölçütler SYG-PERS §5'te kesinleşir.

| KPÖ taslağı | Eşik | İK'nın görüşü |
|---|---|---|
| KPÖ-PERS-1 Personel listesi açılma süresi | Fotoğraflarla bir sayfa; 20 eş zamanlı kullanıcıda p95 < 2 sn | Not düşülmedi |
| KPÖ-PERS-2 Kişi kartı açılma süresi | p95 < 1 sn | Not düşülmedi |
| KPÖ-PERS-3 Fotoğraflı senkronizasyon süresi | İlk aktarım dışında < 2 dk; değişmeyen fotoğraf yeniden aktarılmaz | Not düşülmedi |
| KPÖ-PERS-4 Telefon ekranında kullanılabilirlik | 360 piksel genişlikte yan kaydırma yok | Not düşülmedi |
| KPÖ-PERS-5 "İK'ya bildir" e-postasının ulaşma süresi | p95 < 60 sn | Not düşülmedi |

KPÖ'lerle ilişkili gereksinimler (PG-PERS-39, 40, 41) olduğu gibi onaylandı.

---

## 11. Toplantı sonrası Bilgi İşlem değerlendirmesi

> **Bu bölüm toplantı mutabakatının parçası değildir.** Toplantıdan sonra, cevapların
> birbiriyle ve mevcut kararlarla tutarlılığı incelenirken yazıldı. Hiçbiri **İK kararını
> değiştirmez**; kararın uygulanma biçimine ilişkin önerilerdir. İK ile teyit edilecektir.

### 11.1 Y3 Referans Veri bağımlılığı (PG-PERS-21, 43)

Ünvan karşılıkları ve görev tanımları Y3'ten alınacak. Ancak Y3 henüz yok; modül
listesinde T4'e bağlı olarak A3'te planlı.

**Öneri:** T3'teki `KR-077` gibi davranılsın. T1, yalnızca kendi onaylı gereksinimlerinin
gerektirdiği referans veri çekirdeğini kursun: tanım listeleri (ünvan, görev) ve bunları
İK ile Sistem Yöneticisinin yönettiği basit bir ekran. Y3 sonra bu çekirdeği genişletir.
Alternatif, T1'i Y3'ü bekletmektir. Bu da T1'i A3'e kadar erteler.

**Karar gerekiyor:** Bilgi İşlem + İK.

### 11.2 HRMS'te tutulan alanlar (PG-PERS-03, 19, 43, 44)

Toplantıda dört bilginin LOGO'da olmadığı ve HRMS'te girileceği kararlaştırıldı: evlilik
tarihi, evlilik yardımı, görev ve acil durum kişisi. PG-PERS-03 ("HRMS'te düzenlenemez")
olduğu gibi onaylandığı için Bilgi İşlem kuralı şöyle yorumladı: kural LOGO'dan gelen
alanlar içindir. Yorum PG-PERS-03'ün gerekçesine ve `KR-112`'ye yazıldı.

**Teyit istenen ayrıntılar:**

| # | Soru | Bilgi İşlem önerisi |
|---|---|---|
| a | Görevi kim girer? (PG-PERS-43'te belirtilmedi) | Evlilik bilgisi ve acil durum kişisiyle aynı kural: yalnızca İK ve Sistem Yöneticisi |
| b | Görev kişiye mi, istihdama (sicile) mı bağlı; tarih aralıklı mı? | İstihdama bağlı ve tarih aralıklı. PG-PERS-12 görevi tarih aralıklı nitelikler arasında sayıyor |
| c | Bir kişiye birden fazla acil durum kişisi girilebilir mi? | Evet (ör. en fazla 2) |
| d | Evlilik tarihi ve evlilik yardımını kimler görür? | Kartı görebilen herkes. Daha dar olması isteniyorsa İK, Sistem Yöneticisi ve kişinin kendisi |
| e | Medeni hâl LOGO'da Evli'den Bekar'a dönerse girilmiş evlilik bilgisi ne olur? | Silinmez, gizlenir; denetim izi korunur |

### 11.3 Kan grubu (PG-PERS-36)

Kan grubu KVKK md. 6'ya göre sağlık verisidir, yani özel nitelikli kişisel veridir.
Karar, KVKK Sorumlusu olarak Doğuş Uçanok'un toplantıdaki değerlendirmesiyle alındı
(`KR-113`). Uygulamada şunlar gerekir:

- Veritabanında kolon bazlı şifreleme (ADR-0009 §5). Şifreli alanda arama yapılmaz.
  Bu nedenle kan grubu filtresi (PG-PERS-14) ancak ayrı bir arama anahtarıyla
  mümkündür; tasarımda değerlendirilir.
- Görüntülenmesi erişim kaydına yazılır (PG-PERS-28).
- "İK'ya bildir" ile gönderilen kan grubu değeri de özel nitelikli veridir. Öneri:
  e-postaya hiçbir bildirimde yeni değer yazılmasın, e-posta HRMS'teki kayda bağlantı
  versin.
- **Öneri:** Kan grubunun md. 6/3'teki hangi işleme şartına dayandığı ve aydınlatma
  metnindeki yeri KVKK Sorumlusunun kaydıyla bilgi kayıt defterine yazılsın. Denetimde ilk
  sorulacak soru budur.

LOGO'da kan grubu 585 aktif kartın yalnızca 89'unda dolu (20.10.2026). Veri kalitesi
raporuna "kan grubu boş" uyarısı eklenmesi İK'ya sorulabilir. Toplantıda istenmediği için
eklenmedi.

### 11.4 Ayrılan personelin süresiz görünürlüğü (PG-PERS-38)

Karar ekrandaki **görünürlüğü** düzenler: ayrılan personel, parametreye süre girilene
kadar filtreyle görünür. Kaydın **saklanma ve imha** süresi ayrı konudur. ADR-0009 §6 bu
konuda şöyle der: *"'Süresiz saklama' KVKK md. 7 karşısında savunulabilir bir konum
değildir."* Bilgi kayıt defterinde HRMS'teki personel verisinin saklama süresi henüz
tanımlı değil. `KR-109` yalnızca denetim izi, erişim kaydı ve güvenlik olaylarını
kapsıyor.

**Öneri:** `KR-114`, görünürlüğün saklama süresi olmadığını açıkça yazar. Personel
verisinin saklama süresi Y2 Denetim ve Erişim Kayıtları modülünden önce KVKK
Sorumlusunun kararıyla bilgi kayıt defterine işlenmeli.

### 11.5 Filtre ve dışa aktarmada görme yetkisi (PG-PERS-14, 42)

"Bilgilerin tamamı için filtre" kararı, görme yetkisiyle birlikte uygulanmalıdır. Örnek:
adresi göremeyen bir kullanıcı adres filtresiyle aradığında, dönen sonuç ona kimin hangi
adreste oturduğunu söyler. Bu nedenle kullanıcının göremediği alan ne filtre ne de dışa
aktarma seçeneği olarak sunulur. Bu kural PG-PERS-14 ve 42'nin gerekçe ve kabul
kriterine yazıldı. **İK teyidi istenir.**

### 11.6 Bilgi İşlem'in yazdığı alanlar (PG-PERS-42, 43, 44)

Toplantıda eklenen iki satırda öncelik ve kabul kriteri boş kaldı. S-08 cevabından doğan
PG-PERS-44 de yeni yazıldı. Üçü için öncelik **Zorunlu** önerildi; kabul kriterlerini
Bilgi İşlem yazdı. **İK teyidi istenir.**

### 11.7 Kıdem süresi (PG-PERS-21)

İK, kıdemin işe giriş tarihinden hesaplanmasını istedi.

**Öneri:** Kıdem istihdam sekmesi başına hesaplanır. Aktif istihdamda bugüne, ayrılan
istihdamda çıkış tarihine kadar sayılır. Toplam kıdem (birden fazla sicili olan kişide
dönemlerin toplamı) istenirse ayrıca belirtilmeli. **İK teyidi istenir.**

### 11.8 P7 Mali İşler

P7 toplantıda yoktu. HRMS LOGO'ya yazmadığı için (`KR-004`) bordro sürecine etkisi yok.
Ücret de kapsam dışı (S-06). Paydaş listesine göre P7 "bilgilendirilen" paydaştır.
**Öneri:** PG-PERS özeti P7'ye bilgi olarak iletilsin.

### 11.9 LOGO bulgusu: aile tablosu

Taslak hazırlanırken aile/resim tablosunun aile bireylerini de tutabileceği not edilmişti
(16.10.2026). 20.10.2026 sayımında bu tablodaki 1.548 satırın tamamı personelin kendi
satırıydı (yakınlık = 0). 585 aktif kartın her birinde tam bir satır var. Fotoğraf ve
medeni hâlin hangi satırdan okunacağı sorusu böylece kapandı.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-20 | 1.0 | Toplantı kaydı (#239) | Bilgi İşlem |
