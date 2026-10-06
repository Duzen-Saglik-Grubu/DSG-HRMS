# Proje Planı ve RACI — Onay Talebi ve Kaydı

**Belge kimliği:** MAN.1-ONY-2026-10-06
**Süreç:** MAN.1 — Proje Planlama (`MAN.1.BP3`: projenin onaylanması), MAN.4 — Risk Yönetimi
**Sunan:** Bilgi İşlem · **Onaylayacak:** Üst Yönetim
**Dayanak:** Proje durum raporu 1 (`MAN.2-proje-degerlendirme-ve-kontrol/raporlar/2026-10-06-durum-raporu.md`) §5
**Tarih:** 2026-10-06 · **Kaynak:** #127

> Bu form doldurulup imzalandıktan sonra taranır. Taranmış nüsha depo dışında saklanır ve
> bilgi kayıt defterine yazılır. Kararlar bu belgenin bir sonraki sürümüne ve karar
> defterine işlenir. Proje planı onaylanana kadar **taslak** sayılır.

---

## 1. Onaya sunulan belgeler

| Belge | Sürüm | İçerik |
|---|---|---|
| Proje planı (`proje-plani.md`) | 0.3 | Hedefler, kapsam, modül çevrimi ve kapılar, aşamalar ve sürümleme, kaynaklar, ortamlar, kısıtlar, izleme, iletişim |
| Roller ve sorumluluklar (`roller-ve-sorumluluklar.md`) | 0.2 | Roller, RACI matrisi, yetkinlik tablosu, tek kişiye bağımlılık |
| İş kırılım yapısı (`is-kirilim-yapisi.md`) | 0.4 | Aşama ve modül kırılımı, kritik yol |

**İlk sürümden (07.09.2026) bu yana öne çıkan değişiklikler:**
- **Modül sırası:** T3 Kimlik, T1 ve T2'den önce geliştirildi (`KR-040`, `KR-077`).
- **Sürümleme:** Her kabul edilen modül bir sürüm alır; kabule sunulan sürüm "aday" olarak etiketlenir (`KR-097`).
- **Test ortamı (UAT):** Kuruldu ve gerçek personel verisiyle çalışıyor (karar 2).
- **Durum raporu:** Her modül kapanışında ve en geç ayda bir yazılır (`KR-101`).

## 2. Kararlar

### Karar 1 — Proje planı, RACI ve iş kırılım yapısı

☐ **Onaylandı** ☐ **Değişiklikle onaylandı** (aşağıya yazılır) ☐ **Reddedildi**

Not: ……………………………………………………………………………………………

### Karar 2 — Risk kabulü: test ortamının gerçek personel verisiyle çalışması (R-25)

**Risk:** İK kabul testlerinin gerçek personelle yapılabilmesi için test ortamı, LOGO'dan
gerçek personel verisini okur ve maskelemez. E-posta ve SMS gönderimi gerçektir. Test
ortamına yetkisiz erişim, gerçek personel verisinin ifşası demektir. Puan 2 × 3 = 6. RACI'ye
göre puanı 6 ve üzeri olan bir riski Üst Yönetim kabul eder.

**Önlemler:**
- **İleti gönderimi:** Yalnızca izin listesindeki adreslere gider; liste kabul katılımcılarıyla sınırlıdır.
- **Erişim:**
  - Ortam yalnızca kurum içinden ve şifreli bağlantıyla (HTTPS) erişilebilir.
  - Veritabanı dışarıya kapalıdır.
  - Sunucuya erişim anahtarla yapılır.
- **Kayıtlar:** Günlüklerde kişisel veri maskelenir. Hesap işlemleri ekranında yalnızca ad, sicil ve firma görünür.
- **Kopyalar:** Test ortamının veritabanı düzenli yedeklenmez; böylece gerçek verinin kopyası çoğalmaz.

☐ **Kabul edildi** ☐ **Ek önlemle kabul edildi** (aşağıya yazılır) ☐ **Kabul edilmedi** (test ortamı maskelenmiş veriye geçirilir; kabul testlerinin yöntemi değişir)

Not: ……………………………………………………………………………………………

### Karar 3 — Üretim altyapısı

Üretime geçiş için gerekenler:
- **B4:** üretim sunucusu (Linux ve Docker);
- **B2:** güncel bir PostgreSQL sunucusu;
- **B5, B9:** yedekleme ve geri yükleme düzeni; hedef veri kaybı süresi (RPO) ve hizmetin geri gelme süresi (RTO) belirlenmeli.

Mevcut İK sisteminin veritabanının (PostgreSQL 14) üretici desteği Kasım 2026'da bitiyor (R-09).

☐ **Kaynak ayrıldı;** hedef tarih: ………… ☐ **Daha sonra değerlendirilecek;** tarih: …………

### Karar 4 — Başarı ölçütleri (plan §1)

| # | Hedef | Ölçüt | Teyit |
|---|---|---|---|
| H1 | Veri kaybı olmadan geçiş | Sıfır açıklanamayan fark | ☐ |
| H2 | Personelin sistemi benimsemesi | 2 ay içinde aktif personelin ≥ %90'ı hesap açmış | ☐ |
| H3 | İK'nın elle yaptığı işin azalması | İzin taleplerinin ≥ %95'i personel tarafından girilmiş | ☐ |
| H4 | Süreç şeffaflığı | Talep → sonuç süresi ölçülebilir | ☐ |
| H5 | Mevzuat uyumu | KVKK erişim ve işlem kayıtları eksiksiz | ☐ |
| H6 | Süreç olgunluğu | TS ISO/IEC TS 33061 Seviye 2 | ☐ |
| H7 | Sürdürülebilirlik | Kalite eşikleri sağlanıyor, kararlar yazılı | ☐ |

Değiştirilmesi istenen ölçüt: ……………………………………………………………

## 3. İmza

| | Ad soyad | Unvan | Tarih | İmza |
|---|---|---|---|---|
| Onaylayan (Üst Yönetim) | | | | |
| Sunan (Bilgi İşlem) | Doğuş Uçanok | | | |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-06 | 1.0 | Onay talebi hazırlandı (#127) | Bilgi İşlem |
