# TEC.10 — Süreç Yaklaşımı

**Belge kimliği:** TEC.10-YAK
**Süreç:** TEC.10 — Geçiş
**Son güncelleme:** 2026-10-05
**Karşıladığı öznitelik maddeleri:** `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir. `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir.

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Kurulum ve dağıtım
> komutları runbook'tadır (`uat-ortami-kurulum-runbook.md`); bu belge onları süreç
> adımlarına bağlar.

---

## 1. Sürecin amacı ve sınırı

Geçiş, doğrulanmış sistemi **çalışacağı ortama kurar** ve orada işletime hazır hâle getirir (TS ISO/IEC TS 33061, TEC.10).

Bugün projede iki tür geçiş vardır; yalnızca birincisi işletilmektedir:

| | UAT'ye geçiş (bugün) | Üretime geçiş (gelecek) |
|---|---|---|
| Ne zaman | Her kabul adayı ve gerektiğinde ara dağıtım | Proje planı aşaması **AS — Geçiş** |
| Ortam | `https://insankaynaklaritest.duzen.com.tr` | Henüz kurulmadı |
| Sürüm | `vX.Y.0-rc.N` (`KR-097`) | `v1.0.0` (proje planı §5.1) |
| Veri | Gerçek personel verisi; LOGO'dan okunur, maskelenmez (runbook §1, R-25) | Eski HRMS'ten veri göçü (R-01) |
| Kullanıcı | İK ve kabul katılımcıları (`KR-048`) | Tüm personel |

**Kapsamda değildir:** sistemin kabulü. Kabulü İK verir (TEC.11). Geçiş, kabulün yapılacağı ortamı ve sürümü hazırlar.

---

## 2. Süreç nasıl işletilir? (UAT)

### 2.1 Ortamın hazırlanması (bir kez)

Sunucu, sırlar, TLS, LOGO bağlantısı, ileti gönderimi ve sunucu saati runbook'a göre kurulur (§2, §7–§12). Her yeni gereklilik runbook'a bir bölüm olarak eklenir. Runbook'un amacı kurulumu **tekrarlanabilir** kılmak ve tek kişiye bağımlılığı azaltmaktır (R-05).

### 2.2 Her dağıtımda

1. **Sürüm seçilir** (runbook §3.0). Kabul oturumu için etiketli sürüm kullanılır (`KR-097`): kabul adayı `vX.Y.0-rc.N` olarak etiketlenir ve UAT'ye **bu etiketten** kurulur. Ara dağıtım `main`'in son hâlinden yapılabilir; betik bu durumda uyarı verir.
2. **Dağıtım betiği çalıştırılır:** `./docker/deploy-uat.sh`. Betik şunları yapar:
   - kodun commit edilmiş ve GitHub'a gönderilmiş olmasını şart koşar;
   - kaynağı `git archive` ile aktarır; yerel ortam dosyaları sunucuya gitmez (#125);
   - imajları derler; imaj, sürümü ve commit'i OCI etiketinde taşır;
   - şemayı idempotent SQL betiğiyle uygular; şema uygulamanın açılışında değişmez (`KR-065`);
   - yığını başlatır ve doğrular (runbook §4): konteynerler sağlıklı, HTTPS ve sertifika geçerli, API ve veritabanı dışarıya kapalı, çalışan imajın commit'i dağıtılanla aynı.
3. **Dağıtım kaydedilir** (runbook §3.5). Doğrulama geçerse betik bir satırı iki yere yazar: sunucuda `/opt/dsg-hrms/deployments.log`, depoda `kayitlar/uat-dagitim-kaydi.md`. Depodaki satır PR ile commit edilir. Doğrulama geçmezse kayıt yazılmaz.
4. **Kabul adayıysa** sürüm, kabul planının §3 ve §4'üne yazılır (TEC.11). CHANGELOG'da sürümün bölümü vardır.
5. **Kabul sonrasında düzeltme gerekirse** yeni aday `rc.N+1` olarak etiketlenir ve yeniden kurulur. Örnek: #138 düzeltmesi için `v0.2.0-rc.2`, `v0.2.0-rc.1`'in yerine kuruldu (#160).
6. Dağıtımda bulunan sorun `[HATA]` issue'su olur (örn. #79, #103) ve TEC.13 akışıyla kapanır.

### 2.3 İşletim

Durum, günlük ve yeniden başlatma komutları runbook §5'tedir. **Yedek kuralı (`KR-098`):** UAT veritabanı düzenli yedeklenmez; riskli bir işlemden önce elle kopya alınır ve işlem doğrulanınca silinir. **TLS sertifikası** 90 günde bir elle yenilenir (runbook §7.2, `KR-067`, R-18); bitiş tarihi runbook §1'de yazılıdır.

### 2.4 Kullanıcı bilgilendirmesi

Kullanıcıya dönük ilk yönlendirme, kabul katılımcıları için yazılan notur (`TEC.11-gecerleme/T3-kullanim-notu.md`). Not bilinçli olarak adım adım yönlendirme içermez: kabul, personelin sistemi kendi başına kullanabildiğini ölçer. Üretim için kullanıcı eğitimi ve kullanım kılavuzu henüz yoktur (§8).

---

## 3. Geçiş kısıtları (`TEC.10.BP1`)

| Kısıt | Kaynak |
|---|---|
| UAT ayrı yığın, ayrı veritabanı ve ayrı portlarla çalışır | `KR-024`, `KR-065` |
| UAT'ye kabul için yalnızca etiketli sürüm gider | `KR-065`, `KR-097` |
| Sırlar imaja ve depoya girmez; sunucuda kaynak ağacının dışında durur | `KR-065`, runbook §2.2 |
| İleti yalnızca izin listesindeki alıcılara gider (`AllowList`); `Send` UAT'de kullanılmaz | `KR-083`, runbook §10.2 |
| API yalnızca HTTPS ile gelen isteğe hizmet verir | `KR-093`, runbook §10.4 |
| Sertifika elle yenilenir; HSTS kapalıdır | `KR-067`, R-18 |
| Sunucu saati eşitlenmiş olmalıdır | runbook §12, R-21 |
| UAT veritabanı düzenli yedeklenmez | `KR-098` |

---

## 4. Roller

| Rol | Sorumluluk |
|---|---|
| **Bilgi İşlem** (Doğuş Uçanok) | Runbook, dağıtım betiği, dağıtım ve dağıtım kaydı; sunucu işletimi; sertifika yenileme (RACI: R) |
| **İK** | Geçişin onayı ve kullanıcı tarafının hazırlığı (RACI: A); katılımcı notunu gözden geçirir |
| **Kurum DNS ve e-posta yöneticileri** | TXT kaydı (TLS yenileme), Postfix sertifikası (R-20) |

RACI: `MAN.1-proje-planlama/roller-ve-sorumluluklar.md` §3.1.

---

## 5. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Kurulum ve dağıtım yordamı | `uat-ortami-kurulum-runbook.md` | Komutlar, beklenen sonuçlar, gerekçeler; değişiklik geçmişi | Bilgi İşlem |
| Dağıtım betiği | `docker/deploy-uat.sh` | Kod; öz-denetim adımları betikte | Bilgi İşlem |
| Dağıtım kaydı | `kayitlar/uat-dagitim-kaydi.md`; sunucuda `/opt/dsg-hrms/deployments.log` ve `VERSION` | Satır başına zaman (UTC), sürüm, commit, dağıtan | Betik yazar, Bilgi İşlem commit eder |
| Sürüm etiketi ve sürüm notu | Git etiketi (açıklamalı); `CHANGELOG.md` | Etiket CHANGELOG bölümüyle eşleşir (`KR-097`) | Bilgi İşlem |
| Çalışan sürümün kanıtı | İmaj etiketleri `org.opencontainers.image.version` ve `.revision` | `docker inspect` ile okunur (runbook §3.2) | Betik |
| Geçiş anomalileri | GitHub issue (`[HATA]`) | Belirti, neden, düzeltme | Bulan |
| Katılımcı notu | `TEC.11-gecerleme/T3-kullanim-notu.md` | Kısa metin, İK gözden geçirir | Bilgi İşlem |

---

## 6. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** Runbook, betik, dağıtım kaydı ve bu belge yalnızca PR ile değişir. Runbook'un değişiklik geçmişi her bölümün hangi issue ile geldiğini yazar.
- **Kaydın doğruluğu:** Dağıtım kaydını kişi değil betik yazar ve yalnızca doğrulama geçince yazar. Çalışan imajın commit'i dağıtılanla eşleşmezse dağıtım başarısız sayılır.
- **Kaydın değişmezliği:** Dağıtım kaydına yalnızca satır eklenir. 04.10.2026 öncesi dağıtımlar kayıtlı değildir; bu durum kaydın başında açıkça yazılıdır.
- **Sırlar:** Hiçbir kayıtta sır yoktur. Sırlar ekrana yazdırılmadan doğrudan sunucudaki sır dosyasına eklenir (runbook §9–§10).
- **Kişisel veri:** UAT gerçek veriyle çalışır. Runbook ve kayıtlara kişisel veri yazılmaz; yedek kopyalar `KR-098`'e göre az ve kısa ömürlüdür.

---

## 7. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (TEC.10) | Karşılığı |
|---|---|
| a) Geçiş kısıtları belirlenir | §3 |
| b) Destekleyici sistemler mevcut | UAT sunucusu, TLS, NTP, sır dizini (runbook §1, §7, §12) |
| c) Kurulum yeri hazırlanır | Runbook §2 ve §7–§12 |
| d) Kurulu sistem işlevlerini sunabilir | Betiğin doğrulaması (§2.2), runbook §4 |
| e) Operatör ve kullanıcılar hazırlanır | Runbook (operatör); katılımcı notu (kullanıcı). Üretim eğitimi yok (§8) |
| f) Sonuçlar ve anomaliler belirlenir | Dağıtım kaydı; `[HATA]` issue'ları |
| g) Kurulu sistem etkin ve işletime hazır | UAT için sağlık uçları ve HTTPS; üretim için henüz yok |
| h) Geçişi yapılan öğelerin izlenebilirliği | Sürüm etiketi → dağıtım kaydı → imaj etiketi → kabul formu |

---

## 8. Açık noktalar

| Konu | Durum |
|---|---|
| **Üretim ortamı ve geçiş planı** | Yok. Aşama AS'de (`v1.0.0`) planlanacak: ortam, kesim (cut-over), takvim |
| **Veri göçü** | Başlamadı. Önlemler R-01'de yazılı (idempotent betik, mutabakat raporu, üç deneme); `raporlar/` boş |
| **Geri dönüş (rollback)** | Yazılı yordam yok. Önceki etiketten betik yeniden çalıştırılabilir; ancak şema betiği yalnızca eksik migration'ları uygular, şemayı geri almaz |
| **Kullanıcı eğitimi ve kılavuz** | Üretim için yok. Bugün yalnızca kabul katılımcı notu var |
| **Runbook'ta bayat satırlar** | §4 doğrulama komutları `http://` ile `200` bekliyor (TLS'ten beri `301`; betik doğru denetliyor). §6'da "uygulamada oturum yok" ve ufw satırları güncel değil |
| **SSH parola girişi** | Hâlâ açık (runbook §1 ve §6, R-25) |
| **Sertifika takibi** | Elle; bitiş tarihi runbook §1'de. Sistem içi uyarı Y4'te gelecek (#42, R-18) |

---

## 9. Gözden geçirme

Bu belge her modül sonu süreç denetiminde (MAN.8) ve üretim geçişi planlanırken gözden geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
