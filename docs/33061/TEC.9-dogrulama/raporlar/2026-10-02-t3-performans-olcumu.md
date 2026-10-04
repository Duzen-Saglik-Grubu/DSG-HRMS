# T3 Kimlik Yönetimi — UAT Performans Ölçümü

**Belge kimliği:** TEC.9-RPR-2026-10-02-PERF
**Süreç:** TEC.9 — Doğrulama (gereksinim doğrulama yöntemi: Analiz)
**Ortam:** UAT (`insankaynaklaritest.duzen.com.tr`): 2 çekirdek, 7,4 GB bellek, Docker yığını; sürüm `4199526` (ilk ölçüm), `2d79f01` (tekrar, #120 sonrası)
**Ölçüm tarihi:** 2026-10-02; tekrar ve e-posta ölçümü 2026-10-04
**Ölçen:** Bilgi İşlem

> Bu rapor SYG-KMLK-077, 078 ve 079'un (KPÖ-KMLK-1, 3, 4) UAT'de ölçülen sonuçlarını
> verir. Her ölçümün yöntemi, ham sonucu ve gereksinime göre değerlendirmesi ayrı ayrı
> yazılıdır.

---

## 1. Özet

| Gereksinim | Ölçüt | Hedef | Sonuç | Değerlendirme |
|---|---|---|---|---|
| SYG-KMLK-077 | Giriş yanıt süresi, p95 | < 1 sn (60 sn'ye yayılan 50 giriş); < 5 sn (aynı anda 50 giriş) | **0,279 sn**; **4,42 sn** (04.10, #120 sonrası) | ✅ Karşılanıyor. İlk ölçümde (02.10) karşılanmıyordu; bkz. §2.4–2.5 |
| SYG-KMLK-078 | Tam senkronizasyon süresi | < 60 sn (≈1.540 kart) | p95 **0,43 sn**, en kötü **3,6 sn** (573 çalışma) | ✅ Karşılanıyor |
| SYG-KMLK-079 | Kodun teslim süresi, p95 | < 60 sn (e-posta ve SMS) | SMS p95 **0,50 sn** (16 gönderim); e-posta p95 **0,43 sn** (25 gönderim) | ✅ Karşılanıyor |

---

## 2. SYG-KMLK-077 — Giriş yanıt süresi

### 2.1 Yöntem

- **İstemci:** Kurum ağındaki bir iş istasyonu. İstekler HTTPS ile gerçek yoldan geçer: TLS, nginx ve API.
- **İstek:** `POST /api/v1/identity/sessions`, var olmayan sentetik adreslerle (`perf.<çalışma>.<n>@duzen.com.tr`).
  - Sunucu, var olmayan kullanıcı için de parola özetini hesaplar (SYG-KMLK-032). Bu yüzden isteğin maliyeti gerçek girişle aynıdır.
  - Gerçek hesaplara dokunulmamıştır; hiçbir hesap kilitlenmemiştir.
- **Betik:** `signin-load.mjs` (Node.js 24, `fetch`). Süre, isteğin gönderilmesinden yanıtın son baytına kadar ölçülür.

| Senaryo | Tanım | İstek |
|---|---|---|
| Yüksüz | Tek kullanıcı, ardışık 20 giriş | 20 |
| Gerçekçi (mesai başı) | 50 kullanıcı 60 saniyeye yayılarak birer kez giriş yapar; 3 tur | 150 |
| Dalga | 50 istek aynı anda; 5 tur, aralarında 5 sn | 250 |
| En kötü durum | 50 kullanıcı aynı anda ve ardışık 10'ar kez (kapalı döngü) | 500 |

### 2.2 Sonuçlar (ms)

| Senaryo | p50 | p90 | p95 | p99 | En az | En çok | İşlem/sn |
|---|---|---|---|---|---|---|---|
| Yüksüz | 1.013 | 1.019 | 1.023 | 1.092 | 1.004 | 1.092 | 0,98 |
| Gerçekçi (mesai başı) | 1.019 | 1.020 | **1.021** | 1.022 | 1.004 | 1.043 | — |
| Dalga | 3.875 | 4.523 | **4.707** | 4.946 | 1.160 | 5.039 | 5,19 |
| En kötü durum | 3.801 | 6.107 | **7.416** | 8.707 | 1.006 | 11.500 | **11,27** |

Bütün yanıtlar beklenen `401` koduyla döndü; hata, zaman aşımı veya `5xx` olmadı.

### 2.3 Analiz

1. **1 saniyelik yapay alt sınır.**
   - Giriş ucuna, üyelik için tasarlanmış en kısa yanıt süresi (`MinimumResponseTime`, varsayılan 1 sn) uygulanmış. Bu ayar PR #94'te eklendi.
   - Yüksüz ve gerçekçi senaryolarda ölçülen sürenin neredeyse tamamı bu alt sınırdır; asıl iş ≈20 ms'nin altındadır.
   - Bu sınır varken p95 hiçbir donanımda 1 saniyenin altına inemez. SYG-KMLK-077 ile **yapısal çelişki** vardır.
   - Giriş ucu için bu sınırı isteyen bir gereksinim yoktur. SYG-KMLK-015 üyelik içindir. SYG-KMLK-032 yalnızca var olmayan kullanıcı için de parola özeti hesaplanmasını ister; bu zaten yapılıyor.
2. **Kapasite.**
   - En kötü durumda sunucu saniyede ≈11 giriş işledi. Bu, iki çekirdekte giriş başına ≈180 ms işlemci süresi demektir. Bunun neredeyse tamamı parola özetidir: PBKDF2-HMAC-SHA512, 210.000 yineleme (SYG-KMLK-049, 100–500 ms aralığı).
   - 50 isteğin **aynı anda** yanıtlanması için gereken işlemci süresi, 2 çekirdekte en az ≈4,5 sn'dir.
   - 2 çekirdekte p95'in 1 sn altında kalması için özetin ≈40 ms'nin altına inmesi gerekir; bu, SYG-KMLK-049'un alt sınırını (100 ms) ihlal eder. Alternatif olarak sunucuda ≈10 veya daha fazla çekirdek gerekir.
3. **Gereksinimin anlamı.**
   - "50 eş zamanlı kullanıcı" iki türlü okunabilir:
     - **(a)** Sistemde aynı anda çalışan 50 kullanıcı, girişlerin mesai başına yayılması. Yük testlerinde yaygın kullanılan anlam budur.
     - **(b)** Aynı anda gönderilmiş 50 giriş isteği.
   - Okuma (a) alt sınır kaldırılınca karşılanır. Okuma (b), SYG-KMLK-049 korunarak bu donanımda karşılanamaz.

### 2.4 Değerlendirme ve öneri

SYG-KMLK-077 **bugünkü hâliyle karşılanmıyor.** Önerilen düzeltmeler:

1. **Alt sınırın kaldırılması:** Giriş ucundaki `MinimumResponseTime` kaldırılır; üyelik ve parola sıfırlama uçlarında (SYG-KMLK-015) korunur. Bu bir hata düzeltmesidir.
2. **Gereksinimin netleştirilmesi (AN-25, İK veya Bilgi İşlem kararı):** "Eş zamanlı kullanıcı" okuma (a) ile tanımlanır. Ek olarak, aynı anda gelen 50 istek için bir **üst sınır** konur (örneğin p95 < 5 sn); bu en kötü durum davranışını belgelemiş olur.
3. Düzeltmeden sonra ölçüm tekrarlanır ve bu rapor güncellenir.

**Karar (Doğuş Uçanok, 03.10.2026; `KR-095`, AN-25):** Öneri 1 ve 2 kabul edildi.
- Alt sınır giriş ucundan kaldırıldı (#120, PR #141).
- SYG-KMLK-077 şöyle netleştirildi: 60 saniyeye yayılan 50 giriş için p95 < 1 sn; aynı anda gelen 50 giriş için p95 < 5 sn.

### 2.5 Tekrar ölçümü (04.10.2026, sürüm `2d79f01`)

Yöntem ve betik §2.1 ile aynıdır.

| Senaryo | p50 | p90 | p95 | p99 | En az | En çok | İşlem/sn | Hedef |
|---|---|---|---|---|---|---|---|---|
| Yüksüz (20) | 185 | 201 | 208 | 602 | 171 | 602 | 4,85 | — |
| Gerçekçi: 60 sn'ye yayılan 50 giriş (150) | 207 | 275 | **279** | 318 | 170 | 321 | — | p95 < 1.000 ✅ |
| Aynı anda 50 giriş, 5 dalga (250) | 3.840 | 4.280 | **4.418** | 4.591 | 479 | 4.632 | 5,33 | p95 < 5.000 ✅ |
| Sürekli yük: 50 kullanıcı × 10 ardışık (500) | 3.906 | 5.976 | 6.962 | 9.335 | 182 | 10.914 | 11,78 | tanım dışı (bilgi) |

- **Gerçekçi yük:** Alt sınır kalkınca p95 1.021 ms'den **279 ms**'ye indi. Giriş başına süre ≈180 ms'dir; bunun neredeyse tamamı parola özetidir (SYG-KMLK-049).
- **Aynı anda 50 giriş:** p95 4,42 sn, üst sınırın (5 sn) altında. Sınıra yakın olduğu için sunucunun çekirdek sayısı azaltılmamalıdır. Kurumda eş zamanlı giriş sayısı artarsa yeniden ölçülmelidir (R-kaydı: #126).
- **Sürekli yük:** 50 kullanıcının art arda 10'ar giriş yapması gereksinimin tanımı dışındadır. Sistemin doyma noktasını gösterir: saniyede ≈12 giriş.

**Değerlendirme:** ✅ SYG-KMLK-077 karşılanıyor.

---

## 3. SYG-KMLK-078 — Tam senkronizasyon süresi

- **Yöntem:** UAT veritabanındaki çalışma kayıtları (`personnel.sync_run`), 26.09.2026–02.10.2026, zamanlanmış 573 çalışma.
- **Kapsam:** Her çalışma tam senkronizasyondur. Ortalama okunan kart 1.542, en çok 1.543.

| Ölçü | Değer |
|---|---|
| Çalışma sayısı | 573 (tümü `CompletedWithWarnings`) |
| p50 | 0,27 sn |
| p95 | 0,43 sn |
| En kötü | 3,60 sn |

**Değerlendirme:** ✅ Hedefin (60 sn) çok altında. İstihdamı biten kişinin hesabı en geç periyot (15 dk) ile birkaç saniye içinde kapanır.

> Not: Çalışmaların tümü uyarıyla tamamlanıyor. Bunun nedeni LOGO veri kalitesi uyarılarıdır (SYG-KMLK-005, `sync_warning`); süreyi etkilemez.

---

## 4. SYG-KMLK-079 — Doğrulama kodunun teslim süresi

- **Yöntem:** Gönderim kayıtları (`notification.delivery`). Ölçülen süre, kuyruğa alınmadan (`queued_at`) alıcının sunucusunun veya operatörün kabulüne (`completed_at`) kadardır.
- **Örnek:** UAT (`AllowList` kipi) ve yerel ortam (gerçek gönderim).

| Kanal | Ortam | Başarılı | p50 | p95 | En kötü | En çok deneme |
|---|---|---|---|---|---|---|
| SMS | UAT | 10 | 0,24 sn | 0,50 sn | 0,59 sn | 1 |
| SMS | Yerel | 6 | 0,30 sn | 0,35 sn | 0,35 sn | 1 |
| E-posta | UAT | 25 | ≈0,09 sn | **0,43 sn** | 0,61 sn | 1 |

E-posta örneği, 04.10.2026'da alıcının onayıyla izin listesindeki adrese hesap işlemleri ekranından gönderilen 20 davet iletisiyle tamamlandı. Davet ve doğrulama kodu iletileri aynı SMTP yolunu kullanır. Toplam 25 başarılı gönderimin 23'ü davet, 2'si doğrulama kodudur; en çok deneme 1'dir.

UAT'deki 1 e-posta gönderimi başarısızdır (0,19 sn, `smtp-tls`). Bu deneme, 01.10.2026'daki SMTP sertifika sorunu sırasında yapılmıştır; sorun giderildi.

**Değerlendirme:**
- **SMS:** ✅ Karşılanıyor; 16 gönderimin tamamı 1 saniyenin altında.
- **E-posta:** ✅ Karşılanıyor; 25 gönderimde p95 0,43 sn, en kötüsü 0,61 sn.
- **Yeniden deneme ve sonuç kaydı** (gereksinimin ikinci cümlesi): `NotificationDispatcherTests` ile doğrulandı (PR #86).

---

## 5. Ölçümün yan etkileri

- Giriş testi, var olmayan 920 adres için `identity.login_throttle` tablosunda hatalı giriş sayacı oluşturdu.
- Bu satırlar gerçek bir hesaba bağlı değildir ve kilit oluşturmaz.
- 02.10'daki 920 satır 03.10.2026'da kullanıcı tarafından silindi.
- 04.10'daki tekrar ölçüm yine 920 sayaç satırı bıraktı: `updated_at >= 2026-10-04T10:26:14Z`, `failed_count = 1`, `locked_until IS NULL`. Temizlik onaya bırakılmıştır.
- **Kimlik olayları (SYG-KMLK-060):** 04.10 ölçümü `audit.security_event` tablosuna 920 `SignInFailed / invalid-credentials` olayı yazdı; hesap bilgisi yok. Kayıt değiştirilemez olduğundan (`KR-060`) bu olaylar **silinmez**. Zaman aralığıyla (04.10.2026 10:26–10:32 UTC) ve kurum ağındaki ölçüm istemcisinin IP'siyle ayırt edilir.
- **İleri ölçümler için öneri:** Sentetik yük testleri, olay incelemesini karıştırmaması için kayda geçirilmelidir (bu rapor).

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-02 | 0.1 | İlk ölçüm | Bilgi İşlem |
| 2026-10-04 | 0.2 | SYG-KMLK-077 tekrar ölçümü (#120 sonrası, `KR-095`); SYG-KMLK-079 e-posta ölçümü tamamlandı (#121) | Bilgi İşlem |
