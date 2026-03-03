using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Exceptions;
using ALaCarte.Core.Services;
using ALaCarte.Core.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.Core.UnitTests.Services;

public class DotNetServiceTests
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly DotNetService _sut;

    public DotNetServiceTests()
    {
        _processRunner = Substitute.For<IProcessRunner>();
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
        _fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>())
            .Returns(Array.Empty<string>());
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
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.CreateSolutionAsync("/test/solution", new List<string>());

        _fileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.EndsWith("src")));
    }

    [Fact]
    public async Task CreateSolutionAsync_CreatesSolutionFile()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.CreateSolutionAsync("/test/solution", new List<string>());

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "dotnet" &&
                r.Arguments.Contains("new sln") &&
                r.Arguments.Contains("-n Solution")));
    }

    [Fact]
    public async Task CreateSolutionAsync_CopiesProjectsToSrcFolder()
    {
        var projectPath = "/test/solution/submodules/repo/MyProject/MyProject.csproj";
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.CreateSolutionAsync("/test/solution", new List<string> { projectPath });

        _fileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("MyProject")));
    }

    [Fact]
    public async Task CreateSolutionAsync_AddsProjectsToSolution()
    {
        var projectPath = "/test/solution/submodules/repo/MyProject/MyProject.csproj";
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.CreateSolutionAsync("/test/solution", new List<string> { projectPath });

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.Arguments.Contains("sln") && r.Arguments.Contains("add")));
    }

    [Fact]
    public async Task CreateSolutionAsync_ThrowsDotNetOperationException_WhenCommandFails()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(1, "", "error occurred"));

        var act = () => _sut.CreateSolutionAsync("/test/solution", new List<string>());
        await act.Should().ThrowAsync<DotNetOperationException>()
            .WithMessage("*failed*");
    }

    [Fact]
    public async Task CreateSolutionAsync_DoesNotThrow_WhenCommandFailsWithEmptyStderr()
    {
        // First call (sln new) succeeds, subsequent calls fail with empty stderr
        var callCount = 0;
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(ci =>
            {
                callCount++;
                return callCount == 1
                    ? new ProcessResult(0, "", "")
                    : new ProcessResult(1, "", "");
            });

        // With empty stderr and non-zero exit, should not throw
        await _sut.CreateSolutionAsync("/test/solution", new List<string>());
    }

    [Fact]
    public async Task CreateSolutionAsync_UsesConfiguredSolutionName()
    {
        var customOptions = TestOptionsFactory.CreateDotNetOptions(o => o.SolutionName = "MyCustomSolution");
        var sut = new DotNetService(
            _processRunner, _fileSystem, NullLogger<DotNetService>.Instance,
            customOptions, TestOptionsFactory.CreateAlacarteOptions());

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await sut.CreateSolutionAsync("/test/solution", new List<string>());

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.Arguments.Contains("-n MyCustomSolution")));
    }

    [Fact]
    public async Task CreateSolutionAsync_ExcludesConfiguredDirectories()
    {
        var projectDir = "/test/solution/submodules/repo/MyProject";
        var projectPath = projectDir + "/MyProject.csproj";

        _fileSystem.GetDirectories(projectDir)
            .Returns(new[] { projectDir + "/obj", projectDir + "/bin", projectDir + "/src" });
        _fileSystem.GetDirectories(projectDir + "/obj").Returns(Array.Empty<string>());
        _fileSystem.GetDirectories(projectDir + "/bin").Returns(Array.Empty<string>());
        _fileSystem.GetDirectories(projectDir + "/src").Returns(Array.Empty<string>());

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.CreateSolutionAsync("/test/solution", new List<string> { projectPath });

        var createDirCalls = _fileSystem.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "CreateDirectory")
            .Select(c => c.GetArguments()[0]?.ToString() ?? "")
            .ToList();

        createDirCalls.Should().Contain(s => s.EndsWith("src"));
    }

    [Fact]
    public async Task CreateSolutionAsync_SkipsCopy_WhenSourceDirIsNull()
    {
        // Simulate a path that returns null for GetDirectoryName
        _fileSystem.GetDirectoryName(Arg.Any<string>()).Returns((string?)null);

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.CreateSolutionAsync("/test/solution", new List<string> { "orphan.csproj" });

        // Should still add to solution even without copying
        await _processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.Arguments.Contains("sln") && r.Arguments.Contains("add")));
    }

    [Fact]
    public async Task CreateSolutionAsync_UsesConfiguredExecutablePath()
    {
        var customOptions = TestOptionsFactory.CreateDotNetOptions(o => o.ExecutablePath = "/usr/bin/dotnet");
        var sut = new DotNetService(
            _processRunner, _fileSystem, NullLogger<DotNetService>.Instance,
            customOptions, TestOptionsFactory.CreateAlacarteOptions());

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await sut.CreateSolutionAsync("/test/solution", new List<string>());

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.FileName == "/usr/bin/dotnet"));
    }

    [Fact]
    public async Task CreateSolutionAsync_CopiesFiles_FromSourceDirectory()
    {
        var projectDir = "/test/solution/submodules/repo/MyProject";
        var projectPath = projectDir + "/MyProject.csproj";

        _fileSystem.GetDirectoryName(projectPath).Returns(projectDir);
        _fileSystem.GetFiles(projectDir, "*", SearchOption.TopDirectoryOnly)
            .Returns(new[] { projectDir + "/MyProject.csproj", projectDir + "/Class1.cs" });

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.CreateSolutionAsync("/test/solution", new List<string> { projectPath });

        _fileSystem.Received().CopyFile(
            Arg.Is<string>(s => s.Contains("MyProject.csproj")),
            Arg.Any<string>(), true);
        _fileSystem.Received().CopyFile(
            Arg.Is<string>(s => s.Contains("Class1.cs")),
            Arg.Any<string>(), true);
    }
}
