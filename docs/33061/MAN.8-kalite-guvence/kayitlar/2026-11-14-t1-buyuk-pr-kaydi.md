# T1 Dönemi Büyük PR'lar — Geriye Dönük Kayıt

**Belge kimliği:** MAN.8-KYT-2026-11-14
**Süreç:** MAN.8 — Kalite Güvence
**Tarih:** 2026-11-14
**Kaynak:** T1 geliştirme sonu denetimi, bulgular MAN.8-12, MAN.8-13, MAN.8-14 (`raporlar/2026-11-12-t1-gelistirme-sonu-denetimi.md`); düzeltici faaliyet #390

---

## 1. Amaç

CI'daki boyut denetimi (#216), şablonun kendi DoD satırı "boyut gerekçesi" ifadesini taşıdığı için hiçbir PR'da düşmedi. CONTRIBUTING §5'teki inceleme notu kuralı ise hiç denetlenmedi. Bu kayıt, iki kuralın etkisiz kaldığı dönemde 1.500 satırı aşan PR'ları gösterir. Sayım, #390'da düzeltilen kuralla (`.github/scripts/pr-rules.cjs`) geriye dönük yapıldı.

## 2. Büyük PR'lar (13.10–13.11.2026)

Satır: üretilen dosyalar hariç eklenen + silinen satır.

| PR | Birleştirme | Satır | Boyut gerekçesi (kendi paragrafı) | Onayda `İncelenen:` satırı |
|---|---|---:|---|---|
| #220 | 2026-10-13 | 2.108 | yok | yok |
| #277 | 2026-10-23 | 1.591 | yok | yok |
| #281 | 2026-10-23 | 1.651 | yok | yok |
| #304 | 2026-11-01 | 1.712 | yok | yok |
| #314 | 2026-11-02 | 1.648 | var | yok |
| #328 | 2026-11-04 | 1.857 | var | yok |
| #331 | 2026-11-04 | 2.541 | var | yok |
| #364 | 2026-11-07 | 1.713 | yok | yok |
| #396 | 2026-11-13 | 1.610 | var | yok |

- **Dokuz PR'ın dördünde gerekçe yok.** Eski denetim hepsini geçirdi. Denetim raporu yedi PR saymıştı; dönem sınırındaki #220 ile denetimden sonra birleşen #396 bu tabloya eklendi.
- **Hiçbirinde inceleme notu yok.** Onayların hepsi birleştirmeden önce yazıldı (K5 89/89). Eksik olan onayın kendisi değil, büyük PR'da neyin incelendiğinin kaydıdır.

## 3. Şablona uygunluk (T1 dönemi, 91 PR: #205–#396)

#390'da eklenen şablon denetimi, T1 dönemindeki birleşmiş PR'ların gövdelerine uygulandı:

| Sonuç | PR sayısı | Not |
|---|---:|---|
| Uygun | 5 | #378, #380, #382, #384, #396 |
| "Neden?" veya "Notlar" başlığı yok | 62 | #205–#349 arası |
| DoD maddesi silinmiş veya kısaltılmış (başlıklar tam) | 24 | #240–#263 arasındaki 12 PR'da 12–20 madde silinmiş; #351–#376 arasındaki 12 PR'da madde metni kısaltılmış (ör. çok satırlı izlenebilirlik maddesi tek satıra indirilmiş) |

Uygunsuz 86 PR'ın hepsi CI'dan ve incelemeden geçti; CI şablon yapısını denetlemiyordu.

## 4. Karar ve önlem

- **Geriye dönük düzeltme yapılmaz.** Birleşmiş PR gövdeleri değiştirilmez: gövde, birleştirme anındaki kanıttır. Bu kayıt, eksikliği ve kapsamını gösterir.
- **Önlem (#390):**
  - Boyut gerekçesi kendi paragrafı olmalı; şablon metni gerekçe sayılmaz.
  - PR gövdesi şablonla karşılaştırılır: başlıklar, 26 maddenin tam metni, gerekçeler, izlenebilirlik tablosu.
  - Büyük PR'ın onayında `İncelenen:` satırı `main`'de denetlenir (`KR-140`).
  - Kuralların hepsi şablonun boş hâliyle de sınanır.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-11-14 | 1.0 | İlk kayıt (#390) | Bilgi İşlem |
