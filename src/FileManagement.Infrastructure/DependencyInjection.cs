using FileManagement.Application.Abstractions.Persistence;
using FileManagement.Application.Abstractions.Storage;
using FileManagement.Infrastructure.Persistence;
using FileManagement.Infrastructure.Storage.Minio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using global::Minio;

namespace FileManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("PostgreSql");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:PostgreSql is not configured.");
        }

        services.AddDbContext<FileManagementDbContext>(
            options => options.UseNpgsql(connectionString));

        services.AddScoped<
            IStoredFileRepository,
            StoredFileRepository>();

        services.AddOptions<MinioOptions>()
            .Bind(
                configuration.GetSection(
                    MinioOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.Endpoint),
                "Minio:Endpoint is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.AccessKey),
                "Minio:AccessKey is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.SecretKey),
                "Minio:SecretKey is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.BucketName),
                "Minio:BucketName is required.")
            .Validate(
                options =>
                    options.BucketName.Length is >= 3 and <= 63,
                "Minio:BucketName must contain between 3 and 63 characters.")
            .ValidateOnStart();

        services.AddSingleton<IMinioClient>(
            serviceProvider =>
            {
                var options = serviceProvider
                    .GetRequiredService<
                        IOptions<MinioOptions>>()
                    .Value;

                return new MinioClient()
                    .WithEndpoint(options.Endpoint)
                    .WithCredentials(
                        options.AccessKey,
                        options.SecretKey)
                    .WithSSL(options.UseSsl)
                    .Build();
            });

        services.AddSingleton<
            IFileStorageService,
            MinioFileStorageService>();

        return services;
    }
}