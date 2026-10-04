# PR İnceleme Onayları — Geriye Dönük Kayıt

**Belge kimliği:** MAN.8-KYT-2026-10-04-ONAY
**Süreç:** MAN.8 — Kalite Güvence (`PA 2.2 (d)`: bilgi gözden geçirilir ve onaylanır)
**Kapsam:** 04.10.2026'ya kadar birleştirilen 65 Pull Request
**Hazırlayan:** Bilgi İşlem · **Onaylayan (incelemeleri yapan):** Doğuş Uçanok

## 1. Neden bu kayıt?

Projede PR'ları açan ve birleştiren aynı GitHub hesabıdır. GitHub, PR sahibinin kendi PR'ını
onaylamasına ("Approve") izin vermediği için **hiçbir PR'da GitHub inceleme kaydı oluşmadı**
(T3 süreç denetimi, bulgu T3-06).

İncelemeler yine de yapıldı: her PR, birleştirilmeden önce Doğuş Uçanok tarafından incelendi
ve onay yazılı olarak bildirildi. Bu yazışmalar depo dışındaki soru-cevap arşivinde
(`claude/claude_questions_answers/`, git'e girmez) tutuluyor. Arşiv, depoya alınmaması
gereken bilgiler de içerdiği için kayda yalnızca PR numarası, tarih ve arşiv dosya numarası
alınmıştır.

**Bundan sonrası:** Onay, birleştirmeden önce PR'a yorum olarak yazılır ve CI bunu denetler
(`KR-096`, CONTRIBUTING §5). Bu kayıt yalnızca geçmişi kapatır.

## 2. Yöntem

- **Arşivin taranması:** Arşivin 107 dosyası tarandı. "PR #n … onaylıyorum" biçimindeki cümleler PR numarasıyla eşlendi.
- **Karşılaştırma:** Sonuç, GitHub'daki birleştirilmiş PR listesiyle (`gh pr list --state merged`) karşılaştırıldı.
- **Sonuç:** 65 PR'ın 62'sinde açık onay cümlesi var. #34 "onay için uygun" diye onaylandı ve sonra birleştirildi. #36 için birleştirme bildirimi var. #2 için arşivde açık onay cümlesi bulunamadı; PR'ı kullanıcı birleştirdi.

## 3. Kayıt

| PR | Başlık | Birleştirme | Onay tarihi | Arşiv dosyası | Kanıt |
|---|---|---|---|---|---|
| #2 | ci(github): akış altyapısı ve dal koruma telafi kontrolleri | 2026-09-08 | 2026-09-09 | 13 | Açık onay cümlesi bulunamadı; birleştirme kullanıcı tarafından yapıldı |
| #4 | docs(veri-kalitesi): telefon kontrolleri ve bildirim istisnası | 2026-09-09 | 2026-09-09 | 14 | Açık onay |
| #6 | feat(iskelet): solution yapısı, paket yönetimi ve mimari testleri | 2026-09-09 | 2026-09-09 | 15 | Açık onay |
| #8 | feat(veri): veritabanı altyapısı, yapılandırma ve sır yönetimi | 2026-09-10 | 2026-09-10 | 17 | Açık onay |
| #10 | feat(denetim): loglama altyapisi ve kişisel veri maskeleme | 2026-09-10 | 2026-09-10 | 18 | Açık onay |
| #12 | feat(denetim): denetim izi altyapısı ve değiştirilemezlik koruması | 2026-09-10 | 2026-09-10 | 19 | Açık onay |
| #14 | feat(denetim): erişim kaydı altyapısı | 2026-09-10 | 2026-09-10 | 20 | Açık onay |
| #16 | feat(api): merkezî hata yönetimi ve Problem Details (RFC 9457) | 2026-09-10 | 2026-09-10 | 21 | Açık onay |
| #18 | ci(github): PR izlenebilirlik denetimi (milestone + issue bağlantısı) | 2026-09-10 | 2026-09-10 | 22 | Açık onay |
| #20 | feat(api): OpenAPI sözleşmesi ve derleme zamanı üretimi | 2026-09-10 | 2026-09-10 | 23 | Açık onay |
| #22 | feat(api): sağlık kontrolleri ve gözlemlenebilirlik (OpenTelemetry) | 2026-09-10 | 2026-09-10 | 24 | Açık onay |
| #24 | ci(kalite): kalite kapıları hattı | 2026-09-10 | 2026-09-10 | 25 | Açık onay |
| #26 | feat(frontend): iskelet ve OpenAPI tip üretimi | 2026-09-11 | 2026-09-11 | 26 | Açık onay |
| #28 | fix(frontend): geliştirme vekiline sağlık ucu eklendi | 2026-09-11 | 2026-09-11 | 27 | Açık onay |
| #30 | feat(docker): imajlar ve Compose yığınları (dev + UAT) | 2026-09-11 | 2026-09-11 | 29 | Açık onay |
| #32 | feat(frontend): ortak bileşenler | 2026-09-11 | 2026-09-11 | 30 | Açık onay |
| #34 | docs(33061): A1 aşama kapanış değerlendirmesi | 2026-09-12 | 2026-09-12 | 33 | Onay ("onay için uygun"), birleştirme sonra |
| #36 | fix(frontend): tsconfig içindeki baseUrl kaldırıldı | 2026-09-12 | 2026-09-12 | 33 | Birleştirme bildirimi |
| #38 | feat(uat): UAT ortamı kurulumu, runbook ve dağıtım betiği | 2026-09-16 | 2026-09-16 | 36 | Açık onay |
| #40 | fix(frontend): izleme kimliği güvenli olmayan bağlamda da üretiliyor | 2026-09-16 | 2026-09-16 | 36 | Açık onay |
| #43 | feat(uat): TLS (HTTPS) devreye alındı | 2026-09-17 | 2026-09-17 | 42 | Açık onay |
| #45 | ci(github): PR başlığı denetimi | 2026-09-17 | 2026-09-17 | 45 | Açık onay |
| #47 | docs(plan): modül başlangıç koşulu (İK gereksinim toplantısı) | 2026-09-18 | 2026-09-18 | 47 | Açık onay |
| #49 | docs(33061): A0-A1 süreç gözden geçirmesi ve öz değerlendirme | 2026-09-19 | 2026-09-19 | 49 | Açık onay |
| #51 | docs(plan): milestone düzeni WBS ile hizalandı | 2026-09-19 | 2026-09-19 | 49 | Açık onay |
| #53 | docs(tec2): yaklaşım belgesi ve toplantı kaydı şablonları | 2026-09-19 | 2026-09-19 | 49 | Açık onay |
| #55 | docs(33061): izlenebilirlik matrisi kuruldu | 2026-09-19 | 2026-09-19 | 49 | Açık onay |
| #57 | docs(karar): 21.09.2026 kararları ve KR-017'nin yürürlükten kaldırılma | 2026-09-21 | 2026-09-21 | 52 | Açık onay |
| #59 | docs(veri): LOGO veri kalitesi yeniden ölçüldü (21.09.2026) | 2026-09-21 | 2026-09-21 | 54 | Açık onay |
| #61 | docs(veri): LOGO ölçümü yenilendi; R-12 kapandı (22.09.2026) | 2026-09-22 | 2026-09-22 | 56 | Açık onay |
| #63 | docs(kimlik): T3 onaylı gereksinimleri ve toplantı kaydı | 2026-09-24 | 2026-09-24 | 60 | Açık onay |
| #65 | docs(kimlik): T3 sistem/yazilim gereksinimleri (TEC.3) | 2026-09-25 | 2026-09-25 | 64 | Açık onay |
| #68 | refactor: tanımlayıcılar İngilizce (KR-058) | 2026-09-25 | 2026-09-25 | 66 | Açık onay |
| #69 | refactor(uat): sunucuya bağlı adlar ve konteyner yolları (KR-058) | 2026-09-25 | 2026-09-25 | 68 | Açık onay |
| #70 | refactor(uat): volume adları İngilizce (KR-058) | 2026-09-25 | 2026-09-25 | 69 | Açık onay |
| #71 | ci(github): dal adı denetimi | 2026-09-25 | 2026-09-25 | 70 | Açık onay |
| #75 | feat(kimlik): kişi/istihdam modeli ve senkronizasyon motoru | 2026-09-26 | 2026-09-26 | 72 | Açık onay |
| #76 | feat(kimlik): LOGO bağlantısı ve yazma reddinin CI kanıtı | 2026-09-26 | 2026-09-26 | 73 | Açık onay |
| #78 | feat(kimlik): ortak e-posta ve ad çelişkisinde yalnızca aktif siciller | 2026-09-26 | 2026-09-26 | 74 | Açık onay |
| #80 | fix(uat): sır dosyasında LOGO bağlantı dizesi tek tırnak içinde | 2026-09-26 | 2026-09-26 | 74 | Açık onay |
| #82 | refactor: kod dosyası adları İngilizce | 2026-09-26 | 2026-09-26 | 75 | Açık onay |
| #84 | feat(kimlik): parametre deposu, hesap modeli ve hesap yaşam döngüsü | 2026-09-26 | 2026-09-26 | 78 | Açık onay |
| #86 | feat(kimlik): doğrulama kodu ve e-posta/SMS iletim altyapısı | 2026-09-27 | 2026-09-27 | 81 | Açık onay |
| #88 | feat(kimlik): üyelik akışı API'si ve parola politikası | 2026-09-27 | 2026-09-27 | 82 | Açık onay |
| #91 | feat(kimlik): üyelik ekranları | 2026-09-28 | 2026-09-28 | 86 | Açık onay |
| #94 | feat(kimlik): giriş ve oturum API'si | 2026-09-28 | 2026-09-28 | 87 | Açık onay |
| #96 | feat(kimlik): giriş ekranı ve istemci oturum yönetimi | 2026-09-29 | 2026-09-29 | 88 | Açık onay |
| #99 | feat(kimlik): parola sıfırlama ve oturum içinde parola değişikliği | 2026-09-29 | 2026-09-29 | 89 | Açık onay |
| #101 | feat(kimlik): eylem yetkisi altyapısı ve senkronizasyon uçları | 2026-09-29 | 2026-09-29 | 90 | Açık onay |
| #104 | fix(kimlik): yenilemede oturum kapanması ve sunucu saati farkı | 2026-09-29 | 2026-09-29 | 91 | Açık onay |
| #106 | feat(kimlik): İK hesap işlemleri ekranı | 2026-09-30 | 2026-09-30 | 92 | Açık onay |
| #108 | feat(kimlik): İK davet bağlantısı | 2026-10-01 | 2026-10-01 | 94 | Açık onay |
| #110 | feat(kimlik): parametre ekranı ve kurumsal logo | 2026-10-01 | 2026-10-01 | 95 | Açık onay |
| #112 | feat(kimlik): 2FA açılmadan önce etki uyarısı | 2026-10-01 | 2026-10-01 | 96 | Açık onay |
| #114 | feat(kimlik): periyodik ve ilk girişte parola değişimi (#113) | 2026-10-01 | 2026-10-02 | 97 | Açık onay |
| #116 | feat(kimlik): API'de HTTPS zorunluluğu (#115) | 2026-10-02 | 2026-10-02 | 99 | Açık onay |
| #118 | docs(33061): T3 sonu süreç denetimi ve performans ölçümü (#117) | 2026-10-02 | 2026-10-03 | 101 | Açık onay |
| #139 | fix(kimlik): davet bağlantısının jetonu açılışta kayboluyordu (#137) | 2026-10-03 | 2026-10-03 | 103 | Açık onay |
| #140 | feat(kimlik): kimlik olayları, günlük maskeleme ve istisna (#119) | 2026-10-03 | 2026-10-03 | 103 | Açık onay |
| #141 | fix(kimlik): girişte en kısa yanıt süresi kaldırıldı (#120) | 2026-10-04 | 2026-10-04 | 104 | Açık onay |
| #142 | ci(33061): her SYG'nin doğrulama kanıtı CI'da denetlenir (#122) | 2026-10-04 | 2026-10-04 | 105 | Açık onay |
| #143 | docs(33061): UAT performans ölçümü tamamlandı (#121) | 2026-10-04 | 2026-10-04 | 106 | Açık onay |
| #144 | docs(33061): T3 doğrulama raporu, G2 kaydı ve TEC.9 yaklaşımı (#123) | 2026-10-04 | 2026-10-04 | 106 | Açık onay |
| #145 | fix(ci): kapsam ölçümü süreç içi toplayıcıyla kararlı (#89) | 2026-10-04 | 2026-10-04 | 107 | Açık onay |
| #147 | test(kimlik): Playwright ile uçtan uca kimlik senaryoları (#146) | 2026-10-04 | 2026-10-04 | 107 | Açık onay |

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-04 | 1.0 | İlk oluşturma (#128) | Bilgi İşlem |
