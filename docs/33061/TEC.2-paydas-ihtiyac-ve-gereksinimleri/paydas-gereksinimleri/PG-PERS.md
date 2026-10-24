# Paydaş Gereksinimleri — T1 Personel Yönetimi

**Belge kimliği:** TEC.2-PG-PERS
**Süreç:** TEC.2 — Paydaş İhtiyaç ve Gereksinimlerinin Tanımlanması
**Son güncelleme:** 2026-10-24
**Kaynak toplantı:** `kayitlar/2026-10-19-personel-gereksinim-toplantisi.md`
**Onay durumu:** **Onaylandı — 2026-10-19** (İK gereksinim toplantısı)

> **Bu belge, T1 geliştirmesinin başlangıç koşuludur** (`KR-068`). Burada yazmayan bir
> davranış geliştirilmez. Burada yazan bir davranış değiştirilecekse
> `tur:degisiklik-talebi` issue'su açılır (TEC.2 YAKLAŞIM §4.1).

---

## 1. Kapsam

Personel verisinin LOGO Bordro'dan senkronize edilmesi; personel listesi (kart ızgarası
ve tablo), kişi kartı, kendi profilim, veri kalitesi raporu ve senkronizasyon durumu
ekranları. LOGO'da bulunmayan birkaç bilgi HRMS'te tutulur: evlilik tarihi, evlilik
yardımı, görev ve acil durum kişisi (`KR-112`). Personel, hatalı bilgisini İK'ya
bildirebilir; İK düzeltmeyi LOGO'da yapar.

Personel çekirdeği T3 kapsamında kuruldu (`KR-077`): kişi/istihdam modeli, LOGO
senkronizasyonu, çalışma kaydı ve şema denetimi. T1 bu çekirdeği genişletir ve
ekranlarını ekler. Ünvan ve görev tanımları için gereken referans veri çekirdeği de T1'de
kurulur; Y3 Referans Veri Yönetimi onu sonra genişletir (`KR-115`). T3'te karşılanan maddeler §4'te "T3'te karşılandı" notuyla
işaretlidir.

**Kapsam dışı:**
- Personel ekleme, düzenleme ve silme LOGO'da kalır.
- Ücret, bordro ve SGK bildirimleri kapsam dışıdır. Ücret hiçbir şekilde gösterilmez.
- Yönetici ataması T2'de yapılır. Yönetici bilgisi LOGO'dan alınmaz.
- Özlük belgeleri Y7'nin, izin İ1'in, eğitim ve performans İ2, İ3 ve İ5'in kapsamındadır.
- Din ve sağlık bilgileri hiçbir modülde alınmaz.
- Personel kendi fotoğrafını yükleyemez.

Ayrıntı için toplantı kaydı §4'e bakılabilir.

## 2. Özet

| | |
|---|---:|
| İK'ya sunulan gereksinim | 41 |
| Olduğu gibi onaylanan | 34 |
| Değişiklikle onaylanan | 7 (PG-PERS-13, 14, 19, 21, 36, 37, 38) |
| Açık soru cevabıyla netleşen | 5 (PG-PERS-17, 20, 23, 31, 32) |
| Toplantı sonucu eklenen | 3 (PG-PERS-42, 43 İK'nın eklediği; PG-PERS-44 S-08 cevabından) |
| Toplantı sonrası teyitle eklenen | 1 (PG-PERS-45, KVKK Sorumlusu kararı, 20.10.2026) |
| **Toplam onaylı gereksinim** | **45** |
| Onaydan sonraki değişiklik talebi | 2 (#270: PG-PERS-12'den "çalışma şekli" çıkarıldı; #284: PG-PERS-12'ye firma eklendi) — gereksinim sayısı değişmedi |

**Toplantı sonrası teyit (20.10.2026):** Toplantı kaydı §11'deki değerlendirmeler İK ve KVKK
Sorumlusu tarafından cevaplandı (kayıt §12, #241). PG-PERS-14, 21, 36, 38, 42, 43 ve 44
netleşti; PG-PERS-45 eklendi.

## 3. Kimlik numaraları hakkında

Gereksinimler toplantıya `REQ-PERS-nnn` kimlikleriyle getirildi. Bu belge, TEC.2
YAKLAŞIM §3.1 gereği `PG-PERS-nn` biçimini kullanır. Eşleme bire birdir: `PG-PERS-13`
taslaktaki `REQ-PERS-013`'tür. Böylece toplantı dosyasıyla depo arasındaki bağ
numarayla kurulur. Taslakta olmayan `PG-PERS-44` (S-08 cevabı) sona eklendi.

> **Not:** Bu listenin bir bölümü sistem düzeyinde gereksinimdir (ör. senkronizasyon
> periyodu, 404 yanıtı). TEC.3 sistem gereksinimleri (SYG-PERS) yazılırken bu ayrım
> yapılır ve her sistem gereksinimi buradaki kimliğe bağlanır.

---

## 4. Gereksinimler

### Veri kaynağı ve senkronizasyon

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **PG-PERS-01** | Personel verisinin kaynağı LOGO Bordro veritabanıdır; HRMS bu veriyi kendi tablolarına senkronize eder ve ekranlar HRMS'ten okur. | Canlı sorgu, LOGO bakımdayken sistemi durdurur ve izin/eğitim kayıtları LOGO satırına bağlanamaz. (ADR-0003 §1) T3'te kısmen karşılandı (`KR-077`); T1 yeni alanları senkronizasyona ekler. | Zorunlu | LOGO erişilemez durumdayken personel listesi açılmaya devam ediyor. | Onaylandı |
| **PG-PERS-02** | LOGO'ya hiçbir koşulda yazma, güncelleme veya silme yapılmaz. | Bordro, kurumun kaynak sistemidir; bütünlüğü riske atılamaz. Yetki veritabanı düzeyinde DENY ile de engellenmiştir. (KR-003, KR-004) T3'te karşılandı. | Zorunlu | LOGO bağlantısıyla yazma denemesi veritabanı tarafından reddediliyor. | Onaylandı |
| **PG-PERS-03** | Personel bilgileri HRMS üzerinden DÜZENLENEMEZ; düzeltme LOGO'da yapılır ve senkronizasyonla yansır. | İki yerde düzenlenebilen veri, er geç ikisi de yanlış olan veriye dönüşür. Tek doğruluk kaynağı LOGO'dur. **Kapsamı:** LOGO'dan gelen alanlar. LOGO'da bulunmayan ve HRMS'te tutulan alanlar (PG-PERS-19, 43, 44) bu kuralın dışındadır; onları yalnızca İK ve Sistem Yöneticisi girer (`KR-112`). | Zorunlu | Personel kartında LOGO'dan gelen alanlar için düzenleme alanı bulunmuyor; veri salt görüntüleniyor. | Onaylandı |
| **PG-PERS-04** | Senkronizasyon 15 dakikada bir otomatik çalışır; periyot Sistem Yönetimi parametresidir (PRM-ENT-07). | 15 dakikalık gecikme İK süreçleri için kabul edilebilir; daha sık çalışmak LOGO üzerinde gereksiz yük oluşturur. (ADR-0003 §5) T3'te karşılandı. | Zorunlu | Arka plan servisi 15 dakikada bir çalışıyor ve çalışma kaydı üretiyor. | Onaylandı |
| **PG-PERS-05** | Yetkili kullanıcı "şimdi senkronize et" diyerek elle tetikleyebilir; periyodu beklemek zorunda kalmaz (PRM-ENT-07). | Yeni personel girişi gibi acil durumlarda 15 dakika beklemek gerekmemeli. (ADR-0003 §5) T3'te karşılandı; T1 düğmeyi senkronizasyon durumu ekranına ekler. | Zorunlu | Elle tetikleme düğmesi çalışıyor ve sonucu ekranda gösteriliyor. | Onaylandı |
| **PG-PERS-06** | Her senkronizasyon bir çalışma kaydı üretir: okunan, eklenen, güncellenen ve uyarı üretilen kayıt sayıları. | "Senkronizasyon çalıştı mı, ne değişti" sorusu kayıt olmadan cevaplanamaz. (ADR-0003 §5) T3'te karşılandı; T1 listeyi ekranda gösterir. | Zorunlu | Son çalışmaların listesi sayılarıyla görüntülenebiliyor. | Onaylandı |
| **PG-PERS-07** | Personel listesi ve kartında "son senkronizasyon" zamanı görünür. | Kullanıcı, gördüğü verinin ne kadar güncel olduğunu bilmelidir. (ADR-0003 §7) | Olmalı | Ekranda son senkronizasyon zamanı yazıyor. | Onaylandı |
| **PG-PERS-08** | LOGO şemasındaki beklenmeyen değişiklikler (şema sapması) günlük olarak denetlenir ve sapma bulunduğunda sistem yöneticisine bildirilir; alıcı, tek kişi değil rol olarak belirlenir (PRM-BLD-01). | LOGO sürüm yükseltmesi entegrasyonu sessizce kırabilir; hatayı kullanıcıdan önce biz görmeliyiz. (R-02, ADR-0003 §6) T3'te **kısmen** karşılandı: şema denetimi çalışıyor; sistem yöneticisine bildirim Y1 Bildirim Merkezi'ne bırakıldı (SYG-KMLK-010). T1 yeni okunan tabloları denetime ekler. | Zorunlu | Beklenen bir kolon kaldırıldığında denetim hata üretiyor. | Onaylandı |

### Kişi ve istihdam

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **PG-PERS-09** | Aynı TCKN'ye sahip farklı sicil kayıtları TEK kişi olarak gösterilir; her sicil ayrı bir istihdam kaydıdır. | LOGO'da 1.547 kart 1.371 kişiye karşılık geliyor; 143 kişinin birden fazla sicili, 13 kişinin aynı anda birden fazla AKTİF sicili var (16.10.2026 ölçümü). (ADR-0005 §1) T3'te karşılandı. | Zorunlu | Üç sicili olan bir kişi listede bir kez görünüyor, kartında üç istihdam listeleniyor. | Onaylandı |
| **PG-PERS-10** | TCKN'si olmayan LOGO kartı kişi olarak oluşturulmaz; uyarı listesine alınır. | Sistem kartları ve eksik kayıtlar gerçek kişi gibi görünmemelidir. Sicil numarasıyla tek tek dışlamak kırılgandır. (KR-043) T3'te karşılandı. | Zorunlu | TCKN'si boş kart kişi listesinde yok, uyarı listesinde var. | Onaylandı |
| **PG-PERS-11** | Kişinin birden fazla sicili varsa kart detayında sekmeli (tab) gösterim kullanılır; her sekme bir istihdam dönemidir. | İK talebi (20.09.2026). Tek ekranda birleştirmek, hangi bilginin hangi istihdama ait olduğunu belirsizleştirir. | Zorunlu | İki sicilli kişinin kartında iki sekme var; sekme başlığında sicil numarası ve durumu yazıyor. | Onaylandı |
| **PG-PERS-12** | Değişen nitelikler (**firma**, şube, birim, görev, ~~çalışma şekli~~) tarih aralıklı tutulur; "geçen yıl bu personel hangi şubedeydi" sorusu cevaplanabilir. | Tarih farkındalığı olmayan İK sistemi ikinci yılında raporlanamaz hâle gelir. (ADR-0005 §3) **Değişiklik talebi #270 (İK onayı 22.10.2026):** "çalışma şekli" çıkarıldı; LOGO'da verisi yok (`WORKTYPE`, 588 aktif kartın tamamında 0). **Değişiklik talebi #284 (24.10.2026):** firma da tarih aralıklı tutulur; bir sicilin firması LOGO'da değişebilir ve şube ile birim firmaya bağlıdır. | Zorunlu | Geçmiş bir tarih verildiğinde o tarihteki firma/şube/birim/görev döndürülüyor. | Onaylandı; değişiklik talebi #270, İK onaylı (22.10.2026); değişiklik talebi #284, Bilgi İşlem Birim Sorumlusu ve İK onaylı (24.10.2026) |

### Liste ekranı

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **PG-PERS-13** | Personel listesi, kişi kartlarından oluşan ızgara (grid) düzeninde gösterilir; üstte filtre çubuğu bulunur. Kartta **fotoğraf, ad soyad, sicil, şube/birim ve ünvan/görev** görünür. Kart ızgarası ile **klasik tablo görünümü** arasında geçiş yapılabilir. | İK talebi ve örnek tasarım (Personel.png, 20.09.2026). İK notu (19.10.2026): kartta şube/birim ve ünvan/görev görünsün. Tablo görünümü: S-09 cevabı. | Zorunlu | Liste kart ızgarası olarak açılıyor; kartta fotoğraf, ad soyad, sicil, şube/birim ve ünvan/görev görünüyor; görünüm değiştirici tabloya geçiyor ve geri dönüyor. | Değişiklikle onaylandı |
| **PG-PERS-14** | **Kişi kartında gösterilen bilgilerin tamamı için filtre yapılabilir.** Seçim listesi olan filtrelerde (ör. firma, şube, birim, ünvan, görev, eğitim) **birden fazla değer seçilebilir.** Yönetici filtresi PG-PERS-34'e tabidir. | İK notu (19.10.2026): "Personel bilgilerinin tamamı için filtreleme yapılabilmeli. Filtre seçme alanlarında çoklu seçim mutlaka olmalı." Taslaktaki dokuz alanlık liste bununla genişledi. Kullanıcının görme yetkisi olmayan bir alanın filtresi sunulmaz: aksi hâlde filtre, gizli değeri sızdırır (toplantı kaydı §11.5; İK teyidi 20.10.2026). | Zorunlu | Kartta gösterilen her alan için filtre var; seçim listeli bir filtrede iki değer seçildiğinde ikisinden birine uyan kayıtlar geliyor; farklı filtreler birlikte uygulanıyor; adresi göremeyen kullanıcıya adres filtresi sunulmuyor. | Değişiklikle onaylandı; görme yetkisi kuralı İK teyitli (20.10.2026) |
| **PG-PERS-15** | Filtreler birlikte uygulanır (VE mantığı); seçili filtreler ekranda görünür ve tek tuşla temizlenebilir. | Kullanıcı hangi filtrenin açık olduğunu görmezse "kayıt gelmiyor" sanır. (ADR-0015 §6) | Olmalı | Seçili filtreler etiket olarak görünüyor; "temizle" hepsini kaldırıyor. | Onaylandı |
| **PG-PERS-16** | Liste varsayılan olarak yalnızca AKTİF çalışanları gösterir; ayrılanlar filtreyle görüntülenir. | Günlük kullanımın tamamına yakını aktif personelle ilgilidir. 961 ayrılan kart (16.10.2026) varsayılan listeyi gereksiz büyütür. Ayrılanların görünür kalma süresi: PG-PERS-38. | Olmalı | Ekran açıldığında yalnızca aktif çalışanlar geliyor. | Onaylandı |
| **PG-PERS-17** | Liste sayfalanır; sayfa boyutu ve toplam kayıt sayısı gösterilir. Varsayılan ve en büyük sayfa boyutu Sistem Yönetimi parametresidir (PRM-GRN-02, PRM-GRN-03). **Kart ızgarasında varsayılan sayfa boyutu 24'tür;** değer Sistem Yönetimi parametresinden alınır. | Bin beş yüz kaydı tek seferde çizmek tarayıcıyı yorar. (ADR-0010 §4) Kart ızgarası için 24: S-07 cevabı. | Zorunlu | Sayfalama çalışıyor; toplam kayıt sayısı görünüyor; kart ızgarası ilk açıldığında 24 kart gösteriyor; parametre değiştirildiğinde sayı kod değişikliği olmadan değişiyor. | Onaylandı (açık soru cevabıyla netleşti) |
| **PG-PERS-18** | Ad, soyad, sicil numarası veya TCKN üzerinden tek kutudan hızlı arama yapılabilir. | İK'nın en sık ihtiyacı "şu kişiyi bul"dur; dört ayrı alanı doldurmak yavaştır. | Olmalı | Arama kutusuna yazılan değer dört alanda birden aranıyor. | Onaylandı |
| **PG-PERS-42** | Dışa aktarmada (Excel/PDF) hangi alanların aktarılacağı seçilebilir. Excel'e düz liste aktarılır. PDF'e, fotoğraf alanı seçildiyse **fotoğraflı** aktarım yapılabilir. | İK talebi (19.10.2026, toplantıda eklendi). Dışa aktarma yetkisi ve kaydı: PG-PERS-29, 30. Kullanıcının görme yetkisi olmayan alan seçenek olarak sunulmaz (§11.5; İK teyidi 20.10.2026). | Zorunlu | Dışa aktarma ekranında alanlar seçilebiliyor; Excel'de seçilen alanlar düz liste olarak, PDF'te fotoğraf seçildiyse fotoğrafla aktarılıyor; görme yetkisi olmayan alan seçenekler arasında yok; aktarım PG-PERS-30'a göre kaydediliyor. | Toplantı sonucu eklendi; öncelik ve kabul kriteri İK onaylı (20.10.2026) |

### Kişi kartı

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **PG-PERS-19** | Kart detayında Genel Bilgiler bölümü: fotoğraf, ad, soyad, cinsiyet, doğum tarihi, yaş, eğitim, medeni hâli, kan grubu (PG-PERS-36). **Kadın personelde LOGO'da önceki soyadı doluysa önceki soyadı da gösterilir.** **Medeni hâli Evli olan personelde evlilik tarihi ve "evlilik yardımı almıştır / almamıştır" bilgisi tutulur;** bu iki bilgi LOGO'da bulunmaz, HRMS'te yalnızca İK ve Sistem Yöneticisi tarafından girilir ve değiştirilir. **Evlilik tarihi ve evlilik yardımını yalnızca İK ve Sistem Yöneticisi görür** (kişinin kendisi dahil başka kimse görmez). Medeni hâl sonradan Bekar'a dönerse daha önce girilmiş evlilik bilgisi **görülebilir kalır**. | İK talebi (20.09.2026). İK notu (19.10.2026): evlilik tarihi, evlilik yardımı ve önceki soyadı. LOGO kaynağı: fotoğraf ve medeni hâl aile/resim tablosundan (1 = Evli, diğer = Bekar), eğitim tanım tablosundan, önceki soyadı kimlik ek bilgileri tablosundan. HRMS'te tutulan alanlar: `KR-112`. Evlilik bilgisinin görünürlüğü ve medeni hâl değişince korunması: İK teyidi (22.10.2026, toplantı kaydı §13). | Zorunlu | Sayılan alanların tamamı görüntüleniyor; önceki soyadı yalnızca kadın personelde ve LOGO'da doluysa görünüyor; evlilik tarihi ve evlilik yardımı alanları yalnızca medeni hâli Evli olan personelde, yalnızca İK ve Sistem Yöneticisi için düzenlenebilir; evlilik bilgisi İK ve Sistem Yöneticisi dışında hiçbir kullanıcının yanıtında yok, kişinin kendi kartında da yok; medeni hâli Bekar'a dönen kişide girilmiş evlilik bilgisi görünmeye devam ediyor; değişiklik denetim izine yazılıyor. | Değişiklikle onaylandı; evlilik bilgisi İK teyitli (22.10.2026) |
| **PG-PERS-20** | İletişim Bilgileri bölümü: e-posta, telefon, adres. **Adresi yalnızca İK, Sistem Yöneticisi ve kişinin kendisi görür; sorumlular dahil başka kimse görmez.** | İK talebi (20.09.2026). Adres görünürlüğü: S-02 cevabı. LOGO kaynağı: iletişim tablosu (tür 3 telefon, 6 e-posta, 1 adres). 16.10.2026: 585 aktif karttan 8'inde telefon, 6'sında e-posta, 74'ünde adres yok. | Zorunlu | Üç alan da görüntüleniyor; kayıtlı olmayan alanda "Kayıtlı değil" yazıyor; İK, Sistem Yöneticisi ve kişinin kendisi dışındaki kullanıcı adresi göremiyor (API yanıtında da yok). | Onaylandı (açık soru cevabıyla netleşti) |
| **PG-PERS-21** | Çalışma Hayatı bölümü: şehir, firma, şube, birim, **ünvanı**, görevi (PG-PERS-43), çalışma durumu, statü, sicil numarası, SGK meslek kodu, SGK meslek, işe giriş tarihi, işten çıkış tarihi, **kıdem süresi**. LOGO'daki 1–4 kodlu alan **"Ünvanı"** etiketiyle gösterilir (1 Birim Operatörü, 2 Birim Teknik Sorumlusu, 3 Birim Sorumlusu, 4 Yardımcı Hizmetler); kodların karşılıkları **Y3 Referans Veri Yönetimi**'nden alınır; karşılığı olmayan değer **boş** gösterilir. Kıdem süresi **aktif istihdamda işe giriş tarihinden bugüne, ayrılan istihdamda işe giriş tarihinden işten çıkış tarihine** kadar geçen süre olarak hesaplanır, saklanmaz ve **"XX Yıl YY Ay ZZ Gün"** biçiminde gösterilir; yıl dolmadıysa yalnızca ay ve gün, ay dolmadıysa yalnızca gün gösterilir. | İK talebi (20.09.2026). İK notu (19.10.2026) ve S-10 cevabı: alan "Görevi" değil "Ünvanı" etiketiyle, "Bilinmiyor" değil boş gösterilecek; karşılıklar Y3'ten; kıdem süresi gösterilecek. LOGO kaynakları "LOGO Alan Eşleme" listesinde (toplantı kaydı §6). Kıdem gösterimi: İK teyidi (20.10.2026); ayrılan istihdamda hesap: İK teyidi (22.10.2026). Y3 bağımlılığı: ünvan karşılıkları T1'de kurulan referans veri çekirdeğinden gelir (`KR-115`). | Zorunlu | Sayılan alanların tamamı ilgili istihdam sekmesinde görüntüleniyor; ünvanı boş olan kartta alan boş görünüyor; ünvan karşılığı referans veride değiştirildiğinde ekran kod değişikliği olmadan güncelleniyor; kıdem süresi aktif istihdamda bugüne, ayrılan istihdamda işten çıkış tarihine göre doğru hesaplanıyor; bir yılı dolmamış kişide yıl, bir ayı dolmamış kişide yıl ve ay gösterilmiyor. | Değişiklikle onaylandı; kıdem gösterimi İK teyitli (20.10.2026); ayrılan istihdamda hesap İK teyitli (22.10.2026) |
| **PG-PERS-22** | Yaş, doğum tarihinden hesaplanarak gösterilir; ayrı bir alan olarak saklanmaz. | Saklanan yaş her yıl yanlışa döner. Hesaplanan değer her zaman doğrudur. | Zorunlu | Doğum günü geçen kişinin yaşı ertesi gün kendiliğinden artıyor. | Onaylandı |
| **PG-PERS-23** | Fotoğraf LOGO'daki personel resminden alınır; resmi olmayan kişide ad-soyad baş harflerinden oluşan yer tutucu gösterilir. **Personel kendi fotoğrafını yükleyemez.** | Boş çerçeve yerine anlamlı bir gösterim, listeyi okunabilir tutar. 16.10.2026: 585 aktif karttan 488'inin fotoğrafı var. Kendi fotoğrafını yükleme: S-04 cevabı (LOGO tek kaynak). | Olmalı | Resmi olmayan kişide baş harfli yer tutucu görünüyor; personelin fotoğraf yükleme seçeneği yok. | Onaylandı (açık soru cevabıyla netleşti) |
| **PG-PERS-37** | Personel kendi kartında hatalı bir bilgi gördüğünde **"İK'ya bildir"** düğmesine basar, hatalı alanı seçer, doğru değeri yazar ve gönderir. Bildirilebilen alanlar: **telefon, e-posta, adres, eğitim, medeni hâl, kan grubu.** Bildirim **uygulama içinde İK rolündeki herkese**, **e-posta olarak Sistem Yönetimi parametresinde tanımlı İK adresine** gider. İK düzeltmeyi LOGO'da yapar; düzeltme senkronizasyonla HRMS'e yansır. | Doğuş Uçanok (16.10.2026); örnek: taşınan personelin adresi. İK notu (19.10.2026): eğitim, medeni hâl ve kan grubu da bildirilebilsin. Alıcılar: S-12 cevabı. HRMS'te düzenleme yapılmaz (PG-PERS-03); bildirim, düzeltme talebinin kayıtsız iletilmesini önler. | Zorunlu | Personel bildirimi gönderiyor; İK rolündeki her kullanıcı uygulama içi bildirimi, parametredeki adres e-postayı alıyor; bildirim İK'nın listesinde görünüyor ve "Tamamlandı" olarak kapatılabiliyor. | Değişiklikle onaylandı |
| **PG-PERS-43** | Kişi kartında **Görevi** alanı bulunur ve HRMS içinden girilir. Görev, **Y3 Referans Veri Yönetimi**'ndeki tanımlardan seçilir (ör. Yazılım Uzmanı, İnsan Kaynakları Uzmanı, Biyolog, Donanım Uzmanı). Görevi **İK veya Sistem Yöneticisi** girer. Görev **sicile (istihdama) bağlıdır ve tarih aralıklı** tutulur (PG-PERS-12). | İK talebi (19.10.2026, toplantıda eklendi). LOGO'daki 1–4 kodlu alan ünvanı verir (PG-PERS-21); kişinin fiilî görevi LOGO'da yok. HRMS'te tutulan alan: `KR-112`. Kimin gireceği, sicile bağlılık ve tarih aralığı: İK teyidi (20.10.2026, toplantı kaydı §12). Tanım listesi T1'deki referans veri çekirdeğinden gelir (`KR-115`). | Zorunlu | İK kullanıcısı kişi kartında görev seçebiliyor; seçenekler referans veri tanımlarından geliyor; yeni tanım eklendiğinde kod değişikliği olmadan seçilebiliyor; yetkisi olmayan kullanıcı görevi değiştiremiyor; değişiklik denetim izine yazılıyor; görev değiştirildiğinde önceki görev tarih aralığıyla korunuyor ve geçmiş bir tarih için o tarihteki görev dönüyor; iki sicilli kişide her sicilin görevi ayrı. | Toplantı sonucu eklendi; öncelik ve kabul kriteri İK onaylı (20.10.2026) |
| **PG-PERS-44** | Kişinin **acil durumda aranacak kişi** bilgisi (yakınlığı, adı, soyadı, cep telefonu) HRMS'te tutulur; **LOGO'dan alınmaz.** Bilgiyi **yalnızca İK ve Sistem Yöneticisi girer**; **yalnızca İK, Sistem Yöneticisi ve kişinin kendisi görür.** Bir kişiye **birden fazla** acil durum kişisi girilebilir. | İK kararı (19.10.2026, S-08 cevabı). HRMS'te tutulan alan: `KR-112`. Birden fazla kişi: İK teyidi (20.10.2026). | Zorunlu | İK kullanıcısı acil durum kişisini girip değiştirebiliyor; kişinin kendisi kendi kartında görüyor ama değiştiremiyor; başka bir kullanıcı göremiyor (API yanıtında da yok); değişiklik denetim izine yazılıyor; bir kişiye iki acil durum kişisi girilebiliyor. | Toplantı sonucu eklendi; öncelik ve kabul kriteri İK onaylı (20.10.2026) |

### Yetki ve kapsam

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **PG-PERS-24** | Bir personel yalnızca kendisine DOĞRUDAN atanmış astlarını görüntüleyebilir; astının astı, kendisine ayrıca atanmamışsa görünmez. | İK biriminin tanımı: A'nın altına B, C, D, E; B'nin altına D, E atanmışsa A → B, C, D, E; B → D, E görür. (KR-022, ADR-0007 §2) T2 ile devreye girer; o zamana kadar PG-PERS-35. | Zorunlu | B kullanıcısı C'yi göremiyor; A kullanıcısı D'yi görüyor. | Onaylandı |
| **PG-PERS-25** | Her personel rol yetkisinden bağımsız olarak KENDİ kaydını görüntüleyebilir. | Self kapsamı varsayılan olarak herkese verilir; ayrı istisna mekanizması kurulmaz. (ADR-0007 §5) | Zorunlu | Hiçbir rolü olmayan kullanıcı kendi kartını açabiliyor. | Onaylandı |
| **PG-PERS-26** | Kapsam filtresi veri katmanında uygulanır; kapsam dışı bir kaydın kimliğiyle doğrudan API çağrısı yapıldığında da veri dönmez. | "Ekranda göstermiyoruz" bir güvenlik kontrolü değildir. (ADR-0007 §3) | Zorunlu | Kapsam dışı kayıt kimliğiyle yapılan istek 404 dönüyor (yetki sızıntısı testi). | Onaylandı |
| **PG-PERS-27** | Kapsam dışı kayıt için 403 değil 404 döndürülür. | 403, "böyle bir kayıt var ama göremezsin" bilgisini sızdırır; ardışık kimlik denemesiyle personel sayısı çıkarılabilir. (ADR-0007 §4) | Zorunlu | Var olan ama kapsam dışı kayıt ile hiç olmayan kayıt aynı yanıtı veriyor. | Onaylandı |
| **PG-PERS-34** | "Yöneticisi" filtresi T2 Organizasyon Yönetimi ile BİRLİKTE devreye alınır; T2 hazır değilken filtre arayüzde gösterilmez. | İK kararı (21.09.2026, S-05; 19.10.2026'da yinelendi). Yönetici bilgisi LOGO'dan HİÇBİR ŞEKİLDE alınmaz; atamalar T2'de yapılır. Boş çalışan bir filtre, kullanıcıya sistemin bozuk olduğunu düşündürür. | Zorunlu | T2 devrede değilken filtre görünmüyor; devreye alındığında kod değişikliği olmadan görünür oluyor. | Onaylandı |
| **PG-PERS-35** | T2 Organizasyon Yönetimi ve T4 Rol ve Yetki Yönetimi devreye girene kadar geçiş kuralı: İK ve Sistem Yöneticisi rolleri tüm personeli görür; diğer personel yalnızca kendi kartını görür. | Doğrudan ast görünürlüğü (PG-PERS-24) T2'deki atamalara bağlıdır; atama yokken kapsam boş kalır. Bilgi İşlem önerisi (16.10.2026), İK tarafından S-11 cevabıyla teyit edildi (19.10.2026). | Zorunlu | İK kullanıcısı tüm personeli görüyor; rolü olmayan kullanıcı yalnızca kendi kartını görüyor; başka bir kişinin kart adresini açan personel 404 alıyor. | Onaylandı |

### KVKK

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **PG-PERS-28** | Bir personelin kişisel verisinin görüntülenmesi erişim kaydına yazılır: kim, kimin verisini, ne zaman görüntüledi. | Değişiklik kaydı tek başına yetmez; KVKK açısından görüntüleme de kayıt altına alınmalıdır. (ADR-0009 §3) Erişim kayıtları 10 yıl saklanır (`KR-109`). | Zorunlu | Kart açıldığında erişim kaydı oluşuyor. | Onaylandı |
| **PG-PERS-29** | Dışa aktarma (Excel/PDF) ayrı bir yetkidir; görüntüleme yetkisi dışa aktarma hakkı vermez. | Ekranda görmek ile bin kişilik listeyi dışarı çıkarmak aynı risk değildir. (ADR-0007 §1) | Zorunlu | export yetkisi olmayan kullanıcıda dışa aktar düğmesi çalışmıyor. | Onaylandı |
| **PG-PERS-30** | Her dışa aktarma işlemi kaydedilir: kim, hangi filtreyle, kaç kayıt aktardı. | Dışa aktarma, verinin kurum denetiminden çıktığı andır. (ADR-0014) | Zorunlu | Dışa aktarma sonrası kayıt oluşuyor ve raporlanabiliyor. | Onaylandı |
| **PG-PERS-31** | TCKN ekranda maskelenmiş gösterilir (örn. 123*****89). **İK ve Sistem Yöneticisi tam değeri görür; sorumlular dahil diğer herkes maskeli görür.** | TCKN, günlük işlerin çoğunda gerekmez. Varsayılan olarak açık göstermek gereksiz risktir. Tam görme yetkisi: S-01 cevabı. | Olmalı | İK ve Sistem Yöneticisi tam TCKN'yi görüyor; diğer kullanıcılar maskeli değeri görüyor (API yanıtında da maskeli). | Onaylandı (açık soru cevabıyla netleşti) |
| **PG-PERS-36** | **Din ve sağlık bilgileri** LOGO'dan senkronize edilmez, HRMS'te saklanmaz ve hiçbir ekranda gösterilmez. **Kan grubu** LOGO'dan alınır ve gösterilir; **yalnızca İK, Sistem Yöneticisi ve kişinin kendisi** görür. Kan grubu **veritabanında şifreli** saklanır; görüntülenmesi **değiştirilemez erişim kaydına** yazılır; kan grubunu gören personelin **gizlilik taahhüdü** bulunur. | İK notu ve KVKK Sorumlusu değerlendirmesi (19.10.2026): kan grubu gösterilecek, "KVKK kapsamında bir sorun yaratmıyor". Kan grubu KVKK md. 6'daki özel nitelikli veridir (sağlık): veritabanında uygulama seviyesinde şifrelenir (ADR-0009 §5), görüntülenmesi erişim kaydına yazılır (PG-PERS-28). `KR-113`. İşleme koşulları KVKK Sorumlusunun kararıyla (20.10.2026, `KR-116`) bilgi kayıt defterinde (BK-29, §3.1). LOGO'da 585 aktif kartın 89'unda kan grubu dolu (20.10.2026). | Zorunlu | Senkronizasyonun okuduğu alan listesinde din ve sağlık bilgisi yok (otomatik test); kan grubu İK, Sistem Yöneticisi ve kişinin kendisine görünüyor, başka kullanıcıya görünmüyor (API yanıtında da yok); veritabanında kan grubu şifreli; kan grubu görüntülemesinin erişim kaydı değiştirilemiyor. | Değişiklikle onaylandı; işleme koşulları KVKK Sorumlusu kararıyla (20.10.2026) |
| **PG-PERS-38** | İşten ayrılan personelin kaydı, ayrılış tarihinden sonra **Sistem Yönetimi parametresinde tanımlı süre** boyunca listede (Çalışma Durumu filtresiyle) ve kartta görünür. **Parametrenin varsayılanı süresizdir.** Süre tanımlanırsa süresi dolan kayıt gösterilmez. | İK kararı (19.10.2026, S-03 cevabı): varsayılan süresiz, süre parametreden; KVKK Sorumlusu: "KVKK kapsamında bir sorun yaratmıyor". Taslaktaki 10 yıl önerisinin yerine geçti. Bu gereksinim ekranda **görünürlüğü** düzenler; kaydın saklanma ve imha süresi ayrı konudur (ADR-0009 §6, Y2). `KR-114`. Saklama süresi: PG-PERS-45. | Zorunlu | Parametre boşken ayrılışının üzerinden 10 yıldan fazla geçmiş kayıt filtreyle görünüyor; parametreye süre girildiğinde süresi dolan kayıt listede çıkmıyor. | Değişiklikle onaylandı |
| **PG-PERS-45** | HRMS'teki personel verisinin (LOGO'dan senkronize edilen ve HRMS'te tutulan) **saklama süresi Sistem Yönetimi parametresinde** tanımlanır; koda gömülmez. Süresi dolan kaydın imhası Y2 Denetim ve Erişim Kayıtları modülünde geliştirilir; o zamana kadar kayıt silinmez. | KVKK Sorumlusu kararı (20.10.2026, toplantı kaydı §12; `KR-116`). Ekranda görünürlük (PG-PERS-38) ile saklama ayrı konulardır; ADR-0009 §6 saklama süresinin veri türü bazında parametre olmasını ister. Süre değeri parametrede belirlenir. | Zorunlu | Saklama süresi parametresi Sistem Yönetimi ekranında görünüyor ve değiştirilebiliyor; değişiklik denetim izine yazılıyor; T1'de hiçbir kayıt bu parametreye göre silinmiyor (imha Y2'de). | Toplantı sonrası eklendi (KVKK Sorumlusu, 20.10.2026) |

### Veri kalitesi

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **PG-PERS-32** | Senkronizasyonda uyarı üreten kayıtlar (TCKN yok, doğum tarihi yok, iletişim bilgisi yok, geçersiz telefon biçimi, paylaşılan e-posta, **ünvanı boş, görevi boş**) İK'nın görebileceği bir veri kalitesi raporunda listelenir. | Düzeltme LOGO'da yapılacağı için İK'nın somut bir listeye ihtiyacı var. "Ünvanı boş" ve "görevi boş" uyarıları: S-10 cevabı ve E-04 notu (19.10.2026). 16.10.2026: 13 aktif kartta ünvan boş. | Zorunlu | Rapor ekranda görüntüleniyor ve dışa aktarılabiliyor; ünvanı veya görevi boş kayıt raporda görünüyor. | Onaylandı (açık soru cevabıyla netleşti) |
| **PG-PERS-33** | Veri kalitesi raporu, T3 Kimlik Yönetimi'nin üyelik akışını etkileyen kayıtları ayrıca işaretler. | İletişim bilgisi olmayan personel kendi başına üye olamaz; İK bu kişilere davet bağlantısıyla hesap açar (T3, REQ-KMLK-011, KR-090). (R-12) | Olmalı | Üye olamayacak kişiler raporda ayrı başlık altında görünüyor. | Onaylandı |

### Performans ve kullanılabilirlik

| Kimlik | Gereksinim | Gerekçe / Kaynak | Öncelik | Kabul kriteri | Durum |
|---|---|---|---|---|---|
| **PG-PERS-39** | Personel listesinin bir sayfası (fotoğraflarla birlikte) ve kişi kartı, aynı anda 20 kullanıcı çalışırken 2 saniye içinde açılır. | Yavaş açılan liste, İK'yı Excel'e geri döndürür. Ölçüm UAT'de yapılır. Kritik performans ölçütleri SYG-PERS §5'te kesinleşir (toplantı kaydı §10). | Olmalı | UAT ölçümünde sayfaların %95'i 2 saniyenin altında açılıyor. | Onaylandı |
| **PG-PERS-40** | Personel listesi, kişi kartı, kendi profilim ve "İK'ya bildir" ekranları telefonda (360 piksel genişlik) yan kaydırma olmadan kullanılabilir; filtre çubuğu telefonda açılır panele dönüşür. | Personelin önemli bölümü masa başında çalışmıyor; kendi kartını telefondan açacak. T3 ekranları aynı kuralla yapıldı. | Zorunlu | 360 piksel genişlikte yan kaydırma yok; düğmeler parmakla rahat dokunulacak boyutta. | Onaylandı |
| **PG-PERS-41** | Fotoğraflar yalnızca LOGO'da değiştiğinde aktarılır; fotoğraflı senkronizasyon LOGO'yu yormaz ve 15 dakikalık periyodu aşmaz. | Aktif personelin fotoğrafları toplam ≈271 MB; her çalışmada yeniden okumak LOGO'ya gereksiz yük bindirir (ADR-0003 §5). | Olmalı | Fotoğrafı değişmeyen kişide ikinci senkronizasyonda fotoğraf aktarılmıyor; ilk aktarım dışındaki çalışmalar 2 dakikanın altında bitiyor. | Onaylandı |

---

## 5. İzlenebilirlik

Her gereksinim `docs/33061/izlenebilirlik-matrisi.md` §7'de bir satır olarak açılmıştır.
Sistem gereksinimi, tasarım, kod ve test halkaları, işin yapıldığı PR'da doldurulur
(matris §3). **Bağlanmamış gereksinim tamamlanmamış sayılır.**

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-20 | 1.0 | İlk sürüm — 19.10.2026 İK toplantısında onaylanan 44 gereksinim (#239) | Bilgi İşlem |
| 2026-10-20 | 1.1 | Toplantı sonrası teyit (İK ve KVKK Sorumlusu, 20.10.2026; Doğuş Uçanok bildirdi): PG-PERS-21 kıdem gösterimi; 36 kan grubu işleme koşulları; 43 görevi İK/Sistem Yöneticisi girer, sicile bağlı ve tarih aralıklı; 44 birden fazla acil durum kişisi; 14 ve 42 görme yetkisi kuralı; 42–44 öncelik ve kabul kriteri onaylı; PG-PERS-45 saklama süresi parametresi eklendi; Y3 çekirdeği `KR-115` (#241) | Bilgi İşlem |
| 2026-10-20 | 1.2 | PG-PERS-08 gerekçesi: T3'te kısmen karşılandı, bildirim Y1'e bırakıldı (tasarım gözden geçirmesi BG-08, #234) | Bilgi İşlem |
| 2026-10-22 | 1.3 | İK teyidi (22.10.2026; Doğuş Uçanok bildirdi): PG-PERS-19 evlilik bilgisini yalnızca İK ve Sistem Yöneticisi görür, medeni hâl Bekar'a dönünce korunur; PG-PERS-21 ayrılan istihdamda kıdem çıkış tarihine kadar (#265) | Bilgi İşlem |
| 2026-10-22 | 1.4 | **Değişiklik talebi #270** (İK onayı 22.10.2026, Doğuş Uçanok bildirdi): PG-PERS-12'den "çalışma şekli" çıkarıldı; LOGO'da verisi yok (T1 tasarım gözden geçirmesi B-05) | Bilgi İşlem |
| 2026-10-24 | 1.5 | **Değişiklik talebi #284** (Bilgi İşlem Birim Sorumlusu onayı 24.10.2026, Doğuş Uçanok): PG-PERS-12'ye firma eklendi; kabul kriteri "firma/şube/birim/görev" | Bilgi İşlem |
| 2026-10-24 | 1.6 | Değişiklik talebi #284: İK onayı da kayda geçti (24.10.2026, Doğuş Uçanok bildirdi) | Bilgi İşlem |
