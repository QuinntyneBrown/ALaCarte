using System.Text.Json;
using System.Text.RegularExpressions;
using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ALaCarte.Core.Services;

public class ProjectDiscoveryService : IProjectDiscoveryService
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ProjectDiscoveryService> _logger;
    private readonly AlacarteOptions _alacarteOptions;
    private readonly DotNetOptions _dotNetOptions;

    public ProjectDiscoveryService(
        IFileSystem fileSystem,
        ILogger<ProjectDiscoveryService> logger,
        IOptions<AlacarteOptions> alacarteOptions,
        IOptions<DotNetOptions> dotNetOptions)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _alacarteOptions = alacarteOptions.Value;
        _dotNetOptions = dotNetOptions.Value;
    }

    public Task<List<string>> DiscoverDotNetProjectsAsync(string solutionPath, string[]? projectFilters = null, CancellationToken ct = default)
    {
        var projects = new List<string>();
        var submodulesPath = _fileSystem.Combine(solutionPath, _alacarteOptions.SubmodulesFolderName);

        if (!_fileSystem.DirectoryExists(submodulesPath))
        {
            return Task.FromResult(projects);
        }

        var csprojFiles = _fileSystem.GetFiles(submodulesPath, "*.csproj", SearchOption.AllDirectories);

        foreach (var csproj in csprojFiles)
        {
            var relativePath = _fileSystem.GetRelativePath(submodulesPath, csproj);

            if (!IsExcludedPath(relativePath, _dotNetOptions.ExcludedDirectories))
            {
                if (projectFilters == null || projectFilters.Length == 0 || MatchesFilter(csproj, relativePath, projectFilters))
                {
                    projects.Add(csproj);
                }
            }
        }

        _logger.LogDebug("Discovered {Count} .NET projects", projects.Count);
        return Task.FromResult(projects);
    }

    public async Task<List<AngularWorkspace>> DiscoverAngularProjectsAsync(string solutionPath, string[]? projectFilters = null, CancellationToken ct = default)
    {
        var workspaces = new List<AngularWorkspace>();
        var submodulesPath = _fileSystem.Combine(solutionPath, _alacarteOptions.SubmodulesFolderName);

        if (!_fileSystem.DirectoryExists(submodulesPath))
        {
            return workspaces;
        }

        var angularFiles = _fileSystem.GetFiles(submodulesPath, "angular.json", SearchOption.AllDirectories);

        foreach (var angularFile in angularFiles)
        {
            var workspaceDir = _fileSystem.GetDirectoryName(angularFile);
            if (workspaceDir == null) continue;

            var workspaceRelativePath = _fileSystem.GetRelativePath(submodulesPath, workspaceDir);
            var matchingProjects = new List<string>();

            try
            {
                var angularJsonContent = await _fileSystem.ReadAllTextAsync(angularFile, ct);
                using var angularJson = JsonDocument.Parse(angularJsonContent);

                if (angularJson.RootElement.TryGetProperty("projects", out var projects))
                {
                    foreach (var project in projects.EnumerateObject())
                    {
                        var projectName = project.Name;
                        var projectRoot = "";
                        if (project.Value.TryGetProperty("root", out var rootElement))
                        {
                            projectRoot = rootElement.GetString() ?? "";
                        }

                        var projectFullPath = _fileSystem.Combine(workspaceDir, projectRoot);
                        var projectRelativePath = _fileSystem.GetRelativePath(submodulesPath, projectFullPath);

                        if (projectFilters == null || projectFilters.Length == 0 ||
                            MatchesFilter(projectFullPath, projectRelativePath, projectFilters) ||
                            MatchesFilter(projectName, projectName, projectFilters))
                        {
                            matchingProjects.Add(projectName);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Failed to parse angular.json at {Path}: {Message}", angularFile, ex.Message);

                if (projectFilters == null || projectFilters.Length == 0 ||
                    MatchesFilter(workspaceDir, workspaceRelativePath, projectFilters))
                {
                    matchingProjects.Add("*");
                }
            }

            if (matchingProjects.Count > 0)
            {
                workspaces.Add(new AngularWorkspace(workspaceDir, matchingProjects));
            }
        }

        _logger.LogDebug("Discovered {Count} Angular workspaces", workspaces.Count);
        return workspaces;
    }

    private bool IsExcludedPath(string relativePath, string[] excludedDirectories)
    {
        var normalizedPath = relativePath.Replace('\\', '/');
        foreach (var excluded in excludedDirectories)
        {
            if (normalizedPath.Contains($"/{excluded}/") || normalizedPath.Contains($"\\{excluded}\\"))
            {
                return true;
            }
        }
        return false;
    }

    public bool MatchesFilter(string fullPath, string relativePath, string[] filters)
    {
        var projectName = _fileSystem.GetFileNameWithoutExtension(fullPath);
        var normalizedRelativePath = relativePath.Replace('\\', '/');

        foreach (var filter in filters)
        {
            var normalizedFilter = filter.Replace('\\', '/');

            if (projectName.Equals(normalizedFilter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (MatchesWildcard(normalizedRelativePath, normalizedFilter))
            {
                return true;
            }

            if (normalizedRelativePath.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool MatchesWildcard(string input, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }
}
