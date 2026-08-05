using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FileManagement.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext :
    IdentityDbContext<
        ApplicationUser,
        IdentityRole<Guid>,
        Guid>
{
    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }
}
