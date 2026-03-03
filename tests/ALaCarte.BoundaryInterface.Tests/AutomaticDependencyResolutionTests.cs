using System.Xml.Linq;
using ALaCarte.BoundaryInterface.Tests.Helpers;
using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ALaCarte.BoundaryInterface.Tests;

/// <summary>
/// Boundary interface tests for L2-3: Automatic Dependency Resolution [L1-3]
/// </summary>
public class AutomaticDependencyResolutionTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IProcessRunner _processRunner;
    private readonly DotNetService _sut;

    public AutomaticDependencyResolutionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"alacarte-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _processRunner = Substitute.For<IProcessRunner>();
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        // Use real FileSystemService for XML manipulation tests
        var fileSystem = new FileSystemService();

        _sut = new DotNetService(
            _processRunner,
            fileSystem,
            NullLogger<DotNetService>.Instance,
            TestOptionsFactory.CreateDotNetOptions(),
            TestOptionsFactory.CreateAlacarteOptions());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    #region L2-3.1 NuGet-to-ProjectReference Replacement

    // L2-3.1 AC: The system reads the `PackageId` element from each discovered `.csproj` file and builds a mapping of package ID to project file path.
    [Fact]
    public async Task L2_3_1_CreateSolution_ReadsPackageIdFromCsproj()
    {
        var projectDir = Path.Combine(_tempDir, "submodules", "repo", "LibA");
        Directory.CreateDirectory(projectDir);

        var csprojContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <PackageId>My.Custom.LibA</PackageId>
              </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(projectDir, "LibA.csproj"), csprojContent);

        var consumerDir = Path.Combine(_tempDir, "submodules", "repo", "Consumer");
        Directory.CreateDirectory(consumerDir);

        var consumerContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="My.Custom.LibA" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(consumerDir, "Consumer.csproj"), consumerContent);

        await _sut.CreateSolutionAsync(_tempDir, new List<string>
        {
            Path.Combine(projectDir, "LibA.csproj"),
            Path.Combine(consumerDir, "Consumer.csproj")
        });

        var resultCsproj = Path.Combine(_tempDir, "src", "Consumer", "Consumer.csproj");
        var doc = XDocument.Load(resultCsproj);
        doc.Descendants("ProjectReference").Should().NotBeEmpty();
    }

    // L2-3.1 AC: For each `PackageReference` whose `Include` attribute matches a known `PackageId`, the element is removed and a `ProjectReference` with the correct relative path is inserted.
    [Fact]
    public async Task L2_3_1_CreateSolution_ReplacesPackageReferenceWithProjectReference()
    {
        var libDir = Path.Combine(_tempDir, "submodules", "repo", "MyLib");
        Directory.CreateDirectory(libDir);
        File.WriteAllText(Path.Combine(libDir, "MyLib.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <PackageId>MyLib</PackageId>
              </PropertyGroup>
            </Project>
            """);

        var appDir = Path.Combine(_tempDir, "submodules", "repo", "MyApp");
        Directory.CreateDirectory(appDir);
        File.WriteAllText(Path.Combine(appDir, "MyApp.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="MyLib" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """);

        await _sut.CreateSolutionAsync(_tempDir, new List<string>
        {
            Path.Combine(libDir, "MyLib.csproj"),
            Path.Combine(appDir, "MyApp.csproj")
        });

        var resultCsproj = Path.Combine(_tempDir, "src", "MyApp", "MyApp.csproj");
        var doc = XDocument.Load(resultCsproj);

        doc.Descendants("PackageReference")
            .Where(e => e.Attribute("Include")?.Value == "MyLib")
            .Should().BeEmpty("PackageReference for MyLib should be removed");

        doc.Descendants("ProjectReference")
            .Should().ContainSingle("A ProjectReference to MyLib should be inserted");
    }

    // L2-3.1 AC: The `ItemGroup` for `ProjectReference` elements is created if it does not exist.
    [Fact]
    public async Task L2_3_1_CreateSolution_CreatesItemGroup_ForProjectReference_WhenNoneExists()
    {
        var libDir = Path.Combine(_tempDir, "submodules", "repo", "Lib");
        Directory.CreateDirectory(libDir);
        File.WriteAllText(Path.Combine(libDir, "Lib.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><PackageId>Lib</PackageId></PropertyGroup>
            </Project>
            """);

        var appDir = Path.Combine(_tempDir, "submodules", "repo", "App");
        Directory.CreateDirectory(appDir);
        File.WriteAllText(Path.Combine(appDir, "App.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Lib" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);

        await _sut.CreateSolutionAsync(_tempDir, new List<string>
        {
            Path.Combine(libDir, "Lib.csproj"),
            Path.Combine(appDir, "App.csproj")
        });

        var resultCsproj = Path.Combine(_tempDir, "src", "App", "App.csproj");
        var doc = XDocument.Load(resultCsproj);

        var projectRefItemGroup = doc.Descendants("ItemGroup")
            .Where(ig => ig.Elements("ProjectReference").Any());
        projectRefItemGroup.Should().NotBeEmpty();
    }

    // L2-3.1 AC: Projects that do not match any known `PackageId` retain their original `PackageReference` unchanged.
    [Fact]
    public async Task L2_3_1_CreateSolution_RetainsUnmatchedPackageReferences()
    {
        var appDir = Path.Combine(_tempDir, "submodules", "repo", "MyApp");
        Directory.CreateDirectory(appDir);
        File.WriteAllText(Path.Combine(appDir, "MyApp.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.0" />
              </ItemGroup>
            </Project>
            """);

        await _sut.CreateSolutionAsync(_tempDir, new List<string>
        {
            Path.Combine(appDir, "MyApp.csproj")
        });

        var resultCsproj = Path.Combine(_tempDir, "src", "MyApp", "MyApp.csproj");
        var doc = XDocument.Load(resultCsproj);

        doc.Descendants("PackageReference")
            .Where(e => e.Attribute("Include")?.Value == "Newtonsoft.Json")
            .Should().ContainSingle("Unmatched PackageReference should be retained");
    }

    #endregion

    #region L2-3.2 Relative Reference Stripping

    // L2-3.2 AC: `Reference` elements with paths starting with `..` are removed.
    [Fact]
    public async Task L2_3_2_CreateSolution_RemovesRelativeReferenceElements()
    {
        var projectDir = Path.Combine(_tempDir, "submodules", "repo", "MyProj");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(projectDir, "MyProj.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <Reference Include="..\..\lib\External.dll" />
                <Reference Include="LocalRef.dll" />
              </ItemGroup>
            </Project>
            """);

        await _sut.CreateSolutionAsync(_tempDir, new List<string>
        {
            Path.Combine(projectDir, "MyProj.csproj")
        });

        var resultCsproj = Path.Combine(_tempDir, "src", "MyProj", "MyProj.csproj");
        var doc = XDocument.Load(resultCsproj);

        doc.Descendants("Reference")
            .Where(e => e.Attribute("Include")?.Value?.StartsWith("..") == true)
            .Should().BeEmpty("Relative Reference elements should be removed");

        doc.Descendants("Reference")
            .Where(e => e.Attribute("Include")?.Value == "LocalRef.dll")
            .Should().ContainSingle("Local references should be preserved");
    }

    // L2-3.2 AC: `Compile`, `Content`, and `None` item elements with relative paths pointing outside the project are removed.
    [Fact]
    public async Task L2_3_2_CreateSolution_RemovesRelativeCompileContentNoneElements()
    {
        var projectDir = Path.Combine(_tempDir, "submodules", "repo", "Proj");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(projectDir, "Proj.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <Compile Include="..\..\shared\SharedCode.cs" />
                <Content Include="..\..\assets\image.png" />
                <None Include="..\..\config\settings.json" />
              </ItemGroup>
            </Project>
            """);

        await _sut.CreateSolutionAsync(_tempDir, new List<string>
        {
            Path.Combine(projectDir, "Proj.csproj")
        });

        var resultCsproj = Path.Combine(_tempDir, "src", "Proj", "Proj.csproj");
        var doc = XDocument.Load(resultCsproj);

        doc.Descendants("Compile").Where(e => e.Attribute("Include")?.Value?.StartsWith("..") == true)
            .Should().BeEmpty();
        doc.Descendants("Content").Where(e => e.Attribute("Include")?.Value?.StartsWith("..") == true)
            .Should().BeEmpty();
        doc.Descendants("None").Where(e => e.Attribute("Include")?.Value?.StartsWith("..") == true)
            .Should().BeEmpty();
    }

    // L2-3.2 AC: Absolute and local references are preserved.
    [Fact]
    public async Task L2_3_2_CreateSolution_PreservesAbsoluteAndLocalReferences()
    {
        var projectDir = Path.Combine(_tempDir, "submodules", "repo", "Proj2");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(projectDir, "Proj2.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <Compile Include="LocalFile.cs" />
                <Content Include="wwwroot\index.html" />
              </ItemGroup>
            </Project>
            """);

        await _sut.CreateSolutionAsync(_tempDir, new List<string>
        {
            Path.Combine(projectDir, "Proj2.csproj")
        });

        var resultCsproj = Path.Combine(_tempDir, "src", "Proj2", "Proj2.csproj");
        var doc = XDocument.Load(resultCsproj);

        doc.Descendants("Compile")
            .Where(e => e.Attribute("Include")?.Value == "LocalFile.cs")
            .Should().ContainSingle("Local Compile should be preserved");
        doc.Descendants("Content")
            .Where(e => e.Attribute("Include")?.Value == @"wwwroot\index.html")
            .Should().ContainSingle("Local Content should be preserved");
    }

    #endregion
}
