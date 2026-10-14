# KVKK Saklama ve İmha Süreleri — Onay Talebi

**Belge kimliği:** MAN.6-KVK-2026-10-14
**Süreç:** MAN.6 — Bilgi Yönetimi
**Sunan:** Bilgi İşlem (Doğuş Uçanok) · **Onaylayacak:** KVKK Sorumlusu (RACI: saklama süreleri "A")
**Tarih:** 2026-10-14 · **Kaynak:** #215, bilgi kayıt defteri açık işi Ö-2
**Durum:** Hazırlandı; **iletilmedi**. KVKK Sorumlusu'nun projedeki rolü Üst Yönetim tarafından henüz resmen bildirilmedi (RACI §7 iş 3). Rol bildirildiğinde bu talep iletilir ve iletim tarihi bu kayda yazılır.

---

## 1. Neden bu talep?

DSG-HRMS projesinde kişisel veri içeren bilgi kümeleri bilgi kayıt defterinde
(`bilgi-kayit-defteri.md`) sahibi, yeri, gizlilik sınıfı, saklama süresi ve imha koşuluyla
kayıtlıdır. Bilgi İşlem bu sürelerin bir kısmını **öneri** olarak yazdı. RACI'ye göre saklama
sürelerine KVKK Sorumlusu karar verir. Aşağıdaki her satır için onay, değişiklik veya ret
istenir.

## 2. Onayı istenen süreler

| # | Bilgi kümesi | Yer | İçerik | Önerilen saklama | Önerilen imha | Karar |
|---|---|---|---|---|---|---|
| 1 | Denetim izi (değişiklik kaydı) — `PRM-KVK-01` | Uygulama veritabanı (`audit` şeması) | Kim, neyi, ne zaman değiştirdi; değişen alanlar | **Karar bekliyor** (SYG-KMLK AN-16). Bugün süresiz tutuluyor; kayıtlar değiştirilemez (`KR-060`) | Süre belirlenince süresi dolan kayıtlar silinir; silme işi o zaman geliştirilir | ☐ Onay ☐ Değişiklik: …… |
| 2 | Erişim kaydı — `PRM-KVK-02` | Uygulama veritabanı | Kim, hangi kişisel veriyi ne zaman görüntüledi | **Karar bekliyor** (AN-16). Bugün süresiz | Aynı | ☐ Onay ☐ Değişiklik: …… |
| 2a | Güvenlik olayları (`audit.security_event`) | Uygulama veritabanı | Giriş, kilitlenme, kod gönderimi gibi kimlik olayları; IP ve takip numarası dahil (SYG-KMLK-060) | **Süre parametresi tanımlı değil**; bugün süresiz, kayıtlar değiştirilemez (`KR-094`). Önerimiz denetim iziyle (satır 1) aynı süre | Aynı | ☐ Onay ☐ Değişiklik: …… |
| 3 | Uygulama günlükleri (UAT, yerel) — BK-11, BK-17 | UAT sunucusu; geliştirici makinesi | Kişisel veri maskelenmiş teknik günlük | 90 gün (ADR-0009) | Otomatik silinir | ☐ Onay ☐ Değişiklik: …… |
| 4 | UAT veritabanı — BK-10 | UAT sunucusu | Gerçek personel verisi, hesaplar, denetim izi (`KR-108`, R-25) | UAT kullanıldıkça; düzenli yedek yok (`KR-098`) | Üretime geçişten sonra veya UAT kapatıldığında silinir; silme kayda geçer | ☐ Onay ☐ Değişiklik: …… |
| 5 | Yerel geliştirme veritabanı — BK-15 | Geliştirici makinesi | Gerçek personel verisi (`KR-108`) | Geliştirme süresince | Geliştirici değiştiğinde veya makine el değiştirdiğinde silinir | ☐ Onay ☐ Değişiklik: …… |
| 6 | LOGO kart çelişkileri listesi — BK-20 | Geliştirici makinesi | Ad, sicil, iletişim bilgisi | İK'nın düzeltmesi bitip yeniden ölçüm yapılana kadar | Yeniden ölçümden sonra silinir | ☐ Onay ☐ Değişiklik: …… |
| 7 | UAT izin listesi — BK-22 | Geliştirici makinesi; UAT sır dosyası | Kabul katılımcılarının kurumsal e-posta ve telefonu | Kabul oturumları sürdükçe; katılımcılar sonraki kabuller için kalır (`KR-106`) | Kabullerin tamamı bittiğinde veya kişi kurumdan ayrıldığında çıkarılır | ☐ Onay ☐ Değişiklik: …… |
| 8 | Geliştirme yardımcısıyla yazışma arşivi — BK-25 | Geliştirici makinesi | Kararların ham kaydı; kişisel veri içerebilir | Kararlar ve onaylar resmî kayıtlara aktarıldıktan sonra arşiv olarak | — | ☐ Onay ☐ Değişiklik: …… |

**Bilgi için (karar verildi, onay gerekmez):**
- Eski HRMS kaynak kodu (`src_old/`, BK-06): geçiş tamamlanana kadar tutulur, sonra silinir (Doğuş Uçanok, 14.10.2026). Kişisel veri değil, eski sistemin bağlantı bilgilerini içerir.
- İK veri düzeltme listesi ve T3 gereksinim çalışma dosyası 14.10.2026'da silindi.

## 3. Karar

| Rol | Ad soyad | Tarih | Not |
|---|---|---|---|
| KVKK Sorumlusu | …………………… | …… / …… / …… | |
| Sunan (Bilgi İşlem) | Doğuş Uçanok | | |

Kararlar bilgi kayıt defterine işlenir; "Öneri" ibareleri kaldırılır. `PRM-KVK-01` ve `02`
için süre belirlenince silme işi bir iş kaydıyla geliştirilir (AN-16).

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-14 | 0.1 | Talep hazırlandı (#215) | Bilgi İşlem |
