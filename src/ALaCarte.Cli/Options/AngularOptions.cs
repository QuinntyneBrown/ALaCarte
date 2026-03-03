namespace ALaCarte.Cli.Options;

public class AngularOptions
{
    public const string SectionName = "Angular";

    public string WorkspaceFolderName { get; set; } = "Ui";
    public bool AutoInstallCli { get; set; } = true;
    public string[] ExcludedDirectories { get; set; } = ["node_modules", "dist"];
}
