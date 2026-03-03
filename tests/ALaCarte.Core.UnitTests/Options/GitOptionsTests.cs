using ALaCarte.Core.Options;
using FluentAssertions;

namespace ALaCarte.Core.UnitTests.Options;

public class GitOptionsTests
{
    [Fact]
    public void SectionName_IsGit()
    {
        GitOptions.SectionName.Should().Be("Git");
    }

    [Fact]
    public void ExecutablePath_DefaultsToGit()
    {
        var options = new GitOptions();
        options.ExecutablePath.Should().Be("git");
    }

    [Fact]
    public void TimeoutSeconds_DefaultsTo300()
    {
        var options = new GitOptions();
        options.TimeoutSeconds.Should().Be(300);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new GitOptions
        {
            ExecutablePath = "/usr/bin/git",
            TimeoutSeconds = 600
        };

        options.ExecutablePath.Should().Be("/usr/bin/git");
        options.TimeoutSeconds.Should().Be(600);
    }
}
