using FileManagement.Application.Abstractions.Caching;
using FileManagement.Application.Files;
using FileManagement.Application.Files.Models;

namespace FileManagement.UnitTests.Application;

public sealed class CachedFileManagementServiceTests
{
    [Fact]
    public async Task ListAsync_WhenCacheHit_DoesNotCallInner()
    {
        var expected = new[]
        {
            CreateFile()
        };

        var inner = new FakeFileManagementService();
        var cache = new FakeFileMetadataCache
        {
            ListLookup =
                CacheLookup<
                    IReadOnlyList<StoredFileDto>>
                    .Hit(expected)
        };

        var service =
            new CachedFileManagementService(
                inner,
                cache);

        var result = await service.ListAsync();

        Assert.Same(
            expected,
            result);

        Assert.Equal(
            0,
            inner.ListCallCount);

        Assert.Equal(
            0,
            cache.SetListCallCount);
    }

    [Fact]
    public async Task ListAsync_WhenCacheMiss_CachesInnerResult()
    {
        var expected = new[]
        {
            CreateFile()
        };

        var inner =
            new FakeFileManagementService
            {
                ListResult = expected
            };

        var cache =
            new FakeFileMetadataCache();

        var service =
            new CachedFileManagementService(
                inner,
                cache);

        var result = await service.ListAsync(
            " Student ",
            " 42 ");

        Assert.Same(
            expected,
            result);

        Assert.Equal(
            1,
            inner.ListCallCount);

        Assert.Equal(
            1,
            cache.SetListCallCount);

        Assert.Same(
            expected,
            cache.LastListValue);
    }

    [Fact]
    public async Task ListAsync_WithIncompleteAssociation_BypassesCache()
    {
        var inner =
            new FakeFileManagementService();

        var cache =
            new FakeFileMetadataCache();

        var service =
            new CachedFileManagementService(
                inner,
                cache);

        await service.ListAsync(
            "Student",
            null);

        Assert.Equal(
            1,
            inner.ListCallCount);

        Assert.Equal(
            0,
            cache.GetListCallCount);

        Assert.Equal(
            0,
            cache.SetListCallCount);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheHit_DoesNotCallInner()
    {
        var expected = CreateFile();

        var inner =
            new FakeFileManagementService();

        var cache =
            new FakeFileMetadataCache
            {
                FileLookup =
                    CacheLookup<StoredFileDto>
                        .Hit(expected)
            };

        var service =
            new CachedFileManagementService(
                inner,
                cache);

        var result = await service.GetByIdAsync(
            expected.Id);

        Assert.Same(
            expected,
            result);

        Assert.Equal(
            0,
            inner.GetByIdCallCount);

        Assert.Equal(
            0,
            cache.SetFileCallCount);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheMiss_CachesInnerResult()
    {
        var expected = CreateFile();

        var inner =
            new FakeFileManagementService
            {
                GetByIdResult = expected
            };

        var cache =
            new FakeFileMetadataCache();

        var service =
            new CachedFileManagementService(
                inner,
                cache);

        var result = await service.GetByIdAsync(
            expected.Id);

        Assert.Same(
            expected,
            result);

        Assert.Equal(
            1,
            inner.GetByIdCallCount);

        Assert.Equal(
            1,
            cache.SetFileCallCount);

        Assert.Same(
            expected,
            cache.LastFileValue);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFileDoesNotExist_DoesNotNegativeCache()
    {
        var inner =
            new FakeFileManagementService
            {
                GetByIdResult = null
            };

        var cache =
            new FakeFileMetadataCache();

        var service =
            new CachedFileManagementService(
                inner,
                cache);

        var result = await service.GetByIdAsync(
            Guid.NewGuid());

        Assert.Null(result);

        Assert.Equal(
            1,
            inner.GetByIdCallCount);

        Assert.Equal(
            0,
            cache.SetFileCallCount);
    }

    [Fact]
    public async Task UploadAsync_WhenSuccessful_WarmsDetailAndInvalidatesLists()
    {
        var expected = CreateFile();

        var inner =
            new FakeFileManagementService
            {
                UploadResult = expected
            };

        var cache =
            new FakeFileMetadataCache();

        var service =
            new CachedFileManagementService(
                inner,
                cache);

        await using var content =
            new MemoryStream([1, 2, 3]);

        var result = await service.UploadAsync(
            "report.pdf",
            "application/pdf",
            content.Length,
            content,
            "Student",
            "42");

        Assert.Same(
            expected,
            result);

        Assert.Equal(
            1,
            cache.SetFileCallCount);

        Assert.Equal(
            1,
            cache.InvalidateListsCallCount);
    }

    [Fact]
    public async Task DeleteAsync_WhenSuccessful_EvictsDetailAndInvalidatesLists()
    {
        var file = CreateFile();

        var inner =
            new FakeFileManagementService
            {
                DeleteResult = true
            };

        var cache =
            new FakeFileMetadataCache();

        var service =
            new CachedFileManagementService(
                inner,
                cache);

        var deleted = await service.DeleteAsync(
            file.Id);

        Assert.True(deleted);

        Assert.Equal(
            1,
            cache.RemoveFileCallCount);

        Assert.Equal(
            1,
            cache.InvalidateListsCallCount);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_EvictsOnlyPossiblyStaleDetail()
    {
        var inner =
            new FakeFileManagementService
            {
                DeleteResult = false
            };

        var cache =
            new FakeFileMetadataCache();

        var service =
            new CachedFileManagementService(
                inner,
                cache);

        var deleted = await service.DeleteAsync(
            Guid.NewGuid());

        Assert.False(deleted);

        Assert.Equal(
            1,
            cache.RemoveFileCallCount);

        Assert.Equal(
            0,
            cache.InvalidateListsCallCount);
    }

    [Fact]
    public async Task StreamAndPresignedOperations_BypassMetadataCache()
    {
        var file = CreateFile();

        var inner =
            new FakeFileManagementService
            {
                StreamResult = file,
                PresignedResult =
                    new FileAccessUrlDto(
                        "https://example.test/file",
                        DateTimeOffset.UtcNow
                            .AddMinutes(5))
            };

        var cache =
            new FakeFileMetadataCache();

        var service =
            new CachedFileManagementService(
                inner,
                cache);

        await using var download =
            new MemoryStream();

        await using var preview =
            new MemoryStream();

        await service.DownloadAsync(
            file.Id,
            download);

        await service.PreviewAsync(
            file.Id,
            preview);

        await service.CreatePresignedGetUrlAsync(
            file.Id,
            TimeSpan.FromMinutes(5));

        Assert.Equal(
            0,
            cache.TotalCallCount);
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

    private sealed class FakeFileMetadataCache :
        IFileMetadataCache
    {
        public CacheLookup<StoredFileDto>
            FileLookup
        { get; set; } =
                CacheLookup<StoredFileDto>.Miss;

        public CacheLookup<
            IReadOnlyList<StoredFileDto>>
            ListLookup
        { get; set; } =
                CacheLookup<
                    IReadOnlyList<StoredFileDto>>
                    .Miss;

        public int GetFileCallCount { get; private set; }

        public int SetFileCallCount { get; private set; }

        public int RemoveFileCallCount { get; private set; }

        public int GetListCallCount { get; private set; }

        public int SetListCallCount { get; private set; }

        public int InvalidateListsCallCount
        {
            get;
            private set;
        }

        public int TotalCallCount =>
            GetFileCallCount +
            SetFileCallCount +
            RemoveFileCallCount +
            GetListCallCount +
            SetListCallCount +
            InvalidateListsCallCount;

        public StoredFileDto? LastFileValue
        {
            get;
            private set;
        }

        public IReadOnlyList<StoredFileDto>?
            LastListValue
        { get; private set; }

        public Task<CacheLookup<StoredFileDto>>
            GetFileAsync(
                Guid id,
                CancellationToken cancellationToken = default)
        {
            GetFileCallCount++;

            return Task.FromResult(
                FileLookup);
        }

        public Task SetFileAsync(
            StoredFileDto file,
            CancellationToken cancellationToken = default)
        {
            SetFileCallCount++;
            LastFileValue = file;

            return Task.CompletedTask;
        }

        public Task RemoveFileAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            RemoveFileCallCount++;

            return Task.CompletedTask;
        }

        public Task<
            CacheLookup<
                IReadOnlyList<StoredFileDto>>>
            GetListAsync(
                string? relatedRecordType,
                string? relatedRecordId,
                CancellationToken cancellationToken = default)
        {
            GetListCallCount++;

            return Task.FromResult(
                ListLookup);
        }

        public Task SetListAsync(
            string? relatedRecordType,
            string? relatedRecordId,
            IReadOnlyList<StoredFileDto> files,
            CancellationToken cancellationToken = default)
        {
            SetListCallCount++;
            LastListValue = files;

            return Task.CompletedTask;
        }

        public Task InvalidateListsAsync(
            CancellationToken cancellationToken = default)
        {
            InvalidateListsCallCount++;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileManagementService :
        IFileManagementService
    {
        public StoredFileDto UploadResult { get; set; } =
            CreateFile();

        public IReadOnlyList<StoredFileDto>
            ListResult
        { get; set; } =
                Array.Empty<StoredFileDto>();

        public StoredFileDto? GetByIdResult
        {
            get;
            set;
        }

        public StoredFileDto? StreamResult
        {
            get;
            set;
        }

        public FileAccessUrlDto? PresignedResult
        {
            get;
            set;
        }

        public bool DeleteResult { get; set; }

        public int ListCallCount { get; private set; }

        public int GetByIdCallCount { get; private set; }

        public Task<StoredFileDto> UploadAsync(
            string originalFileName,
            string contentType,
            long sizeBytes,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                UploadResult);
        }

        public Task<StoredFileDto> UploadAsync(
            string originalFileName,
            string contentType,
            long sizeBytes,
            Stream content,
            string? relatedRecordType,
            string? relatedRecordId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                UploadResult);
        }

        public Task<IReadOnlyList<StoredFileDto>>
            ListAsync(
                CancellationToken cancellationToken = default)
        {
            ListCallCount++;

            return Task.FromResult(
                ListResult);
        }

        public Task<IReadOnlyList<StoredFileDto>>
            ListAsync(
                string? relatedRecordType,
                string? relatedRecordId,
                CancellationToken cancellationToken = default)
        {
            ListCallCount++;

            return Task.FromResult(
                ListResult);
        }

        public Task<StoredFileDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            return Task.FromResult(
                GetByIdResult);
        }

        public Task<StoredFileDto?> DownloadAsync(
            Guid id,
            Stream destination,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                StreamResult);
        }

        public Task<StoredFileDto?> PreviewAsync(
            Guid id,
            Stream destination,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                StreamResult);
        }

        public Task<FileAccessUrlDto?>
            CreatePresignedGetUrlAsync(
                Guid id,
                TimeSpan expiresIn,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                PresignedResult);
        }

        public Task<bool> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                DeleteResult);
        }
    }
}
