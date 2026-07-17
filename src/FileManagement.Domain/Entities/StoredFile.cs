namespace FileManagement.Domain.Entities;

public sealed class StoredFile
{
    private StoredFile()
    {
    }

    public StoredFile(
        string originalFileName,
        string objectName,
        string bucketName,
        string contentType,
        long sizeBytes)
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

        Id = Guid.NewGuid();
        OriginalFileName = originalFileName.Trim();
        ObjectName = objectName.Trim();
        BucketName = bucketName.Trim();
        ContentType = contentType.Trim();
        SizeBytes = sizeBytes;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ObjectName { get; private set; } = string.Empty;

    public string BucketName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}