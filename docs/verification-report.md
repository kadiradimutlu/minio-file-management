# Doğrulama Raporu

## Tarih

22 Temmuz 2026

## Doğrulanan Branch

~~~text
feature/related-record-association
~~~

## Otomatik Kontroller

| Kontrol | Sonuç |
|---|---|
| Backend Release build | Başarılı |
| xUnit testleri | 14 / 14 başarılı |
| EF Core pending model changes | Değişiklik yok |
| Frontend lint | 0 uyarı, 0 hata |
| Frontend production build | Başarılı |
| Docker Compose build | Başarılı |
| Git whitespace kontrolü | Başarılı |

Vite production build sırasında bundle boyutuyla ilgili performans uyarısı görüntülenmiştir. Bu uyarı build işlemini başarısız kılmamıştır.

## Docker Servisleri

Aşağıdaki dört servis healthy durumda doğrulanmıştır:

- `postgres`
- `minio`
- `api`
- `web`

## İlgili Kayıt API Smoke Testi

Kullanılan örnek ilişki:

~~~text
relatedRecordType = Student
relatedRecordId   = UUID
~~~

| Test | Beklenen | Sonuç |
|---|---|---|
| İlişkili dosya yükleme | `201 Created` | Başarılı |
| Upload cevabında ilişki alanları | Doğru değerler | Başarılı |
| İlgili kayda göre filtreleme | Tek eşleşen dosya | Başarılı |
| Metadata detay endpoint'i | İlişki alanları mevcut | Başarılı |
| Yalnızca ilişki türüyle upload | `400 Bad Request` | Başarılı |
| Yalnızca ilişki türüyle listeleme | `400 Bad Request` | Başarılı |

## PostgreSQL Doğrulaması

Aşağıdaki kolonlar doğrulanmıştır:

| Kolon | Nullable | Maksimum uzunluk |
|---|---|---:|
| `related_record_type` | Evet | 100 |
| `related_record_id` | Evet | 255 |

Doğrulanan index:

~~~text
ix_stored_files_related_record
~~~

Index kolon sırası:

~~~text
related_record_type, related_record_id
~~~

## Dosya Bütünlüğü

Kaynak dosya ve aşağıdaki üç erişim yöntemi için SHA-256 değerlerinin aynı olduğu doğrulanmıştır:

- Doğrudan download endpoint'i
- Preview endpoint'i
- Presigned MinIO URL'si

Bu sonuç dosya içeriğinin upload ve download akışlarında değişmediğini doğrular.

## OpenAPI ve Swagger

| Endpoint | HTTP sonucu |
|---|---:|
| `/openapi/v1.json` | 200 |
| `/swagger/index.html` | 200 |
| `/swagger` | 200 |

OpenAPI dokümanında aşağıdaki alanlar doğrulanmıştır:

~~~text
relatedRecordType
relatedRecordId
~~~

## Silme ve Temizlik

Silme sonrasında:

- Delete endpoint'i `204 No Content` döndürdü.
- Metadata detail endpoint'i `404 Not Found` döndürdü.
- Filtreli liste boş dizi döndürdü.
- PostgreSQL kalan kayıt sayısı `0` oldu.
- Eski presigned URL üzerinden MinIO nesnesine erişim reddedildi.

## Tarayıcı Smoke Testi

Aşağıdaki frontend davranışları manuel olarak doğrulanmıştır:

1. İlgili kayıt türü ve kimliği alanları görüntülendi.
2. Yalnızca bir ilişki alanı doldurulduğunda upload engellendi.
3. İki ilişki alanıyla dosya başarıyla yüklendi.
4. İlişki bilgileri dosya tablosunda görüntülendi.
5. İlgili kayda göre filtreleme çalıştı.
6. Filtre temizlendiğinde bütün dosyalar yeniden görüntülendi.
7. Test dosyası arayüz üzerinden başarıyla silindi.

## Sonuç

İlgili kayıt ilişkilendirme özelliği domain, persistence, application, API, frontend ve Docker ortamında uçtan uca doğrulanmıştır.
