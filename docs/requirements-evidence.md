# Gereksinim–Kanıt Matrisi

Bu belge, dosya yönetimi, Identity, JWT, frontend authentication ve gözlemlenebilirlik gereksinimlerini repository içindeki uygulama ve doğrulama kanıtlarıyla eşleştirir.

## Backend ve Depolama

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Yeniden kullanılabilir storage servisi | `IFileStorageService` ve MinIO implementasyonu | Application birim testleri ve Docker smoke testi |
| Bucket oluşturma | `EnsureBucketExistsAsync` | File API başlangıcında bucket hazırlama |
| Dosya yükleme | `FileManagementService.UploadAsync` | Unit test ve gerçek MinIO upload testi |
| Dosya indirme | `DownloadAsync` | JWT download ve SHA-256 eşitlik testi |
| Dosya silme | `DeleteAsync` | API `204`, metadata `404` ve filtre sonucu boş |
| Nesne varlık kontrolü | `ExistsAsync` | Storage soyutlaması |
| Süreli erişim URL'si | `CreatePresignedGetUrlAsync` | Presigned URL üzerinden SHA-256 testi |
| Metadata PostgreSQL üzerinde saklanmalı | `StoredFile`, `FileManagementDbContext`, repository | PostgreSQL persistence ve API detail testi |
| Metadata başarısızlığında MinIO geri alma | `FileManagementService.UploadAsync` rollback akışı | `UploadAsync_WhenDatabaseSaveFails_DeletesObject` |
| File database migration'ları | `FileManagement.Infrastructure/Persistence/Migrations` | Startup migration ve model kontrolü |
| Private object storage | MinIO bucket ve yalnızca API üzerinden erişim | JWT endpoint ve presigned URL testleri |

## İlgili Kayıt İlişkilendirmesi

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Dosya ilgili kayıtla ilişkilendirilebilmeli | `StoredFile.RelatedRecordType`, `StoredFile.RelatedRecordId` | Domain, application ve E2E testleri |
| İki alan birlikte zorunlu olmalı | Domain, application ve API validation | Eksik alanla upload ve listeleme `400` |
| İlişki alanları normalize edilmeli | Domain ve application trim işlemleri | Birim testleri |
| İlişki bilgisi DTO'da dönmeli | `StoredFileDto` | Upload, detail ve list cevapları |
| İlgili kayda göre filtrelenebilmeli | Repository ve service `ListAsync` | Gerçek PostgreSQL filtreleme testi |
| Filtre hızlı sorgulanabilmeli | `ix_stored_files_related_record` | PostgreSQL index doğrulaması |
| Eski ilişkisiz kullanım korunmalı | Nullable kolonlar ve opsiyonel association | İlişkisiz upload testi |

## Identity ve JWT

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Identity ayrı servis olmalı | `FileManagement.Identity.Api` | Ayrı executable, image, port ve health endpoint'i |
| Identity persistence ayrı katman olmalı | `FileManagement.Identity.Infrastructure` | Solution ve container build |
| Kullanıcı verisi ayrı veritabanında olmalı | `IdentityDbContext`, `identity_management` | Startup migration ve login testi |
| Kullanıcı oluşturulabilmeli | `POST /api/auth/register` | `201 Created` |
| Aynı e-posta tekrar kullanılamamalı | `UserManager.FindByEmailAsync` | `409 Conflict` |
| Parolalar hashlenmeli | ASP.NET Core Identity `UserManager` | Identity persistence davranışı |
| Hatalı girişler lockout'a sayılmalı | `CheckPasswordSignInAsync(..., lockoutOnFailure: true)` | Hatalı login `401` |
| JWT üretilebilmeli | `IJwtTokenService`, `JwtTokenService` | Identity unit testi ve login smoke testi |
| JWT issuer doğrulanmalı | Identity ve File API `JwtOptions` | File API geçerli token testi |
| JWT audience doğrulanmalı | Identity ve File API `JwtOptions` | File API geçerli token testi |
| JWT imzası doğrulanmalı | `SymmetricSecurityKey` | Bozulmuş token `401` |
| JWT süresi doğrulanmalı | `ValidateLifetime` | Frontend süre yönetimi ve API doğrulaması |
| Kullanıcı rolleri token'a eklenmeli | `IdentityRoleNames`, JWT claims | Admin cevabında `Admin, User` |
| Normal kullanıcı `User` rolü almalı | Register akışı | Register cevabında `User` |
| Admin hesabı seed edilmeli | `IdentityDataSeeder` | Admin login ve `/admin/ping` |
| Role dayalı erişim olmalı | `[Authorize(Roles = "Admin")]` | Admin `200`, normal kullanıcı `403` |
| Aktif kullanıcı görüntülenebilmeli | `GET /api/auth/me` | Geçerli JWT ile `200` |

## File API Güvenliği

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| File endpoint'leri authentication gerektirmeli | `FilesController` üzerindeki `[Authorize]` | Anonim `/api/files` `401` |
| File API token'ı yerel doğrulamalı | `AddJwtBearer` ve `TokenValidationParameters` | Identity API'ye ek doğrulama çağrısı olmadan `200` |
| Geçersiz token reddedilmeli | JWT Bearer middleware | Bozulmuş token `401` |
| Health anonim kalmalı | `/health` endpoint'i | Token olmadan `200` |
| Swagger anonim erişilebilir olmalı | `UseSwaggerUI`, `MapOpenApi` | Swagger ve OpenAPI `200` |
| OpenAPI Bearer scheme içermeli | `BearerSecuritySchemeTransformer` | Scheme değeri `bearer` |
| Korumalı operation'lar işaretlenmeli | `AuthorizationOperationTransformer` | 7 File operation'ı Bearer korumalı |
| Register ve login anonim görünmeli | Identity OpenAPI transformer | Security requirement bulunmuyor |
| `me` ve admin endpoint'i korumalı görünmeli | Identity OpenAPI transformer | Bearer security requirement mevcut |

## Frontend Authentication

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Login ekranı olmalı | `LoginScreen.tsx` | Tarayıcı smoke testi |
| Login Identity API'ye gitmeli | `authApi.ts` | Nginx proxy üzerinden `200` |
| Token otomatik eklenmeli | `httpClient.ts` request interceptor | Korumalı dosya listesi `200` |
| Oturum sekme bazlı saklanmalı | `authSession.ts`, `sessionStorage` | Refresh sonrasında oturum devam etti |
| Süresi dolan oturum temizlenmeli | Expiration timer | Otomatik logout davranışı |
| `401` oturumu temizlemeli | Axios response interceptor | Unauthorized akışı |
| Logout desteklenmeli | `clearAuthSession` ve UI düğmesi | Login ekranına dönüş |
| Kullanıcı ve roller görüntülenmeli | `App.tsx` header | Admin e-postası ve `Admin · User` |
| Download Bearer token göndermeli | Axios Blob download | Tarayıcı ve hash testi |
| Preview Bearer token göndermeli | Axios Blob preview | Tarayıcı ve API `200` |
| Mevcut upload/filter/table korunmalı | `App.tsx`, `FileTable`, `FileUploadDropzone` | Tarayıcı E2E testi |
| Responsive login ve uygulama görünümü | `App.css` | Masaüstü tarayıcı testi |

## Gözlemlenebilirlik

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Yapılandırılmış loglama | Serilog yapılandırması | Console ve Seq event'leri |
| Merkezi log hedefi | `Serilog.Sinks.Seq` | Seq ekranında Identity ve File logları |
| Servis kimliği loglanmalı | `Application` property | `FileManagement.Api` ve `FileManagement.Identity.Api` |
| Ortam bilgisi loglanmalı | `Environment` property | Production log property |
| HTTP istekleri loglanmalı | `UseSerilogRequestLogging` | Method, path, status ve elapsed |
| Correlation ID desteklenmeli | Her iki API'de `CorrelationIdMiddleware` | Request ve response ID eşleşmesi |
| Kullanıcı bilgisi loglanmalı | File API diagnostic context | `UserId` ve `UserName` |
| Health log gürültüsü azaltılmalı | Health için Debug seviyesi | Seq event yoğunluğu azaltıldı |
| Data Protection gürültüsü azaltılmalı | Serilog category override | Yeni startup loglarında uyarı yok |

## API

| Gereksinim | Endpoint veya dosya | Doğrulama |
|---|---|---|
| Register | `POST /api/auth/register` | `201 Created` |
| Login | `POST /api/auth/login` | `200 OK` ve access token |
| Current user | `GET /api/auth/me` | `200 OK` |
| Admin authorization | `GET /api/auth/admin/ping` | Admin `200`, User `403` |
| Upload | `POST /api/files` | Bearer token ile `201 Created` |
| Listeleme | `GET /api/files` | Bearer token ile `200 OK` |
| Filtreli listeleme | `relatedRecordType`, `relatedRecordId` | Tek eşleşen kayıt |
| Metadata detayı | `GET /api/files/{id}` | İlişki alanlarıyla `200 OK` |
| Download | `GET /api/files/{id}/download` | Kaynakla aynı SHA-256 |
| Preview | `GET /api/files/{id}/preview` | Desteklenen içerikte `200` |
| Presigned URL | `GET /api/files/{id}/presigned-url` | URL üzerinden başarılı indirme |
| Delete | `DELETE /api/files/{id}` | `204 No Content` |
| Validation problemi | ASP.NET Core model validation | Problem Details biçiminde `400` |
| OpenAPI | Her iki API'de `/openapi/v1.json` | `200 OK` |
| Swagger UI | Her iki API'de `/swagger` | `200 OK` |

## Altyapı ve Kalite

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| PostgreSQL container | Compose `postgres` | Healthy |
| Identity database init işi | Compose `identity-db-init` | Başarıyla tamamlandı |
| MinIO container | Compose `minio` | Healthy |
| Seq container | Compose `seq` | Çalışıyor ve `/health` `200` |
| File API container | Compose `api` | Healthy |
| Identity API container | Compose `identity-api` | Healthy |
| Web container | Compose `web` | Healthy |
| Ayrı Identity image | `docker/identity-api/Dockerfile` | CI container build |
| Nginx Identity yönlendirmesi | `/api/auth/` location | Proxy login `200` |
| Nginx File yönlendirmesi | `/api/` location | Anonim `401`, yetkili `200` |
| File birim testleri | `FileManagement.UnitTests` | 14 test başarılı |
| Identity birim testleri | `FileManagement.Identity.UnitTests` | 1 test başarılı |
| Toplam birim testi | İki test projesi | 15 / 15 başarılı |
| Frontend lint | `npm run lint` | 0 uyarı, 0 hata |
| Frontend production build | `npm run build` | Başarılı |
| npm güvenlik denetimi | `npm audit --audit-level=high` | Zafiyet yok |
| Backend Release build | `dotnet build -c Release` | Başarılı |
| NuGet güvenlik denetimi | `NuGetAuditMode=all` | Zafiyet yok |
| CI | `.github/workflows/ci.yml` | Backend, Frontend ve Containers işleri |

## Identity/JWT Özellik Commit'leri

| Commit | Kapsam |
|---|---|
| `eac37dd` | Identity servisinin backend, persistence, migration, seed ve unit test temeli |
| `620a338` | Identity Swagger/OpenAPI Bearer desteği |
| `ea8998a` | Identity API Docker ve Compose entegrasyonu |
| `fe2847a` | File API JWT koruması ve şablon endpoint temizliği |
| `bdbac98` | React login, session, interceptor, download/preview ve logout akışı |
| `4192e84` | File API Data Protection log gürültüsü düzenlemesi |

## İlgili Kayıt Özellik Commit'leri

| Commit | Kapsam |
|---|---|
| `70a6768` | Domain modeli, EF yapılandırması, migration ve domain testleri |
| `3463617` | Application servisi, DTO ve repository filtreleme |
| `c367f3c` | API modelleri, controller, OpenAPI ve Swagger UI |
| `bd7eb20` | Frontend upload, filtre ve tablo desteği |
| `1471407` | Dokümantasyon ve gereksinim kanıtları |
