using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Options;
using ALaCarte.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ALaCarte.BoundaryInterface.Tests;

public class DotNetServiceSolutionFileDetectionTests
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<DotNetService> _logger;
    private readonly DotNetOptions _dotNetOptions;
    private readonly AlacarteOptions _alacarteOptions;
    private readonly DotNetService _service;

    public DotNetServiceSolutionFileDetectionTests()
    {
        _processRunner = Substitute.For<IProcessRunner>();
        _fileSystem = Substitute.For<IFileSystem>();
        _logger = Substitute.For<ILogger<DotNetService>>();
        _dotNetOptions = new DotNetOptions { SolutionName = "Solution" };
        _alacarteOptions = new AlacarteOptions();

        _fileSystem.Combine(Arg.Any<string[]>())
            .Returns(callInfo =>
            {
                var paths = callInfo.Arg<string[]>();
                return string.Join("/", paths);
            });

        _fileSystem.GetFileNameWithoutExtension(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var fileName = path.Split('/').Last();
                var dotIndex = fileName.LastIndexOf('.');
                return dotIndex >= 0 ? fileName[..dotIndex] : fileName;
            });

        _fileSystem.GetDirectoryName(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var lastSlash = path.LastIndexOf('/');
                return lastSlash >= 0 ? path[..lastSlash] : null;
            });

        _fileSystem.GetFileName(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                return path.Split('/').Last();
            });

        _fileSystem.GetRelativePath(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo => callInfo.ArgAt<string>(1));

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        var dotNetOpts = Substitute.For<IOptions<DotNetOptions>>();
        dotNetOpts.Value.Returns(_dotNetOptions);

        var alacarteOpts = Substitute.For<IOptions<AlacarteOptions>>();
        alacarteOpts.Value.Returns(_alacarteOptions);

        _service = new DotNetService(_processRunner, _fileSystem, _logger, dotNetOpts, alacarteOpts);
    }

    [Fact]
    public async Task CreateSolutionAsync_WhenDotNetCreatesSlnxFile_UsesSlnxFileForAddingProjects()
    {
        // Arrange
        var solutionPath = "/workspace";
        var projectFiles = new List<string> { "/source/MyProject/MyProject.csproj" };

        // Simulate .NET 10 creating a .slnx file
        _fileSystem.GetFiles(solutionPath, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns(Array.Empty<string>());
        _fileSystem.GetFiles(solutionPath, "*.slnx", SearchOption.TopDirectoryOnly)
            .Returns(["/workspace/Solution.slnx"]);

        // Act
        await _service.CreateSolutionAsync(solutionPath, projectFiles);

        // Assert - should use the .slnx file, not .sln
        await _processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("Solution.slnx") &&
                !r.Arguments.Contains("Solution.sln\"")));
    }

    [Fact]
    public async Task CreateSolutionAsync_WhenDotNetCreatesSlnFile_UsesSlnFileForAddingProjects()
    {
        // Arrange
        var solutionPath = "/workspace";
        var projectFiles = new List<string> { "/source/MyProject/MyProject.csproj" };

        // Simulate older .NET creating a .sln file
        _fileSystem.GetFiles(solutionPath, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns(["/workspace/Solution.sln"]);

        // Act
        await _service.CreateSolutionAsync(solutionPath, projectFiles);

        // Assert - should use the .sln file
        await _processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("Solution.sln")));
    }

    [Fact]
    public async Task CreateSolutionAsync_WhenNoSolutionFileFound_FallsBackToDefaultSlnName()
    {
        // Arrange
        var solutionPath = "/workspace";
        var projectFiles = new List<string> { "/source/MyProject/MyProject.csproj" };

        // No solution file was created (e.g., mocked process runner)
        _fileSystem.GetFiles(solutionPath, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns(Array.Empty<string>());
        _fileSystem.GetFiles(solutionPath, "*.slnx", SearchOption.TopDirectoryOnly)
            .Returns(Array.Empty<string>());

        // Act
        await _service.CreateSolutionAsync(solutionPath, projectFiles);

        // Assert - falls back to default {SolutionName}.sln
        await _processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("Solution.sln")));
    }

    [Fact]
    public async Task CreateSolutionAsync_PrefersSlnOverSlnxWhenBothExist()
    {
        // Arrange
        var solutionPath = "/workspace";
        var projectFiles = new List<string> { "/source/MyProject/MyProject.csproj" };

        // Both files exist (unlikely but possible)
        _fileSystem.GetFiles(solutionPath, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns(["/workspace/Solution.sln"]);
        _fileSystem.GetFiles(solutionPath, "*.slnx", SearchOption.TopDirectoryOnly)
            .Returns(["/workspace/Solution.slnx"]);

        // Act
        await _service.CreateSolutionAsync(solutionPath, projectFiles);

        // Assert - should prefer .sln
        await _processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("Solution.sln")));
    }
}
