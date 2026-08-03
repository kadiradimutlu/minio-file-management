namespace FileManagement.Reporting.Worker.Options;

public sealed class ReportingDashboardOptions
{
    public const string SectionName =
        "Dashboard";

    public string Username { get; set; } =
        string.Empty;

    public string Password { get; set; } =
        string.Empty;
}
