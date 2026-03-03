namespace ALaCarte.Cli.Abstractions;

public interface IProjectDiscoveryService
{
    Task<List<string>> DiscoverDotNetProjectsAsync(string solutionPath, string[]? projectFilters = null, CancellationToken ct = default);
    Task<List<AngularWorkspace>> DiscoverAngularProjectsAsync(string solutionPath, string[]? projectFilters = null, CancellationToken ct = default);
}

public record AngularWorkspace(string WorkspacePath, List<string> ProjectNames);
