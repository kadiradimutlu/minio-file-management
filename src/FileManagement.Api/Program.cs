using FileManagement.Api.Middleware;
using FileManagement.Api.Options;
using FileManagement.Application;
using FileManagement.Application.Abstractions.Storage;
using FileManagement.Infrastructure;
using FileManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

const string WebClientCorsPolicy =
    "WebClient";

const string ApplicationName =
    "FileManagement.Api";

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

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins must contain at least one origin.");
}

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            WebClientCorsPolicy,
            policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
    });

builder.Services.AddOptions<FileUploadOptions>()
    .Bind(
        builder.Configuration.GetSection(
            FileUploadOptions.SectionName))
    .Validate(
        options =>
            options.MaxFileSizeBytes > 0,
        "FileUpload:MaxFileSizeBytes must be greater than zero.")
    .Validate(
        options =>
            options.AllowedExtensions.Length > 0,
        "FileUpload:AllowedExtensions must not be empty.")
    .Validate(
        options =>
            options.AllowedContentTypes.Length > 0,
        "FileUpload:AllowedContentTypes must not be empty.")
    .ValidateOnStart();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

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

    var dbContext =
        startupScope.ServiceProvider
            .GetRequiredService<
                FileManagementDbContext>();

    await dbContext.Database.MigrateAsync();

    app.Logger.LogInformation(
        "Database migrations completed");

    var storageService =
        startupScope.ServiceProvider
            .GetRequiredService<
                IFileStorageService>();

    app.Logger.LogInformation(
        "Ensuring MinIO bucket {BucketName} exists",
        storageService.BucketName);

    await storageService
        .EnsureBucketExistsAsync();

    app.Logger.LogInformation(
        "MinIO bucket {BucketName} is ready",
        storageService.BucketName);
}

app.UseMiddleware<
    CorrelationIdMiddleware>();

app.UseSerilogRequestLogging(
    options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000} ms";

        options.GetLevel =
            (
                httpContext,
                _,
                exception) =>
            {
                if (
                    exception is not null ||
                    httpContext.Response.StatusCode >=
                        StatusCodes.Status500InternalServerError
                )
                {
                    return LogEventLevel.Error;
                }

                if (
                    httpContext.Response.StatusCode >=
                    StatusCodes.Status400BadRequest
                )
                {
                    return LogEventLevel.Warning;
                }

                if (
                    httpContext.Request.Path
                        .StartsWithSegments("/health")
                )
                {
                    return LogEventLevel.Debug;
                }

                return LogEventLevel.Information;
            };

        options.EnrichDiagnosticContext =
            (
                diagnosticContext,
                httpContext) =>
            {
                diagnosticContext.Set(
                    "RequestHost",
                    httpContext.Request
                        .Host.Value);

                diagnosticContext.Set(
                    "RequestScheme",
                    httpContext.Request
                        .Scheme);

                diagnosticContext.Set(
                    "CorrelationId",
                    httpContext.TraceIdentifier);
            };
    });

app.UseSwaggerUI(
    options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "File Management API v1");

        options.RoutePrefix =
            "swagger";
    });

app.UseCors(
    WebClientCorsPolicy);

app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
