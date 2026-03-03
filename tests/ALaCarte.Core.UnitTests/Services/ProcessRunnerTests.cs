using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ALaCarte.Core.UnitTests.Services;

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
        var request = new ProcessRunRequest(
            FileName: "dotnet",
            Arguments: "--version",
            WorkingDirectory: Directory.GetCurrentDirectory());

        var result = await _sut.RunAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RunAsync_ReturnsFailureResult_WhenCommandFails()
    {
        var request = new ProcessRunRequest(
            FileName: "dotnet",
            Arguments: "nonexistent-command-12345",
            WorkingDirectory: Directory.GetCurrentDirectory());

        var result = await _sut.RunAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task RunAsync_UseShellExecute_RunsCommand()
    {
        var request = new ProcessRunRequest(
            FileName: "echo",
            Arguments: "test",
            WorkingDirectory: Directory.GetCurrentDirectory(),
            UseShellExecute: true);

        var result = await _sut.RunAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.StandardOutput.Should().Contain("test");
    }

    [Fact]
    public async Task RunAsync_ThrowsOperationCanceled_WhenCanceled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new ProcessRunRequest(
            FileName: "dotnet",
            Arguments: "--version",
            WorkingDirectory: Directory.GetCurrentDirectory(),
            CancellationToken: cts.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.RunAsync(request));
    }

    [Fact]
    public async Task RunAsync_CapturesStandardError()
    {
        var request = new ProcessRunRequest(
            FileName: "dotnet",
            Arguments: "nonexistent-command-12345",
            WorkingDirectory: Directory.GetCurrentDirectory());

        var result = await _sut.RunAsync(request);

        result.StandardError.Should().NotBeNull();
    }
}
