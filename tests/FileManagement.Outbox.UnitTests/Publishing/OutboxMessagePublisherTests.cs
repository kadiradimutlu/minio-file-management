using FileManagement.Infrastructure.Persistence.Outbox;
using FileManagement.Outbox.Worker.Messaging;
using FileManagement.Outbox.Worker.Publishing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FileManagement.Outbox.UnitTests.Publishing;

public sealed class OutboxMessagePublisherTests
{
    [Fact]
    public async Task PublishAsync_WithSuccessfulDelivery_MarksMessageProcessed()
    {
        var message =
            CreateMessage();

        var processedAtUtc =
            new DateTimeOffset(
                2026,
                7,
                31,
                7,
                0,
                0,
                TimeSpan.Zero);

        var producer =
            new FakeOutboxEventProducer(
                new KafkaDeliveryResult(
                    2,
                    42));

        var publisher =
            CreatePublisher(
                producer,
                processedAtUtc);

        var result =
            await publisher.PublishAsync(
                message,
                CancellationToken.None);

        Assert.True(result);

        Assert.Equal(
            processedAtUtc,
            message.ProcessedAtUtc);

        Assert.Equal(
            0,
            message.RetryCount);

        Assert.Null(
            message.LastError);

        Assert.Equal(
            1,
            producer.CallCount);

        Assert.Same(
            message,
            producer.LastMessage);
    }

    [Fact]
    public async Task PublishAsync_WhenProducerFails_RecordsFailure()
    {
        var message =
            CreateMessage();

        var producer =
            new FakeOutboxEventProducer(
                new InvalidOperationException(
                    " Kafka unavailable "));

        var publisher =
            CreatePublisher(
                producer,
                DateTimeOffset.UtcNow);

        var result =
            await publisher.PublishAsync(
                message,
                CancellationToken.None);

        Assert.False(result);

        Assert.Null(
            message.ProcessedAtUtc);

        Assert.Equal(
            1,
            message.RetryCount);

        Assert.Equal(
            "Kafka unavailable",
            message.LastError);

        Assert.Equal(
            1,
            producer.CallCount);
    }

    [Fact]
    public async Task PublishAsync_WithLongFailure_TruncatesStoredError()
    {
        var message =
            CreateMessage();

        var error =
            new string(
                'x',
                OutboxMessage.LastErrorMaxLength + 100);

        var producer =
            new FakeOutboxEventProducer(
                new InvalidOperationException(
                    error));

        var publisher =
            CreatePublisher(
                producer,
                DateTimeOffset.UtcNow);

        var result =
            await publisher.PublishAsync(
                message,
                CancellationToken.None);

        Assert.False(result);

        Assert.NotNull(
            message.LastError);

        Assert.Equal(
            OutboxMessage.LastErrorMaxLength,
            message.LastError.Length);

        Assert.Equal(
            new string(
                'x',
                OutboxMessage.LastErrorMaxLength),
            message.LastError);
    }

    [Fact]
    public async Task PublishAsync_WhenCancelled_DoesNotRecordFailure()
    {
        var message =
            CreateMessage();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var producer =
            new FakeOutboxEventProducer(
                new OperationCanceledException(
                    cancellationTokenSource.Token));

        var publisher =
            CreatePublisher(
                producer,
                DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<
            OperationCanceledException>(
            () =>
                publisher.PublishAsync(
                    message,
                    cancellationTokenSource.Token));

        Assert.Null(
            message.ProcessedAtUtc);

        Assert.Equal(
            0,
            message.RetryCount);

        Assert.Null(
            message.LastError);

        Assert.Equal(
            1,
            producer.CallCount);
    }

    [Fact]
    public async Task PublishAsync_WithProcessedMessage_ThrowsBeforeProducing()
    {
        var message =
            CreateMessage();

        message.MarkProcessed(
            DateTimeOffset.UtcNow);

        var producer =
            new FakeOutboxEventProducer(
                new KafkaDeliveryResult(
                    0,
                    1));

        var publisher =
            CreatePublisher(
                producer,
                DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<
            InvalidOperationException>(
            () =>
                publisher.PublishAsync(
                    message,
                    CancellationToken.None));

        Assert.Equal(
            0,
            producer.CallCount);
    }

    private static OutboxMessagePublisher CreatePublisher(
        IOutboxEventProducer producer,
        DateTimeOffset utcNow)
    {
        return new OutboxMessagePublisher(
            producer,
            new FixedTimeProvider(
                utcNow),
            NullLogger<OutboxMessagePublisher>.Instance);
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
            "{\"eventId\":\"123\"}");
    }

    private sealed class FakeOutboxEventProducer :
        IOutboxEventProducer
    {
        private readonly KafkaDeliveryResult? _deliveryResult;
        private readonly Exception? _exception;

        public FakeOutboxEventProducer(
            KafkaDeliveryResult deliveryResult)
        {
            _deliveryResult =
                deliveryResult;
        }

        public FakeOutboxEventProducer(
            Exception exception)
        {
            _exception =
                exception;
        }

        public int CallCount { get; private set; }

        public OutboxMessage? LastMessage { get; private set; }

        public Task<KafkaDeliveryResult> ProduceAsync(
            OutboxMessage message,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastMessage = message;

            if (_exception is not null)
            {
                return Task.FromException<
                    KafkaDeliveryResult>(
                    _exception);
            }

            return Task.FromResult(
                _deliveryResult ??
                throw new InvalidOperationException(
                    "Delivery result is not configured."));
        }
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}