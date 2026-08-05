namespace FileManagement.Outbox.Worker.Options;

public sealed class OutboxPublisherOptions
{
    public const string SectionName =
        "OutboxPublisher";

    public int BatchSize { get; set; } =
        50;

    public int PollIntervalMilliseconds { get; set; } =
        1000;
}