# ADR-0017 — İki Adımlı Doğrulamanın Kullanıcı Tercihine Bağlanması

**Durum:** **Kabul Edildi**
**Tarih:** 2026-10-10 (karar) · 2026-10-14 (ADR olarak kayda geçti, #212)
**Karar defteri karşılığı:** `KR-104`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)
**Değiştirdiği:** ADR-0006 §7'nin iki uygulama kuralı: "2FA açıkken parola doğrulandıktan sonra doğrulama kodu istenir" (herkesten) ve "Parametre açılmadan önce hiçbir doğrulama kanalı bulunmayan personel listelenir ve uyarı verilir". ADR-0006 §7'nin geri kalanı yürürlüktedir: 2FA geliştirilir, Sistem Yönetimi parametresiyle (PRM-KML-08) yönetilir, varsayılanı kapalıdır, üyelikle aynı kod altyapısını kullanır, açık ve kapalı hâlleri ayrı test edilir.

> **Neden yeni ADR?** Karar 10.10.2026'da ADR-0006 §7'ye "gerçekleştirme notu" olarak
> yazılmıştı. T3 modül sonu denetimi (MAN.8 M-07, Ek B TEC.5-7) bunun iki kuralın yerine
> başka bir çözüm koyduğunu tespit etti. `KR-099` bu durumda yeni ADR ister; belirsizlikte
> de yeni ADR yazılır.

---

## Bağlam

ADR-0006 §7 (21.09.2026, `KR-069`), 2FA'yı sistem genelinde bir anahtar olarak tasarladı:
parametre açılınca **herkes** parolasından sonra kod girer. Bu tasarımın iki sonucu vardı:

- Doğrulama kanalı olmayan personel (cep telefonu ve kurumsal e-postası olmayan; R-08)
  parametre açılınca sisteme giremezdi. Bu yüzden parametre açılmadan önce bu kişilerin
  listelenip uyarı verilmesi gerekiyordu (REQ-KMLK-052).
- 2FA herkese birden açılır ya da kapanırdı; ara bir durum yoktu.

UAT'de İK ile yapılan denemelerde (10.10.2026) İK, 2FA'nın herkese birden uygulanmasını
istemedi: kullanıcı kendi güvenliğini seçebilmeli. Değişiklik talebi #190 olarak işlendi ve
İK tarafından onaylandı.

## Karar

**Sistem parametresi 2FA'yı yalnızca kullanıma açar; her kullanıcı kendi hesabında açar ve
kapatır. Kullanıcı tercihi varsayılan olarak kapalıdır.**

| Sistem 2FA (PRM-KML-08) | Kullanıcının tercihi | Giriş |
|---|---|---|
| Kapalı | herhangi | Tek adım |
| Açık | Kapalı (varsayılan) | Tek adım |
| Açık | Açık | Parola + kod |

- **Açma:** Mevcut parola ve seçilen kanala gönderilen kodun doğrulanması gerekir
  (`VerificationPurpose.TwoFactorSetup`). Kod yalnızca o kişiye ve bu amaca aittir.
  Doğrulama kanalı olmayan kullanıcı 2FA'yı açamaz.
- **Kapatma:** Mevcut parola.
- **Kurtarma:** Kodu alamayan kişinin (e-posta ve telefon erişimi yok) 2FA'sını İK,
  "Hesap işlemleri" ekranından gerekçe girerek kapatır (SYG-KMLK-081). Kullanıcı kendi
  hesabına bu işlemi uygulayamaz.
- **Parametre açılırken uyarı (PRM-KML-15):** 2FA'yı kendi hesabında açmış aktif kişi
  sayısı gösterilir; sayı sıfırdan büyükse onay istenir.
- Kod altyapısı tek kalır (ADR-0006 §3).

## Gerekçe

- **Kilitlenme riski kaynağında kalkar.** 2FA'yı ancak kanalına gelen kodu doğrulayan
  kişi açabildiği için, kanalı olmayan kişi hiçbir zaman 2FA ile karşılaşmaz. ADR-0006
  §7'deki "listele ve uyar" kuralına gerek kalmaz.
- **Kullanım kolaylığı.** Kullanıcıların çoğu seyrek ve düşük teknik seviyelidir (`KR-048`);
  zorunlu ikinci adım destek yükünü artırır. Güvenliğe daha çok önem veren kullanıcı açabilir.
- **Uygulama yalnızca yerel ağda çalışır** (ADR-0006 varsayımı). Bu varsayım değişirse
  (internete açılma), 2FA'nın zorunlu olması yeniden değerlendirilir (ADR dizini, gözden
  geçirme tetikleyicileri).

## Değerlendirilen alternatifler

| Alternatif | Artıları | Eksileri | Neden seçilmedi |
|---|---|---|---|
| ADR-0006 §7 olduğu gibi (sistem geneli) | Tek anahtar; davranış herkes için aynı | Kanalı olmayan kilitlenir; İK istemedi | Paydaş onaylamadı (#190) |
| Rol bazlı zorunluluk (ör. yöneticilere zorunlu) | Yetkili hesaplar korunur | Rol yönetimi T4'te; T3'te karşılığı yok | Kapsam dışı; T4'te değerlendirilebilir |
| Kullanıcı tercihi, kodla açma (**seçilen**) | Kilitlenme yok; kullanıcı seçer | 2FA'yı açmayan hesap tek adımla girer | — |

## Sonuçlar

**Olumlu:**
- Doğrulama kanalı olmayan personel hiçbir koşulda 2FA'da kalmaz.
- Parametre açıldığında kimse için davranış birden değişmez.

**Olumsuz / kabul edilen ödünler:**
- Parametre açıkken bile 2FA'yı açmayan hesaplar tek adımla girer.
- Kanallarını sonradan kaybeden kullanıcı İK'ya başvurmak zorundadır (R-27); İK talep edenin
  kimliğini yüz yüze veya bilinen bir kurum içi kanaldan doğrular.

**Bu kararın getirdiği yükümlülükler:**
- Dört durumun (sistem × kullanıcı) her biri otomatik testle doğrulanır (REQ-KMLK-053,
  SYG-KMLK-036; `SessionApiTests`, `TwoFactorPreferenceApiTests`).
- Açma, kapatma ve İK kurtarması denetim izine ve güvenlik olaylarına yazılır.

## Geri dönüş maliyeti

**Düşük–Orta.** Sistem geneli davranışa dönmek için giriş koşulundan kullanıcı tercihi
çıkarılır ve "kanalı olmayanları listele" uyarısı geri getirilir. Kullanıcı tercihi verisi
(`identity.user_account.two_factor_enabled`) kalabilir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-14 | 1.0 | İlk oluşturma: `KR-104` (10.10.2026) ADR olarak kayda geçti; ADR-0006 §7'deki gerçekleştirme notunun yerini aldı (`KR-099`, #212) | Bilgi İşlem |
