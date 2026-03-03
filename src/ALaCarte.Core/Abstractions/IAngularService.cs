namespace ALaCarte.Core.Abstractions;

public interface IAngularService
{
    Task CreateWorkspaceAsync(string solutionPath, List<AngularWorkspace> angularWorkspaces, CancellationToken ct = default);
}
