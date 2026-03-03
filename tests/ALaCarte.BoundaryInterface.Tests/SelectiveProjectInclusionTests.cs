using ALaCarte.BoundaryInterface.Tests.Helpers;
using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.BoundaryInterface.Tests;

/// <summary>
/// Boundary interface tests for L2-2: Selective Project Inclusion [L1-2]
/// </summary>
public class SelectiveProjectInclusionTests
{
    private readonly IFileSystem _fileSystem;
    private readonly ProjectDiscoveryService _discoveryService;

    public SelectiveProjectInclusionTests()
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

        _discoveryService = new ProjectDiscoveryService(
            _fileSystem,
            NullLogger<ProjectDiscoveryService>.Instance,
            TestOptionsFactory.CreateAlacarteOptions(),
            TestOptionsFactory.CreateDotNetOptions());
    }

    #region L2-2.1 Project Filtering via CLI

    // L2-2.1 AC: When `--projects` is omitted, all discovered projects are included.
    [Fact]
    public async Task L2_2_1_DotNet_ReturnsAllProjects_WhenProjectFiltersOmitted()
    {
        var submodulesPath = Path.Combine("/workspace", "submodules");
        var project1 = Path.Combine(submodulesPath, "repo", "ProjectA.csproj");
        var project2 = Path.Combine(submodulesPath, "repo", "ProjectB.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { project1, project2 });

        var result = await _discoveryService.DiscoverDotNetProjectsAsync("/workspace", null);

        result.Should().HaveCount(2);
    }

    // L2-2.1 AC: When `--projects` is provided, only projects matching at least one filter are included.
    [Fact]
    public async Task L2_2_1_DotNet_ReturnsOnlyMatchingProjects_WhenFiltersProvided()
    {
        var submodulesPath = Path.Combine("/workspace", "submodules");
        var project1 = Path.Combine(submodulesPath, "repo", "ProjectA.csproj");
        var project2 = Path.Combine(submodulesPath, "repo", "ProjectB.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { project1, project2 });

        var result = await _discoveryService.DiscoverDotNetProjectsAsync("/workspace", new[] { "ProjectA" });

        result.Should().ContainSingle().Which.Should().Be(project1);
    }

    // L2-2.1 AC: Filters apply to both .NET and Angular project discovery.
    [Fact]
    public async Task L2_2_1_Angular_AppliesFilters_ToAngularDiscovery()
    {
        var submodulesPath = Path.Combine("/workspace", "submodules");
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

        var result = await _discoveryService.DiscoverAngularProjectsAsync("/workspace", new[] { "app1" });

        result.Should().ContainSingle();
        result[0].ProjectNames.Should().Contain("app1");
        result[0].ProjectNames.Should().NotContain("app2");
    }

    #endregion

    #region L2-2.2 Filter Matching Rules

    // L2-2.2 AC: Exact name match is case-insensitive (e.g., `ProjectA` matches `ProjectA.csproj`).
    [Fact]
    public void L2_2_2_MatchesFilter_CaseInsensitiveExactMatch()
    {
        var result = _discoveryService.MatchesFilter(
            "path/MyProject.csproj", "repo/MyProject.csproj", new[] { "myproject" });

        result.Should().BeTrue();
    }

    // L2-2.2 AC: Wildcard `*` matches any characters within a single path segment (e.g., `test*` matches `testing`).
    [Theory]
    [InlineData("test*", "testing", true)]
    [InlineData("test*", "test123", true)]
    [InlineData("test*", "tests/file", false)]
    public void L2_2_2_MatchesWildcard_SingleStar_MatchesWithinSegment(string pattern, string input, bool expected)
    {
        var result = ProjectDiscoveryService.MatchesWildcard(input, pattern);
        result.Should().Be(expected);
    }

    // L2-2.2 AC: Wildcard `**` matches across path separators (e.g., `**/ConferenceEventManager/*` matches nested paths).
    [Theory]
    [InlineData("**/test", "a/b/test", true)]
    [InlineData("a/**/c", "a/b/c", true)]
    [InlineData("a/**/c", "a/x/y/c", true)]
    public void L2_2_2_MatchesWildcard_DoubleStar_MatchesAcrossSegments(string pattern, string input, bool expected)
    {
        var result = ProjectDiscoveryService.MatchesWildcard(input, pattern);
        result.Should().Be(expected);
    }

    // L2-2.2 AC: Wildcard `?` matches a single character.
    [Theory]
    [InlineData("test?", "test1", true)]
    [InlineData("test?", "testAB", false)]
    public void L2_2_2_MatchesWildcard_QuestionMark_MatchesSingleChar(string pattern, string input, bool expected)
    {
        var result = ProjectDiscoveryService.MatchesWildcard(input, pattern);
        result.Should().Be(expected);
    }

    // L2-2.2 AC: Path containment: a filter matches if it appears anywhere in the project's relative path.
    [Fact]
    public void L2_2_2_MatchesFilter_PathContainment()
    {
        var result = _discoveryService.MatchesFilter(
            "fullpath/repo/ProjectName/src/file.csproj",
            "repo/ProjectName/src/file.csproj",
            new[] { "repo/ProjectName" });

        result.Should().BeTrue();
    }

    #endregion
}
