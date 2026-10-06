# ADR-0016 — Modül Sınırının Denetimi: Bildirilen Bağımlılık Tablosu

**Durum:** **Kabul Edildi**
**Tarih:** 2026-10-05
**Karar defteri karşılığı:** `KR-100`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)
**Değiştirdiği:** ADR-0002 "Her katman projesinin içinde modüller aynı biçimde bölünür" ve "Modül sınırlarının ihlali nasıl önlenir?" bölümleri. ADR-0002'nin katman kuralları (Kural 1–4) yürürlükte.

---

## Bağlam

ADR-0002 modülleri her katmanda `Modules/<Modül>/` klasörüne yerleştirmeyi öngörüyordu.
Her modülün dışa açık yüzeyi `Modules/<Modül>/Abstractions/` altında toplanacaktı.
Mimari testi de bu ad alanını arıyordu.

Kod bu düzene göre yazılmadı. Modüller katmanın doğrudan altında duruyor
(`Dsg.Hrms.Application.Identity`, `…Personnel`, `…Settings`, `…Notifications`) ve
`Abstractions` katmanı yok. Bu yüzden modül yalıtım testi **hiçbir tipi denetlemeden**
geçiyordu (#171). T3 boyunca modüller arası bağımlılıklar denetimsiz oluştu.

05.10.2026'da yapılan ölçümde modüller arasında 13 bağımlılık bulundu. Biri karşılıklı:
`Identity` kişi ve istihdam bilgisini `Personnel`'den okuyor; `Personnel` senkronizasyonu
istihdamı biten kişinin hesabını pasife alıyor (`KR-015`). T1'in çekirdeği T3 içinde
yazıldığı için (`KR-077`) bu iki modül birlikte gelişti.

## Karar

**Modüller arası her bağımlılık, gerekçesiyle birlikte mimari testindeki bir tabloda
bildirilir. Test, ölçülen bağımlılıkları tabloyla birebir karşılaştırır.**

- **Modül:** Her katmanın doğrudan altındaki iş ad alanıdır (`Dsg.Hrms.<Katman>.<Modül>`). Bugün: `Identity`, `Personnel`, `Organization`, `Notifications`, `Settings`, `Audit`.
- **Ortak ad alanları** (`Common`, `Data`, `Configuration`, `Logging`, `Time`, `Logo`) modül değildir.
- **Yeni üst ad alanı:** Modül ya da ortak olarak sınıflandırılmazsa test düşer. Yeni bir modül sessizce denetim dışında kalamaz.
- **Bildirilmemiş bağımlılık** testi düşürür. Bilinçliyse PR'da gerekçesiyle tabloya eklenir; değilse kaldırılır.
- **Tabloda olup kodda bulunmayan bağımlılık** da testi düşürür. Böylece tablo bayatlamaz.
- **Hiç bağımlılık ölçülemezse** test düşer; test boş geçmez.

Tablo: `src/backend/tests/Dsg.Hrms.Architecture.Tests/LayerDependencyTests.cs`, `AllowedModuleDependencies`.

## Gerekçe

- **Görünürlük:** Modüller arası bağımlılık engellenmiyor ama **görünür ve gerekçeli** hâle geliyor. Yeni bir bağımlılık PR incelemesinde tablo değişikliği olarak görülüyor.
- **Maliyet:** Kodu ADR-0002 düzenine taşımak yüzlerce dosyayı ve ad alanını değiştirirdi. Kazancı, bugün zaten bilinen bağımlılıkların yeni bir klasör altında yeniden görünmesi olurdu.
- **Kanıt:** Ölçülen küme ile bildirilen kümenin eşitliği, tasarım kararının (TEC.5) uygulamada korunduğunun otomatik kanıtıdır. Testin bir ihlali yakaladığı 05.10.2026'da kasıtlı bir bağımlılıkla sınandı.

## Değerlendirilen alternatifler

| Alternatif | Artıları | Eksileri | Neden seçilmedi |
|---|---|---|---|
| Kodu ADR-0002 düzenine (`Modules/<Modül>/Abstractions`) taşımak | Belgedeki düzene uyar; dış yüzey tek yerde | Çok geniş yeniden yapılandırma; T3 kabul adayını değiştirir; mevcut bağımlılıklar yine kalır | Maliyeti kazancından büyük (Doğuş Uçanok, 05.10.2026) |
| Modüller arası bağımlılığı tamamen yasaklamak | En katı sınır | Bugünkü 13 bağımlılığın hepsi meşru; ara katman ve olay altyapısı gerekir | Bugünkü ihtiyaca orantısız |
| Testi silmek | Yanlış güven ortadan kalkar | Sınır hiç denetlenmez | ADR-0002'nin amacı kaybolur |

## Sonuçlar

**Olumlu:**
- Modül sınırı ilk kez gerçekten denetleniyor.
- Bağımlılıkların gerekçesi kodun yanında duruyor.

**Olumsuz / kabul edilen ödünler:**
- Bir modülün "dış yüzeyi" tanımlı değil. Bildirilmiş bir bağımlılık hedef modülün her tipini kullanabilir.
- `Identity` ↔ `Personnel` karşılıklı bağımlılığı kalıcıdır. Bu iki modülü ayrı servise çıkarmak ileride önce bu döngünün çözülmesini gerektirir; örneğin istihdam sonu için bir olay yayımlanabilir.

**Bu kararın getirdiği yükümlülükler:**
- Yeni modül eklenince `Modules` dizisine, yeni teknik ad alanı eklenince `SharedNamespaces` dizisine yazılır.
- Tabloya eklenen her bağımlılık bir gerekçe taşır ve PR açıklamasında anılır.

## Geri dönüş maliyeti

**Düşük.** Karar yalnızca bir testi ve bu belgeyi kapsar. İleride `Abstractions` düzenine
geçilirse test o düzene göre yeniden yazılır.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-05 | 0.1 | İlk oluşturma (#171) | Bilgi İşlem |
