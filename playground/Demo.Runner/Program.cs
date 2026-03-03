using ALaCarte.Cli;

// Configuration for the Meta solution
var repos = new[]
{
    "https://github.com/QuinntyneBrown/Apps",
    "https://github.com/QuinntyneBrown/Books"
};

var projectFilters = new[]
{
    "ConferenceEventManager/src/Messaging.Contracts",  // Specific Messaging.Contracts from ConferenceEventManager
    "Books/src/Ui/projects/components"  // Angular workspace containing the components library
};

var branch = "main";

// Calculate the path to artifacts/Meta from the repository root
var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var artifactsFolder = Path.Combine(repoRoot, "artifacts", "Meta");

Console.WriteLine("=== ALaCarte Demo Runner ===");
Console.WriteLine($"Repository root: {repoRoot}");
Console.WriteLine($"Output folder: {artifactsFolder}");
Console.WriteLine();

// Execute the CLI to generate the Meta solution
var handler = new InitCommandHandler();
await handler.ExecuteAsync(repos, branch, artifactsFolder, projectFilters);
