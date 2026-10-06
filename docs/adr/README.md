# Mimari Karar Kayıtları (ADR)

**Son güncelleme:** 2026-10-04
**İlgili süreç:** TEC.5 (Tasarım Tanımlama) — *"Design rationale"* çıktısı

---

## ADR nedir, neden tutuyoruz?

Bir **Mimari Karar Kaydı (Architecture Decision Record)**, önemli bir teknik kararın
*ne olduğunu*, *hangi bağlamda alındığını*, *hangi alternatiflerin değerlendirildiğini*
ve *hangi sonuçları doğurduğunu* kayda geçirir.

Bu proje için iki nedenle kritiktir:

1. **10–15 yıllık kullanım hedefi.** Bugün açık olan gerekçeler üç yıl sonra
   hatırlanmaz. "Neden AutoMapper kullanmamışlar?" sorusunun cevabı yazılı olmazsa,
   birisi onu geri ekler. ADR'ler `R-05` (bilgi tekelliği) riskinin ana önlemidir.
2. **TS ISO/IEC TS 33061 TEC.5.** Standart, tasarım sürecinin çıktıları arasında
   *"System/software design rationale"* kalemini sayar. ADR'ler bunun karşılığıdır.

## Kurallar

1. Her ADR `ADR-NNNN-<kisa-baslik>.md` biçiminde adlandırılır; numaralar yeniden
   kullanılmaz.
2. **Ek ve sınırlı değişiklik yerinde, karar değişikliği yeni ADR ile (`KR-099`).**
   - Kararı **genişleten veya netleştiren** değişiklik (gerçekleştirme notu, yeni bir alt
     kural, ölçülen değer) ADR'nin içinde yapılır. ADR'nin değişiklik geçmişine tarih,
     sürüm ve ilgili `KR-NNN` yazılır.
   - Kararı **tersine çeviren veya yerine başka bir çözüm koyan** değişiklik için yeni ADR
     yazılır. Eski ADR silinmez; durumu `Yerini aldı: ADR-NNNN` yapılır. Böylece kararın
     geçmişi korunur.
   - Hangisi olduğu belirsizse yeni ADR yazılır.
3. Her ADR, `docs/karar-kayit-defteri.md` içindeki ilgili `KR-NNN` kayıtlarına atıf
   yapar. Karar defteri **ne** karar verildiğini, ADR **neden** karar verildiğini tutar.
4. Yeni ADR `docs/sablonlar/ADR-sablonu.md` şablonuyla oluşturulur.
5. ADR değişiklikleri de Pull Request ile yapılır (MAN.5, PA 2.2).

## Ne zaman ADR yazılır?

- Geri dönüşü pahalı bir karar alındığında (veri modeli, mimari, teknoloji)
- Bir kütüphane veya yaklaşım **reddedildiğinde** (bu, ileride tekrar gündeme
  gelmemesi için en az kabul kadar önemlidir)
- Alışılmışın dışında bir tercih yapıldığında
- Güvenlik veya KVKK etkisi olan bir tasarım kararında

Gündelik uygulama ayrıntıları (bir metodun adı, bir bileşenin yerleşimi) ADR konusu
değildir.

---

## Kayıtlı ADR'ler

| No | Başlık | Durum | Son değişiklik | Konu |
|---|---|---|---|---|
| [ADR-0001](ADR-0001-teknoloji-yigini.md) | Teknoloji Yığını | Kabul Edildi | 2026-09-09 (0.2) | .NET 10 LTS, React 19, PostgreSQL; AutoMapper/MediatR/QuestPDF reddi |
| [ADR-0002](ADR-0002-cozum-yapisi-ve-katmanli-mimari.md) | Çözüm Yapısı ve Katmanlı Mimari | Kabul Edildi; modül düzeni → ADR-0016 | 2026-10-05 (0.2) | Modüler monolit, 4 katman, bağımlılık kuralları, mimari testi |
| [ADR-0003](ADR-0003-logo-entegrasyon-stratejisi.md) | LOGO Entegrasyon Stratejisi | Kabul Edildi | 2026-09-26 (0.2) | Salt okuma, yalıtım katmanı, 15 dk snapshot senkronizasyon, şema sapma denetimi |
| [ADR-0004](ADR-0004-veritabani-tasarim-standartlari.md) | Veritabanı Tasarım Standartları | Kabul Edildi | 2026-09-06 (0.1) | Adlandırma, tipler, soft delete, tarih aralıklı tablolar, migration kuralları |
| [ADR-0005](ADR-0005-cekirdek-veri-modeli.md) | Çekirdek Veri Modeli | Kabul Edildi | 2026-09-06 (0.1) | Kişi/İstihdam ayrımı, tarih farkındalığı, yönetici grafı |
| [ADR-0006](ADR-0006-kimlik-dogrulama-ve-oturum.md) | Kimlik Doğrulama, Üyelik ve Oturum | Kabul Edildi | 2026-10-01 (1.0) | Üyelik akışı, doğrulama kodu, parola politikası, JWT + yenileme jetonu |
| [ADR-0007](ADR-0007-yetkilendirme-modeli.md) | Yetkilendirme Modeli | Kabul Edildi | 2026-09-29 (0.2) | Rol (eylem) × Kapsam (satır); `404` tercihi; yetki sızıntısı testi |
| [ADR-0008](ADR-0008-sir-ve-yapilandirma-yonetimi.md) | Sır ve Yapılandırma Yönetimi | Kabul Edildi | 2026-09-26 (0.2) | User Secrets / ortam değişkeni, `gitleaks`, açılışta doğrulama |
| [ADR-0009](ADR-0009-loglama-denetim-izi-ve-kvkk.md) | Loglama, Denetim İzi ve KVKK | Kabul Edildi | 2026-10-03 (0.3) | Üç kayıt türü, maskeleme, erişim kaydı, saklama ve imha |
| [ADR-0010](ADR-0010-api-sozlesmesi-ve-hata-yonetimi.md) | API Sözleşmesi ve Hata Yönetimi | Kabul Edildi | 2026-09-06 (0.1) | OpenAPI, Problem Details, sayfalama, eşzamanlılık |
| [ADR-0011](ADR-0011-test-stratejisi.md) | Test Stratejisi ve Kalite Kapıları | Kabul Edildi | 2026-10-04 (0.3) | Test piramidi, kapsam eşikleri, Testcontainers, CI kapıları |
| [ADR-0012](ADR-0012-bildirim-altyapisi.md) | Bildirim Altyapısı | Kabul Edildi | 2026-09-27 (0.3) | E-posta/SMS/uygulama içi, NetGSM standart servisi, dayanıklılık |
| [ADR-0013](ADR-0013-dosya-saklama.md) | Dosya Saklama ve Güvenlik Taraması | Kabul Edildi | 2026-09-06 (0.1) | NAS + `IFileStorage`, ClamAV, yükleme/indirme denetimleri |
| [ADR-0014](ADR-0014-raporlama-ve-disa-aktarma.md) | Raporlama ve Dışa Aktarma | Kabul Edildi | 2026-09-06 (0.1) | ClosedXML + PDFsharp, `export` izni, dışa aktarma kaydı |
| [ADR-0015](ADR-0015-frontend-mimarisi.md) | Frontend Mimarisi ve Arayüz İlkeleri | Kabul Edildi | 2026-09-06 (0.1) | Modül bazlı yapı, TanStack Query, ortak bileşenler, kullanılabilirlik |
| [ADR-0016](ADR-0016-modul-siniri-denetimi.md) | Modül Sınırının Denetimi | Kabul Edildi | 2026-10-05 (0.1) | Bildirilen bağımlılık tablosu; ölçülen ve bildirilen bağımlılıkların birebir karşılaştırılması |

---

## Birbirini etkileyen kararlar

Bir ADR'yi değiştirmeden önce hangi kararların etkileneceğini görmek için:

| Değişirse | Etkilenenler |
|---|---|
| ADR-0003 (LOGO stratejisi) | ADR-0005 (veri modeli), ADR-0006 (üyelik kimlik kaynağı) |
| ADR-0004 (veritabanı standartları) | ADR-0005, ADR-0009 (denetim tabloları) |
| ADR-0005 (çekirdek veri modeli) | ADR-0006 (hesap–kişi), ADR-0007 (kapsam hesabı) — **en geniş etki** |
| ADR-0007 (yetkilendirme) | ADR-0010 (`404` davranışı), ADR-0013 (dosya erişimi), ADR-0014 (rapor kapsamı) |
| ADR-0009 (loglama/KVKK) | ADR-0012 (bildirim kaydı), ADR-0013 (dosya erişim kaydı), ADR-0014 (dışa aktarma kaydı) |
| ADR-0010 (API sözleşmesi) | ADR-0015 (frontend tip üretimi ve hata yönetimi) |

---

## Gözden geçirme tetikleyicileri

Bazı kararlar belirli bir varsayıma dayanır. Varsayım değişirse karar yeniden
değerlendirilmelidir:

| ADR | Varsayım | Değişirse |
|---|---|---|
| ADR-0006 | Uygulama **yalnızca yerel ağda** çalışacak | İnternete açılırsa 2FA'nın varsayılan olarak **açık** olması değerlendirilir. 2FA geliştirildi ve parametreyle açılabilir (`KR-069`, PRM-KML-08); varsayılan kapalı |
| ADR-0003 | LOGO tablo yapısı sabit; tek `LH_001_PERSON` tablosu var | Firma bazlı tablo eklenirse eşleme yaklaşımı gözden geçirilir |
| ADR-0012 | NetGSM hesabında OTP paketi yok | Paket tanımlanırsa doğrulama kodları OTP servisine taşınabilir |
| ADR-0014 | Rapor hacmi 50.000 satırın altında | Aşılırsa arka planda üretim modeline geçilir |
| ADR-0001 | Kullanılan paketlerin lisansları izin verici | Bir paket ticari lisansa geçerse alternatif değerlendirilir |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma; ADR-0001…ADR-0015 dizini | Bilgi İşlem |
| 2026-10-04 | 0.2 | Kural 2: ek ve sınırlı değişiklik yerinde, karar değişikliği yeni ADR ile (`KR-099`); dizine son değişiklik sütunu; ADR-0006 gözden geçirme tetikleyicisi 2FA geliştirmesine göre güncellendi (#133) | Bilgi İşlem |
| 2026-10-05 | 0.3 | ADR-0016 eklendi; ADR-0002 durumu (#171) | Bilgi İşlem |
