namespace FileManagement.Application.Files.Models;

public sealed record StoredFileDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAtUtc,
    string? RelatedRecordType,
    string? RelatedRecordId);
