using ALaCarte.Core.Commands;

namespace ALaCarte.BoundaryInterface.Tests;

public class SkillContentExceptionHandlingTests
{
    private readonly string _content = InstallSkillCommandHandler.SkillContent;

    [Fact]
    public void SkillContent_DocumentsGitOperationException()
    {
        Assert.Contains("GitOperationException", _content);
    }

    [Fact]
    public void SkillContent_DocumentsDotNetOperationException()
    {
        Assert.Contains("DotNetOperationException", _content);
    }

    [Fact]
    public void SkillContent_DocumentsAngularOperationException()
    {
        Assert.Contains("AngularOperationException", _content);
    }

    [Fact]
    public void SkillContent_DocumentsCommandProperty()
    {
        Assert.Contains("Command", _content);
    }

    [Fact]
    public void SkillContent_DocumentsExitCodeProperty()
    {
        Assert.Contains("ExitCode", _content);
    }

    [Fact]
    public void SkillContent_DocumentsStandardErrorProperty()
    {
        Assert.Contains("StandardError", _content);
    }

    [Fact]
    public void SkillContent_DocumentsCommonPropertiesAcrossAllExceptions()
    {
        // Verifies all three exception types share the same properties
        Assert.Contains("Command", _content);
        Assert.Contains("ExitCode", _content);
        Assert.Contains("StandardError", _content);
        Assert.Contains("All three exception types share", _content);
    }

    [Fact]
    public void SkillContent_IncludesCatchExceptionCodeExample()
    {
        // Must show a catch block for handling exceptions
        Assert.Contains("catch (GitOperationException", _content);
        Assert.Contains("catch (DotNetOperationException", _content);
        Assert.Contains("catch (AngularOperationException", _content);
    }

    [Fact]
    public void SkillContent_IncludesExceptionsUsingDirective()
    {
        Assert.Contains("using ALaCarte.Core.Exceptions;", _content);
    }
}
