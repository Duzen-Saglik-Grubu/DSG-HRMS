# Paydaş Listesi

**Belge kimliği:** TEC.2-PL
**Süreç:** TEC.2 — Paydaş İhtiyaç ve Gereksinimlerinin Tanımlanması
**33061 karşılığı:** *Stakeholder identification* (TEC.2.BP1)
**Son güncelleme:** 2026-09-07
**Gözden geçirme:** Her modül gereksinim toplantısı öncesi

---

## 1. Amaç

Bu belge, projeden etkilenen ve projeyi etkileyen tarafları tanımlar. Amacı üç sorunun
cevabını sabitlemektir:

- Bir modülün gereksinimleri **kimden** toplanacak?
- Bir kararı **kim** onaylayacak?
- Bir değişiklik **kimi** etkiler, kime haber verilmeli?

Paydaş atlanması, gereksinim eksikliğinin en yaygın nedenidir. Bu liste, her modül
toplantısı öncesinde gözden geçirilir.

---

## 2. Paydaş sınıflandırması

**Etki:** Paydaşın proje sonucundan ne kadar etkilendiği.
**Yetki:** Paydaşın proje kararları üzerindeki söz hakkı.

| Kod | Paydaş | Etki | Yetki | Katılım biçimi |
|---|---|---|---|---|
| P1 | İnsan Kaynakları Birimi | Yüksek | Yüksek | **Aktif** — gereksinim kaynağı ve kabul mercii |
| P2 | Bilgi İşlem Birimi | Yüksek | Yüksek | **Aktif** — geliştirme ve teknik kararlar |
| P3 | Personel (son kullanıcı) | Yüksek | Düşük | Dolaylı — İK üzerinden; **UAT'ye temsilci katılımı** (`KR-048`) |
| P4 | Birim sorumluları / birim teknik sorumluları | Yüksek | Orta | **Aktif** — gereksinim toplantılarına katılır (`KR-047`) |
| P5 | Üst Yönetim | Orta | Yüksek | Bilgilendirilen — kapsam, bütçe, takvim onayı |
| P6 | KVKK Sorumlusu | Orta | Orta | Danışılan — saklama süreleri, uyum kararları |
| P7 | Mali İşler / Bordro Birimi | Orta | Orta | Danışılan — LOGO bütünlüğü ve erişim kuralları |
| P8 | Belgelendirme Kuruluşu | Orta | Dış | Dış taraf — 33061 değerlendirme kriterleri |
| P9 | LOGO tedarikçisi / bakım firması | Düşük | Dış | Bilgilendirilen — sürüm yükseltmeleri |
| P10 | NetGSM (SMS sağlayıcı) | Düşük | Dış | Hizmet sağlayıcı |
| P11 | İş Sağlığı ve Güvenliği Uzmanı | Orta | Orta | Danışılan — İSG ve İş Kazası modülü gereksinimleri |

---

## 3. Paydaş ayrıntıları

### P1 — İnsan Kaynakları Birimi

| | |
|---|---|
| **Rolü** | Sistemin ana kullanıcısı; iş süreçlerinin sahibi |
| **Temel ihtiyaçları** | Manuel Excel takibinin sona ermesi; izin bakiyelerinin doğru ve otomatik hesaplanması; eğitim ve sertifika takibinin sistemde olması; hızlı raporlama ve Excel'e aktarma |
| **Başarı ölçütü (kendi gözünden)** | "Personel kendi işlemini kendisi yapıyor, ben süreci yönetiyorum" |
| **Katılım** | Her modül öncesi gereksinim toplantısı; UAT ortamında kabul testi; kabul formu imzası |
| **İletişim** | Bilgi İşlem Birim Sorumlusu üzerinden, yüz yüze toplantı |
| **Riskler** | Toplantı takvimi projeyi bloke edebilir (`KS7`); kapsam kayması (`R-04`) |

### P2 — Bilgi İşlem Birimi

| | |
|---|---|
| **Rolü** | Geliştirme, mimari kararlar, işletme, bakım |
| **Temel ihtiyaçları** | Sürdürülebilir kod tabanı; test edilebilirlik; güvenlik; 33061 uyumu; tek kişiye bağımlılığın azalması |
| **Katılım** | Sürekli |
| **Riskler** | Bilgi tekelliği (`R-05`); tek geliştirme kanalı (`KS8`) |

### P3 — Personel (son kullanıcı)

| | |
|---|---|
| **Rolü** | Self-servis kullanıcı — izin talebi, eğitim geçmişi, kendi bilgileri |
| **Sayı** | ~584 aktif |
| **Profil** | Ağırlıklı olarak **düşük teknik seviyeli ve seyrek kullanıcı** |
| **Temel ihtiyaçları** | Kolay ve hızlı izin talebi; izin bakiyesini görebilme; talebin durumunu takip edebilme; telefondan erişebilme |
| **Katılım** | Doğrudan katılmaz. Gereksinimleri İK üzerinden temsil edilir. UAT sırasında **en az 2–3 temsilci personel** dâhil edilecektir (`KR-048`) |
| **Riskler** | Arayüz karmaşık olursa sistem kullanılmaz ve yük İK'ya telefonla döner |

> **Karar (`KR-048`):** UAT'lere İK dışından en az 2–3 gerçek personel dâhil edilecektir.
> Kullanılabilirlik açısından en değerli geri bildirim bu gruptan gelir.

### P4 — Birim sorumluları ve birim teknik sorumluları

| | |
|---|---|
| **Rolü** | Ekibini görüntüleme, izin onayı |
| **Sayı** | ~50 (İK tarafından teyit edilecek) |
| **Temel ihtiyaçları** | Kendine bağlı personeli görebilme; izin taleplerini hızlı onaylayabilme; ekip izin takvimini görebilme |
| **Özel durum** | Bir personelin **birden fazla yöneticisi olabilir** (birim sorumlusu + birim teknik sorumlusu). Görüş kapsamı **doğrudan atamalarla** sınırlıdır (`KR-022`) |
| **Katılım** | **Aktif.** Gereksinim toplantılarına birim sorumlusu veya birim teknik sorumlusu katılımı sağlanacaktır (`KR-047`). 14 modülde katılımları gereklidir (bkz. §4) |

> Bu grup, sistemin günlük kullanımında İK kadar kritiktir ancak gereksinim toplantılarında
> en sık atlanan gruptur. `KR-047` bu boşluğu kapatmak için alınmıştır.

### P5 — Üst Yönetim

| | |
|---|---|
| **Rolü** | Kapsam, öncelik ve takvim onayı; kaynak tahsisi |
| **Temel ihtiyaçları** | Projenin öngörülebilir ilerlemesi; 33061 Seviye 2 belgesinin alınması; kurumsal raporlama |
| **Katılım** | Vizyon ve kapsam onayı; dönemsel durum raporları (MAN.2) |

### P6 — KVKK Sorumlusu

| | |
|---|---|
| **Rolü** | Kişisel veri işleme uyumu |
| **Temel ihtiyaçları** | Özel nitelikli verinin korunması; saklama sürelerinin tanımlı olması; erişim ve dışa aktarma kayıtlarının tutulması; veri sahibi başvurularına cevap verilebilmesi |
| **Katılım** | Danışılan. **Açık iş:** veri türü bazında saklama süreleri hakkında yazılı görüş (`KR-023`) |
| **Riskler** | Görüş gecikirse saklama parametreleri varsayılan (çok uzun) değerlerde kalır (`R-03`) |

### P7 — Mali İşler / Bordro Birimi

| | |
|---|---|
| **Rolü** | LOGO Bordro sisteminin sahibi |
| **Temel ihtiyaçları** | LOGO verisinin bütünlüğünün korunması; HRMS'in bordro sürecini etkilememesi |
| **Katılım** | Bilgilendirilen. LOGO erişim kuralları ve sürüm yükseltmeleri konusunda danışılır |
| **Güvence** | HRMS'in LOGO'ya yazma yetkisi **teknik olarak reddedilmiştir** (`KR-004`); bu, doğrulama testiyle teyit edilmiştir |

### P8 — Belgelendirme Kuruluşu

| | |
|---|---|
| **Rolü** | TS ISO/IEC TS 33061 Seviye 2 değerlendirmesi |
| **Temel ihtiyaçları** | Süreç kanıtlarının eksiksiz, tarihli ve sahipli olması |
| **Katılım** | Dış taraf. **Açık iş:** seviye kriterlerinin yazılı olarak alınması |

### P9 — LOGO tedarikçisi / bakım firması

| | |
|---|---|
| **Rolü** | LOGO Bordro sürüm yükseltmeleri |
| **Neden paydaş** | Bir sürüm yükseltmesi HRMS entegrasyonunu **sessizce kırabilir** (`R-02`) |
| **Katılım** | Bilgilendirilen. **Süreç hâlihazırda işlemektedir:** TRISOFT yükseltmeyi önceden bildirir, Mali İşler ve Bilgi İşlem ile tarih uygunluğu teyitleşilir, yükseltme mutabık kalınan günün mesai bitiminde yapılır |

> Geçmiş yükseltmelerde majör bir veritabanı tasarım değişikliği olmamış, genellikle
> tablolara kolon eklenmiştir. Bu nedenle `R-02` riskinin olasılığı düşürülmüştür.
> Yükseltme öncesinde ve sonrasında şema doğrulama denetimi çalıştırılacaktır (`KR-049`).

### P10 — NetGSM

| | |
|---|---|
| **Rolü** | SMS hizmet sağlayıcısı |
| **Bağımlılık** | Doğrulama kodları ve bildirimler; kredi bakiyesi ve hesap ayarları |
| **Katılım** | Hizmet sağlayıcı. DSG-HRMS için ayrı API kullanıcısı tanımlanmıştır (`KR-036`) |

### P11 — İş Sağlığı ve Güvenliği Uzmanı

| | |
|---|---|
| **Rolü** | 6331 sayılı İş Sağlığı ve Güvenliği Kanunu kapsamındaki yükümlülüklerin takibi |
| **Neden paydaş** | İSG ve İş Kazası Yönetimi modülü (`KR-051`, İ20) bu kişinin süreçlerini karşılayacaktır. Kaza bildirimi, tutanak, tanık beyanı ve bildirim süreleri mevzuatla belirlidir |
| **Temel ihtiyaçları** | Kaza kaydının mevzuata uygun alanlarla tutulması; bildirim sürelerinin takibi; tutanak ve ek belge saklama; raporlama |
| **Katılım** | Danışılan. İ20 modülünün gereksinim toplantısına **katılması gerekir** |
| **Durum** | Kurumda İSG uzmanı **iç kaynaktır** (2026-09-07 teyidi). İ20 modülünün gereksinim toplantısına katılımı sağlanacaktır |


---

## 4. Gereksinim toplama sorumluluğu

Modül listesi ve bağımlılıklar: `docs/mimari/modul-listesi-ve-bagimliliklar.md`

**Birincil kaynak her modülde P1 (İK Birimi)'dir.** Aşağıdaki tablo, ek olarak
**kimin toplantıda bulunması gerektiğini** gösterir. `KR-047` uyarınca birim sorumlusu
veya birim teknik sorumlusu katılımı sağlanacaktır.

| Modül | Ayrıca danışılacak | Neden |
|---|---|---|
| **Organizasyon Yönetimi** | **P4** | Hiyerarşi ve yönetici ataması doğrudan onların çalışma düzenidir |
| **İş Akışı Yönetimi (Workflow)** | **P4** | Onay kademelerini fiilen kullanacak grup |
| **Delegasyon Yönetimi** | **P4** | Vekâlet ihtiyacı yöneticilerde doğar |
| **İzin Yönetimi** | **P4**, P3 temsilci, P7 Mali İşler | Onay akışı; çalışma takvimi ve mesai/ücret etkisi |
| **Çalışma Takvimi Yönetimi** (ayrı modül, `KR-050`) | P7 Mali İşler, **P4** | Cumartesi dönüşüm düzeni ve pazar mesaisi ücretle ilişkili; birim bazlı düzen yöneticilerce belirlenir |
| **Terfi / Pozisyon Değişikliği** | **P4** | Öneri ve onay yöneticiden gelir |
| **Performans / Yetkinlik Yönetimi** | **P4** | Değerlendirmeyi yöneticiler yapacak |
| **Disiplin Süreci** | **P4**, P6 KVKK | Onay akışı; özel nitelikli veri |
| **İSG ve İş Kazası Yönetimi** | **P4**, P6 KVKK, İSG Uzmanı | Kaza bildirimi birimde başlar; sağlık verisi özel niteliklidir; 6331 sayılı kanun yükümlülüğü |
| **Ödül ve Takdir** | **P4** | Öneri yöneticiden gelir |
| **Varlık (Zimmet) Yönetimi** | **P4** | Zimmet teslim/iade süreci birimde yürür |
| **İşten Çıkış Süreci / Çıkış Görüşmesi** | **P4**, P7 Mali İşler | Çıkış onayı ve bordro etkisi |
| **İşe Alım / Aday Havuzu / Mülakat** | **P4** | Talep ve mülakat yöneticiden gelir |
| **Oryantasyon / Deneme Süresi** | **P4** | Değerlendirme yöneticide |
| Kimlik, Kullanıcı, Rol ve Yetki Yönetimi | P2 Bilgi İşlem, P6 KVKK | Güvenlik ve erişim kararları |
| Personel Yönetimi | P7 Mali İşler, P6 KVKK | LOGO bütünlüğü, kişisel veri |
| Çalışan Belge Yönetimi | P6 KVKK | Özel nitelikli belge saklama |
| Denetim ve Erişim Kayıtları | P6 KVKK | Saklama süreleri ve erişim izleme |
| Eğitim / Sertifika Yönetimi | — | |
| Bildirim Merkezi | P2 Bilgi İşlem | Kanal ve altyapı |
| Kurum İçi Duyuru / Tebrik Servisi | — | |
| Raporlama ve Dışa Aktarma | P5 Üst yönetim, P6 KVKK | Yönetim raporları; dışa aktarma riski |
| Dashboard | P5 Üst yönetim | Yönetim göstergeleri |
| Referans Veri / Sistem Yönetimi | P2 Bilgi İşlem | Parametre ve tanım yönetimi |

> **14 modülde birim sorumlusu katılımı gerekiyor.** Bu, P4'ün neden aktif paydaş
> sayılması gerektiğini gösterir: modüllerin yaklaşık yarısı doğrudan onların günlük
> işleyişini değiştirmektedir.

---

## 5. Paydaş listesinden doğan açık işler

| # | İş | Sorumlu |
|---|---|---|
| 1 | Birim sorumlusu / teknik sorumlu sayısının netleştirilmesi | İK |
| 2 | İK uzmanı sayısının netleştirilmesi | İK |
| 3 | Gereksinim toplantılarına birim sorumlusu / birim teknik sorumlusu katılımının sağlanması (`KR-047`) | İK |
| 4 | UAT'ye en az 2–3 temsilci personelin dâhil edilmesi (`KR-048`) | İK |
| 5 | KVKK Sorumlusundan saklama süreleri görüşü | Bilgi İşlem → KVKK Sorumlusu |
| 6 | ~~LOGO sürüm yükseltmelerinin önceden bildirilmesi mutabakatı~~ — **Kapandı:** süreç zaten işliyor; TRISOFT önceden bildiriyor, Mali İşler ve Bilgi İşlem tarih teyitleşiyor (`KR-049`) | — |
| 7 | Belgelendirme kuruluşundan seviye kriterlerinin yazılı alınması | Bilgi İşlem |
| 8 | İ20 (İSG ve İş Kazası) gereksinim toplantısına İSG uzmanının katılımının sağlanması — uzman iç kaynaktır | İK |

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
