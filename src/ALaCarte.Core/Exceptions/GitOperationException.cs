namespace ALaCarte.Core.Exceptions;

public class GitOperationException : Exception
{
    public string? Command { get; }
    public int? ExitCode { get; }
    public string? StandardError { get; }

    public GitOperationException(string message) : base(message)
    {
    }

    public GitOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public GitOperationException(string message, string command, int exitCode, string standardError)
        : base(message)
    {
        Command = command;
        ExitCode = exitCode;
        StandardError = standardError;
    }
}
