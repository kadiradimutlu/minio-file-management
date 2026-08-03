using System.Text.Json.Serialization;

namespace FileManagement.Contracts.Events;

public sealed class IntegrationEventEnvelope<TPayload>
{
    [JsonConstructor]
    public IntegrationEventEnvelope(
        Guid eventId,
        string eventType,
        int eventVersion,
        DateTimeOffset occurredAtUtc,
        string producer,
        string correlationId,
        TPayload payload)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException(
                "Event id cannot be empty.",
                nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException(
                "Event type is required.",
                nameof(eventType));
        }

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

        if (string.IsNullOrWhiteSpace(producer))
        {
            throw new ArgumentException(
                "Producer is required.",
                nameof(producer));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException(
                "Correlation id is required.",
                nameof(correlationId));
        }

        if (payload is null)
        {
            throw new ArgumentNullException(
                nameof(payload));
        }

        EventId = eventId;
        EventType = eventType.Trim();
        EventVersion = eventVersion;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        Producer = producer.Trim();
        CorrelationId = correlationId.Trim();
        Payload = payload;
    }

    [JsonPropertyName("eventId")]
    public Guid EventId { get; }

    [JsonPropertyName("eventType")]
    public string EventType { get; }

    [JsonPropertyName("eventVersion")]
    public int EventVersion { get; }

    [JsonPropertyName("occurredAtUtc")]
    public DateTimeOffset OccurredAtUtc { get; }

    [JsonPropertyName("producer")]
    public string Producer { get; }

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; }

    [JsonPropertyName("payload")]
    public TPayload Payload { get; }
}
