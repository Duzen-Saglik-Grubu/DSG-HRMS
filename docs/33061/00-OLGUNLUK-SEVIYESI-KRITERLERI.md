# Olgunluk Seviyesi Kriterleri ve Öz Değerlendirme

**Belge kimliği:** 33061-OSK
**Son güncelleme:** 2026-09-18
**Hedef:** TS ISO/IEC TS 33061 kapsamındaki süreçlerde **Yetenek Seviyesi 2 (Yönetilen Süreç)**
**Dayanak:** TS ISO/IEC TS 33061 (süreç boyutu) + TS ISO/IEC 33020 (ölçüm çerçevesi, Madde 5)

> **Neden iki standart?** TS ISO/IEC TS 33061 yalnızca **süreç boyutunu** tanımlar: her sürecin
> amacı, çıktıları (outcomes), temel uygulamaları (base practices) ve süreç ürünleri.
> "Seviye 2" tanımı ise **TS ISO/IEC 33020**'de yer alır: süreç öznitelikleri, derecelendirme
> ölçeği ve seviye modeli. İkisi birlikte kullanılır.
>
> **Telif uyarısı:** Her iki standart da TSE lisanslıdır, depoya eklenmemiştir. Bu belgede
> standart metni kopyalanmamış; kriterler kendi ifademizle özetlenmiş ve madde numarasıyla
> atıf yapılmıştır.

---

## 1. Seviye 2 ne demek?

TS ISO/IEC 33020 Madde 5.2.4'e göre Seviye 2 (**Yönetilen Süreç**), Seviye 1'de "yapılan"
sürecin artık **planlanan, izlenen ve gerektiğinde düzeltilen** bir biçimde yürütülmesi ve
sürecin ürettiği **dokümante edilmiş bilginin uygun şekilde oluşturulup kontrol edilmesi**
anlamına gelir.

Seviye 2'ye ulaşmak için sürecin üç özniteliği sağlaması gerekir (Madde 5.6, Tablo 1):

| Süreç Özniteliği | Gerekli Derece |
|---|---|
| **PA 1.1** — Süreç Performansı | **Tam (F)** — %85'in üzeri |
| **PA 2.1** — Performans Yönetimi | **Büyük Ölçüde veya Tam (L/F)** — %50'nin üzeri |
| **PA 2.2** — Dokümante Edilmiş Bilgi Yönetimi | **Büyük Ölçüde veya Tam (L/F)** — %50'nin üzeri |

### ⚠️ Kritik nokta — kapsam yanlış anlaşılmamalı

Proje kapsam tablomuzda süreçler "Seviye 1" ve "Seviye 2" başlıkları altında listelenmiştir.
Bu ayrım, **hangi süreçlerin değerlendirmeye dâhil olduğunu** gösterir; hangi süreçlerin hangi
özniteliği sağlayacağını değil.

**Seviye 2 hedeflendiğinde, kapsamdaki 15 sürecin tamamı** — TEC.2, TEC.3, TEC.5, TEC.7,
TEC.8, TEC.10, TEC.11, TEC.13 dâhil — PA 1.1, PA 2.1 ve PA 2.2 özniteliklerinin
tamamını yukarıdaki derecelerde sağlamak zorundadır. "Seviye 1 süreçleri için sadece
PA 1.1 yeterli" şeklinde bir okuma hatalıdır ve değerlendirmede seviye kaybına yol açar.

---

## 2. Derecelendirme ölçeği (33020, Madde 5.3)

| Kod | Anlam | Başarı yüzdesi |
|---|---|---|
| **N** | Sağlanmadı | %0 – %15 |
| **P** | Kısmen sağlandı | %15 – %50 |
| **L** | Büyük ölçüde sağlandı | %50 – %85 |
| **F** | Tam sağlandı | %85 – %100 |

İhtiyaç hâlinde P ve L dereceleri `P−/P+` (%15–32,5 / %32,5–50) ve `L−/L+`
(%50–67,5 / %67,5–85) olarak ayrıştırılabilir.

**Yorum:** "L" derecesi, *sistematik bir yaklaşımın var olduğu ancak bazı zayıflıkların
bulunduğu* durumdur. "F" ise *eksiksiz ve sistematik bir yaklaşım, kayda değer zayıflık yok*
demektir. Bizim için pratik sonuç: PA 1.1'de zayıflık kabul edilmez, PA 2.1 ve PA 2.2'de
sınırlı zayıflık tolere edilir — ama yaklaşımın sistematik olduğu **kanıtlanabilmelidir**.

---

## 3. PA 1.1 — Süreç Performansı

**Tanım (33020, 5.2.3.2):** Sürecin amacına ulaşma derecesinin ölçüsüdür.

**Tek çıktısı:** Süreç, tanımlı süreç çıktılarına (outcomes) ulaşır.

**Bizdeki karşılığı:** Her süreç için TS ISO/IEC TS 33061 Madde 5'te sayılan
*process output* kalemlerinin üretilmiş olması. Bunların nerede tutulduğu
`docs/00-DOKUMAN-HARITASI.md` belgesinde süreç süreç tablolanmıştır.

**Kanıt:** İlgili süreç klasöründeki dosyaların varlığı, güncelliği ve içeriğinin
sürecin amacını karşılaması.

---

## 4. PA 2.1 — Performans Yönetimi

**Tanım (33020, 5.2.4.2):** Süreç performansının, gerekli kaynak ve yetkinliklerle
yönetilme derecesinin ölçüsüdür. Yedi çıktısı vardır.

| # | 33020 çıktısı | Bu projedeki karşılığı | Kanıt konumu |
|---|---|---|---|
| a | Ulaşılacak sonuçlar belirlenir ve duyurulur | Her modül için yazılı gereksinim ve kabul kriteri; modül hedefleri Milestone olarak tanımlanır | `TEC.2/paydas-gereksinimleri/`, GitHub Milestone |
| b | Süreç performansını etkileyebilecek riskler belirlenir ve ele alınır | Risk kayıt defteri; her modül başlangıcında gözden geçirilir | `MAN.4-risk-yonetimi/risk-kayit-defteri.md` |
| c | Süreç performansı planlanır, izlenir, ölçülür, değerlendirilir ve gerektiğinde düzeltilir | Proje planı + GitHub Projects panosu + dönemsel durum raporları | `MAN.1-proje-planlama/proje-plani.md`, `MAN.2/raporlar/` |
| d | Sorumluluk ve yetkiler belirlenir, atanır ve duyurulur | RACI tablosu; GitHub'da issue/PR sahipliği ve dal koruma kuralları | `MAN.1-proje-planlama/roller-ve-sorumluluklar.md` |
| e | Gerekli kaynaklar belirlenir, sağlanır ve sürdürülür | Proje planında altyapı ve kaynak bölümü (Dev/UAT/Üretim ortamları, NAS, SMS, sunucular) | `MAN.1-proje-planlama/proje-plani.md` |
| f | Süreci yürütenler eğitim/deneyim temelinde yetkindir | Yetkinlik ve rol tanımı; kullanılan teknoloji ve araçlara ilişkin yetkinlik kaydı | `MAN.1-proje-planlama/roller-ve-sorumluluklar.md` |
| g | Taraflar arası arayüzler etkin iletişim ve beklenen kontrol düzeyi için yönetilir | İK ↔ Bilgi İşlem toplantı kayıtları; kabul (UAT) süreci; karar geçmişi kayıtları | `TEC.2/kayitlar/`, `TEC.11/kayitlar/`, `docs/karar-kayit-defteri.md` |

> **En sık kaybedilen puan:** (c) ve (f). "Planladık" demek yetmez; **izlendiğinin ve
> gerektiğinde düzeltildiğinin kaydı** gerekir. Bu yüzden dönemsel durum raporu ve
> modül kapanış gözden geçirmesi zorunlu tutulmuştur.

---

## 5. PA 2.2 — Dokümante Edilmiş Bilgi Yönetimi

**Tanım (33020, 5.2.4.3):** Süreç yürütülürken üretilen veya dışarıdan alınan dokümante
edilmiş bilginin uygun şekilde yönetilme derecesinin ölçüsüdür. Beş çıktısı vardır.

| # | 33020 çıktısı | Bu projedeki karşılığı | Kanıt konumu |
|---|---|---|---|
| a | Sürecin dokümante edilmiş bilgi gereksinimleri belirlenir | Her süreç klasöründeki `YAKLASIM.md`, hangi belgelerin üretileceğini tanımlar | `33061/<SÜREÇ>/YAKLASIM.md` |
| b | Bu bilginin kontrol gereksinimleri belirlenir | Doküman kuralları (kimliklendirme, sürüm, onay, saklama) | `docs/00-DOKUMAN-HARITASI.md` §4 |
| c | Bilgi uygun şekilde tanımlanır ve gereksinimlere göre kontrol edilir | Git sürüm kontrolü; dal koruma; belge kimlik şeması (`PG-`, `REQ-`, `ADR-`, `R-`, `TS-`) | Git deposu, `MAN.5` |
| d | Bilgi, planlanan düzenlemelere göre uygunluk açısından gözden geçirilir ve onaylanır | Doküman değişiklikleri de Pull Request ile yapılır; en az bir gözden geçiren onayı zorunludur | GitHub PR kayıtları |
| e | Bilgi, sürecin planlandığı gibi yürütüldüğüne güven verecek ölçüde saklanır | Bilgi kayıt defteri (sahip, konum, saklama süresi); yedekleme | `MAN.6-bilgi-yonetimi/bilgi-kayit-defteri.md` |

> **Bu projedeki en güçlü kanıt kaynağı Git'tir.** Her belge sürümlenmiş, her değişiklik
> gerekçeli ve onaylı, her kaydın tarihi ve sahibi bellidir. Dokümanların da PR ile
> değiştirilmesi kararı doğrudan (c) ve (d) maddelerini karşılamak içindir.

---

## 6. Öz değerlendirme tablosu

Bu tablo, her modül kapanışında ve en geç üç ayda bir güncellenir. Derecelendirme,
kanıtın fiilen mevcut olmasına göre yapılır — "yapılacak" olan sayılmaz.

**Durum kodları:** `—` henüz başlanmadı · `N` · `P` · `L` · `F`

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | Not |
|---|---|---|---|---|---|
| MAN.4 Risk yönetimi | **F** | L | L | **2** | 18 risk; ölçülüyor, düzeltiliyor, kapanıyor |
| MAN.1 Proje planlama | L | L | L | 1 | Plan ve RACI **onay bekliyor** → MAN.1(c) eksik |
| MAN.5 Konfigürasyon yönetimi | L− | L | L | 1 | Konfigürasyon öğeleri belgesi ve denetim yok; CHANGELOG bayat |
| MAN.6 Bilgi yönetimi | L− | P+ | L− | 1 | Bilgi kayıt defteri, saklama/imha düzeni yok |
| MAN.8 Kalite güvence | L | L | L | 1 | Ürün değerlendirmesi güçlü; süreç değerlendirmesi ilk kez 18.09'da yapıldı |
| TEC.5 Tasarım tanımlama | L | L | L | 1 | 16 ADR + OpenAPI; izlenebilirlik yok |
| TEC.7 Gerçekleştirme | L+ | L | L | 1 | Çalışan iskelet; gereksinim ↔ kod bağı yok |
| TEC.8 Entegrasyon | L | L | P+ | 1 | UAT yığını çalışıyor; dış arayüzler ve rapor yok |
| TEC.9 Doğrulama | L+ | L | L− | 1 | 168 test, kontroller kırılarak kanıtlandı; izlenebilirlik yok |
| TEC.10 Geçiş | L− | L | L | 1 | UAT kuruldu ve TLS ile yayında; göç ve eğitim sırası gelmedi |
| MAN.2 Proje değerlendirme ve kontrol | P+ | P+ | L | 0 | Ölçütler ölçülmüyor; dönemsel durum raporu yok |
| TEC.2 Paydaş ihtiyaç ve gereksinimleri | P+ | P+ | L− | 0 | Modül gereksinimleri İK toplantılarıyla gelecek (`KR-068`) |
| TEC.13 Bakım | P− | P | P | 0 | Üretim yok; yalnızca hata issue'ları |
| TEC.3 Sistem/yazılım gereksinimleri | N | — | — | 0 | Sırası gelmedi |
| TEC.11 Geçerleme | N | — | — | 0 | Sırası gelmedi — kabul edilecek modül yok |

**Dayanak:** `MAN.8-kalite-guvence/raporlar/2026-09-18-surec-gozden-gecirme-raporu.md`

> **Seviye 2'nin önündeki en büyük tek engel:** `izlenebilirlik-matrisi.md` hiç
> oluşturulmadı. Sekiz sürecin çıktı listesinde izlenebilirlik maddesi bulunuyor;
> matris olmadan hiçbiri `F` alamaz (bkz. rapor, BULGU-01).

> **Ölçek kuralı:** Bu tabloda **yalnızca N/P/L/F** kullanılır. Aşama kapanış belgeleri
> kendi sözlüğünü ("Kurulmuş", "İşletiliyor" gibi) üretmez; bu tabloya atıf yapar.
> A1 kapanışında iki farklı ölçek kullanılmıştı ve dereceler birleştirilemiyordu
> (BULGU-03).

**Seviye sütunu nasıl doldurulur:** PA 1.1 = F **ve** PA 2.1 ≥ L **ve** PA 2.2 ≥ L ise
Seviye 2. PA 1.1 ≥ L ise Seviye 1. Aksi hâlde Seviye 0.

---

## 7. Uygulama ilkeleri

1. **Kanıtı süreç üretsin, ayrıca doküman yazılmasın.** Git geçmişi, PR kayıtları, CI
   çalışmaları ve issue akışı doğal olarak kanıt üretir. Denetim için sonradan
   doküman üretmek hem maliyetlidir hem de "sistematik yaklaşım" izlenimi vermez.
2. **Kanıt tarihli ve sahipli olmalı.** Tarihsiz veya sahipsiz bir belge, sürecin
   yönetildiğini kanıtlamaz.
3. **Her modül bir çevrim (döngü) tamamlar.** Gereksinim → tasarım → geliştirme →
   doğrulama → kabul → kapanış gözden geçirmesi. Her çevrim, 15 sürecin tamamı için
   yeni kanıt üretir. Modül sayısı arttıkça "sistematik yaklaşım" kanıtı güçlenir.
4. **Zayıflık gizlenmez, kaydedilir.** Gözden geçirmelerde tespit edilen eksikler
   düzeltici faaliyet olarak kayda alınır. Bu, seviye kaybı değil, MAN.8'in
   çalıştığının kanıtıdır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-04 | 0.1 | TS ISO/IEC 33020 incelemesi sonrası ilk oluşturma | Bilgi İşlem |
| 2026-09-18 | 0.2 | §6 öz değerlendirme tablosu ilk kez gerçek kanıtla güncellendi; 15 süreç derecelendirildi (MAN.8 süreç gözden geçirmesi) | Bilgi İşlem |
