using FileManagement.Application.Abstractions.Caching;
using FileManagement.Application.Files.Models;

namespace FileManagement.Infrastructure.Caching;

public sealed class NullFileMetadataCache :
    IFileMetadataCache
{
    public Task<CacheLookup<StoredFileDto>>
        GetFileAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        return Task.FromResult(
            CacheLookup<StoredFileDto>.Miss);
    }

    public Task SetFileAsync(
        StoredFileDto file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        cancellationToken
            .ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public Task RemoveFileAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public Task<
        CacheLookup<IReadOnlyList<StoredFileDto>>>
        GetListAsync(
            string? relatedRecordType,
            string? relatedRecordId,
            CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        return Task.FromResult(
            CacheLookup<
                IReadOnlyList<StoredFileDto>>.Miss);
    }

    public Task SetListAsync(
        string? relatedRecordType,
        string? relatedRecordId,
        IReadOnlyList<StoredFileDto> files,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        cancellationToken
            .ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public Task InvalidateListsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }
}
