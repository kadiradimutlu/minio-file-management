namespace FileManagement.Identity.Infrastructure.Security;

public sealed record JwtTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);
