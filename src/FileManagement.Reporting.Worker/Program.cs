using FileManagement.Infrastructure.Persistence;
using FileManagement.Reporting.Worker.Endpoints;
using FileManagement.Reporting.Worker.Options;
using FileManagement.Reporting.Worker.Reporting;
using FileManagement.Reporting.Worker.Security;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

const string ApplicationName =
    "FileManagement.Reporting.Worker";

var builder =
    WebApplication.CreateBuilder(args);

var seqServerUrl =
    builder.Configuration[
        "Seq:ServerUrl"];

builder.Services.AddSerilog(
    (
        services,
        loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(
                builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty(
                "Application",
                ApplicationName)
            .Enrich.WithProperty(
                "Environment",
                builder.Environment
                    .EnvironmentName)
            .WriteTo.Console();

        if (
            !string.IsNullOrWhiteSpace(
                seqServerUrl)
        )
        {
            loggerConfiguration
                .WriteTo.Seq(
                    seqServerUrl);
        }
    });

var connectionString =
    builder.Configuration
        .GetConnectionString(
            "PostgreSql");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:PostgreSql is not configured.");
}

builder.Services
    .AddOptions<ReportingOptions>()
    .Bind(
        builder.Configuration.GetSection(
            ReportingOptions.SectionName))
    .Validate(
        static options =>
            !string.IsNullOrWhiteSpace(
                options.DailyReportCron),
        "Reporting:DailyReportCron is required.")
    .Validate(
        static options =>
            options.WorkerCount
                is >= 1 and <= 16,
        "Reporting:WorkerCount must be between 1 and 16.")
    .Validate(
        static options =>
            options.MaxManualLookbackDays
                is >= 1 and <= 36500,
        "Reporting:MaxManualLookbackDays must be between 1 and 36500.")
    .Validate(
        static options =>
            options.MaxResultCount
                is >= 1 and <= 1000,
        "Reporting:MaxResultCount must be between 1 and 1000.")
    .ValidateOnStart();

builder.Services
    .AddOptions<ReportingDashboardOptions>()
    .Bind(
        builder.Configuration.GetSection(
            ReportingDashboardOptions
                .SectionName))
    .Validate(
        static options =>
            options.Username.Trim().Length >= 3,
        "Dashboard:Username must contain at least 3 characters.")
    .Validate(
        static options =>
            options.Password.Length >= 16,
        "Dashboard:Password must contain at least 16 characters.")
    .ValidateOnStart();

builder.Services
    .AddPooledDbContextFactory<
        FileManagementDbContext>(
        options =>
            options.UseNpgsql(
                connectionString));

builder.Services.AddHangfire(
    configuration =>
        configuration
            .SetDataCompatibilityLevel(
                CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                options =>
                    options.UseNpgsqlConnection(
                        connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire",
                    PrepareSchemaIfNecessary =
                        true,
                    AllowDegradedModeWithoutStorage =
                        false,
                    DistributedLockTimeout =
                        TimeSpan.FromMinutes(10)
                }));

var reportingOptions =
    builder.Configuration
        .GetSection(
            ReportingOptions.SectionName)
        .Get<ReportingOptions>() ??
    new ReportingOptions();

builder.Services.AddHangfireServer(
    options =>
    {
        options.Queues =
        [
            "reporting"
        ];
        options.WorkerCount =
            reportingOptions.WorkerCount;
    });

builder.Services
    .AddAuthentication(
        ReportingBasicAuthenticationHandler
            .SchemeName)
    .AddScheme<
        AuthenticationSchemeOptions,
        ReportingBasicAuthenticationHandler>(
        ReportingBasicAuthenticationHandler
            .SchemeName,
        _ =>
        {
        });

builder.Services.AddAuthorization(
    options =>
    {
        options.AddPolicy(
            ReportingBasicAuthenticationHandler
                .AdministratorRole,
            policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireRole(
                        ReportingBasicAuthenticationHandler
                            .AdministratorRole));
    });

builder.Services.AddSingleton<TimeProvider>(
    TimeProvider.System);
builder.Services.AddSingleton<
    ReportingDashboardCredentialValidator>();
builder.Services.AddSingleton<
    ReportingDashboardAuthorizationFilter>();
builder.Services.AddSingleton<
    FileOperationEventParser>();
builder.Services.AddSingleton<
    DailyFileOperationsReportCalculator>();
builder.Services.AddTransient<
    DailyFileOperationsReportJob>();

builder.Services.AddHealthChecks();

var app =
    builder.Build();

app.Logger.LogInformation(
    "Starting {ApplicationName}",
    ApplicationName);

await using (
    var startupScope =
        app.Services.CreateAsyncScope()
)
{
    app.Logger.LogInformation(
        "Applying File Management database migrations");

    var dbContextFactory =
        startupScope.ServiceProvider
            .GetRequiredService<
                IDbContextFactory<
                    FileManagementDbContext>>();

    await using var dbContext =
        await dbContextFactory
            .CreateDbContextAsync();

    await dbContext.Database.MigrateAsync();

    app.Logger.LogInformation(
        "Database migrations completed");
}

app.UseSerilogRequestLogging(
    options =>
    {
        options.GetLevel =
            (
                httpContext,
                _,
                exception) =>
            {
                if (
                    exception is not null ||
                    httpContext.Response.StatusCode >=
                        StatusCodes
                            .Status500InternalServerError
                )
                {
                    return LogEventLevel.Error;
                }

                if (
                    httpContext.Response.StatusCode >=
                        StatusCodes
                            .Status400BadRequest
                )
                {
                    return LogEventLevel.Warning;
                }

                if (
                    httpContext.Request.Path
                        .StartsWithSegments(
                            "/health")
                )
                {
                    return LogEventLevel.Debug;
                }

                return LogEventLevel.Information;
            };
    });

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard(
    "/hangfire",
    new DashboardOptions
    {
        Authorization =
        [
            app.Services.GetRequiredService<
                ReportingDashboardAuthorizationFilter>()
        ],
        IsReadOnlyFunc = _ => true,
        DashboardTitle =
            "File Management Reporting"
    });

app.MapHealthChecks("/health");
app.MapReportingEndpoints();

var recurringJobManager =
    app.Services.GetRequiredService<
        IRecurringJobManager>();

recurringJobManager.AddOrUpdate<
    DailyFileOperationsReportJob>(
    ReportingOptions.DailyReportJobId,
    job =>
        job.GeneratePreviousDayAsync(),
    reportingOptions.DailyReportCron,
    new RecurringJobOptions
    {
        TimeZone =
            TimeZoneInfo.Utc
    });

app.Run();

public partial class Program;
