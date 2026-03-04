namespace ALaCarte.Core.Abstractions;

public enum SolutionFormat { Auto, Sln, Slnx }

public interface IDotNetService
{
    Task CreateSolutionAsync(string solutionPath, List<string> projectFiles, CancellationToken ct = default, SolutionFormat format = SolutionFormat.Auto);
}
