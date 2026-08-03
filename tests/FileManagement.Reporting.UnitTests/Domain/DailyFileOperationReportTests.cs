using FileManagement.Domain.Entities;

namespace FileManagement.Reporting.UnitTests.Domain;

public sealed class DailyFileOperationReportTests
{
    [Fact]
    public void Refresh_UpdatesMetricsAndPreservesCreationTime()
    {
        var createdAt =
            new DateTimeOffset(
                2026,
                8,
                1,
                1,
                0,
                0,
                TimeSpan.Zero);

        var updatedAt =
            createdAt.AddHours(2);

        var report =
            CreateReport(
                createdAt);

        report.Refresh(
            uploadedCount: 2,
            downloadedCount: 3,
            deletedCount: 4,
            uploadedBytes: 500,
            downloadedBytes: 700,
            pendingOutboxCount: 1,
            failedOutboxCount: 1,
            invalidEventCount: 2,
            contentTypeBreakdownJson:
                "{\"application/pdf\":2}",
            generatedAtUtc: updatedAt);

        Assert.Equal(2, report.UploadedCount);
        Assert.Equal(3, report.DownloadedCount);
        Assert.Equal(4, report.DeletedCount);
        Assert.Equal(500, report.UploadedBytes);
        Assert.Equal(700, report.DownloadedBytes);
        Assert.Equal(1, report.PendingOutboxCount);
        Assert.Equal(1, report.FailedOutboxCount);
        Assert.Equal(2, report.InvalidEventCount);
        Assert.Equal(
            "{\"application/pdf\":2}",
            report.ContentTypeBreakdownJson);
        Assert.Equal(
            createdAt,
            report.CreatedAtUtc);
        Assert.Equal(
            updatedAt,
            report.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WithNegativeMetric_Throws()
    {
        var action = () =>
            new DailyFileOperationReport(
                new DateOnly(2026, 8, 1),
                uploadedCount: -1,
                downloadedCount: 0,
                deletedCount: 0,
                uploadedBytes: 0,
                downloadedBytes: 0,
                pendingOutboxCount: 0,
                failedOutboxCount: 0,
                invalidEventCount: 0,
                contentTypeBreakdownJson: "{}",
                generatedAtUtc:
                    DateTimeOffset.UtcNow);

        Assert.Throws<
            ArgumentOutOfRangeException>(
            action);
    }

    [Fact]
    public void Refresh_WithBlankContentBreakdown_Throws()
    {
        var report =
            CreateReport(
                DateTimeOffset.UtcNow);

        var action = () =>
            report.Refresh(
                uploadedCount: 0,
                downloadedCount: 0,
                deletedCount: 0,
                uploadedBytes: 0,
                downloadedBytes: 0,
                pendingOutboxCount: 0,
                failedOutboxCount: 0,
                invalidEventCount: 0,
                contentTypeBreakdownJson: " ",
                generatedAtUtc:
                    DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(
            action);
    }

    private static DailyFileOperationReport
        CreateReport(
            DateTimeOffset generatedAtUtc)
    {
        return new DailyFileOperationReport(
            new DateOnly(2026, 8, 1),
            uploadedCount: 1,
            downloadedCount: 1,
            deletedCount: 1,
            uploadedBytes: 100,
            downloadedBytes: 100,
            pendingOutboxCount: 0,
            failedOutboxCount: 0,
            invalidEventCount: 0,
            contentTypeBreakdownJson:
                "{\"application/pdf\":1}",
            generatedAtUtc:
                generatedAtUtc);
    }
}
