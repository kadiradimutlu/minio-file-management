using Microsoft.AspNetCore.Identity;

namespace FileManagement.Identity.Infrastructure.Persistence;

public sealed class ApplicationUser :
    IdentityUser<Guid>
{
    public DateTime CreatedAtUtc { get; set; }
}
