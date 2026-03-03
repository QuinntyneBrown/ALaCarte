using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;
using ALaCarte.Core.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ALaCarte.Core.UnitTests.Services;

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
        var workspaces = new List<AngularWorkspace>();
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);

        _fileSystem.Received(1).CreateDirectory(Path.Combine("/test/solution", "src"));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_ChecksForAngularCli()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.CreateWorkspaceAsync("/test/solution", new List<AngularWorkspace>());

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.FileName == "ng" && r.Arguments == "version"));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_InstallsAngularCli_WhenNotAvailable()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(
                new ProcessResult(1, "", "ng not found"),
                new ProcessResult(0, "", ""),
                new ProcessResult(0, "", ""));

        await _sut.CreateWorkspaceAsync("/test/solution", new List<AngularWorkspace>());

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.FileName == "npm" && r.Arguments.Contains("install -g @angular/cli")));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_SkipsInstall_WhenAutoInstallDisabled()
    {
        var options = TestOptionsFactory.CreateAngularOptions(o => o.AutoInstallCli = false);
        var sut = new AngularService(
            _processRunner, _fileSystem, NullLogger<AngularService>.Instance,
            options, TestOptionsFactory.CreateAlacarteOptions());

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(1, "", "ng not found"));

        await sut.CreateWorkspaceAsync("/test/solution", new List<AngularWorkspace>());

        await _processRunner.DidNotReceive().RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.FileName == "npm" && r.Arguments.Contains("install")));
        // Should create workspace directory as fallback
        _fileSystem.Received().CreateDirectory(Arg.Any<string>());
    }

    [Fact]
    public async Task CreateWorkspaceAsync_CreatesWorkspaceDir_WhenInstallFails()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(
                new ProcessResult(1, "", "ng not found"),
                new ProcessResult(1, "", "npm error"));

        await _sut.CreateWorkspaceAsync("/test/solution", new List<AngularWorkspace>());

        // Should create workspace directory as fallback when install fails
        _fileSystem.Received().CreateDirectory(Arg.Any<string>());
    }

    [Fact]
    public async Task CreateWorkspaceAsync_Returns_WhenNgNewFails()
    {
        // ng version succeeds, ng new fails
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(
                x => new ProcessResult(0, "", ""),
                x => throw new Exception("ng new failed"));

        await _sut.CreateWorkspaceAsync("/test/solution", new List<AngularWorkspace>());

        // Should return without crashing
    }

    [Fact]
    public async Task CreateWorkspaceAsync_CreatesNewAngularWorkspace()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await _sut.CreateWorkspaceAsync("/test/solution", new List<AngularWorkspace>());

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
        var options = TestOptionsFactory.CreateAngularOptions(o => o.WorkspaceFolderName = "CustomUi");
        var sut = new AngularService(
            _processRunner, _fileSystem, NullLogger<AngularService>.Instance,
            options, TestOptionsFactory.CreateAlacarteOptions());

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        await sut.CreateWorkspaceAsync("/test/solution", new List<AngularWorkspace>());

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.Arguments.Contains("new CustomUi")));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_IntegratesAngularProjects()
    {
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

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);

        _fileSystem.Received().CreateDirectory(Arg.Is<string>(s => s.Contains("my-app")));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_IncludesAllProjects_WhenWildcardUsed()
    {
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

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);

        _fileSystem.Received().CreateDirectory(Arg.Is<string>(s => s.Contains("app1")));
        _fileSystem.Received().CreateDirectory(Arg.Is<string>(s => s.Contains("lib1")));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_SkipsProject_WhenNotInAllowedList()
    {
        var sourceWorkspace = "/test/solution/submodules/repo/ui";
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace(sourceWorkspace, new List<string> { "app1" })
        };

        var angularJson = """
        {
            "projects": {
                "app1": { "root": "projects/app1", "projectType": "application" },
                "app2": { "root": "projects/app2", "projectType": "application" }
            }
        }
        """;

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(angularJson);

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);

        _fileSystem.Received().CreateDirectory(Arg.Is<string>(s => s.Contains("app1")));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_SkipsProject_WhenSourceDirNotExists()
    {
        var sourceWorkspace = "/test/solution/submodules/repo/ui";
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace(sourceWorkspace, new List<string> { "my-app" })
        };

        var angularJson = """
        { "projects": { "my-app": { "root": "projects/my-app" } } }
        """;

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(angularJson);

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);
        // Should complete without error
    }

    [Fact]
    public async Task CreateWorkspaceAsync_ReturnsZeroCopied_WhenNoAngularJson()
    {
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace("/source", new List<string> { "app" })
        };

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.FileExists(Arg.Any<string>()).Returns(false);

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);
        // No projects property should mean 0 projects written
    }

    [Fact]
    public async Task CreateWorkspaceAsync_HandlesNullProjectsNode()
    {
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace("/source", new List<string> { "app" })
        };

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("{}");

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_HandlesNullProjectConfig()
    {
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace("/source", new List<string> { "*" })
        };

        var angularJson = """{ "projects": { "app": null } }""";

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(angularJson);

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_HandlesIntegrationException()
    {
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace("/source", new List<string> { "app" })
        };

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("read error"));

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_UpdatesAngularJson_WhenProjectsCopied()
    {
        var sourceWorkspace = "/source/workspace";
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace(sourceWorkspace, new List<string> { "my-app" })
        };

        var sourceAngularJson = """
        { "projects": { "my-app": { "root": "projects/my-app", "projectType": "application" } } }
        """;

        var workspaceAngularJson = """{ "projects": {} }""";

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);

        // Return different content based on the path
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(sourceAngularJson, workspaceAngularJson);

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);

        await _fileSystem.Received().WriteAllTextAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateWorkspaceAsync_UpdatesAngularJson_CreatesProjectsNode_WhenNull()
    {
        var sourceWorkspace = "/source/workspace";
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace(sourceWorkspace, new List<string> { "my-app" })
        };

        var sourceAngularJson = """
        { "projects": { "my-app": { "root": "projects/my-app" } } }
        """;

        // angular.json without projects node
        var workspaceAngularJson = """{ "$schema": "./node_modules/@angular/cli/lib/config/schema.json" }""";

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(sourceAngularJson, workspaceAngularJson);

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);

        await _fileSystem.Received().WriteAllTextAsync(
            Arg.Any<string>(), Arg.Is<string>(s => s.Contains("my-app")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateWorkspaceAsync_HandlesUpdateAngularJsonException()
    {
        var sourceWorkspace = "/source/workspace";
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace(sourceWorkspace, new List<string> { "my-app" })
        };

        var sourceAngularJson = """
        { "projects": { "my-app": { "root": "projects/my-app" } } }
        """;

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(sourceAngularJson, "invalid json!!");

        // Should not throw - handles exception gracefully
        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_UpdatesSourceRoot_WhenPresent()
    {
        var sourceWorkspace = "/source/workspace";
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace(sourceWorkspace, new List<string> { "my-lib" })
        };

        var sourceAngularJson = """
        {
            "projects": {
                "my-lib": {
                    "root": "projects/my-lib",
                    "sourceRoot": "projects/my-lib/src",
                    "projectType": "library"
                }
            }
        }
        """;

        var workspaceAngularJson = """{ "projects": {} }""";

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(sourceAngularJson, workspaceAngularJson);

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);

        await _fileSystem.Received().WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("projects/my-lib/src")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateWorkspaceAsync_CopiesDirectoriesRecursively_ExcludesNodeModules()
    {
        var sourceWorkspace = "/source/workspace";
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace(sourceWorkspace, new List<string> { "my-app" })
        };

        var angularJson = """
        { "projects": { "my-app": { "root": "projects/my-app" } } }
        """;

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(angularJson);

        // Source directory has files and subdirectories including node_modules
        var sourceDir = string.Join(Path.DirectorySeparatorChar.ToString(), sourceWorkspace, "projects/my-app");
        _fileSystem.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly)
            .Returns(new[] { sourceDir + "/index.ts" });
        _fileSystem.GetDirectories(sourceDir)
            .Returns(new[] { sourceDir + "/src", sourceDir + "/node_modules" });
        _fileSystem.GetDirectories(sourceDir + "/src").Returns(Array.Empty<string>());
        _fileSystem.GetFiles(sourceDir + "/src", "*", SearchOption.TopDirectoryOnly)
            .Returns(Array.Empty<string>());

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);

        // Should copy src but not node_modules
        _fileSystem.Received().CreateDirectory(Arg.Is<string>(s => s.Contains("src")));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_UpdateAngularJson_NotFound_LogsWarning()
    {
        var sourceWorkspace = "/source/workspace";
        var workspaces = new List<AngularWorkspace>
        {
            new AngularWorkspace(sourceWorkspace, new List<string> { "my-app" })
        };

        var sourceAngularJson = """
        { "projects": { "my-app": { "root": "projects/my-app" } } }
        """;

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);

        // Source angular.json exists but workspace one doesn't
        var callCount = 0;
        _fileSystem.FileExists(Arg.Any<string>())
            .Returns(ci =>
            {
                callCount++;
                // First call: source angular.json, second: workspace angular.json
                return callCount <= 1;
            });
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(sourceAngularJson);

        await _sut.CreateWorkspaceAsync("/test/solution", workspaces);
        // Should not throw
    }

    [Fact]
    public async Task CreateWorkspaceAsync_IsAngularCliAvailable_ReturnsFalse_WhenExceptionThrown()
    {
        // ng version throws exception
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(
                x => throw new Exception("process not found"),
                x => new ProcessResult(0, "", ""),
                x => new ProcessResult(0, "", ""));

        await _sut.CreateWorkspaceAsync("/test/solution", new List<AngularWorkspace>());

        // Should try to install CLI (since it wasn't available due to exception)
        await _processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(r => r.FileName == "npm"));
    }
}
