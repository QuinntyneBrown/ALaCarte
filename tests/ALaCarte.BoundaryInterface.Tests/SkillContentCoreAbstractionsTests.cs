using ALaCarte.Core.Commands;

namespace ALaCarte.BoundaryInterface.Tests;

public class SkillContentCoreAbstractionsTests
{
    private readonly string _content = InstallSkillCommandHandler.SkillContent;

    [Fact]
    public void SkillContent_DocumentsIGitService()
    {
        Assert.Contains("IGitService", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIGitService_InitializeRepositoryAsync()
    {
        Assert.Contains("InitializeRepositoryAsync", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIGitService_AddSubmodulesAsync()
    {
        Assert.Contains("AddSubmodulesAsync", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIGitService_GetRepositoryName()
    {
        Assert.Contains("GetRepositoryName", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIDotNetService()
    {
        Assert.Contains("IDotNetService", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIDotNetService_CreateSolutionAsync()
    {
        Assert.Contains("CreateSolutionAsync", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIAngularService()
    {
        Assert.Contains("IAngularService", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIAngularService_CreateWorkspaceAsync()
    {
        Assert.Contains("CreateWorkspaceAsync", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIProjectDiscoveryService()
    {
        Assert.Contains("IProjectDiscoveryService", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIProjectDiscoveryService_DiscoverDotNetProjectsAsync()
    {
        Assert.Contains("DiscoverDotNetProjectsAsync", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIProjectDiscoveryService_DiscoverAngularProjectsAsync()
    {
        Assert.Contains("DiscoverAngularProjectsAsync", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIFileSystem()
    {
        Assert.Contains("IFileSystem", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIFileSystem_KeyOperations()
    {
        Assert.Contains("CreateDirectory", _content);
        Assert.Contains("DirectoryExists", _content);
        Assert.Contains("FileExists", _content);
        Assert.Contains("WriteAllTextAsync", _content);
        Assert.Contains("ReadAllTextAsync", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIProcessRunner()
    {
        Assert.Contains("IProcessRunner", _content);
    }

    [Fact]
    public void SkillContent_DocumentsIProcessRunner_RunAsync()
    {
        Assert.Contains("RunAsync", _content);
    }

    [Fact]
    public void SkillContent_DocumentsProcessRunRequest()
    {
        Assert.Contains("ProcessRunRequest", _content);
    }

    [Fact]
    public void SkillContent_DocumentsProcessResultProperties()
    {
        // Documents the result type properties
        Assert.Contains("IsSuccess", _content);
        Assert.Contains("StandardOutput", _content);
        Assert.Contains("StandardError", _content);
    }

    [Fact]
    public void SkillContent_InterfacesIncludeMethodSignaturesWithParameterTypes()
    {
        // Verify that method signatures include parameter types
        Assert.Contains("solutionPath", _content);
        Assert.Contains("repoUrls", _content);
        Assert.Contains("projectFiles", _content);
    }

    [Fact]
    public void SkillContent_InterfacesIncludeReturnTypes()
    {
        // Verify return types are documented
        Assert.Contains("List<string>", _content);
        Assert.Contains("List<AngularWorkspace>", _content);
    }
}
