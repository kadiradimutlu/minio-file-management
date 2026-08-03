namespace FileManagement.Outbox.Worker.Publishing;

public sealed class OutboxPublisherCycle(
    IOutboxBatchProcessor batchProcessor,
    ILogger<OutboxPublisherCycle> logger)
{
    public async Task<bool> RunAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await batchProcessor.ProcessAsync(
                    cancellationToken);

            return result.SelectedCount == 0 ||
                result.FailedCount > 0;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Outbox batch processing failed.");

            return true;
        }
    }
}