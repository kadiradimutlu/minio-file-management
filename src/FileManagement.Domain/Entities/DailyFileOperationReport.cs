namespace FileManagement.Domain.Entities;

public sealed class DailyFileOperationReport
{
    private DailyFileOperationReport()
    {
    }

    public DailyFileOperationReport(
        DateOnly reportDate,
        int uploadedCount,
        int downloadedCount,
        int deletedCount,
        long uploadedBytes,
        long downloadedBytes,
        int pendingOutboxCount,
        int failedOutboxCount,
        int invalidEventCount,
        string contentTypeBreakdownJson,
        DateTimeOffset generatedAtUtc)
    {
        ReportDate = reportDate;
        CreatedAtUtc =
            ValidateGeneratedAt(
                generatedAtUtc);

        Refresh(
            uploadedCount,
            downloadedCount,
            deletedCount,
            uploadedBytes,
            downloadedBytes,
            pendingOutboxCount,
            failedOutboxCount,
            invalidEventCount,
            contentTypeBreakdownJson,
            generatedAtUtc);
    }

    public DateOnly ReportDate { get; private set; }

    public int UploadedCount { get; private set; }

    public int DownloadedCount { get; private set; }

    public int DeletedCount { get; private set; }

    public long UploadedBytes { get; private set; }

    public long DownloadedBytes { get; private set; }

    public int PendingOutboxCount { get; private set; }

    public int FailedOutboxCount { get; private set; }

    public int InvalidEventCount { get; private set; }

    public string ContentTypeBreakdownJson
    {
        get;
        private set;
    } = "{}";

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Refresh(
        int uploadedCount,
        int downloadedCount,
        int deletedCount,
        long uploadedBytes,
        long downloadedBytes,
        int pendingOutboxCount,
        int failedOutboxCount,
        int invalidEventCount,
        string contentTypeBreakdownJson,
        DateTimeOffset generatedAtUtc)
    {
        UploadedCount =
            ValidateCount(
                uploadedCount,
                nameof(uploadedCount));
        DownloadedCount =
            ValidateCount(
                downloadedCount,
                nameof(downloadedCount));
        DeletedCount =
            ValidateCount(
                deletedCount,
                nameof(deletedCount));
        UploadedBytes =
            ValidateBytes(
                uploadedBytes,
                nameof(uploadedBytes));
        DownloadedBytes =
            ValidateBytes(
                downloadedBytes,
                nameof(downloadedBytes));
        PendingOutboxCount =
            ValidateCount(
                pendingOutboxCount,
                nameof(pendingOutboxCount));
        FailedOutboxCount =
            ValidateCount(
                failedOutboxCount,
                nameof(failedOutboxCount));
        InvalidEventCount =
            ValidateCount(
                invalidEventCount,
                nameof(invalidEventCount));

        if (
            string.IsNullOrWhiteSpace(
                contentTypeBreakdownJson)
        )
        {
            throw new ArgumentException(
                "Content type breakdown JSON is required.",
                nameof(contentTypeBreakdownJson));
        }

        ContentTypeBreakdownJson =
            contentTypeBreakdownJson.Trim();
        UpdatedAtUtc =
            ValidateGeneratedAt(
                generatedAtUtc);
    }

    private static int ValidateCount(
        int value,
        string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Report counts cannot be negative.");
        }

        return value;
    }

    private static long ValidateBytes(
        long value,
        string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Report byte totals cannot be negative.");
        }

        return value;
    }

    private static DateTimeOffset
        ValidateGeneratedAt(
            DateTimeOffset value)
    {
        if (value == default)
        {
            throw new ArgumentException(
                "Generated time is required.",
                nameof(value));
        }

        return value.ToUniversalTime();
    }
}
