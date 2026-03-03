using ALaCarte.Core.Abstractions;
using FluentAssertions;

namespace ALaCarte.Core.UnitTests.Abstractions;

public class ProcessResultTests
{
    [Fact]
    public void IsSuccess_ReturnsTrue_WhenExitCodeIsZero()
    {
        var result = new ProcessResult(0, "output", "");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IsSuccess_ReturnsFalse_WhenExitCodeIsNonZero()
    {
        var result = new ProcessResult(1, "", "error");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void IsSuccess_ReturnsFalse_WhenExitCodeIsNegative()
    {
        var result = new ProcessResult(-1, "", "");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var result = new ProcessResult(0, "stdout", "stderr");

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Be("stdout");
        result.StandardError.Should().Be("stderr");
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        var a = new ProcessResult(0, "out", "err");
        var b = new ProcessResult(0, "out", "err");

        a.Should().Be(b);
    }
}

public class ProcessRunRequestTests
{
    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var request = new ProcessRunRequest(
            FileName: "dotnet",
            Arguments: "build",
            WorkingDirectory: "/test");

        request.FileName.Should().Be("dotnet");
        request.Arguments.Should().Be("build");
        request.WorkingDirectory.Should().Be("/test");
    }

    [Fact]
    public void UseShellExecute_DefaultsToFalse()
    {
        var request = new ProcessRunRequest("cmd", "args", "/dir");
        request.UseShellExecute.Should().BeFalse();
    }

    [Fact]
    public void CancellationToken_DefaultsToNone()
    {
        var request = new ProcessRunRequest("cmd", "args", "/dir");
        request.CancellationToken.Should().Be(CancellationToken.None);
    }

    [Fact]
    public void UseShellExecute_CanBeSetToTrue()
    {
        var request = new ProcessRunRequest("cmd", "args", "/dir", UseShellExecute: true);
        request.UseShellExecute.Should().BeTrue();
    }

    [Fact]
    public void CancellationToken_CanBeSet()
    {
        using var cts = new CancellationTokenSource();
        var request = new ProcessRunRequest("cmd", "args", "/dir", CancellationToken: cts.Token);
        request.CancellationToken.Should().Be(cts.Token);
    }
}
