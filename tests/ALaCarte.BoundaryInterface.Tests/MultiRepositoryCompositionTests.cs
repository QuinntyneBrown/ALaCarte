using ALaCarte.BoundaryInterface.Tests.Helpers;
using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using ALaCarte.Core.Exceptions;
using ALaCarte.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.BoundaryInterface.Tests;

/// <summary>
/// Boundary interface tests for L2-1: Multi-Repository Composition [L1-1]
/// </summary>
public class MultiRepositoryCompositionTests
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly GitService _gitService;

    public MultiRepositoryCompositionTests()
    {
        _processRunner = Substitute.For<IProcessRunner>();
        _fileSystem = Substitute.For<IFileSystem>();

        _fileSystem.Combine(Arg.Any<string[]>())
            .Returns(ci => string.Join(Path.DirectorySeparatorChar.ToString(), ci.ArgAt<string[]>(0)));
        _fileSystem.GetFileName(Arg.Any<string>())
            .Returns(ci => Path.GetFileName(ci.ArgAt<string>(0)));

        _gitService = new GitService(
            _processRunner,
            _fileSystem,
            NullLogger<GitService>.Instance,
            TestOptionsFactory.CreateGitOptions(),
            TestOptionsFactory.CreateAlacarteOptions());
    }

    #region L2-1.1 Git Repository Initialization

    // L2-1.1 AC: A `.git` directory is created in the workspace root via `git init`.
    [Fact]
    public async Task L2_1_1_InitializeRepository_CallsGitInit_InWorkspaceRoot()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _gitService.InitializeRepositoryAsync("/workspace");

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "git" &&
                r.Arguments == "init" &&
                r.WorkingDirectory == "/workspace"));
    }

    // L2-1.1 AC: The workspace folder must not already exist; the system shall fail with a validation error if it does.
    [Fact]
    public async Task L2_1_1_InitCommandHandler_ReturnsError_WhenFolderAlreadyExists()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.GetFullPath(Arg.Any<string>()).Returns("/existing-folder");
        fileSystem.DirectoryExists("/existing-folder").Returns(true);

        var handler = new InitCommandHandler(
            Substitute.For<IGitService>(),
            Substitute.For<IProjectDiscoveryService>(),
            Substitute.For<IDotNetService>(),
            Substitute.For<IAngularService>(),
            fileSystem,
            NullLogger<InitCommandHandler>.Instance);

        var result = await handler.ExecuteAsync(
            new[] { "https://github.com/org/repo.git" },
            "main",
            "existing-folder");

        result.Should().Be(1);
    }

    #endregion

    #region L2-1.2 Submodule-Based Repository Integration

    // L2-1.2 AC: Each repository URL provided via `--repos` is added as a submodule under `{SubmodulesFolderName}/` (default: `submodules/`).
    [Fact]
    public async Task L2_1_2_AddSubmodules_AddsEachRepo_UnderSubmodulesFolder()
    {
        var repos = new[] { "https://github.com/org/repo1.git", "https://github.com/org/repo2.git" };
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _gitService.AddSubmodulesAsync("/workspace", repos, "main");

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("submodule add") &&
                r.Arguments.Contains("submodules") &&
                r.Arguments.Contains("repo1")));
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("submodule add") &&
                r.Arguments.Contains("submodules") &&
                r.Arguments.Contains("repo2")));
    }

    // L2-1.2 AC: The submodule is checked out on the branch specified by `--branch` (default: `main`).
    [Fact]
    public async Task L2_1_2_AddSubmodules_UsesSpecifiedBranch()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _gitService.AddSubmodulesAsync("/workspace", new[] { "https://github.com/org/repo.git" }, "develop");

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.Arguments.Contains("-b develop")));
    }

    // L2-1.2 AC: HTTPS URLs (e.g., `https://github.com/org/repo.git`) are accepted.
    [Fact]
    public async Task L2_1_2_AddSubmodules_AcceptsHttpsUrls()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _gitService.AddSubmodulesAsync("/workspace",
            new[] { "https://github.com/org/repo.git" }, "main");

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("https://github.com/org/repo.git")));
    }

    // L2-1.2 AC: SSH URLs (e.g., `git@github.com:org/repo.git`) are accepted, including custom hosts and nested paths.
    [Fact]
    public async Task L2_1_2_AddSubmodules_AcceptsSshUrls()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _gitService.AddSubmodulesAsync("/workspace",
            new[] { "git@github.com:org/repo.git" }, "main");

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("git@github.com:org/repo.git")));
    }

    // L2-1.2 AC: If a submodule addition fails, the system logs a warning and continues processing remaining repositories.
    [Fact]
    public async Task L2_1_2_AddSubmodules_ContinuesOnFailure()
    {
        var repos = new[] { "https://github.com/org/fail.git", "https://github.com/org/succeed.git" };
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(
                new ProcessResult(1, "", "error"),
                new ProcessResult(0, "", ""));

        await _gitService.AddSubmodulesAsync("/workspace", repos, "main");

        await _processRunner.Received(2).RunAsync(Arg.Any<ProcessRunRequest>());
    }

    #endregion

    #region L2-1.3 Repository Name Extraction

    // L2-1.3 AC: For HTTPS URLs, the last segment of the URI path is used (e.g., `https://github.com/org/my-project.git` yields `my-project`).
    [Theory]
    [InlineData("https://github.com/org/my-project.git", "my-project")]
    [InlineData("https://github.com/user/repo", "repo")]
    [InlineData("https://gitlab.com/group/subgroup/repo.git", "repo")]
    public void L2_1_3_GetRepositoryName_ExtractsLastSegment_FromHttpsUrls(string url, string expected)
    {
        var result = _gitService.GetRepositoryName(url);
        result.Should().Be(expected);
    }

    // L2-1.3 AC: For SSH URLs, the portion after `:` is parsed and the last path segment is used (e.g., `git@github.com:group/subgroup/repo.git` yields `repo`).
    [Theory]
    [InlineData("git@github.com:user/repo.git", "repo")]
    [InlineData("git@gitlab.com:group/subgroup/repo.git", "repo")]
    [InlineData("git@git.company.com:owner/repo.git", "repo")]
    public void L2_1_3_GetRepositoryName_ParsesSshUrls(string url, string expected)
    {
        var result = _gitService.GetRepositoryName(url);
        result.Should().Be(expected);
    }

    // L2-1.3 AC: The `.git` suffix is stripped from all extracted names.
    [Theory]
    [InlineData("https://github.com/org/repo.git", "repo")]
    [InlineData("git@github.com:user/project.git", "project")]
    public void L2_1_3_GetRepositoryName_StripsGitSuffix(string url, string expected)
    {
        var result = _gitService.GetRepositoryName(url);
        result.Should().Be(expected);
        result.Should().NotEndWith(".git");
    }

    #endregion
}
