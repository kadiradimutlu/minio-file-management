namespace FileManagement.Domain.Entities;

public sealed class StoredFile
{
    public const int RelatedRecordTypeMaxLength = 100;
    public const int RelatedRecordIdMaxLength = 255;

    private StoredFile()
    {
    }

    public StoredFile(
        string originalFileName,
        string objectName,
        string bucketName,
        string contentType,
        long sizeBytes,
        string? relatedRecordType = null,
        string? relatedRecordId = null)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException(
                "Original file name is required.",
                nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new ArgumentException(
                "Object name is required.",
                nameof(objectName));
        }

        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new ArgumentException(
                "Bucket name is required.",
                nameof(bucketName));
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

        var normalizedRelatedRecordType =
            NormalizeOptionalValue(relatedRecordType);

        var normalizedRelatedRecordId =
            NormalizeOptionalValue(relatedRecordId);

        if (
            (normalizedRelatedRecordType is null) !=
            (normalizedRelatedRecordId is null)
        )
        {
            throw new ArgumentException(
                "Related record type and related record id must be provided together.");
        }

        if (
            normalizedRelatedRecordType?.Length >
            RelatedRecordTypeMaxLength
        )
        {
            throw new ArgumentException(
                $"Related record type cannot exceed {RelatedRecordTypeMaxLength} characters.",
                nameof(relatedRecordType));
        }

        if (
            normalizedRelatedRecordId?.Length >
            RelatedRecordIdMaxLength
        )
        {
            throw new ArgumentException(
                $"Related record id cannot exceed {RelatedRecordIdMaxLength} characters.",
                nameof(relatedRecordId));
        }

        Id = Guid.NewGuid();
        OriginalFileName = originalFileName.Trim();
        ObjectName = objectName.Trim();
        BucketName = bucketName.Trim();
        ContentType = contentType.Trim();
        SizeBytes = sizeBytes;
        RelatedRecordType = normalizedRelatedRecordType;
        RelatedRecordId = normalizedRelatedRecordId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string OriginalFileName { get; private set; } =
        string.Empty;

    public string ObjectName { get; private set; } =
        string.Empty;

    public string BucketName { get; private set; } =
        string.Empty;

    public string ContentType { get; private set; } =
        string.Empty;

    public long SizeBytes { get; private set; }

    public string? RelatedRecordType { get; private set; }

    public string? RelatedRecordId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
