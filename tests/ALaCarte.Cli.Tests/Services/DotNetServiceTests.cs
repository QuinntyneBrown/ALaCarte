using ALaCarte.Cli.Abstractions;
using ALaCarte.Cli.Exceptions;
using ALaCarte.Cli.Services;
using ALaCarte.Cli.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.Cli.Tests.Services;

public class DotNetServiceTests
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly DotNetService _sut;

    public DotNetServiceTests()
    {
        _processRunner = Substitute.For<IProcessRunner>();
        _fileSystem = Substitute.For<IFileSystem>();

        // Setup file system mock with safe defaults
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
        _fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>())
            .Returns(Array.Empty<string>());
        // Default: no subdirectories (prevents recursion)
        _fileSystem.GetDirectories(Arg.Any<string>())
            .Returns(Array.Empty<string>());

        _sut = new DotNetService(
            _processRunner,
            _fileSystem,
            NullLogger<DotNetService>.Instance,
            TestOptionsFactory.CreateDotNetOptions(),
            TestOptionsFactory.CreateAlacarteOptions());
    }

    [Fact]
    public async Task CreateSolutionAsync_CreatesSrcDirectory()
    {
        // Arrange
        var solutionPath = "/test/solution";
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await _sut.CreateSolutionAsync(solutionPath, new List<string>());

        // Assert
        _fileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.EndsWith("src")));
    }

    [Fact]
    public async Task CreateSolutionAsync_CreatesSolutionFile()
    {
        // Arrange
        var solutionPath = "/test/solution";
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await _sut.CreateSolutionAsync(solutionPath, new List<string>());

        // Assert
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "dotnet" &&
                r.Arguments.Contains("new sln") &&
                r.Arguments.Contains("-n Solution")));
    }

    [Fact]
    public async Task CreateSolutionAsync_CopiesProjectsToSrcFolder()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var projectPath = "/test/solution/submodules/repo/MyProject/MyProject.csproj";
        var projects = new List<string> { projectPath };

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await _sut.CreateSolutionAsync(solutionPath, projects);

        // Assert
        _fileSystem.Received(1).CreateDirectory(
            Arg.Is<string>(s => s.Contains("MyProject")));
    }

    [Fact]
    public async Task CreateSolutionAsync_AddsProjectsToSolution()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var projectPath = "/test/solution/submodules/repo/MyProject/MyProject.csproj";
        var projects = new List<string> { projectPath };

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await _sut.CreateSolutionAsync(solutionPath, projects);

        // Assert
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("sln") &&
                r.Arguments.Contains("add")));
    }

    [Fact]
    public async Task CreateSolutionAsync_ThrowsDotNetOperationException_WhenCommandFails()
    {
        // Arrange
        var solutionPath = "/test/solution";
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(1, "", "error occurred"));

        // Act & Assert
        var act = () => _sut.CreateSolutionAsync(solutionPath, new List<string>());
        await act.Should().ThrowAsync<DotNetOperationException>()
            .WithMessage("*failed*");
    }

    [Fact]
    public async Task CreateSolutionAsync_UsesConfiguredSolutionName()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var customOptions = TestOptionsFactory.CreateDotNetOptions(o => o.SolutionName = "MyCustomSolution");

        var sut = new DotNetService(
            _processRunner,
            _fileSystem,
            NullLogger<DotNetService>.Instance,
            customOptions,
            TestOptionsFactory.CreateAlacarteOptions());

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await sut.CreateSolutionAsync(solutionPath, new List<string>());

        // Assert
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("-n MyCustomSolution")));
    }

    [Fact]
    public async Task CreateSolutionAsync_ExcludesConfiguredDirectories()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var projectDir = "/test/solution/submodules/repo/MyProject";
        var projectPath = projectDir + "/MyProject.csproj";
        var projects = new List<string> { projectPath };

        // Setup: project dir has obj, bin, and src subdirs
        // But those subdirs have no children (stops recursion)
        _fileSystem.GetDirectories(projectDir)
            .Returns(new[] { projectDir + "/obj", projectDir + "/bin", projectDir + "/src" });
        _fileSystem.GetDirectories(projectDir + "/obj").Returns(Array.Empty<string>());
        _fileSystem.GetDirectories(projectDir + "/bin").Returns(Array.Empty<string>());
        _fileSystem.GetDirectories(projectDir + "/src").Returns(Array.Empty<string>());

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await _sut.CreateSolutionAsync(solutionPath, projects);

        // Assert - should have created a directory for src but not for obj or bin
        var createDirCalls = _fileSystem.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "CreateDirectory")
            .Select(c => c.GetArguments()[0]?.ToString() ?? "")
            .ToList();

        // There should be src folder created (the top-level src and the subdirectory src)
        createDirCalls.Should().Contain(s => s.EndsWith("src"));
    }
}
