namespace FileManagement.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public const int EventTypeMaxLength = 200;
    public const int ProducerMaxLength = 100;
    public const int CorrelationIdMaxLength = 128;
    public const int LastErrorMaxLength = 2000;

    private OutboxMessage()
    {
    }

    public OutboxMessage(
        Guid id,
        string eventType,
        int eventVersion,
        DateTimeOffset occurredAtUtc,
        string producer,
        string correlationId,
        string payload)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Outbox message id cannot be empty.",
                nameof(id));
        }

        EventType = NormalizeRequired(
            eventType,
            EventTypeMaxLength,
            nameof(eventType));

        if (eventVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(eventVersion),
                "Event version must be greater than zero.");
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException(
                "Occurred time is required.",
                nameof(occurredAtUtc));
        }

        Producer = NormalizeRequired(
            producer,
            ProducerMaxLength,
            nameof(producer));

        CorrelationId = NormalizeRequired(
            correlationId,
            CorrelationIdMaxLength,
            nameof(correlationId));

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException(
                "Event payload is required.",
                nameof(payload));
        }

        Id = id;
        EventVersion = eventVersion;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        Payload = payload.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; } =
        string.Empty;

    public int EventVersion { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string Producer { get; private set; } =
        string.Empty;

    public string CorrelationId { get; private set; } =
        string.Empty;

    public string Payload { get; private set; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public int RetryCount { get; private set; }

    public string? LastError { get; private set; }

    public void MarkProcessed(
        DateTimeOffset processedAtUtc)
    {
        if (processedAtUtc == default)
        {
            throw new ArgumentException(
                "Processed time is required.",
                nameof(processedAtUtc));
        }

        ProcessedAtUtc =
            processedAtUtc.ToUniversalTime();

        LastError = null;
    }

    public void RecordFailure(
        string error)
    {
        LastError = NormalizeRequired(
            error,
            LastErrorMaxLength,
            nameof(error));

        RetryCount++;
    }

    private static string NormalizeRequired(
        string value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}
