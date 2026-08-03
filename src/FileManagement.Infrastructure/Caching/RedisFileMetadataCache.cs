using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FileManagement.Application.Abstractions.Caching;
using FileManagement.Application.Files.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FileManagement.Infrastructure.Caching;

public sealed class RedisFileMetadataCache(
    IDistributedCache distributedCache,
    IOptions<FileMetadataCacheOptions> options,
    ILogger<RedisFileMetadataCache> logger)
    : IFileMetadataCache
{
    private const string InitialListGeneration =
        "initial";

    private static readonly JsonSerializerOptions
        SerializerOptions =
            new(JsonSerializerDefaults.Web);

    private readonly FileMetadataCacheOptions _options =
        options.Value;

    public async Task<CacheLookup<StoredFileDto>>
        GetFileAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        var key = CreateDetailKey(id);

        return await GetAsync<StoredFileDto>(
            key,
            "detail",
            cancellationToken);
    }

    public Task SetFileAsync(
        StoredFileDto file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        return SetAsync(
            CreateDetailKey(file.Id),
            file,
            TimeSpan.FromSeconds(
                _options.DetailTtlSeconds),
            "detail",
            cancellationToken);
    }

    public Task RemoveFileAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return RemoveAsync(
            CreateDetailKey(id),
            "detail",
            cancellationToken);
    }

    public async Task<
        CacheLookup<IReadOnlyList<StoredFileDto>>>
        GetListAsync(
            string? relatedRecordType,
            string? relatedRecordId,
            CancellationToken cancellationToken = default)
    {
        var generation =
            await GetListGenerationAsync(
                cancellationToken);

        if (generation is null)
        {
            return CacheLookup<
                IReadOnlyList<StoredFileDto>>
                .Miss;
        }

        var key = CreateListKey(
            generation,
            relatedRecordType,
            relatedRecordId);

        var cached =
            await GetAsync<StoredFileDto[]>(
                key,
                "list",
                cancellationToken);

        return cached.Found
            ? CacheLookup<
                IReadOnlyList<StoredFileDto>>
                .Hit(cached.Value!)
            : CacheLookup<
                IReadOnlyList<StoredFileDto>>
                .Miss;
    }

    public async Task SetListAsync(
        string? relatedRecordType,
        string? relatedRecordId,
        IReadOnlyList<StoredFileDto> files,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        var generation =
            await GetListGenerationAsync(
                cancellationToken);

        if (generation is null)
        {
            return;
        }

        var key = CreateListKey(
            generation,
            relatedRecordType,
            relatedRecordId);

        await SetAsync(
            key,
            files.ToArray(),
            TimeSpan.FromSeconds(
                _options.ListTtlSeconds),
            "list",
            cancellationToken);
    }

    public Task InvalidateListsAsync(
        CancellationToken cancellationToken = default)
    {
        var generation =
            Guid.NewGuid()
                .ToString("N");

        return SetStringAsync(
            CreateListGenerationKey(),
            generation,
            "list-generation",
            cancellationToken);
    }

    private async Task<CacheLookup<T>> GetAsync<T>(
        string key,
        string cacheArea,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var payload =
                await distributedCache.GetAsync(
                    key,
                    cancellationToken);

            if (
                payload is null ||
                payload.Length == 0
            )
            {
                logger.LogDebug(
                    "File metadata cache miss. Area: {CacheArea}",
                    cacheArea);

                return CacheLookup<T>.Miss;
            }

            var value =
                JsonSerializer.Deserialize<T>(
                    payload,
                    SerializerOptions);

            if (value is null)
            {
                logger.LogWarning(
                    "File metadata cache payload was empty after deserialization. Area: {CacheArea}",
                    cacheArea);

                return CacheLookup<T>.Miss;
            }

            logger.LogDebug(
                "File metadata cache hit. Area: {CacheArea}",
                cacheArea);

            return CacheLookup<T>.Hit(value);
        }
        catch (OperationCanceledException)
            when (
                cancellationToken
                    .IsCancellationRequested
            )
        {
            throw;
        }
        catch (Exception exception)
            when (IsRecoverable(exception))
        {
            logger.LogWarning(
                exception,
                "File metadata cache read failed; PostgreSQL fallback will be used. Area: {CacheArea}",
                cacheArea);

            if (exception is JsonException)
            {
                await RemoveAsync(
                    key,
                    cacheArea,
                    cancellationToken);
            }

            return CacheLookup<T>.Miss;
        }
    }

    private async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration,
        string cacheArea,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload =
                JsonSerializer.SerializeToUtf8Bytes(
                    value,
                    SerializerOptions);

            var entryOptions =
                new DistributedCacheEntryOptions();

            if (expiration is not null)
            {
                entryOptions
                    .AbsoluteExpirationRelativeToNow =
                        expiration;
            }

            await distributedCache.SetAsync(
                key,
                payload,
                entryOptions,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (
                cancellationToken
                    .IsCancellationRequested
            )
        {
            throw;
        }
        catch (Exception exception)
            when (IsRecoverable(exception))
        {
            logger.LogWarning(
                exception,
                "File metadata cache write failed; primary operation remains successful. Area: {CacheArea}",
                cacheArea);
        }
    }

    private async Task SetStringAsync(
        string key,
        string value,
        string cacheArea,
        CancellationToken cancellationToken)
    {
        try
        {
            await distributedCache.SetStringAsync(
                key,
                value,
                new DistributedCacheEntryOptions(),
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (
                cancellationToken
                    .IsCancellationRequested
            )
        {
            throw;
        }
        catch (Exception exception)
            when (IsRecoverable(exception))
        {
            logger.LogWarning(
                exception,
                "File metadata cache write failed; primary operation remains successful. Area: {CacheArea}",
                cacheArea);
        }
    }

    private async Task RemoveAsync(
        string key,
        string cacheArea,
        CancellationToken cancellationToken)
    {
        try
        {
            await distributedCache.RemoveAsync(
                key,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (
                cancellationToken
                    .IsCancellationRequested
            )
        {
            throw;
        }
        catch (Exception exception)
            when (IsRecoverable(exception))
        {
            logger.LogWarning(
                exception,
                "File metadata cache eviction failed; primary operation remains successful. Area: {CacheArea}",
                cacheArea);
        }
    }

    private async Task<string?>
        GetListGenerationAsync(
            CancellationToken cancellationToken)
    {
        try
        {
            var generation =
                await distributedCache
                    .GetStringAsync(
                        CreateListGenerationKey(),
                        cancellationToken);

            return string.IsNullOrWhiteSpace(
                generation)
                    ? InitialListGeneration
                    : generation;
        }
        catch (OperationCanceledException)
            when (
                cancellationToken
                    .IsCancellationRequested
            )
        {
            throw;
        }
        catch (Exception exception)
            when (IsRecoverable(exception))
        {
            logger.LogWarning(
                exception,
                "File metadata list generation read failed; PostgreSQL fallback will be used.");

            return null;
        }
    }

    private string CreateDetailKey(
        Guid id)
    {
        return
            $"{NormalizePrefix()}:detail:{id:N}";
    }

    private string CreateListGenerationKey()
    {
        return
            $"{NormalizePrefix()}:list-generation";
    }

    private string CreateListKey(
        string generation,
        string? relatedRecordType,
        string? relatedRecordId)
    {
        return
            $"{NormalizePrefix()}:list:{generation}:{CreateFilterHash(relatedRecordType, relatedRecordId)}";
    }

    private string NormalizePrefix()
    {
        return _options.KeyPrefix
            .Trim()
            .Trim(':');
    }

    private static string CreateFilterHash(
        string? relatedRecordType,
        string? relatedRecordId)
    {
        var normalizedType =
            NormalizeOptional(
                relatedRecordType);

        var normalizedId =
            NormalizeOptional(
                relatedRecordId);

        if (
            normalizedType is null &&
            normalizedId is null
        )
        {
            return "all";
        }

        var value =
            $"{normalizedType}\0{normalizedId}";

        var hash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value));

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static bool IsRecoverable(
        Exception exception)
    {
        return exception is
            RedisException or
            TimeoutException or
            JsonException;
    }
}
