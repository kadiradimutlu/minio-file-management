using FileManagement.Application.Files.Models;

namespace FileManagement.Application.Files;

public interface IFileManagementService
{
    Task<StoredFileDto> UploadAsync(
        string originalFileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<StoredFileDto> UploadAsync(
        string originalFileName,
        string contentType,
        long sizeBytes,
        Stream content,
        string? relatedRecordType,
        string? relatedRecordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredFileDto>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredFileDto>> ListAsync(
        string? relatedRecordType,
        string? relatedRecordId,
        CancellationToken cancellationToken = default);

    Task<StoredFileDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StoredFileDto?> DownloadAsync(
        Guid id,
        Stream destination,
        CancellationToken cancellationToken = default);

    Task<FileAccessUrlDto?> CreatePresignedGetUrlAsync(
        Guid id,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
