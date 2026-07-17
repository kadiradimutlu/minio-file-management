using FileManagement.Application.Abstractions.Persistence;
using FileManagement.Application.Abstractions.Storage;
using FileManagement.Application.Files;
using FileManagement.Domain.Entities;

namespace FileManagement.UnitTests.Application;

public sealed class FileManagementServiceTests
{
    [Fact]
    public async Task UploadAsync_WithValidFile_SavesMetadata()
    {
        var repository = new FakeStoredFileRepository();
        var storage = new FakeFileStorageService();
        var service = new FileManagementService(
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
    public async Task UploadAsync_WhenDatabaseSaveFails_DeletesObject()
    {
        var repository = new FakeStoredFileRepository
        {
            ThrowOnSave = true
        };

        var storage = new FakeFileStorageService();
        var service = new FileManagementService(
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
    public async Task DeleteAsync_WhenFileDoesNotExist_ReturnsFalse()
    {
        var service = new FileManagementService(
            new FakeStoredFileRepository(),
            new FakeFileStorageService());

        var deleted = await service.DeleteAsync(
            Guid.NewGuid());

        Assert.False(deleted);
    }

    private sealed class FakeStoredFileRepository :
        IStoredFileRepository
    {
        public List<StoredFile> StoredFiles { get; } = [];

        public bool ThrowOnSave { get; init; }

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
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<StoredFile> result =
                StoredFiles.ToArray();

            return Task.FromResult(result);
        }

        public void Remove(StoredFile storedFile)
        {
            StoredFiles.Remove(storedFile);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
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