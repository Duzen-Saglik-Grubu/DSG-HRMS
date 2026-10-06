# Proje Planı

**Belge kimliği:** MAN.1-PP
**Süreç:** MAN.1 — Proje Planlama
**Sürüm:** 0.3 (revizyon — üst yönetim onayı bekliyor)
**Son güncelleme:** 2026-10-06
**Plan sahibi:** Bilgi İşlem Birim Sorumlusu
**33061 karşılığı:** *Project objectives*, *Project constraints*, *Project plan*,
*Project infrastructure needs*, *Project human resources needs*

> **Onay durumu:** Bu plan, üst yönetim tarafından onaylanmadan yürürlüğe girmez
> (33061 MAN.1.BP3 — *"Obtain approval for the project"*). Onay sonrası değişiklikler
> plan revizyonu olarak `kayitlar/` altında kayda geçirilir.

---

## 1. Proje hedefleri

Ayrıntılı iş hedefleri ve başarı ölçütleri: `docs/mimari/vizyon-ve-kapsam.md` §4

| # | Hedef | Ölçüt |
|---|---|---|
| H1 | Veri kaybı olmadan geçiş | Sıfır açıklanamayan fark |
| H2 | Personelin sistemi benimsemesi | 2 ay içinde aktif personelin ≥ %90'ı hesap açmış |
| H3 | İK'nın manuel yükünün azalması | İzin taleplerinin ≥ %95'i personel tarafından girilmiş |
| H4 | Süreç şeffaflığı | Talep→sonuç süresi ölçülebilir |
| H5 | Mevzuat uyumu | KVKK erişim ve işlem kayıtları eksiksiz |
| H6 | Süreç olgunluğu | TS ISO/IEC TS 33061 **Seviye 2** |
| H7 | Sürdürülebilirlik | Kapsam eşikleri sağlanıyor, kararlar yazılı |

---

## 2. Kapsam özeti

| | |
|---|---|
| **Modül sayısı** | **35** (5 Temel + 10 Yatay + 20 İş) |
| **Kapsam belgesi** | `docs/mimari/vizyon-ve-kapsam.md` |
| **Modül listesi ve bağımlılıklar** | `docs/mimari/modul-listesi-ve-bagimliliklar.md` |
| **Veri göçü kapsamı** | İzin, eğitim, sertifika (`KR-046`) |
| **Kapsam dışı** | Bordro, PDKS, mobil uygulama, LOGO'ya yazma |

---

## 3. Hayat döngüsü modeli

Proje, **modül bazlı artımlı** bir hayat döngüsü kullanır. Her modül aynı çevrimi
tamamlar ve çevrim tamamlanmadan sonraki modüle geçilmez.

```
┌─────────────────────────────────────────────────────────────────┐
│  MODÜL ÇEVRİMİ                                                  │
│                                                                 │
│  1. Gereksinim toplantısı (İK + P4)          → TEC.2            │
│  2. Paydaş gereksinimleri yazılır ve kilitlenir → TEC.2         │
│  3. Sistem gereksinimleri (SYG-...) türetilir → TEC.3           │
│  4. Tasarım ve gerekirse ADR                  → TEC.5           │
│  5. Geliştirme (dal → PR → inceleme)          → TEC.7           │
│  6. Entegrasyon ve CI kapıları                → TEC.8           │
│  7. Doğrulama: testler, kapsam, güvenlik      → TEC.9           │
│  8. Kabul adayı etiketi (-rc.N) → UAT'ye dağıtım → MAN.5/TEC.10 │
│  9. Kabul testi (İK + temsilci personel)      → TEC.11          │
│ 10. Kabul formu imzası → kabul etiketi (vX.Y.0) → TEC.11 / MAN.5│
│ 11. Kapanış gözden geçirmesi + alınan dersler → MAN.2           │
│ 12. Öz değerlendirme tablosu güncellenir      → MAN.8           │
└─────────────────────────────────────────────────────────────────┘
```

**Kritik kural:** Bir modül, 11 ve 12. adımlar tamamlanmadan "bitti" sayılmaz.
Kabul formu ve kapanış gözden geçirmesi, 33061 Seviye 2'nin **PA 2.1 (c)** maddesinin
kanıtıdır — "izlendi ve gerektiğinde düzeltildi".

### 3.1 Karar noktaları (decision gate)

| Kapı | Ne zaman | Kim karar verir | Geçme kriteri |
|---|---|---|---|
| **G1 — Gereksinim kilidi** | Modül başlangıcı | İK | Gereksinimler yazılı ve kabul kriterleri tanımlı |
| **G2 — Geliştirme tamam** | UAT öncesi | Bilgi İşlem | Tüm CI kalite kapıları geçildi (ADR-0011 §6) |
| **G3 — Kabul** | UAT sonrası | İK | Kabul kriterleri karşılandı, kabul formu imzalandı |
| **G4 — Kapanış** | Modül sonu | Bilgi İşlem | Kapanış gözden geçirmesi yapıldı, dersler kaydedildi |

---

## 4. Planlama ilkeleri

| # | İlke | Karar |
|---|---|---|
| P1 | **Temel modüller önce**, sırayla; önceliklendirmeye tabi değil | `KR-040` |
| P2 | **Yatay modüllere öncelik**; altyapı iş modüllerinden önce | `KR-053` |
| P3 | **Zincirli modüllerde ilk halkadan başla** | `KR-052` |
| P4 | Modül sırası İK toplantılarında belirlenir; **bir modül önden bilinmez** | Kurum işleyişi |
| P5 | Ön koşulu tamamlanmamış modül seçilmez — **seçim rehberi** kullanılır | `modul-listesi-ve-bagimliliklar.md` §5 |
| P6 | İK onayı beklenirken **enine kesen işler** yapılır; boş zaman oluşmaz | `R-15` önlemi |

### 4.1 İK onayı beklerken yapılacak işler

Modül sırasının önceden bilinmemesi (P4) bir bekleme riski doğurur. Bu aralıklarda
yapılacak, hiçbir modüle bağımlı olmayan işler:

- Test altyapısı ve kalite kapılarının geliştirilmesi
- Veri göçü araçlarının hazırlanması ve kuru koşu denemeleri
- Dokümantasyon ve 33061 kanıtlarının tamamlanması
- Güvenlik ve performans testleri
- Bağımlılık güncellemeleri ve teknik borç azaltma
- Bir sonraki muhtemel modüller için ön veri profillemesi
- Ortak arayüz bileşenlerinin geliştirilmesi (ADR-0015 §5)

---

## 5. Aşama planı

Takvim **tarih değil sıra** üzerinden planlanmıştır. Gerekçe: modül önceliklendirmesi
İK toplantılarına bağlıdır ve tek geliştirme kanalı vardır (`KS8`); sabit tarih vermek
gerçekçi olmaz ve MAN.2 izlemesini anlamsızlaştırır.

| Aşama | İçerik | Çıktı (baseline) |
|---|---|---|
| **A0 — Hazırlık** | Doküman altyapısı, ADR'ler, kapsam ✅ *(tamamlanıyor)* | Onaylı vizyon-kapsam ve plan |
| **A1 — İskelet** | Solution yapısı, CI/CD, Docker, GitHub kurulumu, kalite kapıları | `v0.1.0` |
| **A2 — Temel** | T1 Personel · T2 Organizasyon · T3 Kimlik ★ · T4 Rol/Yetki · T5 Kullanıcı | Modül başına bir sürüm (`KR-097`): T3 → `v0.2.0` — **ilk kullanıcı teslimi**; kalanlar kabul sırasıyla `v0.3.0` … |
| **A3 — Yatay altyapı** | Y1–Y5, Y8, Y10 (bildirim, denetim, referans veri, sistem, iş akışı, raporlama, çalışma takvimi) | Modül başına bir sürüm |
| **A4+ — İş modülleri** | İK önceliklendirmesine göre, modül başına bir sürüm | Modül başına bir sürüm |
| **AS — Geçiş** | Veri göçü denemeleri, kesim, devreye alma | `v1.0.0` |

> **A2 sonunda personel sisteme girebilir hâle gelir** ancak henüz iş yapamaz. Bu, İK'ya
> baştan anlatılmalıdır: ilk teslim bir "vitrin" değil, üzerine her şeyin kurulacağı
> temeldir.
>
> **A3 kısa görünür ama projenin en değerli aşamasıdır.** `KR-053` gereği buraya yatırım
> yapılır; 20 iş modülü bu altyapıyı kullanacaktır.

### 5.1 Sürümleme

**Semantic Versioning** (`MAJOR.MINOR.PATCH`):

| Artış | Ne zaman |
|---|---|
| `PATCH` | Hata düzeltmesi |
| `MINOR` | Yeni modül veya yeni yetenek |
| `MAJOR` | `v1.0.0` = üretime geçiş; sonrasında kırıcı değişiklik |

Her kabul edilen modül bir **MINOR sürüm** ve bir **baseline** üretir (MAN.5, `KR-097`).
Kabule sunulan sürüm `vX.Y.0-rc.N` ön sürüm etiketini alır ve UAT'ye bu etiketten kurulur;
kabulden sonra aynı commit `vX.Y.0` olarak etiketlenir. Numaralar kabul sırasına göre verilir.

### 5.2 Gerçekleşen sıra

| Aşama / modül | Durum | Sürüm |
|---|---|---|
| A0 — Hazırlık | ✅ Tamamlandı | — |
| A1 — İskelet | ✅ Tamamlandı (12.09.2026) | `v0.1.0` |
| A2 — T3 Kimlik | Geliştirme tamam (G2, 04.10.2026); İK kabulü bekliyor | `v0.2.0-rc.2` |
| A2 — T1, T2, T4, T5 | İK gereksinim toplantısı bekliyor (`KR-068`) | kabul sırasıyla `v0.3.0` … |

**T3 önce geliştirildi.** Diğer bütün modüller giriş ve yetkilendirmeye bağımlı olduğu için
ilk modül Kullanıcı Girişi'dir (`KR-040`). T3'ün ihtiyaç duyduğu personel çekirdeği
(kişi ve istihdam modeli, LOGO senkronizasyonu) T3 kapsamında kuruldu; T1'in ekranları ve
işlevleri T1'in kendi gereksinim toplantısından sonra gelir (`KR-077`).

---

## 6. Kaynaklar

### 6.1 İnsan kaynağı

| Rol | Kişi | Ayrılan zaman |
|---|---|---|
| Proje sorumlusu / Geliştirici | Bilgi İşlem Birim Sorumlusu | Ana kaynak |
| İş sahibi / Gereksinim kaynağı | İK Birimi | Modül başına toplantı + UAT |
| Danışılan | Birim sorumluları (P4), KVKK Sorumlusu (P6), Mali İşler (P7), İSG Uzmanı (P11) | Modül bazında |
| Onay mercii | Üst Yönetim (P5) | Plan onayı + dönemsel rapor |

> **`R-05` ve `KS8`:** Tek geliştirme kanalı, projenin en büyük yapısal kısıtıdır.
> Önlemi ADR'ler, test kapsamı ve güncel dokümantasyondur — kaynak artırımı bu planın
> yetkisi dışındadır ancak üst yönetime raporlanan bir konudur.

### 6.2 Ortamlar

| Ortam | Amaç | Veri | Durum |
|---|---|---|---|
| **Geliştirme** | Günlük geliştirme | Gerçek LOGO verisi (salt okunur); otomatik testlerde sentetik | Yerel |
| **UAT** | İK kabul testleri (`KR-024`) | **Gerçek personel verisi**, maskelenmez; ileti gönderimi izin listesiyle sınırlı (`KR-083`, R-25) | ✅ Kuruldu (16.09.2026; TLS 17.09.2026) |
| **Üretim** | Canlı kullanım | Gerçek | **Kurulacak** (Linux) |

### 6.3 Altyapı ihtiyaçları

| # | İhtiyaç | Durum | Sorumlu |
|---|---|---|---|
| B1 | LOGO salt-okunur erişim | ✅ Sağlandı | Bilgi İşlem |
| B2 | Yeni PostgreSQL sunucusu (`KR-035`) | ⏳ Üretim için bekliyor; UAT'de konteyner içinde PostgreSQL 17 | Bilgi İşlem |
| B3 | UAT ortamı (ayrı Compose yığını + veritabanı) | ✅ Sağlandı (16.09.2026) | Bilgi İşlem |
| B4 | Üretim sunucusu (Linux + Docker) | ⏳ Bekliyor | Bilgi İşlem |
| B5 | NAS erişimi ve yedekleme düzeni | ⏳ Teyit bekliyor | Bilgi İşlem |
| B6 | NetGSM API kullanıcısı | ✅ Sağlandı | Bilgi İşlem |
| B7 | SMTP erişimi | ✅ Sağlandı | Bilgi İşlem |
| B8 | GitHub deposu, Actions, Projects | ✅ Sağlandı. Sunucu tarafı dal koruma Free planda yok; telafi kontrolleri (`KR-055`, `KR-096`) | Bilgi İşlem |
| B9 | Veritabanı yedekleme ve geri yükleme düzeni | ⏳ Üretim için bekliyor; UAT düzenli yedeklenmez (`KR-098`) | Bilgi İşlem |

### 6.4 Araçlar ve lisanslar

Tüm geliştirme araçları ve kütüphaneler **ücretsiz ve izin verici lisanslıdır**
(ADR-0001). Ücretli tek kalem, satın alınmış olan **TSE standard dokümanlarıdır**
(TS ISO/IEC TS 33061, TS ISO/IEC 33020).

---

## 7. Kısıtlar

Ayrıntı: `docs/mimari/vizyon-ve-kapsam.md` §10

| # | Kısıt |
|---|---|
| KS1 | LOGO'ya yazma yapılamaz |
| KS2 | Üretim ortamı Linux |
| KS3 | Yalnızca ücretsiz, koşulsuz lisanslı paketler |
| KS4 | Uygulama yalnızca yerel ağda |
| KS5 | Active Directory / Entra ID yok |
| KS6 | TS ISO/IEC TS 33061 Seviye 2 çerçevesi |
| KS7 | Modül gereksinimleri sırayla toplanır; bir modül kabul edilmeden sonraki başlamaz |
| KS8 | Tek geliştirme kanalı |
| KS9 | NetGSM'de OTP paketi yok |

---

## 8. İzleme ve kontrol

MAN.2 kapsamında yürütülür.

| Faaliyet | Sıklık | Çıktı |
|---|---|---|
| Görev durumu izleme | Sürekli | GitHub Projects panosu |
| Risk gözden geçirme | Her modül kapanışında, en geç ayda bir | `MAN.4/risk-kayit-defteri.md` |
| Durum raporu | Her modül kapanışında, en geç ayda bir (`KR-101`) | `MAN.2/raporlar/YYYY-AA-GG-durum-raporu.md` |
| Modül kapanış gözden geçirmesi | Her modül sonunda | `MAN.2/kayitlar/` |
| Öz değerlendirme (33061) | Her modül kapanışında, en geç 3 ayda bir | `33061/00-OLGUNLUK-SEVIYESI-KRITERLERI.md` §6 |
| Kalite kapısı sonuçları | Her PR | GitHub Actions |

### 8.1 İzlenecek ölçütler

| Ölçüt | Neden |
|---|---|
| Modül çevrim süresi (gereksinim → kabul) | Kalan modüller için tahmin üretir (`R-15`) |
| Açık / kapanan risk sayısı | Risk yönetiminin işlediğinin göstergesi |
| Test kapsamı yüzdesi | Kalite eşiklerinin sürdürüldüğü |
| Kabul testinde çıkan bulgu sayısı | Gereksinim kalitesinin göstergesi |
| Değişiklik talebi sayısı | Kapsam kayması göstergesi (`R-04`) |

> Modül çevrim süresi, ilk 3–4 modülden sonra anlamlı bir tahmin tabanı verecektir.
> O noktada kalan modüller için gerçekçi bir süre öngörüsü üst yönetime sunulacaktır.

---

## 9. İletişim planı

| Kim | Ne | Sıklık | Kanal |
|---|---|---|---|
| İK Birimi | Gereksinim toplantısı | Modül başına | Yüz yüze |
| İK Birimi + P4 + temsilci personel | Kabul testi | Modül başına | UAT ortamı |
| Üst Yönetim | Durum raporu | Her modül kapanışında, en geç ayda bir (`KR-101`) | Yazılı rapor |
| KVKK Sorumlusu | Uyum konuları | İhtiyaç hâlinde | Yazılı görüş |
| Tüm personel | Devreye alma bilgilendirmesi | Geçişte | E-posta + kılavuz |

---

## 10. Plan varsayımları

| # | Varsayım | Yanlışsa |
|---|---|---|
| PV1 | İK, modül başına gereksinim toplantısı ve UAT için zaman ayırabilecek | Modül çevrimi uzar; `R-15` büyür |
| PV2 | Altyapı ihtiyaçları (B2–B4, B9) zamanında sağlanacak | A1/A2 aşamaları gecikir |
| PV3 | Geliştirme kaynağı kesintisiz kalacak | Proje durur (`R-05`) |
| PV4 | Kapsam, onaylandıktan sonra büyük ölçüde sabit kalacak | `R-04` gerçekleşir |

---

## 11. Plandan doğan açık işler

| # | İş | Sorumlu |
|---|---|---|
| 1 | Planın üst yönetim tarafından onaylanması (MAN.1.BP3) | Üst Yönetim |
| 2 | B2 — Yeni PostgreSQL sunucusunun sağlanması | Bilgi İşlem |
| 3 | ~~B3 — UAT ortamı~~ (✅ 16.09.2026); B4 — üretim ortamının kurulması | Bilgi İşlem |
| 4 | B5 — NAS yedekleme düzeninin teyidi | Bilgi İşlem |
| 5 | ~~B8 — GitHub kurulumu~~ (✅; dal koruma yerine telafi kontrolleri, `KR-055`) | Bilgi İşlem |
| 6 | B9 — Yedekleme ve geri yükleme düzeninin tanımlanması (RPO/RTO) | Bilgi İşlem |
| 7 | Başarı ölçütlerinin (§1) üst yönetimle teyidi | Bilgi İşlem |

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-07 | 0.1 | İlk taslak | Bilgi İşlem |
| 2026-10-04 | 0.2 | §3 aşama tablosu: modül başına sürüm (`KR-097`, #125) | Bilgi İşlem |
| 2026-10-06 | 0.3 | **Plan revizyonu (#127):** başlıktaki sürüm (0.1 kalmıştı) geçmişle eşitlendi; §3 çevrim adımları SYG ve kabul adayı etiketi; §5.2 gerçekleşen sıra ve T3'ün öne alınması (`KR-040`, `KR-077`); §6.2 ortamlar (UAT kuruldu, gerçek veri); §6.3 B2, B3, B8, B9; §8–§9 durum raporu sıklığı (`KR-101`); §11 durumlar. Üst Yönetim onayına sunuldu | Bilgi İşlem |
