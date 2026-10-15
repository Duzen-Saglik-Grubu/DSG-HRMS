# Sürüm Kontrol Listesi

**Belge kimliği:** TEC.10-KL-SURUM
**Süreç:** TEC.10 — Geçiş, MAN.5 — Konfigürasyon Yönetimi, TEC.9 — Doğrulama, TEC.11 — Geçerleme
**Son güncelleme:** 2026-10-15 · **Sahibi:** Bilgi İşlem
**Dayanak:** `KR-097` (sürümleme), `KR-110` (konfigürasyon denetimi sıklığı), CONTRIBUTING §9

> **Neden var?** T3'te sürüm adımları beş ayrı belgede yazılıydı ve kontrol listesi yoktu.
> Sonuçları: rc.3'ün CI kanıt özeti etiket anında çıkarılmadı (#194); rc.3–rc.5 için
> konfigürasyon denetimi yapılmadı; yerel ortam güncellenmediği için eski sürüm görüldü
> (12.10); kabulden sonra modülün gereksinim issue'su açık kaldı (#62). Alınan dersler
> D-04, D-05 (`MAN.2-…/kayitlar/2026-10-13-t3-alinan-dersler.md`), #218.

Her adım bir PR'da veya kayıtta iz bırakır. Bir sürümde atlanan adım, ilgili PR'da
gerekçesiyle yazılır.

---

## A. Kabul adayı (`vX.Y.0-rc.N`)

**Önce (D-01):** İlk aday etiketlenmeden önce İK, G2'den sonra UAT'de sistemi serbestçe
dener ve Bilgi İşlem kabul planının senaryolarını prova eder.

| # | Adım | Kanıt |
|---|---|---|
| A1 | **Sürüm hazırlık PR'ı:** CHANGELOG'da `[X.Y.0-rc.N] — <tarih>` bölümü (tarih, etiketin konacağı gün, Türkiye saati); README sürüm satırı; kabul planı §3 sürüm satırı ve §4 "etiketlendi" maddesi **işaretsiz** | PR |
| A2 | Doğrulama raporu, G2'den sonra gelen değişiklik talepleri ve düzeltmeler için güncel mi? Değilse önce rapor güncellenir ve G2 yeniden kaydedilir (TEC.9 YAKLAŞIM §2.2) | Rapor sürümü |
| A3 | PR birleşince **`main` CI'ı yeşil** (CI, API sözleşme denetimi, Dal Koruma Denetimi) | Çalışma numarası |
| A4 | Açıklamalı etiket `main`'deki commit'e konur ve gönderilir: `git tag -a vX.Y.0-rc.N -m "<CHANGELOG başlığı>" <commit>` | `git tag -n2` |
| A5 | UAT'ye etiketten kurulum: `git checkout vX.Y.0-rc.N && bash docker/deploy-uat.sh`. Betik kendi doğrulamasını yapar ve dağıtım kaydına satır ekler | Betik çıktısı |
| A6 | **Kurulum sonrası kısa kontrol** (her adayda): konteynerler `healthy`; sağlık uçlarının üçü `Healthy`; çalışan imajın commit'i etiketle aynı; son migration depodakiyle aynı; dışarıya açık portlar (`ss -ltnup`) yalnız 22/80/443; `secrets/` dizininde yalnız `.env.uat`; API günlüğünde hata yok | Kayıt PR'ında satır |
| A7 | **CI kanıt özeti** etiketin commit'inin CI çalışmasından **hemen** üretilir: `node .github/scripts/ci-evidence-summary.mjs <çalışma> docs/33061/TEC.9-dogrulama/kayitlar/<tarih>-vX.Y.0-rc.N-ci-kanit-ozeti.md "<başlık>"` | Özet dosyası |
| A8 | **Tam konfigürasyon denetimi** — yalnız **İK kabul oturumuna sunulacak son adayda** (`KR-110`). Ara adaylarda A6 yeterlidir | `MAN.5-…/raporlar/` |
| A9 | **Kayıt PR'ı:** dağıtım kaydı satırı, CI kanıt özeti, A6 sonucu, kabul planı §4 "etiketlendi ve UAT'ye kuruldu" maddesi işaretlenir | PR |
| A10 | İK belge seti (kabul planı, form, katılımcı notu) yeni sürüme göre **Word ile** yeniden üretilir; Masaüstü klasörü (BK-23) güncellenir | Dosya tarihleri |
| A11 | **Yerel ortam** aynı commit'ten yeniden derlenir ve migration'lar uygulanır: `cd docker && docker compose up -d --build api web`; `dotnet ef database update --context HrmsDbContext …` | `public-settings` yanıtı |

## B. Kabul (G3) ve modül sürümü (`vX.Y.0`)

| # | Adım | Kanıt |
|---|---|---|
| B1 | **Kabul kaydı PR'ı:** kabul formunun Markdown kaydı ve doldurulmuş Word formun üst verisi temizlenmiş kopyası (aslının ve kopyanın SHA-256 özetiyle, `KR-107`); kabul raporu; izlenebilirlik matrisinin "Kabul" sütunu; kabul planı durumu; karar defterine kabul kaydı; bilgi kayıt defteri; CHANGELOG `[X.Y.0]` ve README | PR |
| B2 | Kabul edilen commit (son aday) **`vX.Y.0`** olarak etiketlenir; bu modülün baseline'ıdır. UAT'de aynı commit çalıştığı için yeniden kurulum gerekmez; dağıtım kaydına not düşülür | `git tag -n2` |
| B3 | Modülün **`[GEREKSİNİM]` issue'su** kabul ölçütlerinin kanıtlarıyla kapatılır | Issue yorumu |

## C. Modül kapanışı (G4)

| # | Adım | Kanıt |
|---|---|---|
| C1 | 33061 süreç denetimi (15 süreç) ve öz değerlendirme tablosu | `MAN.8-…/raporlar/` |
| C2 | **Tam konfigürasyon denetimi** (`KR-110`); bilgi kayıt defteri depo dışı konumlarla karşılaştırılır (`KR-101`) | `MAN.5-…/raporlar/` |
| C3 | Kapanış gözden geçirmesi (G4) ve alınan dersler | `MAN.2-…/kayitlar/` |
| C4 | Durum raporu ve risk gözden geçirmesi (`KR-101`) | `MAN.2-…/raporlar/`, risk defteri |
| C5 | Denetim bulguları issue'ya veya gerekçeli kapatmaya bağlanır | Issue listesi |

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-15 | 1.0 | İlk oluşturma: T3 deneyiminden (#218, `KR-110`) | Bilgi İşlem |
