using ALaCarte.Core.Options;
using FluentAssertions;

namespace ALaCarte.Core.UnitTests.Options;

public class DotNetOptionsTests
{
    [Fact]
    public void SectionName_IsDotNet()
    {
        DotNetOptions.SectionName.Should().Be("DotNet");
    }

    [Fact]
    public void ExecutablePath_DefaultsToDotnet()
    {
        var options = new DotNetOptions();
        options.ExecutablePath.Should().Be("dotnet");
    }

    [Fact]
    public void SolutionName_DefaultsToSolution()
    {
        var options = new DotNetOptions();
        options.SolutionName.Should().Be("Solution");
    }

    [Fact]
    public void ExcludedDirectories_DefaultsToObjAndBin()
    {
        var options = new DotNetOptions();
        options.ExcludedDirectories.Should().BeEquivalentTo("obj", "bin");
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new DotNetOptions
        {
            ExecutablePath = "/usr/bin/dotnet",
            SolutionName = "MySolution",
            ExcludedDirectories = ["obj", "bin", "packages"]
        };

        options.ExecutablePath.Should().Be("/usr/bin/dotnet");
        options.SolutionName.Should().Be("MySolution");
        options.ExcludedDirectories.Should().HaveCount(3);
    }
}
