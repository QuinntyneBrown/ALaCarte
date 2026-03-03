using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;
using ALaCarte.Core.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.Core.UnitTests.Services;

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
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        var result = await _sut.DiscoverDotNetProjectsAsync("/test/solution");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverDotNetProjectsAsync_FindsCsprojFiles()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var csprojPath = Path.Combine(submodulesPath, "repo", "src", "Project.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { csprojPath });

        var result = await _sut.DiscoverDotNetProjectsAsync("/test/solution");

        result.Should().ContainSingle().Which.Should().Be(csprojPath);
    }

    [Fact]
    public async Task DiscoverDotNetProjectsAsync_ExcludesObjAndBinDirectories()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var validPath = Path.Combine(submodulesPath, "repo", "src", "Project.csproj");
        var objPath = Path.Combine(submodulesPath, "repo", "obj", "Project.csproj");
        var binPath = Path.Combine(submodulesPath, "repo", "bin", "Project.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { validPath, objPath, binPath });

        var result = await _sut.DiscoverDotNetProjectsAsync("/test/solution");

        result.Should().ContainSingle().Which.Should().Be(validPath);
    }

    [Fact]
    public async Task DiscoverDotNetProjectsAsync_AppliesProjectFilters()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var project1 = Path.Combine(submodulesPath, "repo", "ProjectA.csproj");
        var project2 = Path.Combine(submodulesPath, "repo", "ProjectB.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { project1, project2 });

        var result = await _sut.DiscoverDotNetProjectsAsync("/test/solution", new[] { "ProjectA" });

        result.Should().ContainSingle().Which.Should().Be(project1);
    }

    [Fact]
    public async Task DiscoverDotNetProjectsAsync_ReturnsAll_WhenNoFilters()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var project1 = Path.Combine(submodulesPath, "repo", "ProjectA.csproj");
        var project2 = Path.Combine(submodulesPath, "repo", "ProjectB.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { project1, project2 });

        var result = await _sut.DiscoverDotNetProjectsAsync("/test/solution", null);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task DiscoverDotNetProjectsAsync_ReturnsAll_WhenEmptyFilters()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var project1 = Path.Combine(submodulesPath, "repo", "ProjectA.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { project1 });

        var result = await _sut.DiscoverDotNetProjectsAsync("/test/solution", Array.Empty<string>());

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_ReturnsEmptyList_WhenSubmodulesFolderNotExists()
    {
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        var result = await _sut.DiscoverAngularProjectsAsync("/test/solution");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_FindsAngularJson()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var angularJsonPath = Path.Combine(submodulesPath, "repo", "angular.json");
        var angularJson = """
        {
            "projects": {
                "my-app": { "root": "projects/my-app", "projectType": "application" }
            }
        }
        """;

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { angularJsonPath });
        _fileSystem.ReadAllTextAsync(angularJsonPath, Arg.Any<CancellationToken>())
            .Returns(angularJson);

        var result = await _sut.DiscoverAngularProjectsAsync("/test/solution");

        result.Should().ContainSingle();
        result[0].ProjectNames.Should().Contain("my-app");
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_HandlesInvalidAngularJson()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var angularJsonPath = Path.Combine(submodulesPath, "repo", "angular.json");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { angularJsonPath });
        _fileSystem.ReadAllTextAsync(angularJsonPath, Arg.Any<CancellationToken>())
            .Returns("invalid json");

        var result = await _sut.DiscoverAngularProjectsAsync("/test/solution");

        result.Should().ContainSingle();
        result[0].ProjectNames.Should().Contain("*");
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_FiltersProjectsByName()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var angularJsonPath = Path.Combine(submodulesPath, "repo", "angular.json");
        var angularJson = """
        {
            "projects": {
                "app1": { "root": "projects/app1" },
                "app2": { "root": "projects/app2" }
            }
        }
        """;

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { angularJsonPath });
        _fileSystem.ReadAllTextAsync(angularJsonPath, Arg.Any<CancellationToken>())
            .Returns(angularJson);

        var result = await _sut.DiscoverAngularProjectsAsync("/test/solution", new[] { "app1" });

        result.Should().ContainSingle();
        result[0].ProjectNames.Should().Contain("app1");
        result[0].ProjectNames.Should().NotContain("app2");
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_SkipsWorkspace_WhenNoMatchingProjects()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var angularJsonPath = Path.Combine(submodulesPath, "repo", "angular.json");
        var angularJson = """
        {
            "projects": {
                "app1": { "root": "projects/app1" }
            }
        }
        """;

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { angularJsonPath });
        _fileSystem.ReadAllTextAsync(angularJsonPath, Arg.Any<CancellationToken>())
            .Returns(angularJson);

        var result = await _sut.DiscoverAngularProjectsAsync("/test/solution", new[] { "nonexistent" });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_SkipsWorkspace_WhenDirNameIsNull()
    {
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { "angular.json" });
        _fileSystem.GetDirectoryName("angular.json").Returns((string?)null);

        var result = await _sut.DiscoverAngularProjectsAsync("/test/solution");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_HandlesJsonWithoutProjectsProperty()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var angularJsonPath = Path.Combine(submodulesPath, "repo", "angular.json");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { angularJsonPath });
        _fileSystem.ReadAllTextAsync(angularJsonPath, Arg.Any<CancellationToken>())
            .Returns("""{ "version": 1 }""");

        var result = await _sut.DiscoverAngularProjectsAsync("/test/solution");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_HandlesProjectWithoutRootProperty()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var angularJsonPath = Path.Combine(submodulesPath, "repo", "angular.json");
        var angularJson = """
        { "projects": { "my-app": { "projectType": "application" } } }
        """;

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { angularJsonPath });
        _fileSystem.ReadAllTextAsync(angularJsonPath, Arg.Any<CancellationToken>())
            .Returns(angularJson);

        var result = await _sut.DiscoverAngularProjectsAsync("/test/solution");

        result.Should().ContainSingle();
        result[0].ProjectNames.Should().Contain("my-app");
    }

    [Fact]
    public async Task DiscoverAngularProjectsAsync_InvalidJson_WithFilter_NoMatch_ExcludesWorkspace()
    {
        var submodulesPath = Path.Combine("/test/solution", "submodules");
        var angularJsonPath = Path.Combine(submodulesPath, "repo", "angular.json");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { angularJsonPath });
        _fileSystem.ReadAllTextAsync(angularJsonPath, Arg.Any<CancellationToken>())
            .Returns("invalid json");

        var result = await _sut.DiscoverAngularProjectsAsync("/test/solution", new[] { "nonexistent-project" });

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("ProjectName", "some/path/ProjectName.csproj", true)]
    [InlineData("Other", "some/path/ProjectName.csproj", false)]
    [InlineData("repo/ProjectName", "repo/ProjectName/src/file.csproj", true)]
    [InlineData("ProjectName", "repo/ProjectName/src/file.csproj", true)]
    public void MatchesFilter_ReturnsExpectedResult(string filter, string relativePath, bool expected)
    {
        var result = _sut.MatchesFilter("fullpath/" + relativePath, relativePath, new[] { filter });
        result.Should().Be(expected);
    }

    [Fact]
    public void MatchesFilter_NormalizesBackslashes()
    {
        var result = _sut.MatchesFilter("fullpath\\repo\\Project.csproj", "repo\\Project", new[] { "repo/Project" });
        result.Should().BeTrue();
    }

    [Fact]
    public void MatchesFilter_MatchesByProjectName_CaseInsensitive()
    {
        var result = _sut.MatchesFilter("path/MyProject.csproj", "repo/MyProject.csproj", new[] { "myproject" });
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("test", "test", true)]
    [InlineData("test*", "testing", true)]
    [InlineData("test*", "test123", true)]
    [InlineData("test*", "tests/file", false)]
    [InlineData("**/test", "a/b/test", true)]
    [InlineData("a/**/c", "a/b/c", true)]
    [InlineData("a/**/c", "a/x/y/c", true)]
    [InlineData("test?", "test1", true)]
    [InlineData("test?", "testAB", false)]
    public void MatchesWildcard_ReturnsExpectedResult(string pattern, string input, bool expected)
    {
        var result = ProjectDiscoveryService.MatchesWildcard(input, pattern);
        result.Should().Be(expected);
    }
}
