namespace ALaCarte.Cli;

public static class ProjectDiscovery
{
    public static async Task<List<string>> DiscoverDotNetProjects(string solutionPath)
    {
        var projects = new List<string>();
        var submodulesPath = Path.Combine(solutionPath, "submodules");

        if (!Directory.Exists(submodulesPath))
        {
            return projects;
        }

        // Find all .csproj files in submodules
        var csprojFiles = Directory.GetFiles(submodulesPath, "*.csproj", SearchOption.AllDirectories);
        
        foreach (var csproj in csprojFiles)
        {
            // Exclude test projects and obj/bin directories
            var relativePath = Path.GetRelativePath(submodulesPath, csproj);
            if (!relativePath.Contains("/obj/") && 
                !relativePath.Contains("/bin/") && 
                !relativePath.Contains("\\obj\\") && 
                !relativePath.Contains("\\bin\\"))
            {
                projects.Add(csproj);
            }
        }

        return await Task.FromResult(projects);
    }

    public static async Task<List<string>> DiscoverAngularProjects(string solutionPath)
    {
        var projects = new List<string>();
        var submodulesPath = Path.Combine(solutionPath, "submodules");

        if (!Directory.Exists(submodulesPath))
        {
            return projects;
        }

        // Find all angular.json files in submodules
        var angularFiles = Directory.GetFiles(submodulesPath, "angular.json", SearchOption.AllDirectories);
        
        foreach (var angularFile in angularFiles)
        {
            var projectDir = Path.GetDirectoryName(angularFile);
            if (projectDir != null)
            {
                projects.Add(projectDir);
            }
        }

        return await Task.FromResult(projects);
    }
}
