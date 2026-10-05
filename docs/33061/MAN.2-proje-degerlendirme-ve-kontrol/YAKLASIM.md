# MAN.2 — Süreç Yaklaşımı

**Belge kimliği:** MAN.2-YAK
**Süreç:** MAN.2 — Proje Değerlendirme ve Kontrol
**Son güncelleme:** 2026-10-05
**Karşıladığı öznitelik maddeleri:** `PA 2.1`: hedefler, izleme sıklığı, sorumluluklar, kaynaklar ve arayüzler. `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir. `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir.

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Bu belge bugünkü uygulamayı
> anlatır; henüz uygulanmayan adımları ayrıca işaretler (§8).

---

## 1. Sürecin amacı ve sınırı

Proje değerlendirme ve kontrol, projenin plana göre nerede olduğunu belirler, sapmaları
inceler, gerektiğinde düzeltici faaliyet veya yeniden planlama başlatır ve paydaşları
bilgilendirir (TS ISO/IEC TS 33061, MAN.2).

**Kapsamdadır:** ilerlemenin izlenmesi, ölçütler, kapı ve geçiş kararları, sapma ve
düzeltici faaliyet, durum raporu, kapanış gözden geçirmesi ve alınan dersler.

**Kapsamda değildir:** planın kendisi (MAN.1), risk kaydı (MAN.4), süreç uygunluk
denetimi (MAN.8). MAN.8 denetimleri bu sürece girdi verir.

---

## 2. Süreç bugün nasıl işletiliyor?

Bugün proje kontrolü dört araçla yapılıyor. Dönemsel durum raporu henüz yok (§2.5).

### 2.1 Sürekli izleme: GitHub

- **Milestone'lar** aşama ilerlemesini ölçer (WBS §5). Örnek, 05.10.2026: `A2 — Temel Modüller` 86 kapalı / 9 açık.
- **Issue'lar** işi taşır. Her issue `tur:`, `surec:`, `oncelik:` etiketi ve milestone alır (CONTRIBUTING §5, §10).
- **Projects panosu** işin durumunu gösterir: `Backlog` → `In Progress` → `Review` → `Done`.
- **CI** her PR'da kalite kapılarını çalıştırır. `main` kırmızıya düşerse `[HATA]` issue'su kendiliğinden açılır (CONTRIBUTING §1.1, #129).

### 2.2 Kapılar ve geçiş yetkisi

Bir sonraki adıma geçiş, `proje-plani.md` §3.1'deki kapılarla yetkilendirilir:

| Kapı | Karar veren | Kayıt | Örnek |
|---|---|---|---|
| G1 — Gereksinim kilidi | İK | Onaylı `PG-<MODÜL>.md` (`KR-068`) | T3: İK toplantısı 23.09.2026, `PG-KMLK.md` |
| G2 — Geliştirme tamam | Bilgi İşlem | Doğrulama raporu (TEC.9) | T3: GEÇTİ, 04.10.2026 (`TEC.9-dogrulama/raporlar/2026-10-04-t3-dogrulama-raporu.md` §5) |
| G3 — Kabul | İK | İmzalı kabul formu (TEC.11) | T3: `v0.2.0-rc.2` kabul bekliyor |
| G4 — Kapanış | Bilgi İşlem | Kapanış gözden geçirmesi (§2.4) | Henüz kapanan modül yok |

### 2.3 Aşama kapanışı ve modül sonu denetimi

- **Aşama kapanışı:** Her aşama bir kapanış değerlendirmesiyle biter. Ne üretildiği, neyin
  kanıtlandığı ve neyin devredildiği yazılır. Örnek: `MAN.1-proje-planlama/A1-asama-kapanis-degerlendirmesi.md` (MAN-004, 12.09.2026).
- **Modül sonu süreç denetimi (MAN.8):** Bugün projenin en güçlü kontrol aracıdır. T3 sonu
  denetimi (`MAN.8-kalite-guvence/raporlar/2026-10-02-t3-surec-denetimi.md`) bulgularını
  düzeltici iş planına çevirdi; plan maddeleri issue olarak açıldı (#125–#136).

### 2.4 Sapma, düzeltici faaliyet ve yeniden planlama

| Durum | Ne yapılır | Kayıt |
|---|---|---|
| Ürün hatası | Kök nedeniyle issue; regresyon testiyle kapanır | `tur:hata` (TEC.13) |
| Süreç sapması | Düzeltici faaliyet; neden, önlem, doğrulama | `tur:duzeltici-faaliyet` (örn. #128, #129) |
| Kapsam değişikliği | Değişiklik talebi; İK kabul eder (RACI §3.2) | `tur:degisiklik-talebi` (örn. #77) |
| Sıra veya kapsam sapması | Karar yazılır, plana yansıtılır (MAN.1 §2.6) | `KR-NNN` + plan revizyonu |
| Risk niteliğinde olay | Olay kaydı ve risk defteri (MAN.4) | `TEC.13-bakim/kayitlar/`, `R-NN` |

Modül kapanışında (G4) **kapanış gözden geçirmesi** yapılır: planla gerçekleşenin
karşılaştırması, sapmalar, ölçütler ve alınan dersler. Kayıt `kayitlar/` altına yazılır.

### 2.5 Durum raporu

`proje-plani.md` §8 ve §9, Üst Yönetime **dönemsel** yazılı durum raporu öngörür ama
dönemi tanımlamaz. Bu belge dönemi şöyle tanımlar:

- **Her modül kapanışında** ve **en geç ayda bir.** Bu, risk gözden geçirmesinin sıklığıyla aynıdır (`risk-kayit-defteri.md`); iki iş birlikte yapılır.

Raporun içeriği:

1. Aşama ve modül ilerlemesi (milestone sayıları, geçilen kapılar).
2. §3'teki ölçütlerin gerçek değerleri.
3. Puanı 6 ve üzeri riskler; kabul bekleyen riskler (risk defteri §1, MAN.4).
4. Plandan sapmalar, alınan kararlar, yeniden planlama ihtiyacı.
5. Rol ve kaynak yeterliliği (tek geliştirme kanalı, `R-05`; bekleyen altyapı B2, B4, B5, B9).
6. Üst Yönetimden beklenen onaylar.

Rapor Üst Yönetime iletilir; iletim tarihi ve alıcı rapora yazılır. **İlk rapor #127'dir.**

---

## 3. İzlenen ölçütler

Ölçütler `proje-plani.md` §8.1'den gelir. Hedef değerleri henüz tanımlı değildir.

| Ölçüt | Veri kaynağı |
|---|---|
| Modül çevrim süresi (gereksinim toplantısı → kabul) | Toplantı kaydı tarihi, kabul formu tarihi |
| Açık / kapanan risk sayısı | `risk-kayit-defteri.md` §3 |
| Test kapsamı yüzdesi | CI kanıt özeti (`TEC.9-dogrulama/kayitlar/`) |
| Kabul testinde çıkan bulgu sayısı | Kabul formu, TEC.11 kayıtları |
| Değişiklik talebi sayısı | `tur:degisiklik-talebi` etiketli issue'lar |

---

## 4. Roller

| Rol | Görev |
|---|---|
| Bilgi İşlem (Doğuş Uçanok) | İzler, raporu yazar, G2 ve G4 kararlarını verir (RACI: A/R). Değişiklikleri onaylar (`KR-096`) |
| Geliştirme yardımcısı (Claude) | Ölçütleri derler, taslak hazırlar. Onay vermez |
| İK Birimi | G1 ve G3 kararları; değişiklik talebinin kabulü. Danışılır (C) |
| Üst Yönetim | Durum raporunu alır (I); kapsam, kaynak ve risk kabulü kararları |

---

## 5. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Durum raporu | `raporlar/YYYY-AA-durum-raporu.md` | §2.5'teki altı başlık; iletim kaydı | Bilgi İşlem |
| Modül kapanış gözden geçirmesi | `kayitlar/YYYY-AA-GG-<modül>-kapanis-gozden-gecirmesi.md` | Plan–gerçekleşen, sapmalar, ölçütler, G4 kararı | Bilgi İşlem |
| Alınan dersler | `kayitlar/YYYY-AA-GG-<modül>-alinan-dersler.md` | Ne oldu, neden, ne değişecek | Bilgi İşlem |
| Aşama kapanış değerlendirmesi | `MAN.1-proje-planlama/A<n>-asama-kapanis-degerlendirmesi.md` | Üretilen, kanıtlanan, devredilen | Bilgi İşlem |
| İlerleme | GitHub milestone, issue, Projects panosu | Etiket ve milestone zorunlu | PR/issue sahibi |
| Sapma ve düzeltici faaliyet | GitHub issue (`tur:hata`, `tur:duzeltici-faaliyet`, `tur:degisiklik-talebi`) | Şablonlu issue | Bulan |

---

## 6. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** Raporlar ve kayıtlar PR ile depoya girer; inceleme onayı birleştirmeden önce yazılır (`KR-096`).
- **Değişmezlik:** Gönderilmiş bir durum raporu sonradan değiştirilmez. Düzeltme gerekirse bir sonraki raporda belirtilir.
- **Tarih ve sahip:** Her rapor ve kayıtta tarih, hazırlayan ve değişiklik geçmişi bulunur.
- **Kişisel veri:** Raporlar sayı ve karar taşır; kişi adı veya kişisel veri içermez.
- **GitHub verisinin kalıcılığı:** Milestone sayıları ve ölçüt değerleri rapora yazılarak dondurulur; panonun anlık görüntüsüne güvenilmez.

---

## 7. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (MAN.2) | Karşılığı |
|---|---|
| a) Performans ölçütleri veya değerlendirme sonuçları mevcut | §3, durum raporu |
| b) Rol ve sorumlulukların yeterliliği değerlendirilir | Durum raporu başlık 5 |
| c) Kaynakların yeterliliği değerlendirilir | Durum raporu başlık 5 |
| d) Teknik ilerleme gözden geçirilir | §2.3, kapanış gözden geçirmesi |
| e) Sapmalar incelenir ve analiz edilir | §2.4 |
| f) Paydaşlar bilgilendirilir | §2.5 durum raporu |
| g) Düzeltici faaliyet tanımlanır ve yönlendirilir | §2.4, `tur:duzeltici-faaliyet` |
| h) Gerektiğinde yeniden planlama başlatılır | §2.4, MAN.1 §2.6 |
| i) Bir sonraki kilometre taşına geçiş yetkilendirilir | §2.2, G1–G4 |
| j) Proje hedeflerine ulaşılır | Kabul formları, durum raporu |

---

## 8. Açık noktalar

| # | Açık nokta | İzlendiği yer |
|---|---|---|
| 1 | Hiç durum raporu yazılmadı. 18.09.2026'da "A2 başlarken" diye söz verilmişti; A2 24.09'da başladı | #127 |
| 2 | §3'teki beş ölçütün hiçbiri henüz ölçülmedi; hedef değerleri yok | #127 |
| 3 | Kapanmış modül olmadığı için kapanış gözden geçirmesi ve alınan dersler kaydı yok; `kayitlar/` ve `raporlar/` boş | T3 kapanışı |
| 4 | A1 kapanış değerlendirmesi "üst yönetim bilgisine sunulacak" durumunda kaldı; iletim kaydı yok | #127 |
| 5 | `KR-077` fiilî bir yeniden planlamadır; plana etkisi analiz edilmedi | #127 (MAN.1) |
| 6 | Ölçütleri GitHub'dan otomatik derleyen bir araç yok; değerler elle toplanacak | — |
| 7 | Eskalasyon eşiği tanımlı değil. Bugünkü tek yazılı kural: puanı 6 ve üzeri risk Üst Yönetime gider (MAN.4) | — |

---

## 9. Gözden geçirme

Bu belge her modül sonu süreç denetiminde (MAN.8) ve ilk durum raporundan (#127) sonra
gözden geçirilir. Rapor dönemi pratikte işlemiyorsa burada değiştirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
