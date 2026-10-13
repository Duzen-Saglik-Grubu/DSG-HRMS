# T3 Kimlik Yönetimi — Kabul Raporu

**Belge kimliği:** TEC.11-RPR-2026-10-13-T3
**Süreç:** TEC.11 — Geçerleme (çıktılar d, f, g, h; YAKLAŞIM §7)
**Modül:** T3 Kimlik Yönetimi
**Kabul edilen sürüm:** `v0.2.0-rc.5` (`149f6ee`) → `v0.2.0` (`KR-097`)
**Hazırlayan:** Bilgi İşlem · **Tarih:** 2026-10-13

---

## 1. Sonuç

İK, T3 Kimlik Yönetimi'ni 13.10.2026'daki kabul oturumunda **kabul etti** (G3). Koşul yok;
karşılanmayan "Olmalı" gereksinim yok. Kabul formu:
`kayitlar/2026-10-13-t3-kabul-formu.md`.

| Ölçüt | Sonuç |
|---|---|
| Kabul senaryoları (KS-01…KS-11) | **11 / 11 geçti** |
| Kanıt gösterimi (KG-01…KG-10) | **10 / 10 İK inceledi** |
| İK'ya ayrıca gösterilen durumlar (AN-01, SYG-064 istisnaları) | **2 / 2 uygun** |
| Bilgi İşlem yorumları (Kabul Planı §6.1, T-01…T-04) | **4 / 4 uygun** |
| Kabulde açılan `[HATA]` kaydı | **Yok** |
| Onaylı paydaş gereksinimleri (PG-KMLK, 56) | **56 / 56** bir senaryo veya kanıt gösterimiyle kabul edildi |

---

## 2. Oturum

| | |
|---|---|
| Tarih ve yer | 13.10.2026 · Düzen Akademi Toplantı Salonu |
| Ortam | UAT, gerçek LOGO verisi (`KR-024`); iletiler izin listesi kipinde (`KR-083`) |
| Kabul kararı | Elvan Laleli Şahin, İnsan Kaynaklarından Sorumlu Yönetim Kurulu Üyesi |
| Katılımcılar | İK kullanıcısı 1, personel 4, sistem yöneticisi 1 (ad soyad kabul formunda) |
| Sürüm | `v0.2.0-rc.5`; UAT'ye 12.10.2026 22:51'de bu etiketten kuruldu |
| CI kanıtı | `TEC.9-dogrulama/kayitlar/2026-10-12-v0.2.0-rc.5-ci-kanit-ozeti.md`: backend 796/796, ön yüz 247/247 test; backend satır kapsamı %92,9 |

**Katılımcı seçimi:** İK, katılımcıları UAT izin listesinde zaten bulunan kişiler arasından
seçti; listeye ek kişi eklenmedi (Doğuş Uçanok'un bildirimi, 12.10.2026). Personel
katılımcıların dördü de Bilgi İşlem'dendir. Plan "İK dışından gönüllü personel" ister ve bu
karşılanmıştır. Ancak sonraki modüllerde, son kullanıcıyı daha iyi temsil etmesi için
katılımcıların farklı birimlerden seçilmesi önerilir (§6).

---

## 3. Kabule giden yol

### 3.1 Kabul adayları (`KR-097`)

| Aday | Neden yenilendi |
|---|---|
| `v0.2.0-rc.1` (04.10.2026) | İlk aday |
| `v0.2.0-rc.2` (04.10.2026) | Sistem yöneticisinin kendi hesabını pasife alabilmesi düzeltildi (#138) |
| `v0.2.0-rc.3` (10.10.2026) | Üyelik deneme ve kod gönderim sınırları 10 (değişiklik talebi #185, İK onaylı) |
| `v0.2.0-rc.4` (11.10.2026) | 2FA kullanıcı tercihine bağlı; İK kurtarma işlemi (değişiklik talebi #190, `KR-104`, İK onaylı) |
| `v0.2.0-rc.5` (12.10.2026) | Bilgi İşlem provasında bulunan düzeltme: T5'e ait PRM-HSP-03 parametre ekranından çıkarıldı (#198) |

### 3.2 Onaylı gereksinimlerdeki değişiklik talepleri

| Talep | Değişiklik | İK onayı | Kabulde |
|---|---|---|---|
| #185 | Üyelik deneme ve kod gönderim sınırları 10 | 10.10.2026 | T-03 uygun |
| #190 | 2FA kullanıcı tercihine bağlı; İK'nın kurtarma için kapatması | 10.10.2026 | T-04 uygun |

### 3.3 Bilgi İşlem provası (11–12.10.2026)

Doğuş Uçanok, kabul senaryolarını oturumdan önce yerel ortamda denedi. Bulgular oturumdan
önce kapatıldı:

| Bulgu | Düzeltme |
|---|---|
| KS-05: iptal 3. değil 4. yanlış denemede | Uygulama onaylı gereksinime (REQ-KMLK-017) uygundu; plan metni düzeltildi (#196) |
| KS-05: kodun ikinci kez kullanımı ekrandan denenemiyor | Kanıt gösterimine taşındı, KG-09 (#196) |
| KS-07: T3'te video ekranı yok | Kanıt gösterimine taşındı, KG-10 (#196) |
| Parametre ekranında T5'e ait PRM-HSP-03 | Ekrandan çıkarıldı (#198, `v0.2.0-rc.5`) |

---

## 4. Kalan riskler ve ertelenenler

| Konu | Durum |
|---|---|
| AN-01: Cep telefonu olmayan kişide üç bilginin doğruluğu kanallardan dolaylı anlaşılabilir (`KR-078`) | İK'ya gösterildi; **uygun** bulundu |
| REQ-KMLK-054: Video oynarken oturumun açık kalması | Kodda var, otomatik testle gösterildi (KG-10). Ekranda deneme **Eğitim Yönetimi** kabulünde |
| Kilitli hesabı İK'nın açması (PRM-HSP-03) | **T5 Kullanıcı Yönetimi** kapsamında (23.09.2026 toplantısı S-03) |
| R-27: Kanallarını kaybeden kullanıcı | İK kurtarma işlemiyle azaltıldı (SYG-KMLK-081); izleniyor |
| R-25: UAT'de gerçek personel verisi | Üst Yönetim tarafından kabul edildi (`KR-103`) |

---

## 5. İzlenebilirlik

İzlenebilirlik matrisinin (§5) 56 satırının tamamında "Kabul" sütunu dolduruldu. T3
kapsama ölçümü (matris §6): kaynaktan kabule **56 / 56 kalem eksiksiz → %100**.

---

## 6. Öneriler ve sonraki adımlar

1. **Sürüm ve baseline:** Kabul edilen commit (`149f6ee`) `v0.2.0` olarak etiketlenir
   (`KR-097`). UAT'de aynı commit çalıştığı için yeniden kurulum gerekmez.
2. **Modül sonu işleri:** 33061 süreç denetimi, durum raporu (`KR-101`), bilgi kayıt defteri
   gözden geçirmesi ve konfigürasyon denetimi.
3. **UAT izin listesi:** Kabul katılımcıları listeden çıkarılmaz; sonraki modüllerin
   kabulünde de katılımcı olabilirler (`KR-106`, BK-22).
4. **Katılımcı seçimi (öneri):** Sonraki modüllerin kabulünde personel katılımcılar farklı
   birimlerden seçilir.
5. **İmzalar:** İmza gereken belgelerin tamamı onaylı kalite belgeleriyle birlikte
   imzalatılır; asıl kanıt imzalı nüsha olur. Saklama yeri o zaman belirlenir (BK-24, Ö-3).

---

## 7. 33061 eşlemesi

| TEC.11 çıktısı (YAKLAŞIM §7) | Kanıt |
|---|---|
| d) Sistem geçerlenir | 13.10.2026 kabul oturumu |
| f) Sonuçlar ve anomaliler belirlenir | Kabul formu §2–§5; kabulde `[HATA]` kaydı açılmadı |
| g) Nesnel kanıt | Kabul formu (asıl nüsha BK-24), bu rapor, CI kanıt özeti |
| h) İzlenebilirlik | İzlenebilirlik matrisi §5 "Kabul" sütunu, §6 ölçümü |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-13 | 1.0 | İlk sürüm (#204) | Bilgi İşlem |
