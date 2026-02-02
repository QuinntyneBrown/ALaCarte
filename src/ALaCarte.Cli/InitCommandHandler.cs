using System.Diagnostics;

namespace ALaCarte.Cli;

public class InitCommandHandler
{
    public async Task ExecuteAsync(string[] repos, string branch, string? folder)
    {
        try
        {
            Console.WriteLine("ALaCarte - Initializing solution...");
            Console.WriteLine($"Repositories: {string.Join(", ", repos)}");
            Console.WriteLine($"Branch: {branch}");
            Console.WriteLine($"Folder: {folder ?? "(auto-generated)"}");

            // Determine folder name
            var solutionFolder = folder ?? $"alacarte-{DateTime.Now:yyyyMMdd-HHmmss}";
            var solutionPath = Path.GetFullPath(solutionFolder);

            if (Directory.Exists(solutionPath))
            {
                Console.WriteLine($"Error: Folder '{solutionPath}' already exists.");
                return;
            }

            Console.WriteLine($"\nCreating solution in: {solutionPath}");

            // Create the solution folder
            Directory.CreateDirectory(solutionPath);

            // Initialize git repository
            await GitOperations.InitializeRepository(solutionPath);

            // Add submodules
            await GitOperations.AddSubmodules(solutionPath, repos, branch);

            // Discover projects
            var dotnetProjects = await ProjectDiscovery.DiscoverDotNetProjects(solutionPath);
            var angularProjects = await ProjectDiscovery.DiscoverAngularProjects(solutionPath);

            // Create .NET solution if needed
            if (dotnetProjects.Any())
            {
                Console.WriteLine($"\nFound {dotnetProjects.Count} .NET project(s)");
                await DotNetIntegration.CreateSolution(solutionPath, dotnetProjects);
            }

            // Create Angular workspace if needed
            if (angularProjects.Any())
            {
                Console.WriteLine($"\nFound {angularProjects.Count} Angular project(s)");
                await AngularIntegration.CreateWorkspace(solutionPath, angularProjects);
            }

            Console.WriteLine($"\n✓ Solution created successfully at: {solutionPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            throw;
        }
    }
}
