using ALaCarte.Core.Exceptions;
using FluentAssertions;

namespace ALaCarte.Core.UnitTests.Exceptions;

public class DotNetOperationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var ex = new DotNetOperationException("test error");

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
        var ex = new DotNetOperationException("test error", inner);

        ex.Message.Should().Be("test error");
        ex.InnerException.Should().BeSameAs(inner);
        ex.Command.Should().BeNull();
        ex.ExitCode.Should().BeNull();
        ex.StandardError.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCommandDetails_SetsAllProperties()
    {
        var ex = new DotNetOperationException("failed", "dotnet build", 2, "build error");

        ex.Message.Should().Be("failed");
        ex.Command.Should().Be("dotnet build");
        ex.ExitCode.Should().Be(2);
        ex.StandardError.Should().Be("build error");
    }

    [Fact]
    public void IsException_DerivesFromException()
    {
        var ex = new DotNetOperationException("test");
        ex.Should().BeAssignableTo<Exception>();
    }
}
