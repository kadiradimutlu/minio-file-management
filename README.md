# MinIO File Management

MinIO Object Storage, PostgreSQL, ASP.NET Core ve React kullanılarak geliştirilmiş yeniden kullanılabilir dosya yönetim modülü.

Dosyaların fiziksel içerikleri MinIO üzerinde, dosya metadata bilgileri ise PostgreSQL üzerinde saklanır.

## Özellikler

- Tekli dosya yükleme API'si
- React arayüzünden çoklu dosya seçimi ve yükleme
- Sürükle-bırak dosya yükleme
- Yükleme ilerleme göstergesi
- Dosya listeleme ve detay görüntüleme
- Dosya indirme
- PDF ve görseller için tarayıcı içi önizleme
- Dosya silme
- Süreli MinIO erişim bağlantısı oluşturma
- Dosya boyutu, uzantı ve content type doğrulaması
- Metadata bilgilerinin PostgreSQL üzerinde saklanması
- Dosya içeriklerinin private MinIO bucket üzerinde saklanması
- Metadata kaydı başarısız olursa MinIO nesnesinin geri alınması
- İlgili kayıt türü ve kimliğiyle dosya ilişkilendirme
- İlgili kayda göre dosya filtreleme
- OpenAPI dokümanı ve interaktif Swagger UI
- Docker Compose ile dört servisli lokal çalışma ortamı
- GitHub Actions CI doğrulamaları
- xUnit birim testleri

## İlgili Kayıt İlişkilendirmesi

Bir dosya isteğe bağlı olarak başka bir sistem kaydıyla ilişkilendirilebilir.

Kullanılan alanlar:

| Alan | Maksimum uzunluk | Açıklama |
|---|---:|---|
| `relatedRecordType` | 100 | İlgili kaydın türü. Örnek: `Student` |
| `relatedRecordId` | 255 | İlgili kaydın kimliği. Sayısal değer veya UUID olabilir. |

Kurallar:

- İki alan birlikte verilmelidir.
- İki alan da boş bırakılırsa dosya ilişkisiz yüklenir.
- Yalnızca bir alan verilirse API `400 Bad Request` döndürür.
- Listeleme endpoint'i aynı alanlarla filtrelenebilir.
- PostgreSQL üzerinde iki alanı kapsayan birleşik bir index bulunur.

## Teknolojiler

### Backend

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- MinIO .NET SDK
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

- Docker
- Docker Compose
- Nginx
- GitHub Actions

## Mimari

~~~text
src/
├── FileManagement.Api
├── FileManagement.Application
├── FileManagement.Domain
├── FileManagement.Infrastructure
└── FileManagement.Web

tests/
└── FileManagement.UnitTests

docs/
├── requirements-evidence.md
└── verification-report.md
~~~

Katmanların sorumlulukları:

- `Domain`: Entity'ler ve domain kuralları
- `Application`: Servisler, DTO'lar ve soyutlamalar
- `Infrastructure`: PostgreSQL, Entity Framework Core ve MinIO implementasyonları
- `Api`: HTTP endpoint'leri, doğrulamalar, OpenAPI ve Swagger
- `Web`: React ve Ant Design kullanıcı arayüzü
- `UnitTests`: Domain ve application servis testleri

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

`.env` içindeki PostgreSQL ve MinIO parolalarını lokal kullanım için güvenli değerlerle değiştirin.

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
    ps
~~~

API başlangıcında:

- Entity Framework Core migration'ları uygulanır.
- MinIO bucket'ı hazırlanır.

## Lokal Adresler

| Servis | Adres |
|---|---|
| Web uygulaması | `http://127.0.0.1:8080` |
| API health | `http://127.0.0.1:5080/health` |
| Swagger UI | `http://127.0.0.1:5080/swagger` |
| OpenAPI JSON | `http://127.0.0.1:5080/openapi/v1.json` |
| MinIO API | `http://127.0.0.1:9000` |
| MinIO Console | `http://127.0.0.1:9001` |
| PostgreSQL | `127.0.0.1:5432` |

Web container'ındaki Nginx, `/api` isteklerini API container'ına yönlendirir.

## API Endpoint'leri

| Metot | Endpoint | Açıklama |
|---|---|---|
| `POST` | `/api/files` | Multipart dosya yükleme |
| `GET` | `/api/files` | Dosyaları listeleme |
| `GET` | `/api/files/{id}` | Dosya metadata detayını alma |
| `GET` | `/api/files/{id}/download` | Dosyayı indirme |
| `GET` | `/api/files/{id}/preview` | Desteklenen dosyayı önizleme |
| `GET` | `/api/files/{id}/presigned-url` | Süreli MinIO URL'si oluşturma |
| `DELETE` | `/api/files/{id}` | Dosyayı ve metadata kaydını silme |
| `GET` | `/health` | API sağlık kontrolü |

## API Kullanım Örnekleri

### İlişkisiz dosya yükleme

~~~powershell
curl.exe `
    --request POST `
    "http://127.0.0.1:5080/api/files" `
    --form "file=@C:\Temp\report.pdf;type=application/pdf"
~~~

### İlgili kayıtla dosya yükleme

~~~powershell
curl.exe `
    --request POST `
    "http://127.0.0.1:5080/api/files" `
    --form "file=@C:\Temp\report.pdf;type=application/pdf" `
    --form "relatedRecordType=Student" `
    --form "relatedRecordId=42"
~~~

### İlgili kayda göre listeleme

~~~powershell
Invoke-RestMethod `
    -Method Get `
    -Uri "http://127.0.0.1:5080/api/files?relatedRecordType=Student&relatedRecordId=42"
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
dotnet restore "MinioFileManagement.sln"

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

Pop-Location
~~~

### EF Core model kontrolü

~~~powershell
dotnet ef migrations has-pending-model-changes `
    --project `
    "src\FileManagement.Infrastructure\FileManagement.Infrastructure.csproj" `
    --startup-project `
    "src\FileManagement.Api\FileManagement.Api.csproj"
~~~

## CI

GitHub Actions aşağıdaki işleri çalıştırır.

### Backend

- NuGet restore ve güvenlik denetimi
- Release build
- xUnit testleri
- Zafiyetli NuGet paket raporu

### Frontend

- `npm ci`
- Lint
- Production build
- Yüksek önem seviyeli npm güvenlik denetimi

### Containers

- Docker Compose yapılandırma kontrolü
- Servis listesinin doğrulanması
- API ve Web image build işlemleri
- Oluşturulan image'ların doğrulanması

## Servisleri Durdurma

Volume verilerini koruyarak:

~~~powershell
docker compose `
    --env-file ".env" `
    down
~~~

PostgreSQL ve MinIO volume verilerini de silerek:

~~~powershell
docker compose `
    --env-file ".env" `
    down `
    --volumes
~~~

`--volumes` seçeneği lokal PostgreSQL ve MinIO verilerini kalıcı olarak siler.

## Güvenlik ve Üretim Notları

Bu repository bir dosya yönetim modülü ve lokal çalışma örneğidir.

Üretim ortamından önce ayrıca değerlendirilmesi gereken konular:

- Authentication ve authorization
- Kullanıcı veya tenant izolasyonu
- Zararlı dosya taraması
- Rate limiting
- HTTPS ve reverse proxy güvenliği
- Secret yönetimi
- Yedekleme ve geri yükleme
- Loglama, gözlemlenebilirlik ve alarm mekanizmaları
- MinIO lisans ve destek koşulları

MinIO Community image'ı sabitlenmiş kaynak kod tag'inden oluşturulur:

~~~text
RELEASE.2025-10-15T17-29-55Z
~~~

## Kanıt ve Doğrulama

- [Gereksinim–kanıt matrisi](docs/requirements-evidence.md)
- [Doğrulama raporu](docs/verification-report.md)
