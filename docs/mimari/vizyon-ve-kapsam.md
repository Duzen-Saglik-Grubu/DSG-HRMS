# Vizyon ve Kapsam Belgesi

**Belge kimliği:** VK-001
**Proje:** DSG-HRMS — Düzen Sağlık Grubu İnsan Kaynakları Yönetim Sistemi
**Sürüm:** 0.1 (taslak — İK birimi onayı bekliyor)
**Son güncelleme:** 2026-09-07
**Belge sahibi:** Bilgi İşlem Birim Sorumlusu
**İlgili süreç:** TEC.2 — Paydaş İhtiyaç ve Gereksinimlerinin Tanımlanması
**33061 karşılığı:** *Life cycle concepts*, *Stakeholder identification* (kısmi)

> **Onay durumu:** Bu belge, projenin kapsamını ve hedeflerini tanımlar. **İK birimi ve
> üst yönetim tarafından onaylanmadan** kapsam kesinleşmiş sayılmaz. Onay sonrası
> değişiklikler değişiklik talebi olarak yönetilir (MAN.2, `R-04`).

---

## 1. Belgenin amacı

Bu belge şu sorulara cevap verir:

- Bu sistemi **neden** yapıyoruz?
- **Ne** yapacak, **ne yapmayacak**?
- Kimler kullanacak, kimler etkilenecek?
- Başarılı sayılması için hangi koşullar sağlanmalı?
- Hangi kısıt ve varsayımlar altında çalışıyoruz?

Modül düzeyindeki ayrıntılı gereksinimler bu belgede **yer almaz**; her modül için
İK birimiyle yapılacak toplantılarda ayrı ayrı toplanır ve
`docs/33061/TEC.2-paydas-ihtiyac-ve-gereksinimleri/paydas-gereksinimleri/` altında
kayıt altına alınır.

---

## 2. Mevcut durum ve problem tanımı

### 2.1 Bugünkü işleyiş

Kurumda İK süreçleri iki ayrı sistem üzerinden yürümektedir:

| Sistem | Kapsam | Teknoloji |
|---|---|---|
| **LOGO Bordro** | Personel ana verisi, bordro, ücret | MSSQL |
| **Mevcut HRMS** | Yıllık izin, eğitim, anket, sertifika | ASP.NET Core MVC + PostgreSQL 14 |

Mevcut HRMS, personel verisini LOGO'dan **canlı** olarak okumaktadır.

### 2.2 Tespit edilen problemler

| # | Problem | Kanıt |
|---|---|---|
| 1 | **Tek katmanlı, test edilemez kod yapısı.** Denetleyici, servis, veri erişimi ve model aynı projede, katman sınırı olmadan duruyor | `src_old` kod incelemesi |
| 2 | **Kimlik bilgileri kaynak kodda açık metin.** Veritabanı bağlantı dizeleri, SMTP ve SMS parolaları koda gömülü | `src_old` güvenlik taraması |
| 3 | **Veri modeli tarih farkındalığından yoksun.** "Geçen yıl bu personel hangi şubedeydi" sorusu cevaplanamıyor | Şema incelemesi |
| 4 | **Kişi ile personel kartı ayrımı yok.** 140 kişinin birden fazla sicili, 12 kişinin eş zamanlı birden fazla aktif sicili var | LOGO ölçümü, 2026-09-03 |
| 5 | **Canlı LOGO bağımlılığı.** LOGO erişilemediğinde İK sistemi tamamen duruyor; raporlar tekrarlanabilir değil | Mimari inceleme |
| 6 | **Yetersiz denetim izi.** Kimin hangi personelin verisini görüntülediği izlenmiyor | Şema incelemesi |
| 7 | **Kullanılmayan modüller.** Performans değerlendirme (0 kayıt), anket cevapları (0 kayıt), İSG (2–3 kayıt) yapılmış ama kullanılmamış | Veri envanteri |
| 8 | **Altyapı desteği sona eriyor.** PostgreSQL 14 desteği Kasım 2026'da bitiyor | Sürüm bilgisi |

Ayrıntılar: `docs/analiz/01-mevcut-sistem-veri-envanteri.md`

---

## 3. Vizyon

> **Düzen Sağlık Grubu'nun tüm insan kaynakları süreçlerini tek bir modern platformda
> toplayan; personelin kendi işlemlerini kendisi yapabildiği, yöneticilerin ekibini
> görebildiği, İK'nın manuel takip yükünden kurtulduğu; kişisel verileri mevzuata uygun
> koruyan ve en az 10–15 yıl sürdürülebilir bir sistem.**

Vizyonun üç ayağı:

1. **Self-servis.** Personel izin talebini kendisi girer, eğitim geçmişini kendisi görür.
   İK, veri girişi yapan değil, süreci yöneten birim olur.
2. **Tek doğruluk kaynağı.** İK verisi Excel dosyalarına dağılmaz; tek sistemde,
   geçmişiyle birlikte tutulur.
3. **Sürdürülebilirlik.** Sistem, onu yazan kişiden bağımsız olarak bakılabilir olur:
   yazılı kararlar, test kapsamı, güncel dokümantasyon.

---

## 4. İş hedefleri ve başarı ölçütleri

> **Not:** Aşağıdaki ölçütler Bilgi İşlem tarafından **önerilmiştir**. İK birimi ve
> üst yönetim tarafından teyit edilmesi gerekir; teyit sonrası MAN.2 kapsamında
> dönemsel olarak izlenecektir.

| # | Hedef | Ölçüt | Ölçüm yöntemi |
|---|---|---|---|
| H1 | **Veri kaybı olmadan geçiş** | Eski sistemden aktarılan izin, eğitim ve sertifika kayıtlarında **sıfır açıklanamayan fark** | Göç mutabakat raporu (TEC.10) |
| H2 | **Personelin sistemi benimsemesi** | Devreye almadan sonraki 2 ay içinde aktif personelin **≥ %90'ı** hesap açmış olmalı | Kullanıcı hesabı raporu |
| H3 | **İK'nın manuel yükünün azalması** | İzin taleplerinin **≥ %95'i** sisteme personel tarafından girilmiş olmalı (İK tarafından değil) | Talep kaynağı raporu |
| H4 | **Süreç şeffaflığı** | İzin talebinin oluşturulmasından sonuçlanmasına kadar geçen ortalama süre ölçülebilir ve izlenebilir olmalı | Sistem raporu |
| H5 | **Mevzuat uyumu** | KVKK kapsamında erişim ve işlem kayıtlarının eksiksiz tutulması; veri sahibi başvurusuna cevap verilebilmesi | Denetim kaydı raporu |
| H6 | **Süreç olgunluğu** | **TS ISO/IEC TS 33061 Seviye 2** kapsamındaki 15 sürecin tamamında PA 1.1 = Tam, PA 2.1 ve PA 2.2 ≥ Büyük Ölçüde | Öz değerlendirme tablosu |
| H7 | **Sürdürülebilirlik** | Kod kapsamı eşiklerinin sürekli sağlanması; tüm mimari kararların yazılı olması | CI kapsam raporları, ADR dizini |

---

## 5. Paydaşlar

Ayrıntılı liste: `docs/33061/TEC.2-paydas-ihtiyac-ve-gereksinimleri/paydas-listesi.md`

| Paydaş | İlgisi | Karar yetkisi |
|---|---|---|
| **İnsan Kaynakları Birimi** | Ana kullanıcı; gereksinim kaynağı; kabul mercii | **Yüksek** — modül kapsamı ve kabulü |
| **Bilgi İşlem Birimi** | Geliştirme, işletme, bakım, teknik kararlar | **Yüksek** — teknik kararlar |
| **Personel (≈584 aktif)** | Self-servis kullanıcı | Düşük — dolaylı (İK üzerinden) |
| **Birim sorumluları / teknik sorumlular** | Ekibini görüntüleme, izin onayı | Orta |
| **Üst yönetim** | Bütçe, öncelik, raporlama | **Yüksek** — kapsam ve takvim |
| **KVKK Sorumlusu** | Kişisel veri uyumu, saklama süreleri | Orta — uyum kararları |
| **Mali İşler / Bordro** | LOGO'nun bütünlüğü; bordro süreci | Orta — LOGO erişim kuralları |
| **Belgelendirme kuruluşu** | 33061 Seviye 2 değerlendirmesi | Dış — kriterleri belirler |

---

## 6. Kullanıcı sınıfları

| Sınıf | Yaklaşık sayı | Kullanım sıklığı | Teknik seviye | Temel ihtiyaç |
|---|---:|---|---|---|
| **Personel** | ~584 | Ayda birkaç kez | Düşük | İzin talebi, izin bakiyesi, eğitim geçmişi, kendi bilgileri |
| **Birim sorumlusu / teknik sorumlu** | ~50 (tahmin) | Haftalık | Düşük–orta | Ekibini görme, izin onayı, ekip raporları |
| **İK uzmanı** | ~5 (tahmin) | Günlük, yoğun | Orta | Tüm modüller, veri girişi, raporlama, dışa aktarma |
| **İK yöneticisi** | 1–2 | Günlük | Orta | Raporlama, onay, kural tanımlama |
| **Sistem yöneticisi** | 1–2 | Gerektikçe | Yüksek | Kullanıcı/rol yönetimi, senkronizasyon, sistem sağlığı |

> **Tasarım sonucu:** Kullanıcıların büyük çoğunluğu **düşük teknik seviyeli ve seyrek
> kullanıcıdır.** Arayüz bu gerçeğe göre tasarlanacaktır (ADR-0015): en sık yapılan iş
> en az tıklamada, hata mesajları insan dilinde, boş durumlar yönlendirici.

---

## 7. Kapsam

> **Kritik ayrım — geliştirme kapsamı ile veri göçü kapsamı farklıdır.**
> Bir modülün **geliştirilmesi** kapsamda olabilir, ancak eski sistemden **veri göçü**
> kapsam dışı olabilir. Bu durumda modül geliştirilir ve **boş veriyle** çalışmaya başlar.
> Bu iki kapsam ayrı ayrı tanımlanmıştır (`KR-045`, `KR-046`).

### 7.1 Kapsam içi modüller

Sistem **35 modülden** oluşacaktır (`KR-045`, `KR-050`, `KR-051`). Modüllerin tamamı
geliştirilecektir; yalnızca **geliştirme sırası** İK birimiyle yapılacak toplantılarda
netleşir.

**Çalışma biçimi:** Bir modül geliştirilirken bir sonraki modülün hangisi olacağı
bilinmez. Sıradaki modül, mevcut modül kabul edildikten sonraki toplantıda belirlenir.

**İki planlama ilkesi:**
- **Yatay modüllere öncelik verilir** (`KR-053`). Altyapı bir kez yazılır, 20 iş modülü
  onu kullanır; "önce altyapı" bir gecikme değil hızlanma tercihidir.
- **Zincirli modüllerde daima ilk halkadan başlanır** (`KR-052`); zincirin ortasından
  modül seçilmez.

Bağımlılıklar, seçim rehberi ve grup ayrımı için:
**`docs/mimari/modul-listesi-ve-bagimliliklar.md`**

#### Temel modüller — sırayla, en başta

| Modül | Not |
|---|---|
| Personel Yönetimi | LOGO senkronizasyonu, kişi/istihdam modeli |
| Organizasyon Yönetimi | Firma, şube, birim, hiyerarşi |
| **Kullanıcı Girişi ve Üyelik (Kimlik Yönetimi)** | **İlk teslim edilecek modül** (`KR-040`) |
| Rol ve Yetki Yönetimi | |
| Kullanıcı Yönetimi | |

#### Yatay (enine kesen) modüller

Bildirim Merkezi · Denetim ve Erişim Kayıtları · Referans Veri Yönetimi ·
Sistem Yönetimi · İş Akışı Yönetimi (Workflow) · Delegasyon Yönetimi ·
Çalışan Belge Yönetimi · Raporlama ve Dışa Aktarma · Dashboard ·
**Çalışma Takvimi Yönetimi**

#### İş modülleri

İzin Yönetimi · Eğitim Yönetimi · Sertifika Yönetimi · Yetkinlik Yönetimi ·
Performans Yönetimi · Terfi Yönetimi · Pozisyon Değişikliği Yönetimi ·
İşe Alım Yönetimi · Aday Havuzu Yönetimi · Mülakat Yönetimi · Oryantasyon Yönetimi ·
Deneme Süresi Yönetimi · Disiplin Süreci Yönetimi · Ödül ve Takdir Yönetimi ·
Varlık Yönetimi (Zimmet ve İade) · Kurum İçi Duyuru Yönetimi ·
İşten Çıkış Süreci Yönetimi · İşten Çıkış Görüşmesi Yönetimi · Tebrik Servisi ·
**İSG ve İş Kazası Yönetimi**

### 7.2 Veri göçü kapsamı

Eski sistemden yeni sisteme aktarılacak veriler (`KR-046`):

| Modül | Göç | Hacim |
|---|---|---|
| **İzin Yönetimi** | ✅ Var | 7.494 izin + 841 devir bakiyesi + 30 ücretsiz izin + 7 iş göremezlik + kural/tür tanımları |
| **Eğitim Yönetimi** | ✅ Var | 2.508 personel eğitimi + 1.585 değerlendirme + 210 eğitim tanımı + soru havuzları |
| **Sertifika Yönetimi** | ✅ Var | 603 sertifika |
| Diğer tüm modüller | ❌ Yok | Yeni sistemde boş veriyle başlar |

**Göç kapsamı dışında bırakılan veriler:**

| Veri | Gerekçe |
|---|---|
| Log, hata, bildirim servisi logu, oturum jetonları | Yeni sistemde ihtiyaç yok (`KR-033`) |
| Talepler ve talep türleri | Göç kapsamı dışı (`KR-046`) — modül geliştirilecek, veri taşınmayacak |
| Personel devamsızlık takibi | Göç kapsamı dışı (`KR-046`) |
| Performans değerlendirme verisi | Mevcut sistemde **0 kayıt** |
| Anket verisi | Cevap tabloları **boş** |
| İSG / iş kazası verisi | 2–3 kayıt |
| İşe alım verisi | Sınırlı kullanım |

> Kesim öncesi eski veritabanının **salt-okunur arşiv kopyası** alınacaktır; göç
> edilmeyen veriler geçmiş denetim ihtiyacı için orada erişilebilir kalır.

### 7.3 Kapsam dışı

| Konu | Gerekçe |
|---|---|
| **Bordro ve ücret hesaplama** | LOGO'da kalmaya devam edecek. HRMS'in LOGO'dan aldığı tek kapsam **personel ana verisidir**; maaş ve ücret verisi alınmayacaktır |
| **LOGO'ya yazma işlemi** | Kesinlikle yapılmayacak (`KR-003`); teknik olarak da engellenmiştir (`KR-004`) |
| **Mobil uygulama** | Web arayüzü telefonda kullanılabilir olacak; ayrı mobil uygulama kapsam dışı |
| **Puantaj / PDKS entegrasyonu** | Bu sürümde kapsam dışı |
| **Anket Yönetimi** | Modül listesinde yer almıyor — teyit edilecek (§7.4, K1) |

### 7.4 Teyit edilmesi gereken kapsam maddeleri

| # | Konu | Durum |
|---|---|---|
| K1 | **Anket Yönetimi** modül listesinde yok | Teyit edilecek |
| K2 | **RADYOLOJİ A.Ş.** firmasının kapsamda olup olmadığı (aktif personeli yok) | Teyit edilecek |
| K3 | Veri saklama süreleri | KVKK Sorumlusundan görüş alınacak (`KR-023`) |
| K4 | Çalışan Belge Yönetimi'nde mevcut sistemden dosya göçü olacak mı? | Modül geliştirilirken netleşecek |
| K5 | Personelin "o cumartesi iş başı yapacağım" beyanının sistemde nasıl kayda geçeceği | İzin Yönetimi toplantısında netleşecek |

**Kapanan maddeler:**

| Konu | Sonuç |
|---|---|
| Çalışma Takvimi nerede ele alınacak? | ✅ **Ayrı modül** olarak ele alınacak (`KR-050`). Referans Veri Yönetimi, sistem genelinde kullanılan temel tanımların yönetimidir; çalışma takvimi kendi iş kuralları ve hesaplama mantığı olan bir modüldür |
| İSG / İş Kazası kapsamda mı? | ✅ **Kapsama alındı** (`KR-051`). 6331 sayılı kanun kapsamında iş kazası kayıtlarının tutulacağı bir modül olacaktır |

---

## 8. Sistem sınırları ve dış arayüzler

```
                    ┌─────────────────────────────────────────┐
                    │              DSG-HRMS                   │
   Personel ────────▶  Web arayüzü (React SPA)                │
   Yönetici ────────▶                                         │
   İK ──────────────▶  Web API (ASP.NET Core)                 │
                    │                                         │
                    │  PostgreSQL (HRMS veritabanı)           │
                    └───┬───────┬───────┬──────────┬──────────┘
                        │       │       │          │
              salt okuma│       │SMTP   │HTTPS     │dosya
                        ▼       ▼       ▼          ▼
                 ┌──────────┐ ┌─────┐ ┌────────┐ ┌─────┐
                 │  LOGO    │ │Post-│ │NetGSM  │ │ NAS │
                 │ Bordro   │ │fix  │ │  SMS   │ │     │
                 │ (MSSQL)  │ └─────┘ └────────┘ └─────┘
                 └──────────┘                      │
                                                   ▼
                                              ┌─────────┐
                                              │ ClamAV  │
                                              └─────────┘
```

| Dış sistem | Yön | İçerik | Karar |
|---|---|---|---|
| **LOGO Bordro (MSSQL)** | Yalnızca okuma | Personel ana verisi, organizasyon | ADR-0003 |
| **Postfix (SMTP)** | Giden | E-posta bildirimleri | ADR-0012 |
| **NetGSM** | Giden | SMS bildirimleri ve doğrulama kodları | ADR-0012 |
| **NAS** | Okuma/yazma | Belge ve dosya saklama | ADR-0013 |
| **ClamAV** | Çağrı | Virüs taraması | ADR-0013 |

**Sistem sınırının dışında kalanlar:** bordro hesaplama, muhasebe, hasta/laboratuvar
bilgi sistemleri, PDKS cihazları.

---

## 9. Kalite hedefleri (fonksiyonel olmayan gereksinimler)

| Kategori | Hedef | Nasıl doğrulanacak |
|---|---|---|
| **Performans** | Liste ekranları %95 dilimde **< 1 saniye**; rapor üretimi < 30 saniye | Yük testi, izleme |
| **Ölçek** | 600 kullanıcı, ~100 eş zamanlı oturum; 10 yıllık veri birikimi | Kapasite planı |
| **Erişilebilirlik (uptime)** | Mesai saatleri içinde **%99**; LOGO kesintisi sistemi durdurmaz | İzleme, sağlık kontrolü |
| **Güvenlik** | Sır sızıntısı yok; yetki sızıntısı yok; kaba kuvvete karşı hız sınırlama | `gitleaks`, sızıntı testleri, güvenlik gözden geçirmesi |
| **KVKK uyumu** | Özel nitelikli veri şifreli; erişim ve dışa aktarma kayıtlı; maskeleme uygulanıyor | Maskeleme testleri, denetim kaydı raporu |
| **Kullanılabilirlik** | Yeni personel, izin talebini **yardım almadan** oluşturabilmeli | Kabul testi (UAT) gözlemi |
| **Bakım kolaylığı** | Domain kapsamı ≥ %90, genel ≥ %75; mimari kuralları otomatik denetleniyor | CI kalite kapıları |
| **Taşınabilirlik** | Linux + Docker; tek komutla ayağa kalkabilme | Docker Compose |
| **Gözlemlenebilirlik** | Her istek izlenebilir (`traceId`); kritik hatalar bildirim üretir | OpenTelemetry, log |
| **Çoklu dil** | Arayüzde sabit metin yok; ikinci dil kod değişikliği gerektirmez | ESLint kuralı |

---

## 10. Kısıtlar

| # | Kısıt | Kaynak |
|---|---|---|
| KS1 | LOGO veritabanına **hiçbir koşulda** yazma yapılamaz | Kurum kararı (`KR-003`) |
| KS2 | Üretim ortamı **Linux** olacak | Kurum kararı |
| KS3 | Kullanılacak paketler **tamamen ücretsiz** olmalı; koşullu ticari lisans kabul edilmez | Kurum kararı (`KR-025`) |
| KS4 | Uygulama **yalnızca yerel ağda** çalışacak, internete açılmayacak | Kurum kararı (`KR-017`) |
| KS5 | Kurumda **Active Directory / Entra ID yok** | Mevcut durum (`KR-013`) |
| KS6 | Proje **TS ISO/IEC TS 33061 Seviye 2** çerçevesinde yürütülecek | Kurum kararı |
| KS7 | Modül gereksinimleri **sırayla** toplanacak; bir modül İK tarafından kabul edilmeden sonraki modülün gereksinimleri toplanmayacak | Kurum işleyişi |
| KS8 | Geliştirme kaynağı sınırlıdır (tek geliştirme kanalı) | Mevcut durum (`R-05`) |
| KS9 | NetGSM hesabında **OTP paketi yok**; standart SMS servisi kullanılacak | Mevcut durum (`KR-042`) |

---

## 11. Varsayımlar ve bağımlılıklar

### 11.1 Varsayımlar

| # | Varsayım | Yanlışsa etkisi |
|---|---|---|
| V1 | LOGO sürüm yükseltmeleri geçmişte olduğu gibi yalnızca **kolon ekleme** biçiminde kalacak; mevcut tablo ve kolonlar değişmeyecek | Entegrasyon kırılır (`R-02`). Varsayımı destekleyen durum: yükseltmeler TRISOFT tarafından **önceden bildiriliyor**, Mali İşler ve Bilgi İşlem ile tarih teyitleşiliyor ve mesai bitiminde yapılıyor. Şema sapma denetimi ayrıca güvence sağlar |
| V2 | İK, devreye alma öncesi eksik iletişim bilgilerini LOGO'da tamamlayacak | Kurumsal e-postası olmayan personel üye olamaz ve giriş yapamaz (`R-08`, `KR-073`). **Durum (22.09.2026):** varsayım büyük ölçüde gerçekleşti — iki düzeltme turunda kurumsal e-postası olmayan 38 → 4, hiçbir iletişim bilgisi olmayan 12 → 2 kişi |
| V3 | Uygulama internete açılmayacak | 2FA parametresi açılır — işlev geliştirilmiş olacağı için yeni sürüm gerekmez (`KR-069`) |
| V4 | NAS'ın kendi yedekleme düzeni var ve çalışıyor | Dosya kaybı riski (ADR-0013 §8) |
| V5 | Mevcut sistemin verisi göç edilebilir kalitede | Göç süresi ve maliyeti artar (`R-01`) |
| V6 | İzin hakediş kuralları mevcut mevzuata göre parametrelenebilir | Kural motoru tasarımı değişir (`R-06`) |

### 11.2 Dış bağımlılıklar

| # | Bağımlılık | Sorumlu |
|---|---|---|
| B1 | LOGO veritabanına salt-okunur erişim | Bilgi İşlem ✅ *(sağlandı)* |
| B2 | Yeni PostgreSQL sunucusu | Bilgi İşlem (`KR-035`) |
| B3 | NAS erişimi ve yedekleme | Bilgi İşlem |
| B4 | NetGSM API kullanıcısı | Bilgi İşlem ✅ *(sağlandı)* |
| B5 | SMTP erişimi | Bilgi İşlem ✅ *(parola yenilendi)* |
| B6 | İK biriminin gereksinim toplantıları ve kabul testleri için ayıracağı zaman | İK Birimi |
| B7 | KVKK saklama süreleri hakkında görüş | KVKK Sorumlusu |
| B8 | 33061 Seviye 2 değerlendirme kriterleri | Belgelendirme kuruluşu |

---

## 12. Hayat döngüsü kavramları

> 33061 TEC.2, *"life cycle concepts"* çıktısını ister: sistemin yalnızca nasıl
> geliştirileceği değil, nasıl **işletileceği, bakılacağı ve sonlandırılacağı**.

### 12.1 Edinim ve geliştirme kavramı

Sistem **kurum içinde geliştirilecektir**; dış tedarikçi kullanılmayacaktır. Geliştirme
modüler ve artımlıdır: her modül gereksinim → tasarım → geliştirme → doğrulama →
kabul → kapanış çevrimini tamamlar. Her çevrim, 33061 kapsamındaki 15 süreç için yeni
kanıt üretir.

### 12.2 İşletim kavramı

| Konu | Yaklaşım |
|---|---|
| Ortamlar | **Geliştirme**, **UAT** (İK kabul testleri, `KR-024`), **Üretim** |
| Çalışma şekli | Linux sunucuda Docker Compose; ters vekil sunucu (reverse proxy) arkasında |
| Erişim | Yalnızca kurum yerel ağı |
| Çalışma saatleri | Mesai saatleri yoğun; 7/24 erişilebilir |
| LOGO senkronizasyonu | 15 dakikada bir otomatik + manuel tetikleme |
| İzleme | Sağlık kontrolü uç noktaları, yapılandırılmış log, kritik olay bildirimi |
| Yedekleme | Veritabanı düzenli yedeklenir; NAS ayrı yedeklenir. **Geri yükleme senaryosunda ikisi aynı ana ait olmalıdır** |

### 12.3 Geçiş (devreye alma) kavramı

1. Eski sistemden veri göçü **en az üç kez** denenir: kuru koşu → doğrulama → gerçek kesim.
2. Her denemede **mutabakat raporu** üretilir; kabul kriteri **sıfır açıklanamayan farktır**.
3. Kesim öncesi eski veritabanının **salt-okunur arşiv kopyası** alınır.
4. **Geri dönüş (rollback) planı** hazır bulundurulur.
5. Kullanıcı eğitimi ve kısa kullanım kılavuzu sağlanır.
6. Devreye alma sonrası bir süre eski sistem **salt okunur** olarak erişilebilir kalır.

### 12.4 Bakım kavramı

| Konu | Yaklaşım |
|---|---|
| Sorumlu | Bilgi İşlem Birimi |
| Talep kanalı | GitHub Issue (`hata`, `bakim`, `degisiklik-talebi` etiketleri) |
| Sürümleme | Semantic Versioning; her sürüm etiketlenir ve sürüm notu yazılır |
| Düzeltme dağıtımı | Aynı CI/CD hattından; doğrudan üretime elle müdahale yapılmaz |
| Bağımlılık güncellemesi | Dependabot; güvenlik güncellemeleri öncelikli |
| Mevzuat değişikliği | İzin kuralları parametrik olduğu için çoğu değişiklik **kod değişikliği gerektirmez** (`KR-011`) |

### 12.5 Kullanımdan kaldırma kavramı

Sistem bir gün değiştirilecekse:

- Veri, standart biçimlerde (SQL yedeği, CSV/Excel) dışa aktarılabilir olmalıdır.
- Şema ve veri sözlüğü belgelenmiş olmalıdır.
- Mevzuat gereği saklanması gereken veriler, sistem kapatılsa dahi erişilebilir bir
  arşivde tutulmalıdır.
- Dosyalar NAS'ta bağımsız olarak durduğu için sistemden ayrı erişilebilir kalır.

> Bu, bugün yapılacak bir iş değildir; ancak **veri taşınabilirliğinin baştan
> tasarlanması** gerektiğini kayda geçirir. Mevcut sistemin yenilenmesinde yaşanan
> zorluklar bu maddenin gerekçesidir.

---

## 13. Teslimat yaklaşımı

| Konu | Yaklaşım |
|---|---|
| Yöntem | Modül bazlı artımlı teslimat |
| Modül çevrimi | Gereksinim toplantısı → yazılı gereksinim ve kabul kriteri → tasarım → geliştirme → doğrulama (CI) → UAT → İK kabulü → kapanış gözden geçirmesi |
| Kapsam kilidi | Modül gereksinimleri yazılı olarak kilitlenir; sonradan gelen talepler değişiklik talebi olur (`R-04`) |
| Sürüm | Her kabul edilen modül bir sürüm etiketi (baseline) üretir (MAN.5) |
| Boş zaman yönetimi | İK onayı beklenirken enine kesen işler yapılır: test altyapısı, dokümantasyon, göç araçları, güvenlik ve performans çalışmaları (`KS7`'nin etkisini azaltmak için) |
| İzlenebilirlik | Gereksinim → tasarım → kod/PR → test → kabul zinciri izlenebilirlik matrisinde tutulur |

---

## 14. Bu belgeden doğan açık işler

| # | İş | Sorumlu | Süreç |
|---|---|---|---|
| 1 | Belgenin İK birimi ve üst yönetim tarafından onaylanması | İK + Üst yönetim | TEC.2 |
| 2 | Başarı ölçütlerinin (§4) teyit edilmesi | İK + Üst yönetim | TEC.2 / MAN.2 |
| 3 | Anket Yönetimi'nin kapsam durumu (§7.4 K1) | İK | TEC.2 |
| 4 | RADYOLOJİ A.Ş. firmasının kapsamda olup olmadığı (§7.4 K2) | İK | TEC.2 |
| 5 | Cumartesi iş başı beyanının nasıl kayda geçeceği (§7.4 K5) | İK | TEC.2 |
| 6 | Birim sorumlusu ve İK uzmanı sayılarının netleştirilmesi (§6 tahminleri) | İK | TEC.2 |
| 7 | Gereksinim toplantılarına birim sorumlusu / birim teknik sorumlusu katılımının sağlanması | İK | TEC.2 |
| 8 | UAT'ye 2–3 temsilci personelin dâhil edilmesi | İK | TEC.11 |
| 9 | Veri saklama süreleri hakkında görüş | KVKK Sorumlusu | TEC.2 / `KR-023` |
| 10 | 33061 Seviye 2 değerlendirme kriterlerinin yazılı alınması | Bilgi İşlem | MAN.8 |

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk taslak | Bilgi İşlem |
