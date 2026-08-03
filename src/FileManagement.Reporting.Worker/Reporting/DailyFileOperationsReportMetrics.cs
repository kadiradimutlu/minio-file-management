namespace FileManagement.Reporting.Worker.Reporting;

public sealed record DailyFileOperationsReportMetrics(
    int UploadedCount,
    int DownloadedCount,
    int DeletedCount,
    long UploadedBytes,
    long DownloadedBytes,
    int PendingOutboxCount,
    int FailedOutboxCount,
    int InvalidEventCount,
    IReadOnlyDictionary<string, int>
        UploadedContentTypes);
