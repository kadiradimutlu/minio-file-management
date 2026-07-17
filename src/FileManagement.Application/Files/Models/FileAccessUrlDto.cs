namespace FileManagement.Application.Files.Models;

public sealed record FileAccessUrlDto(
    string Url,
    DateTimeOffset ExpiresAtUtc);