# İzlenebilirlik Matrisi

**Belge kimliği:** 33061-IZM
**Son güncelleme:** 2026-09-24
**Hizmet ettiği süreçler:** TEC.2, TEC.3, TEC.5, TEC.7, TEC.8, TEC.9, TEC.10, TEC.11, TEC.13
**Sahibi:** Bilgi İşlem

> **Bu belge, "bu kod hangi gereksinimi karşılıyor?" ve "bu gereksinim nerede
> doğrulandı?" sorularının cevabıdır.** Sekiz sürecin çıktı listesinde izlenebilirlik
> maddesi bulunur; hiçbiri bu matris olmadan karşılanamaz.

---

## 1. Neden var?

TS ISO/IEC TS 33061, aşağıdaki süreç çıktılarında izlenebilirlik ister:

| Süreç | Çıktı | Ne istiyor |
|---|---|---|
| TEC.2 | (i) | Paydaş gereksiniminin, paydaşa ve ihtiyacına kadar izlenebilmesi |
| TEC.3 | (f) | Sistem gereksiniminin paydaş gereksinimine bağlanması |
| TEC.5 | (h) | Tasarım özelliklerinin mimari öğelere bağlanması |
| TEC.7 | (e) | Gerçekleştirmenin izlenebilir olması |
| TEC.8 | (h) | Entegre edilen öğelerin izlenebilir olması |
| TEC.9 | (g) | Doğrulanan öğelerin izlenebilir olması |
| TEC.10 | (h) | Geçişi yapılan öğelerin izlenebilir olması |
| TEC.11 | (h) | Geçerlenen öğelerin izlenebilir olması |

18.09.2026 süreç gözden geçirmesi, bu matrisin **hiç oluşturulmamış** olmasını en
yüksek önemli bulgu (BULGU-01) olarak kaydetti: tek başına bu eksik, sekiz sürecin
`PA 1.1 = F` almasını yapısal olarak engelliyordu.

---

## 2. Zincir

```
Paydaş ihtiyacı  →  PG-<MODÜL>-nn  →  REQ-<MODÜL>-nn  →  Tasarım  →  Kod / PR  →  Test  →  Kabul
   (toplantı)        (TEC.2)          (TEC.3)         (TEC.5)     (TEC.7)    (TEC.9)  (TEC.11)
```

Her halka bir öncekine **kimlikle** bağlanır. Bir halka kopuksa, kopukluk matriste
görünür — gizlenmez.

### 2.1 Sütunlar

| Sütun | Ne yazılır | Kaynak |
|---|---|---|
| **Kaynak** | Paydaş gereksinimi (`PG-…`) veya kurumsal karar (`KR-…`) | TEC.2 / karar kayıt defteri |
| **Sistem gereksinimi** | `REQ-<MODÜL>-nn` | TEC.3 |
| **Tasarım** | `ADR-nnnn` veya `docs/mimari/` altındaki belge | TEC.5 |
| **Gerçekleştirme** | Kod yolu + birleştiren PR numarası | TEC.7 |
| **Doğrulama** | Test dosyası veya CI kapısı | TEC.9 |
| **Kabul** | Kabul formu / kabul raporu | TEC.11 |

### 2.2 Kaynak neden iki türlü olabilir?

Zincirin başı çoğunlukla bir **paydaş gereksinimidir**. Ancak bazı kalemler paydaştan
değil, **kurumsal bir karardan** doğar: "LOGO'ya yazma yapılmayacak" gibi. Bunlar
`KR-` kimliğiyle girer ve aynı zinciri izler.

Bu ayrım bilinçlidir: kurumsal kısıtı paydaş gereksinimi gibi göstermek, gereksinimin
kaynağını yanlış kaydeder — ki izlenebilirliğin asıl amacı **kaynağı** doğru
tutmaktır.

---

## 3. Bakım kuralı

> **Matris, değişikliğin yapıldığı PR'da güncellenir.** Ayrı bir iş olarak
> bırakılmaz.

Gerekçe: ayrı bırakılan izlenebilirlik kaydı **çürür**. Bir hafta sonra kimse hangi
kodun hangi gereksinimden geldiğini hatırlamaz; matris "sonra doldurulacak" satırlarla
dolar ve kanıt değerini kaybeder. Denetimde değerli olan, bağın **iş yapılırken**
kurulmuş olmasıdır.

| Ne zaman | Kim | Ne yapar |
|---|---|---|
| Paydaş gereksinimi onaylandığında | Bilgi İşlem | Satırı açar; `PG` ve kaynak dolu, gerisi boş |
| Sistem gereksinimi yazıldığında | Bilgi İşlem | `REQ` sütununu doldurur |
| Kod birleştirildiğinde | PR sahibi | `Gerçekleştirme` ve `Doğrulama` sütunlarını doldurur |
| Modül kabul edildiğinde | Bilgi İşlem | `Kabul` sütununu doldurur |

**Boş hücre yasak değildir; açıklamasız boş hücre yasaktır.** Sırası gelmemiş bir
halka `—` ile, uygulanmayan bir halka `yok (gerekçe)` ile işaretlenir.

---

## 4. A1 — Teknik İskelet

> Bu bölüm, matrisin **çalıştığını gösterir**. A1'de paydaş gereksinimi üretilmedi
> (modüller başlamamıştı); zincirlerin başı kurumsal kararlardır. Buna rağmen karar →
> tasarım → kod → test zinciri **eksiksiz izlenebilmektedir**.

| # | Kaynak | Sistem gereksinimi | Tasarım | Gerçekleştirme | Doğrulama | Kabul |
|---|---|---|---|---|---|---|
| 1 | `KR-060` Denetim izi değiştirilemez | — (A2'de) | `ADR-0009` §2 | `src/backend/.../Audit/` · PR #12 | `AuditImmutabilityTests.cs`, `AuditTrailTests.cs` | — |
| 2 | `KR-061` Erişim kaydı fail-closed | — | `ADR-0009` §3 | PR #14 | `AccessLogTests.cs` | — |
| 3 | `KR-059` Günlükte iki katmanlı maskeleme | — | `ADR-0009` §4 | `MaskingDestructuringPolicy` · PR #10 | `MaskingDestructuringPolicyTests.cs`, `MaskTests.cs`, `MaskRulesTests.cs` | — |
| 4 | `KR-062` Hata ayrıntısı hiçbir ortamda sızmaz | — | `ADR-0010` §5 | Problem Details ara katmanı · PR #16 | `ProblemDetailsTests.cs`, `problemDetails.test.ts` | — |
| 5 | `KR-063` Sağlık kontrollerinin ayrımı | — | `ADR-0011` | `/health/live`, `/health/ready` · PR #22 | `HealthCheckTests.cs`, `PersonalDataScrubbingProcessorTests.cs` | — |
| 6 | `ADR-0002` Katman bağımlılık kuralları | — | `ADR-0002` | Katman projeleri · PR #8 | `LayerDependencyTests.cs` (NetArchTest) | — |
| 7 | `ADR-0004` Veritabanı adlandırma standartları | — | `ADR-0004` | EF yapılandırması · PR #8 | `DatabaseConventionTests.cs` | — |
| 8 | `KR-067` UAT ve üretimde TLS zorunlu | — | Runbook §7 | `nginx/tls.conf`, `tls-yenile.sh` · PR #43 | Dağıtım betiğinin doğrulaması (`uat-dagit.sh`) + elle ölçüm | — |

**Kabul sütunu neden boş?** TEC.11 geçerleme, **kabul edilecek bir modül** gerektirir.
A1 teknik iskelettir; İK kabulüne sunulan bir işlev üretmedi. İlk kabul T3 ile
gelecektir.

### 4.1 Zinciri tamamlanmamış kalemler

| Kaynak | Eksik halka | Neden |
|---|---|---|
| `KR-003`, `KR-004` LOGO salt-okunur | Kod ve test | LOGO yalıtım katmanı **T1**'de yazılacak. Kısıt bugün veritabanı düzeyinde `DENY` ile uygulanıyor ve elle doğrulandı; **depoda kanıt üreten bir test yok**. T1'de otomatik doğrulamaya bağlanacak. |
| `KR-056` Bildirim istisnası (#3) | Tasarım sonrası tüm halkalar | Y1 Bildirim Merkezi'nde uygulanacak (A3) |
| `KR-068` Modül başlangıç koşulu | Kod/test | Süreç kuralı; kodda karşılığı yok — `yok (süreç kuralı)` |

> Bu tablo bilinçli olarak tutuluyor: **kopuk halkayı göstermek, matrisi dolu
> göstermekten daha değerlidir.** Bir denetçi, eksiğin bilindiğini ve nereye
> bağlandığını görmek ister.

---

## 5. T3 — Kimlik Yönetimi

> İlk gerçek modül çevrimi. Gereksinimler **23.09.2026 İK toplantısında onaylandı**
> (`TEC.2/kayitlar/2026-09-23-kimlik-gereksinim-toplantisi.md`); 56 satırın tamamı
> açıldı. Kaynak sütunu dolu, diğer halkalar işin yapıldığı PR'larda doldurulacak
> (§3). Kaynak metinlerin tamamı: `TEC.2/paydas-gereksinimleri/PG-KMLK.md`.

| # | Kaynak | Sistem gereksinimi | Tasarım | Gerçekleştirme | Doğrulama | Kabul |
|---|---|---|---|---|---|---|
| 1 | `REQ-KMLK-001` Personel, giriş ekranındaki "Üye Ol" bağlantısıyla kendi hesabını kend… | — | — | — | — | — |
| 2 | `REQ-KMLK-002` Üyelik doğrulaması TCKN + doğum tarihi + kurumsal e-posta bilgileriyle… | — | — | — | — | — |
| 3 | `REQ-KMLK-003` Eşleşme LOGO anlık görüntüsü üzerinden yapılır; yalnızca AKTİF istihda… | — | — | — | — | — |
| 4 | `REQ-KMLK-004` Bilgiler eşleşse de eşleşmese de kullanıcıya AYNI ekran ve AYNI mesaj … | — | — | — | — | — |
| 5 | `REQ-KMLK-005` Doğrulama kanalı kullanıcıya seçtirilir: e-posta veya SMS. Hangi kanal… | — | — | — | — | — |
| 6 | `REQ-KMLK-006` SMS seçilip kod ulaşmazsa kullanıcı aynı ekrandan e-posta kanalına geç… | — | — | — | — | — |
| 7 | `REQ-KMLK-007` Kurumsal e-posta adresi bulunmayan personel için e-posta kanalı sunulm… | — | — | — | — | — |
| 8 | `REQ-KMLK-008` Kurumsal alan adı dışındaki e-posta adreslerine (gmail, hotmail vb.) d… | — | — | — | — | — |
| 9 | `REQ-KMLK-009` Aynı e-posta adresi birden fazla kişiye tanımlıysa o adrese doğrulama … | — | — | — | — | — |
| 10 | `REQ-KMLK-010` Cep telefonu tanımlı olmayan personel için SMS kanalı sunulmaz. | — | — | — | — | — |
| 11 | `REQ-KMLK-011` Hiçbir kanalı kullanılamayan personel için İK hesabı elle açabilir (PR… | — | — | — | — | — |
| 12 | `REQ-KMLK-012` Kabul edilen kurumsal alan adları koda gömülmez; Sistem Yönetimi param… | — | — | — | — | — |
| 13 | `REQ-KMLK-013` TCKN’si bulunmayan LOGO kartı için sistemde Kişi kaydı oluşturulmaz; k… | — | — | — | — | — |
| 14 | `REQ-KMLK-014` Bir kişinin birden fazla sicil numarası olsa da TEK hesabı olur. | — | — | — | — | — |
| 15 | `REQ-KMLK-015` Doğrulama kodu 6 hanedir (parametre: PRM-KML-09) ve kriptografik güven… | — | — | — | — | — |
| 16 | `REQ-KMLK-016` Kod 5 dakika geçerlidir (parametre: PRM-KML-10). | — | — | — | — | — |
| 17 | `REQ-KMLK-017` Kod en fazla 3 kez yanlış girilebilir; aşılırsa kod iptal edilir ve ye… | — | — | — | — | — |
| 18 | `REQ-KMLK-018` Kod veritabanında şifrelenmiş (hash) saklanır; düz metin tutulmaz ve h… | — | — | — | — | — |
| 19 | `REQ-KMLK-019` Doğrulanan kod anında geçersiz kılınır (tek kullanımlık). | — | — | — | — | — |
| 20 | `REQ-KMLK-020` Kod ekranında kalan süre görünür ve "kodu tekrar gönder" seçeneği bulu… | — | — | — | — | — |
| 21 | `REQ-KMLK-021` SMS gönderimleri NetGSM standart servisi üzerinden, DUZEN başlığıyla y… | — | — | — | — | — |
| 22 | `REQ-KMLK-022` Kullanıcı kurumsal e-posta adresi ve parolasıyla giriş yapar. | — | — | — | — | — |
| 23 | `REQ-KMLK-023` Hatalı girişte "kullanıcı adı veya parola hatalı" denir; hangisinin ya… | — | — | — | — | — |
| 24 | `REQ-KMLK-024` 5 başarısız giriş denemesinden sonra hesap 15 dakika kilitlenir (param… | — | — | — | — | — |
| 25 | `REQ-KMLK-025` Giriş sonrası ikinci doğrulama adımı (2FA) GELİŞTİRİLİR; varsayılan ol… | — | — | — | — | — |
| 26 | `REQ-KMLK-026` Oturum 15 dakikalık erişim jetonu ve 8 saatlik yenileme jetonu ile yön… | — | — | — | — | — |
| 27 | `REQ-KMLK-027` Çıkış yapıldığında oturum sunucu tarafında da sonlandırılır. | — | — | — | — | — |
| 28 | `REQ-KMLK-028` Parola en az 6 karakter olmalıdır; karmaşıklık (büyük/küçük harf, raka… | — | — | — | — | — |
| 29 | `REQ-KMLK-029` Yaygın sızıntı listesinde bulunan parolalar kabul edilmez; kontrol çev… | — | — | — | — | — |
| 30 | `REQ-KMLK-030` Zorunlu periyodik parola değişimi varsayılan olarak uygulanmaz (parame… | — | — | — | — | — |
| 31 | `REQ-KMLK-031` Parola sıfırlama, üyelikle aynı doğrulama akışını kullanır (kanal seçi… | — | — | — | — | — |
| 32 | `REQ-KMLK-032` Oturum içinde parola değiştirirken mevcut parola sorulur. | — | — | — | — | — |
| 33 | `REQ-KMLK-033` Parola geri döndürülebilir biçimde saklanmaz ve hiçbir kayda yazılmaz. | — | — | — | — | — |
| 34 | `REQ-KMLK-034` Personelin tüm aktif istihdamları sona erdiğinde hesabı otomatik pasif… | — | — | — | — | — |
| 35 | `REQ-KMLK-035` Personel yeniden işe girdiğinde mevcut hesabı yeniden aktifleşir; yeni… | — | — | — | — | — |
| 36 | `REQ-KMLK-036` İK, bir hesabı elle pasife alabilir; gerekçe zorunludur. | — | — | — | — | — |
| 37 | `REQ-KMLK-037` Hesap durum değişiklikleri (açılma, pasifleşme, kilitlenme) denetim iz… | — | — | — | — | — |
| 38 | `REQ-KMLK-038` Üyelik denemeleri TCKN başına saatte 5, IP başına saatte 20 ile sınırl… | — | — | — | — | — |
| 39 | `REQ-KMLK-039` Kod gönderimi kişi başına 15 dakikada en fazla 3 kez yapılabilir; sını… | — | — | — | — | — |
| 40 | `REQ-KMLK-040` Tüm kimlik olayları (giriş, başarısız giriş, kilitlenme, kod gönderimi… | — | — | — | — | — |
| 41 | `REQ-KMLK-041` Kişisel veriler günlük kayıtlarında maskelenir (TCKN 123*901, telefon … | — | — | — | — | — |
| 42 | `REQ-KMLK-042` Doğrulama kodu ve parola sıfırlama iletileri bildirim istisnasından MU… | — | — | — | — | — |
| 43 | `REQ-KMLK-043` Tüm ekranlar ve hata mesajları Türkçedir; teknik terim kullanılmaz. | — | — | — | — | — |
| 44 | `REQ-KMLK-044` Giriş ve üyelik ekranları telefon ve tablette de kullanılabilir. | — | — | — | — | — |
| 45 | `REQ-KMLK-045` Ekranlar klavye ile kullanılabilir; form alanlarının etiketi vardır. | — | — | — | — | — |
| 46 | `REQ-KMLK-046` Hata ekranında kullanıcıya bir takip numarası gösterilir. | — | — | — | — | — |
| 47 | `REQ-KMLK-047` Giriş, üye ol, doğrulama ve parola ekranları ekranı iki bölüme ayıran … | — | — | — | — | — |
| 48 | `REQ-KMLK-048` Giriş ve doğrulama ekranlarında kurumsal logo kullanılır; logo, Sistem… | — | — | — | — | — |
| 49 | `REQ-KMLK-049` Bir kullanıcı yeni bir tarayıcıdan giriş yaptığında önceki oturumu son… | — | — | — | — | — |
| 50 | `REQ-KMLK-050` Oturumun başka bir yerden sonlandırıldığı kullanıcıya açıkça bildirili… | — | — | — | — | — |
| 51 | `REQ-KMLK-051` 2FA açıkken, parola doğrulandıktan sonra kullanıcıdan doğrulama kodu i… | — | — | — | — | — |
| 52 | `REQ-KMLK-052` 2FA açıkken hiçbir doğrulama kanalı bulunmayan kullanıcı sisteme girem… | — | — | — | — | — |
| 53 | `REQ-KMLK-053` 2FA'nın açık ve kapalı hâli (PRM-KML-08) ayrı ayrı test edilir. | — | — | — | — | — |
| 54 | `REQ-KMLK-054` Meşru uzun süreli etkinlik sırasında (ör. eğitim videosu oynatılırken)… | — | — | — | — | — |
| 55 | `REQ-KMLK-055` İlk girişte parola değiştirme zorunluluğu Sistem Yönetimi parametresiy… | — | — | — | — | — |
| 56 | `REQ-KMLK-056` Üyelik ve giriş ekranlarında "sorun yaşarsanız" başvurulacak birim ola… | — | — | — | — | — |

---

## 6. Kapsama ölçütü

Aşağıdaki oran, her modül kapanışında ölçülür ve öz değerlendirmeye girer:

```
İzlenebilirlik kapsaması = zinciri eksiksiz kalem / toplam kalem
```

Bir kalem, **kaynağından testine kadar** her halkası dolu (veya gerekçeli `yok`) ise
"eksiksiz" sayılır.

**A1 ölçümü:** 8 kalemden 8'i eksiksiz (kabul halkası hariç — sırası gelmedi) → **%100**.
LOGO kalemi zinciri tamamlanmamış olduğu için §4.1'de ayrıca listelendi ve bu orana
dâhil edilmedi; T1'de dâhil olacak.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-18 | 0.1 | İlk oluşturma — yapı, bakım kuralı ve A1 kalemleri (BULGU-01) | Bilgi İşlem |
| 2026-09-24 | 0.2 | §5 T3 satırları açıldı — 23.09.2026 İK toplantısında onaylanan 56 gereksinim | Bilgi İşlem |
