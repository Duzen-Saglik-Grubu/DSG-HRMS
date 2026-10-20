# Doküman Haritası — Ne Nerede?

**Proje:** DSG-HRMS — Düzen Sağlık Grubu İnsan Kaynakları Yönetim Sistemi
**Amaç:** Bu doküman, TS ISO/IEC TS 33061 Seviye 2 kapsamında üretilen tüm kanıtların nerede tutulduğunu tek sayfadan gösterir. Bir denetçi ya da yeni katılan bir ekip üyesi aradığı kaydı buradan bulur.

**Son güncelleme:** 2026-10-20
**Doküman sahibi:** Bilgi İşlem Birim Sorumlusu

---

## 1. Klasör düzeni

```
DSG-HRMS/
├── docs/
│   ├── 00-DOKUMAN-HARITASI.md      ← bu doküman
│   ├── 33061/                      ← süreç kodu bazlı kanıt klasörleri
│   │   ├── 00-OLGUNLUK-SEVIYESI-KRITERLERI.md  ← Seviye 2 ölçütleri ve öz değerlendirme
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
│   ├── mimari/                     ← vizyon-kapsam, modül listesi, paket envanteri, izin listesi
│   ├── analiz/                     ← mevcut sistem ve veri analizleri
│   ├── api/openapi-v1.json         ← API sözleşmesi (derlemeden üretilir, CI'da denetlenir)
│   └── sablonlar/                  ← doküman şablonları (ADR şablonu, kabul formu)
├── CHANGELOG.md                    ← sürüm notları (her etikete bir bölüm, KR-097)
├── CONTRIBUTING.md                 ← çalışma kuralları, Tamamlanma Tanımı (DoD)
├── .github/                        ← CI iş akışları, denetim betikleri, issue/PR şablonları
├── docker/                         ← Compose yığınları, dağıtım ve TLS betikleri
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
| Paydaş gereksinimleri (Stakeholder requirements) | `paydas-gereksinimleri/PG-<MODÜL>.md` (şablon `SABLON-PG.md`; T3: `PG-KMLK.md`; T1: `PG-PERS.md`) |
| Geçerleme kriterleri (Validation criteria) | Her paydaş gereksiniminin "Kabul Kriteri" bölümü |
| İzlenebilirlik eşlemesi (Traceability mapping) | `docs/33061/izlenebilirlik-matrisi.md` |
| Kayıt (Record) | `kayitlar/YYYY-AA-GG-<modül>-gereksinim-toplantisi.md` (şablon `kayitlar/SABLON-ik-gereksinim-toplantisi.md`; T3: `2026-09-23-kimlik-gereksinim-toplantisi.md`; T1: `2026-10-19-personel-gereksinim-toplantisi.md`); değişiklik talepleri `tur:degisiklik-talebi` issue'su ve paydaş onayı (YAKLASIM §4) |

> **Kaynak:** Gereksinimler, İK birimi ile yapılan modül toplantılarında toplanır. Her toplantı bir kayıt dosyası üretir ve ilgili paydaş gereksinimlerine referans verir.

#### TEC.3 — Sistem/Yazılım Gereksinimlerinin Tanımlanması
📁 `docs/33061/TEC.3-sistem-yazilim-gereksinimleri/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` |
| Sistem/yazılım gereksinimleri | `gereksinimler/SYG-<MODÜL>.md` (her madde `SYG-<MODÜL>-<no>` kimlikli; bkz. TEC.3 YAKLASIM §3.1) |
| Doğrulama kriterleri (Verification criteria) | `SYG-<MODÜL>.md` §4 "Doğrulama" sütunu (Test · Gösterim · İnceleme · Analiz); testler SYG kimliğini anar ve bu CI'da denetlenir |
| Sistem tanımı (öğeler, arayüzler, durumlar) | `SYG-<MODÜL>.md` §2–§3 |
| İzlenebilirlik eşlemesi | `SYG-<MODÜL>.md` §8 + `docs/33061/izlenebilirlik-matrisi.md`; tutarlılığı CI'da denetlenir (`requirement-traceability-check.mjs`) |
| Kayıt | GitHub Issue (etiket: `tur:gereksinim`) |

#### TEC.5 — Tasarım Tanımlama
📁 `docs/33061/TEC.5-tasarim-tanimlama/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` |
| Tasarım modeli | ADR'ler (veri modeli ADR-0005) ve `docs/mimari/` (modül listesi, paket envanteri, izin listesi). Diyagram yok (YAKLASIM §8 açık nokta 4; #234) |
| Tasarım gerekçesi (Design rationale) | `docs/adr/` — Mimari Karar Kayıtları |
| Arayüz tanımı (Interface definition) | `docs/api/openapi-v1.json` (OpenAPI sözleşmesi) + `SYG-<MODÜL>.md` §2.3 dış arayüzler |
| Tasarım değerlendirme raporu | `raporlar/YYYY-AA-GG-tasarim-gozden-gecirme.md` (henüz yazılmadı) |
| İzlenebilirlik eşlemesi | `docs/33061/izlenebilirlik-matrisi.md` |

#### TEC.7 — Gerçekleştirme (Implementation)
📁 `docs/33061/TEC.7-gerceklestirme/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md`; kodlama kuralları ve Tamamlanma Tanımı (DoD) `CONTRIBUTING.md` §6 |
| Sistem öğesi (yazılım) | `src/` — Git deposundaki kaynak kod |
| Öğe tanımı | Kod içi dokümantasyon + `docs/mimari/` |
| Gerçekleştirme raporu | Sürüm notları (`CHANGELOG.md`) + Pull Request özetleri |
| Kayıt | Git commit geçmişi + PR kayıtları (GitHub) |

#### TEC.8 — Entegrasyon
📁 `docs/33061/TEC.8-entegrasyon/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (CI hattı, entegrasyon sırası, Docker Compose yığını) |
| Arayüz tanımı | `docs/api/openapi-v1.json`; dış arayüzler (LOGO, NetGSM, SMTP, tarayıcı) YAKLASIM §2 ve `SYG-<MODÜL>.md` §2.3. NAS arayüzü Y7 Çalışan Belge Yönetimi ile gelecek (WBS 4.Y7) |
| Entegre edilmiş sistem | Açıklamalı sürüm etiketi ve ondan derlenen Docker imajları (commit OCI `revision` etiketinde) |
| Entegrasyon raporu | `raporlar/YYYY-AA-GG-<modül>-entegrasyon-raporu.md` (T3: `2026-10-05-t3-entegrasyon-raporu.md`) + GitHub Actions çalışma kayıtları |

#### TEC.9 — Doğrulama (Verification)
📁 `docs/33061/TEC.9-dogrulama/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (test stratejisi, kapsam eşikleri, kalite kapıları) |
| Doğrulama kriterleri | `SYG-<MODÜL>.md` §4 "Doğrulama" sütunu; ADR-0011 §6 CI kalite kapıları; testler (`src/backend/tests`, ön yüz `*.test.tsx`, `e2e/`) |
| Doğrulanmış yazılım | CI'da tüm kapıları geçmiş, etiketlenmiş sürüm (G2 kapısı) |
| Doğrulama raporu | `raporlar/YYYY-AA-GG-<modül>-dogrulama-raporu.md` (T3: `2026-10-04-t3-dogrulama-raporu.md`, G2 kaydı §5) ve ölçüm/inceleme raporları (`2026-10-02-t3-performans-olcumu.md`, `2026-10-04-syg-064-metin-incelemesi.md`) |
| Kayıt | `kayitlar/YYYY-AA-GG-<sürüm>-ci-kanit-ozeti.md` (CI çalışmasının kalıcı özeti); PR inceleme onay yorumları (`KR-096`) |

> **Not:** Doğrulama = "ürünü doğru mu yaptık" (test, gözden geçirme). Geçerleme (TEC.11) = "doğru ürünü mü yaptık" (İK kabulü). İkisi ayrı süreçtir ve ayrı kanıt üretir.

#### TEC.10 — Geçiş (Transition)
📁 `docs/33061/TEC.10-gecis/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (kurulum, kesim/cut-over, geri dönüş planı) |
| **Ortam kurulum runbook** | `uat-ortami-kurulum-runbook.md` — UAT sunucusu kurulumu, dağıtım ve doğrulama |
| **Sürüm kontrol listesi** | `surum-kontrol-listesi.md` — aday etiketleme, UAT kurulumu, kurulum sonrası kontrol, CI kanıt özeti, kabul ve modül kapanışı adımları (`KR-110`, #218) |
| Kurulmuş sistem | `kayitlar/uat-dagitim-kaydi.md` (UAT; sunucuda `deployments.log`). Üretim dağıtım kaydı AS aşamasında |
| Geçiş raporu | `raporlar/` — veri göçü mutabakat raporları, kesim raporu (AS aşamasında) |
| Kayıt | `kayitlar/` — UAT işlemleri (ör. `2026-10-10-uat-hesap-temizligi.md`); ileride göç denemeleri, kullanıcı eğitim kayıtları |

> Eski HRMS'ten veri göçü bu sürecin kapsamındadır. Mutabakat raporları burada tutulur.

#### TEC.11 — Geçerleme (Validation)
📁 `docs/33061/TEC.11-gecerleme/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (UAT süreci, kabul kriterleri, ortam) |
| Geçerleme kriterleri | Paydaş gereksinimlerinin kabul kriterleri (TEC.2'den gelir) + modül kabul planı (`T3-kabul-plani.md`: KS senaryoları, KG kanıt gösterimleri) |
| Kullanıcı hazırlığı | Katılımcı kullanım notu (`T3-kullanim-notu.md`) |
| Geçerlenmiş yazılım | İK tarafından kabul edilmiş, etiketlenmiş sürüm (T3: `v0.2.0`, `KR-105`) |
| Geçerleme raporu | `raporlar/YYYY-AA-GG-<modül>-kabul-raporu.md` (T3: `2026-10-13-t3-kabul-raporu.md`) |
| Kayıt | `kayitlar/YYYY-AA-GG-<modül>-kabul-formu.md` + oturumda doldurulan Word formu (şablon `docs/sablonlar/kabul-formu.md`). Taranmış imzalı nüsha proje sonunda kalite belgeleriyle eklenir (`KR-107`) |

#### TEC.13 — Bakım
📁 `docs/33061/TEC.13-bakim/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (destek modeli, hata sınıflandırma, çözüm süreleri) |
| Bakımı yapılmış sistem | Üretimdeki güncel sürüm |
| Bakım raporu | `raporlar/` — dönemsel bakım ve olay raporları |
| Kayıt | GitHub Issue (`[HATA]`, etiket `tur:hata`) + `kayitlar/` olay kayıtları (ör. `2026-10-04-t3-olay-kayitlari.md`) |

### 2.2 Teknik Yönetim Süreçleri (MAN)

#### MAN.1 — Proje Planlama
📁 `docs/33061/MAN.1-proje-planlama/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (plan seti, revizyon tetikleyicileri, onay) |
| Proje hedefleri ve kısıtları | `proje-plani.md` (Hedefler, Kısıtlar bölümleri) |
| Proje planı | `proje-plani.md` |
| İş kırılım yapısı (WBS) | `is-kirilim-yapisi.md` + GitHub Projects panosu |
| Zaman planı | GitHub Projects + Milestone kayıtları |
| Roller ve sorumluluklar | `roller-ve-sorumluluklar.md` (RACI) |
| Aşama kapanış değerlendirmesi | `A1-asama-kapanis-degerlendirmesi.md` (her aşama için bir belge) |
| Altyapı ve kaynak ihtiyaçları | `proje-plani.md` (Kaynaklar bölümü) |
| Planlama kaydı | `kayitlar/` — plan revizyonları ve onay kayıtları (`2026-10-06-plan-ve-raci-onay-talebi.md`, `2026-10-09-ust-yonetim-onay-formu.docx`; `KR-103`) |

#### MAN.2 — Proje Değerlendirme ve Kontrol
📁 `docs/33061/MAN.2-proje-degerlendirme-ve-kontrol/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (izleme sıklığı, ölçütler, eskalasyon) |
| Proje durum raporu | `raporlar/YYYY-AA-GG-durum-raporu.md` (her modül kapanışında ve en geç ayda bir, `KR-101`) |
| Gözden geçirme sonucu | `kayitlar/YYYY-AA-GG-<modül>-kapanis-gozden-gecirmesi.md` (G4) |
| Değişiklik talebi | GitHub Issue (etiket: `tur:degisiklik-talebi`) |
| Alınan dersler | `kayitlar/YYYY-AA-GG-<modül>-alinan-dersler.md` |

#### MAN.4 — Risk Yönetimi
📁 `docs/33061/MAN.4-risk-yonetimi/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (risk ölçeği, sahiplik, gözden geçirme sıklığı) |
| Risk kayıt defteri (Risk register) | `risk-kayit-defteri.md` |
| Risk raporu | Durum raporunun risk bölümü (MAN.2); ayrı rapor gerekirse `raporlar/` |

#### MAN.5 — Konfigürasyon Yönetimi
📁 `docs/33061/MAN.5-konfigurasyon-yonetimi/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (dallanma, sürümleme, etiketleme, baseline tanımı) |
| Konfigürasyon yönetim sistemi | Git + GitHub; PR zorunluluğu. Sunucu tarafı dal koruma yok; yerine telafi kontrolleri (`dal-koruma-telafi-kontrolleri.md`, `KR-055`) |
| Konfigürasyon öğeleri | `konfigurasyon-ogeleri.md` — neyin kontrol altında olduğu (`KÖ-NN`) |
| Baseline | Açıklamalı Git etiketleri (`KR-097`): kabul adayı `vX.Y.0-rc.N`, kabul edilen `vX.Y.0` (`v0.1.0`, `v0.2.0` …) |
| Değişiklik talebi | GitHub Issue + Pull Request |
| Sürüm kontrolü | `CHANGELOG.md` + açıklamalı Git etiketi (`KR-097`). GitHub Release kullanılmaz |
| Konfigürasyon denetim sonucu | `raporlar/YYYY-AA-GG-*konfigurasyon-denetimi.md` (`2026-10-05-ilk-…`, `2026-10-13-t3-…`) |

#### MAN.6 — Bilgi Yönetimi
📁 `docs/33061/MAN.6-bilgi-yonetimi/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` (bilgi sahipliği, erişim, saklama, yedekleme) |
| Bilgi kayıt defteri (Information register) | `bilgi-kayit-defteri.md` — hangi bilgi nerede, sahibi kim, saklama süresi ne |
| Bilgi yönetim raporu | `raporlar/` (henüz yok; defterin depo dışı konumlarla karşılaştırması modül sonu denetiminde yapılır, `KR-101`) |

> Bu doküman haritasının kendisi MAN.6 kapsamındadır. "Son güncelleme" alanının değişiklik
> geçmişiyle tutarlılığı CI'da denetlenir (`.github/scripts/doc-consistency-check.mjs`).

#### MAN.8 — Kalite Güvence
📁 `docs/33061/MAN.8-kalite-guvence/`

| 33061 Süreç Çıktısı | Bizdeki karşılığı |
|---|---|
| Yaklaşım | `YAKLASIM.md` |
| Kalite kriterleri ve yöntemleri | Tamamlanma Tanımı (DoD): tek kaynak PR şablonu (`.github/PULL_REQUEST_TEMPLATE.md`), `CONTRIBUTING.md` §6 ona atıf yapar; ADR-0011 §6 kalite kapısı tanımı ve tek kapı listesi; kalite güvence ölçütleri K1–K6 (YAKLASIM §2.5); `docs/33061/00-OLGUNLUK-SEVIYESI-KRITERLERI.md` |
| Kalite güvence sistemi | CI/CD kalite kapıları (GitHub Actions) |
| Düzeltici faaliyet | GitHub Issue (`[DÜZELTİCİ]`, etiket `tur:duzeltici-faaliyet`) |
| Kalite güvence raporu | Ayrı rapor yazılmaz: K1–K6 değerleri MAN.2 durum raporu §3'te ve modül sonu denetiminde verilir (`KR-111`); T3 başlangıç değerleri `kayitlar/2026-10-15-kg-olcutleri-t3-baslangic.md` |
| **Süreç değerlendirme raporu** (`MAN.8.BP3`) | `raporlar/YYYY-AA-GG-surec-gozden-gecirme-raporu.md` ve modül sonu denetimleri (`2026-10-02-t3-surec-denetimi*.md`, `2026-10-13-t3-modul-sonu-denetimi*.md`) — 33061 süreçlerinin standarda karşı değerlendirilmesi |
| Kayıt | `kayitlar/` (ör. `2026-10-04-pr-onay-kaydi.md`, `2026-10-15-kg-olcutleri-t3-baslangic.md`) |
| Olay ve problem kayıtları | GitHub Issue (etiket: `tur:hata`) |

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
| Paket envanteri ve izin listesi | `docs/mimari/paket-envanteri.md`, `docs/mimari/izin-listesi.md` | TEC.5, TEC.7, MAN.5 |
| API sözleşmesi | `docs/api/openapi-v1.json` | TEC.5, TEC.8, TEC.9 |
| Sürüm notları | `CHANGELOG.md` | TEC.7, TEC.10, MAN.5 |
| Çalışma kuralları ve DoD | `CONTRIBUTING.md` | TEC.7, TEC.9, MAN.5, MAN.8 |
| Olgunluk ölçütleri ve öz değerlendirme | `docs/33061/00-OLGUNLUK-SEVIYESI-KRITERLERI.md` | MAN.8 (tüm süreçler) |
| Bilgi kayıt defteri (depo dışı bilgi dahil) | `docs/33061/MAN.6-bilgi-yonetimi/bilgi-kayit-defteri.md` | MAN.6 (tüm süreçler) |

---

## 4. Doküman kuralları

1. **Dil:** Teknik terimler dışında tüm dokümanlar Türkçedir.
2. **Kimliklendirme:** Paydaş gereksinimi `PG-<MODÜL>-<no>`, sistem gereksinimi `SYG-<MODÜL>-<no>` (T3 paydaş gereksinimleri istisnaen `REQ-KMLK-<no>` kimliğiyle onaylandı), risk `R-<no>`, mimari karar `ADR-<no>`, proje kararı `KR-<no>`, konfigürasyon öğesi `KÖ-<no>`, bilgi kümesi `BK-<no>`, kabul senaryosu `KS-<no>` ve kanıt gösterimi `KG-<no>` (modül kabul planında).
3. **Tarih biçimi:** Dosya adlarında ve içerikte `YYYY-AA-GG`.
4. **Değişiklik:** Her dokümanın başında "Son güncelleme", sonunda değişiklik geçmişi bulunur. "Son güncelleme" geçmişin son satırından eski olamaz; bu her PR'da CI'da denetlenir (`doc-consistency-check.mjs`, #214). Doküman değişiklikleri de Pull Request ile yapılır (MAN.5).
5. **Sır barındırmama:** Hiçbir dokümanda parola, bağlantı dizesi (connection string), API anahtarı veya gerçek kişisel veri örneği yer almaz.
6. **Telif:** TSE standard dokümanı `.gitignore` ile depo dışında tutulur; içeriği dokümanlarımıza kopyalanmaz, yalnızca madde numarasıyla atıf yapılır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-03 | 0.1 | İlk oluşturma | Bilgi İşlem |
| 2026-09-07 | 0.2 | Karar kayıt defteri, ADR dizini, modül listesi ve vizyon-kapsam belgeleri haritaya eklendi | Bilgi İşlem |
| 2026-09-18 | 0.3 | MAN.8 çıktılarına **süreç değerlendirme raporu** eklendi (`MAN.8.BP3`) | Bilgi İşlem |
| 2026-09-24 | 0.4 | Sistem gereksinimi kimliği `REQ-<MODÜL>-<no>` → `SYG-<MODÜL>-<no>` (T3 paydaş gereksinimleriyle çakışma; TEC.3 YAKLASIM §3.1) | Bilgi İşlem |
| 2026-10-14 | 0.5 | T3 modül sonu denetimi (MAN.8 Ek A, A-09; MAN.5 D-20): var olmayan dosyalara atıflar kaldırıldı (`tamamlanma-tanimi.md`, `kod-gozden-gecirme-kontrol-listesi.md`, `docs/mimari/fonksiyon-modeli.md`, `docs/mimari/entegrasyon-arayuzleri.md`); "GitHub Releases" yerine açıklamalı Git etiketi (`KR-097`); bilgi kayıt defteri, konfigürasyon öğeleri, T3 doğrulama, entegrasyon, kabul ve denetim kayıtları, MAN.1 yaklaşımı, ortak kanıtlar ve kimlik önekleri eklendi; etiket adları güncel (#214) | Bilgi İşlem |
| 2026-10-15 | 0.6 | TEC.10: sürüm kontrol listesi eklendi (#218) | Bilgi İşlem |
| 2026-10-15 | 0.7 | MAN.8: DoD'nin tek kaynağı PR şablonu; kalite güvence ölçütleri K1–K6 ve T3 başlangıç kaydı; ayrı kalite raporu yok. TEC.5: diyagram notu YAKLASIM §8 ve #234'e bağlandı (`KR-111`, #217) | Bilgi İşlem |
| 2026-10-20 | 0.8 | TEC.2: T1 toplantı kaydı ve `PG-PERS.md` (#239) | Bilgi İşlem |
