# Modül Listesi ve Bağımlılık Haritası

**Belge kimliği:** MIM-002
**Son güncelleme:** 2026-09-07
**İlgili süreç:** TEC.2 (Kapsam), TEC.5 (Tasarım), MAN.1 (Planlama)
**İlgili kararlar:** `KR-040`, `KR-045`, `KR-046`, `KR-050`, `KR-051`, `KR-052`, `KR-053`

---

## 1. Bu belgenin amacı

Sistem **35 modülden** oluşacaktır. Geliştirme sırası önceden belli değildir: her modülün
önceliği İK birimiyle yapılacak toplantıda netleşir ve **bir sonraki modülün hangisi
olacağı ancak bir önceki modül kabul edildikten sonra bilinir.**

Bu çalışma biçimi esneklik sağlar, ancak bir riski vardır: **ön koşulu tamamlanmamış bir
modül seçilebilir.** Örneğin İzin Yönetimi, onay akışı ve çalışma takvimi altyapısı
olmadan geliştirilemez; Performans Yönetimi, yetkinlik tanımları olmadan anlamlı çalışmaz.

Bu belge, o riski ortadan kaldırmak için **hangi modülün neye bağımlı olduğunu** gösterir.
İK toplantısı öncesinde bakılacak belge budur: *"Bu ay hangi modülleri seçebiliriz?"*
sorusunun cevabı buradadır.

---

## 2. Modül grupları

| Grup | Anlamı | Ne zaman yapılır | Adet |
|---|---|---|---:|
| **T — Temel** | Diğer her şeyin üzerine kurulduğu altyapı | **En başta**, sırayla | 5 |
| **Y — Yatay (enine kesen)** | Birden fazla modülün ortak kullandığı yetenek | Temelden sonra; **öncelikli** (`KR-053`) | 10 |
| **İ — İş modülü** | İK süreçlerini karşılayan modüller | İK önceliklendirmesine göre | 20 |
| | | **Toplam** | **35** |

---

## 3. Modül listesi

### 3.1 Temel modüller (T)

Bu beş modül **sırayla ve en başta** geliştirilir. İK önceliklendirmesine tabi
değildir; diğer tüm modüllerin ön koşuludur.

| # | Modül | Ön koşul | Göç kapsamı | Not |
|---|---|---|---|---|
| T1 | **Personel Yönetimi** | — | Yok (LOGO'dan gelir) | LOGO senkronizasyonu, kişi/istihdam modeli, veri kalitesi raporu |
| T2 | **Organizasyon Yönetimi** | T1 | Yok (LOGO'dan gelir) | Firma, şube, birim, hiyerarşi, yönetici ataması |
| T3 | **Kullanıcı Girişi ve Üyelik (Kimlik Yönetimi)** | T1 | Yok | **İlk teslim edilecek modül** (`KR-040`) |
| T4 | **Rol ve Yetki Yönetimi** | T2, T3 | Yok | Rol tanımı, izin atama, satır bazlı kapsam |
| T5 | **Kullanıcı Yönetimi** | T3, T4 | Yok | Hesap yönetimi, istisna hesap açma, pasifleştirme |

> **Önemli sıralama notu:** T3 (Üyelik) çalışabilmesi için **doğrulama kodu gönderimi**
> gerekir; bu da bildirim altyapısına bağlıdır. Bu nedenle **Y1'in gönderim altyapısı
> (e-posta + SMS) T3 ile birlikte gelir**; Bildirim Merkezi'nin kullanıcıya dönük yüzü
> (uygulama içi bildirim listesi, tercihler) daha sonra tamamlanır.
>
> Benzer şekilde T1 ve T2, teknik olarak T3'ten önce hazır olmalıdır — çünkü üyelik
> LOGO'dan senkronize edilmiş kişi verisi üzerinde kimlik eşleştirmesi yapar. Kullanıcıya
> **teslim edilen ilk modül T3'tür**, ancak T1 ve T2 onunla birlikte inşa edilir.

### 3.2 Yatay (enine kesen) modüller (Y)

Tek başına bir İK süreci karşılamazlar; başka modüllerin kullandığı yeteneklerdir.
**Bu gruba öncelik verilecektir** (`KR-053`): bir kez yazılırlar, 20 iş modülü aynı
altyapıyı kullanır.

| # | Modül | Ön koşul | Göç kapsamı | Kimler kullanır |
|---|---|---|---|---|
| Y1 | **Bildirim Merkezi** | T3 | Yok | Neredeyse tüm modüller |
| Y2 | **Denetim ve Erişim Kayıtları** | T4 | Yok | Tüm modüller — **temelle birlikte inşa edilir** |
| Y3 | **Referans Veri Yönetimi** | T4 | Kısmi | Tüm modüller — sistem genelinde kullanılan **temel tanımların** yönetimi |
| Y4 | **Sistem Yönetimi** | T4 | Kısmi | Yöneticiler (senkronizasyon, sistem sağlığı, parametreler) |
| Y5 | **İş Akışı Yönetimi (Workflow)** | T4, Y1 | Yok | İzin, Terfi, Disiplin, Varlık, İşten Çıkış, Pozisyon Değişikliği, Ödül, İSG |
| Y6 | **Delegasyon Yönetimi** | Y5 | Yok | Onay akışı olan tüm modüller (vekâlet) |
| Y7 | **Çalışan Belge Yönetimi** | T4, Y2 | Değerlendirilecek | Özlük, eğitim, sertifika, işe alım, disiplin, İSG |
| Y8 | **Raporlama ve Dışa Aktarma** | T4, Y2 | Yok | Tüm modüller |
| Y9 | **Dashboard** | İlgili iş modülleri | Yok | Tüm kullanıcılar — **veri üreten modüller hazır oldukça büyür** |
| Y10 | **Çalışma Takvimi Yönetimi** | T2, Y3 | Kısmi (dinî izin günleri) | **İzin Yönetimi**, Eğitim planlama, devamsızlık, raporlama |

> **Y2 (Denetim ve Erişim Kayıtları) ayrı bir "modül" gibi sıraya alınmamalıdır.**
> Denetim izi ve erişim kaydı, her modülün kendi geliştirmesinin parçasıdır (ADR-0009).
> Bu satır, altyapının **temelle birlikte** kurulduğunu ve sonrasında her modülün buna
> uyduğunu belirtir.
>
> **Y3 ile Y10 ayrı modüllerdir** (`KR-050`). Y3, sistem genelinde kullanılan temel
> tanımların (izin türleri, ünvanlar, belge türleri, iller vb.) yönetimidir. Y10 ise
> takvim ve çalışma düzeni mantığını taşır; kendi iş kuralları ve hesaplama mantığı
> vardır. Bkz. §6.

### 3.3 İş modülleri (İ)

İK önceliklendirmesine göre sıralanır.

| # | Modül | Ön koşul | Göç kapsamı |
|---|---|---|---|
| İ1 | **İzin Yönetimi** | T5, Y5, **Y10** | ✅ **Var** — 7.494 izin + 841 devir + 30 ücretsiz + 7 iş göremezlik |
| İ2 | **Eğitim Yönetimi** | T5, Y1, Y7 | ✅ **Var** — 2.508 personel eğitimi + 1.585 değerlendirme + 210 tanım |
| İ3 | **Sertifika Yönetimi** | T5, Y7, Y1 | ✅ **Var** — 603 sertifika |
| İ4 | **Yetkinlik Yönetimi** | T5, Y3 | ❌ Yok |
| İ5 | **Performans Yönetimi** | İ4, Y5 | ❌ Yok — mevcut sistemde hiç kullanılmamış |
| İ6 | **Terfi Yönetimi** | T2, Y5, İ4 | ❌ Yok |
| İ7 | **Pozisyon Değişikliği Yönetimi** | T2, Y5 | ❌ Yok |
| İ8 | **İşe Alım Yönetimi** | T2, Y5, Y7 | ❌ Yok |
| İ9 | **Aday Havuzu Yönetimi** | İ8, Y7 | ❌ Yok |
| İ10 | **Mülakat Yönetimi** | İ8, İ9 | ❌ Yok |
| İ11 | **Oryantasyon Yönetimi** | İ8, İ2 | ❌ Yok |
| İ12 | **Deneme Süresi Yönetimi** | İ11, Y5 | ❌ Yok |
| İ13 | **Disiplin Süreci Yönetimi** | Y5, Y7 | ❌ Yok |
| İ14 | **Ödül ve Takdir Yönetimi** | Y5, Y1 | ❌ Yok |
| İ15 | **Varlık Yönetimi (Zimmet ve İade)** | Y5, Y7 | ❌ Yok |
| İ16 | **Kurum İçi Duyuru Yönetimi** | T4, Y1 | ❌ Yok |
| İ17 | **İşten Çıkış Süreci Yönetimi** | Y5, İ15, Y7 | ❌ Yok |
| İ18 | **İşten Çıkış Görüşmesi Yönetimi** | İ17 | ❌ Yok |
| İ19 | **Tebrik Servisi** | T1, Y1 | ❌ Yok |
| İ20 | **İSG ve İş Kazası Yönetimi** | T5, Y7, Y5 | ❌ Yok — mevcut sistemde 2–3 kayıt |

---

## 4. Bağımlılık haritası

```
                         ┌──────────────────────────┐
   TEMEL (sırayla)       │ T1 Personel Yönetimi     │  ← LOGO senkronizasyonu
                         │ T2 Organizasyon          │
                         │ T3 Kimlik / Üyelik ★     │  ← ilk teslim
                         │ T4 Rol ve Yetki          │
                         │ T5 Kullanıcı Yönetimi    │
                         └────────────┬─────────────┘
                                      │
        ┌─────────────────────────────┼──────────────────────────┐
        ▼                             ▼                          ▼
  ┌────────────┐             ┌────────────────┐          ┌──────────────┐
  │ Y1 Bildirim│             │ Y3 Referans    │          │ Y2 Denetim   │
  │ Y7 Belge   │             │    Veri        │          │    Kayıtları │
  │ Y8 Raporlama│            │ Y4 Sistem Yön. │          │ (temelle     │
  └─────┬──────┘             └───────┬────────┘          │  birlikte)   │
        │                            │                    └──────────────┘
        │                            ▼
        │                   ┌──────────────────┐
        │                   │ Y10 Çalışma      │
        │                   │     Takvimi      │
        │                   └────────┬─────────┘
        └──────────┬─────────────────┘
                   ▼
          ┌─────────────────┐
          │ Y5 İş Akışı     │───▶ Y6 Delegasyon
          └────────┬────────┘
                   │
   ┌───────────────┼──────────────┬──────────────┬─────────────┐
   ▼               ▼              ▼              ▼             ▼
┌────────┐  ┌────────────┐  ┌────────────┐  ┌──────────┐  ┌──────────┐
│İ1 İzin │  │İ13 Disiplin│  │İ15 Varlık  │  │İ6 Terfi  │  │İ8 İşe    │
│(+Y10)  │  │İ14 Ödül    │  │İ20 İSG     │  │İ7 Pozisyon│ │   Alım   │
└────────┘  └────────────┘  └─────┬──────┘  └────▲─────┘  └────┬─────┘
                                   │              │             │
                                   ▼              │             ▼
                            ┌─────────────┐  ┌────┴─────┐  ┌───────────┐
                            │İ17 İşten    │  │İ4 Yetkin.│  │İ9 Aday    │
                            │    Çıkış    │  │İ5 Perfor.│  │İ10 Mülakat│
                            └──────┬──────┘  └──────────┘  │İ11 Oryant.│
                                   ▼                        │İ12 Deneme │
                            ┌─────────────┐                 └───────────┘
                            │İ18 Çıkış    │
                            │    Görüşmesi│
                            └─────────────┘

   Bağımsız (temelden sonra herhangi bir anda):
   İ2 Eğitim · İ3 Sertifika · İ16 Duyuru · İ19 Tebrik Servisi · Y9 Dashboard

   ★ = kullanıcıya teslim edilen ilk modül
```

---

## 5. İK toplantısı için seçim rehberi

Aşağıdaki tablo, **her aşamada hangi modüllerin seçilebilir olduğunu** gösterir. İK
toplantısına giderken bu tabloya bakılır; ön koşulu tamamlanmamış bir modül önerilmez.

| Aşama | Tamamlananlar | **Seçilebilir modüller** |
|---|---|---|
| **0** | — | T1–T5 (temel; önceliklendirmeye tabi değil) |
| **1** | Temel + Y1, Y2, Y3, Y4 | İ2 Eğitim · İ3 Sertifika · İ4 Yetkinlik · İ16 Duyuru · İ19 Tebrik · Y7 Belge · Y8 Raporlama · **Y10 Çalışma Takvimi** |
| **2** | Aşama 1 + **Y5 İş Akışı** | İ7 Pozisyon Değ. · İ13 Disiplin · İ14 Ödül · İ15 Varlık · Y6 Delegasyon |
| **3** | Aşama 2 + **Y10 Çalışma Takvimi** | **İ1 İzin Yönetimi** |
| **4** | Aşama 2 + İ4 Yetkinlik | İ5 Performans · İ6 Terfi |
| **5** | Aşama 2 + Y7 Belge | İ8 İşe Alım → İ9 Aday Havuzu → İ10 Mülakat → İ11 Oryantasyon → İ12 Deneme Süresi · İ20 İSG |
| **6** | Aşama 2 + İ15 Varlık | İ17 İşten Çıkış → İ18 Çıkış Görüşmesi |
| Sürekli | — | Y9 Dashboard (veri üreten modüller arttıkça genişler) |

> **Y5 (İş Akışı) darboğazdır.** 8 iş modülü ona bağlıdır. `KR-053` uyarınca yatay
> modüllere öncelik verileceği için Y5 erken yapılacaktır — hangi iş modülü seçilirse
> seçilsin muhtemelen gerekecektir.
>
> **İ1 (İzin Yönetimi) iki ön koşula birden bağlıdır:** Y5 İş Akışı ve Y10 Çalışma
> Takvimi. İK'nın en çok talep edeceği modül büyük olasılıkla İzin olduğu için, bu iki
> yatay modülün erken tamamlanması önceliklidir.

### 5.1 Zincirli modüller

Bazı modüller tek başına anlamlı değildir; birbirini takip eder. **Planlama daima
zincirin ilk halkasından başlar** (`KR-052`); zincirin ortasından bir modül seçilmez.

| Zincir | Modüller (sırayla) |
|---|---|
| **İşe alım zinciri** | İ8 İşe Alım → İ9 Aday Havuzu → İ10 Mülakat → İ11 Oryantasyon → İ12 Deneme Süresi |
| **Çıkış zinciri** | İ17 İşten Çıkış Süreci → İ18 Çıkış Görüşmesi |
| **Değerlendirme zinciri** | İ4 Yetkinlik → İ5 Performans → İ6 Terfi |

---

## 6. Y10 — Çalışma Takvimi Yönetimi

`KR-050` ile **ayrı bir modül** olarak ele alınmıştır. Referans Veri Yönetimi (Y3) içine
konmamasının gerekçesi: Y3, sistem genelinde kullanılan **temel tanımların** (izin
türleri, ünvanlar, belge türleri, iller vb.) yönetimidir; Y10 ise kendi **iş kuralları
ve hesaplama mantığı** olan bir modüldür.

### 6.1 Kapsamı

- Resmî tatil takvimi
- Dinî bayram günleri
- Yarım gün uygulamaları (arife vb.)
- Mesai şablonları (08:00–17:00 / 09:00–18:00 / 10:00–19:00)
- Cumartesi dönüşümlü çalışma düzeni (birim bazlı)
- Pazar günü açık olan şube bilgisi ve pazar mesaisi

### 6.2 İzin gün sayımı — ön bilgi

> **Bu bölüm ön bilgidir.** Kuralların tamamı, İzin Yönetimi ve Çalışma Takvimi
> modülleri geliştirilirken İK ile ayrıntılı olarak konuşulacaktır. Burada kaydedilme
> nedeni, **tasarımı doğrudan etkilemesidir.**

İK biriminin bildirdiği mevcut işleyiş:

| Gün türü | İzinden düşer mi? |
|---|---|
| Pazartesi – Cuma | ✅ Evet |
| **Cumartesi** | ✅ **Evet** — o hafta personelin çalışma cumartesisi olup olmadığına bakılmaksızın |
| **Pazar** | ❌ Hayır |
| **Resmî tatil** | ❌ Hayır |
| **Dinî bayram** | ❌ Hayır |

**Örnek (İK'nın verdiği):** Bir personel cumayı kapsayacak şekilde izin alırsa ve o hafta
cumartesi *"iş başı yapacağım"* demediyse, **cumartesi de izinli sayılır ve bakiyesinden
düşer.** O hafta cumartesi çalışma haftası olsun ya da olmasın, süreç böyle işler.

### 6.3 Bu kuralın tasarıma etkisi — önemli sadeleşme

Cumartesi dönüşümlü çalışma düzeni **izin gün hesabını etkilememektedir.** Bu, tasarım
açısından önemli bir sadeleşmedir:

| | Beklenen (karmaşık) durum | Gerçek durum |
|---|---|---|
| İzin gün hesabı | **Kişiye özel** çalışma takvimine bağlı | **Kurum genelinde ortak** takvime bağlı |
| Her personel için | Ayrı cumartesi düzeni hesaplanmalı | Gerek yok |
| Hesaplama | Personel × tarih matrisi | Tek takvim üzerinden |

Yani izin gün sayımı şu sade kurala indirgenir:

```
izin_günü = takvim_günleri − pazarlar − resmî_tatiller − dinî_bayram_günleri
```

Bu, hem hesaplamayı hem de test edilebilirliği belirgin biçimde kolaylaştırır. Kural
yine de **parametrik** tutulacaktır (`KR-011`); mevzuat veya kurum uygulaması değişirse
kod değişikliği gerekmemelidir.

Cumartesi dönüşüm düzeni, izin hesabına girmese de **devamsızlık takibi, mesai ve
raporlama** açısından yine de tutulmalıdır.

### 6.4 Bu bölümden doğan açık soru

> Personelin *"o cumartesi iş başı yapacağım"* beyanı sistemde nasıl kayda geçecek?
> İzin talebi formunda bir işaret mi olacak, ayrı bir bildirim mi, yoksa yöneticinin
> onayına mı bağlı olacak? Bu, İzin Yönetimi modülünün gereksinim toplantısında
> netleştirilecektir.

---

## 7. Açık maddeler

| # | Konu | Durum |
|---|---|---|
| A1 | Çalışma Takvimi'nin nerede ele alınacağı | ✅ **Kapandı** — ayrı modül olarak ele alınacak (`KR-050`, Y10) |
| A2 | İSG / İş Kazası modülü | ✅ **Kapandı** — kapsama alındı (`KR-051`, İ20) |
| A3 | **Anket Yönetimi** listede yok. Mevcut sistemde vardı ancak cevap tablosu boştu | İK ile teyit edilecek |
| A4 | Puantaj / PDKS entegrasyonu kapsam dışı | Teyit edildi |
| A5 | Y7 Çalışan Belge Yönetimi'nde mevcut sistemden dosya göçü olacak mı? | Modül geliştirilirken netleşecek |
| A6 | Mevcut sistemdeki `imza`, `varsayilan_personel_kartlari`, `ayarlar`, `sirket_faaliyetleri` tablolarının karşılığı | İlgili modül geliştirilirken değerlendirilecek |
| A7 | Cumartesi iş başı beyanının sistemde nasıl kayda geçeceği (§6.4) | İzin Yönetimi toplantısında netleşecek |

---

## 8. Kapsam büyüklüğü üzerine bir not

35 modül, bu proje için **büyük ama yönetilebilir** bir kapsamdır. Bunu mümkün kılan
tasarım kararları:

| Karar | Kapsam üzerindeki etkisi |
|---|---|
| Modüler monolit (ADR-0002) | Yeni modül = yeni klasör; altyapı yeniden kurulmaz |
| Yatay modüller (Y1–Y10) | Onay akışı, bildirim, dosya, raporlama, denetim, takvim **bir kez** yazılır; 20 iş modülü aynı altyapıyı kullanır |
| İzin modeli (ADR-0007) | Yeni modül = yeni izin tanımı; yetki modeli değişmez |
| Referans veri yönetimi (Y3) | Tanım listeleri kod değişikliği olmadan yönetilir |

> **Kritik sonuç:** Yatay modüller ne kadar sağlam kurulursa, sonraki 20 iş modülünün
> her biri o kadar hızlı çıkar. Y1–Y10'a harcanan zaman tek seferlik bir yatırım değil,
> **20 kez geri dönen** bir yatırımdır. `KR-053` bu nedenle alınmıştır: **önce altyapı,
> bir gecikme değil hızlanma tercihidir.**

Kapsam büyüklüğünün getirdiği süre riski `R-15` olarak kayıt altına alınmıştır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-07 | 0.1 | İlk oluşturma — 33 modül, bağımlılık haritası, seçim rehberi | Bilgi İşlem |
| 2026-09-07 | 0.2 | **Y10 Çalışma Takvimi** ve **İ20 İSG ve İş Kazası** modülleri eklendi (35 modül); izin gün sayımı ön bilgisi ve tasarım etkisi kaydedildi; zincir ve yatay öncelik kararları işlendi | Bilgi İşlem |
