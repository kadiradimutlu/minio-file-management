using FileManagement.Application.Files;
using Microsoft.Extensions.DependencyInjection;

namespace FileManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<
            IFileManagementService,
            FileManagementService>();

        return services;
    }
}