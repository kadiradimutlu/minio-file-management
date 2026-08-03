using FileManagement.Application.Files;
using FileManagement.Application.Abstractions.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace FileManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(
            TimeProvider.System);

        services.AddScoped<
            FileManagementService>();

        services.AddScoped<
            IFileManagementService>(
            serviceProvider =>
                new CachedFileManagementService(
                    serviceProvider.GetRequiredService<
                        FileManagementService>(),
                    serviceProvider.GetRequiredService<
                        IFileMetadataCache>()));

        return services;
    }
}
