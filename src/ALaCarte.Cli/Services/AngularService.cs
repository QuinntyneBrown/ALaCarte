using System.Text.Json;
using System.Text.Json.Nodes;
using ALaCarte.Cli.Abstractions;
using ALaCarte.Cli.Exceptions;
using ALaCarte.Cli.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ALaCarte.Cli.Services;

public class AngularService : IAngularService
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<AngularService> _logger;
    private readonly AngularOptions _options;
    private readonly AlacarteOptions _alacarteOptions;

    public AngularService(
        IProcessRunner processRunner,
        IFileSystem fileSystem,
        ILogger<AngularService> logger,
        IOptions<AngularOptions> options,
        IOptions<AlacarteOptions> alacarteOptions)
    {
        _processRunner = processRunner;
        _fileSystem = fileSystem;
        _logger = logger;
        _options = options.Value;
        _alacarteOptions = alacarteOptions.Value;
    }

    public async Task CreateWorkspaceAsync(string solutionPath, List<AngularWorkspace> angularWorkspaces, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating Angular workspace...");

        var srcPath = _fileSystem.Combine(solutionPath, _alacarteOptions.SourceFolderName);
        _fileSystem.CreateDirectory(srcPath);

        var workspacePath = _fileSystem.Combine(srcPath, _options.WorkspaceFolderName);

        // Check if Angular CLI is available
        if (!await IsAngularCliAvailableAsync(ct))
        {
            if (_options.AutoInstallCli)
            {
                _logger.LogInformation("  Angular CLI not found. Installing @angular/cli...");
                try
                {
                    await InstallAngularCliAsync(ct);
                    _logger.LogInformation("  Angular CLI installed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("  Failed to install Angular CLI: {Message}", ex.Message);
                    _logger.LogWarning("  To install manually: npm install -g @angular/cli");
                    _fileSystem.CreateDirectory(workspacePath);
                    return;
                }
            }
            else
            {
                _logger.LogWarning("  Angular CLI not found. Please install it manually: npm install -g @angular/cli");
                _fileSystem.CreateDirectory(workspacePath);
                return;
            }
        }

        // Create new Angular workspace
        _logger.LogInformation("  Creating new Angular workspace...");
        try
        {
            await RunShellCommandAsync($"ng new {_options.WorkspaceFolderName} --skip-git --create-application=false", srcPath, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("  Failed to create Angular workspace: {Message}", ex.Message);
            return;
        }

        // Copy Angular projects and collect configurations
        var allProjectConfigs = new Dictionary<string, JsonNode>();
        var totalProjects = 0;

        foreach (var workspace in angularWorkspaces)
        {
            var (copiedCount, projectConfigs) = await IntegrateAngularProjectAsync(workspacePath, workspace.WorkspacePath, workspace.ProjectNames, ct);
            totalProjects += copiedCount;
            foreach (var (name, config) in projectConfigs)
            {
                allProjectConfigs[name] = config;
            }
        }

        // Update workspace angular.json
        if (allProjectConfigs.Count > 0)
        {
            await UpdateWorkspaceAngularJsonAsync(workspacePath, allProjectConfigs, ct);
        }

        _logger.LogInformation("Created Angular workspace with {Count} project(s)", totalProjects);
    }

    private async Task<bool> IsAngularCliAvailableAsync(CancellationToken ct)
    {
        try
        {
            var result = await RunShellCommandAsync("ng version", _fileSystem.GetCurrentDirectory(), ct);
            return result.IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    private async Task InstallAngularCliAsync(CancellationToken ct)
    {
        var result = await RunShellCommandAsync("npm install -g @angular/cli", _fileSystem.GetCurrentDirectory(), ct);
        if (!result.IsSuccess)
        {
            throw new AngularOperationException(
                $"Failed to install Angular CLI: {result.StandardError}",
                "npm install -g @angular/cli",
                result.ExitCode,
                result.StandardError);
        }
    }

    private async Task<(int CopiedCount, Dictionary<string, JsonNode> ProjectConfigs)> IntegrateAngularProjectAsync(
        string workspacePath,
        string sourceWorkspaceDir,
        List<string> allowedProjectNames,
        CancellationToken ct)
    {
        var copiedCount = 0;
        var projectConfigs = new Dictionary<string, JsonNode>();
        var includeAll = allowedProjectNames.Contains("*");

        try
        {
            var angularJsonPath = _fileSystem.Combine(sourceWorkspaceDir, "angular.json");
            if (!_fileSystem.FileExists(angularJsonPath))
            {
                return (0, projectConfigs);
            }

            var angularJsonContent = await _fileSystem.ReadAllTextAsync(angularJsonPath, ct);
            var angularJsonNode = JsonNode.Parse(angularJsonContent);
            var projects = angularJsonNode?["projects"]?.AsObject();

            if (projects == null)
            {
                return (0, projectConfigs);
            }

            foreach (var project in projects)
            {
                var projectName = project.Key;
                var projectConfig = project.Value;

                if (projectConfig == null) continue;

                if (!includeAll && !allowedProjectNames.Contains(projectName))
                {
                    continue;
                }

                _logger.LogInformation("  Integrating Angular project: {ProjectName}", projectName);

                var projectRoot = projectConfig["root"]?.GetValue<string>() ?? "";
                var sourceProjectPath = _fileSystem.Combine(sourceWorkspaceDir, projectRoot);

                if (_fileSystem.DirectoryExists(sourceProjectPath))
                {
                    var projectType = projectConfig["projectType"]?.GetValue<string>() ?? "application";

                    var destProjectPath = _fileSystem.Combine(workspacePath, "projects", projectName);
                    CopyDirectory(sourceProjectPath, destProjectPath);

                    var updatedConfig = UpdateProjectPaths(projectConfig, projectName);
                    projectConfigs[projectName] = updatedConfig;

                    _logger.LogInformation("    Copied {ProjectType}: {ProjectName}", projectType, projectName);
                    copiedCount++;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("  Failed to integrate Angular project: {Message}", ex.Message);
        }

        return (copiedCount, projectConfigs);
    }

    private static JsonNode UpdateProjectPaths(JsonNode originalConfig, string projectName)
    {
        var newRoot = $"projects/{projectName}";
        var configCopy = JsonNode.Parse(originalConfig.ToJsonString())!;

        configCopy["root"] = newRoot;

        if (configCopy["sourceRoot"] != null)
        {
            configCopy["sourceRoot"] = $"{newRoot}/src";
        }

        return configCopy;
    }

    private async Task UpdateWorkspaceAngularJsonAsync(string workspacePath, Dictionary<string, JsonNode> projectConfigs, CancellationToken ct)
    {
        var angularJsonPath = _fileSystem.Combine(workspacePath, "angular.json");

        if (!_fileSystem.FileExists(angularJsonPath))
        {
            _logger.LogWarning("  angular.json not found in workspace");
            return;
        }

        try
        {
            var angularJsonContent = await _fileSystem.ReadAllTextAsync(angularJsonPath, ct);
            var angularJson = JsonNode.Parse(angularJsonContent)!;

            var projects = angularJson["projects"]?.AsObject();
            if (projects == null)
            {
                projects = new JsonObject();
                angularJson["projects"] = projects;
            }

            foreach (var (projectName, projectConfig) in projectConfigs)
            {
                projects[projectName] = JsonNode.Parse(projectConfig.ToJsonString());
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedContent = angularJson.ToJsonString(options);
            await _fileSystem.WriteAllTextAsync(angularJsonPath, updatedContent, ct);

            _logger.LogInformation("  Updated angular.json with project configurations");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("  Failed to update angular.json: {Message}", ex.Message);
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        _fileSystem.CreateDirectory(destDir);

        var files = _fileSystem.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            var fileName = _fileSystem.GetFileName(file);
            var destFile = _fileSystem.Combine(destDir, fileName);
            _fileSystem.CopyFile(file, destFile, true);
        }

        var directories = _fileSystem.GetDirectories(sourceDir);
        foreach (var dir in directories)
        {
            var dirName = _fileSystem.GetFileName(dir);

            if (_options.ExcludedDirectories.Contains(dirName))
                continue;

            var destSubDir = _fileSystem.Combine(destDir, dirName);
            CopyDirectory(dir, destSubDir);
        }
    }

    private async Task<ProcessResult> RunShellCommandAsync(string command, string workingDirectory, CancellationToken ct)
    {
        var parts = command.Split(' ', 2);
        var fileName = parts[0];
        var arguments = parts.Length > 1 ? parts[1] : "";

        var request = new ProcessRunRequest(
            fileName,
            arguments,
            workingDirectory,
            UseShellExecute: true,
            CancellationToken: ct);

        return await _processRunner.RunAsync(request);
    }
}
