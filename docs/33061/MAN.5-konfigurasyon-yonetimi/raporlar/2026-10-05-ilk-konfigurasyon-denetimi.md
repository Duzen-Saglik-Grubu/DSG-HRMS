# İlk Konfigürasyon Denetimi

**Belge kimliği:** MAN.5-KD-2026-10-05
**Süreç:** MAN.5 — Konfigürasyon Yönetimi (`MAN.5.BP5`)
**Denetlenen baseline:** `v0.2.0-rc.2` (commit `69bafad`), T3 kabul adayı
**Ölçüt:** `konfigurasyon-ogeleri.md` 1.0
**Tarih:** 2026-10-05 · **Denetleyen:** Bilgi İşlem · **Kaynak:** #132

> Denetim şu soruyu cevaplar: **Kabule sunulan yapı, kayıtlarda yazan yapı mı?** Depodaki
> kayıtlar (etiket, CHANGELOG, dağıtım kaydı, CI kanıtı) birbirleriyle ve sunucudaki
> gerçek durumla karşılaştırıldı.

---

## 1. Sonuç

| | Sayı |
|---|---|
| Denetim maddesi | 12 |
| Uygun | **12** |
| Bu denetimde sınanamadı | 0 — D-11 ve D-12, 06.10.2026'da sınandı (1.1) |
| Uygunsuzluk | **0** |

Kabul adayı `v0.2.0-rc.2` için etiket, CHANGELOG, imaj, dağıtım kaydı ve CI kanıtı
birbiriyle tutarlı. Depo dışı öğelerin ikisi (NTP ve sır dosyasının izinleri) sunucuya
erişim gerektirdiği için bu denetimde sınanamadı.

---

## 2. Maddeler

| # | Madde | Yöntem | Sonuç |
|---|---|---|---|
| D-01 | Her sürüm etiketinin CHANGELOG'da bölümü var | `git tag -l` ↔ `CHANGELOG.md` | ✅ `v0.1.0`, `v0.2.0-rc.1`, `v0.2.0-rc.2`; üçü de açıklamalı etiket ve bölüm var |
| D-02 | Kabul adayı etiketi CI'dan geçmiş commit'te | `69bafad` CI çalışması 37229959287 | ✅ Başarılı; 770 backend testi; kanıt özeti `TEC.9-dogrulama/kayitlar/2026-10-04-v0.2.0-rc.2-ci-kanit-ozeti.md` |
| D-03 | UAT'de çalışan imajlar kabul adayının commit'inden | Dağıtım betiğinin OCI etiketi karşılaştırması ve `docker inspect` (04.10.2026 20:04 UTC) | ✅ API ve web: `v0.2.0-rc.2`, commit `69bafad…` |
| D-04 | Örnek ortam dosyaları, yığınların kullandığı değişkenleri tam karşılıyor (KÖ-12) | `compose*.yml` içindeki `${…}` ↔ `.env*.example` | ✅ UAT 22/22, geliştirme 19/19; iki yönde de fazla veya eksik yok |
| D-05 | Sunucudaki dağıtım kaydı depodaki kayıtla aynı (KÖ-21) | `deployments.log` ↔ `uat-dagitim-kaydi.md` (04.10.2026) | ✅ İki satır (rc.1, rc.2) ikisinde de aynı |
| D-06 | Sunucudaki kaynak kopyası commit'ten (KÖ-22) | Dağıtım betiği `git archive` kullanıyor (#125) | ✅ Yerel ortam dosyaları sunucuya gitmiyor. Önceki dağıtımlarla kopyalanmış `docker/.env`, 04.10.2026'da silindi ve silindiği doğrulandı |
| D-07 | UAT TLS sertifikası kayıttaki tarihte (KÖ-31) | `openssl s_client` (05.10.2026) | ✅ `CN=insankaynaklaritest.duzen.com.tr`, Let's Encrypt, bitiş 16.12.2026 05:50 GMT; runbook ve SYG ile aynı |
| D-08 | E-posta sunucusu sertifikası kayıttaki tarihte (KÖ-43) | `openssl s_client -starttls smtp` (05.10.2026) | ✅ `CN=mail.duzen.com.tr`, bitiş 11.10.2026 23:59 GMT; R-20 ile aynı |
| D-09 | Dış arayüzler sağlıklı (KÖ-40…42) | UAT `/health/ready`, `/health/notifications`, `/health/sync` (05.10.2026) | ✅ Hepsi `Healthy` (`TEC.8-entegrasyon/raporlar/2026-10-05-t3-entegrasyon-raporu.md`) |
| D-10 | Git kancaları geliştirme makinesinde etkin (KÖ-15) | `git config core.hooksPath` | ✅ `.githooks` |
| D-11 | NTP eşitlemesi çalışıyor (KÖ-32) | `timedatectl` (06.10.2026) | ✅ `System clock synchronized: yes`, `NTP service: active` |
| D-12 | Sır dosyasının yeri ve izni (KÖ-30) | `stat -c '%a %U %n'` (06.10.2026) | ✅ `600 root /opt/dsg-hrms/secrets/.env.uat` |

## 3. Bir sonraki sunucu işinde yapılacaklar

Aşağıdaki komutlar ileti göndermez ve hiçbir şeyi değiştirmez:

```bash
timedatectl | grep -E "synchronized|NTP service"
stat -c '%a %U %n' /opt/dsg-hrms/secrets/.env.uat
cat /etc/chrony/sources.d/dsg-hrms-ntp.sources
```

Sonuç bu raporun bir sonraki sürümüne D-11 ve D-12 olarak yazılır.

## 4. Gözlemler

(m) => m

- **G-1:** Kabul edildiğinde (`v0.2.0`) aynı denetim tekrarlanır. Kabul edilen commit `rc.2` ile aynıysa D-02…D-05 yeniden yapılmaz; etiketin aynı commit'i gösterdiği denetlenir.
- **G-2:** D-04 denetimi şu an elle yapılıyor. Değişken eklenip örnek dosya unutulursa ilk kurulumu yapan kişi eksik değişkeni ancak açılış hatasıyla görür. CI'a alınması düşünülebilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk denetim (#132) | Bilgi İşlem |
| 2026-10-06 | 1.1 | D-11, D-12 sınandı (uygun); G-0 Webmin bulgusu (#177) | Bilgi İşlem |
| 2026-10-06 | 1.2 | G-0: Webmin kaldırıldı (#179) | Bilgi İşlem |
