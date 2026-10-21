# Bilgi Kayıt Defteri

**Belge kimliği:** MAN.6-BKD
**Süreç:** MAN.6 — Bilgi Yönetimi (`MAN.6.BP1`–`BP4`)
**Son güncelleme:** 2026-10-21 · **Sahibi:** Bilgi İşlem
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
| BK-06 | Eski HRMS kaynak kodu | Geliştirici makinesi, depo kökünde `src_old/` (`.gitignore`'da, `KR-031`) | Bilgi İşlem | **Gizli**: kullanımdaki eski sistemin düz metin veritabanı bağlantı dizelerini ve SMTP parolasını içerir | Geçiş (AS) tamamlanana kadar; göç kuralları için başvuru kaynağı (Doğuş Uçanok'un kararı, 14.10.2026) | AS kapanışında silinir. İçindeki parolalar eski sistemindir; eski sistem kapatılınca geçersizleşir. Klasör geliştirici makinesinden başka yere kopyalanmaz |

## 3. Ortamlar

| # | Bilgi kümesi | Konum | Sahibi | Gizlilik | Saklama | İmha |
|---|---|---|---|---|---|---|
| BK-10 | UAT veritabanı: personel, hesaplar, denetim izi, güvenlik olayları | UAT sunucusu, Docker volume `dsg-hrms-uat_uat-postgres-data` | Bilgi İşlem | **Gizli**: gerçek personel verisi (R-25) | UAT kullanıldıkça tutulur; düzenli yedeklenmez (`KR-098`) | Üretime geçişten sonra veya UAT kapatıldığında volume silinir (`docker compose down -v`); silme kayda geçer |
| BK-19 | Denetim izi, erişim kaydı ve güvenlik olayları (uygulama veritabanında; UAT'de BK-10 içinde, üretimde üretim veritabanında) | `audit` şeması: `change_log`, erişim kaydı, `security_event` | Bilgi İşlem | **Gizli**: kim, neyi, ne zaman; IP | **10 yıl** (KVKK Sorumlusu kararı, 14.10.2026, `KR-109`). Kayıtlar değiştirilemez (`KR-060`, `KR-094`) | Süre dolunca silinir veya anonimleştirilir; imha işi Y2 Denetim ve KVKK modülünde geliştirilir, imha kayda geçer (ADR-0009 §6) |
| BK-29 | HRMS personel verisi: kişi ve istihdam kayıtları, LOGO'dan senkronize alanlar, HRMS'te tutulan bilgiler (evlilik bilgisi, görev, acil durum kişisi; `KR-112`) ve kan grubu | Uygulama veritabanı (UAT'de BK-10, yerelde BK-15 içinde; üretimde üretim veritabanında) | Bilgi İşlem; verinin kaynağı LOGO ve İK | **Gizli**: kişisel veri; kan grubu özel nitelikli (§3.1) | Sistem Yönetimi parametresinde tanımlanan süre (KVKK Sorumlusu kararı, 20.10.2026, `KR-116`; PG-PERS-45) | Süresi dolan kaydın imhası Y2'de geliştirilir; o zamana kadar silinmez |
| BK-11 | UAT uygulama günlükleri | UAT, volume `dsg-hrms-uat_uat-api-logs` | Bilgi İşlem | Kurum içi: kişisel veri maskelenir (#119) | **90 dosya (gün)**: `RetainedFileCountLimit`, ADR-0009 | Otomatik: eski dosya silinir |
| BK-12 | UAT sır dosyası ve TLS özel anahtarı | `/opt/dsg-hrms/secrets/.env.uat` (21.10.2026'dan beri uygulamanın veritabanı çalışma rolü parolası `HRMS_APP_DB_PASSWORD` dahil, `KR-118`); TLS sertifikası ve özel anahtarı `/opt/dsg-hrms/tls/` (KÖ-31) | Bilgi İşlem | **Gizli** | UAT kullanıldıkça | UAT kapatıldığında silinir. Bir sır değiştirildiğinde eski değer dosyada tutulmaz. Dosyanın yedek kopyası bırakılmaz; 27.09 tarihli kopya 13.10.2026'da silindi (#207) |
| BK-13 | UAT geçici veritabanı kopyaları | `/opt/dsg-hrms/yedek-*.sql.gz` | Bilgi İşlem | **Gizli** | Yalnızca riskli işlem sürince (`KR-098`) | İşlem doğrulanınca silinir |
| BK-14 | UAT dağıtım kaydı | `/opt/dsg-hrms/deployments.log`; depoda `TEC.10-gecis/kayitlar/` | Bilgi İşlem | Kurum içi | Sistemin ömrü boyunca (depodaki) | Gerekmez |
| BK-15 | Yerel geliştirme veritabanı | Geliştirici makinesi, Docker volume'leri | Bilgi İşlem | **Gizli**: geliştirme ortamı gerçek LOGO verisiyle çalışır | Geliştirme süresince | Geliştirici değiştiğinde veya makine el değiştirdiğinde volume'ler silinir |
| BK-16 | Yerel geliştirme ortamı dosyası | Geliştirici makinesi, `docker/.env` (`.gitignore`'da; KÖ-36) | Bilgi İşlem | **Gizli**: bağlantı bilgileri ve sırlar (uygulamanın veritabanı çalışma rolü parolası dahil, `KR-118`; LOGO salt okunur hesabı, SMTP, NetGSM) | Geliştirme süresince | Geliştirici değiştiğinde veya makine el değiştirdiğinde silinir; sırlar ekrana ve depoya yazılmaz |
| BK-17 | Yerel API günlükleri | Geliştirici makinesi, `src/backend/Dsg.Hrms.Api/logs/` (`.gitignore`'da) | Bilgi İşlem | **Gizli**: yerel ortam gerçek veriyle çalışır (`KR-108`); kişisel veri maskelenir (#119) ama günlük yerel diskte durur | **90 dosya (gün)**: `RetainedFileCountLimit`, UAT ile aynı (ADR-0009) | Otomatik: eski dosya silinir |
| BK-18 | UAT şema betiği | `/opt/dsg-hrms/sema.sql` | Bilgi İşlem | Kurum içi: yalnızca veritabanı şeması, veri yok | Her dağıtımda üzerine yazılır (`deploy-uat.sh`) | Gerekmez |

> **#73:** `KR-058` taşımasında güvence için bırakılan eski volume'ler ve UAT'deki
> `/root/kr058-yedek-2026-09-25/` dizini (sır dosyasının bir kopyasını içeriyordu)
> silindi; issue kapandı. Bu defter, böyle geçici kopyaların **oluştuğu anda** kayda
> geçmesini sağlar (BK-13).

### 3.1 Kan grubu işleme koşulları

KVKK Sorumlusu kararı, 20.10.2026 (`KR-116`). Kan grubu özel nitelikli kişisel veridir (KVKK
md. 6, sağlık; `KR-113`). HRMS'te yalnızca aşağıdaki koşullarla işlenir:

| # | Koşul | Nasıl karşılanır |
|---|---|---|
| 1 | Kriptografik yöntemlerle şifrelenerek saklanır | Veritabanında kolon bazlı şifreleme (ADR-0009 §5); T1'de uygulanır, SYG-PERS'te doğrulanır |
| 2 | Veriye erişen personelden gizlilik taahhüdü alınır | Laboratuvarda çalışan tüm personelin gizlilik sözleşmesi bulunuyor. Kan grubunu yalnızca İK, Sistem Yöneticisi ve kişinin kendisi görür (PG-PERS-36) |
| 3 | Kimin ne zaman eriştiği değiştirilemez kayıtta tutulur | Erişim kaydı (BK-19) değiştirilemez (`KR-060`, `KR-094`); kart görüntülemesi erişim kaydına yazılır (PG-PERS-28) |

## 4. Depo dışındaki çalışma dosyaları

| # | Bilgi kümesi | Konum | Sahibi | Gizlilik | Saklama | İmha |
|---|---|---|---|---|---|---|
| BK-20 | LOGO kart çelişkileri listesi | `Masaüstü\HRMS\DSG-HRMS_LOGO_Kart_Celiskileri_Listesi.xlsx` | Bilgi İşlem; listeyi İK kullanır | **Gizli**: ad, sicil, iletişim bilgisi | İK'nın düzeltmesi bitip yeniden ölçüm yapılana kadar | Yeniden ölçümden sonra silinir; yeni liste gerekirse yeniden üretilir. **Veri düzeltme listesi** (`…_Personel_Veri_Duzeltme_Listesi.xlsx`, 22.09.2026) İK düzeltmeleri bittiği için 14.10.2026'da silindi (Doğuş Uçanok'un onayı, #215) |
| BK-21 | Modül gereksinim çalışma dosyaları | `Masaüstü\HRMS\DSG-HRMS_*_Gereksinimleri.xlsx`, `…_Sistem_Yonetimi_Parametreleri.xlsx` | Bilgi İşlem | Kurum içi | Onaylı içerik depoya (TEC.2) alınınca çalışma kopyasıdır | Modülün gereksinimleri depoda onaylandıktan sonra silinebilir. T3 (Kimlik) dosyası, içerik depoda onaylı ve T3 kabul edildiği için 14.10.2026'da silindi (#215) |
| BK-22 | UAT izin listesi | `Masaüstü\HRMS\DSG-HRMS_UAT_Izin_Listesi.txt` | Bilgi İşlem | **Gizli**: e-posta adresleri ve telefon numaraları | Kabul oturumları sürdükçe; katılımcılar sonraki kabuller için listede kalır (`KR-106`) | Kabullerin tamamı bittiğinde veya kişi kurumdan ayrıldığında listeden çıkarılır |
| BK-23 | T3 İK inceleme seti | `Masaüstü\HRMS\T3_Kabul_IK_Inceleme\` | Bilgi İşlem | Kurum içi | Kabul kararına kadar | Diğer belgelerin asılları depodadır. Doldurulmuş kabul formunun kopyası 14.10.2026'da depoya alındı (BK-24); klasör silinebilir. Aslı silinmeden önce özeti depo kaydıyla karşılaştırılır |
| BK-24 | Doldurulmuş ve imzalı kabul formları | **Depo:** `TEC.11-gecerleme/kayitlar/<tarih>-<modül>-kabul-formu.docx` (doldurulmuş form; üst verisi temizlenmiş kopya) ve aynı adlı `.md` kaydı. Taranmış imzalı nüshalar proje sonunda kalite belgeleri imzalatılınca depoya konur (`KR-107`). T3: `2026-10-13-t3-kabul-formu.docx` | İK | Kurum içi: ad ve unvan | Sistemin ömrü boyunca (belgelendirme kanıtı) | — |
| BK-25 | Geliştirme yardımcısıyla yazışma arşivi | Geliştirici makinesi, `claude/` (`.gitignore`'da) | Bilgi İşlem | **Gizli**: kararların ve onayların ham kaydı; parolalar 05.10.2026'da temizlendi | Kararlar karar defterine, onaylar onay kaydına aktarıldıktan sonra arşiv olarak | Parolalar 05.10.2026'da temizlendi (Ö-1). Arşive yeni parola yazılmaz |
| BK-26 | Üst Yönetim onay formu (doldurulmuş) ve onay seti | **Depo:** `MAN.1-proje-planlama/kayitlar/2026-10-09-ust-yonetim-onay-formu.docx` (üst verisi temizlenmiş kopya; `KR-107`). Onay setinin diğer belgeleri depodaki Markdown asıllarından üretilmişti; set `Masaüstü\HRMS\Ust_Yonetim_Onay_Seti_2026-10\` | Bilgi İşlem | Kurum içi: ad, unvan | Sistemin ömrü boyunca (belgelendirme kanıtı) | Masaüstü seti, aslının özeti depo kaydıyla karşılaştırıldıktan sonra silinebilir |
| BK-27 | İK'dan gelen rapor istekleri | `Masaüstü\HRMS\Rapor_Onerileri\` (İK'nın istediği raporların görüntüsü) | Bilgi İşlem; kaynağı İK | Kurum içi | İlgili modülün (raporlama) gereksinimleri onaylanana kadar | Onaylı içerik depoya alınınca silinebilir |
| BK-28 | Görsel dosyalar | `Masaüstü\HRMS\duzen_logo.png` (kurum logosu; depodaki kopyası `assets/`, KÖ-08a), `Masaüstü\HRMS\Personel.png` (başka bir ürünün personel listesi ekranından tasarım örneği) | Bilgi İşlem | Kurum içi. `Personel.png` kurum personeline ait değildir, ama üçüncü kişilerin ad ve fotoğraflarını içerir; depoya girmez | Ön yüz tasarımında başvuru kaynağı olarak, ilgili ekranlar yapılana kadar | Kullanılmayınca silinir |

## 5. Açık işler

| # | İş | Neden | Sahibi |
|---|---|---|---|
| Ö-1 | ~~Yazışma arşivinden parolaların temizlenmesi; UAT sunucu parolasının değiştirilip SSH'nin anahtarla yapılması~~ **Kapandı (06.10.2026):** arşivden 24 parola temizlendi (UAT 21, LOGO, eski İK veritabanı, SMTP); SSH anahtarla; `root` parolası değiştirildi; parola girişi bilerek açık (`KR-102`, #174). Kalan: parola, yardımcının bu oturuma ait dökümünde duruyor; parola değiştirildiği için geçersiz | UAT `root` parolası her sunucu işinde yazışmaya düz metin olarak yazılıyor. Parola bu arşivde ve yardımcının oturum dökümlerinde kalıcı olarak duruyor. Parola girişi açık olduğu için (R-25) bu parola sunucuya tam erişim demektir | Bilgi İşlem |
| Ö-2 | ~~"Öneri" olarak işaretlenen saklama ve imha sürelerinin KVKK Sorumlusu tarafından onaylanması~~ **Kapandı (14.10.2026, `KR-109`):** KVKK Sorumlusu (Doğuş Uçanok) önerilen süreleri onayladı; denetim izi, erişim kaydı ve güvenlik olayları için 10 yıl belirlendi. Karar: `kayitlar/2026-10-14-kvkk-saklama-imha-onay-talebi.md` | — | KVKK Sorumlusu |
| Ö-3 | ~~İmzalı kabul formlarının (BK-24) saklanacağı konumun belirlenmesi~~ **Kapandı (14.10.2026, `KR-107`):** Konum depodur. Doldurulmuş formlar depoya alındı; taranmış imzalı nüshalar proje sonunda kalite belgeleriyle birlikte depoya konur | — | İK + Bilgi İşlem |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#131) | Bilgi İşlem |
| 2026-10-06 | 1.1 | Ö-1 kapandı; BK-25 güncellendi (#177) | Bilgi İşlem |
| 2026-10-10 | 1.2 | BK-26: Üst Yönetim onay formu (#127) | Bilgi İşlem |
| 2026-10-13 | 1.3 | BK-23, BK-24: T3 kabul formunun geçici yeri; Ö-3 açık; BK-22: katılımcılar listede kalır (`KR-106`) (#204) | Bilgi İşlem |
| 2026-10-13 | 1.4 | BK-12: yedek kopya bırakılmaz; eski kopya silindi (#207) | Bilgi İşlem |
| 2026-10-14 | 1.5 | BK-23, BK-24, BK-26: doldurulmuş formlar depoda; Ö-3 kapandı (`KR-107`, #213) | Bilgi İşlem |
| 2026-10-14 | 1.6 | Modül sonu karşılaştırmasında bulunan kümeler eklendi: BK-06 (`src_old/`, AS sonuna kadar), BK-16, BK-17, BK-18, BK-27, BK-28; BK-12'ye TLS anahtarı; BK-20 ve BK-21'den iki dosya silindi (#215) | Bilgi İşlem |
| 2026-10-14 | 1.7 | KVKK Sorumlusu kararı: "Öneri" ibareleri kaldırıldı; BK-19 denetim kayıtları 10 yıl; Ö-2 kapandı (`KR-109`, #215) | Bilgi İşlem |
| 2026-10-20 | 1.8 | BK-29: HRMS personel verisi, saklama süresi parametresi; §3.1 kan grubu işleme koşulları (KVKK Sorumlusu kararı, `KR-116`, #241) | Bilgi İşlem |
| 2026-10-21 | 1.9 | BK-12, BK-16: uygulamanın veritabanı çalışma rolü parolası (`KR-118`, #244) | Bilgi İşlem |
