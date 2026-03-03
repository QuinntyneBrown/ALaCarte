// Legacy tests - now covered by Services/ProjectDiscoveryServiceTests.cs
// This file is kept for backward compatibility with existing test runs
// but the tests now use the new service-based architecture

using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;
using ALaCarte.Core.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace ALaCarte.Core.Tests;

public class ProjectDiscoveryTests
{
    [Fact]
    public async Task DiscoverDotNetProjects_ReturnsEmptyList_WhenNoSubmodulesExist()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        var fileSystem = new FileSystemService();
        var service = new ProjectDiscoveryService(
            fileSystem,
            NullLogger<ProjectDiscoveryService>.Instance,
            TestOptionsFactory.CreateAlacarteOptions(),
            TestOptionsFactory.CreateDotNetOptions());

        try
        {
            // Act
            var projects = await service.DiscoverDotNetProjectsAsync(tempPath);

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

        var fileSystem = new FileSystemService();
        var service = new ProjectDiscoveryService(
            fileSystem,
            NullLogger<ProjectDiscoveryService>.Instance,
            TestOptionsFactory.CreateAlacarteOptions(),
            TestOptionsFactory.CreateDotNetOptions());

        try
        {
            // Act
            var projects = await service.DiscoverAngularProjectsAsync(tempPath);

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

        var fileSystem = new FileSystemService();
        var service = new ProjectDiscoveryService(
            fileSystem,
            NullLogger<ProjectDiscoveryService>.Instance,
            TestOptionsFactory.CreateAlacarteOptions(),
            TestOptionsFactory.CreateDotNetOptions());

        try
        {
            // Act
            var projects = await service.DiscoverDotNetProjectsAsync(tempPath);

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

        // Create a valid angular.json with projects
        var angularJsonPath = Path.Combine(submodulesPath, "angular.json");
        File.WriteAllText(angularJsonPath, """
        {
            "projects": {
                "my-app": {
                    "root": "projects/my-app",
                    "projectType": "application"
                }
            }
        }
        """);

        var fileSystem = new FileSystemService();
        var service = new ProjectDiscoveryService(
            fileSystem,
            NullLogger<ProjectDiscoveryService>.Instance,
            TestOptionsFactory.CreateAlacarteOptions(),
            TestOptionsFactory.CreateDotNetOptions());

        try
        {
            // Act
            var projects = await service.DiscoverAngularProjectsAsync(tempPath);

            // Assert
            Assert.Single(projects);
            Assert.Equal(submodulesPath, projects[0].WorkspacePath);
            Assert.Contains("my-app", projects[0].ProjectNames);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }
}
