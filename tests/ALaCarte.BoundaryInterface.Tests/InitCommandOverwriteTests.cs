using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ALaCarte.BoundaryInterface.Tests;

public class InitCommandOverwriteTests
{
    private readonly IGitService _gitService;
    private readonly IProjectDiscoveryService _projectDiscovery;
    private readonly IDotNetService _dotNetService;
    private readonly IAngularService _angularService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<InitCommandHandler> _logger;
    private readonly InitCommandHandler _handler;

    public InitCommandOverwriteTests()
    {
        _gitService = Substitute.For<IGitService>();
        _projectDiscovery = Substitute.For<IProjectDiscoveryService>();
        _dotNetService = Substitute.For<IDotNetService>();
        _angularService = Substitute.For<IAngularService>();
        _fileSystem = Substitute.For<IFileSystem>();
        _logger = Substitute.For<ILogger<InitCommandHandler>>();

        _fileSystem.GetFullPath(Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<string>());

        _projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        _handler = new InitCommandHandler(
            _gitService, _projectDiscovery, _dotNetService, _angularService,
            _fileSystem, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFolderExistsAndOverwriteFalse_ReturnsError()
    {
        _fileSystem.DirectoryExists("my-folder").Returns(true);

        var result = await _handler.ExecuteAsync(
            repos: ["https://github.com/org/repo.git"],
            branch: "main",
            folder: "my-folder",
            overwrite: false);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFolderExistsAndOverwriteTrue_Proceeds()
    {
        _fileSystem.DirectoryExists("my-folder").Returns(true);

        var result = await _handler.ExecuteAsync(
            repos: ["https://github.com/org/repo.git"],
            branch: "main",
            folder: "my-folder",
            overwrite: true);

        Assert.Equal(0, result);
        await _gitService.Received().InitializeRepositoryAsync("my-folder", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenFolderDoesNotExist_SucceedsRegardlessOfOverwrite()
    {
        _fileSystem.DirectoryExists("new-folder").Returns(false);

        var result = await _handler.ExecuteAsync(
            repos: ["https://github.com/org/repo.git"],
            branch: "main",
            folder: "new-folder",
            overwrite: false);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_OverwriteDefaultsToFalse()
    {
        _fileSystem.DirectoryExists("existing").Returns(true);

        // Call without overwrite parameter — should default to false and fail
        var result = await _handler.ExecuteAsync(
            repos: ["https://github.com/org/repo.git"],
            branch: "main",
            folder: "existing");

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOverwriteTrue_StillCreatesDirectoryIfNotExists()
    {
        _fileSystem.DirectoryExists("new-folder").Returns(false);

        var result = await _handler.ExecuteAsync(
            repos: ["https://github.com/org/repo.git"],
            branch: "main",
            folder: "new-folder",
            overwrite: true);

        Assert.Equal(0, result);
        _fileSystem.Received().CreateDirectory("new-folder");
    }
}
