namespace FileManagement.Identity.Api.Models;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles);
