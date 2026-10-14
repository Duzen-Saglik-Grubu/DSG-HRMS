# ADR-0018 — Ortamların Verisi: Geliştirme ve UAT Gerçek Personel Verisiyle Çalışır

**Durum:** **Kabul Edildi**
**Tarih:** 2026-10-14 (ADR olarak kayda geçti, #212). Kararlar: yerel geliştirme ortamı 28.09.2026; UAT riski Üst Yönetim tarafından 09.10.2026'da kabul edildi.
**Karar defteri karşılığı:** `KR-108` (bu ADR ile), `KR-024`, `KR-083`, `KR-098`, `KR-103`, `KR-106`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama), TEC.10 (Geçiş)
**Değiştirdiği:** ADR-0011 §7 "Ortamlar" tablosunun **Veri** sütunu: geliştirme için "Sentetik", UAT için "Gerçek veriden maskelenmiş kopya". ADR-0011'in geri kalanı yürürlüktedir; CI ve otomatik testler sentetik veriyle çalışmaya devam eder.

> **Neden yeni ADR?** ADR-0011 §7 (06.09.2026) geliştirme ve UAT için gerçek olmayan veri
> öngörüyordu. Uygulamada iki ortam da gerçek personel verisiyle çalışıyor; kararlar
> runbook'ta, risk defterinde ve Üst Yönetim onayında yazılıydı ama ADR güncellenmemişti
> (T3 modül sonu denetimi MAN.8 M-07, Ek C YB-11). Karar tersine döndüğü için `KR-099`
> gereği yeni ADR yazılır.

---

## Bağlam

Personel verisinin kaynağı LOGO Bordro'dur ve salt okunurdur (ADR-0003). T3 Kimlik
Yönetimi'nin en kritik akışı, kişinin TCKN, doğum tarihi ve kurumsal e-postasının LOGO'daki
kayıtla eşleşmesidir (üyelik). Bu akışın gerçek kullanımı ancak gerçek kayıtlarla sınanabilir:
- **LOGO veri kalitesi:** Kart çelişkileri, eksik TCKN, ortak ve kişisel e-posta adresleri
  maskelenmiş veride görünmez; T3'te bulunan veri sorunlarının çoğu (AN-17, AN-18, `KR-079`)
  gerçek veriyle yapılan denemede ortaya çıktı.
- **Kabul testi:** İK ve personel, kendi gerçek bilgileriyle üye olmayı denemelidir (KS-01…04).
  Maskelenmiş veride personel kendi kaydını bulamaz.
- **Gönderim:** Doğrulama kodunun gerçekten ulaşması (SMTP, NetGSM) kabulün parçasıdır
  (SYG-KMLK-079).

## Karar

| Ortam | Veri | Gönderim | Koruma |
|---|---|---|---|
| **CI ve otomatik testler** | **Sentetik** (Testcontainers, uçtan uca tohum verisi) | Yok | — |
| **Yerel geliştirme** | **Gerçek**: LOGO'dan salt okunur senkronizasyon | Gerçek (`Send` kipi) | Yalnızca geliştirici kullanır; sırlar depo dışında (`docker/.env`); veri MAN.6 BK-15; LOGO'ya yazma yasağı geçerli (`KR-003`) |
| **UAT** | **Gerçek**: LOGO'dan salt okunur senkronizasyon, maskelenmez | Yalnızca izin listesindekilere (`AllowList`, `KR-083`); kabul katılımcıları listede kalır (`KR-106`) | Kurum içi erişim ve HTTPS; veritabanı dışa kapalı; SSH anahtarla; düzenli yedek yok (`KR-098`); risk R-25 Üst Yönetim tarafından kabul edildi (`KR-103`) |
| **Üretim** | Gerçek | Tüm alıcılar (`Send`) | Üretim için ayrıca belirlenir (AS) |

## Gerekçe

- Kabulün anlamı, gerçek kullanıcının gerçek kaydıyla akışı tamamlayabilmesidir; maskelenmiş
  veride bu sınanamaz.
- Yerel ortamın üretimle aynı çalışması, UAT'ye hatasız çıkmayı sağlar: değişiklik önce
  yerelde gerçek veriyle denenir, sonra UAT'ye gider (Doğuş Uçanok, 28.09.2026).
- Otomatik testler gerçek veriye bağımlı olmamalıdır: tekrarlanabilir ve kişisel veri
  içermeyen sentetik veriyle çalışırlar.

## Değerlendirilen alternatifler

| Alternatif | Artıları | Eksileri | Neden seçilmedi |
|---|---|---|---|
| ADR-0011 §7 olduğu gibi (UAT maskelenmiş, geliştirme sentetik) | Gerçek veri yalnızca üretimde | Eşleşme ve veri kalitesi sorunları kabulden önce görünmez; personel kendi kaydıyla deneyemez | Kabulün amacına aykırı |
| UAT'de maskeleme, kabul katılımcıları için gerçek kayıt | Gerçek veri azalır | Maskeleme betiği ve eşleme bakımı; LOGO senkronizasyonu her 15 dakikada gerçek veriyi yeniden getirir | Senkronizasyon tasarımıyla çelişir; bakım yükü |
| Gerçek veri, korumalarla (**seçilen**) | Gerçek kullanım sınanır | Kişisel veri iki ek ortamda | Risk kabul edildi (R-25, `KR-103`) |

## Sonuçlar

**Olumlu:**
- T3'teki veri kalitesi bulguları kabulden önce görüldü ve düzeltildi.
- Kabul oturumu personelin gerçek kaydıyla yapıldı (13.10.2026, `KR-105`).

**Olumsuz / kabul edilen ödünler:**
- Gerçek kişisel veri geliştirme makinesinde ve UAT sunucusunda bulunur (R-25, R-30).
- UAT yedeklenmez; veri kaybı test ortamı için kabul edilmiştir (`KR-098`).

**Bu kararın getirdiği yükümlülükler:**
- UAT ve yerel ortamdaki gerçek veri bilgi kayıt defterinde kayıtlıdır (BK-10, BK-15); saklama
  ve imha koşulları KVKK Sorumlusu'nun onayına sunulur (MAN.6 Ö-2).
- Yerel ve UAT ortamından kişisel veri içeren dışa aktarım depoya girmez.
- Ortam internete açılırsa veya ortamı geliştirici dışında biri kullanırsa bu karar yeniden
  değerlendirilir.

## Geri dönüş maliyeti

**Orta.** UAT'yi maskelenmiş veriye geçirmek için senkronizasyonun maskeleme katmanı veya ayrı
bir veri yükleme yolu gerekir; kabul senaryoları maskelenmiş kayıtlara göre yeniden yazılır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-14 | 1.0 | İlk oluşturma: geliştirme ve UAT ortamlarının gerçek veriyle çalışması ADR olarak kayda geçti; ADR-0011 §7 Veri sütununun yerini aldı (`KR-108`, `KR-099`, #212) | Bilgi İşlem |
