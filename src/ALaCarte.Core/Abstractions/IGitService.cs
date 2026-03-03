namespace ALaCarte.Core.Abstractions;

public interface IGitService
{
    Task InitializeRepositoryAsync(string path, CancellationToken ct = default);
    Task AddSubmodulesAsync(string solutionPath, string[] repoUrls, string branch, CancellationToken ct = default);
    string GetRepositoryName(string repoUrl);
}
