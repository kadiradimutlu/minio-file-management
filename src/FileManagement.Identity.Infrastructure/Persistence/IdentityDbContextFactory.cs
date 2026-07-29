using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FileManagement.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContextFactory :
    IDesignTimeDbContextFactory<IdentityDbContext>
{
    private const string DefaultConnectionString =
        "Host=127.0.0.1;Port=5432;Database=identity_management;Username=file_management;Password=design-time-only";

    public IdentityDbContext CreateDbContext(
        string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "IDENTITY_DESIGN_CONNECTION_STRING");

        if (
            string.IsNullOrWhiteSpace(
                connectionString)
        )
        {
            connectionString =
                DefaultConnectionString;
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<
                IdentityDbContext>();

        optionsBuilder.UseNpgsql(
            connectionString);

        return new IdentityDbContext(
            optionsBuilder.Options);
    }
}
