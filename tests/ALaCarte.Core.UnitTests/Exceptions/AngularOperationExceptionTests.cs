using ALaCarte.Core.Exceptions;
using FluentAssertions;

namespace ALaCarte.Core.UnitTests.Exceptions;

public class AngularOperationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var ex = new AngularOperationException("test error");

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
        var ex = new AngularOperationException("test error", inner);

        ex.Message.Should().Be("test error");
        ex.InnerException.Should().BeSameAs(inner);
        ex.Command.Should().BeNull();
        ex.ExitCode.Should().BeNull();
        ex.StandardError.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCommandDetails_SetsAllProperties()
    {
        var ex = new AngularOperationException("failed", "ng build", 1, "compilation error");

        ex.Message.Should().Be("failed");
        ex.Command.Should().Be("ng build");
        ex.ExitCode.Should().Be(1);
        ex.StandardError.Should().Be("compilation error");
    }

    [Fact]
    public void IsException_DerivesFromException()
    {
        var ex = new AngularOperationException("test");
        ex.Should().BeAssignableTo<Exception>();
    }
}
