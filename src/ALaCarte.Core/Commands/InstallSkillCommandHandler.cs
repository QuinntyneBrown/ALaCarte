using ALaCarte.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace ALaCarte.Core.Commands;

public class InstallSkillCommandHandler
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<InstallSkillCommandHandler> _logger;

    public const string SkillContent =
"""
---
name: a-la-carte
description: Create composite solutions from multiple git repositories using ALaCarte
user-invocable: true
---

# ALaCarte Skill

Use this skill when asked to create composite .NET/Angular solutions from multiple git repositories using the ALaCarte CLI tool.

## Package Reference

Add the ALaCarte.Core NuGet package to your project:

```xml
<PackageReference Include="QuinntyneBrown.ALaCarte.Core" Version="1.0.0" />
```

## Service Registration

Register ALaCarte services in your DI container:

```csharp
using ALaCarte.Core;
using Microsoft.Extensions.DependencyInjection;

services.AddAlacarteCoreServices();
```

This registers: IFileSystem, IProcessRunner, IGitService, IProjectDiscoveryService, IDotNetService, IAngularService, and InitCommandHandler.

## Core Service Abstractions

### IGitService

Manages git repository initialization and submodule operations:

```csharp
using ALaCarte.Core.Abstractions;

var gitService = serviceProvider.GetRequiredService<IGitService>();
await gitService.InitializeRepositoryAsync(path, ct);
await gitService.AddSubmodulesAsync(solutionPath, repoUrls, branch, ct);
string repoName = gitService.GetRepositoryName(repoUrl);
```

### IProjectDiscoveryService

Discovers .NET and Angular projects within a solution directory:

```csharp
using ALaCarte.Core.Abstractions;

var discovery = serviceProvider.GetRequiredService<IProjectDiscoveryService>();
List<string> dotnetProjects = await discovery.DiscoverDotNetProjectsAsync(solutionPath, projectFilters, ct);
List<AngularWorkspace> angularProjects = await discovery.DiscoverAngularProjectsAsync(solutionPath, projectFilters, ct);
```

The `AngularWorkspace` record: `record AngularWorkspace(string WorkspacePath, List<string> ProjectNames)`

### IDotNetService

Creates .NET solution files from discovered projects:

```csharp
using ALaCarte.Core.Abstractions;

var dotNetService = serviceProvider.GetRequiredService<IDotNetService>();
await dotNetService.CreateSolutionAsync(solutionPath, projectFiles, ct);
```

### IAngularService

Creates Angular workspace configurations:

```csharp
using ALaCarte.Core.Abstractions;

var angularService = serviceProvider.GetRequiredService<IAngularService>();
await angularService.CreateWorkspaceAsync(solutionPath, angularWorkspaces, ct);
```

### IFileSystem

Abstracts file system operations for testability:

```csharp
using ALaCarte.Core.Abstractions;

var fs = serviceProvider.GetRequiredService<IFileSystem>();
fs.CreateDirectory(path);
bool exists = fs.DirectoryExists(path);
bool fileExists = fs.FileExists(path);
await fs.WriteAllTextAsync(path, contents, ct);
string content = await fs.ReadAllTextAsync(path, ct);
fs.CopyFile(source, destination, overwrite);
string fullPath = fs.GetFullPath(path);
string combined = fs.Combine(paths);
string[] files = fs.GetFiles(path, searchPattern, searchOption);
string[] dirs = fs.GetDirectories(path);
string fileName = fs.GetFileName(path);
string nameNoExt = fs.GetFileNameWithoutExtension(path);
string? dirName = fs.GetDirectoryName(path);
string relative = fs.GetRelativePath(relativeTo, path);
string cwd = fs.GetCurrentDirectory();
```

### IProcessRunner

Runs external processes:

```csharp
using ALaCarte.Core.Abstractions;

var runner = serviceProvider.GetRequiredService<IProcessRunner>();
var result = await runner.RunAsync(new ProcessRunRequest(
    FileName: "dotnet",
    Arguments: "new sln -n MySolution",
    WorkingDirectory: outputDir));

if (result.IsSuccess)
    Console.WriteLine(result.StandardOutput);
else
    Console.Error.WriteLine(result.StandardError);
```

## Options Configuration

Configure via `appsettings.json`:

```json
{
  "Alacarte": {
    "DefaultBranch": "main",
    "SubmodulesFolderName": "submodules",
    "SourceFolderName": "src"
  },
  "Git": {
    "ExecutablePath": "git",
    "TimeoutSeconds": 300
  },
  "DotNet": {
    "ExecutablePath": "dotnet",
    "SolutionName": "Solution",
    "ExcludedDirectories": ["obj", "bin"]
  },
  "Angular": {
    "WorkspaceFolderName": "Ui",
    "AutoInstallCli": true,
    "ExcludedDirectories": ["node_modules", "dist"]
  }
}
```

Register options:

```csharp
using ALaCarte.Core.Options;

services.Configure<AlacarteOptions>(configuration.GetSection("Alacarte"));
services.Configure<GitOptions>(configuration.GetSection("Git"));
services.Configure<DotNetOptions>(configuration.GetSection("DotNet"));
services.Configure<AngularOptions>(configuration.GetSection("Angular"));
```

## Exception Handling

ALaCarte defines domain-specific exceptions with command context:

```csharp
using ALaCarte.Core.Exceptions;

// Git failures
catch (GitOperationException ex)
{
    Console.Error.WriteLine($"Git command failed: {ex.Command}");
    Console.Error.WriteLine($"Exit code: {ex.ExitCode}");
    Console.Error.WriteLine($"Error: {ex.StandardError}");
}

// .NET CLI failures
catch (DotNetOperationException ex)
{
    Console.Error.WriteLine($"DotNet command failed: {ex.Command}");
    Console.Error.WriteLine($"Exit code: {ex.ExitCode}");
    Console.Error.WriteLine($"Error: {ex.StandardError}");
}

// Angular CLI failures
catch (AngularOperationException ex)
{
    Console.Error.WriteLine($"Angular command failed: {ex.Command}");
    Console.Error.WriteLine($"Exit code: {ex.ExitCode}");
    Console.Error.WriteLine($"Error: {ex.StandardError}");
}
```

All three exception types share: `Command`, `ExitCode`, and `StandardError` properties.

## Command Handler Pattern

### InitCommandHandler

The primary command handler for creating composite solutions:

```csharp
using ALaCarte.Core.Commands;

var handler = serviceProvider.GetRequiredService<InitCommandHandler>();
int exitCode = await handler.ExecuteAsync(
    repos: new[] { "https://github.com/org/repo1", "https://github.com/org/repo2" },
    branch: "main",
    folder: "my-solution",
    projectFilters: new[] { "*.Api", "*.Core" },
    ct: cancellationToken);
```

The handler orchestrates: git init, submodule add, project discovery, .NET solution creation, and Angular workspace creation. Returns 0 on success, 1 on failure.
""";

    public InstallSkillCommandHandler(
        IFileSystem fileSystem,
        ILogger<InstallSkillCommandHandler> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(string outputDirectory, CancellationToken ct = default)
    {
        try
        {
            var skillDirectory = _fileSystem.Combine(outputDirectory, ".claude", "skills", "a-la-carte");
            _fileSystem.CreateDirectory(skillDirectory);

            var skillFilePath = _fileSystem.Combine(skillDirectory, "SKILL.md");
            await _fileSystem.WriteAllTextAsync(skillFilePath, SkillContent, ct);

            var fullPath = _fileSystem.GetFullPath(skillFilePath);
            _logger.LogInformation("Installed Claude skill: {Path}", fullPath);

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing skill: {Message}", ex.Message);
            return 1;
        }
    }
}
