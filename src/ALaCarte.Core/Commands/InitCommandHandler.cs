using ALaCarte.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace ALaCarte.Core.Commands;

public class InitCommandHandler
{
    private readonly IGitService _gitService;
    private readonly IProjectDiscoveryService _projectDiscovery;
    private readonly IDotNetService _dotNetService;
    private readonly IAngularService _angularService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<InitCommandHandler> _logger;

    public InitCommandHandler(
        IGitService gitService,
        IProjectDiscoveryService projectDiscovery,
        IDotNetService dotNetService,
        IAngularService angularService,
        IFileSystem fileSystem,
        ILogger<InitCommandHandler> logger)
    {
        _gitService = gitService;
        _projectDiscovery = projectDiscovery;
        _dotNetService = dotNetService;
        _angularService = angularService;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(string[] repos, string branch, string? folder, string[]? projectFilters = null, bool overwrite = false, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("ALaCarte - Initializing solution...");
            _logger.LogInformation("Repositories: {Repos}", string.Join(", ", repos));
            _logger.LogInformation("Branch: {Branch}", branch);
            _logger.LogInformation("Folder: {Folder}", folder ?? "(auto-generated)");

            if (projectFilters != null && projectFilters.Length > 0)
            {
                _logger.LogInformation("Project filters: {Filters}", string.Join(", ", projectFilters));
            }

            // Determine folder name
            var solutionFolder = folder ?? $"alacarte-{DateTime.Now:yyyyMMdd-HHmmss}";
            var solutionPath = _fileSystem.GetFullPath(solutionFolder);

            if (_fileSystem.DirectoryExists(solutionPath) && !overwrite)
            {
                _logger.LogError("Folder '{SolutionPath}' already exists. Use overwrite option to re-use existing directory.", solutionPath);
                return 1;
            }

            _logger.LogInformation("Creating solution in: {SolutionPath}", solutionPath);

            // Create the solution folder
            _fileSystem.CreateDirectory(solutionPath);

            // Initialize git repository
            await _gitService.InitializeRepositoryAsync(solutionPath, ct);

            // Add submodules
            await _gitService.AddSubmodulesAsync(solutionPath, repos, branch, ct);

            // Discover projects
            var dotnetProjects = await _projectDiscovery.DiscoverDotNetProjectsAsync(solutionPath, projectFilters, ct);
            var angularProjects = await _projectDiscovery.DiscoverAngularProjectsAsync(solutionPath, projectFilters, ct);

            // Create .NET solution if needed
            if (dotnetProjects.Count > 0)
            {
                _logger.LogInformation("Found {Count} .NET project(s)", dotnetProjects.Count);
                await _dotNetService.CreateSolutionAsync(solutionPath, dotnetProjects, ct);
            }

            // Create Angular workspace if needed
            if (angularProjects.Count > 0)
            {
                var totalAngularProjects = angularProjects.Sum(w => w.ProjectNames.Contains("*") ? 1 : w.ProjectNames.Count);
                _logger.LogInformation("Found {Count} Angular project(s)", totalAngularProjects);
                await _angularService.CreateWorkspaceAsync(solutionPath, angularProjects, ct);
            }

            _logger.LogInformation("Solution created successfully at: {SolutionPath}", solutionPath);
            return 0;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation was cancelled");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: {Message}", ex.Message);
            return 1;
        }
    }
}
