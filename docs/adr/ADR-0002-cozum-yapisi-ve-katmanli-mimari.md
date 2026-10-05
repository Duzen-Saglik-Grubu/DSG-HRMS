# ADR-0002 — Çözüm Yapısı ve Katmanlı Mimari

**Durum:** Kabul Edildi — modül düzeni ve modül sınırının denetimi **ADR-0016 ile değiştirildi** (05.10.2026)
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-012`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)

---

## Bağlam

Mevcut HRMS tek projeli bir ASP.NET Core MVC uygulamasıdır: `Controllers`, `Services`,
`Context`, `Dto`, `Models` aynı proje içinde, katman sınırı olmadan durmaktadır. Bu yapı
büyüdükçe test edilemez ve bakılamaz hâle gelmiştir (`KR-002`).

Yeni sistemde katmanlı mimari isteniyor. Ancak diğer uçta da bir tuzak var: her modül için
ayrı proje açan ağır bir Clean Architecture kurgusu, 40+ `.csproj` dosyası, uzun derleme
süreleri ve tek geliştiricinin taşıyamayacağı bir yönetim yükü üretir.

Ekip büyüklüğü ve iş hacmi (≈600 aktif kullanıcı, tek geliştirme kanalı) mikroservis
mimarisini de gereksiz kılmaktadır.

## Karar

**Modüler monolit.** Tek dağıtılabilir uygulama, dört katman projesi, modüller klasör ve
ad alanı (namespace) ile ayrılmış.

```
src/
├── backend/
│   ├── Dsg.Hrms.Domain/           → varlıklar, değer nesneleri, iş kuralları, arayüzler
│   ├── Dsg.Hrms.Application/      → kullanım senaryoları, DTO, doğrulama, servis arayüzleri
│   ├── Dsg.Hrms.Infrastructure/   → EF Core, LOGO erişimi, dosya, SMS, e-posta, dış sistemler
│   ├── Dsg.Hrms.Api/              → uç noktalar, kimlik doğrulama, ara katmanlar, OpenAPI
│   └── tests/
│       ├── Dsg.Hrms.Domain.Tests/
│       ├── Dsg.Hrms.Application.Tests/
│       └── Dsg.Hrms.Api.IntegrationTests/
└── frontend/
    └── dsg-hrms-web/              → React + TypeScript + Vite
```

Her katman projesinin içinde modüller aynı biçimde bölünür:

```
Dsg.Hrms.Application/
├── Common/                 → ortak davranışlar, ara katmanlar, soyutlamalar
└── Modules/
    ├── Identity/           → kullanıcı, üyelik, oturum
    ├── Organization/       → firma, şube, birim, hiyerarşi
    ├── Personnel/          → kişi, istihdam
    ├── Leave/              → izin
    ├── Training/           → eğitim
    └── ...
```

### Bağımlılık kuralları

```
Api  →  Application  →  Domain
 └────  Infrastructure  ─────┘
```

1. **Domain hiçbir projeye bağımlı değildir.** Ne EF Core, ne ASP.NET, ne dış kütüphane.
2. **Application yalnızca Domain'e bağımlıdır.** Dış dünyaya erişim arayüzlerle tanımlanır
   (`ILogoPersonnelSource`, `IFileStorage`, `ISmsSender`, `IUnitOfWork` …).
3. **Infrastructure, Application ve Domain'e bağımlıdır**; arayüzlerin uygulamalarını içerir.
4. **Api yalnızca Application'a bağımlıdır**; Infrastructure'a sadece bağımlılık kaydı
   (DI) için, başlangıç dosyasında dokunur.
5. **Modüller arası doğrudan erişim yoktur.** Bir modül başka bir modülün iç sınıflarını
   kullanamaz; yalnızca o modülün açıkça yayımladığı arayüz üzerinden konuşur.

Bu kurallar **mimari testi** ile otomatik doğrulanacaktır (NetArchTest veya eşdeğeri);
kural ihlali derlemeyi değil, **testi** kırar ve PR birleştirilemez.

### Modül sınırlarının ihlali nasıl önlenir?

- Her modülün dışa açık yüzeyi `Modules/<Modül>/Abstractions/` altında toplanır.
- Diğer her şey `internal` görünürlüktedir.
- Mimari testi hem katman hem modül bağımlılıklarını denetler.

## Gerekçe

- **Tek dağıtım birimi**, işletim karmaşıklığını düşük tutar: tek konteyner, tek sürüm,
  tek veritabanı işlemi (transaction). Dağıtık işlem yönetimi gerekmez.
- **Katman ayrımı**, iş kurallarının (izin hakedişi, kıdem hesabı) altyapıdan bağımsız
  ve saf biçimde test edilmesini sağlar. Bu, `KR-011` ile birlikte projenin en kritik
  kalite noktasıdır.
- **Modül sınırları**, ileride bir modülün ayrılması gerekirse yolu açık tutar; ancak
  bugün bedelini ödemez (YAGNI).
- **Dört proje**, derleme süresini makul tutar ve tek geliştiricinin yönetebileceği
  bir yapıdır (KISS).

## Değerlendirilen alternatifler

| Alternatif | Artıları | Eksileri | Neden seçilmedi |
|---|---|---|---|
| Tek projeli katmansız yapı (mevcut sistem) | Hızlı başlangıç | Test edilemez, bakılamaz | `KR-002` ile reddedildi |
| Modül başına ayrı proje seti (ağır Clean Architecture) | Katı sınırlar | 40+ proje, uzun derleme, yüksek yönetim yükü | Ekip büyüklüğüne göre orantısız |
| Mikroservis | Bağımsız ölçekleme ve dağıtım | Dağıtık işlem, servis keşfi, izleme, dağıtım karmaşıklığı | Maliyeti faydasından fazla; ölçek gerektirmiyor |
| Dikey dilim (vertical slice) mimarisi | Modül içi tutarlılık | Katmanlar arası ortak kuralları zayıflatır | Katmanlı yapı, 33061 tasarım kanıtı için daha net izlenebilirlik veriyor |

## Sonuçlar

**Olumlu:**
- İş kuralları veritabanı ve HTTP'den bağımsız test edilebilir.
- Mimari kurallar otomatik denetlenir; zamanla aşınmaz.
- İşletim basit kalır: tek konteyner, tek sürüm etiketi.

**Olumsuz / kabul edilen ödünler:**
- Bir modülün başka bir modülün verisine ihtiyacı olduğunda arayüz tanımlamak gerekir;
  doğrudan `JOIN` yazmaktan daha fazla iş çıkarır. Bu, bilinçli bir ödündür.
- Tek dağıtım birimi olduğu için her sürüm tüm modülleri kapsar; kısmi dağıtım yapılamaz.

**Yükümlülükler:**
- Mimari testi iskeletle birlikte yazılacak ve CI kalite kapısına eklenecektir.
- Yeni modül eklenirken aynı klasör düzeni korunacaktır.

## Geri dönüş maliyeti

**Orta.** Modül sınırları korunduğu sürece bir modülün ayrı servise çıkarılması
mümkündür. Katman yapısından dönmek ise pratikte yeniden yazım demektir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
| 2026-10-05 | 0.2 | Durum: modül düzeni ve modül sınırı denetimi ADR-0016 ile değiştirildi (`KR-099`, #171) | Bilgi İşlem |
