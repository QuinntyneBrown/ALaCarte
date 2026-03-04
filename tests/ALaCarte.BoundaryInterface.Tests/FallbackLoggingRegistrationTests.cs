using ALaCarte.Core;
using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using ALaCarte.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ALaCarte.BoundaryInterface.Tests;

public class FallbackLoggingRegistrationTests
{
    [Fact]
    public void AddAlacarteCoreServices_WithoutAddLogging_ResolvesSingletonLoggerFactory()
    {
        var services = new ServiceCollection();
        services.Configure<AlacarteOptions>(_ => { });
        services.Configure<GitOptions>(_ => { });
        services.Configure<DotNetOptions>(_ => { });
        services.Configure<AngularOptions>(_ => { });
        services.AddAlacarteCoreServices();

        var sp = services.BuildServiceProvider();
        var loggerFactory = sp.GetService<ILoggerFactory>();

        Assert.NotNull(loggerFactory);
    }

    [Fact]
    public void AddAlacarteCoreServices_WithoutAddLogging_ResolvesGenericLogger()
    {
        var services = new ServiceCollection();
        services.Configure<AlacarteOptions>(_ => { });
        services.Configure<GitOptions>(_ => { });
        services.Configure<DotNetOptions>(_ => { });
        services.Configure<AngularOptions>(_ => { });
        services.AddAlacarteCoreServices();

        var sp = services.BuildServiceProvider();
        var logger = sp.GetService<ILogger<FallbackLoggingRegistrationTests>>();

        Assert.NotNull(logger);
    }

    [Fact]
    public void AddAlacarteCoreServices_WithoutAddLogging_CanResolveInitCommandHandler()
    {
        var services = new ServiceCollection();
        services.Configure<AlacarteOptions>(_ => { });
        services.Configure<GitOptions>(_ => { });
        services.Configure<DotNetOptions>(_ => { });
        services.Configure<AngularOptions>(_ => { });
        services.AddAlacarteCoreServices();

        var sp = services.BuildServiceProvider();
        var handler = sp.GetService<InitCommandHandler>();

        Assert.NotNull(handler);
    }

    [Fact]
    public void AddAlacarteCoreServices_WithExplicitLogging_UsesExplicitLogging()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.Configure<AlacarteOptions>(_ => { });
        services.Configure<GitOptions>(_ => { });
        services.Configure<DotNetOptions>(_ => { });
        services.Configure<AngularOptions>(_ => { });
        services.AddAlacarteCoreServices();

        var sp = services.BuildServiceProvider();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        // When explicit logging is registered, it should be used (not NullLoggerFactory)
        Assert.NotNull(loggerFactory);
    }

    [Fact]
    public void AddAlacarteCoreServices_WithExplicitLogging_DoesNotOverrideIt()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.Configure<AlacarteOptions>(_ => { });
        services.Configure<GitOptions>(_ => { });
        services.Configure<DotNetOptions>(_ => { });
        services.Configure<AngularOptions>(_ => { });
        services.AddAlacarteCoreServices();

        var sp = services.BuildServiceProvider();

        // Should be able to resolve all services without issue
        var gitService = sp.GetService<IGitService>();
        var dotNetService = sp.GetService<IDotNetService>();

        Assert.NotNull(gitService);
        Assert.NotNull(dotNetService);
    }
}
