using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;
using ALaCarte.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.Core.Tests.Services;

public class AngularServiceTests
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly AngularService _sut;

    public AngularServiceTests()
    {
        _processRunner = Substitute.For<IProcessRunner>();
        _fileSystem = Substitute.For<IFileSystem>();

        _fileSystem.Combine(Arg.Any<string[]>())
            .Returns(ci => string.Join(Path.DirectorySeparatorChar.ToString(), ci.ArgAt<string[]>(0)));
        _fileSystem.GetRelativePath(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => Path.GetRelativePath(ci.ArgAt<string>(0), ci.ArgAt<string>(1)));
        _fileSystem.GetFileName(Arg.Any<string>())
            .Returns(ci => Path.GetFileName(ci.ArgAt<string>(0)));
        _fileSystem.GetCurrentDirectory()
            .Returns(Directory.GetCurrentDirectory());
        _fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>())
            .Returns(Array.Empty<string>());
        _fileSystem.GetDirectories(Arg.Any<string>())
            .Returns(Array.Empty<string>());

        _sut = new AngularService(
            _processRunner,
            _fileSystem,
            NullLogger<AngularService>.Instance,
            TestOptionsFactory.CreateAngularOptions(),
            TestOptionsFactory.CreateAlacarteOptions());
    }

    [Fact]
    public async Task CreateWorkspaceAsync_CreatesSrcDirectory()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var workspaces = new List<AngularWorkspace>();

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await _sut.CreateWorkspaceAsync(solutionPath, workspaces);

        // Assert
        _fileSystem.Received(1).CreateDirectory(Path.Combine(solutionPath, "src"));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_ChecksForAngularCli()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var workspaces = new List<AngularWorkspace>();

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await _sut.CreateWorkspaceAsync(solutionPath, workspaces);

        // Assert
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "ng" &&
                r.Arguments == "version"));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_InstallsAngularCli_WhenNotAvailable()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var workspaces = new List<AngularWorkspace>();

        // ng version fails, npm install succeeds, ng new succeeds
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(
                new ProcessResult(1, "", "ng not found"), // ng version
                new ProcessResult(0, "", ""),              // npm install
                new ProcessResult(0, "", ""));             // ng new

        // Act
        await _sut.CreateWorkspaceAsync(solutionPath, workspaces);

        // Assert
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "npm" &&
                r.Arguments.Contains("install -g @angular/cli")));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_SkipsInstall_WhenAutoInstallDisabled()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var workspaces = new List<AngularWorkspace>();
        var options = TestOptionsFactory.CreateAngularOptions(o => o.AutoInstallCli = false);

        var sut = new AngularService(
            _processRunner,
            _fileSystem,
            NullLogger<AngularService>.Instance,
            options,
            TestOptionsFactory.CreateAlacarteOptions());

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(1, "", "ng not found"));

        // Act
        await sut.CreateWorkspaceAsync(solutionPath, workspaces);

        // Assert
        await _processRunner.DidNotReceive().RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "npm" &&
                r.Arguments.Contains("install")));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_CreatesNewAngularWorkspace()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var workspaces = new List<AngularWorkspace>();

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await _sut.CreateWorkspaceAsync(solutionPath, workspaces);

        // Assert
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "ng" &&
                r.Arguments.Contains("new Ui") &&
                r.Arguments.Contains("--skip-git") &&
                r.Arguments.Contains("--create-application=false")));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_UsesConfiguredWorkspaceFolderName()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var workspaces = new List<AngularWorkspace>();
        var options = TestOptionsFactory.CreateAngularOptions(o => o.WorkspaceFolderName = "CustomUi");

        var sut = new AngularService(
            _processRunner,
            _fileSystem,
            NullLogger<AngularService>.Instance,
            options,
            TestOptionsFactory.CreateAlacarteOptions());

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Act
        await sut.CreateWorkspaceAsync(solutionPath, workspaces);

        // Assert
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("new CustomUi")));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_IntegratesAngularProjects()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var sourceWorkspace = "/test/solution/submodules/repo/ui";
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace(sourceWorkspace, new List<string> { "my-app" })
        };

        var angularJson = """
        {
            "projects": {
                "my-app": {
                    "root": "projects/my-app",
                    "projectType": "application"
                }
            }
        }
        """;

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(angularJson);

        // Act
        await _sut.CreateWorkspaceAsync(solutionPath, workspaces);

        // Assert - should have tried to copy the project
        _fileSystem.Received().CreateDirectory(
            Arg.Is<string>(s => s.Contains("my-app")));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_IncludesAllProjects_WhenWildcardUsed()
    {
        // Arrange
        var solutionPath = "/test/solution";
        var sourceWorkspace = "/test/solution/submodules/repo/ui";
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace(sourceWorkspace, new List<string> { "*" })
        };

        var angularJson = """
        {
            "projects": {
                "app1": { "root": "projects/app1", "projectType": "application" },
                "lib1": { "root": "projects/lib1", "projectType": "library" }
            }
        }
        """;

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(angularJson);

        // Act
        await _sut.CreateWorkspaceAsync(solutionPath, workspaces);

        // Assert - should copy both projects
        _fileSystem.Received().CreateDirectory(
            Arg.Is<string>(s => s.Contains("app1")));
        _fileSystem.Received().CreateDirectory(
            Arg.Is<string>(s => s.Contains("lib1")));
    }
}
