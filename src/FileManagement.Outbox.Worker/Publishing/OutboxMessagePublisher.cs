using FileManagement.Infrastructure.Persistence.Outbox;
using FileManagement.Outbox.Worker.Messaging;

namespace FileManagement.Outbox.Worker.Publishing;

public sealed class OutboxMessagePublisher(
    IOutboxEventProducer eventProducer,
    TimeProvider timeProvider,
    ILogger<OutboxMessagePublisher> logger)
{
    public async Task<bool> PublishAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.ProcessedAtUtc is not null)
        {
            throw new InvalidOperationException(
                "A processed outbox message cannot be published again.");
        }

        try
        {
            var deliveryResult =
                await eventProducer.ProduceAsync(
                    message,
                    cancellationToken);

            message.MarkProcessed(
                timeProvider.GetUtcNow());

            logger.LogInformation(
                "Outbox message published. EventId: {EventId}, " +
                "EventType: {EventType}, Partition: {Partition}, " +
                "Offset: {Offset}, CorrelationId: {CorrelationId}",
                message.Id,
                message.EventType,
                deliveryResult.Partition,
                deliveryResult.Offset,
                message.CorrelationId);

            return true;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failureMessage =
                CreateFailureMessage(
                    exception);

            message.RecordFailure(
                failureMessage);

            logger.LogError(
                exception,
                "Outbox message publish failed. EventId: {EventId}, " +
                "EventType: {EventType}, RetryCount: {RetryCount}, " +
                "CorrelationId: {CorrelationId}",
                message.Id,
                message.EventType,
                message.RetryCount,
                message.CorrelationId);

            return false;
        }
    }

    private static string CreateFailureMessage(
        Exception exception)
    {
        var failureMessage =
            string.IsNullOrWhiteSpace(
                exception.Message)
                ? exception.GetType().Name
                : exception.Message.Trim();

        return failureMessage.Length <=
            OutboxMessage.LastErrorMaxLength
                ? failureMessage
                : failureMessage[
                    ..OutboxMessage.LastErrorMaxLength];
    }
}