using System.Text.Json;
using Confluent.Kafka;
using FileManagement.Operations.Worker.Options;
using Microsoft.Extensions.Options;

namespace FileManagement.Operations.Worker.Messaging;

public sealed class KafkaFileOperationConsumer(
    IOptions<KafkaConsumerOptions> options,
    FileOperationEventDeserializer deserializer,
    IFileOperationEventHandler eventHandler,
    ILogger<KafkaFileOperationConsumer> logger)
    : BackgroundService
{
    private readonly KafkaConsumerOptions _options =
        options.Value;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var consumerConfig =
            new ConsumerConfig
            {
                BootstrapServers =
                    _options.BootstrapServers,
                GroupId =
                    _options.GroupId,
                ClientId =
                    _options.ClientId,
                AutoOffsetReset =
                    AutoOffsetReset.Earliest,
                EnableAutoCommit =
                    false,
                EnableAutoOffsetStore =
                    false,
                AllowAutoCreateTopics =
                    false
            };

        using var consumer =
            new ConsumerBuilder<Ignore, string>(
                consumerConfig)
            .SetErrorHandler(
                (_, error) =>
                {
                    logger.LogWarning(
                        "Kafka client error. Code: {Code}, Reason: {Reason}",
                        error.Code,
                        error.Reason);
                })
            .Build();

        consumer.Subscribe(
            _options.Topic);

        logger.LogInformation(
            "Kafka consumer started. Topic: {Topic}, " +
            "GroupId: {GroupId}, BootstrapServers: {BootstrapServers}",
            _options.Topic,
            _options.GroupId,
            _options.BootstrapServers);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<Ignore, string> consumeResult;

                try
                {
                    consumeResult =
                        consumer.Consume(
                            stoppingToken);
                }
                catch (ConsumeException exception)
                {
                    logger.LogError(
                        exception,
                        "Kafka message consumption failed.");

                    await Task.Delay(
                        TimeSpan.FromSeconds(2),
                        stoppingToken);

                    continue;
                }

                if (
                    consumeResult.Message is null ||
                    string.IsNullOrWhiteSpace(
                        consumeResult.Message.Value)
                )
                {
                    logger.LogWarning(
                        "Empty Kafka message skipped at {Offset}.",
                        consumeResult.TopicPartitionOffset);

                    consumer.Commit(
                        consumeResult);

                    continue;
                }

                try
                {
                    var envelope =
                        deserializer.Deserialize(
                            consumeResult.Message.Value);

                    await eventHandler.HandleAsync(
                        envelope,
                        stoppingToken);

                    consumer.Commit(
                        consumeResult);

                    logger.LogInformation(
                        "Kafka offset committed. Topic: {Topic}, " +
                        "Partition: {Partition}, Offset: {Offset}",
                        consumeResult.Topic,
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value);
                }
                catch (Exception exception)
                    when (
                        exception is JsonException or
                        InvalidDataException or
                        ArgumentException
                    )
                {
                    logger.LogError(
                        exception,
                        "Invalid Kafka event discarded at {Offset}.",
                        consumeResult.TopicPartitionOffset);

                    consumer.Commit(
                        consumeResult);
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Kafka consumer cancellation requested.");
        }
        finally
        {
            consumer.Close();

            logger.LogInformation(
                "Kafka consumer stopped.");
        }
    }
}
