# Mevcut İzin Süreci — Olduğu Gibi (AS-IS) Analizi

**Belge kimliği:** ANL-003
**Son güncelleme:** 2026-09-07
**Kaynak:** İK biriminin aktardığı mevcut işleyiş
**İlgili süreç:** TEC.2 (Paydaş İhtiyaç ve Gereksinimleri)
**İlgili modüller:** İ1 İzin Yönetimi, Y5 İş Akışı, Y10 Çalışma Takvimi

> **Bu belge bir gereksinim belgesi değildir.** Bugünkü işleyişi kayda geçirir ve
> yeni sistemin tasarımını etkileyecek noktaları işaretler. İzin Yönetimi modülünün
> **gereksinim toplantısında** tüm kurallar ayrıntılı olarak alınacak ve
> `TEC.2/paydas-gereksinimleri/PG-IZIN.md` belgesinde tanımlanacaktır.

---

## 1. Bugünkü akış

```
1. Personel  ──sözlü──▶  Yöneticisi                    (uygunluk alınır)
2. Personel  ──e-posta──▶ yillikizin@duzen.com.tr      (başlangıç + iş başı tarihi)
3. İK        ──▶ Tüm Personeller ekranı → personeli bul
4. İK        ──▶ İzin İşlemleri → Yıllık İzin Girişi
                 • tarih aralığı girilir
                 • gün sayısı OTOMATİK hesaplanır
                 • gün sayısı ELLE DEĞİŞTİRİLEBİLİR
5. İK        ──▶ Kaydet → İzin Formu oluşur
6. İK        ──e-posta──▶ Personel                     (form gönderilir)
7. Personel  ──▶ Formu yazdırır
8. Personel  ──▶ Yönetici(ler)ine ıslak imza attırır
9. Personel  ──▶ Kendisi imzalar
10. Personel ──fiziksel──▶ İK                          (imzalı form teslim)
```

**Adım sayısı:** 10 · **Kanal sayısı:** 4 (sözlü, e-posta, sistem, kâğıt)
**Veri girişini yapan:** İK (personel değil)

---

## 2. Gün sayısı hesabı — kural doğrulaması

İK'nın verdiği örnek:

| | Değer |
|---|---|
| Personelin bildirdiği izin başlangıcı | 07.09.2026 (Pazartesi) |
| Personelin bildirdiği iş başı tarihi | 14.09.2026 (Pazartesi) |
| İK'nın sisteme girdiği izin bitişi | 13.09.2026 (Pazar) |
| Sistemin otomatik hesapladığı gün sayısı | **6** |

**Doğrulama:** 07.09 – 13.09 arası 7 takvim günüdür. 13.09 Pazar olduğu için düşülür
→ **6 gün**. Bu, `docs/mimari/modul-listesi-ve-bagimliliklar.md` §6.2'de kaydedilen
kuralla **birebir örtüşmektedir**:

```
izin_günü = takvim_günleri − pazarlar − resmî_tatiller − dinî_bayram_günleri
```

Cumartesi (12.09) sayılmaktadır. Personel *"o cumartesi iş başı yapacağım"* dediyse
İK gün sayısını elle **6'dan 5'e** düşürmektedir.

> Bu örnek, kuralın doğru anlaşıldığını teyit eder. Hesaplama motoru bu senaryoyu
> birim testi olarak içerecektir.

---

## 3. Tasarımı etkileyen tespitler

Aşağıdakiler, yeni sistemin tasarımında karara bağlanması gereken noktalardır.
**Öneriler Bilgi İşlem tarafından yapılmıştır; kararlar İzin Yönetimi gereksinim
toplantısında alınacaktır.**

### 3.1 Onay, kaydın **sonrasında** ve kâğıt üzerinde alınıyor

Bugün izin kaydı sisteme girildikten sonra form basılıyor ve imzalar sonradan
toplanıyor. Yani **sistemdeki kayıt, onay tamamlanmadan önce oluşuyor.**

**Sonuçları:**
- Sistem "onaylanmış izin" ile "henüz onaylanmamış izin"i ayırt edemiyor.
- Yöneticinin sözlü uygunluğu **hiçbir yerde kayıtlı değil**; kim, ne zaman onayladı
  belli değil.
- İmzalı form fiziksel olarak İK'ya ulaşmazsa süreç yarım kalıyor ve bu görünmüyor.

**Öneri:** Yeni sistemde onay, kaydın **önünde** olmalı. İzin talebi `Talep edildi →
Onay bekliyor → Onaylandı / Reddedildi` durumları üzerinden ilerlemeli; bakiyeden
düşüm **onay tamamlandığında** gerçekleşmeli. Bu, Y5 İş Akışı modülünün doğrudan
karşıladığı bir ihtiyaçtır.

### 3.2 Gün sayısı elle değiştirilebiliyor — denetlenemeyen müdahale

Bugün tarih aralığı değiştirilmeden gün sayısı elle düzeltilebiliyor. Bu, cumartesi
iş başı beyanını karşılamak için kullanılan **pratik ama izlenemeyen** bir çözüm.

**Sonuçları:**
- Tarih aralığı ile gün sayısı arasındaki tutarlılık bozulabilir.
- Değişikliğin **neden** yapıldığı kayıtlı değil.
- Yanlışlıkla yapılan bir değişiklik fark edilemez; bakiye sessizce yanlış hesaplanır.

**Öneri — üç seçenek:**

| Seçenek | Açıklama | Değerlendirme |
|---|---|---|
| A | Gün sayısı **hiç değiştirilemez**; cumartesi iş başı beyanı ayrı bir alan olarak alınır ve hesaba otomatik yansır | **Önerilen.** Kuralı sisteme taşır, tutarlılığı garanti eder |
| B | Değiştirilebilir ama **gerekçe zorunlu** ve denetim kaydına yazılır | Kabul edilebilir ara çözüm |
| C | Bugünkü gibi serbest değiştirilebilir | Önerilmez — denetlenemez |

Seçenek A'da izin talebi formunda *"Cumartesi (12.09) iş başı yapacağım"* gibi bir
işaret bulunur; sistem gün sayısını buna göre hesaplar. Hem personelin beyanı kayda
geçer hem de hesap otomatik kalır.

### 3.3 İzin bitiş tarihi ile iş başı tarihi karıştırılabiliyor

Personel **iş başı tarihini** bildiriyor (14.09), İK ise **izin bitiş tarihini**
giriyor (13.09). Aradaki bir günlük dönüşüm elle yapılıyor.

**Öneri:** Yeni sistemde her iki tarih de ekranda **birlikte** gösterilmeli:
*"İzin bitişi: 13.09.2026 · İş başı: 14.09.2026"*. Personel hangi kavramla düşünürse
düşünsün doğru sonucu görür; en sık yapılan bir günlük kaydırma hatası ortadan kalkar.

### 3.4 Veri girişini personel değil İK yapıyor

Bugün talebi personel e-posta ile bildiriyor, sisteme **İK giriyor**. Vizyon
belgesindeki **H3 hedefi** (izin taleplerinin ≥ %95'inin personel tarafından girilmesi)
bu akışın tersine çevrilmesi anlamına gelir.

**Sonuç:** Bu, teknik olduğu kadar **alışkanlık değişimi** gerektiren bir dönüşümdür.
Devreye alma sırasında personel bilgilendirmesi ve kısa kullanım kılavuzu, teknik
geliştirme kadar önemlidir (TEC.10).

### 3.5 Dört ayrı kanal, tek süreç

Sözlü + e-posta + sistem + kâğıt. Sürecin hiçbir noktasında **uçtan uca izlenebilirlik**
yok: bir talebin hangi aşamada olduğu ancak ilgili kişiye sorularak öğrenilebiliyor.

**Öneri:** Tüm akış tek kanala taşınmalı. Personel talebin durumunu kendi ekranından
görebilmeli; yönetici bekleyen onayları tek listede görebilmeli.

### 3.6 Islak imza yerine ne geçecek?

Bugün formun ıslak imzalı fiziksel kopyası İK'ya teslim ediliyor. Yeni sistemde bunun
karşılığı netleştirilmelidir.

| Seçenek | Not |
|---|---|
| Sistem içi onay kaydı (kullanıcı + zaman damgası + IP) | Denetim izi olarak güçlüdür; iç süreçler için genellikle yeterlidir |
| Ek olarak imzalı PDF çıktısı üretme yeteneği | Denetim veya uyuşmazlık durumunda kâğıt istenirse kullanılır |
| Nitelikli elektronik imza (NES/KEP) | Yüksek maliyet ve altyapı; bu aşamada gerekli görünmüyor |

**Bu, hukuki bir sorudur ve teknik bir tercih değildir.** İK ve gerekirse hukuk/İK
mevzuatı sorumlusuyla netleştirilmelidir: *işveren, izin kaydını ıslak imza olmadan
saklayabilir mi?* (4857 sayılı İş Kanunu'nun izin kayıt yükümlülüğü ve Yıllık Ücretli
İzin Yönetmeliği'ndeki izin defteri/kayıt hükümleri bu sorunun kaynağıdır.)

---

## 4. Gereksinim toplantısına götürülecek sorular

| # | Soru | İlgili bölüm |
|---|---|---|
| S1 | Onay akışı kaç kademeli olacak? Birden fazla yöneticisi olan personelde **hepsinin** onayı mı, birinin onayı mı yeterli? | §3.1 |
| S2 | Gün sayısı elle değiştirilebilecek mi? (A / B / C seçenekleri) | §3.2 |
| S3 | Cumartesi iş başı beyanı sistemde nasıl alınacak? | §3.2 |
| S4 | Onay sonrası izin iptali veya değişikliği nasıl işleyecek? | — |
| S5 | Islak imza yerine ne geçecek? Hukuki gereklilik var mı? | §3.6 |
| S6 | İK, personel adına talep girebilecek mi? (istisna akışı) | §3.4 |
| S7 | Yönetici izindeyken onay yetkisi kime geçecek? (Y6 Delegasyon) | — |
| S8 | İzin bakiyesi hangi anda düşecek: talep anında mı, onay anında mı? | §3.1 |
| S9 | Geçmişe dönük izin girişi mümkün olacak mı? Sınırı ne? | — |
| S10 | Yıllık izin dışındaki türler (ücretsiz, mazeret, iş göremezlik, doğum vb.) aynı akıştan mı geçecek? | — |

---

## 5. Bu belgeden doğan açık işler

| # | İş | Sorumlu |
|---|---|---|
| 1 | İzin Yönetimi gereksinim toplantısında §4'teki soruların cevaplanması | İK + Bilgi İşlem |
| 2 | Islak imza yerine geçecek yöntemin hukuki teyidi | İK / mevzuat sorumlusu |
| 3 | Diğer izin türlerinin akışlarının belirlenmesi | İK |

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-07 | 0.1 | İK'nın aktardığı mevcut işleyişin kayda geçirilmesi ve tasarım tespitleri | Bilgi İşlem |
