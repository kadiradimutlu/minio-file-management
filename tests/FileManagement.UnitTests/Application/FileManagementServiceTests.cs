using FileManagement.Application.Abstractions.Execution;
using FileManagement.Application.Abstractions.Persistence;
using FileManagement.Application.Abstractions.Storage;
using FileManagement.Application.Files;
using FileManagement.Contracts.Files;
using FileManagement.Domain.Entities;

namespace FileManagement.UnitTests.Application;

public sealed class FileManagementServiceTests
{
    [Fact]
    public async Task UploadAsync_WithValidFile_SavesMetadata()
    {
        var repository = new FakeStoredFileRepository();
        var storage = new FakeFileStorageService();
        var service = CreateService(
            repository,
            storage);

        var bytes = "test-content"u8.ToArray();

        using var stream = new MemoryStream(
            bytes,
            writable: false);

        var result = await service.UploadAsync(
            @"C:\fakepath\report.PDF",
            "application/pdf",
            bytes.LongLength,
            stream);

        Assert.Equal("report.PDF", result.OriginalFileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(bytes.LongLength, result.SizeBytes);
        Assert.Null(result.RelatedRecordType);
        Assert.Null(result.RelatedRecordId);

        var storedFile = Assert.Single(
            repository.StoredFiles);

        Assert.Equal("files", storedFile.BucketName);
        Assert.EndsWith(
            ".pdf",
            storedFile.ObjectName);

        Assert.True(
            storage.Objects.ContainsKey(
                storedFile.ObjectName));
    }

    [Fact]
    public async Task UploadAsync_WithRelatedRecord_SavesAssociation()
    {
        var repository = new FakeStoredFileRepository();
        var storage = new FakeFileStorageService();
        var service = CreateService(
            repository,
            storage);

        var bytes = "associated-content"u8.ToArray();

        using var stream = new MemoryStream(
            bytes,
            writable: false);

        var result = await service.UploadAsync(
            "student-document.pdf",
            "application/pdf",
            bytes.LongLength,
            stream,
            " Student ",
            " 42 ");

        Assert.Equal(
            "Student",
            result.RelatedRecordType);

        Assert.Equal(
            "42",
            result.RelatedRecordId);

        var storedFile = Assert.Single(
            repository.StoredFiles);

        Assert.Equal(
            "Student",
            storedFile.RelatedRecordType);

        Assert.Equal(
            "42",
            storedFile.RelatedRecordId);
    }

    [Fact]
    public async Task UploadAsync_WithIncompleteAssociation_DoesNotUploadObject()
    {
        var repository = new FakeStoredFileRepository();
        var storage = new FakeFileStorageService();
        var service = CreateService(
            repository,
            storage);

        var bytes = "invalid-association"u8.ToArray();

        using var stream = new MemoryStream(
            bytes,
            writable: false);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UploadAsync(
                "student-document.pdf",
                "application/pdf",
                bytes.LongLength,
                stream,
                "Student",
                null));

        Assert.Empty(repository.StoredFiles);
        Assert.Empty(storage.Objects);
        Assert.Empty(storage.DeletedObjectNames);
    }

    [Fact]
    public async Task UploadAsync_WithValidFile_EnqueuesUploadedOperation()
    {
        var repository =
            new FakeStoredFileRepository();

        var storage =
            new FakeFileStorageService();

        var outbox =
            new FakeFileOperationOutbox();

        var service = CreateService(
            repository,
            storage,
            outbox);

        var bytes =
            "outbox-upload"u8.ToArray();

        using var stream = new MemoryStream(
            bytes,
            writable: false);

        var result = await service.UploadAsync(
            "report.pdf",
            "application/pdf",
            bytes.LongLength,
            stream);

        var operation = Assert.Single(
            outbox.Operations);

        Assert.Equal(
            result.Id,
            operation.StoredFile.Id);

        Assert.Equal(
            FileOperationKinds.Uploaded,
            operation.Operation);

        Assert.Equal(
            "user-42",
            operation.ActorUserId);

        Assert.Equal(
            "correlation-123",
            operation.CorrelationId);

        Assert.Equal(
            FakeTimeProvider.FixedUtcNow,
            operation.OccurredAtUtc);

        Assert.Equal(
            1,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task UploadAsync_WhenOutboxEnqueueFails_DeletesObject()
    {
        var repository =
            new FakeStoredFileRepository();

        var storage =
            new FakeFileStorageService();

        var outbox =
            new FakeFileOperationOutbox
            {
                ThrowOnEnqueue = true
            };

        var service = CreateService(
            repository,
            storage,
            outbox);

        var bytes =
            "outbox-failure"u8.ToArray();

        using var stream = new MemoryStream(
            bytes,
            writable: false);

        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () => service.UploadAsync(
                    "report.pdf",
                    "application/pdf",
                    bytes.LongLength,
                    stream));

        Assert.Empty(
            storage.Objects);

        Assert.Single(
            storage.DeletedObjectNames);

        Assert.Empty(
            outbox.Operations);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }
    [Fact]
    public async Task ListAsync_WithRelatedRecord_ReturnsMatchingFiles()
    {
        var repository = new FakeStoredFileRepository();
        var service = CreateService(
            repository,
            new FakeFileStorageService());

        repository.StoredFiles.AddRange(
        [
            new StoredFile(
                "student-42.pdf",
                "student-42.pdf",
                "files",
                "application/pdf",
                100,
                "Student",
                "42"),
            new StoredFile(
                "student-43.pdf",
                "student-43.pdf",
                "files",
                "application/pdf",
                100,
                "Student",
                "43"),
            new StoredFile(
                "unrelated.pdf",
                "unrelated.pdf",
                "files",
                "application/pdf",
                100)
        ]);

        var results = await service.ListAsync(
            " Student ",
            " 42 ");

        var result = Assert.Single(results);

        Assert.Equal(
            "student-42.pdf",
            result.OriginalFileName);

        Assert.Equal(
            "Student",
            result.RelatedRecordType);

        Assert.Equal(
            "42",
            result.RelatedRecordId);
    }

    [Fact]
    public async Task UploadAsync_WhenDatabaseSaveFails_DeletesObject()
    {
        var repository = new FakeStoredFileRepository
        {
            ThrowOnSave = true
        };

        var storage = new FakeFileStorageService();
        var service = CreateService(
            repository,
            storage);

        var bytes = "rollback-test"u8.ToArray();

        using var stream = new MemoryStream(
            bytes,
            writable: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadAsync(
                "report.pdf",
                "application/pdf",
                bytes.LongLength,
                stream));

        Assert.Empty(storage.Objects);
        Assert.Single(storage.DeletedObjectNames);
    }

    [Fact]
    public async Task DownloadAsync_WithExistingFile_EnqueuesDownloadedOperation()
    {
        var repository =
            new FakeStoredFileRepository();

        var storage =
            new FakeFileStorageService();

        var outbox =
            new FakeFileOperationOutbox();

        var service = CreateService(
            repository,
            storage,
            outbox);

        var bytes =
            "download-content"u8.ToArray();

        var storedFile =
            AddStoredFile(
                repository,
                storage,
                bytes);

        using var destination =
            new MemoryStream();

        var result =
            await service.DownloadAsync(
                storedFile.Id,
                destination);

        Assert.NotNull(
            result);

        Assert.Equal(
            storedFile.Id,
            result.Id);

        Assert.Equal(
            bytes,
            destination.ToArray());

        Assert.Equal(
            1,
            storage.DownloadCallCount);

        var operation = Assert.Single(
            outbox.Operations);

        Assert.Equal(
            storedFile.Id,
            operation.StoredFile.Id);

        Assert.Equal(
            FileOperationKinds.Downloaded,
            operation.Operation);

        Assert.Equal(
            "user-42",
            operation.ActorUserId);

        Assert.Equal(
            "correlation-123",
            operation.CorrelationId);

        Assert.Equal(
            FakeTimeProvider.FixedUtcNow,
            operation.OccurredAtUtc);

        Assert.Equal(
            1,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task DownloadAsync_WhenFileDoesNotExist_HasNoSideEffects()
    {
        var repository =
            new FakeStoredFileRepository();

        var storage =
            new FakeFileStorageService();

        var outbox =
            new FakeFileOperationOutbox();

        var service = CreateService(
            repository,
            storage,
            outbox);

        using var destination =
            new MemoryStream();

        var result =
            await service.DownloadAsync(
                Guid.NewGuid(),
                destination);

        Assert.Null(
            result);

        Assert.Equal(
            0,
            destination.Length);

        Assert.Equal(
            0,
            storage.DownloadCallCount);

        Assert.Empty(
            outbox.Operations);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task DownloadAsync_WhenStorageFails_DoesNotEnqueueOperation()
    {
        var repository =
            new FakeStoredFileRepository();

        var storage =
            new FakeFileStorageService
            {
                ThrowOnDownload = true
            };

        var outbox =
            new FakeFileOperationOutbox();

        var service = CreateService(
            repository,
            storage,
            outbox);

        var storedFile =
            AddStoredFile(
                repository,
                storage,
                "storage-failure"u8.ToArray());

        using var destination =
            new MemoryStream();

        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () =>
                    service.DownloadAsync(
                        storedFile.Id,
                        destination));

        Assert.Equal(
            1,
            storage.DownloadCallCount);

        Assert.Empty(
            outbox.Operations);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task PreviewAsync_WithExistingFile_DoesNotEnqueueDownloadedOperation()
    {
        var repository =
            new FakeStoredFileRepository();

        var storage =
            new FakeFileStorageService();

        var outbox =
            new FakeFileOperationOutbox();

        var service = CreateService(
            repository,
            storage,
            outbox);

        var bytes =
            "preview-content"u8.ToArray();

        var storedFile =
            AddStoredFile(
                repository,
                storage,
                bytes);

        using var destination =
            new MemoryStream();

        var result =
            await service.PreviewAsync(
                storedFile.Id,
                destination);

        Assert.NotNull(
            result);

        Assert.Equal(
            storedFile.Id,
            result.Id);

        Assert.Equal(
            bytes,
            destination.ToArray());

        Assert.Equal(
            1,
            storage.DownloadCallCount);

        Assert.Empty(
            outbox.Operations);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }
    [Fact]
    public async Task DeleteAsync_WhenFileDoesNotExist_ReturnsFalse()
    {
        var service = CreateService(
            new FakeStoredFileRepository(),
            new FakeFileStorageService());

        var deleted = await service.DeleteAsync(
            Guid.NewGuid());

        Assert.False(deleted);
    }

    private static StoredFile AddStoredFile(
        FakeStoredFileRepository repository,
        FakeFileStorageService storage,
        byte[] content)
    {
        var storedFile =
            new StoredFile(
                "report.pdf",
                "2026/07/report.pdf",
                "files",
                "application/pdf",
                content.LongLength);

        repository.StoredFiles.Add(
            storedFile);

        storage.Objects[storedFile.ObjectName] =
            content.ToArray();

        return storedFile;
    }
    private static FileManagementService CreateService(
        FakeStoredFileRepository repository,
        FakeFileStorageService storage,
        FakeFileOperationOutbox? outbox = null)
    {
        return new FileManagementService(
            repository,
            storage,
            outbox ??
                new FakeFileOperationOutbox(),
            new FakeFileOperationContext(),
            new FakeTimeProvider());
    }

    private sealed record EnqueuedFileOperation(
        StoredFile StoredFile,
        string Operation,
        string ActorUserId,
        string CorrelationId,
        DateTimeOffset OccurredAtUtc);

    private sealed class FakeFileOperationOutbox :
        IFileOperationOutbox
    {
        public List<EnqueuedFileOperation>
            Operations { get; } = [];

        public bool ThrowOnEnqueue { get; init; }

        public Task EnqueueAsync(
            StoredFile storedFile,
            string operation,
            string actorUserId,
            string correlationId,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken = default)
        {
            if (ThrowOnEnqueue)
            {
                throw new InvalidOperationException(
                    "Simulated outbox failure.");
            }

            Operations.Add(
                new EnqueuedFileOperation(
                    storedFile,
                    operation,
                    actorUserId,
                    correlationId,
                    occurredAtUtc));

            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileOperationContext :
        IFileOperationContext
    {
        public string ActorUserId =>
            "user-42";

        public string CorrelationId =>
            "correlation-123";
    }

    private sealed class FakeTimeProvider :
        TimeProvider
    {
        public static DateTimeOffset FixedUtcNow { get; } =
            new(
                2026,
                7,
                30,
                11,
                15,
                0,
                TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return FixedUtcNow;
        }
    }
    private sealed class FakeStoredFileRepository :
        IStoredFileRepository
    {
        public List<StoredFile> StoredFiles { get; } = [];

        public bool ThrowOnSave { get; init; }

        public int SaveChangesCallCount
        {
            get;
            private set;
        }

        public Task AddAsync(
            StoredFile storedFile,
            CancellationToken cancellationToken = default)
        {
            StoredFiles.Add(storedFile);
            return Task.CompletedTask;
        }

        public Task<StoredFile?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                StoredFiles.SingleOrDefault(
                    storedFile => storedFile.Id == id));
        }

        public Task<IReadOnlyList<StoredFile>> ListAsync(
            string? relatedRecordType = null,
            string? relatedRecordId = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<StoredFile> query =
                StoredFiles;

            if (
                relatedRecordType is not null &&
                relatedRecordId is not null
            )
            {
                query = query.Where(
                    storedFile =>
                        storedFile.RelatedRecordType ==
                            relatedRecordType &&
                        storedFile.RelatedRecordId ==
                            relatedRecordId);
            }

            IReadOnlyList<StoredFile> result =
                query
                    .OrderByDescending(
                        storedFile =>
                            storedFile.CreatedAtUtc)
                    .ToArray();

            return Task.FromResult(result);
        }

        public void Remove(StoredFile storedFile)
        {
            StoredFiles.Remove(storedFile);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            if (ThrowOnSave)
            {
                throw new InvalidOperationException(
                    "Simulated database failure.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileStorageService :
        IFileStorageService
    {
        public string BucketName => "files";

        public Dictionary<string, byte[]> Objects { get; } = [];

        public List<string> DeletedObjectNames { get; } = [];

        public bool ThrowOnDownload { get; init; }

        public int DownloadCallCount
        {
            get;
            private set;
        }

        public Task EnsureBucketExistsAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task UploadAsync(
            string objectName,
            Stream content,
            long sizeBytes,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();

            await content.CopyToAsync(
                memoryStream,
                cancellationToken);

            Objects[objectName] =
                memoryStream.ToArray();
        }

        public async Task DownloadAsync(
            string objectName,
            Stream destination,
            CancellationToken cancellationToken = default)
        {
            DownloadCallCount++;

            if (ThrowOnDownload)
            {
                throw new InvalidOperationException(
                    "Simulated storage download failure.");
            }

            await destination.WriteAsync(
                Objects[objectName],
                cancellationToken);
        }

        public Task<bool> ExistsAsync(
            string objectName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Objects.ContainsKey(objectName));
        }

        public Task DeleteAsync(
            string objectName,
            CancellationToken cancellationToken = default)
        {
            DeletedObjectNames.Add(objectName);
            Objects.Remove(objectName);

            return Task.CompletedTask;
        }

        public Task<string> CreatePresignedGetUrlAsync(
            string objectName,
            TimeSpan expiresIn,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                $"http://localhost/{objectName}");
        }
    }
}
