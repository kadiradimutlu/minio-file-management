using FileManagement.Contracts.Events;
using FileManagement.Contracts.Files;

namespace FileManagement.Operations.Worker.Messaging;

public sealed class LoggingFileOperationEventHandler(
    ILogger<LoggingFileOperationEventHandler> logger)
    : IFileOperationEventHandler
{
    public Task HandleAsync(
        IntegrationEventEnvelope<FileOperationOccurredV1> envelope,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "File operation event handled. EventId: {EventId}, " +
            "FileId: {FileId}, Operation: {Operation}, " +
            "ActorUserId: {ActorUserId}, CorrelationId: {CorrelationId}",
            envelope.EventId,
            envelope.Payload.FileId,
            envelope.Payload.Operation,
            envelope.Payload.ActorUserId,
            envelope.CorrelationId);

        return Task.CompletedTask;
    }
}
