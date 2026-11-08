# İzin Listesi

**Belge kimliği:** MIM-IZIN
**Son güncelleme:** 2026-11-08
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
| `personnel.sync.view` | Senkronizasyon durumunu görme (T3'te `identity.sync.view`) | `GET /api/v1/identity/sync-runs/latest`; `GET /api/v1/personnel/sync-runs` | KMLK-072; PERS-008 |
| `personnel.sync.create` | Senkronizasyonu elle başlatma (T3'te `identity.sync.create`) | `POST /api/v1/identity/sync-runs`; `POST /api/v1/personnel/sync-runs` | KMLK-072; PERS-009 |
| `system.parameter.view` | Sistem parametrelerini görme | Parametre ekranı (sonraki iş) | 076 |
| `system.parameter.update` | Sistem parametrelerini değiştirme | Parametre ekranı (sonraki iş) | 076 |
| `personnel.person.view` | Tüm personeli görme (kapsam `All`); yoksa yalnızca kendisi | Kişi okumaları (`IPersonnelQuery`) | PERS-050 |
| `personnel.person.view_national_id` | Tam T.C. kimlik numarasını görme ve TCKN ile arama | Kişi kartı; liste araması (sonraki iş) | PERS-030, 053 |
| `personnel.person.view_sensitive` | Adres, kan grubu, acil durum kişileri ve evlilik bilgisini görme | Kişi kartı (evlilik bölümü yalnızca bu izinle, #361), süzgeç (adres, kan grubu, evlilik yardımı ve tarihi, acil durum kişisi; #371), dışa aktarma | PERS-052 |
| `personnel.person.update` | Görev, evlilik bilgisi ve acil durum kişisini girme; acil durum formunun yakınlık seçeneklerini (`KR-135`) ve görev formunun görev seçeneklerini okuma | Görev uçları ve `GET /api/v1/personnel/duty-options`; evlilik uçları `PUT`/`DELETE /api/v1/personnel/persons/{personId}/marriage` (#361); kişi kartının "Acil Durum Kişileri" bölümü (#346), "Çalışma Hayatı" bölümündeki "Görevi" alanı (#353) ve "Genel Bilgiler" bölümündeki evlilik bilgisi (#361) | PERS-036, 042, 043 |
| `personnel.person.export` | Personel listesini dışa aktarma | `GET /personnel/persons/export`, `/export/fields` (#330) | PERS-032 |
| `personnel.correction.view` | "İK'ya bildir" taleplerini görme | Düzeltme talepleri (sonraki iş) | PERS-048 |
| `personnel.correction.update` | "İK'ya bildir" taleplerini kapatma | Düzeltme talepleri (sonraki iş) | PERS-048 |
| `personnel.quality.view` | Veri kalitesi raporunu görme | `GET /personnel/quality`, `/quality/ineligible` (#335) | PERS-070, 071 |
| `personnel.quality.export` | Veri kalitesi raporunu dışa aktarma | `GET /personnel/quality/export` (#335) | PERS-070 |
| `reference.definition.view` | Ünvan, görev, kan grubu ve yakınlık tanımlarını görme | `GET /reference/definitions`; "Referans veri" ekranı (#350) | PERS-072 |
| `reference.definition.update` | Ünvan, görev ve yakınlık tanımlarını değiştirme | `POST /reference/definitions`, `PUT …/{id}/name`, `POST …/{id}/deactivation`, `POST …/{id}/reactivation` (`KR-136`); "Referans veri" ekranı (#350) | PERS-072 |
| `audit.log.view` | Denetim kayıtlarını görme | `GET /api/v1/audit/entries`; Sistem → "Denetim kayıtları" ekranı (#363) | PERS-069 |
| `system.encryption.update` | Özel nitelikli veri şifrelemesini yönetme (yeniden şifreleme) | `GET /api/v1/personnel/encryption`, `POST /api/v1/personnel/encryption/re-encryption`; Sistem → Parametreler, "Özel nitelikli veri şifrelemesi" (#375). **Yalnızca Sistem Yöneticisi**; İK rolünde yok (`KR-138`) | PERS-062 |

Kişi kartı ve "Profilim" (`GET /api/v1/personnel/persons/{id}`, `/me`) izin değil yalnızca oturum ister; kimin kimi göreceğini kapsam belirler (ADR-0019 §1).

## Hazır roller

| Rol | Kod | İzinler |
|---|---|---|
| **Sistem Yöneticisi** | `system-administrator` | Tümü |
| **İnsan Kaynakları** (T3'te "İK Kimlik İşlemleri") | `hr-identity-operations` | `identity.*` (üçü), `personnel.*` (tümü), `reference.*` (ikisi), `system.parameter.*`, `audit.log.view`; bugün Sistem Yöneticisiyle aynı (`KR-126`, `KR-128`) |

- Rol yönetim ekranı **T4 kapsamındadır**. Satır bazlı kapsam T1'de iki değerlidir: `personnel.person.view` izni olan tüm personeli, olmayan yalnızca kendini görür (SYG-PERS-050). Doğrudan ast kapsamı T2'de eklenir.
- **T4 yönü (`KR-128`):** İzinler istenen rollere, roller istenen kullanıcılara atanabilecek. O zamana kadar rol dağılımı koddadır ve migration ile değişir.
- İlk sistem yöneticisi, kurulum yapılandırmasındaki `AccessControl__BootstrapAdministrators` listesiyle belirlenir (runbook §11). Listedeki e-postayla giriş yapan hesaba rol bir kez atanır; atama denetim izine yazılır.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-29 | 0.1 | İlk oluşturma: T3 izinleri ve hazır iki rol (#100) | Bilgi İşlem |
| 2026-10-22 | 0.2 | Biçim: `view_<alan>` eylemi; T1 izinlerine atıf (ADR-0019 §4, #268) | Bilgi İşlem |
| 2026-10-22 | 0.3 | T1 izinleri eklendi; `identity.sync.*` → `personnel.sync.*`; İK rolünün adı "İnsan Kaynakları", elle senkronizasyon İK'da (`KR-126`, #276) | Bilgi İşlem |
| 2026-10-23 | 0.4 | İK rolüne `audit.log.view` ve `system.parameter.*`; T4 yönü (`KR-128`, #278) | Bilgi İşlem |
| 2026-11-04 | 0.5 | `personnel.person.export` uçlara bağlandı (#330) | Bilgi İşlem |
| 2026-11-05 | 0.6 | `personnel.quality.view` ve `.export` uçlara bağlandı (#335) | Bilgi İşlem |
| 2026-11-05 | 0.7 | `personnel.sync.view` ve `.create` personel uçlarına da bağlandı (#340) | Bilgi İşlem |
| 2026-11-05 | 0.8 | `personnel.person.update` acil durum kişisi uçlarına ve kişi kartına bağlandı (#346) | Bilgi İşlem |
| 2026-11-05 | 0.9 | Yakınlık tanımları ve seçenek ucu (`KR-135`, #348) | Bilgi İşlem |
| 2026-11-06 | 0.10 | `reference.definition.*` ekrana bağlandı; veritabanındaki izin açıklamaları bu listeyle eşitlendi (#350); yeniden aktif etme ucu (`KR-136`, #352) | Bilgi İşlem |
| 2026-11-06 | 0.11 | `personnel.person.update`: görev seçenek ucu ve kartta görev girişi (#353) | Bilgi İşlem |
| 2026-11-07 | 0.12 | `personnel.person.update` evlilik uçlarına, `view_sensitive` kartın evlilik bölümüne bağlandı (#361) | Bilgi İşlem |
| 2026-11-07 | 0.13 | `audit.log.view` denetim kayıtları ucuna ve ekranına bağlandı (#363) | Bilgi İşlem |
| 2026-11-08 | 0.14 | `view_sensitive` kan grubu, evlilik ve acil durum kişisi süzgeçlerine bağlandı (#371) | Bilgi İşlem |
| 2026-11-08 | 0.15 | `system.encryption.update` eklendi; yalnızca Sistem Yöneticisi (`KR-138`, #374, #375) | Bilgi İşlem |
