using System.Globalization;
using System.Text.Json;
using FileManagement.Infrastructure.Persistence;
using FileManagement.Reporting.Worker.Options;
using FileManagement.Reporting.Worker.Reporting;
using FileManagement.Reporting.Worker.Security;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FileManagement.Reporting.Worker.Endpoints;

public static class ReportingEndpoints
{
    private static readonly JsonSerializerOptions
        SerializerOptions =
            new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder
        MapReportingEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints.MapGroup(
                    "/api/reports")
                .RequireAuthorization(
                    ReportingBasicAuthenticationHandler
                        .AdministratorRole);

        group.MapGet(
            "/daily",
            GetDailyReportsAsync);

        group.MapPost(
            "/daily/{reportDate}/enqueue",
            EnqueueDailyReport);

        return endpoints;
    }

    private static async Task<IResult>
        GetDailyReportsAsync(
            int? limit,
            IDbContextFactory<FileManagementDbContext>
                dbContextFactory,
            IOptions<ReportingOptions> options)
    {
        var requestedLimit =
            limit ?? 30;

        if (
            requestedLimit < 1 ||
            requestedLimit >
                options.Value.MaxResultCount
        )
        {
            return Results.ValidationProblem(
                new Dictionary<
                    string,
                    string[]>
                {
                    ["limit"] =
                    [
                        $"Limit must be between 1 and {options.Value.MaxResultCount}."
                    ]
                });
        }

        await using var dbContext =
            await dbContextFactory
                .CreateDbContextAsync();

        var reports =
            await dbContext
                .DailyFileOperationReports
                .AsNoTracking()
                .OrderByDescending(
                    report =>
                        report.ReportDate)
                .Take(requestedLimit)
                .ToListAsync();

        return Results.Ok(
            reports.Select(
                report =>
                    new
                    {
                        report.ReportDate,
                        report.UploadedCount,
                        report.DownloadedCount,
                        report.DeletedCount,
                        report.UploadedBytes,
                        report.DownloadedBytes,
                        report.PendingOutboxCount,
                        report.FailedOutboxCount,
                        report.InvalidEventCount,
                        UploadedContentTypes =
                            JsonSerializer.Deserialize<
                                Dictionary<
                                    string,
                                    int>>(
                                report
                                    .ContentTypeBreakdownJson,
                                SerializerOptions) ??
                            [],
                        report.CreatedAtUtc,
                        report.UpdatedAtUtc
                    }));
    }

    private static IResult
        EnqueueDailyReport(
            string reportDate,
            IBackgroundJobClient
                backgroundJobClient,
            IOptions<ReportingOptions> options,
            TimeProvider timeProvider)
    {
        if (
            !DateOnly.TryParseExact(
                reportDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate)
        )
        {
            return Results.ValidationProblem(
                new Dictionary<
                    string,
                    string[]>
                {
                    ["reportDate"] =
                    [
                        "Report date must use yyyy-MM-dd format."
                    ]
                });
        }

        var todayUtc =
            DateOnly.FromDateTime(
                timeProvider.GetUtcNow()
                    .UtcDateTime);

        var oldestAllowedDate =
            todayUtc.AddDays(
                -options.Value
                    .MaxManualLookbackDays);

        if (
            parsedDate > todayUtc ||
            parsedDate < oldestAllowedDate
        )
        {
            return Results.ValidationProblem(
                new Dictionary<
                    string,
                    string[]>
                {
                    ["reportDate"] =
                    [
                        $"Report date must be between {oldestAllowedDate:yyyy-MM-dd} and {todayUtc:yyyy-MM-dd}."
                    ]
                });
        }

        var dateTimeUtc =
            parsedDate.ToDateTime(
                TimeOnly.MinValue,
                DateTimeKind.Utc);

        var jobId =
            backgroundJobClient.Enqueue<
                DailyFileOperationsReportJob>(
                job =>
                    job.GenerateAsync(
                        dateTimeUtc));

        return Results.Accepted(
            $"/hangfire/jobs/details/{jobId}",
            new
            {
                JobId = jobId,
                ReportDate = parsedDate
            });
    }
}
