namespace ALaCarte.Cli.Abstractions;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessRunRequest request);
}

public record ProcessRunRequest(
    string FileName,
    string Arguments,
    string WorkingDirectory,
    bool UseShellExecute = false,
    CancellationToken CancellationToken = default);

public record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool IsSuccess => ExitCode == 0;
}
