using FileManagement.Api.Options;
using FileManagement.Application;
using FileManagement.Application.Abstractions.Storage;
using FileManagement.Infrastructure;
using FileManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

const string WebClientCorsPolicy =
    "WebClient";

var builder =
    WebApplication.CreateBuilder(args);

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

await using (
    var startupScope =
        app.Services.CreateAsyncScope()
)
{
    var dbContext =
        startupScope.ServiceProvider
            .GetRequiredService<
                FileManagementDbContext>();

    await dbContext.Database.MigrateAsync();

    var storageService =
        startupScope.ServiceProvider
            .GetRequiredService<
                IFileStorageService>();

    await storageService
        .EnsureBucketExistsAsync();
}

app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/openapi/v1.json",
        "File Management API v1");

    options.RoutePrefix = "swagger";
});

app.UseCors(
    WebClientCorsPolicy);

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();