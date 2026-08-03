using FileManagement.Contracts.Files;
using FileManagement.Reporting.Worker.Reporting;

namespace FileManagement.Reporting.UnitTests.Reporting;

public sealed class DailyFileOperationsReportCalculatorTests
{
    private readonly
        DailyFileOperationsReportCalculator
        _calculator = new();

    [Fact]
    public void Calculate_WithMixedOperations_AggregatesMetrics()
    {
        FileOperationOccurredV1[] operations =
        [
            CreateOperation(
                FileOperationKinds.Uploaded,
                "Application/PDF",
                100),
            CreateOperation(
                FileOperationKinds.Uploaded,
                " application/pdf ",
                250),
            CreateOperation(
                FileOperationKinds.Downloaded,
                "image/png",
                100),
            CreateOperation(
                FileOperationKinds.Deleted,
                "image/png",
                100)
        ];

        var result =
            _calculator.Calculate(
                operations,
                pendingOutboxCount: 2,
                failedOutboxCount: 1,
                invalidEventCount: 3);

        Assert.Equal(2, result.UploadedCount);
        Assert.Equal(1, result.DownloadedCount);
        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(350, result.UploadedBytes);
        Assert.Equal(100, result.DownloadedBytes);
        Assert.Equal(2, result.PendingOutboxCount);
        Assert.Equal(1, result.FailedOutboxCount);
        Assert.Equal(3, result.InvalidEventCount);
        Assert.Equal(
            2,
            result.UploadedContentTypes[
                "application/pdf"]);
    }

    [Fact]
    public void Calculate_WithNoOperations_ReturnsZeroMetrics()
    {
        var result =
            _calculator.Calculate(
                [],
                pendingOutboxCount: 0,
                failedOutboxCount: 0,
                invalidEventCount: 0);

        Assert.Equal(0, result.UploadedCount);
        Assert.Equal(0, result.DownloadedCount);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(
            result.UploadedContentTypes);
    }

    [Fact]
    public void Calculate_WithNegativeDiagnosticCount_Throws()
    {
        var action = () =>
            _calculator.Calculate(
                [],
                pendingOutboxCount: -1,
                failedOutboxCount: 0,
                invalidEventCount: 0);

        Assert.Throws<
            ArgumentOutOfRangeException>(
            action);
    }

    private static FileOperationOccurredV1
        CreateOperation(
            string operation,
            string contentType,
            long sizeBytes)
    {
        return new FileOperationOccurredV1(
            Guid.NewGuid(),
            operation,
            "report.pdf",
            contentType,
            sizeBytes,
            null,
            null,
            "user-123");
    }
}
