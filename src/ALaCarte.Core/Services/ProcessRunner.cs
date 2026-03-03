using System.Diagnostics;
using ALaCarte.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace ALaCarte.Core.Services;

public class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;

    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessResult> RunAsync(ProcessRunRequest request)
    {
        _logger.LogDebug("Running process: {FileName} {Arguments} in {WorkingDirectory}",
            request.FileName, request.Arguments, request.WorkingDirectory);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            Arguments = request.Arguments,
            WorkingDirectory = request.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (request.UseShellExecute)
        {
            var isWindows = OperatingSystem.IsWindows();
            processStartInfo.FileName = isWindows ? "cmd.exe" : "/bin/bash";
            processStartInfo.Arguments = isWindows
                ? $"/c {request.FileName} {request.Arguments}"
                : $"-c \"{request.FileName} {request.Arguments}\"";
        }

        using var process = new Process { StartInfo = processStartInfo };

        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(request.CancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(request.CancellationToken);

            await process.WaitForExitAsync(request.CancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            _logger.LogDebug("Process exited with code {ExitCode}", process.ExitCode);

            return new ProcessResult(process.ExitCode, output, error);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Process was cancelled: {FileName} {Arguments}", request.FileName, request.Arguments);
            throw;
        }
    }
}
