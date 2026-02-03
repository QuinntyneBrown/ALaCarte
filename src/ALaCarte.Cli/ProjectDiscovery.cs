using System.Text.RegularExpressions;

namespace ALaCarte.Cli;

public static class ProjectDiscovery
{
    public static async Task<List<string>> DiscoverDotNetProjects(string solutionPath, string[]? projectFilters = null)
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
                // Apply project filters if specified
                if (projectFilters == null || projectFilters.Length == 0 || MatchesFilter(csproj, relativePath, projectFilters))
                {
                    projects.Add(csproj);
                }
            }
        }

        return projects;
    }

    public static async Task<List<string>> DiscoverAngularProjects(string solutionPath, string[]? projectFilters = null)
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
                var relativePath = Path.GetRelativePath(submodulesPath, projectDir);

                // Apply project filters if specified
                if (projectFilters == null || projectFilters.Length == 0 || MatchesFilter(projectDir, relativePath, projectFilters))
                {
                    projects.Add(projectDir);
                }
            }
        }

        return projects;
    }

    private static bool MatchesFilter(string fullPath, string relativePath, string[] filters)
    {
        var projectName = Path.GetFileNameWithoutExtension(fullPath);
        var normalizedRelativePath = relativePath.Replace('\\', '/');

        foreach (var filter in filters)
        {
            var normalizedFilter = filter.Replace('\\', '/');

            // Check for exact project name match
            if (projectName.Equals(normalizedFilter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check if filter matches the relative path (supports wildcards)
            if (MatchesWildcard(normalizedRelativePath, normalizedFilter))
            {
                return true;
            }

            // Check if filter is contained in the path (for partial path matches)
            if (normalizedRelativePath.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesWildcard(string input, string pattern)
    {
        // Convert wildcard pattern to regex
        // * matches any sequence of characters except /
        // ** matches any sequence of characters including /
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }
}
