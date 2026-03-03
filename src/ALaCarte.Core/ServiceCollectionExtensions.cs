using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using ALaCarte.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ALaCarte.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlacarteCoreServices(this IServiceCollection services)
    {
        // Register core services
        services.AddSingleton<IFileSystem, FileSystemService>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();

        // Register domain services
        services.AddTransient<IGitService, GitService>();
        services.AddTransient<IProjectDiscoveryService, ProjectDiscoveryService>();
        services.AddTransient<IDotNetService, DotNetService>();
        services.AddTransient<IAngularService, AngularService>();

        // Register command handlers
        services.AddTransient<InitCommandHandler>();
        services.AddTransient<InstallSkillCommandHandler>();

        return services;
    }
}
