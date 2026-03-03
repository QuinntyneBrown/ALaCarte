using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Services;
using FluentAssertions;

namespace ALaCarte.Core.UnitTests.Services;

public class FileSystemServiceTests
{
    private readonly FileSystemService _sut = new();

    [Fact]
    public void DirectoryExists_ReturnsFalse_ForNonexistentDirectory()
    {
        _sut.DirectoryExists("/nonexistent/path/that/does/not/exist").Should().BeFalse();
    }

    [Fact]
    public void DirectoryExists_ReturnsTrue_ForExistingDirectory()
    {
        _sut.DirectoryExists(Directory.GetCurrentDirectory()).Should().BeTrue();
    }

    [Fact]
    public void CreateDirectory_CreatesAndCleanup()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            _sut.CreateDirectory(tempDir);
            Directory.Exists(tempDir).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FileExists_ReturnsFalse_ForNonexistentFile()
    {
        _sut.FileExists("/nonexistent/file.txt").Should().BeFalse();
    }

    [Fact]
    public void Combine_JoinsPaths()
    {
        var result = _sut.Combine("a", "b", "c");
        result.Should().Be(Path.Combine("a", "b", "c"));
    }

    [Fact]
    public void GetFileName_ReturnsFileName()
    {
        _sut.GetFileName("/some/path/file.txt").Should().Be("file.txt");
    }

    [Fact]
    public void GetFileNameWithoutExtension_ReturnsName()
    {
        _sut.GetFileNameWithoutExtension("/some/path/file.txt").Should().Be("file");
    }

    [Fact]
    public void GetDirectoryName_ReturnsParentDirectory()
    {
        var result = _sut.GetDirectoryName(Path.Combine("some", "path", "file.txt"));
        result.Should().Contain("path");
    }

    [Fact]
    public void GetRelativePath_ReturnsRelative()
    {
        var basePath = Path.Combine("/", "base");
        var fullPath = Path.Combine("/", "base", "sub", "file.txt");
        var result = _sut.GetRelativePath(basePath, fullPath);
        result.Should().Be(Path.Combine("sub", "file.txt"));
    }

    [Fact]
    public void GetCurrentDirectory_ReturnsNonEmpty()
    {
        _sut.GetCurrentDirectory().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetFullPath_ReturnsAbsolutePath()
    {
        var result = _sut.GetFullPath("relative");
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public async Task ReadAllTextAsync_ReadsFileContent()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "test content");
            var result = await _sut.ReadAllTextAsync(tempFile);
            result.Should().Be("test content");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task WriteAllTextAsync_WritesContent()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await _sut.WriteAllTextAsync(tempFile, "written content");
            var result = await File.ReadAllTextAsync(tempFile);
            result.Should().Be("written content");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void CopyFile_CopiesFileContent()
    {
        var source = Path.GetTempFileName();
        var dest = Path.GetTempFileName();
        try
        {
            File.WriteAllText(source, "copy me");
            _sut.CopyFile(source, dest, true);
            File.ReadAllText(dest).Should().Be("copy me");
        }
        finally
        {
            File.Delete(source);
            File.Delete(dest);
        }
    }

    [Fact]
    public void GetFiles_ReturnsMatchingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "test.txt"), "");
            File.WriteAllText(Path.Combine(tempDir, "test.cs"), "");

            var result = _sut.GetFiles(tempDir, "*.txt", SearchOption.TopDirectoryOnly);
            result.Should().ContainSingle();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetDirectories_ReturnsSubdirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "sub1"));
        try
        {
            var result = _sut.GetDirectories(tempDir);
            result.Should().ContainSingle();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ImplementsIFileSystem()
    {
        _sut.Should().BeAssignableTo<IFileSystem>();
    }
}
