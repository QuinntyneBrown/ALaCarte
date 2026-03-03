using ALaCarte.Core;
using ALaCarte.Core.Options;
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
        services.AddAlacarteCoreServices();

        return services;
    }
}
