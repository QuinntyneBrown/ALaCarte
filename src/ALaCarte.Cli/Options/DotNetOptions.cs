namespace ALaCarte.Cli.Options;

public class DotNetOptions
{
    public const string SectionName = "DotNet";

    public string ExecutablePath { get; set; } = "dotnet";
    public string SolutionName { get; set; } = "Solution";
    public string[] ExcludedDirectories { get; set; } = ["obj", "bin"];
}
