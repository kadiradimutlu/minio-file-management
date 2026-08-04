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

- 12 uzun yaşayan servis çalışır ve sağlıklıdır.
- `identity-db-init`, `kafka-data-init` ve `kafka-init` başarıyla
  tamamlanıp `Exited (0)` durumunda kalır.
- Compose toplam 15 servis içerir.

Durumu görüntülemek için:

```powershell
docker compose `
    --env-file ".env" `
    ps `
    --all
```

## On Dakikalık Demo Akışı

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

## Otomatik Final Doğrulaması

Final doğrulama betiği mevcut `.env` dosyasını ve çalışan normal
stack'i kullanmaz. Ayrı bir Compose proje adı, ayrı portlar, çalışma
anında üretilen rastgele parolalar ve ayrı volume'lar oluşturur:

```powershell
& ".\scripts\verify-isolated-e2e.ps1"
```

Betik aşağıdaki kontrolleri yapar:

- 15 servislik Compose configuration ve temiz image build
- 12 uzun yaşayan servis ve 3 başarılı tek-seferlik init işi
- Web, Gateway, File API, Identity API ve Reporting health
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
| Compose servisleri | 15 |
| Uzun yaşayan servisler | 12 |
| Başarılı init işleri | 3 |
| Health endpoint'leri | 5 / 5 |
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
loglarını ve `file-operations.v1` topic'ini kontrol edin.

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

`--volumes` seçeneği PostgreSQL, MinIO, Kafka ve Seq verilerini
siler. Açık bir veri temizleme kararı olmadan kullanılmamalıdır.
