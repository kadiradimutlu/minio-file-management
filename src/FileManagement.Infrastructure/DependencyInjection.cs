using FileManagement.Application.Abstractions.Persistence;
using FileManagement.Application.Abstractions.Storage;
using FileManagement.Application.Abstractions.Caching;
using FileManagement.Infrastructure.Caching;
using FileManagement.Infrastructure.Persistence;
using FileManagement.Infrastructure.Persistence.Outbox;
using FileManagement.Infrastructure.Storage.Minio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
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
        services.AddScoped<
            IFileOperationOutbox,
            FileOperationOutbox>();

        AddFileMetadataCache(
            services,
            configuration);

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

    private static void AddFileMetadataCache(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheSection =
            configuration.GetSection(
                FileMetadataCacheOptions.SectionName);

        services.AddOptions<
                FileMetadataCacheOptions>()
            .Bind(cacheSection)
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.KeyPrefix),
                "FileCache:KeyPrefix is required.")
            .Validate(
                options =>
                    options.DetailTtlSeconds
                        is >= 1 and <= 86400,
                "FileCache:DetailTtlSeconds must be between 1 and 86400.")
            .Validate(
                options =>
                    options.ListTtlSeconds
                        is >= 1 and <= 86400,
                "FileCache:ListTtlSeconds must be between 1 and 86400.")
            .ValidateOnStart();

        var enabledValue =
            configuration[
                $"{FileMetadataCacheOptions.SectionName}:Enabled"];

        var cacheEnabled =
            bool.TryParse(
                enabledValue,
                out var enabled) &&
            enabled;

        if (!cacheEnabled)
        {
            services.AddSingleton<
                IFileMetadataCache,
                NullFileMetadataCache>();

            return;
        }

        var redisSection =
            configuration.GetSection(
                RedisCacheConnectionOptions
                    .SectionName);

        var redisOptions =
            new RedisCacheConnectionOptions();

        redisSection.Bind(
            redisOptions);

        ValidateRedisOptions(
            redisOptions);

        services.AddStackExchangeRedisCache(
            options =>
            {
                var redisConfiguration =
                    new ConfigurationOptions
                    {
                        AbortOnConnectFail = false,
                        ConnectTimeout =
                            redisOptions
                                .ConnectTimeoutMilliseconds,
                        SyncTimeout =
                            redisOptions
                                .OperationTimeoutMilliseconds,
                        AsyncTimeout =
                            redisOptions
                                .OperationTimeoutMilliseconds,
                        Password =
                            redisOptions.Password,
                        Ssl =
                            redisOptions.UseSsl
                    };

                redisConfiguration.EndPoints.Add(
                    redisOptions.Host,
                    redisOptions.Port);

                options.ConfigurationOptions =
                    redisConfiguration;
            });

        services.AddSingleton<
            IFileMetadataCache,
            RedisFileMetadataCache>();
    }

    private static void ValidateRedisOptions(
        RedisCacheConnectionOptions options)
    {
        if (
            string.IsNullOrWhiteSpace(
                options.Host)
        )
        {
            throw new InvalidOperationException(
                "Redis:Host is required when FileCache:Enabled is true.");
        }

        if (
            options.Port is < 1 or > 65535
        )
        {
            throw new InvalidOperationException(
                "Redis:Port must be between 1 and 65535.");
        }

        if (
            string.IsNullOrWhiteSpace(
                options.Password)
        )
        {
            throw new InvalidOperationException(
                "Redis:Password is required when FileCache:Enabled is true.");
        }

        if (
            options.ConnectTimeoutMilliseconds
                is < 100 or > 30000
        )
        {
            throw new InvalidOperationException(
                "Redis:ConnectTimeoutMilliseconds must be between 100 and 30000.");
        }

        if (
            options.OperationTimeoutMilliseconds
                is < 100 or > 30000
        )
        {
            throw new InvalidOperationException(
                "Redis:OperationTimeoutMilliseconds must be between 100 and 30000.");
        }
    }
}
