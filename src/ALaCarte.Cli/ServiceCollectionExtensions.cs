using ALaCarte.Cli.Abstractions;
using ALaCarte.Cli.Commands;
using ALaCarte.Cli.Options;
using ALaCarte.Cli.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ALaCarte.Cli;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlacarteServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<AlacarteOptions>(configuration.GetSection(AlacarteOptions.SectionName));
        services.Configure<GitOptions>(configuration.GetSection(GitOptions.SectionName));
        services.Configure<DotNetOptions>(configuration.GetSection(DotNetOptions.SectionName));
        services.Configure<AngularOptions>(configuration.GetSection(AngularOptions.SectionName));

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

        return services;
    }
}
