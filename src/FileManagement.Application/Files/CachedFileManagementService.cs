using FileManagement.Application.Abstractions.Caching;
using FileManagement.Application.Files.Models;
using FileManagement.Domain.Entities;

namespace FileManagement.Application.Files;

public sealed class CachedFileManagementService(
    IFileManagementService inner,
    IFileMetadataCache cache)
    : IFileManagementService
{
    public async Task<StoredFileDto> UploadAsync(
        string originalFileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var file = await inner.UploadAsync(
            originalFileName,
            contentType,
            sizeBytes,
            content,
            cancellationToken);

        await RefreshAfterUploadAsync(
            file,
            CancellationToken.None);

        return file;
    }

    public async Task<StoredFileDto> UploadAsync(
        string originalFileName,
        string contentType,
        long sizeBytes,
        Stream content,
        string? relatedRecordType,
        string? relatedRecordId,
        CancellationToken cancellationToken = default)
    {
        var file = await inner.UploadAsync(
            originalFileName,
            contentType,
            sizeBytes,
            content,
            relatedRecordType,
            relatedRecordId,
            cancellationToken);

        await RefreshAfterUploadAsync(
            file,
            CancellationToken.None);

        return file;
    }

    public Task<IReadOnlyList<StoredFileDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return ListAsync(
            null,
            null,
            cancellationToken);
    }

    public async Task<IReadOnlyList<StoredFileDto>> ListAsync(
        string? relatedRecordType,
        string? relatedRecordId,
        CancellationToken cancellationToken = default)
    {
        if (
            !CanUseListCache(
                relatedRecordType,
                relatedRecordId)
        )
        {
            return await inner.ListAsync(
                relatedRecordType,
                relatedRecordId,
                cancellationToken);
        }

        var cached = await cache.GetListAsync(
            relatedRecordType,
            relatedRecordId,
            cancellationToken);

        if (cached.Found)
        {
            return cached.Value!;
        }

        var files = await inner.ListAsync(
            relatedRecordType,
            relatedRecordId,
            cancellationToken);

        await cache.SetListAsync(
            relatedRecordType,
            relatedRecordId,
            files,
            cancellationToken);

        return files;
    }

    public async Task<StoredFileDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetFileAsync(
            id,
            cancellationToken);

        if (cached.Found)
        {
            return cached.Value;
        }

        var file = await inner.GetByIdAsync(
            id,
            cancellationToken);

        if (file is not null)
        {
            await cache.SetFileAsync(
                file,
                cancellationToken);
        }

        return file;
    }

    public Task<StoredFileDto?> DownloadAsync(
        Guid id,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        return inner.DownloadAsync(
            id,
            destination,
            cancellationToken);
    }

    public Task<StoredFileDto?> PreviewAsync(
        Guid id,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        return inner.PreviewAsync(
            id,
            destination,
            cancellationToken);
    }

    public Task<FileAccessUrlDto?> CreatePresignedGetUrlAsync(
        Guid id,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        return inner.CreatePresignedGetUrlAsync(
            id,
            expiresIn,
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await inner.DeleteAsync(
            id,
            cancellationToken);

        await cache.RemoveFileAsync(
            id,
            CancellationToken.None);

        if (deleted)
        {
            await cache.InvalidateListsAsync(
                CancellationToken.None);
        }

        return deleted;
    }

    private async Task RefreshAfterUploadAsync(
        StoredFileDto file,
        CancellationToken cancellationToken)
    {
        await cache.SetFileAsync(
            file,
            cancellationToken);

        await cache.InvalidateListsAsync(
            cancellationToken);
    }

    private static bool CanUseListCache(
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
            (normalizedType is null) !=
            (normalizedId is null)
        )
        {
            return false;
        }

        if (
            normalizedType is null &&
            normalizedId is null
        )
        {
            return true;
        }

        return
            normalizedType!.Length <=
                StoredFile
                    .RelatedRecordTypeMaxLength &&
            normalizedId!.Length <=
                StoredFile
                    .RelatedRecordIdMaxLength;
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
