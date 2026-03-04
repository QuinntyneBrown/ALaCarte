using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using ALaCarte.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ALaCarte.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlacarteCoreServices(this IServiceCollection services)
    {
        // Add NullLogger fallback if no logging is registered
        services.TryAddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));

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
        services.AddTransient<ScaffoldCommandHandler>();

        return services;
    }
}
