// Legacy tests - now covered by Services/GitServiceTests.cs
// This file is kept for backward compatibility with existing test runs
// but the tests now use the new service-based architecture

using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;
using ALaCarte.Core.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.Core.Tests;

public class GitOperationsTests
{
    [Theory]
    [InlineData("https://github.com/user/repo.git", "repo")]
    [InlineData("https://github.com/user/repo", "repo")]
    [InlineData("https://github.com/organization/my-project.git", "my-project")]
    [InlineData("https://github.com/organization/my-project", "my-project")]
    [InlineData("https://gitlab.com/user/repo.git", "repo")]
    [InlineData("https://gitlab.com/user/repo", "repo")]
    [InlineData("https://gitlab.com/group/subgroup/repo.git", "repo")]
    [InlineData("https://gitlab.com/group/subgroup/repo", "repo")]
    [InlineData("https://git.company.com/owner/repo.git", "repo")]
    [InlineData("https://git.company.com/owner/repo", "repo")]
    [InlineData("https://git.company.com/team/owner/repo.git", "repo")]
    [InlineData("https://git.company.com/team/owner/repo", "repo")]
    [InlineData("https://user@github.com/org/repo.git", "repo")]
    [InlineData("https://token@gitlab.com/group/project.git", "project")]
    [InlineData("git@github.com:user/repo.git", "repo")]
    [InlineData("git@gitlab.com:user/repo.git", "repo")]
    [InlineData("git@git.company.com:owner/repo.git", "repo")]
    public void GetRepositoryName_ExtractsCorrectName_FromVariousGitUrls(string repoUrl, string expectedName)
    {
        // Arrange
        var processRunner = Substitute.For<IProcessRunner>();
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Combine(Arg.Any<string[]>())
            .Returns(ci => Path.Combine(ci.ArgAt<string[]>(0)));
        fileSystem.GetFileName(Arg.Any<string>())
            .Returns(ci => Path.GetFileName(ci.ArgAt<string>(0)));

        var gitService = new GitService(
            processRunner,
            fileSystem,
            NullLogger<GitService>.Instance,
            TestOptionsFactory.CreateGitOptions(),
            TestOptionsFactory.CreateAlacarteOptions());

        // Act
        var result = gitService.GetRepositoryName(repoUrl);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public async Task InitializeRepository_CreatesGitRepo()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        var processRunner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);
        var fileSystem = new FileSystemService();

        var gitService = new GitService(
            processRunner,
            fileSystem,
            NullLogger<GitService>.Instance,
            TestOptionsFactory.CreateGitOptions(),
            TestOptionsFactory.CreateAlacarteOptions());

        try
        {
            // Act
            await gitService.InitializeRepositoryAsync(tempPath);

            // Assert
            var gitDir = Path.Combine(tempPath, ".git");
            Assert.True(Directory.Exists(gitDir), "Git repository should be initialized");
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
            catch
            {
                // Suppress cleanup exceptions
            }
        }
    }
}
