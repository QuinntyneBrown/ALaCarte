using ALaCarte.Core.Options;
using Microsoft.Extensions.Options;

namespace ALaCarte.BoundaryInterface.Tests.Helpers;

public static class TestOptionsFactory
{
    public static IOptions<AlacarteOptions> CreateAlacarteOptions(Action<AlacarteOptions>? configure = null)
    {
        var options = new AlacarteOptions();
        configure?.Invoke(options);
        return Options.Create(options);
    }

    public static IOptions<GitOptions> CreateGitOptions(Action<GitOptions>? configure = null)
    {
        var options = new GitOptions();
        configure?.Invoke(options);
        return Options.Create(options);
    }

    public static IOptions<DotNetOptions> CreateDotNetOptions(Action<DotNetOptions>? configure = null)
    {
        var options = new DotNetOptions();
        configure?.Invoke(options);
        return Options.Create(options);
    }

    public static IOptions<AngularOptions> CreateAngularOptions(Action<AngularOptions>? configure = null)
    {
        var options = new AngularOptions();
        configure?.Invoke(options);
        return Options.Create(options);
    }
}
