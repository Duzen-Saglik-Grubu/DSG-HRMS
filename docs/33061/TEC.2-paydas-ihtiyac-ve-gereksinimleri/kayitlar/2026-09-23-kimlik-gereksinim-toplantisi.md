# İK Gereksinim Toplantısı — T3 Kimlik Yönetimi

**Belge kimliği:** TEC.2-KAYIT-2026-09-23-KMLK
**Süreç:** TEC.2 — Paydaş İhtiyaç ve Gereksinimlerinin Tanımlanması
**Tarih:** 2026-09-23, 15:45–17:00
**Yer / biçim:** Yüz yüze
**Kaydı tutan:** Doğuş Uçanok (Bilgi İşlem)
**Kayıt tarihi:** 2026-09-24 (toplantıdan sonra 48 saat içinde — `YAKLASIM.md` §3)

> **Toplantı girdisi:** 22.09.2026 tarihli gereksinim taslağı (53 gereksinim, 13 açık
> soru, 10 ekran). Taslak toplantıda satır satır görüşülmüş, İK Onayı / İK Notu / İK
> Cevabı sütunları toplantı sırasında doldurulmuştur. Doldurulmuş dosyanın özgün hâli
> Bilgi İşlem'de saklanmaktadır; **kişisel veri içermediği hâlde depoya alınmamıştır**,
> çünkü bu belge onun içeriğini eksiksiz aktarır ve tek doğruluk kaynağı depodur.

---

## 1. Katılımcılar

| Ad | Rol | Paydaş kodu |
|---|---|---|
| Doğuş Uçanok | Bilgi İşlem Birim Sorumlusu | P2 |
| Gamze Demir | İnsan Kaynakları Uzmanı | P1 |
| Ahmet Erdoğan | İnsan Kaynakları Uzmanı | P1 |

**Çağrılıp katılamayanlar:** yok.

> Paydaş listesi toplantı öncesi gözden geçirildi. Birim sorumlularının katılımı bu
> modül için gerekli görülmedi: üyelik ve giriş, tüm personel için aynı akıştır ve
> onay akışı içermez. (Birim sorumlularının katılımı Organizasyon ve İzin modüllerinde
> önerilmektedir — `paydas-listesi.md`.)

---

## 2. Kapsam ve kullanım bağlamı *(TEC.2 çıktısı b)*

**Modül:** T3 Kimlik Yönetimi (Üyelik + Giriş)

**Kimler kullanacak:** Tüm aktif personel (564 kişi / 584 sicil kaydı, 22.09.2026).

**Hangi durumda kullanılacak:** Personel sisteme ilk kez kendi başına üye olur; sonra
her kullanımda kurumsal e-posta adresi ve parolasıyla giriş yapar. Kullanıcıların
önemli bölümü **seyrek kullanıcıdır** (izin talebi, bordro dışı bilgi görüntüleme).

**Mevcut durumda nasıl yapılıyor:** Bu modül, personelin kendi başına üye olmasını ilk kez sağlayacaktır (vizyon hedefi H2: devreye almadan sonraki 2 ay içinde aktif personelin ≥ %90'ı hesap açmış olmalı).

---

## 3. Görüşülen gereksinimler *(TEC.2 çıktısı d)*

| Sonuç | Sayı |
|---|---:|
| Olduğu gibi onaylanan | **52** |
| Değişiklikle onaylanan | **1** |
| Reddedilen | 0 |
| **Toplam** | **53** |

**Değişiklikle onaylanan — REQ-KMLK-026 (oturum süreleri):**

> İK Onayı: *Değiştirilsin.*
> İK Notu: *"Sistem parametresi varsayılanı olarak 30 dk'lık hareketsizlikte oturum
> düşsün. Yalnız önemli bir not: Eğitim modülünde kullanıcı video süresince 30 dk'dan
> fazla hareketsiz kalabilir, burada bir önlem alınmalıdır. Bu ve benzer durumlarda
> oturum düşmemelidir."*

Bu not, yeni bir gereksinim doğurdu: **REQ-KMLK-054** (meşru uzun süreli etkinlikte
oturumun düşmemesi).

Onaylı gereksinimlerin tamamı: `paydas-gereksinimleri/PG-KMLK.md`.

---

## 4. Tartışılan ve elenen talepler

| Talep | Neden kapsam dışı | Sonra ele alınacak mı? |
|---|---|---|
| Rol ve yetki tanımlama ekranları | Kimlik doğrulama ile yetkilendirme ayrı konulardır | T4 Rol ve Yetki Yönetimi |
| Kullanıcı listesi ve hesap yönetimi | Hesapların toplu yönetimi ayrı modüldür | T5 Kullanıcı Yönetimi |
| Active Directory / tek oturum açma | Kurumda AD kullanılmıyor | Hayır |
| Mobil uygulama | Web arayüzü mobil uyumlu olacak | Hayır |
| Parolasız giriş (passkey vb.) | Cihaz çeşitliliği ve kullanıcıların teknik seviyesi | Sonraki sürümlerde değerlendirilebilir |

> **2FA bu listede değildir.** 21.09.2026 kararıyla (`KR-069`) kapsama alınmış ve
> geliştirilecektir; parametre varsayılan olarak kapalıdır.

---

## 5. Kısıtlar *(TEC.2 çıktısı c)*

| Kısıt | Kaynak | İlgili kayıt |
|---|---|---|
| LOGO'ya hiçbir koşulda yazma yapılmaz | Kurumsal | `KR-003`, `KR-004` |
| Doğrulama kodu yalnızca kurumsal alan adlarına gönderilir | Güvenlik | `KR-019` |
| Paylaşılan e-posta adresine doğrulama kodu gönderilmez | Güvenlik | `KR-018` |
| Sistem yalnızca kurum içinden erişilebilir | Kurumsal | `KR-067` |
| Parola en az 6 karakter (bilinçli ödün, telafi edici kontrollerle) | İK kararı | `KR-070` |
| Sistemde kullanılan değerler koda gömülmez, parametre olur | İK talebi | Y4 parametre kataloğu |

---

## 6. Destekleyici sistem ve hizmetler *(TEC.2 çıktısı h)*

| Bağımlılık | Gerekli mi | Durum |
|---|---|---|
| LOGO Bordro (salt okunur) | Evet — kimlik eşleştirme | ✅ Erişim var, salt okunur yetki doğrulandı |
| NetGSM (SMS) | Evet — doğrulama kodu | ✅ DSG-HRMS için ayrı API kullanıcısı var |
| Kurum SMTP sunucusu | Evet — doğrulama kodu, parola bağlantısı | ✅ Mevcut |
| TLS sertifikası | Evet — güvenli çerez (`Secure`) | ✅ UAT'de devrede (`KR-067`) |

**Veri ön koşulu (22.09.2026 ölçümü):** 564 aktif kişiden 2'sinde hiçbir iletişim
bilgisi yok, 4'ünde kurumsal e-posta yok; kişisel e-posta ve paylaşılan adres kalmadı.
İK iki düzeltme turunda hiçbir kanaldan doğrulanamayan kişi sayısını **12 → 4 → 2**'ye
indirdi.

---

## 7. Mutabakat *(TEC.2 çıktısı g)*

**Üretilen paydaş gereksinimleri:** `REQ-KMLK-001` … `REQ-KMLK-056` (56 gereksinim)
— bkz. `paydas-gereksinimleri/PG-KMLK.md`.

| Katılımcı | İhtiyaçların doğru yansıdığını onaylıyor mu | Tarih |
|---|---|---|
| Doğuş Uçanok | Evet | 2026-09-23 |
| Gamze Demir | Evet | 2026-09-23 |
| Ahmet Erdoğan | Evet | 2026-09-23 |

**Çekince veya şerh (toplantıda):** yok.

> Toplantıdan **sonra** Bilgi İşlem tarafından yapılan değerlendirme §10'dadır ve
> toplantı mutabakatının parçası değildir.

---

## 8. Açık soruların cevapları

13 sorunun tamamı cevaplandı.

| # | Soru | İK cevabı | Gereksinime etkisi |
|---|---|---|---|
| S-01 | Giriş kimliği ne olsun? | **E-posta ile giriş uygun.** | REQ-KMLK-022 netleşti |
| S-02 | İlk girişte parola yeniden değiştirilsin mi? | Üyelikte belirlenen parola yeterli. Sistem parametresi olsun, **varsayılanı kapalı**. | REQ-KMLK-055 eklendi, PRM-KML-20 |
| S-03 | Kilitlenen hesabı İK hemen açabilsin mi? | **İK açabilsin** (parametre, varsayılan açık; kapatılabilir). | T5: REQ-KLNC-008, PRM-HSP-03 |
| S-04 | İletişim bilgisi olmayan personelin hesabını kim açacak? | İstisna olarak **İK açabilecek**. *(İK notu: soru metninde 12 kişi yazıyordu, son ölçümde 2 kişi — güncellenmesi unutulmuş.)* | REQ-KMLK-011 netleşti |
| S-05 | Elle açılan hesabın parolası nasıl iletilecek? | **Revize edildi (24.09.2026, §11):** İK tetikler; personelin **LOGO'da tanımlı kurumsal e-postası varsa** sistem o adrese otomatik olarak **tek kullanımlık bağlantı** gönderir. Kurumsal e-posta tanımlı değilse bağlantı **gönderilemez.** Bağlantı geçerlilik süresi sistem parametresidir, **varsayılan 3 saat.** Personel parolasını bağlantı üzerinden kendisi oluşturur. *(Toplantıdaki ilk cevap: İK e-posta adresini elle girecekti.)* | REQ-KMLK-011 netleşti, PRM-HSP-04 |
| S-06 | Aynı kullanıcı birden fazla cihazda açık kalabilsin mi? | **Yeni giriş öncekini sonlandıracak.** | REQ-KMLK-049/050 (15.09'da netleşmişti) |
| S-07 | Oturum süresi ve hareketsizlik uygun mu? | Hareketsizlik varsayılanı **30 dk** uygun. | REQ-KMLK-026 değişti, PRM-KML-13 |
| S-08 | TCKN + doğum tarihi + e-posta yeterli mi? | **Yeterli.** | Değişiklik yok |
| S-09 | Ayrılan personel geçmiş kayıtlarını görebilsin mi? | **Hayır, göremeyecek.** | Değişiklik yok (REQ-KMLK-034) |
| S-10 | "Sorun yaşarsanız" bilgisi ne olsun? | **Bilgi İşlem** olmalı. | REQ-KMLK-056 eklendi, PRM-GRN-04 |
| S-11 | Devreye alma nasıl duyurulacak? | **Toplu e-posta** ile; içerik İK tarafından hazırlanacak. | Sistem dışı; devreye alma (TEC.10) sırasında uygulanacak |
| S-12 | Eksik iletişim bilgileri ne zamana kadar tamamlanır? | *(İK notu: soru metninde 12 kişi yazıyordu, son ölçümde 2 kişi.)* **Kalan bu kişiler sisteme giriş yapmayacak.** | `KR-076` |
| S-13 | Giriş ekranının görsel bölümünde ne yer alsın? | **Claude tarafından en uygun görsel üretilsin.** | REQ-KMLK-047 notu |

> **Bayat sayı kaydı:** S-04 ve S-12'nin metinlerinde iletişim bilgisi olmayan kişi
> sayısı 09.09 ölçümüyle (12) kalmış, 21.09 ve 22.09 ölçümlerinde güncellenmemişti.
> Hatayı **İK toplantıda fark etti.** Kök neden: Bilgi İşlem'in taslak taramaları
> yalnızca "Gereksinimler" sayfasını kapsıyordu; "Açık Sorular" sayfası taranmıyordu.
> 24.09'da tüm taslakların tüm sayfaları tarandı: Sistem Yönetimi taslağında iki bayat
> atıf daha bulundu (2FA cevabı ve PRM-HSP-01 notu). Depoda da iki yerde (ADR-0006 ve
> vizyon belgesi) aynı dönemin "39 personel" ifadesi kalmıştı. Hepsi düzeltildi.

---

## 9. Sonraki adımlar

| # | İş | Sorumlu |
|---|---|---|
| 1 | Onaylı gereksinimlerin depoya alınması (`PG-KMLK.md`) | Bilgi İşlem |
| 2 | İzlenebilirlik matrisine T3 satırlarının açılması | Bilgi İşlem |
| 3 | §10'daki iki çekincenin İK ile teyidi | Bilgi İşlem + İK |
| 4 | T3 sistem gereksinimleri (TEC.3) ve geliştirmeye başlanması | Bilgi İşlem |
| 5 | Devreye alma duyurusunun hazırlanması | İK |

---

## 10. Toplantı sonrası Bilgi İşlem değerlendirmesi

> **Durum: iki çekince de 24.09.2026'da İK ile görüşülerek çözüldü** — bkz. §11.

> **Bu bölüm toplantı mutabakatının parçası değildir.** Toplantıdan sonra, cevapların
> birbiriyle ve mevcut kararlarla tutarlılığı incelenirken tespit edilmiştir. Her ikisi
> de **İK kararını değiştirmez**; kararın uygulanma biçimine ilişkin bir çekincedir ve
> İK ile teyit edilecektir.

### 10.1 E-posta ile giriş, kurumsal e-postası olmayan personeli dışarıda bırakır — ✅ çözüldü

**Durum:** Giriş kimliği kurumsal e-posta oldu (S-01). 22.09.2026 ölçümüne göre
**4 aktif kişide kurumsal e-posta yok:**

| | Kişi | Sonuç |
|---|---:|---|
| Ne e-posta ne telefon | 2 | İK: sisteme giriş yapmayacak (S-12) — **karar verildi** |
| Telefonu var, e-postası yok | **2** | SMS ile üye olabilir, **ama giriş yapamaz** — giriş kimliği yok |

İkinci satırdaki 2 kişi için hiçbir karar alınmadı; S-12 yalnızca iletişim bilgisi
**hiç olmayan** kişileri kapsıyor.

**Önerimiz:** Bu 2 kişiye kurumsal e-posta tanımlanması (veri düzeltmesi — İK'nın
izlediği yolla uyumlu). Alternatif olarak TCKN'nin ikinci giriş kimliği olması
mümkündür, ancak S-01 kararıyla çelişir ve saldırı yüzeyini büyütür (TCKN, e-postaya
göre çok daha kolay ele geçirilir).

**Bir sonuç daha:** Kurumsal e-posta artık giriş kimliği olduğu için **kişiye tekil
olmak zorundadır.** Paylaşılan adres yasağı (`KR-018`) bir güvenlik kuralı olmaktan
çıkıp bir **veritabanı kısıtına** dönüşür. Bugün paylaşılan adres yok (22.09 ölçümü:
0), ancak LOGO'da yarın açılacak bir kayıt bu durumu yeniden üretebilir; senkronizasyon
bu durumda kaydı uyarı listesine almalıdır.

### 10.2 Elle hesap açmada girilen e-posta adresinin sınırlanması — ✅ çözüldü

**Durum:** İK, e-posta adresini girip onaylayacak; tek kullanımlık bağlantı o adrese
gidecek (S-05).

**Risk:** Bu akışın kullanılacağı kişilerin LOGO'da e-posta adresi **yoktur** — adres
İK tarafından elle girilir. Adres sınırlanmazsa:

1. Bağlantı kurum dışı bir adrese gidebilir → `KR-019` ile çelişir.
2. Bağlantı **başka bir personelin** adresine gidebilir → o kişi, bağlantıyla hesabı ele
   geçirir → `KR-018` ile çelişir.
3. Girilen adres yalnızca HRMS'te yaşar, LOGO'da yoktur → e-posta ile giriş (S-01)
   nedeniyle **iki farklı doğruluk kaynağı** oluşur; bir sonraki senkronizasyonda
   çakışabilir.

**Önerimiz:**

- Girilen adres **kabul edilen kurumsal alan adlarıyla** sınırlansın (PRM-KML-01).
- Adres LOGO'da **başka bir kişiye tanımlı olmasın.**
- Bağlantı tek kullanımlık olsun ve **süreli** olsun (öneri: 24 saat, parametre).
- İşlem gerekçe ister ve denetim izine yazılır (bu zaten gereksinimde var).
- En sağlıklı çözüm: İK, adresi **LOGO'ya** da girsin — o zaman kişi normal üyelik
  akışıyla kendi başına üye olabilir ve istisna akışına hiç gerek kalmaz.

**Pratik etki:** Bugün düşük. İletişim bilgisi olmayan 2 kişinin sisteme girmeyeceği
kararlaştırıldı (S-12). Ancak akış kalıcıdır ve gelecekteki her personeli etkiler;
kuralları şimdi netleştirmek, sonradan açık kapatmaktan ucuzdur.

---

## 11. Toplantı sonrası teyit — 2026-09-24

§10'daki iki çekince, Bilgi İşlem Birim Sorumlusu tarafından İK ile ayrıca görüşüldü.
İK, S-05 cevabını doldurulmuş gereksinim dosyasında **kendisi revize etti.**

### 11.1 → §10.1: telefonu olup e-postası olmayan 2 kişi

**Karar:** İK bu kişilere **kurumsal e-posta tanımlayacak.**

Böylece e-posta ile giriş kararı (S-01, `KR-073`) hiçbir aktif personeli dışarıda
bırakmaz — hiçbir iletişim bilgisi olmayan 2 kişi hariç; onlar için ayrı karar var
(`KR-076`).

### 11.2 → §10.2: elle hesap açmada adres

**Karar (S-05 revize):** İK **adres girmez.** Bağlantı yalnızca personelin **LOGO'da
tanımlı kurumsal e-postasına** gider; tanımlı değilse gönderilemez. Bağlantı
tek kullanımlıktır, geçerlilik süresi parametredir (**varsayılan 3 saat**, PRM-HSP-04).

Bu karar, §10.2'deki üç riskin **üçünü birden** ortadan kaldırır:

| Risk | Nasıl kapandı |
|---|---|
| Bağlantının kurum dışına gitmesi (`KR-019`) | Adres LOGO'dan gelir; LOGO adresleri zaten kurumsal alan adı kuralına tabi |
| Bağlantının başka bir personele gitmesi (`KR-018`) | İK adres giremediği için bu yol yok; paylaşılan adres kuralı LOGO adresine de uygulanır |
| HRMS ile LOGO arasında iki doğruluk kaynağı | Adres yalnızca LOGO'dan okunur; HRMS'te ayrı bir adres tutulmaz |

Bilgi İşlem'in önerisinden **daha güçlü** bir çözümdür: öneri, elle girilen adresi
kurallarla sınırlamaktı; İK kararı elle girişi tamamen kaldırdı.

### 11.3 Akışın niteliği değişti — not

Bağlantı artık yalnızca kurumsal e-postası **olan** personele gönderilebildiği için bu
akış, "iletişim bilgisi olmayanlar için istisna" olmaktan çıktı; **İK destekli davet**
hâline geldi. Kurumsal e-postası olan personel zaten kendi başına üye olabilir; bu akış,
üyelik adımlarında zorlanan veya İK'nın doğrudan davet etmek istediği personel içindir.

İletişim bilgisi olmayan personel için sisteme giriş yolu kalmamıştır — bu, `KR-076`
ile tutarlıdır.

### 11.4 Kapsam kararı — T1 çekirdeği

Onaylı T3 gereksinimlerinin bir kısmı (REQ-KMLK-002, 003, 013, 014, 034) LOGO'dan
senkronize edilmiş kişi/istihdam verisine dayanır. Karar: **yalnızca T3'ün ihtiyaç
duyduğu kadarı** (kişi/istihdam modeli + LOGO senkronizasyonu) T3 kapsamında kurulur;
T1 Personel Yönetimi ekranları T1'in kendi gereksinim toplantısından sonra geliştirilir
(`KR-077`, `KR-068`).

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-24 | 1.0 | Toplantı kaydı | Bilgi İşlem |
| 2026-09-24 | 1.1 | §11 eklendi: iki çekincenin İK ile teyidi (S-05 revize, 2 kişiye e-posta tanımı) ve T1 kapsam kararı | Bilgi İşlem |
