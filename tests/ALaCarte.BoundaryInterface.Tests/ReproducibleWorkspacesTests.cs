using ALaCarte.BoundaryInterface.Tests.Helpers;
using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Commands;
using ALaCarte.Core.Options;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.BoundaryInterface.Tests;

/// <summary>
/// Boundary interface tests for L2-7: Reproducible Workspaces [L1-7]
/// </summary>
public class ReproducibleWorkspacesTests
{
    #region L2-7.1 Deterministic Configuration

    // L2-7.1 AC: Configuration is loaded from `appsettings.json` at the CLI root.
    // Note: This is verified at the CLI layer (Program.cs). Boundary test verifies that options are consumed.
    [Fact]
    public void L2_7_1_Configuration_OptionsClassesHaveCorrectSectionNames()
    {
        AlacarteOptions.SectionName.Should().Be("Alacarte");
        GitOptions.SectionName.Should().Be("Git");
        DotNetOptions.SectionName.Should().Be("DotNet");
        AngularOptions.SectionName.Should().Be("Angular");
    }

    // L2-7.1 AC: The following settings are configurable: DefaultBranch, SubmodulesFolderName, SourceFolderName, SolutionName, WorkspaceFolderName, AutoInstallCli, ExcludedDirectories (for both .NET and Angular), ExecutablePath (for git and dotnet), and TimeoutSeconds for Git operations.
    [Fact]
    public void L2_7_1_Configuration_AllSettingsAreConfigurable()
    {
        var alacarte = new AlacarteOptions();
        alacarte.DefaultBranch = "custom";
        alacarte.SubmodulesFolderName = "custom";
        alacarte.SourceFolderName = "custom";

        var git = new GitOptions();
        git.ExecutablePath = "/custom/git";
        git.TimeoutSeconds = 600;

        var dotnet = new DotNetOptions();
        dotnet.ExecutablePath = "/custom/dotnet";
        dotnet.SolutionName = "CustomSolution";
        dotnet.ExcludedDirectories = new[] { "custom" };

        var angular = new AngularOptions();
        angular.WorkspaceFolderName = "CustomUi";
        angular.AutoInstallCli = false;
        angular.ExcludedDirectories = new[] { "custom" };

        // All properties should accept custom values without error
        alacarte.DefaultBranch.Should().Be("custom");
        git.TimeoutSeconds.Should().Be(600);
        dotnet.SolutionName.Should().Be("CustomSolution");
        angular.WorkspaceFolderName.Should().Be("CustomUi");
    }

    // L2-7.1 AC: All settings have documented defaults: branch=`main`, submodules folder=`submodules`, source folder=`src`, solution name=`Solution`, Angular workspace=`Ui`, auto-install CLI=`true`, excluded .NET dirs=`[obj, bin]`, excluded Angular dirs=`[node_modules, dist]`, git timeout=`300s`.
    [Fact]
    public void L2_7_1_Configuration_DefaultValues_MatchDocumentedDefaults()
    {
        var alacarte = new AlacarteOptions();
        alacarte.DefaultBranch.Should().Be("main");
        alacarte.SubmodulesFolderName.Should().Be("submodules");
        alacarte.SourceFolderName.Should().Be("src");

        var git = new GitOptions();
        git.ExecutablePath.Should().Be("git");
        git.TimeoutSeconds.Should().Be(300);

        var dotnet = new DotNetOptions();
        dotnet.ExecutablePath.Should().Be("dotnet");
        dotnet.SolutionName.Should().Be("Solution");
        dotnet.ExcludedDirectories.Should().BeEquivalentTo("obj", "bin");

        var angular = new AngularOptions();
        angular.WorkspaceFolderName.Should().Be("Ui");
        angular.AutoInstallCli.Should().BeTrue();
        angular.ExcludedDirectories.Should().BeEquivalentTo("node_modules", "dist");
    }

    #endregion

    #region L2-7.2 CLI Reproducibility

    // L2-7.2 AC: Given identical `--repos`, `--branch`, `--projects`, and `--folder` arguments, the resulting workspace contains the same submodules, projects, solution structure, and dependency rewiring.
    [Fact]
    public async Task L2_7_2_SameInputs_ProduceSameBehavior()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.GetFullPath(Arg.Any<string>()).Returns("/workspace");
        fileSystem.DirectoryExists("/workspace").Returns(false);

        var gitService = Substitute.For<IGitService>();
        var discoveryService = Substitute.For<IProjectDiscoveryService>();
        discoveryService.DiscoverDotNetProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "/workspace/submodules/repo/Project.csproj" });
        discoveryService.DiscoverAngularProjectsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AngularWorkspace>());

        var dotNetService = Substitute.For<IDotNetService>();
        var angularService = Substitute.For<IAngularService>();

        var handler = new InitCommandHandler(
            gitService, discoveryService, dotNetService, angularService,
            fileSystem, NullLogger<InitCommandHandler>.Instance);

        // Execute with explicit folder name
        var result = await handler.ExecuteAsync(
            new[] { "https://github.com/org/repo.git" },
            "main",
            "workspace",
            new[] { "Project" });

        result.Should().Be(0);

        // Verify deterministic calls: same repos, branch, folder, and filters
        await gitService.Received(1).AddSubmodulesAsync(
            "/workspace",
            Arg.Is<string[]>(a => a.Length == 1 && a[0] == "https://github.com/org/repo.git"),
            "main",
            Arg.Any<CancellationToken>());
        await discoveryService.Received(1).DiscoverDotNetProjectsAsync(
            "/workspace",
            Arg.Is<string[]?>(f => f != null && f.Contains("Project")),
            Arg.Any<CancellationToken>());
    }

    // L2-7.2 AC: The workspace folder name can be specified via `--folder` or auto-generated if omitted.
    [Fact]
    public async Task L2_7_2_FolderName_AutoGenerated_WhenOmitted()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.GetFullPath(Arg.Any<string>())
            .Returns(ci => "/resolved/" + ci.ArgAt<string>(0));
        fileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        var handler = new InitCommandHandler(
            Substitute.For<IGitService>(),
            Substitute.For<IProjectDiscoveryService>(),
            Substitute.For<IDotNetService>(),
            Substitute.For<IAngularService>(),
            fileSystem,
            NullLogger<InitCommandHandler>.Instance);

        // folder is null, should auto-generate
        await handler.ExecuteAsync(
            new[] { "https://github.com/org/repo.git" }, "main", null);

        fileSystem.Received(1).GetFullPath(Arg.Is<string>(s => s.StartsWith("alacarte-")));
    }

    // L2-7.2 AC: The workspace folder name can be specified via `--folder`.
    [Fact]
    public async Task L2_7_2_FolderName_UsesSpecifiedValue_WhenProvided()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.GetFullPath("my-workspace").Returns("/resolved/my-workspace");
        fileSystem.DirectoryExists("/resolved/my-workspace").Returns(false);

        var handler = new InitCommandHandler(
            Substitute.For<IGitService>(),
            Substitute.For<IProjectDiscoveryService>(),
            Substitute.For<IDotNetService>(),
            Substitute.For<IAngularService>(),
            fileSystem,
            NullLogger<InitCommandHandler>.Instance);

        await handler.ExecuteAsync(
            new[] { "https://github.com/org/repo.git" }, "main", "my-workspace");

        fileSystem.Received(1).GetFullPath("my-workspace");
        fileSystem.Received(1).CreateDirectory("/resolved/my-workspace");
    }

    #endregion
}
