# ADR-0007 — Yetkilendirme Modeli

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-021`, `KR-022`, `KR-089`
**İlgili süreç:** TEC.5 (Tasarım Tanımlama)
**İlgili riskler:** `R-03`

---

## Bağlam

Yetkilendirme, İK sisteminde iki ayrı sorunun cevabıdır ve bu ikisi birbirine
karıştırılırsa ilerleyen modüllerde çözümsüz öncelik tartışmaları doğar:

1. **Kullanıcı ne yapabilir?** (görüntüle, oluştur, güncelle, sil, raporla, dışa aktar)
2. **Kimin verisi üzerinde yapabilir?** (kendisi, kendine bağlı personel, tüm kurum)

Ek olarak KVKK gereği (`R-03`) satır bazlı erişim zorunludur: bir şube sorumlusu tüm
kurumun özlük verisini görememelidir.

## Karar

**Etkin yetki = Rol (eylem) ∩ Kapsam (satır)** — iki eksen ayrı tutulur, kesişimleri
uygulanır (`KR-021`).

### 1. Eksen 1 — Rol tabanlı eylem yetkisi

```
user_account *───* role *───* permission
```

**İzin (permission) tanımı:** `<modül>.<kaynak>.<eylem>`

| Eylem | Anlamı |
|---|---|
| `view` | Görüntüleme |
| `create` | Oluşturma |
| `update` | Güncelleme |
| `delete` | Silme |
| `report` | Raporlama |
| `export` | Dışa aktarma (Excel/PDF) |
| `approve` | Onaylama (onay akışı olan modüllerde) |

Örnek: `leave.request.approve`, `personnel.person.export`

**Kurallar:**
- İzinler **sabit ve kodda tanımlıdır** (migration ile seed edilir); kullanıcı yeni izin
  türü **üretemez**. Böylece kodda `[HasPermission("leave.request.approve")]` yazıldığında
  karşılığı garanti edilir.
- **Roller yönetilebilir**: İK yeni rol oluşturabilir, rollere izin atayabilir,
  kullanıcıya rol verebilir.
- Bir kullanıcı **birden fazla role** sahip olabilir; izinler birleşir (union).
- **Reddetme (deny) izni yoktur.** Karmaşıklık üretir ve hata kaynağıdır; yetki
  yalnızca verilir.
- `export` izni `view`'dan **ayrıdır**. Veriyi ekranda görebilmek, dışa aktarabilmek
  anlamına gelmez — KVKK açısından dışa aktarma ayrı bir risktir.

> **Gerçekleştirme (29.09.2026, `KR-089`, #100).** Tablolar `identity.permission`,
> `identity.role`, `identity.role_permission`, `identity.user_role`. İzinler ve hazır iki rol
> (Sistem Yöneticisi, İK Kimlik İşlemleri) migration ile yüklenir; liste
> `docs/mimari/izin-listesi.md`. İzinler her istekte veritabanından okunur, erişim jetonunda
> taşınmaz. İlk sistem yöneticisi `AccessControl__BootstrapAdministrators` ile atanır. Satır
> bazlı kapsam (eksen 2) T4 kapsamındadır.

### 2. Eksen 2 — Satır bazlı kapsam

Kapsam, kullanıcının **hangi kişilerin** verisine erişebileceğini belirler.

| Kapsam türü | Anlamı | Tipik kullanıcı |
|---|---|---|
| `Self` | Yalnızca kendisi | Tüm personel (varsayılan) |
| `DirectReports` | Kendisi + **doğrudan atanmış** astları | Birim sorumlusu, birim teknik sorumlusu |
| `Branch` | Belirli şube(ler) | Şube İK sorumlusu |
| `Company` | Belirli firma(lar) | Firma İK sorumlusu |
| `All` | Tüm kurum | İK yöneticisi, sistem yöneticisi |

**`DirectReports` kapsamının tanımı kritiktir (`KR-022`):**

> Kullanıcı yalnızca **kendisine doğrudan atanmış** personeli görür. Astının astı,
> kendisine ayrıca atanmamışsa **görünmez**.

İK biriminin tanımladığı örnek: A'nın altına B, C, D, E atanmış; B'nin altına D, E
atanmış. **A → B, C, D, E görür. B → D, E görür.** A, D'yi B üzerinden değil, kendi
doğrudan ataması sayesinde görür.

**Teknik sonuç:** Özyinelemeli sorgu (recursive CTE) **gerekmez**; düz kenar listesi
yeterlidir. Bu hem doğru hem hızlıdır.

Kapsam hesabı, **tarih aralıklı** yönetici atama tablosuna dayanır (ADR-0005 §4);
geçmişe dönük sorgularda o tarihteki atama kullanılır.

### 3. Uygulama noktası

```
İstek → Kimlik doğrulama → Eylem yetkisi kontrolü → Sorgu + kapsam filtresi → Yanıt
                              (rol/izin)                  (satır bazlı)
```

- **Eylem yetkisi**, uç nokta üzerinde bildirimsel olarak (`[HasPermission(...)]`)
  kontrol edilir.
- **Kapsam filtresi**, sorgu katmanında **otomatik** uygulanır. Her serviste elle
  yazılmaz; kapsam bilgisi bir `ICurrentUserScope` soyutlamasından okunur ve sorgulara
  merkezî olarak eklenir.

> **Kritik kural:** Kapsam filtresi **veri katmanında** uygulanır, sunum katmanında
> değil. "Ekranda göstermiyoruz" yeterli değildir; API doğrudan çağrıldığında da veri
> dönmemelidir.

Bu kuralı doğrulamak için her modülde **yetki sızıntısı testi** yazılır: kapsam dışı
bir kaydın kimliğiyle yapılan istek `404` döndürmelidir.

### 4. `403` yerine `404`

Kapsam dışı bir kayda erişim denendiğinde **`403 Forbidden` değil `404 Not Found`**
döndürülür. Gerekçe: `403`, "böyle bir kayıt var ama göremezsin" bilgisini sızdırır.
Bir yönetici, ardışık kimlikler deneyerek kaç personel olduğunu çıkarabilir.

Eylem yetkisi olmayan (rol kaynaklı) durumlarda `403` döndürülür — orada sızdırılacak
bir bilgi yoktur.

### 5. Kendi verisi üzerindeki özel durum

Her personel, rol yetkisinden bağımsız olarak **kendi** verisini görebilir ve kendi
izin talebini oluşturabilir. Bu, `Self` kapsamının varsayılan olarak herkese verilmesiyle
sağlanır; ayrı bir istisna mekanizması kurulmaz.

Ancak kendi verisi üzerinde de her şey serbest değildir: örneğin kişi kendi izin
talebini **onaylayamaz**. Bu tür kurallar iş kuralı olarak Domain katmanında tanımlanır,
yetki modelinde değil.

### 6. Yönetici hesabı

Sistem yöneticisi rolü **kapsam olarak `All`** ve tüm izinlere sahiptir. Ancak:
- Bu rolün her eylemi denetim kaydına yazılır (ADR-0009).
- Rolün kaç kullanıcıya atandığı yönetim ekranında görünür ve raporlanır.

## Gerekçe

- İki eksenin ayrılması, "rol mü kazandı kapsam mı" tartışmasını baştan ortadan kaldırır.
- İzinlerin sabit, rollerin yönetilebilir olması, hem kod güvenliğini hem İK'nın
  esnekliğini birlikte sağlar.
- `export` izninin ayrılması, KVKK açısından somut bir kontroldür: veri dışarı çıkarma
  yetkisi ayrıca verilir ve ayrıca loglanır.
- `404` tercihi düşük maliyetli, etkili bir bilgi sızıntısı önlemidir.
- Kapsam filtresinin veri katmanında olması, en sık görülen yetkilendirme açığını
  (arayüzde gizlenmiş ama API'de açık veri) yapısal olarak engeller.

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| Tek eksen (yalnızca rol) | Satır bazlı erişim sağlanamaz; KVKK ihlali |
| Rol içine kapsamı gömmek ("Şube İK Sorumlusu" rolü) | Her şube için ayrı rol gerekir; 32 şube × rol sayısı kadar rol patlaması |
| Özyinelemeli (tüm alt kırılım) kapsam | İK'nın tanımladığı düzene aykırı; ayrıca sorgu maliyeti |
| Reddetme (deny) izinleri | Çakışma çözümü karmaşık, hata kaynağı |
| Kapsam filtresini sunum katmanında uygulamak | API doğrudan çağrıldığında veri sızar |
| `403` döndürmek | Kayıt varlığını sızdırır |

## Sonuçlar

**Olumlu:** Esnek, denetlenebilir, KVKK uyumlu; yeni modül eklendiğinde yalnızca izin
tanımları eklenir, model değişmez.

**Olumsuz / kabul edilen ödünler:**
- Kapsam filtresinin merkezî uygulanması dikkat gerektirir; unutulan bir sorgu açık
  bırakır. Bu nedenle yetki sızıntısı testleri **her modülde zorunludur**.
- `DirectReports` kapsamı doğrudan atamaya dayandığı için İK'nın atamaları eksiksiz
  yapması gerekir; eksik atama, yöneticinin astını görememesi olarak ortaya çıkar.

**Yükümlülükler:**
- Her modülde yetki sızıntısı testi yazılacak; kod gözden geçirme kontrol listesinde
  madde olacaktır.
- İzin listesi `docs/mimari/izin-listesi.md` altında güncel tutulacaktır.
- Rol ve yetki değişiklikleri denetim kaydına yazılacaktır.

## Geri dönüş maliyeti

**Orta.** Kapsam türleri genişletilebilir (örneğin ileride özyinelemeli kapsam
eklenmesi istenirse yeni bir kapsam türü olarak eklenir, mevcut davranış bozulmaz).
İki eksenli yapıdan dönmek ise tüm modülleri etkiler.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
| 2026-09-29 | 0.2 | §1: eylem yetkisinin gerçekleştirme notu (`KR-089`, #100) | Bilgi İşlem |
