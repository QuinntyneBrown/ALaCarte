namespace ALaCarte.Cli.Abstractions;

public interface IAngularService
{
    Task CreateWorkspaceAsync(string solutionPath, List<AngularWorkspace> angularWorkspaces, CancellationToken ct = default);
}
