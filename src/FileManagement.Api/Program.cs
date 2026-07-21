using FileManagement.Api.Options;
using FileManagement.Application;
using FileManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();