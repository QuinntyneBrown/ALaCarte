using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;

namespace ALaCarte.BoundaryInterface.Tests;

public class FileSystemCopyDirectoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sourceDir;
    private readonly string _destDir;
    private readonly IFileSystem _fileSystem;

    public FileSystemCopyDirectoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"alacarte-copydir-{Guid.NewGuid()}");
        _sourceDir = Path.Combine(_tempDir, "source");
        _destDir = Path.Combine(_tempDir, "dest");
        Directory.CreateDirectory(_sourceDir);

        _fileSystem = new FileSystemService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void CopyDirectory_CopiesFilesFromSourceToDestination()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(_sourceDir, "file2.cs"), "content2");

        _fileSystem.CopyDirectory(_sourceDir, _destDir);

        Assert.True(File.Exists(Path.Combine(_destDir, "file1.txt")));
        Assert.True(File.Exists(Path.Combine(_destDir, "file2.cs")));
        Assert.Equal("content1", File.ReadAllText(Path.Combine(_destDir, "file1.txt")));
        Assert.Equal("content2", File.ReadAllText(Path.Combine(_destDir, "file2.cs")));
    }

    [Fact]
    public void CopyDirectory_CopiesSubdirectoriesRecursively()
    {
        var subDir = Path.Combine(_sourceDir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested");

        _fileSystem.CopyDirectory(_sourceDir, _destDir);

        Assert.True(File.Exists(Path.Combine(_destDir, "sub", "nested.txt")));
        Assert.Equal("nested", File.ReadAllText(Path.Combine(_destDir, "sub", "nested.txt")));
    }

    [Fact]
    public void CopyDirectory_ExcludesObjDirectoryByDefault()
    {
        var objDir = Path.Combine(_sourceDir, "obj");
        Directory.CreateDirectory(objDir);
        File.WriteAllText(Path.Combine(objDir, "debug.txt"), "debug");
        File.WriteAllText(Path.Combine(_sourceDir, "main.cs"), "code");

        _fileSystem.CopyDirectory(_sourceDir, _destDir);

        Assert.True(File.Exists(Path.Combine(_destDir, "main.cs")));
        Assert.False(Directory.Exists(Path.Combine(_destDir, "obj")));
    }

    [Fact]
    public void CopyDirectory_ExcludesBinDirectoryByDefault()
    {
        var binDir = Path.Combine(_sourceDir, "bin");
        Directory.CreateDirectory(binDir);
        File.WriteAllText(Path.Combine(binDir, "app.dll"), "binary");

        _fileSystem.CopyDirectory(_sourceDir, _destDir);

        Assert.False(Directory.Exists(Path.Combine(_destDir, "bin")));
    }

    [Fact]
    public void CopyDirectory_ExcludesNodeModulesDirectoryByDefault()
    {
        var nmDir = Path.Combine(_sourceDir, "node_modules");
        Directory.CreateDirectory(nmDir);
        File.WriteAllText(Path.Combine(nmDir, "package.json"), "{}");

        _fileSystem.CopyDirectory(_sourceDir, _destDir);

        Assert.False(Directory.Exists(Path.Combine(_destDir, "node_modules")));
    }

    [Fact]
    public void CopyDirectory_ExcludesDistDirectoryByDefault()
    {
        var distDir = Path.Combine(_sourceDir, "dist");
        Directory.CreateDirectory(distDir);
        File.WriteAllText(Path.Combine(distDir, "bundle.js"), "js");

        _fileSystem.CopyDirectory(_sourceDir, _destDir);

        Assert.False(Directory.Exists(Path.Combine(_destDir, "dist")));
    }

    [Fact]
    public void CopyDirectory_WithCustomExcludedDirectories_ExcludesOnlyThose()
    {
        var objDir = Path.Combine(_sourceDir, "obj");
        var testDir = Path.Combine(_sourceDir, "test");
        Directory.CreateDirectory(objDir);
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(objDir, "debug.txt"), "debug");
        File.WriteAllText(Path.Combine(testDir, "test.txt"), "test");

        _fileSystem.CopyDirectory(_sourceDir, _destDir, excludedDirectories: ["test"]);

        // obj should be included (not in custom exclusion list)
        Assert.True(Directory.Exists(Path.Combine(_destDir, "obj")));
        // test should be excluded
        Assert.False(Directory.Exists(Path.Combine(_destDir, "test")));
    }

    [Fact]
    public void CopyDirectory_CreatesDestinationDirectoryIfNotExists()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "file.txt"), "content");

        _fileSystem.CopyDirectory(_sourceDir, _destDir);

        Assert.True(Directory.Exists(_destDir));
    }

    [Fact]
    public void CopyDirectory_HandlesEmptySourceDirectory()
    {
        _fileSystem.CopyDirectory(_sourceDir, _destDir);

        Assert.True(Directory.Exists(_destDir));
    }

    [Fact]
    public void CopyDirectory_HandlesDeepNesting()
    {
        var deepPath = Path.Combine(_sourceDir, "a", "b", "c");
        Directory.CreateDirectory(deepPath);
        File.WriteAllText(Path.Combine(deepPath, "deep.txt"), "deep");

        _fileSystem.CopyDirectory(_sourceDir, _destDir);

        Assert.True(File.Exists(Path.Combine(_destDir, "a", "b", "c", "deep.txt")));
    }

    [Fact]
    public void IFileSystem_HasCopyDirectoryMethod()
    {
        var method = typeof(IFileSystem).GetMethod("CopyDirectory");
        Assert.NotNull(method);

        var parameters = method.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal(typeof(string[]), parameters[2].ParameterType);
    }
}
