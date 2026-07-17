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
```

## Katmanlar

- `Domain`: Temel entity ve domain modelleri
- `Application`: Interface, DTO ve uygulama servisleri
- `Infrastructure`: PostgreSQL, Entity Framework Core ve MinIO implementasyonları
- `Api`: HTTP endpointleri ve API yapılandırması
- `Web`: React ve Ant Design kullanıcı arayüzü
- `UnitTests`: Kritik servislerin birim testleri

## Mevcut Durum

- Backend solution iskeleti oluşturuldu.
- React ve Ant Design frontend iskeleti oluşturuldu.
- Backend derlemesi başarılı.
- Frontend production derlemesi başarılı.
- Başlangıç testleri başarılı.
- PostgreSQL dosya metadata modeli oluşturuldu.
- İlk Entity Framework Core migration'ı eklendi.

## Lokal Altyapının Çalıştırılması

### Gereksinimler

- Docker Desktop
- Docker Compose
- WSL 2

### Ortam Dosyası

Örnek ortam dosyasını kopyalayın:

```powershell
Copy-Item .env.example .env
```

`.env` içerisindeki PostgreSQL ve MinIO parolalarını güçlü lokal
değerlerle değiştirin. `.env` dosyası Git tarafından takip edilmez.

### Servisleri Başlatma

```powershell
docker compose --env-file .env up --detach --build
```

Çalışan servisleri kontrol edin:

```powershell
docker compose --env-file .env ps
```

### Lokal Adresler

- PostgreSQL: `localhost:5432`
- MinIO API: `http://localhost:9000`
- MinIO Console: `http://localhost:9001`

### Servisleri Durdurma

```powershell
docker compose --env-file .env down
```

Named volume verilerini de tamamen silmek için:

```powershell
docker compose --env-file .env down --volumes
```

`--volumes` seçeneği PostgreSQL ve MinIO üzerindeki lokal verileri kalıcı
olarak siler.

### MinIO Sürüm Notu

MinIO Community sürümü, sabitlenmiş kaynak kod tag'inden Docker image
olarak derlenmektedir:

`RELEASE.2025-10-15T17-29-55Z`

MinIO Community kaynak kodu GNU AGPLv3 lisansı altındadır. Üretim veya
ticari kullanım öncesinde lisans ve destek gereksinimleri ayrıca
değerlendirilmelidir.
