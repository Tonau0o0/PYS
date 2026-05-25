using Microsoft.Extensions.DependencyInjection;
using PYS.Service.Interfaces;
using PYS.Service.Services;

namespace PYS.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IResourceService, ResourceService>();
        return services;
    }
}
