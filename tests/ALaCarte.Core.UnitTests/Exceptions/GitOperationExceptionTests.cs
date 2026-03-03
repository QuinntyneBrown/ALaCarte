using ALaCarte.Core.Exceptions;
using FluentAssertions;

namespace ALaCarte.Core.UnitTests.Exceptions;

public class GitOperationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var ex = new GitOperationException("test error");

        ex.Message.Should().Be("test error");
        ex.Command.Should().BeNull();
        ex.ExitCode.Should().BeNull();
        ex.StandardError.Should().BeNull();
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new GitOperationException("test error", inner);

        ex.Message.Should().Be("test error");
        ex.InnerException.Should().BeSameAs(inner);
        ex.Command.Should().BeNull();
        ex.ExitCode.Should().BeNull();
        ex.StandardError.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCommandDetails_SetsAllProperties()
    {
        var ex = new GitOperationException("failed", "git clone", 128, "fatal: error");

        ex.Message.Should().Be("failed");
        ex.Command.Should().Be("git clone");
        ex.ExitCode.Should().Be(128);
        ex.StandardError.Should().Be("fatal: error");
    }

    [Fact]
    public void IsException_DerivesFromException()
    {
        var ex = new GitOperationException("test");
        ex.Should().BeAssignableTo<Exception>();
    }
}
