# UAT Hesap Temizliği

**Belge kimliği:** TEC.10-KYT-2026-10-10-HESAP
**Süreç:** TEC.10 — Geçiş, TEC.11 — Geçerleme (kabul hazırlığı)
**Tarih:** 2026-10-10 · **Yapan:** Bilgi İşlem · **Talep eden:** Doğuş Uçanok (#187)

## Neden

T3 kabul oturumundan önce UAT'de deneme amacıyla açılmış hesaplar silindi. İK, kabul için
önceden belirlenen izin listesinin yeterli olduğunu bildirdi. Kabul senaryoları (KS-01…KS-03)
hesabı olmayan personelle başlamalıdır.

## Yapılan

| Kapsam | Silinen | Not |
|---|---|---|
| Kullanıcı hesabı | 3 | Sistem Yöneticisi hesabı (Doğuş Uçanok) **korundu** |
| Bu hesapların oturumları ve yenileme jetonları | 3 + 3 | |
| Doğrulama kodları | 20 | Tüm kişiler |
| Kod gönderim istekleri (hız sınırı sayacı) | 21 | Tüm kişiler |
| Üyelik denemeleri (hız sınırı sayacı) | 21 | Tüm kişiler |
| Giriş kısıtlama sayaçları | 4 | Tüm kişiler |
| Davet bağlantıları | 25 | Tüm kişiler |

**Yöntem:** İşlem tek bir veritabanı işlemi (transaction) içinde yapıldı. Önce aynı komutlar
çalıştırılıp **geri alındı** (deneme). Sayılar beklenenle aynı çıkınca kalıcı olarak
uygulandı. Sonrasında kalan hesap sayısı 1, Sistem Yöneticisi sayısı 1; UAT sağlıklı.

**Silinmeyenler:** Denetim izi ve güvenlik olayı kayıtları değiştirilemezdir (`KR-060`,
`KR-094`); silinen hesaplarla ilgili geçmiş kayıtlar bu tablolarda durur. Personel verisi
(LOGO senkronizasyonu) silinmedi.

**Yedek:** Alınmadı. Silinen veri test ortamına aittir ve yeniden üretilebilir (`KR-098`).

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-10 | 1.0 | İlk kayıt (#187) | Bilgi İşlem |
