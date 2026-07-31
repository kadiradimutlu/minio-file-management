using FileManagement.Outbox.Worker.Publishing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FileManagement.Outbox.UnitTests.Publishing;

public sealed class OutboxPublisherCycleTests
{
    [Fact]
    public async Task RunAsync_WithSuccessfulNonEmptyBatch_DoesNotRequestDelay()
    {
        var processor =
            new FakeBatchProcessor(
                new OutboxBatchResult(
                    3,
                    3,
                    0));

        var cycle =
            CreateCycle(
                processor);

        var shouldDelay =
            await cycle.RunAsync(
                CancellationToken.None);

        Assert.False(
            shouldDelay);

        Assert.Equal(
            1,
            processor.CallCount);
    }

    [Fact]
    public async Task RunAsync_WithEmptyBatch_RequestsDelay()
    {
        var processor =
            new FakeBatchProcessor(
                new OutboxBatchResult(
                    0,
                    0,
                    0));

        var cycle =
            CreateCycle(
                processor);

        var shouldDelay =
            await cycle.RunAsync(
                CancellationToken.None);

        Assert.True(
            shouldDelay);

        Assert.Equal(
            1,
            processor.CallCount);
    }

    [Fact]
    public async Task RunAsync_WithFailedMessages_RequestsDelay()
    {
        var processor =
            new FakeBatchProcessor(
                new OutboxBatchResult(
                    3,
                    2,
                    1));

        var cycle =
            CreateCycle(
                processor);

        var shouldDelay =
            await cycle.RunAsync(
                CancellationToken.None);

        Assert.True(
            shouldDelay);

        Assert.Equal(
            1,
            processor.CallCount);
    }

    [Fact]
    public async Task RunAsync_WhenProcessorFails_RequestsDelay()
    {
        var processor =
            new FakeBatchProcessor(
                new InvalidOperationException(
                    "PostgreSQL unavailable"));

        var cycle =
            CreateCycle(
                processor);

        var shouldDelay =
            await cycle.RunAsync(
                CancellationToken.None);

        Assert.True(
            shouldDelay);

        Assert.Equal(
            1,
            processor.CallCount);
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_RethrowsCancellation()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var processor =
            new FakeBatchProcessor(
                new OperationCanceledException(
                    cancellationTokenSource.Token));

        var cycle =
            CreateCycle(
                processor);

        await Assert.ThrowsAsync<
            OperationCanceledException>(
            () =>
                cycle.RunAsync(
                    cancellationTokenSource.Token));

        Assert.Equal(
            1,
            processor.CallCount);
    }

    private static OutboxPublisherCycle CreateCycle(
        IOutboxBatchProcessor processor)
    {
        return new OutboxPublisherCycle(
            processor,
            NullLogger<OutboxPublisherCycle>.Instance);
    }

    private sealed class FakeBatchProcessor :
        IOutboxBatchProcessor
    {
        private readonly OutboxBatchResult? _result;
        private readonly Exception? _exception;

        public FakeBatchProcessor(
            OutboxBatchResult result)
        {
            _result =
                result;
        }

        public FakeBatchProcessor(
            Exception exception)
        {
            _exception =
                exception;
        }

        public int CallCount { get; private set; }

        public Task<OutboxBatchResult> ProcessAsync(
            CancellationToken cancellationToken)
        {
            CallCount++;

            if (_exception is not null)
            {
                return Task.FromException<
                    OutboxBatchResult>(
                    _exception);
            }

            return Task.FromResult(
                _result ??
                throw new InvalidOperationException(
                    "Batch result is not configured."));
        }
    }
}