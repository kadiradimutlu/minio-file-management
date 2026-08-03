using System.Text.Json;
using FileManagement.Application.Abstractions.Persistence;
using FileManagement.Contracts.Events;
using FileManagement.Contracts.Files;
using FileManagement.Domain.Entities;

namespace FileManagement.Infrastructure.Persistence.Outbox;

public sealed class FileOperationOutbox :
    IFileOperationOutbox
{
    private static readonly JsonSerializerOptions
        SerializerOptions =
            new(JsonSerializerDefaults.Web);

    private readonly FileManagementDbContext _dbContext;

    public FileOperationOutbox(
        FileManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnqueueAsync(
        StoredFile storedFile,
        string operation,
        string actorUserId,
        string correlationId,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storedFile);

        var payload = new FileOperationOccurredV1(
            storedFile.Id,
            operation,
            storedFile.OriginalFileName,
            storedFile.ContentType,
            storedFile.SizeBytes,
            storedFile.RelatedRecordType,
            storedFile.RelatedRecordId,
            actorUserId);

        var envelope =
            new IntegrationEventEnvelope<
                FileOperationOccurredV1>(
                Guid.NewGuid(),
                FileOperationOccurredV1.EventType,
                FileOperationOccurredV1.EventVersion,
                occurredAtUtc,
                FileOperationOccurredV1.Producer,
                correlationId,
                payload);

        var serializedEnvelope =
            JsonSerializer.Serialize(
                envelope,
                SerializerOptions);

        var outboxMessage = new OutboxMessage(
            envelope.EventId,
            envelope.EventType,
            envelope.EventVersion,
            envelope.OccurredAtUtc,
            envelope.Producer,
            envelope.CorrelationId,
            serializedEnvelope);

        await _dbContext.OutboxMessages.AddAsync(
            outboxMessage,
            cancellationToken);
    }
}
