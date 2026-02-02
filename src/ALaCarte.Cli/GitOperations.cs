using System.Diagnostics;

namespace ALaCarte.Cli;

public static class GitOperations
{
    public static async Task InitializeRepository(string path)
    {
        Console.WriteLine("Initializing git repository...");
        await RunGitCommand("init", path);
    }

    public static async Task AddSubmodules(string solutionPath, string[] repoUrls, string branch)
    {
        Console.WriteLine("\nAdding submodules...");
        
        foreach (var repoUrl in repoUrls)
        {
            var repoName = GetRepositoryName(repoUrl);
            var submodulePath = Path.Combine("submodules", repoName);
            
            Console.WriteLine($"  Adding: {repoName} (branch: {branch})");
            
            try
            {
                await RunGitCommand($"submodule add -b {branch} {repoUrl} {submodulePath}", solutionPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Warning: Failed to add submodule {repoName}: {ex.Message}");
            }
        }
    }

    private static string GetRepositoryName(string repoUrl)
    {
        var uri = new Uri(repoUrl.Replace(".git", ""));
        return Path.GetFileName(uri.LocalPath);
    }

    private static async Task<string> RunGitCommand(string arguments, string workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
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

        if (process.ExitCode != 0)
        {
            throw new Exception($"Git command failed: {error}");
        }

        return output;
    }
}
