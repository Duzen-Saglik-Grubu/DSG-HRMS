# Risk Kayıt Defteri

**Belge kimliği:** MAN.4-RKD
**Son güncelleme:** 2026-10-10
**Gözden geçirme sıklığı:** Her modül kapanışında ve en geç ayda bir
**Risk sahibi (genel):** Bilgi İşlem Birim Sorumlusu

---

## 1. Ölçek tanımları

**Olasılık (O):**

| Değer | Anlam |
|---|---|
| 1 – Düşük | Gerçekleşmesi beklenmiyor |
| 2 – Orta | Gerçekleşebilir |
| 3 – Yüksek | Büyük olasılıkla gerçekleşecek veya hâlihazırda gerçekleşiyor |

**Etki (E):**

| Değer | Anlam |
|---|---|
| 1 – Düşük | Küçük gecikme veya yeniden çalışma; kullanıcı etkilenmez |
| 2 – Orta | Modül gecikir, kapsam daralır veya kullanıcı geçici olarak etkilenir |
| 3 – Yüksek | Veri kaybı, hukuki yükümlülük, üretim durması veya belgelendirmenin riske girmesi |

**Risk Puanı = O × E.** Puan 6 ve üzeri riskler **aktif takip** gerektirir ve her durum
raporunda ayrıca ele alınır.

**Durum değerleri:** `Açık`, `İzleniyor`, `Kapandı`, `Gerçekleşti`, `Kabul edildi`

**Risk kabulü (RACI §3):** Puanı **6 ve üzeri** olan bir riski kabul etme yetkisi Üst Yönetim'dedir. Puanı 6'nın altındaki riski Bilgi İşlem kabul edebilir. Her iki durumda da kabul eden ve tarih kayda yazılır. Puan, kabul yetkisini belirlediği için gerekçesiyle yazılır.

**Gözden geçirmeyi tetikleyenler:** modül kapanışı, ayda bir gözden geçirme ve risk niteliğindeki her olay (kesinti, güvenlik bulgusu, ortam arızası). Olay, olay kaydıyla birlikte deftere de işlenir.

---

## 2. Risk Kayıtları

### R-01 — Eski HRMS'ten veri göçünün eksik veya hatalı olması
| | |
|---|---|
| **Kategori** | Veri / Geçiş (TEC.10) |
| **Açıklama** | Mevcut sistemdeki izin, eğitim ve sertifika verilerinin yeni sisteme eksiksiz aktarılamaması. Kaynak veride tutarsızlık, boş alan veya kural dışı kayıt bulunması. |
| **O / E / Puan** | 2 / 3 / **6** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) Göç betiği tekrar çalıştırılabilir (idempotent) yazılacak. (2) Her göç denemesinde kaynak–hedef **mutabakat raporu** üretilecek: adet, alan bazlı karşılaştırma, eşleşmeyen kayıt listesi. (3) En az üç deneme yapılacak: kuru koşu → doğrulama → gerçek kesim. (4) Kabul kriteri: **sıfır açıklanamayan fark**. Açıklanabilen farklar gerekçesiyle listelenir ve İK tarafından yazılı onaylanır. (5) Kesim öncesi eski veritabanının salt-okunur arşiv kopyası alınacak. |
| **Durum** | Açık |

### R-02 — LOGO şema veya sürüm değişikliğinin entegrasyonu kırması
| | |
|---|---|
| **Kategori** | Entegrasyon (TEC.8) |
| **Açıklama** | LOGO Bordro sürüm yükseltmesi sonrası tablo/kolon yapısının değişmesi ve personel senkronizasyonunun sessizce bozulması. |
| **O / E / Puan** | **1** / 3 / **3** *(2026-09-07'de 2/3/6'dan düşürüldü)* |
| **Sahibi** | Bilgi İşlem |
| **Olasılığı düşüren durum** | Sürüm yükseltmeleri **TRISOFT firması tarafından önceden bildirilmektedir**; Mali İşler ve Bilgi İşlem ile tarih uygunluğu teyitleşilir ve yükseltme mutabık kalınan günün mesai bitiminde yapılır. Bugüne kadarki yükseltmelerde **majör bir veritabanı tasarım değişikliği olmamış**, genellikle tablolara kolon eklenmiş; mevcut tablo ve kolonlar değiştirilmemiştir. |
| **Önlem** | (1) LOGO erişimi tek bir bileşenin (`ILogoPersonnelSource`) arkasında yalıtılacak. (2) **Şema doğrulama denetimi** yazılacak: beklenen tablo, kolon ve veri tipleri günlük olarak kontrol edilecek, sapma varsa uyarı üretilecek. (3) Sürüm yükseltmesi **öncesinde ve sonrasında** şema doğrulama denetimi çalıştırılacak ve sonuç kayıt altına alınacak (`KR-049`). (4) Senkronizasyon hatası sessiz kalmayacak, bildirim üretecek. (5) Personel verisi yerel kopyada tutulduğu için LOGO erişimi kesilse dahi sistem çalışmaya devam edecek. |
| **Durum** | İzleniyor |

### R-03 — Kişisel verilerin korunması (KVKK) yükümlülüğünün karşılanamaması
| | |
|---|---|
| **Kategori** | Hukuki / Güvenlik |
| **Açıklama** | Sağlık grubu personel verisi özel nitelikli kişisel veri (sağlık raporu, engellilik, adli sicil vb.) içerir. Yetersiz koruma hukuki yaptırım ve itibar kaybı doğurur. |
| **O / E / Puan** | 2 / 3 / **6** |
| **Sahibi** | Bilgi İşlem + KVKK Sorumlusu |
| **Önlem** | (1) Özel nitelikli alanlarda uygulama seviyesinde şifreleme. (2) Log ve denetim kayıtlarında TCKN/IBAN/iletişim bilgisi **maskeleme**. (3) Görüntüleme (erişim) logu tutulacak — sadece değişiklik logu yeterli değildir. (4) Satır bazlı yetkilendirme: kullanıcı yalnızca yetkili olduğu personeli görebilir. (5) Veri türü bazında yapılandırılabilir saklama süresi ve süre sonunda arşivleme/anonimleştirme yeteneği. (6) UAT ortamına gerçek veri kopyalanacaksa maskeleme uygulanacak. **Güncelleme (2026-10-04):** UAT, kabul testlerinin gerçek personelle yapılabilmesi için gerçek LOGO verisiyle çalışıyor; maskeleme uygulanmıyor. Bu durumun riski ve önlemleri **R-25**'te izlenir. T3'te uygulanan önlemler: günlükte maskeleme (gerçek günlük dosyasıyla test edilir, #119), erişim kaydı (hesap işlemleri listelemesi), hesap işlemleri ekranında yalnızca ad, sicil ve firma (SYG-KMLK-073). |
| **Durum** | Açık |

### R-04 — Kapsam kayması
| | |
|---|---|
| **Kategori** | Proje Yönetimi (MAN.2) |
| **Açıklama** | Modül gereksinim toplantılarında sürekli yeni talep gelmesi nedeniyle modüllerin kapanmaması ve projenin uzaması. |
| **O / E / Puan** | 3 / 2 / **6** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) Her modül için gereksinimler yazılı olarak kilitlenecek ve İK tarafından onaylanacak. (2) Sonradan gelen talepler `degisiklik-talebi` etiketli GitHub Issue olarak açılacak, mevcut modüle eklenmeyecek, sonraki sürüme alınacak. (3) Değişikliklerin etkisi durum raporlarında görünür kılınacak. |
| **Durum** | Açık |

### R-05 — Bilgi tekelliği (tek kişiye bağımlılık)
| | |
|---|---|
| **Kategori** | Proje Yönetimi / Sürdürülebilirlik |
| **Açıklama** | Sistemin 10–15 yıl kullanılması hedefleniyor. Tasarım gerekçeleri yazılı değilse, ekip değiştiğinde sistem bakım yapılamaz hale gelir. |
| **O / E / Puan** | 2 / 3 / **6** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) Her önemli karar ADR olarak kayda geçirilecek. (2) Kod okunabilirliği ve test kapsamı kalite kapılarıyla zorunlu tutulacak. (3) Kurulum, işletim ve bakım dokümanları güncel tutulacak. (4) Alınan her karar, gerekçesiyle birlikte `docs/karar-kayit-defteri.md` içinde kayıt altına alınacak. |
| **Durum** | İzleniyor |

### R-06 — Mevzuat değişikliğinin izin/kıdem hesaplarını geçersiz kılması
| | |
|---|---|
| **Kategori** | İş Kuralları |
| **Açıklama** | 10–15 yıllık kullanım süresinde 4857 sayılı İş Kanunu kaynaklı izin hakediş kurallarının değişmesi. Kurallar koda gömülü olursa her değişiklik yeni sürüm gerektirir. |
| **O / E / Puan** | 3 / 2 / **6** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) Hakediş kuralları **tarih aralıklı, sürümlenmiş parametre tablolarında** tutulacak. (2) Hesaplama motoru saf fonksiyon olarak yazılacak ve birim testlerinin ağırlık merkezi olacak. (3) Geçmişe dönük hesaplar, işlemin yapıldığı tarihte geçerli kural sürümüyle yeniden üretilebilecek. |
| **Durum** | Açık |

### R-07 — LOGO'da TCKN'si boş personel kartları
| | |
|---|---|
| **Kategori** | Veri Kalitesi |
| **Açıklama** | LOGO'da 2 personel kartında TCKN alanı boştur. Bu kişiler kimlik doğrulaması yapılamadığı için üye olamaz ve kişi–istihdam eşleştirmesine giremez. (Bkz. `docs/analiz/01-mevcut-sistem-veri-envanteri.md` §1.4) |
| **O / E / Puan** | 3 / 1 / **3** |
| **Sahibi** | İK Birimi |
| **Önlem** | İki kayıt incelendi: biri işten ayrılmış personele, diğeri (`0001000`) gerçek olmayan bir sicile aittir. `KR-034` ile veri kalitesi kontrolleri **aktif personel** ile sınırlandırılmış ve `0001000` sicili kapsam dışına alınmıştır. Bu kapsamda TCKN'si boş aktif personel bulunmamaktadır. Sistem yine de TCKN'si olmayan kaydı senkronizasyonda **kişi olarak oluşturmayacak**, uyarı listesine alacaktır. |
| **Durum** | Kapandı — 2026-09-05 |

### R-08 — Aktif personelde eksik iletişim bilgisi nedeniyle üye olunamaması
| | |
|---|---|
| **Kategori** | Veri Kalitesi / Devreye Alma |
| **Açıklama** | **Güncel ölçüm (2026-09-22):** 564 aktif kişiden **4'ünde kurumsal e-posta**, **10'unda cep telefonu** kayıtlı değildir; **2'sinde ise ikisi de yoktur**. Bu 2 kişi **hiçbir kanaldan** doğrulama kodu alamaz ve kendi başına üye olamaz. Geçersiz telefon biçimi kalmamıştır. **Önceki ölçümler:** 21.09.2026 → 5 / 12 / 4; 09.09.2026 → 38 / 13 / 12 ve 1 geçersiz numara. İK'nın iki turda yaptığı düzeltme, hiçbir kanaldan doğrulanamayan kişi sayısını **12 → 4 → 2**'ye indirdi. Kurumsal adresi olmayanlar için SMS doğrulaması zorunlu kılınmadığından (`KR-020`), kalıcı çözüm veri düzeltmesidir. (Bkz. `docs/analiz/01-mevcut-sistem-veri-envanteri.md` §1.5) |
| **O / E / Puan** | 2 / 2 / **4** (ilk değer 3 / 2 / 6 idi; etkilenen kişi sayısı 12'den 2'ye indiği için olasılık düşürüldü. Puan 4'te tutuluyor: kalan 2 kişi düzeltilmezse etki KESİNDİR — o kişiler sisteme giremez) |
| **Sahibi** | İK Birimi |
| **Önlem** | (0) **Öncelik: iletişim bilgisi hiç olmayan 2 kişi.** Bu kişiler tamamlanmadan sisteme giremezler. (1) İK, devreye alma öncesinde eksik iletişim bilgilerini LOGO'da tamamlayacak. (2) Sistem, eksik iletişim bilgisi olan aktif personeli listeleyen bir yönetim raporu sunacak. (3) Hiçbir iletişim bilgisi olmayan personel için İK'nın gerekçe girerek hesap açabildiği, tam loglanan **istisna akışı** tasarlanacak (Kullanıcı Yönetimi modülü). **Güncelleme (2026-10-04):** İK, iletişim bilgisi hiç olmayan 2 kişinin sisteme alınmayacağına karar verdi (`KR-076`); bu kişiler için istisna akışı kullanılmayacak. Telefonu olup kurumsal e-postası olmayan kişiler SMS ile üye olabiliyor (T3, PR #88/#91). Ölçüm 22.09.2026'dan beri yenilenmedi; kabul öncesinde yenilenmeli. |
| **Durum** | Açık |

### R-09 — Mevcut sistemin PostgreSQL 14 sürümünün destek dışına çıkması
| | |
|---|---|
| **Kategori** | Altyapı |
| **Açıklama** | Mevcut HRMS, PostgreSQL 14 üzerinde çalışmaktadır ve bu sürümün üretici desteği **Kasım 2026**'da sona ermektedir. Yeni sisteme geçiş bu tarihten sonraya sarkarsa mevcut sistem desteksiz kalır. |
| **O / E / Puan** | 2 / 2 / **4** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) **Karar (2026-09-04):** Yeni sistem, **ayrı bir sunucuda kurulacak güncel PostgreSQL sürümü** üzerinde çalışacaktır; mevcut sunucu ve sürüm yükseltilmeyecektir. (2) Geçiş tamamlanana kadar mevcut sistem yerinde kalacağı için, kesim tarihi Kasım 2026 sonrasına sarkarsa eski sistem desteksiz sürümde çalışmaya devam eder. Bu süre boyunca eski sistem yalnızca veri kaynağı olarak kullanılacak, yeni geliştirme yapılmayacaktır. (3) Kesim sonrası eski veritabanı salt-okunur arşive alınacaktır. |
| **Durum** | İzleniyor — kalıntı risk düşük |

### R-10 — Değerlendirme (rating) çerçevesinin elde bulunmaması
| | |
|---|---|
| **Kategori** | Belgelendirme |
| **Açıklama** | TS ISO/IEC TS 33061 yalnızca **süreç boyutunu** (amaç, çıktı, temel uygulamalar, süreç ürünleri) tanımlar. "Seviye 2" tanımını yapan süreç öznitelikleri (PA 2.1 Performans Yönetimi, PA 2.2 Dokümante Edilmiş Bilgi Yönetimi) ve N/P/L/F derecelendirme ölçeği **TS ISO/IEC 33020** standardındadır. Bu olmadan olgunluk seviyesinin karşılanıp karşılanmadığı objektif olarak ölçülemez. |
| **O / E / Puan** | 3 / 2 / **6** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | TS ISO/IEC 33020 standardı TSE'den temin edilmiş ve incelenmiştir. Seviye 2 kriterleri, derecelendirme ölçeği ve öz değerlendirme tablosu `docs/33061/00-OLGUNLUK-SEVIYESI-KRITERLERI.md` belgesinde tanımlanmıştır. Kapsamdaki 15 sürecin tamamı için PA 1.1 (Tam), PA 2.1 ve PA 2.2 (en az Büyük Ölçüde) kanıtı üretilecek şekilde tasarım yapılmaktadır. |
| **Durum** | Kapandı — 2026-09-04 (standart temin edildi ve kriterler belgelendi) |

### R-12 — Aynı e-posta adresinin birden fazla personele tanımlı olması
| | |
|---|---|
| **Kategori** | Güvenlik / Veri Kalitesi |
| **Açıklama** | **Güncel ölçüm (2026-09-22): 0 paylaşılan adres.** İK, paylaşılan adres kullanan personele kendilerine ait e-posta adresi tanımlamıştır. **Önceki ölçüm (2026-09-12 ve 2026-09-21): 1 e-posta adresi, 4 ayrı kişiye (farklı TCKN) tanımlıydı.** Üyelik doğrulama kodu bu adrese gideceği için, posta kutusuna erişimi olan kişi **başkasının adına hesap açabilir** ve o kişinin özlük verilerine erişebilir. Kontrol tüm aktif personel üzerinde çalıştırılmıştır, belirli bir görev grubuyla sınırlı değildir. (Bkz. `docs/analiz/01-mevcut-sistem-veri-envanteri.md` §1.6) |
| **O / E / Puan** | 2 / 3 / **6** (gerçekleşmedi; veri düzeltmesiyle ortadan kalktı) |
| **Sahibi** | İK Birimi + Bilgi İşlem |
| **Önlem** | (1) İK, ilgili personellere kendilerine ait ayrı e-posta adresi tanımlayacak. **Bu, Kimlik Yönetimi modülünün devreye alınması için ön koşuldur.** (2) Sistem, üyelik akışında **e-posta adresinin birden fazla kişiye tanımlı olması durumunda o adrese doğrulama kodu göndermeyecek**, kaydı istisna akışına yönlendirecek. (3) Senkronizasyon, paylaşılan e-posta adreslerini tespit edip uyarı raporunda listeleyecek. |
| **Durum** | **Kapandı — 2026-09-22.** Ölçüm 0 paylaşılan adres. **Teknik kontrol kaldırılmaz:** paylaşılan adrese doğrulama kodu göndermeme kuralı (`KR-018`, `REQ-KMLK-009`) kalıcıdır. Bugün paylaşılan adres bulunmaması, yarın açılacak bir kaydın da öyle olacağı anlamına gelmez |

### R-13 — Kurumsal olmayan e-posta adresine doğrulama kodu gönderilmesi
| | |
|---|---|
| **Kategori** | Güvenlik |
| **Açıklama** | **Güncel ölçüm (2026-09-21): 0 kişi.** İK, kişisel adreslerin tamamını kurumsal adreslerle değiştirmiştir. **Önceki ölçüm (2026-09-05):** 584 aktif kayıttan 59'unda e-posta adresi kişisel bir adresti (gmail, hotmail, icloud, yahoo); doğrulama kodu kurum denetimi dışındaki bir posta kutusuna gidiyordu. (Bkz. `docs/analiz/01-mevcut-sistem-veri-envanteri.md` §1.6) |
| **O / E / Puan** | 3 / 2 / **6** (gerçekleşmedi; veri düzeltmesiyle ortadan kalktı) |
| **Sahibi** | İK Birimi + Bilgi İşlem |
| **Önlem** | (1) İK, kurumsal e-posta adresi olmayan personele kurumsal adres tanımlayacak. Kalıcı çözüm veri düzeltmesidir; kurumsal adresi olmayanlar için SMS doğrulaması **zorunlu kılınmayacaktır** (`KR-020`). (2) Sistem, kabul edilen kurumsal alan adlarını (`@duzen.com.tr`, `@zeytinim.com`, `@labpt.com.tr`) **yapılandırılabilir bir liste** olarak tutacak; liste dışı adreslere e-posta ile doğrulama kodu gönderilmeyecek. (3) Bu kural Kullanıcı Yönetimi modülü gereksinimlerinde karara bağlanacaktır. |
| **Durum** | **Kapandı — 2026-09-21.** Ölçüm 0 kişi. **Teknik kontrol kaldırılmaz:** LOGO canlı bir sistemdir, yeni kayıt her gün açılabilir. Kurumsal olmayan adrese doğrulama kodu göndermeme kuralı (`KR-019`, `REQ-KMLK-008`) kalıcıdır; risk kapanmıştır, kural kapanmaz |

### R-11 — TSE standard dokümanının telif ihlali oluşturacak şekilde paylaşılması
| | |
|---|---|
| **Kategori** | Hukuki |
| **Açıklama** | Satın alınan TSE standard dokümanı, TSE lisansı gereği çoğaltılamaz ve dağıtılamaz. GitHub deposuna eklenmesi telif ihlali oluşturur. |
| **O / E / Puan** | 1 / 3 / **3** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | `docs/TSE_ISO_IEC_TS_33061/` klasörü `.gitignore` ile depo dışında tutulmuştur. Standardın içeriği proje dokümanlarına kopyalanmaz; yalnızca madde numarasıyla atıf yapılır. Doküman yerel olarak veya kurum içi güvenli bir konumda saklanır. |
| **Durum** | Kapandı — önlem uygulandı (2026-09-03) |

### R-14 — Doğrulama SMS'inin kara liste nedeniyle ulaşmaması
| | |
|---|---|
| **Kategori** | Entegrasyon / Devreye Alma |
| **Açıklama** | Doğrulama kodları NetGSM **standart SMS servisi** üzerinden gönderilecektir (`KR-042`). Standart servis kara liste filtresine tabidir: bir personelin numarası geçmişte NetGSM kara listesine girdiyse (örneğin bir gönderime "RED" yanıtı verdiyse) doğrulama SMS'i kendisine ulaşmaz ve üyeliğini tamamlayamaz. Bu filtre OTP servisinde uygulanmaz, ancak hesapta OTP paketi bulunmamaktadır. |
| **O / E / Puan** | 2 / 2 / **4** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) **Kanal değiştirme akışı:** Kişi LOGO üzerinden doğrulandıktan sonra kendisine "e-posta ile doğrula" ve "SMS ile doğrula" seçenekleri sunulur. SMS'i seçip kod eline ulaşmazsa, aynı ekrandan **e-posta ile doğrulamaya geçebilecektir**. Geçiş otomatik değil, kullanıcının tercihidir. Kanal değiştirildiğinde önceki kod geçersiz kılınır, yeni kod üretilir ve kanal değiştirme denemeleri de hız sınırlamasına tabidir. Tek kanala bağımlılık böylece ortadan kalkar. (2) Her gönderim veritabanına kaydedilecek ve NetGSM rapor servisi ile teslim durumu sorgulanabilecek; ulaşmayan mesajlar tespit edilip İK'ya raporlanacak. (3) Gerekirse kara liste, NetGSM panelinden veya `sms/blacklist` servisiyle sorgulanacak. (4) İhtiyaç doğarsa OTP paketi tanımlatılması yeniden değerlendirilecek. **Güncelleme (2026-10-04):** Önlem (1) T3'te uygulandı: kod ekranında "Başka bir yöntemle doğrula" ile kanal değiştirilir, önceki kod geçersizleşir (PR #86, #88, #91; kabulde KS-02). Her gönderim ve sonucu kaydediliyor (PR #86). Teslim durumu sorgusu ve kara liste raporu (önlem 2–3) henüz yapılmadı. |
| **Durum** | İzleniyor — kanal değiştirme önlemi yürürlükte |

### R-15 — Kapsam büyüklüğünün proje süresini uzatması
| | |
|---|---|
| **Kategori** | Proje Yönetimi (MAN.1 / MAN.2) |
| **Açıklama** | Sistem **35 modülden** oluşacaktır (`KR-045`, `KR-050`, `KR-051`). Geliştirme kaynağı sınırlıdır (tek geliştirme kanalı, `KS8`) ve modül sırası önceden bilinmediği için uzun vadeli takvim öngörülemez. Projenin, mevcut sistemin PostgreSQL 14 desteğinin biteceği tarihi (Kasım 2026) ve kurumun beklentisini aşması riski vardır. |
| **O / E / Puan** | 3 / 2 / **6** |
| **Sahibi** | Bilgi İşlem + Üst Yönetim |
| **Önlem** | (1) **Yatay modüllere öncelik verilecek.** Onay akışı, bildirim, dosya, raporlama, denetim ve çalışma takvimi altyapısı bir kez yazılır; 20 iş modülü aynı altyapıyı kullanır. Bu, tek seferlik değil **20 kez geri dönen** bir yatırımdır (`KR-053`). (2) Modül bağımlılık haritası (`docs/mimari/modul-listesi-ve-bagimliliklar.md`) ile İK'nın ön koşulu tamamlanmamış modül seçmesi engellenir; yeniden çalışma önlenir. (3) Her modül kapanışında hız ölçülür ve kalan modüller için tahmin güncellenir (MAN.2). (4) Kapsam ve takvim beklentisi üst yönetimle dönemsel durum raporlarında paylaşılır. (5) İK onayı beklenirken enine kesen işler yapılarak boş zaman oluşması engellenir. |
| **Durum** | Açık |

### R-16 — Sunucu tarafı dal koruma özelliğinin kullanılamaması
| | |
|---|---|
| **Kategori** | Konfigürasyon Yönetimi (MAN.5) |
| **Açıklama** | Depo **özel** (private) ve organizasyon **GitHub Free** planında olduğu için `main` dalında sunucu tarafı koruma (branch protection ve repository ruleset) etkinleştirilememektedir. Sonuç: `main`'e doğrudan gönderim **teknik olarak engellenememekte**, kod incelemesi ve CI kalite kapıları birleştirme için **zorunlu kılınamamaktadır**. Depoyu herkese açık yapmak, içerik kuruma özel olduğu için seçenek değildir. |
| **O / E / Puan** | 2 / 2 / **4** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) **İstemci tarafı `pre-push` kancası** — `main`'e doğrudan gönderimi engeller (`--no-verify` ile atlanabilir). (2) **İstemci tarafı `commit-msg` kancası** — commit biçimini denetler. (3) **Sunucu tarafı tespit edici CI denetimi** — `main`'e PR olmadan gelen her commit'i yakalar, iş akışını başarısız kılar ve otomatik **düzeltici faaliyet issue'su** açar; bu kontrol atlatılamaz. (4) `CODEOWNERS` ve Tamamlanma Tanımı kontrol listesi. (5) **Kalıcı çözüm: GitHub Team planı** (kullanıcı başına aylık ~4 USD) — karar bekliyor. Ayrıntı: `docs/33061/MAN.5-konfigurasyon-yonetimi/dal-koruma-telafi-kontrolleri.md` |
| **Seviye 2'ye etkisi** | PA 2.2 (c) ve (d) özniteliklerinde `F` (Tam) yerine `L` (Büyük Ölçüde) beklenir. Seviye 2 için PA 2.2'de **`L` yeterlidir** (33020 Madde 5.6); dolayısıyla hedef engellenmemektedir, ancak zayıflık kayıtlıdır. |
| **Karar (2026-09-09)** | Kurum, deponun **özel kalmasına** ve organizasyonun **Free planında devam etmesine** karar vermiştir; GitHub Team planına geçilmeyecektir (`KR-055`). Risk, uygulanan telafi edici kontrollerle **kabul edilmiştir** (risk kabulü). Denetimde `dal-koruma-telafi-kontrolleri.md` belgesi ve CI denetim kayıtları gösterilecektir. |
| **Durum** | **Kabul edildi** — telafi edici kontrollerle izleniyor |

---

## 3. Risk özeti

| Puan | Risk sayısı | Riskler |
|---|---:|---|
| 9 | 1 | R-18 |
| 6 | 8 | R-01, R-03, R-04, R-05, R-06, R-15, R-20, R-25 |
| 4 | 6 | R-08, R-09, R-14, R-16, R-23, R-24 |
| 3 | 1 | R-02 |
| 2 | 5 | R-19, R-21, R-22, R-26, R-27 |
| Kapandı | 6 | R-07, R-10, R-11, R-12, R-13, R-17 |

**Aktif takip gerektiren (puan ≥ 6):** R-01, R-03, R-04, R-05, R-06, R-15, R-18, R-20, R-25

**Kabul edilen riskler:** R-25 (Üst Yönetim, 09.10.2026, puan 6; `KR-103`), R-16 (Bilgi İşlem, puan 4; `KR-055`), R-19 (Bilgi İşlem, puan 2; `KR-078`). İkisinin de puanı 6'nın altında olduğu için kabul yetkisi Bilgi İşlem'dedir. 04.10.2026'daki özet R-16 için yanlışlıkla "Üst Yönetim kararı" yazıyordu (#177).

**Son gözden geçirme:** 2026-10-04 — T3 sonu risk gözden geçirmesi (#126, denetim bulgusu T3-05). **R-01'in özet tablosundaki puanı düzeltildi:** kayıt 2×3 = 6 diyordu, özet 9 sayıyordu. R-03, R-08, R-14 güncellendi. T3 döneminde yaşanan veya ortaya çıkan riskler eklendi: R-19 (`KR-078` kalan riski), R-20 (SMTP sertifikası), R-21 (sunucu saati), R-22 (güvenilen vekil ağı), R-23 (bağımsız olmayan inceleme), R-24 (UAT kapasitesi), R-25 (UAT'de gerçek veri ve gerçek gönderim), R-26 (yerel çalışma kaybı). T3 boyunca deftere risk eklenmemişti; olayların da gözden geçirmeyi tetiklemesi §1'e yazıldı.

Önceki: 2026-09-22 — İK'nın ikinci düzeltme turundan sonra ölçüm yenilendi. `R-12` **kapandı** (paylaşılan adres 4 kişi / 1 adres → 0). `R-08` ölçümü güncellendi: hiçbir iletişim bilgisi olmayan kişi **4 → 2**; puan 4'te tutuldu.
Önceki: 2026-09-21 — LOGO verisi, İK'nın düzeltme çalışmasından sonra yeniden ölçüldü. `R-13` **kapandı** (kişisel e-posta 59 → 0); `R-08` puanı 6'dan 4'e düştü (hiçbir iletişim bilgisi olmayan kişi 12 → 4); `R-12` değişmedi (4 kişi / 1 adres).
Önceki: 2026-09-16 — `R-18` (elle sertifika yenilemesi) eklendi; R-17'nin önlemi Let's Encrypt / DNS-01 kararıyla güncellendi. Bu gözden geçirmede, R-17 eklenirken **özet tablosunun güncellenmediği** fark edildi ve düzeltildi: kayıt eklemek yeterli değildir, özet de aynı anda güncellenmelidir.
Önceki: 2026-09-08 — `R-16` (dal koruma kısıtı) eklendi; GitHub kurulumu sırasında tespit edildi.
Önceki: 2026-09-07 — `R-15` (kapsam büyüklüğü) eklendi; `R-02`'nin
olasılığı, LOGO sürüm yükseltmelerinin TRISOFT tarafından önceden bildirildiği ve geçmiş
yükseltmelerde majör şema değişikliği olmadığı bilgisiyle **6'dan 3'e düşürüldü**.
Önceki: 2026-09-06 — R-14 (SMS kara listesi) eklendi.
Önceki: 2026-09-05 — R-07 kapandı; R-08, R-12, R-13 güncel ölçümlerle güncellendi.

---

### R-17 — UAT ortamının şifrelenmemiş bağlantı (HTTP) üzerinden yayımlanması
| | |
|---|---|
| **Kategori** | Güvenlik |
| **Açıklama** | UAT ortamı `http://insankaynaklaritest.duzen.com.tr` adresinde **düz HTTP** ile yayımlanmaktadır. Şu an uygulamada oturum açma bulunmadığı için taşınan veri yoktur; ancak **T3 Kimlik Yönetimi devreye girdiğinde** parolalar, doğrulama kodları ve oturum çerezleri aynı ağdaki bir dinleyici tarafından okunabilir hâle gelir. Ayrıca yenileme jetonu çerezi `Secure` işaretiyle tanımlanacağından (ADR-0006 §8) HTTP üzerinde **hiç çalışmaz** — yani TLS olmadan modül teknik olarak da kullanılamaz. |
| **O / E / Puan** | 3 / 3 / **9** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) **Let's Encrypt** sertifikası ile UAT ve üretim ortamlarında **TLS zorunlu** kılınacak; HTTP isteği HTTPS'e yönlendirilecek. Kurum içi (self-signed) sertifika yerine Let's Encrypt seçilmiştir: her istemciye elle kök sertifika dağıtma yükü doğmaz. Doğrulama **DNS-01** yöntemiyle yapılır; bu yöntem sunucunun internete açılmasını gerektirmez, dolayısıyla "yalnızca kurum içi erişim" kısıtı korunur. (2) Bu iş **T3 Kimlik Yönetimi devreye alınmadan önce** tamamlanacaktır — modülün ön koşuludur. (3) Yenileme riski ayrıca **R-18** altında izlenir. |
| **Durum** | **Kapandı — 17.09.2026.** UAT `https://insankaynaklaritest.duzen.com.tr` üzerinden yayında; HTTP 301 ile yönlendiriliyor, sertifika doğrulaması `curl` ile sınandı (`ssl_verify_result=0`). Yenileme riski **R-18** altında devam eder. |

---

### R-18 — TLS sertifikası yenilemesinin elle yapılması nedeniyle süresinin dolması
| | |
|---|---|
| **Kategori** | Kullanılabilirlik |
| **Açıklama** | Let's Encrypt sertifikaları **90 gün** geçerlidir. Kurum DNS yöneticisiyle yapılan görüşmede, DNS TXT kaydının otomatik güncellenmeyeceği, yenileme zamanı yaklaştığında **elle güncelleneceği** kararlaştırılmıştır (2026-09-16). Böylece yenileme, otomatik bir işin değil bir kişinin hatırlamasına bağlı hâle gelir. Sertifika süresi dolduğunda tarayıcı bağlantıyı engeller ve sistem **tamamen erişilemez** olur — kısmi değil, tam kesinti. Risk 90 günde bir tekrarlanır; tek bir unutma yeterlidir. |
| **O / E / Puan** | 3 / 3 / **9** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) **Sistem içi uyarı** (asıl önlem): Y4 Sistem Yönetimi modülünde sertifikanın kalan geçerlilik günü izlenir; eşiğin (varsayılan 10 gün) altına düştüğünde sistem yöneticisine **e-posta ve SMS** gönderilir — bkz. gereksinim #42. Uyarı, **sunulan** sertifikadan okunur; böylece "dosya yenilendi ama nginx yeniden yüklenmedi" durumu da yakalanır. (2) Alıcı tek kişi değil, **sistem yöneticisi rolüdür**; tek kişinin izinli olması uyarıyı kör etmez. (3) **Ara dönem** (Y4 hazır olana kadar): sertifikanın bitiş tarihi UAT runbook'una yazılır ve Bilgi İşlem takvim hatırlatmasıyla takip eder. (4) **HSTS bilinçli olarak kapalı tutulur**: sertifika kaçırılırsa kullanıcı uyarıyı atlayarak sisteme erişebilir; HSTS bunu imkânsız kılardı. HSTS ancak yenileme otomatikleştiğinde açılacaktır. |
| **Durum** | Açık — ara dönem önlemi yürürlükte; asıl önlem A3 aşamasında (#42) |

---

### R-19 — Üyelikte bilgilerin eşleştiğinin dolaylı olarak anlaşılması (`KR-078`)
| | |
|---|---|
| **Kategori** | Güvenlik / KVKK |
| **Açıklama** | Üyelikte bilgiler eşleşmese de kanal seçim ekranı gösterilir (REQ-KMLK-004). Ancak cep telefonu kayıtlı olmayan kişiye yalnızca e-posta kanalı sunulur (REQ-KMLK-010). İki kural birlikte, telefonu olmayan personel için (22.09.2026: 10 kişi) girilen bilgilerin eşleştiğini dolaylı olarak gösterir. |
| **O / E / Puan** | 1 / 2 / **2** |
| **Puan gerekçesi** | **Olasılık 1:** Farkı görebilmek için kişinin T.C. Kimlik Numarası, doğum tarihi ve kurumsal e-postasının **üçünün birden** doğru bilinmesi gerekir; üyelik denemeleri kişi ve IP başına hız sınırına tabidir (SYG-KMLK-059). **Etki 2:** Öğrenilen tek bilgi kişinin kurumda çalıştığıdır. Hesaba erişim sağlanmaz: kod yine kişinin kendi adresine gider. |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | Onaylı gereksinim metni korunur. Kalan risk T3 kabulünde İK'ya gösterilir (kabul planı KS-04 notu, kabul formu §4). Telefonu olmayan kişi sayısı azaldıkça risk de küçülür (R-08). |
| **Durum** | **Kabul edildi** — Bilgi İşlem, 25.09.2026 (`KR-078`). Puan 6'nın altında olduğu için kabul yetkisi RACI'ye göre Bilgi İşlem'dedir; puan ve gerekçesi 04.10.2026'da kayda geçti; puanı Doğuş Uçanok 05.10.2026'da onayladı (#126) |

### R-20 — SMTP sunucusu sertifikasının süresinin dolması
| | |
|---|---|
| **Kategori** | Entegrasyon / Kullanılabilirlik |
| **Açıklama** | Doğrulama kodları ve davet bağlantıları `mail.duzen.com.tr:587` üzerinden gönderilir. Gönderici sunucu sertifikasını doğrular; doğrulama kapatılmaz (runbook §10.5). `mail.duzen.com.tr` sertifikasının geçerliliği **11.10.2026**'da bitiyor. Yenilenmezse veya yenilenip Postfix yeniden yüklenmezse **e-posta kanalı tamamen durur**: e-postayla üyelik, parola sıfırlama ve davet yapılamaz. 27.09.2026'da 587 portunun süresi dolmuş, kendinden imzalı bir sertifika sunduğu görülmüştü; bu durum sunucu tarafında düzeltildi. |
| **O / E / Puan** | 2 / 3 / **6** |
| **Sahibi** | Bilgi İşlem (e-posta sunucusu yöneticisi) |
| **Önlem** | (1) Sertifika bitişten 2–3 gün önce yenilenir (Doğuş Uçanok, 05.10.2026; ilk yenileme 06.10.2026 öncesinde yapıldı); ardından Postfix yeniden yüklenecek ve runbook §10.5'teki `openssl` denetimi çalıştırılacak. (2) API'nin hazır olma kontrolü e-posta kanalını `Unhealthy` gösterir; dağıtım sonrası ve kabul öncesinde bakılır. (3) Kalıcı önlem: R-18'deki sertifika uyarısı (Y4) SMTP sertifikasını da kapsayacak. |
| **Durum** | İzleniyor — sertifika 06.10.2026'de yenilenmiş bulundu: Sectigo, geçerlilik 04.10.2026–**27.03.2027**; 587 portunda doğrulama `0 (ok)`; UAT e-posta kanalı Healthy (#179). Sonraki yenileme bitişten 2–3 gün önce |

### R-21 — Sunucu saatinin sapması
| | |
|---|---|
| **Kategori** | Altyapı |
| **Açıklama** | **Gerçekleşti (29.09.2026, #103):** UAT sunucusunun saati yaklaşık 10 dakika gerideydi; NTP eşitlemesi çalışmıyordu. Kod ekranı açılır açılmaz "süresi doldu" diyordu. Saat sapması ayrıca denetim izindeki ve günlüklerdeki zamanları kaydırır, oturum ve kod sürelerini bozar. |
| **O / E / Puan** | 1 / 2 / **2** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) Sunucuda düz NTP kaynakları tanımlandı (runbook, NTP bölümü). (2) İstemci, geri sayımları sunucu saatine göre düzeltir (PR #104); tarayıcı veya sunucu saati sapsa da süreler doğru gösterilir. (3) NTP eşitlemesi konfigürasyon öğeleri listesine ve üretim kurulum denetimine alınacak (#132). |
| **Durum** | İzleniyor — önlemler yürürlükte |

### R-22 — Güvenilen vekil ağının yanlış yapılandırılması
| | |
|---|---|
| **Kategori** | Güvenlik / Kullanılabilirlik |
| **Açıklama** | API, istemci IP'sini ve bağlantı şemasını yalnızca güvenilen ağdan (`ReverseProxy__TrustedNetworks`, UAT'de `172.16.0.0/12`) gelen başlıklardan okur. Docker ağı bu aralığın dışına düşerse (a) tüm API istekleri `403 https-required` ile reddedilir, (b) denetim izine ve IP başına hız sınırına gerçek istemci yerine nginx'in adresi yazılır. Aralık gereğinden geniş tanımlanırsa sahte `X-Forwarded-For` başlığıyla IP sınırı aşılabilir. |
| **O / E / Puan** | 1 / 2 / **2** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) Runbook §10.4'teki ağ denetimi kurulumda uygulanır. (2) API konteyneri dışarıya kapalıdır; başlıkları yalnızca aynı Docker ağındaki nginx gönderebilir (dağıtım betiği dış erişimi sınar). (3) (a) durumu kurulumdan hemen sonra fark edilir: hiçbir ekran çalışmaz. |
| **Durum** | İzleniyor |

### R-23 — Kod incelemesinin bağımsız olmaması
| | |
|---|---|
| **Kategori** | Kalite Güvencesi (MAN.8) |
| **Açıklama** | Geliştirmeyi ve incelemeyi aynı kişi yürütüyor; PR'ı açan ve birleştiren aynı hesap. İnceleme, geliştirme yardımcısının (Claude) ürettiği kodun ve belgenin tek bir kişi tarafından gözden geçirilmesine dayanıyor. Gözden kaçan bir hatayı ikinci bir göz yakalamaz. |
| **O / E / Puan** | 2 / 2 / **4** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) İnceleme onayı birleştirmeden önce PR'a yazılır; CI denetler (`KR-096`, #128). (2) Otomatik kapılar: derleme, birim, entegrasyon ve uçtan uca testler, kapsam eşiği, mimari testi, gereksinim izlenebilirliği, statik analiz, güvenlik taramaları (ADR-0011 §6). (3) Modül sonunda süreç denetimi yapılır. (4) İK kabulü ürünü geliştiriciden bağımsız bir gözle sınar (TEC.11). |
| **Durum** | İzleniyor |

### R-24 — UAT sunucusunun işlemci kapasitesi
| | |
|---|---|
| **Kategori** | Altyapı / Performans |
| **Açıklama** | UAT sunucusu 2 çekirdeklidir. Giriş başına ≈180 ms işlemci süresi parola özetine harcanır. Aynı anda 50 girişte p95 4,42 sn ölçüldü; üst sınır 5 sn (SYG-KMLK-077, `KR-095`). Sınıra yakındır: üretim sunucusu aynı boyutta kurulursa veya eş zamanlı giriş sayısı artarsa sınır aşılır. |
| **O / E / Puan** | 2 / 2 / **4** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) UAT'nin çekirdek sayısı azaltılmayacak. (2) Üretim sunucusu boyutlandırılırken performans raporu (`TEC.9-dogrulama/raporlar/2026-10-02-t3-performans-olcumu.md`) esas alınacak ve ölçüm üretimde tekrarlanacak. (3) Kurumun eş zamanlı giriş profili değişirse ölçüm yenilenecek. |
| **Durum** | İzleniyor |

### R-25 — UAT'nin gerçek kişisel veriyle ve gerçek ileti gönderimiyle çalışması
| | |
|---|---|
| **Kategori** | KVKK / Güvenlik |
| **Açıklama** | Kabul testlerinin gerçek personelle yapılabilmesi için UAT, LOGO'dan gerçek personel verisini okur; maskeleme uygulanmaz (R-03 önlem 6'nın istisnası). SMTP ve NetGSM gerçektir. Yanlış yapılandırılmış bir gönderim kipi gerçek personele doğrulama kodu veya davet gönderebilir. UAT'ye yetkisiz erişim, gerçek personel verisinin ifşası demektir. |
| **O / E / Puan** | 2 / 3 / **6** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) UAT'de ileti gönderimi `AllowList` kipindedir; yalnızca listedeki adreslere gider (`KR-083`). Liste kabul katılımcılarıyla sınırlı tutulur ve oturumdan sonra daraltılır. (2) UAT yalnızca kurum içinden erişilebilir ve TLS ile yayındadır; API ve veritabanı dışarıya kapalıdır. (3) Günlükte kişisel veri maskelenir; hesap işlemleri ekranı yalnızca ad, sicil ve firmayı gösterir. (4) Sırlar kaynak ağacının dışında tutulur; dağıtım yerel ortam dosyalarını sunucuya taşımaz (#125). (5) SSH (06.10.2026, `KR-102`, #174): erişim anahtarla yapılıyor ve sunucu parolası artık yazışmaya girmiyor; `root` parolası değiştirildi. Parola ile giriş, erişimin tamamen kaybolmaması için bilerek açık bırakıldı. UAT yedekleme kuralı `KR-098`. (6) Sunucuda belgelenmemiş Webmin yönetim paneli (10000 portu, ağa açık) bulundu ve 06.10.2026'de kaldırıldı (#177, #179); dışarıya açık portlar 22, 80, 443 |
| **Durum** | **Kabul edildi** — Üst Yönetim, Elvan Laleli Şahin (Yönetim Kurulu Üyesi), 09.10.2026 (`KR-103`; onay kaydı `MAN.1-proje-planlama/kayitlar/2026-10-06-plan-ve-raci-onay-talebi.md`). Önlemler izlenmeye devam eder |

### R-26 — Geliştirme makinesindeki commit edilmemiş çalışmanın kaybı
| | |
|---|---|
| **Kategori** | Konfigürasyon Yönetimi (MAN.5) |
| **Açıklama** | **Gerçekleşti (30.09.2026):** `git stash -u` sırasında izlenmeyen boş klasörler kayboldu; PR #118'de geri getirildi. Commit edilmemiş çalışma tek makinede durur; yanlış bir Git komutu veya disk arızası onu geri getirilemez biçimde siler. |
| **O / E / Puan** | 1 / 2 / **2** |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) `git stash -u` ve `git clean -d` kullanılmaz; ara çalışma dalda WIP commit'i olarak saklanır. (2) Boş klasörler `.gitkeep` ile izlenir. (3) Dallar sık sık GitHub'a gönderilir. (4) Olay kaydı #129'da yazılır. |
| **Durum** | İzleniyor |

### R-27 — Doğrulama kanallarını kaybeden kullanıcının iki adımlı girişte kalması
| | |
|---|---|
| **Kategori** | Kullanılabilirlik |
| **Açıklama** | 2FA kullanıcı tercihine bağlıdır (`KR-104`, #190). Kendi 2FA'sını açmış bir kullanıcı hem kurumsal e-postasını hem cep telefonunu kaybederse (numara değişti, e-posta kapandı) kod alamaz ve giriş yapamaz. Parola sıfırlama 2FA'yı kapatmaz. |
| **O / E / Puan** | 1 / 2 / **2** |
| **Puan gerekçesi** | **Olasılık 1:** İki kanalın birden aynı anda kaybedilmesi gerekir ve 2FA varsayılan kapalıdır. **Etki 2:** Tek kişi etkilenir; veri kaybı yoktur. |
| **Sahibi** | Bilgi İşlem |
| **Önlem** | (1) İletişim bilgisi LOGO'da güncellenince senkronizasyonla HRMS'e gelir; kişi yeni kanalıyla kod alabilir. (2) **Kurtarma:** İK, "Hesap işlemleri" ekranından kişinin 2FA'sını gerekçe girerek kapatır; kişi parolasıyla girer (SYG-KMLK-081). İK, talep edenin kişinin kendisi olduğunu yüz yüze veya bilinen bir kurum içi kanaldan doğrular; işlem denetim izine ve güvenlik olaylarına yazılır. (3) Son çare: Sistem Yöneticisi PRM-KML-08'i geçici olarak kapatır. |
| **Durum** | İzleniyor; kurtarma işlemi eklendi (#190) |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-03 | 0.1 | İlk oluşturma; R-01…R-11 kayıtları | Bilgi İşlem |
| 2026-09-12 | 0.4 | R-12 ölçümü düzeltildi: sicil bazlı sayım nedeniyle 37 kişi görünmüştü; kişi (TCKN) bazlı doğru değer **4 kişi / 1 adres**. Puan 6 olarak kaldı | Bilgi İşlem |
| 2026-09-15 | 0.5 | R-17 eklendi (UAT ortamında TLS bulunmaması) | Bilgi İşlem |
| 2026-09-16 | 0.6 | R-17 önlemi güncellendi (Let's Encrypt / DNS-01 kararı); **R-18** eklendi (elle yenileme nedeniyle sertifika süresinin dolması) | Bilgi İşlem |
| 2026-09-17 | 0.7 | **R-17 kapandı** (UAT TLS ile yayında). R-18 açık kalmaya devam ediyor | Bilgi İşlem |
| 2026-09-21 | 0.8 | LOGO verisi yeniden ölçüldü: **R-13 kapandı** (kişisel e-posta 59 → 0); R-08 ölçümü güncellendi ve puanı 6 → 4 düştü (12 → 4 kişi); R-12 değişmedi | Bilgi İşlem |
| 2026-09-22 | 0.9 | İK ikinci düzeltme turu: **R-12 kapandı** (paylaşılan adres → 0); R-08 ölçümü güncellendi (4 → 2 kişi) | Bilgi İşlem |
| 2026-10-04 | 1.0 | T3 sonu gözden geçirme (#126): R-01 özet puanı düzeltildi; "Kabul edildi" durumu ve kabul yetkisi kuralı §1'e yazıldı; R-03, R-08, R-14 güncellendi; R-19…R-26 eklendi | Bilgi İşlem |
| 2026-10-06 | 1.1 | Kabul edilen riskler satırında R-16 düzeltildi (Bilgi İşlem, `KR-055`); R-25 önlem 5–6: SSH kararı ve Webmin bulgusu (#177) | Bilgi İşlem |
| 2026-10-06 | 1.2 | R-20: sertifika yenilendi (bitiş 27.03.2027), durum İzleniyor; R-25: Webmin kaldırıldı (#179) | Bilgi İşlem |
| 2026-10-10 | 1.3 | R-25 Üst Yönetim tarafından kabul edildi (`KR-103`, #127) | Bilgi İşlem |
| 2026-10-10 | 1.4 | R-27 eklendi: kanallarını kaybeden kullanıcının 2FA ile girişte kalması; önlem İK'nın kurtarma işlemi (#190) | Bilgi İşlem |
