# Mimari Karar Kayıtları (ADR)

**Son güncelleme:** 2026-09-06
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
2. **ADR'ler değiştirilmez, değiştirilir.** Bir karar geçersizleşirse ADR silinmez;
   durumu `Değiştirildi` yapılır ve yerine geçen ADR'ye bağlantı verilir. Böylece
   kararın geçmişi korunur.
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

| No | Başlık | Durum | Konu |
|---|---|---|---|
| [ADR-0001](ADR-0001-teknoloji-yigini.md) | Teknoloji Yığını | Kabul Edildi | .NET 10 LTS, React 19, PostgreSQL; AutoMapper/MediatR/QuestPDF reddi |
| [ADR-0002](ADR-0002-cozum-yapisi-ve-katmanli-mimari.md) | Çözüm Yapısı ve Katmanlı Mimari | Kabul Edildi | Modüler monolit, 4 katman, bağımlılık kuralları, mimari testi |
| [ADR-0003](ADR-0003-logo-entegrasyon-stratejisi.md) | LOGO Entegrasyon Stratejisi | Kabul Edildi | Salt okuma, yalıtım katmanı, 15 dk snapshot senkronizasyon, şema sapma denetimi |
| [ADR-0004](ADR-0004-veritabani-tasarim-standartlari.md) | Veritabanı Tasarım Standartları | Kabul Edildi | Adlandırma, tipler, soft delete, tarih aralıklı tablolar, migration kuralları |
| [ADR-0005](ADR-0005-cekirdek-veri-modeli.md) | Çekirdek Veri Modeli | Kabul Edildi | Kişi/İstihdam ayrımı, tarih farkındalığı, yönetici grafı |
| [ADR-0006](ADR-0006-kimlik-dogrulama-ve-oturum.md) | Kimlik Doğrulama, Üyelik ve Oturum | Kabul Edildi | Üyelik akışı, doğrulama kodu, parola politikası, JWT + yenileme jetonu |
| [ADR-0007](ADR-0007-yetkilendirme-modeli.md) | Yetkilendirme Modeli | Kabul Edildi | Rol (eylem) × Kapsam (satır); `404` tercihi; yetki sızıntısı testi |
| [ADR-0008](ADR-0008-sir-ve-yapilandirma-yonetimi.md) | Sır ve Yapılandırma Yönetimi | Kabul Edildi | User Secrets / ortam değişkeni, `gitleaks`, açılışta doğrulama |
| [ADR-0009](ADR-0009-loglama-denetim-izi-ve-kvkk.md) | Loglama, Denetim İzi ve KVKK | Kabul Edildi | Üç kayıt türü, maskeleme, erişim kaydı, saklama ve imha |
| [ADR-0010](ADR-0010-api-sozlesmesi-ve-hata-yonetimi.md) | API Sözleşmesi ve Hata Yönetimi | Kabul Edildi | OpenAPI, Problem Details, sayfalama, eşzamanlılık |
| [ADR-0011](ADR-0011-test-stratejisi.md) | Test Stratejisi ve Kalite Kapıları | Kabul Edildi | Test piramidi, kapsam eşikleri, Testcontainers, CI kapıları |
| [ADR-0012](ADR-0012-bildirim-altyapisi.md) | Bildirim Altyapısı | Kabul Edildi | E-posta/SMS/uygulama içi, NetGSM standart servisi, dayanıklılık |
| [ADR-0013](ADR-0013-dosya-saklama.md) | Dosya Saklama ve Güvenlik Taraması | Kabul Edildi | NAS + `IFileStorage`, ClamAV, yükleme/indirme denetimleri |
| [ADR-0014](ADR-0014-raporlama-ve-disa-aktarma.md) | Raporlama ve Dışa Aktarma | Kabul Edildi | ClosedXML + PDFsharp, `export` izni, dışa aktarma kaydı |
| [ADR-0015](ADR-0015-frontend-mimarisi.md) | Frontend Mimarisi ve Arayüz İlkeleri | Kabul Edildi | Modül bazlı yapı, TanStack Query, ortak bileşenler, kullanılabilirlik |

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
| ADR-0006 | Uygulama **yalnızca yerel ağda** çalışacak | İnternete açılırsa **2FA kararı yeniden değerlendirilir** |
| ADR-0003 | LOGO tablo yapısı sabit; tek `LH_001_PERSON` tablosu var | Firma bazlı tablo eklenirse eşleme yaklaşımı gözden geçirilir |
| ADR-0012 | NetGSM hesabında OTP paketi yok | Paket tanımlanırsa doğrulama kodları OTP servisine taşınabilir |
| ADR-0014 | Rapor hacmi 50.000 satırın altında | Aşılırsa arka planda üretim modeline geçilir |
| ADR-0001 | Kullanılan paketlerin lisansları izin verici | Bir paket ticari lisansa geçerse alternatif değerlendirilir |
