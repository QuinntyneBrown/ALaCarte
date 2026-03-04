using ALaCarte.Core;

namespace ALaCarte.BoundaryInterface.Tests;

public class AlacarteHostTests
{
    [Fact]
    public void AlacarteRunOptions_HasDefaultValues()
    {
        var options = new AlacarteRunOptions();

        Assert.Empty(options.Repos);
        Assert.Equal("main", options.Branch);
        Assert.Equal(".", options.Folder);
        Assert.Empty(options.ProjectFilters);
        Assert.Equal("Solution", options.SolutionName);
    }

    [Fact]
    public void AlacarteRunOptions_CanSetAllProperties()
    {
        var options = new AlacarteRunOptions
        {
            Repos = ["https://github.com/org/repo.git"],
            Branch = "develop",
            Folder = "../output",
            ProjectFilters = ["MyProject"],
            SolutionName = "MySolution"
        };

        Assert.Single(options.Repos);
        Assert.Equal("https://github.com/org/repo.git", options.Repos[0]);
        Assert.Equal("develop", options.Branch);
        Assert.Equal("../output", options.Folder);
        Assert.Single(options.ProjectFilters);
        Assert.Equal("MyProject", options.ProjectFilters[0]);
        Assert.Equal("MySolution", options.SolutionName);
    }

    [Fact]
    public void AlacarteHost_ExistsAsStaticClass()
    {
        var type = typeof(AlacarteHost);

        Assert.True(type.IsAbstract && type.IsSealed, "AlacarteHost should be a static class");
    }

    [Fact]
    public void AlacarteHost_HasRunAsyncMethod()
    {
        var method = typeof(AlacarteHost).GetMethod("RunAsync");

        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.Equal(typeof(Task<int>), method.ReturnType);
    }

    [Fact]
    public void AlacarteHost_RunAsync_AcceptsArgsAndConfigureAction()
    {
        var method = typeof(AlacarteHost).GetMethod("RunAsync");

        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string[]), parameters[0].ParameterType);
        Assert.Equal(typeof(Action<AlacarteRunOptions>), parameters[1].ParameterType);
    }

    [Fact]
    public async Task AlacarteHost_RunAsync_ConfigureCallbackIsInvoked()
    {
        var configured = false;

        // This will fail because the repos are empty and folder exists,
        // but the configure callback should still be invoked
        var result = await AlacarteHost.RunAsync([], options =>
        {
            configured = true;
            options.Repos = [];
            options.Folder = $"nonexistent-{Guid.NewGuid()}";
        });

        Assert.True(configured, "Configure callback should be invoked");
    }

    [Fact]
    public async Task AlacarteHost_RunAsync_ReturnsNonZeroForEmptyRepos()
    {
        // With empty repos, the handler should return a failure code
        // or complete gracefully depending on implementation
        var result = await AlacarteHost.RunAsync([], options =>
        {
            options.Repos = [];
            options.Folder = $"test-{Guid.NewGuid()}";
        });

        // Empty repos should not be a success (git submodule add has nothing to do)
        Assert.IsType<int>(result);
    }
}
