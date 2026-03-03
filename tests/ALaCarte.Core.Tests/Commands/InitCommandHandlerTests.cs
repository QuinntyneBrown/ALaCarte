using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.Core.Tests.Commands;

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
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);

        // Act
        var result = await _sut.ExecuteAsync(repos, "main", "existing-folder");

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesSolutionFolder()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        // Act
        await _sut.ExecuteAsync(repos, "main", "test-folder");

        // Assert
        _fileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("test-folder")));
    }

    [Fact]
    public async Task ExecuteAsync_InitializesGitRepository()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        // Act
        await _sut.ExecuteAsync(repos, "main", "test-folder");

        // Assert
        await _gitService.Received(1).InitializeRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AddsSubmodules()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo1.git", "https://github.com/user/repo2.git" };
        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        // Act
        await _sut.ExecuteAsync(repos, "develop", "test-folder");

        // Assert
        await _gitService.Received(1).AddSubmodulesAsync(
            Arg.Any<string>(),
            Arg.Is<string[]>(r => r.Length == 2),
            "develop",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CreatesDotNetSolution_WhenProjectsFound()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        var projects = new List<string> { "/path/to/Project.csproj" };

        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(projects);
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        // Act
        await _sut.ExecuteAsync(repos, "main", "test-folder");

        // Assert
        await _dotNetService.Received(1).CreateSolutionAsync(
            Arg.Any<string>(),
            Arg.Is<List<string>>(p => p.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreateDotNetSolution_WhenNoProjectsFound()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        // Act
        await _sut.ExecuteAsync(repos, "main", "test-folder");

        // Assert
        await _dotNetService.DidNotReceive().CreateSolutionAsync(
            Arg.Any<string>(),
            Arg.Any<List<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CreatesAngularWorkspace_WhenProjectsFound()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        var angularWorkspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace("/path/to/workspace", new List<string> { "app1" })
        };

        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(angularWorkspaces);

        // Act
        await _sut.ExecuteAsync(repos, "main", "test-folder");

        // Assert
        await _angularService.Received(1).CreateWorkspaceAsync(
            Arg.Any<string>(),
            Arg.Is<List<AngularWorkspace>>(w => w.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreateAngularWorkspace_WhenNoProjectsFound()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        // Act
        await _sut.ExecuteAsync(repos, "main", "test-folder");

        // Assert
        await _angularService.DidNotReceive().CreateWorkspaceAsync(
            Arg.Any<string>(),
            Arg.Any<List<AngularWorkspace>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PassesProjectFilters()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        var filters = new[] { "ProjectA", "ProjectB" };

        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        // Act
        await _sut.ExecuteAsync(repos, "main", "test-folder", filters);

        // Assert
        await _projectDiscovery.Received(1).DiscoverDotNetProjectsAsync(
            Arg.Any<string>(),
            Arg.Is<string[]>(f => f.Length == 2 && f.Contains("ProjectA") && f.Contains("ProjectB")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsZero_OnSuccess()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        // Act
        var result = await _sut.ExecuteAsync(repos, "main", "test-folder");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsOne_OnException()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        _gitService.InitializeRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new Exception("Git error"));

        // Act
        var result = await _sut.ExecuteAsync(repos, "main", "test-folder");

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsOne_OnCancellation()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _gitService.InitializeRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new OperationCanceledException());

        // Act
        var result = await _sut.ExecuteAsync(repos, "main", "test-folder", null, cts.Token);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesTimestampedFolder_WhenNoFolderSpecified()
    {
        // Arrange
        var repos = new[] { "https://github.com/user/repo.git" };
        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        // Act
        await _sut.ExecuteAsync(repos, "main", null);

        // Assert
        _fileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("alacarte-")));
    }
}
