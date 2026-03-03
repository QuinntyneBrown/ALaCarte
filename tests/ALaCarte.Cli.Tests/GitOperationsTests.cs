namespace ALaCarte.Cli.Tests;

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
    [InlineData("https://user@github.com/org/repo.git", "repo")] // HTTPS with userinfo
    [InlineData("https://token@gitlab.com/group/project.git", "project")] // HTTPS with token
    [InlineData("git@github.com:user/repo.git", "repo")]
    [InlineData("git@gitlab.com:user/repo.git", "repo")]
    [InlineData("git@git.company.com:owner/repo.git", "repo")]
    public void GetRepositoryName_ExtractsCorrectName_FromVariousGitUrls(string repoUrl, string expectedName)
    {
        // This test verifies that GetRepositoryName works with:
        // - GitHub URLs (https and ssh)
        // - GitLab URLs (https and ssh, including nested groups)
        // - Self-hosted git URLs (https and ssh)
        // - URLs with and without .git suffix

        var result = GitOperations.GetRepositoryName(repoUrl);

        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData("https://github.com/user/repo/tree/feature-branch", "repo", "feature-branch")]
    [InlineData("https://github.com/user/repo/tree/main", "repo", "main")]
    [InlineData("https://github.com/user/repo", "repo", null)]
    public void GetRepositoryName_WithBranchUrl_ExtractsBaseRepoName(string repoUrl, string expectedName, string? expectedBranch)
    {
        // GitUrlParser.Parse strips the /tree/<branch> segment; verify the remaining URL
        // produces the correct repo name and that the branch is correctly extracted.
        var (cloneUrl, branch) = QuinntyneBrown.Git.Core.GitUrlParser.Parse(repoUrl);

        var result = GitOperations.GetRepositoryName(cloneUrl);

        Assert.Equal(expectedName, result);
        Assert.Equal(expectedBranch, branch);
    }

    [Fact]
    public async Task InitializeRepository_CreatesGitRepo()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        try
        {
            // Act
            await GitOperations.InitializeRepository(tempPath);

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
                // Suppress cleanup exceptions to avoid masking test failures
            }
        }
    }
}
