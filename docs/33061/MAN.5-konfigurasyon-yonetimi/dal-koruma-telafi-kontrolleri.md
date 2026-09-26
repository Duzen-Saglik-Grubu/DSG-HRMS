# Dal Koruma — Kısıt ve Telafi Edici Kontroller

**Belge kimliği:** MAN.5-DK
**Süreç:** MAN.5 — Konfigürasyon Yönetimi
**Son güncelleme:** 2026-09-09
**İlgili risk:** `R-16`
**İlgili kararlar:** `KR-054`, `KR-055`

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

Üç katmanlı bir düzen uygulanmaktadır. `KR-055` ile bu düzen **kalıcı çözüm** olarak
kabul edilmiştir (bkz. §4.1).

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

`.github/workflows/branch-protection-check.yml`, `main` dalına gelen **her commit'i**
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

## 4. Kalıcı çözüm — değerlendirme ve karar

| Seçenek | Maliyet | Sonuç | Değerlendirme |
|---|---|---|---|
| A — GitHub Team planı | Kullanıcı başına aylık ~4 USD (1–2 kullanıcı) | Dal koruma + kural kümesi + zorunlu durum kontrolleri açılır | Değerlendirildi, **tercih edilmedi** |
| B — Depoyu herkese açık yapmak | Ücretsiz | Kurum içi belgeler dışarı açılır | ❌ Kabul edilemez |
| C — Kendi barındırılan Git (GitLab/Gitea) | Sunucu + bakım | Tam kontrol | Mevcut kuruluma göre orantısız |
| **D — Mevcut telafi kontrolleriyle devam** | Ücretsiz | Tespit edici kontrol; önleyici kontrol zayıf | ✅ **Seçilen** (`KR-055`) |

### 4.1 Alınan karar (2026-09-09)

> **Seçenek D — mevcut telafi kontrolleriyle devam edilecektir.**
>
> Kurum, şirket projesi olması nedeniyle deponun **özel kalmasına** ve
> `Duzen-Saglik-Grubu` organizasyonunun **Free planında devam etmesine** karar
> vermiştir. **GitHub Team planına geçilmeyecektir** (`KR-055`).
>
> Risk `R-16`, uygulanan telafi edici kontrollerle birlikte **kabul edilmiştir**
> (risk kabulü). Denetimde bu belge ve CI denetim kayıtları gösterilecektir.

Bu karar sonucunda:

- §3'teki üç katmanlı düzen **kalıcı çözümdür**, geçici değildir.
- Kontrollerin fiilen çalıştığının kanıtı düzenli olarak toplanacaktır:
  - Her `main` gönderiminde çalışan **CI denetim kaydı** (GitHub Actions geçmişi),
  - Denetimin ürettiği **düzeltici faaliyet issue'ları** (varsa),
  - PR geçmişi — her değişikliğin inceleme ve onaydan geçtiği.
- Konfigürasyon denetimlerinde (MAN.5.BP5) bu kayıtlar örneklenerek kontrol
  edilecektir.

> **Denetçiye anlatılacak özet:** *"Sunucu tarafı dal koruma, plan kısıtı nedeniyle
> kullanılamamaktadır. Bunun yerine istemci tarafı önleyici kancalar ve sunucu
> tarafı, atlatılamaz bir tespit edici denetim uygulanmaktadır. Denetim, kural dışı
> her commit'i yakalar ve otomatik düzeltici faaliyet kaydı üretir. Kısıt, riski ve
> telafisi ile birlikte kayıt altındadır."*

### 4.2 İleride Team planına geçilirse

Karar değişirse §3.1–3.2 kancaları kaldırılmaz; sunucu tarafı korumanın
**tamamlayıcısı** olarak kalır. Uygulanacak kurallar:

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

Kısıt kalıcı olarak kabul edildiği için, ilgili özniteliklerde **`F` (Tam) derecesi
hedeflenmemektedir**:

| Öznitelik | Telafi kontrolleriyle (mevcut) | Team planıyla (tercih edilmedi) |
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
| 1 | GitHub Team planı kararının alınması | Üst Yönetim / Bilgi İşlem | ✅ **Kapandı** — Free planda kalınacak (`KR-055`) |
| 2 | `R-16` riskinin kabul edilmesi ve kayda geçirilmesi | Bilgi İşlem | ✅ Kapandı |
| 3 | Telafi kontrollerinin çalıştığına dair kanıtların dönemsel örneklenmesi (MAN.5.BP5) | Bilgi İşlem | 🔄 Sürekli |
| 4 | `core.hooksPath` kurulumunun her çalışma kopyasında yapılması | Bilgi İşlem | ✅ Kurulum kılavuzunda |

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-08 | 0.1 | Kısıtın tespiti ve telafi edici kontrollerin tanımlanması | Bilgi İşlem |
| 2026-09-09 | 0.2 | Kalıcı çözüm kararı işlendi: Free planda kalınacak, telafi kontrolleri kalıcıdır (`KR-055`); `R-16` kabul edildi | Bilgi İşlem |
