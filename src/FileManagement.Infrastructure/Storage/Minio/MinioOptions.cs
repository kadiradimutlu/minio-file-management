namespace FileManagement.Infrastructure.Storage.Minio;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; init; } = string.Empty;

    public string PublicEndpoint { get; init; } = string.Empty;

    public string AccessKey { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public string BucketName { get; init; } = string.Empty;

    public bool UseSsl { get; init; }

    public bool PublicUseSsl { get; init; }
}