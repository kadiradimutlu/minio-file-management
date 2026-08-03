namespace FileManagement.Infrastructure.Caching;

public sealed class FileMetadataCacheOptions
{
    public const string SectionName =
        "FileCache";

    public bool Enabled { get; set; }

    public string KeyPrefix { get; set; } =
        "file-management:local:files:v1";

    public int DetailTtlSeconds { get; set; } =
        300;

    public int ListTtlSeconds { get; set; } =
        30;
}
