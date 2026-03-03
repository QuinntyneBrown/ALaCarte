using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;
using ALaCarte.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.Core.Tests.Services;

public class ProjectDiscoveryServiceTests
{
    private readonly IFileSystem _fileSystem;
    private readonly ProjectDiscoveryService _sut;

    public ProjectDiscoveryServiceTests()
    {
        _fileSystem = Substitute.For<IFileSystem>();

        _fileSystem.Combine(Arg.Any<string[]>())
            .Returns(ci => string.Join(Path.DirectorySeparatorChar.ToString(), ci.ArgAt<string[]>(0)));
        _fileSystem.GetRelativePath(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => Path.GetRelativePath(ci.ArgAt<string>(0), ci.ArgAt<string>(1)));
        _fileSystem.GetFileNameWithoutExtension(Arg.Any<string>())
            .Returns(ci => Path.GetFileNameWithoutExtension(ci.ArgAt<string>(0)));
        _fileSystem.GetDirectoryName(Arg.Any<string>())
            .Returns(ci => Path.GetDirectoryName(ci.ArgAt<string>(0)));
        _fileSystem.GetFileName(Arg.Any<string>())
            .Returns(ci => Path.GetFileName(ci.ArgAt<string>(0)));

        _sut = new ProjectDiscoveryService(
            _fileSystem,
            NullLogger<ProjectDiscoveryService>.Instance,
            TestOptionsFactory.CreateAlacarteOptions(),
            TestOptionsFactory.CreateDotNetOptions());
    }

    [Fact]
    public async Task DiscoverDotNetProjectsAsync_ReturnsEmptyList_WhenSubmodulesFolderNotExists()
    {
        // Arrange
        var solutionPath = "/test/solution";
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        // Act
        var result = await _sut.DiscoverDotNetProjectsAsync(solutionPath);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverDotNetProjectsAsync_FindsCsprojFiles()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var submodulesPath = Path.Combine(solutionPath, "submodules");
        var csprojPath = Path.Combine(submodulesPath, "repo", "src", "Project.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { csprojPath });

        // Act
        var result = await _sut.DiscoverDotNetProjectsAsync(solutionPath);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be(csprojPath);
    }

    [Fact]
    public async Task DiscoverDotNetProjectsAsync_ExcludesObjAndBinDirectories()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var submodulesPath = Path.Combine(solutionPath, "submodules");
        var validPath = Path.Combine(submodulesPath, "repo", "src", "Project.csproj");
        var objPath = Path.Combine(submodulesPath, "repo", "obj", "Project.csproj");
        var binPath = Path.Combine(submodulesPath, "repo", "bin", "Project.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { validPath, objPath, binPath });

        // Act
        var result = await _sut.DiscoverDotNetProjectsAsync(solutionPath);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be(validPath);
    }

    [Fact]
    public async Task DiscoverDotNetProjectsAsync_AppliesProjectFilters()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var submodulesPath = Path.Combine(solutionPath, "submodules");
        var project1 = Path.Combine(submodulesPath, "repo", "ProjectA.csproj");
        var project2 = Path.Combine(submodulesPath, "repo", "ProjectB.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { project1, project2 });

        // Act
        var result = await _sut.DiscoverDotNetProjectsAsync(solutionPath, new[] { "ProjectA" });

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be(project1);
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_ReturnsEmptyList_WhenSubmodulesFolderNotExists()
    {
        // Arrange
        var solutionPath = "/test/solution";
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        // Act
        var result = await _sut.DiscoverAngularProjectsAsync(solutionPath);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_FindsAngularJson()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var submodulesPath = Path.Combine(solutionPath, "submodules");
        var angularJsonPath = Path.Combine(submodulesPath, "repo", "angular.json");
        var angularJson = """
        {
            "projects": {
                "my-app": {
                    "root": "projects/my-app",
                    "projectType": "application"
                }
            }
        }
        """;

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { angularJsonPath });
        _fileSystem.ReadAllTextAsync(angularJsonPath, Arg.Any<CancellationToken>())
            .Returns(angularJson);

        // Act
        var result = await _sut.DiscoverAngularProjectsAsync(solutionPath);

        // Assert
        result.Should().ContainSingle();
        result[0].ProjectNames.Should().Contain("my-app");
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_HandlesInvalidAngularJson()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var submodulesPath = Path.Combine(solutionPath, "submodules");
        var angularJsonPath = Path.Combine(submodulesPath, "repo", "angular.json");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { angularJsonPath });
        _fileSystem.ReadAllTextAsync(angularJsonPath, Arg.Any<CancellationToken>())
            .Returns("invalid json");

        // Act
        var result = await _sut.DiscoverAngularProjectsAsync(solutionPath);

        // Assert - should include workspace with "*" marker
        result.Should().ContainSingle();
        result[0].ProjectNames.Should().Contain("*");
    }

    [Theory]
    [InlineData("ProjectName", "some/path/ProjectName.csproj", true)]
    [InlineData("Other", "some/path/ProjectName.csproj", false)]
    [InlineData("repo/ProjectName", "repo/ProjectName/src/file.csproj", true)] // partial path match
    [InlineData("ProjectName", "repo/ProjectName/src/file.csproj", true)] // contained in path
    public void MatchesFilter_ReturnsExpectedResult(string filter, string relativePath, bool expected)
    {
        // Act
        var result = _sut.MatchesFilter("fullpath/" + relativePath, relativePath, new[] { filter });

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("test", "test", true)]
    [InlineData("test*", "testing", true)]
    [InlineData("test*", "test123", true)]
    [InlineData("test*", "tests/file", false)]
    [InlineData("**/test", "a/b/test", true)]
    [InlineData("a/**/c", "a/b/c", true)]
    [InlineData("a/**/c", "a/x/y/c", true)]
    public void MatchesWildcard_ReturnsExpectedResult(string pattern, string input, bool expected)
    {
        // Act
        var result = ProjectDiscoveryService.MatchesWildcard(input, pattern);

        // Assert
        result.Should().Be(expected);
    }
}
