# MinIO File Management

MinIO Object Storage, PostgreSQL, ASP.NET Core Identity, JWT, YARP API Gateway, Seq ve React kullanılarak geliştirilmiş güvenli ve yeniden kullanılabilir dosya yönetim sistemi.

Dosyaların fiziksel içerikleri private MinIO bucket üzerinde, dosya metadata bilgileri PostgreSQL içindeki `file_management` veritabanında, kullanıcı ve rol bilgileri ise ayrı `identity_management` veritabanında saklanır.

## Özellikler

### Dosya yönetimi

- Tekli dosya yükleme API'si
- React arayüzünden çoklu dosya seçimi ve yükleme
- Sürükle-bırak dosya yükleme
- Yükleme ilerleme göstergesi
- Dosya listeleme ve metadata detayını görüntüleme
- JWT doğrulamalı dosya indirme
- PDF ve görseller için JWT doğrulamalı tarayıcı önizlemesi
- Dosya silme
- Süreli MinIO erişim bağlantısı oluşturma
- Dosya boyutu, uzantı ve content type doğrulaması
- Metadata kaydı başarısız olursa MinIO nesnesini geri alma
- İlgili kayıt türü ve kimliğiyle dosya ilişkilendirme
- İlgili kayda göre dosya filtreleme

### Kimlik ve erişim yönetimi

- Ayrı Identity API ve Identity persistence katmanı
- ASP.NET Core Identity kullanıcı ve rol yönetimi
- PostgreSQL üzerinde ayrı Identity veritabanı
- Parola hashleme ve başarısız giriş kilitleme desteği
- JWT access token üretimi
- Issuer, audience, imza ve süre doğrulaması
- `User` ve `Admin` rolleri
- Role dayalı admin endpoint'i
- File API endpoint'lerinin Bearer authentication ile korunması
- OpenAPI ve Swagger üzerinde Bearer authorization desteği
- React login ve logout akışı
- Sekme bazlı `sessionStorage` oturumu
- Axios Bearer interceptor'ı
- Token süresi dolduğunda veya `401` alındığında otomatik logout

### Gözlemlenebilirlik

- Serilog ile yapılandırılmış loglama
- Console ve merkezi Seq log hedefleri
- Uygulama ve ortam bilgileriyle zenginleştirilmiş loglar
- `X-Correlation-ID` request/response desteği
- HTTP request süreleri ve durum kodları
- File API loglarında kullanıcı kimliği ve kullanıcı adı
- Health endpoint'leri

### Altyapı ve kalite

- OpenAPI 3.1 dokümanları
- İnteraktif Swagger UI
- Docker Compose ile lokal çalışma ortamı
- Nginx reverse proxy
- GitHub Actions CI
- xUnit birim testleri
- NuGet ve npm güvenlik denetimleri

## Mimari

~~~text
Browser
   |
   v
Web / Nginx :8080
   |
   | /api/*
   v
YARP Gateway :8080
   |
   |-- /api/auth/*  --> Identity API :8080
   |                     |
   |                     `--> identity_management
   |
   `-- /api/files/* --> File API :8080
                         |
                         |--> file_management
                         `--> MinIO private bucket

Gateway ------\
Identity API --+---- Serilog ----> Seq
File API -----/
~~~

Nginx yalnızca statik React dosyalarını sunar ve bütün `/api/*` isteklerini YARP Gateway'e iletir. Servis seçimi Gateway içindeki route ve cluster yapılandırmasıyla yapılır.

Gateway aşağıdaki route'ları yönetir:

| Gateway route | Cluster | Hedef |
|---|---|---|
| `/api/auth/{**catch-all}` | `identityCluster` | Identity API |
| `/api/files/{**catch-all}` | `fileCluster` | File API |

Gateway authentication işlemini kendisi yapmaz. Bearer token ve diğer request header'larını ilgili downstream servise taşır. Identity API JWT üretir; File API ise JWT'yi yerel olarak doğrular.

`X-Correlation-ID` değeri istemciden geldiyse korunur. Gönderilmediyse Gateway yeni bir correlation ID üretir, downstream request'e ekler ve response header'ında döndürür.

File API ve Identity API host portları lokal tanılama ve Swagger erişimi için açık tutulur. Web uygulamasının normal API trafiği Gateway üzerinden geçer.

Identity API, File API ve Gateway ayrı executable, Docker image, health endpoint'i ve log kimliğine sahiptir.

Her iki veri servisi aynı PostgreSQL container'ını kullanır; ancak ayrı mantıksal veritabanlarına sahiptir:

~~~text
file_management
identity_management
~~~

File API, her dosya isteğinde Identity API'ye çağrı yapmaz. Identity API tarafından imzalanan JWT'yi issuer, audience, süre ve imza bilgileriyle yerel olarak doğrular.

### Repository yapısı

~~~text
src/
├── FileManagement.Api
├── FileManagement.Application
├── FileManagement.Domain
├── FileManagement.Infrastructure
├── FileManagement.Gateway
├── FileManagement.Identity.Api
├── FileManagement.Identity.Infrastructure
└── FileManagement.Web

tests/
├── FileManagement.UnitTests
└── FileManagement.Identity.UnitTests

docs/
├── requirements-evidence.md
└── verification-report.md
~~~

Katmanların sorumlulukları:

- `FileManagement.Domain`: Dosya entity'leri ve domain kuralları
- `FileManagement.Application`: Dosya servisleri, DTO'lar ve soyutlamalar
- `FileManagement.Infrastructure`: PostgreSQL, Entity Framework Core ve MinIO implementasyonları
- `FileManagement.Gateway`: YARP route/cluster yönetimi, API trafiği ve correlation ID başlangıç noktası
- `FileManagement.Api`: Korumalı dosya endpoint'leri, JWT doğrulaması, OpenAPI ve Swagger
- `FileManagement.Identity.Infrastructure`: Identity persistence, kullanıcı/rol yönetimi ve JWT üretimi
- `FileManagement.Identity.Api`: Register, login, current user ve admin endpoint'leri
- `FileManagement.Web`: React kullanıcı arayüzü, oturum yönetimi ve API istemcileri
- `FileManagement.UnitTests`: Domain ve application servis testleri
- `FileManagement.Identity.UnitTests`: JWT üretim ve doğrulama testleri

## Authentication Akışı

1. React uygulaması `/api/auth/login` isteğini Nginx üzerinden Gateway'e gönderir.
2. Gateway isteği `identityCluster` üzerinden Identity API'ye yönlendirir.
3. Identity API kullanıcı bilgilerini ASP.NET Core Identity üzerinden doğrular.
4. Başarılı girişte kullanıcı kimliği, e-posta ve rollerini taşıyan JWT üretilir.
5. React uygulaması token ve kullanıcı bilgilerini `sessionStorage` içinde saklar.
6. Axios interceptor korumalı isteklere `Authorization: Bearer <token>` header'ını ekler.
7. Nginx bütün `/api/*` trafiğini Gateway'e iletir.
8. Gateway `/api/files/*` isteklerini File API'ye yönlendirirken Bearer token'ı korur.
9. File API token'ın issuer, audience, imza ve süresini doğrular.
10. Token süresi dolduğunda veya API `401` döndürdüğünde frontend oturumu temizler.

Varsayılan access token süresi ortam değişkeni üzerinden yapılandırılır ve örnek ortamda 60 dakikadır.

## İlgili Kayıt İlişkilendirmesi

Bir dosya isteğe bağlı olarak başka bir sistem kaydıyla ilişkilendirilebilir.

| Alan | Maksimum uzunluk | Açıklama |
|---|---:|---|
| `relatedRecordType` | 100 | İlgili kaydın türü. Örnek: `Student` |
| `relatedRecordId` | 255 | İlgili kaydın kimliği. Sayısal değer veya UUID olabilir. |

Kurallar:

- İki alan birlikte verilmelidir.
- İki alan da boş bırakılırsa dosya ilişkisiz yüklenir.
- Yalnızca bir alan verilirse API `400 Bad Request` döndürür.
- Listeleme endpoint'i aynı alanlarla filtrelenebilir.
- PostgreSQL üzerinde iki alanı kapsayan birleşik index bulunur.

## Teknolojiler

### Backend

- .NET 10
- ASP.NET Core Web API
- ASP.NET Core Identity
- JWT Bearer Authentication
- Entity Framework Core
- PostgreSQL
- MinIO .NET SDK
- Serilog
- Seq
- OpenAPI
- Swagger UI
- xUnit

### Frontend

- React
- TypeScript
- Vite
- Ant Design
- Axios

### Altyapı

- YARP Reverse Proxy
- Docker
- Docker Compose
- Nginx
- GitHub Actions

## Hızlı Başlangıç

### Gereksinimler

- Docker Desktop
- Docker Compose
- WSL 2

### Ortam dosyası

Örnek ortam dosyasını kopyalayın:

~~~powershell
Copy-Item ".env.example" ".env"
~~~

`.env` içindeki aşağıdaki değerleri güvenli lokal değerlerle değiştirin:

- `POSTGRES_PASSWORD`
- `MINIO_ROOT_PASSWORD`
- `SEQ_ADMIN_PASSWORD`
- `JWT_SIGNING_KEY`
- `IDENTITY_ADMIN_PASSWORD`

`JWT_SIGNING_KEY` en az 32 karakterden oluşan rastgele bir değer olmalıdır.

`.env` dosyası Git tarafından takip edilmez.

### Servisleri başlatma

~~~powershell
docker compose `
    --env-file ".env" `
    up `
    --detach `
    --build `
    --wait
~~~

Servisleri görüntüleme:

~~~powershell
docker compose `
    --env-file ".env" `
    ps `
    --all
~~~

Başlangıç sırasında:

- File API migration'ları uygulanır.
- Identity migration'ları uygulanır.
- `User` ve `Admin` rolleri hazırlanır.
- İlk admin hesabı gerektiğinde oluşturulur.
- MinIO bucket'ı hazırlanır.
- Identity veritabanı yoksa `identity-db-init` işi tarafından oluşturulur.

## Docker Compose Servisleri

| Servis | Sorumluluk |
|---|---|
| `postgres` | File ve Identity mantıksal veritabanları |
| `identity-db-init` | Identity veritabanını hazırlayan tek seferlik init işi |
| `minio` | Dosya içeriği depolama |
| `seq` | Merkezi yapılandırılmış loglar |
| `identity-api` | Kullanıcı, rol ve JWT işlemleri |
| `api` | Dosya yönetimi ve JWT doğrulaması |
| `gateway` | YARP route/cluster yönetimi ve merkezi API giriş noktası |
| `web` | React uygulaması ve Nginx statik dosya/reverse proxy katmanı |

`identity-db-init` başarılı çalıştıktan sonra `Exited (0)` durumunda kalması beklenen davranıştır.

## Lokal Adresler

| Servis | Adres |
|---|---|
| Web uygulaması | `http://127.0.0.1:8080` |
| Web health | `http://127.0.0.1:8080/health` |
| Gateway health | `http://127.0.0.1:5070/health` |
| Gateway API giriş noktası | `http://127.0.0.1:5070/api` |
| File API health | `http://127.0.0.1:5080/health` |
| File API Swagger | `http://127.0.0.1:5080/swagger` |
| File API OpenAPI | `http://127.0.0.1:5080/openapi/v1.json` |
| Identity API health | `http://127.0.0.1:5090/health` |
| Identity API Swagger | `http://127.0.0.1:5090/swagger` |
| Identity API OpenAPI | `http://127.0.0.1:5090/openapi/v1.json` |
| Seq | `http://127.0.0.1:5341` |
| MinIO API | `http://127.0.0.1:9000` |
| MinIO Console | `http://127.0.0.1:9001` |
| PostgreSQL | `127.0.0.1:5432` |

Uygulama yönlendirme zinciri:

~~~text
Web / Nginx
   |
   `-- /api/* --> Gateway
                    |
                    |-- /api/auth/*  --> Identity API
                    `-- /api/files/* --> File API
~~~

Vite geliştirme sunucusu da `/api/*` isteklerini `http://127.0.0.1:5070` adresindeki Gateway'e gönderir.

## Identity API Endpoint'leri

| Metot | Endpoint | Erişim | Açıklama |
|---|---|---|---|
| `POST` | `/api/auth/register` | Anonim | Yeni `User` hesabı oluşturur |
| `POST` | `/api/auth/login` | Anonim | JWT access token üretir |
| `GET` | `/api/auth/me` | Bearer | Aktif kullanıcı ve roller |
| `GET` | `/api/auth/admin/ping` | `Admin` | Role dayalı erişim testi |
| `GET` | `/health` | Anonim | Identity sağlık kontrolü |

## File API Endpoint'leri

Bütün `/api/files` endpoint'leri geçerli Bearer token gerektirir.

| Metot | Endpoint | Açıklama |
|---|---|---|
| `POST` | `/api/files` | Multipart dosya yükleme |
| `GET` | `/api/files` | Dosyaları listeleme |
| `GET` | `/api/files/{id}` | Dosya metadata detayını alma |
| `GET` | `/api/files/{id}/download` | Dosyayı indirme |
| `GET` | `/api/files/{id}/preview` | Desteklenen dosyayı önizleme |
| `GET` | `/api/files/{id}/presigned-url` | Süreli MinIO URL'si oluşturma |
| `DELETE` | `/api/files/{id}` | Dosyayı ve metadata kaydını silme |
| `GET` | `/health` | Anonim File API sağlık kontrolü |

## API Kullanım Örnekleri

### Admin hesabıyla giriş

~~~powershell
$loginResponse = Invoke-RestMethod `
    -Method Post `
    -Uri "http://127.0.0.1:8080/api/auth/login" `
    -ContentType "application/json" `
    -Body (
        @{
            email = "admin@filemanagement.local"
            password = "replace-with-your-local-admin-password"
        } |
        ConvertTo-Json
    )

$accessToken = $loginResponse.accessToken

$headers = @{
    Authorization = "Bearer $accessToken"
}
~~~

### Aktif kullanıcıyı görüntüleme

~~~powershell
Invoke-RestMethod `
    -Method Get `
    -Uri "http://127.0.0.1:8080/api/auth/me" `
    -Headers $headers
~~~

### İlişkisiz dosya yükleme

~~~powershell
curl.exe `
    --request POST `
    "http://127.0.0.1:8080/api/files" `
    --header "Authorization: Bearer $accessToken" `
    --form "file=@C:\Temp\report.pdf;type=application/pdf"
~~~

### İlgili kayıtla dosya yükleme

~~~powershell
curl.exe `
    --request POST `
    "http://127.0.0.1:8080/api/files" `
    --header "Authorization: Bearer $accessToken" `
    --form "file=@C:\Temp\report.pdf;type=application/pdf" `
    --form "relatedRecordType=Student" `
    --form "relatedRecordId=42"
~~~

### İlgili kayda göre listeleme

~~~powershell
Invoke-RestMethod `
    -Method Get `
    -Uri "http://127.0.0.1:8080/api/files?relatedRecordType=Student&relatedRecordId=42" `
    -Headers $headers
~~~

## Varsayılan Dosya Doğrulamaları

Frontend aşağıdaki uzantıları kabul eder:

~~~text
.pdf
.png
.jpg
.jpeg
.txt
.docx
.xlsx
~~~

Varsayılan maksimum dosya boyutu `20 MB`'dır.

Backend ayrıca izin verilen uzantıları ve content type değerlerini yapılandırma üzerinden doğrular.

## Geliştirme Komutları

### Backend

~~~powershell
dotnet restore `
    "MinioFileManagement.sln" `
    --force-evaluate `
    -p:NuGetAudit=true `
    -p:NuGetAuditMode=all `
    -warnaserror

dotnet build `
    "MinioFileManagement.sln" `
    --configuration Release `
    --no-restore

dotnet test `
    "MinioFileManagement.sln" `
    --configuration Release `
    --no-build
~~~

### Frontend

~~~powershell
Push-Location "src\FileManagement.Web"

npm ci
npm run lint
npm run build
npm audit --audit-level=high

Pop-Location
~~~

### File database EF Core model kontrolü

~~~powershell
dotnet ef migrations has-pending-model-changes `
    --project `
    "src\FileManagement.Infrastructure\FileManagement.Infrastructure.csproj" `
    --startup-project `
    "src\FileManagement.Api\FileManagement.Api.csproj"
~~~

### Identity database EF Core model kontrolü

~~~powershell
dotnet ef migrations has-pending-model-changes `
    --project `
    "src\FileManagement.Identity.Infrastructure\FileManagement.Identity.Infrastructure.csproj" `
    --startup-project `
    "src\FileManagement.Identity.Api\FileManagement.Identity.Api.csproj" `
    --context IdentityDbContext
~~~

## CI

GitHub Actions aşağıdaki işleri çalıştırır.

### Backend

- NuGet restore ve güvenlik denetimi
- Bütün solution için Release build
- File ve Identity birim testleri
- Zafiyetli NuGet paket raporu

### Frontend

- `npm ci`
- Lint
- Production build
- Yüksek önem seviyeli npm güvenlik denetimi

### Containers

- Docker Compose yapılandırma kontrolü
- Servis listesinin doğrulanması
- File API, Identity API, Gateway ve Web image build işlemleri
- Oluşturulan image'ların doğrulanması

## Servisleri Durdurma

Volume verilerini koruyarak:

~~~powershell
docker compose `
    --env-file ".env" `
    down
~~~

PostgreSQL, MinIO ve Seq volume verilerini de silerek:

~~~powershell
docker compose `
    --env-file ".env" `
    down `
    --volumes
~~~

`--volumes` seçeneği lokal PostgreSQL, MinIO ve Seq verilerini kalıcı olarak siler.

## Güvenlik ve Üretim Notları

Mevcut uygulamada JWT authentication, temel role dayalı authorization, parola hashleme, lockout, private MinIO bucket ve merkezi loglama uygulanmıştır.

Üretim ortamından önce ayrıca değerlendirilmesi gereken konular:

- HTTPS zorunluluğu
- JWT signing key'in secret manager veya key vault içinde tutulması
- Signing key rotasyonu veya asimetrik imzalama
- Refresh token ya da BFF/HttpOnly cookie yaklaşımı
- Public registration politikasının sınırlandırılması
- E-posta doğrulama ve parola sıfırlama akışları
- Kullanıcı veya tenant bazlı dosya izolasyonu
- Zararlı dosya taraması
- Rate limiting
- CSRF ve XSS risk değerlendirmesi
- Yedekleme ve geri yükleme
- Seq alarm ve retention politikaları
- MinIO lisans ve destek koşulları

MinIO Community binary'si sabitlenmiş kaynak kod tag'inden oluşturulur:

~~~text
RELEASE.2025-10-15T17-29-55Z
~~~

## Kanıt ve Doğrulama

- [Gereksinim–kanıt matrisi](docs/requirements-evidence.md)
- [Doğrulama raporu](docs/verification-report.md)
