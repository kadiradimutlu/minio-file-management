namespace FileManagement.Api.Options;

public sealed class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    public long MaxFileSizeBytes { get; init; }

    public string[] AllowedExtensions { get; init; } = [];

    public string[] AllowedContentTypes { get; init; } = [];
}