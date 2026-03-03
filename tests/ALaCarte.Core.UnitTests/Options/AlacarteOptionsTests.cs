using ALaCarte.Core.Options;
using FluentAssertions;

namespace ALaCarte.Core.UnitTests.Options;

public class AlacarteOptionsTests
{
    [Fact]
    public void SectionName_IsAlacarte()
    {
        AlacarteOptions.SectionName.Should().Be("Alacarte");
    }

    [Fact]
    public void DefaultBranch_DefaultsToMain()
    {
        var options = new AlacarteOptions();
        options.DefaultBranch.Should().Be("main");
    }

    [Fact]
    public void SubmodulesFolderName_DefaultsToSubmodules()
    {
        var options = new AlacarteOptions();
        options.SubmodulesFolderName.Should().Be("submodules");
    }

    [Fact]
    public void SourceFolderName_DefaultsToSrc()
    {
        var options = new AlacarteOptions();
        options.SourceFolderName.Should().Be("src");
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new AlacarteOptions
        {
            DefaultBranch = "develop",
            SubmodulesFolderName = "libs",
            SourceFolderName = "source"
        };

        options.DefaultBranch.Should().Be("develop");
        options.SubmodulesFolderName.Should().Be("libs");
        options.SourceFolderName.Should().Be("source");
    }
}
