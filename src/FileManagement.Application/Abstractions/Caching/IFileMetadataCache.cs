using FileManagement.Application.Files.Models;

namespace FileManagement.Application.Abstractions.Caching;

public interface IFileMetadataCache
{
    Task<CacheLookup<StoredFileDto>> GetFileAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task SetFileAsync(
        StoredFileDto file,
        CancellationToken cancellationToken = default);

    Task RemoveFileAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CacheLookup<IReadOnlyList<StoredFileDto>>>
        GetListAsync(
            string? relatedRecordType,
            string? relatedRecordId,
            CancellationToken cancellationToken = default);

    Task SetListAsync(
        string? relatedRecordType,
        string? relatedRecordId,
        IReadOnlyList<StoredFileDto> files,
        CancellationToken cancellationToken = default);

    Task InvalidateListsAsync(
        CancellationToken cancellationToken = default);
}
