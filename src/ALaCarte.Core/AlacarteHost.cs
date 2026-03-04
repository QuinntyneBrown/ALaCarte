using ALaCarte.Core.Commands;
using ALaCarte.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ALaCarte.Core;

public static class AlacarteHost
{
    public static async Task<int> RunAsync(string[] args, Action<AlacarteRunOptions> configure)
    {
        var options = new AlacarteRunOptions();
        configure(options);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddAlacarteCoreServices();

        services.Configure<AlacarteOptions>(o => o.DefaultBranch = options.Branch);
        services.Configure<DotNetOptions>(o => o.SolutionName = options.SolutionName);

        var sp = services.BuildServiceProvider();
        var handler = sp.GetRequiredService<InitCommandHandler>();

        return await handler.ExecuteAsync(
            repos: options.Repos,
            branch: options.Branch,
            folder: options.Folder,
            projectFilters: options.ProjectFilters,
            overwrite: options.Overwrite,
            ct: CancellationToken.None);
    }
}

public class AlacarteRunOptions
{
    public string[] Repos { get; set; } = [];
    public string Branch { get; set; } = "main";
    public string Folder { get; set; } = ".";
    public string[] ProjectFilters { get; set; } = [];
    public string SolutionName { get; set; } = "Solution";
    public bool Overwrite { get; set; } = false;
}
