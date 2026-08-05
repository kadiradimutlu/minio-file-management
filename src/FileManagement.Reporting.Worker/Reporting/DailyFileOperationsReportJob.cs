using System.Text.Json;
using FileManagement.Contracts.Files;
using FileManagement.Domain.Entities;
using FileManagement.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace FileManagement.Reporting.Worker.Reporting;

public sealed class DailyFileOperationsReportJob(
    IDbContextFactory<FileManagementDbContext>
        dbContextFactory,
    FileOperationEventParser eventParser,
    DailyFileOperationsReportCalculator calculator,
    TimeProvider timeProvider,
    ILogger<DailyFileOperationsReportJob> logger)
{
    private static readonly JsonSerializerOptions
        SerializerOptions =
            new(JsonSerializerDefaults.Web);

    [Queue("reporting")]
    [AutomaticRetry(
        Attempts = 3,
        DelaysInSeconds = new[] { 60, 300, 900 },
        OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(600)]
    public Task GeneratePreviousDayAsync()
    {
        var previousUtcDate =
            DateOnly.FromDateTime(
                timeProvider.GetUtcNow()
                    .UtcDateTime)
                .AddDays(-1);

        return GenerateCoreAsync(
            previousUtcDate);
    }

    [Queue("reporting")]
    [AutomaticRetry(
        Attempts = 3,
        DelaysInSeconds = new[] { 60, 300, 900 },
        OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(600)]
    public Task GenerateAsync(
        DateTime reportDateUtc)
    {
        return GenerateCoreAsync(
            DateOnly.FromDateTime(
                reportDateUtc.ToUniversalTime()));
    }

    private async Task GenerateCoreAsync(
        DateOnly reportDate)
    {
        var periodStart =
            new DateTimeOffset(
                reportDate.ToDateTime(
                    TimeOnly.MinValue,
                    DateTimeKind.Utc));

        var periodEnd =
            periodStart.AddDays(1);

        await using var dbContext =
            await dbContextFactory
                .CreateDbContextAsync();

        var messages =
            await dbContext.OutboxMessages
                .AsNoTracking()
                .Where(
                    message =>
                        message.EventType ==
                            FileOperationOccurredV1
                                .EventType &&
                        message.EventVersion ==
                            FileOperationOccurredV1
                                .EventVersion &&
                        message.OccurredAtUtc >=
                            periodStart &&
                        message.OccurredAtUtc <
                            periodEnd)
                .Select(
                    message =>
                        new
                        {
                            message.Id,
                            message.Payload,
                            message.ProcessedAtUtc,
                            message.LastError
                        })
                .ToListAsync();

        var operations =
            new List<FileOperationOccurredV1>(
                messages.Count);

        var invalidEventCount = 0;

        foreach (var message in messages)
        {
            if (
                eventParser.TryParse(
                    message.Payload,
                    out var operation) &&
                operation is not null
            )
            {
                operations.Add(operation);
                continue;
            }

            invalidEventCount++;

            logger.LogWarning(
                "Skipping invalid outbox event {OutboxMessageId} while generating report for {ReportDate}",
                message.Id,
                reportDate);
        }

        var metrics =
            calculator.Calculate(
                operations,
                messages.Count(
                    message =>
                        message.ProcessedAtUtc is null),
                messages.Count(
                    message =>
                        message.ProcessedAtUtc is null &&
                        !string.IsNullOrWhiteSpace(
                            message.LastError)),
                invalidEventCount);

        var generatedAtUtc =
            timeProvider.GetUtcNow();

        var contentTypeBreakdownJson =
            JsonSerializer.Serialize(
                metrics.UploadedContentTypes,
                SerializerOptions);

        var report =
            await dbContext
                .DailyFileOperationReports
                .SingleOrDefaultAsync(
                    existing =>
                        existing.ReportDate ==
                            reportDate);

        if (report is null)
        {
            report =
                new DailyFileOperationReport(
                    reportDate,
                    metrics.UploadedCount,
                    metrics.DownloadedCount,
                    metrics.DeletedCount,
                    metrics.UploadedBytes,
                    metrics.DownloadedBytes,
                    metrics.PendingOutboxCount,
                    metrics.FailedOutboxCount,
                    metrics.InvalidEventCount,
                    contentTypeBreakdownJson,
                    generatedAtUtc);

            dbContext
                .DailyFileOperationReports
                .Add(report);
        }
        else
        {
            report.Refresh(
                metrics.UploadedCount,
                metrics.DownloadedCount,
                metrics.DeletedCount,
                metrics.UploadedBytes,
                metrics.DownloadedBytes,
                metrics.PendingOutboxCount,
                metrics.FailedOutboxCount,
                metrics.InvalidEventCount,
                contentTypeBreakdownJson,
                generatedAtUtc);
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Generated daily file operations report for {ReportDate}: uploads {UploadedCount}, downloads {DownloadedCount}, deletes {DeletedCount}, pending outbox {PendingOutboxCount}, failed outbox {FailedOutboxCount}, invalid events {InvalidEventCount}",
            reportDate,
            metrics.UploadedCount,
            metrics.DownloadedCount,
            metrics.DeletedCount,
            metrics.PendingOutboxCount,
            metrics.FailedOutboxCount,
            metrics.InvalidEventCount);
    }
}
