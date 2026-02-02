using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ALaCarte.Cli;

public static class DotNetIntegration
{
    public static async Task CreateSolution(string solutionPath, List<string> projectFiles)
    {
        Console.WriteLine("Creating .NET solution...");

        var srcPath = Path.Combine(solutionPath, "src");
        Directory.CreateDirectory(srcPath);

        var solutionFile = Path.Combine(solutionPath, "Solution.sln");
        
        // Create solution
        await RunDotNetCommand($"new sln -n Solution -o \"{solutionPath}\"", solutionPath);

        var projectMap = new Dictionary<string, string>(); // oldPath -> newPath
        var packageToProject = new Dictionary<string, string>(); // packageName -> projectPath

        // First pass: Copy projects and build mapping
        foreach (var oldProjectPath in projectFiles)
        {
            var projectName = Path.GetFileNameWithoutExtension(oldProjectPath);
            var newProjectPath = Path.Combine(srcPath, projectName);
            
            Console.WriteLine($"  Processing project: {projectName}");

            // Copy project directory
            CopyDirectory(Path.GetDirectoryName(oldProjectPath)!, newProjectPath);

            var newCsprojPath = Path.Combine(newProjectPath, $"{projectName}.csproj");
            projectMap[oldProjectPath] = newCsprojPath;

            // Read PackageId if specified
            var packageId = GetPackageId(newCsprojPath) ?? projectName;
            packageToProject[packageId] = newCsprojPath;

            // Strip relative file references
            await StripRelativeReferences(newCsprojPath);
        }

        // Second pass: Replace NuGet references with project references
        foreach (var projectPath in projectMap.Values)
        {
            await ReplaceNuGetWithProjectReferences(projectPath, packageToProject, projectMap);
        }

        // Add all projects to solution
        foreach (var projectPath in projectMap.Values)
        {
            var relativePath = Path.GetRelativePath(solutionPath, projectPath);
            await RunDotNetCommand($"sln \"{solutionFile}\" add \"{relativePath}\"", solutionPath);
        }

        Console.WriteLine($"✓ Created solution with {projectMap.Count} project(s)");
    }

    private static string? GetPackageId(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            return doc.Descendants("PackageId").FirstOrDefault()?.Value;
        }
        catch
        {
            return null;
        }
    }

    private static async Task StripRelativeReferences(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            bool modified = false;

            // Remove relative file references (references starting with ..)
            var relativeReferences = doc.Descendants("Reference")
                .Where(r => r.Attribute("Include")?.Value.StartsWith("..") == true)
                .ToList();

            foreach (var reference in relativeReferences)
            {
                reference.Remove();
                modified = true;
            }

            // Remove Compile items with relative paths
            var relativeCompileItems = doc.Descendants("Compile")
                .Where(c => c.Attribute("Include")?.Value.StartsWith("..") == true)
                .ToList();

            foreach (var item in relativeCompileItems)
            {
                item.Remove();
                modified = true;
            }

            // Remove Content items with relative paths
            var relativeContentItems = doc.Descendants("Content")
                .Where(c => c.Attribute("Include")?.Value.StartsWith("..") == true)
                .ToList();

            foreach (var item in relativeContentItems)
            {
                item.Remove();
                modified = true;
            }

            // Remove None items with relative paths
            var relativeNoneItems = doc.Descendants("None")
                .Where(n => n.Attribute("Include")?.Value.StartsWith("..") == true)
                .ToList();

            foreach (var item in relativeNoneItems)
            {
                item.Remove();
                modified = true;
            }

            if (modified)
            {
                doc.Save(csprojPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Failed to strip relative references: {ex.Message}");
        }
    }

    private static async Task ReplaceNuGetWithProjectReferences(
        string csprojPath, 
        Dictionary<string, string> packageToProject,
        Dictionary<string, string> projectMap)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            bool modified = false;

            // Find PackageReferences that match project names
            var packageReferences = doc.Descendants("PackageReference").ToList();

            foreach (var packageRef in packageReferences)
            {
                var packageName = packageRef.Attribute("Include")?.Value;
                if (packageName != null && packageToProject.ContainsKey(packageName))
                {
                    var targetProjectPath = packageToProject[packageName];
                    var relativePath = Path.GetRelativePath(
                        Path.GetDirectoryName(csprojPath)!, 
                        targetProjectPath);

                    // Remove PackageReference
                    var parent = packageRef.Parent;
                    packageRef.Remove();

                    // Add ProjectReference
                    var projectReference = new XElement("ProjectReference",
                        new XAttribute("Include", relativePath));
                    
                    if (parent != null && parent.Name == "ItemGroup")
                    {
                        parent.Add(projectReference);
                    }
                    else
                    {
                        // Find or create ItemGroup for ProjectReferences
                        var itemGroup = doc.Descendants("ItemGroup")
                            .FirstOrDefault(ig => ig.Elements("ProjectReference").Any());
                        
                        if (itemGroup == null)
                        {
                            itemGroup = new XElement("ItemGroup");
                            doc.Root?.Add(itemGroup);
                        }
                        
                        itemGroup.Add(projectReference);
                    }

                    modified = true;
                    Console.WriteLine($"    Replaced NuGet '{packageName}' with project reference");
                }
            }

            if (modified)
            {
                doc.Save(csprojPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Failed to replace NuGet references: {ex.Message}");
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            
            // Skip obj and bin directories
            if (dirName == "obj" || dirName == "bin")
                continue;

            var destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(dir, destSubDir);
        }
    }

    private static async Task<string> RunDotNetCommand(string arguments, string workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
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

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            throw new Exception($"dotnet command failed: {error}");
        }

        return output;
    }
}
