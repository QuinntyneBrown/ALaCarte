using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Exceptions;
using ALaCarte.Core.Services;
using ALaCarte.Core.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ALaCarte.Core.UnitTests.Services;

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
        var result = _sut.GetRepositoryName(repoUrl);
        result.Should().Be(expectedName);
    }

    [Fact]
    public void GetRepositoryName_HandlesHttpUrlWithCredentials()
    {
        // URL with @ but also http:// prefix - should use Uri path
        var result = _sut.GetRepositoryName("https://token@github.com/org/project.git");
        result.Should().Be("project");
    }

    [Fact]
    public async Task InitializeRepositoryAsync_CallsGitInit()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.InitializeRepositoryAsync("/test/path");

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "git" &&
                r.Arguments == "init" &&
                r.WorkingDirectory == "/test/path"));
    }

    [Fact]
    public async Task InitializeRepositoryAsync_ThrowsGitOperationException_WhenGitFails()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(1, "", "fatal: error"));

        var act = () => _sut.InitializeRepositoryAsync("/test/path");
        await act.Should().ThrowAsync<GitOperationException>()
            .WithMessage("*failed*");
    }

    [Fact]
    public async Task InitializeRepositoryAsync_UsesConfiguredExecutablePath()
    {
        var customOptions = TestOptionsFactory.CreateGitOptions(o => o.ExecutablePath = "/usr/bin/git");
        var sut = new GitService(
            _processRunner, _fileSystem, NullLogger<GitService>.Instance,
            customOptions, TestOptionsFactory.CreateAlacarteOptions());

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await sut.InitializeRepositoryAsync("/test");

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.FileName == "/usr/bin/git"));
    }

    [Fact]
    public async Task AddSubmodulesAsync_AddsEachSubmodule()
    {
        var repos = new[] { "https://github.com/user/repo1.git", "https://github.com/user/repo2.git" };
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.AddSubmodulesAsync("/test/solution", repos, "main");

        await _processRunner.Received(2).RunAsync(Arg.Any<ProcessRunRequest>());
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.Arguments.Contains("repo1") && r.Arguments.Contains("-b main")));
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.Arguments.Contains("repo2") && r.Arguments.Contains("-b main")));
    }

    [Fact]
    public async Task AddSubmodulesAsync_ContinuesOnFailure_AndLogsWarning()
    {
        var repos = new[] { "https://github.com/user/repo1.git", "https://github.com/user/repo2.git" };
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(
                new ProcessResult(1, "", "error"),
                new ProcessResult(0, "", ""));

        await _sut.AddSubmodulesAsync("/test/solution", repos, "main");

        await _processRunner.Received(2).RunAsync(Arg.Any<ProcessRunRequest>());
    }

    [Fact]
    public async Task AddSubmodulesAsync_ContinuesOnException()
    {
        var repos = new[] { "https://github.com/user/repo1.git", "https://github.com/user/repo2.git" };
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(
                x => throw new InvalidOperationException("network error"),
                x => new ProcessResult(0, "", ""));

        await _sut.AddSubmodulesAsync("/test/solution", repos, "main");

        await _processRunner.Received(2).RunAsync(Arg.Any<ProcessRunRequest>());
    }

    [Fact]
    public async Task AddSubmodulesAsync_RethrowsOperationCanceledException()
    {
        var repos = new[] { "https://github.com/user/repo1.git" };
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .ThrowsAsync(new OperationCanceledException());

        var act = () => _sut.AddSubmodulesAsync("/test/solution", repos, "main");
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AddSubmodulesAsync_UsesConfiguredSubmodulesFolder()
    {
        var repos = new[] { "https://github.com/user/repo.git" };
        var customOptions = TestOptionsFactory.CreateAlacarteOptions(o => o.SubmodulesFolderName = "custom-submodules");
        var sut = new GitService(
            _processRunner, _fileSystem, NullLogger<GitService>.Instance,
            TestOptionsFactory.CreateGitOptions(), customOptions);

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await sut.AddSubmodulesAsync("/test/solution", repos, "main");

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.Arguments.Contains("custom-submodules")));
    }
}
