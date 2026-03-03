using ALaCarte.BoundaryInterface.Tests.Helpers;
using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.BoundaryInterface.Tests;

/// <summary>
/// Boundary interface tests for L2-5: Source Repository Preservation [L1-5]
/// </summary>
public class SourceRepositoryPreservationTests
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly GitService _gitService;

    public SourceRepositoryPreservationTests()
    {
        _processRunner = Substitute.For<IProcessRunner>();
        _fileSystem = Substitute.For<IFileSystem>();

        _fileSystem.Combine(Arg.Any<string[]>())
            .Returns(ci => string.Join(Path.DirectorySeparatorChar.ToString(), ci.ArgAt<string[]>(0)));
        _fileSystem.GetFileName(Arg.Any<string>())
            .Returns(ci => Path.GetFileName(ci.ArgAt<string>(0)));
        _fileSystem.GetFileNameWithoutExtension(Arg.Any<string>())
            .Returns(ci => Path.GetFileNameWithoutExtension(ci.ArgAt<string>(0)));
        _fileSystem.GetDirectoryName(Arg.Any<string>())
            .Returns(ci => Path.GetDirectoryName(ci.ArgAt<string>(0)));
        _fileSystem.GetRelativePath(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => Path.GetRelativePath(ci.ArgAt<string>(0), ci.ArgAt<string>(1)));
        _fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>())
            .Returns(Array.Empty<string>());
        _fileSystem.GetDirectories(Arg.Any<string>())
            .Returns(Array.Empty<string>());

        _gitService = new GitService(
            _processRunner, _fileSystem, NullLogger<GitService>.Instance,
            TestOptionsFactory.CreateGitOptions(),
            TestOptionsFactory.CreateAlacarteOptions());
    }

    #region L2-5.1 Submodule Traceability

    // L2-5.1 AC: Each source repository remains a fully functional Git submodule under `{SubmodulesFolderName}/`, retaining its `.git` metadata and remote configuration.
    [Fact]
    public async Task L2_5_1_AddSubmodules_UsesGitSubmoduleAdd_PreservingGitMetadata()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _gitService.AddSubmodulesAsync("/workspace",
            new[] { "https://github.com/org/repo.git" }, "main");

        // git submodule add preserves .git metadata and remote configuration by design
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("submodule add") &&
                r.Arguments.Contains("submodules")));
    }

    // L2-5.1 AC: The workspace root `.gitmodules` file records the URL and branch of each source repository.
    [Fact]
    public async Task L2_5_1_AddSubmodules_PassesUrlAndBranch_ForGitmodules()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _gitService.AddSubmodulesAsync("/workspace",
            new[] { "https://github.com/org/repo.git" }, "develop");

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("https://github.com/org/repo.git") &&
                r.Arguments.Contains("-b develop")));
    }

    // L2-5.1 AC: The original source repositories are never modified during composition.
    [Fact]
    public async Task L2_5_1_Composition_NeverWritesToSubmodulesDirectory()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        var dotNetService = new DotNetService(
            _processRunner, _fileSystem, NullLogger<DotNetService>.Instance,
            TestOptionsFactory.CreateDotNetOptions(),
            TestOptionsFactory.CreateAlacarteOptions());

        var projectPath = "/workspace/submodules/repo/MyProject/MyProject.csproj";
        await dotNetService.CreateSolutionAsync("/workspace", new List<string> { projectPath });

        // Verify: CopyFile destination should be under src/, never writing back to submodules/
        _fileSystem.DidNotReceive().CopyFile(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("submodules")),
            Arg.Any<bool>());
    }

    #endregion

    #region L2-5.2 Copied Project Attribution

    // L2-5.2 AC: The directory structure under `src/` preserves the original project name, allowing correlation with the corresponding submodule project.
    [Fact]
    public async Task L2_5_2_CopiedProject_PreservesOriginalProjectName_InSrcDirectory()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        var dotNetService = new DotNetService(
            _processRunner, _fileSystem, NullLogger<DotNetService>.Instance,
            TestOptionsFactory.CreateDotNetOptions(),
            TestOptionsFactory.CreateAlacarteOptions());

        var projectPath = "/workspace/submodules/repo/MySpecialProject/MySpecialProject.csproj";
        await dotNetService.CreateSolutionAsync("/workspace", new List<string> { projectPath });

        _fileSystem.Received().CreateDirectory(
            Arg.Is<string>(s => s.Contains("MySpecialProject")));
    }

    // L2-5.2 AC: The submodule directory retains the unmodified original source for reference and diffing.
    [Fact]
    public async Task L2_5_2_SubmoduleDirectory_RemainsUnmodified_AfterCopy()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        var dotNetService = new DotNetService(
            _processRunner, _fileSystem, NullLogger<DotNetService>.Instance,
            TestOptionsFactory.CreateDotNetOptions(),
            TestOptionsFactory.CreateAlacarteOptions());

        var projectPath = "/workspace/submodules/repo/MyProject/MyProject.csproj";
        await dotNetService.CreateSolutionAsync("/workspace", new List<string> { projectPath });

        // Source submodule files are only read (via CopyFile source), never written
        await _fileSystem.DidNotReceive().WriteAllTextAsync(
            Arg.Is<string>(s => s.Contains("submodules")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
