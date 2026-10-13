# Bulgular B — MAN.8, TEC.2, TEC.3, TEC.5, TEC.7

**Değerlendirme tarihi:** 2026-10-13
**Dönem:** 2026-10-02 (T3 sonu denetimi `MAN.8-SGR-2026-10-02`, `main` @ `4199526`) … 2026-10-13 (`main` @ `d354e94`, PR #205). T3, İK tarafından 13.10.2026'da kabul edildi (`KR-105`); `v0.2.0` = `149f6ee`.
**Dayanak:** TS ISO/IEC TS 33061 Madde 5 (çıktılar ve BP'ler) · TS ISO/IEC 33020 Madde 5.2.4 (PA 2.1 a–g, PA 2.2 a–e) · Ölçek N/P/L/F (33020 Madde 5.3)
**Yöntem:** Salt okuma. Depo (`main`, temiz), `git log 4199526..HEAD` (45 commit = 45 PR, #118–#205), `gh issue/pr` (salt okuma), CI kayıtları, `node .github/scripts/requirement-traceability-check.mjs` (13.10.2026: ✓, çıkış 0). Kanıt yalnızca fiilen varsa sayıldı; "planlandı" ve YAKLASIM'daki "açık nokta" kanıt sayılmadı.
**Bağımsızlık:** Bu bir öz değerlendirme destek çalışmasıdır; 33020 anlamında bağımsız değerlendirme değildir. Ek, geliştirme yardımcısı (Claude) tarafından hazırlanmıştır; değerlendirilen PR'ların çoğunu da aynı yardımcı yazmıştır. Bağımsızlık 02.10'dakinden daha zayıftır (§2.2, BP1).

---

## 0. Özet tablo

| Süreç | PA 1.1 | PA 2.1 | PA 2.2 | Seviye | 02.10 durumu | Ana engel |
|---|---|---|---|---|---|---|
| MAN.8 Kalite güvence | **L+** | **L** | **L** | 1 | L− / P+ / L− | 02.10 Ek B önerilerinin ≈13'ü ve BULGU-10 issue'ya dönüşmedi; KG ölçütü ölçülmüyor; onay kaydı yalnızca bir anahtar sözcük |
| TEC.2 Paydaş gereksinimleri | **L+** | **L** | **L** | 1 | L+ / L / L | Kritik performans ölçütü PG'de hâlâ yok (f); değişiklik talebi yordamı yazılı değil, İK onayı ikinci elden |
| TEC.3 Sistem/yazılım gereksinimleri | **L+** | **L** | **L** | 1 | L+ / L− / L− | Uygulanan bir kural (#138) SYG'de yok; §1 özet, başlık tarihleri ve matris kaynak metinleri bayat, CI görmüyor |
| TEC.5 Tasarım tanımlama | **L** | **L−** | **L−** | 1 | L / P+ / P+ | SYG → tasarım tahsisi, diyagram ve tasarım gözden geçirmesi hâlâ yok; `KR-099` iki gün sonra yeniden ihlal edildi (#185, #191) |
| TEC.7 Gerçekleştirme | **L+** | **L** | **L** | 1 | L / L− / L− | Kapsam dışı bir öğe (#198) 16 gün ve beş kabul adayı boyunca fark edilmedi; DoD kayması ve PR boyutu sürüyor |

**Hiçbir süreç Seviye 2 değil.** Beşinin dördünde 02.10'a göre **gerçek ilerleme** var. İlerlemenin kaynağı 02.10 raporunun §6 iş planıdır: 18 düzeltici iş issue olarak açıldı (#119–#136) ve **tamamı 10.10'a kadar kapandı**. Bu, 18.09 raporunun önerilerinin unutulmasının tam tersidir ve MAN.8'in çalıştığının en güçlü kanıtıdır.

**Neden yine Seviye 2 değil?** Engel artık "süreç işletilmiyor" değil, **ikinci kademe bakım**dır. Kurallar yazıldı (KR-096, KR-099, MAN.8 YAKLASIM §2.3(5)), ama yazıldıktan birkaç gün sonra kural dışı örnekler yeniden üretildi ve otomatik denetim bunları görmedi:
- ADR-0006 §5 hâlâ eski sınırları (5/saat, 3/15 dk) gösteriyor; README dizini ADR-0006'nın son sürümünü göstermiyor.
- SYG §1 özet sayımları, üç belgenin "Son güncelleme" alanı ve matrisin üç kaynak metni bayat.
- 02.10 Ek B'deki ayrıntılı önerilerin ≈13'ü yalnızca YAKLASIM belgelerinin "açık noktalar" tablolarında duruyor; issue'ları yok.

---

## 1. Değişiklik talepleri #185 ve #190 — denetimli yol (istenen özel kontrol)

TEC.2 YAKLASIM §4 ve PG-KMLK başlığı, kesinleşmiş gereksinimin değişiminin `tur:degisiklik-talebi` issue'su açmasını ister. TEC.3 YAKLASIM §4 ise anlamı değiştiren SYG değişikliği için **önce** değişiklik talebi ve İK onayını şart koşar.

### 1.1 Zaman çizelgesi (UTC; yerel saat = UTC+3)

| Adım | #185 (üyelik ve kod gönderim sınırları 10) | #190 (2FA kullanıcı tercihine bağlı) |
|---|---|---|
| Talep issue'su | 10.10 10:32, `tur:degisiklik-talebi` ✔ — gövde: "İK'nın onayı **kabul oturumunda** kayda geçer" | 10.10 13:10, `tur:degisiklik-talebi` ✔ — gövde: "İK onayı alındı"; "ayrı bir kurtarma akışı **yok**" |
| Etki analizi | Issue'da etkilenen REQ (038, 039) ve değişmeyenler ✔ | Issue'da etkilenen REQ (025, 051, 052, 053) ve tasarım ✔ |
| Uygulama PR'ı birleşti | PR #186, 12:16 | PR #191, 21:48 (+5.319/−203; beş commit) |
| İK onayının kaydı | #187, 13:05 — PR birleştikten ve `v0.2.0-rc.3` UAT'ye kurulduktan **sonra** ("Doğuş Uçanok bildirdi") | Tercih için talep anında; kurtarma işlevi (SYG-KMLK-081) için PR içinde 21:13 ("Doğuş Uçanok bildirdi") |
| PG-KMLK | 1.3 (talep) + 1.4 (İK onayı) ✔; metinde üstü çizili eski değer ✔ | 1.5 + 1.6 ✔; REQ-025, 051, 052, 053 güncellendi ✔ |
| SYG-KMLK | Değişiklik gerekmedi (SYG-059 sayıyı parametreye bırakıyor) ✔ | 1.5: SYG-034, 035, 036 güncellendi; SYG-080, 081 eklendi ✔ |
| Matris | 2.9: satır 38, 39 kaynak metni ✔ | 2.10: SYG-080/081 eklendi ✔; **satır 51, 52, 53'ün kaynak özeti eski metinde kaldı** ✘ |
| Karar / ADR | KR yok; **ADR-0006 §5 tablosu hâlâ 5 deneme/saat ve 3 gönderim/15 dk** ✘ | `KR-104` ✔; ADR-0006 §7 gerçekleştirme notu ✔; README dizini güncellenmedi ✘ (§5) |
| Kabulde İK teyidi | T-03 "Uygun" (kabul formu §4.1, 13.10) ✔ | T-04 "Uygun" (kabul formu §4.1, 13.10) ✔ |
| Risk | — | R-27 (kurtarma) ✔ |

### 1.2 Değerlendirme

**Yol büyük ölçüde denetimliydi.** İki talep de doğru etiketli issue ile açıldı, etki analizi yazıldı, PG/SYG/matris aynı PR'da güncellendi ve kabulde İK'ya ayrıca gösterildi. Gereksinim izlenebilirlik betiği iki yeni SYG'nin eksik tablo satırını birleştirmeden önce yakaladı (PR #191 commit dizisi). 02.10'da yalnızca #77'nin izlediği yol bu kez kural olarak işledi. TEC.2(g) ve TEC.3 BP4 için bu belirgin bir ilerlemedir.

**Zayıflıklar:**
1. **Sıra:** #185'te uygulama birleşti ve UAT'ye kuruldu, İK onayı ≈50 dakika sonra kayda geçti. Bu, TEC.3 YAKLASIM §4'teki "önce İK onayı" kuralına aykırıdır. Etkisi düşüktür: UAT'dir ve aynı gün düzeldi. Ama kural yazılı olduğu hâlde ihlal edildi.
2. **Onayın kanıtı ikinci elden:** Her iki talepte de İK onayı "Doğuş Uçanok bildirdi" diye kayda geçti; İK'dan gelen bir yanıt, e-posta numarası veya bilgi kayıt defteri (BK) atfı yok. İK'nın kendi kaydı ilk kez 13.10 kabul formunda var. Formun ıslak imzalı nüshası da henüz yok (kabul formu §6, BK-24).
3. **Kapsam genişlemesi:** #190'ın gövdesi "kurtarma akışı yok, kalan risk" diyordu. Kurtarma işlevi (SYG-081) PR açıkken eklendi ve İK onayı alındı, ama talep issue'su güncellenmedi. Issue, kapandığı hâliyle onaylanan kapsamı göstermiyor.
4. **Yordam yazılı değil:** Bu adımların (onayın nasıl ve hangi kanıtla kaydedileceği, kabulde teyit) hiçbiri TEC.2 veya TEC.3 YAKLASIM'da yok. Uygulama iyi, ama kişiye bağlı.

### 1.3 Tutarlılık sayımı (SYG-KMLK, PG-KMLK, matris)

| Kontrol | Sonuç |
|---|---|
| `requirement-traceability-check.mjs` | ✓ "her paydaş gereksinimi karşılanıyor; SYG §8 ve izlenebilirlik matrisi SYG §4 ile tutarlı; her sistem gereksinimi doğrulanmış" |
| SYG sayısı | §4'te 81 satır = §1 "81" ✔ |
| SYG §1 tür sayımları | §1: İşlevsel **46**, Test **71**, Parametreye bağlı **31**. §4'ten sayılan: İşlevsel **48**, Test **73**, Parametreye bağlı **32** ✘ (SYG-080, 081 eklenince güncellenmedi) |
| "Son güncelleme" alanları | SYG 2026-10-03 (geçmiş 1.5, 10.10) ✘ · PG-KMLK 2026-09-28 (geçmiş 1.6, 11.10) ✘ · matris 2026-10-03 (geçmiş 2.12, 13.10) ✘ · ADR README 2026-10-04 (geçmiş 0.3, 05.10) ✘ |
| PG §2 özet | "Değişiklikle onaylanan: 1 (REQ-026)". Kabulden önce altı gereksinim değişiklik talebiyle değişti; özet bunu göstermiyor ✘ |
| REQ-KMLK-053 kabul kriteri | Gereksinim "her birleşim" diyor, kabul kriteri hâlâ "her iki durum" ✘ (küçük) |
| Matris §5 kaynak özeti | Satır 38, 39 güncel; **51, 52, 53 eski metin** ✘ |
| Matris "kısmen" | 0 satır (02.10'da 26) ✔ — #133 ile temizlendi |
| Matris §6 kapsama | T3: 56/56 kaynaktan kabule kadar eksiksiz, %100 (13.10) ✔ |
| Kabul sütunu | 56/56 dolu, kabul formuna bağlı ✔ |

**Neden yakalanmadı?** Betik kimlikleri ve eşlemeleri denetliyor; sayımları, tarihleri ve metin özetlerini denetlemiyor. 02.10'da "bakım kuralı var, bakım denetimi yok" denen sorun, bu kez başka alanlarda (özetler, başlıklar, ADR'ler) tekrarlandı.

---

## 2. MAN.8 — Kalite güvence

### 2.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | KG yordamları tanımlanır ve **uygulanır** | **Tanım:** `MAN.8/YAKLASIM.md` (#130, 05.10); `KR-096`; CONTRIBUTING §1.1, §5, §6 (#128, #129). **Uygulama:** #148'den #205'e birleştirilen **36 PR'ın 36'sında** onay yorumu birleştirmeden önce var (`gh pr view --json comments`). `branch-protection-check.yml` ve `main-failure-check.yml` `main`'de çalışıyor; 11–13.10 arasındaki son 30 `main` çalışması başarılı (CI, sözleşme, dal koruma) veya beklendiği gibi atlandı (`main` kırmızı denetimi). **Zayıflıklar:** DoD kayması (§6.1); PR boyutu kuralı ne değiştirildi ne ölçülüyor (#191 +5.319, #140 +3.014); onay yorumu birleştirmeden 7–37 sn önce yazılıyor ve inceleme içeriği kayıtsız (§2.3). | **L** |
| b | Değerlendirme ölçüt ve yöntemleri tanımlanır | ADR-0011 §6 (KR-096 ile güncellendi); YAKLASIM §2.3 (modül sonu yöntemi, salt okuma, "planlandı kanıt değildir"); `00-OLGUNLUK` §2–§6. BULGU-10 (kapı sayısı tutarsızlığı) 18.09'dan beri açık ve issue'su yok. | **L+** |
| c | Ürün, hizmet ve süreç değerlendirmeleri yapılır | **Ürün:** CI her PR'da; SYG → test halkası artık CI'da (#122/PR #142); uçtan uca testler (#146). **Araç:** #89 kapsam ölçümü kararsızlığı kapandı (PR #145); #171 ile mimari testin "hiçbir şeyi denetlemeden geçtiği" bulundu ve düzeltildi (ADR-0016) — iyi bir araç değerlendirmesi örneği. **Süreç:** 02.10 denetimi ve bu denetim. T3 kapanış gözden geçirmesi ve alınan dersler (MAN.2) bu denetim sırasında çalışma kopyasında birleştirilmemiş taslaktı; kanıt sayılmadı. | **L+** |
| d | Sonuçlar ilgili paydaşlara iletilir | 02.10 raporu ve `00-OLGUNLUK` §6 aynı gün güncellendi (#118). İlk durum raporu Üst Yönetim'e sunuldu (MAN.2, 06.10; onay `KR-103`). Dönemsel KG raporu hâlâ yok (YAKLASIM §8-2). Kalite ölçütleri (inceleme oranı, bulgu kapanma süresi, kaçan hata) hiçbir yerde raporlanmıyor. | **L** |
| e | Olaylar çözülür | #120, #137, #138, #149, #175, #182, #198 aynı veya ertesi gün kapandı; `main` kırmızı kuralı ve denetimi var (#129). **Ancak:** #198'de "neden şimdiye kadar yakalanmadı?" sorusu cevaplanmadı (YAKLASIM §2.4 madde 2) ve `tur:hata` etiketi yok. | **L+** |
| f | Önceliklendirilmiş problemler ele alınır | 02.10 §6'daki 18 işin 18'i issue oldu (#119–#136) ve 03.10–10.10 arasında kapandı. BULGU-04, 05, 06, 07, 11 bu işlerle kapandı. **Eksik:** 02.10 Ek B'deki ayrıntılı önerilerden ≈13'ü issue'ya dönüşmedi (§2.5). Bunlar yalnızca TEC.5/TEC.7/MAN.8 YAKLASIM "açık noktalar" tablolarında duruyor. #72 (eğilim/envanter) 25.09'dan beri açık. MAN.8 YAKLASIM §2.3(5) "her bulgu bir issue olur" diyor; kural 05.10'da yazıldı ama geriye dönük uygulanmadı. | **L** |

**PA 1.1 = L+** (02.10: L−). Gerekçe: Yordam ilk kez fiilen işliyor (onay yorumu, düzeltici işlerin kapanması, tespit edici denetimler). F olmamasının nedeni (a)'daki DoD ve boyut kuralının işlememesi ve (f)'deki issue'suz bulgular.

### 2.2 BP kontrolü

| BP | Durum |
|---|---|
| BP1 Hazırlık (strateji, **bağımsızlık**) | Strateji yazılı ✔ (YAKLASIM). Bağımsızlık yok ve açıkça kabul edilmiş (R-23, YAKLASIM §3). Telafi kontrolleri tanımlı ✔. Bu denetimi kodun çoğunu yazan geliştirme yardımcısının hazırlaması bağımsızlığı daha da zayıflatıyor. İK kabulü (TEC.11) geliştiriciden bağımsız tek göz. **Kısmen** |
| BP2 Ürün değerlendirmesi | Güçlü ✔. BP2.2 "doğrulama/geçerlemenin yapıldığının izlenmesi": SYG → test CI'da ✔, T3 doğrulama raporu ✔, kabul ✔ |
| BP3 Süreç değerlendirmesi | 02.10 + 13.10 (modül sonu kuralı ilk kez tam çevrimde işledi) ✔. BP3.2 araç değerlendirmesi (#89, #171) ✔ |
| BP4 Kayıt ve raporlar | Raporlar ✔, onay kaydı ✔. Dönemsel KG raporu 0 |
| BP5 Olay/problem | Kayıt ve çözüm ✔. BP5.4 kök neden: #198'de eksik. BP5.6 eğilim: `main`'de kayıtlı bir eğilim analizi yok (MAN.2 alınan dersler taslağı birleştirilmemişti); #72 açık. BP5.9 kapanışa kadar takip: 02.10 §6 için ✔, Ek B için ✘ |

### 2.3 PA 2.1 / PA 2.2

- **PA 2.1 = L** (02.10: P+). (a) Hedefler YAKLASIM §1'de yazılı ✔. (c) Modül sonu denetimi planlandığı gibi yapıldı; 02.10 iş planı izlendi ve kapandı ✔. **Ancak** YAKLASIM'ın kendi hedefleri ("her bulgu bir issue") ölçülmüyor; KG ölçütü yok (§8-2). (d) Roller tanımlı, RACI onaylı (`KR-103`, 10.10) ✔. (e)(f) GitHub Actions ✔.
- **PA 2.2 = L** (02.10: L−). (a)(b) YAKLASIM §5–§6 ✔ (raporların değişmezliği kuralı dâhil). (c) Belgeler PR ile ✔. (d) Onay yorumu var ✔; ama:
  - Onay denetimi (`branch-protection-check.yml`) yalnızca `onaylıyorum` sözcüğünü ve zamanı arıyor.
  - PR'ı açan, yorumu yazan ve birleştiren aynı hesap. Yorumun inceleyen tarafından yazıldığı (KR-096 madde 3) GitHub kaydından ayırt edilemez.
  - Geriye dönük onay kaydı #114 ve #118'i "Açık onay" sayıyor. Oysa ikisinin onay tarihi birleştirme tarihinden sonra (#114: 01.10 → 02.10; #118: 02.10 → 03.10). Kayıt bunu belirtmiyor.
  
  (e) Bilgi kayıt defteri ✔ (#131).

### 2.4 02.10 bulgularının durumu (MAN.8)

| Bulgu / iş | Durum |
|---|---|
| T3-06 / MAN.8-1 Kayıtlı inceleme yok | ✅ Kapandı (#128, `KR-096`). 36/36 PR'da birleştirme öncesi onay yorumu; geçmiş için geriye dönük kayıt (§2.3'teki iki istisna hariç) |
| MAN.8-2 Ara süreç değerlendirmesi, §6 tablosu | ⚠️ §6, 02.10'da güncellendi; ara değerlendirme yapılmadı; bu denetim modül sonu denetimidir |
| MAN.8-3 18.09'un 6 takipsiz bulgusu | ⚠️ 04, 05, 06, 07, 11 iş planıyla kapandı (#125, #127, #130–#132). **BULGU-10 hâlâ açık ve issue'suz** |
| MAN.8-4 Tek DoD | ❌ Açık (YAKLASIM §8-6); issue yok |
| MAN.8-5 PR boyutu | ❌ Açık (YAKLASIM §8-3); issue yok; #191 +5.319 satır |
| MAN.8-6 #89, #72 | ⚠️ #89 kapandı (PR #145); #72 açık |
| MAN.8-7 Aylık KG raporu | ❌ Açık (YAKLASIM §8-2); issue yok |
| T3-07 Olaylar kayda geçmedi | ✅ Kapandı (#129; `main-failure-check.yml`) |
| T3-08 Yaklaşım belgeleri | ✅ Kapandı (#130; MAN.8, TEC.5, TEC.7 dâhil) |

### 2.5 Yeni bulgular ve iyileştirme işleri

| Öncelik | İş (issue boyutunda) |
|---|---|
| **Yüksek** | **MAN.8-8 — Açık bulguların issue'ya çevrilmesi.** 02.10 Ek B'de issue'su olmayan maddeler: MAN.8-4, 5, 7; TEC.2-2, 3; TEC.3-1 (onay alanı), 3, 5; TEC.5-3, 4, 5, 6; TEC.7-6. Bunlara BULGU-10 ve bu ekin yeni bulguları eklenir. Her biri için bir issue açılır **ya da** gerekçeli bir "kapatma/erteleme" kararı (`KR-`) yazılır. YAKLASIM "açık noktalar" tabloları issue numarasına bağlanır. Böylece MAN.8 YAKLASIM §2.3(5) geriye dönük de uygulanmış olur. |
| Orta | **MAN.8-9 — Onay kaydının anlamı.** (1) Onay yorumu sabit biçim taşır: "İnceledim ve onaylıyorum. İnceleme: §7 listesi / <kısa not>". 1.000 satırı aşan PR'da en az bir inceleme notu (ör. dosya bazında görülen risk) zorunludur. (2) Denetim betiği sabit biçimi arar. (3) Geriye dönük onay kaydına "#114 ve #118: onay birleştirmeden sonra" notu eklenir. |
| Orta | **MAN.8-10 — Etiket disiplini.** 04.10'dan sonra açılan 23 issue'nun 13'ünde `tur:` etiketi yok (#154, 157, 160, 174, 177, 179, 187, 192, 196, 198, 200, 202, 204). #185, #190, #154, #157, #160'da `surec:` etiketi yok. #196 `[DÜZELTİCİ]` ama `tur:duzeltici-faaliyet` değil; #198 `[HATA]` ama `tur:hata` değil. Düzeltici faaliyet ve hata sayımları bu yüzden yanlış çıkar. Önlem: başlık önekine göre `tur:` ve en az bir `surec:` etiketini denetleyen bir issue iş akışı. Eksik etiketler geriye dönük düzeltilir. |
| Düşük | **MAN.8-11 — KG ölçütleri.** MAN.8-7 ile birleştirilebilir. İlk üç ölçüt: PR başına onay yorumu oranı (bugün 36/36); bulgu → issue oranı; kaçan hata sayısı (insan tarafından bulunan hata: #137, #138, #198). Her modül sonu denetiminde ölçülür. |

---

## 3. TEC.2 — Paydaş ihtiyaç ve gereksinimleri

### 3.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Paydaşlar tanımlanır | `paydas-listesi.md` 07.09'dan beri değişmedi; gözden geçirme kaydı yok. P6 KVKK Sorumlusu AN-16 için hâlâ danışılmadı (SYG §6: ⏳). Kabuldeki dört personel katılımcının dördü de Bilgi İşlem'den (kabul formu §1); son kullanıcı kesimi temsil edilmedi. | **L+** |
| b | Kullanım bağlamı | Değişmedi (vizyon, toplantı kaydı §2). UAT'de İK ile yapılan denemeler (#185 gerekçesi) bağlam bilgisini zenginleştirdi, ama bir senaryo setine yazılmadı. | **L+** |
| c | Kısıtlar | Değişmedi | **F** |
| d | İhtiyaçlar tanımlanır | UAT deneyiminden yeni ihtiyaçlar talep olarak alındı (#185, #190); yeni modül ihtiyacı doğru kanaldan girdi (#189, Y4) | **F** |
| e | Önceliklendirilir, gereksinime dönüşür | PG-KMLK 1.3–1.6 ✔ | **F** |
| f | **Kritik performans ölçütleri** | Değişmedi: PG'de "Ölçüt" alanı yok (TEC.2-2 açık). Bu boşluğun etkisi görüldü: KPÖ-KMLK-1, UAT'de karşılanmayınca Bilgi İşlem kararıyla yeniden tanımlandı (AN-25, `KR-095`). PG düzeyinde ölçüt olmadığı için İK'ya gitmedi (§4.1). | **L−** |
| g | Paydaş mutabakatı | AN-23/AN-24 İK teyidi kabulde alındı (T-01, T-02 "Uygun", 13.10) — 02.10 TEC.2-1 kapandı ✔. #185/#190 talep yoluyla işlendi ve kabulde teyit edildi ✔. Zayıflık §1.2: onay ikinci elden, #185'te uygulamadan sonra; ıslak imzalı nüsha bekliyor. | **L+** |
| h | Destekleyici sistemler | UAT, izin listesi (`KR-083`, `KR-106`) | **F** |
| i | İzlenebilirlik | Değişiklikler issue'ya ve PG geçmişine bağlı; REQ → SYG CI'da ✔ | **F** |

**PA 1.1 = L+** (02.10: L+). F'nin önündeki tek ciddi engel (f). (a) ve (g) küçük zayıflıklar.

### 3.2 BP kontrolü

BP1 ✔ · BP2 ✔ · BP3 kısmen (senaryo seti yok; UAT bulguları senaryoya dönmedi) · BP4 ✔ · BP5 kısmen (ölçüt hâlâ TEC.3'te; AN-01 geri bildirimi kabulde verildi ✔) · BP6 ✔ mutabakat; baseline `v0.2.0` etiketiyle oluştu (`KR-097`, `KR-105`) ✔ — 02.10'daki "baseline yok" kapandı.

### 3.3 PA 2.1 / PA 2.2

- **PA 2.1 = L** (02.10: L). (a)(c) Değişiklik talepleri issue ile izlendi ✔. Gereksinim issue'su #62, modül kabul edildiği hâlde açık (CONTRIBUTING §5.2: kabule kadar açık kalır). (d) RACI onaylı (`KR-103`) ✔; ama İK onayını Bilgi İşlem sorumlusu iletiyor. (g) İK ↔ Bİ arayüzü: UAT denemeleri, prova, kabul ✔.
- **PA 2.2 = L** (02.10: L). (a)(b) `TEC.2/YAKLASIM.md` 18.09'dan beri gözden geçirilmedi. Kimlik şeması (`PG-<MODÜL>-nn` ↔ fiilî `REQ-KMLK-nnn`), "Ölçüt" alanı ve değişiklik talebi yordamı fiilî uygulamadan ayrık. (c) PG geçmişi ✔, üstü çizili eski metin ✔; başlık ve §2 özet bayat (§1.3). (d) Belge değişiklikleri onaylı PR'larla ✔. (e) BK-24 ✔.

### 3.4 02.10 bulgularının durumu (TEC.2)

| İş | Durum |
|---|---|
| T3-10 / TEC.2-1 AN-23, AN-24 İK teyidi | ✅ Kapandı (#135; kabul formu §4.1 T-01, T-02) |
| TEC.2-2 PG'ye ölçüt bağı | ❌ Açık; issue yok |
| TEC.2-3 YAKLASIM hizalama, paydaş listesi gözden geçirmesi, P6 danışması | ❌ Açık; issue yok |
| TEC.2-4 PG baseline etiketi | ✅ `v0.2.0` (`KR-097`, `KR-105`) |

### 3.5 Yeni bulgular ve iyileştirme işleri

| Öncelik | İş |
|---|---|
| **Yüksek** | **TEC.2-5 — Değişiklik talebi yordamı.** TEC.2 YAKLASIM §4'e (TEC.3 YAKLASIM §4 atıf yapar) yazılı adımlar eklenir: (1) `tur:degisiklik-talebi` + `surec:TEC.2` issue'su, etki analizi; (2) **İK onayının kaydı: tarih, onaylayan rol, kanal ve mümkünse İK'nın yazılı yanıtının BK numarası**; (3) PG/SYG/matris/ADR güncellemesi; (4) uygulama PR'ı ancak (2)'den sonra birleşir; (5) kabul planında T-xx teyidi. Kapsam PR sırasında genişlerse talep issue'suna "Kapsam değişikliği" bölümü eklenir (#190 örneği). Önlem: PR izlenebilirlik denetimi, `Closes` ettiği issue `tur:degisiklik-talebi` ise PG geçmişinde o issue için "İK onayı" satırını arar. |
| Orta | **TEC.2-6 — PG-KMLK bakım düzeltmesi.** Başlık tarihi (1.6 / 2026-10-11), §2 özetine "Değişiklik talebiyle değişen: 6 (REQ-025, 038, 039, 051, 052, 053; #185, #190)" satırı, REQ-053 kabul kriteri ("her birleşim"). Modül kabul edildiği için #62 kapatılır. |
| Orta | TEC.2-2 ve TEC.2-3 (02.10) issue olarak açılır (MAN.8-8). **TEC.2-2 bir sonraki modülün İK toplantısından önce** yapılmalıdır; yoksa (f) yine L− kalır. |

---

## 4. TEC.3 — Sistem/yazılım gereksinimleri

### 4.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Sistem/öğe tanımı | §2.2 API yolu düzeldi (#133, SYG 1.3) ✔. Sistem işlev modeli hâlâ yok (TEC.3-5). | **L+** |
| b | Gereksinimler ve kısıtlar | 81 SYG ✔. **Ancak #138'in getirdiği kural** (kişi kendi hesabını ve aktif kalan son sistem yöneticisinin hesabını pasife alamaz; PR #159) kodda ve testte var, **SYG'de yok**. AN kaydı da yok. SYG'nin kendi giriş kuralı: "paydaş gereksinimi olmadan eklenen davranış, onaylanmamış kapsamdır". Kural meşru ve REQ-036'dan türetilebilir, ama türetme yazılmadı. Matris yalnızca satır 36'ya "#138" notu düşüyor. | **L+** |
| c | Kritik performans ölçütleri | KPÖ-1…5 tanımlı ve 1–4 UAT'de ölçüldü (#121/PR #143; doğrulama raporu §3) ✔. KPÖ-6 devreye almadan sonra ölçülür. Gözlem: KPÖ-1 karşılanmayınca tanım değişti (AN-25, `KR-095`). Gerekçesi (2 çekirdek, SYG-049 alt sınırı) analizle sağlam; ama ölçüt paydaşa sunulmadı (TEC.2 f). | **F** |
| d | Gereksinimler analiz edilir | AN-25 ✔. AN-23/24 İK'ya geri bildirim verildi ✔ (BP3.3). #138 ve #198'in gereksinim boyutu analiz edilmedi: #138 için AN yok; #198 SYG-076'ya aykırılık olarak doğru tanındı. | **L+** |
| e | Destekleyici sistemler | §7 güncel (#133, #179) ✔ | **F** |
| f | Paydaş gereksinimlerine izlenebilirlik | 56/56; SYG-080, 081 iki yönde bağlı; CI ✔ | **F** |

**PA 1.1 = L+** (02.10: L+). Beş süreç içinde F'ye en yakın olanıdır. F için (b)'deki türetilmiş kuralın SYG'ye girmesi ve (a)'daki işlev modelinin ya üretilmesi ya da SYG §2–§3 ile karşılandığının yazılması yeterlidir.

### 4.2 BP kontrolü

BP1 ✔ · BP2 ✔ (BP2.4 risk ilişkili gereksinim: #138'in kuralı tam da budur — sistemin yönetilemez hâle gelmesi riski — ve analizde atlanmıştı) · BP3 ✔ (geri bildirim kabulde) · BP4: BP4.1 açık mutabakat — SYG için İK onayı gerekmiyor (RACI: TEC.3 A = Bİ), ama SYG'de onay alanı yok (TEC.3-1 açık). BP4.3 baseline ✔ (`v0.2.0`). BP4.2 değişiklik ✔ (#190).

### 4.3 PA 2.1 / PA 2.2

- **PA 2.1 = L** (02.10: L−). (a) ✔. (c) Belge durumu bu kez izlendi: §2.2, §7 düzeldi; değişiklik geçmişi 1.1–1.5 ✔. Başlık tarihi ve §1 sayımları kaçtı. (d) RACI onaylı ✔. (g) Geri bildirim döngüsü kabulde kapandı ✔.
- **PA 2.2 = L** (02.10: L−). (a)(b) YAKLASIM ✔ (25.09'dan beri değişmedi; değişiklik yordamı eksik, bkz. TEC.2-5). (c) Geçmiş ✔; iç tutarlılık ✘ (§1.3). (d) PR onay yorumu ✔; SYG onay alanı ✘. (e) ✔.

### 4.4 02.10 bulgularının durumu (TEC.3)

| İş | Durum |
|---|---|
| T3-01 SYG-060/061/062 doğrulanmadı | ✅ Kapandı (#119/PR #140; matris 2.1) |
| T3-02 SYG-077 karşılanmıyor | ✅ Kapandı (#120/PR #141, AN-25; UAT ölçümü PR #143: p95 0,279 sn / aynı anda 4,42 sn) |
| TEC.3-1 SYG onay alanı ve baseline | ⚠️ Baseline ✔ (`v0.2.0`); onay alanı ❌, issue yok |
| TEC.3-2 Bayat §2.2/§7, SYG-065 | ✅ Kapandı (#133) |
| TEC.3-3 SYG iç tutarlılık denetimi | ❌ Açık; issue yok — etkisi bu dönemde görüldü (§1.3) |
| TEC.3-4 KPÖ ölçümleri | ✅ Kapandı (#121) |
| TEC.3-5 İşlev modeli | ❌ Açık; issue yok |

### 4.5 Yeni bulgular ve iyileştirme işleri

| Öncelik | İş |
|---|---|
| **Yüksek** | **TEC.3-6 — Türetilmiş kural SYG'de.** #138'in kuralı AN-26 olarak analiz edilir ve SYG-KMLK-073'e eklenir (ya da yeni SYG-KMLK-082 açılır; REQ-KMLK-036). SYG'deki karşılığı olan testler anılır; matris satır 36 güncellenir. Kural: hata düzeltmesi yeni bir davranış getiriyorsa PR'da SYG/AN güncellemesi zorunludur (TEC.3 YAKLASIM §4'e satır). |
| Orta | **TEC.3-7 — Belge iç tutarlılığının otomatik denetimi** (02.10 TEC.3-3'ün genişletilmiş hâli). `requirement-traceability-check.mjs` şunları denetler: (1) SYG §1 sayımları §4'ten hesaplananla aynı; (2) matris §5 kaynak özeti PG metninin başıyla aynı; (3) her belgede "Son güncelleme" ≥ değişiklik geçmişinin son tarihi (SYG, PG, matris, ADR README). Bugün dört belgede düşer; düzeltme aynı PR'da yapılır. |
| Düşük | **TEC.3-8 — Ölçütün paydaşa sunulması.** KPÖ-KMLK-1'in iki eşiği (yayılmış 50 girişte < 1 sn; aynı anda 50 girişte < 5 sn) ilk İK toplantısında bilgi olarak sunulur ve toplantı kaydına geçer. Sonraki modülde TEC.2-2 ile birlikte PG'ye girer. |

---

## 5. TEC.5 — Tasarım tanımlama

### 5.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Her öğenin tasarım özellikleri | ADR-0006 §7 notu (`KR-104`), ADR-0009 §3.1 (`KR-094`), ADR-0016 (yeni). Diyagram hâlâ yok (YAKLASIM §8-4). | **L** |
| b | SYG'lerin öğelere tahsisi | Değişmedi: SYG §2.2 öğe düzeyi; SYG → öğe tablosu yok (TEC.5-3) | **L−** |
| c | Tasarım etkinleştiricileri | Değişmedi | **F** |
| d | Öğeler arası arayüzler | OpenAPI + sözleşme denetimi ✔; T3 entegrasyon raporu dış arayüzleri anlatıyor (TEC.8, #134). ADR-0016 modüller arası 13 bağımlılığı gerekçesiyle bildiriyor ve testle doğruluyor ✔ | **L+** |
| e | Alternatifler değerlendirilir | ADR-0016'da alternatifler tablosu ✔; `KR-104`'te gerekçe ✔. #190'daki tasarım kararı (kurtarma) PR içinde alındı | **L** |
| f | Tasarım ürünleri geliştirilir | Kodla aynı PR'da (YAKLASIM §2.3 bunu bilinçli tercih olarak yazdı) ✔. Tasarım gözden geçirme raporu yok | **L** |
| g | Destekleyici sistemler | ✔ | **F** |
| h | Tasarım ↔ mimari varlık izlenebilirliği | **İlerleme:** ADR-0016 ile modül (mimari varlık) ↔ kod bağımlılığı birebir ve testle izleniyor (`LayerDependencyTests.AllowedModuleDependencies`). SYG düzeyinde tasarım izlenebilirliği hâlâ yok. `docs/mimari/modul-listesi-ve-bagimliliklar.md` (09.09) ADR-0016'ya atıf yapmıyor. | **L−** |

**PA 1.1 = L** (02.10: L).

### 5.2 BP kontrolü

BP1 ✔ (YAKLASIM, #130) · BP2 kısmen (tahsis) · BP3 ✔ · BP4: BP4.1 gerekçe ✔; BP4.2 izlenebilirlik kısmen; BP4.3 tasarım durumu — README "Son değişiklik" sütunu eklendi ✔, ama ADR-0006 için bayat; BP4.4 baseline `v0.2.0` ✔. Tasarım değerlendirme raporu hâlâ yok.

### 5.3 ADR güncellemeleri ve `KR-099` (istenen özel kontrol)

| Kontrol | Sonuç |
|---|---|
| Kural konuldu mu? | ✔ `KR-099` (04.10, #133); README kural #2 yeniden yazıldı; README'ye "Son değişiklik" sütunu ve değişiklik geçmişi eklendi |
| ADR-0002 → ADR-0016 | ✔ Örnek uygulama: yeni ADR yazıldı; ADR-0002'nin durumu "ADR-0016 ile değiştirildi" oldu; README'de iki satır da güncel. Kuralın doğru işlediği vaka |
| ADR-0006 §7 notu (`KR-104`, #191) | ⚠️ Not eklendi ve değişiklik geçmişine 1.1 yazıldı ✔. **Ancak:** (1) README dizini ADR-0006'yı hâlâ "2026-10-01 (1.0)" gösteriyor ✘. (2) ADR başlığındaki "Karar defteri karşılığı" listesinde `KR-092` ve `KR-104` yok ✘. (3) Sınıflandırma tartışmalıdır. KR-104, 2FA'nın **kime uygulandığını** değiştiriyor (herkes → yalnızca kendi açan). §7'nin üç kuralından biri olan "kanalı olmayanların açmadan önce listelenmesi" de yerini başka bir kurala bırakıyor. Bu "netleştirme"den çok kararın kısmen tersine çevrilmesidir. KR-099 madde 3 ("belirsizse yeni ADR") yeni ADR'yi ya da en azından notta neden "genişletme" sayıldığının gerekçesini isterdi. §7'nin eski kural metni olduğu gibi duruyor; okuyan, notu görmezse yanlış kuralı okur |
| ADR-0006 §5 (#185) | ✘ Hız sınırı tablosu hâlâ "5 deneme / saat" ve "3 gönderim / 15 dakika". #185 için ne ADR notu ne `KR-` var. Onaylı gereksinim ve kod 10'dur |
| ADR-0009 §3.1 (`KR-094`) | ✔ Gerçekleştirme notu, geçmiş ve README (0.3) uyumlu |
| ADR-0011 §6 (`KR-096`) | ✔ |
| ADR-0016 modül bağımlılıkları | ✔ 13 bağımlılık; tablo ve ADR metni aynı sayıyı veriyor; "boş geçme" ve "bayat tablo" testleri var. Karşılıklı `Identity ↔ Personnel` bağımlılığı kalıcı ödün olarak yazılı. Zayıflık: modülün "dış yüzeyi" tanımsız (ADR'nin kendisi kabul ediyor) |

### 5.4 PA 2.1 / PA 2.2

- **PA 2.1 = L−** (02.10: P+). (a) YAKLASIM §1.2 hedefleri ✔. (c) Hedeflerden ikisi ("tasarım kararı kodla aynı PR'da", "mimari kural testle korunur") tutuldu. Üçüncüsü (`KR-099`'un tam uygulanması) iki gün içinde iki kez kaçtı. Tasarım gözden geçirmesi yok. (d) Rol tanımlı, tek kişi (R-05, R-23).
- **PA 2.2 = L−** (02.10: P+). (a)(b) YAKLASIM ✔, `KR-099` ✔. (c) README dizini ve ADR-0006 başlığı bayat ✘. (d) ADR değişiklikleri onay yorumlu PR'larla ✔. (e) ✔.

### 5.5 02.10 bulgularının durumu (TEC.5)

| İş | Durum |
|---|---|
| TEC.5-1 ADR yönetişimi | ⚠️ Kural ve dizin düzeldi (#133, #171); ADR-0006 için yeniden bayatladı (§5.3) |
| TEC.5-2 YAKLASIM | ✅ Kapandı (#130) |
| TEC.5-3 SYG → öğe tahsisi; REQ-042 tasarım hücresi | ⚠️ REQ-042 gerekçesi yazıldı (#133); tahsis tablosu ❌, issue yok |
| TEC.5-4 Mimari nitelikli KR'lerin ADR'ye alınması | ❌ Açık (YAKLASIM §8-5); issue yok |
| TEC.5-5 Diyagramlar, `entegrasyon-arayuzleri.md` | ❌ Açık; issue yok (TEC.8 raporu kısmen karşılıyor) |
| TEC.5-6 Tasarım gözden geçirme raporu | ❌ Açık; T3 bu rapor olmadan kabul edildi |

### 5.6 Yeni bulgular ve iyileştirme işleri

| Öncelik | İş |
|---|---|
| **Yüksek** | **TEC.5-7 — `KR-099`'un uygulanması.** (1) ADR-0006 §5 tablosuna #185 gerçekleştirme notu (varsayılan 10; aralıklar) ve değişiklik geçmişine 1.2. (2) ADR-0006 başlığına `KR-092`, `KR-094` (ilgiliyse), `KR-104`. (3) §7 için karar: ya "ADR-0017 — 2FA kullanıcı tercihine bağlı" yazılır ve §7 "Yerini aldı" notu alır, ya da §7 notuna "neden netleştirme sayıldı" gerekçesi eklenir ve eski kural metni üstü çizilir. (4) README dizini. Önlem: README "Son değişiklik" sütununun her ADR'nin değişiklik geçmişindeki son satırla aynı olduğunu denetleyen küçük bir CI adımı (TEC.3-7 ile aynı betikte). |
| Orta | TEC.5-3, 4, 5, 6 issue olarak açılır (MAN.8-8). En azından **TEC.5-6 (tasarım gözden geçirmesi) T4'ün ilk tasarım PR'ından önce** yapılmalıdır; yoksa PA 2.1 L'ye çıkamaz. |
| Düşük | **TEC.5-8** — `docs/mimari/modul-listesi-ve-bagimliliklar.md`'ye ADR-0016'ya ve `AllowedModuleDependencies` tablosuna atıf. Ölçülen bağımlılık tek kaynakta kalır. |

---

## 6. TEC.7 — Gerçekleştirme

### 6.1 Çıktılar

| # | Çıktı (kısa) | Kanıt | Derece |
|---|---|---|---|
| a | Gerçekleştirme kısıtları | YAKLASIM §2.2 ✔; ADR-0016 ile modül sınırı ilk kez testle korunuyor (#171) ✔ | **F** |
| b | Sistem öğesi gerçeklenir | 81/81 SYG gerçeklendi ve doğrulandı (betik ✓; doğrulama raporu); T3 kabul edildi (`KR-105`). **Ancak:** kabul öncesi üç hata insan gözüyle bulundu. #137 (davet jetonu) ve #138 (yönetici kendini pasife alabiliyordu) UAT'de, #198 (T5'e ait PRM-HSP-03 T3 ekranında) provada bulundu. #198'deki parametre #84 ile 26.09'da girmişti; G2 doğrulamasından (04.10) ve beş kabul adayından geçti. SYG-076'ya ("yalnızca T3 parametreleri") 16 gün aykırı kaldı. | **L+** |
| c | Paketlenir veya saklanır | `v0.1.0`, `v0.2.0-rc.1…rc.5`, `v0.2.0` açıklamalı etiketler ✔; CHANGELOG her etiketle eşleşen bölümle ✔ (#125/PR #156); UAT etiketten kuruluyor; dağıtım kaydı ✔ | **F** |
| d | Destekleyici sistemler | Playwright (#146), kararlı kapsam ölçümü (#89) ✔ | **F** |
| e | İzlenebilirlik | Matris "kısmen" 26 → 0 (#133); SYG → test CI'da (#122) ✔. Zayıflık: #138 kuralının SYG karşılığı yok (TEC.3-6); matris satır 51–53 kaynak metni bayat; satırlarda kod yolu hâlâ yok (TEC.7-6). | **L+** |

**PA 1.1 = L+** (02.10: L). F'ye yakın. Engel: kapsam dışı bir öğenin otomatik hiçbir kontrolde yakalanmadan kabul adayına girmesi (b) ve türetilmiş kuralın izlenmemesi (e).

### 6.2 BP kontrolü

BP1 ✔ (YAKLASIM) · BP2 ✔ (BP2.5 paketleme ✔; BP2.6 nesnel kanıt: PR "Nasıl doğrulandı" tabloları) · BP3.1 anomaliler hata issue'larında ✔ (#198'de kök neden yarım) · BP3.3 baseline ✔.

### 6.3 PR disiplini (istenen özel kontrol)

**Örneklem:** #140, #153, #159, #166, #172, #173, #186, #191, #199, #201, #205 (11 PR). Onay sayımı #148–#205 arasındaki tüm PR'ları kapsar (36).

| Kural | Sonuç |
|---|---|
| Başlık Conventional Commits | 45/45 ✔ (CI kapısı) |
| Dal adı `<tür>/<issue>-…` | 45/45 ✔ (CI kapısı) |
| `Closes #n` | 11/11 ✔ |
| Milestone | 45/45 ✔ |
| Onay yorumu birleştirmeden önce (`KR-096`) | **36/36 ✔** (#148–#205). Yorumla birleştirme arası 7–37 sn. PR'ı açan, yorumlayan ve birleştiren aynı hesap; GitHub "review" sayısı 0 (beklenen). Önceki 9 PR (#139–#147) ve #118 geriye dönük kayıtta; #118'in onayı birleştirmeden sonra |
| Tamamlanma Tanımı (şablon) | ✘ Şablondaki liste hiçbir örnekte olduğu gibi kullanılmamış; her PR kendi kısa listesini yazıyor. CONTRIBUTING §6'da "**atlanamaz**" denen "yetki sızıntısı testi" ve "maskeleme testi" maddeleri **11 PR'ın hiçbirinde yok**. Bunlara yeni izinli uç açan #191 de dâhil. Ürün tarafında #191'in yetkisiz erişim testi var (`AccountsApiTests`: yetkisiz kullanıcıyla `two-factor/reset`). Yani eksik testte değil, kayıtta. Uygulanmayan maddeler çoğunlukla üstü çizili ve gerekçeli (#186, #199) ✔ |
| PR boyutu (< 400) | ✘ #191 +5.319/−203 (63 dosya; ≈1.850 satırı üretilmiş migration tasarımcı dosyası). İki ayrı özelliği (kullanıcı tercihi + İK kurtarma) taşıyor; ikincisi PR açıkken eklendi. #140 +3.014, #172 +1.836 (belge) |
| CI kapıları | 11–13.10 arasındaki son 30 `main` çalışması: CI, API sözleşme ve dal koruma denetimleri başarılı; `main` kırmızı denetimi beklendiği gibi atlandı |
| Etiketler | §2.5 MAN.8-10 |

### 6.4 #198 ve #138 — kapsam ve gereksinim kontrol boşlukları

- **#198 (PRM-HSP-03):** Kök neden PR #199'da doğru yazılmış: parametre kataloğu Y4 listesinden toplu kurulmuş ve parametre bir işleve bağlanmamış. Eksik olanlar:
  - **"Neden şimdiye kadar yakalanmadı?"** sorusunun cevabı (MAN.8 YAKLASIM §2.4). Cevap: hiçbir kontrol kataloğu onaylı SYG'lerle karşılaştırmıyordu. G2 doğrulaması SYG → test yönünde çalışır, "fazladan öğe" yönünde çalışmaz.
  - **Önleyici denetim.** Yok; issue da yok. MAN.2 alınan dersler taslağında aynı öneri var, ama taslak denetim anında `main`'de değildi.
  - **Etiketler:** `tur:hata` ve `modul:` yok.
  - **Ölçüm:** Bugünkü katalogdaki 33 parametrenin 33'ü SYG-KMLK'de geçiyor (bu denetimde sayıldı). Yani önerilen test bugün geçer ve PRM-HSP-03'ü #84'te yakalardı.
- **#138:** Ürün hatası iyi kapatıldı: aynı gün, regresyon testli, kapsam dışı kalan durum (senkronizasyonla pasifleşme) PR'da açıkça yazılı. Ama getirdiği kural gereksinim zincirine girmedi (TEC.3-6).
- **Ortak örüntü:** Gereksinim zinciri **ileri yönde** (REQ → SYG → test) otomatik ve güçlü. **Geri yönde** (kod/katalog → SYG) kontrol yok. Fazladan öğe (#198) ve gereksinimsiz davranış (#138) bu yüzden ancak insan gözüyle bulunuyor.

### 6.5 PA 2.1 / PA 2.2

- **PA 2.1 = L** (02.10: L−). (a) YAKLASIM §1.1 hedefleri; dördünden üçü tutuldu (PR ile giriş, kapılar, SYG ↔ test, etiket + CHANGELOG) ✔. (c) İş paketleri issue ile; kabul adayı döngüsü (rc.1–rc.5) planlı ve kayıtlı ✔. PR boyutu kontrolsüz ✘. (d) Rol ve onay tanımlı ✔. (e)(f) ✔.
- **PA 2.2 = L** (02.10: L−). (a)(b) YAKLASIM ✔ (§6 (c) satırı yalnızca rc.1/rc.2'yi sayıyor, bayat). (c) Etiket, CHANGELOG, Git ✔. (d) Onay yorumu ✔; DoD kaydı kayıyor ✘. (e) BK ✔.

### 6.6 02.10 bulgularının durumu (TEC.7)

| İş | Durum |
|---|---|
| TEC.7-1 Matris temizliği, §6 ölçümü | ✅ Kapandı (#133; §6 T3 %100, 13.10) |
| TEC.7-2 SYG-060/061/062/065 | ✅ Kapandı (#119; SYG-065 testi CI denetiminde) |
| TEC.7-3 SYG → gerçekleştirme/test denetimi | ✅ Kapandı (#122/PR #142) |
| TEC.7-4 CHANGELOG, etiket | ✅ Kapandı (#125; `v0.2.0`) |
| TEC.7-5 YAKLASIM | ✅ Kapandı (#130) |
| TEC.7-6 Şablon örneği `REQ-KIMLIK-07`, matriste kod yolu | ❌ Açık (YAKLASIM §7-3, §7-4); issue yok |
| T3-09 PR boyutu ve DoD | ❌ Açık (§6.3) |

### 6.7 Yeni bulgular ve iyileştirme işleri

| Öncelik | İş |
|---|---|
| **Yüksek** | **TEC.7-7 — Geri yönde kapsam denetimi.** `ParameterCatalogTests`'e bir test eklenir: katalogdaki her `PRM-` kimliği, modülün SYG belgesinin "Parametre" sütununda geçmeli ve kodda en az bir yerde okunmalı. İkinci adımda aynı yaklaşım izinlere (`identity.*`) ve uç noktalara uygulanır (OpenAPI yolu → SYG). #198'e "neden yakalanmadı" cevabı ve `tur:hata` etiketi eklenir. |
| Orta | **TEC.7-8 — DoD'nin korunması** (02.10 MAN.8-4'ün uygulanabilir hâli). PR izlenebilirlik denetimi, PR gövdesinde CONTRIBUTING §6'nın iki "atlanamaz" maddesinin işaretli **veya** üstü çizili ve gerekçeli olarak bulunmasını ister. PR şablonu CONTRIBUTING §6 ile tek listeye indirilir; onay maddesi şablona eklenir. |
| Orta | **TEC.7-9 — PR boyutu kuralı.** CONTRIBUTING §5'teki kural gerçekçi bir eşikle yeniden yazılır: üretilen dosyalar (migration tasarımcısı, `openapi-v1.json`, `schema.ts`) hariç ≤ 1.500 satır. CI bu eşiği aşan PR'da uyarı verir ve PR'da gerekçe ister. Bir değişiklik talebi PR açıkken genişlerse ikinci özellik ayrı PR olur (#191 örneği). |
| Düşük | **TEC.7-10** — TEC.7 YAKLASIM §6 (c) satırı etiket listesi yerine "tüm etiketler: `git tag -l`" diye yazılır; TEC.7-6 issue olarak açılır. |

---

## 7. Süreçler arası gözlemler

1. **Birinci kademe düzeltme çalıştı, ikinci kademe bakım çalışmadı.** 02.10'un 18 düzeltici işi on günde kapandı. Bu dönemin bulgularının neredeyse tamamı aynı türden: bir kural ya da tablo düzeltildi, sonraki değişiklikte kopyalardan biri güncellenmedi. Örnekler: ADR README, ADR-0006 §5, SYG §1, PG §2, matris kaynak metni, başlık tarihleri. Ortak çare, gereksinim betiğine eklenecek birkaç ucuz tutarlılık denetimidir (TEC.3-7, TEC.5-7). 02.10'daki "bakım kuralı var, bakım denetimi yok" tespiti hâlâ geçerli; yalnızca yeri değişti.
2. **Gereksinim zinciri tek yönlü.** REQ → SYG → test otomatik. Kod/katalog → SYG denetlenmiyor. T3'ün kabul öncesi son üç insan bulgusunun ikisi (#138, #198) bu boşluktan geçti (TEC.7-7, TEC.3-6).
3. **"Açık nokta" tablosu issue'nun yerini aldı.** 05.10'da yazılan YAKLASIM belgeleri 02.10'un önerilerini dürüstçe "açık" diye listeliyor. Ama bu tablolar sahipsiz ve tarihsiz. MAN.8 YAKLASIM §2.3(5)'in amacı tam da bunu önlemekti (MAN.8-8).
4. **İK arayüzü güçlendi, kanıtı ikinci elden.** UAT denemeleri, prova, iki değişiklik talebi ve kabul, İK ↔ Bİ arayüzünün işlediğini gösteriyor. Ama 13.10'a kadar İK'nın tek bir doğrudan kaydı yok; her onay Bilgi İşlem sorumlusu üzerinden geliyor. Islak imzalı nüsha da ertelendi (TEC.2-5).
5. **Bağımsızlık.** Onay yorumu kanıt üretiyor, ikinci bir göz üretmiyor. Değerlendirmeyi de geliştirme yardımcısı hazırlıyor. 33020 anlamında bağımsız bir değerlendirmeye (ör. kurum içinden başka bir birim veya dış değerlendirici) gidilmeden PA 2.2 (d) ve MAN.8 BP1.2 L'nin üstüne çıkamaz. Bu bir eleştiri değil, tek kişilik düzenin bilinen sınırıdır (R-23).
6. **İyi uygulamalar (korunmalı):**
   - düzeltici iş planının issue'larla on günde kapatılması;
   - değişiklik taleplerinin PG geçmişi, SYG, matris ve kabul teyidiyle işlenmesi;
   - mimari testin "boş geçme"ye karşı kendini sınaması (ADR-0016);
   - kabul adayı etiketleri ve dağıtım kayıtları;
   - Bilgi İşlem provasının kabul oturumundan önce dört bulguyu kapatması.

---

## 8. Öncelikli iş listesi (birleşik)

| # | Öncelik | İş | Süreç |
|---|---|---|---|
| 1 | Yüksek | Açık bulguların (02.10 Ek B'den ≈13 madde + BULGU-10 + bu ek) issue'ya veya gerekçeli kapatma kararına çevrilmesi (MAN.8-8) | MAN.8, tümü |
| 2 | Yüksek | Değişiklik talebi yordamı: İK onayının kanıtı, sıra, kapsam değişikliği (TEC.2-5) | TEC.2, TEC.3 |
| 3 | Yüksek | Geri yönde kapsam denetimi: katalog/izin/uç nokta → SYG; #198 kök neden (TEC.7-7) | TEC.7, TEC.3 |
| 4 | Yüksek | #138 kuralının SYG'ye alınması (AN-26) ve "hata düzeltmesi yeni davranış getirirse SYG güncellenir" kuralı (TEC.3-6) | TEC.3 |
| 5 | Yüksek | ADR-0006 §5 ve §7, başlık, README; `KR-099` sınıflandırma kararı (TEC.5-7) | TEC.5 |
| 6 | Orta | Belge iç tutarlılığı ve README ↔ ADR geçmişi denetimi betikte (TEC.3-7, TEC.5-7 önlem kısmı) | TEC.3, TEC.5, MAN.8 |
| 7 | Orta | DoD'nin CI'da korunması; tek DoD (TEC.7-8 = MAN.8-4) | TEC.7, MAN.8 |
| 8 | Orta | PR boyutu kuralının yeniden yazılması ve ölçülmesi (TEC.7-9 = MAN.8-5) | TEC.7, MAN.8 |
| 9 | Orta | Onay yorumu biçimi ve büyük PR'da inceleme notu; geriye dönük kayıtta #114/#118 notu (MAN.8-9) | MAN.8, tümü PA 2.2(d) |
| 10 | Orta | Issue etiket denetimi ve eksik etiketlerin düzeltilmesi (MAN.8-10) | MAN.8 |
| 11 | Orta | PG-KMLK bakım düzeltmesi; #62'nin kapatılması (TEC.2-6) | TEC.2 |
| 12 | Orta | PG'ye ölçüt bağı — sonraki modülün İK toplantısından önce (TEC.2-2) | TEC.2, TEC.3 |
| 13 | Orta | T4'ün ilk tasarım PR'ından önce tasarım gözden geçirmesi; SYG → öğe tahsisi (TEC.5-6, TEC.5-3) | TEC.5 |
| 14 | Düşük | KG ölçütleri ve dönemsel KG raporu (MAN.8-11 = MAN.8-7) | MAN.8 |
| 15 | Düşük | KPÖ-1 tanımının İK'ya sunulması (TEC.3-8); modül listesi ↔ ADR-0016 (TEC.5-8); TEC.7 YAKLASIM §6, şablon örneği (TEC.7-10, TEC.7-6) | TEC.3, TEC.5, TEC.7 |

---

## Ek — Başlıca kanıt konumları

- `docs/33061/MAN.8-kalite-guvence/{YAKLASIM.md, kayitlar/2026-10-04-pr-onay-kaydi.md, raporlar/2026-10-02-t3-surec-denetimi*.md}`
- `docs/33061/TEC.2-paydas-ihtiyac-ve-gereksinimleri/paydas-gereksinimleri/PG-KMLK.md` (geçmiş 1.3–1.6)
- `docs/33061/TEC.3-sistem-yazilim-gereksinimleri/gereksinimler/SYG-KMLK.md` (geçmiş 1.1–1.5; 81 SYG)
- `docs/33061/izlenebilirlik-matrisi.md` (2.12, 13.10)
- `docs/33061/TEC.5-tasarim-tanimlama/YAKLASIM.md`, `docs/33061/TEC.7-gerceklestirme/YAKLASIM.md`
- `docs/adr/README.md` (0.3), `ADR-0002` (0.2), `ADR-0006` (1.1), `ADR-0009` (0.3), `ADR-0011` (0.3), `ADR-0016` (0.1)
- `docs/karar-kayit-defteri.md` (`KR-094`…`KR-106`)
- `docs/33061/TEC.11-gecerleme/{T3-kabul-plani.md, kayitlar/2026-10-13-t3-kabul-formu.md, raporlar/2026-10-13-t3-kabul-raporu.md}`
- Kanıt sayılmadı: `MAN.2-proje-degerlendirme-ve-kontrol/kayitlar/2026-10-13-t3-*` (denetim anında birleştirilmemiş taslak)
- `.github/scripts/requirement-traceability-check.mjs`, `.github/workflows/{branch-protection-check.yml, main-failure-check.yml}`, `src/backend/tests/Dsg.Hrms.Architecture.Tests/LayerDependencyTests.cs`, `src/backend/Dsg.Hrms.Application/Settings/ParameterCatalog.cs`
- GitHub: issue #62, #72, #119–#138, #171, #185, #187, #190, #196, #198; PR #118–#205 (onay yorumu: #148–#205'te 36/36)

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-13 | 1.0 | İlk sürüm: T3 modül sonu denetimi, Ek B | Bilgi İşlem |
