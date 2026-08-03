# Gereksinim–Kanıt Matrisi

Bu belge; dosya yönetimi, Redis metadata cache, Identity, JWT, YARP API Gateway, frontend authentication, transactional outbox, Kafka event pipeline, Hangfire reporting ve gözlemlenebilirlik gereksinimlerini repository içindeki uygulama ve doğrulama kanıtlarıyla eşleştirir.

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

## Redis Metadata Cache

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Cache API sözleşmesini değiştirmemeli | `CachedFileManagementService` decorator'ı | Mevcut File API endpoint'leri ve DTO'ları değişmedi |
| Liste metadata'sı cache'lenmeli | `GetListAsync` / `SetListAsync` ve filtre hash'i | Aynı filtre için beklenen Redis anahtarı runtime'da doğrulandı |
| Detail metadata'sı cache'lenmeli | `GetFileAsync` / `SetFileAsync` | Upload sonrası detail anahtarı runtime'da bulundu |
| Dosya içeriği cache'lenmemeli | Download, preview ve presigned URL çağrıları doğrudan ana servise delegate edilir | Decorator birim testleri |
| Upload cache'i güncellemelidir | Detail warm-up ve liste nesli invalidation | Runtime upload ve düz GUID generation doğrulaması |
| Delete cache'i temizlemelidir | Detail eviction ve liste nesli invalidation | Runtime delete sonrası anahtar ve generation doğrulaması |
| Eski liste anahtarları erişilemez olmalıdır | Generation tabanlı liste anahtarı | Invalidation birim testi |
| Redis kesintisi ana işlemi durdurmamalıdır | Recoverable cache hatalarında fail-open davranışı | Redis durdurularak list/detail/upload/delete doğrulandı |
| Cache isteğe bağlı olmalıdır | `NullFileMetadataCache` ve `FileCache:Enabled` | Cache kapalı yapılandırma kod doğrulaması |
| TTL ve timeout değerleri sınırlandırılmalıdır | `FileMetadataCacheOptions` ve `RedisCacheConnectionOptions` validation | Başlangıç validation ve birim testleri |
| Redis parola korumalı olmalıdır | Compose `requirepass` ve authenticated healthcheck | Authenticated `PONG`, unauthenticated `NOAUTH` |
| Redis verisi geçici olmalıdır | `--save ""` ve `--appendonly no` | Compose configuration doğrulaması |

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
| Servis logları ayrıştırılabilmeli | `Application` enrichment | Gateway, Identity, File API, Outbox Worker ve Operations Worker log kimlikleri |
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

## Transactional Outbox ve Kafka

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Versiyonlu event contract'ı | `IntegrationEventEnvelope<T>` ve `FileOperationOccurredV1` | 4 contract testi |
| Upload event'i üretilmeli | `FileManagementService.UploadAsync` ve `FileOperationOutbox` | Application ve outbox birim testleri |
| Download event'i üretilmeli | `FileManagementService.DownloadAsync` | Application birim testleri ve runtime Kafka tüketimi |
| Delete event'i üretilmeli | `FileManagementService.DeleteAsync` | Application birim testleri ve runtime Kafka tüketimi |
| Preview download sayılmamalı | `PreviewAsync` içinde `recordDownloadOperation: false` | Application birim testi |
| Presigned URL download sayılmamalı | `CreatePresignedGetUrlAsync` outbox yazmaz | Application birim testi |
| Metadata ve outbox atomik olmalı | Aynı `FileManagementDbContext` ve `SaveChangesAsync` transaction sınırı | Persistence ve application testleri |
| Pending outbox mesajları Kafka'ya yayımlanmalı | `FileManagement.Outbox.Worker` | Publisher/cycle testleri ve runtime pending `0` |
| Kafka topic açıkça hazırlanmalı | `kafka-init` ve `file-operations.v1` | Compose runtime topic describe |
| Consumer auto commit kullanmamalı | `EnableAutoCommit = false` ve manuel `Commit` | Consumer kod doğrulaması |
| Eventler Operations Worker tarafından tüketilmeli | `KafkaFileOperationConsumer` ve `LoggingFileOperationEventHandler` | Runtime consumer logları ve lag `0` |
| Event ve request izlenebilir olmalı | Event ID, file ID, actor user ID ve correlation ID | Contract testleri ve yapılandırılmış loglar |

## Hangfire Reporting

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Ayrı reporting worker olmalı | `FileManagement.Reporting.Worker` | Solution Release build ve ayrı container image |
| Job storage kalıcı olmalı | `Hangfire.PostgreSql`, ayrı `hangfire` şeması | Runtime şema ve recurring job kaydı |
| Günlük upload/download/delete özeti üretilmeli | `DailyFileOperationsReportJob` ve `DailyFileOperationsReportCalculator` | Manuel job ile gerçek rapor satırı |
| Upload content type ve byte toplamları raporlanmalı | `UploadedContentTypes`, `UploadedBytes`, `DownloadedBytes` | Calculator birim testleri |
| Outbox tanılama bilgileri raporlanmalı | Pending, failed ve invalid event alanları | Unit test ve runtime rapor sonucu |
| Rapor üretimi idempotent olmalı | `report_date` doğal primary key ve `Refresh` akışı | Domain testi ve aynı tarih için upsert davranışı |
| Geçici hatalar retry edilmeli | `AutomaticRetry`, 60/300/900 saniye gecikmeler | Reflection tabanlı job configuration testleri |
| Aynı job eşzamanlı çalışmamalı | `DisableConcurrentExecution(600)` | Job configuration testleri |
| Dashboard korunmalı | Ayrı Basic Authentication doğrulaması | Kimliksiz `401`, doğru kimlikle `200` |
| Dashboard salt okunur olmalı | `DashboardOptions.IsReadOnlyFunc` | Configuration ve runtime dashboard kontrolü |
| Reporting API korunmalı | `ReportingAdministrator` authorization policy | Kimliksiz `401`, yetkili enqueue `202` |
| Servis host erişimi sınırlandırılmalı | Compose port binding `127.0.0.1` | Render edilmiş Compose configuration |

## Altyapı ve Kalite

| Kontrol | Sonuç |
|---|---|
| Solution Release build | Başarılı |
| Toplam birim testi | 83 / 83 başarılı |
| Contracts testleri | 4 / 4 başarılı |
| Operations testleri | 3 / 3 başarılı |
| Outbox testleri | 10 / 10 başarılı |
| Identity testleri | 1 / 1 başarılı |
| Domain, application ve infrastructure testleri | 48 / 48 başarılı |
| Reporting testleri | 17 / 17 başarılı |
| NuGet vulnerability audit | Güvenlik açığı bulunmadı |
| Frontend lint | 0 hata, 0 uyarı |
| Frontend production build | Başarılı |
| Gateway image build | Başarılı |
| Operations Worker image build | Başarılı |
| Reporting Worker image build | Başarılı |
| Web image build | Başarılı |
| Docker Compose servis sayısı | 15 |
| Web, Gateway, File API, Identity API ve Seq health | `200` |
| Gateway container health | `healthy` |
| Redis container health | `healthy` |
| Reporting container health | `healthy` |
| Hangfire Dashboard authentication | Kimliksiz `401`, yetkili `200` |
| Reporting manual enqueue | `202`, rapor satırı üretildi |
| Redis kesintisinde PostgreSQL fallback | Başarılı |
| Download hash doğrulaması | SHA-256 eşleşti |
| Pending outbox mesajı | `0` |
| Kafka consumer group lag | `0` |
| Working tree kontrolü | Değişiklikler yalnız Hangfire reporting milestone kapsamındadır; commit oluşturulmadı |

Vite, ana JavaScript chunk'ı için 500 kB sınır uyarısı vermektedir. Build başarılıdır; code splitting daha sonraki performans iyileştirmesi olarak izlenecektir.

## Gateway Commit Kanıtları

| Commit | Açıklama |
|---|---|
| `e43be37` | YARP Gateway proje temeli, route/cluster yapılandırması ve health endpoint'i |
| `0d514ce` | Gateway tarafından üretilen correlation ID'nin downstream request'e aktarılması |
| `ccf7828` | Docker, Compose, Nginx, Vite ve CI Gateway entegrasyonu |

## Kafka Operations Commit Kanıtları

| Commit | Açıklama |
|---|---|
| `802c83c` | Kafka broker ve topic initialization |
| `d1ed593` | Versiyonlu file operation contract'ları |
| `b020a57` | Contract testleri için xUnit namespace düzeltmesi |
| `9e6e43e` | Operations Kafka consumer temeli |
| `91306b7` | Operations Worker container entegrasyonu |
| `78aac1c` | Transactional outbox persistence temeli |
| `3312daf` | File operation outbox writer |
| `b86acea` | Upload event enqueue akışı |
| `940537f` | Outbox eventlerini Kafka'ya yayımlama |
| `0eea9c5` | Download event üretimi |
| `ce1e2ae` | Delete event üretimi |
