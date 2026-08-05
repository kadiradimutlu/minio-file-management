# Demo ve Operasyon Runbook'u

## Amaç

Bu runbook, MinIO File Management projesinin güvenli biçimde
başlatılması, kısa bir teknik demo yapılması ve final doğrulamasının
tekrar çalıştırılması için hazırlanmıştır.

## Ön Koşullar

- Docker Desktop çalışıyor olmalıdır.
- Docker Compose kullanılabilir olmalıdır.
- Backend doğrulaması için .NET 10 SDK kurulmalıdır.
- Frontend doğrulaması için Node.js 24 ve npm kurulmalıdır.
- Repository kök dizininde çalışılmalıdır.

## Normal Lokal Başlangıç

Örnek ortam dosyasını yalnızca ilk kurulumda kopyalayın:

```powershell
Copy-Item ".env.example" ".env"
```

`.env` içindeki bütün placeholder parolaları güçlü ve birbirinden
farklı değerlerle değiştirin. JWT signing key en az 32 rastgele
karakter olmalıdır. Gerçek secret değerlerini commit etmeyin.

Servisleri başlatın:

```powershell
docker compose `
    --env-file ".env" `
    up `
    --detach `
    --build `
    --wait
```

Beklenen durum:

- 14 uzun yaşayan servis çalışır ve sağlıklıdır.
- `identity-db-init`, `kafka-data-init` ve `kafka-init` başarıyla
  tamamlanıp `Exited (0)` durumunda kalır.
- Compose toplam 17 servis içerir.

Durumu görüntülemek için:

```powershell
docker compose `
    --env-file ".env" `
    ps `
    --all
```

## On Beş Dakikalık Demo Akışı

1. Web uygulamasını `http://127.0.0.1:8080` adresinde açın.
2. `.env` içindeki admin hesabıyla oturum açın.
3. Desteklenen bir PNG, JPEG veya PDF dosyası yükleyin.
4. Dosyanın listede ve detay görünümünde bulunduğunu gösterin.
5. Preview işlemini çalıştırın.
6. Dosyayı indirin ve içeriğin değişmediğini gösterin.
7. Presigned URL üretin ve dosyaya süreli erişimi gösterin.
8. Dosyayı silin ve artık listelenmediğini doğrulayın.
9. Seq arayüzünü `http://127.0.0.1:5341` adresinde açıp correlation
   ID içeren yapılandırılmış logları gösterin.
10. Hangfire dashboard'u `http://127.0.0.1:5100/hangfire` adresinde
    açıp recurring job ve tamamlanan job durumlarını gösterin.
11. Kafbat UI'ı `http://127.0.0.1:8085` adresinde açıp
    `file-operations.v1` topic mesajlarını, üç partition'ı ve
    `operations-worker-v1` consumer lag değerini gösterin.
12. RedisInsight'ı `http://127.0.0.1:5540` adresinde açıp metadata
    cache key'lerini gösterin.
13. Reporting Swagger'ı `http://127.0.0.1:5100/swagger` adresinde
    açıp Basic Authentication ve günlük rapor endpoint'lerini
    gösterin.
14. pgAdmin'de `stored_files`, `outbox_messages`,
    `daily_file_operation_reports` ve `hangfire` şemasını gösterin.

Her arayüzün bağlantı ve güvenli kullanım ayrıntıları
[Görsel Yönetim Arayüzleri Rehberi](visual-management-guide.md)
içinde bulunur.

## Otomatik Final Doğrulaması

Final doğrulama betiği mevcut `.env` dosyasını ve çalışan normal
stack'i kullanmaz. Ayrı bir Compose proje adı, ayrı portlar, çalışma
anında üretilen rastgele parolalar ve ayrı volume'lar oluşturur:

```powershell
& ".\scripts\verify-isolated-e2e.ps1"
```

Betik aşağıdaki kontrolleri yapar:

- 17 servislik Compose configuration ve temiz image build
- 14 uzun yaşayan servis ve 3 başarılı tek-seferlik init işi
- Web, Gateway, File API, Identity API, Reporting, Kafbat UI ve
  RedisInsight health
- Reporting Swagger ve Basic Authentication OpenAPI sözleşmesi
- anonymous erişim, hatalı login ve admin JWT sınırları
- upload, list, detail, download, preview, presigned URL ve delete
- upload/download/preview/presigned içerikleri için SHA-256 eşitliği
- Redis durdurulduğunda PostgreSQL fail-open fallback
- `uploaded`, `downloaded` ve `deleted` outbox kayıtları
- pending outbox `0`
- Kafka consumer group maksimum lag `0`
- Hangfire dashboard için kimliksiz `401`, yetkili `200`
- manuel günlük rapor ve `1/1/1/0/0/0` sayaçları

Başarılı veya hatalı tamamlanma sonunda betik yalnızca
`minio-file-management-final-audit` adlı geçici projenin container,
network ve volume'larını kaldırır. Normal proje volume'larını silmez.

## Kalite Kontrolleri

Backend:

```powershell
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
```

Frontend:

```powershell
Push-Location "src\FileManagement.Web"

npm ci
npm test
npm run lint
npm run build
npm audit --audit-level=high

Pop-Location
```

## Beklenen Final Kanıtları

| Kontrol | Beklenen |
|---|---:|
| Backend testleri | 99 / 99 |
| Frontend testleri | 11 / 11 |
| Toplam otomatik test | 110 / 110 |
| Compose servisleri | 17 |
| Uzun yaşayan servisler | 14 |
| Başarılı init işleri | 3 |
| Health endpoint'leri | 7 / 7 |
| Pending outbox | 0 |
| Maksimum Kafka lag | 0 |
| Günlük rapor operasyonları | 1 upload / 1 download / 1 delete |

## Sorun Giderme

### Port kullanımda

Normal stack için `.env` içindeki host portlarını değiştirin. İzole
doğrulama betiği port kullanımını başlamadan önce kontrol eder ve
mevcut bir kaynağı durdurmadan güvenli biçimde hata verir.

### Init servisleri exited görünüyor

`identity-db-init`, `kafka-data-init` ve `kafka-init` için
`Exited (0)` beklenen durumdur. Sıfırdan farklı exit code hata
anlamına gelir.

### Redis kullanılamıyor

File API metadata okumalarında PostgreSQL'e döner. Redis kaynak
değildir; toparlandığında cache yeniden doldurulur. Sürekli hata
durumunda API ve Redis logları Seq üzerinden incelenmelidir.

### Outbox pending sıfıra inmiyor

PostgreSQL, `outbox-worker`, Kafka ve `kafka-init` durumlarını
kontrol edin. Outbox worker loglarında publish/retry hata ayrıntıları
correlation ID ile bulunabilir.

### Kafka lag sıfıra inmiyor

`operations-worker-v1` consumer group durumunu, Operations Worker
loglarını ve Kafbat UI içindeki `file-operations.v1` topic'ini
kontrol edin.

### Kafbat UI giriş ekranı açılmıyor

`kafbat-ui` container health durumunu ve `KAFBAT_UI_PORT` değerini
kontrol edin. Kullanıcı adı ve parola `.env` içindeki
`KAFBAT_UI_USERNAME` ve `KAFBAT_UI_PASSWORD` değerleridir.

### RedisInsight bağlantısı görünmüyor

`redisinsight` ve `redis` container health durumlarını kontrol edin.
Bağlantı Compose tarafından `File Management Cache` adıyla
hazırlanır. RedisInsight içinden key silmeyin veya flush çalıştırmayın.

### Reporting erişimi 401

Reporting dashboard kimlik bilgileri uygulama admin hesabından
ayrıdır. `REPORTING_DASHBOARD_USERNAME` ve
`REPORTING_DASHBOARD_PASSWORD` değerlerini kullanın.

## Güvenli Durdurma

Verileri koruyarak:

```powershell
docker compose `
    --env-file ".env" `
    down
```

`--volumes` seçeneği PostgreSQL, MinIO, Kafka, Seq ve RedisInsight
verilerini siler. Açık bir veri temizleme kararı olmadan
kullanılmamalıdır.
