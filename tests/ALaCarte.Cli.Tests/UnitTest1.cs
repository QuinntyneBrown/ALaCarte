namespace ALaCarte.Cli.Tests;

public class ProjectDiscoveryTests
{
    [Fact]
    public async Task DiscoverDotNetProjects_ReturnsEmptyList_WhenNoSubmodulesExist()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        try
        {
            // Act
            var projects = await ProjectDiscovery.DiscoverDotNetProjects(tempPath);

            // Assert
            Assert.Empty(projects);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task DiscoverAngularProjects_ReturnsEmptyList_WhenNoSubmodulesExist()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        try
        {
            // Act
            var projects = await ProjectDiscovery.DiscoverAngularProjects(tempPath);

            // Assert
            Assert.Empty(projects);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task DiscoverDotNetProjects_FindsCsprojFiles_InSubmodules()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var submodulesPath = Path.Combine(tempPath, "submodules", "test-repo");
        Directory.CreateDirectory(submodulesPath);

        var csprojPath = Path.Combine(submodulesPath, "TestProject.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        try
        {
            // Act
            var projects = await ProjectDiscovery.DiscoverDotNetProjects(tempPath);

            // Assert
            Assert.Single(projects);
            Assert.Contains("TestProject.csproj", projects[0]);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task DiscoverAngularProjects_FindsAngularJson_InSubmodules()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var submodulesPath = Path.Combine(tempPath, "submodules", "test-repo");
        Directory.CreateDirectory(submodulesPath);

        var angularJsonPath = Path.Combine(submodulesPath, "angular.json");
        File.WriteAllText(angularJsonPath, "{}");

        try
        {
            // Act
            var projects = await ProjectDiscovery.DiscoverAngularProjects(tempPath);

            // Assert
            Assert.Single(projects);
            Assert.Equal(submodulesPath, projects[0]);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }
}
