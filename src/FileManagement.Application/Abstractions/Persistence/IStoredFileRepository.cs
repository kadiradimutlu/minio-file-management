using FileManagement.Domain.Entities;

namespace FileManagement.Application.Abstractions.Persistence;

public interface IStoredFileRepository
{
    Task AddAsync(
        StoredFile storedFile,
        CancellationToken cancellationToken = default);

    Task<StoredFile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredFile>> ListAsync(
        CancellationToken cancellationToken = default);

    void Remove(StoredFile storedFile);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}