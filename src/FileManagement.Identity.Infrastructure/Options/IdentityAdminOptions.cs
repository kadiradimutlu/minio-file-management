namespace FileManagement.Identity.Infrastructure.Options;

public sealed class IdentityAdminOptions
{
    public const string SectionName =
        "IdentityAdmin";

    public string Email { get; set; } =
        string.Empty;

    public string Password { get; set; } =
        string.Empty;
}
