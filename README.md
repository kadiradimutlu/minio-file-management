# MinIO File Management

MinIO tabanlı, yeniden kullanılabilir dosya yönetim modülü.

## Amaç

Uygulama aşağıdaki işlemleri destekleyecektir:

- Tekli ve çoklu dosya yükleme
- Dosya listeleme
- Dosya indirme
- Dosya silme
- Uygun dosya türleri için ön izleme
- Süreli erişim bağlantısı oluşturma
- Dosya metadata bilgilerinin PostgreSQL üzerinde saklanması

Dosyaların fiziksel içerikleri MinIO Object Storage üzerinde tutulacaktır.

## Teknolojiler

### Backend

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- MinIO .NET SDK
- OpenAPI / Swagger
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

## Proje Yapısı

```text
src/
├── FileManagement.Api
├── FileManagement.Application
├── FileManagement.Domain
├── FileManagement.Infrastructure
└── FileManagement.Web

tests/
└── FileManagement.UnitTests
Katmanlar
Domain: Temel entity ve domain modelleri
Application: Interface, DTO ve uygulama servisleri
Infrastructure: PostgreSQL, Entity Framework Core ve MinIO implementasyonları
Api: HTTP endpointleri ve API yapılandırması
Web: React ve Ant Design kullanıcı arayüzü
UnitTests: Kritik servislerin birim testleri
Mevcut Durum
Backend solution iskeleti oluşturuldu.
React ve Ant Design frontend iskeleti oluşturuldu.
Backend derlemesi başarılı.
Frontend production derlemesi başarılı.
Başlangıç testleri başarılı.
