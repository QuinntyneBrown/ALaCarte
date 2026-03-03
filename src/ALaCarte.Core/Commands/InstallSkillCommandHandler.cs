using ALaCarte.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace ALaCarte.Core.Commands;

public class InstallSkillCommandHandler
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<InstallSkillCommandHandler> _logger;

    public const string SkillContent = """
        ---
        name: a-la-carte
        description: Create composite solutions from multiple git repositories using ALaCarte
        user-invocable: true
        ---

        # ALaCarte Skill

        Use this skill when asked to create composite .NET/Angular solutions from multiple git repositories.
        """;

    public InstallSkillCommandHandler(
        IFileSystem fileSystem,
        ILogger<InstallSkillCommandHandler> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(string outputDirectory, CancellationToken ct = default)
    {
        try
        {
            var skillDirectory = _fileSystem.Combine(outputDirectory, ".claude", "skills", "a-la-carte");
            _fileSystem.CreateDirectory(skillDirectory);

            var skillFilePath = _fileSystem.Combine(skillDirectory, "SKILL.md");
            await _fileSystem.WriteAllTextAsync(skillFilePath, SkillContent, ct);

            var fullPath = _fileSystem.GetFullPath(skillFilePath);
            _logger.LogInformation("Installed Claude skill: {Path}", fullPath);

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing skill: {Message}", ex.Message);
            return 1;
        }
    }
}
