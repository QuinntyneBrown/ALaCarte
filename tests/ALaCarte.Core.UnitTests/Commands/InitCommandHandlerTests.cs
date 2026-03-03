using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.Core.UnitTests.Commands;

public class InitCommandHandlerTests
{
    private readonly IGitService _gitService;
    private readonly IProjectDiscoveryService _projectDiscovery;
    private readonly IDotNetService _dotNetService;
    private readonly IAngularService _angularService;
    private readonly IFileSystem _fileSystem;
    private readonly InitCommandHandler _sut;

    public InitCommandHandlerTests()
    {
        _gitService = Substitute.For<IGitService>();
        _projectDiscovery = Substitute.For<IProjectDiscoveryService>();
        _dotNetService = Substitute.For<IDotNetService>();
        _angularService = Substitute.For<IAngularService>();
        _fileSystem = Substitute.For<IFileSystem>();

        _fileSystem.GetFullPath(Arg.Any<string>())
            .Returns(ci => Path.GetFullPath(ci.ArgAt<string>(0)));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        _sut = new InitCommandHandler(
            _gitService,
            _projectDiscovery,
            _dotNetService,
            _angularService,
            _fileSystem,
            NullLogger<InitCommandHandler>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsError_WhenFolderExists()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);

        var result = await _sut.ExecuteAsync(repos, "main", "existing-folder");

        result.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesSolutionFolder()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        SetupEmptyDiscovery();

        await _sut.ExecuteAsync(repos, "main", "test-folder");

        _fileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("test-folder")));
    }

    [Fact]
    public async Task ExecuteAsync_InitializesGitRepository()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        SetupEmptyDiscovery();

        await _sut.ExecuteAsync(repos, "main", "test-folder");

        await _gitService.Received(1).InitializeRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AddsSubmodules()
    {
        var repos = new[] { "https://github.com/user/repo1.git", "https://github.com/user/repo2.git" };
        SetupEmptyDiscovery();

        await _sut.ExecuteAsync(repos, "develop", "test-folder");

        await _gitService.Received(1).AddSubmodulesAsync(
            Arg.Any<string>(),
            Arg.Is<string[]>(r => r.Length == 2),
            "develop",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CreatesDotNetSolution_WhenProjectsFound()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        var projects = new List<string> { "/path/to/Project.csproj" };

        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(projects);
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        await _sut.ExecuteAsync(repos, "main", "test-folder");

        await _dotNetService.Received(1).CreateSolutionAsync(
            Arg.Any<string>(),
            Arg.Is<List<string>>(p => p.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreateDotNetSolution_WhenNoProjectsFound()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        SetupEmptyDiscovery();

        await _sut.ExecuteAsync(repos, "main", "test-folder");

        await _dotNetService.DidNotReceive().CreateSolutionAsync(
            Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CreatesAngularWorkspace_WhenProjectsFound()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        var angularWorkspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace("/path/to/workspace", new List<string> { "app1" })
        };

        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(angularWorkspaces);

        await _sut.ExecuteAsync(repos, "main", "test-folder");

        await _angularService.Received(1).CreateWorkspaceAsync(
            Arg.Any<string>(),
            Arg.Is<List<AngularWorkspace>>(w => w.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CountsWildcardWorkspace_AsOne()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        var angularWorkspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace("/path", new List<string> { "*" })
        };

        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(angularWorkspaces);

        var result = await _sut.ExecuteAsync(repos, "main", "test-folder");

        result.Should().Be(0);
        await _angularService.Received(1).CreateWorkspaceAsync(
            Arg.Any<string>(), Arg.Any<List<AngularWorkspace>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreateAngularWorkspace_WhenNoProjectsFound()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        SetupEmptyDiscovery();

        await _sut.ExecuteAsync(repos, "main", "test-folder");

        await _angularService.DidNotReceive().CreateWorkspaceAsync(
            Arg.Any<string>(), Arg.Any<List<AngularWorkspace>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PassesProjectFilters()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        var filters = new[] { "ProjectA", "ProjectB" };
        SetupEmptyDiscovery();

        await _sut.ExecuteAsync(repos, "main", "test-folder", filters);

        await _projectDiscovery.Received(1).DiscoverDotNetProjectsAsync(
            Arg.Any<string>(),
            Arg.Is<string[]>(f => f.Length == 2 && f.Contains("ProjectA") && f.Contains("ProjectB")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_LogsProjectFilters_WhenProvided()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        var filters = new[] { "ProjectA" };
        SetupEmptyDiscovery();

        var result = await _sut.ExecuteAsync(repos, "main", "test-folder", filters);

        result.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotLogProjectFilters_WhenEmpty()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        SetupEmptyDiscovery();

        var result = await _sut.ExecuteAsync(repos, "main", "test-folder", Array.Empty<string>());

        result.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsZero_OnSuccess()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        SetupEmptyDiscovery();

        var result = await _sut.ExecuteAsync(repos, "main", "test-folder");

        result.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsOne_OnException()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        _gitService.InitializeRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new Exception("Git error"));

        var result = await _sut.ExecuteAsync(repos, "main", "test-folder");

        result.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsOne_OnCancellation()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _gitService.InitializeRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new OperationCanceledException());

        var result = await _sut.ExecuteAsync(repos, "main", "test-folder", null, cts.Token);

        result.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesTimestampedFolder_WhenNoFolderSpecified()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        SetupEmptyDiscovery();

        await _sut.ExecuteAsync(repos, "main", null);

        _fileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("alacarte-")));
    }

    [Fact]
    public async Task ExecuteAsync_PassesNullProjectFilters_WhenNotProvided()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        SetupEmptyDiscovery();

        await _sut.ExecuteAsync(repos, "main", "test-folder");

        await _projectDiscovery.Received(1).DiscoverDotNetProjectsAsync(
            Arg.Any<string>(), null, Arg.Any<CancellationToken>());
    }

    private void SetupEmptyDiscovery()
    {
        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());
    }
}
