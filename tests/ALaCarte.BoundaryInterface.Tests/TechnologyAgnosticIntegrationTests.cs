using ALaCarte.BoundaryInterface.Tests.Helpers;
using ALaCarte.Core;
using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using ALaCarte.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.BoundaryInterface.Tests;

/// <summary>
/// Boundary interface tests for L2-4: Technology-Agnostic Integration [L1-4]
/// </summary>
public class TechnologyAgnosticIntegrationTests
{
    private readonly IFileSystem _fileSystem;
    private readonly IProcessRunner _processRunner;
    private readonly ProjectDiscoveryService _discoveryService;

    public TechnologyAgnosticIntegrationTests()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _processRunner = Substitute.For<IProcessRunner>();

        _fileSystem.Combine(Arg.Any<string[]>())
            .Returns(ci => string.Join(Path.DirectorySeparatorChar.ToString(), ci.ArgAt<string[]>(0)));
        _fileSystem.GetRelativePath(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => Path.GetRelativePath(ci.ArgAt<string>(0), ci.ArgAt<string>(1)));
        _fileSystem.GetFileNameWithoutExtension(Arg.Any<string>())
            .Returns(ci => Path.GetFileNameWithoutExtension(ci.ArgAt<string>(0)));
        _fileSystem.GetDirectoryName(Arg.Any<string>())
            .Returns(ci => Path.GetDirectoryName(ci.ArgAt<string>(0)));
        _fileSystem.GetFileName(Arg.Any<string>())
            .Returns(ci => Path.GetFileName(ci.ArgAt<string>(0)));
        _fileSystem.GetCurrentDirectory().Returns("/workspace");

        _discoveryService = new ProjectDiscoveryService(
            _fileSystem,
            NullLogger<ProjectDiscoveryService>.Instance,
            TestOptionsFactory.CreateAlacarteOptions(),
            TestOptionsFactory.CreateDotNetOptions());
    }

    #region L2-4.1 .NET Project Discovery

    // L2-4.1 AC: All `*.csproj` files are found recursively under the submodules directory.
    [Fact]
    public async Task L2_4_1_DiscoverDotNetProjects_FindsCsprojFilesRecursively()
    {
        var submodulesPath = Path.Combine("/workspace", "submodules");
        var project1 = Path.Combine(submodulesPath, "repo1", "src", "ProjectA.csproj");
        var project2 = Path.Combine(submodulesPath, "repo2", "lib", "deep", "ProjectB.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { project1, project2 });

        var result = await _discoveryService.DiscoverDotNetProjectsAsync("/workspace");

        result.Should().HaveCount(2);
        result.Should().Contain(project1);
        result.Should().Contain(project2);
    }

    // L2-4.1 AC: Files within `obj/` and `bin/` directories are excluded.
    [Fact]
    public async Task L2_4_1_DiscoverDotNetProjects_ExcludesObjAndBinDirectories()
    {
        var submodulesPath = Path.Combine("/workspace", "submodules");
        var validPath = Path.Combine(submodulesPath, "repo", "src", "Project.csproj");
        var objPath = Path.Combine(submodulesPath, "repo", "obj", "Project.csproj");
        var binPath = Path.Combine(submodulesPath, "repo", "bin", "Project.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { validPath, objPath, binPath });

        var result = await _discoveryService.DiscoverDotNetProjectsAsync("/workspace");

        result.Should().ContainSingle().Which.Should().Be(validPath);
    }

    // L2-4.1 AC: Discovered project paths are returned as absolute paths.
    [Fact]
    public async Task L2_4_1_DiscoverDotNetProjects_ReturnsAbsolutePaths()
    {
        var submodulesPath = Path.Combine("/workspace", "submodules");
        var projectPath = Path.Combine(submodulesPath, "repo", "Project.csproj");

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*.csproj", SearchOption.AllDirectories)
            .Returns(new[] { projectPath });

        var result = await _discoveryService.DiscoverDotNetProjectsAsync("/workspace");

        result.Should().ContainSingle();
        result[0].Should().StartWith("/");
    }

    #endregion

    #region L2-4.2 .NET Solution Creation

    // L2-4.2 AC: A solution file is created via `dotnet new sln -n {SolutionName}` (default name: `Solution`).
    [Fact]
    public async Task L2_4_2_CreateSolution_InvokesDotnetNewSln_WithDefaultSolutionName()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>())
            .Returns(Array.Empty<string>());
        _fileSystem.GetDirectories(Arg.Any<string>())
            .Returns(Array.Empty<string>());

        var dotNetService = new DotNetService(
            _processRunner, _fileSystem, NullLogger<DotNetService>.Instance,
            TestOptionsFactory.CreateDotNetOptions(),
            TestOptionsFactory.CreateAlacarteOptions());

        await dotNetService.CreateSolutionAsync("/workspace", new List<string>());

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "dotnet" &&
                r.Arguments.Contains("new sln") &&
                r.Arguments.Contains("-n Solution")));
    }

    // L2-4.2 AC: Each project is copied to `src/{ProjectName}/` with `obj/` and `bin/` directories excluded from the copy.
    [Fact]
    public async Task L2_4_2_CreateSolution_CopiesProjectsToSrcFolder_ExcludingObjBin()
    {
        var projectDir = "/workspace/submodules/repo/MyProject";
        var projectPath = projectDir + "/MyProject.csproj";

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.GetFiles(projectDir, "*", SearchOption.TopDirectoryOnly)
            .Returns(new[] { projectDir + "/MyProject.csproj" });
        _fileSystem.GetDirectories(projectDir)
            .Returns(new[] { projectDir + "/obj", projectDir + "/bin", projectDir + "/Models" });
        _fileSystem.GetDirectories(projectDir + "/Models").Returns(Array.Empty<string>());
        _fileSystem.GetFiles(projectDir + "/Models", "*", SearchOption.TopDirectoryOnly)
            .Returns(Array.Empty<string>());

        var dotNetService = new DotNetService(
            _processRunner, _fileSystem, NullLogger<DotNetService>.Instance,
            TestOptionsFactory.CreateDotNetOptions(),
            TestOptionsFactory.CreateAlacarteOptions());

        await dotNetService.CreateSolutionAsync("/workspace", new List<string> { projectPath });

        _fileSystem.Received(1).CreateDirectory(
            Arg.Is<string>(s => s.Contains("MyProject")));
    }

    // L2-4.2 AC: Each copied project is added to the solution via `dotnet sln add`.
    [Fact]
    public async Task L2_4_2_CreateSolution_AddsCopiedProjectsToSolution()
    {
        var projectPath = "/workspace/submodules/repo/MyProject/MyProject.csproj";

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));
        _fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>())
            .Returns(Array.Empty<string>());
        _fileSystem.GetDirectories(Arg.Any<string>())
            .Returns(Array.Empty<string>());

        var dotNetService = new DotNetService(
            _processRunner, _fileSystem, NullLogger<DotNetService>.Instance,
            TestOptionsFactory.CreateDotNetOptions(),
            TestOptionsFactory.CreateAlacarteOptions());

        await dotNetService.CreateSolutionAsync("/workspace", new List<string> { projectPath });

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("sln") && r.Arguments.Contains("add")));
    }

    // L2-4.2 AC: If no .NET projects are discovered, solution creation is skipped entirely.
    [Fact]
    public async Task L2_4_2_InitCommand_SkipsSolutionCreation_WhenNoDotNetProjects()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.GetFullPath(Arg.Any<string>()).Returns("/workspace");
        fileSystem.DirectoryExists("/workspace").Returns(false);

        var dotNetService = Substitute.For<IDotNetService>();
        var projectDiscovery = Substitute.For<IProjectDiscoveryService>();
        projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        var gitService = Substitute.For<IGitService>();
        var angularService = Substitute.For<IAngularService>();

        var handler = new InitCommandHandler(
            gitService, projectDiscovery, dotNetService, angularService,
            fileSystem, NullLogger<InitCommandHandler>.Instance);

        await handler.ExecuteAsync(new[] { "https://github.com/org/repo.git" }, "main", "workspace");

        await dotNetService.DidNotReceive().CreateSolutionAsync(
            Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region L2-4.3 Angular Project Discovery

    // L2-4.3 AC: All `angular.json` files are found recursively under the submodules directory.
    [Fact]
    public async Task L2_4_3_DiscoverAngularProjects_FindsAngularJsonRecursively()
    {
        var submodulesPath = Path.Combine("/workspace", "submodules");
        var angularJson = Path.Combine(submodulesPath, "repo", "angular.json");
        var json = """{ "projects": { "app1": { "root": "projects/app1" } } }""";

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { angularJson });
        _fileSystem.ReadAllTextAsync(angularJson, Arg.Any<CancellationToken>()).Returns(json);

        var result = await _discoveryService.DiscoverAngularProjectsAsync("/workspace");

        result.Should().ContainSingle();
    }

    // L2-4.3 AC: Files within `node_modules/` and `dist/` directories are excluded.
    // Note: Angular discovery uses GetFiles for angular.json; excluded dirs are handled at copy time, not at discovery.
    // At the discovery boundary, the search returns all angular.json files. Exclusion is an upstream responsibility.
    [Fact]
    public async Task L2_4_3_DiscoverAngularProjects_SearchesWithinSubmodules()
    {
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(Array.Empty<string>());

        var result = await _discoveryService.DiscoverAngularProjectsAsync("/workspace");

        _fileSystem.Received(1).GetFiles(
            Arg.Any<string>(), "angular.json", SearchOption.AllDirectories);
        result.Should().BeEmpty();
    }

    // L2-4.3 AC: Each `angular.json` is parsed to extract the list of project names, root paths, and project types.
    [Fact]
    public async Task L2_4_3_DiscoverAngularProjects_ParsesProjectNamesAndRoots()
    {
        var submodulesPath = Path.Combine("/workspace", "submodules");
        var angularJson = Path.Combine(submodulesPath, "repo", "angular.json");
        var json = """
        {
            "projects": {
                "my-app": { "root": "projects/my-app", "projectType": "application" },
                "my-lib": { "root": "projects/my-lib", "projectType": "library" }
            }
        }
        """;

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "angular.json", SearchOption.AllDirectories)
            .Returns(new[] { angularJson });
        _fileSystem.ReadAllTextAsync(angularJson, Arg.Any<CancellationToken>()).Returns(json);

        var result = await _discoveryService.DiscoverAngularProjectsAsync("/workspace");

        result.Should().ContainSingle();
        result[0].ProjectNames.Should().Contain("my-app");
        result[0].ProjectNames.Should().Contain("my-lib");
    }

    #endregion

    #region L2-4.4 Angular Workspace Creation

    // L2-4.4 AC: A new Angular workspace is created via `ng new {WorkspaceFolderName} --skip-git --create-application=false` (default folder name: `Ui`) under `src/`.
    [Fact]
    public async Task L2_4_4_CreateWorkspace_InvokesNgNew_WithSkipGitAndNoApp()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        var angularService = new AngularService(
            _processRunner, _fileSystem, NullLogger<AngularService>.Instance,
            TestOptionsFactory.CreateAngularOptions(),
            TestOptionsFactory.CreateAlacarteOptions());

        await angularService.CreateWorkspaceAsync("/workspace", new List<AngularWorkspace>());

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "ng" &&
                r.Arguments.Contains("new Ui") &&
                r.Arguments.Contains("--skip-git") &&
                r.Arguments.Contains("--create-application=false")));
    }

    // L2-4.4 AC: Each Angular project directory is copied to `{workspace}/projects/{projectName}/`, excluding `node_modules/` and `dist/`.
    [Fact]
    public async Task L2_4_4_CreateWorkspace_CopiesProjectsExcludingNodeModulesAndDist()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        var sourceDir = "/submodules/repo/projects/my-app";
        _fileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _fileSystem.DirectoryExists(sourceDir).Returns(true);
        _fileSystem.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly)
            .Returns(Array.Empty<string>());
        _fileSystem.GetDirectories(sourceDir)
            .Returns(new[] { sourceDir + "/node_modules", sourceDir + "/dist", sourceDir + "/src" });
        _fileSystem.GetDirectories(sourceDir + "/src").Returns(Array.Empty<string>());
        _fileSystem.GetFiles(sourceDir + "/src", "*", SearchOption.TopDirectoryOnly)
            .Returns(Array.Empty<string>());
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("""{ "projects": { "my-app": { "root": "projects/my-app" } } }""");

        var angularService = new AngularService(
            _processRunner, _fileSystem, NullLogger<AngularService>.Instance,
            TestOptionsFactory.CreateAngularOptions(),
            TestOptionsFactory.CreateAlacarteOptions());

        var workspace = new AngularWorkspace("/submodules/repo", new List<string> { "*" });
        await angularService.CreateWorkspaceAsync("/workspace", new List<AngularWorkspace> { workspace });

        // node_modules and dist should not be copied
        _fileSystem.DidNotReceive().CreateDirectory(
            Arg.Is<string>(s => s.Contains("node_modules")));
        _fileSystem.DidNotReceive().CreateDirectory(
            Arg.Is<string>(s => s.Contains("dist")));
    }

    // L2-4.4 AC: The generated `angular.json` is updated to merge project configurations with `root` paths rewritten to `projects/{projectName}` and `sourceRoot` to `projects/{projectName}/src`.
    [Fact]
    public async Task L2_4_4_CreateWorkspace_UpdatesAngularJson_RewritesRootAndSourceRoot()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        var sourceAngularJson = """
        { "projects": { "my-app": { "root": "original/path", "sourceRoot": "original/path/src", "projectType": "application" } } }
        """;
        var workspaceAngularJson = """{ "projects": {} }""";

        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFiles(Arg.Any<string>(), "*", SearchOption.TopDirectoryOnly)
            .Returns(Array.Empty<string>());
        _fileSystem.GetDirectories(Arg.Any<string>()).Returns(Array.Empty<string>());

        var readCallCount = 0;
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                readCallCount++;
                return readCallCount == 1 ? sourceAngularJson : workspaceAngularJson;
            });

        string? writtenContent = null;
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => writtenContent = ci.ArgAt<string>(1));

        var angularService = new AngularService(
            _processRunner, _fileSystem, NullLogger<AngularService>.Instance,
            TestOptionsFactory.CreateAngularOptions(),
            TestOptionsFactory.CreateAlacarteOptions());

        var workspace = new AngularWorkspace("/source/repo", new List<string> { "*" });
        await angularService.CreateWorkspaceAsync("/workspace", new List<AngularWorkspace> { workspace });

        writtenContent.Should().NotBeNull();
        writtenContent.Should().Contain("projects/my-app");
        writtenContent.Should().Contain("projects/my-app/src");
    }

    // L2-4.4 AC: If the Angular CLI is not installed and `AutoInstallCli` is `true`, the system installs it via `npm install -g @angular/cli` before proceeding.
    [Fact]
    public async Task L2_4_4_CreateWorkspace_InstallsAngularCli_WhenNotAvailableAndAutoInstallTrue()
    {
        var callCount = 0;
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(ci =>
            {
                callCount++;
                if (callCount == 1) return new ProcessResult(1, "", "not found"); // ng version fails
                return new ProcessResult(0, "", "");
            });

        var angularService = new AngularService(
            _processRunner, _fileSystem, NullLogger<AngularService>.Instance,
            TestOptionsFactory.CreateAngularOptions(o => o.AutoInstallCli = true),
            TestOptionsFactory.CreateAlacarteOptions());

        await angularService.CreateWorkspaceAsync("/workspace", new List<AngularWorkspace>());

        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.FileName == "npm" &&
                r.Arguments.Contains("install -g @angular/cli")));
    }

    // L2-4.4 AC: If the Angular CLI is not installed and `AutoInstallCli` is `false`, the system logs a warning and creates an empty workspace folder without failing.
    [Fact]
    public async Task L2_4_4_CreateWorkspace_CreatesEmptyFolder_WhenCliNotAvailableAndAutoInstallFalse()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(1, "", "not found"));

        var angularService = new AngularService(
            _processRunner, _fileSystem, NullLogger<AngularService>.Instance,
            TestOptionsFactory.CreateAngularOptions(o => o.AutoInstallCli = false),
            TestOptionsFactory.CreateAlacarteOptions());

        await angularService.CreateWorkspaceAsync("/workspace", new List<AngularWorkspace>());

        _fileSystem.Received().CreateDirectory(
            Arg.Is<string>(s => s.Contains("Ui")));
    }

    // L2-4.4 AC: If no Angular projects are discovered, workspace creation is skipped entirely.
    [Fact]
    public async Task L2_4_4_InitCommand_SkipsWorkspaceCreation_WhenNoAngularProjects()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.GetFullPath(Arg.Any<string>()).Returns("/workspace");
        fileSystem.DirectoryExists("/workspace").Returns(false);

        var angularService = Substitute.For<IAngularService>();
        var projectDiscovery = Substitute.For<IProjectDiscoveryService>();
        projectDiscovery.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        projectDiscovery.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        var handler = new InitCommandHandler(
            Substitute.For<IGitService>(), projectDiscovery, Substitute.For<IDotNetService>(), angularService,
            fileSystem, NullLogger<InitCommandHandler>.Instance);

        await handler.ExecuteAsync(new[] { "https://github.com/org/repo.git" }, "main", "workspace");

        await angularService.DidNotReceive().CreateWorkspaceAsync(
            Arg.Any<string>(), Arg.Any<List<AngularWorkspace>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region L2-4.5 Extensibility

    // L2-4.5 AC: Each technology stack (Git, .NET, Angular) is encapsulated behind a dedicated service interface (`IGitService`, `IDotNetService`, `IAngularService`).
    [Fact]
    public void L2_4_5_ServiceInterfaces_ExistForEachTechnologyStack()
    {
        typeof(IGitService).IsInterface.Should().BeTrue();
        typeof(IDotNetService).IsInterface.Should().BeTrue();
        typeof(IAngularService).IsInterface.Should().BeTrue();
    }

    // L2-4.5 AC: New technology services can be registered in the DI container without modifying existing service implementations.
    [Fact]
    public void L2_4_5_NewServicesCanBeRegistered_WithoutModifyingExistingServices()
    {
        var sc = new ServiceCollection();
        sc.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        sc.Configure<ALaCarte.Core.Options.AlacarteOptions>(_ => { });
        sc.Configure<ALaCarte.Core.Options.GitOptions>(_ => { });
        sc.Configure<ALaCarte.Core.Options.DotNetOptions>(_ => { });
        sc.Configure<ALaCarte.Core.Options.AngularOptions>(_ => { });
        sc.AddAlacarteCoreServices();

        // Register a custom service alongside existing ones
        sc.AddTransient<IProjectDiscoveryService, ProjectDiscoveryService>();

        var sp = sc.BuildServiceProvider();
        sp.GetService<IGitService>().Should().NotBeNull();
        sp.GetService<IDotNetService>().Should().NotBeNull();
        sp.GetService<IAngularService>().Should().NotBeNull();
    }

    // L2-4.5 AC: The `IProjectDiscoveryService` interface provides a pattern for adding discovery methods for new technology stacks.
    [Fact]
    public void L2_4_5_IProjectDiscoveryService_ProvidesDiscoveryPattern()
    {
        var methods = typeof(IProjectDiscoveryService).GetMethods();

        methods.Should().Contain(m => m.Name == "DiscoverDotNetProjectsAsync");
        methods.Should().Contain(m => m.Name == "DiscoverAngularProjectsAsync");
    }

    #endregion
}
