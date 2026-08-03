using FileManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FileManagement.Api.Persistence;

public sealed class FileManagementDbContextFactory :
    IDesignTimeDbContextFactory<FileManagementDbContext>
{
    private const string FallbackConnectionString =
        "Host=127.0.0.1;Port=5432;Database=file_management;Username=file_management";

    public FileManagementDbContext CreateDbContext(
        string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "ConnectionStrings__PostgreSql");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString =
                FallbackConnectionString;
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<
                FileManagementDbContext>();

        optionsBuilder.UseNpgsql(
            connectionString);

        return new FileManagementDbContext(
            optionsBuilder.Options);
    }
}
