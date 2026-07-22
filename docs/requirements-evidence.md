# Gereksinim–Kanıt Matrisi

Bu belge, dosya yönetim modülü gereksinimlerini repository içindeki uygulama ve doğrulama kanıtlarıyla eşleştirir.

## Backend ve Depolama

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Yeniden kullanılabilir storage servisi | `IFileStorageService` ve MinIO implementasyonu | Application birim testleri ve Docker smoke testi |
| Bucket oluşturma | `EnsureBucketExistsAsync` | API başlangıcında bucket hazırlama ve container testi |
| Dosya yükleme | `UploadAsync` | Unit test ve gerçek MinIO upload testi |
| Dosya indirme | `DownloadAsync` | SHA-256 eşitlik testi |
| Dosya silme | `DeleteAsync` | API `204`, PostgreSQL kayıt sayısı `0`, MinIO erişimi başarısız |
| Nesne varlık kontrolü | `ExistsAsync` | Storage soyutlaması |
| Süreli erişim URL'si | `CreatePresignedGetUrlAsync` | Presigned URL üzerinden SHA-256 testi |
| Metadata PostgreSQL üzerinde saklanmalı | `StoredFile`, `FileManagementDbContext`, repository | PostgreSQL sorgusu ve API detay endpoint'i |
| Metadata başarısızlığında MinIO geri alma | `FileManagementService.UploadAsync` rollback akışı | `UploadAsync_WhenDatabaseSaveFails_DeletesObject` |
| EF Core migration | `Persistence/Migrations` | Pending model change kontrolü |

## İlgili Kayıt İlişkilendirmesi

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Dosya ilgili kayıtla ilişkilendirilebilmeli | `StoredFile.RelatedRecordType`, `StoredFile.RelatedRecordId` | Domain ve application testleri |
| İki alan birlikte zorunlu olmalı | Domain, application ve API validation | Eksik alanla upload ve listeleme `400` |
| İlişki alanları normalize edilmeli | Domain ve application trim işlemleri | Unit testlerde boşluklu değerler |
| İlişki bilgisi DTO'da dönmeli | `StoredFileDto` | Upload, detail ve list API cevapları |
| İlgili kayda göre filtrelenebilmeli | Repository ve service `ListAsync` | Gerçek PostgreSQL filtreleme smoke testi |
| Filtre hızlı sorgulanabilmeli | `ix_stored_files_related_record` | `pg_indexes` sorgusu |
| Eski ilişkisiz kullanım korunmalı | Eski service overload'ları ve nullable kolonlar | İlişkisiz upload birim testi |

## API

| Gereksinim | Endpoint veya dosya | Doğrulama |
|---|---|---|
| Upload | `POST /api/files` | `201 Created` |
| Listeleme | `GET /api/files` | `200 OK` |
| Filtreli listeleme | Query: `relatedRecordType`, `relatedRecordId` | Tek eşleşen kayıt |
| Metadata detayı | `GET /api/files/{id}` | İlişki alanlarıyla `200 OK` |
| Download | `GET /api/files/{id}/download` | Kaynakla aynı SHA-256 |
| Preview | `GET /api/files/{id}/preview` | Kaynakla aynı SHA-256 |
| Presigned URL | `GET /api/files/{id}/presigned-url` | URL üzerinden başarılı indirme |
| Delete | `DELETE /api/files/{id}` | `204 No Content` |
| Validation problemi | ASP.NET Core model validation | Problem Details biçiminde `400` |
| OpenAPI | `/openapi/v1.json` | `200 OK` ve ilişki alanları mevcut |
| Swagger UI | `/swagger` | `200 OK` |

## Frontend

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| Yeniden kullanılabilir drag-drop bileşeni | `FileUploadDropzone.tsx` | Tarayıcı smoke testi |
| Tekli ve çoklu seçim | Ant Design `Dragger` ve `multiple` | Tarayıcı smoke testi |
| Boyut ve uzantı doğrulaması | `beforeUpload` | Lint, build ve tarayıcı testi |
| Yükleme ilerlemesi | Axios `onUploadProgress` | Tarayıcı smoke testi |
| Başarı ve hata bildirimleri | Ant Design message API | Tarayıcı smoke testi |
| Dosya listeleme | `FileTable.tsx` | Tarayıcı smoke testi |
| Download ve preview | Tablo aksiyonları | API ve tarayıcı testi |
| Silme | `Popconfirm` ve API çağrısı | Tarayıcı smoke testi |
| İlişki bilgisi girişi | Upload form alanları | Tarayıcı smoke testi |
| İlişkiye göre filtreleme | `App.tsx` filtre paneli | Tarayıcı smoke testi |
| İlişki bilgisini tabloda gösterme | İlgili kayıt sütunu | Tarayıcı smoke testi |
| Responsive görünüm | Ant Design grid ve CSS media query | Production build ve tarayıcı testi |

## Altyapı ve Kalite

| Gereksinim | Uygulama kanıtı | Doğrulama |
|---|---|---|
| PostgreSQL container | Docker Compose `postgres` servisi | Healthy |
| MinIO container | Docker Compose `minio` servisi | Healthy |
| API container | Docker Compose `api` servisi | Healthy |
| Web container | Docker Compose `web` servisi | Healthy |
| Nginx reverse proxy | Web container yapılandırması | Web ve `/api` erişimi |
| Birim testleri | `FileManagement.UnitTests` | 14 test başarılı |
| Frontend lint | `npm run lint` | 0 uyarı, 0 hata |
| Frontend production build | `npm run build` | Başarılı |
| Backend Release build | `dotnet build -c Release` | Başarılı |
| CI | `.github/workflows/ci.yml` | Backend, Frontend ve Containers işleri |

## Özellik Commit'leri

| Commit | Kapsam |
|---|---|
| `70a6768` | Domain modeli, EF yapılandırması, migration ve domain testleri |
| `3463617` | Application servisi, DTO ve repository filtreleme |
| `c367f3c` | API modelleri, controller, OpenAPI ve Swagger UI |
| `bd7eb20` | Frontend upload, filtre ve tablo desteği |
