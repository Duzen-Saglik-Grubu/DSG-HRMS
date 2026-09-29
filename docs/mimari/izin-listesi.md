# İzin Listesi

**Belge kimliği:** MIM-IZIN
**Son güncelleme:** 2026-09-29
**Sahibi:** Bilgi İşlem
**İlgili:** ADR-0007 §1, `KR-021`, `KR-089`, SYG-KMLK-074

> Bu liste, koddaki sabit izin listesinin (`Application/Identity/Authorization/Permissions.cs`)
> belgesidir. İzinler **kodda tanımlıdır** ve migration ile veritabanına yüklenir; kullanıcı
> yeni izin üretemez. Kodda bir izin eklendiğinde ya da kaldırıldığında bu belge **aynı
> PR'da** güncellenir (ADR-0007 yükümlülüğü).

## Biçim

`<modül>.<kaynak>.<eylem>`. Eylemler: `view`, `create`, `update`, `delete`, `report`, `export`, `approve`.

- `export` ve `view` ayrı izinlerdir. Veriyi ekranda görebilmek, dışa aktarabilmek anlamına gelmez.
- Reddetme (deny) izni yoktur. Kullanıcının izinleri, rollerinin izinlerinin birleşimidir.

## İzinler

| İzin | Anlamı | Kullanıldığı yer | SYG |
|---|---|---|---|
| `identity.account.view` | Hesap durumunu görme | İK hesap işlemleri ekranı (sonraki iş) | 073 |
| `identity.account.update` | Hesabı pasife alma ve yeniden aktifleştirme | İK hesap işlemleri ekranı (sonraki iş) | 057, 073 |
| `identity.invite.create` | Parola oluşturma bağlantısı gönderme | İK davet (sonraki iş) | 051 |
| `identity.sync.view` | Senkronizasyonun son çalışma bilgisini görme | `GET /api/v1/identity/sync-runs/latest` | 072 |
| `identity.sync.create` | Senkronizasyonu elle başlatma | `POST /api/v1/identity/sync-runs` | 072 |
| `system.parameter.view` | Sistem parametrelerini görme | Parametre ekranı (sonraki iş) | 076 |
| `system.parameter.update` | Sistem parametrelerini değiştirme | Parametre ekranı (sonraki iş) | 076 |

## Hazır roller

| Rol | Kod | İzinler |
|---|---|---|
| **Sistem Yöneticisi** | `system-administrator` | Tümü |
| **İK Kimlik İşlemleri** | `hr-identity-operations` | `identity.account.view`, `identity.account.update`, `identity.invite.create`, `identity.sync.view` |

- Rol yönetim ekranı ve satır bazlı kapsam **T4 kapsamındadır**. O zamana kadar İK rolünün kapsamı tüm personeldir (SYG-KMLK-074).
- İlk sistem yöneticisi, kurulum yapılandırmasındaki `AccessControl__BootstrapAdministrators` listesiyle belirlenir (runbook §11). Listedeki e-postayla giriş yapan hesaba rol bir kez atanır; atama denetim izine yazılır.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-29 | 0.1 | İlk oluşturma: T3 izinleri ve hazır iki rol (#100) | Bilgi İşlem |
