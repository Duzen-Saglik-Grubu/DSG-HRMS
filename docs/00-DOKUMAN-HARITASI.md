# Doküman Haritası — Ne Nerede?

**Proje:** DSG-HRMS — Düzen Sağlık Grubu İnsan Kaynakları Yönetim Sistemi
**Amaç:** Bu doküman, TS ISO/IEC TS 33061 Seviye 2 kapsamında üretilen tüm kanıtların nerede tutulduğunu tek sayfadan gösterir. Bir denetçi ya da yeni katılan bir ekip üyesi aradığı kaydı buradan bulur.

**Son güncelleme:** 2026-09-18
**Doküman sahibi:** Bilgi İşlem Birim Sorumlusu

---

## 1. Klasör düzeni

```
DSG-HRMS/
├── docs/
│   ├── 00-DOKUMAN-HARITASI.md      ← bu doküman
│   ├── 33061/                      ← süreç kodu bazlı kanıt klasörleri
│   │   ├── izlenebilirlik-matrisi.md
│   │   └── <SÜREÇ-KODU>-<ad>/
│   │       ├── YAKLASIM.md         ← sürecin nasıl işletildiği (Approach)
│   │       ├── <kalıcı ürünler>    ← plan, kayıt defteri, matris vb.
│   │       ├── kayitlar/           ← tarihli kayıtlar (Record)
│   │       └── raporlar/           ← tarihli raporlar (Report)
│   ├── karar-kayit-defteri.md      ← proje kararlarının resmî kaydı (KR-NNN)
│   ├── adr/                        ← mimari karar kayıtları (ADR)
│   │   ├── README.md               ← ADR dizini, kurallar, etkileşim haritası
│   │   └── ADR-NNNN-<baslik>.md
│   ├── mimari/                     ← mimari ve tasarım dokümanları
│   ├── analiz/                     ← mevcut sistem ve veri analizleri
│   └── sablonlar/                  ← doküman şablonları (ADR şablonu vb.)
├── src/                            ← kaynak kod (backend + frontend)
└── src_old/                        ← mevcut HRMS kaynak arşivi (depo dışı, bkz. KR-031)
```

**Kural:** Her kanıt yalnızca **tek bir yerde** tutulur. Başka bir süreç aynı kanıta ihtiyaç duyuyorsa kopyalamaz, bu haritadan bağlantı verir. (Bkz. MAN.6 Bilgi Yönetimi)

---

## 2. Süreç → Klasör → Kanıt haritası

Aşağıdaki tablolarda "33061 Süreç Çıktısı" sütunu, standardın Madde 5'te her süreç için saydığı *process output* kalemlerinin Türkçe karşılığıdır. "Bizdeki karşılığı" sütunu ise o kalemin bu projede hangi somut dosya veya araçla üretildiğini gösterir.

### 2.1 Teknik Süreçler (TEC)

#### TEC.2 — Paydaş İhtiyaç ve Gereksinimlerinin Tanımlanması
📁 `docs/33061/TEC.2-paydas-ihtiyac-ve-gereksinimleri/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım (Approach) | `YAKLASIM.md` |
| Paydaş tanımlama (Stakeholder identification) | `paydas-listesi.md` |
| Hayat döngüsü kavramları (Life cycle concepts) | `docs/mimari/vizyon-ve-kapsam.md` |
| Paydaş gereksinimleri (Stakeholder requirements) | `paydas-gereksinimleri/PG-<MODÜL>.md` |
| Geçerleme kriterleri (Validation criteria) | Her paydaş gereksiniminin "Kabul Kriteri" bölümü |
| İzlenebilirlik eşlemesi (Traceability mapping) | `docs/33061/izlenebilirlik-matrisi.md` |
| Kayıt (Record) | `kayitlar/YYYY-AA-GG-ik-gereksinim-toplantisi.md` |

> **Kaynak:** Gereksinimler, İK birimi ile yapılan modül toplantılarında toplanır. Her toplantı bir kayıt dosyası üretir ve ilgili paydaş gereksinimlerine referans verir.

#### TEC.3 — Sistem/Yazılım Gereksinimlerinin Tanımlanması
📁 `docs/33061/TEC.3-sistem-yazilim-gereksinimleri/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` |
| Sistem/yazılım gereksinimleri | `gereksinimler/SG-<MODÜL>.md` (her madde `REQ-<MODÜL>-<no>` kimlikli) |
| Doğrulama kriterleri (Verification criteria) | Her gereksinimin "Doğrulama Yöntemi" alanı → test senaryosu kimliği |
| Sistem fonksiyon modeli | `docs/mimari/fonksiyon-modeli.md` |
| İzlenebilirlik eşlemesi | `docs/33061/izlenebilirlik-matrisi.md` |
| Kayıt | GitHub Issue (etiket: `gereksinim`) + `kayitlar/` |

#### TEC.5 — Tasarım Tanımlama
📁 `docs/33061/TEC.5-tasarim-tanimlama/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` |
| Tasarım modeli | `docs/mimari/` (C4 diyagramları, veri modeli, modül tasarımları) |
| Tasarım gerekçesi (Design rationale) | `docs/adr/` — Mimari Karar Kayıtları |
| Arayüz tanımı (Interface definition) | OpenAPI sözleşmesi + `docs/mimari/entegrasyon-arayuzleri.md` |
| Tasarım değerlendirme raporu | `raporlar/YYYY-AA-GG-tasarim-gozden-gecirme.md` |
| İzlenebilirlik eşlemesi | `docs/33061/izlenebilirlik-matrisi.md` |

#### TEC.7 — Gerçekleştirme (Implementation)
📁 `docs/33061/TEC.7-gerceklestirme/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (kodlama standartları, dallanma stratejisi, DoD) |
| Sistem öğesi (yazılım) | `src/` — Git deposundaki kaynak kod |
| Öğe tanımı | Kod içi dokümantasyon + `docs/mimari/` |
| Gerçekleştirme raporu | Sürüm notları (`CHANGELOG.md`) + Pull Request özetleri |
| Kayıt | Git commit geçmişi + PR kayıtları (GitHub) |

#### TEC.8 — Entegrasyon
📁 `docs/33061/TEC.8-entegrasyon/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (CI hattı, entegrasyon sırası, Docker Compose yığını) |
| Arayüz tanımı | OpenAPI sözleşmesi; LOGO ve NAS entegrasyon arayüzleri |
| Entegre edilmiş sistem | Docker imajları (sürüm etiketli) |
| Entegrasyon raporu | GitHub Actions çalışma kayıtları + `raporlar/` |

#### TEC.9 — Doğrulama (Verification)
📁 `docs/33061/TEC.9-dogrulama/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (test stratejisi, kapsam eşikleri, kalite kapıları) |
| Doğrulama kriterleri | Test senaryoları (`src/**/tests`) + kod gözden geçirme kontrol listesi |
| Doğrulanmış yazılım | CI'da tüm kapıları geçmiş, etiketlenmiş sürüm |
| Doğrulama raporu | `raporlar/` — test sonuç ve kapsam raporları |
| Kayıt | GitHub Actions çalışma kayıtları, PR inceleme yorumları |

> **Not:** Doğrulama = "ürünü doğru mu yaptık" (test, gözden geçirme). Geçerleme (TEC.11) = "doğru ürünü mü yaptık" (İK kabulü). İkisi ayrı süreçtir ve ayrı kanıt üretir.

#### TEC.10 — Geçiş (Transition)
📁 `docs/33061/TEC.10-gecis/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (kurulum, kesim/cut-over, geri dönüş planı) |
| **Ortam kurulum runbook** | `uat-ortami-kurulum-runbook.md` — UAT sunucusu kurulumu, dağıtım ve doğrulama |
| Kurulmuş sistem | Üretim ortamı dağıtım kaydı |
| Geçiş raporu | `raporlar/` — veri göçü mutabakat raporları, kesim raporu |
| Kayıt | `kayitlar/` — göç denemeleri, kullanıcı eğitim kayıtları |

> Eski HRMS'ten veri göçü bu sürecin kapsamındadır. Mutabakat raporları burada tutulur.

#### TEC.11 — Geçerleme (Validation)
📁 `docs/33061/TEC.11-gecerleme/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (UAT süreci, kabul kriterleri, ortam) |
| Geçerleme kriterleri | Paydaş gereksinimlerinin kabul kriterleri (TEC.2'den gelir) |
| Geçerlenmiş yazılım | İK tarafından kabul edilmiş, etiketlenmiş sürüm |
| Geçerleme raporu | `raporlar/YYYY-AA-GG-<modül>-kabul-raporu.md` |
| Kayıt | `kayitlar/` — İK imzalı kabul formları |

#### TEC.13 — Bakım
📁 `docs/33061/TEC.13-bakim/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (destek modeli, hata sınıflandırma, çözüm süreleri) |
| Bakımı yapılmış sistem | Üretimdeki güncel sürüm |
| Bakım raporu | `raporlar/` — dönemsel bakım ve olay raporları |
| Kayıt | GitHub Issue (etiket: `hata`, `bakim`) |

### 2.2 Teknik Yönetim Süreçleri (MAN)

#### MAN.1 — Proje Planlama
📁 `docs/33061/MAN.1-proje-planlama/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Proje hedefleri ve kısıtları | `proje-plani.md` (Hedefler, Kısıtlar bölümleri) |
| Proje planı | `proje-plani.md` |
| İş kırılım yapısı (WBS) | `is-kirilim-yapisi.md` + GitHub Projects panosu |
| Zaman planı | GitHub Projects + Milestone kayıtları |
| Roller ve sorumluluklar | `roller-ve-sorumluluklar.md` (RACI) |
| Aşama kapanış değerlendirmesi | `A1-asama-kapanis-degerlendirmesi.md` (her aşama için bir belge) |
| Altyapı ve kaynak ihtiyaçları | `proje-plani.md` (Kaynaklar bölümü) |
| Planlama kaydı | `kayitlar/` — plan revizyonları |

#### MAN.2 — Proje Değerlendirme ve Kontrol
📁 `docs/33061/MAN.2-proje-degerlendirme-ve-kontrol/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (izleme sıklığı, ölçütler, eskalasyon) |
| Proje durum raporu | `raporlar/YYYY-AA-durum-raporu.md` (dönemsel) |
| Gözden geçirme sonucu | `kayitlar/` — modül kapanış gözden geçirmeleri |
| Değişiklik talebi | GitHub Issue (etiket: `degisiklik-talebi`) |
| Alınan dersler | `kayitlar/YYYY-AA-GG-<modül>-alinan-dersler.md` |

#### MAN.4 — Risk Yönetimi
📁 `docs/33061/MAN.4-risk-yonetimi/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (risk ölçeği, sahiplik, gözden geçirme sıklığı) |
| Risk kayıt defteri (Risk register) | `risk-kayit-defteri.md` |
| Risk raporu | `raporlar/` — dönemsel risk durumu |

#### MAN.5 — Konfigürasyon Yönetimi
📁 `docs/33061/MAN.5-konfigurasyon-yonetimi/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (dallanma, sürümleme, etiketleme, baseline tanımı) |
| Konfigürasyon yönetim sistemi | Git + GitHub (dal koruma kuralları, PR zorunluluğu) |
| Konfigürasyon öğeleri | `konfigurasyon-ogeleri.md` — neyin kontrol altında olduğu |
| Baseline | Git etiketleri (`v0.1.0`, `v0.2.0` …) |
| Değişiklik talebi | GitHub Issue + Pull Request |
| Sürüm kontrolü | `CHANGELOG.md` + GitHub Releases |
| Konfigürasyon denetim sonucu | `raporlar/YYYY-AA-GG-konfigurasyon-denetimi.md` |

#### MAN.6 — Bilgi Yönetimi
📁 `docs/33061/MAN.6-bilgi-yonetimi/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (bilgi sahipliği, erişim, saklama, yedekleme) |
| Bilgi kayıt defteri (Information register) | `bilgi-kayit-defteri.md` — hangi bilgi nerede, sahibi kim, saklama süresi ne |
| Bilgi yönetim raporu | `raporlar/` |

> Bu doküman haritasının kendisi MAN.6 kapsamındadır.

#### MAN.8 — Kalite Güvence
📁 `docs/33061/MAN.8-kalite-guvence/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` |
| Kalite kriterleri ve yöntemleri | `tamamlanma-tanimi.md` (DoD) + `kod-gozden-gecirme-kontrol-listesi.md` |
| Kalite güvence sistemi | CI/CD kalite kapıları (GitHub Actions) |
| Düzeltici faaliyet | GitHub Issue (etiket: `duzeltici-faaliyet`) |
| Kalite güvence raporu | `raporlar/` — dönemsel kalite raporu |
| **Süreç değerlendirme raporu** (`MAN.8.BP3`) | `raporlar/YYYY-AA-GG-surec-gozden-gecirme-raporu.md` — 33061 süreçlerinin standarda karşı değerlendirilmesi |
| Olay ve problem kayıtları | GitHub Issue (etiket: `hata`) |

---

## 3. Süreçler arası ortak kanıtlar

Bazı kanıtlar birden fazla sürece hizmet eder. Tek kopya tutulur:

| Kanıt | Konum | Hizmet ettiği süreçler |
|---|---|---|
| İzlenebilirlik matrisi | `docs/33061/izlenebilirlik-matrisi.md` | TEC.2, TEC.3, TEC.5, TEC.7, TEC.8, TEC.9, TEC.10, TEC.11, TEC.13 |
| Mimari Karar Kayıtları (ADR) | `docs/adr/` | TEC.5, MAN.5 |
| Karar kayıt defteri | `docs/karar-kayit-defteri.md` | TEC.2, TEC.5, MAN.2 |
| Git geçmişi ve PR kayıtları | GitHub | TEC.7, TEC.9, MAN.5, MAN.8 |
| CI/CD çalışma kayıtları | GitHub Actions | TEC.8, TEC.9, MAN.8 |
| GitHub Projects panosu | GitHub | MAN.1, MAN.2 |
| Mevcut sistem ve süreç analizleri | `docs/analiz/` | TEC.2, TEC.10 |
| Modül listesi ve bağımlılık haritası | `docs/mimari/modul-listesi-ve-bagimliliklar.md` | TEC.2, TEC.5, MAN.1 |
| Vizyon ve kapsam belgesi | `docs/mimari/vizyon-ve-kapsam.md` | TEC.2 |

---

## 4. Doküman kuralları

1. **Dil:** Teknik terimler dışında tüm dokümanlar Türkçedir.
2. **Kimliklendirme:** Paydaş gereksinimi `PG-<MODÜL>-<no>`, sistem gereksinimi `REQ-<MODÜL>-<no>`, risk `R-<no>`, mimari karar `ADR-<no>`, test senaryosu `TS-<MODÜL>-<no>`.
3. **Tarih biçimi:** Dosya adlarında ve içerikte `YYYY-AA-GG`.
4. **Değişiklik:** Her dokümanın başında "Son güncelleme", sonunda değişiklik geçmişi bulunur. Doküman değişiklikleri de Pull Request ile yapılır (MAN.5).
5. **Sır barındırmama:** Hiçbir dokümanda parola, bağlantı dizesi (connection string), API anahtarı veya gerçek kişisel veri örneği yer almaz.
6. **Telif:** TSE standard dokümanı `.gitignore` ile depo dışında tutulur; içeriği dokümanlarımıza kopyalanmaz, yalnızca madde numarasıyla atıf yapılır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-03 | 0.1 | İlk oluşturma | Bilgi İşlem |
| 2026-09-07 | 0.2 | Karar kayıt defteri, ADR dizini, modül listesi ve vizyon-kapsam belgeleri haritaya eklendi | Bilgi İşlem |
| 2026-09-18 | 0.3 | MAN.8 çıktılarına **süreç değerlendirme raporu** eklendi (`MAN.8.BP3`) | Bilgi İşlem |
