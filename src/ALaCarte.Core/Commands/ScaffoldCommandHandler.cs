using ALaCarte.Core.Abstractions;
using CodeGenerator.Core.Artifacts.Abstractions;
using CodeGenerator.DotNet.Artifacts.Projects;
using CodeGenerator.DotNet.Artifacts.Projects.Enums;
using CodeGenerator.DotNet.Artifacts.Solutions;
using Microsoft.Extensions.Logging;

namespace ALaCarte.Core.Commands;

public class ScaffoldCommandHandler
{
    private readonly IFileSystem _fileSystem;
    private readonly IArtifactGenerator _artifactGenerator;
    private readonly InstallSkillCommandHandler _installSkillHandler;
    private readonly ILogger<ScaffoldCommandHandler> _logger;

    private static readonly char[] InvalidChars = ['<', '>', ':', '"', '|', '?', '*'];

    public ScaffoldCommandHandler(
        IFileSystem fileSystem,
        IArtifactGenerator artifactGenerator,
        InstallSkillCommandHandler installSkillHandler,
        ILogger<ScaffoldCommandHandler> logger)
    {
        _fileSystem = fileSystem;
        _artifactGenerator = artifactGenerator;
        _installSkillHandler = installSkillHandler;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(string name, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogError("Name is required. Use --name or -n to specify a workspace name.");
                return 1;
            }

            if (name.IndexOfAny(InvalidChars) >= 0)
            {
                _logger.LogError("Name '{Name}' contains invalid directory characters. Avoid: {Chars}",
                    name, string.Join(" ", InvalidChars));
                return 1;
            }

            var currentDirectory = _fileSystem.GetCurrentDirectory();
            var workspacePath = _fileSystem.Combine(currentDirectory, name);

            if (_fileSystem.DirectoryExists(workspacePath))
            {
                _logger.LogError("Directory '{Path}' already exists.", workspacePath);
                return 1;
            }

            _fileSystem.CreateDirectory(workspacePath);

            var solution = new SolutionModel("build", currentDirectory)
            {
                SolutionDirectory = workspacePath
            };

            var project = new ProjectModel(DotNetProjectType.Console, "build", workspacePath);
            project.Packages.Add(new PackageModel { Name = "QuinntyneBrown.ALaCarte.Core" });
            solution.Projects.Add(project);

            await _artifactGenerator.GenerateAsync(solution);

            var buildDirectory = _fileSystem.Combine(workspacePath, "build");
            await _installSkillHandler.ExecuteAsync(buildDirectory, ct);

            _logger.LogInformation("Created workspace '{Name}' at {Path}", name, workspacePath);

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workspace: {Message}", ex.Message);
            return 1;
        }
    }
}
