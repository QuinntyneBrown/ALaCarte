using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using QuinntyneBrown.Git.Core;
using QuinntyneBrown.Git.Core.Services;

namespace ALaCarte.Cli;

public static class GitOperations
{
    private static readonly IGitService GitService = new GitService(NullLogger<GitService>.Instance);

    public static async Task InitializeRepository(string path)
    {
        Console.WriteLine("Initializing git repository...");
        await RunGitCommand("init", path);
    }

    public static async Task AddRepositories(string solutionPath, string[] repoUrls, string branch)
    {
        Console.WriteLine("\nAdding repositories...");

        foreach (var repoUrl in repoUrls)
        {
            var (cloneUrl, urlBranch) = GitUrlParser.Parse(repoUrl);
            var effectiveBranch = urlBranch ?? branch;
            var repoName = GetRepositoryName(cloneUrl);
            var repoPath = Path.Combine(solutionPath, "submodules", repoName);

            Console.WriteLine($"  Cloning: {repoName} (branch: {effectiveBranch})");

            try
            {
                var success = await GitService.CloneAsync(cloneUrl, repoPath, effectiveBranch);
                if (!success)
                    Console.WriteLine($"  Warning: Failed to clone {repoName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Warning: Failed to clone {repoName}: {ex.Message}");
            }
        }
    }

    internal static string GetRepositoryName(string repoUrl)
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
