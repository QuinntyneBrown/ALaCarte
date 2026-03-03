namespace ALaCarte.Core.Exceptions;

public class DotNetOperationException : Exception
{
    public string? Command { get; }
    public int? ExitCode { get; }
    public string? StandardError { get; }

    public DotNetOperationException(string message) : base(message)
    {
    }

    public DotNetOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public DotNetOperationException(string message, string command, int exitCode, string standardError)
        : base(message)
    {
        Command = command;
        ExitCode = exitCode;
        StandardError = standardError;
    }
}
