using System.CommandLine;
using ALaCarte.Cli;

var rootCommand = new RootCommand("ALaCarte - A tool to create a new solution from git repositories");

var initCommand = new Command("init", "Initialize a new solution from git repositories");

var reposOption = new Option<string[]>(
    aliases: ["--repos", "-r"],
    description: "Git repository URLs to include")
{
    IsRequired = true,
    AllowMultipleArgumentsPerToken = true
};

var branchOption = new Option<string>(
    aliases: ["--branch", "-b"],
    description: "Git branch to use",
    getDefaultValue: () => "main");

var folderOption = new Option<string?>(
    aliases: ["--folder", "-f"],
    description: "Folder name for the new solution (optional)");

initCommand.AddOption(reposOption);
initCommand.AddOption(branchOption);
initCommand.AddOption(folderOption);

initCommand.SetHandler(async (string[] repos, string branch, string? folder) =>
{
    var handler = new InitCommandHandler();
    await handler.ExecuteAsync(repos, branch, folder);
}, reposOption, branchOption, folderOption);

rootCommand.AddCommand(initCommand);

return await rootCommand.InvokeAsync(args);
