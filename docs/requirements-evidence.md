# Gereksinim–Kanıt Matrisi

Bu belge; dosya yönetimi, Identity, JWT, YARP API Gateway, frontend authentication ve gözlemlenebilirlik gereksinimlerini repository içindeki uygulama ve doğrulama kanıtlarıyla eşleştirir.

## Backend ve Depolama

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Yeniden kullanılabilir storage servisi | `IFileStorageService` ve MinIO implementasyonu | Application birim testleri ve container smoke testi |
| Bucket oluşturma | `EnsureBucketExistsAsync` | File API başlangıcında bucket hazırlama |
| Dosya yükleme | `FileManagementService.UploadAsync` | Unit test ve Gateway üzerinden gerçek multipart upload |
| Dosya indirme | `DownloadAsync` | Gateway JWT download ve SHA-256 eşitlik testi |
| Dosya silme | `DeleteAsync` | Gateway üzerinden `204`, ardından detail `404` |
| Süreli erişim URL'si | `CreatePresignedGetUrlAsync` | JWT ve presigned URL doğrulamaları |
| Metadata PostgreSQL üzerinde saklanmalı | `StoredFile`, `FileManagementDbContext`, repository | Persistence ve API detail testleri |
| Metadata başarısızlığında MinIO geri alma | `FileManagementService.UploadAsync` rollback akışı | `UploadAsync_WhenDatabaseSaveFails_DeletesObject` |
| Private object storage | MinIO bucket ve API kontrollü erişim | JWT endpoint ve hash testleri |

## İlgili Kayıt İlişkilendirmesi

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Dosya başka bir kayıtla ilişkilendirilebilmeli | `RelatedRecordType`, `RelatedRecordId` | Gateway multipart upload |
| Alanlar birlikte verilmelidir | Domain ve request doğrulamaları | Eksik association birim testi |
| İlgili kayda göre filtreleme | Repository ve File API query parametreleri | Gateway filtre sonucu `1` |
| Metadata uzunluk kuralları | Domain sabitleri ve EF configuration | Domain birim testleri |

## Identity ve JWT

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Ayrı Identity servisi | `FileManagement.Identity.Api` | Ayrı executable, image ve health endpoint'i |
| Ayrı Identity persistence katmanı | `FileManagement.Identity.Infrastructure` | Solution ve build doğrulaması |
| Kullanıcı ve rol yönetimi | ASP.NET Core Identity | Admin/User seed ve login cevabı |
| JWT üretimi | `JwtTokenService` | Identity birim testi ve gerçek login |
| JWT doğrulaması | File API JWT Bearer yapılandırması | Geçerli token `200`, bozuk token `401` |
| Role dayalı authorization | Admin endpoint'i | `/api/auth/admin/ping` sonucu `200` |
| Anonim File API erişimi engellenmeli | `[Authorize]` | Gateway üzerinden `401` |
| OpenAPI Bearer desteği | `BearerSecuritySchemeTransformer` | OpenAPI doğrulaması |

## YARP API Gateway

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Ayrı Gateway executable olmalı | `FileManagement.Gateway` | Solution Release build |
| Merkezi API giriş noktası | `MapReverseProxy()` | Nginx ve Vite yalnızca Gateway'e yönleniyor |
| Identity trafiği ayrılmalı | `identityRoute` ve `identityCluster` | Login, me ve admin ping sonuçları `200` |
| File trafiği ayrılmalı | `fileRoute` ve `fileCluster` | Listeleme, upload, download ve delete testleri |
| Bilinmeyen route reddedilmeli | Yalnızca tanımlı YARP route'ları | `/api/unknown` sonucu `404` |
| Request boyutu sınırları | Route bazlı `MaxRequestBodySize` | Identity ve File route configuration kontrolü |
| Docker image olmalı | `docker/gateway/Dockerfile` | Image build ve inspect |
| Compose servisi olmalı | `gateway` servisi | Container `healthy` |
| Nginx downstream servislere doğrudan gitmemeli | `proxy_pass http://gateway:8080` | Eski API hedeflerinin bulunmadığı doğrulandı |
| CI Gateway image'ını build etmeli | `.github/workflows/ci.yml` | Gateway build ve inspect adımları |

## Correlation ID ve Gözlemlenebilirlik

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Gateway correlation ID kabul etmeli | `CorrelationIdMiddleware` | İstemci değeri response içinde aynı döndü |
| Eksik correlation ID üretilmeli | `Guid.NewGuid().ToString("N")` | Middleware kod doğrulaması |
| Correlation ID downstream'e taşınmalı | `context.Request.Headers[HeaderName]` | Gateway → File API zinciri |
| Gateway logları merkezi olmalı | Serilog Console ve Seq sink | Gateway startup ve proxy logları |
| Servis logları ayrıştırılabilmeli | `Application` enrichment | Gateway, Identity ve File API log kimlikleri |
| Health endpoint'i bulunmalı | `/health` | Gateway health `200` |

## Frontend ve Routing

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Login ekranı | `LoginScreen` | Gerçek admin login |
| Oturum saklama | `sessionStorage` | Frontend authentication akışı |
| Bearer interceptor | `httpClient.ts` | Gateway üzerinden korumalı istek |
| Nginx tek API hedefi kullanmalı | `docker/web/nginx.conf` | Bütün `/api/*` trafiği Gateway'e gider |
| Vite geliştirme proxy'si Gateway'i kullanmalı | `vite.config.ts` | Hedef port `5070` |
| JWT download ve preview | Axios Blob akışı | Download SHA-256 testi |

## Altyapı ve Kalite

| Kontrol | Sonuç |
|---|---|
| Solution Release build | Başarılı |
| Toplam birim testi | 15 / 15 başarılı |
| NuGet vulnerability audit | Güvenlik açığı bulunmadı |
| Frontend lint | 0 hata, 0 uyarı |
| Frontend production build | Başarılı |
| Gateway image build | Başarılı |
| Web image build | Başarılı |
| Docker Compose servis sayısı | 8 |
| Web, Gateway, File API, Identity API ve Seq health | `200` |
| Gateway container health | `healthy` |
| Download hash doğrulaması | SHA-256 eşleşti |
| Working tree kontrolü | Uygulama commit'lerinden sonra temiz |

Vite, ana JavaScript chunk'ı için 500 kB sınır uyarısı vermektedir. Build başarılıdır; code splitting daha sonraki performans iyileştirmesi olarak izlenecektir.

## Gateway Commit Kanıtları

| Commit | Açıklama |
|---|---|
| `e43be37` | YARP Gateway proje temeli, route/cluster yapılandırması ve health endpoint'i |
| `0d514ce` | Gateway tarafından üretilen correlation ID'nin downstream request'e aktarılması |
| `ccf7828` | Docker, Compose, Nginx, Vite ve CI Gateway entegrasyonu |
