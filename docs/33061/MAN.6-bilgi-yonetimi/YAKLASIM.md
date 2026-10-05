# MAN.6 — Süreç Yaklaşımı

**Belge kimliği:** MAN.6-YAK
**Süreç:** MAN.6 — Bilgi Yönetimi
**Son güncelleme:** 2026-10-05
**Karşıladığı öznitelik maddeleri:** `PA 2.1`: amaç, planlama, sorumluluk, kaynak ve arayüzler (§1–§4). `PA 2.2 (a)`: sürecin dokümante edilmiş bilgi gereksinimleri belirlenir (§5). `PA 2.2 (b)`: bu bilginin kontrol gereksinimleri belirlenir (§6).

> **Yaklaşım belgesi nedir?** Sürecin **nasıl işletildiğini** tanımlar: hangi bilgi
> üretilir, hangi biçimde, kim üretir, nasıl kontrol edilir. MAN.6 ayrıca diğer
> süreçlerin bilgisinin **nerede durduğunu, ne kadar saklandığını ve nasıl yok edildiğini**
> düzenler.

---

## 1. Sürecin amacı ve sınırı

Bilgi yönetimi, projenin ürettiği ve kullandığı bilginin bulunabilir, güncel, doğru kişilere açık ve gerektiği kadar saklanır olmasını sağlar. Gereği kalmayan bilgi de kontrollü biçimde imha edilir (TS ISO/IEC TS 33061, MAN.6).

| Bu süreç | Komşu süreç |
|---|---|
| Hangi bilgi nerede, sahibi kim, gizliliği, saklama ve imha | Bilginin sürümü ve değişiklik yolu (PR, etiket): MAN.5 |
| Belge kuralları: dil, kimlik, tarih, değişiklik geçmişi | Belgelerin içeriği: her sürecin kendisi |
| Depo dışındaki bilgi kümeleri (sunucu, masaüstü, arşiv) | Kişisel veri işleme kuralları: ADR-0009, KVKK Sorumlusu |

**Hedef (`PA 2.1 a`):** Proje bilgisinin tamamı bilgi kayıt defterinde yer alır. Her belge PR ile güncel tutulur. "Gizli" sınıftaki her kümenin saklama ve imha kuralı onaylıdır.

---

## 2. Süreç nasıl işletilir?

### 2.1 Belge üretilirken (her PR'da)

1. Belge, `00-DOKUMAN-HARITASI.md`'deki klasöre konur: süreç klasörü, kalıcı ürünler, `kayitlar/` (tarihli kayıt), `raporlar/` (tarihli rapor). Boş klasörler `.gitkeep` ile izlenir.
2. Belge harita §4'teki kurallara uyar:
   - Türkçe yazılır.
   - Kimlik şeması kullanılır (`PG-`, `SYG-`, `KR-`, `ADR-`, `R-`, `TS-`).
   - Tarih biçimi `YYYY-AA-GG`'dir.
   - Başta "Son güncelleme", sonda **Değişiklik Geçmişi** tablosu bulunur.
   - Sır ve gerçek kişisel veri yazılmaz.
3. Şablon varsa kullanılır: `docs/sablonlar/ADR-sablonu.md`, `docs/sablonlar/kabul-formu.md`, TEC.2 toplantı ve paydaş gereksinimi şablonları (`SABLON-*.md`).
4. Belge yalnızca PR ile değişir (MAN.5). Kararlar `docs/karar-kayit-defteri.md`'ye `KR-NNN` olarak, mimari gerekçeler ADR olarak yazılır.
5. ADR'de netleştirme yerinde yapılır ve ADR'nin geçmişine yazılır. Kararı tersine çeviren değişiklik ise yeni ADR ile yapılır (`KR-099`, `docs/adr/README.md` kural 2).

### 2.2 Yeni bir bilgi kümesi oluştuğunda

1. Depo dışında yeni bir küme oluşursa (sunucuda kopya, masaüstünde liste, yeni arşiv) **oluştuğu anda** `bilgi-kayit-defteri.md`'ye eklenir. Deftere şunlar yazılır:
   - konum ve sahibi;
   - gizlilik sınıfı (Genel / Kurum içi / Gizli);
   - saklama süresi;
   - imha yöntemi.
2. Kişisel veri içeren dosyalar depoya girmez. Bu dosyalar `Masaüstü\HRMS` klasöründe tutulur ve defterde kayıtlıdır (BK-20…BK-23).
3. Geçici kopyalar (örn. riskli işlem öncesi veritabanı kopyası) işi bitince silinir (`KR-098`, BK-13).

### 2.3 Saklama, yedekleme ve imha

- **Saklama süresi kararı:** Kişisel veri içeren kümelerin saklama süresini RACI'ye göre KVKK Sorumlusu belirler (`roller-ve-sorumluluklar.md`). Onay gelene kadar defterdeki süreler **"öneri"** olarak işaretlidir.
- **UAT veritabanı** düzenli yedeklenmez. Riskli işlemden önce elle kopya alınır ve işlem doğrulanınca silinir (`KR-098`). Kayıp olursa veri LOGO'dan yeniden çekilir.
- **CI kayıtları** 90 gün sonra GitHub tarafından silinir. Kalıcı özet her kabul adayında depoya alınır (#136, TEC.9 YAKLASIM §6).
- **İmha** defterdeki yönteme göre yapılır. Gizli sınıftaki bir kümenin silinmesi kayda geçer (örn. #73: eski volume'ler ve sır kopyası içeren dizin silindi, issue'da kayıtlı).

---

## 3. Roller (`PA 2.1 d`)

RACI'de MAN.6'nın sorumlusu ve hesap vereni Bilgi İşlem'dir. İK ve KVKK Sorumlusu danışılan, Üst Yönetim bilgilendirilen taraftır.

| Rol | Görev |
|---|---|
| Bilgi İşlem | Doküman haritası, bilgi kayıt defteri, belge kuralları, depo dışı kümelerin kaydı ve imhası |
| KVKK Sorumlusu | Kişisel veri saklama sürelerinin onayı |
| İK | İmzalı kabul formlarının saklanması (BK-24); kendisine sunulan belgelerin gözden geçirilmesi |
| Her PR sahibi | Değiştirdiği belgenin "Son güncelleme" ve değişiklik geçmişi satırı |

---

## 4. Kaynaklar ve arayüzler (`PA 2.1 e, f`)

| Kaynak / arayüz | Kullanım |
|---|---|
| GitHub özel depo | Belgelerin ve kayıtların ana yeri |
| UAT sunucusu | Gizli sınıf kümeler (BK-10…BK-14) |
| Geliştirici makinesi (`Masaüstü\HRMS`, `claude/`) | Depoya girmemesi gereken çalışma dosyaları ve yazışma arşivi |
| MAN.5 | Değişiklik yolu ve sürüm |
| Tüm süreçler | Kendi kayıt ve raporlarını haritadaki yere koyar |

---

## 5. Üretilen bilgi ve biçimi (`PA 2.2 a`)

| Bilgi | Yer | Biçim | Kim |
|---|---|---|---|
| Doküman haritası | `docs/00-DOKUMAN-HARITASI.md` | Süreç → klasör → kanıt tablosu; §4 belge kuralları | Bilgi İşlem |
| Bilgi kayıt defteri | `bilgi-kayit-defteri.md` | `BK-NN`: konum, sahip, gizlilik, saklama, imha; açık işler `Ö-N` | Bilgi İşlem |
| Şablonlar | `docs/sablonlar/`, TEC.2 `SABLON-*.md` | Doldurulacak iskelet | Bilgi İşlem |
| Karar kaydı | `docs/karar-kayit-defteri.md` | `KR-NNN`: tarih, karar, gerekçe, karar veren, durum | Bilgi İşlem |
| ADR dizini ve kuralları | `docs/adr/README.md` | Dizin; değişiklik kuralı (`KR-099`) | Bilgi İşlem |
| Bilgi yönetimi raporu | `raporlar/` | Tarihli rapor | Bilgi İşlem |

---

## 6. Bilginin kontrolü (`PA 2.2 b`)

- **Değişiklik yolu:** Depodaki her belge yalnızca PR ile değişir ve bir değişiklik geçmişi taşır (MAN.5).
- **Gizlilik:** Depo özeldir. "Gizli" sınıftaki kümeler (UAT veritabanı, sır dosyası, LOGO düzeltme listeleri, izin listesi, yazışma arşivi) depoya girmez. `gitleaks` sır sızıntısını CI'da yakalar.
- **Telif:** Lisanslı standartlar (`docs/TSE_ISO_IEC_TS_33061/`, `docs/TS_ISO_IEC_33020/`) `.gitignore`'dadır. Metinleri belgelere kopyalanmaz; yalnızca madde numarasıyla atıf yapılır (`R-11`).
- **Bilgi kaybına karşı:** `git stash -u` ve `git clean -d` kullanılmaz; ara çalışma dalda WIP commit'i olarak saklanır ve dallar sık sık GitHub'a gönderilir (`R-26`). 30.09.2026'daki klasör kaybı `git stash -u` ile oldu (`TEC.13-bakim/kayitlar/2026-10-04-t3-olay-kayitlari.md`).
- **Defterin güncelliği:** Modül sonu süreç denetiminde (MAN.8) defter, depo dışı konumlarla karşılaştırılır.

---

## 7. Sürecin çıktıları ile bu belgenin eşlemesi

| 33061 çıktısı (MAN.6) | Karşılığı |
|---|---|
| a) Yönetilecek bilgi belirlenir | Doküman haritası, bilgi kayıt defteri (§2.2) |
| b) Bilginin gösterim biçimleri tanımlanır | Harita §4 kuralları, kimlik şeması, şablonlar (§2.1) |
| c) Bilgi üretilir, saklanır, doğrulanır, sunulur ve imha edilir | PR akışı; defterdeki saklama ve imha sütunları (§2.3) |
| d) Bilginin durumu belirlenir | "Son güncelleme" ve değişiklik geçmişi; `KR` durum sütunu |
| e) Bilgi ilgili paydaşlara erişilebilir | Depo (Bilgi İşlem); İK'ya sunulan inceleme setleri (BK-23); kabul formu (BK-24) |

---

## 8. Açık noktalar

| # | Konu | Durum |
|---|---|---|
| 1 | **Ö-1:** Yazışma arşivinde (`claude/`, BK-25) düz metin sunucu parolası var. Parolanın değiştirilmesi ve SSH'nin anahtarla yapılması gerekiyor | Açık; Bilgi İşlem |
| 2 | **Ö-2:** "Öneri" saklama ve imha süreleri KVKK Sorumlusu onayını bekliyor. KVKK Sorumlusunun projedeki rolü de resmen bildirilmedi (RACI §7 iş 3) | Açık |
| 3 | **Ö-3:** İmzalı kabul formlarının saklanacağı yer belirlenmedi | T3 kabul oturumundan önce |
| 4 | Doküman haritası 24.09.2026'dan beri güncellenmedi. Bilgi kayıt defteri ve yaklaşım belgeleri haritada yok. Harita var olmayan `tamamlanma-tanimi.md` ve `kod-gozden-gecirme-kontrol-listesi.md` dosyalarını gösteriyor | Haritanın güncellenmesi gerekiyor |
| 5 | Paydaşlara hangi bilginin hangi kanalla sunulduğu kayda geçmiyor (örn. İK inceleme seti) | Dağıtım kaydı tanımlanmadı |
| 6 | Deponun ve geliştirici makinesindeki kümelerin planlı yedeği tanımlı değil (plan B5, B9 "bekliyor") | Açık |

---

## 9. Gözden geçirme

Bu belge ve bilgi kayıt defteri her modül sonu süreç denetiminde (MAN.8) gözden geçirilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 1.0 | İlk oluşturma (#130) | Bilgi İşlem |
