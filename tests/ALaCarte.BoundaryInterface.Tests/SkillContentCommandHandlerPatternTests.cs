using ALaCarte.Core.Commands;

namespace ALaCarte.BoundaryInterface.Tests;

public class SkillContentCommandHandlerPatternTests
{
    private readonly string _content = InstallSkillCommandHandler.SkillContent;

    [Fact]
    public void SkillContent_DocumentsInitCommandHandler()
    {
        Assert.Contains("InitCommandHandler", _content);
    }

    [Fact]
    public void SkillContent_DocumentsInitCommandHandlerComposesServices()
    {
        // Should describe how it orchestrates core services
        Assert.Contains("orchestrates", _content);
    }

    [Fact]
    public void SkillContent_DocumentsWorkflowSequence_GitInit()
    {
        Assert.Contains("git init", _content);
    }

    [Fact]
    public void SkillContent_DocumentsWorkflowSequence_SubmoduleAdd()
    {
        Assert.Contains("submodule add", _content);
    }

    [Fact]
    public void SkillContent_DocumentsWorkflowSequence_ProjectDiscovery()
    {
        Assert.Contains("project discovery", _content);
    }

    [Fact]
    public void SkillContent_DocumentsWorkflowSequence_DotNetSolutionCreation()
    {
        Assert.Contains(".NET solution creation", _content);
    }

    [Fact]
    public void SkillContent_DocumentsWorkflowSequence_AngularWorkspaceCreation()
    {
        Assert.Contains("Angular workspace creation", _content);
    }

    [Fact]
    public void SkillContent_DocumentsReposParameter()
    {
        Assert.Contains("repos:", _content);
    }

    [Fact]
    public void SkillContent_DocumentsBranchParameter()
    {
        Assert.Contains("branch:", _content);
    }

    [Fact]
    public void SkillContent_DocumentsFolderParameter()
    {
        Assert.Contains("folder:", _content);
    }

    [Fact]
    public void SkillContent_DocumentsProjectFiltersParameter()
    {
        Assert.Contains("projectFilters:", _content);
    }

    [Fact]
    public void SkillContent_IncludesCommandHandlerCodeExample()
    {
        Assert.Contains("ExecuteAsync", _content);
        Assert.Contains("using ALaCarte.Core.Commands;", _content);
    }
}
