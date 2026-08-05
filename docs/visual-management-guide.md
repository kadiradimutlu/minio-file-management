# Görsel Yönetim Arayüzleri Rehberi

Bu rehber, MinIO File Management sisteminin çalışan parçalarını
sunum sırasında görsel olarak incelemek için kullanılacak tek
başvuru noktasıdır. Bütün arayüzler yalnız `127.0.0.1` üzerinde
yayınlanır; lokal geliştirme ve tanılama amacı taşır.

## Arayüz Özeti

| Bileşen | Arayüz | Adres | Kimlik bilgisi |
|---|---|---|---|
| Uygulama | React Web | `http://127.0.0.1:8080` | Identity admin veya kayıtlı kullanıcı |
| File API | Swagger UI | `http://127.0.0.1:5080/swagger` | Bearer JWT |
| Identity API | Swagger UI | `http://127.0.0.1:5090/swagger` | Endpoint'e göre anonim veya Bearer JWT |
| Reporting API | Swagger UI | `http://127.0.0.1:5100/swagger` | Reporting Basic Authentication |
| Job yönetimi | Hangfire Dashboard | `http://127.0.0.1:5100/hangfire` | Reporting Basic Authentication |
| Object storage | MinIO Console | `http://127.0.0.1:9001` | MinIO root hesabı |
| Loglar | Seq | `http://127.0.0.1:5341` | Seq admin hesabı |
| Kafka | Kafbat UI | `http://127.0.0.1:8085` | Kafbat UI hesabı |
| Redis | RedisInsight | `http://127.0.0.1:5540` | Lokal arayüz; Redis bağlantısı önceden tanımlıdır |
| PostgreSQL | Kurulu pgAdmin | Masaüstü uygulaması | PostgreSQL hesabı |

Kimlik bilgileri repository'ye yazılmaz. Lokal değerler `.env`
dosyasındaki ilgili değişkenlerden alınır:

- Web: `IDENTITY_ADMIN_EMAIL`, `IDENTITY_ADMIN_PASSWORD`
- Reporting Swagger ve Hangfire:
  `REPORTING_DASHBOARD_USERNAME`, `REPORTING_DASHBOARD_PASSWORD`
- MinIO: `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`
- Seq: kullanıcı adı `admin`, parola `SEQ_ADMIN_PASSWORD`
- Kafbat UI: `KAFBAT_UI_USERNAME`, `KAFBAT_UI_PASSWORD`
- PostgreSQL: `POSTGRES_USER`, `POSTGRES_PASSWORD`

## Kafbat UI ile Kafka

1. `http://127.0.0.1:8085` adresini açın.
2. `.env` içindeki Kafbat UI kullanıcı adı ve parolasıyla giriş
   yapın.
3. `MinIO File Management` cluster'ını seçin.
4. `Topics` bölümünde `file-operations.v1` topic'ini açın.
5. Üç partition'ı ve mesaj sayılarını gösterin.
6. `Messages` görünümünde upload, download ve delete eventlerini;
   event ID, file ID ve correlation ID alanlarıyla inceleyin.
7. `Consumers` bölümünde `operations-worker-v1` consumer group'unu
   açıp lag değerinin `0` olduğunu gösterin.

Kafbat UI Compose üzerinde salt okunur modda çalışır. Topic oluşturma,
silme veya yapılandırma değiştirme işlemleri kapalıdır.

## RedisInsight ile Redis

1. `http://127.0.0.1:5540` adresini açın.
2. Önceden tanımlı `File Management Cache` bağlantısını seçin.
3. Browser bölümünde
   `file-management:production:files:v1:*` prefix'li key'leri
   arayın.
4. Web uygulamasında bir dosya listesini veya detayını açtıktan sonra
   RedisInsight görünümünü yenileyerek cache kaydını gösterin.
5. Dosyayı sildikten sonra ilgili detail cache kaydının
   temizlendiğini gösterin.

Redis kaynak sistem değildir; PostgreSQL kaynak sistemdir. RedisInsight
veri üzerinde işlem yapabilen bir tanılama aracıdır. Sunumda yalnız
inceleme yapın; key silme, düzenleme veya flush işlemi uygulamayın.
Arayüz yalnız loopback'te yayınlanır ve bağlantı yönetimi Compose
üzerinde kapatılmıştır.

## pgAdmin ile PostgreSQL

pgAdmin içinde bir sunucu kaydedin:

### General

- Name: `MinIO File Management Local`

### Connection

- Host name/address: `127.0.0.1`
- Port: `.env` içindeki `POSTGRES_PORT` (`5432`)
- Maintenance database: `.env` içindeki `POSTGRES_DB`
  (`file_management`)
- Username: `.env` içindeki `POSTGRES_USER`
- Password: `.env` içindeki `POSTGRES_PASSWORD`
- Save password: yalnız kişisel lokal makinede isteğe bağlı

Bağlantı açıldığında şu alanlar gösterilebilir:

- `Databases > file_management > Schemas > public > Tables`
  - `stored_files`: dosya metadata kayıtları
  - `outbox_messages`: Kafka'ya yayımlanan güvenilir event kayıtları
  - `daily_file_operation_reports`: Hangfire günlük rapor sonuçları
- `Databases > file_management > Schemas > hangfire > Tables`
  - job, state, server ve recurring job storage tabloları
- `Databases > identity_management > Schemas > public > Tables`
  - `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` ve diğer Identity
    tabloları

Sunumda salt okunur kontrol için Query Tool içinde şu sorgular
kullanılabilir:

```sql
select
    id,
    original_file_name,
    content_type,
    size_bytes,
    created_at_utc
from stored_files
order by created_at_utc desc
limit 20;
```

```sql
select
    event_type,
    correlation_id,
    retry_count,
    processed_at_utc,
    created_at_utc
from outbox_messages
order by created_at_utc desc
limit 20;
```

```sql
select *
from daily_file_operation_reports
order by report_date desc
limit 20;
```

pgAdmin'de tablo veya kayıt silmeyin; yalnız `SELECT` sorguları
kullanın.

## Swagger Arayüzleri

### Identity API

`http://127.0.0.1:5090/swagger`

- Login ile JWT üretimini gösterin.
- Token'ı kopyalayıp `Authorize` düğmesinde kullanın.
- `me` ve admin ping endpoint'leriyle authentication ve role
  sınırını gösterin.

### File API

`http://127.0.0.1:5080/swagger`

- Identity API'den alınan JWT ile `Authorize` olun.
- Upload, list, detail, download, preview, presigned URL ve delete
  endpoint'lerini gösterin.
- Normal kullanıcı akışının Web ve Gateway üzerinden geçtiğini;
  doğrudan File API Swagger'ın tanılama amacı taşıdığını belirtin.

### Reporting API

`http://127.0.0.1:5100/swagger`

- `Authorize` düğmesinde `.env` içindeki reporting kullanıcı adı ve
  parolasını kullanın.
- Günlük raporları listeleyin.
- Gerekirse izin verilen bir tarih için rapor job'ını kuyruğa alın.
- Aynı kimlik bilgilerinin Hangfire Dashboard'u da koruduğunu
  belirtin.

## MinIO Console, Hangfire ve Seq

### MinIO Console

`http://127.0.0.1:9001`

- `files` bucket'ını açın.
- Web'den yüklenen nesnenin tarih klasörlü ve üretilmiş object name
  altında tutulduğunu gösterin.
- Orijinal dosya adının PostgreSQL metadata'sında kaldığını anlatın.

### Hangfire Dashboard

`http://127.0.0.1:5100/hangfire`

- `Recurring Jobs` içinde `daily-file-operations-report-v1`
  kaydını gösterin.
- `Succeeded` ekranında tamamlanan rapor job'larını gösterin.
- Dashboard salt okunurdur; delete, retry veya trigger işlemleri
  kapalıdır.

### Seq

`http://127.0.0.1:5341`

- Web işleminden dönen `X-Correlation-ID` değerini kullanarak
  `CorrelationId = 'değer'` sorgusu çalıştırın.
- Aynı zincirde Gateway, File API, Outbox Worker ve Operations
  Worker kayıtlarını gösterin.
- Upload, download ve delete operasyonlarının Kafka'ya yayımlanıp
  tüketildiğini loglardan doğrulayın.

## Önerilen Sunum Sırası

1. Web'de oturum açıp dosya upload, preview, download, presigned URL
   ve delete akışını gösterin.
2. MinIO Console'da fiziksel nesneyi, pgAdmin'de metadata ve outbox
   satırlarını gösterin.
3. RedisInsight'ta cache key'lerini gösterin.
4. Kafbat UI'da topic mesajlarını, partition'ları ve consumer lag
   değerini gösterin.
5. Seq'de aynı correlation ID boyunca uçtan uca log zincirini
   gösterin.
6. Hangfire'da recurring ve succeeded job'ları; Reporting Swagger'da
   üretilen günlük raporu gösterin.
7. File ve Identity Swagger ekranlarıyla API sözleşmesi ve
   authentication sınırını özetleyin.

Bu sıra, tek bir kullanıcı işleminin Web'den başlayıp object storage,
database, cache, outbox, Kafka, consumer, log ve raporlama
katmanlarında nasıl gözlemlendiğini anlatır.
