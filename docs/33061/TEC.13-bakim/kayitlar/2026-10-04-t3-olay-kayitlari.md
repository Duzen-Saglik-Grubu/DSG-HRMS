# T3 Dönemi Olay Kayıtları (Geriye Dönük)

**Belge kimliği:** TEC.13-OLY-2026-10-04-T3
**Süreç:** TEC.13 — Bakım, MAN.5 — Konfigürasyon Yönetimi
**Kaynak:** T3 süreç denetimi, bulgu T3-07 (#129)
**Tarih:** 2026-10-04 · **Hazırlayan:** Bilgi İşlem

T3 geliştirmesi sırasında (24.09–02.10.2026) yaşanan dört olay o tarihte kayda geçmedi.
Bu belge onları geriye dönük olarak kaydeder. 04.10.2026'dan sonra:
- `main` kırmızıya düştüğünde `[HATA]` issue'su otomatik açılır (CONTRIBUTING §1.1).
- Dal koruma denetimi yapılamazsa `[DÜZELTİCİ]` issue'su otomatik açılır.
- Diğer olaylar `[HATA]` issue'su olarak kayda geçer; risk niteliğindeyse risk defterine de işlenir (MAN.4 §1).

---

## O-1 — Dal koruma denetiminin çökmesi (28.09.2026)

| | |
|---|---|
| **Ne oldu** | `main`'e gelen `387c4d2` commit'i (PR #94) için dal koruma denetimi 19:16 UTC'de başarısız oldu ([çalışma 36471125839](https://github.com/Duzen-Saglik-Grubu/DSG-HRMS/actions/runs/36471125839)). Hata: `HttpError: Unexpected end of JSON input`. GitHub API'sinin commit–PR sorgusu geçici olarak bozuk yanıt döndü. |
| **Etki** | Commit denetlenmeden geçti. İş akışı yalnızca kırmızı göründü; issue açılmadı, olay fark edilmedi. |
| **Sonradan doğrulama** | `387c4d2`, PR #94 ile 28.09.2026 19:16:44 UTC'de birleştirildi (GitHub commit–PR ilişkisi, 04.10.2026'da sorgulandı). PR #94'ün inceleme onayı geriye dönük onay kaydındadır (`MAN.8-kalite-guvence/kayitlar/2026-10-04-pr-onay-kaydi.md`). **Uygunsuzluk yok.** |
| **Önlem** | Denetim adımı API hatalarında 3 kez yeniden dener. Yeniden denemelere rağmen denetim yapılamazsa `[DÜZELTİCİ] Dal koruma denetimi yapılamadı` issue'su açılır (#129). |

## O-2 — `main`'in güvenlik açığı nedeniyle kırmızı kalması (30.09–01.10.2026)

| | |
|---|---|
| **Ne oldu** | 30.09.2026 09:54 UTC'de `main`'deki CI'ın konteyner taraması başarısız oldu ([çalışma 36699066208](https://github.com/Duzen-Saglik-Grubu/DSG-HRMS/actions/runs/36699066208)). API imajının taban katmanındaki `libssl3t64` paketinde yüksek önemli açık bulundu: **CVE-2026-84782**. Açık yeni yayımlanmıştı; kodda değişiklik yoktu. |
| **Süre** | `main`, 01.10.2026 07:01 UTC'deki `ef9975f` commit'ine kadar yaklaşık **21 saat** kırmızı kaldı. |
| **Düzeltme** | API Dockerfile'ına çalışma katmanındaki paketleri güncelleyen `apt-get upgrade` adımı eklendi. Düzeltme ayrı bir PR yerine bir özellik PR'ının (#108, İK davet bağlantısı) içinde geldi. |
| **Etki** | Kırmızı `main` kayda geçmedi; düzeltmenin hangi açığı kapattığı PR başlığından anlaşılmıyordu. |
| **Önlem** | `main` kırmızıya düştüğünde `[HATA] main kırmızı` issue'su otomatik açılır. `main` kırmızıyken yeni özellik birleştirilmez; düzeltme ayrı PR ile gelir (CONTRIBUTING §1.1, #129). |

## O-3 — Boş klasörlerin kaybolması (30.09.2026)

| | |
|---|---|
| **Ne oldu** | Geliştirme makinesinde `git stash -u` kullanıldı. Komut, izlenmeyen dosyalarla birlikte 33061 klasör düzenindeki boş `kayitlar/` ve `raporlar/` klasörlerini de sildi. Git boş klasörleri izlemediği için bu kayıp depoda görünmedi. |
| **Fark edilme** | 02.10.2026'da kullanıcı, `docs/33061` altındaki klasörlerin neden eksik olduğunu sordu. |
| **Düzeltme** | Klasörler geri getirildi ve her birine `.gitkeep` kondu; artık depoda izleniyorlar (PR #118). |
| **Önlem** | `git stash -u` ve `git clean -d` kullanılmaz. Ara çalışma dalda WIP commit'i olarak saklanır. Risk: R-26. |

## O-4 — SMTP sunucusunun süresi dolmuş sertifika sunması (27.09.2026)

| | |
|---|---|
| **Ne oldu** | E-posta gönderimi geliştirilirken (#85) `mail.duzen.com.tr:587` portunun kendinden imzalı, `CN=localhost` adlı ve süresi 07.03.2026'da **dolmuş** bir sertifika sunduğu görüldü. Gönderici sertifikayı doğruladığı için e-posta kanalı `Unhealthy` oldu; e-postayla kod gönderilemedi. |
| **Neden** | Postfix, web sunucusundaki geçerli `mail.duzen.com.tr` sertifikası yerine eski bir sertifikayı kullanıyordu. Sertifikanın süresi altı ay önce dolmuştu; neden daha önce fark edilmediği kayıtlı değil. |
| **Düzeltme** | Sertifika doğrulaması kapatılmadı. Postfix'in sertifika ayarları sunucu tarafında geçerli sertifikayı gösterecek şekilde düzeltildi. Düzeltme tarihi kayda geçmedi; e-posta kanalı 03.10.2026'daki davet gönderimlerinde çalışıyordu. Doğrulama komutu runbook §10.5'tedir. |
| **Açık kalan** | Geçerli sertifikanın süresi 11.10.2026'da doluyor. Yenilendikten sonra Postfix de yeniden yüklenmelidir. Risk: R-20. |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-04 | 1.0 | İlk oluşturma: O-1…O-4 geriye dönük kayıtları (#129) | Bilgi İşlem |
