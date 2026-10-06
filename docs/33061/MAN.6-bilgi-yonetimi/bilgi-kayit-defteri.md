# Bilgi Kayıt Defteri

**Belge kimliği:** MAN.6-BKD
**Süreç:** MAN.6 — Bilgi Yönetimi (`MAN.6.BP1`–`BP4`)
**Son güncelleme:** 2026-10-05 · **Sahibi:** Bilgi İşlem
**Kaynak:** T3 süreç denetimi, bulgu BULGU-06 (#131)

> Proje boyunca üretilen veya kullanılan her bilgi kümesinin **nerede durduğu, kimin
> sorumluluğunda olduğu, ne kadar gizli olduğu, ne kadar saklanacağı ve nasıl yok
> edileceği** burada tutulur. Depo dışında duran kümeler de dahildir. Yeni bir küme
> oluştuğunda veya yeri değiştiğinde defter PR ile güncellenir.

---

## 1. Gizlilik sınıfları

| Sınıf | Anlamı | Örnek |
|---|---|---|
| **Genel** | Kurum dışına verilebilir | Lisanslı açık kaynak bileşen listesi |
| **Kurum içi** | Kurum çalışanları görebilir; kişisel veri yok | Kod, belgeler, CI kayıtları |
| **Gizli** | Kişisel veri veya sır içerir; yalnızca yetkili kişiler | UAT veritabanı, sır dosyaları, LOGO düzeltme listeleri |

**Saklama süresi kararları:** RACI'ye göre kişisel verinin saklama süresini KVKK Sorumlusu
belirler (`roller-ve-sorumluluklar.md` §3, ADR-0009 §6). Aşağıda "öneri" diye işaretlenen
süreler yazılı bir karara dayanmıyor; KVKK Sorumlusu'nun onayını bekliyor.

---

## 2. Depo ve geliştirme altyapısı

| # | Bilgi kümesi | Konum | Sahibi | Gizlilik | Saklama | İmha |
|---|---|---|---|---|---|---|
| BK-01 | Kaynak kodu ve proje belgeleri | GitHub, özel depo `Duzen-Saglik-Grubu/DSG-HRMS` | Bilgi İşlem | Kurum içi | Sistemin ömrü boyunca | Gerekmez. Kişisel veri depoya girmez; `gitleaks` CI'da denetler |
| BK-02 | Issue, PR ve inceleme kayıtları | GitHub | Bilgi İşlem | Kurum içi | Sistemin ömrü boyunca | Gerekmez. Issue'ya kişisel veri yazılmaz (kabul planı §9) |
| BK-03 | CI çalışma kayıtları ve yapıtları | GitHub Actions | Bilgi İşlem | Kurum içi; test verisi sentetik | **90 gün**: GitHub otomatik siler | Otomatik. Kalıcı özet BK-04'te |
| BK-04 | CI kanıt özetleri ve doğrulama raporları | Depo, `TEC.9-dogrulama/` | Bilgi İşlem | Kurum içi | Sistemin ömrü boyunca | Gerekmez |
| BK-05 | Satın alınmış standartlar (TSE 33061, 33020) ve NetGSM belgeleri | Geliştirici makinesi, `docs/TSE_*`, `docs/NetGSM` (`.gitignore`'da) | Bilgi İşlem | Kurum içi; lisanslı, çoğaltılamaz (R-11) | Lisans süresince | Lisans bitince silinir |

## 3. Ortamlar

| # | Bilgi kümesi | Konum | Sahibi | Gizlilik | Saklama | İmha |
|---|---|---|---|---|---|---|
| BK-10 | UAT veritabanı: personel, hesaplar, denetim izi, güvenlik olayları | UAT sunucusu, Docker volume `dsg-hrms-uat_uat-postgres-data` | Bilgi İşlem | **Gizli**: gerçek personel verisi (R-25) | **Öneri:** UAT kullanıldıkça tutulur; düzenli yedeklenmez (`KR-098`) | **Öneri:** Üretime geçişten sonra veya UAT kapatıldığında volume silinir (`docker compose down -v`); silme kayda geçer |
| BK-11 | UAT uygulama günlükleri | UAT, volume `dsg-hrms-uat_uat-api-logs` | Bilgi İşlem | Kurum içi: kişisel veri maskelenir (#119) | **90 dosya (gün)**: `RetainedFileCountLimit`, ADR-0009 | Otomatik: eski dosya silinir |
| BK-12 | UAT sır dosyası | `/opt/dsg-hrms/secrets/.env.uat` | Bilgi İşlem | **Gizli** | UAT kullanıldıkça | UAT kapatıldığında silinir. Bir sır değiştirildiğinde eski değer dosyada tutulmaz |
| BK-13 | UAT geçici veritabanı kopyaları | `/opt/dsg-hrms/yedek-*.sql.gz` | Bilgi İşlem | **Gizli** | Yalnızca riskli işlem sürince (`KR-098`) | İşlem doğrulanınca silinir |
| BK-14 | UAT dağıtım kaydı | `/opt/dsg-hrms/deployments.log`; depoda `TEC.10-gecis/kayitlar/` | Bilgi İşlem | Kurum içi | Sistemin ömrü boyunca (depodaki) | Gerekmez |
| BK-15 | Yerel geliştirme veritabanı | Geliştirici makinesi, Docker volume'leri | Bilgi İşlem | **Gizli**: geliştirme ortamı gerçek LOGO verisiyle çalışır | **Öneri:** Geliştirme süresince | **Öneri:** Geliştirici değiştiğinde veya makine el değiştirdiğinde volume'ler silinir |

> **#73:** `KR-058` taşımasında güvence için bırakılan eski volume'ler ve UAT'deki
> `/root/kr058-yedek-2026-09-25/` dizini (sır dosyasının bir kopyasını içeriyordu)
> silindi; issue kapandı. Bu defter, böyle geçici kopyaların **oluştuğu anda** kayda
> geçmesini sağlar (BK-13).

## 4. Depo dışındaki çalışma dosyaları

| # | Bilgi kümesi | Konum | Sahibi | Gizlilik | Saklama | İmha |
|---|---|---|---|---|---|---|
| BK-20 | LOGO veri düzeltme ve kart çelişkileri listeleri | `Masaüstü\HRMS\DSG-HRMS_LOGO_*.xlsx` | Bilgi İşlem; listeyi İK kullanır | **Gizli**: ad, sicil, iletişim bilgisi | **Öneri:** İK'nın düzeltmesi bitip yeniden ölçüm yapılana kadar | **Öneri:** Yeniden ölçümden sonra silinir; yeni liste gerekirse yeniden üretilir |
| BK-21 | Modül gereksinim çalışma dosyaları | `Masaüstü\HRMS\DSG-HRMS_*_Gereksinimleri.xlsx`, `…_Sistem_Yonetimi_Parametreleri.xlsx` | Bilgi İşlem | Kurum içi | Onaylı içerik depoya (TEC.2) alınınca çalışma kopyasıdır | **Öneri:** Modülün gereksinimleri depoda onaylandıktan sonra silinebilir |
| BK-22 | UAT izin listesi | `Masaüstü\HRMS\DSG-HRMS_UAT_Izin_Listesi.txt` | Bilgi İşlem | **Gizli**: e-posta adresleri ve telefon numaraları | Kabul oturumları sürdükçe | **Öneri:** Kabul oturumundan sonra listedeki kişiler çıkarılır (`KR-083`) |
| BK-23 | T3 İK inceleme seti | `Masaüstü\HRMS\T3_Kabul_IK_Inceleme\` | Bilgi İşlem | Kurum içi | Kabul kararına kadar | Asılları depodadır; kabulden sonra silinebilir |
| BK-24 | İmzalı kabul formları (taranmış) | **Belirlenecek** (kurum içi güvenli konum; TEC.11 YAKLASIM §6) | İK | Kurum içi: ad ve imza | **Öneri:** Sistemin ömrü boyunca (belgelendirme kanıtı) | — |
| BK-25 | Geliştirme yardımcısıyla yazışma arşivi | Geliştirici makinesi, `claude/` (`.gitignore`'da) | Bilgi İşlem | **Gizli**: kararların ve onayların ham kaydı; parolalar 05.10.2026'da temizlendi | **Öneri:** Kararlar karar defterine, onaylar onay kaydına aktarıldıktan sonra arşiv olarak | Parolalar 05.10.2026'da temizlendi (Ö-1). Arşive yeni parola yazılmaz |

## 5. Açık işler

| # | İş | Neden | Sahibi |
|---|---|---|---|
| Ö-1 | ~~Yazışma arşivinden parolaların temizlenmesi; UAT sunucu parolasının değiştirilip SSH'nin anahtarla yapılması~~ **Kapandı (06.10.2026):** arşivden 24 parola temizlendi (UAT 21, LOGO, eski İK veritabanı, SMTP); SSH anahtarla; `root` parolası değiştirildi; parola girişi bilerek açık (`KR-102`, #174). Kalan: parola, yardımcının bu oturuma ait dökümünde duruyor; parola değiştirildiği için geçersiz | UAT `root` parolası her sunucu işinde yazışmaya düz metin olarak yazılıyor. Parola bu arşivde ve yardımcının oturum dökümlerinde kalıcı olarak duruyor. Parola girişi açık olduğu için (R-25) bu parola sunucuya tam erişim demektir | Bilgi İşlem |
| Ö-2 | "Öneri" olarak işaretlenen saklama ve imha sürelerinin KVKK Sorumlusu tarafından onaylanması | RACI: saklama süreleri KVKK Sorumlusu'nda; ADR-0009 §6 açık iş | KVKK Sorumlusu |
| Ö-3 | İmzalı kabul formlarının (BK-24) saklanacağı konumun belirlenmesi | T3 kabul oturumundan önce gerekli | İK + Bilgi İşlem |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#131) | Bilgi İşlem |
| 2026-10-06 | 1.1 | Ö-1 kapandı; BK-25 güncellendi (#177) | Bilgi İşlem |
