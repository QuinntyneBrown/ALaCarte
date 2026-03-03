using System.Xml.Linq;
using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Exceptions;
using ALaCarte.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ALaCarte.Core.Services;

public class DotNetService : IDotNetService
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<DotNetService> _logger;
    private readonly DotNetOptions _options;
    private readonly AlacarteOptions _alacarteOptions;

    public DotNetService(
        IProcessRunner processRunner,
        IFileSystem fileSystem,
        ILogger<DotNetService> logger,
        IOptions<DotNetOptions> options,
        IOptions<AlacarteOptions> alacarteOptions)
    {
        _processRunner = processRunner;
        _fileSystem = fileSystem;
        _logger = logger;
        _options = options.Value;
        _alacarteOptions = alacarteOptions.Value;
    }

    public async Task CreateSolutionAsync(string solutionPath, List<string> projectFiles, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating .NET solution...");

        var srcPath = _fileSystem.Combine(solutionPath, _alacarteOptions.SourceFolderName);
        _fileSystem.CreateDirectory(srcPath);

        var solutionFile = _fileSystem.Combine(solutionPath, $"{_options.SolutionName}.sln");

        // Create solution
        await RunDotNetCommandAsync($"new sln -n {_options.SolutionName} -o \"{solutionPath}\"", solutionPath, ct);

        var projectMap = new Dictionary<string, string>();
        var packageToProject = new Dictionary<string, string>();

        // First pass: Copy projects and build mapping
        foreach (var oldProjectPath in projectFiles)
        {
            var projectName = _fileSystem.GetFileNameWithoutExtension(oldProjectPath);
            var newProjectPath = _fileSystem.Combine(srcPath, projectName);

            _logger.LogInformation("  Processing project: {ProjectName}", projectName);

            // Copy project directory
            var sourceDir = _fileSystem.GetDirectoryName(oldProjectPath);
            if (sourceDir != null)
            {
                CopyDirectory(sourceDir, newProjectPath);
            }

            var newCsprojPath = _fileSystem.Combine(newProjectPath, $"{projectName}.csproj");
            projectMap[oldProjectPath] = newCsprojPath;

            var packageId = GetPackageId(newCsprojPath) ?? projectName;
            packageToProject[packageId] = newCsprojPath;

            await StripRelativeReferencesAsync(newCsprojPath);
        }

        // Second pass: Replace NuGet references with project references
        foreach (var projectPath in projectMap.Values)
        {
            await ReplaceNuGetWithProjectReferencesAsync(projectPath, packageToProject, projectMap);
        }

        // Add all projects to solution
        foreach (var projectPath in projectMap.Values)
        {
            var relativePath = _fileSystem.GetRelativePath(solutionPath, projectPath);
            await RunDotNetCommandAsync($"sln \"{solutionFile}\" add \"{relativePath}\"", solutionPath, ct);
        }

        _logger.LogInformation("Created solution with {Count} project(s)", projectMap.Count);
    }

    private string? GetPackageId(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            return doc.Descendants("PackageId").FirstOrDefault()?.Value;
        }
        catch
        {
            return null;
        }
    }

    private async Task StripRelativeReferencesAsync(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            bool modified = false;

            var elementsToRemove = new[] { "Reference", "Compile", "Content", "None" };
            foreach (var elementName in elementsToRemove)
            {
                var relativeItems = doc.Descendants(elementName)
                    .Where(e => e.Attribute("Include")?.Value.StartsWith("..") == true)
                    .ToList();

                foreach (var item in relativeItems)
                {
                    item.Remove();
                    modified = true;
                }
            }

            if (modified)
            {
                doc.Save(csprojPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to strip relative references from {Path}: {Message}", csprojPath, ex.Message);
        }
    }

    private async Task ReplaceNuGetWithProjectReferencesAsync(
        string csprojPath,
        Dictionary<string, string> packageToProject,
        Dictionary<string, string> projectMap)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            bool modified = false;

            var packageReferences = doc.Descendants("PackageReference").ToList();

            foreach (var packageRef in packageReferences)
            {
                var packageName = packageRef.Attribute("Include")?.Value;
                if (packageName != null && packageToProject.TryGetValue(packageName, out var targetProjectPath))
                {
                    var csprojDir = _fileSystem.GetDirectoryName(csprojPath);
                    if (csprojDir == null) continue;

                    var relativePath = _fileSystem.GetRelativePath(csprojDir, targetProjectPath);

                    var parent = packageRef.Parent;
                    packageRef.Remove();

                    var projectReference = new XElement("ProjectReference",
                        new XAttribute("Include", relativePath));

                    if (parent != null && parent.Name == "ItemGroup")
                    {
                        parent.Add(projectReference);
                    }
                    else
                    {
                        var itemGroup = doc.Descendants("ItemGroup")
                            .FirstOrDefault(ig => ig.Elements("ProjectReference").Any());

                        if (itemGroup == null)
                        {
                            itemGroup = new XElement("ItemGroup");
                            doc.Root?.Add(itemGroup);
                        }

                        itemGroup.Add(projectReference);
                    }

                    modified = true;
                    _logger.LogInformation("    Replaced NuGet '{PackageName}' with project reference", packageName);
                }
            }

            if (modified)
            {
                doc.Save(csprojPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to replace NuGet references in {Path}: {Message}", csprojPath, ex.Message);
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

    private async Task<ProcessResult> RunDotNetCommandAsync(string arguments, string workingDirectory, CancellationToken ct)
    {
        var request = new ProcessRunRequest(
            _options.ExecutablePath,
            arguments,
            workingDirectory,
            CancellationToken: ct);

        var result = await _processRunner.RunAsync(request);

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardError))
        {
            throw new DotNetOperationException(
                $"dotnet command failed: {result.StandardError}",
                arguments,
                result.ExitCode,
                result.StandardError);
        }

        return result;
    }
}
