# İzin Listesi

**Belge kimliği:** MIM-IZIN
**Son güncelleme:** 2026-10-22
**Sahibi:** Bilgi İşlem
**İlgili:** ADR-0007 §1, ADR-0019 §4, `KR-021`, `KR-089`, `KR-126`, SYG-KMLK-074, SYG-PERS-055

> Bu liste, koddaki sabit izin listesinin (`Application/Identity/Authorization/Permissions.cs`)
> belgesidir. İzinler **kodda tanımlıdır** ve migration ile veritabanına yüklenir; kullanıcı
> yeni izin üretemez. Kodda bir izin eklendiğinde ya da kaldırıldığında bu belge **aynı
> PR'da** güncellenir (ADR-0007 yükümlülüğü).

## Biçim

`<modül>.<kaynak>.<eylem>`. Eylemler: `view`, `create`, `update`, `delete`, `report`, `export`, `approve`; ayrıca alan düzeyinde görme için `view_<alan>` (ör. `personnel.person.view_sensitive`; ADR-0007 §1, ADR-0019 §4).

- `export` ve `view` ayrı izinlerdir. Veriyi ekranda görebilmek, dışa aktarabilmek anlamına gelmez.
- Reddetme (deny) izni yoktur. Kullanıcının izinleri, rollerinin izinlerinin birleşimidir.

## İzinler

| İzin | Anlamı | Kullanıldığı yer | SYG |
|---|---|---|---|
| `identity.account.view` | Hesap durumunu görme | İK hesap işlemleri ekranı (sonraki iş) | 073 |
| `identity.account.update` | Hesabı pasife alma ve yeniden aktifleştirme | İK hesap işlemleri ekranı (sonraki iş) | 057, 073 |
| `identity.invite.create` | Parola oluşturma bağlantısı gönderme | İK davet (sonraki iş) | 051 |
| `personnel.sync.view` | Senkronizasyon durumunu görme (T3'te `identity.sync.view`) | `GET /api/v1/identity/sync-runs/latest` | KMLK-072; PERS-008 |
| `personnel.sync.create` | Senkronizasyonu elle başlatma (T3'te `identity.sync.create`) | `POST /api/v1/identity/sync-runs` | KMLK-072; PERS-009 |
| `system.parameter.view` | Sistem parametrelerini görme | Parametre ekranı (sonraki iş) | 076 |
| `system.parameter.update` | Sistem parametrelerini değiştirme | Parametre ekranı (sonraki iş) | 076 |
| `personnel.person.view` | Tüm personeli görme (kapsam `All`); yoksa yalnızca kendisi | Kişi okumaları (`IPersonnelQuery`) | PERS-050 |
| `personnel.person.view_national_id` | Tam T.C. kimlik numarasını görme ve TCKN ile arama | Kişi kartı; liste araması (sonraki iş) | PERS-030, 053 |
| `personnel.person.view_sensitive` | Adres, kan grubu, acil durum kişileri ve evlilik bilgisini görme | Kişi kartı, süzgeç, dışa aktarma (sonraki iş) | PERS-052 |
| `personnel.person.update` | Görev, evlilik bilgisi ve acil durum kişisini girme | Kişi kartı (sonraki iş) | PERS-036, 042, 043 |
| `personnel.person.export` | Personel listesini dışa aktarma | Liste (sonraki iş) | PERS-032 |
| `personnel.correction.view` | "İK'ya bildir" taleplerini görme | Düzeltme talepleri (sonraki iş) | PERS-048 |
| `personnel.correction.update` | "İK'ya bildir" taleplerini kapatma | Düzeltme talepleri (sonraki iş) | PERS-048 |
| `personnel.quality.view` | Veri kalitesi raporunu görme | Veri kalitesi (sonraki iş) | PERS-070 |
| `personnel.quality.export` | Veri kalitesi raporunu dışa aktarma | Veri kalitesi (sonraki iş) | PERS-070 |
| `reference.definition.view` | Ünvan ve görev tanımlarını görme | Referans veri (sonraki iş) | PERS-072 |
| `reference.definition.update` | Ünvan ve görev tanımlarını değiştirme | Referans veri (sonraki iş) | PERS-072 |
| `audit.log.view` | Denetim kayıtlarını görme | Denetim kayıtları (sonraki iş) | PERS-069 |

Kişi kartı ve "Profilim" (`GET /api/v1/personnel/persons/{id}`, `/me`) izin değil yalnızca oturum ister; kimin kimi göreceğini kapsam belirler (ADR-0019 §1).

## Hazır roller

| Rol | Kod | İzinler |
|---|---|---|
| **Sistem Yöneticisi** | `system-administrator` | Tümü |
| **İnsan Kaynakları** (T3'te "İK Kimlik İşlemleri") | `hr-identity-operations` | `identity.*` (üçü), `personnel.*` (tümü), `reference.*` (ikisi). Denetim kayıtları ve parametreler yok (`KR-126`) |

- Rol yönetim ekranı **T4 kapsamındadır**. Satır bazlı kapsam T1'de iki değerlidir: `personnel.person.view` izni olan tüm personeli, olmayan yalnızca kendini görür (SYG-PERS-050). Doğrudan ast kapsamı T2'de eklenir.
- İlk sistem yöneticisi, kurulum yapılandırmasındaki `AccessControl__BootstrapAdministrators` listesiyle belirlenir (runbook §11). Listedeki e-postayla giriş yapan hesaba rol bir kez atanır; atama denetim izine yazılır.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-29 | 0.1 | İlk oluşturma: T3 izinleri ve hazır iki rol (#100) | Bilgi İşlem |
| 2026-10-22 | 0.2 | Biçim: `view_<alan>` eylemi; T1 izinlerine atıf (ADR-0019 §4, #268) | Bilgi İşlem |
| 2026-10-22 | 0.3 | T1 izinleri eklendi; `identity.sync.*` → `personnel.sync.*`; İK rolünün adı "İnsan Kaynakları", elle senkronizasyon İK'da (`KR-126`, #276) | Bilgi İşlem |
