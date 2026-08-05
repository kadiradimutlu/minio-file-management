using FileManagement.Contracts.Events;
using FileManagement.Contracts.Files;

namespace FileManagement.Operations.Worker.Messaging;

public interface IFileOperationEventHandler
{
    Task HandleAsync(
        IntegrationEventEnvelope<FileOperationOccurredV1> envelope,
        CancellationToken cancellationToken);
}
