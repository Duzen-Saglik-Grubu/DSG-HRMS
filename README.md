# DSG-HRMS

**Düzen Sağlık Grubu — İnsan Kaynakları Yönetim Sistemi**

Kurum içi geliştirilen, web tabanlı insan kaynakları yönetim sistemi.
Proje **TS ISO/IEC TS 33061 Seviye 2** çerçevesinde yürütülmektedir.

---

## Durum

| | |
|---|---|
| **Aşama** | A1 tamamlandı → A2 — Temel Modüller |
| **Sürüm** | `v0.2.0-rc.2` — T3 Kimlik Yönetimi kabul adayı ([CHANGELOG](CHANGELOG.md)) |
| **Kapsam** | 35 modül (5 Temel · 10 Yatay · 20 İş) |

---

## Belgeler

Başlangıç noktası: **[docs/00-DOKUMAN-HARITASI.md](docs/00-DOKUMAN-HARITASI.md)** —
hangi bilginin nerede olduğunu gösteren harita.

| Belge | İçerik |
|---|---|
| [Vizyon ve Kapsam](docs/mimari/vizyon-ve-kapsam.md) | Neden, ne, kim, kapsam içi/dışı |
| [Modül Listesi ve Bağımlılıklar](docs/mimari/modul-listesi-ve-bagimliliklar.md) | 35 modül, bağımlılık haritası, seçim rehberi |
| [Mimari Karar Kayıtları (ADR)](docs/adr/README.md) | 15 teknik karar, gerekçeleriyle |
| [Karar Kayıt Defteri](docs/karar-kayit-defteri.md) | Tüm proje kararlarının resmî kaydı |
| [Proje Planı](docs/33061/MAN.1-proje-planlama/proje-plani.md) | Hedefler, aşamalar, kaynaklar, izleme |
| [İş Kırılım Yapısı](docs/33061/MAN.1-proje-planlama/is-kirilim-yapisi.md) | WBS ve kritik yol |
| [Risk Kayıt Defteri](docs/33061/MAN.4-risk-yonetimi/risk-kayit-defteri.md) | Açık riskler ve önlemler |
| [A1 Kapanış Değerlendirmesi](docs/33061/MAN.1-proje-planlama/A1-asama-kapanis-degerlendirmesi.md) | Teknik iskelet aşamasının sonuçları ve kanıtları |
| [Olgunluk Kriterleri](docs/33061/00-OLGUNLUK-SEVIYESI-KRITERLERI.md) | Seviye 2 kriterleri ve öz değerlendirme |

---

## Teknoloji

| Katman | Seçim |
|---|---|
| Backend | .NET 10 (LTS) · ASP.NET Core Web API · EF Core |
| Frontend | React 19 · TypeScript · Vite · Material UI |
| Veritabanı | PostgreSQL |
| Kaynak sistem | LOGO Bordro (MSSQL) — **yalnızca okuma** |
| Paketleme | Docker · Docker Compose |
| CI/CD | GitHub Actions |

Ayrıntı ve gerekçeler: [ADR-0001](docs/adr/ADR-0001-teknoloji-yigini.md)

---

## Geliştirme ortamı kurulumu

### Gereksinimler

| Araç | Sürüm |
|---|---|
| .NET SDK | 10.0.400+ (`global.json` ile sabit) |
| Node.js | 22+ |
| Docker Desktop | Güncel |
| Git | 2.40+ |

### İlk kurulum

```bash
git clone https://github.com/Duzen-Saglik-Grubu/DSG-HRMS.git
cd DSG-HRMS

# Git kancalarını etkinleştir (ZORUNLU)
git config core.hooksPath .githooks
```

### Seçenek 1 — Tümü konteynerde (en hızlı)

Üretime en yakın çalışma biçimi; makineye PostgreSQL kurmayı gerektirmez.

```bash
cd docker
cp .env.example .env          # bir kez — .env depoya GİRMEZ, parolayı değiştirin
docker compose up -d --build

# Veritabanı şemasını oluştur
cd ../src/backend
Database__Hrms="Host=localhost;Port=5433;Database=dsg_hrms;Username=postgres;Password=<parolanız>" \
  dotnet ef database update --project Dsg.Hrms.Infrastructure --startup-project Dsg.Hrms.Api
```

| Adres | Servis |
|---|---|
| http://localhost:8080 | Web arayüzü |
| http://localhost:5199 | API |
| `localhost:5433` | PostgreSQL — veritabanı istemcinizle bağlanabilirsiniz |

Durdurmak için: `docker compose down` · veriyi de silmek için: `docker compose down -v`

### Seçenek 2 — Uygulamalar yerelde, veritabanı konteynerde

Kod değişikliğini anında görmek (hot reload) için tercih edilir.

```bash
cd docker && docker compose up -d postgres

cd ../src/backend
dotnet user-secrets set "Database:Hrms" \
  "Host=localhost;Port=5433;Database=dsg_hrms;Username=postgres;Password=<parolanız>" \
  --project Dsg.Hrms.Api
dotnet run --project Dsg.Hrms.Api            # http://localhost:5199

cd ../frontend/dsg-hrms-web
npm ci && npm run dev                         # http://localhost:5173
```

### UAT ortamı

Kabul testi ortamı **ayrı sunucuda** çalışır (`KR-024`); geliştirme verisiyle karışmaz.

| | |
|---|---|
| Adres | http://insankaynaklaritest.duzen.com.tr |
| Sunucu | `192.168.3.202` — Ubuntu 26.04 LTS |

Dağıtım tek komutla yapılır:

```bash
./docker/deploy-uat.sh
```

Betik kaynağı aktarır, imajları sunucuda derler, şemayı **idempotent** SQL betiğiyle
uygular, yığını başlatır ve **doğrular** — web yanıt vermiyorsa veya API dışarıya
açık kalmışsa hata verip durur.

Ayrıntı, ilk kurulum ve işletim komutları:
[UAT Kurulum Runbook](docs/33061/TEC.10-gecis/uat-ortami-kurulum-runbook.md)

> **Git kancaları zorunludur.** `main` dalına doğrudan gönderimi engeller ve commit
> mesajı biçimini denetler. GitHub Free planında özel depolarda sunucu tarafı dal
> koruma kullanılamadığı için bu kancalar telafi edici kontroldür.
> Ayrıntı: [dal-koruma-telafi-kontrolleri.md](docs/33061/MAN.5-konfigurasyon-yonetimi/dal-koruma-telafi-kontrolleri.md)

### Yapılandırma ve sırlar

Bağlantı dizeleri, API parolaları ve SMTP bilgileri **kaynak koda yazılmaz**
([ADR-0008](docs/adr/ADR-0008-sir-ve-yapilandirma-yonetimi.md)).

- **Geliştirme:** .NET User Secrets
- **UAT / Üretim:** ortam değişkenleri

Gerekli ayarların listesi ADR-0008 §4'tedir. Zorunlu bir ayar eksikse uygulama açılmaz.

---

## Katkı akışı

Ayrıntı: [CONTRIBUTING.md](CONTRIBUTING.md)

```
issue → dal → geliştirme → PR → CI kapıları → inceleme → main
```

`main` dalına doğrudan yazılmaz. Her değişiklik — **dokümanlar dâhil** — Pull Request
ile yapılır.

---

## Depoya girmeyen içerik

| İçerik | Neden |
|---|---|
| `src_old/` | Açık metin kimlik bilgileri içeriyor ([KR-031](docs/karar-kayit-defteri.md)) |
| `claude/` | Yazışma kayıtları paylaşılan parolalar içeriyor (KR-041) |
| `docs/TSE_ISO_IEC_TS_33061/`, `docs/TS_ISO_IEC_33020/` | TSE telif hakkı (KR-030) |
| `docs/NetGSM/` | Üçüncü taraf doküman |
| `.env`, `*.pfx`, `*.key`, `secrets.json` | Sır |

---

## Lisans

Bu depo **Düzen Sağlık Grubu'na** aittir ve kurum içi kullanım içindir.
Kurum dışına dağıtılamaz.
