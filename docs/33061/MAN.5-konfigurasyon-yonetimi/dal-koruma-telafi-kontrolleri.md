# Dal Koruma — Kısıt ve Telafi Edici Kontroller

**Belge kimliği:** MAN.5-DK
**Süreç:** MAN.5 — Konfigürasyon Yönetimi
**Son güncelleme:** 2026-09-08
**İlgili risk:** `R-16`
**İlgili kararlar:** `KR-054`

---

## 1. Tespit edilen kısıt

GitHub kurulumu sırasında, `main` dalı için sunucu tarafı koruma kurallarının
etkinleştirilemediği tespit edilmiştir.

| Denenen yöntem | Sonuç |
|---|---|
| Klasik dal koruma (*branch protection*) | ❌ `403 — Upgrade to GitHub Pro or make this repository public` |
| Depo kural kümesi (*repository ruleset*) | ❌ Aynı hata |

**Neden:** Depo **özeldir** (private) ve `Duzen-Saglik-Grubu` organizasyonu
**Free** planındadır. GitHub, özel depolarda dal koruma ve kural kümesi
özelliklerini ücretli planlara ayırmıştır.

**Depoyu herkese açık yapmak bir seçenek değildir**; içerik kuruma özel süreç ve
analiz belgeleridir.

---

## 2. Bunun neden önemli olduğu

Aşağıdaki belgelerde "dal koruma kuralları" bir kanıt olarak gösterilmektedir:

| Belge | Nerede | Ne için |
|---|---|---|
| `00-OLGUNLUK-SEVIYESI-KRITERLERI.md` | PA 2.2 (c) | Bilginin kontrol edilmesi |
| `roller-ve-sorumluluklar.md` | PA 2.1 (d) | Yetkinin duyurulması |
| `00-DOKUMAN-HARITASI.md` | MAN.5 | Konfigürasyon yönetim sistemi |
| `CONTRIBUTING.md` | §1 | `main`'e doğrudan yazılmaması |

TS ISO/IEC 33020 açısından bakıldığında:

> **PA 2.2 (c)** — *dokümante edilmiş bilgi uygun şekilde tanımlanır ve
> **gereksinimlere göre kontrol edilir***
> **PA 2.2 (d)** — *bilgi, planlanan düzenlemelere göre **gözden geçirilir ve
> onaylanır***

Standart, kontrolün **hangi araçla** sağlanacağını dayatmaz; kontrolün *var
olduğunun ve işlediğinin* kanıtlanmasını ister. Ancak yalnızca kurala dayanan,
teknik olarak zorlanmayan bir kontrol **"kayda değer zayıflık"** sayılabilir ve
ilgili öznitelikte `F` (Tam) yerine `L` (Büyük Ölçüde) derecesine yol açar.

---

## 3. Uygulanan telafi edici kontroller

Kalıcı çözüm sağlanana kadar üç katmanlı bir düzen uygulanmaktadır.

### 3.1 Önleyici (istemci tarafı) — `pre-push` kancası

`.githooks/pre-push`, `main` dalına doğrudan gönderimi engeller.

```bash
git config core.hooksPath .githooks     # tek seferlik kurulum
```

| Özellik | Değer |
|---|---|
| Türü | Önleyici |
| Kapsamı | Kancayı kurmuş yerel çalışma kopyası |
| **Zayıflığı** | `--no-verify` ile atlanabilir; kurulmayan makinede çalışmaz |

### 3.2 Önleyici (istemci tarafı) — `commit-msg` kancası

`.githooks/commit-msg`, commit mesajlarının Conventional Commits biçimine
uymasını denetler (`CONTRIBUTING.md` §4). Aynı zayıflıklar geçerlidir.

### 3.3 Tespit edici (sunucu tarafı) — CI denetimi

`.github/workflows/dal-koruma-denetimi.yml`, `main` dalına gelen **her commit'i**
denetler:

1. Commit'in birleştirilmiş bir Pull Request ile gelip gelmediği kontrol edilir.
2. Gelmemişse:
   - İş akışı **başarısız** olur (görünür uyarı),
   - Otomatik olarak bir **düzeltici faaliyet issue'su** açılır
     (`tur:duzeltici-faaliyet`, `surec:MAN.5`, `oncelik:yuksek`),
   - Commit'in kimliği, yazarı, mesajı ve tarihi kayda geçer.

| Özellik | Değer |
|---|---|
| Türü | **Tespit edici** |
| Kapsamı | Sunucu tarafı — atlatılamaz |
| **Zayıflığı** | Olayı **önlemez**, gerçekleştikten sonra yakalar |

> Bu kontrol, MAN.8.BP5 (*"olay ve problemleri ele al"*) açısından da kanıt
> üretir: uygunsuzluk gizlenmez, otomatik olarak kayıt altına alınır ve kapatılır.

### 3.4 Süreç kontrolü

- `CONTRIBUTING.md` §1 kuralı yazılıdır.
- `CODEOWNERS` tanımlıdır (koruma olmadan **tavsiye niteliğindedir**).
- Her PR, Tamamlanma Tanımı kontrol listesini doldurur.
- CI kalite kapıları PR üzerinde çalışır — **ancak birleştirme için zorunlu
  kılınamaz**.

---

## 4. Kalıcı çözüm önerisi

| Seçenek | Maliyet | Sonuç | Değerlendirme |
|---|---|---|---|
| **A — GitHub Team planı** | Kullanıcı başına aylık ~4 USD (1–2 kullanıcı) | Dal koruma + kural kümesi + zorunlu durum kontrolleri açılır | ✅ **Önerilen** |
| B — Depoyu herkese açık yapmak | Ücretsiz | Kurum içi belgeler dışarı açılır | ❌ Kabul edilemez |
| C — Kendi barındırılan Git (GitLab/Gitea) | Sunucu + bakım | Tam kontrol | Mevcut kuruluma göre orantısız |
| D — Mevcut telafi kontrolleriyle devam | Ücretsiz | Tespit edici kontrol; önleyici kontrol zayıf | Kısa vadede kabul edilebilir |

**Öneri:** **Seçenek A.** Aylık ~4–8 USD, belgelendirme hedefinin ve
konfigürasyon bütünlüğünün yanında ihmal edilebilir bir maliyettir. Team planına
geçildiğinde §3.1–3.2 kancaları kaldırılmaz; sunucu tarafı korumanın
**tamamlayıcısı** olarak kalırlar.

Team planına geçildiğinde uygulanacak kurallar:

| Kural | Değer |
|---|---|
| Doğrudan gönderim | Engelli |
| Zorunlu onay sayısı | 1 |
| Kod sahibi incelemesi | Zorunlu |
| Eskiyen onayların düşürülmesi | Açık |
| Zorunlu durum kontrolleri | Tüm CI kalite kapıları |
| Zorunlu konuşma çözümü | Açık |
| Doğrusal geçmiş | Zorunlu (squash merge) |
| Zorla gönderim / dal silme | Engelli |

---

## 5. Öz değerlendirmeye etkisi

Bu kısıt giderilene kadar, ilgili özniteliklerde **`F` (Tam) derecesi
hedeflenmemektedir**:

| Öznitelik | Telafi kontrolleriyle | Team planıyla |
|---|---|---|
| PA 2.2 (c) Bilginin kontrolü | `L` — sistematik yaklaşım var, zayıflık mevcut | `F` erişilebilir |
| PA 2.2 (d) Gözden geçirme ve onay | `L` | `F` erişilebilir |
| MAN.5 PA 1.1 | Etkilenmez — süreç çıktıları üretiliyor | — |

Seviye 2 için PA 2.2'de **`L` yeterlidir** (33020 Madde 5.6, Tablo 1). Dolayısıyla
mevcut durum **Seviye 2 hedefini engellememektedir**; ancak zayıflık kayıtlıdır ve
denetimde sorulduğunda bu belge gösterilecektir.

> Zayıflığın kayıtlı ve gerekçeli olması, kayıtsız olmasından iyidir. Denetçi
> "bunu biliyor musunuz ve ne yapıyorsunuz" sorusunun cevabını burada bulur.

---

## 6. Açık işler

| # | İş | Sorumlu | Durum |
|---|---|---|---|
| 1 | GitHub Team planı kararının alınması | Üst Yönetim / Bilgi İşlem | ⏳ Açık |
| 2 | Geçiş sonrası §4 tablosundaki kuralların uygulanması | Bilgi İşlem | ⏳ Bekliyor |
| 3 | Bu belgenin ve `R-16`'nın güncellenmesi | Bilgi İşlem | ⏳ Bekliyor |
| 4 | `core.hooksPath` kurulumunun her çalışma kopyasında yapılması | Bilgi İşlem | ✅ Kurulum kılavuzunda |

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-08 | 0.1 | Kısıtın tespiti ve telafi edici kontrollerin tanımlanması | Bilgi İşlem |
