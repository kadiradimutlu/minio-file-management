namespace FileManagement.Identity.Api.Models;

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles);
