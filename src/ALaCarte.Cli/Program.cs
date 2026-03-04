using System.CommandLine;
using ALaCarte.Cli;
using ALaCarte.Core.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .Build();

// Build service provider
var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
services.AddAlacarteServices(configuration);

var serviceProvider = services.BuildServiceProvider();

// Build command structure
var rootCommand = new RootCommand("ALaCarte - A tool to create a new solution from git repositories");

var nameOption = new Option<string?>(
    aliases: ["--name", "-n"],
    description: "Name for the new workspace")
{
    IsRequired = true
};

rootCommand.AddOption(nameOption);

rootCommand.SetHandler(async (context) =>
{
    var name = context.ParseResult.GetValueForOption(nameOption);

    if (string.IsNullOrWhiteSpace(name))
    {
        Console.Error.WriteLine("Error: --name is required. Usage: a-la-carte --name <workspace-name>");
        context.ExitCode = 1;
        return;
    }

    var handler = serviceProvider.GetRequiredService<ScaffoldCommandHandler>();
    context.ExitCode = await handler.ExecuteAsync(name, context.GetCancellationToken());
});

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

var projectsOption = new Option<string[]?>(
    aliases: ["--projects", "-p"],
    description: "Project names or paths to include (supports wildcards, e.g., 'Messaging.Contracts' or '*/ConferenceEventManager/*')")
{
    AllowMultipleArgumentsPerToken = true
};

initCommand.AddOption(reposOption);
initCommand.AddOption(branchOption);
initCommand.AddOption(folderOption);
initCommand.AddOption(projectsOption);

initCommand.SetHandler(async (context) =>
{
    var repos = context.ParseResult.GetValueForOption(reposOption)!;
    var branch = context.ParseResult.GetValueForOption(branchOption)!;
    var folder = context.ParseResult.GetValueForOption(folderOption);
    var projects = context.ParseResult.GetValueForOption(projectsOption);

    var handler = serviceProvider.GetRequiredService<InitCommandHandler>();
    context.ExitCode = await handler.ExecuteAsync(repos, branch, folder, projects, ct: context.GetCancellationToken());
});

rootCommand.AddCommand(initCommand);

var installCommand = new Command("install", "Install ALaCarte components");

var skillsOption = new Option<bool>(
    aliases: ["--skills", "-s"],
    description: "Install the ALaCarte Claude skill");

installCommand.AddOption(skillsOption);

installCommand.SetHandler(async (context) =>
{
    var skills = context.ParseResult.GetValueForOption(skillsOption);

    if (skills)
    {
        var handler = serviceProvider.GetRequiredService<InstallSkillCommandHandler>();
        var currentDirectory = Directory.GetCurrentDirectory();
        context.ExitCode = await handler.ExecuteAsync(currentDirectory, context.GetCancellationToken());
    }
});

rootCommand.AddCommand(installCommand);

return await rootCommand.InvokeAsync(args);
