namespace FileManagement.Outbox.Worker.Options;

public sealed class KafkaProducerOptions
{
    public const string SectionName =
        "Kafka";

    public string BootstrapServers { get; set; } =
        string.Empty;

    public string Topic { get; set; } =
        string.Empty;

    public string ClientId { get; set; } =
        string.Empty;
}