using ALaCarte.Core.Commands;

namespace ALaCarte.BoundaryInterface.Tests;

public class SkillContentOptionsConfigurationTests
{
    private readonly string _content = InstallSkillCommandHandler.SkillContent;

    [Fact]
    public void SkillContent_DocumentsAlacarteOptions()
    {
        Assert.Contains("AlacarteOptions", _content);
    }

    [Fact]
    public void SkillContent_DocumentsAlacarteOptions_DefaultBranch()
    {
        Assert.Contains("DefaultBranch", _content);
    }

    [Fact]
    public void SkillContent_DocumentsAlacarteOptions_SubmodulesFolderName()
    {
        Assert.Contains("SubmodulesFolderName", _content);
    }

    [Fact]
    public void SkillContent_DocumentsAlacarteOptions_SourceFolderName()
    {
        Assert.Contains("SourceFolderName", _content);
    }

    [Fact]
    public void SkillContent_DocumentsDotNetOptions()
    {
        Assert.Contains("DotNetOptions", _content);
    }

    [Fact]
    public void SkillContent_DocumentsDotNetOptions_ExecutablePath()
    {
        // DotNet section has ExecutablePath
        Assert.Contains("\"ExecutablePath\": \"dotnet\"", _content);
    }

    [Fact]
    public void SkillContent_DocumentsDotNetOptions_SolutionName()
    {
        Assert.Contains("SolutionName", _content);
    }

    [Fact]
    public void SkillContent_DocumentsDotNetOptions_ExcludedDirectories()
    {
        // DotNet ExcludedDirectories
        Assert.Contains("ExcludedDirectories", _content);
    }

    [Fact]
    public void SkillContent_DocumentsAngularOptions()
    {
        Assert.Contains("AngularOptions", _content);
    }

    [Fact]
    public void SkillContent_DocumentsAngularOptions_WorkspaceFolderName()
    {
        Assert.Contains("WorkspaceFolderName", _content);
    }

    [Fact]
    public void SkillContent_DocumentsAngularOptions_AutoInstallCli()
    {
        Assert.Contains("AutoInstallCli", _content);
    }

    [Fact]
    public void SkillContent_DocumentsAngularOptions_ExcludedDirectories()
    {
        // Angular section contains node_modules and dist
        Assert.Contains("node_modules", _content);
    }

    [Fact]
    public void SkillContent_DocumentsGitOptions()
    {
        Assert.Contains("GitOptions", _content);
    }

    [Fact]
    public void SkillContent_DocumentsGitOptions_ExecutablePath()
    {
        Assert.Contains("\"ExecutablePath\": \"git\"", _content);
    }

    [Fact]
    public void SkillContent_DocumentsGitOptions_TimeoutSeconds()
    {
        Assert.Contains("TimeoutSeconds", _content);
    }

    [Fact]
    public void SkillContent_IncludesOptionsBindingCodeExample()
    {
        // Must show the IOptions<T> / Configure<T> pattern
        Assert.Contains("Configure<AlacarteOptions>", _content);
        Assert.Contains("Configure<GitOptions>", _content);
        Assert.Contains("Configure<DotNetOptions>", _content);
        Assert.Contains("Configure<AngularOptions>", _content);
    }

    [Fact]
    public void SkillContent_IncludesOptionsUsingDirective()
    {
        Assert.Contains("using ALaCarte.Core.Options;", _content);
    }
}
