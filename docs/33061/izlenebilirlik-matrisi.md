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
Paydaş ihtiyacı  →  PG-<MODÜL>-nn  →  SYG-<MODÜL>-nnn  →  Tasarım  →  Kod / PR  →  Test  →  Kabul
   (toplantı)        (TEC.2)          (TEC.3)         (TEC.5)     (TEC.7)    (TEC.9)  (TEC.11)
```

Her halka bir öncekine **kimlikle** bağlanır. Bir halka kopuksa, kopukluk matriste
görünür — gizlenmez.

### 2.1 Sütunlar

| Sütun | Ne yazılır | Kaynak |
|---|---|---|
| **Kaynak** | Paydaş gereksinimi (`PG-…`) veya kurumsal karar (`KR-…`) | TEC.2 / karar kayıt defteri |
| **Sistem gereksinimi** | `SYG-<MODÜL>-nnn` (TEC.3 YAKLASIM §3.1) | TEC.3 |
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
| Sistem gereksinimi yazıldığında | Bilgi İşlem | "Sistem gereksinimi" sütununu doldurur; tutarlılık her PR'da otomatik denetlenir (TEC.3 YAKLASIM §4) |
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
| `KR-003`, `KR-004` LOGO salt-okunur | Kod ve test | LOGO yalıtım katmanı **T3**'te yazılacak (`KR-077`; `SYG-KMLK-002`, `003`). Kısıt bugün veritabanı düzeyinde `DENY` ile uygulanıyor ve elle doğrulandı; **depoda kanıt üreten bir test yok**. T3'te CI'da çalışan otomatik teste bağlanacak. |
| `KR-056` Bildirim istisnası (#3) | Tasarım sonrası tüm halkalar | Y1 Bildirim Merkezi'nde uygulanacak (A3) |
| `KR-068` Modül başlangıç koşulu | Kod/test | Süreç kuralı; kodda karşılığı yok — `yok (süreç kuralı)` |

> Bu tablo bilinçli olarak tutuluyor: **kopuk halkayı göstermek, matrisi dolu
> göstermekten daha değerlidir.** Bir denetçi, eksiğin bilindiğini ve nereye
> bağlandığını görmek ister.

---

## 5. T3 — Kimlik Yönetimi

> İlk gerçek modül çevrimi. Gereksinimler **23.09.2026 İK toplantısında onaylandı**
> (`TEC.2/kayitlar/2026-09-23-kimlik-gereksinim-toplantisi.md`); 56 satırın tamamı
> açıldı. **Kaynak** ve **Sistem gereksinimi** sütunları dolu (`TEC.3/gereksinimler/SYG-KMLK.md`,
> 79 madde); diğer halkalar işin yapıldığı PR'larda doldurulacak
> (§3). Kaynak metinlerin tamamı: `TEC.2/paydas-gereksinimleri/PG-KMLK.md`.

| # | Kaynak | Sistem gereksinimi | Tasarım | Gerçekleştirme | Doğrulama | Kabul |
|---|---|---|---|---|---|---|
| 1 | `REQ-KMLK-001` Personel, giriş ekranındaki "Üye Ol" bağlantısıyla kendi hesabını kend… | SYG-KMLK-013, 079 | — | — | — | — |
| 2 | `REQ-KMLK-002` Üyelik doğrulaması TCKN + doğum tarihi + kurumsal e-posta bilgileriyle… | SYG-KMLK-006, 013, 014 | — | — | — | — |
| 3 | `REQ-KMLK-003` Eşleşme LOGO anlık görüntüsü üzerinden yapılır; yalnızca AKTİF istihda… | SYG-KMLK-001, 002, 003, 004, 006, 011, 012, 013, 072 | — | — | — | — |
| 4 | `REQ-KMLK-004` Bilgiler eşleşse de eşleşmese de kullanıcıya AYNI ekran ve AYNI mesaj … | SYG-KMLK-015, 016, 018, 020 | — | — | — | — |
| 5 | `REQ-KMLK-005` Doğrulama kanalı kullanıcıya seçtirilir: e-posta veya SMS. Hangi kanal… | SYG-KMLK-016, 017, 030, 075 | — | — | — | — |
| 6 | `REQ-KMLK-006` SMS seçilip kod ulaşmazsa kullanıcı aynı ekrandan e-posta kanalına geç… | SYG-KMLK-019 | — | — | — | — |
| 7 | `REQ-KMLK-007` Kurumsal e-posta adresi bulunmayan personel için e-posta kanalı sunulm… | SYG-KMLK-017 | — | — | — | — |
| 8 | `REQ-KMLK-008` Kurumsal alan adı dışındaki e-posta adreslerine (gmail, hotmail vb.) d… | SYG-KMLK-009, 017 | — | — | — | — |
| 9 | `REQ-KMLK-009` Aynı e-posta adresi birden fazla kişiye tanımlıysa o adrese doğrulama … | SYG-KMLK-008, 017 | — | — | — | — |
| 10 | `REQ-KMLK-010` Cep telefonu tanımlı olmayan personel için SMS kanalı sunulmaz. | SYG-KMLK-009, 017 | — | — | — | — |
| 11 | `REQ-KMLK-011` İK, personele tek kullanımlık parola bağlantısı gönderebilir; yalnızca LOGO'daki kurumsal e-postaya… | SYG-KMLK-030, 051, 052, 053, 073, 074 | — | — | — | — |
| 12 | `REQ-KMLK-012` Kabul edilen kurumsal alan adları koda gömülmez; Sistem Yönetimi param… | SYG-KMLK-017, 075, 076 | — | — | — | — |
| 13 | `REQ-KMLK-013` TCKN’si bulunmayan LOGO kartı için sistemde Kişi kaydı oluşturulmaz; k… | SYG-KMLK-007 | — | — | — | — |
| 14 | `REQ-KMLK-014` Bir kişinin birden fazla sicil numarası olsa da TEK hesabı olur. | SYG-KMLK-001, 020, 021 | — | — | — | — |
| 15 | `REQ-KMLK-015` Doğrulama kodu 6 hanedir (parametre: PRM-KML-09) ve kriptografik güven… | SYG-KMLK-022 | — | — | — | — |
| 16 | `REQ-KMLK-016` Kod 5 dakika geçerlidir (parametre: PRM-KML-10). | SYG-KMLK-023, 079 | — | — | — | — |
| 17 | `REQ-KMLK-017` Kod en fazla 3 kez yanlış girilebilir; aşılırsa kod iptal edilir ve ye… | SYG-KMLK-024 | — | — | — | — |
| 18 | `REQ-KMLK-018` Kod veritabanında şifrelenmiş (hash) saklanır; düz metin tutulmaz ve h… | SYG-KMLK-025, 026 | — | — | — | — |
| 19 | `REQ-KMLK-019` Doğrulanan kod anında geçersiz kılınır (tek kullanımlık). | SYG-KMLK-027 | — | — | — | — |
| 20 | `REQ-KMLK-020` Kod ekranında kalan süre görünür ve "kodu tekrar gönder" seçeneği bulu… | SYG-KMLK-028 | — | — | — | — |
| 21 | `REQ-KMLK-021` SMS gönderimleri NetGSM standart servisi üzerinden, DUZEN başlığıyla y… | SYG-KMLK-029, 075, 079 | — | — | — | — |
| 22 | `REQ-KMLK-022` Kullanıcı kurumsal e-posta adresi ve parolasıyla giriş yapar. | SYG-KMLK-008, 031, 077 | — | — | — | — |
| 23 | `REQ-KMLK-023` Hatalı girişte "kullanıcı adı veya parola hatalı" denir; hangisinin ya… | SYG-KMLK-032 | — | — | — | — |
| 24 | `REQ-KMLK-024` 5 başarısız giriş denemesinden sonra hesap 15 dakika kilitlenir (param… | SYG-KMLK-033 | — | — | — | — |
| 25 | `REQ-KMLK-025` Giriş sonrası ikinci doğrulama adımı (2FA) GELİŞTİRİLİR; varsayılan ol… | SYG-KMLK-034 | — | — | — | — |
| 26 | `REQ-KMLK-026` Oturum 15 dakikalık erişim jetonu ve 8 saatlik yenileme jetonu ile yön… | SYG-KMLK-037, 038, 040, 063 | — | — | — | — |
| 27 | `REQ-KMLK-027` Çıkış yapıldığında oturum sunucu tarafında da sonlandırılır. | SYG-KMLK-043 | — | — | — | — |
| 28 | `REQ-KMLK-028` Parola en az 6 karakter olmalıdır; karmaşıklık (büyük/küçük harf, raka… | SYG-KMLK-044, 075, 076 | — | — | — | — |
| 29 | `REQ-KMLK-029` Yaygın sızıntı listesinde bulunan parolalar kabul edilmez; kontrol çev… | SYG-KMLK-045 | — | — | — | — |
| 30 | `REQ-KMLK-030` Zorunlu periyodik parola değişimi varsayılan olarak uygulanmaz (parame… | SYG-KMLK-046 | — | — | — | — |
| 31 | `REQ-KMLK-031` Parola sıfırlama, üyelikle aynı doğrulama akışını kullanır (kanal seçi… | SYG-KMLK-047 | — | — | — | — |
| 32 | `REQ-KMLK-032` Oturum içinde parola değiştirirken mevcut parola sorulur. | SYG-KMLK-048 | — | — | — | — |
| 33 | `REQ-KMLK-033` Parola geri döndürülebilir biçimde saklanmaz ve hiçbir kayda yazılmaz. | SYG-KMLK-049 | — | — | — | — |
| 34 | `REQ-KMLK-034` Personelin tüm aktif istihdamları sona erdiğinde hesabı otomatik pasif… | SYG-KMLK-004, 005, 054, 055, 078 | — | — | — | — |
| 35 | `REQ-KMLK-035` Personel yeniden işe girdiğinde mevcut hesabı yeniden aktifleşir; yeni… | SYG-KMLK-056 | — | — | — | — |
| 36 | `REQ-KMLK-036` İK, bir hesabı elle pasife alabilir; gerekçe zorunludur. | SYG-KMLK-057, 073, 074 | — | — | — | — |
| 37 | `REQ-KMLK-037` Hesap durum değişiklikleri (açılma, pasifleşme, kilitlenme) denetim iz… | SYG-KMLK-053, 058 | — | — | — | — |
| 38 | `REQ-KMLK-038` Üyelik denemeleri TCKN başına saatte 5, IP başına saatte 20 ile sınırl… | SYG-KMLK-059 | — | — | — | — |
| 39 | `REQ-KMLK-039` Kod gönderimi kişi başına 15 dakikada en fazla 3 kez yapılabilir; sını… | SYG-KMLK-059 | — | — | — | — |
| 40 | `REQ-KMLK-040` Tüm kimlik olayları (giriş, başarısız giriş, kilitlenme, kod gönderimi… | SYG-KMLK-005, 010, 060 | — | — | — | — |
| 41 | `REQ-KMLK-041` Kişisel veriler günlük kayıtlarında maskelenir (TCKN 123*901, telefon … | SYG-KMLK-026, 061 | — | — | — | — |
| 42 | `REQ-KMLK-042` Doğrulama kodu ve parola sıfırlama iletileri bildirim istisnasından MU… | SYG-KMLK-062 | — | — | — | — |
| 43 | `REQ-KMLK-043` Tüm ekranlar ve hata mesajları Türkçedir; teknik terim kullanılmaz. | SYG-KMLK-064, 071 | — | — | — | — |
| 44 | `REQ-KMLK-044` Giriş ve üyelik ekranları telefon ve tablette de kullanılabilir. | SYG-KMLK-065 | — | — | — | — |
| 45 | `REQ-KMLK-045` Ekranlar klavye ile kullanılabilir; form alanlarının etiketi vardır. | SYG-KMLK-066 | — | — | — | — |
| 46 | `REQ-KMLK-046` Hata ekranında kullanıcıya bir takip numarası gösterilir. | SYG-KMLK-067 | — | — | — | — |
| 47 | `REQ-KMLK-047` Giriş, üye ol, doğrulama ve parola ekranları ekranı iki bölüme ayıran … | SYG-KMLK-068 | — | — | — | — |
| 48 | `REQ-KMLK-048` Giriş ve doğrulama ekranlarında kurumsal logo kullanılır; logo, Sistem… | SYG-KMLK-069, 076 | — | — | — | — |
| 49 | `REQ-KMLK-049` Bir kullanıcı yeni bir tarayıcıdan giriş yaptığında önceki oturumu son… | SYG-KMLK-041 | — | — | — | — |
| 50 | `REQ-KMLK-050` Oturumun başka bir yerden sonlandırıldığı kullanıcıya açıkça bildirili… | SYG-KMLK-042 | — | — | — | — |
| 51 | `REQ-KMLK-051` 2FA açıkken, parola doğrulandıktan sonra kullanıcıdan doğrulama kodu i… | SYG-KMLK-034 | — | — | — | — |
| 52 | `REQ-KMLK-052` 2FA açıkken hiçbir doğrulama kanalı bulunmayan kullanıcı sisteme girem… | SYG-KMLK-035 | — | — | — | — |
| 53 | `REQ-KMLK-053` 2FA'nın açık ve kapalı hâli (PRM-KML-08) ayrı ayrı test edilir. | SYG-KMLK-036 | — | — | — | — |
| 54 | `REQ-KMLK-054` Meşru uzun süreli etkinlik sırasında (ör. eğitim videosu oynatılırken)… | SYG-KMLK-038, 039 | — | — | — | — |
| 55 | `REQ-KMLK-055` İlk girişte parola değiştirme zorunluluğu Sistem Yönetimi parametresiy… | SYG-KMLK-050 | — | — | — | — |
| 56 | `REQ-KMLK-056` Üyelik ve giriş ekranlarında "sorun yaşarsanız" başvurulacak birim ola… | SYG-KMLK-070, 076 | — | — | — | — |

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
dâhil edilmedi; T3'te dâhil olacak (`KR-077`).

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-18 | 0.1 | İlk oluşturma — yapı, bakım kuralı ve A1 kalemleri (BULGU-01) | Bilgi İşlem |
| 2026-09-24 | 0.2 | §5 T3 satırları açıldı — 23.09.2026 İK toplantısında onaylanan 56 gereksinim | Bilgi İşlem |
| 2026-09-24 | 0.3 | Sistem gereksinimi kimliği `SYG-<MODÜL>-nnn`; §5 "Sistem gereksinimi" sütunu SYG-KMLK ile dolduruldu; LOGO kalemi T1 → T3 (`KR-077`) | Bilgi İşlem |
