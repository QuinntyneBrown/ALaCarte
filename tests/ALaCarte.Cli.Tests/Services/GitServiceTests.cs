using ALaCarte.Cli.Abstractions;
using ALaCarte.Cli.Exceptions;
using ALaCarte.Cli.Services;
using ALaCarte.Cli.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.Cli.Tests.Services;

public class GitServiceTests
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly GitService _sut;

    public GitServiceTests()
    {
        _processRunner = Substitute.For<IProcessRunner>();
        _fileSystem = Substitute.For<IFileSystem>();

        _fileSystem.Combine(Arg.Any<string[]>())
            .Returns(ci => string.Join(Path.DirectorySeparatorChar.ToString(), ci.ArgAt<string[]>(0)));
        _fileSystem.GetFileName(Arg.Any<string>())
            .Returns(ci => Path.GetFileName(ci.ArgAt<string>(0)));

        _sut = new GitService(
            _processRunner,
            _fileSystem,
            NullLogger<GitService>.Instance,
            TestOptionsFactory.CreateGitOptions(),
            TestOptionsFactory.CreateAlacarteOptions());
    }

    [Theory]
    [InlineData("https://github.com/user/repo.git", "repo")]
    [InlineData("https://github.com/user/repo", "repo")]
    [InlineData("https://github.com/organization/my-project.git", "my-project")]
    [InlineData("https://gitlab.com/group/subgroup/repo.git", "repo")]
    [InlineData("git@github.com:user/repo.git", "repo")]
    [InlineData("git@gitlab.com:user/repo.git", "repo")]
    [InlineData("git@git.company.com:owner/repo.git", "repo")]
    public void GetRepositoryName_ExtractsCorrectName_FromVariousGitUrls(string repoUrl, string expectedName)
    {
        // Act
        var result = _sut.GetRepositoryName(repoUrl);

        // Assert
        result.Should().Be(expectedName);
    }

    [Fact]
    public async Task InitializeRepositoryAsync_CallsGitInit()
    {
        // Arrange
        var path = "/test/path";
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await _sut.InitializeRepositoryAsync(path);

        // Assert
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "git" &&
                r.Arguments == "init" &&
                r.WorkingDirectory == path));
    }

    [Fact]
    public async Task InitializeRepositoryAsync_ThrowsGitOperationException_WhenGitFails()
    {
        // Arrange
        var path = "/test/path";
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(1, "", "fatal: error"));

        // Act & Assert
        var act = () => _sut.InitializeRepositoryAsync(path);
        await act.Should().ThrowAsync<GitOperationException>()
            .WithMessage("*failed*");
    }

    [Fact]
    public async Task AddSubmodulesAsync_AddsEachSubmodule()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var repos = new[] { "https://github.com/user/repo1.git", "https://github.com/user/repo2.git" };
        var branch = "main";

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await _sut.AddSubmodulesAsync(solutionPath, repos, branch);

        // Assert
        await _processRunner.Received(2).RunAsync(Arg.Any<ProcessRunRequest>());
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("repo1") &&
                r.Arguments.Contains("-b main")));
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("repo2") &&
                r.Arguments.Contains("-b main")));
    }

    [Fact]
    public async Task AddSubmodulesAsync_ContinuesOnFailure_AndLogsWarning()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var repos = new[] { "https://github.com/user/repo1.git", "https://github.com/user/repo2.git" };
        var branch = "main";

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(
                new ProcessResult(1, "", "error"),
                new ProcessResult(0, "", ""));

        // Act - should not throw
        await _sut.AddSubmodulesAsync(solutionPath, repos, branch);

        // Assert - both repos should have been attempted
        await _processRunner.Received(2).RunAsync(Arg.Any<ProcessRunRequest>());
    }

    [Fact]
    public async Task AddSubmodulesAsync_UsesConfiguredSubmodulesFolder()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var repos = new[] { "https://github.com/user/repo.git" };
        var branch = "main";

        var customOptions = TestOptionsFactory.CreateAlacarteOptions(o => o.SubmodulesFolderName = "custom-submodules");
        var sut = new GitService(
            _processRunner,
            _fileSystem,
            NullLogger<GitService>.Instance,
            TestOptionsFactory.CreateGitOptions(),
            customOptions);

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await sut.AddSubmodulesAsync(solutionPath, repos, branch);

        // Assert
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("custom-submodules")));
    }
}
