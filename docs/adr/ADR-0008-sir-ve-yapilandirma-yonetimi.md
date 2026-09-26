# ADR-0008 — Sır ve Yapılandırma Yönetimi

**Durum:** Kabul Edildi
**Tarih:** 2026-09-06
**Karar defteri karşılığı:** `KR-030`, `KR-031`, `KR-038`, `KR-041`, `KR-044`
**İlgili süreç:** TEC.5 (Tasarım), MAN.5 (Konfigürasyon Yönetimi)

---

## Bağlam

Mevcut sistemin kaynak kodunda **açık metin kimlik bilgileri** bulunmaktadır:

| Konum | İçerik |
|---|---|
| `Api/Sms/NetGsm/NetGsmSmsApi.cs` | NetGSM kullanıcı kodu ve API parolası (sabit olarak koda yazılmış) |
| `Library/MMail.cs` | Kurumsal SMTP hesabının parolası ve sunucu bilgisi |
| `appsettings.json` | Veritabanı bağlantı dizeleri (parolalar dâhil) |

Bu, kaynak koda erişimi olan herkesin kurumsal e-posta hesabından posta gönderebilmesi
ve SMS gönderebilmesi anlamına gelir. Aynı hatanın yeni sistemde tekrarlanmaması bir
tasarım gereksinimidir.

Ayrıca depo GitHub'da tutulacaktır; depoya giren her şey kalıcıdır (sürüm geçmişinden
silinse bile klonlarda kalır).

## Karar

### 1. Sırlar kaynak koda **hiçbir koşulda** yazılmaz

| Ortam | Yöntem |
|---|---|
| **Geliştirme** | .NET User Secrets (`dotnet user-secrets`) — kullanıcı profilinde, depo dışında |
| **UAT / Üretim** | **Ortam değişkeni** (Docker Compose `env_file`, dosya depo dışında ve `600` izinli) |
| Frontend | Sır **hiç bulunmaz.** Yalnızca API taban adresi gibi genel yapılandırma; bu da derleme zamanında değil, çalışma zamanında sunulan bir yapılandırma uç noktasından okunur |

`appsettings.json` yalnızca **sır olmayan** varsayılanları içerir ve depoya girer.
`appsettings.*.Local.json`, `.env` dosyaları `.gitignore` ile korunur.

### 2. Depoya girmeyecekler

`.gitignore` ile korunan ve bu ADR kapsamında **konfigürasyon kararı** olan öğeler:

| Öğe | Gerekçe | Karar |
|---|---|---|
| `src_old/` | Açık metin kimlik bilgileri içeriyor | `KR-031` |
| `claude/` | Yazışma kayıtları paylaşılan parolaları içeriyor | `KR-041` |
| `docs/TSE_ISO_IEC_TS_33061/`, `docs/TS_ISO_IEC_33020/` | TSE telif hakkı; çoğaltma ve dağıtım yasak | `KR-030` |
| `docs/NetGSM/` | Üçüncü taraf doküman | — |
| `.env`, `*.pfx`, `*.key`, `*.pem`, `secrets.json` | Sır | — |

### 3. Sızıntı önleme — otomatik denetim

- **`gitleaks`** (veya eşdeğeri) her Pull Request'te çalışır. Sır tespit edilirse
  yapı **kırılır** ve PR birleştirilemez.
- **GitHub secret scanning** ve **push protection** depoda açık tutulur.
- Bu, "dikkat ederiz" yerine geçen teknik kontroldür.

### 4. Yapılandırma katmanları ve doğrulama

Yapılandırma seçenekleri güçlü tipli sınıflara bağlanır (`IOptions<T>`) ve
**uygulama açılışında doğrulanır** (`ValidateOnStart`). Zorunlu bir ayar eksikse
uygulama **açılmaz** — yarı çalışan bir sistemle üretime çıkılmaz.

Doğrulanacak zorunlu ayarlar (örnek):
- HRMS veritabanı bağlantı dizesi
- LOGO veritabanı bağlantı dizesi (salt okunur kullanıcı)
- JWT imzalama anahtarı (en az 32 bayt)
- SMTP sunucu, kullanıcı, parola
- NetGSM kullanıcı adı, parola, mesaj başlığı
- NAS dosya kök yolu
- Kabul edilen kurumsal e-posta alan adları (`KR-019`)

### 5. Anahtar/parola döndürme

- Sırlar **ortam değişkeninden okunduğu için** değiştirilmesi yeniden dağıtım
  gerektirmez; konteyner yeniden başlatılır.
- JWT imzalama anahtarı değiştirildiğinde mevcut oturumlar geçersizleşir; bu bilinçli
  bir davranıştır ve güvenlik olayında kullanılır.
- Sır döndürme işlemleri `docs/33061/MAN.5-konfigurasyon-yonetimi/kayitlar/` altında
  kayıt altına alınır (parola değeri değil, **işlemin kendisi**).

### 6. Mevcut sistemden devralınan riskler

| Durum | Karar |
|---|---|
| Eski NetGSM API kullanıcısı | **İptal edilmeyecek** — mevcut HRMS ve laboratuvar bünyesindeki diğer uygulamalarda aktif kullanımda (`KR-037`). Parolayı barındıran `src_old` depoya alınmadığı için sızma riski yönetilmektedir. DSG-HRMS için ayrı API kullanıcısı tanımlanmıştır (`KR-036`) |
| Kurumsal SMTP parolası | **Değiştirildi** (2026-09-05) |
| NetGSM API IP kısıtlaması | Uygulanmayacak (`KR-044`). Erişim bilgileri tek koruma katmanıdır; bu ADR'deki kurallar bu nedenle daha kritiktir |

### 7. Parametre ekranından değiştirilebilen sırlar (`KR-081`)

SMTP ve NetGSM parolaları hem sır hem sistem parametresidir (`PRM-ENT-02`,
`PRM-ENT-06`): SYG-KMLK-075/076 bunların parametre ekranından değiştirilebilmesini
ister. Bu iki ihtiyaç şöyle birleştirilir:

| Kural | Uygulama |
|---|---|
| Değer veritabanında **şifreli** saklanır | AES-256-GCM; parametre kimliği ek doğrulanmış veri olarak bağlanır. Değer başka bir parametrenin satırına taşınırsa çözülmez |
| Anahtar veritabanında **değil**, ortam değişkenindedir | `ParameterProtection__Key` (32 bayt, Base64; `openssl rand -base64 32`). Açılışta doğrulanır |
| Ekrandan girilmemişse ortam değişkeni kullanılır | `Parameters__SmtpPassword`, `Parameters__NetGsmPassword`. İlk kurulum ekransız yapılabilir |
| Değer geri okunamaz | Ekranda, API yanıtında ve denetim izinde görünmez; yalnızca "tanımlı" bilgisi döner (`KR-071`) |

**Anahtar kaybı:** Veritabanındaki sır değerleri çözülemez; parametre ekranından
yeniden girilmeleri gerekir. Anahtar, veritabanı yedeğiyle **aynı yerde** saklanmaz.

## Gerekçe

- Sır sızıntısının en yaygın yolu, kaynak kod deposudur. Teknik kontrol (tarama +
  push koruması) kurulmadan yalnızca kurala güvenmek yetersizdir; mevcut sistem bunun
  kanıtıdır.
- Açılışta doğrulama, "üretimde eksik ayar" sınıfı hataları dağıtım anında yakalar.
- Ortam değişkeni yöntemi, harici bir sır yönetim sistemi (Vault vb.) kurmadan yeterli
  koruma sağlar; kurumun ölçeği için orantılıdır.

## Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| HashiCorp Vault / Azure Key Vault | Ek sistem kurulumu, yedekleme ve bakım yükü; kurum ölçeği için orantısız |
| Şifrelenmiş yapılandırma dosyası depoda | Şifre çözme anahtarı da bir yerde durmalı; sorunu öteler |
| Docker secrets | Docker Swarm gerektiriyor; tek sunuculu Compose kurulumunda ek değer yok |
| Yalnızca `.gitignore`'a güvenmek | İnsan hatasına açık; tarama kontrolü şart |

## Sonuçlar

**Olumlu:** Sır sızıntısı hem süreçle hem otomatik denetimle engellenir; ortam değişimi
kod değişikliği gerektirmez.

**Olumsuz / kabul edilen ödünler:**
- Ortam değişkeni dosyası sunucuda düz metindir; dosya izinleri (`600`) ve sunucu
  erişim denetimi ile korunur. Merkezî sır yönetimine göre daha zayıftır ancak
  kurum ölçeği için kabul edilebilir.
- Yeni geliştirici katıldığında User Secrets'ı elle kurmalıdır; kurulum adımı
  `README.md`'de belgelenecektir.

**Yükümlülükler:**
- `gitleaks` iş akışı iskeletle birlikte kurulacaktır.
- GitHub'da secret scanning ve push protection açılacaktır.
- Yapılandırma doğrulaması iskeletin bir parçası olacaktır.

## Geri dönüş maliyeti

**Düşük.** Sır kaynağı soyutlanmış olduğu için ileride merkezî bir sır yöneticisine
geçilmesi yapılandırma sağlayıcısı eklemekten ibarettir.

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-09-06 | 0.1 | İlk oluşturma | Bilgi İşlem |
| 2026-09-26 | 0.2 | §7 parametre ekranından değiştirilebilen sırlar (`KR-081`, #83) | Bilgi İşlem |
