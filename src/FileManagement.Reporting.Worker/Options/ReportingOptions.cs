namespace FileManagement.Reporting.Worker.Options;

public sealed class ReportingOptions
{
    public const string SectionName =
        "Reporting";

    public const string DailyReportJobId =
        "daily-file-operations-report-v1";

    public string DailyReportCron { get; set; } =
        "0 1 * * *";

    public int WorkerCount { get; set; } = 2;

    public int MaxManualLookbackDays
    {
        get;
        set;
    } = 3650;

    public int MaxResultCount { get; set; } =
        100;
}
