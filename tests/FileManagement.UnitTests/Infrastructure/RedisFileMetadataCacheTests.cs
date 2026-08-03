using FileManagement.Application.Files.Models;
using FileManagement.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FileManagement.UnitTests.Infrastructure;

public sealed class RedisFileMetadataCacheTests
{
    [Fact]
    public async Task FileRoundTrip_PreservesMetadata()
    {
        var distributedCache =
            new FakeDistributedCache();

        var cache = CreateCache(
            distributedCache);

        var expected = CreateFile();

        await cache.SetFileAsync(expected);

        var result = await cache.GetFileAsync(
            expected.Id);

        Assert.True(result.Found);

        Assert.Equal(
            expected,
            result.Value);
    }

    [Fact]
    public async Task EmptyListRoundTrip_IsAHit()
    {
        var distributedCache =
            new FakeDistributedCache();

        var cache = CreateCache(
            distributedCache);

        await cache.SetListAsync(
            null,
            null,
            Array.Empty<StoredFileDto>());

        var result = await cache.GetListAsync(
            null,
            null);

        Assert.True(result.Found);

        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task InvalidateListsAsync_MakesPreviousGenerationUnreachable()
    {
        var distributedCache =
            new FakeDistributedCache();

        var cache = CreateCache(
            distributedCache);

        await cache.SetListAsync(
            null,
            null,
            [CreateFile()]);

        var before =
            await cache.GetListAsync(
                null,
                null);

        await cache.InvalidateListsAsync();

        var after =
            await cache.GetListAsync(
                null,
                null);

        Assert.True(before.Found);
        Assert.False(after.Found);
    }

    [Fact]
    public async Task InvalidateListsAsync_StoresPlainGuidGeneration()
    {
        var distributedCache =
            new FakeDistributedCache();

        var cache = CreateCache(
            distributedCache);

        await cache.InvalidateListsAsync();

        var rawGeneration =
            distributedCache.GetRaw(
                "file-management:test:files:v1:list-generation");

        Assert.NotNull(rawGeneration);

        var generation =
            System.Text.Encoding.UTF8.GetString(
                rawGeneration);

        Assert.True(
            Guid.TryParseExact(
                generation,
                "N",
                out _));
    }

    [Fact]
    public async Task FilterKey_TrimsValuesButPreservesCase()
    {
        var distributedCache =
            new FakeDistributedCache();

        var cache = CreateCache(
            distributedCache);

        await cache.SetListAsync(
            " Student ",
            " 42 ",
            [CreateFile()]);

        var normalized =
            await cache.GetListAsync(
                "Student",
                "42");

        var differentCase =
            await cache.GetListAsync(
                "student",
                "42");

        Assert.True(normalized.Found);
        Assert.False(differentCase.Found);
    }

    [Fact]
    public async Task ReadTimeout_ReturnsCacheMiss()
    {
        var distributedCache =
            new FakeDistributedCache
            {
                GetException =
                    new TimeoutException(
                        "Redis timeout.")
            };

        var cache = CreateCache(
            distributedCache);

        var result = await cache.GetFileAsync(
            Guid.NewGuid());

        Assert.False(result.Found);
    }

    [Fact]
    public async Task WriteTimeout_DoesNotFailPrimaryOperation()
    {
        var distributedCache =
            new FakeDistributedCache
            {
                SetException =
                    new TimeoutException(
                        "Redis timeout.")
            };

        var cache = CreateCache(
            distributedCache);

        await cache.SetFileAsync(
            CreateFile());

        await cache.InvalidateListsAsync();
    }

    [Fact]
    public async Task RemoveTimeout_DoesNotFailPrimaryOperation()
    {
        var distributedCache =
            new FakeDistributedCache
            {
                RemoveException =
                    new TimeoutException(
                        "Redis timeout.")
            };

        var cache = CreateCache(
            distributedCache);

        await cache.RemoveFileAsync(
            Guid.NewGuid());
    }

    [Fact]
    public async Task CancelledRead_PropagatesCancellation()
    {
        var distributedCache =
            new FakeDistributedCache();

        var cache = CreateCache(
            distributedCache);

        using var cancellation =
            new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<
            OperationCanceledException>(
                () => cache.GetFileAsync(
                    Guid.NewGuid(),
                    cancellation.Token));
    }

    private static RedisFileMetadataCache
        CreateCache(
            IDistributedCache distributedCache)
    {
        var options =
            Options.Create(
                new FileMetadataCacheOptions
                {
                    Enabled = true,
                    KeyPrefix =
                        "file-management:test:files:v1",
                    DetailTtlSeconds = 300,
                    ListTtlSeconds = 30
                });

        return new RedisFileMetadataCache(
            distributedCache,
            options,
            NullLogger<
                RedisFileMetadataCache>.Instance);
    }

    private static StoredFileDto CreateFile()
    {
        return new StoredFileDto(
            Guid.NewGuid(),
            "report.pdf",
            "application/pdf",
            128,
            DateTimeOffset.UtcNow,
            "Student",
            "42");
    }

    private sealed class FakeDistributedCache :
        IDistributedCache
    {
        private readonly Dictionary<
            string,
            byte[]> _entries = [];

        public Exception? GetException
        {
            get;
            init;
        }

        public Exception? SetException
        {
            get;
            init;
        }

        public Exception? RemoveException
        {
            get;
            init;
        }

        public byte[]? Get(
            string key)
        {
            ThrowIfConfigured(
                GetException);

            return _entries.GetValueOrDefault(
                key);
        }

        public byte[]? GetRaw(
            string key)
        {
            return _entries.GetValueOrDefault(
                key);
        }

        public Task<byte[]?> GetAsync(
            string key,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            ThrowIfConfigured(
                GetException);

            return Task.FromResult(
                _entries.GetValueOrDefault(
                    key));
        }

        public void Refresh(
            string key)
        {
        }

        public Task RefreshAsync(
            string key,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }

        public void Remove(
            string key)
        {
            ThrowIfConfigured(
                RemoveException);

            _entries.Remove(key);
        }

        public Task RemoveAsync(
            string key,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            Remove(key);

            return Task.CompletedTask;
        }

        public void Set(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options)
        {
            ThrowIfConfigured(
                SetException);

            _entries[key] = value;
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            Set(
                key,
                value,
                options);

            return Task.CompletedTask;
        }

        private static void ThrowIfConfigured(
            Exception? exception)
        {
            if (exception is not null)
            {
                throw exception;
            }
        }
    }
}
