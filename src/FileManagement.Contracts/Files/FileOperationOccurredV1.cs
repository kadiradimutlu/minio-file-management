using System.Text.Json.Serialization;

namespace FileManagement.Contracts.Files;

public sealed class FileOperationOccurredV1
{
    public const string EventType =
        "file.operation.occurred.v1";

    public const int EventVersion = 1;

    public const string Producer =
        "file-api";

    [JsonConstructor]
    public FileOperationOccurredV1(
        Guid fileId,
        string operation,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string? relatedRecordType,
        string? relatedRecordId,
        string actorUserId)
    {
        if (fileId == Guid.Empty)
        {
            throw new ArgumentException(
                "File id cannot be empty.",
                nameof(fileId));
        }

        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new ArgumentException(
                "Operation is required.",
                nameof(operation));
        }

        var normalizedOperation =
            operation.Trim().ToLowerInvariant();

        if (
            !FileOperationKinds.IsSupported(
                normalizedOperation)
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(operation),
                operation,
                "Unsupported file operation.");
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

        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            throw new ArgumentException(
                "Actor user id is required.",
                nameof(actorUserId));
        }

        var normalizedRelatedRecordType =
            NormalizeOptionalValue(
                relatedRecordType);

        var normalizedRelatedRecordId =
            NormalizeOptionalValue(
                relatedRecordId);

        if (
            (normalizedRelatedRecordType is null) !=
            (normalizedRelatedRecordId is null)
        )
        {
            throw new ArgumentException(
                "Related record type and related record id must be provided together.");
        }

        FileId = fileId;
        Operation = normalizedOperation;
        OriginalFileName = originalFileName.Trim();
        ContentType = contentType.Trim();
        SizeBytes = sizeBytes;
        RelatedRecordType =
            normalizedRelatedRecordType;
        RelatedRecordId =
            normalizedRelatedRecordId;
        ActorUserId = actorUserId.Trim();
    }

    [JsonPropertyName("fileId")]
    public Guid FileId { get; }

    [JsonPropertyName("operation")]
    public string Operation { get; }

    [JsonPropertyName("originalFileName")]
    public string OriginalFileName { get; }

    [JsonPropertyName("contentType")]
    public string ContentType { get; }

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; }

    [JsonPropertyName("relatedRecordType")]
    public string? RelatedRecordType { get; }

    [JsonPropertyName("relatedRecordId")]
    public string? RelatedRecordId { get; }

    [JsonPropertyName("actorUserId")]
    public string ActorUserId { get; }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
