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
        // Remove .git suffix if present
        var url = repoUrl.Replace(".git", "");
        
        // Handle SSH URLs (e.g., git@github.com:user/repo or user@host:path/repo)
        // SSH URLs have the format: [user@]host:path
        // We check if it contains '@' followed by ':' and doesn't start with http(s)://
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            url.Contains('@'))
        {
            // Find the last colon which separates the host from the path
            var lastColonIndex = url.LastIndexOf(':');
            if (lastColonIndex > 0 && lastColonIndex < url.Length - 1)
            {
                // Get the path part after the last colon
                var path = url.Substring(lastColonIndex + 1);
                return Path.GetFileName(path);
            }
        }
        
        // Handle HTTPS URLs (e.g., https://github.com/user/repo)
        var uri = new Uri(url);
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
