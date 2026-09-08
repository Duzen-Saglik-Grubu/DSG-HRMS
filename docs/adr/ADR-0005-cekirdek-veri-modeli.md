# ADR-0005 — Çekirdek Veri Modeli: Kişi, İstihdam ve Tarih Farkındalığı

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-009`, `KR-010`, `KR-014`, `KR-043`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)

---

## Bağlam

Bu, projenin en kritik veri modeli kararıdır. Yanlış kurulursa sonradan düzeltilemez.

**LOGO verisi üzerinde yapılan ölçüm (2026-09-03):**

| Ölçüm | Değer |
|---|---:|
| Personel kartı (sicil) | 1.533 |
| Tekil TCKN (kişi) | 1.360 |
| **Birden fazla sicili olan kişi** | **140** |
| **Aynı anda birden fazla AKTİF sicili olan kişi** | **12** |

İK biriminin doğruladığı iki gerçek durum:

1. **Emekli olup çalışmaya devam eden personel.** LOGO'da mevcut kart "işten ayrılan"
   yapılır, yeni bir sicil koduyla yeni kart açılır. Aynı kişi, iki kart.
2. **Aynı kişinin farklı görevlerde ayrı sicillerle tanımlanması.** Bu 12 kişide
   kartlar **eş zamanlı olarak aktiftir**.

Tek tablolu bir personel modeli bu 12 kişiyi doğru temsil edemez: kişi mi yoksa kart mı
tekildir sorusuna cevap veremez, kıdem hesabı yanlış çıkar, kullanıcı hesabı çoğalır.

Ayrıca İK süreçlerinde geçmişe dönük sorular kaçınılmazdır: *"Mart ayındaki izni kim
onaylamıştı?"*, *"Geçen yıl bu personel hangi şubedeydi?"*, *"O tarihte ünvanı neydi?"*
Bugünkü durumu tutan bir model bu sorulara cevap veremez.

## Karar

### 1. Kişi (`person`) ve İstihdam (`employment`) ayrılır

```
person                          employment
──────────────────────          ──────────────────────────────
id                    1 ──── *  id
public_id                       person_id
national_id (TCKN) UQ           registry_code (LOGO sicil) UQ
first_name                      company_id
last_name                       hire_date
birth_date                      termination_date  (null = aktif)
gender                          is_active
                                logo_ref
```

| Varlık | Anlamı | Yaşam süresi |
|---|---|---|
| `person` | **Kişi.** TCKN ile tekil. Ömür boyu tek kayıt | Kalıcı |
| `employment` | **İstihdam dönemi.** Bir sicil kaydına karşılık gelir | Giriş–çıkış arası |

Bir kişinin **birden fazla eş zamanlı aktif istihdamı olabilir.** Model bunu kısıtlamaz.

**Kimlik eşleştirme kuralları:**
- Kişi eşleştirmesi **TCKN** üzerinden yapılır.
- İstihdam eşleştirmesi **sicil kodu** üzerinden yapılır.
- **TCKN'si olmayan LOGO kartı kişi olarak oluşturulmaz**, uyarı listesine alınır
  (`KR-043`).

### 2. Kullanıcı hesabı kişiye bağlanır, istihdama değil

```
person 1 ──── 0..1 user_account
```

Bir kişinin kaç sicili olursa olsun **tek hesabı** vardır (`KR-014`). Hesap, kişinin
**tüm aktif istihdamlarını** kapsar. Yetki kapsamı, kişinin aktif istihdamlarının
birleşimidir.

Üyelik ön koşulu: kişinin **en az bir aktif istihdamı** olmalıdır (`KR-015`). Tüm
istihdamları sonlandığında hesap otomatik olarak pasife düşer.

### 3. Değişen nitelikler tarih aralıklı tutulur

İstihdamın zaman içinde değişen nitelikleri `employment` tablosunda **tutulmaz**;
ayrı tarih aralıklı tablolarda tutulur (ADR-0004 §7 kuralları geçerlidir).

| Tablo | İçerik |
|---|---|
| `employment_assignment` | Şube, birim, görev/ünvan, çalışma şekli |
| `employment_manager` | Yönetici ataması (bkz. §4) |
| `organization_unit_history` | Birimin bağlı olduğu üst birim, adı, durumu |

Her kayıt `valid_from` / `valid_to` taşır. Böylece **"X tarihinde durum neydi"**
sorusu tek sorguyla cevaplanır.

### 4. Yönetici ataması bir graftır, ağaç değil

İK biriminin tanımladığı yapı:

> Bir birimde A (birim sorumlusu), B (birim teknik sorumlusu), C, D, E, F var.
> A'nın altına B, C, D, E atanmış. B'nin altına D ve E atanmış.
> **A → B, C, D, E görür. B → D, E görür.** F, aynı birimde olsa da birim dışı
> birine atanmış olabilir.

Bu yapının üç sonucu var:

1. Bir kişinin **birden fazla yöneticisi olabilir** (D ve E hem A'ya hem B'ye bağlı).
   Yapı bir ağaç değil, **yönlü graftır**.
2. Görüş kapsamı **yalnızca doğrudan atamalardır** (`KR-022`). A, D'yi B üzerinden
   değil, kendi doğrudan ataması sayesinde görür. **Özyinelemeli sorgu gerekmez** —
   düz kenar listesi yeterlidir, bu da performans avantajıdır.
3. Birim üyeliği ile yönetici ataması **farklı şeylerdir**. F'nin durumu bunu gösterir.
   Model ikisini ayrı tutar.

```
employment_manager
──────────────────────────
id
employment_id            → yönetilen (ast)
manager_employment_id    → yönetici (üst)
valid_from, valid_to
```

**Döngü kontrolü zorunludur.** A→B→C→A gibi bir zincir, ileride özyinelemeli bir
sorgu yazılırsa sonsuz döngüye yol açar ve yetki mantığını bozar. Atama kaydedilirken
döngü kontrolü yapılır ve engellenir.

### 5. Organizasyon yapısı

```
company (firma)  1 ─── *  branch (şube)  1 ─── *  organization_unit (birim)
```

Ölçülen hacim: **7 firma, 32 şube, 128 birim.** Sistem baştan **çok firmalı** olarak
tasarlanır; firma, yetki kapsamında ve raporlamada birinci sınıf bir boyuttur.

Organizasyon kayıtları da tarih aralıklıdır: şube açılır/kapanır, birim birleşir,
bağlılık değişir. Geçmişe dönük raporlar o tarihteki organizasyona göre üretilir.

### 6. LOGO referansı

`employment.logo_ref` alanı LOGO'daki kayda işaret eder, ancak **yabancı anahtar
değildir** — farklı veritabanındadır. Yalnızca izleme ve mutabakat amaçlıdır. HRMS'in
tüm iç ilişkileri kendi birincil anahtarları üzerinden kurulur (ADR-0003 §4).

## Gerekçe

- 12 kişinin eş zamanlı çoklu aktif sicili, `Kişi`/`İstihdam` ayrımını **kanıtlanmış
  bir zorunluluk** hâline getiriyor; bu bir varsayım değil, ölçüm sonucudur.
- Kıdem hesabı kişinin tüm istihdam dönemlerinin toplamına dayanır; ayrım olmadan
  emekli olup dönen personelin kıdemi yanlış hesaplanır.
- Tarih aralıklılık sonradan eklenemez: geçmiş veri zaten kaybolmuş olur. Baştan
  kurulmak zorundadır.
- Doğrudan atama modeli hem İK'nın gerçek çalışma düzenine uyuyor hem de özyinelemeli
  sorgu maliyetini ortadan kaldırıyor — nadir görülen bir "hem doğru hem hızlı" durumu.

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| Tek `personel` tablosu (mevcut sistem yaklaşımı) | 12 kişiyi temsil edemiyor; kıdem ve hesap mantığı bozuluyor |
| TCKN'yi birincil anahtar yapmak | TCKN kişisel veridir, yabancı anahtar olarak her tabloya yayılmamalı; ayrıca değişebilir (nadiren de olsa) |
| Hesabı sicil bazlı açmak | Aynı kişi için birden fazla hesap; izin bakiyesi ve kıdem bölünür |
| Yönetici hiyerarşisini ağaç (tek üst) olarak modellemek | D ve E'nin iki yöneticisi olan gerçek durumu temsil edemiyor |
| Özyinelemeli (tüm alt kırılım) görüş kapsamı | İK'nın tanımladığı düzene aykırı; ayrıca gereksiz sorgu maliyeti |
| Nitelikleri `employment` üzerinde tutup geçmişi denetim logundan okumak | Denetim logu raporlama için tasarlanmamıştır; sorgulanabilir değildir |

## Sonuçlar

**Olumlu:**
- Çoklu sicil, emeklilik sonrası dönüş ve çoklu görev durumları doğru temsil edilir.
- Geçmişe dönük her soru sorgulanabilir.
- Yetki kapsamı hem doğru hem performanslı hesaplanır.

**Olumsuz / kabul edilen ödünler:**
- Sorgular tek tablolu modele göre karmaşıktır. "Bugünkü personel listesi" gibi sık
  kullanılan görünümler için yardımcı sorgu/görünüm tanımlanacaktır.
- Kullanıcı arayüzünde "kişi" ve "istihdam" ayrımının kullanıcıya sezgisel biçimde
  sunulması gerekir; İK bu ayrımı teknik terimlerle görmemelidir.

**Yükümlülükler:**
- Döngü kontrolü ve çakışan tarih aralığı kontrolü birim testleriyle kapsanacaktır.
- Senkronizasyon, kişi sayısının kaynaktaki tekil TCKN sayısına eşit olduğunu
  doğrulayacaktır.

## Geri dönüş maliyeti

**Çok yüksek.** Bu model üzerine tüm modüller kurulacaktır. Değiştirmek yeniden yazım
anlamına gelir. Bu nedenle ölçüme dayandırılmış ve İK ile doğrulanmıştır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
