using FileManagement.Domain.Entities;
using FileManagement.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FileManagement.Infrastructure.Persistence;

public sealed class FileManagementDbContext : DbContext
{
    public FileManagementDbContext(
        DbContextOptions<FileManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<StoredFile> StoredFiles =>
        Set<StoredFile>();

    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    public DbSet<DailyFileOperationReport>
        DailyFileOperationReports =>
            Set<DailyFileOperationReport>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FileManagementDbContext).Assembly);
    }
}
