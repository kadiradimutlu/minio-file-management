# Redis Metadata Cache Doğrulama Raporu

## Kapsam

Bu rapor, `feature/redis-file-metadata-cache` branch'inde File API'ye eklenen Redis tabanlı metadata cache milestone'unu kapsar.

Cache yalnızca aşağıdaki okuma sonuçlarını hızlandırır:

- `GET /api/files`
- `GET /api/files/{id}`

Dosya byte'ları, download ve preview stream'leri ile presigned URL'ler cache'e yazılmaz. PostgreSQL metadata için, MinIO ise dosya içeriği için kaynak olmaya devam eder.

## Tasarım

~~~text
File API
   |
   v
CachedFileManagementService
   |
   |-- cache hit ------> Redis metadata
   |
   `-- cache miss -----> FileManagementService
                              |
                              |--> PostgreSQL metadata
                              `--> MinIO file content
~~~

`CachedFileManagementService`, mevcut `IFileManagementService` sözleşmesini decorator olarak sarar. Böylece controller ve API contract'ları değiştirilmeden cache-aside davranışı eklenmiştir.

| Davranış | Sonuç |
|---|---|
| Liste cache miss | PostgreSQL'den oku, sonucu Redis'e yaz |
| Detail cache miss | PostgreSQL'den oku, sonucu Redis'e yaz |
| Upload başarılı | Detail metadata'yı cache'e yaz, liste neslini değiştir |
| Delete başarılı | Detail anahtarını sil, liste neslini değiştir |
| Redis erişilemiyor | Uyarı logla, PostgreSQL üzerinden devam et |
| Cache kapalı | `NullFileMetadataCache` ile doğrudan ana servise devam et |

## Anahtar ve Süre Politikası

| Alan | Değer |
|---|---|
| Compose key prefix | `file-management:production:files:v1` |
| Detail TTL | 300 saniye |
| Liste TTL | 30 saniye |
| Liste invalidation | GUID tabanlı generation anahtarı |
| Filtre ayrımı | Normalize edilmiş association değerlerinin SHA-256 hash'i |
| Redis connect timeout | 1000 ms |
| Redis operation timeout | 500 ms |

Generation değeri düz, 32 karakterli GUID olarak saklanır. Eski liste anahtarlarını topluca silmek yerine generation değiştirilir; eski anahtarlar kısa TTL sonunda kendiliğinden sona erer.

## Güvenlik ve Compose

- Resmî `redis:8.8.1-alpine` image'ı kullanılır.
- Redis `requirepass` ile parola korumalıdır.
- Healthcheck aynı parolayla authenticated `PING` çalıştırır.
- Host portu yalnızca `127.0.0.1` üzerinde açılır.
- Cache için persistence kapalıdır: RDB save ve AOF kullanılmaz.
- Container belleği 128 MB ile sınırlandırılmıştır.
- Parola source control'e yazılmaz; `.env.example` yalnızca placeholder içerir.

Runtime doğrulamasında authenticated `PING` sonucu `PONG`, parolasız bağlantı sonucu `NOAUTH` olmuştur.

## Otomatik Doğrulama

| Kontrol | Sonuç |
|---|---|
| NuGet restore ve audit | Başarılı |
| Solution Release build | 0 uyarı, 0 hata |
| Contracts testleri | 4 / 4 |
| Operations testleri | 3 / 3 |
| Outbox testleri | 10 / 10 |
| Identity testleri | 1 / 1 |
| Domain, application ve infrastructure testleri | 48 / 48 |
| Toplam test | 66 / 66 |
| Whitespace kontrolü | Başarılı |
| Compose configuration | Geçerli |
| Compose servis sayısı | 14 |

Cache testleri; hit/miss, boş liste, filtre anahtarı, generation invalidation, düz GUID generation, timeout fail-open, cancellation propagation, decorator delegasyonu, upload warm-up ve delete eviction davranışlarını kapsar.

## Runtime Doğrulama

JWT ile Gateway üzerinden aşağıdaki senaryo çalıştırılmıştır:

1. Aynı filtreyle iki liste isteği gönderildi ve tam beklenen Redis liste anahtarı doğrulandı.
2. Geçici dosya yüklendi; detail cache warm-up ve liste generation değişimi doğrulandı.
3. Detail ve filtreli liste endpoint'leri yeni dosyayı döndürdü.
4. Dosya silindi; detail cache eviction ve ikinci liste invalidation doğrulandı.
5. Redis container'ı durduruldu.
6. Liste, upload, detail ve delete işlemlerinin PostgreSQL fallback ile başarılı kaldığı doğrulandı.
7. Redis yeniden başlatıldı ve `healthy` durumuna döndüğü doğrulandı.
8. API bağlantıyı otomatik toparladı ve ilk liste isteğinde cache'i yeniden doldurdu.
9. Doğrulama kayıtlarının tamamı temizlendi.

Kesinti sırasında API yalnızca beklenen fail-open warning loglarını üretti; ana dosya operasyonları başarısız olmadı.

## Kafka ve Outbox Regresyonu

Cache milestone'undan sonra mevcut event pipeline ayrıca kontrol edilmiştir:

| Kontrol | Sonuç |
|---|---|
| Pending outbox mesajı | `0` |
| `operations-worker-v1` partition sayısı | `3` |
| Maksimum Kafka consumer lag | `0` |

Bu sonuç, Redis katmanının upload/delete outbox üretimini ve Kafka consumer akışını bozmadığını gösterir.

## Sonuç

Redis metadata cache; API contract'ı, PostgreSQL kaynak doğruluğu, MinIO stream akışı ve transactional outbox/Kafka pipeline korunarak entegre edilmiştir.

Cache kullanılabilir olduğunda liste ve detail metadata okumalarını hızlandırır; kullanılamadığında servis PostgreSQL üzerinden çalışmaya devam eder.
