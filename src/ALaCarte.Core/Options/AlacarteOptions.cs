namespace ALaCarte.Core.Options;

public class AlacarteOptions
{
    public const string SectionName = "Alacarte";

    public string DefaultBranch { get; set; } = "main";
    public string SubmodulesFolderName { get; set; } = "submodules";
    public string SourceFolderName { get; set; } = "src";
}
