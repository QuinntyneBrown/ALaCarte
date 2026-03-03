namespace ALaCarte.Cli.Exceptions;

public class AngularOperationException : Exception
{
    public string? Command { get; }
    public int? ExitCode { get; }
    public string? StandardError { get; }

    public AngularOperationException(string message) : base(message)
    {
    }

    public AngularOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public AngularOperationException(string message, string command, int exitCode, string standardError)
        : base(message)
    {
        Command = command;
        ExitCode = exitCode;
        StandardError = standardError;
    }
}
