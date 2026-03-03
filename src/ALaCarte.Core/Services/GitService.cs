using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Exceptions;
using ALaCarte.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ALaCarte.Core.Services;

public class GitService : IGitService
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<GitService> _logger;
    private readonly GitOptions _options;
    private readonly AlacarteOptions _alacarteOptions;

    public GitService(
        IProcessRunner processRunner,
        IFileSystem fileSystem,
        ILogger<GitService> logger,
        IOptions<GitOptions> options,
        IOptions<AlacarteOptions> alacarteOptions)
    {
        _processRunner = processRunner;
        _fileSystem = fileSystem;
        _logger = logger;
        _options = options.Value;
        _alacarteOptions = alacarteOptions.Value;
    }

    public async Task InitializeRepositoryAsync(string path, CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing git repository...");

        var result = await RunGitCommandAsync("init", path, ct);
        if (!result.IsSuccess)
        {
            throw new GitOperationException(
                $"Failed to initialize git repository: {result.StandardError}",
                "init",
                result.ExitCode,
                result.StandardError);
        }
    }

    public async Task AddSubmodulesAsync(string solutionPath, string[] repoUrls, string branch, CancellationToken ct = default)
    {
        _logger.LogInformation("Adding submodules...");

        foreach (var repoUrl in repoUrls)
        {
            var repoName = GetRepositoryName(repoUrl);
            var submodulePath = _fileSystem.Combine(_alacarteOptions.SubmodulesFolderName, repoName);

            _logger.LogInformation("  Adding: {RepoName} (branch: {Branch})", repoName, branch);

            try
            {
                var arguments = $"submodule add -b {branch} {repoUrl} {submodulePath}";
                var result = await RunGitCommandAsync(arguments, solutionPath, ct);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("  Failed to add submodule {RepoName}: {Error}", repoName, result.StandardError);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("  Warning: Failed to add submodule {RepoName}: {Message}", repoName, ex.Message);
            }
        }
    }

    public string GetRepositoryName(string repoUrl)
    {
        // Remove .git suffix if present
        var url = repoUrl.Replace(".git", "");

        // Handle SSH URLs (e.g., git@github.com:user/repo or user@host:path/repo)
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            url.Contains('@'))
        {
            var lastColonIndex = url.LastIndexOf(':');
            if (lastColonIndex > 0 && lastColonIndex < url.Length - 1)
            {
                var path = url.Substring(lastColonIndex + 1);
                return _fileSystem.GetFileName(path);
            }
        }

        // Handle HTTPS URLs
        var uri = new Uri(url);
        return _fileSystem.GetFileName(uri.LocalPath);
    }

    private async Task<ProcessResult> RunGitCommandAsync(string arguments, string workingDirectory, CancellationToken ct)
    {
        var request = new ProcessRunRequest(
            _options.ExecutablePath,
            arguments,
            workingDirectory,
            CancellationToken: ct);

        return await _processRunner.RunAsync(request);
    }
}
