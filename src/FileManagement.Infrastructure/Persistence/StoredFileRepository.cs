using FileManagement.Application.Abstractions.Persistence;
using FileManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileManagement.Infrastructure.Persistence;

public sealed class StoredFileRepository :
    IStoredFileRepository
{
    private readonly FileManagementDbContext _dbContext;

    public StoredFileRepository(
        FileManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        StoredFile storedFile,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.StoredFiles.AddAsync(
            storedFile,
            cancellationToken);
    }

    public Task<StoredFile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.StoredFiles
            .SingleOrDefaultAsync(
                storedFile => storedFile.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<StoredFile>> ListAsync(
        string? relatedRecordType = null,
        string? relatedRecordId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StoredFile> query =
            _dbContext.StoredFiles
                .AsNoTracking();

        if (
            relatedRecordType is not null &&
            relatedRecordId is not null
        )
        {
            query = query.Where(
                storedFile =>
                    storedFile.RelatedRecordType ==
                        relatedRecordType &&
                    storedFile.RelatedRecordId ==
                        relatedRecordId);
        }

        return await query
            .OrderByDescending(
                storedFile => storedFile.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public void Remove(StoredFile storedFile)
    {
        _dbContext.StoredFiles.Remove(storedFile);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
