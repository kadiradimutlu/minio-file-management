using FileManagement.Application.Abstractions.Execution;
using FileManagement.Application.Abstractions.Persistence;
using FileManagement.Application.Abstractions.Storage;
using FileManagement.Application.Files.Models;
using FileManagement.Contracts.Files;
using FileManagement.Domain.Entities;

namespace FileManagement.Application.Files;

public sealed class FileManagementService : IFileManagementService
{
    private readonly IStoredFileRepository _repository;
    private readonly IFileStorageService _storageService;
    private readonly IFileOperationOutbox _outbox;
    private readonly IFileOperationContext _operationContext;
    private readonly TimeProvider _timeProvider;

    public FileManagementService(
        IStoredFileRepository repository,
        IFileStorageService storageService,
        IFileOperationOutbox outbox,
        IFileOperationContext operationContext,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _storageService = storageService;
        _outbox = outbox;
        _operationContext = operationContext;
        _timeProvider = timeProvider;
    }

    public Task<StoredFileDto> UploadAsync(
        string originalFileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        return UploadAsync(
            originalFileName,
            contentType,
            sizeBytes,
            content,
            null,
            null,
            cancellationToken);
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
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "The upload stream must be readable.",
                nameof(content));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException(
                "Original file name is required.",
                nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException(
                "Content type is required.",
                nameof(contentType));
        }

        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizeBytes),
                "File size cannot be negative.");
        }

        var safeFileName =
            GetSafeFileName(originalFileName);

        var objectName =
            CreateObjectName(safeFileName);

        var storedFile = new StoredFile(
            safeFileName,
            objectName,
            _storageService.BucketName,
            contentType,
            sizeBytes,
            relatedRecordType,
            relatedRecordId);

        await _storageService.UploadAsync(
            objectName,
            content,
            sizeBytes,
            contentType,
            cancellationToken);

        try
        {
            await _repository.AddAsync(
                storedFile,
                cancellationToken);

            var actorUserId =
                _operationContext.ActorUserId;

            var correlationId =
                _operationContext.CorrelationId;

            var occurredAtUtc =
                _timeProvider.GetUtcNow();

            await _outbox.EnqueueAsync(
                storedFile,
                FileOperationKinds.Uploaded,
                actorUserId,
                correlationId,
                occurredAtUtc,
                cancellationToken);

            await _repository.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            try
            {
                await _storageService.DeleteAsync(
                    objectName,
                    CancellationToken.None);
            }
            catch
            {
                // Preserve the original persistence exception.
            }

            throw;
        }

        return Map(storedFile);
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
        var association =
            NormalizeRelatedRecordAssociation(
                relatedRecordType,
                relatedRecordId);

        var storedFiles = await _repository.ListAsync(
            association.Type,
            association.Id,
            cancellationToken);

        return storedFiles
            .Select(Map)
            .ToArray();
    }

    public async Task<StoredFileDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var storedFile = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        return storedFile is null
            ? null
            : Map(storedFile);
    }

    public async Task<StoredFileDto?> DownloadAsync(
        Guid id,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!destination.CanWrite)
        {
            throw new ArgumentException(
                "The destination stream must be writable.",
                nameof(destination));
        }

        var storedFile = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (storedFile is null)
        {
            return null;
        }

        await _storageService.DownloadAsync(
            storedFile.ObjectName,
            destination,
            cancellationToken);

        return Map(storedFile);
    }

    public async Task<FileAccessUrlDto?> CreatePresignedGetUrlAsync(
        Guid id,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        var storedFile = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (storedFile is null)
        {
            return null;
        }

        var url =
            await _storageService.CreatePresignedGetUrlAsync(
                storedFile.ObjectName,
                expiresIn,
                cancellationToken);

        return new FileAccessUrlDto(
            url,
            DateTimeOffset.UtcNow.Add(expiresIn));
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var storedFile = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (storedFile is null)
        {
            return false;
        }

        await _storageService.DeleteAsync(
            storedFile.ObjectName,
            cancellationToken);

        _repository.Remove(storedFile);

        await _repository.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private static StoredFileDto Map(
        StoredFile storedFile)
    {
        return new StoredFileDto(
            storedFile.Id,
            storedFile.OriginalFileName,
            storedFile.ContentType,
            storedFile.SizeBytes,
            storedFile.CreatedAtUtc,
            storedFile.RelatedRecordType,
            storedFile.RelatedRecordId);
    }

    private static (
        string? Type,
        string? Id
    ) NormalizeRelatedRecordAssociation(
        string? relatedRecordType,
        string? relatedRecordId)
    {
        var normalizedType =
            NormalizeOptionalValue(relatedRecordType);

        var normalizedId =
            NormalizeOptionalValue(relatedRecordId);

        if (
            (normalizedType is null) !=
            (normalizedId is null)
        )
        {
            throw new ArgumentException(
                "Related record type and related record id must be provided together.");
        }

        if (
            normalizedType?.Length >
            StoredFile.RelatedRecordTypeMaxLength
        )
        {
            throw new ArgumentException(
                $"Related record type cannot exceed {StoredFile.RelatedRecordTypeMaxLength} characters.",
                nameof(relatedRecordType));
        }

        if (
            normalizedId?.Length >
            StoredFile.RelatedRecordIdMaxLength
        )
        {
            throw new ArgumentException(
                $"Related record id cannot exceed {StoredFile.RelatedRecordIdMaxLength} characters.",
                nameof(relatedRecordId));
        }

        return (
            normalizedType,
            normalizedId
        );
    }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string GetSafeFileName(
        string originalFileName)
    {
        var normalizedName = originalFileName
            .Trim()
            .Replace('\\', '/');

        var safeFileName = Path.GetFileName(
            normalizedName);

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException(
                "Original file name is invalid.",
                nameof(originalFileName));
        }

        return safeFileName;
    }

    private static string CreateObjectName(
        string fileName)
    {
        var extension = Path
            .GetExtension(fileName)
            .ToLowerInvariant();

        return $"{DateTimeOffset.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
    }
}
