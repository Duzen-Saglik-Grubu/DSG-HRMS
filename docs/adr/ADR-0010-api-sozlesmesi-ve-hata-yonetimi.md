# ADR-0010 — API Sözleşmesi ve Hata Yönetimi

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**İlgili süreç:** TEC.5 (Tasarım), TEC.8 (Entegrasyon)

---

## Bağlam

Backend ve frontend birlikte geliştirilecektir. İkisi arasındaki sözleşme belirsizse
her modülde yeniden tartışılır, hata işleme her ekranda farklı yazılır ve tutarsız bir
kullanıcı deneyimi ortaya çıkar.

Ayrıca API sözleşmesi, 33061 TEC.5'in *"System/software interface definition"* çıktısıdır;
belgelenmesi bir tasarım kanıtıdır.

## Karar

### 1. Sözleşme kaynağı: OpenAPI

- API, **OpenAPI** belgesi üretir; bu belge tek doğruluk kaynağıdır.
- Frontend'in TypeScript tipleri bu belgeden **otomatik üretilir**; elle yazılmaz.
  Böylece backend'de değişen bir alan, frontend'de derleme hatası olarak ortaya çıkar.
- OpenAPI belgesi CI'da üretilir ve konfigürasyon öğesi olarak saklanır (MAN.5).

### 2. URL ve sürümleme

```
/api/v1/<modül>/<kaynak>
```

- Kaynak adları **çoğul ve kebab-case**: `/api/v1/leave/leave-requests`
- Sürüm URL'de taşınır (`v1`). **Kırıcı değişiklik** yeni sürüm gerektirir; mevcut
  sürüm bir geçiş dönemi boyunca çalışmaya devam eder.
- Kırıcı olmayan ekleme (yeni alan, yeni uç nokta) sürüm artırmaz.

### 3. Kimlikler

Dışa açık kimlik olarak **`public_id` (uuid)** kullanılır (ADR-0004 §2). Veritabanı
`bigint` kimliği API'de görünmez; kayıt sayısı ve oluşturma sırası sızmaz.

### 4. Listeleme

Tüm listeleme uç noktaları **sayfalama zorunludur**:

```
GET /api/v1/personnel/persons?page=1&pageSize=25&sort=lastName&order=asc&q=...
```

```json
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 584,
  "totalPages": 24
}
```

- `pageSize` üst sınırı **100**'dür; aşan istek `400` alır.
- Sayfasız liste dönen uç nokta yazılmaz.

### 5. Hata yanıtı: RFC 9457 Problem Details

Tüm hatalar tek bir biçimde döner:

```json
{
  "type": "https://dsg-hrms/errors/validation",
  "title": "Doğrulama hatası",
  "status": 400,
  "detail": "Gönderilen veri geçerli değil.",
  "instance": "/api/v1/leave/leave-requests",
  "traceId": "00-4bf92f...-01",
  "errors": {
    "startDate": ["Başlangıç tarihi geçmiş bir tarih olamaz."]
  }
}
```

**Kurallar:**
- `traceId` her yanıtta bulunur; kullanıcı hata ekranında bunu görebilir ve destek
  talebinde iletebilir. Log kayıtlarıyla eşleştirme bu alan üzerinden yapılır.
- **Hata mesajları Türkçe** ve kullanıcıya yöneliktir.
- **İç ayrıntı sızdırılmaz:** istisna yığını (stack trace), SQL metni, tablo adı,
  dosya yolu asla yanıta konmaz. Bunlar yalnızca sunucu log'una yazılır.
- Beklenmeyen hatalarda `500` ve genel bir mesaj döner; ayrıntı `traceId` ile log'da
  aranır.

### 6. Durum kodları

| Kod | Kullanım |
|---|---|
| `200` | Başarılı okuma/güncelleme |
| `201` | Oluşturma (`Location` başlığı ile) |
| `204` | İçerik dönmeyen başarılı işlem |
| `400` | Doğrulama hatası |
| `401` | Kimlik doğrulanmamış veya jeton geçersiz |
| `403` | **Eylem yetkisi yok** (rol kaynaklı) |
| `404` | Kayıt yok **veya kapsam dışı** (ADR-0007 §4) |
| `409` | İş kuralı çakışması veya eşzamanlılık çakışması |
| `422` | Sözdizimi doğru ama iş kuralı ihlali |
| `429` | Hız sınırı aşıldı |

`403` ile `404` ayrımı bilinçlidir: kapsam dışı kayıt `404` döner ki kaydın varlığı
sızmasın.

### 7. Eşzamanlılık

Güncelleme isteklerinde satır sürümü (`row_version`) gönderilir. Kayıt bu arada
değiştiyse `409` döner ve kullanıcıya *"Bu kayıt siz görüntülerken başkası tarafından
güncellendi"* mesajı gösterilir. Sessiz üzerine yazma **yapılmaz**.

### 8. Doğrulama

- Backend doğrulaması **FluentValidation** ile; tek doğruluk kaynağı budur.
- Frontend doğrulaması **Zod** ile; yalnızca kullanıcı deneyimi içindir.
- Frontend doğrulaması backend'i **asla ikame etmez**; her istek sunucuda yeniden
  doğrulanır.
- Doğrulama hataları `errors` sözlüğünde alan bazında döner; frontend bunları doğrudan
  ilgili form alanına bağlar.

### 9. Fikir birliği kuralları

| Konu | Karar |
|---|---|
| Tarih alanları | `date` için `YYYY-AA-GG`; zaman damgaları **UTC ISO 8601** (`Z` sonekli) |
| Sayı biçimi | Ondalık ayırıcı nokta; biçimlendirme sunum katmanında |
| Boş değer | `null` gönderilir; boş dize (`""`) anlamlı değer sayılmaz |
| Alan adları | `camelCase` |
| Enum | Metin değer (`"Approved"`), sayı değil — okunabilirlik ve ileri uyumluluk |

### 10. Belgelenmiş uç noktalar

Her uç nokta OpenAPI'de şunları taşır: özet, açıklama, gerekli izin (`HasPermission`),
olası hata kodları ve örnek yanıt. Bu, hem frontend geliştirmeyi hem TEC.5 arayüz
tanımı kanıtını karşılar.

## Gerekçe

- Problem Details standart bir biçimdir; her hatayı tek yerde ele almayı ve frontend'de
  tek bir hata bileşeni yazmayı mümkün kılar.
- `traceId`'nin kullanıcıya gösterilmesi, destek sürecini ölçülebilir biçimde hızlandırır.
- OpenAPI'den tip üretimi, backend–frontend uyumsuzluğunu çalışma zamanından derleme
  zamanına taşır.
- Eşzamanlılık kontrolü, İK sisteminde iki kullanıcının aynı kaydı düzenlemesi
  gerçek bir senaryo olduğu için zorunludur.

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| GraphQL | Ek karmaşıklık; yetkilendirme ve sayfalama kontrolü zorlaşır; ekip aşina değil |
| Özel hata biçimi | Standart varken yeniden icat; kütüphane desteği kaybı |
| Sürümü başlıkta taşımak | URL'de sürüm daha görünür ve hata ayıklaması kolay |
| Frontend tiplerini elle yazmak | Uyumsuzluk çalışma zamanında ortaya çıkar |
| İyimser kilit yerine son yazan kazanır | Sessiz veri kaybı |

## Sonuçlar

**Olumlu:** Tutarlı sözleşme, tek noktadan hata yönetimi, otomatik tip güvenliği,
belgelenmiş arayüz (TEC.5 kanıtı).

**Olumsuz / kabul edilen ödünler:** Sözleşme disiplini her uç noktada ek özen
gerektirir; kod gözden geçirme kontrol listesiyle sağlanacaktır.

**Yükümlülükler:** OpenAPI belgesinin güncelliği CI'da kontrol edilecek; tip üretimi
derleme adımına bağlanacaktır.

## Geri dönüş maliyeti

**Düşük–Orta.** Sözleşme kuralları merkezî ara katmanlarda uygulandığı için değişiklik
tek noktadan yapılabilir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
