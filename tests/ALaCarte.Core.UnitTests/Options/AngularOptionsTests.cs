using ALaCarte.Core.Options;
using FluentAssertions;

namespace ALaCarte.Core.UnitTests.Options;

public class AngularOptionsTests
{
    [Fact]
    public void SectionName_IsAngular()
    {
        AngularOptions.SectionName.Should().Be("Angular");
    }

    [Fact]
    public void WorkspaceFolderName_DefaultsToUi()
    {
        var options = new AngularOptions();
        options.WorkspaceFolderName.Should().Be("Ui");
    }

    [Fact]
    public void AutoInstallCli_DefaultsToTrue()
    {
        var options = new AngularOptions();
        options.AutoInstallCli.Should().BeTrue();
    }

    [Fact]
    public void ExcludedDirectories_DefaultsToNodeModulesAndDist()
    {
        var options = new AngularOptions();
        options.ExcludedDirectories.Should().BeEquivalentTo("node_modules", "dist");
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new AngularOptions
        {
            WorkspaceFolderName = "Frontend",
            AutoInstallCli = false,
            ExcludedDirectories = ["build"]
        };

        options.WorkspaceFolderName.Should().Be("Frontend");
        options.AutoInstallCli.Should().BeFalse();
        options.ExcludedDirectories.Should().BeEquivalentTo("build");
    }
}
