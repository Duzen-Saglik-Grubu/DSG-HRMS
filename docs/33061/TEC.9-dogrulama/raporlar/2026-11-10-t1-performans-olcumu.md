# T1 Personel Yönetimi — UAT Performans Ölçümü

**Belge kimliği:** TEC.9-RPR-2026-11-10-PERF
**Süreç:** TEC.9 — Doğrulama (gereksinim doğrulama yöntemi: Analiz)
**Ortam:** UAT sunucusu (`insankaynaklaritest.duzen.com.tr`): 2 çekirdek, 7,4 GB bellek, Docker yığını; sürüm `v0.2.0-89-g6dbe9d5` (09.11.2026 21:26 UTC'de kuruldu, #381)
**Ölçüm tarihi:** 2026-11-10 (00:20–01:05, Türkiye saati; mesai dışı)
**Ölçen:** Bilgi İşlem (Claude, Doğuş Uçanok onayıyla)

> Bu rapor SYG-PERS-076, 077, 078 ve 079'un (KPÖ-PERS-1…5) UAT'de ölçülen sonuçlarını
> verir. Her ölçümün yöntemi, ham sonucu ve gereksinime göre değerlendirmesi ayrı ayrı
> yazılıdır.

---

## 1. Özet

| Gösterge | Gereksinim | Hedef | Sonuç | Değerlendirme |
|---|---|---|---|---|
| KPÖ-PERS-1 — liste sayfası (24 kart ve küçük fotoğrafları) | SYG-PERS-076 | p95 < 2 sn | **0,178 sn** (2.351 ölçüm) | ✅ Karşılanıyor |
| KPÖ-PERS-2 — kişi kartı ve fotoğrafı | SYG-PERS-077 | p95 < 2 sn | **0,045 sn** (2.386 ölçüm) | ✅ Karşılanıyor |
| KPÖ-PERS-3 — fotoğrafı değişmeyen senkronizasyon | SYG-PERS-078, 013 | < 2 dk; kişi ve istihdam adımı < 60 sn | **1,2 sn** (ön sonuç, gece) | ⏳ İş günü ölçümü §4.3 |
| KPÖ-PERS-4 — fotoğraf özeti sorgusunun LOGO'daki maliyeti | SYG-PERS-014, 078 | < 10 sn; okunan sayfa kaydedilir | **0,36 sn**; 144.732 LOB mantıksal okuma | ✅ Karşılanıyor |
| KPÖ-PERS-5 — kan grubu süzgeçli liste | SYG-PERS-079 | p95 < 2 sn | **0,249 sn** (2.336 ölçüm) | ✅ Karşılanıyor |

---

## 2. Ön koşul: T1'in UAT'ye kurulması

UAT'de 21.10.2026'dan beri T1 öncesi sürüm (`v0.2.0-27-ge488898`) çalışıyordu. Ölçüm için
`main` kuruldu (`deploy-uat.sh`; dağıtım kaydı `TEC.10-gecis/kayitlar/uat-dagitim-kaydi.md`).

- Kurulumdan önce veritabanının elle kopyası alındı (runbook §5). Kurulum doğrulandıktan sonra kopya silindi.
- Kurulum mesai dışında yapıldı, çünkü ilk senkronizasyon bütün fotoğrafları LOGO'dan bir kez okur.
- Betiğin doğrulamaları geçti:
  - web `200`, `/health/ready` sağlıklı;
  - API dışarı kapalı;
  - uygulama kısıtlı rolle (`hrms_app`) bağlı;
  - çalışan imajlar dağıtılan commit'ten.
- İlk T1 senkronizasyonu (fotoğrafların ilk aktarımı dahil) 40,4 sn sürdü ve uyarılarla tamamlandı. Uyarıların kaynağı LOGO veri kalitesidir.
- Kurulum sonrası UAT toplamları (yalnızca sayılar):

| Toplam | Değer |
|---|---|
| Kişi | 1.378 |
| Aktif kişi | 565 |
| Sicil (aktif) | 1.555 (587) |
| Fotoğraf (aktif kişilerde) | 873 (467); ortalama 14,5 KB |
| Kan grubu kayıtlı kişi (aktif) | 153 (87) |

Kan grubu UAT'de LOGO'dan şifreli okunur (`KR-125`, KVKK Sorumlusu onayı 22.10.2026).

---

## 3. KPÖ-PERS-1, 2, 5 — Yük testi

### 3.1 Yöntem

- **Ortam:** UAT sunucusunda, UAT yığınından **ayrı**, geçici bir sentetik yığın (`dsg-hrms-yuk`; Doğuş Uçanok kararı, #381).
  - Ayrı veritabanı ve ayrı API; dışarı kapalı (`127.0.0.1`).
  - İmajlar UAT kurulumunda derlenenlerin aynısı (aynı commit).
  - Şema, kurulumun ürettiği betikle uygulandı; uygulama aynı kısıtlı rolle bağlandı.
- **Neden sentetik?** Gerçek UAT'de liste ve kart ölçümü gerçek bir oturumla yapılmak zorundadır. 10 dakikalık profil gerçek kişiler için binlerce görüntüleme kaydı yazar. Bu kayıtlar değiştirilemez ve 10 yıl saklanır (`KR-109`). Sentetik yığın aynı donanımda ölçer ve UAT'nin kayıtlarına iz bırakmaz.
- **Veri:** UAT dağılımına göre üretildi (`seed-gen.mjs`; gerçek kişi yok, TCKN'ler uydurma).

| | Sentetik yığın | UAT |
|---|---|---|
| Kişi (aktif) | 1.383 (570) | 1.378 (565) |
| Fotoğraf | 903; her biri 16,3 KB | 873; ortalama 14,5 KB |
| Aktif kişide fotoğraf oranı | %83 | %83 |
| Kan grubu kayıtlı kişi | 1.378 (herkes) | 153 |
| Şube / birim ataması | Her istihdamda | Her istihdamda |

  - Kan grubu bilerek herkese verildi. Kan grubu süzgeci kapsamdaki değerleri bellekte çözer (SYG-PERS-063); süzgeç bu yüzden gerçekteki 87 yerine 565 değeri çözdü. Sonuç en kötü durumu gösterir.
  - Kan grubu, uygulamanın biçimiyle yığının kendi anahtarıyla şifrelendi (ADR-0020 §1).
- **Hesaplar:** 20 sentetik kişiye İK rolüyle hesap açıldı. Aynı hesapla açılan yeni oturum öncekini kapattığı için her sanal kullanıcının kendi hesabı vardır.
- **İstemci:** Aynı sunucuda Node.js 24 kapsayıcısı (`personnel-load.mjs`, `fetch`).
  - İstekler web kapsayıcısından (nginx) API'ye gider.
  - TLS yoktur; ağ gecikmesi yoktur. Kurum ağındaki bir tarayıcı bunların üzerine birkaç on milisaniye ekler.
- **Profil (KPÖ-PERS-1, AN-15):** 20 sanal kullanıcı, her senaryo 10 dakika. Kullanıcılar ilk 5 sn içinde dağılarak başlar; istekler arasında 5 sn bekler.

| Senaryo | Bir ölçüm | Süre |
|---|---|---|
| Liste (KPÖ-PERS-1) | Rastgele bir sayfanın listesi (24 kart, aktif personel), ardından sayfadaki fotoğraflar; tarayıcı gibi aynı anda en çok 6 fotoğraf | Liste isteğinin başlangıcından son fotoğrafın son baytına |
| Kart (KPÖ-PERS-2) | Rastgele bir aktif kişinin kartı, ardından fotoğrafı | Kart isteğinin başlangıcından fotoğrafın son baytına |
| Kan grubu süzgeçli liste (KPÖ-PERS-5) | Liste senaryosu, `bloodGroup=3` (A Rh+) süzgeciyle | Liste senaryosuyla aynı |

### 3.2 Sonuçlar (ms)

| Senaryo | Ölçüm | Hatalı | p50 | p90 | p95 | p99 | En az | En çok | Sayfa/sn | Ort. fotoğraf | Hedef |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Liste (09.11 21:34–21:44 UTC) | 2.351 | 0 | 93 | 154 | **178** | 236 | 44 | 362 | 3,89 | 19,7 | p95 < 2.000 ✅ |
| Kart (21:44–21:54 UTC) | 2.386 | 0 | 28 | 40 | **45** | 58 | 11 | 89 | 3,95 | 1 | p95 < 2.000 ✅ |
| Kan grubu süzgeçli liste (21:54–22:04 UTC) | 2.336 | 0 | 123 | 210 | **249** | 332 | 32 | 446 | 3,86 | 19 | p95 < 2.000 ✅ |

- Bütün yanıtlar `200` döndü; hata, zaman aşımı veya `5xx` olmadı.
- **Sunucu yükü:** Ortalama yük (1 dk) liste ve kan grubu senaryolarının sonunda ≈3,0, kart senaryosunun sonunda ≈0,7 idi. 2 çekirdekte bu, liste senaryolarında sunucunun dolu ama doymamış olduğunu gösterir.
- **Arka plan:** Ölçüm sırasında aynı sunucuda UAT'nin senkronizasyonu da çalıştı (21:41 ve 21:56 UTC, 1,2 sn).

### 3.3 Değerlendirme

- **SYG-PERS-076 (KPÖ-PERS-1):** ✅ Karşılanıyor. p95 0,18 sn, hedefin onda birinin altında. Liste sayfasının küçültülmüş fotoğraf kullanması (ADR-0021 §5) belirleyicidir: sayfa başına ≈20 × 16 KB.
- **SYG-PERS-077 (KPÖ-PERS-2):** ✅ Karşılanıyor. p95 0,045 sn.
- **SYG-PERS-079 (KPÖ-PERS-5):** ✅ Karşılanıyor. p95 0,25 sn, en kötü durum verisiyle.
  - Bellek içi süzgecin bedeli p95'te ≈70 ms'dir (liste senaryosuna göre).
  - ADR-0021'deki `pg_trgm` dizinine ve ADR-0020'deki süzgeç alternatiflerine gerek görülmedi.
- **Sınır:** Ölçüm TLS'siz ve ağ gecikmesiz yapıldı. Ölçülen değerlerle hedef arasındaki pay (≥ 8 kat) bunu karşılar.

---

## 4. KPÖ-PERS-3 — Senkronizasyon süresi

### 4.1 Yöntem

- UAT'deki çalışma kayıtları (`personnel.sync_run`), kurulumdan sonraki zamanlanmış çalışmalar.
- Çalışma kaydı adım sürelerini ayrıca tutmaz; yalnızca başlangıç ve bitiş vardır.
  - Kişi ve istihdam adımı (SYG-PERS-013) çalışmanın bir parçasıdır. Bu yüzden toplam süre 60 sn'nin altındaysa iki hedef birlikte karşılanır.
  - Toplam süre 60 sn'yi aşarsa adım süresi ayrıca ölçülmelidir.

### 4.2 Ön sonuç (gece, 09.11.2026)

| Çalışma (UTC) | Durum | Süre | Not |
|---|---|---|---|
| 21:26 | Uyarılarla tamamlandı | 40,4 sn | İlk T1 çalışması: bütün fotoğraflar ilk kez aktarıldı (KPÖ dışı) |
| 21:41 | Uyarılarla tamamlandı | 1,24 sn | Fotoğrafı değişmeyen çalışma; yük testi sürerken |
| 21:56 | Uyarılarla tamamlandı | 1,18 sn | Fotoğrafı değişmeyen çalışma; yük testi sürerken |

Karşılaştırma: T1 öncesi sürümde (yalnızca kişi ve istihdam adımı) çalışma 0,27–0,31 sn sürüyordu.

### 4.3 İş günü ölçümü

KPÖ-PERS-3 bir iş günü boyunca, yani LOGO yoğunken ölçülür. 10.11.2026 iş gününün çalışmaları
bu bölüme eklenecektir (çalışma sayısı, p50, p95, en kötü).

---

## 5. KPÖ-PERS-4 — Fotoğraf özeti sorgusunun LOGO'daki maliyeti

### 5.1 Yöntem

- Sorgu, uygulamanın kullandığıyla birebir aynıdır (`LogoPersonnelSource.GetPhotoDigestsAsync`):

```sql
SELECT PERREF AS LogoRef, CONVERT(char(64), HASHBYTES('SHA2_256', CAST(LDATA AS varbinary(max))), 2) AS Hash
FROM LH_001_FAMILY
WHERE RELATION = 0 AND PERREF IS NOT NULL AND LDATA IS NOT NULL AND DATALENGTH(LDATA) > 0
ORDER BY PERREF, LREF;
```

- Salt okunur `hrms_logo_reader` oturumuyla, `SET STATISTICS IO ON; SET STATISTICS TIME ON;` altında, **bir kez** çalıştırıldı (Doğuş Uçanok onayı, #381).
- Zaman: 10.11.2026 00:23 (mesai dışı); UAT kurulumu henüz T1'i başlatmamışken. Sorgu sırasında LOGO'da HRMS kaynaklı başka okuma yoktu.
- Araç: UAT sunucusunda geçici `mcr.microsoft.com/mssql-tools` kapsayıcısı; ölçümden sonra silindi.
- Sonuç satırları (kart referansı ve özet) geçici bir dosyada kaldı ve silindi. Yalnızca istatistik iletileri okundu. Bağlantı bilgisi ekrana ve süreç listesine yazılmadı.

### 5.2 Sonuç

| Ölçü | Değer |
|---|---|
| Sunucu süresi (`elapsed time`) | **361 ms** |
| İşlemci süresi | 360 ms |
| İstemci tarafı toplam (kapsayıcı açılışı dahil) | 1,6 sn |
| Dönen satır (fotoğraflı kart) | 988 |
| `LH_001_FAMILY` tablo sayfası | 28 mantıksal okuma, 0 fiziksel okuma |
| LOB sayfası | 144.732 mantıksal okuma; 35.914 ileri okuma (≈281 MB) |

### 5.3 Değerlendirme

✅ KPÖ-PERS-4 karşılanıyor: sorgu LOGO'da 0,36 sn sürdü (hedef < 10 sn).
- Fotoğrafların LOB sayfaları her çalışmada okunur (≈144 bin sayfa); maliyetin tamamı budur. Süre buna rağmen 15 dakikalık periyodun çok küçük bir kesridir.
- ADR-0021 §5'teki iki yedek yöntemin (LOGO'da hesaplanmış sütun ve değişiklik tarihi) uygulanmasına gerek görülmedi.
- İş günü yükünde süre artabilir; KPÖ-PERS-3'ün iş günü ölçümü (§4.3) toplam çalışma süresinde bunu gösterecektir.

---

## 6. Ölçümün yan etkileri

- **UAT kurulumu:** UAT artık T1'i çalıştırıyor (`v0.2.0-89-g6dbe9d5`). Senkronizasyon 15 dakikada bir T1 alanlarını, fotoğraf özetlerini ve (`KR-125`) şifreli kan grubunu okuyor.
- **Veritabanı kopyası:** Kurulum öncesi alınan kopya (`yedek-2026-11-10.sql.gz`, yalnızca root okur) kurulum doğrulandıktan sonra silindi.
- **LOGO:** Fotoğraf özeti sorgusu bir kez elle çalıştırıldı (§5). Kurulumdan sonraki ilk senkronizasyon bütün fotoğrafları bir kez okudu (40,4 sn).
- **Sentetik yığın:** Kapsayıcılar, veritabanı birimi, ağ ve geçici imajlar (`node:24-alpine`, `axllent/mailpit`, `mssql-tools`) ölçümden sonra silindi. Sunucuda yalnızca UAT'nin üç kapsayıcısı çalışıyor.
- **UAT kayıtları:** Yük testi UAT'nin veritabanına bağlanmadı. UAT'de ölçüm kaynaklı erişim kaydı, hesap veya güvenlik olayı yoktur.
- **Betikler:** `personnel-load.mjs` ve `seed-gen.mjs` depoya alınmadı. Yeniden ölçüm gerekirse bu raporun yöntemiyle yeniden yazılabilir; ikisi de yalnızca sentetik yığına bağlanır.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-11-10 | 0.1 | İlk ölçüm: KPÖ-PERS-1, 2, 4, 5; KPÖ-PERS-3 ön sonucu (#381) | Bilgi İşlem |
