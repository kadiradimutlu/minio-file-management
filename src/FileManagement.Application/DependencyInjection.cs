using FileManagement.Application.Files;
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
            IFileManagementService,
            FileManagementService>();

        return services;
    }
}
