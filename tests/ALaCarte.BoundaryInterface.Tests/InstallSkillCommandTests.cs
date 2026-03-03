using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ALaCarte.BoundaryInterface.Tests;

public class InstallSkillCommandTests
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<InstallSkillCommandHandler> _logger;
    private readonly InstallSkillCommandHandler _handler;

    public InstallSkillCommandTests()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _logger = Substitute.For<ILogger<InstallSkillCommandHandler>>();

        _fileSystem.Combine(Arg.Any<string[]>())
            .Returns(callInfo =>
            {
                var paths = callInfo.Arg<string[]>();
                return string.Join("/", paths);
            });

        _fileSystem.GetFullPath(Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<string>());

        _handler = new InstallSkillCommandHandler(_fileSystem, _logger);
    }

    [Fact]
    public async Task InstallSkills_CreatesSkillDirectoryStructure()
    {
        // Arrange
        var outputDirectory = "/test/output";

        // Act
        var exitCode = await _handler.ExecuteAsync(outputDirectory);

        // Assert
        _fileSystem.Received(1).CreateDirectory(
            Arg.Is<string>(s => s.Contains(".claude/skills/a-la-carte")));
    }

    [Fact]
    public async Task InstallSkills_GeneratesSkillMdFile()
    {
        // Arrange
        var outputDirectory = "/test/output";

        // Act
        var exitCode = await _handler.ExecuteAsync(outputDirectory);

        // Assert
        await _fileSystem.Received(1).WriteAllTextAsync(
            Arg.Is<string>(s => s.EndsWith("SKILL.md")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InstallSkills_OverwritesExistingSkillFile()
    {
        // Arrange
        var outputDirectory = "/test/output";
        var skillFilePath = "/test/output/.claude/skills/a-la-carte/SKILL.md";
        _fileSystem.FileExists(skillFilePath).Returns(true);

        // Act - execute twice to simulate overwrite
        await _handler.ExecuteAsync(outputDirectory);
        await _handler.ExecuteAsync(outputDirectory);

        // Assert - WriteAllTextAsync overwrites by default, so called twice
        await _fileSystem.Received(2).WriteAllTextAsync(
            Arg.Is<string>(s => s.EndsWith("SKILL.md")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InstallSkills_LogsFullPathOnSuccess()
    {
        // Arrange
        var outputDirectory = "/test/output";
        var expectedFullPath = "/test/output/.claude/skills/a-la-carte/SKILL.md";
        _fileSystem.GetFullPath(Arg.Any<string>()).Returns(expectedFullPath);

        // Act
        var exitCode = await _handler.ExecuteAsync(outputDirectory);

        // Assert
        Assert.Equal(0, exitCode);
        _fileSystem.Received(1).GetFullPath(Arg.Is<string>(s => s.EndsWith("SKILL.md")));
    }

    [Fact]
    public async Task InstallSkills_ReturnsZeroExitCodeOnSuccess()
    {
        // Arrange
        var outputDirectory = "/test/output";

        // Act
        var exitCode = await _handler.ExecuteAsync(outputDirectory);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task InstallSkills_WritesSkillContentToFile()
    {
        // Arrange
        var outputDirectory = "/test/output";
        string? writtenContent = null;

        await _fileSystem.WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Do<string>(content => writtenContent = content),
            Arg.Any<CancellationToken>());

        // Act
        await _handler.ExecuteAsync(outputDirectory);

        // Assert
        Assert.NotNull(writtenContent);
        Assert.Contains("a-la-carte", writtenContent);
    }
}
