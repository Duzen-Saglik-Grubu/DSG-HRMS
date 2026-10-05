# MAN.8 — Süreç Yaklaşımı

**Belge kimliği:** MAN.8-YAK
**Süreç:** MAN.8 — Kalite Güvencesi
**Son güncelleme:** 2026-10-05
**Karşıladığı öznitelik maddeleri:** `PA 2.1`: amaç, planlama, sorumluluk, kaynak ve arayüzler (§1–§4). `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir (§5). `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir (§6).

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Kalite kapılarının teknik
> kararları ADR-0011 §6'dadır; bu belge onları süreç adımlarına bağlar.

---

## 1. Sürecin amacı ve sınırı

Kalite güvencesi, ürünün ve süreçlerin tanımlı kurallara uyduğuna dair güven üretir. Bulunan uygunsuzluklar kapanana kadar izlenir (TS ISO/IEC TS 33061, MAN.8).

| Değerlendirilen | Nasıl | Sıklık |
|---|---|---|
| **Ürün** (kod, test, belge) | CI kalite kapıları + inceleme onayı + Tamamlanma Tanımı | Her PR |
| **Kural uyumu** (dal, PR, onay, `main` durumu) | Otomatik tespit edici denetimler | `main`'e gelen her commit |
| **Süreç** (33061 süreçleri) | Standarda göre süreç denetimi; öz değerlendirme tablosu | Her modül sonunda |

**Sınır:** Gereksinime karşı ürün doğrulaması TEC.9'dadır. MAN.8 doğrulamanın **yapıldığını** izler, kendisi yapmaz.

**Hedef (`PA 2.1 a`):**
- Her PR'da kalite kapıları geçer ve onay yorumu bulunur.
- Her modül sonunda süreç denetimi yapılır.
- Her bulgu bir issue'ya dönüşür.

---

## 2. Süreç nasıl işletilir?

### 2.1 Her PR'da (ürün değerlendirmesi)

1. PR sahibi, PR şablonundaki (`.github/PULL_REQUEST_TEMPLATE.md`) **Tamamlanma Tanımı** listesini doldurur (`CONTRIBUTING.md` §6). Uygulanmayan madde işaretlenmez; gerekçesi PR'ın "Notlar" bölümüne yazılır.
2. CI, ADR-0011 §6'daki kapıları çalıştırır. Kapılar uyarı değil engeldir:
   - derleme, test ve kapsam;
   - ön yüz;
   - migration;
   - açıklı paket;
   - `trivy`, `gitleaks`;
   - OpenAPI;
   - uçtan uca testler;
   - PR izlenebilirliği (başlık, dal, milestone, issue) ve gereksinim izlenebilirliği.

   Bir kapı geçici olarak kapatılırsa gerekçe PR'a yazılır ve `tur:duzeltici-faaliyet` etiketli issue açılır.
3. İnceleyen, §7 kontrol listesine göre inceler. Birleştirmeden önce PR'a "İnceledim ve onaylıyorum." yorumunu yazar (`KR-096`). **Onay yorumunu yalnızca inceleyen yazar**; geliştirme yardımcısı (Claude) yazmaz.

### 2.2 `main`'e gelen her commit'te (kural uyumu)

| Denetim | Neyi yakalar | Sonuç |
|---|---|---|
| `branch-protection-check.yml` | PR'sız commit; birleştirmeden önce onay yorumu olmayan PR; denetimin kendisinin çökmesi | `[DÜZELTİCİ]` issue'su |
| `main-failure-check.yml` | `main`'de CI veya API sözleşme denetiminin başarısız olması | `[HATA] main kırmızı` issue'su; kırmızıyken özellik birleştirilmez |

### 2.3 Modül sonunda (süreç değerlendirmesi)

Kural: **Her modül tamamlandığında 33061 süreçleri standarda göre denetlenir** (Doğuş Uçanok, 02.10.2026).

1. Bilgi İşlem 15 süreci TS ISO/IEC TS 33061'in çıktıları ve TS ISO/IEC 33020'nin ölçeğiyle (N/P/L/F) değerlendirir.
2. Yöntem salt okumadır. Her çıktı için kanıt aranır: dosya, issue/PR numarası, CI çalışması veya tarih. "Planlandı" kanıt sayılmaz.
3. Rapor `raporlar/<tarih>-*-surec-denetimi.md` olarak yazılır; ayrıntılı bulgular eklere konur. Standart metni kopyalanmaz; çıktılar harf ve kısa özetle anılır (`R-11`).
4. Öz değerlendirme tablosu (`00-OLGUNLUK-SEVIYESI-KRITERLERI.md` §6) güncellenir. Bu adım proje planındaki modül çevriminin 12. adımıdır.
5. **Her bulgu bir issue olur.** Bulgular yalnızca "öneri" olarak bırakılmaz. 18.09 raporunun önerileri issue'ya çevrilmediği için T3'te unutuldu; bu yüzden kural bu şekilde konmuştur.

### 2.4 Uygunsuzluk ve düzeltici faaliyet

1. Her uygunsuzluk `[DÜZELTİCİ]` başlıklı, `tur:duzeltici-faaliyet` ve ilgili `surec:` etiketli bir issue olur. Bu issue'yu ya CI ya da bulan kişi açar.
2. Issue'ya şunlar yazılır:
   - ne oldu;
   - kök neden ve "neden şimdiye kadar yakalanmadı?" sorusunun cevabı;
   - tekrarı önleyen önlem (mümkünse otomatik denetim).
3. Düzeltme bir PR ile yapılır ve issue'yu kapatır (dal türü `duzeltici`, `CONTRIBUTING.md` §3). Örnekler: #17, #44, #66, #67, #128, #129.
4. Aynı kök neden tekrarlanırsa bu durum eğilim olarak süreç denetimine taşınır (örn. #72).

---

## 3. Roller ve bağımsızlık (`PA 2.1 d`)

RACI'de MAN.8'in sorumlusu ve hesap vereni Bilgi İşlem'dir. İK danışılan, Üst Yönetim bilgilendirilen taraftır.

| Rol | Görev |
|---|---|
| Bilgi İşlem | Süreç denetimi, öz değerlendirme, düzeltici faaliyetlerin takibi |
| İnceleyen (Doğuş Uçanok) | PR incelemesi ve onay yorumu |
| CI | Kalite kapıları ve tespit edici denetimler |
| İK | Kabulde ürünü geliştiriciden bağımsız bir gözle sınar (TEC.11) |

**Bağımsızlık zayıflığı (`R-23`):** Geliştiren, inceleyen, birleştiren ve süreci denetleyen aynı birimdir. Süreç denetimi bu yüzden bağımsız bir değerlendirme değil, bir öz değerlendirmedir; dereceler teşhis içindir. Telafi olarak şunlar vardır:
- onay yorumu ve onun CI denetimi;
- otomatik kapılar;
- modül sonu denetimi;
- İK kabulü.

---

## 4. Kaynaklar ve arayüzler (`PA 2.1 e, f`)

| Kaynak / arayüz | Kullanım |
|---|---|
| GitHub Actions | Kalite kapıları ve tespit edici denetimler |
| Lisanslı standartlar (depo dışında) | Süreç denetiminin ölçütü |
| TEC.9 | Doğrulama raporu ve G2 kaydı; denetimde kanıt olarak okunur |
| MAN.4 | Denetimde ortaya çıkan riskler risk defterine yazılır |
| MAN.5 | Telafi denetimlerinin açtığı issue'lar |
| TEC.13 | Olay kayıtları ve `[HATA]` issue'ları |

---

## 5. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Kalite ölçütleri | `CONTRIBUTING.md` §6 (Tamamlanma Tanımı), §7 (inceleme listesi); ADR-0011 §6 | Kontrol listesi, kapı tablosu | Bilgi İşlem |
| Seviye ölçütleri | `00-OLGUNLUK-SEVIYESI-KRITERLERI.md` | PA tanımları, ölçek, öz değerlendirme tablosu (§6) | Bilgi İşlem |
| PR değerlendirmesi | PR gövdesi, CI çalışması, onay yorumu | Şablon; "Nasıl doğrulandı?" bölümü | PR sahibi, CI, inceleyen |
| Süreç denetim raporu | `raporlar/<tarih>-*.md` (`2026-09-18-surec-gozden-gecirme-raporu.md`, `2026-10-02-t3-surec-denetimi.md` + ekler A–C) | Çıktı başına kanıt ve derece; bulgular; düzeltici iş planı | Bilgi İşlem |
| Onay kaydı (geriye dönük) | `kayitlar/2026-10-04-pr-onay-kaydi.md` | 65 PR: onay tarihi, arşiv dosyası | Bilgi İşlem |
| Düzeltici faaliyet | GitHub issue (`[DÜZELTİCİ]`, `tur:duzeltici-faaliyet`) | Olay, kök neden, önlem | CI veya bulan |

---

## 6. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** Raporlar, kayıtlar ve bu belge yalnızca PR ile değişir. Her birinin değişiklik geçmişi vardır.
- **Raporların değişmezliği:** Denetim raporu sonradan düzeltilmez. Bulgunun durumu bir sonraki denetimde "bulguların durumu" tablosunda izlenir (örn. 02.10 raporu §5).
- **Kanıt kaynağı:** Raporda her iddia bir dosyaya, issue/PR numarasına veya CI çalışmasına bağlanır. Kritik iddialar rapor yazılmadan önce ayrıca doğrulanır.
- **Depo dışı kanıt:** Onay kaydı, depo dışındaki yazışma arşivinden yalnızca PR numarası, tarih ve dosya numarası alarak oluşturulur. Arşivin kendisi depoya girmez (MAN.6, BK-25).

---

## 7. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (MAN.8) | Karşılığı |
|---|---|
| a) Kalite güvence yordamları tanımlanır ve uygulanır | §2.1, §2.2; `CONTRIBUTING.md` §5–§7 |
| b) Değerlendirme ölçüt ve yöntemleri tanımlanır | ADR-0011 §6; `00-OLGUNLUK` §2–§6; §2.3 |
| c) Ürün, hizmet ve süreç değerlendirmeleri yapılır | CI, onay yorumu; modül sonu süreç denetimi |
| d) Sonuçlar ilgili paydaşlara iletilir | PR gövdeleri; denetim raporları; öz değerlendirme tablosu |
| e) Olaylar çözülür | `[HATA]` issue'ları (TEC.13), `main` kırmızı kaydı |
| f) Önceliklendirilmiş problemler ele alınır | `[DÜZELTİCİ]` issue'ları; denetim raporundaki düzeltici iş planı (§2.4) |

---

## 8. Açık noktalar

| # | Konu | Durum |
|---|---|---|
| 1 | İnceleme bağımsız değil (`R-23`); onay yorumu kanıt üretir ama ikinci bir göz sağlamaz | Riskle izleniyor |
| 2 | "Dönemsel kalite raporu" hiç üretilmedi. PR inceleme oranı, bulgu kapanma süresi gibi kalite ölçütleri izlenmiyor | Tanımlanmalı |
| 3 | `CONTRIBUTING.md` §5'teki "< 400 satır" PR boyutu kuralı T3'te uygulanmadı; kural ne değiştirildi ne ölçülüyor | Açık |
| 4 | Yazılı olup otomatik denetlenmeyen kuralların envanteri (#72) | Issue açık |
| 5 | Doküman haritası MAN.8 için var olmayan `tamamlanma-tanimi.md` ve `kod-gozden-gecirme-kontrol-listesi.md` dosyalarını gösteriyor; uygulamada bunlar `CONTRIBUTING.md` §6 ve §7'dir. Haritadaki etiket adları da (`duzeltici-faaliyet`, `hata`) gerçek etiketlerle (`tur:` ön ekli) uyuşmuyor | Harita düzeltilmeli (MAN.6) |
| 6 | İki Tamamlanma Tanımı hâlâ aynı değil: `CONTRIBUTING.md` §6'daki "onay PR'a yorum olarak yazıldı" maddesi PR şablonunda yok. PR'dan PR'a madde düşmesini yakalayan bir denetim de yok | Açık |
| 7 | Süreç denetimi "modül sonunda" yapılıyor. Modül uzun sürerse ara denetim için bir tetikleyici tanımlı değil; plan yalnızca "en geç 3 ayda bir" diyor | Açık |

---

## 9. Gözden geçirme

Bu belge her modül sonu süreç denetiminde gözden geçirilir. Denetim bu belgenin kendisini de kapsar.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
