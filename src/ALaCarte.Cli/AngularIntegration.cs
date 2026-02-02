using System.Diagnostics;
using System.Text.Json;

namespace ALaCarte.Cli;

public static class AngularIntegration
{
    public static async Task CreateWorkspace(string solutionPath, List<string> angularProjectDirs)
    {
        Console.WriteLine("Creating Angular workspace...");

        var workspacePath = Path.Combine(solutionPath, "angular-workspace");
        Directory.CreateDirectory(workspacePath);

        // Check if Angular CLI is available
        if (!await IsAngularCliAvailable())
        {
            Console.WriteLine("  Warning: Angular CLI not found. Skipping Angular workspace creation.");
            Console.WriteLine("  To install: npm install -g @angular/cli");
            return;
        }

        // Create new Angular workspace
        Console.WriteLine("  Creating new Angular workspace...");
        try
        {
            await RunCommand("ng", "new angular-workspace --skip-git --create-application=false", solutionPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Failed to create Angular workspace: {ex.Message}");
            return;
        }

        // Copy Angular projects/libraries into workspace
        foreach (var angularProjectDir in angularProjectDirs)
        {
            await IntegrateAngularProject(workspacePath, angularProjectDir);
        }

        Console.WriteLine($"✓ Created Angular workspace with {angularProjectDirs.Count} project(s)");
    }

    private static async Task<bool> IsAngularCliAvailable()
    {
        try
        {
            await RunCommand("ng", "version", Directory.GetCurrentDirectory());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task IntegrateAngularProject(string workspacePath, string sourceProjectDir)
    {
        try
        {
            var angularJsonPath = Path.Combine(sourceProjectDir, "angular.json");
            if (!File.Exists(angularJsonPath))
            {
                return;
            }

            var angularJsonContent = await File.ReadAllTextAsync(angularJsonPath);
            var angularJson = JsonDocument.Parse(angularJsonContent);

            // Get projects from angular.json
            if (angularJson.RootElement.TryGetProperty("projects", out var projects))
            {
                foreach (var project in projects.EnumerateObject())
                {
                    var projectName = project.Name;
                    Console.WriteLine($"  Integrating Angular project: {projectName}");

                    // Get project root
                    if (project.Value.TryGetProperty("root", out var rootElement))
                    {
                        var projectRoot = rootElement.GetString() ?? "";
                        var sourceProjectPath = Path.Combine(sourceProjectDir, projectRoot);

                        if (Directory.Exists(sourceProjectPath))
                        {
                            // Determine project type
                            var projectType = "application";
                            if (project.Value.TryGetProperty("projectType", out var typeElement))
                            {
                                projectType = typeElement.GetString() ?? "application";
                            }

                            // Copy to workspace
                            var destProjectPath = Path.Combine(workspacePath, "projects", projectName);
                            CopyDirectory(sourceProjectPath, destProjectPath);

                            Console.WriteLine($"    Copied {projectType}: {projectName}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Failed to integrate Angular project: {ex.Message}");
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            
            // Skip node_modules and dist directories
            if (dirName == "node_modules" || dirName == "dist")
                continue;

            var destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(dir, destSubDir);
        }
    }

    private static async Task<string> RunCommand(string command, string arguments, string workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            throw new Exception($"{command} command failed: {error}");
        }

        return output;
    }
}
