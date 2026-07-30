using FileManagement.Infrastructure.Persistence.Outbox;

namespace FileManagement.UnitTests.Infrastructure;

public sealed class OutboxMessageTests
{
    [Fact]
    public void Constructor_WithValidValues_NormalizesMessage()
    {
        var id = Guid.NewGuid();

        var occurredAt =
            new DateTimeOffset(
                2026,
                7,
                30,
                11,
                0,
                0,
                TimeSpan.FromHours(3));

        var message = new OutboxMessage(
            id,
            " file.operation.occurred.v1 ",
            1,
            occurredAt,
            " file-api ",
            " correlation-123 ",
            " {\"eventId\":\"123\"} ");

        Assert.Equal(
            id,
            message.Id);

        Assert.Equal(
            "file.operation.occurred.v1",
            message.EventType);

        Assert.Equal(
            1,
            message.EventVersion);

        Assert.Equal(
            new DateTimeOffset(
                2026,
                7,
                30,
                8,
                0,
                0,
                TimeSpan.Zero),
            message.OccurredAtUtc);

        Assert.Equal(
            "file-api",
            message.Producer);

        Assert.Equal(
            "correlation-123",
            message.CorrelationId);

        Assert.Equal(
            "{\"eventId\":\"123\"}",
            message.Payload);

        Assert.Equal(
            TimeSpan.Zero,
            message.CreatedAtUtc.Offset);

        Assert.Null(
            message.ProcessedAtUtc);

        Assert.Equal(
            0,
            message.RetryCount);

        Assert.Null(
            message.LastError);
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsException()
    {
        var action = () =>
            new OutboxMessage(
                Guid.Empty,
                "file.operation.occurred.v1",
                1,
                DateTimeOffset.UtcNow,
                "file-api",
                "correlation-123",
                "{}");

        Assert.Throws<ArgumentException>(
            action);
    }

    [Fact]
    public void RecordFailure_IncrementsRetryCountAndStoresError()
    {
        var message = CreateMessage();

        message.RecordFailure(
            " Kafka unavailable ");

        Assert.Equal(
            1,
            message.RetryCount);

        Assert.Equal(
            "Kafka unavailable",
            message.LastError);

        Assert.Null(
            message.ProcessedAtUtc);
    }

    [Fact]
    public void MarkProcessed_SetsUtcTimeAndClearsError()
    {
        var message = CreateMessage();

        message.RecordFailure(
            "Kafka unavailable");

        var processedAt =
            new DateTimeOffset(
                2026,
                7,
                30,
                12,
                0,
                0,
                TimeSpan.FromHours(3));

        message.MarkProcessed(
            processedAt);

        Assert.Equal(
            new DateTimeOffset(
                2026,
                7,
                30,
                9,
                0,
                0,
                TimeSpan.Zero),
            message.ProcessedAtUtc);

        Assert.Null(
            message.LastError);

        Assert.Equal(
            1,
            message.RetryCount);
    }

    private static OutboxMessage CreateMessage()
    {
        return new OutboxMessage(
            Guid.NewGuid(),
            "file.operation.occurred.v1",
            1,
            DateTimeOffset.UtcNow,
            "file-api",
            "correlation-123",
            "{}");
    }
}
