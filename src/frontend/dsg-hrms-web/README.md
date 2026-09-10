# DSG-HRMS Web

Düzen Sağlık Grubu İnsan Kaynakları Yönetim Sistemi — web arayüzü.

Mimari kararlar: [`ADR-0015`](../../../docs/adr/ADR-0015-frontend-mimarisi.md)

## Kurulum

```bash
npm ci
npm run dev
```

Geliştirme sunucusu `http://localhost:5173` adresinde çalışır ve `/api` isteklerini
`http://localhost:5199` adresindeki backend'e yönlendirir.

## Komutlar

| Komut                   | İşlevi                             |
| ----------------------- | ---------------------------------- |
| `npm run dev`           | Geliştirme sunucusu                |
| `npm run build`         | Üretim derlemesi                   |
| `npm run lint`          | ESLint — **uyarıya izin verilmez** |
| `npm run format`        | Prettier ile biçimle               |
| `npm run format:check`  | Biçim denetimi (CI)                |
| `npm run typecheck`     | TypeScript tip denetimi            |
| `npm test`              | Testler                            |
| `npm run test:coverage` | Test + kapsam (eşik %70)           |
| `npm run api:types`     | **OpenAPI'den tip üretimi**        |

## Klasör düzeni

```
src/
├── app/        → uygulama kabuğu, yönlendirme, sağlayıcılar
├── shared/     → api, bileşenler, tema, i18n — modüller arası ortak
├── features/   → iş modülleri (backend modül isimleriyle aynı)
└── locales/    → çeviri dosyaları
```

**Özellikler birbirinin içine doğrudan erişemez** (ADR-0015 §1); ortak ihtiyaç
`shared/` altına taşınır. Kural ESLint ile denetlenir.

## Kurallar

- **Arayüzde sabit metin yazılmaz.** Tüm metinler `locales/tr/common.json` içinde
  (ADR-0015 §9). Başlangıç dili Türkçe; ikinci dil eklendiğinde kod değişmez.
- **Bileşen içinde renk kodu yazılmaz.** Tema belirteçleri kullanılır (ADR-0015 §10).
- **API tipleri elle yazılmaz.** `npm run api:types` ile üretilir; güncel değilse CI
  başarısız olur.
- **Bileşen içinde doğrudan `fetch`/`axios` çağrılmaz.** Her istek `shared/api`
  üzerinden geçer.
