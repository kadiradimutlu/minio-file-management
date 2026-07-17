namespace FileManagement.Application.Abstractions.Storage;

public interface IFileStorageService
{
    Task EnsureBucketExistsAsync(
        CancellationToken cancellationToken = default);

    Task UploadAsync(
        string objectName,
        Stream content,
        long sizeBytes,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DownloadAsync(
        string objectName,
        Stream destination,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string objectName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectName,
        CancellationToken cancellationToken = default);

    Task<string> CreatePresignedGetUrlAsync(
        string objectName,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);
}