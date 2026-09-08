# ADR-0015 — Frontend Mimarisi ve Kullanıcı Arayüzü İlkeleri

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-001`, `KR-039`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)

---

## Bağlam

Kurumun açık gereksinimi: *"Kullanıcı arayüzü modern, kullanıcı dostu, kullanım
kolaylığı en üst seviyede tasarlanacak."* Bu, projenin dört olmazsa olmazından biridir.

Kullanıcı kitlesi teknik değildir: ≈584 personelin tamamı sistemi kullanacak (kendi
izin talebini kendisi girecek). Arayüz karmaşık olursa sistem kullanılmaz ve İK'ya
telefon yükü olarak geri döner.

## Karar

### 1. Klasör düzeni — modül bazlı

Backend'deki modül bölümlemesiyle **aynı isimlendirme** kullanılır; ekip iki tarafta
aynı zihinsel haritayı kullanır.

```
src/frontend/dsg-hrms-web/src/
├── app/                    → uygulama kabuğu, yönlendirme, sağlayıcılar, tema
├── shared/
│   ├── api/                → Axios örneği, ara katmanlar, üretilen tipler
│   ├── components/         → ortak bileşenler (DataTable, ConfirmDialog, PageHeader…)
│   ├── hooks/              → ortak kancalar
│   ├── auth/               → oturum durumu, korumalı rota, yetki kontrolü
│   └── utils/              → biçimlendirme, tarih, doğrulama yardımcıları
├── features/
│   ├── identity/           → giriş, üyelik, parola
│   ├── organization/
│   ├── personnel/
│   ├── leave/
│   └── training/
└── locales/                → tr/, (ileride) en/
```

Her `feature` klasörü kendi içinde `pages/`, `components/`, `api/`, `types/` barındırır.
**Özellikler arası doğrudan içe aktarma (import) yapılmaz**; ortak ihtiyaç `shared/`'a
taşınır. Bu kural ESLint ile denetlenir.

### 2. Durum yönetimi — iki ayrı kavram

| Durum türü | Araç | Örnek |
|---|---|---|
| **Sunucu durumu** | TanStack Query | Personel listesi, izin talepleri, raporlar |
| **İstemci durumu** | React yerel durumu / Zustand | Açık sekme, filtre paneli, tema |

**Redux benzeri genel durum yönetimi kullanılmayacaktır.** İK uygulaması ağırlıklı
olarak "sunucudan oku, göster, güncelle" örüntüsündedir; sunucu durumu TanStack Query
tarafından yönetildiğinde geriye kalan istemci durumu çok azdır (KISS, YAGNI).

TanStack Query kuralları:
- Sorgu anahtarları (query keys) merkezî bir yerde tanımlanır; dize (string) elle
  yazılmaz.
- Değişiklik (mutation) sonrası ilgili sorgular **geçersiz kılınır** (invalidate).
- Varsayılan `staleTime` makul bir değere ayarlanır; her odaklanmada yeniden çekme
  kapatılır.

### 3. API katmanı

- Tek bir Axios örneği; **ara katmanlar (interceptors)** merkezî davranışı taşır:

| Ara katman | Davranış |
|---|---|
| İstek | Erişim jetonunu ekler |
| Yanıt `401` | Yenileme jetonu ile jetonu tazeler, isteği bir kez tekrarlar; başarısızsa girişe yönlendirir |
| Yanıt hata | Problem Details'i çözümler, `traceId`'yi saklar |
| Yanıt `403` | "Bu işlem için yetkiniz yok" bildirimi |

- **Tipler OpenAPI'den otomatik üretilir** (ADR-0010 §1); elle yazılmaz.
- Ham `fetch` veya doğrudan `axios` çağrısı bileşen içinde yapılmaz; her zaman
  `features/*/api/` katmanından geçilir.

### 4. Formlar

- **React Hook Form + Zod**. Şema doğrulaması form ile birlikte tanımlanır.
- Sunucudan gelen alan bazlı hatalar (`errors` sözlüğü) doğrudan ilgili alana bağlanır.
- **Kaydedilmemiş değişiklik koruması:** kullanıcı formu doldurup sayfadan ayrılmaya
  çalışırsa uyarılır. İzin talebi gibi formlarda veri kaybı kullanıcı güvenini bozar.
- Gönderim sırasında düğme devre dışı bırakılır; **çift gönderim engellenir**.

### 5. Ortak bileşenler — tekrarı önlemek için

Aşağıdakiler bir kez yazılır ve her modülde yeniden kullanılır:

| Bileşen | İşlevi |
|---|---|
| `DataTable` | Sunucu tarafı sayfalama, sıralama, filtreleme, sütun seçimi, dışa aktarma düğmesi |
| `PageHeader` | Başlık, kırıntı yolu (breadcrumb), eylem düğmeleri |
| `ConfirmDialog` | Yıkıcı işlemler için onay |
| `FormDialog` | Standart form penceresi |
| `EmptyState` | Boş liste durumu — "kayıt yok" yerine ne yapılacağını söyler |
| `ErrorState` | Hata durumu + `traceId` gösterimi + yeniden dene |
| `PermissionGate` | Yetkiye göre arayüz öğesi gizleme |
| `DateRangePicker` | Türkçe biçimli tarih aralığı seçimi |

> **`PermissionGate` yalnızca görsel bir kolaylıktır.** Gerçek yetki kontrolü sunucudadır
> (ADR-0007 §3). Arayüzde gizlemek güvenlik önlemi değildir.

### 6. Kullanılabilirlik ilkeleri

1. **Türkçe ve insan dili.** Hata mesajları "Bir hata oluştu" değil, ne yapılacağını
   söyler. Teknik terim kullanıcıya gösterilmez.
2. **Yükleniyor durumu her zaman görünür.** İskelet (skeleton) yükleyiciler kullanılır;
   boş ekran gösterilmez.
3. **Boş durumlar yönlendirir.** "Henüz izin talebiniz yok" + "Yeni talep oluştur" düğmesi.
4. **Yıkıcı işlemler onay ister** ve neyin silineceğini açıkça yazar.
5. **Klavye ile kullanılabilirlik.** İK personeli gün boyu veri girer; sekme sırası
   ve kısayollar önemsenir.
6. **En sık yapılan iş en az tıklamada.** İzin talebi oluşturma, ana ekrandan tek
   tıkla erişilebilir olmalıdır.
7. **Tutarlılık.** Aynı işlem her modülde aynı yerde ve aynı görünümde olur.

### 7. Erişilebilirlik

- MUI bileşenleri erişilebilirlik desteğiyle gelir; özel bileşenlerde bu korunur.
- Form alanlarında etiket (`label`) zorunludur; yalnızca yer tutucu (placeholder)
  kullanılmaz.
- Renk **tek başına** anlam taşımaz; durum bilgisi metin veya simge ile de verilir.
- Kontrast oranları WCAG AA seviyesini karşılar.

### 8. Duyarlı tasarım (responsive)

Öncelik masaüstüdür (İK ve personel bilgisayardan kullanacak), ancak tablet ve telefonda
**okunabilir ve kullanılabilir** olmalıdır. Özellikle izin talebi ve onay ekranları
telefonda çalışmalıdır — yönetici sahada olabilir.

### 9. Çoklu dil

- **i18next** baştan kurulur; arayüzde **sabit metin (hardcoded string) yazılmaz**.
- Başlangıç dili TR (`KR-039`). İkinci dil eklendiğinde kod değişikliği gerekmez.
- Bu kural ESLint kuralıyla denetlenir.

### 10. Tema

- MUI teması tek yerde tanımlanır: kurumsal renkler, tipografi, aralık ölçeği,
  bileşen varsayılanları.
- Bileşen içinde doğrudan renk kodu (`#1F3864`) yazılmaz; tema belirteçleri kullanılır.
- Koyu tema **ilk sürümde kapsam dışıdır** (YAGNI), ancak tema yapısı buna izin verecek
  şekilde kurulur.

### 11. Performans

- Rota bazlı **kod bölme** (lazy loading); ilk yükleme küçük tutulur.
- Uzun listelerde sunucu tarafı sayfalama zorunludur (ADR-0010 §4).
- Derleme çıktısı boyutu izlenir; belirlenen eşiği aşan artış PR'da fark edilir.

## Gerekçe

- Modül bazlı klasör düzeni, backend ile aynı zihinsel haritayı kurar; tek geliştiricinin
  iki taraf arasında geçiş maliyetini düşürür.
- Sunucu/istemci durumu ayrımı, İK uygulamalarında en sık görülen aşırı mühendislik
  hatasını (her şeyi Redux'a koymak) baştan önler.
- Ortak bileşen seti, "kullanım kolaylığı en üst seviyede" hedefinin tek uygulanabilir
  yolu: tutarlılık ancak yeniden kullanımla sağlanır.
- i18next'in baştan kurulması, sabit metinlerin koda yayılmasını engeller — bu, sonradan
  düzeltilmesi en pahalı eksiklerden biridir.

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| Redux Toolkit | İK uygulaması için gereğinden ağır; TanStack Query yeterli |
| Next.js | Sunucu tarafı işleme gerekmiyor; ek karmaşıklık ve dağıtım yükü |
| Tailwind CSS | MUI kurum tercihi; hazır ve erişilebilir bileşen seti daha hızlı sonuç veriyor |
| Tip üretmek yerine elle yazmak | Uyumsuzluk çalışma zamanında ortaya çıkar |
| Çoklu dili sonraya bırakmak | Sabit metinler koda yayılır, sonradan toplanması pahalı |
| Katman bazlı klasör düzeni (`components/`, `pages/`, `services/`) | Modül büyüdükçe ilgili dosyalar birbirinden uzaklaşır |

## Sonuçlar

**Olumlu:** Tutarlı ve öğrenilmesi kolay arayüz; backend ile hizalı yapı; tip güvenliği;
ileriye dönük çoklu dil hazırlığı.

**Olumsuz / kabul edilen ödünler:**
- Ortak bileşen setinin baştan yazılması ilk modülde ek süre gerektirir; sonraki her
  modülde geri kazanılır.
- MUI'nin görsel dili kurumsal kimlikle birebir örtüşmeyebilir; tema ile uyarlanacaktır.

**Yükümlülükler:**
- Özellikler arası içe aktarma yasağı ve sabit metin yasağı ESLint kurallarıyla
  denetlenecektir.
- Ortak bileşenler için Storybook benzeri bir katalog ilk sürümde kapsam dışıdır;
  ihtiyaç doğarsa değerlendirilir.

## Geri dönüş maliyeti

**Orta.** Klasör düzeni ve durum yönetimi yaklaşımı sonradan değiştirilebilir ancak
tüm modülleri etkiler. Bileşen ve tema kararları düşük maliyetle değiştirilebilir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
