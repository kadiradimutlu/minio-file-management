using FileManagement.Outbox.Worker.Options;
using Microsoft.Extensions.Options;

namespace FileManagement.Outbox.Worker.Publishing;

public sealed class OutboxPublisherWorker(
    OutboxPublisherCycle publisherCycle,
    IOptions<OutboxPublisherOptions> options,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    private readonly TimeSpan _pollInterval =
        TimeSpan.FromMilliseconds(
            options.Value.PollIntervalMilliseconds);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Outbox publisher started. PollInterval: {PollInterval}",
            _pollInterval);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var shouldDelay =
                    await publisherCycle.RunAsync(
                        stoppingToken);

                if (!shouldDelay)
                {
                    continue;
                }

                await Task.Delay(
                    _pollInterval,
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Outbox publisher cancellation requested.");
        }
        finally
        {
            logger.LogInformation(
                "Outbox publisher stopped.");
        }
    }
}