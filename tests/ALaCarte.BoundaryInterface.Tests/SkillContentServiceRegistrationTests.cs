using ALaCarte.Core.Commands;

namespace ALaCarte.BoundaryInterface.Tests;

public class SkillContentServiceRegistrationTests
{
    [Fact]
    public void SkillContent_DocumentsAddAlacarteCoreServicesMethod()
    {
        var content = InstallSkillCommandHandler.SkillContent;
        Assert.Contains("AddAlacarteCoreServices()", content);
    }

    [Fact]
    public void SkillContent_IncludesUsingDirectiveForALaCarteCore()
    {
        var content = InstallSkillCommandHandler.SkillContent;
        Assert.Contains("using ALaCarte.Core;", content);
    }

    [Fact]
    public void SkillContent_IncludesUsingDirectiveForDependencyInjection()
    {
        var content = InstallSkillCommandHandler.SkillContent;
        Assert.Contains("using Microsoft.Extensions.DependencyInjection;", content);
    }

    [Fact]
    public void SkillContent_IncludesServiceRegistrationCodeExample()
    {
        var content = InstallSkillCommandHandler.SkillContent;
        // Should have a code block with the service registration call
        Assert.Contains("services.AddAlacarteCoreServices();", content);
    }

    [Fact]
    public void SkillContent_ListsNuGetPackageReference()
    {
        var content = InstallSkillCommandHandler.SkillContent;
        Assert.Contains("QuinntyneBrown.ALaCarte.Core", content);
    }

    [Fact]
    public void SkillContent_NuGetPackageReferenceIsInXmlFormat()
    {
        var content = InstallSkillCommandHandler.SkillContent;
        Assert.Contains("<PackageReference Include=\"QuinntyneBrown.ALaCarte.Core\"", content);
    }
}
