using ALaCarte.Core;
using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using ALaCarte.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ALaCarte.Core.UnitTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAlacarteCoreServices_RegistersFileSystem()
    {
        var services = CreateServiceProvider();
        services.GetService<IFileSystem>().Should().NotBeNull();
        services.GetService<IFileSystem>().Should().BeOfType<FileSystemService>();
    }

    [Fact]
    public void AddAlacarteCoreServices_RegistersProcessRunner()
    {
        var services = CreateServiceProvider();
        services.GetService<IProcessRunner>().Should().NotBeNull();
        services.GetService<IProcessRunner>().Should().BeOfType<ProcessRunner>();
    }

    [Fact]
    public void AddAlacarteCoreServices_RegistersGitService()
    {
        var services = CreateServiceProvider();
        services.GetService<IGitService>().Should().NotBeNull();
        services.GetService<IGitService>().Should().BeOfType<GitService>();
    }

    [Fact]
    public void AddAlacarteCoreServices_RegistersProjectDiscoveryService()
    {
        var services = CreateServiceProvider();
        services.GetService<IProjectDiscoveryService>().Should().NotBeNull();
        services.GetService<IProjectDiscoveryService>().Should().BeOfType<ProjectDiscoveryService>();
    }

    [Fact]
    public void AddAlacarteCoreServices_RegistersDotNetService()
    {
        var services = CreateServiceProvider();
        services.GetService<IDotNetService>().Should().NotBeNull();
        services.GetService<IDotNetService>().Should().BeOfType<DotNetService>();
    }

    [Fact]
    public void AddAlacarteCoreServices_RegistersAngularService()
    {
        var services = CreateServiceProvider();
        services.GetService<IAngularService>().Should().NotBeNull();
        services.GetService<IAngularService>().Should().BeOfType<AngularService>();
    }

    [Fact]
    public void AddAlacarteCoreServices_RegistersInitCommandHandler()
    {
        var services = CreateServiceProvider();
        services.GetService<InitCommandHandler>().Should().NotBeNull();
    }

    [Fact]
    public void AddAlacarteCoreServices_ReturnsSameServiceCollection()
    {
        var sc = new ServiceCollection();
        var result = sc.AddAlacarteCoreServices();
        result.Should().BeSameAs(sc);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var sc = new ServiceCollection();
        sc.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        sc.Configure<ALaCarte.Core.Options.AlacarteOptions>(_ => { });
        sc.Configure<ALaCarte.Core.Options.GitOptions>(_ => { });
        sc.Configure<ALaCarte.Core.Options.DotNetOptions>(_ => { });
        sc.Configure<ALaCarte.Core.Options.AngularOptions>(_ => { });
        sc.AddAlacarteCoreServices();
        return sc.BuildServiceProvider();
    }
}
