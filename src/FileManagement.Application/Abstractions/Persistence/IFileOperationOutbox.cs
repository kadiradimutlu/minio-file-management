using FileManagement.Domain.Entities;

namespace FileManagement.Application.Abstractions.Persistence;

public interface IFileOperationOutbox
{
    Task EnqueueAsync(
        StoredFile storedFile,
        string operation,
        string actorUserId,
        string correlationId,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken = default);
}
