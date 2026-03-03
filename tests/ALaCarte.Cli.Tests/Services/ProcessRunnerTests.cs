using ALaCarte.Cli.Abstractions;
using ALaCarte.Cli.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ALaCarte.Cli.Tests.Services;

public class ProcessRunnerTests
{
    private readonly ProcessRunner _sut;

    public ProcessRunnerTests()
    {
        _sut = new ProcessRunner(NullLogger<ProcessRunner>.Instance);
    }

    [Fact]
    public async Task RunAsync_ReturnsSuccessResult_WhenCommandSucceeds()
    {
        // Arrange
        var request = new ProcessRunRequest(
            FileName: "dotnet",
            Arguments: "--version",
            WorkingDirectory: Directory.GetCurrentDirectory());

        // Act
        var result = await _sut.RunAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RunAsync_ReturnsFailureResult_WhenCommandFails()
    {
        // Arrange
        var request = new ProcessRunRequest(
            FileName: "dotnet",
            Arguments: "nonexistent-command-12345",
            WorkingDirectory: Directory.GetCurrentDirectory());

        // Act
        var result = await _sut.RunAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task RunAsync_UseShellExecute_RunsCommand()
    {
        // Arrange - using echo which works on both platforms via shell
        var isWindows = OperatingSystem.IsWindows();
        var request = new ProcessRunRequest(
            FileName: isWindows ? "echo" : "echo",
            Arguments: "test",
            WorkingDirectory: Directory.GetCurrentDirectory(),
            UseShellExecute: true);

        // Act
        var result = await _sut.RunAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StandardOutput.Should().Contain("test");
    }

    [Fact]
    public async Task RunAsync_ThrowsOperationCanceled_WhenCanceled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new ProcessRunRequest(
            FileName: "dotnet",
            Arguments: "--version",
            WorkingDirectory: Directory.GetCurrentDirectory(),
            CancellationToken: cts.Token);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.RunAsync(request));
    }
}
