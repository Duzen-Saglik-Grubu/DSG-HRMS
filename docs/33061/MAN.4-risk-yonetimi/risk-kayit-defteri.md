# Risk Kayıt Defteri

**Belge kimliği:** MAN.4-RKD
**Son güncelleme:** 2026-09-11
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

**Durum değerleri:** `Açık`, `İzleniyor`, `Kapandı`, `Gerçekleşti`

---

## 2. Risk Kayıtları

### R-01 — Eski HRMS'ten veri göçünün eksik veya hatalı olması
| | |
|---|---|
| **Kategori** | Veri / Geçiş (TEC.10) |
| **Açıklama** | Mevcut sistemdeki izin, eğitim ve sertifika verilerinin yeni sisteme eksiksiz aktarılamaması. Kaynak veride tutarsızlık, boş alan veya kural dışı kayıt bulunması. |
| **O / E / Puan** | 3 / 3 / **9** |
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
| **Önlem** | (1) Özel nitelikli alanlarda uygulama seviyesinde şifreleme. (2) Log ve denetim kayıtlarında TCKN/IBAN/iletişim bilgisi **maskeleme**. (3) Görüntüleme (erişim) logu tutulacak — sadece değişiklik logu yeterli değildir. (4) Satır bazlı yetkilendirme: kullanıcı yalnızca yetkili olduğu personeli görebilir. (5) Veri türü bazında yapılandırılabilir saklama süresi ve süre sonunda arşivleme/anonimleştirme yeteneği. (6) UAT ortamına gerçek veri kopyalanacaksa maskeleme uygulanacak. |
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
| **Açıklama** | 583 aktif personelin **38'inde kurumsal e-posta**, **13'ünde cep telefonu** kayıtlı değildir; **12'sinde ise ikisi de yoktur**. Bu 12 kişi **hiçbir kanaldan** doğrulama kodu alamaz ve kendi başına üye olamaz. Ayrıca 1 kişinin telefon numarası geçerli cep biçiminde değildir. Kurumsal adresi olmayanlar için SMS doğrulaması zorunlu kılınmadığından (`KR-020`), kalıcı çözüm veri düzeltmesidir. (Bkz. `docs/analiz/01-mevcut-sistem-veri-envanteri.md` §1.5) |
| **O / E / Puan** | 3 / 2 / **6** |
| **Sahibi** | İK Birimi |
| **Önlem** | (0) **Öncelik: iletişim bilgisi hiç olmayan 12 kişi.** Bu kişiler tamamlanmadan sisteme giremezler. (1) İK, devreye alma öncesinde eksik iletişim bilgilerini LOGO'da tamamlayacak. (2) Sistem, eksik iletişim bilgisi olan aktif personeli listeleyen bir yönetim raporu sunacak. (3) Hiçbir iletişim bilgisi olmayan personel için İK'nın gerekçe girerek hesap açabildiği, tam loglanan **istisna akışı** tasarlanacak (Kullanıcı Yönetimi modülü). |
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
| **Açıklama** | **2026-09-11 ölçümü: 13 e-posta adresi, 37 aktif personele tanımlıdır; bir adres 10 kişiye aittir.** (İlk ölçümde 2 adres / 4 kişi görünmüştü; kapsamlı ölçüm sorunun çok daha büyük olduğunu gösterdi.) Üyelik doğrulama kodu bu adrese gideceği için, posta kutusuna erişimi olan kişi **başkasının adına hesap açabilir** ve o kişinin özlük verilerine erişebilir. Kontrol tüm aktif personel üzerinde çalıştırılmıştır, belirli bir görev grubuyla sınırlı değildir. (Bkz. `docs/analiz/01-mevcut-sistem-veri-envanteri.md` §1.6) |
| **O / E / Puan** | 3 / 3 / **9** |
| **Sahibi** | İK Birimi + Bilgi İşlem |
| **Önlem** | (1) İK, ilgili personellere kendilerine ait ayrı e-posta adresi tanımlayacak. **Bu, Kimlik Yönetimi modülünün devreye alınması için ön koşuldur.** (2) Sistem, üyelik akışında **e-posta adresinin birden fazla kişiye tanımlı olması durumunda o adrese doğrulama kodu göndermeyecek**, kaydı istisna akışına yönlendirecek. (3) Senkronizasyon, paylaşılan e-posta adreslerini tespit edip uyarı raporunda listeleyecek. |
| **Durum** | Açık |

### R-13 — Kurumsal olmayan e-posta adresine doğrulama kodu gönderilmesi
| | |
|---|---|
| **Kategori** | Güvenlik |
| **Açıklama** | 584 aktif personelin **59'unda** LOGO'da kayıtlı e-posta adresi kişisel bir adrestir (gmail, hotmail, icloud, yahoo). Doğrulama kodu kurum denetimi dışındaki bir posta kutusuna gider. Personel işten ayrıldıktan sonra da bu kutuya erişimi sürer; kurum erişimi kesemez. (Bkz. `docs/analiz/01-mevcut-sistem-veri-envanteri.md` §1.6) |
| **O / E / Puan** | 3 / 2 / **6** |
| **Sahibi** | İK Birimi + Bilgi İşlem |
| **Önlem** | (1) İK, kurumsal e-posta adresi olmayan personele kurumsal adres tanımlayacak. Kalıcı çözüm veri düzeltmesidir; kurumsal adresi olmayanlar için SMS doğrulaması **zorunlu kılınmayacaktır** (`KR-020`). (2) Sistem, kabul edilen kurumsal alan adlarını (`@duzen.com.tr`, `@zeytinim.com`, `@labpt.com.tr`) **yapılandırılabilir bir liste** olarak tutacak; liste dışı adreslere e-posta ile doğrulama kodu gönderilmeyecek. (3) Bu kural Kullanıcı Yönetimi modülü gereksinimlerinde karara bağlanacaktır. |
| **Durum** | Açık |

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
| **Önlem** | (1) **Kanal değiştirme akışı:** Kişi LOGO üzerinden doğrulandıktan sonra kendisine "e-posta ile doğrula" ve "SMS ile doğrula" seçenekleri sunulur. SMS'i seçip kod eline ulaşmazsa, aynı ekrandan **e-posta ile doğrulamaya geçebilecektir**. Geçiş otomatik değil, kullanıcının tercihidir. Kanal değiştirildiğinde önceki kod geçersiz kılınır, yeni kod üretilir ve kanal değiştirme denemeleri de hız sınırlamasına tabidir. Tek kanala bağımlılık böylece ortadan kalkar. (2) Her gönderim veritabanına kaydedilecek ve NetGSM rapor servisi ile teslim durumu sorgulanabilecek; ulaşmayan mesajlar tespit edilip İK'ya raporlanacak. (3) Gerekirse kara liste, NetGSM panelinden veya `sms/blacklist` servisiyle sorgulanacak. (4) İhtiyaç doğarsa OTP paketi tanımlatılması yeniden değerlendirilecek. |
| **Durum** | Açık |

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
| 9 | 1 | R-01 |
| 6 | 8 | R-03, R-04, R-05, R-06, R-08, R-12, R-13, R-15 |
| 4 | 3 | R-09, R-14, R-16 |
| 3 | 1 | R-02 |
| Kapandı | 3 | R-07, R-10, R-11 |

**Aktif takip gerektiren (puan ≥ 6):** R-01, R-03, R-04, R-05, R-06, R-08, R-12, R-13, R-15

**Son gözden geçirme:** 2026-09-08 — `R-16` (dal koruma kısıtı) eklendi; GitHub kurulumu sırasında tespit edildi.
Önceki: 2026-09-07 — `R-15` (kapsam büyüklüğü) eklendi; `R-02`'nin
olasılığı, LOGO sürüm yükseltmelerinin TRISOFT tarafından önceden bildirildiği ve geçmiş
yükseltmelerde majör şema değişikliği olmadığı bilgisiyle **6'dan 3'e düşürüldü**.
Önceki: 2026-09-06 — R-14 (SMS kara listesi) eklendi.
Önceki: 2026-09-05 — R-07 kapandı; R-08, R-12, R-13 güncel ölçümlerle güncellendi.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-03 | 0.1 | İlk oluşturma; R-01…R-11 kayıtları | Bilgi İşlem |
| 2026-09-11 | 0.4 | R-12 yeniden ölçüldü: 4 kişi → 37 kişi; puan 6 → 9 (A1 kapanış değerlendirmesi) | Bilgi İşlem |
