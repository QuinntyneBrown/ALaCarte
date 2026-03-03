namespace ALaCarte.Core.Abstractions;

public interface IDotNetService
{
    Task CreateSolutionAsync(string solutionPath, List<string> projectFiles, CancellationToken ct = default);
}
