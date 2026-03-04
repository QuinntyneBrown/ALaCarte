using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using CodeGenerator.Core.Artifacts.Abstractions;
using CodeGenerator.DotNet.Artifacts.Projects;
using CodeGenerator.DotNet.Artifacts.Projects.Enums;
using CodeGenerator.DotNet.Artifacts.Solutions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ALaCarte.BoundaryInterface.Tests;

public class ScaffoldCommandTests
{
    private readonly IFileSystem _fileSystem;
    private readonly IArtifactGenerator _artifactGenerator;
    private readonly InstallSkillCommandHandler _installSkillHandler;
    private readonly ILogger<ScaffoldCommandHandler> _logger;
    private readonly ScaffoldCommandHandler _handler;

    public ScaffoldCommandTests()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _artifactGenerator = Substitute.For<IArtifactGenerator>();
        _logger = Substitute.For<ILogger<ScaffoldCommandHandler>>();

        _fileSystem.Combine(Arg.Any<string[]>())
            .Returns(callInfo =>
            {
                var paths = callInfo.Arg<string[]>();
                return string.Join("/", paths);
            });

        _fileSystem.GetFullPath(Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<string>());

        _fileSystem.GetCurrentDirectory()
            .Returns("/working");

        var skillLogger = Substitute.For<ILogger<InstallSkillCommandHandler>>();
        _installSkillHandler = new InstallSkillCommandHandler(_fileSystem, skillLogger);

        _handler = new ScaffoldCommandHandler(_fileSystem, _artifactGenerator, _installSkillHandler, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_WithName_CreatesWorkspaceNamedAfterArgument()
    {
        // Arrange
        var name = "Foo";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        _fileSystem.Received().CreateDirectory(
            Arg.Is<string>(s => s == "/working/Foo"));
    }

    [Fact]
    public async Task ExecuteAsync_WithName_UsesNameAsTopLevelDirectory()
    {
        // Arrange
        var name = "MyProject";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        _fileSystem.Received().CreateDirectory(
            Arg.Is<string>(s => s.Contains("MyProject")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithEmptyOrWhitespaceName_ReturnsValidationError(string name)
    {
        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(1, exitCode);
        _fileSystem.DidNotReceive().CreateDirectory(Arg.Any<string>());
    }

    [Theory]
    [InlineData("my<project")]
    [InlineData("my>project")]
    [InlineData("my:project")]
    [InlineData("my\"project")]
    [InlineData("my|project")]
    [InlineData("my?project")]
    [InlineData("my*project")]
    public async Task ExecuteAsync_WithInvalidDirectoryNameChars_ReturnsValidationError(string name)
    {
        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(1, exitCode);
        _fileSystem.DidNotReceive().CreateDirectory(Arg.Any<string>());
    }

    [Theory]
    [InlineData("ValidName")]
    [InlineData("my-project")]
    [InlineData("my_project")]
    [InlineData("MyProject123")]
    public async Task ExecuteAsync_WithValidDirectoryName_Succeeds(string name)
    {
        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesDirectoryInCurrentWorkingDirectory()
    {
        // Arrange
        _fileSystem.GetCurrentDirectory().Returns("/home/user");
        var name = "Foo";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        _fileSystem.Received(1).CreateDirectory("/home/user/Foo");
    }

    [Fact]
    public async Task ExecuteAsync_WhenDirectoryAlreadyExists_ReturnsValidationError()
    {
        // Arrange
        var name = "Foo";
        _fileSystem.DirectoryExists("/working/Foo").Returns(true);

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(1, exitCode);
        _fileSystem.DidNotReceive().CreateDirectory(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        var name = "Foo";
        _fileSystem.DirectoryExists("/working/Foo").Returns(false);

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        _fileSystem.Received(1).CreateDirectory("/working/Foo");
    }

    [Fact]
    public async Task ExecuteAsync_CreatesDirectoryBeforeAnyOtherOperations()
    {
        // Arrange
        var name = "Foo";
        var callOrder = new List<string>();

        _fileSystem.When(x => x.CreateDirectory(Arg.Any<string>()))
            .Do(_ => callOrder.Add("CreateDirectory"));

        _artifactGenerator.GenerateAsync(Arg.Any<object>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("GenerateAsync"));

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("CreateDirectory", callOrder);
        Assert.Equal("CreateDirectory", callOrder.First());
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesSolutionUsingArtifactGenerator()
    {
        // Arrange
        var name = "Foo";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        await _artifactGenerator.Received(1).GenerateAsync(
            Arg.Is<SolutionModel>(s => s.Name == "build"));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesSolutionInWorkspaceDirectory()
    {
        // Arrange
        var name = "Foo";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        await _artifactGenerator.Received(1).GenerateAsync(
            Arg.Is<SolutionModel>(s =>
                s.Name == "build" &&
                s.SolutionDirectory == "/working/Foo"));
    }

    [Fact]
    public async Task ExecuteAsync_AddsConsoleProjectToSolution()
    {
        // Arrange
        var name = "Foo";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        await _artifactGenerator.Received(1).GenerateAsync(
            Arg.Is<SolutionModel>(s =>
                s.Projects.Count == 1 &&
                s.Projects[0].Name == "build" &&
                s.Projects[0].DotNetProjectType == DotNetProjectType.Console));
    }

    [Fact]
    public async Task ExecuteAsync_ConsoleProjectIsGeneratedViaCodeGeneratorLibrary()
    {
        // Arrange
        var name = "Foo";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        // The project is part of the SolutionModel passed to IArtifactGenerator
        await _artifactGenerator.Received(1).GenerateAsync(
            Arg.Is<SolutionModel>(s =>
                s.Projects.Any(p => p.Name == "build")));
    }

    [Fact]
    public async Task ExecuteAsync_ProjectContainsALaCarteCorePackageReference()
    {
        // Arrange
        var name = "Foo";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        await _artifactGenerator.Received(1).GenerateAsync(
            Arg.Is<SolutionModel>(s =>
                s.Projects[0].Packages.Any(p =>
                    p.Name == "QuinntyneBrown.ALaCarte.Core")));
    }

    [Fact]
    public async Task ExecuteAsync_ALaCarteCorePackageDoesNotPinVersion()
    {
        // Arrange
        var name = "Foo";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        await _artifactGenerator.Received(1).GenerateAsync(
            Arg.Is<SolutionModel>(s =>
                s.Projects[0].Packages
                    .Where(p => p.Name == "QuinntyneBrown.ALaCarte.Core")
                    .All(p => string.IsNullOrEmpty(p.Version))));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesSkillDirectoryInBuildFolder()
    {
        // Arrange
        var name = "Foo";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        _fileSystem.Received().CreateDirectory(
            Arg.Is<string>(s => s.Contains("build/.claude/skills/a-la-carte")));
    }

    [Fact]
    public async Task ExecuteAsync_WritesSkillMdToBuildFolder()
    {
        // Arrange
        var name = "Foo";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        await _fileSystem.Received().WriteAllTextAsync(
            Arg.Is<string>(s =>
                s.Contains("build/.claude/skills/a-la-carte/SKILL.md")),
            Arg.Is<string>(content => content.Contains("a-la-carte")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SkillIsInstalledAutomaticallyDuringScaffolding()
    {
        // Arrange
        var name = "Foo";

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert - skill file is written as part of scaffolding, not a separate command
        Assert.Equal(0, exitCode);
        await _fileSystem.Received().WriteAllTextAsync(
            Arg.Is<string>(s => s.EndsWith("SKILL.md")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SkillContentMatchesInstallSkillHandler()
    {
        // Arrange
        var name = "Foo";
        string? writtenContent = null;

        await _fileSystem.WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Do<string>(content => writtenContent = content),
            Arg.Any<CancellationToken>());

        // Act
        var exitCode = await _handler.ExecuteAsync(name);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.NotNull(writtenContent);
        Assert.Equal(InstallSkillCommandHandler.SkillContent, writtenContent);
    }
}
