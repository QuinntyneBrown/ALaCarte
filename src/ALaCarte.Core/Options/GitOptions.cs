namespace ALaCarte.Core.Options;

public class GitOptions
{
    public const string SectionName = "Git";

    public string ExecutablePath { get; set; } = "git";
    public int TimeoutSeconds { get; set; } = 300;
}
