using System.Data;
using FileManagement.Infrastructure.Persistence;
using FileManagement.Outbox.Worker.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FileManagement.Outbox.Worker.Publishing;

public sealed class OutboxBatchProcessor(
    IDbContextFactory<FileManagementDbContext> dbContextFactory,
    OutboxMessagePublisher messagePublisher,
    IOptions<OutboxPublisherOptions> options,
    ILogger<OutboxBatchProcessor> logger)
    : IOutboxBatchProcessor
{
    private readonly int _batchSize =
        options.Value.BatchSize;

    public async Task<OutboxBatchResult> ProcessAsync(
        CancellationToken cancellationToken)
    {
        await using var dbContext =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var messages =
            await dbContext.OutboxMessages
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM outbox_messages
                    WHERE processed_at_utc IS NULL
                    ORDER BY created_at_utc, id
                    LIMIT {_batchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(
                    cancellationToken);

        if (messages.Count == 0)
        {
            await transaction.CommitAsync(
                cancellationToken);

            return new OutboxBatchResult(
                0,
                0,
                0);
        }

        var publishedCount = 0;
        var failedCount = 0;

        foreach (var message in messages)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var published =
                await messagePublisher.PublishAsync(
                    message,
                    cancellationToken);

            if (published)
            {
                publishedCount++;
            }
            else
            {
                failedCount++;
            }
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        var result =
            new OutboxBatchResult(
                messages.Count,
                publishedCount,
                failedCount);

        logger.LogInformation(
            "Outbox batch completed. Selected: {SelectedCount}, " +
            "Published: {PublishedCount}, Failed: {FailedCount}",
            result.SelectedCount,
            result.PublishedCount,
            result.FailedCount);

        return result;
    }
}