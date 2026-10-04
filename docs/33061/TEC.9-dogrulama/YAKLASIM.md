# TEC.9 — Süreç Yaklaşımı

**Belge kimliği:** TEC.9-YAK
**Süreç:** TEC.9 — Doğrulama
**Son güncelleme:** 2026-10-04 (1.1)
**Karşıladığı öznitelik maddeleri:** `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir. `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir.

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. Test stratejisinin teknik
> kararları ADR-0011'dedir; bu belge onları süreç adımlarına bağlar.

---

## 1. Sürecin amacı ve sınırı

Doğrulama, sistemin **belirtilen gereksinimlerini karşıladığına dair nesnel kanıt**
üretir ve bulunan hataları (anomali) çözülene kadar izler (TS ISO/IEC TS 33061, TEC.9).

| Doğrulama (bu süreç) | Geçerleme (TEC.11) |
|---|---|
| "Ürünü doğru mu yaptık?" | "Doğru ürünü mü yaptık?" |
| Ölçüt: sistem gereksinimleri (`SYG-`) | Ölçüt: paydaş gereksinimleri ve kabul kriterleri (`REQ-`) |
| Bilgi İşlem, CI | İK birimi |
| Kanıt: test, ölçüm, inceleme kayıtları; doğrulama raporu | Kanıt: imzalı kabul formu |

İkisi birbirinin yerine geçmez (ADR-0011 §8).

---

## 2. Süreç nasıl işletilir?

### 2.1 Her PR'da (sürekli doğrulama)

1. Geliştirici, değiştirdiği gereksinimin testini **aynı PR'da** yazar. Testte gereksinim
   kimliği anılır (sınıf özeti veya test adı, örn. `(SYG-KMLK-060)`).
2. CI, ADR-0011 §6'daki kalite kapılarını çalıştırır. Kapılar uyarı değil engeldir.
3. CI ayrıca izlenebilirliği denetler (`requirement-traceability-check.mjs`):
   - her paydaş gereksinimi bir sistem gereksinimine bağlıdır;
   - **her sistem gereksinimi en az bir testte anılır.** Analiz, İnceleme veya Gösterim yöntemli gereksinimler için TEC.9 raporu da kanıt sayılır.
4. Hata bulunursa kök nedeniyle birlikte `[HATA]` issue'su açılır ve bir regresyon testiyle kapatılır (TEC.13). Testin eski kodda düştüğü gösterilir.
5. PR açıklamasının "Nasıl doğrulandı?" bölümü test sayılarını ve yeni testleri yazar.

### 2.2 Modül sonunda

1. **Analiz yöntemli gereksinimler** UAT'de ölçülür ve bir ölçüm raporu yazılır.
2. **Doğrulama raporu** yazılır (`raporlar/<tarih>-<modül>-dogrulama-raporu.md`). Raporda şunlar yer alır:
   - her SYG için yöntem, kanıt ve sonuç;
   - zorunlu test türlerinin durumu;
   - kapsam;
   - açık noktalar.
3. **G2 kapı kaydı** raporun içinde tutulur. Bilgi İşlem kararını tarihle yazar. Geçmeden UAT kabulüne (TEC.11) geçilmez.
4. Ardından modül sonu 33061 süreç denetimi yapılır (MAN.8).

---

## 3. Doğrulama yöntemleri, koşulları ve uygunluk ölçütleri (`TEC.9.BP1`)

Her sistem gereksiniminin yöntemi `SYG-<MODÜL>.md`'deki "Doğrulama" sütunundadır. Yöntem, gereksinim yazılırken seçilir (TEC.3).

| Yöntem | Ne zaman seçilir | Prosedür | Uygunluk ölçütü | Kayıt |
|---|---|---|---|---|
| **Test** | Davranış otomatik olarak sınanabiliyorsa (varsayılan) | Birim, entegrasyon veya bileşen testi. Gerçek PostgreSQL kullanılır, In-Memory sağlayıcı kullanılmaz (ADR-0011 §3). | Testler CI'da geçer; test gereksinimi anar | CI çalışması, PR |
| **Analiz** | Performans, kapasite, istatistiksel özellikler | UAT'de ölçüm. Yöntem, senaryo, ham sonuç ve hedefle karşılaştırma yazılır. Sentetik veri kullanılır; gerçek hesaplara dokunulmaz. Yan etkiler kayda geçer. | Ölçülen değer hedefi karşılar | `raporlar/` altında ölçüm raporu |
| **İnceleme** | Belge, metin veya kapsam kuralları | Ölçüte karşı satır satır gözden geçirme. Mümkünse sonuç bir testle sabitlenir (örn. SYG-KMLK-006). | Bulgu yok ya da bulgular kapatıldı | İnceleme kaydı veya doğrulama raporu |
| **Gösterim** | Kullanıcının göreceği davranış (ekran düzeni, akış) | Çalışan sistemde gösterim. Testle desteklenir; nihai gösterim kabulde (TEC.11) yapılır. | Gösterim beklenen davranışı sergiler | Kabul kaydı |

**Gerçek tarayıcıda doğrulama:** Kritik kullanıcı akışları uçtan uca testlerle (Playwright, `src/frontend/dsg-hrms-web/e2e/`) her PR'da sınanır. Testler ayrı ve atılabilir bir yığında çalışır (`docker/compose.e2e.yml`): sentetik veri, e-postalar Mailpit'te kalır. Sayfa düzeni gibi testle sabitlenmemiş ölçümler (örn. yatay kaydırma) Edge başsız kipinde yapılır ve doğrulama raporuna yazılır.

---

## 4. Destekleyici sistemler (`TEC.9.BP1.5–6`)

| Sistem | Amaç | Durum |
|---|---|---|
| GitHub Actions | CI kalite kapıları | Var |
| Testcontainers (PostgreSQL 17) | Gerçek veritabanıyla entegrasyon testi | Var |
| SQL Server Express (CI) | LOGO salt okunur erişim kanıtı | Var |
| Mailpit | Gerçek SMTP konuşması | Var |
| UAT ortamı | Analiz yöntemli ölçümler | Var (runbook: TEC.10) |
| Edge başsız + CDP | Gerçek tarayıcı ölçümleri | Var (betik geliştirici makinesinde) |
| Playwright + uçtan uca yığın | Uçtan uca test (ADR-0011 §1): 7 kimlik senaryosu | Var (#146); yerelde `bash docker/e2e/up.sh` ve `npm run e2e` |

---

## 5. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Test kodu | `src/backend/tests/`, `src/frontend/**/__tests__/` | Gereksinim kimliğini anar | PR sahibi |
| CI çalışma kaydı | GitHub Actions | Otomatik; 90 gün saklanır | CI |
| Ölçüm raporu | `raporlar/<tarih>-<modül>-performans-olcumu.md` | Yöntem, ham sonuç, hedef, yan etkiler | Bilgi İşlem |
| Doğrulama raporu ve G2 kaydı | `raporlar/<tarih>-<modül>-dogrulama-raporu.md` | §2.2 | Bilgi İşlem |
| Hata kaydı | GitHub issue (`[HATA]`, `surec:TEC.13`) | Belirti, kök neden, düzeltme, regresyon testi | Bulan |
| İzlenebilirlik | `../izlenebilirlik-matrisi.md` "Doğrulama" sütunu | Test sınıfı adları | PR sahibi |

---

## 6. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** Raporlar ve bu belge yalnızca PR ile değişir. Her belgenin bir değişiklik geçmişi vardır.
- **Raporların değişmezliği:** Bir rapordaki sonuç sonradan değişirse (örn. yeniden ölçüm) rapor silinmez. Yeni sürüm eklenir ve eski sonuç tabloda korunur.
- **Kalıcı özet:** CI kayıtları 90 gün sonra silindiği için doğrulama raporu, CI çalışma numarasını ve sayıları kalıcı olarak yazar.
- **Kişisel veri:** Ölçüm ve test kayıtlarında gerçek kişisel veri bulunmaz (ADR-0011 §5).

---

## 7. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (TEC.9) | Karşılığı |
|---|---|
| a) Doğrulama kısıtları belirlenir | §3 (yöntem seçimi), SYG "Doğrulama" sütunu |
| b) Destekleyici sistemler mevcut | §4 |
| c) Sistem doğrulanır | §2, CI |
| d) Düzeltici faaliyet bilgisi raporlanır | `[HATA]` issue'ları, doğrulama raporu "Açık noktalar" |
| e) Gereksinimleri karşıladığına dair nesnel kanıt | Doğrulama ve ölçüm raporları |
| f) Sonuçlar ve anomaliler belirlenir | Doğrulama raporu, hata kayıtları |
| g) Doğrulanan öğelerin izlenebilirliği | İzlenebilirlik matrisi, CI izlenebilirlik denetimi |

---

## 8. Gözden geçirme

Bu belge her modül sonu süreç denetiminde (MAN.8) gözden geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-04 | 1.0 | İlk oluşturma (#123) | Bilgi İşlem |
| 2026-10-04 | 1.1 | Uçtan uca testler kuruldu (#146): §3 ve §4 | Bilgi İşlem |
