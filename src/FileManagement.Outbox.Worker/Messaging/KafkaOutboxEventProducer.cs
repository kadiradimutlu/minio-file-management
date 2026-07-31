using Confluent.Kafka;
using FileManagement.Infrastructure.Persistence.Outbox;
using FileManagement.Outbox.Worker.Options;
using Microsoft.Extensions.Options;

namespace FileManagement.Outbox.Worker.Messaging;

public sealed class KafkaOutboxEventProducer :
    IOutboxEventProducer,
    IDisposable
{
    private readonly KafkaProducerOptions _options;
    private readonly IProducer<string, string> _producer;

    public KafkaOutboxEventProducer(
        IOptions<KafkaProducerOptions> options,
        ILogger<KafkaOutboxEventProducer> logger)
    {
        _options = options.Value;

        var producerConfig =
            new ProducerConfig
            {
                BootstrapServers =
                    _options.BootstrapServers,
                ClientId =
                    _options.ClientId,
                AllowAutoCreateTopics =
                    false,
                EnableIdempotence =
                    true,
                Acks =
                    Acks.All
            };

        _producer =
            new ProducerBuilder<string, string>(
                producerConfig)
                .SetErrorHandler(
                    (_, error) =>
                    {
                        logger.LogWarning(
                            "Kafka producer error. Code: {Code}, " +
                            "Reason: {Reason}",
                            error.Code,
                            error.Reason);
                    })
                .Build();
    }

    public async Task<KafkaDeliveryResult> ProduceAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var kafkaMessage =
            new Message<string, string>
            {
                Key =
                    message.Id.ToString("N"),
                Value =
                    message.Payload
            };

        var deliveryResult =
            await _producer.ProduceAsync(
                _options.Topic,
                kafkaMessage,
                cancellationToken);

        return new KafkaDeliveryResult(
            deliveryResult.Partition.Value,
            deliveryResult.Offset.Value);
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}