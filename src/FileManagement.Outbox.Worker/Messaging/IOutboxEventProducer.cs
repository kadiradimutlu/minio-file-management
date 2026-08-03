using FileManagement.Infrastructure.Persistence.Outbox;

namespace FileManagement.Outbox.Worker.Messaging;

public interface IOutboxEventProducer
{
    Task<KafkaDeliveryResult> ProduceAsync(
        OutboxMessage message,
        CancellationToken cancellationToken);
}