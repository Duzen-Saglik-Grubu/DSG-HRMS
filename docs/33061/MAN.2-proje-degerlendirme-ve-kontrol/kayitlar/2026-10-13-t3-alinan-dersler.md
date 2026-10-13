# T3 Kimlik Yönetimi — Alınan Dersler

**Belge kimliği:** MAN.2-DRS-2026-10-13-T3
**Süreç:** MAN.2 — Proje Değerlendirme ve Kontrol (YAKLAŞIM §2.4)
**Modül:** T3 Kimlik Yönetimi · **Hazırlayan:** Bilgi İşlem · **Tarih:** 2026-10-13

Her ders üç soruya cevap verir: ne oldu, neden oldu, sonraki modülde ne değişecek.
"Ne değişecek" sütunundaki her madde, bir sonraki modülün (T1, T2, T4 veya T5) ilk işinde
uygulanır. Uygulama, o modülün kapanış gözden geçirmesinde kontrol edilir.

---

## Dersler

| # | Ne oldu | Neden | Sonraki modülde ne değişecek |
|---|---|---|---|
| D-01 | Kabul adayı beş kez yenilendi (rc.1–rc.5) | İK'nın UAT'de sistemi ilk kez denemesi kabul adayı etiketlendikten sonra oldu; iki değişiklik talebi (#185, #190) buradan çıktı. Bilgi İşlem'in senaryo provası ise son adaydan hemen önce yapıldı | Sıra değişir: (1) G2'den sonra İK UAT'de serbestçe dener; (2) Bilgi İşlem kabul planının senaryolarını prova eder; (3) ancak bunlardan sonra ilk kabul adayı etiketlenir |
| D-02 | Bir T5 parametresi (PRM-HSP-03) T3 kataloğunda işlevsiz kaldı (#198) | Parametre kataloğu, Y4 parametre listesinin tamamından toplu kuruldu (#84). Her parametrenin onaylı bir T3 gereksinimine bağlı olduğu denetlenmedi | G2 kontrol listesine madde: kataloğa eklenen her parametre onaylı bir SYG maddesine bağlı ve kodda kullanılıyor. Kullanılmayan parametre otomatik testle yakalanır |
| D-03 | Kabul planında yanlış ve uygulanamaz adımlar vardı (#196): iptalin 3. denemede olduğu yazılmıştı; kodun ikinci kullanımı ve video adımı ekrandan denenemiyordu | Plan, gereksinim metninden yazıldı; adımlar ekranda denenmeden onaya sunuldu | Kabul planı, İK'ya sunulmadan önce Bilgi İşlem tarafından ekranda bir kez denenir. Her beklenen sonuç, onaylı gereksinimin kabul ölçütüyle karşılaştırılır |
| D-04 | rc.3'ün CI kanıt özeti etiket anında çıkarılmadı; sonradan üretildi (#194) | Kural yazılıydı (TEC.9 YAKLAŞIM §6), ama etiketleme adımları bir kontrol listesinde değildi | Sürüm adımları tek bir kontrol listesinde toplanır: etiket → UAT kurulumu → dağıtım kaydı → CI kanıt özeti → kabul planı §4 → İK belge seti |
| D-05 | Yerel ortam birleştirmelerle güncellenmedi; İK provasında eski sürüm görüldü (12.10) | Yerel konteynerler yalnızca elle yeniden derleniyor | Kod değiştiren her birleştirmeden ve her UAT kurulumundan sonra yerel ortam da yeniden derlenir ve migration'lar uygulanır (D-04 kontrol listesine eklenir) |
| D-06 | İmzalı kabul formlarının saklanacağı yer kabulden önce belirlenmedi (MAN.6 Ö-3) | Açık iş kayıtlıydı, ama kabul planının giriş ölçütlerinde yoktu | Kabul planı şablonunun giriş ölçütlerine "kabul formunun saklanacağı yer belirlendi" maddesi eklenir |
| D-07 | Aynı ortak belgeye (karar defteri, kabul planı, matris) dokunan eşzamanlı PR'lar squash birleştirmede çakıştı | Birden fazla belge PR'ı aynı anda açıktı | Ortak belgelere dokunan PR'lar sırayla açılır; çakışma olursa dal main'e yeniden dayandırılır |
| D-08 | Kabul katılımcılarının dördü de Bilgi İşlem'dendi | İK, katılımcıları UAT izin listesinde zaten bulunan kişilerden seçti (`KR-106`) | Sonraki kabullerde personel katılımcılar farklı birimlerden seçilir; gerekirse izin listesine eklenir |

## Sürdürülecek iyi uygulamalar

| # | Uygulama | Neden işe yaradı |
|---|---|---|
| İ-01 | İzlenebilirlik denetiminin CI'da çalışması | SYG-KMLK-080 ve 081 için eksik tablo satırlarını birleştirmeden önce yakaladı |
| İ-02 | Onaylı gereksinim değişikliklerinin "değişiklik talebi" olarak işlenmesi (PG geçmişi, İK onay tarihi, SYG, matris) | Kabulde T-03 ve T-04 olarak İK'ya ayrıca gösterildi; ikisi de uygun bulundu |
| İ-03 | Bilgi İşlem provası | Kabul oturumunda hiç bulgu çıkmadı; dört bulgu oturumdan önce kapandı |
| İ-04 | Parametrik yapı | İK, ayarların yazılım değişikliği gerektirmemesini olumlu değerlendirdi (kabul formu §5) |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-13 | 1.0 | İlk sürüm | Bilgi İşlem |
