namespace FileManagement.Outbox.Worker.Publishing;

public interface IOutboxBatchProcessor
{
    Task<OutboxBatchResult> ProcessAsync(
        CancellationToken cancellationToken);
}