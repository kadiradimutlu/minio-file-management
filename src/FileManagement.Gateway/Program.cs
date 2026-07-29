using FileManagement.Gateway.Middleware;
using Serilog;
using Serilog.Events;

const string ApplicationName =
    "FileManagement.Gateway";

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

var reverseProxySection =
    builder.Configuration.GetSection(
        "ReverseProxy");

if (!reverseProxySection.Exists())
{
    throw new InvalidOperationException(
        "ReverseProxy configuration is required.");
}

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(
        reverseProxySection);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.Logger.LogInformation(
    "Starting {ApplicationName}",
    ApplicationName);

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
                    httpContext.Response
                        .StatusCode >=
                    StatusCodes
                        .Status500InternalServerError
                )
                {
                    return LogEventLevel.Error;
                }

                if (
                    httpContext.Response
                        .StatusCode >=
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

app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();
