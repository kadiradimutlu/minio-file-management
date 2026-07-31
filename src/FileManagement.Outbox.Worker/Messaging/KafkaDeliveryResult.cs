namespace FileManagement.Outbox.Worker.Messaging;

public sealed record KafkaDeliveryResult(
    int Partition,
    long Offset);