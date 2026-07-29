using System.Security.Claims;
using System.Text;
using FileManagement.Api.Middleware;
using FileManagement.Api.OpenApi;
using FileManagement.Api.Options;
using FileManagement.Application;
using FileManagement.Application.Abstractions.Storage;
using FileManagement.Infrastructure;
using FileManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

var allowedOrigins =
    builder.Configuration
        .GetSection(
            "Cors:AllowedOrigins")
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
                    .WithOrigins(
                        allowedOrigins)
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

var jwtSection =
    builder.Configuration.GetSection(
        JwtOptions.SectionName);

var jwtOptions =
    jwtSection.Get<JwtOptions>() ??
    new JwtOptions();

if (
    string.IsNullOrWhiteSpace(
        jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(
        jwtOptions.Audience) ||
    jwtOptions.SigningKey.Length < 32
)
{
    throw new InvalidOperationException(
        "JWT validation configuration is invalid.");
}

builder.Services.AddOptions<JwtOptions>()
    .Bind(jwtSection)
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(
                options.Issuer),
        "Jwt:Issuer is required.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(
                options.Audience),
        "Jwt:Audience is required.")
    .Validate(
        options =>
            options.SigningKey.Length >= 32,
        "Jwt:SigningKey must contain at least 32 characters.")
    .ValidateOnStart();

builder.Services
    .AddAuthentication(
        JwtBearerDefaults
            .AuthenticationScheme)
    .AddJwtBearer(
        options =>
        {
            options.MapInboundClaims =
                false;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer =
                        jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience =
                        jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey =
                        true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8
                                .GetBytes(
                                    jwtOptions
                                        .SigningKey)),
                    ClockSkew =
                        TimeSpan.FromSeconds(
                            30),
                    NameClaimType =
                        ClaimTypes.Name,
                    RoleClaimType =
                        ClaimTypes.Role
                };
        });

builder.Services.AddAuthorization();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddOpenApi(
    options =>
    {
        options.AddDocumentTransformer<
            BearerSecuritySchemeTransformer>();

        options.AddOperationTransformer<
            AuthorizationOperationTransformer>();
    });

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

                diagnosticContext.Set(
                    "UserId",
                    httpContext.User
                        .FindFirstValue(
                            ClaimTypes.NameIdentifier) ??
                    "anonymous");

                diagnosticContext.Set(
                    "UserName",
                    httpContext.User
                        .Identity
                        ?.Name ??
                    "anonymous");
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

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
